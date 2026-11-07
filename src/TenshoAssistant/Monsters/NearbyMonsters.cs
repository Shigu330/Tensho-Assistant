namespace TenshoAssistant.Monsters;

/// <summary>0.795 (X-34, X-35): what a nearby monster means for the player; information only, from the game's data and the player's lists.</summary>
public static class NearbyMonsters
{
    /// <summary>The items this monster drops that the tracking lists still need, each once, in the order of its drops.</summary>
    public static IReadOnlyList<uint> ListDrops(uint nameId,Func<uint,IReadOnlyList<uint>> drops,IReadOnlySet<uint> needed)
        =>needed.Count==0 ? []:drops(nameId).Where(needed.Contains).Distinct().ToArray();

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
