using TenshoAssistant.Inventory;

// X-422 M4 (0.1401, review of 0.1390 to 0.1397): the walk of the game's copy of the retainers' bags is bounded by its turns, the entries
// skipped included, not by the entries kept; and M5: the evidence keeps « order only, places to probe » for the bank's fallback.
internal static class RetainerFinderBound1401Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Retainer finder bound 1401: every turn of the walk counts, before an entry is skipped", () =>
        {
            var reader = Read("src/TenshoAssistant/Inventory/RetainerFinderReader.Dalamud.cs");
            var turn = reader.IndexOf("if(++turns>RetainerFinderCompare.MaximumEntries)return null;", StringComparison.Ordinal);
            var skip = reader.IndexOf("if(id==0 || pointer.Value==null)continue;", StringComparison.Ordinal);
            Check(turn > 0 && skip > turn, "the turn is counted before the skip");
            Check(!reader.Contains("found.Count>", StringComparison.Ordinal), "no bound on the entries kept alone");
            var bound = RetainerFinderCompare.MaximumEntries;
            Check(bound >= 10 && bound <= 64, "the bound stays above 10 retainers and small");
        }),
        ("Retainer finder bound 1401: the evidence says order only, places to probe, for the bank's fallback", () =>
        {
            var evidence = Read("docs/evidence/0.1394-retainer-finder.md");
            Check(evidence.Contains("order only, places still to probe", StringComparison.Ordinal)
                && !evidence.Contains("(X-275): yes", StringComparison.Ordinal), "the conclusion of the probe");
        }),
    ];
}
