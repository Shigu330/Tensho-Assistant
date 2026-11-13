using TenshoAssistant.Discoveries;
using TenshoAssistant.Reminders;

// 0.1211 (X-185, night review of 12/11, N23, N24, N25): two beds harvested within seconds are two harvests (the bed read in the menu decides, the 5 s
// window only for the lines of one harvest); a bed noted again in another language of the plugin replaces the old entry, and a care line acts on the
// latest planted of two entries of one slot; an imported discovery with its lowest level above its highest is refused (the plugin and the merge tool).
internal static class GardenDiscoveries1211Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,10,12,0,0,TimeSpan.Zero);

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Garden 1211: two beds harvested within seconds are two harvests; the lines of one harvest stay one (N23)",() =>
        {
            var never=DateTimeOffset.MinValue;
            Check(GardenHarvests.IsNew(At,never,(1,6),null),"a first harvest");
            Check(GardenHarvests.IsNew(At.AddSeconds(3),At,(1,7),(1,6)),"bed 7 read in its menu 3 s after bed 6: a new harvest");
            Check(!GardenHarvests.IsNew(At.AddSeconds(1),At,null,(1,6)),"the seeds of bed 6 one second later (the menu already taken): the same harvest");
            Check(!GardenHarvests.IsNew(At.AddSeconds(1),At,(1,6),(1,6)),"the same bed again within 5 s: the same harvest");
            Check(GardenHarvests.IsNew(At.AddSeconds(6),At,null,(1,6)),"without a menu, 5 s later: a new harvest (as before)");
            // The watcher, through GardenMenuMemory as it runs: bed 6's menu, its harvest, its seeds; bed 7's menu, its harvest 3 s after bed 6.
            var memory=new GardenMenuMemory();var place="Shirogane 2-17";var last=never;(int,int)? lastSlot=null;var harvests=new List<(int,int)?>();
            void Line(DateTimeOffset at)
            {
                var slot=memory.Take(place,at);
                if(GardenHarvests.IsNew(at,last,slot,lastSlot)){last=at;lastSlot=slot;harvests.Add(slot);}
            }
            memory.Seen(1,6,place,At.AddSeconds(-2));Line(At);Line(At.AddSeconds(1));
            memory.Seen(1,7,place,At.AddSeconds(2));Line(At.AddSeconds(3));Line(At.AddSeconds(4));
            Check(harvests.SequenceEqual(new (int,int)?[]{(1,6),(1,7)}),$"two harvests, beds 6 and 7 ({string.Join(", ",harvests)})");
        }),
        ("Garden 1211: one slot noted in two languages: a new planting replaces both, a care line takes the latest planted (N24)",() =>
        {
            var here="Shirogane 2-17";
            var older=new GardenBed{Place=$"{here} · patch 1, bed 6",Seed="Old",PlantedAt=At.AddDays(-3),GrowHours=100,TendHours=24};
            var newer=new GardenBed{Place=$"{here} · potager 1, emplacement 6",Seed="New",PlantedAt=At.AddDays(-1),GrowHours=100,TendHours=24};
            var other=new GardenBed{Place=$"{here} · potager 1, emplacement 7",Seed="Other",PlantedAt=At.AddDays(-2),GrowHours=100,TendHours=24};
            var target=GardenJournal.CareTargets([older,newer,other],here,(1,6));
            Check(target.Length==1 && target[0]==newer,"the latest planted of the two entries of bed 6");
            Check(GardenJournal.CareTargets([newer,older],here,(1,6)).Single()==newer,"whatever their order");
            var planted=GardenJournal.Detected([older,other],$"{here} · potager 1, emplacement 6","Fresh",100,24,At)!;
            Check(planted.Count==2 && planted.Contains(other) && !planted.Contains(older) && planted.Single(bed=>bed!=other).Seed=="Fresh","the English entry of bed 6 replaced by the French planting");
            var pot=GardenJournal.Detected([older,other],$"{here} · pot","Fresh",100,24,At)!;
            Check(pot.Count==3,"a pot (no slot) replaces nothing else");
        }),
        ("Discoveries 1211: an imported fact never has its lowest level above its highest (N25)",() =>
        {
            DiscoveryFact Fact(int min,int max)=>new(){Kind=DiscoveryKind.Venture,Subject=1,Detail=7,LevelMin=min,LevelMax=max,Imported=1,First=At.AddDays(-2),Last=At.AddDays(-1)};
            Check(!DiscoveryStore.Valid(Fact(90,10)),"90 to 10: refused");
            Check(DiscoveryStore.Valid(Fact(10,90)) && DiscoveryStore.Valid(Fact(5,5)) && DiscoveryStore.Valid(Fact(90,0)) && DiscoveryStore.Valid(Fact(0,90)),"in order, equal, or a highest of 0 (unknown): kept");
            var store=new DiscoveryStore();
            var merged=store.Merge([Fact(90,10)]);
            Check(merged.Refused==1 && merged.Added==0,"refused by Merge too");
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("if f[\"LevelMax\"]!=0 and f[\"LevelMin\"]>f[\"LevelMax\"]:return False") && tool.Contains("N25: the lowest level never above the highest"),"the merge tool has the same rule and its selftest case");
        }),
    ];
}
