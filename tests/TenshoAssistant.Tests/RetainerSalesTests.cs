using TenshoAssistant.Market;

// 0.508, I-05 (issue #26): a retainer's sale list against other sellers. No data is never « you are the lowest », and nothing in the game is changed.
internal static class RetainerSalesTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.Combine(Path.GetFullPath("src/TenshoAssistant"),file));
    private static readonly DateTimeOffset At=new(2026,11,4,12,0,0,TimeSpan.Zero);
    private static MarketOffer Offer(long price,bool hq,string retainer)=>new(price,1,hq,"Moogle",retainer,0,null);
    private static RetainerItemOffers Offers(params MarketOffer[] offers)=>new(offers,At);
    private static readonly IReadOnlySet<string> Own=new HashSet<string>{"Mine"};
    private static RetainerListing Listing(long? price,bool hq=false)=>new(0,5,hq,1,price);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Retainer sales: prices as the game writes them; item and quality from the raw number",() =>
        {
            Check(RetainerSales.ParsePrice("1,234")==1234 && RetainerSales.ParsePrice("1 234 ")==1234 && RetainerSales.ParsePrice("1.234.567")==1234567,"separators and the gil sign");
            foreach(var bad in new string?[]{null,"","abc","0","12345678901","1000000000"})Check(RetainerSales.ParsePrice(bad) is null,"not a price: "+bad);
            Check(RetainerSales.SplitItem(5057)==(5057u,false) && RetainerSales.SplitItem(1005057)==(5057u,true),"NQ and HQ");
        }),
        ("Retainer sales: other sellers of the same quality only; no data is never « lowest »",() =>
        {
            Check(RetainerSales.Compare(Listing(null),Offers(Offer(5,false,"Other")),Own).Kind==RetainerVerdictKind.PriceUnknown,"your price unread");
            Check(RetainerSales.Compare(Listing(100),null,Own).Kind==RetainerVerdictKind.Unknown,"not asked");
            Check(RetainerSales.Compare(Listing(100),new([],null),Own).Kind==RetainerVerdictKind.Unknown,"never uploaded");
            Check(RetainerSales.Compare(Listing(100),Offers(),Own).Kind==RetainerVerdictKind.Alone,"uploaded, nobody else");
            Check(RetainerSales.Compare(Listing(100),Offers(Offer(50,false,"Mine")),Own).Kind==RetainerVerdictKind.Alone,"your own retainers do not count");
            Check(RetainerSales.Compare(Listing(100,true),Offers(Offer(50,false,"Other")),Own).Kind==RetainerVerdictKind.Alone,"another quality does not count");
            var undercut=RetainerSales.Compare(Listing(100),Offers(Offer(150,false,"A"),Offer(80,false,"B")),Own);
            Check(undercut is {Kind:RetainerVerdictKind.Undercut,Lowest:80,Gap:20,Competitor:"B",Paste:79},"undercut");
            Check(RetainerSales.Compare(Listing(100),Offers(Offer(100,false,"A")),Own) is {Kind:RetainerVerdictKind.Tied,Paste:99},"tied");
            var lowest=RetainerSales.Compare(Listing(100),Offers(Offer(200,false,"A")),Own);
            Check(lowest is {Kind:RetainerVerdictKind.Lowest,Lowest:200,Paste:null},"lowest: nothing to copy, it would raise your price");
            Check(RetainerSales.Compare(Listing(100),Offers(Offer(1,false,"A")),Own).Paste==1,"never under 1");
            var cut=Enumerable.Range(0,MarketBoard.MaximumOffers).Select(index=>Offer(10+index,false,"Other")).ToArray();
            Check(RetainerSales.Compare(Listing(100,true),Offers(cut),Own).Kind==RetainerVerdictKind.Unknown,"a list cut at 100 rows of another quality says nothing");
        }),
        ("Retainer sales: Universalis' two answers, and only what was asked",() =>
        {
            uint[] two=[5,6];
            var many=RetainerSales.ParseMany("""{"items":{"5":{"lastUploadTime":1700000000000,"listings":[{"pricePerUnit":10,"quantity":2,"hq":true,"retainerName":"A","worldName":"Moogle"}]},"6":{"lastUploadTime":0,"listings":[]},"7":{"lastUploadTime":1,"listings":[]}},"unresolvedItems":[]}""","Moogle",two,At);
            Check(many.Count==2 && !many.ContainsKey(7),"only what was asked");
            Check(many[5].DataAt==DateTimeOffset.FromUnixTimeMilliseconds(1700000000000) && many[5].Offers.Single() is {UnitPrice:10,Hq:true,Retainer:"A"},"a row");
            Check(many[6].DataAt is null,"never uploaded");
            var one=RetainerSales.ParseMany("""{"lastUploadTime":1700000000000,"listings":[{"pricePerUnit":3,"quantity":1,"hq":false,"retainerName":"B"}]}""","Moogle",[5],At);
            Check(one[5].Offers.Single().UnitPrice==3,"one item at the root");
            Check(RetainerCompareService.Address("Moogle",two)=="https://universalis.app/api/v2/Moogle/5,6?listings=100&entries=0","address");
        }),
        ("Retainer sales: a request on a click, not twice in 3 s, not with the source off",() =>
        {
            using var service=new RetainerCompareService(new HttpClient(new Refuse()));
            Check(!service.Request("Moogle",[],At),"nothing to ask");
            Check(!service.Request("",[5],At),"no world");
            service.Enabled=()=>false;
            Check(!service.Request("Moogle",[5],At),"source off");
            service.Enabled=()=>true;
            Check(service.Request("Moogle",[5],At),"sent");
            Check(!service.Request("Moogle",[5],At.AddSeconds(1)),"too soon or still running");
        }),
        ("Retainer sales: read-only, with nothing asked by itself",() =>
        {
            var reader=Read("Market/RetainerSalesReader.Dalamud.cs");
            var panel=Read("MainWindow.RetainerSales.cs");
            foreach(var word in new[]{"SetRetainerMarketPrice","MoveToRetainerMarket","MoveFromRetainerMarket","OpenRetainerSell","FireCallback","ReceiveEvent","SetAtkValues","SendCommand"})Check(!reader.Contains(word) && !panel.Contains(word),"changes nothing: "+word);
            Check(reader.Contains("agent->RetainerSellListAddonId != unit->Id") && reader.Contains("count > RetainerSales.MaximumListings") && reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate)"),"window confirmed, rows bounded, cleaned up");
            Check(panel.Split("RetainerCompare?.Request(").Length==2 && panel.Contains("if (ImGui.SmallButton(localization.Get(\"RetainerSales.Compare\")))"),"asked on the click only");
            Check(!reader.Contains("RetainerCompare") && !reader.Contains("Request("),"never by the reader");
            Check(panel.Contains("()=>paste.ToString());"),"the only write");
            Check(Read("Plugin.cs").Contains("(\"RetainerSales\",mainWindow.DrawRetainerSalesPanel)"),"drawn as an isolated panel");
        }),
    ];

    private sealed class Refuse:HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
    }
}
