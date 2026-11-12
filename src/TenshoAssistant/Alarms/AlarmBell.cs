namespace TenshoAssistant.Alarms;

/// <summary>
/// X-163 (0.1132, user request of 12/11): the alarms the desktop application rings, decided by the same rule as the plugin
/// (AlarmSchedule.Check, weather conditions included) but with its own memory of what it rang: the plugin marks an occurrence handled in
/// the saved settings when it rings it in the game, and the application must still ring it when the game is behind other windows.
/// Alarms copied into the game's own alarms ring there; a weather condition waits for the weather catalogue (never rung on a guess).
/// Pure; one instance per application.
/// </summary>
public sealed class AlarmBell
{
    private readonly HashSet<(string Key,DateTimeOffset Occurrence)> rung=[];

    public IReadOnlyList<(TenshoAlarm Alarm,DateTimeOffset Occurrence)> Due(IEnumerable<TenshoAlarm>? alarms,DateTimeOffset now,Func<TenshoAlarm,DateTimeOffset,uint?>? weatherAt)
    {
        var due=new List<(TenshoAlarm,DateTimeOffset)>();
        foreach(var alarm in alarms ?? [])
        {
            if(alarm.GameName.Length>0)continue;
            if(weatherAt is null && alarm.Weathers.Count+alarm.PreviousWeathers.Count>0)continue;
            var (check,occurrence)=AlarmSchedule.Check(alarm,now,weatherAt is null ? null:time=>weatherAt(alarm,time),ownMemory:true);
            if(check!=AlarmCheck.Fire || !rung.Add((Key(alarm),occurrence)))continue;
            due.Add((alarm,occurrence));
        }
        // Old occurrences are forgotten (a day after), so the memory stays small.
        rung.RemoveWhere(entry=>entry.Occurrence<now.AddDays(-1));
        return due;
    }

    // The saved settings are read again into new objects: an alarm is known by what it is, not by its object.
    private static string Key(TenshoAlarm alarm)=>$"{alarm.Label}|{alarm.Repeat}|{alarm.EtMinute}|{alarm.At:O}|{alarm.TerritoryId}|{alarm.ItemId}";
}
