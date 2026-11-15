using TenshoAssistant.Beastmaster;

// 0.1470 (X-481, review of 0.1467 I3): the result window's « Bonus » and « Total » (docs/evidence/0.1470-beast-result-window.md): 14 the number
// of bonuses, 31 onwards each one's points, 13 their sum, 68 the score plus the bonus, on both boards recorded (11/12 and 11/15, the same
// numbers as BeastResult1465Tests). Kept and shown only when they add up; otherwise unknown, the score and the beasts kept.
internal static class BeastResult1470Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const string Board2 = "2:7862;4:5000;6:750;8:800;10:1000;11:780;12:312;13:2000;14:2;31:1000;32:1000;47:1;48:51734;53:20217;63:5;68:9862;70:25;71:100;72:10;"
        + "73:242005;74:242001;75:242022;76:242019;77:242020;78:242041;79:242012;80:242008;81:242016;82:242018;"
        + "89:20;90:97;91:55;92:66;93:20;94:19;95:20;96:89;97:30;98:30;105:54;106:57;107:15;108:51;109:54;110:79;111:54;112:49;113:90;114:90;"
        + "121:5;122:6;123:6;124:4;125:5;126:6;127:5;128:6;129:6;130:6;137:6;138:7;139:7;140:6;141:6;142:6;143:6;144:7;145:6;146:6";
    private const string Board1 = "2:7550;4:5000;6:750;8:800;10:1000;11:361;12:0;13:1000;14:1;31:1000;47:1;48:51734;53:20217;63:4;68:8550;70:25;71:100;72:10;"
        + "73:242005;74:242001;75:242022;76:242019;77:242020;78:242041;79:242012;80:242008;81:242016;82:242018;"
        + "89:0;90:49;91:7;92:34;93:0;94:99;95:0;96:80;97:21;98:21;105:20;106:97;107:55;108:66;109:20;110:19;111:20;112:89;113:30;114:30;"
        + "121:4;122:6;123:6;124:3;125:4;126:4;127:4;128:5;129:5;130:5;137:5;138:6;139:6;140:4;141:5;142:6;143:5;144:6;145:6;146:6";

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
        ("Beast result 1470: both boards give their bonus and total as the game's window (1000 for one bonus, 8550; 2000 for two, 9862)", () =>
        {
            Check(Read(Values(Board1)) is { Score: 7550, Bonus: 1000, Bonuses: 1, Total: 8550 }, "11/12");
            Check(Read(Values(Board2)) is { Score: 7862, Bonus: 2000, Bonuses: 2, Total: 9862 }, "11/15");
            Check(Read(Values(Board2, (13, 0), (14, 0), (68, 7862))) is { Bonus: 0, Bonuses: 0, Total: 7862 } && !Values(Board2, (14, 0)).ContainsKey(-1), "no bonus: the total is the score");
        }),
        ("Beast result 1470: a bonus or total that does not add up stays unknown; the score and the beasts are kept, never a wrong total", () =>
        {
            foreach (var (name, changes) in new (string, (int, long)[])[]
            {
                ("total off by one", [(68, 9861)]), ("sum off", [(13, 1999)]), ("a bonus at 0", [(32, 0), (13, 1000), (68, 8862)]),
                ("more than 16 bonuses", [(14, 17)]), ("count missing", [(14, -1)]),
            })
            {
                var result = Read(Values(Board2, changes));
                Check(result is { Score: 7862, Bonus: null, Bonuses: null, Total: null } && result.Beasts.Count == 10, name);
            }
            var noTotal = Values(Board2);
            noTotal.Remove(68);
            Check(Read(noTotal) is { Total: null, Score: 7862 }, "no total in the window");
        }),
        ("Beast result 1470: a result saved before 0.1470 (no bonus nor total) reads as unknown; the page shows the total only when known", () =>
        {
            var old = new BoardResult(DateTimeOffset.UnixEpoch, 7550, []);
            Check(old.Bonus is null && old.Total is null, "unknown, not 0");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs")).Replace("\r\n", "\n");
            Check(page.Contains("result.Total is { } total ? \" · \" + localization.Format(\"BeastUi.ResultTotal\", result.Bonus ?? 0, result.Bonuses ?? 0, total) : \"\"", StringComparison.Ordinal), "shown when known");
        }),
    ];
}
