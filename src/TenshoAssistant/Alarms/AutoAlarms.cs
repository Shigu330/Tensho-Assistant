namespace TenshoAssistant.Alarms;

/// <summary>A deadline Tensho read in the game: a key that names it for good (« venture:retainer », « vessel:house/kind/slot »), its label and time.</summary>
public sealed record DueEvent(string Key,string Label,string Group,DateTimeOffset At,AlarmSource Source);

/// <summary>
/// g, M-01 (0.371): automatic Tensho alarms at the deadlines Tensho reads (retainer ventures, vessel returns) — one alarm per deadline
/// key, created at the first reading, moved when the time read changes, removed an hour after it rang. Alarms the player made are never
/// touched; no game alarm (/alarm) is ever made automatically. The alarm list limit is kept.
/// </summary>
public static class AutoAlarms
{
    /// <summary>Applies the readings to the alarm list; true when it changed (to be saved).</summary>
    public static bool Sync(List<TenshoAlarm> alarms,IReadOnlyCollection<DueEvent> events,DateTimeOffset now,int lead)
    {
        var changed=false;
        foreach(var due in events)
        {
            if(due.At<=now)continue;
            var known=alarms.FirstOrDefault(alarm=>alarm.AutoKey==due.Key);
            if(known is null)
            {
                if(alarms.Count>=AlarmSchedule.Maximum)continue;
                alarms.Add(new TenshoAlarm{Label=due.Label,Group=due.Group,Source=due.Source,Repeat=AlarmRepeat.Once,At=due.At,
                    LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.Once,lead),AutoKey=due.Key});
                changed=true;
            }
            else if(known.At!=due.At || known.Label!=due.Label)
            {
                known.At=due.At;known.Label=due.Label;known.LastOccurrence=default;
                changed=true;
            }
        }
        // An automatic alarm that rang more than an hour ago and was not read again is done.
        changed|=alarms.RemoveAll(alarm=>alarm.AutoKey.Length>0 && alarm.Repeat==AlarmRepeat.Once && alarm.At<now.AddHours(-1)
            && !events.Any(due=>due.Key==alarm.AutoKey && due.At>now))>0;
        return changed;
    }
}
