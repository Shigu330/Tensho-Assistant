using TenshoAssistant.Market;

// A-06 (0.431): crafting profit — lowest listing (world or data center) × quantity − known purchase cost; last sale beside it.
internal static class CraftProfit431Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft profit: world or data-center price, last sale, profit only with a complete cost",() =>
        {
            var quote=new MarketQuote(40,500,DateTimeOffset.UnixEpoch,null,new MarketQualityPrice(1000,900,null),new MarketQualityPrice(1500,1400,null),
                new MarketListing(950,41,null),new MarketListing(1450,42,null));
            var world=CraftProfits.Compute(quote,false,10,4000,true,false);
            Check(world.UnitPrice==1000 && world.PriceWorld==40 && world.Revenue==10000 && world.Profit==6000 && world.LastSale==900,"world NQ");
            var dc=CraftProfits.Compute(quote,true,10,4000,true,true);
            Check(dc.UnitPrice==1450 && dc.PriceWorld==42 && dc.Profit==10500 && dc.LastSale==1400,"data center HQ");
            Check(CraftProfits.Compute(quote,false,10,4000,false,false) is { Profit:null, Revenue:10000 },"partial cost: no profit");
            Check(CraftProfits.Compute(null,false,10,4000,true,false) is { UnitPrice:null, Profit:null } && CraftProfits.Compute(quote,false,0,0,true,false).UnitPrice is null,"unknown");
            var noListing=quote with {Nq=new MarketQualityPrice(null,900,null)};
            Check(CraftProfits.Compute(noListing,false,10,0,true,false) is { UnitPrice:null, Profit:null },"no listing: unknown, not zero");
            var ui=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.PreparationCost.cs"));
            Check(ui.Contains("Array.IndexOf(ids,recipe.ResultItemId)<0) ids=[..ids,recipe.ResultItemId];") && ui.Contains("DrawCraftProfit(recipe,requested,estimate,world.WorldId,chain);"),"same request");
        }),
    ];
}
