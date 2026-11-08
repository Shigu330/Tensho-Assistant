// 0.879 (X-75.5): region cheapest completed by the worlds seen, festivals' days and vistas' weathers seen shown on their pages.
using TenshoAssistant.Market;
internal static class SeenPages879Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Seen pages: a cheaper offer seen on a region world replaces Universalis' region cheapest; events and vistas read the discoveries",() =>
        {
            var now=new DateTimeOffset(2026,11,8,12,0,0,TimeSpan.Zero);
            var store=new SeenPriceStore();
            store.SeeListings(400,5,1,[new SeenListing(50,false,1)],now.AddHours(-1));
            var quote=new MarketQuote(33,5,now.AddHours(-2),now.AddHours(-2),new(null,null,null,null,null),new(null,null,null,null,null),null,null,new MarketListing(80,401,now.AddHours(-3)),null);
            var shown=store.WithRegion(quote,[400,401],now)!;
            Check(shown.NqRegion is { Price:50,WorldId:400 },"region: seen world cheaper");
            Check(ReferenceEquals(store.WithRegion(quote,[],now),quote),"no worlds: unchanged");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("seenPrices.WithRegion(seenPrices.WithDataCenter("),"wired after the data centre");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Events.cs")).Contains("Events.SeenDays"),"events");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Contains("MapView.VistaSeen"),"vistas");
        }),
    ];
}
