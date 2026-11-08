using TenshoAssistant.Caching;
using TenshoAssistant.Gear;
using TenshoAssistant.Npcs;

// X-76 step 6 (0.955): work moved out of the draw loop. The NPC vendor sheet reads indexes built once with the catalogue; the gear set
// reader keeps its published object when a read copies the same content (Gear page and sort preview caches keyed on it); the Bank
// frames are rebuilt when a snapshot crosses its expiry, not every second; wiki lines find an achievement through the collection index.
internal static class DrawCost955Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static NpcShopSale Sale(uint shop,uint item) => new(shop,NpcShopKind.Gil,"Shop"+shop,item,1,false,[new(1,10)]);
    private static NpcVendor Vendor(uint npc,string name,params uint[] shops) => new(npc,name,shops,[]);

    private static GearsetState State(int level=90,uint item=40_000,bool hq=true,int grade=3,string name="Paladin",int experience=10,bool jobs=true)
        => new([new GameGearset(1,name,19,600,[new(GearSlot.Head,item,hq,[new(5,grade)]),new(GearSlot.Body,40_001,false,[])]),new GameGearset(2,"",21,580,[])],
            new Dictionary<uint,int>{[19]=level,[21]=80},jobs ? [new JobProgressRow(19,level,experience,1000),new JobProgressRow(21,80,0,500)]:null);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("NPC vendor sheet: shops by vendor and vendor by NPC id come from indexes built once, same result as the former scan",() =>
        {
            var vendors = new[]{Vendor(1,"First",20,10,30),Vendor(1,"Duplicate",40),Vendor(2,"Second",10),Vendor(3,"Nothing sold",30)};
            var sales = new[]{Sale(10,100),Sale(20,200),Sale(10,101),Sale(40,400),Sale(20,201)};
            var catalog = NpcVendorCatalog.Build(vendors,sales,"v");
            Check(catalog.Vendor(1)?.Name=="First" && catalog.Vendor(2)?.Name=="Second" && catalog.Vendor(99) is null,"vendor by NPC id, the first one kept");
            foreach (var vendor in catalog.Vendors)
            {
                // The page's former per-frame work, kept here as the reference.
                var former = catalog.Sales.Where(sale=>vendor.ShopIds.Contains(sale.ShopId)).GroupBy(sale=>sale.ShopId).ToArray();
                var indexed = catalog.ShopsOf(vendor.NpcId);
                Check(indexed.Count==former.Length,"same shop count for "+vendor.Name);
                for (var shop = 0; shop < former.Length; shop++)
                    Check(indexed[shop].Count>0 && indexed[shop][0].ShopId==former[shop].Key && indexed[shop].SequenceEqual(former[shop]),"same shop order and sales for "+vendor.Name);
            }
            Check(catalog.ShopsOf(1).Select(shop=>shop[0].ShopId).SequenceEqual(new uint[]{10,20}),"shops in the order of their first sale; a shop with no sale left out");
            Check(catalog.ShopsOf(3).Count==0 && catalog.ShopsOf(99).Count==0,"nothing sold, unknown NPC: empty (the page says « no shop »)");
            Check(catalog.SalesOfShop(40).Select(sale=>sale.ItemId).SequenceEqual(new uint[]{400}) && catalog.SalesOfShop(77).Count==0,"sales by shop, also a shop no vendor opens");
            var page = Read("MainWindow.NpcVendors.cs");
            Check(page.Contains("if(npcVendorFocus!=0 && catalog.Vendor(npcVendorFocus) is { } focused){DrawNpcVendorSheet(catalog,focused);return;}",StringComparison.Ordinal)
                && page.Contains("var sales=catalog.ShopsOf(vendor.NpcId);",StringComparison.Ordinal),"the page reads the indexes");
            Check(!page.Contains("catalog.Sales.Where(",StringComparison.Ordinal) && !page.Contains("catalog.Vendors.FirstOrDefault(",StringComparison.Ordinal),"no scan of every sale or vendor per frame");
        }),
        ("Gear sets: an unchanged read keeps the published state; any change of content publishes a new one",() =>
        {
            var before = State();
            Check(State().SameAs(before) && before.SameAs(before),"same content, other objects: the same");
            Check(!State().SameAs(null),"nothing published yet: publish");
            Check(!State(level:91).SameAs(before),"a level");
            Check(!State(experience:11).SameAs(before),"experience");
            Check(!State(item:40_002).SameAs(before) && !State(hq:false).SameAs(before) && !State(grade:4).SameAs(before),"item, HQ, materia");
            Check(!State(name:"Tank").SameAs(before) && !State(jobs:false).SameAs(before) && !before.SameAs(State(jobs:false)),"name, progress read or not");
            var moreSets = new GearsetState([..before.Sets,new GameGearset(3,"",24,500,[])],before.JobLevels,before.Jobs);
            Check(!moreSets.SameAs(before) && !before.SameAs(moreSets),"a set added or removed");
            var reader = Read("Gear/GameGearsetReader.Dalamud.cs");
            Check(reader.Contains("var next=new GearsetState(sets,levels,progress);",StringComparison.Ordinal)
                && reader.Contains("if(!next.SameAs(Volatile.Read(ref state)))Volatile.Write(ref state,next);",StringComparison.Ordinal),"the reader keeps the old object");
            Check(reader.Contains("nextRead=now.AddSeconds(2);",StringComparison.Ordinal) && reader.Contains("unavailable=true;",StringComparison.Ordinal)
                && reader.Contains("Volatile.Write(ref state,null);",StringComparison.Ordinal) && reader.Contains("CompetitiveGate.Add(framework,OnUpdate);",StringComparison.Ordinal),
                "same cadence, failure still clears the state (R-14), PvP cut");
            Check(Read("MainWindow.Gearsets.cs").Contains("!ReferenceEquals(cache.State,state)",StringComparison.Ordinal)
                && Read("MainWindow.SortPreview.cs").Contains("!ReferenceEquals(sortingViewGearsets,GearsetSource?.State)",StringComparison.Ordinal),"the caches keyed on that object");
        }),
        ("Bank frames: rebuilt when a selected snapshot turns expired, not every second",() =>
        {
            var now = new DateTimeOffset(2026,11,8,12,0,0,TimeSpan.Zero);
            var day = TimeSpan.FromDays(1);
            DateTimeOffset? fresh = now.AddHours(-1), later = now.AddMinutes(5), expired = now.AddDays(-2);
            Check(CacheAge.NextChange([fresh],now,day)==fresh.Value+day,"fresh: at its expiry");
            Check(CacheAge.NextChange([later],now,day)==later,"recorded later than now: when it is reached");
            Check(CacheAge.NextChange([expired,null],now,day) is null && CacheAge.NextChange([],now,day) is null,"expired or no date: never");
            Check(CacheAge.NextChange([fresh,later,expired],now,day)==later,"the earliest change");
            Check(CacheAge.NextChange([fresh],now,TimeSpan.Zero) is null && CacheAge.NextChange([fresh],now,CachePreferences.NeverExpires)==fresh.Value+CachePreferences.NeverExpires,
                "no lifetime, or « never »");
            Check(CacheAge.NextChange([DateTimeOffset.MaxValue.AddDays(-1)],DateTimeOffset.MaxValue.AddDays(-1),CachePreferences.NeverExpires) is null,"no overflow");
            var change = CacheAge.NextChange([fresh],now,day)!.Value;
            Check(CacheAge.Fresh(fresh,change.AddTicks(-1),day) && !CacheAge.Fresh(fresh,change,day),"the mark changes at that very instant");
            Check(CacheAge.NextChange([fresh],change,day) is null,"after it, the key changes again (rebuild)");
            var bank = Read("MainWindow.Bank.cs");
            Check(bank.Contains("TenshoAssistant.Caching.CacheRetention Retention,DateTimeOffset? NextExpiry,DateTime Today);",StringComparison.Ordinal)
                && !bank.Contains("long Second);",StringComparison.Ordinal) && !bank.Contains("now.ToUnixTimeSeconds()",StringComparison.Ordinal),"no per-second field in the frame key");
            Check(bank.Contains("var nextExpiry=TenshoAssistant.Caching.CacheAge.NextChange(selectedEntries.Select(",StringComparison.Ordinal)
                && bank.Contains("configuration.Cache.Get(TenshoAssistant.Caching.CacheArea.ImportedStock),nextExpiry,now.ToLocalTime().Date);",StringComparison.Ordinal),"expiry and local day in the key");
            Check(bank.Contains("expired ? localization.Get(\"BankUi.ExpiredSuffix\"):\"\"",StringComparison.Ordinal),"the « expired » suffix is still written in the title");
        }),
        ("Wiki lines: the achievement named in English is a look-up in the collection index",() =>
        {
            var index = Read("UI/Components/DataIndexes.cs");
            Check(index.Contains("private Dictionary<string,CollectionEntry> achievementsByEnglishName=new(StringComparer.OrdinalIgnoreCase);",StringComparison.Ordinal)
                && index.Contains("var named=new Dictionary<string,CollectionEntry>(StringComparer.OrdinalIgnoreCase);",StringComparison.Ordinal),"case ignored, as before");
            Check(index.Contains("if(row.Kind==CollectionKind.Achievement && row.Achievement is { EnglishName.Length:>0 } english)named.TryAdd(english.EnglishName,row);",StringComparison.Ordinal),
                "achievements with an English name, the first kept (as FirstOrDefault)");
            Check(index.Contains("collectionIndex=index;achievementsByReward=rewards;achievementsByEnglishName=named;",StringComparison.Ordinal),"rebuilt with the index, on a new collection state only");
            var links = Read("MainWindow.SourceLinks.cs");
            Check(links.Contains("? AchievementByEnglishName(line.Text):null;",StringComparison.Ordinal)
                && !links.Contains("collections.State.Entries.FirstOrDefault(entry=>entry.Kind==CollectionKind.Achievement",StringComparison.Ordinal),"no scan per wiki line and frame");
        }),
    ];
}
