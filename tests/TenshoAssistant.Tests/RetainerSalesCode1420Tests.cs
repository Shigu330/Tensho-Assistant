// 0.1420, X-434 and X-435 M1 (review of block 0.1413-0.1419): the end of a retainer's sales is the field the game's own code passes as the
// time of its « until » line (docs/evidence/0.1420-retainer-sales-end-code.md, tools/AddonTextProbe): the evidence names the offsets read in
// the code, and they are the offsets of the fields Tensho reads; a retainer still selling with an unusable time keeps its alarm.
using System.Text.RegularExpressions;
using TenshoAssistant.Alarms;
using TenshoAssistant.Character;

internal static class RetainerSalesCode1420Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private const string Evidence = "docs/evidence/0.1420-retainer-sales-end-code.md";
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 16, 0, 0, TimeSpan.Zero);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Retainer sales code 1420: the game's code passes [retainer+0x34] as the line's time, and 0x34 is MarketExpire, the field Tensho reads", () =>
        {
            var text = Read(Evidence);
            foreach (var quoted in new[] { "movzx eax,byte [rsi+30h]", "movzx eax,byte [rsi+31h]", "mov eax,[rsi+34h]", "cmp byte [rsi+31h],0", "0x90A = 2314", "0x3C7 = 967" })
                Check(text.Contains(quoted, StringComparison.Ordinal), "the evidence quotes " + quoted);
            Check(text.Contains("`0x30 Town`", StringComparison.Ordinal) && text.Contains("`0x31 MarketItemCount`", StringComparison.Ordinal) && text.Contains("`0x34 MarketExpire`", StringComparison.Ordinal),
                "the offsets of the three fields, from FFXIVClientStructs");
            // The order in the code is the order of the line's parameters: town, count, time.
            Check(text.IndexOf("[rsi+30h]", StringComparison.Ordinal) < text.IndexOf("[rsi+31h]", StringComparison.Ordinal)
                && text.IndexOf("[rsi+31h]", StringComparison.Ordinal) < text.IndexOf("[rsi+34h]", StringComparison.Ordinal), "lnum2, lnum3, lnum4 in that order");
            var probe = Read("tools/AddonTextProbe/Program.cs");
            Check(probe.Contains("Decoder.Create(64", StringComparison.Ordinal) && probe.Contains("FieldOffsetAttribute", StringComparison.Ordinal) && !probe.Contains("Process.", StringComparison.Ordinal),
                "the probe reads the client as a file and the offsets from metadata, no process touched");
            Check(Read("tools/README.md").Split("| `AddonTextProbe` |").Length == 3, "the probe listed in both halves of tools/README.md");
            // Tensho reads those very fields, from the same reading.
            Check(Read("src/TenshoAssistant/Character/RetainerReader.Dalamud.cs").Contains("retainer->MarketItemCount,retainer->MarketExpire,", StringComparison.Ordinal), "the table reads MarketExpire");
            Check(Read("src/TenshoAssistant/Alarms/AutoAlarmWatcher.Dalamud.cs").Contains("SaleAlarmAt(retainer->MarketItemCount,retainer->MarketExpire)", StringComparison.Ordinal), "the alarm reads MarketExpire");
            Check(Read("src/TenshoAssistant/Character/CharacterStatus.cs").Contains(Evidence, StringComparison.Ordinal), "the code cites the evidence");
            Check(Read("docs/evidence/0.1415-retainer-sales-end.md").Split(Evidence).Length == 3, "the 0.1415 evidence points to it in both halves");
        }),
        ("Retainer sales code 1420 (X-435 M1): a retainer still selling with an unusable time keeps its « sales end » alarm; one emptied loses it", () =>
        {
            Check(RetainerStates.NothingOnSale(0) && !RetainerStates.NothingOnSale(3), "nothing on sale is a count of 0");
            Check(RetainerStates.SaleAlarmAt(3, 0) is null && !RetainerStates.NothingOnSale(3), "a time of 0 with items on sale: no new alarm, but not « gone »");
            var at = Now.AddDays(2);
            List<TenshoAlarm> Alarms() => [new TenshoAlarm { Label = "A", AutoKey = "market:1", At = at, Repeat = AlarmRepeat.Once }];
            // What the watcher gives Sync for a retainer selling with an odd time: no event, not gone.
            var kept = Alarms();
            string[] oddGone = RetainerStates.NothingOnSale(3) ? ["market:1"] : [];
            AutoAlarms.Sync(kept, [], Now, 10, oddGone);
            Check(kept.Any(alarm => alarm.AutoKey == "market:1" && alarm.At == at), "the alarm still to come is kept");
            var emptied = Alarms();
            string[] emptiedGone = RetainerStates.NothingOnSale(0) ? ["market:1"] : [];
            AutoAlarms.Sync(emptied, [], Now, 10, emptiedGone);
            Check(!emptied.Any(alarm => alarm.AutoKey == "market:1"), "a retainer emptied loses it");
            var watcher = Read("src/TenshoAssistant/Alarms/AutoAlarmWatcher.Dalamud.cs");
            Check(watcher.Contains("else if(TenshoAssistant.Character.RetainerStates.NothingOnSale(retainer->MarketItemCount))gone.Add($\"market:{retainer->RetainerId}\");", StringComparison.Ordinal)
                && !Regex.IsMatch(watcher, @"\belse gone\.Add"), "the watcher takes back only the emptied ones");
        }),
    ];
}
