// X-91 (0.971): the last check of the parsers (docs/evidence/x91-parsers-check.md) found the S rank page without the FATE places seen (X-75.5).
internal static class ParsersCheck971Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Parsers check 971: report kept, S rank page shows the FATE places seen",() =>
        {
            Check(File.Exists(Path.GetFullPath("docs/evidence/x91-parsers-check.md")),"report");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.SRankConditions.cs"));
            Check(page.Contains("seenStore.Find(Discoveries.DiscoveryKind.FateSeen,fate.Id,3)") && page.Contains("\"SRank.FateSeen\""),"fate places");
        }),
    ];
}
