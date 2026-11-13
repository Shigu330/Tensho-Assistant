using TenshoAssistant.Collections;

namespace TenshoAssistant.Monsters;

/// <summary>0.795 (X-34, X-35): what a nearby monster means for the player; information only, from the game's data and the player's lists.</summary>
public static class NearbyMonsters
{
    /// <summary>
    /// X-160 (0.1268): the side of a living combatant in a running FATE, from the game's own « hostile » flag: an enemy of the FATE, or an
    /// ally fighting beside the players (an escort, the guards of a defence). Outside a FATE: no side.
    /// </summary>
    public static FateSide SideOf(uint fateId,bool hostile)=>fateId==0 ? FateSide.None:hostile ? FateSide.Enemy:FateSide.Ally;

    /// <summary>The items this monster drops that the tracking lists still need, each once, in the order of its drops.</summary>
    public static IReadOnlyList<uint> ListDrops(uint nameId,Func<uint,IReadOnlyList<uint>> drops,IReadOnlySet<uint> needed)
        =>needed.Count==0 ? []:drops(nameId).Where(needed.Contains).Distinct().ToArray();

    /// <summary>0.800 (X-41): the items this monster drops that unlock a collection entry still missing (mount, minion, orchestrion roll), each once.</summary>
    public static IReadOnlyList<uint> CollectionDrops(uint nameId,Func<uint,IReadOnlyList<uint>> drops,Func<uint,bool> missing)
        =>drops(nameId).Where(missing).Distinct().ToArray();

    /// <summary>For each needed item, the names of the nearby monsters that drop it, each once.</summary>
    public static IReadOnlyDictionary<uint,IReadOnlyList<string>> DroppedNearby(IEnumerable<(uint NameId,string Name)> monsters,Func<uint,IReadOnlyList<uint>> drops,IReadOnlySet<uint> needed)
    {
        var result=new Dictionary<uint,List<string>>();
        foreach(var (nameId,name) in monsters)
            foreach(var item in ListDrops(nameId,drops,needed))
            {
                if(!result.TryGetValue(item,out var names))result[item]=names=[];
                if(!names.Contains(name))names.Add(name);
            }
        return result.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<string>)pair.Value);
    }
}

/// <summary>X-160 (0.1268): whether a combatant of a FATE fights against the players or beside them.</summary>
public enum FateSide { None, Enemy, Ally }

/// <summary>X-76 (0.956): the collection entries still missing that a nearby monster's drop can unlock, built once per published collections state.</summary>
public sealed record NearbyMissingCollections(IReadOnlySet<uint> Mounts,IReadOnlySet<uint> Minions,IReadOnlySet<uint> Rolls)
{
    public static NearbyMissingCollections From(IReadOnlyList<CollectionEntry> entries)
    {
        HashSet<uint> Missing(CollectionKind kind)=>entries.Where(entry=>entry.Kind==kind && !entry.ConfirmedUnlocked).Select(entry=>entry.Id).ToHashSet();
        return new(Missing(CollectionKind.Mount),Missing(CollectionKind.Companion),Missing(CollectionKind.Orchestrion));
    }
}

/// <summary>X-76 (0.956): the blue magic spells (their ids) and those not learned yet (id to name), built once per published collections state.</summary>
public sealed record NearbyBlueSpells(IReadOnlyList<uint> Spells,IReadOnlyDictionary<uint,string> NotLearned)
{
    public static NearbyBlueSpells From(IReadOnlyList<CollectionEntry> entries)
    {
        var spells=entries.Where(entry=>entry.Kind==CollectionKind.BlueMagic).ToArray();
        return new(spells.Select(entry=>entry.Id).ToArray(),spells.Where(entry=>!entry.ConfirmedUnlocked).ToDictionary(entry=>entry.Id,entry=>entry.Name));
    }
}
