using System.Text.Json;

// 0.1801 (X-772, D-217, R12.2): the player files the repository keeps (community-discoveries/players) held each fact's exact place (X, Z) and its
// days (First, Last) next to the GitHub login: for a fish, where that account was on that day. They keep only what the merge needs now,
// compressed like the common files (<issue>.jsonl.gz); the common set keeps the earliest place and the days, without any name. The merge tool's
// behaviour (a player file produced holds neither place nor day; a file of before converted, the common set the same bytes) is in its selftest
// (tools/discoveries-merge.py --selftest, run before each version); here, the repository's own files and the wiring.
internal static class DiscoveryPlayers1801Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly HashSet<string> Kept = ["Kind","Subject","Detail","Territory","Map","CellX","CellZ","LevelMin","LevelMax","Hq","Values"];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery players 1801: every player file of the repository is of the new format and holds no place nor day, only the merge's fields",() =>
        {
            var players = RepositoryCommon.Players();
            Check(players.Count > 0,"player files read");
            foreach (var (file,header,facts) in players)
            {
                Check(file.EndsWith(".jsonl.gz",StringComparison.Ordinal) && header is not null,"converted by the merge tool (no <issue>.json left): " + file);
                var h = header!.Value;
                Check(h.GetProperty("Format").GetString() == "tensho-discoveries-player" && h.GetProperty("Version").GetInt32() == 1
                    && h.GetProperty("Issue").GetInt32().ToString() + ".jsonl.gz" == file && h.GetProperty("Count").GetInt32() == facts.Count,"its header: " + h);
                Check(h.EnumerateObject().All(member => member.Name is "Format" or "Version" or "Issue" or "Login" or "Count"),"nothing else in the header: " + h);
                var extra = facts.SelectMany(fact => fact.EnumerateObject().Select(member => member.Name)).Where(name => !Kept.Contains(name)).Distinct().ToList();
                Check(extra.Count == 0,file + ": fields a player file no longer keeps (X, Z, First, Last…): " + string.Join(", ",extra));
            }
        }),
        ("Discovery players 1801: the common set still has a place and days for each fact the players hold",() =>
        {
            var common = RepositoryCommon.Facts().ToDictionary(fact => fact.Key);
            var held = RepositoryCommon.Players().SelectMany(player => player.Facts)
                .Select(fact => string.Join(":",new[] { "Kind","Subject","Detail","Territory","Map","CellX","CellZ" }.Select(name => fact.GetProperty(name).GetRawText())))
                .Distinct().ToList();
            Check(held.Count > 0 && held.All(common.ContainsKey),"every player's fact in the common set: " + held.Count(key => !common.ContainsKey(key)) + " missing");
            Check(common.Values.All(fact => fact.First <= fact.Last && fact.First.Year == 2026),"each common fact keeps its days");
        }),
        ("Discovery players 1801: wiring: the tool writes a player file through player_bytes only, reads both formats, and the plugin never reads players/",() =>
        {
            var tool = Read("tools/discoveries-merge.py");
            Check(tool.Contains("PLAYER_ORDER=[\"Kind\",\"Subject\",\"Detail\",\"Territory\",\"Map\",\"CellX\",\"CellZ\",\"LevelMin\",\"LevelMax\",\"Hq\",\"Values\"]",StringComparison.Ordinal),
                "the fields a player file keeps");
            Check(tool.Contains("write_atomic(os.path.join(PLAYERS,f\"{issue}{PLAYER_SUFFIX}\"),player_bytes(",StringComparison.Ordinal)
                && !tool.Contains("write_json(path,{\"Issue\"",StringComparison.Ordinal) && !tool.Contains("write_json(path,player)",StringComparison.Ordinal),"written through player_bytes only");
            Check(tool.Contains("if not path.endswith(PLAYER_SUFFIX):",StringComparison.Ordinal) && tool.Contains("write_player(issue,player.get(\"Login\"),kept,None,now.get(name))",StringComparison.Ordinal),
                "a file of before still read, then written again in the new format");
            Check(tool.Contains("X-772: behaviour: the player file produced holds neither place nor day",StringComparison.Ordinal)
                && tool.Contains("X-772: rebuilt from the new file: the common set the same bytes",StringComparison.Ordinal),"the tool's behaviour cases");
            var sources = Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("community-discoveries/players",StringComparison.Ordinal)).ToList();
            // 0.1817 (X-804, user decision of 22/11): the plugin reads the header line of its own issue's player file (the gist merged), nothing else.
            Check(sources.Select(Path.GetFileName).SequenceEqual(["GistRotation.cs"]) && File.ReadAllText(sources[0]).Contains("public static (string Gist,string Revision)? Note(byte[] compressed)",StringComparison.Ordinal),
                "the plugin reads the common set (v3/), and of a player file only its own issue's header (X-804): " + string.Join(", ",sources.Select(Path.GetFileName)));
        }),
    ];
}
