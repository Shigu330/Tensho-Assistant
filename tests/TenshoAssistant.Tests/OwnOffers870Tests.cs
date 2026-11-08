// 0.870 (X-73.14): seen offers flagged as yours or not, for the retainer comparison and the undercut copy.
using TenshoAssistant.Market;
internal static class OwnOffers870Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now=new(2026,11,8,12,0,0,TimeSpan.Zero);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Own offers: yours left out of the comparison; unknown retainers, stale or older than Universalis give nothing",() =>
        {
            var store=new SeenPriceStore();
            store.SeeListings(1,5,7,[new SeenListing(100,false,1,true),new SeenListing(120,false,2)],Now.AddMinutes(-10),true);
            var offers=SeenPrices.RetainerOffers(store.Get(1,5),"Alpha",null,Now)!;
            Check(offers.Offers.Count==1 && offers.Offers[0] is { UnitPrice:120,Seen:true,Own:false },"yours left out");
            var verdict=RetainerSales.Compare(new RetainerListing(0,5,false,1,130),offers,new HashSet<string>());
            Check(verdict.Kind==RetainerVerdictKind.Undercut && verdict.Paste==119,"undercut by 10, paste 119");
            Check(SeenPrices.RetainerOffers(store.Get(1,5),"Alpha",new([],Now),Now) is null,"Universalis newer");
            Check(SeenPrices.RetainerOffers(store.Get(1,5),"Alpha",null,Now.AddDays(2)) is null,"stale");
            store.SeeListings(1,6,8,[new SeenListing(100,false,1)],Now,false);
            Check(SeenPrices.RetainerOffers(store.Get(1,6),"Alpha",null,Now) is null,"retainers unknown");
            Check(SeenPrices.WithSeen(null,"Alpha",6,[("Alpha",store.Get(1,6)!)],Now)!.Offers.Single().Own is null,"board: unknown stays unknown");
        }),
        ("Own offers: the reader compares retainer ids and keeps only the flag",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/SeenPriceReader.Dalamud.cs"));
            Check(reader.Contains("own?.Contains(listing.RetainerId)==true") && reader.Contains("own is { Count:>0 }"),"flag only");
            Check(!typeof(SeenListing).GetProperties().Any(property=>property.Name.Contains("Retainer") || property.Name.Contains("Name")),"no id kept");
        }),
    ];
}
