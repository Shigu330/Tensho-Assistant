// 0.1424, X-433 step 3 (measure): the readers' compiler keeps its five longest compilations with the clock time each ended, said with its
// other lines in developer mode, to compare with the time of a reader's hitch.
using System.Reflection;
using TenshoAssistant.Diagnostics;

internal static class SlowCompilations1424Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset At = new(2026, 11, 15, 15, 12, 10, TimeSpan.FromHours(1));

    private static void Sample() { }
    private static void Other() { }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Slow compilations 1424: the five longest kept, longest first, each with the time it ended", () =>
        {
            var slow = new SlowCompilations(3);
            Check(slow.Text() == "", "nothing before any");
            foreach (var (name, ms) in new[] { ("A", 10.0), ("B", 300.0), ("C", 5.0), ("D", 120.0), ("E", 40.0) })
                slow.Note(name, ms, At.AddMilliseconds(ms));
            var text = slow.Text();
            Check(text.StartsWith("B 300 ms (ended ", StringComparison.Ordinal) && text.Contains(", D 120 ms (ended ", StringComparison.Ordinal) && text.Contains(", E 40 ms (ended ", StringComparison.Ordinal), text);
            Check(!text.Contains("A 10", StringComparison.Ordinal) && !text.Contains("C 5", StringComparison.Ordinal), "only the three longest");
            Check(text.Contains(At.AddMilliseconds(300).ToLocalTime().ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal), "the clock time it ended");
        }),
        ("Slow compilations 1424: the warm-up times each compilation it makes, and only those", () =>
        {
            var timed = new List<(MethodBase, double)>();
            var handler = typeof(SlowCompilations1424Tests).GetMethod(nameof(Sample), BindingFlags.NonPublic | BindingFlags.Static)!;
            var prepared = JitWarmup.PrepareOwner(handler, prepare: _ => Thread.Sleep(2), timed: (method, ms) => timed.Add((method, ms)));
            Check(prepared > 0 && timed.Count == prepared, "one time per compilation: " + prepared + " " + timed.Count);
            Check(timed.All(pair => pair.Item2 >= 1), "each one's time");
            var plain = JitWarmup.PrepareOwner(handler, prepare: _ => { });
            Check(plain == prepared, "without the timer, the same compilations");
        }),
        ("Slow compilations 1424: wired: the readers' compiler times its warm-up, the plugin says the longest with the compiler's lines", () =>
        {
            var compiler = Read("src/TenshoAssistant/Diagnostics/ReaderCompiler.cs");
            Check(compiler.Contains("(method,milliseconds)=>slowest.Note(SlowCompilations.Name(method),milliseconds,DateTimeOffset.Now));", StringComparison.Ordinal), "the compiler notes them");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.IndexOf("log.Information(\"Readers' longest compilations: {0}.\",slowest);", StringComparison.Ordinal) > plugin.IndexOf("void SayReaders()", StringComparison.Ordinal), "said with the compiler's lines");
        }),
    ];
}
