namespace TenshoAssistant.Island;

/// <summary>N-07 (0.673): what one craft uses, from MJICraftworksObject and MJIItemPouch. Category: the game's name for it.</summary>
public sealed record IslandMaterial(uint ItemId,string Name,int Amount,string Category);

/// <summary>N-10 (0.671): a craft of the island's workshops, from MJICraftworksObject. Id is the row, which indexes popularity and supply.</summary>
public sealed record IslandCraft(uint Id,uint ItemId,string Name,int Hours,int Value,int LevelRequired,IReadOnlyList<uint> Themes,IReadOnlyList<IslandMaterial>? Materials=null);

/// <summary>The workshops' sheets. Popularity: one row per MJICraftworksPopularity, one type per craft row. The ratios are percents, indexed by the row of their sheet.</summary>
public sealed record IslandCatalog(int Rows,IReadOnlyList<IslandCraft> Crafts,IReadOnlyList<byte[]> Popularity,int[] PopularityRatio,int[] SupplyRatio,int[] RankRatio,IReadOnlyDictionary<uint,string> ThemeNames,int[]? GrooveCaps=null)
{
    /// <summary>False after a game update that changed the sheets' shape: nothing is computed from them then.</summary>
    public bool Valid=>Rows>0 && Crafts.Count>0 && Popularity.Count>0 && Popularity.All(row=>row.Length==Rows)
        && PopularityRatio.Length>=5 && SupplyRatio.Length>=5 && RankRatio.Length>=2 && Crafts.All(craft=>craft.Id<Rows);
}

public enum IslandReadStatus { Unavailable, NotOnIsland, NotLoaded, Loaded }
/// <summary>A built workshop. Level: 1 for Workshop I (the game's BuildingLevel plus one).</summary>
public sealed record IslandWorkshop(int Index,int Level,bool UnderConstruction);
/// <summary>A copy of what the game holds, read once on a click. The groove has two readings: the agenda's, then the manager's, which FFXIVClientStructs marks unverified.</summary>
public sealed record IslandSnapshot(IslandReadStatus Status,DateTimeOffset ReadAt,int Rank,IReadOnlyList<IslandWorkshop> Workshops,uint? GrooveAgenda,uint? GrooveManager,int CycleDay,int PopularityRow,int NextPopularityRow,byte[] SupplyDemand,int? Landmarks=null)
{
    public static IslandSnapshot Of(IslandReadStatus status,DateTimeOffset at)=>new(status,at,0,[],null,null,0,0,0,[]);
    public uint Groove=>GrooveAgenda ?? GrooveManager ?? 0;
}

/// <summary>One line of the crafts table. Null: the game's value could not be read for this craft.</summary>
public sealed record IslandRow(IslandCraft Craft,int? Popularity,int? Supply,int? Demand,double? Value);

/// <summary>
/// N-10 (0.671): decoding and the value of a craft: base value × workshop × groove × popularity × supply. The ratios are the sheets'; which row a workshop
/// and a supply use is inferred, and the drift of supply is a community rule (WorkshopOptimizerPlugin, MIT, rewritten): docs/evidence/0.671-island-workshop.md.
/// </summary>
public static class IslandValue
{
    /// <summary>
    /// 0.686 (user report): every slot has a place, built or not: a player with only Fabrique I read as four. In that game the only byte telling them apart is the one
    /// FFXIVClientStructs names HoursToCompletion, 1 for the built one and 0 for the others (docs/evidence/0.686-island-workshops.md). If no slot has it, the place alone decides, as before.
    /// </summary>
    public static IReadOnlyList<IslandWorkshop> BuiltWorkshops(IReadOnlyList<(int Index,int Level,bool Building,int Place,int Marker)> slots)
    {
        var marked=slots.Any(slot=>slot.Marker!=0);
        return slots.Where(slot=>slot.Building || (marked ? slot.Marker!=0:slot.Place!=0)).Select(slot=>new IslandWorkshop(slot.Index,slot.Level,slot.Building)).ToArray();
    }

    public const int UnitsPerSupplyBand=8;
    /// <summary>The cap when the sheet is missing: its highest row, and WorkshopOptimizerPlugin's constant.</summary>
    public const int DefaultGrooveCap=45;

    /// <summary>
    /// The groove's cap, from MJICraftworksTension by finished landmarks: the row is inferred, the wiki gives the same caps for 1 to 5 landmarks. An unknown count
    /// takes the highest cap, and the cap is never under the groove the game gives.
    /// </summary>
    public static int GrooveCap(IslandCatalog catalog,IslandSnapshot island)
    {
        var caps=catalog.GrooveCaps;
        var cap=caps is not { Length:>0 } ? DefaultGrooveCap:island.Landmarks is { } count && count>=0 && count<caps.Length ? caps[count]:caps.Max();
        return Math.Max(cap,(int)Math.Min(island.Groove,int.MaxValue));
    }
    public static int? Supply(byte value)=>value>>4 is var supply && supply<=4 ? supply:null;
    public static int? Demand(byte value)=>(value&0x0F) is var demand && demand<=4 ? demand:null;

    /// <summary>What the game's values allow. Zero everywhere means the game has not loaded them yet.</summary>
    public static IslandReadStatus Check(IslandCatalog catalog,int rank,int popularityRow,int nextRow,bool dirty,ReadOnlySpan<byte> supplyDemand)
    {
        if(!catalog.Valid || supplyDemand.Length!=catalog.Rows || rank>20)return IslandReadStatus.Unavailable;
        if(dirty)return IslandReadStatus.NotLoaded;
        var any=false;
        foreach(var craft in catalog.Crafts)any|=supplyDemand[(int)craft.Id]!=0;
        if(!any)return IslandReadStatus.NotLoaded;
        if(popularityRow>=catalog.Popularity.Count || nextRow>=catalog.Popularity.Count)return IslandReadStatus.Unavailable;
        foreach(var craft in catalog.Crafts)
            if(Supply(supplyDemand[(int)craft.Id]) is null || Demand(supplyDemand[(int)craft.Id]) is null)return IslandReadStatus.Unavailable;
        return IslandReadStatus.Loaded;
    }

    public static int? PopularityType(IslandCraft craft,IslandCatalog catalog,int row)=>row<catalog.Popularity.Count && craft.Id<catalog.Popularity[row].Length ? catalog.Popularity[row][craft.Id]:null;

    /// <summary>The percent of a supply band, moved along the bands by the units already made, between two bands in a straight line.</summary>
    public static double SupplyPercent(IslandCatalog catalog,int supply,int produced)
    {
        var position=Math.Min(supply+(double)Math.Max(produced,0)/UnitsPerSupplyBand,4);
        var lower=(int)Math.Floor(position);
        var upper=Math.Min(lower+1,4);
        return catalog.SupplyRatio[lower]+(catalog.SupplyRatio[upper]-catalog.SupplyRatio[lower])*(position-lower);
    }

    /// <summary>The estimated worth of one unit, or null when a multiplier is missing. Groove: the one at that moment of the cycle, or the one read.</summary>
    public static double? Unit(IslandCraft craft,IslandCatalog catalog,IslandSnapshot island,int workshopLevel,int produced=0,uint? groove=null)
    {
        if(craft.Id>=island.SupplyDemand.Length || Supply(island.SupplyDemand[craft.Id]) is not { } supply)return null;
        if(PopularityType(craft,catalog,island.PopularityRow) is not { } type || type>=catalog.PopularityRatio.Length || catalog.PopularityRatio[type]==0)return null;
        if(workshopLevel<=0 || workshopLevel>=catalog.RankRatio.Length)return null;
        return craft.Value*(catalog.RankRatio[workshopLevel]/100d)*(1+(groove ?? island.Groove)/100d)*(catalog.PopularityRatio[type]/100d)*(SupplyPercent(catalog,supply,produced)/100d);
    }

    public static int BestLevel(IslandSnapshot island)=>island.Workshops.Where(item=>!item.UnderConstruction).Select(item=>item.Level).DefaultIfEmpty(1).Max();

    /// <summary>The table: crafts allowed by the island's rank, the most valuable first, then by row.</summary>
    public static IslandRow[] Rows(IslandCatalog catalog,IslandSnapshot island)
    {
        var level=BestLevel(island);
        return catalog.Crafts.Where(craft=>craft.LevelRequired<=island.Rank)
            .Select(craft=>new IslandRow(craft,PopularityType(craft,catalog,island.PopularityRow),
                craft.Id<island.SupplyDemand.Length ? Supply(island.SupplyDemand[craft.Id]):null,
                craft.Id<island.SupplyDemand.Length ? Demand(island.SupplyDemand[craft.Id]):null,
                Unit(craft,catalog,island,level)))
            .OrderByDescending(row=>row.Value ?? -1).ThenBy(row=>row.Craft.Id).ToArray();
    }

    /// <summary>The cycles change at 08:00 UTC: a reading from before the last change no longer holds.</summary>
    public static bool Stale(DateTimeOffset readAt,DateTimeOffset now)
    {
        var change=new DateTimeOffset(now.UtcDateTime.Date.AddHours(8),TimeSpan.Zero);
        if(now<change)change=change.AddDays(-1);
        return readAt<change;
    }
}
