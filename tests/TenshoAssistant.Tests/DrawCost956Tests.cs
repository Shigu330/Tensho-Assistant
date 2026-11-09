using TenshoAssistant.Caching;
using TenshoAssistant.Collections;
using TenshoAssistant.Maps;
using TenshoAssistant.Monsters;

// X-76 step 6 (0.956): work moved out of the draw loop, second batch. The nearby-monster notes build their collection sets and beast names once
// per published state; the map menus keep their zone list, floors and widths until the shown map, the language or the font size changes; the
// guide's « Windows » step works out its marks when a state it reads changes; the Collections summary counts are made with the cached list.
internal static class DrawCost956Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static int Count(string text,string part) => text.Split(part).Length-1;
    private static CollectionEntry Entry(CollectionKind kind,uint id,bool unlocked,bool negative=false) => new(kind,id,"Name"+id,0,unlocked,NegativeConfirmed:negative);
    private static MapDefinition Map(uint id,uint territory,int category,int priority,int index=0,uint regionPlace=22)
        => new(id,territory,"La Noscea","Zone"+id,"",index,100,RegionPlaceId:regionPlace,MenuCategory:category,MenuPriority:priority);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Reference memo: built once per published object, again for another object even with the same content",() =>
        {
            var calls = 0;
            var memo = new ReferenceMemo<CollectionState,int>(state=>{calls++;return state.Entries.Count;});
            var first = new CollectionState(1,null,[Entry(CollectionKind.Mount,1,false)],false,null);
            Check(memo.Of(first)==1 && memo.Of(first)==1 && memo.Builds==1 && calls==1,"same object: built once");
            var second = first with { };
            Check(memo.Of(second)==1 && memo.Builds==2,"another object, same content: built again (states are replaced, never changed in place)");
            var third = new CollectionState(1,null,[],false,null);
            Check(memo.Of(third)==0 && memo.Of(first)==1 && memo.Builds==4,"each change of object builds again, also back to an older one");
        }),
        ("Nearby monsters: the missing mounts, minions, rolls and the blue spells are the former per-second sets",() =>
        {
            CollectionEntry[] entries = [Entry(CollectionKind.Mount,1,false),Entry(CollectionKind.Mount,2,true),Entry(CollectionKind.Companion,3,false),
                Entry(CollectionKind.Orchestrion,4,false),Entry(CollectionKind.Orchestrion,5,true),Entry(CollectionKind.BlueMagic,6,false),
                Entry(CollectionKind.BlueMagic,7,true),Entry(CollectionKind.Emote,8,false),Entry(CollectionKind.Companion,9,false,true)];
            var missing = NearbyMissingCollections.From(entries);
            Check(missing.Mounts.SetEquals([1u]) && missing.Minions.SetEquals([3u,9u]) && missing.Rolls.SetEquals([4u]),"not unlocked, per kind; unknown and « not obtained » alike");
            var blue = NearbyBlueSpells.From(entries);
            Check(blue.Spells.SequenceEqual([6u,7u]) && blue.NotLearned.Count==1 && blue.NotLearned[6]=="Name6","spell ids in order, not learned by name");
            var empty = NearbyMissingCollections.From([]);
            Check(empty.Mounts.Count==0 && NearbyBlueSpells.From([]).Spells.Count==0,"nothing read: nothing missing");
            var page = Read("MainWindow.NearbyMonsters.cs");
            Check(page.Contains("var (missingMounts,missingMinions,missingRolls)=nearbyMissingCollections.Of(collected);",StringComparison.Ordinal)
                && page.Contains("var (spells,notLearned)=nearbyBlueSpells.Of(collections.State);",StringComparison.Ordinal)
                && page.Contains("var names=nearbyBeastNames.Of(beastCatalog);",StringComparison.Ordinal),"the refresh reads the memos");
            Check(page.Contains("new(state=>NearbyMissingCollections.From(state.Entries));",StringComparison.Ordinal)
                && page.Contains("new(catalog=>catalog.Beasts.ToDictionary(beast=>beast.Id,beast=>beast.Name));",StringComparison.Ordinal),"keyed on the collections state and the beast catalogue");
            Check(!page.Contains("collected.Where(",StringComparison.Ordinal) && Count(page,"collections.State.Entries.Where(")==0
                && Count(page,"beastCatalog.Beasts.ToDictionary(")==0,"no pass over every collection row or beast each second");
            Check(page.Contains("if(now-nearbyMonstersAt<1000)return;",StringComparison.Ordinal) && page.Contains("blue.LoadCached(spells);",StringComparison.Ordinal),"same cadence, disk copies still asked once");
        }),
        ("Map menus: zones and floors kept per catalogue, region, shown map and quest map, same lists as before",() =>
        {
            var region = new MapDefinition(1000,0,"La Noscea","La Noscea","",0,100,RegionPlaceId:22,MenuCategory:1,MenuPriority:0);
            MapDefinition[] maps = [Map(10,134,1,1),Map(11,135,1,2),Map(12,135,1,2,1),Map(20,136,0,0),Map(30,137,2,1),Map(40,138,0,0,0,99)];
            var catalog = new MapCatalog(maps);
            IReadOnlyList<IReadOnlyList<MapDefinition>> Former(MapCatalog source,MapDefinition? at,MapDefinition shown,uint quest)
                => MapMenus.WithExtras(MapMenus.Zones(source.Entries,at),at,shown,source.Find(quest));
            bool Same(IReadOnlyList<IReadOnlyList<MapDefinition>> left,IReadOnlyList<IReadOnlyList<MapDefinition>> right)
                => left.Count==right.Count && left.Zip(right).All(pair=>pair.First.SequenceEqual(pair.Second));
            var menu = new MapZoneMenu();
            var shown = catalog.Find(11)!;
            menu.Update(catalog,region,shown,0);
            Check(menu.Builds==1 && Same(menu.Zones,Former(catalog,region,shown,0)) && menu.Zones.Count==2,"the zone list as before");
            Check(menu.Floors.Select(floor=>floor.MapId).SequenceEqual([11u,12u]),"the shown map's floors");
            var zones = menu.Zones;
            menu.Update(catalog,region,shown,0);
            Check(menu.Builds==1 && ReferenceEquals(zones,menu.Zones),"nothing changed: kept, not built again");
            menu.Update(catalog,region,shown,20);
            Check(menu.Builds==2 && Same(menu.Zones,Former(catalog,region,shown,20)) && menu.Zones.Count==3,"the quest's map without category added");
            menu.Update(catalog,region,shown,40);
            Check(menu.Builds==3 && menu.Zones.Count==2,"a quest map of another region is not added");
            var other = catalog.Find(10)!;
            menu.Update(catalog,region,other,40);
            Check(menu.Builds==4 && menu.Floors.Select(floor=>floor.MapId).SequenceEqual([10u]),"another map: its own floors");
            var reloaded = new MapCatalog(maps);
            menu.Update(reloaded,region,reloaded.Find(10)!,40);
            Check(menu.Builds==5,"a catalogue built again (language): the lists too");
            menu.Update(reloaded,null,reloaded.Find(10)!,40);
            Check(menu.Builds==6 && menu.Zones.Count==0 && menu.Floors.Count==0,"no region (world map): empty, as before");
            var view = Read("MainWindow.MapView.cs");
            Check(Count(view,"mapZoneMenu.Update(catalog,")==2 && !view.Contains("MapMenus.Zones(catalog.Entries",StringComparison.Ordinal),"both layouts read the kept lists");
            Check(view.Contains("var widthKey=new MapMenuWidthKey(catalog,mapZoneMenu.Builds,localization.Revision,ImGui.GetFontSize());",StringComparison.Ordinal)
                && view.Contains("var listKey=new MapMenuWidthKey(catalog,mapZoneMenu.Builds,localization.Revision,ImGui.GetFontSize());",StringComparison.Ordinal),"widths per lists, language and font size");
            Check(view.Contains("ImGui.SetNextItemWidth(MenuWidth(mapZoneNamesWidth,true));",StringComparison.Ordinal) && view.Contains("private static float MenuWidth(float text,bool wing)",StringComparison.Ordinal)
                && Count(view,"MenuTextWidth(")==4,"the names are measured only when the key changes; the padding on every frame");
        }),
        ("Guide « Windows » step: the marks are worked out when a state they read changes, then drawn from the copy",() =>
        {
            var guide = Read("MainWindow.Guide.cs");
            Check(guide.Contains("var key = new GuideWindowsKey(collections.State, bank.State, craftLog.Observation, Beasts?.State, configuration.Voyages is { Count: > 0 },",StringComparison.Ordinal),
                "keyed on every input of GuideWindowDone");
            Check(guide.Contains("guideWindowsDone = tasks.Select(task => GuideWindowDone(task.Key)).ToArray();",StringComparison.Ordinal)
                && guide.Contains("var done = check ? guideWindowsDone[index] : configuration.GuideWindowsDone?.Contains(task.Key) == true;",StringComparison.Ordinal),"computed once, drawn from the copy");
            Check(guide.Contains("guideWindowsKey = key with { Saved = configuration.GuideWindowsDone, SavedCount = configuration.GuideWindowsDone?.Count ?? 0 };",StringComparison.Ordinal),
                "a mark saved for the first time does not ask for another pass");
            Check(Count(guide,"GuideWindowDone(task.Key)")==2 && guide.Contains("guideChecklistNext = now + System.Diagnostics.Stopwatch.Frequency * 2;",StringComparison.Ordinal),
                "no call per task per frame; the Home list keeps its two seconds");
        }),
        ("Collections summary: the three counts are made with the cached list, not on every frame",() =>
        {
            var page = Read("MainWindow.Collections.cs");
            var rebuilt = page.IndexOf("collectionViewEntries = (ExtraKinds.Contains(kind)",StringComparison.Ordinal);
            var counted = page.IndexOf("collectionViewAcquired=collectionViewEntries.Count(entry=>entry.ConfirmedUnlocked);",StringComparison.Ordinal);
            var matches = page.IndexOf("collectionViewMatches = collectionViewEntries.Where(",StringComparison.Ordinal);
            Check(rebuilt>0 && counted>rebuilt && counted<matches,"inside the cache block, after the list (and its blue magic order)");
            Check(page.Contains("collectionViewNotAcquired=collectionViewEntries.Count(entry=>entry.Acquisition==CollectionAcquisition.NotAcquired);",StringComparison.Ordinal)
                && page.Contains("collectionViewUnknown=collectionViewEntries.Count(entry=>entry.Acquisition==CollectionAcquisition.Unknown);",StringComparison.Ordinal),"the same three counts");
            Check(page.Contains("\"CollectionSummary.Acquired\",collectionViewAcquired,entries.Length));",StringComparison.Ordinal)
                && page.Contains("\"CollectionSummary.Missing\",collectionViewNotAcquired,collectionViewUnknown));",StringComparison.Ordinal),"the summary reads them");
            Check(!page.Contains("entries.Count(entry=>",StringComparison.Ordinal),"no count over the whole kind per frame");
        }),
    ];
}
