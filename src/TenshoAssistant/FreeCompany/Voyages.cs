namespace TenshoAssistant.FreeCompany;

public enum VesselKind { Submarine, Airship }

public enum VoyageState { Idle, Voyaging, Returned }

/// <summary>One item brought back by the last voyage (primary or additional find at one point).</summary>
public sealed record VoyageLoot(uint ItemId,int Count);

/// <summary>
/// R-07 (0.307): a free company vessel as last read in the company workshop (HousingManager.WorkshopTerritory). Kept in the
/// configuration, since the game only gives these values inside the workshop. Route names are copied in the client language at
/// read time (SubmarineExploration.Destination); the airship struct gives no route.
/// </summary>
public sealed record VoyageVessel
{
    public ulong HouseId { get; init; }
    public VesselKind Kind { get; init; }
    public int Slot { get; init; }
    public string Name { get; init; } = "";
    public int Rank { get; init; }
    public uint Exp { get; init; }
    public uint NextExp { get; init; }
    public DateTimeOffset? RegisteredAt { get; init; }
    public DateTimeOffset? ReturnAt { get; init; }
    public List<string> Route { get; init; } = [];
    public List<VoyageLoot> Loot { get; init; } = [];
    // 0.309: sector ids of the route and the submarine's stats (parts + rank bonus), for the route planner.
    public List<uint> Points { get; init; } = [];
    public int Surveillance { get; init; }
    public int Retrieval { get; init; }
    public int Speed { get; init; }
    public int Range { get; init; }
    public int Favor { get; init; }
    // 0.315 (R-11): the four part rows in sheet slot order (submarine: hull, stern, bow, bridge; airship: hull, rigging, forecastle,
    // aftcastle); empty or 0 = not read yet (vessels saved before 0.315).
    public List<uint> Parts { get; init; } = [];
    public DateTimeOffset ReadAt { get; init; }
    // T26 (0.919): airships only: the sector names of the last voyage read in the game's voyage log (client language at read time).
    public List<string> LastRoute { get; init; } = [];
}

/// <summary>Pure rules of the free company voyages page (tested).</summary>
public static class Voyages
{
    public static VoyageState StateOf(VoyageVessel vessel,DateTimeOffset now)
        =>vessel.ReturnAt is not { } at ? VoyageState.Idle:at>now ? VoyageState.Voyaging:VoyageState.Returned;

    /// <summary>Share of the experience to the next rank, 0 to 1 (1 when the game gives no next step).</summary>
    public static double ExpProgress(VoyageVessel vessel)
        =>vessel.NextExp==0 ? 1:Math.Clamp(vessel.Exp/(double)vessel.NextExp,0,1);

    /// <summary>A workshop read replaces the vessels of that house; the other houses' vessels are kept. Sorted by house, kind, slot.</summary>
    public static List<VoyageVessel> Merge(IEnumerable<VoyageVessel>? saved,ulong houseId,IEnumerable<VoyageVessel> read)
        =>(saved ?? []).Where(vessel=>vessel.HouseId!=houseId).Concat(read)
            .OrderBy(vessel=>vessel.HouseId).ThenBy(vessel=>vessel.Kind).ThenBy(vessel=>vessel.Slot).ToList();

    /// <summary>Same values, the read time aside (during a workshop visit, a new read that changes nothing is not saved again; 0.331: the first read of a visit is).</summary>
    public static bool SameAs(IReadOnlyList<VoyageVessel> left,IReadOnlyList<VoyageVessel> right)
        =>left.Count==right.Count && left.Zip(right).All(pair=>Same(pair.First,pair.Second));

    private static bool Same(VoyageVessel a,VoyageVessel b)
        =>a.HouseId==b.HouseId && a.Kind==b.Kind && a.Slot==b.Slot && a.Name==b.Name && a.Rank==b.Rank && a.Exp==b.Exp && a.NextExp==b.NextExp
            && a.RegisteredAt==b.RegisteredAt && a.ReturnAt==b.ReturnAt && a.Route.SequenceEqual(b.Route) && a.Loot.SequenceEqual(b.Loot)
            && a.Points.SequenceEqual(b.Points) && a.Surveillance==b.Surveillance && a.Retrieval==b.Retrieval && a.Speed==b.Speed && a.Range==b.Range && a.Favor==b.Favor
            && a.Parts.SequenceEqual(b.Parts) && a.LastRoute.SequenceEqual(b.LastRoute);

    /// <summary>
    /// 0.315 (review): unlocked sectors belong to one company. A selected vessel uses its own house's list (null = not read yet:
    /// no filter); manual input uses the list only when a single company was read.
    /// </summary>
    public static IReadOnlyCollection<uint>? UnlockedFor(IReadOnlyDictionary<string,List<uint>>? known,ulong? houseId)
    {
        if(known is null || known.Count==0)return null;
        if(houseId is { } house)return known.TryGetValue(house.ToString(System.Globalization.CultureInfo.InvariantCulture),out var list) && list.Count>0 ? list:null;
        return known.Count==1 && known.Values.First().Count>0 ? known.Values.First():null;
    }

    /// <summary>
    /// 0.331 (audit): a vessel plan key of that company (« sub:&lt;house&gt;:&lt;slot&gt; » or « air:… », as the Builds tab writes them;
    /// the manual plans « sub:manual », « air:manual » belong to no company). Used to forget a company the player left.
    /// </summary>
    public static bool IsPlanOf(string planKey,ulong houseId)
    {
        var house=houseId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return planKey.StartsWith($"sub:{house}:",StringComparison.Ordinal) || planKey.StartsWith($"air:{house}:",StringComparison.Ordinal);
    }

    /// <summary>0.603 (T23): the vessels by workshop, the most recently read first.</summary>
    public static IReadOnlyList<IReadOnlyList<VoyageVessel>> Workshops(IEnumerable<VoyageVessel> vessels)
        =>vessels.GroupBy(vessel=>vessel.HouseId).OrderByDescending(group=>group.Max(vessel=>vessel.ReadAt)).Select(group=>(IReadOnlyList<VoyageVessel>)group.ToArray()).ToArray();
    /// <summary>Next return still to come, over all vessels (null when none is out).</summary>
    public static DateTimeOffset? NextReturn(IEnumerable<VoyageVessel>? vessels,DateTimeOffset now)
        =>(vessels ?? []).Select(vessel=>vessel.ReturnAt).Where(at=>at>now).Min();
}
