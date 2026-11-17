using TenshoAssistant.Beastmaster;

// 0.1525 (X-481, review of 0.1467 I3; the user's reading R-X-481 of 17/11: each index of XBMResult matched to its line of the game's window):
// the score's lines (4 moves made, 6 enemies, 8 elite enemies, 10 boss, 12 remaining HP with its value at 11), each bonus's points (31 onwards)
// are kept and shown as the window shows them. Real numbers: the board of 11/16 10:35:54 (docs/evidence/0.1470-beast-result-window.md).
internal static class BeastResultLines1525Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const string Board3 = "2:8000;4:5000;6:750;8:800;10:1000;11:912;12:450;13:2000;14:2;31:1000;32:1000;47:1;68:10000;70:25;71:100;72:2;"
        + "73:242005;74:242001;89:20;90:97;105:54;106:57;121:5;122:6;137:6;138:7";

    private static Dictionary<int, long> Values(string line, params (int Index, long Value)[] changes)
    {
        var values = line.Split(';').Select(pair => pair.Split(':')).ToDictionary(pair => int.Parse(pair[0]), pair => long.Parse(pair[1]));
        foreach (var (index, value) in changes) values[index] = value;
        return values;
    }

    private static IReadOnlyList<Beast> Beasts()
    {
        var ranks = Enumerable.Range(1, 25).Select(rank => new BeastStats(rank, rank, rank, rank, rank)).ToArray();
        return Enumerable.Range(1, 50).Select(row => new Beast((uint)row, "Beast " + row, 242000u + (uint)row, "", "", "", "", ranks)).ToArray();
    }

    private static BoardResult Read(Dictionary<int, long> values) => BeastResults.Parse(values, Beasts(), DateTimeOffset.UnixEpoch) ?? throw new Exception("refused: " + BeastResults.Why(values, Beasts()));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast result lines 1525: each line of the game's window kept: moves made, enemies, elite enemies, boss, remaining HP, each bonus", () =>
        {
            var result = Read(Values(Board3));
            Check(result.Parts == new BoardParts(5000, 750, 800, 1000, 450, 912), "the score's lines: " + result.Parts);
            Check(result.BonusPoints is { Count: 2 } points && points.All(point => point == 1000) && result is { Bonus: 2000, Total: 10000, Score: 8000 }, "two bonuses of 1000, total 10000");
            Check(Read(Values(Board3, (11, -5))).Parts?.HpLeft is null, "a remaining HP that is no plain number stays unknown, the rest is kept");
            Check(Read(Values(Board3, (68, 1))) is { BonusPoints: null, Total: null } other && other.Parts is not null, "bonus not adding up: unknown, the lines kept");
        }),
        ("Beast result lines 1525: a result kept before 0.1525 is read back without its lines; the new one goes and comes back whole", () =>
        {
            var old = BeastTeams.Read("{\"Plans\":{},\"Keys\":{},\"Result\":{\"At\":\"2026-11-16T10:35:54+00:00\",\"Score\":8000,\"Beasts\":[],\"Bonus\":2000,\"Bonuses\":2,\"Total\":10000}}");
            Check(old?.Result is { Parts: null, BonusPoints: null, Score: 8000 }, "older file: no lines, nothing invented");
            var saved = new BeastTeams.SavedPlans([], [], null, Read(Values(Board3)));
            var back = BeastTeams.Read(BeastTeams.Write(saved));
            Check(back?.Result?.Parts == new BoardParts(5000, 750, 800, 1000, 450, 912) && back.Result.BonusPoints!.SequenceEqual([1000, 1000]), "written and read back");
        }),
        ("Beast result lines 1525: the page shows them with the game's words", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("DrawBeastResultLines(result);") && page.Contains("Line(\"BeastUi.ResultHp\", parts.HpLeft"), "the lines drawn");
            var fr = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            Check(fr.Contains("\"ResultPath\": \"Chemin parcouru\"") && fr.Contains("\"ResultHp\": \"PV restants\"") && en.Contains("\"ResultPath\": \"Moves Made\"") && en.Contains("\"ResultHp\": \"Remaining HP\""), "Addon 17880 and 17882");
        }),
    ];
}
