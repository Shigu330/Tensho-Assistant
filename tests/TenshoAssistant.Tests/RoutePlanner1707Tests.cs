using System.Numerics;
using TenshoAssistant.FreeCompany;

// X-604 (0.1707, end of the merge of the two route planners): submarines gain the airships' goals (experience per voyage, shortest, an
// item's loot, a sector to unlock), sectors chosen by hand and the fleet; the target list and the sector tick box are written once.
internal static class RoutePlanner1707Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static SubmarineSector Sector(uint id, bool start, float x, uint exp) => new(id, 1, start, 1, new Vector3(x, 0, 0), 10, 60, exp, "S" + id);
    private static readonly SubmarineSector[] Map = [Sector(1, true, 0, 0), Sector(2, false, 1000, 100), Sector(3, false, 2000, 300), Sector(4, false, 3000, 200)];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Route planner 1707: a target keeps only the routes through it, shortest first; none chosen: no route; no target: as before", () =>
        {
            var all = VoyagePlanner.Plan(Map, 10, 100, 1000, RouteGoal.ValuePerVoyage, id => id, 50);
            Check(all.Count == 7, "every set of the three sectors: " + all.Count);
            var through = VoyagePlanner.Plan(Map, 10, 100, 1000, RouteGoal.Loot, id => id, 50, default, null, [3u]);
            Check(through.Count == 4 && through.All(route => route.Sectors.Contains(3u)), "only the routes through 3: " + through.Count);
            Check(through.Zip(through.Skip(1)).All(pair => pair.First.Seconds <= pair.Second.Seconds), "shortest first");
            Check(VoyagePlanner.Plan(Map, 10, 100, 1000, RouteGoal.Unlock, id => id, 50, default, null, []).Count == 0, "no target to visit: no route");
            Check(VoyagePlanner.Plan(Map, 10, 100, 1000, RouteGoal.Shortest, id => id, 50).Count == all.Count, "shortest without a target: every route");
            var perVoyage = VoyagePlanner.Plan(Map, 10, 100, 1000, RouteGoal.ExperiencePerVoyage, id => id, 50);
            Check(perVoyage[0].Exp == 600 && perVoyage.Zip(perVoyage.Skip(1)).All(pair => pair.First.Exp >= pair.Second.Exp), "experience per voyage: the most first");
            Check(VoyagePlanner.Plan(Map, 10, 100, 1000, RouteGoal.ValuePerVoyage, id => id, 50, default, null, null).Count == all.Count, "null: no filter");
        }),
        ("Route planner 1707: the loot target from the community records of the map, the unlock target is the next sector to explore", () =>
        {
            var loot = new Dictionary<uint, SectorLoot> { [2] = new(2, 10, [(100u, 5L), (101u, 2L)]), [3] = new(3, 10, [(101u, 1L), (102u, 0L)]), [9] = new(9, 10, [(200u, 1L)]) };
            Check(string.Join(",", PlannerTargets.LootItems(loot, [2u, 3u, 4u])) == "100,101", "this map's items, once each, none never recorded");
            Check(string.Join(",", PlannerTargets.DropSectors(loot, [2u, 3u, 4u], 101)) == "2,3", "the sectors where it may drop");
            Check(PlannerTargets.DropSectors(loot, [2u, 3u, 4u], 200).Count == 0 && PlannerTargets.DropSectors(loot, [2u, 3u, 4u], 102).Count == 0, "another map's item, a total of 0: nowhere");
            Check(string.Join(",", PlannerTargets.UnlockVisit(SubmarineUnlockState.Path, [5u, 7u])) == "5", "the next sector to explore");
            Check(PlannerTargets.UnlockVisit(SubmarineUnlockState.Unknown, []).Count == 0 && PlannerTargets.UnlockVisit(SubmarineUnlockState.Open, []).Count == 0
                && PlannerTargets.UnlockVisit(SubmarineUnlockState.Unlocked, []).Count == 0, "unknown, open or unlocked: nothing to visit");
        }),
        ("Route planner 1707: wiring: the submarine target, the sectors chosen by hand and the fleet; one target list and one tick box for both kinds", () =>
        {
            var submarines = Read("MainWindow.RoutePlanner.cs");
            Check(submarines.Contains("var targets=DrawSubmarineTarget(catalog,unlockedList);", StringComparison.Ordinal) && submarines.Contains("25,token,excluded,targets));", StringComparison.Ordinal)
                && submarines.Contains("|{(targets is null ? \"-\":string.Join(\",\",targets))}\";", StringComparison.Ordinal), "the target reaches the search and its key");
            Check(submarines.Contains("DrawSubmarineChosen(catalog,inputs);", StringComparison.Ordinal) && submarines.Contains("DrawSubmarineFleet(catalog,targets,useUnlocked);", StringComparison.Ordinal), "chosen by hand, fleet");
            Check(submarines.Contains("return PlannerTargets.UnlockVisit(state,explore);", StringComparison.Ordinal), "the unlock target filters the routes now");
            Check(submarines.Contains("submarineChosen.Clear();submarineChosen.AddRange(route.Sectors);", StringComparison.Ordinal), "a route taken");
            Check(submarines.Contains("DrawSectorChoice(submarineChosen,sector.Id,VoyagePlanner.MaxSectors);", StringComparison.Ordinal), "the tick box shared");
            var airships = Read("MainWindow.AirshipPlanner.cs");
            Check(airships.Contains("chosen=DrawPlannerTarget(", StringComparison.Ordinal) && !airships.Contains("airshipTargetSearch", StringComparison.Ordinal), "the airships use the shared target list");
            Check(airships.Contains("private void DrawAirshipChoice(AirshipSector sector)=>DrawSectorChoice(airshipChosen,sector.Id,AirshipPlanner.MaxSectors);", StringComparison.Ordinal), "and the shared tick box");
            Check(Read("MainWindow.Lifetime.cs").Contains("submarineFleetSearch.Cancel();", StringComparison.Ordinal), "the fleet's search stops with the window");
            var en = Read("Localization/Languages/en.json"); var fr = Read("Localization/Languages/fr.json");
            foreach (var key in new[] { "LootItemTip", "Sectors", "ChooseHint", "Chosen", "Fleet", "FleetTip", "FleetNone", "FleetRoute", "FleetUseTip" })
                Check(en.Contains($"    \"{key}\": ", StringComparison.Ordinal) && fr.Contains($"    \"{key}\": ", StringComparison.Ordinal), "text: " + key);
        }),
    ];
}
