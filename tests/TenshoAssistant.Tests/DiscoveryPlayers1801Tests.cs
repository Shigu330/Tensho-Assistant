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
            // X-826 (0.1827): the header's fields are those the merge tool writes (PLAYER_HEADER: Gist and Revision of X-804, Counted of X-808 too),
            // each of its own shape, and nothing else (DiscoveryHeader1827Tests.Header); an anonymous withdrawn file (X-803) passes the same rule.
            var allowed = DiscoveryHeader1827Tests.ToolHeader();
            foreach (var (file,header,facts) in players)
            {
                Check(file.EndsWith(".jsonl.gz",StringComparison.Ordinal) && header is not null,"converted by the merge tool (no <issue>.json left): " + file);
                DiscoveryHeader1827Tests.Header(file,header!.Value,facts.Count,allowed);
                var extra = facts.SelectMany(fact => fact.EnumerateObject().Select(member => member.Name)).Where(name => !Kept.Contains(name)).Distinct().ToList();
                Check(extra.Count == 0,file + ": fields a player file no longer keeps (X, Z, First, Last…): " + string.Join(", ",extra));
            }
            // X-830 (D-226 a): no file named after an issue (players/43.json of before X-772 and 43.jsonl.gz of before X-830 gone), the gists
            // merged listed apart (hashed), and the private link issue ↔ file never in the repository.
            var folder = Path.GetFullPath("community-discoveries");
            Check(!Directory.GetFiles(Path.Combine(folder,"players")).Any(path => char.IsDigit(Path.GetFileName(path)[0]) && Path.GetFileName(path).Split('.')[0].All(char.IsDigit)),
                "no player file named after its issue");
            Check(File.ReadAllText(Path.Combine(folder,"merged.txt")).Replace("\r\n","\n").Split('\n',StringSplitOptions.RemoveEmptyEntries) is { Length:>0 } merged
                && merged[0] == TenshoAssistant.Discoveries.GistRotation.NotesHeader && merged.Skip(1).All(line => System.Text.RegularExpressions.Regex.IsMatch(line,"^[0-9a-f]{64}$")),
                "merged.txt: its header, then hashes only");
            Check(!Directory.GetFiles(Path.GetFullPath("."),"*links*.json",SearchOption.AllDirectories).Any(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && File.ReadAllText(path).Contains("tensho-discoveries-links",StringComparison.Ordinal)),
                "the private link file is never in the repository");
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
            Check(tool.Contains("write_atomic(os.path.join(PLAYERS,f\"{name}{PLAYER_SUFFIX}\"),player_bytes(",StringComparison.Ordinal)
                && !tool.Contains("write_json(path,{\"Issue\"",StringComparison.Ordinal) && !tool.Contains("write_json(path,player)",StringComparison.Ordinal),"written through player_bytes only");
            Check(tool.Contains("if not path.endswith(PLAYER_SUFFIX):",StringComparison.Ordinal) && tool.Contains("write_player(issue,kept,now.get(name))",StringComparison.Ordinal),
                "a file of before still read, then written again in the new format");
            Check(tool.Contains("X-772: behaviour: the player file produced holds neither place nor day",StringComparison.Ordinal)
                && tool.Contains("X-772: rebuilt from the new file: the common set the same bytes",StringComparison.Ordinal),"the tool's behaviour cases");
            var sources = Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("community-discoveries/players",StringComparison.Ordinal)).ToList();
            // 0.1817 (X-804): the plugin read the header line of its own issue's player file (the gist merged). X-830: it reads no player file at
            // all, only the list of gists merged (merged.txt), where it looks for the hash of its own last upload.
            var rotation = Read("src/TenshoAssistant/Discoveries/GistRotation.cs");
            Check(sources.Count == 0 && rotation.Contains("community-discoveries/merged.txt",StringComparison.Ordinal)
                && rotation.Contains("public static (string Gist,string Revision)? Note(byte[] list,string gist,string revision)",StringComparison.Ordinal),
                "the plugin reads the common set (v3/) and merged.txt, never a player file: " + string.Join(", ",sources.Select(Path.GetFileName)));
        }),
    ];
}
