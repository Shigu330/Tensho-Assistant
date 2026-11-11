// 0.1086 (audit A072, D-138): the work audits and plans left docs/evidence for docs/project/audits/, and no tracked file names their old path.
internal static class AuditsFolder1086Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly string[] Moved = ["x76-2bc-plan.md","x76-0-baseline.md","x76-9-comparison.md","x76-2-duplicates.md",
        "0.857-community-map.md","0.949-network-rules.md","0.874-probe-coverage.md"];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Audits: moved out of docs/evidence, old paths gone",() =>
        {
            foreach (var name in Moved)
            {
                Check(File.Exists(Path.GetFullPath("docs/project/audits/" + name)),"in docs/project/audits: " + name);
                Check(!File.Exists(Path.GetFullPath("docs/evidence/" + name)),"out of docs/evidence: " + name);
            }
            foreach (var path in new[] { "CHANGELOG.md","docs/project/DECISIONS.md","docs/project/GLOBAL-PLAN.md",
                                         "src/TenshoAssistant/Market/SeenPrices.cs","src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs" })
            {
                var text = File.ReadAllText(Path.GetFullPath(path));
                foreach (var name in Moved)
                    Check(!text.Contains("docs/evidence/" + name),path + " still names docs/evidence/" + name);
            }
            Check(File.ReadAllText(Path.GetFullPath("docs/README.md")).Contains("[audits/](project/audits/)"),"audits/ is in the documents' map");
        }),
    ];
}
