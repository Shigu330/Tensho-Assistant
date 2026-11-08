// 0.857 (X-73.1, user request): prices seen on the visited worlds complete Universalis' cheapest of the data center.
using TenshoAssistant.Market;
internal static class DataCenterSeen857Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now=new(2026,11,8,2,0,0,TimeSpan.Zero);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Data center: a visited world cheaper and current wins; Universalis' world gives way to a later sighting; old sightings do not count",() =>
        {
            var universalis=new MarketListing(1000,71,Now.AddHours(-3));
            Check(SeenPrices.Cheapest(universalis,[(72,800m,Now.AddHours(-1))],Now)==new MarketListing(800,72,Now.AddHours(-1)),"visited world cheaper");
            Check(SeenPrices.Cheapest(universalis,[(72,800m,Now.AddDays(-2))],Now)==universalis,"too old");
            Check(SeenPrices.Cheapest(universalis,[(71,1500m,Now.AddHours(-1))],Now)==new MarketListing(1500,71,Now.AddHours(-1)),"same world, later: its new price");
            Check(SeenPrices.Cheapest(universalis,[(71,(decimal?)null,Now.AddHours(-1))],Now) is null,"same world later, none of that quality left");
            Check(SeenPrices.Cheapest(null,[(72,800m,Now.AddHours(-1))],Now)?.WorldId==72,"Universalis off");
        }),
        ("Data center: the store completes a quote from its entries; old entries are not loaded",() =>
        {
            var store=new SeenPriceStore();
            store.SeeListings(72,5,1,[new SeenListing(700,false,1),new SeenListing(900,true,1)],DateTimeOffset.UtcNow);
            var quote=new MarketQuote(71,5,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,new(1000,null,null),new(null,null,null),new MarketListing(1000,71,DateTimeOffset.UtcNow.AddHours(-2)));
            var done=store.WithDataCenter(quote,[71u,72u],DateTimeOffset.UtcNow)!;
            Check(done.NqCheapest?.WorldId==72 && done.NqCheapest.Price==700 && done.HqCheapest?.Price==900,"completed");
            var old=new SeenPriceStore();
            old.Load([new SeenMarketEntry{World=72,Item=5,Listings=[new(1,false,1)],ListingsAt=DateTimeOffset.UtcNow.AddDays(-100)}]);
            Check(old.Count==0,"older than 90 days: dropped");
        }),
    ];
}
