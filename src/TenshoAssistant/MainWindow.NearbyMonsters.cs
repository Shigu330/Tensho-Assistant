using TenshoAssistant.Hunting;
using TenshoAssistant.Maps;
using TenshoAssistant.Monsters;

namespace TenshoAssistant;

/// <summary>0.796: why a nearby monster is shown; the first note picks its map icon.</summary>
internal enum MonsterNoteKind { List, HuntLog, Fate, BlueSpell, Collection, Beast }
internal sealed record MonsterNote(MonsterNoteKind Kind,string Text);

// 0.795 (X-34, X-35), 0.796 (X-36), user requests: what each nearby monster means for the player, worked out once a second, drawn every frame.
internal sealed partial class MainWindow
{
    private static readonly IReadOnlyDictionary<uint,IReadOnlyList<MonsterNote>> NoMonsterNotes=new Dictionary<uint,IReadOnlyList<MonsterNote>>();
    private static readonly IReadOnlyDictionary<uint,IReadOnlyList<string>> NoDroppedItems=new Dictionary<uint,IReadOnlyList<string>>();
    private long nearbyMonstersAt=long.MinValue;
    private IReadOnlyDictionary<uint,IReadOnlyList<MonsterNote>> nearbyMonsterNotes=NoMonsterNotes;
    private IReadOnlyDictionary<uint,IReadOnlyList<string>> nearbyDroppedItems=NoDroppedItems;
    private void RefreshNearbyMonsters()
    {
        var now=Environment.TickCount64;
        if(now-nearbyMonstersAt<1000)return;
        nearbyMonstersAt=now;
        nearbyDroppedItems=NoDroppedItems;nearbyMonsterNotes=NoMonsterNotes;
        var monsters=mapLive?.State?.Monsters ?? [];
        if(monsters.Count==0)return;
        var ids=monsters.Select(monster=>monster.NameId).Distinct().ToArray();
        var notes=new Dictionary<uint,List<MonsterNote>>();
        void Note(uint id,MonsterNoteKind kind,string text){if(!notes.TryGetValue(id,out var list))notes[id]=list=[];list.Add(new(kind,text));}
        // X-35: the drops the tracking lists still need.
        if((configuration.ListPanelsNearbyMonsters || configuration.MapView.ShowListMonsters) && lumina?.State.Data is { } data)
        {
            RefreshRecipeHoldingsCache();
            var needed=new HashSet<uint>();
            foreach(var key in new[]{"CraftList","Workshop","Preparation"})
                if(ListPanelLinesFor(key) is { } known)
                    foreach(var (item,amount) in known.Lines)
                        if(GetRecipeHoldings(item) is not { HasObservation:true } held || held.Total<amount)needed.Add(item);
            foreach(var id in ids)
                if(NearbyMonsters.ListDrops(id,data.ItemsDroppedBy,needed) is { Count:>0 } drops)
                    Note(id,MonsterNoteKind.List,localization.Format("MapView.MonsterDrops",string.Join(", ",drops.Select(item=>itemCatalog.State.Catalog?.Find(item)?.Name ?? item.ToString()))));
            if(configuration.ListPanelsNearbyMonsters)nearbyDroppedItems=NearbyMonsters.DroppedNearby(monsters.Select(monster=>(monster.NameId,monster.Name)),data.ItemsDroppedBy,needed);
        }
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
            var spells=collections.State.Entries.Where(entry=>entry.Kind==TenshoAssistant.Collections.CollectionKind.BlueMagic).ToArray();
            blue.LoadCached(spells.Select(entry=>entry.Id).ToArray());
            var notLearned=spells.Where(entry=>!entry.ConfirmedUnlocked).ToDictionary(entry=>entry.Id,entry=>entry.Name);
            foreach(var id in ids)
                if(blue.SpellsOf(id).Where(notLearned.ContainsKey).Select(spell=>notLearned[spell]).ToArray() is { Length:>0 } taught)
                    Note(id,MonsterNoteKind.BlueSpell,localization.Format("MapView.MonsterBlueSpell",string.Join(", ",taught)));
        }
        // X-39: beasts not tamed yet, by the wiki's « tamed from » enemy; as Beastmaster, or always when chosen.
        if(configuration.MapView.ShowBeastMonsters && (configuration.MapView.BeastMonstersAlways || characterStats.State.JobId==BeastmasterJob)
            && BeastWiki?.Book is { } bestiary && Beasts?.State is { Catalog:{ } beastCatalog, Obtained:{ } tamed } && EnglishMonsterIds is { } english)
        {
            if(beastMonstersFor!=(BeastWiki.Revision,english)){beastMonstersFor=(BeastWiki.Revision,english);beastMonsters=BeastWikiParser.Map(bestiary.Rows,english);}
            var names=beastCatalog.Beasts.ToDictionary(beast=>beast.Id,beast=>beast.Name);
            foreach(var id in ids)
                if(beastMonsters.TryGetValue(id,out var beasts) && beasts.Where(beast=>!tamed.Contains(beast) && names.ContainsKey(beast)).Select(beast=>names[beast]).ToArray() is { Length:>0 } wild)
                    Note(id,MonsterNoteKind.Beast,localization.Format("MapView.MonsterBeast",string.Join(", ",wild)));
        }
        // X-41: drops that unlock a mount, a minion or an orchestrion roll not owned (read in game; not read: nothing said).
        if(configuration.MapView.ShowCollectionMonsters && lumina?.State.Data is { } loot && itemCatalog.State.Catalog is { } catalog && collections.State.Entries is { Count:>0 } collected)
        {
            var missingMounts=collected.Where(entry=>entry.Kind==TenshoAssistant.Collections.CollectionKind.Mount && !entry.ConfirmedUnlocked).Select(entry=>entry.Id).ToHashSet();
            var missingMinions=collected.Where(entry=>entry.Kind==TenshoAssistant.Collections.CollectionKind.Companion && !entry.ConfirmedUnlocked).Select(entry=>entry.Id).ToHashSet();
            var missingRolls=collected.Where(entry=>entry.Kind==TenshoAssistant.Collections.CollectionKind.Orchestrion && !entry.ConfirmedUnlocked).Select(entry=>entry.Id).ToHashSet();
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
    private IReadOnlyList<MonsterNote> MonsterNotes(MapMonster monster,IReadOnlyList<MapFateMarker>? fates)
    {
        var byName=nearbyMonsterNotes.TryGetValue(monster.NameId,out var notes) ? notes:[];
        if(monster.FateId==0 || !configuration.MapView.ShowFateMonsters)return byName;
        var fate=fates?.FirstOrDefault(running=>running.Id==monster.FateId)?.Name;
        return [new MonsterNote(MonsterNoteKind.Fate,localization.Format("MapView.MonsterFate",fate ?? monster.FateId.ToString())),..byName];
    }
}
