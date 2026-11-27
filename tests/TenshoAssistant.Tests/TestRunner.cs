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
    public static readonly (string Test, DateOnly Until, string Reason)[] Entries =
    [
        // 0.1905 (X-882): « Fair lock 1309: behaviour: a client taking the lock again in a tight loop… » out of quarantine: its bound is
        // counted in the greedy client's turns, not on the clock.
        // 0.1904 (X-883): « GitHub token 1171 … (N8) » out of quarantine: its cleanup met github-merged.json's temporary file, written by the
        // exchange after Dispose returned; Dispose now waits for the work (GitHubUnload1904Tests reproduces it).
    ];
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
    /// <summary>X-889 M4 (0.1912): each test's start and end written to a file as they happen (TENSHO_TEST_TRACE, a sweep under load): a run
    /// whose whole process stops (27/11 on 0.1911: every thread waiting with no processor, the watch above included, killed after 16 min)
    /// prints none of its results; the file still says which tests were running.</summary>
    public static TestTrace? Trace { get; set; }

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
            Trace?.Line("BEGIN", name);
            try { run(); }
            catch (SkipTestException skip) { skipped = skip.Message; }
            catch (Exception failure) { error = failure.ToString(); }
            var seconds = Stopwatch.GetElapsedTime(started).TotalSeconds;
            Trace?.Line("END", name);
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

/// <summary>
/// X-889 M4 (review of 0.1909): what stops the test process during a run, measured instead of guessed. On 0.1908 one run of ten under load
/// failed six tests at once (bounds of 10 to 30 s passed); one of them waited 10 s for a raw thread (not one of the pool) to end a body of
/// two lines, and the tests' summed time was about 760 s above the other runs (about 24 s on each of the 32 threads): a stop of every
/// thread, not a pool kept busy. A thread of its own (above normal priority, so a busy processor alone does not delay it) wakes every
/// <see cref="Tick"/>: a late wake is a stop of the whole process or of the PC, with the garbage collector's pauses inside it
/// (GC.GetTotalPauseDuration). A work item put on the pool each <see cref="ProbeEvery"/> says how long the pool keeps a work waiting (the
/// pool starved by tests that block its threads). Said in one line after the slowest tests (« STALLS … »), kept with each run of a sweep under load.
/// </summary>
internal sealed class StallMeter : IDisposable
{
    public static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(200), ProbeEvery = TimeSpan.FromSeconds(1);
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Thread thread;
    private volatile bool stop;
    private int probing;
    public StallRecord Record { get; } = new();

    private StallMeter()
    {
        thread = new Thread(Watch) { IsBackground = true, Name = "stall meter", Priority = ThreadPriority.AboveNormal };
        thread.Start();
    }
    public static StallMeter Start() => new();

    private void Watch()
    {
        try
        {
            var last = clock.Elapsed;
            var paused = GC.GetTotalPauseDuration();
            var probe = TimeSpan.Zero;
            while (!stop)
            {
                Thread.Sleep(Tick);
                var now = clock.Elapsed;
                var pause = GC.GetTotalPauseDuration();
                Record.Wake(now - last - Tick, pause - paused, now, ThreadPool.PendingWorkItemCount, ThreadPool.ThreadCount);
                (last, paused) = (now, pause);
                if (now < probe || Interlocked.CompareExchange(ref probing, 1, 0) != 0) continue;
                probe = now + ProbeEvery;
                var waiting = ThreadPool.PendingWorkItemCount;
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    Record.Pool(clock.Elapsed - now, now, waiting);
                    Volatile.Write(ref probing, 0);
                }, null);
            }
        }
        catch (Exception failure) { Record.Failed(failure.GetType().Name); }
    }

    /// <summary>X-892 M3 (review of 0.1912): the meter asked to stop; true once its thread has ended within <paramref name="wait"/> (it records
    /// nothing more then), false when it still runs (a process stopped under load past the wait).</summary>
    public bool Stop(TimeSpan wait)
    {
        stop = true;
        return thread.Join(wait);
    }
    public void Dispose() => Stop(TimeSpan.FromSeconds(2));
}

/// <summary>X-889 M4: the longest late wake of the stall meter (with the collector's pauses inside it and the pool then) and the longest wait
/// of a work on the pool, each with its moment in the run.</summary>
internal sealed class StallRecord
{
    private readonly object gate = new();
    private TimeSpan late, lateAt, lateCollected, wait, waitAt;
    private long lateQueued, waitQueued;
    private int lateThreads, wakes, probes;
    private string? failure;

    public TimeSpan Late { get { lock (gate) return late; } }
    public TimeSpan PoolWait { get { lock (gate) return wait; } }
    public int Wakes { get { lock (gate) return wakes; } }
    public int Probes { get { lock (gate) return probes; } }

    public void Wake(TimeSpan behind, TimeSpan collected, TimeSpan at, long queued, int threads)
    {
        lock (gate)
        {
            wakes++;
            if (behind <= late) return;
            (late, lateAt, lateCollected, lateQueued, lateThreads) = (behind, at, collected, queued, threads);
        }
    }
    public void Pool(TimeSpan waited, TimeSpan at, long queued)
    {
        lock (gate)
        {
            probes++;
            if (waited <= wait) return;
            (wait, waitAt, waitQueued) = (waited, at, queued);
        }
    }
    public void Failed(string name) { lock (gate) failure = name; }

    public string Line()
    {
        static string Seconds(TimeSpan time) => time.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        lock (gate)
            return $"STALLS the watch thread woke at most {Seconds(late)} s late (at {Seconds(lateAt)} s of the run; garbage collection pauses in it "
                + $"{Seconds(lateCollected)} s; the pool then: {lateQueued} work(s) waiting, {lateThreads} thread(s)); a work on the pool waited at most "
                + $"{Seconds(wait)} s (at {Seconds(waitAt)} s; {waitQueued} waiting before it); {wakes} wakes, {probes} probes"
                + (failure is null ? "." : $"; the meter stopped on {failure}.");
    }
}

/// <summary>X-889 M4 (0.1912): the trace of a run, one line per test start and end (« seconds thread BEGIN|END name »), written at once (a
/// stop of the whole process leaves it as it was). Only when TENSHO_TEST_TRACE names the file.</summary>
internal sealed class TestTrace : IDisposable
{
    private readonly object gate = new();
    private readonly StreamWriter writer;
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private string? failure;
    public TestTrace(string path) : this(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) { }
    /// <summary>X-892 M4: the trace on any stream (the behaviour test gives one whose writes fail).</summary>
    internal TestTrace(Stream stream)
    {
        writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
    }
    /// <summary>X-892 M4 (review of 0.1912): why the trace stopped writing (the exception's type), null while it writes.</summary>
    public string? Failure { get { lock (gate) return failure; } }
    /// <summary>
    /// X-892 M4 (review of 0.1912): a write that fails (a full disk, a locked file) or comes after the end of the run (a test thread left
    /// behind) stops the trace, said once on the error output, and never throws: it was called outside the try that catches the tests'
    /// failures, so the raw test thread died of it and the whole process fell, without its results, in the very sweep the trace is for.
    /// </summary>
    public void Line(string what, string test)
    {
        var line = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{clock.Elapsed.TotalSeconds:0.000} {Environment.CurrentManagedThreadId} {what} {test}");
        lock (gate)
        {
            if (failure is not null) return;
            try { writer.WriteLine(line); }
            catch (Exception error) when (error is IOException or ObjectDisposedException or UnauthorizedAccessException)
            {
                failure = error.GetType().Name;
                Console.Error.WriteLine($"TRACE stopped: {failure} ({error.Message}); the tests go on without it.");
            }
        }
    }
    public void Dispose()
    {
        lock (gate)
        {
            failure ??= "closed";
            try { writer.Dispose(); } catch (Exception error) when (error is IOException or ObjectDisposedException or UnauthorizedAccessException) { }
        }
    }
}
