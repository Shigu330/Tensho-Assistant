using TenshoAssistant.Beastmaster;

// 0.1163 (X-135, user request of 2026-11-11): the board's result window (XBMResult) read: score, then each team beast's rank and experience
// before and after, from the numbers recorded on the user's 1st board on 2026-11-12 (docs/evidence/0.1163-beast-result.md).
internal static class BeastResult1163Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    // The window's numbers as recorded (11:57:58).
    private const string Recorded = "2:7550;4:5000;6:750;8:800;10:1000;11:361;12:0;13:1000;14:1;31:1000;47:1;48:51734;53:20217;63:4;68:8550;70:25;71:100;72:10;"
        + "73:242005;74:242001;75:242022;76:242019;77:242020;78:242041;79:242012;80:242008;81:242016;82:242018;"
        + "89:0;90:49;91:7;92:34;93:0;94:99;95:0;96:80;97:21;98:21;105:20;106:97;107:55;108:66;109:20;110:19;111:20;112:89;113:30;114:30;"
        + "121:4;122:6;123:6;124:3;125:4;126:4;127:4;128:5;129:5;130:5;137:5;138:6;139:6;140:4;141:5;142:6;143:5;144:6;145:6;146:6";

    private static Dictionary<int, long> Values(string line, params (int Index, long Value)[] changes)
    {
        var values = line.Split(';').Select(pair => pair.Split(':')).ToDictionary(pair => int.Parse(pair[0]), pair => long.Parse(pair[1]));
        foreach (var (index, value) in changes) values[index] = value;
        return values;
    }

    // Beasts 1 to 50, icon 242000 + row, 25 ranks each (as XBMPet and XBMPetParamGrow give them).
    private static IReadOnlyList<Beast> Beasts()
    {
        var ranks = Enumerable.Range(1, 25).Select(rank => new BeastStats(rank, rank, rank, rank, rank)).ToArray();
        return Enumerable.Range(1, 50).Select(row => new Beast((uint)row, "Beast " + row, 242000u + (uint)row, "", "", "", "", ranks)).ToArray();
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast result 1163: the recorded window gives the score and the ten beasts, before and after", () =>
        {
            var at = new DateTimeOffset(2026, 11, 12, 10, 57, 58, TimeSpan.Zero);
            var result = BeastResults.Parse(Values(Recorded), Beasts(), at) ?? throw new Exception("the recorded window is refused");
            Check(result.Score == 7550 && result.At == at, "score 7550");
            Check(result.Beasts.Count == 10, "ten beasts: " + result.Beasts.Count);
            Check(result.Beasts.Select(gain => gain.Beast).SequenceEqual(new uint[] { 5, 1, 22, 19, 20, 41, 12, 8, 16, 18 }), "the beasts in the window's order, by their icon");
            Check(result.Beasts[0] == new BeastGain(5, 4, 0, 5, 20), "first: rank 4 (0/100) to rank 5 (20/100)");
            Check(result.Beasts[1] == new BeastGain(1, 6, 49, 6, 97), "second: rank 6, 49 to 97 (the bestiary showed 49/100, rank 6, before)");
            Check(result.Beasts[5] == new BeastGain(41, 4, 99, 6, 19), "two ranks gained");
            Check(result.Beasts.Sum(gain => gain.RankAfter - gain.RankBefore) == 9, "nine ranks gained in all");
        }),
        ("Beast result 1163: anything outside what was seen is refused whole (R08: unavailable, never a wrong rank)", () =>
        {
            var beasts = Beasts();
            var at = DateTimeOffset.UnixEpoch;
            Check(BeastResults.Parse(Values(Recorded, (2, 7551)), beasts, at) is null, "score not the sum of its parts");
            Check(BeastResults.Parse(Values(Recorded, (72, 0)), beasts, at) is null && BeastResults.Parse(Values(Recorded, (72, 11)), beasts, at) is null, "team size outside 1 to 10");
            Check(BeastResults.Parse(Values(Recorded, (73, 243005)), beasts, at) is null, "an icon that is no beast");
            Check(BeastResults.Parse(Values(Recorded, (74, 242005)), beasts, at) is null, "the same beast twice");
            Check(BeastResults.Parse(Values(Recorded, (137, 3)), beasts, at) is null, "a rank going down");
            Check(BeastResults.Parse(Values(Recorded, (137, 26)), beasts, at) is null, "a rank above the beast's last");
            Check(BeastResults.Parse(Values(Recorded, (105, 101)), beasts, at) is null && BeastResults.Parse(Values(Recorded, (89, -1)), beasts, at) is null, "experience outside 0 to 100");
            var missing = Values(Recorded); missing.Remove(121);
            Check(BeastResults.Parse(missing, beasts, at) is null, "a number missing");
            Check(BeastResults.Parse(new Dictionary<int, long>(), beasts, at) is null, "an empty window");
            Check(BeastResults.Parse(Values(Recorded, (72, 3)), beasts, at) is { Beasts.Count: 3 }, "a smaller team reads its own slots only");
        }),
        ("Beast result 1163: ranks merged over those known, the same result not kept twice, the saved file read with or without a result", () =>
        {
            var result = BeastResults.Parse(Values(Recorded), Beasts(), DateTimeOffset.UnixEpoch)!;
            var merged = BeastResults.Merge(new Dictionary<uint, int> { [5] = 2, [30] = 7 }, result);
            Check(merged[5] == 5 && merged[30] == 7 && merged[41] == 6 && merged.Count == 11, "after-ranks over the known ones, others kept");
            var again = BeastResults.Parse(Values(Recorded), Beasts(), DateTimeOffset.UnixEpoch.AddMinutes(1))!;
            Check(BeastResults.Same(result, again) && !BeastResults.Same(result, null), "the same window read again is the same result");
            Check(!BeastResults.Same(result, BeastResults.Parse(Values(Recorded, (106, 98)), Beasts(), DateTimeOffset.UnixEpoch)), "another result is not");
            var saved = new BeastTeams.SavedPlans([], [], merged, result);
            var back = BeastTeams.Read(BeastTeams.Write(saved)) ?? throw new Exception("not read back");
            Check(back.Result is { Score: 7550 } read && read.Beasts.SequenceEqual(result.Beasts) && back.Ranks![41] == 6, "the result kept on disk");
            Check(BeastTeams.Read("{\"Plans\":{},\"Keys\":{},\"Ranks\":{\"3\":4}}") is { Result: null, Ranks: { } ranks } && ranks[3] == 4, "an older file: no result, ranks kept");
        }),
        ("Beast result 1163: wiring (reader of numbers only, the page, the texts)", () =>
        {
            var reader = Read("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs");
            Check(reader.Contains("ReadResult(now);") && reader.Contains("gui.GetAddonByName(BeastResults.Window)") && reader.Contains("value.Type == AtkValueType.Int") && !reader.Contains("AtkValueType.String"), "numbers only");
            Check(reader.Contains("if (seen == resultSeen) return;"), "parsed once per change");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("mainWindow.BeastRanksRead,mainWindow.BeastResultRead)"), "the reader hands the result to the page");
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            Check(page.Contains("DrawBeastResult(catalog);") && page.Contains("BeastResults.Merge(plans.Ranks, result)") && page.Contains("\"BeastUi.KnownRank\""), "the page shows it and uses the ranks");
            Check(page.Contains("var plans = LoadBeastPlans();\n        var known = plans.Ranks"), "ranks read before the page was opened are no longer lost");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "ResultNone", "ResultTitle", "ResultBeast", "ResultRanks", "KnownRank" }) Check(text.Contains($"\"{key}\": "), $"{language}: {key}");
            }
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1163-beast-result.md")), "the evidence");
        }),
    ];
}
