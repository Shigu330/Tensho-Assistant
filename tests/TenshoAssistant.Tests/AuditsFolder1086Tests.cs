// 0.1086 (audit A072, D-138): the work audits and plans left docs/evidence for docs/project/audits/.
// 0.1348 (X-357, the user's sort of 14/11): docs/project/audits/ is gone; the two audits the code cites are evidence again (R06), the
// others are removed (git history keeps them), and no tracked file names the old folder.
internal static class AuditsFolder1086Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Audits: the folder is gone, the two cited by the code are in docs/evidence",() =>
        {
            Check(!Directory.Exists(Path.GetFullPath("docs/project/audits")),"docs/project/audits is gone");
            foreach (var name in new[] { "0.857-community-map.md","0.874-probe-coverage.md" })
                Check(File.Exists(Path.GetFullPath("docs/evidence/" + name)),"in docs/evidence: " + name);
            foreach (var path in new[] { "src/TenshoAssistant/Market/SeenPrices.cs","src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs",
                                         "src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs","docs/README.md","docs/project/DECISIONS.md" })
                Check(!File.ReadAllText(Path.GetFullPath(path)).Replace("/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/audits/","").Contains("project/audits/"),path + " no longer names docs/project/audits/ (but by a permanent link)");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/SeenPrices.cs")).Contains("docs/evidence/0.857-community-map.md"),"the code cites the evidence's new place");
        }),
    ];
}
