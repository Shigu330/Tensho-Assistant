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

    /// <summary>X-73.12 (0.864): days since 2013-08-27 (the game's relaunch), the scale festivals' days are kept in.</summary>
    private static readonly DateTimeOffset DayZero=new(2013,8,27,0,0,0,TimeSpan.Zero);
    public static int DayNumber(DateTimeOffset at)=>(int)Math.Clamp((at-DayZero).TotalDays,0,100_000);

    /// <summary>X-73.18 (0.887): the bite strength from the player's animation slot 7 at the bite (FishingHookStrength 292 !, 293 !!, 294 !!!); 0 unknown.</summary>
    public static uint BiteStrength(ushort timeline)=>timeline switch{292=>1,293=>2,294=>3,_=>0};

    /// <summary>The one item spent, by exactly one unit, in a change (an opened coffer, a used bag); null when anything else was spent too.</summary>
    public static uint? SingleSpent(IReadOnlyDictionary<uint,long> losses)=>losses.Count==1 && losses.First().Value==1 ? losses.First().Key:null;

    /// <summary>What a gain is put down to, in this order; the moments (milliseconds) are the watcher's clock.</summary>
    /// <summary>
    /// X-73.17 (0.873): field content (Eureka, Bozja, Occult Crescent, deep dungeons) is a duty where monsters still drop into the bags, so a kill counts there;
    /// in a deep dungeon a gain with nothing spent and no kill is a coffer of the floor (DungeonCoffer).
    /// </summary>
    public enum Cause { None, Fishing, FateReward, MonsterDrop, Opened, DungeonCoffer }
    public static Cause Attribute(bool fishing,bool busy,long now,long fateEndedAt,uint fate,long deathAt,bool inDuty,uint? spent,bool spentUsable,bool spentGained,
        bool field=false,bool deepDungeon=false)
    {
        if(fishing)return Cause.Fishing;
        if(busy)return Cause.None;
        if(fate!=0 && now-fateEndedAt is >= 0 and <= 15_000)return Cause.FateReward;
        if((!inDuty || field || deepDungeon) && now-deathAt is >= 0 and <= 3_000)return Cause.MonsterDrop;
        if(spent is not null && spentUsable && !spentGained)return Cause.Opened;
        if(deepDungeon && spent is null)return Cause.DungeonCoffer;
        return Cause.None;
    }
}
