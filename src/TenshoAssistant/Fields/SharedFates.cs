namespace TenshoAssistant.Fields;

/// <summary>X-213: one article of a zone's gemstone trader as the sheets list it: the item, its price (the currency item and how many) and
/// the achievement it needs (0: none).</summary>
public sealed record SharedFateShopEntry(uint Item,uint CostItem,int Cost,uint Gate);

/// <summary>X-213: an article of the zone's gemstone trader and the shared FATE rank from which it is offered.</summary>
public sealed record SharedFateReward(uint Item,string Name,uint Icon,uint CostItem,string CostName,int Cost,int Rank);

/// <summary>
/// X-213: a zone with shared FATEs, from the game's sheets: FateProgressUI (the zone, the FATEs completed with the highest rating that each
/// next rank asks, its order) and, when the zone has one, its gemstone trader (FateShop, placed in the zone by the Level sheet): the articles
/// by rank (<see cref="SharedFates.Ranks"/>) and the zone's rank achievement that the last rank's articles need. <paramref name="TraderKnown"/>
/// false: the sheets tie no trader's article to this zone's ranks (the zones of Shadowbringers, probed 22/11).
/// </summary>
public sealed record SharedFateZone(uint Territory,string Name,int Order,int MaxRank,IReadOnlyList<int> Needed,IReadOnlyList<SharedFateReward> Rewards,
    string? Achievement,bool TraderKnown);

/// <summary>X-213: what the game's shared FATE window showed for a zone (AgentFateProgress), kept per character; when it was read.</summary>
public sealed class SharedFateRead
{
    public int Rank{get;set;}
    public int MaxRank{get;set;}
    public int Progress{get;set;}
    public int Needed{get;set;}
    public DateTimeOffset ReadAt{get;set;}
}

/// <summary>A zone as the game's window holds it, before it is checked.</summary>
public readonly record struct SharedFateSeen(uint Territory,int Rank,int MaxRank,int Progress,int Needed);

/// <summary>
/// X-213 (the user's request of 12/11, the comparison with other plugins K-23): the shared FATEs. The game's own help (HowToPage rows 2510
/// to 2512): FATEs completed with the highest rating in a zone raise its rank, and a higher rank widens what its gemstone trader offers; the
/// progress is in the Shared FATE window of the Travel menu. Tensho reads that window only once the player opened it (never asked of the
/// game): until then, a zone's rank is unknown, never 0 (R06). Pure (tests).
/// </summary>
public static class SharedFates
{
    /// <summary>The game's Shared FATE window (AddonFateProgress).</summary>
    public const string Window="FateProgress";
    public const int MaximumRank=9;

    /// <summary>The highest rank: 1, and one more for each next rank the sheet asks FATEs for (0: none).</summary>
    public static int MaxRank(IEnumerable<int> neededForNextRanks)=>1+neededForNextRanks.TakeWhile(needed=>needed>0).Count();

    /// <summary>
    /// Each article at the lowest rank it is offered at. The trader's shops are its stock rank by rank: shop i at rank i + 1 (the trader's
    /// greetings, DefaultTalk 5 to 8, one per rank; the shops grow: the map at rank 2, as the shop of index 1); an article that needs the zone's
    /// rank achievement (« Free Market Friend »: « reach shared FATE rank 3 » or 4) comes at the highest rank. An item offered twice keeps its
    /// lowest rank and that price. In the order of the shops.
    /// </summary>
    public static IReadOnlyList<(SharedFateShopEntry Entry,int Rank)> Ranks(int maxRank,IReadOnlyList<IReadOnlyList<SharedFateShopEntry>> shops)
    {
        var found=new List<(SharedFateShopEntry Entry,int Rank)>();
        var at=new Dictionary<uint,int>();
        for(var index=0;index<shops.Count;index++)
            foreach(var entry in shops[index])
            {
                if(entry.Item==0)continue;
                var rank=entry.Gate!=0 ? maxRank:Math.Clamp(index+1,1,maxRank);
                if(!at.TryGetValue(entry.Item,out var place)){at[entry.Item]=found.Count;found.Add((entry,rank));}
                else if(rank<found[place].Rank)found[place]=(entry,rank);
            }
        return [..found.Select((pair,order)=>(pair,order)).OrderBy(item=>item.pair.Rank).ThenBy(item=>item.order).Select(item=>item.pair)];
    }

    /// <summary>The articles still to come for a character's rank in the zone; null when the rank is unknown (the window not opened yet).</summary>
    public static IReadOnlyList<SharedFateReward>? Remaining(SharedFateZone zone,SharedFateRead? read)=>
        read is null ? null:[..zone.Rewards.Where(reward=>reward.Rank>read.Rank)];

    /// <summary>
    /// What the window showed, merged with what was kept for the character: each zone read replaces the one kept (with the time read when it
    /// changed, or when <paramref name="stamp"/>: the first reading of an opening); the zones not shown stay as they were. A zone that does
    /// not hold a rank (0, not filled yet), or holds values that cannot be (a rank over its highest one, a negative count), is left out: never
    /// a false rank (R08). Null when nothing changed; else a new table (the kept one is never changed in place).
    /// </summary>
    public static Dictionary<uint,SharedFateRead>? Merged(IReadOnlyDictionary<uint,SharedFateRead>? kept,IEnumerable<SharedFateSeen> seen,DateTimeOffset now,bool stamp)
    {
        Dictionary<uint,SharedFateRead>? next=null;
        foreach(var zone in seen)
        {
            if(!Valid(zone))continue;
            var before=kept?.GetValueOrDefault(zone.Territory);
            var same=before is not null && before.Rank==zone.Rank && before.MaxRank==zone.MaxRank && before.Progress==zone.Progress && before.Needed==zone.Needed;
            if(same && !stamp)continue;
            next??=kept is null ? []:new(kept);
            next[zone.Territory]=new SharedFateRead{Rank=zone.Rank,MaxRank=zone.MaxRank,Progress=zone.Progress,Needed=zone.Needed,ReadAt=now};
        }
        return next;
    }

    /// <summary>Whether the window's values for a zone can be kept.</summary>
    public static bool Valid(SharedFateSeen zone)=>
        zone.Territory!=0 && zone.MaxRank is >= 1 and <= MaximumRank && zone.Rank>=1 && zone.Rank<=zone.MaxRank && zone.Progress>=0 && zone.Needed>=0;

    /// <summary>The last time the window was read for the character (null: never).</summary>
    public static DateTimeOffset? LastRead(IReadOnlyDictionary<uint,SharedFateRead>? kept)=>
        kept is { Count:>0 } ? kept.Values.Max(read=>read.ReadAt):null;
}
