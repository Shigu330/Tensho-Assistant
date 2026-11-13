using TenshoAssistant.FreeCompany;

// 0.1243 (X-201, user request of 2026-11-12, slice 1): the airship planner's goals « loot: an item » and « unlock a sector ». The routes
// kept visit a target (a sector where the item may drop, the sector whose voyage unlocks the chosen one), shortest first, a sector of
// unknown experience kept (the target is the point); the drop and unlock lists are LuminaSupplemental's (DEC-023).
internal static class AirshipTargets1243Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static AirshipSector Sector(uint id,int rank,uint exp,AirshipUnlock? unlock=null)=>new(id,$"Sector {id}",$"S{id}",rank,1,13,180,exp,unlock);

    // A line of points 127 ─ 0 ─ 1 ─ 2: each step is 10 distance and 1 000 s; surveys 500 s (as AirshipTests).
    private static AirshipLegTable Table()
    {
        var position=new Dictionary<uint,int>{[AirshipPlanner.Start]=0,[0]=1,[1]=2,[2]=3};
        var legs=new Dictionary<(uint,uint),(uint,uint)>();
        foreach(var from in position.Keys)foreach(var to in position.Keys.Where(point=>point!=AirshipPlanner.Start && point!=from))
            legs[(from,to)]=((uint)(Math.Abs(position[from]-position[to])*1000),(uint)(Math.Abs(position[from]-position[to])*10));
        return new(100,legs,new Dictionary<uint,uint>{[0]=500,[1]=500,[2]=500});
    }

    private static AirshipCatalog Catalog()=>new()
    {
        Sectors=[Sector(0,1,100),Sector(1,1,0,new(0,10,1)),Sector(2,5,300,new(1,20,5)),Sector(3,1,50,new(3,0,1))],
        Drops=new Dictionary<uint,IReadOnlyList<uint>>{[0]=[500u,501],[2]=[501u],[9]=[502u]},
    };

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Airship targets 1243: only the routes through a target, shortest first, unknown experience kept; no target given: no filter",() =>
        {
            var catalog=Catalog();var table=Table();
            var sectors=catalog.Sectors.Where(sector=>sector.Id<=2).ToArray();
            var loot=AirshipPlanner.Plan(sectors,table,50,1000,AirshipGoal.Loot,20,default,[2u]);
            Check(loot.Count>0 && loot.All(route=>route.Sectors.Contains(2u)),"every route visits the target: "+loot.Count);
            Check(loot.Zip(loot.Skip(1)).All(pair=>pair.First.Seconds<=pair.Second.Seconds),"shortest first");
            Check(loot.Any(route=>route.UnknownExp>0),"a sector of unknown experience kept for a target");
            Check(AirshipPlanner.Plan(sectors,table,50,1000,AirshipGoal.Unlock,20,default,[]).Count==0,"no target chosen: no route");
            Check(AirshipPlanner.Plan(sectors,table,4,1000,AirshipGoal.Loot,20,default,[2u]).Count==0,"the target above the rank: no route");
            Check(AirshipPlanner.Plan(sectors,table,50,1000,AirshipGoal.ExpPerVoyage,20).Count==AirshipPlanner.Plan(sectors,table,50,1000,AirshipGoal.ExpPerVoyage,20,default,null).Count,"no target: as before");
            Check(AirshipPlanner.Plan(sectors,table,50,1000,AirshipGoal.ExpPerDay,20).All(route=>route.UnknownExp==0),"experience goals still leave unknown experience out");
        }),
        ("Airship targets 1243: where an item may drop, the items listed, the sector that unlocks another",() =>
        {
            var catalog=Catalog();
            Check(AirshipSectors.DropSectors(catalog,501).SequenceEqual([0u,2u]),"two sectors");
            Check(AirshipSectors.DropSectors(catalog,502).Count==0,"a point that is not a sector of the catalog is not listed");
            Check(AirshipSectors.LootItems(catalog).SequenceEqual([500u,501u]),"each item once, in id order, sectors of the catalog only");
            Check(AirshipSectors.Unlocker(catalog,2) is (1,5,20),"sector 2: by sector 1, rank 5, surveillance 20 (AirshipUnlock is by, surveillance, rank)");
            Check(AirshipSectors.Unlocker(catalog,0) is null && AirshipSectors.Unlocker(catalog,3) is null && AirshipSectors.Unlocker(catalog,7) is null,"open from the start, naming itself, unknown: none");
        }),
        ("Airship targets 1243: the page asks for the target, passes it to the search, keys the search on it, and the texts exist",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.AirshipPlanner.cs");
            Check(page.Contains("var targets=DrawAirshipTarget(catalog);") && page.Contains("AirshipPlanner.Plan(sectors,table,rank,range,goal,15,token,targets)"),"the target reaches the search");
            Check(page.Contains("|{(targets is null ? \"-\":string.Join(\",\",targets))}\";"),"a new target starts a new search");
            Check(page.Contains("return [unlocker.By];") && page.Contains("var where=AirshipSectors.DropSectors(catalog,chosen);"),"targets from the lists");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach(var key in new[]{"GoalLoot","GoalUnlock","LootItem","LootItemTip","UnlockSector","UnlockSectorTip","TargetSearch","TargetSearchTip","LootChoose","LootWhere","UnlockChoose","UnlockBy"})
                Check(en.Contains($"    \"{key}\": ") && fr.Contains($"    \"{key}\": "),"text in both languages: "+key);
        }),
    ];
}
