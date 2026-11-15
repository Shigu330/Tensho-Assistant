using TenshoAssistant.Character;

// X-196 slice 2 (0.1410): the retainers' table of the Reminders page (Status tab): rows by character, sales ending within a day apart.
internal static class RetainerTable1410Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Retainer table 1410: sales: none, selling, ending within a day, ended", () =>
        {
            var selling = new RetainerState { MarketItems = 3, MarketEnds = Now.AddDays(3) };
            Check(RetainerStates.Sale(selling, Now) == RetainerSale.Selling, "three days left");
            Check(RetainerStates.Sale(selling with { MarketEnds = Now.AddHours(23) }, Now) == RetainerSale.EndingSoon, "23 hours left");
            Check(RetainerStates.Sale(selling with { MarketEnds = Now.AddDays(1) }, Now) == RetainerSale.EndingSoon, "a day left: soon");
            Check(RetainerStates.Sale(selling with { MarketEnds = Now }, Now) == RetainerSale.Ended && RetainerStates.Sale(selling with { MarketEnds = Now.AddMinutes(-1) }, Now) == RetainerSale.Ended, "ended");
            Check(RetainerStates.Sale(selling with { MarketEnds = null }, Now) == RetainerSale.Selling, "end unknown: selling");
            Check(RetainerStates.Sale(selling with { MarketItems = 0 }, Now) == RetainerSale.None, "nothing on sale");
        }),
        ("Retainer table 1410: rows by character in the given order, the game's order of retainers, never-read characters left out", () =>
        {
            var a = new CharacterStatus { ContentId = 1, Name = "A", Retainers = [new() { Name = "R2" }, new() { Name = "R1" }] };
            var b = new CharacterStatus { ContentId = 2, Name = "B" };
            var c = new CharacterStatus { ContentId = 3, Name = "C", Retainers = [new() { Name = "S" }] };
            var empty = new CharacterStatus { ContentId = 4, Name = "D", Retainers = [] };
            var rows = RetainerStates.Table([c, a, b, empty]);
            Check(rows.Select(row => row.Character.Name + ":" + row.Retainer.Name).SequenceEqual(["C:S", "A:R2", "A:R1"]), string.Join(",", rows.Select(row => row.Retainer.Name)));
            Check(RetainerStates.Table([b, empty]).Count == 0, "no table before a retainer was read");
        }),
        ("Retainer table 1410: drawn in the Status tab under the characters, every text in both languages", () =>
        {
            Check(Read("MainWindow.Reminders.cs").Contains("()=>{DrawCharacterStatuses();DrawRetainerTable();}", StringComparison.Ordinal), "Status tab");
            var page = Read("MainWindow.CharacterStatus.cs");
            Check(page.Contains("var rows=RetainerStates.Table(all);\n        if(rows.Count==0)return;", StringComparison.Ordinal), "shown once a retainer was read");
            Check(page.Contains("else if(sale==RetainerSale.EndingSoon)ImGui.TextColored(Warning,text);", StringComparison.Ordinal) && page.Contains("if(sale==RetainerSale.Ended)ImGui.TextColored(Error,text);", StringComparison.Ordinal), "colours");
            var keys = new[] { "RetainersTitle", "RetainersHelp", "RetainerColumnCharacter", "RetainerColumnName", "RetainerColumnJob", "RetainerColumnGil", "RetainerColumnItems",
                "RetainerColumnMarket", "RetainerColumnVenture", "RetainerColumnTown", "RetainerJob", "RetainerStock", "RetainerStockUnknown", "RetainerNone", "RetainerMarket",
                "RetainerMarketCount", "RetainerMarketEnded", "RetainerVentureRunning", "RetainersGil" };
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"Localization/Languages/{language}.json");
                var section = text[text.IndexOf("\n  \"CharacterStatus\": {", StringComparison.Ordinal)..];
                section = section[..section.IndexOf("\n  },", StringComparison.Ordinal)];
                foreach (var key in keys) Check(section.Contains("\n    \"" + key + "\": ", StringComparison.Ordinal), language + ": " + key);
            }
        }),
    ];
}
