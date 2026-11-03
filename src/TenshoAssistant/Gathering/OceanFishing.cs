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

/// <summary>
/// g, R-01 (0.392): the ocean fishing timetable. The loop of departures is the game's IKDRouteTable (144 rows: Route = the voyage from
/// Limsa Lominsa, Unknown0 = the voyage from Kugane), one departure every 2 hours from the loop start 748 800 000 ms after 1970-01-01 UTC
/// (1970-01-09 16:00). The start, the 2-hour step and the meaning of the moment rows are facts read in GatherBuddy (Apache-2.0,
/// Plugin/OceanUptime.cs and GameData.cs, read only; original code), docs/evidence/0.392-ocean-fishing.md.
/// </summary>
public sealed class OceanSchedule(IReadOnlyList<(uint Aldenard,uint Othard)> loop,IReadOnlyDictionary<uint,OceanRoute> routes)
{
    public static readonly DateTimeOffset LoopStart=DateTimeOffset.FromUnixTimeMilliseconds(748_800_000);
    public static readonly TimeSpan Step=TimeSpan.FromHours(2),Boarding=TimeSpan.FromMinutes(15);

    public static OceanTime TimeOf(uint defineRow)=>defineRow switch{1=>OceanTime.Night,2=>OceanTime.Day,3=>OceanTime.Sunset,_=>OceanTime.Unknown};

    public IReadOnlyCollection<OceanRoute> Routes=>routes.Values.OrderBy(route=>route.Id).ToArray();

    /// <summary>The departure of loop slot <paramref name="index"/> (counted from the loop start, any integer ≥ 0).</summary>
    public OceanDeparture At(long index)
    {
        var (aldenard,othard)=loop[(int)(index%loop.Count)];
        return new(LoopStart+Step*index,routes.GetValueOrDefault(aldenard),routes.GetValueOrDefault(othard));
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
