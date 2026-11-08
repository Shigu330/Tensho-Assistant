namespace TenshoAssistant.Gathering;

/// <summary>Moment of the day at an ocean fishing stop (IKDTimeDefine row: 1 night, 2 day, 3 sunset).</summary>
public enum OceanTime { Unknown, Night, Day, Sunset }

/// <summary>One stop of a voyage: its place and the moment of the day there.</summary>
public sealed record OceanStop(string Place,OceanTime Time);

/// <summary>An ocean fishing voyage (IKDRoute): its game name, its three stops, and the moment it is named after (the last stop's).</summary>
public sealed record OceanRoute(uint Id,string Name,IReadOnlyList<OceanStop> Stops)
{
    public OceanTime Label=>Stops.Count>0 ? Stops[^1].Time:OceanTime.Unknown;
}

/// <summary>A departure: when boarding opens (15 minutes, then the boat leaves) and the route from each port (Indigo / Ruby).</summary>
public sealed record OceanDeparture(DateTimeOffset At,OceanRoute? Aldenard,OceanRoute? Othard);

/// <summary>The two ports: Limsa Lominsa (Indigo, IKDRouteTable.Route) and Kugane (Ruby, IKDRouteTable.Unknown0).</summary>
public enum OceanPort { Aldenard, Othard }

/// <summary>
/// X-85 V1 (0.917): a departure seen aboard: its loop slot (counted from <see cref="OceanSchedule.LoopStart"/>) and the route the game
/// said (InstanceContentOceanFishing.CurrentRoute). Saved data: only these sightings move the timetable of a port.
/// </summary>
public sealed class OceanSighting
{
    public long Slot { get; set; }
    public uint Route { get; set; }
}

/// <summary>How far the timetable of a port is proven: no sighting or not settled yet, settled by sightings, or contradicted by them.</summary>
public enum OceanCheck { Unverified, Verified, Contradicted }

/// <summary>
/// A port's state: the loop shift in use (0 = the community alignment, -1 = unknown), the shifts the sightings still allow and how many
/// sightings of that port there are.
/// </summary>
public sealed record OceanPortState(OceanCheck Check,int Offset,IReadOnlyList<int> Candidates,int Sightings);

/// <summary>
/// g, R-01 (0.392): the ocean fishing timetable. The loop of departures is the game's IKDRouteTable (144 rows: Route = the voyage from
/// Limsa Lominsa, Unknown0 = the voyage from Kugane), one departure every 2 hours from the loop start 748 800 000 ms after 1970-01-01 UTC
/// (1970-01-09 16:00). The start, the 2-hour step and the meaning of the moment rows are facts read in GatherBuddy (Apache-2.0,
/// Plugin/OceanUptime.cs and GameData.cs, read only; original code), docs/evidence/0.392-ocean-fishing.md.
/// </summary>
/// <remarks>
/// X-85 V1 (0.917): on 2026-11-08 the game said route 14 from Kugane where this alignment gives 15 (log of the user's voyage), so the
/// alignment of each port is no longer taken on trust: <see cref="State"/> keeps the loop shifts that fit every departure seen aboard,
/// a port is « verified » only when one shift is left, and a contradicted port shows the routes still possible instead of a wrong one.
/// </remarks>
public sealed class OceanSchedule(IReadOnlyList<(uint Aldenard,uint Othard)> loop,IReadOnlyDictionary<uint,OceanRoute> routes)
{
    public static readonly DateTimeOffset LoopStart=DateTimeOffset.FromUnixTimeMilliseconds(748_800_000);
    public static readonly TimeSpan Step=TimeSpan.FromHours(2),Boarding=TimeSpan.FromMinutes(15);

    /// <summary>Loop shift of each port (row = slot + shift); -1 = unknown, that port's route is then left out of <see cref="At"/>.</summary>
    public int AldenardOffset { get; init; }
    public int OthardOffset { get; init; }
    public int Count=>loop.Count;

    /// <summary>The loop slot of the departure a player is aboard (or boarding) at <paramref name="aboard"/>.</summary>
    public static long SlotOf(DateTimeOffset aboard)=>Math.Max(0,(long)Math.Floor((aboard+Boarding-LoopStart)/Step));

    public OceanSchedule WithOffsets(int aldenard,int othard)=>new(loop,routes){AldenardOffset=aldenard,OthardOffset=othard};

    private uint Column(OceanPort port,long row)
    {
        var at=loop[(int)(((row%loop.Count)+loop.Count)%loop.Count)];
        return port==OceanPort.Aldenard ? at.Aldenard:at.Othard;
    }

    private OceanRoute? RouteAt(OceanPort port,long index,int offset)=>offset<0 ? null:routes.GetValueOrDefault(Column(port,index+offset));

    /// <summary>The port a route leaves from, read in the loop; null when the route is in both columns or in none.</summary>
    public OceanPort? PortOf(uint route)
    {
        var aldenard=loop.Any(row=>row.Aldenard==route);var othard=loop.Any(row=>row.Othard==route);
        return aldenard==othard ? null:aldenard ? OceanPort.Aldenard:OceanPort.Othard;
    }

    /// <summary>The shifts that fit every sighting of <paramref name="port"/>, and what they prove (see the remarks of the class).</summary>
    public OceanPortState State(OceanPort port,IEnumerable<OceanSighting>? sightings)
    {
        if(loop.Count==0)return new(OceanCheck.Unverified,0,[],0);
        var own=(sightings ?? []).Where(sighting=>sighting.Slot>=0 && PortOf(sighting.Route)==port).ToArray();
        var candidates=Enumerable.Range(0,loop.Count).Where(shift=>own.All(sighting=>Column(port,sighting.Slot+shift)==sighting.Route)).ToArray();
        if(own.Length==0)return new(OceanCheck.Unverified,0,candidates,0);
        if(candidates.Length==1)return new(OceanCheck.Verified,candidates[0],candidates,own.Length);
        if(candidates.Contains(0))return new(OceanCheck.Unverified,0,candidates,own.Length);
        return new(OceanCheck.Contradicted,-1,candidates,own.Length);
    }

    /// <summary>The routes slot <paramref name="index"/> can have from <paramref name="port"/> under the shifts still possible, by id.</summary>
    public IReadOnlyList<OceanRoute> Possible(long index,OceanPort port,IReadOnlyList<int> candidates)
        =>loop.Count==0 ? []:candidates.Select(shift=>Column(port,index+shift)).Distinct().Select(id=>routes.GetValueOrDefault(id)).OfType<OceanRoute>().OrderBy(route=>route.Id).ToArray();

    public static OceanTime TimeOf(uint defineRow)=>defineRow switch{1=>OceanTime.Night,2=>OceanTime.Day,3=>OceanTime.Sunset,_=>OceanTime.Unknown};

    public IReadOnlyCollection<OceanRoute> Routes=>routes.Values.OrderBy(route=>route.Id).ToArray();

    /// <summary>The departure of loop slot <paramref name="index"/> (counted from the loop start, any integer ≥ 0).</summary>
    public OceanDeparture At(long index)
    {
        return new(LoopStart+Step*index,RouteAt(OceanPort.Aldenard,index,AldenardOffset),RouteAt(OceanPort.Othard,index,OthardOffset));
    }

    /// <summary>
    /// The next departures whose boarding has not closed yet, earliest first; with <paramref name="route"/>, only those where that voyage
    /// leaves (from either port). At most <paramref name="count"/>, searched over two full loops.
    /// </summary>
    public IReadOnlyList<OceanDeparture> Next(DateTimeOffset now,int count,uint? route=null)
    {
        if(loop.Count==0 || count<=0)return [];
        var first=Math.Max(0,(long)Math.Floor((now-Boarding-LoopStart)/Step)+1);
        var found=new List<OceanDeparture>();
        for(var index=first;index<first+2L*loop.Count && found.Count<count;index++)
        {
            var departure=At(index);
            if(departure.At+Boarding<=now)continue;
            if(route is { } wanted && departure.Aldenard?.Id!=wanted && departure.Othard?.Id!=wanted)continue;
            found.Add(departure);
        }
        return found;
    }
}
