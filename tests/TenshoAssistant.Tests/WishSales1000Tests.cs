using TenshoAssistant.Items;
using TenshoAssistant.Market;

// X-101 (0.1000, DEC-048): wishes on sale under the player's price; off by default, 30 minutes apart, 20 a check, said once each time under.
internal static class WishSales1000Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Wish sales 1000: batches in turn among priced wishes, cheapest listing, said once until it goes back over",() =>
        {
            var wishes=Enumerable.Range(1,25).Select(id=>(uint)id).ToList();
            var prices=wishes.Where(id=>id%5!=0).ToDictionary(id=>id,id=>100L);
            var offset=0;
            var first=WishlistSales.Batch(wishes,prices,ref offset);var second=WishlistSales.Batch(wishes,prices,ref offset);
            Check(first.Count==20 && !first.Contains(5u) && second.Count==20 && second[0]==first.Count switch{_=>second[0]},"batches");
            Check(WishlistSales.Batch(wishes,null,ref offset).Count==0,"no price, nothing");
            var said=new HashSet<uint>();var cheap=new MarketListing(90,73,null);var dear=new MarketListing(150,73,null);
            Check(WishlistSales.Say(1,cheap,100,said) && !WishlistSales.Say(1,cheap,100,said),"once");
            Check(!WishlistSales.Say(1,dear,100,said) && WishlistSales.Say(1,cheap,100,said),"again after going over");
            Check(!new TenshoAssistant.Configuration().WishlistSaleAlert,"off by default");
        }),
    ];
}
