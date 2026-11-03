using TenshoAssistant.FreeCompany;

// 0.379 (g, M-10): voyage loot history — one record per voyage updated in place, kept only once its loot is known, 180 days, real
// yield per route, CSV export safe for spreadsheets, a bad file never written over.
internal static class VoyageHistory379Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now = new(2026,11,3,12,0,0,TimeSpan.Zero);
    private static VoyageVessel Vessel(int slot,DateTimeOffset? registered,params VoyageLoot[] loot)
        =>new(){HouseId=9,Kind=VesselKind.Submarine,Slot=slot,Name="Sub"+slot,Rank=50,RegisteredAt=registered,ReturnAt=registered?.AddHours(20),
            Route=["A","B"],Points=[1,2],Loot=[..loot]};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Voyage history: one record per voyage, updated in place, only with loot; pruned to 180 days",() =>
        {
            var history = new List<VoyageRecord>();
            Check(!VoyageHistory.Record(history,[Vessel(1,Now.AddDays(-1)),Vessel(2,null,new VoyageLoot(5,1))]),"no loot or no voyage: nothing");
            Check(VoyageHistory.Record(history,[Vessel(1,Now.AddDays(-1),new VoyageLoot(10,3))]) && history.Count==1,"added");
            Check(!VoyageHistory.Record(history,[Vessel(1,Now.AddDays(-1),new VoyageLoot(10,3))]),"same read: unchanged");
            Check(VoyageHistory.Record(history,[Vessel(1,Now.AddDays(-1),new VoyageLoot(10,3),new VoyageLoot(11,1))]) && history.Count==1 && history[0].Loot.Count==2,"updated in place");
            VoyageHistory.Record(history,[Vessel(1,Now.AddDays(-200),new VoyageLoot(10,1))]);
            Check(VoyageHistory.Prune(history,Now).Count==1,"older than 180 days dropped");
        }),
        ("Voyage history: real yield per route and CSV with quoted, formula-safe fields",() =>
        {
            var history = new List<VoyageRecord>();
            VoyageHistory.Record(history,[Vessel(1,Now.AddDays(-2),new VoyageLoot(10,2)),Vessel(2,Now.AddDays(-1),new VoyageLoot(10,4),new VoyageLoot(12,1))]);
            var route = VoyageHistory.ByRoute(history).Single();
            Check(route.Voyages==2 && route.Route=="A → B" && route.Items[0]==(10u,3.0) && route.Items[1]==(12u,0.5),"average per voyage");
            var csv = VoyageHistory.Csv(history,id=>id==10 ? "=cmd" : "Item \"x\"");
            Check(csv.Split('\n',StringSplitOptions.RemoveEmptyEntries).Length==4 && csv.Contains("\"'=cmd\"",StringComparison.Ordinal)
                && csv.Contains("\"Item \"\"x\"\"\"",StringComparison.Ordinal),"lines, quotes and formula guard");
        }),
        ("Voyage history: file round trip; a bad file is reported and never written over",() =>
        {
            var folder = Path.Combine(Path.GetTempPath(),"tensho-voyages-"+Guid.NewGuid().ToString("N"));
            try
            {
                var path = Path.Combine(folder,"VoyageHistory.json");
                var history = new List<VoyageRecord>();
                VoyageHistory.Record(history,[Vessel(1,DateTimeOffset.UtcNow.AddDays(-1),new VoyageLoot(10,2))]);
                VoyageHistory.Write(path,history);
                Check(VoyageHistory.Read(path).Single().Loot.Single()==new VoyageLoot(10,2),"round trip");
                File.WriteAllText(path,"not json");
                var warnings = 0;
                var store = new VoyageHistoryStore(path,(_,_)=>warnings++);
                store.Observe([Vessel(3,DateTimeOffset.UtcNow,new VoyageLoot(1,1))]);
                Check(store.Unavailable && warnings==1 && File.ReadAllText(path)=="not json","bad file kept as is");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }),
    ];
}
