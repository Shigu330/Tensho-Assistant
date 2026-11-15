// 0.1425, X-433 step 4 (measure): the JIT warm-up keeps its five longest compilations with the clock time each ended, said with the readers'
// compiler's lines, to match the game thread's waits about 20 s after a load (the warm-up's rest starts when the readers' compiler ends).
using System.Text.RegularExpressions;
using TenshoAssistant.Diagnostics;

internal static class WarmupSlowest1425Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Warm-up slowest 1425: a real warm-up of a small type times its compilations", () =>
        {
            using var warmup = JitWarmup.Start(() => [typeof(SlowCompilations)], TimeSpan.Zero, measure: static () => true);
            Check(warmup.Wait(TimeSpan.FromSeconds(30)), "finished");
            Check(warmup.Prepared > 0 && Regex.IsMatch(warmup.Slowest, @"^SlowCompilations\.\S+ \d+ ms \((on the processor \d+ ms, )?ended \d\d:\d\d:\d\d\.\d{3}\)"), "timed: " + warmup.Slowest);
        }),
        ("Warm-up slowest 1425: every compilation of the warm-up goes through the timer; the plugin says them with the compiler's lines", () =>
        {
            var warmup = Read("src/TenshoAssistant/Diagnostics/JitWarmup.cs");
            Check(Regex.Matches(warmup, @"if\(Timed\(method,\(\)=>CallGraph\.Prepare\(method\)\)\)Prepared\+\+;else Skipped\+\+;").Count == 2, "the panels' and the window's parts");
            Check(warmup.Contains("try{Timed(method,()=>{RuntimeHelpers.PrepareMethod(method.MethodHandle);return true;});Prepared++;}", StringComparison.Ordinal), "the rest");
            Check(!Regex.IsMatch(warmup, @"(?<!=>)CallGraph\.Prepare\(method\)\)Prepared") && !warmup.Contains("try{RuntimeHelpers.PrepareMethod(method.MethodHandle);Prepared++;}", StringComparison.Ordinal), "none around the timer");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.IndexOf("log.Information(\"JIT warm-up's longest compilations so far: {0}.\",warmup);", StringComparison.Ordinal) > plugin.IndexOf("void SayReaders()", StringComparison.Ordinal), "said with the compiler's lines");
        }),
    ];
}
