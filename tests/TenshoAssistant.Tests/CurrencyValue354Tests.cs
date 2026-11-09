using TenshoAssistant.Market;

// 0.354 (g, I-13): what a currency buys, ranked by the gil each unit is worth; unknown prices last, never zero.
internal static class CurrencyValue354Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Currency value: gil per unit = market price × quantity ÷ cost; best first, unknown last",() =>
        {
            var cheap = new CurrencyBuy(1,1,100,false,500m);
            var bundle = new CurrencyBuy(2,5,200,false,300m);
            var unknown = new CurrencyBuy(3,1,10,false,null);
            Check(cheap.GilPerUnit == 5 && bundle.GilPerUnit == 7.5 && unknown.GilPerUnit is null,"per unit, the quantity counted");
            Check(new CurrencyBuy(4,1,0,false,100m).GilPerUnit is null && new CurrencyBuy(5,1,10,false,0m).GilPerUnit is null,"no cost or no price: unknown");
            Check(CurrencyValue.Rank([unknown,cheap,bundle]).Select(buy => buy.ItemId).SequenceEqual([2u,1u,3u]),"most profitable first, unknown last");
            var page = Read("MainWindow.CurrencyValue.cs");
            // 0.1019 (test audit #10): batches and the Universalis switch are checked in CurrencyValue695Tests.
            Check(page.Contains("currencyPrices.StartListAsync(market,character,world.WorldId,batch,true)",StringComparison.Ordinal),"prices asked by batch for the character's world");
            Check(Read("MainWindow.Lifetime.cs").Contains("currencyPrices.Dispose();",StringComparison.Ordinal),"session disposed");
        }),
    ];
}
