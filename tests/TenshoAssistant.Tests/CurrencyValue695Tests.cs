// 0.695 (user report and request): « What to do with it? » in its own tab, with its prices.
internal static class CurrencyValue695Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Currency value: its own tab, gone from under the currencies",() =>
        {
            Check(Read("MainWindow.Storage.cs").Contains("MeasureMap(\"storage currency value\", DrawCurrencyValueTab);"),"tab (0.1206: timed)");
            Check(!Read("MainWindow.Currencies.cs").Contains("DrawCurrencyValue();"),"not under the table");
        }),
        ("Currency value: prices follow the choice, under the source switch, an item is asked once, and a failure shows",() =>
        {
            var page=Read("MainWindow.CurrencyValue.cs");
            Check(page.Contains("currencyPricesWanted=true;"),"the choice asks");
            Check(page.Contains("&& SourceOn(SourceUse.UniversalisToolPrices))"),"source switch");
            Check(page.Contains("!currencyPricesAsked.Contains(row.ItemId)"),"asked once");
            Check(page.Contains("missing.Take(MarketService.BatchMaximum)"),"bounded batches");
            Check(page.Contains("CurrencyValue.FetchFailed"),"failure shown");
        }),
        ("Currency value: the market can be left alone, a running search stops, and the list filters",() =>
        {
            var page=Read("MainWindow.CurrencyValue.cs");
            Check(new TenshoAssistant.Configuration().CurrencyValueMarket,"on by default");
            Check(page.Contains("else{currencyPricesWanted=false;currencyPrices.Cancel();}"),"stops");
            Check(page.Contains("else if(configuration.CurrencyValueMarket)"),"asks nothing when off");
            Check(page.Contains("foreach(var row in shownRows.Take(200))") && page.Contains("1=>currencyValueOwned.ContainsKey(row.ItemId)"),"filters");
        }),
    ];
}
