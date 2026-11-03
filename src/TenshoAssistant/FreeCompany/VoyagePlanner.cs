using System.Numerics;
using System.Text.Json;

namespace TenshoAssistant.FreeCompany;

/// <summary>A submarine exploration sector (SubmarineExploration row), copied from the game data.</summary>
public sealed record SubmarineSector(uint Id,uint Map,bool Start,int RankReq,Vector3 Position,int SurveyDistance,int SurveyMinutes,uint Exp,string Name);

/// <summary>Community loot of one sector: number of recorded visits and total quantity seen per item; its pools by tier when known.</summary>
public sealed record SectorLoot(uint SectorId,int Records,IReadOnlyList<(uint ItemId,long Total)> Items,IReadOnlyList<SectorTier>? Tiers=null);

/// <summary>
/// 0.335 (DEC-023, point 2): one loot pool of a sector (tier 1 to 3): its records, the total seen per item, and how many of its records
/// were made with a surveillance below tier 2 (Low), between tiers 2 and 3 (Mid) and at tier 3 (High) — FFXIVGachaSpreadsheet « Stats ».
/// </summary>
public sealed record SectorTier(int Records,int Low,int Mid,int High,IReadOnlyList<(uint ItemId,long Total)> Items);

public enum RouteGoal { ValuePerVoyage, ValuePerDay, Experience }

/// <summary>Surveillance band of the records the value is taken from: all records, or those below tier 2, between tiers 2 and 3, at tier 3.</summary>
public enum SurveillanceBand { All, Low, Mid, High }

/// <summary>One route found by the planner: sectors in travel order, distance used, voyage time (12 h included), expected value.</summary>
public sealed record PlannedRoute(IReadOnlyList<uint> Sectors,int Distance,int Seconds,double Value,uint Exp)
{
    public double ValuePerDay=>Seconds<=0 ? 0:Value*86400/Seconds;
}

/// <summary>Sectors, maps and the embedded community loot, ready for the planner (immutable once built).</summary>
public sealed class SubmarineCatalog
{
    public required IReadOnlyDictionary<uint,SubmarineSector> Sectors { get; init; }
    public required IReadOnlyList<(uint Id,string Name)> Maps { get; init; }
    public required IReadOnlyDictionary<uint,SectorLoot> Loot { get; init; }
    /// <summary>Gil per unit when sold to an NPC (Item.PriceLow), for the loot items.</summary>
    public required IReadOnlyDictionary<uint,uint> SellPrices { get; init; }
    /// <summary>0.335: loot items sold on the market board (tradable, with a search category), from the game data.</summary>
    public IReadOnlySet<uint> Marketable { get; init; }=new HashSet<uint>();
    public required string LootSource { get; init; }
    public required string LootBuilt { get; init; }

    public IEnumerable<SubmarineSector> SectorsOf(uint map)=>Sectors.Values.Where(sector=>sector.Map==map).OrderBy(sector=>sector.Id);
}

/// <summary>
/// R-07 (0.309, user request: best routes, loot first): original code. Voyage distance and time follow the community formulas
/// (documented in docs/evidence/0.309-submarine-routes.md): a leg counts floor(length × 0.035) distance and
/// floor(length × 3990 / (speed × 100) × 60) seconds, each sector adds its SurveyDistance and floor(SurveyDurationmin × 7000 /
/// (speed × 100) × 60) seconds, and every voyage adds 12 h. The expected value of a sector is the community average quantity of
/// each item per visit times its value (NPC sell price by default), all surveillance tiers together. Loot averages: FFXIVGachaSpreadsheet
/// (MIT, crowd-sourced, embedded as FreeCompany/Data/submarine-loot.json by tools/submarine-loot.py); the formulas were understood by
/// reading SubmarineTracker (MIT).
/// </summary>
public static class VoyagePlanner
{
    public const int FixedSeconds=43200;
    public const int MaxSectors=5;

    public static int LegDistance(SubmarineSector from,SubmarineSector to)=>(int)Math.Floor(Vector3.Distance(from.Position,to.Position)*0.035);
    public static int LegSeconds(SubmarineSector from,SubmarineSector to,int speed)
        =>(int)Math.Floor(Vector3.Distance(from.Position,to.Position)*3990/(Math.Max(1,speed)*100.0)*60);
    public static int SurveySeconds(SubmarineSector sector,int speed)=>(int)Math.Floor(sector.SurveyMinutes*7000/(Math.Max(1,speed)*100.0)*60);

    /// <summary>Distance and time of a voyage visiting the sectors in this order.</summary>
    public static (int Distance,int Seconds) Measure(SubmarineSector start,IReadOnlyList<SubmarineSector> order,int speed)
    {
        var distance=0;var seconds=FixedSeconds;var previous=start;
        foreach(var sector in order)
        {
            distance+=LegDistance(previous,sector)+sector.SurveyDistance;
            seconds+=LegSeconds(previous,sector,speed)+SurveySeconds(sector,speed);
            previous=sector;
        }
        return (distance,seconds);
    }

    /// <summary>Visiting order with the shortest distance (then the shortest time).</summary>
    public static (int Distance,int Seconds,SubmarineSector[] Order) BestOrder(SubmarineSector start,IReadOnlyList<SubmarineSector> sectors,int speed)
    {
        var best=(Distance:int.MaxValue,Seconds:int.MaxValue,Order:Array.Empty<SubmarineSector>());
        foreach(var order in Permutations(sectors.ToArray(),0))
        {
            var (distance,seconds)=Measure(start,order,speed);
            if(distance<best.Distance || distance==best.Distance && seconds<best.Seconds)best=(distance,seconds,order.ToArray());
        }
        return best;
    }

    private static IEnumerable<SubmarineSector[]> Permutations(SubmarineSector[] items,int from)
    {
        if(from>=items.Length-1){yield return items;yield break;}
        for(var index=from;index<items.Length;index++)
        {
            (items[from],items[index])=(items[index],items[from]);
            foreach(var permutation in Permutations(items,from+1))yield return permutation;
            (items[from],items[index])=(items[index],items[from]);
        }
    }

    /// <summary>Average gil a visit of the sector brings (community quantities per visit × value per unit).</summary>
    public static double ExpectedValue(SectorLoot? loot,Func<uint,double> value)
        =>loot is null || loot.Records<=0 ? 0:loot.Items.Sum(item=>item.Total/(double)loot.Records*value(item.ItemId));

    /// <summary>Average quantity of each item per visit of the sector.</summary>
    public static IEnumerable<(uint ItemId,double PerVisit)> PerVisit(SectorLoot? loot)
        =>loot is null || loot.Records<=0 ? []:loot.Items.Select(item=>(item.ItemId,item.Total/(double)loot.Records));

    /// <summary>
    /// 0.335 (DEC-023, point 2): average quantity of each item per visit, from the records of one surveillance band only: each pool weighs
    /// as much as its share of that band's records, and gives its own average per record. Null when no record was made in that band
    /// (unknown, never zero). All = every record together, as <see cref="PerVisit(SectorLoot?)"/>.
    /// </summary>
    public static IReadOnlyList<(uint ItemId,double PerVisit)>? PerVisit(SectorLoot? loot,SurveillanceBand band)
    {
        if(loot is null || loot.Records<=0)return null;
        if(band==SurveillanceBand.All)return PerVisit(loot).ToArray();
        if(loot.Tiers is not { Count:>0 } tiers)return null;
        double Count(SectorTier tier)=>band switch{SurveillanceBand.Low=>tier.Low,SurveillanceBand.Mid=>tier.Mid,_=>tier.High};
        var total=tiers.Sum(Count);
        if(total<=0)return null;
        return tiers.Where(tier=>tier.Records>0 && Count(tier)>0)
            .SelectMany(tier=>tier.Items.Select(item=>(item.ItemId,PerVisit:Count(tier)/total*item.Total/tier.Records)))
            .GroupBy(entry=>entry.ItemId).Select(group=>(group.Key,group.Sum(entry=>entry.PerVisit))).ToArray();
    }

    /// <summary>Average gil a visit brings from the records of one surveillance band; null when that band has no record.</summary>
    public static double? ExpectedValue(SectorLoot? loot,Func<uint,double> value,SurveillanceBand band)
        =>PerVisit(loot,band) is { } items ? items.Sum(item=>item.PerVisit*value(item.ItemId)):null;

    /// <summary>
    /// 0.335 (DEC-023, point 2): value of one unit when the market is taken into account: the market's lowest NQ price when it is known and
    /// higher than the NPC price (the item is better sold there), else the NPC price. A marketable item without a known price stays at
    /// the NPC price and is reported missing, so a value is never invented.
    /// </summary>
    public static (double Value,bool Missing) UnitValue(uint npcPrice,bool marketable,decimal? marketMinimum)
    {
        if(!marketable)return (npcPrice,false);
        if(marketMinimum is not { } market || market<=0)return (npcPrice,true);
        return ((double)Math.Max(market,npcPrice),false);
    }

    /// <summary>
    /// Every set of 1 to 5 sectors of one map the vessel may reach (rank, range), each in its best order, sorted by the goal.
    /// A set over the range is not extended (distance only grows; a small slack covers the per-leg rounding).
    /// </summary>
    public static IReadOnlyList<PlannedRoute> Plan(IEnumerable<SubmarineSector> mapSectors,int rank,int speed,int range,RouteGoal goal,
        Func<uint,double> sectorValue,int take,CancellationToken token=default,ISet<uint>? excluded=null)
    {
        var all=mapSectors.ToArray();
        var start=all.FirstOrDefault(sector=>sector.Start);
        if(start is null)return [];
        var candidates=all.Where(sector=>!sector.Start && sector.Exp>0 && sector.RankReq<=rank && excluded?.Contains(sector.Id)!=true).ToArray();
        var values=candidates.ToDictionary(sector=>sector.Id,sector=>sectorValue(sector.Id));
        var found=new List<PlannedRoute>();
        var current=new List<SubmarineSector>();
        void Extend(int from)
        {
            for(var index=from;index<candidates.Length;index++)
            {
                token.ThrowIfCancellationRequested();
                current.Add(candidates[index]);
                var (distance,seconds,order)=BestOrder(start,current,speed);
                if(distance<=range)
                    found.Add(new(order.Select(sector=>sector.Id).ToArray(),distance,seconds,current.Sum(sector=>values[sector.Id]),(uint)current.Sum(sector=>sector.Exp)));
                if(current.Count<MaxSectors && distance<=range+5)Extend(index+1);
                current.RemoveAt(current.Count-1);
            }
        }
        Extend(0);
        IOrderedEnumerable<PlannedRoute> sorted=goal switch
        {
            RouteGoal.ValuePerDay=>found.OrderByDescending(route=>route.ValuePerDay),
            RouteGoal.Experience=>found.OrderByDescending(route=>route.Exp*86400.0/route.Seconds),
            _=>found.OrderByDescending(route=>route.Value),
        };
        return sorted.ThenBy(route=>route.Seconds).Take(take).ToArray();
    }

    /// <summary>Reads the embedded community loot (tools/submarine-loot.py output).</summary>
    public static (IReadOnlyDictionary<uint,SectorLoot> Loot,string Source,string Built) ReadLoot(Stream stream)
    {
        using var document=JsonDocument.Parse(stream);
        var root=document.RootElement;
        var loot=new Dictionary<uint,SectorLoot>();
        foreach(var sector in root.GetProperty("Sectors").EnumerateArray())
        {
            var items=sector.GetProperty("Items").EnumerateArray().Select(pair=>(pair[0].GetUInt32(),pair[1].GetInt64())).ToArray();
            var id=sector.GetProperty("Id").GetUInt32();
            // 0.335: pools by tier, [records, low, mid, high, [[item, total], ...]] (absent from files built before 0.335).
            SectorTier[]? tiers=sector.TryGetProperty("Tiers",out var tierList)
                ? tierList.EnumerateArray().Select(tier=>new SectorTier(tier[0].GetInt32(),tier[1].GetInt32(),tier[2].GetInt32(),tier[3].GetInt32(),
                    tier[4].EnumerateArray().Select(pair=>(pair[0].GetUInt32(),pair[1].GetInt64())).ToArray())).ToArray()
                :null;
            loot[id]=new(id,sector.GetProperty("Records").GetInt32(),items,tiers);
        }
        return (loot,root.GetProperty("Source").GetString() ?? "",root.GetProperty("Built").GetString() ?? "");
    }
}
