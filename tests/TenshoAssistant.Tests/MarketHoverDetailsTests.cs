using TenshoAssistant.Items;
using TenshoAssistant.Market;

internal static class MarketHoverDetailsTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset Fetched=DateTimeOffset.FromUnixTimeMilliseconds(1700000100000);
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Data-center minimums keep the world that holds the listing and its own upload date",()=>
        {
            const string json="""
            {"results":[{"itemId":42,
              "nq":{"minListing":{"world":{"price":27000},"dc":{"price":25000,"worldId":80}}},
              "hq":{"minListing":{"world":{"price":40000},"dc":{"price":40000,"worldId":71}}},
              "worldUploadTimes":[{"worldId":71,"timestamp":1700000001000},{"worldId":80,"timestamp":1600000000000}]}]}
            """;
            var quote=MarketQuote.Parse(json,71,42,Fetched);
            Check(quote.Nq.Minimum==27000 && quote.UploadedAt==DateTimeOffset.FromUnixTimeMilliseconds(1700000001000),"current world unchanged");
            Check(quote.NqCheapest==new MarketListing(25000,80,DateTimeOffset.FromUnixTimeMilliseconds(1600000000000)),"cheaper world and its upload date");
            Check(quote.HqCheapest is { WorldId:71, Price:40000 },"cheapest may be the current world");
            Check(!MarketQuote.Recent(quote.NqCheapest!.UploadedAt,Fetched) && MarketQuote.Recent(quote.UploadedAt,Fetched),"staleness judged per world");
        }),
        ("A data-center minimum without an identified world or positive price is not invented",()=>
        {
            var noWorld=MarketQuote.Parse("""{"results":[{"itemId":42,"nq":{"minListing":{"world":{"price":120},"dc":{"price":2}}}}]}""",71,42,Fetched);
            var zero=MarketQuote.Parse("""{"results":[{"itemId":42,"nq":{"minListing":{"dc":{"price":0,"worldId":80}}}}]}""",71,42,Fetched);
            var missing=MarketQuote.Parse("""{"results":[{"itemId":42,"nq":{"minListing":{"world":{"price":5}}}}]}""",71,42,Fetched);
            Check(noWorld.NqCheapest is null && noWorld.Nq.Minimum==120 && zero.NqCheapest is null && missing.NqCheapest is null && missing.HqCheapest is null,"no attribution without evidence");
            var unknownUpload=MarketQuote.Parse("""{"results":[{"itemId":42,"nq":{"minListing":{"dc":{"price":9,"worldId":99}}}}]}""",71,42,Fetched);
            Check(unknownUpload.NqCheapest is { WorldId:99, UploadedAt:null },"missing world upload stays unknown");
        }),
        ("Market state names data-center worlds from a private copy and keeps them across item updates",()=>
        {
            using var market=new MarketService(new HttpClient());
            var names=new Dictionary<uint,string>{[71]="Moogle",[80]="Omega"};
            market.SetWorld(5,71,"Moogle","Chaos",names);names[80]="Changed";
            var state=market.State;
            Check(state.DataCenterName=="Chaos" && state.NameOf(80)=="Omega" && state.NameOf(71)=="Moogle" && state.NameOf(999) is null,"copied names");
            market.SetWorld(0,0,"");
            Check(market.State.WorldNames is null && market.State.DataCenterName=="","logout clears directory");
        }),
        ("Hover anchor follows the visible game tooltip and is dropped with the item",()=>
        {
            var catalog=ItemCatalog.Build([new ItemDefinition(42,"Carte",false)],new("t","fr","v",1));
            using var hover=new ItemHoverService();var now=DateTimeOffset.UtcNow;
            hover.Observe(true,5,42,catalog,now);hover.SetAnchor(new ItemHoverAnchor(100,200,320,180));
            Check(hover.State.Anchor==new ItemHoverAnchor(100,200,320,180),"anchor stored");
            hover.SetAnchor(new ItemHoverAnchor(100,200,0,180));
            Check(hover.State.Anchor is null,"implausible size refused");
            hover.SetAnchor(new ItemHoverAnchor(float.NaN,1,320,180));
            Check(hover.State.Anchor is null,"non-finite position refused");
            hover.SetAnchor(new ItemHoverAnchor(1,1,320,180));hover.Observe(true,5,42,catalog,now.AddMilliseconds(16));
            Check(hover.State.Anchor is not null,"same item keeps anchor");
            hover.Observe(true,5,0,catalog,now.AddMilliseconds(32));
            Check(hover.State.Anchor is null && hover.State.Item is null,"no item, no anchor");
        }),
    };
}
