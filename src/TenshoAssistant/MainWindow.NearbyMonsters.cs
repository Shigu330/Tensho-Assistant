using TenshoAssistant.Caching;
using TenshoAssistant.Hunting;
using TenshoAssistant.Maps;
using TenshoAssistant.Monsters;

namespace TenshoAssistant;

/// <summary>0.796: why a nearby monster is shown; the first note picks its map icon.</summary>
internal enum MonsterNoteKind { List, HuntLog, Fate, BlueSpell, Collection, Beast, Relic, Wish }
internal sealed record MonsterNote(MonsterNoteKind Kind,string Text);

// 0.795 (X-34, X-35), 0.796 (X-36), user requests: what each nearby monster means for the player, worked out once a second, drawn every frame.
internal sealed partial class MainWindow
{
    private static readonly IReadOnlyDictionary<uint,IReadOnlyList<MonsterNote>> NoMonsterNotes=new Dictionary<uint,IReadOnlyList<MonsterNote>>();
    private static readonly IReadOnlyDictionary<uint,IReadOnlyList<string>> NoDroppedItems=new Dictionary<uint,IReadOnlyList<string>>();
    // 0.828: not long.MinValue: now minus it overflows to a negative number, and the refresh never ran (X-53).
    private long nearbyMonstersAt=-100_000;
    private IReadOnlyDictionary<uint,IReadOnlyList<MonsterNote>> nearbyMonsterNotes=NoMonsterNotes;
    private IReadOnlyDictionary<uint,IReadOnlyList<string>> nearbyDroppedItems=NoDroppedItems;
    // X-76 (0.956): what the notes draw from the collections and the beast catalogue, built again only when a new state or catalogue is published
    // (the notes themselves are still worked out once a second).
    private readonly ReferenceMemo<TenshoAssistant.Collections.CollectionState,NearbyMissingCollections> nearbyMissingCollections=new(state=>NearbyMissingCollections.From(state.Entries));
    private readonly ReferenceMemo<TenshoAssistant.Collections.CollectionState,NearbyBlueSpells> nearbyBlueSpells=new(state=>NearbyBlueSpells.From(state.Entries));
    private readonly ReferenceMemo<TenshoAssistant.Beastmaster.BeastCatalog,Dictionary<uint,string>> nearbyBeastNames=new(catalog=>catalog.Beasts.ToDictionary(beast=>beast.Id,beast=>beast.Name));
    // X-40 (0.1002): every crafting list's materials still missing, every 10 s.
    private (long At,HashSet<uint> Items) allCraftListNeeds=(-100_000,[]);
    private HashSet<uint> AllCraftListNeeds()
    {
        var now=Environment.TickCount64;
        if(now-allCraftListNeeds.At<10_000)return allCraftListNeeds.Items;
        var items=new HashSet<uint>();
        if(CraftListStore is { Unavailable:false, Lists:{ Count:>0 } lists })
            foreach(var list in lists)
                foreach(var (item,amount) in Craft.CraftLists.Ingredients(CraftListPlans(list)))
                    if(GetRecipeHoldings(item) is not { HasObservation:true } held || held.Total<amount)items.Add(item);
        allCraftListNeeds=(now,items);
        return items;
    }
    /// <summary>X-40 (0.1002): the wishes not obtained: a collection entry not acquired, or an item held nowhere (unknown counts as not held).</summary>
    private HashSet<uint> WishesNotObtained()
    {
        var items=itemCatalog.State.Catalog;var wanted=new HashSet<uint>();
        foreach(var id in configuration.Wishlist ?? [])
        {
            var definition=items?.Find(id);
            if(CollectionOf(definition) is { } entry){if(CollectionStateOf(entry)!=TenshoAssistant.Collections.CollectionAcquisition.Acquired)wanted.Add(id);continue;}
            if(GetRecipeHoldings(id) is not { HasObservation:true, Total:>0 })wanted.Add(id);
        }
        return wanted;
    }
    /// <summary>
    /// X-40 (0.1002): the relic book under way and what its monsters still need (by the game's name), from the Relics page's reading; the catalogue is loaded
    /// and the book read again once a minute while the layer is on. Nothing known yet: empty.
    /// </summary>
    private long relicNearbyReadAt=-100_000;
    private Dictionary<string,int> RelicNeeds()
    {
        if(!relicCatalogTried && RelicCatalogLoad is { } load){relicCatalogTried=true;relicCatalogTask=Task.Run(load);}
        if(relicCatalogTask is { IsCompleted:true } done && relicCatalog is null)relicCatalog=done.IsCompletedSuccessfully ? done.Result:null;
        ConsumeRelicRead();
        var now=Environment.TickCount64;
        if(relicRead is null && ReadRelics is { } read && now-relicNearbyReadAt>60_000){relicNearbyReadAt=now;relicRead=read();}
        var needs=new Dictionary<string,int>(StringComparer.CurrentCultureIgnoreCase);
        if(relicProgress is not { Note:not 0 } progress || relicCatalog?.Books.GetValueOrDefault(progress.Note) is not { } book)return needs;
        for(var index=0;index<book.Monsters.Count;index++)
        {
            var target=book.Monsters[index];
            var left=target.Needed-(index<progress.Monsters.Count ? progress.Monsters[index]:0);
            if(left>0 && target.Name.Trim().Length>0)needs[target.Name.Trim()]=left;
        }
        return needs;
    }
    private void RefreshNearbyMonsters()
    {
        var now=Environment.TickCount64;
        if(now-nearbyMonstersAt<1000)return;
        nearbyMonstersAt=now;
        nearbyDroppedItems=NoDroppedItems;nearbyMonsterNotes=NoMonsterNotes;
        var monsters=mapLive?.State?.Monsters ?? [];
        if(monsters.Count==0){if(developerMode)NearbyMonstersLog($"no monster read (map state {(mapLive?.State is null ? "none":"read")})");return;}
        var ids=monsters.Select(monster=>monster.NameId).Distinct().ToArray();
        var notes=new Dictionary<uint,List<MonsterNote>>();
        void Note(uint id,MonsterNoteKind kind,string text){if(!notes.TryGetValue(id,out var list))notes[id]=list=[];list.Add(new(kind,text));}
        // X-35: the drops the tracking lists still need.
        // X-40 (0.1002, DEC-049): the wishes not obtained yet use the same drops.
        var wantList=configuration.ListPanelsNearbyMonsters || configuration.MapView.ShowListMonsters;
        var wantWish=configuration.MapView.ShowWishMonsters;
        if((wantList || wantWish) && lumina?.State.Data is { } data)
        {
            RefreshRecipeHoldingsCache();
            var needed=new HashSet<uint>();
            if(wantList)
            {
                foreach(var key in new[]{"CraftList","Workshop","Preparation"})
                    if(ListPanelLinesFor(key) is { } known)
                        foreach(var (item,amount) in known.Lines)
                            if(GetRecipeHoldings(item) is not { HasObservation:true } held || held.Total<amount)needed.Add(item);
                // X-40 (0.1002): every crafting list, not only the one in a panel (worked out again every 10 s).
                if(configuration.MapView.ListMonstersAllLists)needed.UnionWith(AllCraftListNeeds());
            }
            var wishes=wantWish ? WishesNotObtained():[];
            // 0.819 (X-53, user report: a woolly yak, a loaghtan not marked): drops by the monster's row, by its name (other rows of the same name), and by the wiki's
            // pages already on disk for the items the list needs (their English monster names matched to rows); no request is made.
            var names=monsters.GroupBy(monster=>monster.NameId).ToDictionary(group=>group.Key,group=>group.First().Name);
            var wikiDrops=new Dictionary<uint,List<uint>>();
            if(wikiMonsterSources is { } wiki && EnglishMonsterIds is { } englishIds)
                foreach(var item in needed.Concat(wishes))
                {
                    wiki.Warm(item);
                    if(wiki.Cached(item) is not { Monsters:{ Count:>0 } rows })continue;
                    foreach(var row in rows)
                        foreach(var id in englishIds(row.Name.Trim()))
                        {
                            if(!wikiDrops.TryGetValue(id,out var items))wikiDrops[id]=items=[];
                            if(!items.Contains(item))items.Add(item);
                        }
                }
            // X-75.2 (0.875): the drops you saw from that monster first.
            var seenDrops=Seen()?.DropsByMonster;
            IReadOnlyList<uint> Drops(uint id)=>(seenDrops?.GetValueOrDefault(id) ?? []).Concat(data.ItemsDroppedBy(id)).Concat(names.TryGetValue(id,out var name) ? data.ItemsDroppedByName(name):[])
                .Concat(wikiDrops.TryGetValue(id,out var fromWiki) ? fromWiki:[]).Distinct().ToArray();
            if(wantList)
                foreach(var id in ids)
                    if(NearbyMonsters.ListDrops(id,Drops,needed) is { Count:>0 } drops)
                        Note(id,MonsterNoteKind.List,localization.Format("MapView.MonsterDrops",string.Join(", ",drops.Select(item=>itemCatalog.State.Catalog?.Find(item)?.Name ?? item.ToString()))));
            if(wishes.Count>0)
                foreach(var id in ids)
                    if(NearbyMonsters.ListDrops(id,Drops,wishes) is { Count:>0 } wished)
                        Note(id,MonsterNoteKind.Wish,localization.Format("MapView.MonsterWish",string.Join(", ",wished.Select(item=>itemCatalog.State.Catalog?.Find(item)?.Name ?? item.ToString()))));
            if(configuration.ListPanelsNearbyMonsters)nearbyDroppedItems=NearbyMonsters.DroppedNearby(monsters.Select(monster=>(monster.NameId,monster.Name)),Drops,needed);
            // 0.818 (X-53, user report: the woolly yak, the loaghtan not marked): what the chain sees, in developer mode, when it changes.
            if(developerMode)NearbyMonstersLog($"{monsters.Count} monsters ({string.Join(", ",monsters.Select(monster=>$"{monster.Name} {monster.NameId}").Distinct().Take(8))}); list needs {needed.Count} items; "
                +$"drops matched for {notes.Count(pair=>pair.Value.Any(note=>note.Kind==MonsterNoteKind.List))} monsters ({string.Join(", ",ids.Take(8).Select(id=>$"{id}:{data.ItemsDroppedBy(id).Count}/{Drops(id).Count}"))}); wiki {wikiDrops.Count}; "
                +$"panel {configuration.ListPanelsNearbyMonsters}, map {configuration.MapView.ShowListMonsters}, above {configuration.MapView.AboveList}");
        }
        else if(developerMode)NearbyMonstersLog($"{monsters.Count} monsters; drop data {(lumina?.State.Data is null ? "not loaded":"loaded")}, panel {configuration.ListPanelsNearbyMonsters}, map {configuration.MapView.ShowListMonsters}");
        // X-40 (0.1002, DEC-049): the monsters the relic book under way still asks for, by name (the book's names are the game's, in its language).
        if(configuration.MapView.ShowRelicMonsters && RelicNeeds() is { Count:>0 } relicNeeds)
            foreach(var monster in monsters.GroupBy(monster=>monster.NameId).Select(group=>group.First()))
                if(relicNeeds.TryGetValue(monster.Name.Trim(),out var left))Note(monster.NameId,MonsterNoteKind.Relic,localization.Format("MapView.MonsterRelic",left));
        // X-36: what the hunting log still asks for, in each log's current rank.
        if(configuration.MapView.ShowHuntLogMonsters && HuntEntries?.Invoke() is { Count:>0 } entries && HuntSlots?.Invoke() is { } slots)
        {
            var missing=HuntingLog.Missing(entries,slots,HuntGrandCompany?.Invoke());
            foreach(var id in ids)
                if(missing.TryGetValue(id,out var rows))
                    foreach(var (entry,killed,neededKills) in rows)Note(id,MonsterNoteKind.HuntLog,localization.Format("MapView.MonsterHuntLog",entry,killed,neededKills));
        }
        // X-38: the spells a monster teaches that are not learned yet; as Blue Mage, or always when chosen.
        if(configuration.MapView.ShowBlueMageMonsters && (configuration.MapView.BlueMageMonstersAlways || characterStats.State.JobId==BlueMageJob) && BlueSpellNearby is { } blue)
        {
            var (spells,notLearned)=nearbyBlueSpells.Of(collections.State);
            blue.LoadCached(spells);
            foreach(var id in ids)
                if(blue.SpellsOf(id).Where(notLearned.ContainsKey).Select(spell=>notLearned[spell]).ToArray() is { Length:>0 } taught)
                    Note(id,MonsterNoteKind.BlueSpell,localization.Format("MapView.MonsterBlueSpell",string.Join(", ",taught)));
        }
        // X-39: beasts not tamed yet, by the wiki's « tamed from » enemy; as Beastmaster, or always when chosen.
        // X-75.2 (0.875): beasts tamed from a monster (yours or imported) count first, and without the wiki.
        if(configuration.MapView.ShowBeastMonsters && (configuration.MapView.BeastMonstersAlways || characterStats.State.JobId==BeastmasterJob)
            && Beasts?.State is { Catalog:{ } beastCatalog, Obtained:{ } tamed } && (BeastWiki?.Book is not null || Seen()?.BeastsByMonster.Count>0))
        {
            var seenBeasts=Seen()?.BeastsByMonster;
            if(beastMonstersFor!=(BeastWiki?.Revision ?? -1,(object?)EnglishMonsterIds ?? seenBeasts))
            {
                beastMonstersFor=(BeastWiki?.Revision ?? -1,(object?)EnglishMonsterIds ?? seenBeasts);
                var map=BeastWiki?.Book is { } bestiary && EnglishMonsterIds is { } english ? BeastWikiParser.Map(bestiary.Rows,english).ToDictionary(pair=>pair.Key,pair=>pair.Value.ToList()):[];
                foreach(var (monster,list) in seenBeasts ?? new Dictionary<uint,uint[]>())
                    map[monster]=list.Concat(map.GetValueOrDefault(monster) ?? []).Distinct().ToList();
                beastMonsters=map.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<uint>)pair.Value);
            }
            var names=nearbyBeastNames.Of(beastCatalog);
            foreach(var id in ids)
                if(beastMonsters.TryGetValue(id,out var beasts) && beasts.Where(beast=>!tamed.Contains(beast) && names.ContainsKey(beast)).Select(beast=>names[beast]).ToArray() is { Length:>0 } wild)
                    Note(id,MonsterNoteKind.Beast,localization.Format("MapView.MonsterBeast",string.Join(", ",wild)));
        }
        // X-41: drops that unlock a mount, a minion or an orchestrion roll not owned (read in game; not read: nothing said).
        if(configuration.MapView.ShowCollectionMonsters && lumina?.State.Data is { } loot && itemCatalog.State.Catalog is { } catalog && collections.State is { Entries.Count:>0 } collected)
        {
            var (missingMounts,missingMinions,missingRolls)=nearbyMissingCollections.Of(collected);
            bool Missing(uint item)=>catalog.Find(item) is { } found && (found.MountId!=0 && missingMounts.Contains(found.MountId) || found.CompanionId!=0 && missingMinions.Contains(found.CompanionId)
                || found.OrchestrionId!=0 && missingRolls.Contains(found.OrchestrionId));
            foreach(var id in ids)
                if(NearbyMonsters.CollectionDrops(id,loot.ItemsDroppedBy,Missing) is { Count:>0 } wanted)
                    Note(id,MonsterNoteKind.Collection,localization.Format("MapView.MonsterCollection",string.Join(", ",wanted.Select(item=>catalog.Find(item)?.Name ?? item.ToString()))));
        }
        nearbyMonsterNotes=notes.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<MonsterNote>)pair.Value);
    }
    // 0.797 (X-37): a FATE's monster is told per monster, not per name: the same monster outside the FATE is not part of it.
    internal const uint BlueMageJob=36;
    // 0.804: ClassJob row 43 is « beastmaster » (tools/XbmProbe, English).
    internal const uint BeastmasterJob=43;
    internal BeastWikiIndex? BeastWiki { get; set; }
    internal Func<string,IReadOnlyList<uint>>? EnglishMonsterIds { get; set; }
    private (int Revision,object? Names) beastMonstersFor;private IReadOnlyDictionary<uint,IReadOnlyList<uint>> beastMonsters=new Dictionary<uint,IReadOnlyList<uint>>();
    internal BlueSpellMonsterIndex? BlueSpellNearby { get; set; }
    private string nearbyMonstersLogged="";
    // X-177 C6 (0.1189, analysis of 12/11: 8 722 lines in 3.5 days, and the text was built every second out of developer mode too, the
    // « list needs » one calling Drops again for 8 monsters): each caller asks developerMode before it builds the text.
    private void NearbyMonstersLog(string line)
    {
        if(!developerMode || line==nearbyMonstersLogged)return;
        nearbyMonstersLogged=line;
        log.Information("Nearby monsters: {0}",line);
    }
    private IReadOnlyList<MonsterNote> MonsterNotes(MapMonster monster,IReadOnlyList<MapFateMarker>? fates)
    {
        var byName=nearbyMonsterNotes.TryGetValue(monster.NameId,out var notes) ? notes:[];
        if(monster.FateId==0 || !configuration.MapView.ShowFateMonsters)return byName;
        var fate=fates?.FirstOrDefault(running=>running.Id==monster.FateId)?.Name;
        return [new MonsterNote(MonsterNoteKind.Fate,localization.Format("MapView.MonsterFate",fate ?? monster.FateId.ToString())),..byName];
    }
}
