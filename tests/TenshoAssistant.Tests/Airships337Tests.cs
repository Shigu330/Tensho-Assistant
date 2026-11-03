using TenshoAssistant.FreeCompany;

// 0.337 (DEC-023, point 3, user's go-ahead): airship routes by distance and duration from the game's leg table; loot value unknown.
internal static class Airships337Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static AirshipSector Sector(uint id,int rank,uint exp) => new(id,$"S{id}",$"S{id}",rank,1,13,180,exp,null);

    // A line of points 127 ─ 0 ─ 1 ─ 2: each step is 10 distance and 1 000 s; surveys 500 s.
    private static AirshipLegTable Table()
    {
        var position = new Dictionary<uint,int> { [AirshipPlanner.Start] = 0,[0] = 1,[1] = 2,[2] = 3 };
        var legs = new Dictionary<(uint,uint),(uint,uint)>();
        foreach (var from in position.Keys) foreach (var to in position.Keys.Where(point => point != AirshipPlanner.Start && point != from))
            legs[(from,to)] = ((uint)(Math.Abs(position[from] - position[to]) * 1000),(uint)(Math.Abs(position[from] - position[to]) * 10));
        return new(100,legs,new Dictionary<uint,uint> { [0] = 500,[1] = 500,[2] = 500 });
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Airship planner: a voyage adds the legs from the start, each survey and its survey distance, and 12 h; no return leg",() =>
        {
            var table = Table();
            Check(AirshipPlanner.Measure(table,[Sector(0,1,10),Sector(1,1,10)]) == (10 + 13 + 10 + 13,43200 + 1000 + 500 + 1000 + 500),"two sectors in a line");
            var best = AirshipPlanner.Best(table,[Sector(2,1,30),Sector(0,1,10)])!;
            Check(best.Sectors.SequenceEqual([0u,2u]) && best.Distance == 10 + 13 + 20 + 13 && best.Exp == 40 && best.Tanks == 2,"shortest order first, experience and tanks added");
            Check(AirshipPlanner.Measure(table,[Sector(9,1,10)]) is null,"a leg the game did not give: unknown, never zero");
            Check(AirshipPlanner.Best(table,[]) is null,"no sector: no route");
        }),
        ("Airship planner: routes respect the rank and the range, ranked by the goal",() =>
        {
            var table = Table();
            var sectors = new[] { Sector(0,1,100),Sector(1,1,100),Sector(2,10,1000) };
            var low = AirshipPlanner.Plan(sectors,table,5,1000,AirshipGoal.ExpPerVoyage,10);
            Check(low.All(route => !route.Sectors.Contains(2u)) && low.Count == 3,"rank 5: sector 2 left out; {0},{1},{0,1}");
            var all = AirshipPlanner.Plan(sectors,table,50,1000,AirshipGoal.ExpPerVoyage,10);
            Check(all[0].Exp == 1200 && all.Count == 7,"every set, the most experience first");
            Check(AirshipPlanner.Plan(sectors,table,50,23,AirshipGoal.ExpPerVoyage,10).All(route => route.Distance <= 23),"range kept");
            var shortest = AirshipPlanner.Plan(sectors,table,50,1000,AirshipGoal.Shortest,10);
            Check(shortest[0].Seconds <= shortest[^1].Seconds,"shortest first");
        }),
        ("Airship planner: the game's functions are called on the framework thread only, while logged in, after a pointer check, guarded",() =>
        {
            var table = Read("FreeCompany/AirshipVoyageTable.Dalamud.cs");
            Check(table.Contains("framework.Update+=OnUpdate;",StringComparison.Ordinal) && table.Contains("framework.Update-=OnUpdate;",StringComparison.Ordinal),"framework thread, removed on dispose");
            var check = table.IndexOf("(nint)HousingManager.MemberFunctionPointers.GetAirshipVoyageTimeAndDistance==0",StringComparison.Ordinal);
            var call = table.IndexOf("HousingManager.GetAirshipVoyageTimeAndDistance((byte)",StringComparison.Ordinal);
            Check(check > 0 && call > check && table.Contains("(nint)HousingManager.MemberFunctionPointers.GetAirshipSurveyDuration==0",StringComparison.Ordinal),"pointers checked before any call");
            Check(table.IndexOf("client.IsLoggedIn",StringComparison.Ordinal) is > 0 and var logged && logged < call,"only while logged in");
            Check(table.Contains("catch(Exception error)",StringComparison.Ordinal) && table.Contains("failed=true",StringComparison.Ordinal),"a failure leaves it unavailable");
            var page = Read("MainWindow.AirshipPlanner.cs");
            Check(page.Contains("Task.Run(()=>AirshipPlanner.Plan(",StringComparison.Ordinal) && page.Contains("AirshipPlanner.Unconfirmed",StringComparison.Ordinal),"searched on a worker, shown as not confirmed");
            Check(!page.Contains("HousingManager",StringComparison.Ordinal),"the page never calls the game");
            Check(Read("Plugin.cs").Contains("new FreeCompany.AirshipVoyageTable(framework,client,log),()=>airshipTable?.Dispose());",StringComparison.Ordinal),"owned and disposed");
        }),
        ("Airship planner (0.337 review): a sector of unknown experience makes a lower bound, left out of the experience goals",() =>
        {
            var table = Table();
            var sectors = new[] { Sector(0,1,100),Sector(1,1,0) };
            var mixed = AirshipPlanner.Best(table,sectors)!;
            Check(mixed.UnknownExp == 1 && mixed.Exp == 100,"unknown experience counted apart, never as a known 0");
            Check(AirshipPlanner.Plan(sectors,table,50,1000,AirshipGoal.ExpPerDay,10).All(route => route.UnknownExp == 0),"left out of experience per day");
            Check(AirshipPlanner.Plan(sectors,table,50,1000,AirshipGoal.Shortest,10).Any(route => route.UnknownExp > 0),"kept for shortest");
        }),
        ("Route and airship pages (0.337 review): fetched route prices kept apart, band-aware missing count, guards and cleanup",() =>
        {
            var routes = Read("MainWindow.RoutePlanner.cs");
            Check(routes.Contains("routeQuotes[(fetching.WorldId,id)]=quote;",StringComparison.Ordinal) && routes.Contains("noListing.Add(item)",StringComparison.Ordinal),"quotes kept, no listing is not missing");
            Check(routes.Contains("RouteMissing(catalog,counted,route,plannerBand)",StringComparison.Ordinal),"missing count follows the band");
            Check(routes.Contains("!ReferenceEquals(prices.Items,routeQuotesList)",StringComparison.Ordinal) && routes.Contains("if(!prices.Loading)plannerKey=null;",StringComparison.Ordinal),"a list ended between two frames still re-runs the search");
            var table = Read("FreeCompany/AirshipVoyageTable.Dalamud.cs");
            Check(table.Contains("if(duration!=0)survey[to]=duration;",StringComparison.Ordinal),"a 0 survey stays unknown");
            Check(Read("MainWindow.Lifetime.cs").Contains("airshipPlanCancel?.Cancel();",StringComparison.Ordinal),"airship search stops with the window");
            var page = Read("MainWindow.AirshipPlanner.cs");
            Check(page.Contains("Tip(\"AirshipPlanner.ClearTip\");",StringComparison.Ordinal) && page.Contains("Tip(\"AirshipPlanner.ChooseTip\");",StringComparison.Ordinal),"tooltips, also when disabled");
        }),
    ];
}
