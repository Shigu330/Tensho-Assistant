// 0.858 (X-73.2): sales merged from one opening to the next.
using TenshoAssistant.Market;
internal static class SalesMerge858Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now=new(2026,11,8,2,0,0,TimeSpan.Zero);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sales merge: kept and new together, twins once, older than 30 days dropped",() =>
        {
            SeenSale a=new(100,false,1,Now.AddDays(-1)),b=new(120,false,2,Now.AddHours(-2)),old=new(90,false,1,Now.AddDays(-40));
            var merged=SeenPrices.MergeSales([a,old],[a,b],Now);
            Check(merged.Count==2 && merged[0]==b && merged[1]==a,"two, newest first");
            var store=new SeenPriceStore();
            store.SeeSales(72,5,[a],Now);store.SeeSales(72,5,[b],Now);
            Check(store.Get(72,5)!.Sales.Count==2,"the store keeps both openings");
        }),
    ];
}
