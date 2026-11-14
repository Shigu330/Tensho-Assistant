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

internal sealed record TestResult(string Group, string Name, bool Passed, string? Error, double Seconds, string? Note);

internal sealed record TestRun(TestResult[] Results, string[] Notes, double Whole, int Threads)
{
    public int Total => Results.Length;
    public int Failed => Results.Count(result => !result.Passed) + Notes.Count(note => note.StartsWith("FAIL ", StringComparison.Ordinal));
    public int Passed => Results.Count(result => result.Passed);
    public IReadOnlyList<(double Seconds, string Name)> Times => Results.Select(result => (result.Seconds, result.Name)).ToList();
}

internal static class ParallelTests
{
    /// <summary>Runs <paramref name="groups"/>: the parallel ones on <paramref name="threads"/> threads (largest first), then the serial
    /// ones alone. Results come back in the order of the groups and of their tests, whatever order they ran in.</summary>
    public static TestRun Run(IReadOnlyList<TestGroup> groups, int threads,
        IReadOnlyList<(string Test, DateOnly Until, string Reason)> quarantine, DateOnly today, bool fullRun = true)
    {
        threads = Math.Max(1, threads);
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
        var results = new TestResult[groups.Count][];
        var whole = Stopwatch.StartNew();
        // The pool keeps enough threads for the tasks the tests wait on (a test waiting on Task.Run would otherwise wait for the pool to grow).
        ThreadPool.GetMinThreads(out var workers, out var ports);
        ThreadPool.SetMinThreads(Math.Max(workers, threads * 2), ports);
        var queue = new ConcurrentQueue<int>(Enumerable.Range(0, groups.Count).Where(index => !groups[index].Serial)
            .OrderByDescending(index => groups[index].Tests.Length).ThenBy(index => index));
        var runners = Enumerable.Range(0, Math.Min(threads, Math.Max(1, queue.Count))).Select(_ => new Thread(() =>
        {
            while (queue.TryDequeue(out var index)) results[index] = RunGroup(groups[index], held);
        }, 16 * 1024 * 1024) { IsBackground = true, Name = "tests" }).ToList();
        foreach (var runner in runners) runner.Start();
        foreach (var runner in runners) runner.Join();
        for (var index = 0; index < groups.Count; index++)
            if (groups[index].Serial) results[index] = RunGroup(groups[index], held);
        whole.Stop();
        return new TestRun(results.SelectMany(group => group).ToArray(), notes.ToArray(), whole.Elapsed.TotalSeconds, threads);
    }

    private static TestResult[] RunGroup(TestGroup group, IReadOnlyDictionary<string, (DateOnly Until, string Reason)> held)
    {
        var results = new TestResult[group.Tests.Length];
        for (var index = 0; index < group.Tests.Length; index++)
        {
            var (name, run) = group.Tests[index];
            var started = Stopwatch.GetTimestamp();
            string? error = null;
            try { run(); }
            catch (Exception failure) { error = failure.ToString(); }
            var seconds = Stopwatch.GetElapsedTime(started).TotalSeconds;
            if (error is not null && held.TryGetValue(name, out var quarantine))
                results[index] = new(group.Name, name, true, error, seconds,
                    $"QUARANTINE {name} (until {quarantine.Until:yyyy-MM-dd}: {quarantine.Reason}) failed this run, not counted as a failure: {FirstLine(error)}");
            else results[index] = new(group.Name, name, error is null, error, seconds, null);
        }
        return results;
    }

    private static string FirstLine(string text) => text.Split('\n')[0].TrimEnd('\r');

    /// <summary>
    /// The last line, read by the scripts (« N/M passed »). X-284 M4 (review of block 0.1278): a wrong quarantine entry fails the run while
    /// every test passed; the line then says the run failed and no longer reads « N/N passed » (the scripts' pattern does not match it).
    /// </summary>
    public static string CountLine(TestRun run)
    {
        var faults = run.Notes.Count(note => note.StartsWith("FAIL ", StringComparison.Ordinal));
        return faults == 0 ? $"{run.Passed}/{run.Total} passed"
            : $"{run.Passed}/{run.Total} tests passed, but the run FAILED: {faults} quarantine entr{(faults == 1 ? "y" : "ies")} wrong (FAIL lines above)";
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
