using TenshoAssistant.Market;

// X-212 part 2 (0.1756, the user's request of 12/11, K-21): the sales shown on the market board's history copied as CSV for a spreadsheet:
// item, date with its offset, world, unit price, quantity, quality; the table's order kept; no buyer or seller (a sale holds none).
internal static class MarketCsv1756Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Market csv 1756: behaviour: one line per sale in the order given, quotes and commas kept, a formula sign prefixed, the date with its offset; an empty list gives the header", () =>
        {
            var at = new DateTimeOffset(2026, 11, 21, 22, 5, 9, TimeSpan.FromHours(1));
            var sales = new[]
            {
                new MarketSale(1500, 3, true, "Ragnarok", at),
                new MarketSale(99, 1, false, "=Odin", at.AddDays(-2)),
                new MarketSale(1500, 3, true, "Ragnarok", at),
            };
            var text = MarketBoardMath.SalesCsv(5057, "Rôti \"de\" bœuf, aux herbes", sales);
            var lines = text.Split('\n');
            Check(lines[0] == "ItemId,Item,Date,World,UnitPrice,Quantity,Quality", "the header: " + lines[0]);
            Check(lines[1] == "5057,\"Rôti \"\"de\"\" bœuf, aux herbes\",2026-11-21T22:05:09+01:00,\"Ragnarok\",1500,3,HQ", "a sale: " + lines[1]);
            Check(lines[2] == "5057,\"Rôti \"\"de\"\" bœuf, aux herbes\",2026-11-19T22:05:09+01:00,\"'=Odin\",99,1,NQ", "a formula sign prefixed, NQ: " + lines[2]);
            Check(lines[3] == lines[1] && lines.Length == 5 && lines[4] == "", "the order given, the same sale twice kept twice, one line each");
            Check(MarketBoardMath.SalesCsv(1, "x", []) == "ItemId,Item,Date,World,UnitPrice,Quantity,Quality\n", "an empty list: the header only");
            Check(MarketBoardMath.SalesCsv(1, "x", sales) == MarketBoardMath.SalesCsv(1, "x", sales), "the same text each time");
        }),
        ("Market csv 1756: wiring: the history's button copies the rows the table shows, with its tip and its count; texts in English and French", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketBoard.cs"));
            Check(page.Contains("var text=MarketBoardMath.SalesCsv(board.ItemId,itemCatalog.State.Catalog?.Find(board.ItemId)?.Name ?? $\"#{board.ItemId}\",marketSalesShown);", StringComparison.Ordinal), "the rows shown");
            Check(page.Contains("Tip(\"MarketBoard.CopyCsvTip\");", StringComparison.Ordinal) && page.Contains("localization.Format(\"MarketBoard.CsvCopied\",marketSalesShown.Length)", StringComparison.Ordinal), "tip and count");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"CopyCsv\": \"", StringComparison.Ordinal) && text.Contains("\"CsvCopied\": \"{0} ", StringComparison.Ordinal), language);
            }
        }),
    ];
}
