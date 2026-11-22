using TenshoAssistant.Inventory;
using TenshoAssistant.Market;

// X-207 (the user's request of 12/11, K-14), first part: the estimated value of the stocks per storage and per character, at the market prices
// asked on a click only (DEC-048); an item without a price is left out and counted apart, never valued 0 (R06); old prices said with their
// date; the NPC's price against the market's under the tooltip, advice only.
internal static class StockValue1782Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset Now = new(2026, 11, 22, 7, 30, 0, TimeSpan.Zero);

    private static InventoryStack S(uint item, long quantity, bool hq = false) => new(new InventoryItem(new InventoryItemId(item), hq), quantity);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Stock value 1782: behaviour: priced stacks add unit × quantity; an item without a price is left out and counted apart (never 0); untradable apart", () =>
        {
            var prices = new Dictionary<(uint, bool), StockPrice>
            {
                [(1, false)] = new(100, Now.AddHours(-1)),
                [(2, false)] = new(5000, Now.AddHours(-2)),
                [(2, true)] = new(9000, Now.AddDays(-3)),
            };
            var marketable = new HashSet<uint> { 1, 2, 3 };
            var line = StockValue.Of("Bags", [S(1, 99), S(1, 50), S(2, 2), S(2, 1, hq: true), S(3, 10), S(4, 1), S(5, 0)],
                id => marketable.Contains(id), (id, hq) => prices.TryGetValue((id, hq), out var price) ? price : null, Now);
            Check(line.Value == 100 * 149 + 5000 * 2 + 9000, "the value of the priced stacks only: " + line.Value);
            Check(line.Priced == 3, "three items priced (1, 2 NQ, 2 HQ): " + line.Priced);
            Check(line.Unpriced == 1, "item 3 taken by the market without a price: counted apart, not 0 in the value: " + line.Unpriced);
            Check(line.NotSold == 1, "item 4 not taken by the market: apart; an empty stack ignored: " + line.NotSold);
            Check(line.Stale == 1 && line.Oldest == Now.AddDays(-3), "the HQ price of three days ago said old, with its date");
            var none = StockValue.Of("Retainer", [S(3, 5)], id => true, (id, hq) => null, Now);
            Check(none is { Value: 0, Priced: 0, Unpriced: 1 }, "nothing priced: the item counted without a price (the page shows it apart, never « worth 0 » alone)");
            var zeroPrice = StockValue.Of("Retainer", [S(3, 5)], id => true, (id, hq) => new StockPrice(0, Now), Now);
            Check(zeroPrice.Unpriced == 1 && zeroPrice.Priced == 0, "a price of 0 is no price");
        }),
        ("Stock value 1782: per character: the storages added, the oldest date kept; the unit price from a quote: lowest listing, else average sale; the NPC against the market", () =>
        {
            var total = StockValue.Sum("Alice", [new("Bags", 1000, 2, 1, 0, 0, Now.AddHours(-2)), new("Retainer", 500, 1, 2, 3, 1, Now.AddDays(-2)), new("Saddlebag", 0, 0, 0, 1, 0, null)]);
            Check(total is { Place: "Alice", Value: 1500, Priced: 3, Unpriced: 3, NotSold: 4, Stale: 1 } && total.Oldest == Now.AddDays(-2), "summed: " + total);
            var quote = new MarketQuote(73, 5, Now, Now.AddHours(-1), new MarketQualityPrice(120m, 110m, Now.AddHours(-5), 115m), new MarketQualityPrice(null, 300m, Now.AddHours(-6), 310m));
            Check(StockValue.FromQuote(quote, false) == new StockPrice(120, Now.AddHours(-1)), "NQ: the lowest listing, dated by the world's data");
            Check(StockValue.FromQuote(quote, true) == new StockPrice(310, Now.AddHours(-6)), "HQ without a listing: the average sale, dated by the last sale");
            Check(StockValue.FromQuote(quote with { Hq = new MarketQualityPrice(null, null, null) }, true) is null && StockValue.FromQuote(null, false) is null, "neither: no price");
            Check(StockValue.MarketOverNpc(10, 120) == 12 && StockValue.MarketOverNpc(200, 100) == 0.5, "how many times the market pays the NPC's price");
            Check(StockValue.MarketOverNpc(0, 120) is null && StockValue.MarketOverNpc(10, null) is null, "an unknown price: nothing said");
        }),
        ("Stock value 1782: asked on the click only, by lists of 100 under the market service's pauses; the tooltip line; the texts", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.StockValue.cs");
            Check(page.Contains("if(ImGui.Button(localization.Get(\"StockValue.Estimate\")))StartStockValue(character,world);", StringComparison.Ordinal), "on the click only (DEC-048)");
            Check(page.Contains("var allowed=configuration.CommunityUniversalis && world!=0 && character!=0;", StringComparison.Ordinal), "never with Universalis off");
            Check(page.Contains("await market.FillCacheAsync(batch,character,world,cancel.Token,true)", StringComparison.Ordinal) && page.Contains("Market.MarketService.BatchMaximum", StringComparison.Ordinal), "by lists of 100 through the market service");
            Check(!page.Contains("FillCacheAsync", StringComparison.Ordinal) || page.IndexOf("FillCacheAsync", StringComparison.Ordinal) > page.IndexOf("private void StartStockValue", StringComparison.Ordinal), "asked only from the click's method");
            Check(Read("src/TenshoAssistant/MainWindow.StorageReport.cs").Contains("DrawStockValue();", StringComparison.Ordinal), "in Storage › Report");
            Check(Read("src/TenshoAssistant/MainWindow.MarketHover.cs").Contains("Inventory.StockValue.MarketOverNpc(npcSold.SellPrice,Inventory.StockValue.FromQuote(prices,hovered.Hq)?.Unit)", StringComparison.Ordinal), "the NPC against the market under the tooltip");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "\"StockValue\": {", "\"Estimate\": ", "\"UnpricedTip\": ", "\"OldTip\": ", "\"MarketOverNpc\": ", "\"NpcOverMarket\": " })
                    Check(text.Contains(key, StringComparison.Ordinal), $"{language}: {key}");
            }
        }),
    ];
}
