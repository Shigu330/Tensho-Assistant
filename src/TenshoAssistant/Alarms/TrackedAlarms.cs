namespace TenshoAssistant.Alarms;

/// <summary>
/// R-13 (0.415): the rows of the pinned tracking window — the enabled Tensho alarms of an item (gathering or fishing), soonest first.
/// Remaining time from the alarm's next ring (AlarmSchedule.Next); Weather: the ring waits for a weather condition.
/// </summary>
public sealed record TrackedAlarm(Guid Id,string Label,uint ItemId,uint IconId,DateTimeOffset At,bool Weather);

public static class TrackedAlarms
{
    public const int MaxRows=12;

    public static IReadOnlyList<TrackedAlarm> Rows(IEnumerable<TenshoAlarm>? alarms,DateTimeOffset now,int max=MaxRows)
        =>(alarms ?? []).Where(alarm=>alarm.Enabled && alarm.ItemId!=0)
            .Select(alarm=>(Alarm:alarm,At:AlarmSchedule.Next(alarm,now)))
            .Where(pair=>pair.At is not null)
            .OrderBy(pair=>pair.At).ThenBy(pair=>pair.Alarm.Label,StringComparer.CurrentCultureIgnoreCase)
            .Take(Math.Clamp(max,1,MaxRows))
            .Select(pair=>new TrackedAlarm(pair.Alarm.Id,pair.Alarm.Label,pair.Alarm.ItemId,pair.Alarm.IconId,pair.At!.Value,
                pair.Alarm.Weathers.Count+pair.Alarm.PreviousWeathers.Count>0))
            .ToArray();
}
