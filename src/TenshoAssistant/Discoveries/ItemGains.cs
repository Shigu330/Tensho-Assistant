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

    /// <summary>
    /// X-73.18 (0.888): an item id without its HQ (+1 000 000) or collectable (+500 000) offset. 0.911 (X-76): key items (EventItem rows, 2 000 000 and up)
    /// have no offset and are kept as they are.
    /// </summary>
    public const uint KeyItemStart=2_000_000;
    public static uint BaseItem(uint raw)=>raw>=KeyItemStart ? raw:raw%500_000;
    /// <summary>X-76 E1 (0.916): a raw item id carrying the HQ offset (1 000 000 to 1 999 999), as the loot window and the venture and desynthesis results give it.</summary>
    public static bool IsHq(uint raw)=>raw is >= 1_000_000 and < KeyItemStart;

    /// <summary>
    /// X-73.18 (0.898, user request: multiple catches happen often): the shadows of the fish of one multiple catch. The fish caught (chat) and the slots speared
    /// (window) must be as many. All slots alike: every fish has that shadow. Otherwise a fish whose shadow you already saw takes it, and the last fish left takes the
    /// last shadow left (or all the fish left take it when the shadows left are alike). What stays ambiguous is left out.
    /// </summary>
    public static IReadOnlyList<(uint Item,int Size,int Speed)> SpearAssign(IReadOnlyList<uint> caught,IReadOnlyList<(int Size,int Speed)> speared,Func<uint,(int Size,int Speed)?> known)
    {
        if(caught.Count==0 || caught.Count!=speared.Count || speared.Any(shadow=>shadow.Size is < 1 or > 3 || shadow.Speed<=0))return [];
        if(speared.Distinct().Count()==1)return caught.Select(item=>(item,speared[0].Size,speared[0].Speed)).ToArray();
        var left=speared.ToList();var unknown=new List<uint>();var result=new List<(uint,int,int)>();
        foreach(var item in caught)
            if(known(item) is { } shadow && left.Remove(shadow))result.Add((item,shadow.Size,shadow.Speed));
            else unknown.Add(item);
        if(unknown.Count>0 && left.Distinct().Count()==1)result.AddRange(unknown.Select(item=>(item,left[0].Size,left[0].Speed)));
        return result;
    }

    /// <summary>
    /// X-73.18 (0.890, 0.891): the shadow a speared fish had, from the slots that left the window with their size and speed kept just before the catch (size 1–3,
    /// speed): known only when they all had the same size and speed, else null.
    /// </summary>
    public static (int Size,int Speed)? SpearShadow(IReadOnlyList<(int Size,int Speed)> inSight)=>
        inSight.Distinct().ToArray() is [var only] && only.Size is >= 1 and <= 3 && only.Speed>0 ? only:null;

    /// <summary>X-73.18 (0.887): the bite strength from the player's animation slot 7 at the bite (FishingHookStrength 292 !, 293 !!, 294 !!!); 0 unknown.</summary>
    public static uint BiteStrength(ushort timeline)=>timeline switch{292=>1,293=>2,294=>3,_=>0};

    /// <summary>The one item spent, by exactly one unit, in a change (an opened coffer, a used bag); null when anything else was spent too.</summary>
    public static uint? SingleSpent(IReadOnlyDictionary<uint,long> losses)=>losses.Count==1 && losses.First().Value==1 ? losses.First().Key:null;

    /// <summary>What a gain is put down to.</summary>
    public enum Cause { None, Fishing, FateReward, MonsterDrop, Opened, DungeonCoffer }
    /// <summary>
    /// What a gain is put down to, in this order; the moments (milliseconds) are the watcher's clock. X-73.17 (0.873): field content (Eureka, Bozja, Occult
    /// Crescent, deep dungeons) is a duty where monsters still drop into the bags, so a kill counts there; in a deep dungeon a gain with nothing spent and no kill
    /// is a coffer of the floor (DungeonCoffer).
    /// </summary>
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
