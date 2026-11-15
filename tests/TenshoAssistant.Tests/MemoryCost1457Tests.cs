using System.Diagnostics;
using TenshoAssistant.Caching;
using TenshoAssistant.Diagnostics;

// 0.1457 (X-453 M2, M4): the process's private memory read without listing the machine's processes; the garden rules' background build
// (BackgroundBuild): a line that comes first waits and is not lost, a failed build is said once, Dispose stops a build still running.
internal static class MemoryCost1457Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Memory cost 1457: the process's private memory, read on this process alone, agrees with the runtime's reading", () =>
        {
            if (!OperatingSystem.IsWindows()) return;
            var mine = MachineMemory.ProcessPrivateBytes();
            long runtime;
            using (var self = Process.GetCurrentProcess()) runtime = self.PrivateMemorySize64;
            Check(mine > 0, "read: " + mine);
            Check(Math.Abs(mine - runtime) <= Math.Max(64L * 1_048_576, runtime / 5), $"near the runtime's reading: {mine} against {runtime}");
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/MachineMemory.cs"));
            Check(!code.Contains(".PrivateMemorySize64;", StringComparison.Ordinal) && !code.Contains("Process.GetCurrentProcess()", StringComparison.Ordinal)
                && code.Contains("Native.GetProcessMemoryInfo(Native.GetCurrentProcess(),ref counters,counters.Size)", StringComparison.Ordinal), "no process list");
        }),
        ("Memory cost 1457: a line that comes before the background build ends waits for it and is not lost", () =>
        {
            using var release = new ManualResetEventSlim(false);
            var builds = 0;
            var build = new BackgroundBuild<string>(() => { Interlocked.Increment(ref builds); release.Wait(TimeSpan.FromSeconds(10)); return "rules"; }, _ => throw new Exception("no failure"));
            build.Start();
            var line = Task.Run(build.Get);
            Check(!line.Wait(150), "the line waits while the rules are built");
            release.Set();
            Check(line.Wait(5000) && line.Result == "rules", "then it gets them");
            Check(build.Get() == "rules" && Volatile.Read(ref builds) == 1, "built once: " + builds);
            var never = 0;
            var late = new BackgroundBuild<string>(() => { never++; return "now"; }, _ => { });
            Check(late.Get() == "now" && late.Get() == "now" && never == 1, "never started (garden off): built at the first line, once");
        }),
        ("Memory cost 1457: a failed build is said once, also to lines that come together", () =>
        {
            using var release = new ManualResetEventSlim(false);
            var said = 0;
            var build = new BackgroundBuild<string>(() => { release.Wait(TimeSpan.FromSeconds(10)); throw new InvalidOperationException("sheet"); }, _ => Interlocked.Increment(ref said));
            build.Start();
            var lines = Enumerable.Range(0, 4).Select(_ => Task.Run(build.Get)).ToArray();
            Thread.Sleep(50);
            release.Set();
            Check(Task.WaitAll(lines, 5000) && lines.All(line => line.Result is null), "no rules");
            Check(build.Get() is null && Volatile.Read(ref said) == 1, "said once: " + said);
        }),
        ("Memory cost 1457: Dispose stops a build still running; its result or its error is dropped", () =>
        {
            using var release = new ManualResetEventSlim(false);
            var said = 0;
            var build = new BackgroundBuild<string>(() => { release.Wait(TimeSpan.FromSeconds(10)); throw new InvalidOperationException("after unload"); }, _ => Interlocked.Increment(ref said));
            build.Start();
            Check(!build.Stop(TimeSpan.FromMilliseconds(50)), "still running after a short wait");
            release.Set();
            Thread.Sleep(100);
            Check(build.Get() is null && Volatile.Read(ref said) == 0, "nothing given, nothing said after the stop");
            var done = new BackgroundBuild<string>(() => "rules", _ => { });
            done.Start();
            Check(done.Get() == "rules" && done.Stop(TimeSpan.FromMilliseconds(10)), "a build already done: stopped at once");
            var watcher = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs"));
            var dispose = watcher.IndexOf("public void Dispose()", StringComparison.Ordinal);
            Check(dispose > 0 && watcher.IndexOf("rulesBuild.Stop(TimeSpan.FromSeconds(2));", dispose, StringComparison.Ordinal) > dispose, "the watcher's Dispose stops it");
        }),
    ];
}
