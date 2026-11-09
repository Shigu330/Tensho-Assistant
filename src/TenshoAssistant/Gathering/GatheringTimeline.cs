namespace TenshoAssistant.Gathering;

public sealed record GatheringOpening(DateTimeOffset StartsAt,DateTimeOffset EndsAt,bool Current)
{
    /// <summary>Whether this opening is open at <paramref name="now"/>, and the time left until it closes (open) or opens (not open).</summary>
    public (bool Open,TimeSpan Left) At(DateTimeOffset now){var open=StartsAt<=now && now<EndsAt;return (open,(open ? EndsAt:StartsAt)-now);}
}
public sealed record GatheringTimeline(GatheringScheduleKind Kind,int EtMinute,DateTimeOffset CalculatedAt,IReadOnlyList<GatheringOpening> Openings);
/// <summary>0.588 (C7): the timed points open now, then those opening soon. Fish are left out: their weather is not checked here.</summary>
public static class GatheringNow
{
    public static IReadOnlyList<(GatheringSource Source,GatheringOpening Opening)> Rows(IEnumerable<GatheringSource> sources,DateTimeOffset now,TimeSpan soon,int limit)
    {
        var rows=new List<(GatheringSource Source,GatheringOpening Opening)>();
        foreach(var source in sources)
        {
            if(source.IsFishing || source.Timing.Kind!=GatheringScheduleKind.Limited)continue;
            if(GatheringTime.Next(source.Timing,now,1).Openings is not [var opening,..] || !opening.Current && opening.StartsAt>now+soon)continue;
            rows.Add((source,opening));
        }
        return rows.OrderByDescending(row=>row.Opening.Current).ThenBy(row=>row.Opening.Current ? row.Opening.EndsAt:row.Opening.StartsAt).ThenBy(row=>row.Source.ItemId)
            .DistinctBy(row=>(row.Source.ItemId,row.Source.PointId)).Take(limit).ToArray();
    }
}

public static class GatheringTime
{
    private const long DayTicks=4200L*TimeSpan.TicksPerSecond;
    /// <summary>All clipped static openings in a bounded UTC interval, including midnight joins.</summary>
    public static IReadOnlyList<GatheringOpening> Between(GatheringSchedule schedule,DateTimeOffset from,DateTimeOffset until,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(schedule);cancellation.ThrowIfCancellationRequested();
        from=from.ToUniversalTime();until=until.ToUniversalTime();
        if(from<DateTimeOffset.UnixEpoch || until<=from || until-from>TimeSpan.FromSeconds(9*4200))throw new ArgumentOutOfRangeException(nameof(until));
        if(schedule.Kind==GatheringScheduleKind.Unrestricted)return Array.AsReadOnly(new[]{new GatheringOpening(from,until,true)});
        if(schedule.Kind!=GatheringScheduleKind.Limited)return Array.AsReadOnly(Array.Empty<GatheringOpening>());
        var first=from.UtcTicks-DateTimeOffset.UnixEpoch.UtcTicks;var last=until.UtcTicks-DateTimeOffset.UnixEpoch.UtcTicks;
        var candidates=new List<(long Start,long End)>();
        for(var day=first/DayTicks-1;day<=last/DayTicks;day++)
        foreach(var window in schedule.Windows)
        {
            cancellation.ThrowIfCancellationRequested();
            long Boundary(int minute)=>day*DayTicks+(minute*35L*TimeSpan.TicksPerSecond+11)/12;
            var start=Math.Max(first,Boundary(window.StartMinute));var end=Math.Min(last,Boundary(window.EndMinute));
            if(start<end)candidates.Add((start,end));
        }
        var merged=new List<(long Start,long End)>();
        foreach(var value in candidates.OrderBy(row=>row.Start).ThenBy(row=>row.End))
        {
            cancellation.ThrowIfCancellationRequested();
            if(merged.Count>0 && value.Start<=merged[^1].End)merged[^1]=(merged[^1].Start,Math.Max(value.End,merged[^1].End));
            else merged.Add(value);
        }
        return Array.AsReadOnly(merged.Select(value=>new GatheringOpening(DateTimeOffset.UnixEpoch.AddTicks(value.Start),DateTimeOffset.UnixEpoch.AddTicks(value.End),value.Start==first)).ToArray());
    }
    public static int MinuteOfDay(DateTimeOffset date)
    {
        var ticks=date.UtcTicks-DateTimeOffset.UnixEpoch.UtcTicks;
        if(ticks<0)throw new ArgumentOutOfRangeException(nameof(date));
        return (int)(ticks%DayTicks*12/(35L*TimeSpan.TicksPerSecond));
    }
    /// <summary>An Eorzea minute of the day (0 to 1439) as « HH:mm ».</summary>
    public static string Clock(int minute)=>$"{minute/60:00}:{minute%60:00}";
    public static GatheringTimeline Next(GatheringSchedule schedule,DateTimeOffset date,int count=3)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if(count is <1 or >12 || date<DateTimeOffset.UnixEpoch || date>DateTimeOffset.MaxValue.AddSeconds(-(count+3)*4200))throw new ArgumentOutOfRangeException(nameof(date));
        var minute=MinuteOfDay(date);
        if(schedule.Kind!=GatheringScheduleKind.Limited)return new(schedule.Kind,minute,date.ToUniversalTime(),Array.AsReadOnly(Array.Empty<GatheringOpening>()));
        var day=(date.UtcTicks-DateTimeOffset.UnixEpoch.UtcTicks)/DayTicks*DayTicks;
        var candidates=new List<(long Start,long End)>();
        for(var offset=-1;offset<=count+1;offset++)
        foreach(var window in schedule.Windows)
        {
            // First representable tick at the exact rational ET minute boundary (35/12 real seconds).
            long Boundary(int value)=>day+offset*DayTicks+(value*35L*TimeSpan.TicksPerSecond+11)/12;
            candidates.Add((Boundary(window.StartMinute),Boundary(window.EndMinute)));
        }
        var merged=new List<(long Start,long End)>();
        foreach(var candidate in candidates.OrderBy(value=>value.Start).ThenBy(value=>value.End))
        {
            if(merged.Count>0 && candidate.Start<=merged[^1].End)merged[^1]=(merged[^1].Start,Math.Max(merged[^1].End,candidate.End));
            else merged.Add(candidate);
        }
        var now=date.UtcTicks-DateTimeOffset.UnixEpoch.UtcTicks;
        var result=merged.Where(window=>window.End>now).Take(count).Select(window=>new GatheringOpening(DateTimeOffset.UnixEpoch.AddTicks(window.Start),DateTimeOffset.UnixEpoch.AddTicks(window.End),window.Start<=now)).ToArray();
        return new(schedule.Kind,minute,date.ToUniversalTime(),Array.AsReadOnly(result));
    }
}
