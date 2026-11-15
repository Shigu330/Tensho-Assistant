using System.Text.RegularExpressions;

// X-304 (0.1392, user's decision of 14/11, idea I-009): the table of Dalamud's rules in PUBLISHING.md, one row per rule with its proof, in
// both halves; and the code keeps what the rows say: the functions that act on the game stay in the files named, the absent ones stay absent.
// A new site is not wrong in itself: it is checked against the rules, then its row and this list change in the same version.
internal static class DalamudRules1392Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private const int Rules = 22;

    /// <summary>The rows (number → cells) of the table under a heading of a Markdown text.</summary>
    internal static Dictionary<int, string[]> Rows(string text, string heading)
    {
        var at = text.IndexOf("\n" + heading + "\n", StringComparison.Ordinal);
        var rows = new Dictionary<int, string[]>();
        if (at < 0) return rows;
        var end = text.IndexOf("\n## ", at + heading.Length + 2, StringComparison.Ordinal);
        foreach (var line in text[at..(end < 0 ? text.Length : end)].Split('\n'))
        {
            var match = Regex.Match(line, @"^\| (\d+) \|(.*)\|$");
            if (match.Success) rows[int.Parse(match.Groups[1].Value)] = match.Groups[2].Value.Split('|').Select(cell => cell.Trim()).ToArray();
        }
        return rows;
    }

    private static IEnumerable<string> Sources() =>
        Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));

    private static string Name(string path) => Path.GetRelativePath(Path.GetFullPath("src/TenshoAssistant"), path).Replace('\\', '/');

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Dalamud rules 1392: every rule has its row and its proof in both halves of PUBLISHING.md, the files named exist", () =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/PUBLISHING.md")).Replace("\r\n", "\n");
            foreach (var heading in new[] { "## Dalamud rules, one by one", "## Règles de Dalamud, une par une" })
            {
                var rows = Rows(text, heading);
                Check(rows.Count == Rules && Enumerable.Range(1, Rules).All(rows.ContainsKey), heading + ": rows 1 to " + Rules + " (found " + rows.Count + ")");
                foreach (var (number, cells) in rows)
                {
                    Check(cells.Length == 3 && cells.All(cell => cell.Length > 0 && cell != "—"), heading + " row " + number + ": rule, how, proof");
                    foreach (Match path in Regex.Matches(cells[2], @"`((?:src|tests)/[^`]+)`"))
                        Check(File.Exists(Path.GetFullPath(path.Groups[1].Value)), heading + " row " + number + ": " + path.Groups[1].Value + " exists");
                    foreach (Match test in Regex.Matches(cells[2], @"`([A-Za-z]+\d+Tests)`"))
                        Check(File.Exists(Path.GetFullPath("tests/TenshoAssistant.Tests/" + test.Groups[1].Value + ".cs")), heading + " row " + number + ": test " + test.Groups[1].Value);
                }
            }
        }),
        ("Dalamud rules 1392: the functions that act on the game stay in the files the table names", () =>
        {
            var allowed = new Dictionary<string, string[]>
            {
                ["ProcessChatBoxEntry"] = ["Alarms/AlarmService.Dalamud.cs", "Maps/HuntAnnouncer.Dalamud.cs"],   // rules 1 and 9: player's click only
                ["SendTitleIdUpdate"] = ["Collections/TitleChanger.Dalamud.cs"],                                    // rule 1: DEC-035
                ["Teleport("] = ["MainWindow.ChatMapLinks.cs", "MainWindow.MapView.cs", "Maps/MapLiveReader.Dalamud.cs"],   // rule 1: on click
                ["UseAction"] = ["Craft/CraftRecorder.Dalamud.cs", "Plugin.cs"],                                    // rules 12 and 20: watched only
                ["ReceiveEvent"] = ["Maps/ChatLinkHoverWatcher.Dalamud.cs"],                                         // the chat link's hover, read
            };
            var sources = Sources().Select(path => (Name: Name(path), Text: File.ReadAllText(path))).ToList();
            foreach (var (token, files) in allowed)
            {
                var found = sources.Where(source => source.Text.Contains(token, StringComparison.Ordinal)).Select(source => source.Name).Order().ToArray();
                Check(found.SequenceEqual(files.Order()), token + ": in " + string.Join(", ", found) + " (check the rules, then PUBLISHING.md and this list)");
            }
        }),
        ("Dalamud rules 1392: what the table says absent is absent from the code", () =>
        {
            // rules 3, 4, 6, 10, 11, 13, 14, 16, 17
            string[] absent = ["BuyItem", "PurchaseItem", "fflogs", "GetIpcSubscriber", "FireCallback", "AddonTalk", "SelectYesno", "RollLoot", "NeedGreed",
                "InfoProxyFriendList", "CameraManager"];
            var found = Sources().SelectMany(path => absent.Where(token => File.ReadAllText(path).Contains(token, StringComparison.OrdinalIgnoreCase))
                .Select(token => Name(path) + ": " + token)).ToList();
            Check(found.Count == 0, "said absent in PUBLISHING.md: " + string.Join(", ", found));
        }),
    ];
}
