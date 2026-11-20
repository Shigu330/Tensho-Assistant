using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

// X-267 (0.1275, R36.2, method plan site 3): the test run in parallel. Measured before (13 November, 32 threads): 2323 tests one after
// the other in 106.1 s, the longest alone 10.9 s. Every static class with a static « Cases » (property or field) is a group found by itself
// (TestRegistry: a new class is never added to a list by hand, so none is forgotten); the tests written in Program.cs are one more
// group. Groups run at the same time on their own threads (not the thread pool: many tests wait on tasks, which need its threads);
// the tests of one group run in their order. A group marked [SerialTests] touches state shared by the whole process: it runs alone,
// after the others. Results are said in the order of the list, so the output reads as before; the count stays the last line.

/// <summary>X-267: the tests of this class touch state shared by the whole process (a static setting, the culture of every thread…):
/// they run alone, after the parallel part. The reason is written, as for a quarantine.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal sealed class SerialTestsAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}

/// <summary>
/// X-591 A25 (0.1611, external audit of 18/11): a test that cannot run on this system (a Windows-only channel) throws this from its first
/// line, « if(!OperatingSystem.IsWindows())throw Skip.Because(…); »: the run counts it apart (SKIP and its reason), never as passed.
/// </summary>
internal sealed class SkipTestException(string reason) : Exception(reason);

internal static class Skip
{
    public static SkipTestException Because(string reason) => new(reason);
}

/// <summary>A class of tests: its name, its tests in order, and whether it must run alone.</summary>
internal sealed record TestGroup(string Name, (string Name, Action Run)[] Tests, bool Serial);

internal static class TestRegistry
{
    public const string ProgramGroup = "Program";

    private const BindingFlags CasesFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    /// <summary>The « Cases » of a type: a static property or field (public or internal) holding its tests; null when it has none.</summary>
    public static (string Name, Action Run)[]? Cases(Type type) =>
        (type.GetProperty("Cases", CasesFlags)?.GetValue(null) ?? type.GetField("Cases", CasesFlags)?.GetValue(null)) is IEnumerable<(string, Action)> cases
            ? cases.ToArray() : null;

    /// <summary>The static classes of <paramref name="assembly"/> with a static « Cases » of tests, by name (ordinal).</summary>
    public static Type[] Classes(Assembly assembly) =>
        assembly.GetTypes()
            .Where(type => type.IsAbstract && type.IsSealed && !type.IsNested && Cases(type) is not null)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>The tests written in Program.cs first, then one group per class found.</summary>
    public static TestGroup[] Groups((string Name, Action Run)[] program, Assembly assembly) =>
        [new TestGroup(ProgramGroup, program, false),
         .. Classes(assembly).Select(type => new TestGroup(type.Name, Cases(type)!, type.GetCustomAttribute<SerialTestsAttribute>() is not null))];
}

/// <summary>X-267 (R36.1): a test that failed then passed on the same code is put here, with the day its quarantine ends and why. It still
/// runs and stays in the count; a failure is said (QUARANTINE) without failing the run. After its day, or naming no test, the entry fails.</summary>
internal static class Quarantine
{
    public static readonly (string Test, DateOnly Until, string Reason)[] Entries = [];
}

/// <summary>« --only TEXT » (0.1202) applied to each group; the groups left empty are dropped. A full run keeps them all.</summary>
internal static class GroupFilter
{
    public static TestGroup[] Only(IEnumerable<TestGroup> groups, string[] args) =>
        groups.Select(group => group with { Tests = TestFilter.Only(group.Tests, args) }).Where(group => group.Tests.Length > 0).ToArray();
    public static bool Full(string[] args) => !(args.Length == 2 && args[0] == "--only" && args[1].Length > 0);
}

internal sealed record TestResult(string Group, string Name, bool Passed, string? Error, double Seconds, string? Note, bool Skipped = false);

/// <param name="Hung">X-671: a test was left behind, still running (the process is ended by Program.cs, a stuck thread keeping it alive).</param>
internal sealed record TestRun(TestResult[] Results, string[] Notes, double Whole, int Threads, bool Hung = false)
{
    /// <summary>The tests that ran (X-591 A25: the skipped ones apart).</summary>
    public int Total => Results.Count(result => !result.Skipped);
    public int Failed => Results.Count(result => !result.Passed && !result.Skipped) + Notes.Count(note => note.StartsWith("FAIL ", StringComparison.Ordinal));
    public int Passed => Results.Count(result => result.Passed);
    public int Skipped => Results.Count(result => result.Skipped);
    public IReadOnlyList<(double Seconds, string Name)> Times => Results.Select(result => (result.Seconds, result.Name)).ToList();
}

internal static class ParallelTests
{
    /// <summary>X-671 (relay 146: a run under load stopped for good, every thread of the test process waiting with no processor, as Verify
    /// once in 0.1637): a test still running after this long is said hung (FAIL, with its name); the tests after it in its class are said not
    /// run, and the run goes on without its thread.</summary>
    public static readonly TimeSpan HangAfter = TimeSpan.FromMinutes(5);

    // X-671: what one thread runs now (its group and test, and since when), read by the watch.
    // X-673 M2 (review of 0.1643): and whether the watch left it behind, decided under the same lock as the start of a test: a thread left
    // behind, once its test comes back, begins nothing more (neither the next test of its class nor a class still in the queue), so it never
    // runs unwatched, beside the serial classes, or with its results read half way.
    private sealed class Slot
    {
        private readonly object gate = new();
        private int group = -1, test = -1;
        private long started;
        private bool left;
        public bool Begin(int group, int test)
        {
            lock (gate)
            {
                if (left) return false;
                this.group = group; this.test = test; started = Stopwatch.GetTimestamp();
                return true;
            }
        }
        public void Idle() { lock (gate) { group = -1; test = -1; } }
        public bool Left { get { lock (gate) return left; } }
        /// <summary>The test running past <paramref name="limit"/>: the thread marked left behind, its group and test given; else null.</summary>
        public (int Group, int Test, TimeSpan For)? Leave(TimeSpan limit)
        {
            lock (gate)
            {
                if (test < 0) return null;
                var running = Stopwatch.GetElapsedTime(started);
                if (running <= limit) return null;
                left = true;
                return (group, test, running);
            }
        }
    }

    /// <summary>Runs <paramref name="groups"/>: the parallel ones on <paramref name="threads"/> threads (largest first), then the serial
    /// ones alone, each on a thread of its own. Results come back in the order of the groups and of their tests, whatever order they ran in.
    /// X-671: a test running past <paramref name="hangAfter"/> (<see cref="HangAfter"/> by default) is said hung and left behind.
    /// X-673 M2: <paramref name="onLeft"/>, called by the watch each time a thread is left behind (the tests' own signal, never a delay).</summary>
    public static TestRun Run(IReadOnlyList<TestGroup> groups, int threads,
        IReadOnlyList<(string Test, DateOnly Until, string Reason)> quarantine, DateOnly today, bool fullRun = true, TimeSpan? hangAfter = null,
        Action? onLeft = null)
    {
        threads = Math.Max(1, threads);
        var limit = hangAfter ?? HangAfter;
        var notes = new List<string>();
        var known = groups.SelectMany(group => group.Tests).Select(test => test.Name).ToHashSet(StringComparer.Ordinal);
        var held = new Dictionary<string, (DateOnly Until, string Reason)>(StringComparer.Ordinal);
        foreach (var (test, until, reason) in quarantine)
        {
            if (string.IsNullOrWhiteSpace(reason)) notes.Add($"FAIL quarantine without a reason: {test}");
            else if (until < today) notes.Add($"FAIL quarantine ended on {until:yyyy-MM-dd}: {test} (fix it at its cause, or extend it with a new reason)");
            else if (fullRun && !known.Contains(test)) notes.Add($"FAIL quarantine names no test: {test}");
            else held[test] = (until, reason);
        }
        var results = groups.Select(group => new TestResult?[group.Tests.Length]).ToArray();
        var abandoned = new bool[groups.Count];
        var hung = new List<(int Group, int Test, TimeSpan For)>();
        var stop = false;
        var whole = Stopwatch.StartNew();
        // The pool keeps enough threads for the tasks the tests wait on (a test waiting on Task.Run would otherwise wait for the pool to grow).
        ThreadPool.GetMinThreads(out var workers, out var ports);
        ThreadPool.SetMinThreads(Math.Max(workers, threads * 2), ports);
        var queue = new ConcurrentQueue<int>(Enumerable.Range(0, groups.Count).Where(index => !groups[index].Serial)
            .OrderByDescending(index => groups[index].Tests.Length).ThenBy(index => index));
        var runners = Enumerable.Range(0, Math.Min(threads, Math.Max(1, queue.Count))).Select(_ =>
        {
            var slot = new Slot();
            var thread = new Thread(() =>
            {
                while (!Volatile.Read(ref stop) && !slot.Left && queue.TryDequeue(out var index)) RunGroup(groups, index, held, results, abandoned, slot);
            }, 16 * 1024 * 1024) { IsBackground = true, Name = "tests" };
            return (Thread: thread, Slot: slot);
        }).ToList();
        foreach (var runner in runners) runner.Thread.Start();
        Watch(runners, limit, abandoned, hung, onLeft);
        // X-671: what no thread took (every one left behind on a hung test) is never run by a thread that comes back later.
        Volatile.Write(ref stop, true);
        for (var index = 0; index < groups.Count; index++)
        {
            if (!groups[index].Serial) continue;
            var slot = new Slot();
            var serial = index;
            var thread = new Thread(() => RunGroup(groups, serial, held, results, abandoned, slot), 16 * 1024 * 1024) { IsBackground = true, Name = "tests alone" };
            thread.Start();
            Watch([(thread, slot)], limit, abandoned, hung, onLeft);
        }
        whole.Stop();
        var all = new List<TestResult>();
        for (var group = 0; group < groups.Count; group++)
            for (var test = 0; test < groups[group].Tests.Length; test++)
                all.Add(Left(groups, group, test, hung) ?? Volatile.Read(ref results[group][test]) ?? NotRun(groups, group, test, hung));
        return new TestRun(all.ToArray(), notes.ToArray(), whole.Elapsed.TotalSeconds, threads, hung.Count > 0);
    }

    // X-671: waits for the threads; one whose test runs past the limit is no longer waited for (its group marked, so that thread, if its test
    // ever ends, runs nothing more of it).
    private static void Watch(List<(Thread Thread, Slot Slot)> runners, TimeSpan limit, bool[] abandoned, List<(int Group, int Test, TimeSpan For)> hung,
        Action? onLeft)
    {
        var alive = new List<(Thread Thread, Slot Slot)>(runners);
        while (alive.Count > 0)
        {
            alive[0].Thread.Join(100);
            for (var at = alive.Count - 1; at >= 0; at--)
            {
                if (!alive[at].Thread.IsAlive) { alive.RemoveAt(at); continue; }
                if (alive[at].Slot.Leave(limit) is not { } stuck) continue;
                Volatile.Write(ref abandoned[stuck.Group], true);
                hung.Add(stuck);
                alive.RemoveAt(at);
                onLeft?.Invoke();
            }
        }
    }

    private static TestResult? Left(IReadOnlyList<TestGroup> groups, int group, int test, List<(int Group, int Test, TimeSpan For)> hung)
    {
        foreach (var (hungGroup, hungTest, running) in hung)
            if (hungGroup == group && hungTest == test)
                return new(groups[group].Name, groups[group].Tests[test].Name, false,
                    $"hung: still running after {running.TotalSeconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture)} s, its thread left behind (X-671); the tests after it in « {groups[group].Name} » not run",
                    running.TotalSeconds, null);
        return null;
    }

    private static TestResult NotRun(IReadOnlyList<TestGroup> groups, int group, int test, List<(int Group, int Test, TimeSpan For)> hung)
    {
        var before = hung.Where(entry => entry.Group == group).Select(entry => groups[group].Tests[entry.Test].Name).FirstOrDefault();
        return new(groups[group].Name, groups[group].Tests[test].Name, false,
            before is not null ? $"not run: « {before} » hung before it (X-671)" : "not run: every thread was left behind on a hung test (X-671)", 0, null);
    }

    private static void RunGroup(IReadOnlyList<TestGroup> groups, int index, IReadOnlyDictionary<string, (DateOnly Until, string Reason)> held,
        TestResult?[][] results, bool[] abandoned, Slot slot)
    {
        var group = groups[index];
        for (var at = 0; at < group.Tests.Length; at++)
        {
            if (Volatile.Read(ref abandoned[index]) || !slot.Begin(index, at)) break;
            var (name, run) = group.Tests[at];
            var started = Stopwatch.GetTimestamp();
            string? error = null;
            string? skipped = null;
            try { run(); }
            catch (SkipTestException skip) { skipped = skip.Message; }
            catch (Exception failure) { error = failure.ToString(); }
            var seconds = Stopwatch.GetElapsedTime(started).TotalSeconds;
            TestResult result;
            if (skipped is not null) result = new(group.Name, name, false, null, seconds, $"SKIP {name}: {skipped}", true);
            else if (error is not null && held.TryGetValue(name, out var quarantine))
                result = new(group.Name, name, true, error, seconds,
                    $"QUARANTINE {name} (until {quarantine.Until:yyyy-MM-dd}: {quarantine.Reason}) failed this run, not counted as a failure: {FirstLine(error)}");
            else result = new(group.Name, name, error is null, error, seconds, null);
            Volatile.Write(ref results[index][at], result);
        }
        slot.Idle();
    }

    private static string FirstLine(string text) => text.Split('\n')[0].TrimEnd('\r');

    /// <summary>
    /// The last line, read by the scripts (« N/M passed »). X-284 M4 (review of block 0.1278): a wrong quarantine entry fails the run while
    /// every test passed; the line then says the run failed and no longer reads « N/N passed » (the scripts' pattern does not match it).
    /// </summary>
    public static string CountLine(TestRun run)
    {
        var faults = run.Notes.Count(note => note.StartsWith("FAIL ", StringComparison.Ordinal));
        // X-591 A25 (0.1611): the skipped tests after the count, never in it.
        var skipped = run.Skipped == 0 ? "" : $", {run.Skipped} skipped (not run on this system, SKIP lines above)";
        return faults == 0 ? $"{run.Passed}/{run.Total} passed{skipped}"
            : $"{run.Passed}/{run.Total} tests passed{skipped}, but the run FAILED: {faults} quarantine entr{(faults == 1 ? "y" : "ies")} wrong (FAIL lines above)";
    }

    /// <summary>One line on the run itself: threads, groups, the serial ones, and the longest group (the floor of a parallel run).</summary>
    public static string Line(TestRun run, IReadOnlyList<TestGroup> groups)
    {
        var byGroup = run.Results.GroupBy(result => result.Group).Select(group => (Name: group.Key, Seconds: group.Sum(result => result.Seconds)))
            .OrderByDescending(group => group.Seconds).ThenBy(group => group.Name, StringComparer.Ordinal).FirstOrDefault();
        string Number(double seconds) => seconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        var serial = groups.Where(group => group.Serial).Select(group => group.Name).ToArray();
        return $"PARALLEL {groups.Count} classes on {run.Threads} threads in {Number(run.Whole)} s (tests alone: {Number(run.Results.Sum(result => result.Seconds))} s); "
            + $"alone after: {(serial.Length == 0 ? "none" : string.Join(", ", serial))}; longest class: {byGroup.Name} {Number(byGroup.Seconds)} s";
    }
}
