using TenshoAssistant.Beastmaster;

// 0.1465 (X-465): the result window of the board of 11/15 (dalamud.log 21:56:34, developer mode) was refused: its score 7862 is the sum of
// indexes 4, 6, 8, 10 and 12 (5000 + 750 + 800 + 1000 + 312); the first board's (11/12) had 12 at 0. Both read now, as the game shows them.
internal static class BeastResult1465Tests
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

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast result 1465: the board of 11/15 is read as the game shows it (score 7862 with index 12, ten beasts, ranks and experience)", () =>
        {
            var result = BeastResults.Parse(Values(Board2), Beasts(), DateTimeOffset.UnixEpoch) ?? throw new Exception("refused: " + BeastResults.Why(Values(Board2), Beasts()));
            Check(result.Score == 7862 && result.Beasts.Count == 10, "score and team");
            Check(result.Beasts[0] == new BeastGain(5, 5, 20, 6, 54) && result.Beasts[1] == new BeastGain(1, 6, 97, 7, 57) && result.Beasts[9] == new BeastGain(18, 6, 30, 6, 90),
                  "each beast: rank and experience before and after, slot by slot");
            Check(result.Beasts.Sum(gain => gain.RankAfter - gain.RankBefore) == 8, "eight ranks gained");
        }),
        ("Beast result 1465: the first board (index 12 at 0) is still read the same", () =>
        {
            Check(BeastResults.Parse(Values(Board1), Beasts(), DateTimeOffset.UnixEpoch) is { Score: 7550, Beasts.Count: 10 }, "7550");
        }),
        ("Beast result 1465: a score that does not fit is still refused whole, and the log says why in numbers", () =>
        {
            Check(BeastResults.Parse(Values(Board2, (12, 311)), Beasts(), DateTimeOffset.UnixEpoch) is null, "one short");
            Check(BeastResults.Why(Values(Board2, (12, 311)), Beasts()) == "score 7862 is not the sum of indexes 4, 6, 8, 10, 12 (5000 + 750 + 800 + 1000 + 311)", BeastResults.Why(Values(Board2, (12, 311)), Beasts()));
            Check(BeastResults.Why(Values(Board2, (137, 30)), Beasts()) == "slot 0: rank 5 to 30 (top 25)", "a rank above the beast's last");
            Check(BeastResults.Why(Values(Board2), Beasts()) == "", "nothing to say when it is read");
        }),
        ("Beast result 1465: the reader's warning gives the reason; the window is read again when it changes (filled after it opens)", () =>
        {
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs")).Replace("\r\n", "\n");
            Check(reader.Contains("nothing kept ({0}).\", BeastResults.Why(values, known));", StringComparison.Ordinal), "the reason logged");
            Check(reader.Contains("if (seen == resultSeen) return;", StringComparison.Ordinal) && reader.Contains("resultRead(result);", StringComparison.Ordinal), "read again on change");
        }),
    ];
}
