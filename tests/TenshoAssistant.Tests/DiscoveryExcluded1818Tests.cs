// 0.1818 (X-808, review of the block 0.1803 to 0.1806, M5; user choice D-224 a): the common set's place of a fact a player left out holds
// (flagged, or an older issue of the same account) no longer counts. The behaviour (Ann and Bob counted, Bob flagged later: the middle of the
// cell, the fact and its days kept; rebuilt again the same; a counted gist read gives the place; an older issue keeps the same player's place)
// is in the merge tool's selftest (run before each version); here, the wiring and the documents.
internal static class DiscoveryExcluded1818Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery excluded 1818: the tool sets aside the common place of a left-out player's facts, with its behaviour cases",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            Check(tool.Contains("if owner is None and key(f) in tainted:common.setdefault(key(f),f);continue",StringComparison.Ordinal),"the common set's place of a tainted key set aside");
            Check(tool.Contains("places[k]={\"X\":(f[\"CellX\"]+0.5)*CELL,\"Z\":(f[\"CellZ\"]+0.5)*CELL,\"First\":f[\"First\"],\"Last\":f[\"Last\"]}",StringComparison.Ordinal),"the middle of the cell, the days kept");
            foreach(var name in new[]{"X-808: Bob flagged: the place is no longer his","X-808: rebuilt again: the same","X-808: a counted player's gist read now gives the place","X-808: an older issue of the same account"})
                Check(tool.Contains(name,StringComparison.Ordinal),"the tool's behaviour case: "+name);
            Check(Read("docs/project/DECISIONS.md").Contains("Completed in 0.1818 (X-808",StringComparison.Ordinal) && Read("docs/project/DECISIONS.md").Contains("Complétée en 0.1818 (X-808",StringComparison.Ordinal),"DEC-052, both halves");
            Check(Read("community-discoveries/README.md").Contains("since Tensho 0.1818 the place `v3/` holds",StringComparison.Ordinal),"the README");
        }),
    ];
}
