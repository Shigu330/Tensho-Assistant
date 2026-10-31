namespace TenshoAssistant.Gathering;

public sealed record GatheringOpening(DateTimeOffset StartsAt,DateTimeOffset EndsAt,bool Current);
public sealed record GatheringTimeline(GatheringScheduleKind Kind,int EtMinute,DateTimeOffset CalculatedAt,IReadOnlyList<GatheringOpening> Openings);
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
