// 0.911 (X-76 step 2c, parser coverage): a quest's HQ demand is kept (the item's HQ id as the subject), every shop window stops the attribution of a gain,
// the duty coffers of a deep dungeon keep their territory like the loot window, and every discovery kind has its own label, list and summary.
using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Discoveries;
internal static class Parsers911Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,8,12,0,0,TimeSpan.Zero);
    private static string Source(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    // The text from a declaration's start to its end mark (both included), or "" when missing.
    private static string Between(string text,string start,string end)
    {
        var from=text.IndexOf(start,StringComparison.Ordinal);
        if(from<0)return "";
        var to=text.IndexOf(end,from+start.Length,StringComparison.Ordinal);
        return to<0 ? "":text[from..(to+end.Length)];
    }
    private static HashSet<string> Keys(string language,string section)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
        return document.RootElement.TryGetProperty(section,out var found) && found.ValueKind==JsonValueKind.Object
            ? found.EnumerateObject().Select(entry=>entry.Name).ToHashSet(StringComparer.Ordinal):[];
    }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest hand-in: an HQ demand is its own fact under the item's HQ id, the quantity kept in the levels; older plain facts still read",() =>
        {
            Check(QuestItemAsks.Subject(5,false)==5 && QuestItemAsks.Subject(5,true)==1_000_005 && QuestItemAsks.Subject(1_000_005,true)==1_000_005
                && QuestItemAsks.Subject(1_000_005,false)==5 && QuestItemAsks.Subject(0,true)==0,"subject");
            Check(QuestItemAsks.Item(1_000_005)==5 && QuestItemAsks.Item(5)==5 && QuestItemAsks.Hq(1_000_005) && !QuestItemAsks.Hq(5),"item and HQ back");
            Check(QuestItemAsks.IsOf(5,5) && QuestItemAsks.IsOf(1_000_005,5) && !QuestItemAsks.IsOf(6,5) && !QuestItemAsks.IsOf(1_000_006,5) && !QuestItemAsks.IsOf(5,0),"facts of an item");
            var store=new DiscoveryStore();
            const uint quest=0x10000u+1234;
            Check(store.SeeSource(DiscoveryKind.QuestItemAsked,QuestItemAsks.Subject(5,false),quest,3,At),"plain demand");
            Check(store.SeeSource(DiscoveryKind.QuestItemAsked,QuestItemAsks.Subject(5,true),quest,2,At),"HQ demand: a fact of its own");
            Check(!store.SeeSource(DiscoveryKind.QuestItemAsked,QuestItemAsks.Subject(5,true),quest,2,At.AddMinutes(5)),"the same HQ demand again");
            var facts=store.Find(DiscoveryKind.QuestItemAsked,subject=>QuestItemAsks.IsOf(subject,5),16);
            Check(facts.Count==2 && facts.All(fact=>fact.Detail==quest && DiscoveryStore.Valid(fact)),"both found by the item");
            var hq=facts.Single(fact=>QuestItemAsks.Hq(fact.Subject));var plain=facts.Single(fact=>!QuestItemAsks.Hq(fact.Subject));
            Check(hq.LevelMin==2 && hq.LevelMax==2 && hq.Seen==2 && plain.LevelMin==3 && plain.LevelMax==3,"quantities kept in the levels");
            var watcher=Source("Discoveries/ItemDiscoveryWatcher.Dalamud.cs");
            Check(watcher.Contains("store.SeeSource(DiscoveryKind.QuestItemAsked,QuestItemAsks.Subject(item,hq),quest,quantity,at);"),"the watcher keeps the HQ demand");
            var uses=Source("MainWindow.ItemUses.cs");
            Check(uses.Contains("QuestItemAsks.IsOf(subject,id)") && uses.Contains("(Discoveries.QuestItemAsks.Hq(fact.Subject) ? \" HQ\":\"\")"),"the item's uses say HQ");
            var page=Source("MainWindow.Discoveries.cs");
            Check(page.Contains("DiscoveryItemName(QuestItemAsks.Item(fact.Subject))+(QuestItemAsks.Hq(fact.Subject) ? \" HQ\":\"\")"),"the page names the item, with HQ");
            Check(!Source("Discoveries/DiscoveriesModel.cs").Contains("kept as LevelMax = 1"),"the model's comment matches");
        }),
        ("Gain attribution: every shop window is a busy window, declared first (static fields are set in order)",() =>
        {
            var watcher=Source("Discoveries/ItemDiscoveryWatcher.Dalamud.cs");
            var shops=Between(watcher,"private static readonly string[] ShopWindows=[","];");
            var busy=Between(watcher,"private static readonly string[] BusyWindows=[","];");
            Check(shops.Length>0 && busy.Length>0,"both lists");
            Check(watcher.IndexOf(shops,StringComparison.Ordinal)<watcher.IndexOf(busy,StringComparison.Ordinal),"shops declared before");
            Check(busy.StartsWith("private static readonly string[] BusyWindows=[..ShopWindows,",StringComparison.Ordinal),"busy built from the shops");
            var names=Regex.Matches(shops,"\"([A-Za-z0-9]+)\"").Select(match=>match.Groups[1].Value).ToArray();
            foreach(var name in new[]{"Shop","ShopExchangeItem","ShopExchangeCurrency","ShopExchangeCoin","InclusionShop","ShopCard","GrandCompanyExchange","FreeShop","CollectablesShop","SkyIslandExchange"})
                Check(names.Contains(name),"shop window "+name);
            var others=Regex.Matches(busy,"\"([A-Za-z0-9]+)\"").Select(match=>match.Groups[1].Value).ToArray();
            Check(others.Length==others.Distinct().Count() && !others.Intersect(names).Any(),"no window twice");
            foreach(var name in new[]{"Trade","RetainerList","LetterViewer","ItemSearchResult","Synthesis","RetainerTaskResult","SkyIslandFinishReward"})
                Check(others.Contains(name),"busy window "+name);
        }),
        ("Gain attribution: a purchase in a shop just after a FATE or a kill is neither its reward nor a drop nor a coffer",() =>
        {
            // A FATE ended 1 s ago, a shop window open: nothing; the same gain with no window: the FATE's reward.
            Check(ItemGains.Attribute(false,true,10_000,9_000,7,-100_000,false,null,false,false)==ItemGains.Cause.None,"shop after a FATE");
            Check(ItemGains.Attribute(false,false,10_000,9_000,7,-100_000,false,null,false,false)==ItemGains.Cause.FateReward,"no shop: the FATE");
            // The target died 1 s ago.
            Check(ItemGains.Attribute(false,true,10_000,-100_000,0,9_000,false,null,false,false)==ItemGains.Cause.None,"shop after a kill");
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,9_000,false,null,false,false)==ItemGains.Cause.MonsterDrop,"no shop: the drop");
            // Gil spent is not an item: in a deep dungeon a shop open is not a coffer.
            Check(ItemGains.Attribute(false,true,10_000,-100_000,0,-100_000,true,null,false,false,deepDungeon:true)==ItemGains.Cause.None,"shop in a deep dungeon");
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,-100_000,true,null,false,false,deepDungeon:true)==ItemGains.Cause.DungeonCoffer,"no shop: the coffer");
        }),
        ("Duty chests: the deep dungeon coffers and the loot window record the territory, one fact per item and duty",() =>
        {
            var watcher=Source("Discoveries/ItemDiscoveryWatcher.Dalamud.cs");
            Check(watcher.Contains("added=store.SeeSource(DiscoveryKind.DutyChest,item,duty,quantity,at,territory,hq:hq);kind=DiscoveryKind.DutyChest;break;"),"coffer with its territory");
            Check(watcher.Contains("store.SeeSource(DiscoveryKind.DutyChest,item.ItemId%1_000_000,duty,Math.Max(1,(int)item.ItemCount),at,territory,hq:ItemGains.IsHq(item.ItemId));"),"loot with its territory");
            Check(Regex.Matches(watcher,@"SeeSource\(DiscoveryKind\.DutyChest,[^;]*\);").All(match=>Regex.IsMatch(match.Value,@",at,territory(,hq:[^;]*)?\);$")),"every duty chest (0.916: the HQ flag may follow)");
            var store=new DiscoveryStore();
            Check(store.SeeSource(DiscoveryKind.DutyChest,5,780,1,At,561),"from the loot window");
            Check(!store.SeeSource(DiscoveryKind.DutyChest,5,780,2,At.AddMinutes(1),561),"from a coffer: the same fact");
            Check(store.Find(DiscoveryKind.DutyChest,_=>true,4) is [{ Seen:2, LevelMin:1, LevelMax:2, Territory:561 }],"one fact");
        }),
        ("Discoveries page: every kind has its own label branch, and the label keys exist in English and French",() =>
        {
            var page=Source("MainWindow.Discoveries.cs");
            var names=Between(page,"private string DiscoveryFactName(DiscoveryFact fact)=>fact.Kind switch","    };");
            Check(names.Length>0,"DiscoveryFactName found");
            var missing=Enum.GetNames<DiscoveryKind>().Where(kind=>!names.Contains($"DiscoveryKind.{kind}=>",StringComparison.Ordinal)).ToArray();
            Check(missing.Length==0,"kinds without a label: "+string.Join(", ",missing));
            Check(!names.Contains("_=>DiscoveryMonsterName(",StringComparison.Ordinal),"no kind falls to a monster's name");
            var keys=Regex.Matches(names,"\"(DiscoveriesUi|FishingPanel)\\.([A-Za-z]+)\"").Select(match=>(match.Groups[1].Value,match.Groups[2].Value)).Distinct().ToArray();
            Check(keys.Length>=20,"label keys read: "+keys.Length);
            foreach(var language in new[]{"en","fr"})
                foreach(var (section,key) in keys)
                    Check(Keys(language,section).Contains(key),$"{language}: {section}.{key}");
            foreach(var key in new[]{"FishLure","FishTransition","FishBite"})
                Check(Keys("en","DiscoveriesUi").Contains(key) && Keys("fr","DiscoveriesUi").Contains(key),"new label "+key);
        }),
        ("FATEs seen: never at the game's zero position (reader, store, read and imported files, merge tool)",() =>
        {
            var store=new DiscoveryStore();
            Check(!store.SeeFate(516,156,25,System.Numerics.Vector3.Zero,50,At),"zero position not noted");
            Check(!store.SeeFate(516,156,25,new System.Numerics.Vector3(0,12,0),50,At),"zero X and Z not noted");
            Check(store.SeeFate(516,156,25,new System.Numerics.Vector3(120,0,-40),50,At),"a real place noted");
            Check(store.Find(DiscoveryKind.FateSeen,_=>true,4).Count==1,"one fact");
            var zero=new DiscoveryFact{Kind=DiscoveryKind.FateSeen,Subject=516,Territory=156,Map=25,Seen=1,First=At,Last=At};
            Check(!DiscoveryStore.Valid(zero),"a FATE at (0, 0) refused");
            Check(DiscoveryStore.Valid(new DiscoveryFact{Kind=DiscoveryKind.FateSeen,Subject=516,Territory=156,Map=25,X=0.5f,Seen=1,First=At,Last=At}),"near zero kept");
            Check(DiscoveryStore.Valid(new DiscoveryFact{Kind=DiscoveryKind.Desynthesis,Subject=5,Detail=6,Seen=1,First=At,Last=At}),"kinds without a place keep (0, 0)");
            var other=new DiscoveryStore();
            other.Load(new DiscoveryFile{Facts=[zero,DiscoveryStore.Recell(new DiscoveryFact{Kind=DiscoveryKind.FateSeen,Subject=516,Territory=156,Map=25,X=120,Z=-40,Seen=1,First=At,Last=At})]});
            Check(other.Find(DiscoveryKind.FateSeen,_=>true,4) is [{ X:120 }],"dropped when the base is read");
            Check(new DiscoveryStore().Merge([zero]).Refused==1,"refused when imported");
            Check(Source("Maps/MapLiveReader.Dalamud.cs").Contains("|| fate.Position==Vector3.Zero)continue;"),"the reader skips it");
            Check(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("if f[\"Kind\"]==3 and f[\"X\"]==0 and f[\"Z\"]==0:return False"),"the merge tool too");
        }),
        ("Festivals seen: one sighting a day, plugin reloads included; imported days do not hide yours",() =>
        {
            var store=new DiscoveryStore();
            Check(store.SeeFestival(12,1,4_000,At),"first day");
            Check(!store.SeeFestival(12,1,4_000,At.AddMinutes(1)) && !store.SeeFestival(12,1,4_000,At.AddMinutes(2)),"reloads the same day");
            Check(store.Find(DiscoveryKind.FestivalSeen,_=>true,4) is [{ Seen:1, LevelMin:4_000, LevelMax:4_000 }],"counted once");
            Check(!store.SeeFestival(12,1,3_999,At),"an earlier day");
            Check(store.SeeFestival(12,2,4_000,At),"another phase is its own fact");
            store.SeeFestival(12,1,4_001,At.AddDays(1));
            Check(store.Find(DiscoveryKind.FestivalSeen,subject=>subject==12,4).Single(fact=>fact.Detail==1) is { Seen:2, LevelMin:4_000, LevelMax:4_001 },"next day");
            var imported=new DiscoveryStore();
            imported.Merge([new DiscoveryFact{Kind=DiscoveryKind.FestivalSeen,Subject=13,Detail=1,LevelMin=4_000,LevelMax=4_000,Seen=1,First=At,Last=At}]);
            imported.SeeFestival(13,1,4_000,At);
            Check(imported.Find(DiscoveryKind.FestivalSeen,_=>true,4) is [{ Seen:1, Imported:1 }],"yours noted beside an imported day");
            var watcher=Source("Discoveries/ItemDiscoveryWatcher.Dalamud.cs");
            Check(watcher.Contains("if(festival.Id!=0)store.SeeFestival(festival.Id,festival.Phase,day,at);") && !watcher.Contains("SeeSource(DiscoveryKind.FestivalSeen"),"the watcher counts through the store");
        }),
        ("Key items: EventItem ids (2 000 000 and up) are kept by BaseItem and are never an HQ demand",() =>
        {
            Check(ItemGains.BaseItem(2_002_169)==2_002_169 && ItemGains.BaseItem(2_000_000)==2_000_000,"key items kept");
            Check(ItemGains.BaseItem(1_005_057)==5_057 && ItemGains.BaseItem(505_057)==5_057 && ItemGains.BaseItem(5_057)==5_057 && ItemGains.BaseItem(1_999_999)==499_999,"HQ and collectables");
            Check(QuestItemAsks.Subject(2_002_169,false)==2_002_169 && QuestItemAsks.Subject(2_002_169,true)==2_002_169,"a key item stays itself");
            Check(!QuestItemAsks.Hq(2_002_169) && QuestItemAsks.Item(2_002_169)==2_002_169 && QuestItemAsks.Hq(1_999_999),"HQ only below 2 000 000");
            Check(QuestItemAsks.IsOf(2_002_169,2_002_169) && !QuestItemAsks.IsOf(2_002_169,2_169) && !QuestItemAsks.IsOf(2_169,2_002_169),"not confused with item 2169");
            var page=Source("MainWindow.Discoveries.cs");
            Check(page.Contains("id>=ItemGains.KeyItemStart ? KeyItemNameOf?.Invoke(id)"),"the page names key items");
            Check(Source("Plugin.cs").Contains("mainWindow.KeyItemNameOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.EventItem>()"),"from the EventItem sheet");
        }),
        ("Discoveries page: every kind is listed by the search and has its summary line in English and French; fishing, quest and NPC kinds are no item source",() =>
        {
            var page=Source("MainWindow.Discoveries.cs");
            var listed=Between(page,"private static readonly DiscoveryKind[] ItemSourceKinds=[","];");
            var others=Between(page,"private static readonly DiscoveryKind[] OtherSeenKinds=[","];");
            var search=Between(page,"discoveryRows=[..store.Find(DiscoveryKind.MonsterPlace,","];");
            Check(listed.Length>0 && others.Length>0 && search.Contains("..ItemSourceKinds.Concat(OtherSeenKinds)",StringComparison.Ordinal),"lists found");
            var missing=Enum.GetNames<DiscoveryKind>().Where(kind=>!Regex.IsMatch(listed+others+search,$@"DiscoveryKind\.{kind}\b")).ToArray();
            Check(missing.Length==0,"kinds the search never lists: "+string.Join(", ",missing));
            foreach(var kind in new[]{"FishLure","FishTransition","FishBite","QuestItemAsked","SpearShadow","NpcPlace","BlueSpellCaster"})
                Check(!Regex.IsMatch(listed,$@"DiscoveryKind\.{kind}\b"),"not an item source: "+kind);
            foreach(var language in new[]{"en","fr"})
            {
                var keys=Keys(language,"DiscoveriesUi");
                var summaries=Enum.GetNames<DiscoveryKind>().Where(kind=>!keys.Contains("Summary"+kind)).ToArray();
                Check(summaries.Length==0,$"{language}: kinds without a summary: "+string.Join(", ",summaries));
            }
        }),
    ];
}
