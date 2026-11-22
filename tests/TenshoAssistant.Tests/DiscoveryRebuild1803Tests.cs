// 0.1803 (X-795, review of 0.1802, I1; X-796 M1): since X-772 the place and days of a common fact live in the common set only (v3/), beside
// the gists sync reads. A rebuild that read v3/ missing or cut short wrote a common set without them and removed the kind files; a player left
// out (FLAG) still gave places and days through a gist or a file of before. The merge tool's behaviour on real files (index missing, index cut
// short, kind file missing, an index and its file gone compared with git HEAD, withdraw stopped with its player file kept; the left-out
// player's gist and file of before) is in its selftest (tools/discoveries-merge.py --selftest, run before each version); here,
// the repository's own set and the wiring.
internal static class DiscoveryRebuild1803Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery rebuild 1803: the repository's common set is whole: every kind file listed in the index, and listed files there",() =>
        {
            var folder = Path.GetFullPath("community-discoveries/v3");
            var index = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"index.json")));
            var listed = index.RootElement.GetProperty("Files").EnumerateArray().Select(entry => entry.GetProperty("Name").GetString()!).ToHashSet(StringComparer.Ordinal);
            var present = Directory.GetFiles(folder,"kind-*.jsonl.gz").Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
            Check(listed.Count > 0 && listed.SetEquals(present!),"index and folder agree: " + string.Join(", ",listed.Except(present!).Concat(present!.Except(listed))));
        }),
        ("Discovery rebuild 1803: wiring: rebuild reads the common set whole first, sync and withdraw guarded, a stop writes nothing and exits 2",() =>
        {
            var tool = Read("tools/discoveries-merge.py");
            Check(tool.Contains("    current=v3_guard()\n    sources=",StringComparison.Ordinal),"rebuild starts by reading the common set whole");
            Check(tool.Contains("past=head_v3_keys() if history is None else history()",StringComparison.Ordinal),"compared with the common set of git HEAD");
            Check(tool.Contains("    try:v3_guard()\n    except Lossy as error:",StringComparison.Ordinal),"sync checks it before writing a player file");
            Check(tool.Contains("    rebuild(skip=[os.path.basename(path) for path in present],fresh=[(name,f) for f in facts if all(n in f for n in PLACE)],pending={name:(name,None,facts)})\n    for path in present:os.remove(path)",StringComparison.Ordinal),
                "withdraw removes the player file once the common set is written");
            Check(tool.Contains("changed=write_v3(bounded(common[\"Facts\"],say=say),V3_DIR,say)\n    for write in later:write()",StringComparison.Ordinal),"the player files written after the common set");
            Check(tool.Contains("print(f\"STOP: {error}; nothing written\");sys.exit(2)",StringComparison.Ordinal),"the stop said, exit 2");
            Check(tool.Contains("excluded=left|{p[0] for p in placed if p[0] not in counted}",StringComparison.Ordinal),"X-796 M1: a left-out player's places taken out");
            Check(!tool.Contains("from community-discoveries/players/*.json.",StringComparison.Ordinal),"the help no longer says the set comes from the player files alone");
            foreach (var name in new[] { "X-795: v3/index.json missing: rebuild stops","X-795: withdraw stops too, the player file kept",
                "X-795: an index listing fewer kind files than the folder holds","X-795: an index cut short with its file gone",
                "X-795: a kind file listed in the index but missing","X-796 M1: the gist of a player left out gives neither place nor day",
                "X-796 M1: nor their file of before" })
                Check(tool.Contains(name,StringComparison.Ordinal),"the tool's behaviour case: " + name);
        }),
    ];
}
