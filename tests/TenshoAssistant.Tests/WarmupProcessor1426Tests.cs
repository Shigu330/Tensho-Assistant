// 0.1426, X-433 step 5 (measure): the JIT warm-up's longest compilations say the warm-up thread's own processor time, to tell a compilation
// that works from one that waits (a below-normal thread preempted while the game's thread waits for it).
using System.Text.RegularExpressions;
using TenshoAssistant.Diagnostics;

internal static class WarmupProcessor1426Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset At = new(2026, 11, 15, 15, 38, 58, TimeSpan.FromHours(1));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Warmup processor 1426: a compilation's processor time is said when known, the old form otherwise", () =>
        {
            var slow = new SlowCompilations(2);
            slow.Note("MainWindow.DrawDiscoveryPlaces", 682, At, 15.6);
            slow.Note("ReaderCompiler.Other", 40, At);
            var text = slow.Text();
            Check(text.StartsWith("MainWindow.DrawDiscoveryPlaces 682 ms (on the processor 16 ms, ended ", StringComparison.Ordinal), text);
            Check(Regex.IsMatch(text, @", ReaderCompiler\.Other 40 ms \(ended \d\d:\d\d:\d\d\.\d{3}\)$"), "without it, as before: " + text);
        }),
        ("Warmup processor 1426: a real warm-up says its compilations' processor time on Windows", () =>
        {
            using var warmup = JitWarmup.Start(() => [typeof(SlowCompilations)], TimeSpan.Zero);
            Check(warmup.Wait(TimeSpan.FromSeconds(30)), "finished");
            if (OperatingSystem.IsWindows()) Check(warmup.Slowest.Contains(" ms (on the processor ", StringComparison.Ordinal), "the processor time: " + warmup.Slowest);
            Check(Read("src/TenshoAssistant/Diagnostics/JitWarmup.cs").Contains("var processorBefore=ThreadClock.ProcessorMs();\n        var compiled=compile();", StringComparison.Ordinal), "measured around the compilation");
        }),
    ];
}
