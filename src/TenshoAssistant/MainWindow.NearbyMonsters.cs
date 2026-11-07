using TenshoAssistant.Hunting;
using TenshoAssistant.Maps;
using TenshoAssistant.Monsters;

namespace TenshoAssistant;

/// <summary>0.796: why a nearby monster is shown; the first note picks its map icon.</summary>
internal enum MonsterNoteKind { List, HuntLog, Fate }
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
        nearbyMonsterNotes=notes.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<MonsterNote>)pair.Value);
    }
    // 0.797 (X-37): a FATE's monster is told per monster, not per name: the same monster outside the FATE is not part of it.
    private IReadOnlyList<MonsterNote> MonsterNotes(MapMonster monster,IReadOnlyList<MapFateMarker>? fates)
    {
        var byName=nearbyMonsterNotes.TryGetValue(monster.NameId,out var notes) ? notes:[];
        if(monster.FateId==0 || !configuration.MapView.ShowFateMonsters)return byName;
        var fate=fates?.FirstOrDefault(running=>running.Id==monster.FateId)?.Name;
        return [new MonsterNote(MonsterNoteKind.Fate,localization.Format("MapView.MonsterFate",fate ?? monster.FateId.ToString())),..byName];
    }
}
