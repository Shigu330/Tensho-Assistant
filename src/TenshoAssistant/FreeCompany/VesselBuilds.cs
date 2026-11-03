namespace TenshoAssistant.FreeCompany;

/// <summary>The five voyage stats (SubmarineSpecCategory / AirshipExplorationParamType order).</summary>
public readonly record struct VesselStats(int Surveillance,int Retrieval,int Speed,int Range,int Favor)
{
    public static VesselStats operator +(VesselStats a,VesselStats b)
        =>new(a.Surveillance+b.Surveillance,a.Retrieval+b.Retrieval,a.Speed+b.Speed,a.Range+b.Range,a.Favor+b.Favor);
    public int this[int index]=>index switch{0=>Surveillance,1=>Retrieval,2=>Speed,3=>Range,_=>Favor};
}

/// <summary>
/// R-11 step 3 (0.315): one vessel part (SubmarinePart / AirshipExplorationPart row) with its item (Item.FilterGroup 36 / 28 and
/// Item.AdditionalData = part row, docs/evidence/0.315-vessel-builds.md). Slot: submarine 0 hull, 1 stern, 2 bow, 3 bridge; airship
/// 0 hull, 1 rigging, 2 forecastle, 3 aftcastle.
/// </summary>
public sealed record VesselPart(uint Row,VesselKind Kind,int Class,int Slot,int Rank,int Components,int Repair,VesselStats Stats,uint ItemId,string Name);

/// <summary>A rank row: capacity and (submarines only) the stat bonus the rank adds (SubmarineRank / AirshipExplorationLevel).</summary>
public sealed record VesselRank(int Rank,int Capacity,uint ExpToNext,VesselStats Bonus);

/// <summary>
/// Community breakpoints of a submarine sector (SubmarineTracker MapBreakpoints, MIT): surveillance for a chance at tier 2 / tier 3
/// loot, retrieval for normal / optimal quantities, favor for a chance at a second item.
/// </summary>
public sealed record SectorBreakpoint(uint Sector,int T2,int T3,int Normal,int Optimal,int Favor);

/// <summary>A planned or read build: rank and the four part rows (0 = unknown / empty), in slot order 0..3.</summary>
public sealed record VesselBuild(VesselKind Kind,int Rank,uint Slot0,uint Slot1,uint Slot2,uint Slot3)
{
    public uint this[int slot]=>slot switch{0=>Slot0,1=>Slot1,2=>Slot2,_=>Slot3};
    public VesselBuild With(int slot,uint row)=>slot switch{0=>this with{Slot0=row},1=>this with{Slot1=row},2=>this with{Slot2=row},_=>this with{Slot3=row}};
    public bool Complete=>Slot0!=0 && Slot1!=0 && Slot2!=0 && Slot3!=0;
    /// <summary>Configuration shape (plain list): rank, then the four part rows.</summary>
    public List<uint> ToList()=>[(uint)Rank,Slot0,Slot1,Slot2,Slot3];
    public static VesselBuild? FromList(VesselKind kind,IReadOnlyList<uint>? values)
        =>values is { Count:5 } ? new(kind,(int)values[0],values[1],values[2],values[3],values[4]):null;
}

/// <summary>What a build is worth, or why it cannot be built.</summary>
public sealed record BuildSummary(VesselStats Stats,int Cost,int Capacity,int Repair,int HighestPartRank,bool RankTooLow,bool OverCapacity,bool Complete);

public enum SurveillanceTier { Unknown, Low, Mid, High }
public enum RetrievalTier { Unknown, Poor, Normal, Optimal }

/// <summary>A candidate of the « best builds for this route » search.</summary>
public sealed record BuildCandidate(VesselBuild Build,BuildSummary Summary,int Seconds,int DamagePerVoyage);

/// <summary>Parts, ranks and breakpoints, immutable once built (VoyageWatcher builds it from the game sheets + embedded data).</summary>
public sealed class VesselPartCatalog
{
    public required IReadOnlyDictionary<(VesselKind Kind,uint Row),VesselPart> Parts { get; init; }
    public required IReadOnlyList<VesselRank> SubmarineRanks { get; init; }
    public required IReadOnlyList<VesselRank> AirshipRanks { get; init; }
    public required IReadOnlyDictionary<uint,SectorBreakpoint> Breakpoints { get; init; }
    /// <summary>Stat labels from the game (index 0..4), per kind.</summary>
    public required IReadOnlyList<string> SubmarineStatNames { get; init; }
    public required IReadOnlyList<string> AirshipStatNames { get; init; }

    public VesselPart? Find(VesselKind kind,uint row)=>row==0 ? null:Parts.GetValueOrDefault((kind,row));
    public IEnumerable<VesselPart> ForSlot(VesselKind kind,int slot)=>Parts.Values.Where(part=>part.Kind==kind && part.Slot==slot).OrderBy(part=>part.Rank).ThenBy(part=>part.Row);
    public IReadOnlyList<string> StatNames(VesselKind kind)=>kind==VesselKind.Submarine ? SubmarineStatNames:AirshipStatNames;
}

/// <summary>
/// Pure rules of the vessel build planner (tested). Game data: part stats, ranks, capacity (sheets). Community data, credited:
/// submarine breakpoints (SubmarineTracker, MIT), the durability estimate « (335 + sector rank − part rank) × 7 per sector, 30000 per
/// part » (community « Submersibles durability damage » sheet, quoted by SubmarineTracker), and the rule « total components ≤
/// capacity » used by the community tools.
/// </summary>
public static class VesselBuilds
{
    public const int MaxCondition=30000;

    /// <summary>The rank row in force: the row of that rank, else the last row with a capacity (no extrapolation).</summary>
    public static VesselRank? RankRow(IReadOnlyList<VesselRank> ranks,int rank)
    {
        var known=ranks.Where(row=>row.Capacity>0).ToArray();
        if(known.Length==0)return null;
        return known.FirstOrDefault(row=>row.Rank==rank) ?? (rank>known[^1].Rank ? known[^1]:known.LastOrDefault(row=>row.Rank<=rank));
    }

    public static int MaxRank(IReadOnlyList<VesselRank> ranks)=>ranks.Where(row=>row.Capacity>0).Select(row=>row.Rank).DefaultIfEmpty(1).Max();

    public static BuildSummary Summarize(VesselPartCatalog catalog,VesselBuild build)
    {
        var parts=Enumerable.Range(0,4).Select(slot=>catalog.Find(build.Kind,build[slot])).ToArray();
        var ranks=build.Kind==VesselKind.Submarine ? catalog.SubmarineRanks:catalog.AirshipRanks;
        var row=RankRow(ranks,build.Rank);
        // Airship ranks give no stat bonus (AirshipExplorationLevel has only experience and capacity).
        var stats=build.Kind==VesselKind.Submarine && row is not null ? row.Bonus:default;
        foreach(var part in parts)if(part is not null)stats+=part.Stats;
        var cost=parts.Sum(part=>part?.Components ?? 0);var capacity=row?.Capacity ?? 0;
        var highest=parts.Max(part=>part?.Rank ?? 0);
        return new(stats,cost,capacity,parts.Sum(part=>part?.Repair ?? 0),highest,highest>build.Rank,capacity>0 && cost>capacity,parts.All(part=>part is not null));
    }

    /// <summary>Route breakpoints: the highest value of each column over the route's sectors; null when one sector is unknown.</summary>
    public static SectorBreakpoint? RouteBreakpoint(IReadOnlyDictionary<uint,SectorBreakpoint> table,IReadOnlyCollection<uint> sectors)
    {
        if(sectors.Count==0)return null;
        var rows=new List<SectorBreakpoint>();
        foreach(var sector in sectors){if(!table.TryGetValue(sector,out var row))return null;rows.Add(row);}
        return new(0,rows.Max(row=>row.T2),rows.Max(row=>row.T3),rows.Max(row=>row.Normal),rows.Max(row=>row.Optimal),rows.Max(row=>row.Favor));
    }

    /// <summary>
    /// 0.334 (DEC-023, point 1): the 19 sectors where the embedded community value is below every observation of FFXIVGachaSpreadsheet
    /// (docs/evidence/0.315-vessel-builds.md, « Seuils de butin par secteur »): a tier shown as reached there is confirmed by no record.
    /// </summary>
    public static readonly IReadOnlySet<uint> UnconfirmedSectors=new HashSet<uint>{81,86,92,98,105,109,111,112,121,125,126,130,132,133,144,145,146,147,148};
    public static bool Unconfirmed(IEnumerable<uint> sectors)=>sectors.Any(UnconfirmedSectors.Contains);

    public static SurveillanceTier Surveillance(int value,SectorBreakpoint? breakpoint)
        =>breakpoint is null ? SurveillanceTier.Unknown:value>=breakpoint.T3 ? SurveillanceTier.High:value>=breakpoint.T2 ? SurveillanceTier.Mid:SurveillanceTier.Low;
    public static RetrievalTier Retrieval(int value,SectorBreakpoint? breakpoint)
        =>breakpoint is null ? RetrievalTier.Unknown:value>=breakpoint.Optimal ? RetrievalTier.Optimal:value>=breakpoint.Normal ? RetrievalTier.Normal:RetrievalTier.Poor;
    public static bool? FavorReached(int value,SectorBreakpoint? breakpoint)=>breakpoint is null ? null:value>=breakpoint.Favor;

    /// <summary>
    /// Community durability estimate for one voyage: per part, Σ over the sectors of (335 + sector RankReq − part Rank) × 7; the worst
    /// part counts. 0 when a part is missing.
    /// </summary>
    public static int DamagePerVoyage(VesselPartCatalog catalog,VesselBuild build,IEnumerable<int> sectorRanks)
    {
        var ranks=sectorRanks.ToArray();
        var worst=0;
        for(var slot=0;slot<4;slot++)
        {
            if(catalog.Find(build.Kind,build[slot]) is not { } part)return 0;
            worst=Math.Max(worst,ranks.Sum(rank=>Math.Max(0,(335+rank-part.Rank)*7)));
        }
        return worst;
    }

    /// <summary>Voyages from full condition until a part breaks (rounded up), null when the damage is unknown.</summary>
    public static int? VoyagesUntilRepair(int damage)=>damage<=0 ? null:(MaxCondition+damage-1)/damage;

    /// <summary>
    /// Short build name used by the community: one letter per part in the order hull, stern, bow, bridge (S Shark, U Unkiu, W Whale,
    /// C Coelacanth, Y Syldra), « + » after a modified part, or « ++ » at the end when all four are modified. Submarines only.
    /// </summary>
    public static string Identifier(VesselPartCatalog catalog,VesselBuild build)
    {
        if(build.Kind!=VesselKind.Submarine)return "";
        var parts=Enumerable.Range(0,4).Select(slot=>catalog.Find(build.Kind,build[slot])).ToArray();
        if(parts.Any(part=>part is null))return "?";
        static (char Letter,bool Modified) Of(int classId)=>classId switch
        {
            3=>('S',false),2=>('U',false),1=>('W',false),4=>('C',false),5=>('Y',false),
            8=>('S',true),7=>('U',true),6=>('W',true),9=>('C',true),10=>('Y',true),_=>('?',false),
        };
        var codes=parts.Select(part=>Of(part!.Class)).ToArray();
        return codes.All(code=>code.Modified) ? new string(codes.Select(code=>code.Letter).ToArray())+"++"
            :string.Concat(codes.Select(code=>code.Modified ? $"{code.Letter}+":code.Letter.ToString()));
    }

    /// <summary>
    /// Every submarine part combination the rank allows (part ranks, capacity) that reaches the route's range and the wanted tiers,
    /// fastest first (higher speed = shorter voyage), then the cheapest repair, then the lowest cost. secondsOf gives the voyage time
    /// for a speed (the route's order is fixed by the route planner).
    /// </summary>
    public static IReadOnlyList<BuildCandidate> BestBuilds(VesselPartCatalog catalog,int rank,int routeDistance,SectorBreakpoint? breakpoint,
        SurveillanceTier surveillance,RetrievalTier retrieval,bool favor,Func<int,int> secondsOf,IReadOnlyList<int> sectorRanks,int take,CancellationToken token=default)
    {
        // A route the rank cannot sail has no build (0.315 review).
        if(rank<RouteRank(sectorRanks))return [];
        var slots=Enumerable.Range(0,4).Select(slot=>catalog.ForSlot(VesselKind.Submarine,slot).Where(part=>part.Rank<=rank).ToArray()).ToArray();
        var found=new List<BuildCandidate>();
        foreach(var hull in slots[0])
        foreach(var stern in slots[1])
        foreach(var bow in slots[2])
        foreach(var bridge in slots[3])
        {
            token.ThrowIfCancellationRequested();
            var build=new VesselBuild(VesselKind.Submarine,rank,hull.Row,stern.Row,bow.Row,bridge.Row);
            var summary=Summarize(catalog,build);
            if(summary.RankTooLow || summary.OverCapacity || summary.Stats.Range<routeDistance)continue;
            if(surveillance!=SurveillanceTier.Unknown && Surveillance(summary.Stats.Surveillance,breakpoint)<surveillance)continue;
            if(retrieval!=RetrievalTier.Unknown && Retrieval(summary.Stats.Retrieval,breakpoint)<retrieval)continue;
            if(favor && FavorReached(summary.Stats.Favor,breakpoint)!=true)continue;
            found.Add(new(build,summary,secondsOf(Math.Max(1,summary.Stats.Speed)),DamagePerVoyage(catalog,build,sectorRanks)));
        }
        return found.OrderBy(candidate=>candidate.Seconds).ThenBy(candidate=>candidate.Summary.Repair).ThenBy(candidate=>candidate.Summary.Cost)
            .ThenByDescending(candidate=>candidate.Summary.Stats.Surveillance+candidate.Summary.Stats.Retrieval+candidate.Summary.Stats.Favor).Take(take).ToArray();
    }

    /// <summary>Rank a submarine needs for a route: the highest RankReq of its sectors (0 when empty).</summary>
    public static int RouteRank(IEnumerable<int> sectorRanks)=>sectorRanks.DefaultIfEmpty(0).Max();

    /// <summary>A saved plan never stays below the vessel's read rank (0.315 review: the rank bonus follows the vessel as it levels).</summary>
    public static VesselBuild? EffectivePlan(VesselBuild? saved,int readRank)=>saved is not null && saved.Rank<readRank ? saved with{Rank=readRank}:saved;

    /// <summary>Reads the embedded breakpoint table (CSV « sector,T2,T3,Normal,Optimal,Favor », « # » lines are comments).</summary>
    public static IReadOnlyDictionary<uint,SectorBreakpoint> ReadBreakpoints(TextReader reader)
    {
        var table=new Dictionary<uint,SectorBreakpoint>();
        string? line;
        while((line=reader.ReadLine()) is not null)
        {
            if(line.Length==0 || line.StartsWith('#'))continue;
            var cells=line.Split(',');
            if(cells.Length<6 || !uint.TryParse(cells[0],out var sector))continue;
            var values=cells.Skip(1).Take(5).Select(cell=>int.TryParse(cell,out var value) ? value:-1).ToArray();
            if(values.Any(value=>value<0))continue;
            table[sector]=new(sector,values[0],values[1],values[2],values[3],values[4]);
        }
        return table;
    }
}
