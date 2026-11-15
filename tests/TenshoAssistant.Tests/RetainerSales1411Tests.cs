using TenshoAssistant.Character;

// X-196 slice 3 (0.1411): the automatic alarms ring at the end of each retainer's sales, as at the end of its venture.
internal static class RetainerSales1411Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Retainer sales 1411: an alarm at the sales' end, only while something is on sale", () =>
        {
            var end = new DateTimeOffset(2026, 11, 20, 18, 0, 0, TimeSpan.Zero);
            Check(RetainerStates.SaleAlarmAt(3, (uint)end.ToUnixTimeSeconds()) == end, "at the end");
            Check(RetainerStates.SaleAlarmAt(0, (uint)end.ToUnixTimeSeconds()) is null, "nothing on sale: no alarm (the game's leftover time ignored)");
            Check(RetainerStates.SaleAlarmAt(3, 0) is null, "no end known: no alarm");
        }),
        ("Retainer sales 1411: the automatic alarms read it with the ventures, one per retainer, said in the setting", () =>
        {
            var watcher = Read("Alarms/AutoAlarmWatcher.Dalamud.cs");
            Check(watcher.Contains("if(TenshoAssistant.Character.RetainerStates.SaleAlarmAt(retainer->MarketItemCount,retainer->MarketExpire) is { } salesEnd)", StringComparison.Ordinal)
                && watcher.Contains("events.Add(new($\"market:{retainer->RetainerId}\"", StringComparison.Ordinal), "one alarm per retainer's sales");
            Check(watcher.IndexOf("SaleAlarmAt(", StringComparison.Ordinal) < watcher.IndexOf("if(retainer->VentureId==0 || retainer->VentureComplete==0)continue;", StringComparison.Ordinal),
                "a retainer without a venture still gets its sales alarm");
            foreach (var (language, word) in new[] { ("en", "retainer's sales"), ("fr", "ventes de chaque servant") })
            {
                var text = Read($"Localization/Languages/{language}.json");
                Check(text.Contains(word, StringComparison.Ordinal) && text.Contains("\"MarketGroup\": ", StringComparison.Ordinal), language);
            }
        }),
    ];
}
