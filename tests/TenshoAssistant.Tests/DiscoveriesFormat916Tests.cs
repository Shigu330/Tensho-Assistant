using System.Numerics;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.916 (X-76 E1, DEC-042, docs/evidence/0.916-discoveries-format.md): the discoveries file is version 2 before 1.0.0: optional Hq and Values (yours,
// shared) and ImportedHq and ImportedValues (imported, never shared), left out when empty; version 1 files still read; the fact's Key unchanged.
internal static class DiscoveriesFormat916Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,8,12,0,0,TimeSpan.Zero);
    private static string Source(string file)=>File.ReadAllText(Path.GetFullPath(file));
    private static string Histogram(Dictionary<int,int>? values)=>values is null ? "null":string.Join(",",values.OrderBy(pair=>pair.Key).Select(pair=>$"{pair.Key}:{pair.Value}"));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries format: a version 1 file still reads (base, shared file, gist); version 2 is written; an unknown version is refused",() =>
        {
            const string one="{\"Version\":1,\"Producer\":\"Tensho Assistant\",\"Written\":\"2026-11-08T12:00:00+00:00\",\"Facts\":[{\"Kind\":4,\"Subject\":5,\"Detail\":6,"
                +"\"Territory\":0,\"Map\":0,\"CellX\":0,\"CellZ\":0,\"X\":0,\"Z\":0,\"LevelMin\":2,\"LevelMax\":3,\"Seen\":4,\"Imported\":0,"
                +"\"First\":\"2026-11-08T12:00:00+00:00\",\"Last\":\"2026-11-08T12:00:00+00:00\",\"Key\":\"4:5:6:0:0:0:0\"}]}";
            Check(DiscoveryFile.CurrentVersion==2 && DiscoveryFile.OldestVersion==1,"versions");
            var parsed=CommunityDiscoveries.Parse(one);
            Check(parsed is { Version:1, Facts.Count:1 } && parsed.Facts[0] is { Hq:0, Values:null, ImportedHq:0, ImportedValues:null } && DiscoveryStore.Valid(parsed.Facts[0]),"parsed");
            var store=new DiscoveryStore();store.Load(parsed!);
            Check(store.Find(DiscoveryKind.Desynthesis,_=>true,4) is [{ Seen:4, LevelMin:2, LevelMax:3, Values:null }],"loaded as it was");
            var folder=Path.Combine(Path.GetTempPath(),"tensho-916-"+Guid.NewGuid().ToString("N"));
            try
            {
                var path=Path.Combine(folder,"Discoveries.json");
                Directory.CreateDirectory(folder);File.WriteAllText(path,one);
                Check(DiscoveryFiles.Read(path) is { Version:1, Facts.Count:1 },"the base file reads");
                DiscoveryFiles.Write(path,store.Snapshot(At));
                Check(DiscoveryFiles.Read(path) is { Version:2, Facts.Count:1 } && File.ReadAllText(path).StartsWith("{\"Version\":2,",StringComparison.Ordinal),"written as version 2");
                File.WriteAllText(path,one.Replace("\"Version\":1","\"Version\":3"));
                Check(DiscoveryFiles.Read(path) is null && CommunityDiscoveries.Parse(one.Replace("\"Version\":1","\"Version\":0")) is null,"unknown versions refused");
            }
            finally{try{Directory.Delete(folder,true);}catch(Exception){}}
            Check(CommunityDiscoveries.Decode(CommunityDiscoveries.Encode(CommunityDiscoveries.Parse(one)!)) is { Facts.Count:1 },"a gist of version 1 reads");
            Check(CommunityDiscoveries.Mine(store.Snapshot(At,false)).Version==2,"shared as version 2");
        }),
        ("Discoveries format: every sighting counts its value (5, 5, 7 gives 5:2 7:1) and its HQ; the Key is the same seven parts",() =>
        {
            var store=new DiscoveryStore();
            Check(store.SeeSource(DiscoveryKind.FishCatch,4869,2585,5,At,135,16,new Vector3(120,0,40),hq:true),"first");
            store.SeeSource(DiscoveryKind.FishCatch,4869,2585,5,At.AddMinutes(1),135,16,new Vector3(121,0,41));
            store.SeeSource(DiscoveryKind.FishCatch,4869,2585,7,At.AddMinutes(2),135,16,new Vector3(122,0,42),hq:true);
            var fact=store.Find(DiscoveryKind.FishCatch,_=>true,4).Single();
            Check(fact is { Seen:3, Hq:2, LevelMin:5, LevelMax:7 } && Histogram(fact.Values)=="5:2,7:1","values and HQ: "+Histogram(fact.Values)+" HQ "+fact.Hq);
            Check(fact.Key=="10:4869:2585:135:16:2:0" && fact.Key==$"{(int)fact.Kind}:{fact.Subject}:{fact.Detail}:{fact.Territory}:{fact.Map}:{fact.CellX}:{fact.CellZ}","Key unchanged");
            Check(DiscoveryStore.Valid(fact),"valid");
            // Monsters: a level counted at each sighting counted in Seen (10 minutes apart), an unknown level (0) not counted.
            var monsters=new DiscoveryStore();
            monsters.SeeMonster(49,49,135,16,new Vector3(47,0,143),2,At);
            monsters.SeeMonster(49,49,135,16,new Vector3(47,0,143),3,At.AddMinutes(1));
            monsters.SeeMonster(49,49,135,16,new Vector3(47,0,143),3,At.AddMinutes(20));
            monsters.SeeMonster(49,49,135,16,new Vector3(47,0,143),0,At.AddMinutes(40));
            Check(monsters.Find(DiscoveryKind.MonsterPlace,_=>true,4) is [{ Seen:3, Hq:0, LevelMin:2, LevelMax:3 } seen] && Histogram(seen.Values)=="2:1,3:1","monster levels");
        }),
        ("Discoveries format: the histogram keeps 32 values at most; past them a value is counted under the nearest value kept, the lower one on a tie",() =>
        {
            var store=new DiscoveryStore();
            for(var value=0;value<40;value++)store.SeeSource(DiscoveryKind.FishBite,4869,1,value*10,At.AddSeconds(value),135,16,new Vector3(120,0,40));
            var fact=store.Find(DiscoveryKind.FishBite,_=>true,4).Single();
            Check(fact.Values!.Count==ValueCounts.MaxValues && ValueCounts.Total(fact.Values)==40 && fact.Seen==40,"bounded, nothing lost");
            Check(fact.Values[310]==9 && !fact.Values.ContainsKey(320) && fact.LevelMax==390,"values past 32 under the nearest one (310), the levels exact");
            Dictionary<int,int>? tie=new(){[10]=1,[20]=1};
            for(var value=100;value<130;value++)ValueCounts.Add(ref tie,value);
            ValueCounts.Add(ref tie,15,3);
            Check(tie!.Count==32 && tie[10]==4 && tie[20]==1,"a tie goes to the lower value");
            Check(ValueCounts.Valid(fact.Values) && !ValueCounts.Valid(Enumerable.Range(0,33).ToDictionary(value=>value,_=>1))
                && !ValueCounts.Valid(new Dictionary<int,int>{[-1]=1}) && !ValueCounts.Valid(new Dictionary<int,int>{[1]=0}) && !ValueCounts.Valid(new Dictionary<int,int>{[100_001]=1}),"bounds");
            Check(!store.SeeSource(DiscoveryKind.Venture,5,7,100_001,At),"a value past the bound is not noted");
        }),
        ("Discoveries format: HQ never more than the sightings counted; a fact breaking it is refused",() =>
        {
            var fact=new DiscoveryFact{Kind=DiscoveryKind.MonsterDrop,Subject=5,Detail=49,Territory=135,Map=16,Seen=2,First=At,Last=At,Hq=2,Values=new(){[1]=2}};
            Check(DiscoveryStore.Valid(fact),"two HQ of two");
            fact.Hq=3;Check(!DiscoveryStore.Valid(fact),"three HQ of two");
            fact.Hq=-1;Check(!DiscoveryStore.Valid(fact),"negative");
            fact.Hq=0;fact.ImportedHq=1;Check(!DiscoveryStore.Valid(fact),"imported HQ with no imported values");
            fact.ImportedValues=new(){[1]=1};Check(DiscoveryStore.Valid(fact),"imported HQ within imported values");
            Check(new DiscoveryStore().Merge([new DiscoveryFact{Kind=DiscoveryKind.Venture,Subject=5,Detail=7,LevelMin=1,LevelMax=1,Seen=1,First=At,Last=At,Hq=1}]).Refused==1,"refused when imported");
        }),
        ("Discoveries format: two facts joined add their HQ and values; an import is kept apart, the largest per value (read again: nothing added); only yours are shared",() =>
        {
            DiscoveryFact Fact(int hq,Dictionary<int,int> values,float x)=>DiscoveryStore.Recell(new DiscoveryFact{Kind=DiscoveryKind.MonsterDrop,Subject=5,Detail=49,Territory=135,Map=16,
                X=x,Z=40,LevelMin=values.Keys.Min(),LevelMax=values.Keys.Max(),Seen=(int)ValueCounts.Total(values),First=At,Last=At,Hq=hq,Values=values});
            var joined=new DiscoveryStore();
            joined.Load(new DiscoveryFile{Facts=[Fact(1,new(){[1]=2,[2]=1},120),Fact(2,new(){[2]=2,[3]=1},121)]});
            Check(joined.Find(DiscoveryKind.MonsterDrop,_=>true,4) is [{ Seen:6, Hq:3 } one] && Histogram(one.Values)=="1:2,2:3,3:1","joined: "+Histogram(joined.Find(DiscoveryKind.MonsterDrop,_=>true,4)[0].Values));
            var store=new DiscoveryStore();
            store.SeeSource(DiscoveryKind.MonsterDrop,5,49,1,At,135,16,new Vector3(120,0,40),hq:true);
            var shared=Fact(2,new(){[1]=3,[4]=1},120);shared.Seen=0;shared.Imported=3;
            Check(store.Merge([shared]).Joined==1 && store.Merge([shared]).Joined==1,"imported twice");
            var mixed=store.Find(DiscoveryKind.MonsterDrop,_=>true,4).Single();
            Check(mixed is { Seen:1, Hq:1, Imported:2, ImportedHq:2 } && Histogram(mixed.Values)=="1:1" && Histogram(mixed.ImportedValues)=="1:3,4:1","yours apart, read again adds nothing");
            Check(store.Merge([Fact(3,new(){[1]=5},120)]).Joined==1 && store.Find(DiscoveryKind.MonsterDrop,_=>true,4)[0] is { ImportedHq:3 } bigger
                && Histogram(bigger.ImportedValues)=="1:5,4:1","the largest per value");
            var fresh=new DiscoveryStore();fresh.Merge([Fact(1,new(){[2]=4},120)]);
            Check(fresh.Find(DiscoveryKind.MonsterDrop,_=>true,4) is [{ Seen:0, Imported:1, Hq:0, Values:null, ImportedHq:1 } added] && Histogram(added.ImportedValues)=="2:4","a new import is imported only");
            var mine=CommunityDiscoveries.Mine(store.Snapshot(At,false));
            Check(mine.Facts is [{ Hq:1, ImportedHq:0, ImportedValues:null, Imported:0 } sent] && Histogram(sent.Values)=="1:1","only yours shared");
            var back=CommunityDiscoveries.Decode(CommunityDiscoveries.Encode(mine))!;
            Check(back.Version==2 && back.Facts[0].Hq==1 && Histogram(back.Facts[0].Values)=="1:1","the gist keeps them");
        }),
        ("Discoveries format: the written file leaves out empty HQ and values, and writes them as a JSON object when there are some",() =>
        {
            var store=new DiscoveryStore();
            store.Merge([new DiscoveryFact{Kind=DiscoveryKind.Venture,Subject=6,Detail=7,LevelMin=1,LevelMax=1,Imported=1,First=At,Last=At}]);
            var empty=JsonSerializer.Serialize(store.Snapshot(At,false));
            Check(empty.StartsWith("{\"Version\":2,",StringComparison.Ordinal) && !empty.Contains("Hq") && !empty.Contains("Values"),"left out: "+empty);
            store.SeeSource(DiscoveryKind.Venture,5,7,5,At,hq:true);store.SeeSource(DiscoveryKind.Venture,5,7,5,At);store.SeeSource(DiscoveryKind.Venture,5,7,7,At);
            var json=JsonSerializer.Serialize(CommunityDiscoveries.Mine(store.Snapshot(At,false)));
            Check(json.Contains("\"Hq\":1,\"Values\":{\"5\":2,\"7\":1}") && !json.Contains("Imported\":0,\"ImportedHq") && !json.Contains("ImportedValues"),"written: "+json);
            Check(json.Contains("\"Key\":\"5:5:7:0:0:0:0\""),"the Key as before");
        }),
        ("Discoveries format: the merge tool reads versions 1 and 2, sums HQ and values with the same bound, writes version 2; the common file is version 2; the watcher passes HQ",() =>
        {
            var tool=Source("tools/discoveries-merge.py");
            Check(tool.Contains("CELL=50;MAX_FACTS=200_000;VERSIONS={1,2};VERSION=2") && tool.Contains("\"First\",\"Last\",\"Hq\",\"Values\"}") && tool.Contains("MAX_VALUES=32;MAX_VALUE=100_000;MAX_COUNT=100_000_000"),"fields and bounds");
            Check(tool.Contains("if data.get(\"Version\") not in VERSIONS:") && tool.Contains("common={\"Version\":VERSION,"),"versions");
            Check(tool.Contains("min(values,key=lambda v:(abs(v-value),v))") && tool.Contains("known[\"Values\"]=histogram_sum(known.get(\"Values\"),f[\"Values\"])"),"sum, nearest value");
            Check(tool.Contains("0<=f[\"Hq\"]<=sum(f.get(\"Values\",{}).values())") && tool.Contains("f.pop(\"ImportedHq\",None);f.pop(\"ImportedValues\",None)"),"HQ checked, imported parts dropped");
            Check(tool.Contains("FLAG: refusals over 5 %"),"the 5 % flag kept");
            Check(CommunityDiscoveries.Parse(Source("community-discoveries/discoveries.json")) is { Version:2, Facts.Count:>0 },"common file");
            var readme=Source("community-discoveries/README.md");
            Check(readme.Contains("version 2, since Tensho 0.916") && readme.Contains("version 2, depuis Tensho 0.916"),"README in both languages");
            var watcher=Source("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs");
            Check(watcher.Contains("if(slot.IsHq)hq[slot.BaseItemId]=") && watcher.Contains("var hq=hqGains.ContainsKey(item);"),"HQ gains");
            foreach(var call in new[]{"DiscoveryKind.FishCatch,item,bait,quantity,at,territory,map,me.Position,hq);","DiscoveryKind.FateReward,item,fateId,quantity,at,hq:hq);",
                "DiscoveryKind.MonsterDrop,item,death.Name,quantity,at,death.Territory,death.Map,death.Place,hq);","DiscoveryKind.ItemFromItem,item,spent!.Value,quantity,at,hq:hq);",
                "DiscoveryKind.DutyChest,item,duty,quantity,at,territory,hq:hq);","hq:ItemGains.IsHq(item.ItemId));","DiscoveryKind.Venture,item,task,count,at,hq:hq);",
                "DiscoveryKind.Desynthesis,item,source,count,at,hq:hq);"})
                Check(watcher.Contains(call),"HQ passed: "+call);
            Check(ItemGains.IsHq(1_005_057) && !ItemGains.IsHq(5_057) && !ItemGains.IsHq(505_057) && !ItemGains.IsHq(2_002_169),"HQ offset");
        }),
    ];
}
