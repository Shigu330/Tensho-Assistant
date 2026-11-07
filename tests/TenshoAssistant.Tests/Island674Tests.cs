using TenshoAssistant.Island;

// N-10 (0.674): the groove's rise in an agenda's totals, and its cap.
internal static class Island674Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static IslandCraft Craft(uint id,int hours,int value,params uint[] themes)=>Island671Tests.Craft(id,hours,value,1,themes);
    private static IslandCatalog Catalog(params IslandCraft[] crafts)=>Island671Tests.Sample(crafts) with { GrooveCaps=[10,15,20,25,35,45] };
    private static IslandSnapshot Island(uint groove,int? landmarks,params IslandWorkshop[] workshops)=>Island671Tests.Snapshot(groove:groove,workshops:workshops) with { Landmarks=landmarks };
    private static readonly IslandWorkshop One=new(0,1,false),Two=new(1,1,false);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Island groove: the cap follows the landmarks, and stays above what the game gives",() =>
        {
            var catalog=Catalog(Craft(1,8,100,1));
            Check(IslandValue.GrooveCap(catalog,Island(0,0))==10,"no landmark, the sheet's first row");
            Check(IslandValue.GrooveCap(catalog,Island(0,1))==15,"1 landmark, 15 as on the wiki");
            Check(IslandValue.GrooveCap(catalog,Island(0,5))==45,"5 landmarks, 45 as on the wiki");
            Check(IslandValue.GrooveCap(catalog,Island(0,null))==45,"unknown count: the highest");
            Check(IslandValue.GrooveCap(catalog,Island(0,9))==45,"impossible count: the highest");
            Check(IslandValue.GrooveCap(catalog,Island(20,0))==20,"never under what the game gives");
            Check(IslandValue.GrooveCap(Island671Tests.Sample([Craft(1,8,100,1)]),Island(0,1))==IslandValue.DefaultGrooveCap,"no sheet");
        }),
        ("Island groove: an efficient craft raises it for what finishes later, not for itself",() =>
        {
            var catalog=Catalog(Craft(1,8,100,1),Craft(2,8,100,1));
            var agenda=IslandPlanner.Propose(catalog,Island(0,5,One));
            var steps=agenda.Chains[0].Steps;
            Check(steps.Count==3 && !steps[0].Efficient && steps[1].Efficient && steps[2].Efficient,"one chain of three, two efficient");
            Check(agenda.StartGroove==0 && agenda.EndGroove==2 && agenda.GrooveCap==45,"two points");
            var flat=IslandValue.Unit(steps[1].Craft,catalog,Island(0,5,One),1,0,0)!.Value;
            var raised=IslandValue.Unit(steps[1].Craft,catalog,Island(0,5,One),1,0,1)!.Value;
            Check(Math.Abs(raised/flat-1.01)<1e-9,"one point is 1%");
            Check(Math.Abs(steps.Sum(step=>step.Value)-agenda.Total)<1e-9,"the total is the sum of its steps");
        }),
        ("Island groove: crafts finishing in the same hour do not see each other",() =>
        {
            var catalog=Catalog(Craft(1,8,100,1),Craft(2,8,100,1),Craft(3,8,100,2),Craft(4,8,100,2));
            var agenda=IslandPlanner.Propose(catalog,Island(0,5,One,Two));
            Check(agenda.Chains.Count==2 && agenda.EndGroove==4,"two efficient crafts in each workshop");
            var first=agenda.Chains[0].Steps[1];var second=agenda.Chains[1].Steps[1];
            Check(first.Start==second.Start && first.Efficient && second.Efficient,"same hour");
            Check(first.Craft.Id==second.Craft.Id,"the same craft at the same hour, the case that matters here");
            Check(Math.Abs(first.Value-second.Value)<1e-9,"same groove and same supply for both");
        }),
        ("Island groove: the cap stops the rise and the value",() =>
        {
            var catalog=Catalog(Craft(1,8,100,1),Craft(2,8,100,1));
            var capped=IslandPlanner.Propose(catalog,Island(15,1,One));
            Check(capped.StartGroove==15 && capped.EndGroove==15 && capped.GrooveCap==15,"at the cap");
            var steps=capped.Chains[0].Steps;
            Check(Math.Abs(steps[1].Value-steps[2].Value)<steps[1].Value*0.5,"both computed");
            var free=IslandPlanner.Propose(catalog,Island(15,5,One));
            Check(free.EndGroove==17 && free.Total>capped.Total,"without the cap it rises and is worth more");
        }),
    ];
}
