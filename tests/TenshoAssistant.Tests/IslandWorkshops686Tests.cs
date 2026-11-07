using TenshoAssistant.Island;

// 0.686 (user report): one workshop built, four read.
internal static class IslandWorkshops686Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Island workshops: the reading of 2026-11-06 gives Fabrique I alone",() =>
        {
            var built=IslandValue.BuiltWorkshops([(0,1,false,232,1),(1,1,false,44,0),(2,1,false,255,0),(3,1,false,211,0)]);
            Check(built.Count==1 && built[0].Index==0,"one, the first");
        }),
        ("Island workshops: several marked are all kept, one under construction is shown, and with no mark the place decides",() =>
        {
            Check(IslandValue.BuiltWorkshops([(0,1,false,1,1),(1,1,false,2,2),(2,1,false,3,0),(3,1,false,0,0)]).Count==2,"two marked");
            var building=IslandValue.BuiltWorkshops([(0,1,false,1,1),(1,1,true,2,0)]);
            Check(building.Count==2 && building[1].UnderConstruction,"under construction");
            Check(IslandValue.BuiltWorkshops([(0,1,false,1,0),(1,1,false,0,0)]).Count==1,"no mark: the place");
        }),
    ];
}
