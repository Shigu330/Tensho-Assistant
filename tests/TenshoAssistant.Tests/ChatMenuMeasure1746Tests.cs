// X-758 (0.1746, D-203 a recommended): the game's menu of a player far away is never opened by imitating the game chat's click (an unnamed
// handler of the interface, UIModule's external handler 2): no call to an external handler in the plugin; the measure's analysis written,
// both halves alike (docs/evidence/0.1746-chat-player-menu-measure.md).
internal static class ChatMenuMeasure1746Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat menu measure 1746: cliquet: no external handler of the game's interface called by the plugin (nothing imitated, D-203)", () =>
        {
            var folder = Path.GetFullPath("src/TenshoAssistant");
            var calls = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .SelectMany(path => File.ReadAllLines(path).Select((line, at) => (path, line, at)))
                .Where(item => !item.line.TrimStart().StartsWith("//", StringComparison.Ordinal) && (item.line.Contains("CallHandler(", StringComparison.Ordinal) || item.line.Contains("AtkExternalInterface", StringComparison.Ordinal)))
                .Select(item => Path.GetRelativePath(folder, item.path) + ":" + (item.at + 1)).ToList();
            Check(calls.Count == 0, "an external handler called: " + string.Join(", ", calls));
        }),
        ("Chat menu measure 1746: the measure's analysis is written, the five clicks in both halves", () =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/evidence/0.1746-chat-player-menu-measure.md")).ReplaceLineEndings("\n");
            var halves = text.Split("\n---\n");
            Check(halves.Length == 2, "two halves");
            foreach (var half in halves)
                Check(half.Split('\n').Count(line => line.StartsWith("| ", StringComparison.Ordinal) && char.IsDigit(line[2])) == 5, "five clicks in each half");
            Check(halves[0].Contains("D-203 a", StringComparison.Ordinal) && halves[1].Contains("D-203 a", StringComparison.Ordinal), "the recommendation in both");
        }),
    ];
}
