// 0.1818 (X-808) taken back in 0.1819: setting aside the common place of every fact a flagged player holds also dropped the place a counted
// player had brought when the flagged one was never counted. 0.1821 (X-808 done, D-224 a as the master set it on 22/11): only a player flagged
// after a rebuild counted them (« Counted » in their file); the behaviour is in the merge tool's selftest, wired here.
internal static class DiscoveryExcluded1818Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery excluded 1821: the common place set aside only for a player flagged after being counted (X-808)",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            Check(tool.Contains("tainted=frozenset(k for p in placed if p[0] in left and was.get(p[0]) is True for k in keys_of(p[4]))",StringComparison.Ordinal)
                && tool.Contains("if tainted or any(owner in excluded for owner,_ in sources):placed=placed_with(places_of(excluded,tainted))",StringComparison.Ordinal),
                "only a flagged player counted before taints its keys");
            Check(tool.Contains("**({\"Counted\":counted} if counted is not None else {})",StringComparison.Ordinal),"« Counted » written in the player file");
            foreach(var name in new[]{"X-357: the same facts again: the v3 set unchanged","N13: one file per account, the newest issue's","X-796 M1: the gist of a player left out gives neither place nor day",
                "X-808: Bob flagged after being counted","X-808: an older issue of the same account is the same player"})
                Check(tool.Contains(name,StringComparison.Ordinal),"the tool's case: "+name);
            Check(Read("docs/project/DECISIONS.md").Contains("Completed in 0.1821 (X-808",StringComparison.Ordinal) && Read("community-discoveries/README.md").Contains("Since Tensho 0.1821, a player flagged after a rebuild counted them",StringComparison.Ordinal),
                "DEC-052 and the README say it");
        }),
    ];
}
