// 0.1818 (X-808) taken back in 0.1819: setting aside the common place of every fact a flagged player holds also dropped the place a counted
// player had brought when the flagged one was never counted (the merge tool's selftest fell on X-357, N13 and X-796 M1). X-808 stays open:
// only a player counted before can have brought the common place. Here: the behaviour of 0.1817 is back, and its cases are in the selftest.
internal static class DiscoveryExcluded1818Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery excluded 1819: the common place of a flagged player's facts is no longer set aside for every such player (X-808 open)",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            Check(!tool.Contains("tainted=frozenset(key(f) for p in placed",StringComparison.Ordinal) && tool.Contains("if any(owner in excluded for owner,_ in sources):placed=placed_with(places_of(excluded))",StringComparison.Ordinal),
                "no key set aside: the behaviour of 0.1817");
            foreach(var name in new[]{"X-357: the same facts again: the v3 set unchanged","N13: one file per account, the newest issue's","X-796 M1: the gist of a player left out gives neither place nor day"})
                Check(tool.Contains(name,StringComparison.Ordinal),"the tool's case kept: "+name);
            Check(!Read("docs/project/DECISIONS.md").Contains("Completed in 0.1818 (X-808",StringComparison.Ordinal) && !Read("community-discoveries/README.md").Contains("since Tensho 0.1818 the place",StringComparison.Ordinal),
                "DEC-052 and the README no longer say it");
        }),
    ];
}
