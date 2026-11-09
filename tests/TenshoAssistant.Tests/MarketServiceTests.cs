// Test audit of 2026-11-09, batch 2 (0.1021): Market346Tests, MarketBatch608Tests, MarketPace710Tests merged into MarketServiceTests; every case is kept.
using TenshoAssistant.Market;
using TenshoAssistant.UI;
internal static class MarketServiceTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static bool HasCode(string text,string code)=>System.Text.RegularExpressions.Regex.Replace(text,@"\s+","").Contains(System.Text.RegularExpressions.Regex.Replace(code,@"\s+",""),StringComparison.Ordinal);
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static string ReadLf(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Market346Tests: 0.346 (g, I-06 and I-07): NPC sale price in the item information; lowest offer minus 1 for the clipboard, own retainers left out.
        ("Undercut: lowest other offer minus 1, own retainers left out, at least 1 gil, nothing when only own offers",() =>
        {
            var own = new HashSet<ulong> { 7 };
            Check(Undercut.Price([new(500,false,1),new(450,true,2),new(400,false,7)],own) == (450u,449u,true),"own 400 ignored, 450 HQ is the lowest");
            Check(Undercut.Price([new(1,false,3)],own) == (1u,1u,false),"never under 1 gil");
            Check(Undercut.Price([new(400,false,7)],own) is null && Undercut.Price([],own) is null,"only own offers, or none: nothing copied");
            var watcher = Read("Market/UndercutWatcher.Dalamud.cs");
            Check(watcher.Contains("board.OfferingsReceived+=OnOfferings;",StringComparison.Ordinal) && watcher.Contains("board.OfferingsReceived-=OnOfferings;",StringComparison.Ordinal),"subscribed and unsubscribed");
            Check(watcher.Contains("!configuration.UndercutClipboard",StringComparison.Ordinal) && !new TenshoAssistant.Configuration().UndercutClipboard,"off by default, honoured");
            Check(Read("Plugin.cs").Contains("undercut?.FlushClipboard()",StringComparison.Ordinal),"clipboard written from the draw (ImGui)");
        }),
        ("NPC sale: unit price, and the hovered stack's, only for items sold to NPCs",() =>
        {
            var hover = Read("MainWindow.MarketHover.cs");
            Check(hover.Contains("if(item is { SellPrice:>0 } sold)",StringComparison.Ordinal) && hover.Contains("HoverUi.NpcSaleStack",StringComparison.Ordinal),"price 0 = not sold, stack line");
        }),
        // From MarketBatch608Tests: 0.608 (I4): prices read by batch (docs/evidence/0.608-universalis-batch.md).
        ("Market batch: chunks of at most 100, without repeats or empty ids, only when two or more are missing",() =>
        {
            Check(MarketService.BatchIds([1,2,2,0,3],_=>false) is [1,2,3],"distinct, no zero");
            Check(MarketService.BatchIds([1,2,3],id=>id!=3).Length==0,"one missing: the usual request does it");
            Check(MarketService.BatchIds(Enumerable.Range(1,150).Select(id=>(uint)id).ToArray(),_=>false).Length==MarketService.BatchMaximum,"never past what the server answers");
            Check(MarketService.BatchMaximum==100,"the proven limit");
        }),
        ("Market batch: a result per id, an item without a price is left to the usual request",() =>
        {
            var json="""{"results":[{"itemId":5,"nq":{},"hq":{}}],"failedItems":[9]}""";
            var quote=MarketQuote.Parse(json,56,5,DateTimeOffset.UtcNow);
            Check(quote.ItemId==5,"read from the same response");
            try{MarketQuote.Parse(json,56,9,DateTimeOffset.UtcNow);throw new Exception("not refused");}
            catch(InvalidDataException){}
        }),
        ("Market batch: fills the cache first, then the list goes on as before",() =>
        {
            var session=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/PreparationPriceSession.cs"));
            Check(session.Contains("if(await market.FillCacheAsync(copied,context.CharacterId,context.WorldId,token).ConfigureAwait(false) is { } failed)throw"),"before the loop; a server failure ends the list");
            var service=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/MarketService.cs"));
            Check(service.Contains("api/v2/aggregated/{world}/{string.Join(',',missing)}"),"one request");
            Check(service.Contains("Enabled?.Invoke()==false"),"respects the source's switch");
        }),
        // From MarketPace710Tests: 0.710 (user request): faster on a long list, without troubling Universalis.
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
        ("Scrip sort: the list is worked out again when the collectables' sheets arrive",() =>
        {
            var page=Read("MainWindow.CraftFromStock.cs");
            Check(page.Contains("!ReferenceEquals(known.Scrips,scripCatalog)"),"key");
            Check(page.Contains("CraftStock.ScripsLoading"),"says so while loading");
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
            Check(HasCode(Read("Market/PreparationPriceSession.cs"),"state=new(context,list,copied.Length,0,false,null); } return;"),"stops after its request");
        }),
        ("0.567: the market page copies the lowest offer minus 1, without your retainers",() =>
        {
            var board=ReadLf("MainWindow.MarketBoard.cs");
            Check(board.Contains("!ownRetainers.Contains(offer.Retainer)") && board.Contains("()=>lowest.Paste.ToString("),"only copied, own retainers left out");
        }),
    ];
}
