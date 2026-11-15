// 0.1427, X-433 (change kept, cause not proven: docs/evidence/0.1427-warmup-priority.md): the JIT warm-up runs at normal priority, to remove
// one possible cause of the hitches (the thread preempted in the middle of a compilation the game's thread waits on). 0.1428 (X-436): this
// header said « fix ». 0.1432: the hypothesis disproved (docs/evidence/0.1432-background-collection-stall.md), the warm-up back to below
// normal priority; these cases check it, and that it is the only thread of the plugin below normal.
using TenshoAssistant.Diagnostics;

internal static class WarmupPriority1427Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Warmup priority 1427 (0.1432): a real warm-up's thread runs below normal priority, under its name, in the background", () =>
        {
            ThreadPriority? priority = null;string? name = null;bool? background = null;
            using var warmup = JitWarmup.Start(() =>
            {
                priority = Thread.CurrentThread.Priority;name = Thread.CurrentThread.Name;background = Thread.CurrentThread.IsBackground;
                return [typeof(SlowCompilations)];
            }, TimeSpan.Zero);
            Check(warmup.Wait(TimeSpan.FromSeconds(30)), "finished");
            Check(priority == ThreadPriority.BelowNormal, "below normal priority (0.1432): " + priority);
            Check(name == "Tensho JIT warm-up" && background == true, "its name, in the background");
        }),
        ("Warmup priority 1427 (0.1432): only the warm-up runs below normal; the evidence keeps the measures", () =>
        {
            foreach (var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories))
                Check(Path.GetFileName(file) == "JitWarmup.cs" || !File.ReadAllText(file).Contains("ThreadPriority.BelowNormal", StringComparison.Ordinal) && !File.ReadAllText(file).Contains("ThreadPriority.Lowest", StringComparison.Ordinal), Path.GetFileName(file));
            var evidence = Read("docs/evidence/0.1427-warmup-priority.md");
            foreach (var measure in new[] { "**771 ms dont 31 ms de processeur**", "la milliseconde où l'échauffement finit de compiler `MainWindow.DrawDiscoveryPlaces` (682 ms)", "**0 ms hors collecte**", "**515 ms avec 0 ms de processeur**" })
                Check(evidence.Contains(measure, StringComparison.Ordinal), measure);
            Check(Read("src/TenshoAssistant/Diagnostics/JitWarmup.cs").Contains("docs/evidence/0.1427-warmup-priority.md", StringComparison.Ordinal), "the code cites it");
        }),
    ];
}
