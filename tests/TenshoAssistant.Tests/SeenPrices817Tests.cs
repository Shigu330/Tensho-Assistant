using TenshoAssistant.Market;

// 0.817 (X-52, user request): the prices seen at the market board in place of Universalis' for the player's world when newer.
internal static class SeenPrices817Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now=new(2026,11,7,22,0,0,TimeSpan.Zero);
    private static SeenMarketEntry Entry()=>new()
    {
        World=33,Item=5000,
        Listings=[new(120,false,3),new(100,false,5),new(400,true,1)],ListingsAt=Now.AddMinutes(-1),
        Sales=[new(110,false,2,Now.AddDays(-1)),new(90,false,4,Now.AddDays(-2)),new(380,true,1,Now.AddDays(-4))],SalesAt=Now.AddMinutes(-1),
    };
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Seen prices: lowest offer, last sale, average and sales a day per quality, as Universalis gives them",() =>
        {
            var nq=SeenPrices.Quality(Entry().Listings,Entry().Sales,false,Now);
            Check(nq.Minimum==100 && nq.RecentSale==110 && nq.SaleAt==Now.AddDays(-1) && nq.Average==100,"NQ");
            Check(nq.Velocity==1.5m,"6 units over the 4 days the history covers");
            var hq=SeenPrices.Quality(Entry().Listings,Entry().Sales,true,Now);
            Check(hq.Minimum==400 && hq.RecentSale==380 && hq.Velocity==0.25m,"HQ");
            Check(SeenPrices.Quality([],[],false,Now) is { Minimum:null, RecentSale:null, Velocity:null },"nothing seen: unknown, not zero");
        }),
        ("Seen prices: used when newer than Universalis' data, which keeps the other worlds; never a name kept",() =>
        {
            var empty=new MarketQualityPrice(null,null,null);
            var old=new MarketQuote(33,5000,Now.AddHours(-1),Now.AddDays(-1),empty,empty,new MarketListing(95,34,Now));
            var seen=SeenPrices.Prefer(Entry(),old,Now);
            Check(seen is { Nq.Minimum:100 } && seen.NqCheapest?.WorldId==34,"seen, other worlds kept");
            var fresh=old with {UploadedAt=Now};
            Check(SeenPrices.Prefer(Entry(),fresh,Now) is null,"Universalis newer: Universalis");
            Check(SeenPrices.Prefer(Entry(),null,Now) is not null,"no Universalis and its source off: seen alone");
            Check(SeenPrices.Prefer(Entry(),null,Now,true) is null,"0.821: no Universalis yet but its source on: wait for it, so it is asked and its other worlds come");
            var store=new SeenPriceStore();
            store.SeeListings(33,5000,7,[new(100,false,1)],Now);store.SeeListings(33,5000,7,[new(90,false,1)],Now);
            Check(store.Get(33,5000)!.Listings.Count==2,"pages of one request add up");
            store.SeeListings(33,5000,8,[new(80,false,1)],Now);
            Check(store.Get(33,5000)!.Listings.Count==1,"a new request replaces");
            Check(!typeof(SeenListing).GetProperties().Any(property=>property.Name.Contains("Name") || property.Name.Contains("Retainer")),"no name");
        }),
        ("Seen prices: read from the market board events, cut in PvP, behind their setting; the market service asks them first",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("Market/SeenPriceReader.Dalamud.cs").Contains("TenshoAssistant.Competitive.CompetitiveGate.Cut || !enabled()"),"cut, setting");
            Check(Read("Market/MarketService.cs").Contains("return Seen?.Invoke(worldId, itemId, cached) ?? cached;"),"service");
            Check(new TenshoAssistant.Discoveries.DiscoverySettings().MarketPrices,"on by default");
        }),
    ];
}
