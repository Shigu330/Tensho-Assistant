namespace TenshoAssistant.Alarms;

/// <summary>
/// R-13 (0.415): the rows of the pinned tracking window — the enabled Tensho alarms of an item (gathering or fishing), soonest first.
/// Remaining time from the alarm's next ring (AlarmSchedule.Next); Weather: the ring waits for a weather condition.
/// </summary>
public sealed record TrackedAlarm(Guid Id,string Label,uint ItemId,uint IconId,DateTimeOffset At,bool Weather);

public static class TrackedAlarms
{
    public const int MaxRows=12;

    /// <summary>X-727 (0.1733, the user's request of 21/11): a row's icon: the alarm's own (an item's or a gathering point's), else its item's
    /// icon from the Item sheet (an alarm made without one has 0), else none.</summary>
    public static uint Icon(uint alarmIcon,uint itemId,Func<uint,uint>? itemIcon)=>alarmIcon!=0 ? alarmIcon:itemIcon?.Invoke(itemId) ?? 0;

    /// <param name="itemIcon">X-727 (0.1733): an item's icon (the Item sheet), for the alarms made without one; null: the alarm's only.</param>
    public static IReadOnlyList<TrackedAlarm> Rows(IEnumerable<TenshoAlarm>? alarms,DateTimeOffset now,int max=MaxRows,Func<uint,uint>? itemIcon=null)
        =>(alarms ?? []).Where(alarm=>alarm.Enabled && alarm.ItemId!=0)
            .Select(alarm=>(Alarm:alarm,At:AlarmSchedule.Next(alarm,now)))
            .Where(pair=>pair.At is not null)
            .OrderBy(pair=>pair.At).ThenBy(pair=>pair.Alarm.Label,StringComparer.CurrentCultureIgnoreCase)
            .Take(Math.Clamp(max,1,MaxRows))
            .Select(pair=>new TrackedAlarm(pair.Alarm.Id,pair.Alarm.Label,pair.Alarm.ItemId,Icon(pair.Alarm.IconId,pair.Alarm.ItemId,itemIcon),pair.At!.Value,
                pair.Alarm.Weathers.Count+pair.Alarm.PreviousWeathers.Count>0))
            .ToArray();
}
