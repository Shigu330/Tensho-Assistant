// 0.868 (X-73.12): the market page's offers and sales completed by what was seen at the game's board.
using TenshoAssistant.Market;
internal static class SeenBoard868Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now=new(2026,11,8,12,0,0,TimeSpan.Zero);
    private static SeenMarketEntry Entry(DateTimeOffset at,params SeenListing[] listings)=>
        new(){World=1,Item=5,Listings=[..listings],ListingsAt=at,Sales=[new(90,true,2,Now.AddHours(-3))],SalesAt=at};
    private static MarketBoard Board()=>new("Light",5,Now.AddMinutes(-5),
        [new(120,1,true,"Alpha","Someone",6,Now.AddHours(-2)),new(150,1,true,"Beta","Other",8,Now.AddHours(-1))],
        [new(90,2,true,"Alpha",Now.AddHours(-3))],1.5,0.5,1.0);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Seen board: a world seen after Universalis' review replaces its offers, marked seen, no seller; twins sales not doubled",() =>
        {
            var shown=SeenPrices.WithSeen(Board(),"Light",5,[("Alpha",Entry(Now.AddMinutes(-30),new SeenListing(100,true,3)))],Now)!;
            Check(shown.Offers.Count==2 && shown.Offers[0] is { UnitPrice:100,Seen:true,Retainer:"",World:"Alpha",Tax:15 },"Alpha replaced");
            Check(shown.Offers.Any(offer=>offer.World=="Beta" && !offer.Seen),"Beta kept");
            Check(shown.Sales.Count==1 && shown.UnitsPerDay==1.5,"the same sale once; Universalis' speed kept");
        }),
        ("Seen board: older than Universalis' review, or no longer current, changes nothing; no board stands alone",() =>
        {
            var board=Board();
            Check(ReferenceEquals(SeenPrices.WithSeen(board,"Light",5,[("Alpha",Entry(Now.AddHours(-2.5),new SeenListing(100,true,3)))],Now),board),"older");
            var stale=SeenPrices.WithSeen(board,"Light",5,[("Gamma",Entry(Now.AddHours(-30),new SeenListing(100,true,3)))],Now)!;
            Check(stale.Offers.All(offer=>!offer.Seen),"a day old: offers left out");
            var alone=SeenPrices.WithSeen(null,"Light",5,[("Gamma",Entry(Now.AddMinutes(-1),new SeenListing(100,false,4)))],Now)!;
            Check(alone.Offers.Single().Seen && alone.Sales.Count==1 && alone.UnitsPerDay is > 0,"alone, speed from its sales");
        }),
        ("Seen board: the page completes its board, and the undercut copy counts only Universalis' offers",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketBoard.cs"));
            Check(page.Contains("var completed=BoardWithSeen(shown,scopeName,item);") && page.Contains("if(shown is null && state.Loading)return null;"),"completed");
            Check(page.Contains("board.Offers.Where(offer=>(offer.Seen ? offer.Own==false:!ownRetainers.Contains(offer.Retainer))"),"undercut (0.870: seen offers known not yours)");
        }),
    ];
}
