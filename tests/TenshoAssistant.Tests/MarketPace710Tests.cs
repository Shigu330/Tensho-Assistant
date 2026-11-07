using TenshoAssistant.Market;

// 0.710 (user request): faster on a long list, without troubling Universalis.
internal static class MarketPace710Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Market pace: a server asking for room gets at least the usual pause, and what it asks for if longer",() =>
        {
            Check(MarketService.ServerPause(429,null)==MarketService.BusyServerPause,"429");
            Check(MarketService.ServerPause(503,TimeSpan.FromSeconds(5))==MarketService.BusyServerPause,"never shorter");
            Check(MarketService.ServerPause(429,TimeSpan.FromMinutes(2))==TimeSpan.FromMinutes(2),"what it asks");
            Check(MarketService.ServerPause(200,null) is null && MarketService.ServerPause(404,null) is null,"other answers");
        }),
        ("Market pace: one request at a time, a second apart, 100 items at most, and a single item can go in a long list",() =>
        {
            Check(MarketService.ListPause>=TimeSpan.FromSeconds(1),"pause");
            Check(MarketService.BatchMaximum==100,"the server's own limit");
            Check(MarketService.BatchIds([5],_=>false,1).Length==1 && MarketService.BatchIds([5],_=>false).Length==0,"one item");
            var service=Read("Market/MarketService.cs");
            Check(service.Contains("wait=requestActive ?"),"one at a time");
            Check(service.Contains("MarketQuote.Parse(json,world,id,at)") && service.IndexOf("quotes.Add(MarketQuote.Parse",StringComparison.Ordinal)<service.IndexOf("foreach(var quote in quotes)",StringComparison.Ordinal),"parsed off the lock");
        }),
        ("Sold price: what it sold for, not what is asked; the lower of last and average; HQ when sold as such",() =>
        {
            MarketQuote Quote(MarketQualityPrice nq,MarketQualityPrice hq)=>new(1,2,DateTimeOffset.UtcNow,null,nq,hq);
            var asked=new MarketQualityPrice(2000000,null,null);
            Check(Quote(asked,asked).Sold(true) is null,"an asking price alone is no sale");
            var nq=new MarketQualityPrice(2000000,120m,null,100m,3m);var hq=new MarketQualityPrice(2000000,500m,null,900m,1m);
            Check(Quote(nq,hq).Sold(false) is { Price:100m,PerDay:3m },"average lower than last");
            Check(Quote(nq,hq).Sold(true) is { Price:500m,PerDay:1m },"last lower than average, HQ");
            Check(Quote(nq,asked).Sold(true) is { Price:100m },"no HQ sale: NQ");
            Check(Quote(new(null,70m,null),asked).Sold(false) is { Price:70m,PerDay:null },"only the last sale");
        }),
        ("Sold price: both long lists use it",() =>
        {
            Check(Read("MainWindow.CraftFromStock.cs").Contains("quote.Sold(") && !Read("MainWindow.CraftFromStock.cs").Contains("quote.Hq.Minimum"),"what can I craft");
            Check(Read("MainWindow.CurrencyValue.cs").Contains("quote.Sold(offer.Sale.Hq)") && !Read("MainWindow.CurrencyValue.cs").Contains("quote.Nq.Minimum"),"what to do with it");
        }),
        ("Market pace: a long list stays in memory",() =>
        {
            Check(MarketService.CacheCapacity>=1000,"capacity");
            Check(!Read("Market/MarketService.cs").Contains(">=128") && !Read("Market/MarketService.cs").Contains(">= 128"),"the old cap is gone");
        }),
        ("Market pace: a long list asks nothing more for an item with no price",() =>
        {
            Check(Read("Market/PreparationPriceSession.cs").Contains("state=new(context,list,copied.Length,0,false,null); }\n                        return;".Replace("\n",Environment.NewLine)) || Read("Market/PreparationPriceSession.cs").Replace("\r\n","\n").Contains("state=new(context,list,copied.Length,0,false,null); }\n                        return;"),"stops after its request");
        }),
    ];
}
