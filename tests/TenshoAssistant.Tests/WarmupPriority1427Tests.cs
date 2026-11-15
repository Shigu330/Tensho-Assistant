// 0.1427, X-433 (fix, docs/evidence/0.1427-warmup-priority.md): the JIT warm-up runs at normal priority. Below normal, it was preempted in the
// middle of a compilation (771 ms, 31 ms of it on its processor) while the game's thread and the collector waited for it.
using TenshoAssistant.Diagnostics;

internal static class WarmupPriority1427Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Warmup priority 1427: a real warm-up's thread runs at normal priority, under its name, in the background", () =>
        {
            ThreadPriority? priority = null;string? name = null;bool? background = null;
            using var warmup = JitWarmup.Start(() =>
            {
                priority = Thread.CurrentThread.Priority;name = Thread.CurrentThread.Name;background = Thread.CurrentThread.IsBackground;
                return [typeof(SlowCompilations)];
            }, TimeSpan.Zero);
            Check(warmup.Wait(TimeSpan.FromSeconds(30)), "finished");
            Check(priority == ThreadPriority.Normal, "normal priority: " + priority);
            Check(name == "Tensho JIT warm-up" && background == true, "its name, in the background");
        }),
        ("Warmup priority 1427: no thread of the plugin runs below normal; the evidence keeps the measures", () =>
        {
            foreach (var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories))
                Check(!File.ReadAllText(file).Contains("ThreadPriority.BelowNormal", StringComparison.Ordinal) && !File.ReadAllText(file).Contains("ThreadPriority.Lowest", StringComparison.Ordinal), Path.GetFileName(file));
            var evidence = Read("docs/evidence/0.1427-warmup-priority.md");
            foreach (var measure in new[] { "**771 ms dont 31 ms de processeur**", "la milliseconde où l'échauffement finit de compiler `MainWindow.DrawDiscoveryPlaces` (682 ms)", "**0 ms hors collecte**", "**515 ms avec 0 ms de processeur**" })
                Check(evidence.Contains(measure, StringComparison.Ordinal), measure);
            Check(Read("src/TenshoAssistant/Diagnostics/JitWarmup.cs").Contains("docs/evidence/0.1427-warmup-priority.md", StringComparison.Ordinal), "the code cites it");
        }),
    ];
}
