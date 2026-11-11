using System.Text.Json;

namespace TenshoAssistant.Gathering;

/// <summary>One fish caught at one stop of the voyage: how many (NQ and HQ) and the points they brought.</summary>
public sealed record OceanZoneCatch(int Zone,uint Item,int Count,int Points);

/// <summary>
/// 0.1094 (user report, 11/11 15 h: the DLL was reloaded seven times during an ocean voyage): what Tensho keeps of the voyage in progress
/// so that a reload of the plugin (or of the game window) does not lose it: the route, the stop, the catches of each stop, the missions'
/// progress and whether the schedule check and the result line were done. Small, saved beside the settings, read again only for the same
/// voyage (same route, recent, stop not behind).
/// </summary>
public sealed record OceanVoyageRecord(uint RouteId,int Zone,DateTimeOffset Updated,IReadOnlyList<OceanZoneCatch> Catches,IReadOnlyList<int> Missions,
    bool ScheduleChecked,bool ResultLogged)
{
    public const int Format=1;
    /// <summary>A voyage lasts about an hour, each stop under 20 minutes: a record older than this is another voyage.</summary>
    public static readonly TimeSpan Keep=TimeSpan.FromMinutes(25);

    /// <summary>The record is this voyage's: same route, saved recently, the stop now is the same or later.</summary>
    public bool SameVoyage(uint route,int zone,DateTimeOffset now)
        =>RouteId!=0 && RouteId==route && zone>=Zone && now-Updated<=Keep && Updated-now<=TimeSpan.FromMinutes(1);

    /// <summary>
    /// The voyage now: the game's catches and missions, never below what was saved for this voyage (counts and points only grow during a
    /// voyage); a record of another voyage is ignored.
    /// </summary>
    public static OceanVoyageRecord Merge(OceanVoyageRecord? saved,uint route,int zone,IEnumerable<OceanZoneCatch> game,IReadOnlyList<int> missions,DateTimeOffset now)
    {
        var same=saved is not null && saved.SameVoyage(route,zone,now) ? saved:null;
        var catches=new Dictionary<(int,uint),OceanZoneCatch>();
        foreach(var fish in (same?.Catches ?? []).Concat(game))
        {
            if(fish.Item==0 || fish.Zone is < 0 or > 2 || fish.Count<0 || fish.Points<0)continue;
            catches[(fish.Zone,fish.Item)]=catches.TryGetValue((fish.Zone,fish.Item),out var was)
                ? was with{Count=Math.Max(was.Count,fish.Count),Points=Math.Max(was.Points,fish.Points)}:fish;
        }
        var progress=new int[Math.Max(missions.Count,same?.Missions.Count ?? 0)];
        for(var index=0;index<progress.Length;index++)
            progress[index]=Math.Max(index<missions.Count ? missions[index]:0,same is not null && index<same.Missions.Count ? same.Missions[index]:0);
        return new(route,zone,now,catches.Values.OrderBy(fish=>fish.Zone).ThenBy(fish=>fish.Item).ToArray(),progress,same?.ScheduleChecked==true,same?.ResultLogged==true);
    }

    /// <summary>Same content (the time aside): nothing to save.</summary>
    public bool SameContent(OceanVoyageRecord? other)
        =>other is not null && other.RouteId==RouteId && other.Zone==Zone && other.ScheduleChecked==ScheduleChecked && other.ResultLogged==ResultLogged
          && other.Catches.SequenceEqual(Catches) && other.Missions.SequenceEqual(Missions);

    /// <summary>The fish caught at one stop, as the voyage frame gives them (item, NQ+HQ as NQ, 0 HQ, points).</summary>
    public IReadOnlyList<OceanZoneCatch> At(int zone)=>Catches.Where(fish=>fish.Zone==zone).ToArray();

    public string ToJson()=>JsonSerializer.Serialize(new Saved(Format,RouteId,Zone,Updated,
        Catches.Select(fish=>new[]{fish.Zone,(long)fish.Item,fish.Count,fish.Points}).ToArray(),Missions.ToArray(),ScheduleChecked,ResultLogged));

    /// <summary>Null for anything unreadable or of another format: the voyage is then read from the game alone.</summary>
    public static OceanVoyageRecord? FromJson(string? text)
    {
        if(string.IsNullOrWhiteSpace(text))return null;
        try
        {
            var saved=JsonSerializer.Deserialize<Saved>(text);
            if(saved is null || saved.Format!=Format || saved.Catches is null || saved.Missions is null)return null;
            var catches=new List<OceanZoneCatch>();
            foreach(var row in saved.Catches)
            {
                if(row is not { Length:4 } || row[0] is < 0 or > 2 || row[1] is <= 0 or > uint.MaxValue || row[2] is < 0 or > 10_000 || row[3] is < 0 or > 1_000_000)return null;
                catches.Add(new((int)row[0],(uint)row[1],(int)row[2],(int)row[3]));
            }
            return new(saved.Route,saved.Zone,saved.Updated,catches,saved.Missions,saved.ScheduleChecked,saved.ResultLogged);
        }
        catch(JsonException){return null;}
    }

    private sealed record Saved(int Format,uint Route,int Zone,DateTimeOffset Updated,long[][]? Catches,int[]? Missions,bool ScheduleChecked,bool ResultLogged);
}
