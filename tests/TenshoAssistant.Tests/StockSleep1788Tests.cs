using TenshoAssistant.Inventory;

// X-207 (the user's request of 12/11, K-14), second part: « sits and sells »: what sits in the storages and sells on the world, by sold
// price × sales a day, the NPC's price beside; an unknown price or speed counted apart, never 0 (R06). X-779 (the review of 0.1784, M1):
// the estimate keeps each list's quotes as they come, so the market cache (4096 quotes) cannot drop the first lists of a long one.
internal static class StockSleep1788Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static InventoryStack S(uint item, long quantity, bool hq = false) => new(new InventoryItem(new InventoryItemId(item), hq), quantity);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Stock sleep 1788: behaviour: grouped over every storage, by sold price × sales a day; not selling, untradable or unknown left out (unknown counted)", () =>
        {
            var sold = new Dictionary<(uint, bool), (decimal Price, decimal? PerDay)>
            {
                [(1, false)] = (100m, 30m),      // 3000 a day
                [(2, false)] = (5000m, 0.5m),    // 2500 a day
                [(2, true)] = (9000m, 1m),       // 9000 a day
                [(3, false)] = (800m, 0m),       // does not sell
                [(5, false)] = (200m, null),     // speed unknown
                [(7, false)] = (40m, 2.4m),      // 96 a day, rounded
            };
            var marketable = new HashSet<uint> { 1, 2, 3, 5, 6, 7 };
            var npc = new Dictionary<uint, uint> { [1] = 120, [2] = 300 };
            var (lines, unknown) = StockValue.Sleepers(
                [("Bags", S(1, 99)), ("Retainer A", S(1, 50)), ("Bags", S(1, 1)), ("Bags", S(2, 2)), ("Saddlebag", S(2, 1, hq: true)),
                 ("Bags", S(3, 10)), ("Bags", S(4, 5)), ("Bags", S(5, 3)), ("Bags", S(6, 4)), ("Bags", S(7, 0)), ("Retainer B", S(7, 6))],
                id => marketable.Contains(id), (id, hq) => sold.TryGetValue((id, hq), out var price) ? price : null, id => npc.GetValueOrDefault(id), 10);
            Check(lines.Select(line => (line.ItemId, line.Hq)).SequenceEqual([(2u, true), (1u, false), (2u, false), (7u, false)]),
                "by gil a day: 2 HQ (9000), 1 (3000), 2 NQ (2500), 7 (96): " + string.Join(", ", lines.Select(line => $"{line.ItemId}{(line.Hq ? "HQ" : "")}={line.PerDayGil}")));
            var first = lines[1];
            Check(first.Quantity == 150 && first.Places.SequenceEqual(["Bags", "Retainer A"]) && first.Sold == 100 && first.PerDay == 30m && first.PerDayGil == 3000 && first.NpcPrice == 120,
                "item 1: held over two places (each named once, in the order met), its sold price, speed and NPC price");
            Check(lines[0].Quantity == 1 && lines[0].NpcPrice == 300 && lines[2].Quantity == 2, "HQ and NQ apart, each its own quantity");
            Check(lines[3].PerDayGil == 96 && lines[3].Quantity == 6 && lines[3].NpcPrice == 0, "rounded gil a day; an empty stack ignored; no NPC price: 0 (shown « — »)");
            Check(unknown == 2, "item 5 (speed unknown) and item 6 (no sold price) counted apart, never 0: " + unknown);
            Check(!lines.Any(line => line.ItemId is 3 or 4), "item 3 does not sell (0 a day): not listed; item 4 not taken by the market: not listed");
            var (few, _) = StockValue.Sleepers([("Bags", S(1, 1)), ("Bags", S(7, 1))], id => true, (id, hq) => sold.TryGetValue((id, hq), out var price) ? price : null, id => 0, 1);
            Check(few is [{ ItemId: 1 }], "only the first ones shown");
            var (tie, _) = StockValue.Sleepers([("Bags", S(9, 1)), ("Bags", S(8, 1))], id => true, (id, hq) => (id == 9 ? 100m : 50m, id == 9 ? 1m : 2m), id => 0, 5);
            Check(tie.Select(line => line.ItemId).SequenceEqual([9u, 8u]), "the same gil a day: the dearer first");
        }),
        ("Stock sleep 1788: the estimate keeps each list's quotes at once and reads them before the cache; the list drawn with the same prices, no request more", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.StockValue.cs");
            var fill = page.IndexOf("var refused=await market.FillCacheAsync(batch,character,world,cancel.Token,true).ConfigureAwait(false);", StringComparison.Ordinal);
            var keep = page.IndexOf("foreach(var id in batch)if(market.GetCached(world,id) is { } quote)quotes[id]=quote;", StringComparison.Ordinal);
            var stop = page.IndexOf("if(refused is not null)return refused;", StringComparison.Ordinal);
            Check(fill > 0 && keep > fill && stop > keep, "each list's quotes kept right after it, before a refusal stops the rest");
            Check(page.Contains("Market.MarketQuote? Quote(uint id)=>stockValueQuotes.GetValueOrDefault(id) ?? market.GetCached(world,id);", StringComparison.Ordinal),
                "the estimate's own quotes first");
            Check(page.Contains("(id,hq)=>Quote(id)?.Sold(hq)", StringComparison.Ordinal) && page.Contains("DrawStockSleepers();", StringComparison.Ordinal), "the list from the same quotes");
            Check(page.IndexOf("FillCacheAsync", StringComparison.Ordinal) == page.LastIndexOf("FillCacheAsync", StringComparison.Ordinal), "one place asks Universalis: the click (DEC-048)");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "SleepersTitle", "SleepersHelp", "SleepersUnknown", "SleepersUnknownTip", "SleepersNone", "ColumnSold", "ColumnPerDay", "ColumnPerDayGil", "ColumnNpc" })
                    Check(text.Contains($"\"{key}\": ", StringComparison.Ordinal), $"{language}: {key}");
            }
        }),
    ];
}
