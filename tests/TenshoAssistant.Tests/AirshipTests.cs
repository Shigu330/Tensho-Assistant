// Test audit of 2026-11-09, batch 2 (0.1021): AirshipLog919Tests, AirshipLoot1003Tests, Airships336Tests, Airships337Tests merged into AirshipTests; every case is kept.
using TenshoAssistant.FreeCompany;
internal static class AirshipTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Data(string file) => Path.GetFullPath("src/TenshoAssistant/FreeCompany/Data/" + file);
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
        // From AirshipLog919Tests: T26 (0.919): the airship's last route from the voyage log: sector names, longest first, order of first appearance, unknown text ignored.
        ("Airship log 919: sectors in order, « secteur 1 » not inside « secteur 10 », control bytes and unknown names ignored, saved and compared",() =>
        {
            var sectors=new (uint,string)[]{(0,"Mer de nuages - secteur 1"),(9,"Mer de nuages - secteur 10"),(3,"Mer de nuages - secteur 4"),(5,""),};
            var lines=new[]{
                "Montagnes d'Abalathia franchies. Mettons le cap vers \u201cMer de nuages - secteur 1\u201d.",
                "\u201cMer de nuages - secteur 1\u201d atteint. Commençons exploration préliminaire.",
                "H\u0002%I\u0003Cristaux de ventIH découverts.",
                "Cap vers \u201cMer de nuages - secteur 10\u201d.",
                "Cap vers \u201cMer de nuages - secteur 4\u201d puis \u201cMer de nuages - secteur 10\u201d.",
                "",
            };
            var route=AirshipLogRoute.Sectors(lines,sectors);
            Check(route.SequenceEqual(new uint[]{0,9,3}),"route: "+string.Join(",",route));
            Check(AirshipLogRoute.Sectors([],sectors).Count==0 && AirshipLogRoute.Sectors(["Rien"],sectors).Count==0,"nothing found");
            var a=new VoyageVessel{Kind=VesselKind.Airship,LastRoute=["A"]};var b=a with {LastRoute=["B"]};
            Check(!Voyages.SameAs([a],[b]) && Voyages.SameAs([a],[a with {}]),"last route compared");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(airRead.Count==1)") && watcher.Contains("var lastRouteRead=AirshipLastRoute(workshop,houseId);"),"single airship only");
        }),
        // From AirshipLoot1003Tests: X-73 (0.1003): airship finds paired with the voyage log's sectors, only when stops and sectors match; a new discovery kind known everywhere.
        ("Airship loot 1003: stops with finds paired in order with the route, nothing when the counts differ",() =>
        {
            // The user's voyage of 2026-11-08 18:50: stop 1 gave wind crystals (10) and 7588, stop 2 gave 12224.
            var stops=new[]{(10u,19,7588u,15),(12224u,6,0u,0),(0u,0,0u,0),(0u,0,0u,0),(0u,0,0u,0)};
            var paired=AirshipLogRoute.Pair(stops,[1u,2u]);
            Check(paired is { Count:3 } && paired[0]==(10u,1u,19) && paired[1]==(7588u,1u,15) && paired[2]==(12224u,2u,6),"paired");
            Check(AirshipLogRoute.Pair(stops,[1u])==null && AirshipLogRoute.Pair(stops,[1u,2u,3u])==null,"counts differ");
        }),
        ("Airship loot 1003: the kind is labelled, an item source, known by the merge tool; unattributed gains logged in developer mode",() =>
        {
            Check(Enum.IsDefined(TenshoAssistant.Discoveries.DiscoveryKind.AirshipDrop),"kind");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(page.Contains("DiscoveryKind.AirshipDrop=>localization.Format(\"DiscoveriesUi.FromAirshipSector\"") && page.Contains("DiscoveryKind.VoyageDrop,DiscoveryKind.AirshipDrop,"),"label, source");
            // 0.1019 (test audit #11, #51, #52): the merge tool knows exactly the kinds of the plugin, read from its KINDS line.
            var kinds=System.Text.RegularExpressions.Regex.Match(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")),@"(?m)^KINDS=set\(range\(1,(\d+)\)\)-\{([\d,]*)\}");
            Check(kinds.Success,"merge tool: KINDS line");
            var excluded=kinds.Groups[2].Value.Split(',',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
            var tool=Enumerable.Range(1,int.Parse(kinds.Groups[1].Value)-1).Where(kind=>!excluded.Contains(kind)).ToHashSet();
            var plugin=Enum.GetValues<TenshoAssistant.Discoveries.DiscoveryKind>().Select(kind=>(int)kind).Where(kind=>kind>0).ToHashSet();
            Check(tool.SetEquals(plugin),"merge tool kinds = DiscoveryKind: only tool "+string.Join(",",tool.Except(plugin))+", only plugin "+string.Join(",",plugin.Except(tool)));
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs")).Contains("Item gain not attributed:"),"diagnosis");
        }),
        // From Airships336Tests: 0.336 (DEC-023, point 3): the airship unlock tree and possible loot of LuminaSupplemental (GPL-3.0) are embedded with their notice,
        // read as published, and shown without quantity or value.
        ("Airships: embedded unlock tree and drops carry source, commit, hash and GPL-3.0 notice, and read as published",() =>
        {
            foreach (var file in new[] { "airship-unlock.csv","airship-drop.csv" })
            {
                var text = File.ReadAllText(Data(file));
                Check(text.Contains("bd265c2b39868387937ae5b0c038593af997e0f9",StringComparison.Ordinal) && text.Contains("GPL-3.0",StringComparison.Ordinal)
                    && text.Contains("SHA-256",StringComparison.Ordinal),file + ": notice");
            }
            using var unlockFile = new StreamReader(Data("airship-unlock.csv"));
            var unlocks = AirshipSectors.ReadUnlocks(unlockFile);
            Check(unlocks.Count == 24 && !unlocks.ContainsKey(22),"24 points, 22 (old Diadem) absent");
            Check(unlocks[3] == new AirshipUnlock(1,0,7) && unlocks[16] == new AirshipUnlock(15,90,50),"rows kept as published");
            using var dropFile = new StreamReader(Data("airship-drop.csv"));
            var drops = AirshipSectors.ReadDrops(dropFile);
            Check(drops.Values.Sum(items => items.Count) > 200 && drops[0].Contains(12224u),"drops read, point 0 has item 12224");
        }),
        ("Airships: the unlock path walks back to a starting sector and stops on loops or unknown links",() =>
        {
            AirshipSector Sector(uint id,AirshipUnlock? unlock) => new(id,$"S{id}",$"S{id}",1,1,13,180,0,unlock);
            var catalog = new AirshipCatalog
            {
                Sectors = [Sector(0,new(0,0,1)),Sector(1,new(0,0,1)),Sector(3,new(1,0,7)),Sector(5,new(9,0,10)),Sector(6,new(7,0,1)),Sector(7,new(6,0,1))],
                Drops = new Dictionary<uint,IReadOnlyList<uint>>(),
            };
            Check(AirshipSectors.UnlockPath(catalog,3).SequenceEqual([0u,1u,3u]),"3 ← 1 ← 0");
            Check(AirshipSectors.UnlockPath(catalog,0).SequenceEqual([0u]),"a sector unlocking itself starts the tree");
            Check(AirshipSectors.UnlockPath(catalog,5).SequenceEqual([5u]),"unknown link: stops");
            Check(AirshipSectors.UnlockPath(catalog,6).SequenceEqual([7u,6u]),"loop: stops");
        }),
        ("Airships: the page shows the loot as a list without value, and the embedded files are credited",() =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.AirshipSectors.cs"));
            Check(page.Contains("AirshipSectors.LootTitle",StringComparison.Ordinal) && !page.Contains("PriceLow",StringComparison.Ordinal),"no value computed");
            Check(page.Contains("airshipViewKey!=key",StringComparison.Ordinal),"filtered only when an input changes");
            var credits = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Credits/CreditCatalog.cs"));
            Check(credits.Contains("\"FreeCompany/Data/airship-unlock.csv\", \"FreeCompany/Data/airship-drop.csv\"",StringComparison.Ordinal),"credited");
        }),
        // From Airships337Tests: 0.337 (DEC-023, point 3, user's go-ahead): airship routes by distance and duration from the game's leg table; loot value unknown.
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
            Check(table.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);",StringComparison.Ordinal) && table.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);",StringComparison.Ordinal),"framework thread, removed on dispose");
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
