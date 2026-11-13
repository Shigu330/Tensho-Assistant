namespace TenshoAssistant.FreeCompany;

/// <summary>
/// Leg times and distances of airship voyages for one speed, as the game computes them (HousingManager.GetAirshipVoyageTimeAndDistance
/// and GetAirshipSurveyDuration, copied on the framework thread). Keys are the game's point numbers (sectors 0–24, start 127).
/// </summary>
public sealed record AirshipLegTable(int Speed,IReadOnlyDictionary<(uint From,uint To),(uint Time,uint Distance)> Legs,IReadOnlyDictionary<uint,uint> Survey);

/// <summary>
/// One airship route: sectors in travel order, distance used, voyage time, experience and ceruleum tanks. UnknownExp counts the sectors
/// whose experience the game data gives as 0 (not known): Exp is then a lower bound (0.337 review).
/// </summary>
public sealed record AirshipRoute(IReadOnlyList<uint> Sectors,int Distance,int Seconds,uint Exp,int Tanks,int UnknownExp=0)
{
    public double ExpPerDay=>Seconds<=0 ? 0:Exp*86400.0/Seconds;
}

/// <summary>X-201 (0.1243, user request of 2026-11-12): Loot and Unlock rank the routes that visit a target (a sector where the chosen item may
/// drop, the sector that unlocks the chosen one), shortest first: the target is the point, the experience a bonus.</summary>
public enum AirshipGoal { ExpPerDay, ExpPerVoyage, Shortest, Loot, Unlock }

/// <summary>
/// DEC-023, point 3 (0.337): airship routes by distance and duration; the loot stays unknown (no public quantity, docs/evidence/0.336-airship-sectors.md).
/// Leg times and distances come from the game's own functions (seconds assumed, as for the submarine function the IDA notes give in
/// seconds; not confirmed in game). Assembled like a submarine voyage: legs from the start through each sector in order, plus each
/// sector's survey time and SurveyDistance, plus a fixed 12 h, no return leg. The 12 h and the missing return leg are the community's
/// reading (Square Enix forum « Airship Guide », 2015, and SubmarineTracker's submarine model, MIT): not confirmed in game either.
/// </summary>
public static class AirshipPlanner
{
    public const uint Start=127;
    public const int MaxSectors=5;
    public const int FixedSeconds=43200;

    /// <summary>Distance and time of a voyage visiting the sectors in this order; null when the table lacks a leg or a survey time.</summary>
    public static (int Distance,int Seconds)? Measure(AirshipLegTable table,IReadOnlyList<AirshipSector> order)
    {
        long distance=0,seconds=FixedSeconds;var previous=Start;
        foreach(var sector in order)
        {
            if(!table.Legs.TryGetValue((previous,sector.Id),out var leg) || !table.Survey.TryGetValue(sector.Id,out var survey))return null;
            distance+=leg.Distance+(long)sector.SurveyDistance;
            seconds+=leg.Time+(long)survey;
            previous=sector.Id;
        }
        if(distance>int.MaxValue || seconds>int.MaxValue)return null;
        return ((int)distance,(int)seconds);
    }

    /// <summary>Visiting order with the shortest distance (then the shortest time); null when no order can be measured.</summary>
    public static AirshipRoute? Best(AirshipLegTable table,IReadOnlyList<AirshipSector> sectors)
    {
        if(sectors.Count is 0 or > MaxSectors)return null;
        (int Distance,int Seconds,AirshipSector[] Order)? best=null;
        foreach(var order in Permutations(sectors.ToArray(),0))
            if(Measure(table,order) is { } measured
                && (best is not { } current || measured.Distance<current.Distance || measured.Distance==current.Distance && measured.Seconds<current.Seconds))
                best=(measured.Distance,measured.Seconds,order.ToArray());
        return best is { } found
            ? new(found.Order.Select(sector=>sector.Id).ToArray(),found.Distance,found.Seconds,(uint)found.Order.Sum(sector=>(long)sector.Exp),found.Order.Sum(sector=>sector.CeruleumTanks),
                found.Order.Count(sector=>sector.Exp==0))
            :null;
    }

    /// <summary>
    /// Every set of 1 to 5 sectors open at this rank, in its best order, within the range, sorted by the goal (take the first ones).
    /// The experience goals leave out routes with a sector of unknown experience (never ranked as if it gave 0); Shortest keeps them.
    /// X-201 (0.1243): with targets, only the routes visiting at least one of them are kept (Loot, Unlock); none given: no filter.
    /// </summary>
    public static IReadOnlyList<AirshipRoute> Plan(IEnumerable<AirshipSector> sectors,AirshipLegTable table,int rank,int range,AirshipGoal goal,int take,
        CancellationToken token=default,IReadOnlyCollection<uint>? targets=null)
    {
        var candidates=sectors.Where(sector=>sector.RankReq<=rank).OrderBy(sector=>sector.Id).ToArray();
        var found=new List<AirshipRoute>();
        var current=new List<AirshipSector>();
        void Extend(int from)
        {
            for(var index=from;index<candidates.Length;index++)
            {
                token.ThrowIfCancellationRequested();
                current.Add(candidates[index]);
                var route=Best(table,current);
                if(route is not null && route.Distance<=range)found.Add(route);
                // Distance only grows with one more sector: a set over the range is not extended (a small slack covers the per-leg rounding).
                if(current.Count<MaxSectors && route is not null && route.Distance<=range+5)Extend(index+1);
                current.RemoveAt(current.Count-1);
            }
        }
        Extend(0);
        if(targets is not null)found.RemoveAll(route=>!route.Sectors.Any(targets.Contains));
        if(goal is AirshipGoal.ExpPerDay or AirshipGoal.ExpPerVoyage)found.RemoveAll(route=>route.UnknownExp>0);
        IOrderedEnumerable<AirshipRoute> sorted=goal switch
        {
            AirshipGoal.ExpPerVoyage=>found.OrderByDescending(route=>route.Exp),
            AirshipGoal.Shortest or AirshipGoal.Loot or AirshipGoal.Unlock=>found.OrderBy(route=>route.Seconds).ThenByDescending(route=>route.Exp),
            _=>found.OrderByDescending(route=>route.ExpPerDay),
        };
        return sorted.ThenBy(route=>route.Seconds).Take(take).ToArray();
    }

    private static IEnumerable<AirshipSector[]> Permutations(AirshipSector[] items,int from)
    {
        if(from>=items.Length-1){yield return items;yield break;}
        for(var index=from;index<items.Length;index++)
        {
            (items[from],items[index])=(items[index],items[from]);
            foreach(var permutation in Permutations(items,from+1))yield return permutation;
            (items[from],items[index])=(items[index],items[from]);
        }
    }
}

/// <summary>
/// X-201 (0.1248, slice 2): the leg tables kept by speed, so each registered airship gets its own best route. The game computes one speed at
/// a time on the framework thread (a few hundred calls of its static voyage functions); the page's own speed first, then the others, one
/// per framework update, at most MaxSpeeds kept (the ones still wanted first). Pure: the native side (AirshipVoyageTable) only calls these.
/// </summary>
public static class AirshipSpeeds
{
    public const int MaxSpeeds=8;

    /// <summary>The speeds asked for, clamped as the table does, without repeats, in order (at most MaxSpeeds).</summary>
    public static int[] Wanted(IEnumerable<int> speeds)=>[.. speeds.Select(speed=>Math.Clamp(speed,1,short.MaxValue)).Distinct().Order().Take(MaxSpeeds)];

    /// <summary>The next speed to compute: the main one when it is not there, else the first other one missing; null when all are there.</summary>
    public static int? Next(int main,IReadOnlyList<int> others,IReadOnlyDictionary<int,AirshipLegTable> computed)
        => main>0 && !computed.ContainsKey(main) ? main : others.Cast<int?>().FirstOrDefault(speed=>!computed.ContainsKey(speed!.Value));

    /// <summary>The tables kept once <paramref name="table"/> is added: the main and other wanted speeds first, then the rest, MaxSpeeds at most
    /// (a new dictionary: the old one may still be read by the page).</summary>
    public static IReadOnlyDictionary<int,AirshipLegTable> Keep(IReadOnlyDictionary<int,AirshipLegTable> computed,int main,IReadOnlyList<int> others,AirshipLegTable table)
    {
        var all=new Dictionary<int,AirshipLegTable>(computed){[table.Speed]=table};
        return all.OrderBy(pair=>pair.Key==main || pair.Key==table.Speed ? 0 : others.Contains(pair.Key) ? 1 : 2).Take(MaxSpeeds).ToDictionary(pair=>pair.Key,pair=>pair.Value);
    }
}
