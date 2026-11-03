using TenshoAssistant.Alarms;

// 0.371 (g, M-01): automatic alarms — created once per deadline, moved with the readings, removed after they rang, player alarms untouched.
internal static class AutoAlarms371Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Automatic alarms: one per deadline, moved, removed an hour after ringing, never a player alarm, limit kept",() =>
        {
            var now = DateTimeOffset.UnixEpoch.AddDays(100);
            var own = new TenshoAlarm{Label="mine",Repeat=AlarmRepeat.Once,At=now.AddHours(-5)};
            var alarms = new List<TenshoAlarm>{own};
            var venture = new DueEvent("venture:1","A","Ventures",now.AddHours(1),AlarmSource.Reminder);
            Check(AutoAlarms.Sync(alarms,[venture],now,5) && alarms.Count == 2 && alarms[1].AutoKey == "venture:1" && alarms[1].LeadMinutes == 5,"created");
            Check(!AutoAlarms.Sync(alarms,[venture],now,5) && alarms.Count == 2,"same reading: nothing to save");
            Check(AutoAlarms.Sync(alarms,[venture with { At = now.AddHours(2) }],now,5) && alarms[1].At == now.AddHours(2),"moved with the reading");
            Check(AutoAlarms.Sync(alarms,[],now.AddHours(4),5) && alarms.Count == 1 && alarms[0] == own,"removed after ringing; own alarm kept");
            var full = Enumerable.Range(0,AlarmSchedule.Maximum).Select(i => new TenshoAlarm{Label=$"x{i}"}).ToList();
            Check(!AutoAlarms.Sync(full,[venture],now,5) && full.Count == AlarmSchedule.Maximum,"limit kept");
            Check(new AlarmSettings().AutoAlarms,"on by default (user's choice)");
        }),
    ];
}
