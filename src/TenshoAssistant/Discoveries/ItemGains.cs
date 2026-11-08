namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-74 (0.860, user request: learn in game what community data gives, « better take more and remove later »): what changed in the bags between two reads, by item,
/// HQ and NQ together. Pure, for the watcher (ItemDiscoveryWatcher) and its tests.
/// </summary>
public static class ItemGains
{
    public static Dictionary<uint,long> Gains(IReadOnlyDictionary<uint,long> before,IReadOnlyDictionary<uint,long> after)=>
        after.Where(pair=>pair.Value>before.GetValueOrDefault(pair.Key)).ToDictionary(pair=>pair.Key,pair=>pair.Value-before.GetValueOrDefault(pair.Key));

    public static Dictionary<uint,long> Losses(IReadOnlyDictionary<uint,long> before,IReadOnlyDictionary<uint,long> after)=>
        before.Where(pair=>pair.Value>after.GetValueOrDefault(pair.Key)).ToDictionary(pair=>pair.Key,pair=>pair.Value-after.GetValueOrDefault(pair.Key));

    /// <summary>The one item spent, by exactly one unit, in a change (an opened coffer, a used bag); null when anything else was spent too.</summary>
    public static uint? SingleSpent(IReadOnlyDictionary<uint,long> losses)=>losses.Count==1 && losses.First().Value==1 ? losses.First().Key:null;

    /// <summary>What a gain is put down to, in this order; the moments (milliseconds) are the watcher's clock.</summary>
    public enum Cause { None, Fishing, FateReward, MonsterDrop, Opened }
    public static Cause Attribute(bool fishing,bool busy,long now,long fateEndedAt,uint fate,long deathAt,bool inDuty,uint? spent,bool spentUsable,bool spentGained)
    {
        if(fishing)return Cause.Fishing;
        if(busy)return Cause.None;
        if(fate!=0 && now-fateEndedAt is >= 0 and <= 15_000)return Cause.FateReward;
        if(!inDuty && now-deathAt is >= 0 and <= 3_000)return Cause.MonsterDrop;
        if(spent is not null && spentUsable && !spentGained)return Cause.Opened;
        return Cause.None;
    }
}
