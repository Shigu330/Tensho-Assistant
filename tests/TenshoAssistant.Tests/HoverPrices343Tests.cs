using TenshoAssistant.Items;
using TenshoAssistant.Market;

// 0.343 (g, I-01): complete hover price — last sale, average, sales per day, region cheapest, stack value, Ctrl+Alt refresh.
internal static class HoverPrices343Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    // Shape of a real Universalis aggregated answer (item 5057, world 97, read 2026-11-03), values shortened.
    private const string Answer = """
        {"results":[{"itemId":5057,
          "nq":{"minListing":{"world":{"price":89},"dc":{"price":45,"worldId":83},"region":{"price":40,"worldId":33}},
                "recentPurchase":{"world":{"price":71,"timestamp":1793612456000}},
                "averageSalePrice":{"world":{"price":104.09}},
                "dailySaleVelocity":{"world":{"quantity":93.3}}},
          "hq":{},
          "worldUploadTimes":[{"worldId":97,"timestamp":1793612456000}]}]}
        """;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hover prices: Universalis velocity, average and region cheapest parsed; missing values stay unknown",() =>
        {
            var quote = MarketQuote.Parse(Answer,97,5057,DateTimeOffset.UnixEpoch);
            Check(quote.Nq.Minimum == 89 && quote.Nq.RecentSale == 71 && quote.Nq.Average == 104.09m && quote.Nq.Velocity == 93.3m,"world values");
            Check(quote.NqRegion is { Price: 40, WorldId: 33 } && quote.NqCheapest is { Price: 45, WorldId: 83 },"region and data center cheapest");
            Check(quote.Hq.Average is null && quote.Hq.Velocity is null && quote.HqRegion is null,"HQ absent: unknown, never zero");
            Check(Read("Market/MarketService.cs").Contains("\"universalis-aggregated-v3\"",StringComparison.Ordinal),"disk copies without the new fields are fetched again");
        }),
        ("Hover prices: stack of the hovered slot kept only for that item; refresh asked once per Ctrl+Alt press, by the player",() =>
        {
            using var service = new ItemHoverService();
            var catalog = ItemCatalog.Build([new(5057,"Item",true,IsMarketable: true)],new("test","French","test",1));
            service.Observe(true,1,5057,catalog,DateTimeOffset.UnixEpoch);
            service.SetStack(42);
            Check(service.State.Stack == 42,"stack recorded");
            service.Observe(true,1,5057,catalog,DateTimeOffset.UnixEpoch.AddSeconds(1));
            Check(service.State.Stack == 42,"same item: kept");
            service.SetStack(0);
            Check(service.State.Stack is null,"zero is no stack");
            var reader = Read("Items/ItemHoverReader.Dalamud.cs");
            Check(reader.Contains("held && !refreshHeld",StringComparison.Ordinal) && reader.Contains("configuration.HoverPriceRefresh",StringComparison.Ordinal),"one request per press, setting honoured");
            Check(reader.Contains("slot->ItemId==id",StringComparison.Ordinal) && reader.Contains("DetailKind.InventoryItem",StringComparison.Ordinal),"stack only from the player's own slot holding that item");
            Check(Read("Market/ItemHoverCoordinator.cs").Contains("if(!tracker.TakeRequest(now))return null;\n            if(refresh)",StringComparison.Ordinal)
                || Read("Market/ItemHoverCoordinator.cs").Contains("if(!tracker.TakeRequest(now))return null;\r\n            if(refresh)",StringComparison.Ordinal),"refresh still bounded by the 5 s hover cadence");
        }),
    ];
}
