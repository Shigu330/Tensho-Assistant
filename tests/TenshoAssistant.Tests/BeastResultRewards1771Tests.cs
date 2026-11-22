using TenshoAssistant.Beastmaster;

// X-135 (0.1771): the rewards of the Crucible's result window (XBMResult): their number at 47, the Item row at 48, its icon at 53 (checked
// against the Item sheet), 0 at 58, the count at 63, as recorded on the boards of 12/11 and 15/11 (Item 51734, icon 20217, 4 and 5).
// Anything never seen refuses the rewards whole (« not read »), the rest of the result kept (R08). docs/evidence/0.1771-beast-result-rewards.md.
internal static class BeastResultRewards1771Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static uint Icon(uint item) => item == 51734 ? 20217u : item == 9 ? 60009u : 0u;

    // The board of 12/11 as recorded (docs/evidence/0.1163-beast-result.md), team of ten.
    private const string Board1112 = "2:7550;4:5000;6:750;8:800;10:1000;11:361;12:0;13:1000;14:1;31:1000;47:1;48:51734;53:20217;63:4;68:8550;70:25;71:100;72:10;"
        + "73:242005;74:242001;75:242022;76:242019;77:242020;78:242041;79:242012;80:242008;81:242016;82:242018;"
        + "89:0;90:49;91:7;92:34;93:0;94:99;95:0;96:80;97:21;98:21;105:20;106:97;107:55;108:66;109:20;110:19;111:20;112:89;113:30;114:30;"
        + "121:4;122:6;123:6;124:3;125:4;126:4;127:4;128:5;129:5;130:5;137:5;138:6;139:6;140:4;141:5;142:6;143:5;144:6;145:6;146:6";

    private static Dictionary<int, long> Values(string text, params (int Index, long Value)[] changes)
    {
        var values = text.Split(';').Select(pair => pair.Split(':')).ToDictionary(pair => int.Parse(pair[0]), pair => long.Parse(pair[1]));
        foreach (var (index, value) in changes) values[index] = value;
        return values;
    }

    private static IReadOnlyList<Beast> Beasts() => Enumerable.Range(1, 50).Select(row => new Beast((uint)row, "B" + row, 242000u + (uint)row, "", "", "", "",
        Enumerable.Range(1, 25).Select(_ => new BeastStats(1, 1, 1, 1, 1)).ToArray())).ToArray();

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast result rewards 1771: behaviour: the board of 12/11: one reward, the Item 51734 with its icon 20217, ×4; the rest of the result as before", () =>
        {
            var result = BeastResults.Parse(Values(Board1112), Beasts(), DateTimeOffset.UnixEpoch, Icon);
            Check(result is { Score: 7550, Total: 8550 } && result.Beasts.Count == 10, "the result read as before");
            Check(result!.Rewards is [{ Item: 51734, Icon: 20217, Count: 4 }], "one reward: " + string.Join(",", result.Rewards?.Select(reward => $"{reward.Item}/{reward.Icon}/{reward.Count}") ?? ["null"]));
            var board1115 = BeastResults.Rewards(Values(Board1112, (63, 5)), Icon);
            Check(board1115 is [{ Count: 5 }], "the board of 15/11: ×5");
            Check(BeastResults.Parse(Values(Board1112), Beasts(), DateTimeOffset.UnixEpoch)!.Rewards is null, "without the Item sheet: not read");
        }),
        ("Beast result rewards 1771: behaviour: numbers never seen refuse the rewards whole (not read), never a wrong item or count", () =>
        {
            Check(BeastResults.Rewards(Values(Board1112, (53, 20218)), Icon) is null, "an icon that is not the item's");
            Check(BeastResults.Rewards(Values(Board1112, (47, 0)), Icon) is null && BeastResults.Rewards(Values(Board1112, (47, 6)), Icon) is null, "0 or more than five rewards");
            Check(BeastResults.Rewards(Values(Board1112, (58, 1)), Icon) is null, "the row always 0 not 0");
            Check(BeastResults.Rewards(Values(Board1112, (63, 0)), Icon) is null && BeastResults.Rewards(Values(Board1112, (63, 1000)), Icon) is null, "a count of 0 or over 999");
            Check(BeastResults.Rewards(Values(Board1112, (47, 2)), Icon) is null, "a second reward announced but empty");
            Check(BeastResults.Rewards(Values(Board1112, (47, 2), (49, 9), (54, 60009), (64, 3)), Icon) is [{ Item: 51734 }, { Item: 9, Count: 3 }], "two rewards, each checked");
            var refused = BeastResults.Parse(Values(Board1112, (53, 1)), Beasts(), DateTimeOffset.UnixEpoch, Icon);
            Check(refused is { Score: 7550 } && refused.Rewards is null, "the rewards refused, the score and the beasts kept");
        }),
        ("Beast result rewards 1771: behaviour: the same board read again keeps the rewards a reading had; the page knows a change", () =>
        {
            var first = BeastResults.Parse(Values(Board1112), Beasts(), DateTimeOffset.UnixEpoch, Icon)!;
            var again = BeastResults.Parse(Values(Board1112, (53, 1)), Beasts(), DateTimeOffset.UnixEpoch.AddSeconds(1), Icon)!;
            Check(BeastResults.Keep(first, again) is null, "read again without the rewards: nothing changes, the first rewards kept");
            var before = first with { Rewards = null };
            Check(BeastResults.Keep(before, first) is { Rewards: [{ Count: 4 }] } kept && kept.At == before.At, "a result kept without rewards, read again with them: taken, at the first time");
        }),
        ("Beast result rewards 1771: wiring: the reader checks with the Item sheet and logs; the page's line; both languages; the evidence", () =>
        {
            var reader = Read("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs");
            Check(reader.Contains("BeastResults.Parse(values, known, now, ItemIcon)", StringComparison.Ordinal) && reader.Contains("Beastmaster board result, rewards:", StringComparison.Ordinal), "read with the icons, said in the log");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("{ItemIcon=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(id)?.Icon ?? 0u}", StringComparison.Ordinal), "the Item sheet's icon");
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            Check(page.Contains("if (result.Rewards is { } rewards)", StringComparison.Ordinal) && page.Contains("localization.Get(\"BeastUi.ResultRewardsUnread\")", StringComparison.Ordinal), "the line, or « not read »");
            foreach (var language in new[] { "en", "fr" })
                Check(new[] { "ResultReward", "ResultRewardTip", "ResultRewards", "ResultRewardsUnread" }.All(key => Read($"src/TenshoAssistant/Localization/Languages/{language}.json").Contains($"    \"{key}\": ", StringComparison.Ordinal)), "the words, " + language);
            var evidence = Read("docs/evidence/0.1771-beast-result-rewards.md");
            Check(evidence.Contains("51734", StringComparison.Ordinal) && evidence.Contains("20217", StringComparison.Ordinal) && evidence.Contains("\n---\n", StringComparison.Ordinal), "the evidence, both halves");
        }),
    ];
}
