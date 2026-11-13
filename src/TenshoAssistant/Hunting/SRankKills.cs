namespace TenshoAssistant.Hunting;

/// <summary>
/// X-222 (0.1232, user's request of 12/11, K-38): the player's own kills counted for the S ranks whose condition is to defeat a number of monsters
/// (« Kill », « KillFullMoon » in <see cref="SRankConditions.Marks"/>): a kill counts when the game's « You defeat … » message names a monster of the
/// mark's list, in the mark's zone. Only the player's own kills: others' kills, which the game also counts for the spawn, are not seen, so the counter
/// says « at least you ». Kept in the settings (key « mark:monster »), cleared by the player's click (after the mark appeared, for example).
/// </summary>
public static class SRankKills
{
    /// <summary>A count is never beyond this (a damaged setting cannot show a huge number).</summary>
    public const int MaxCount=100_000;

    public static bool Counted(SRankMark mark)=>mark.Territory!=0 && mark.Condition is "Kill" or "KillFullMoon";

    /// <summary>Key of a counter in the settings.</summary>
    public static string Key(uint mark,uint monster)=>$"{mark}:{monster}";

    /// <summary>The counters a kill of <paramref name="monster"/> (BNpcName row) in <paramref name="territory"/> adds one to; empty for most kills.</summary>
    public static IReadOnlyList<string> KeysFor(uint monster,uint territory)
    {
        if(monster==0 || territory==0)return [];
        return SRankConditions.Marks.Where(mark=>Counted(mark) && mark.Territory==territory)
            .SelectMany(mark=>mark.Refs.Where(item=>item.Kind==SRankRefKind.Mob && item.Id==monster).Select(item=>Key(mark.Name,item.Id))).ToArray();
    }

    /// <summary>Whether any counted mark is in this zone (nothing is looked at elsewhere).</summary>
    public static bool AnyIn(uint territory)=>territory!=0 && SRankConditions.Marks.Any(mark=>Counted(mark) && mark.Territory==territory);

    /// <summary>Adds one to each key (bounded); true when something changed.</summary>
    public static bool Add(IDictionary<string,int> counts,IEnumerable<string> keys)
    {
        var changed=false;
        foreach(var key in keys)
        {
            var now=Math.Clamp(counts.TryGetValue(key,out var before) ? before:0,0,MaxCount);
            if(now>=MaxCount)continue;
            counts[key]=now+1;changed=true;
        }
        return changed;
    }

    /// <summary>A mark's counters cleared; true when something was there.</summary>
    public static bool Clear(IDictionary<string,int> counts,uint mark)
    {
        var prefix=mark+":";
        var keys=counts.Keys.Where(key=>key.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
        foreach(var key in keys)counts.Remove(key);
        return keys.Length>0;
    }

    /// <summary>A counter read back, bounded (0 when absent or damaged).</summary>
    public static int Get(IReadOnlyDictionary<string,int>? counts,uint mark,uint monster)
        => counts is not null && counts.TryGetValue(Key(mark,monster),out var count) ? Math.Clamp(count,0,MaxCount):0;
}
