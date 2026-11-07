using TenshoAssistant.Market;

// 0.794 (X-33, user request): « What to do with? » counts the sales per day, like « What can I craft? ».
internal static class CurrencySales794Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Currency sales: one odd sale with no speed goes below what really sells",() =>
        {
            // The capture: Dark Matter grade I, 12 seals, « sold » 350 000 with no sale in the week; Purified water, 20 seals, 1 100, selling.
            var odd=new CurrencyBuy(1,1,12,false,350000m,0m);
            var water=new CurrencyBuy(2,1,20,false,1100m,40m);
            var ranked=CurrencyValue.RankBySales([odd,water],null,7);
            Check(ranked[0]==water && odd.Worth(null,7)==0 && water.Worth(null,7)==280*1100m,"speed first");
            Check(new CurrencyBuy(3,1,10,false,500m).Worth(null,7) is null && CurrencyValue.RankBySales([new CurrencyBuy(3,1,10,false,500m),odd],null,7)[0]==odd,"unknown speed last");
        }),
        ("Currency sales: no more than the balance buys, and one sale a week counts as one piece",() =>
        {
            var water=new CurrencyBuy(2,3,20,false,1100m,40m);
            Check(water.Sellable(100,7)==15,"100 seals: 5 purchases of 3");
            Check(new CurrencyBuy(4,1,5,false,10m,1m/7).Sellable(null,7)==1,"rounded");
        }),
        ("Currency sales: the page shares the days with « What can I craft? » and shows speed and gil",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CurrencyValue.cs"));
            Check(page.Contains("currencyValueRows=CurrencyValue.RankBySales(") && page.Contains("configuration.CraftStockSaleDays=Math.Clamp(saleDays,1,30);"),"ranked, shared days");
            Check(page.Contains("new[]{\"Item\",\"Cost\",\"Market\",\"PerDay\",\"PerUnit\",\"Worth\",\"Collection\"}") && page.Contains("ImGui.BeginTable(\"CurrencyValue\",7,"),"columns");
        }),
    ];
}
