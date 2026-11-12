using TenshoAssistant.Alarms;

// 0.1159 (X-133, user request of 11/11): the reminders' alarms (a reminder's bell, the automatic alarms of ventures, voyages and the
// squadron) are listed in an « Alarms » tab of the Reminders tool, their number beside « Reminders » in the tools' menu; the Alarms tool
// keeps the others, without the reminders' alarms in its list or its count. One drawing of the alarm groups serves both.
internal static class ReminderAlarms1159Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Reminder alarms 1159: which alarms belong to the Reminders tool",() =>
        {
            bool Reminder(TenshoAlarm alarm)=>DiscordWebhook.ReminderRowOf(alarm).Length>0;
            Check(Reminder(new(){ReminderRow="Ventures"}) && Reminder(new(){ReminderRow="Voyages",Source=AlarmSource.Voyage}),"a reminder's bell");
            Check(Reminder(new(){AutoKey="venture:1"}) && Reminder(new(){AutoKey="vessel:2"}) && Reminder(new(){AutoKey="squadron:mission"}) && Reminder(new(){AutoKey="squadron:training"}),"the automatic alarms");
            Check(!Reminder(new(){Label="Copper ore",ItemId=5106,JobId=16,Source=AlarmSource.Gathering}),"a gathering alarm stays in Alarms");
            Check(!Reminder(new(){Label="Fish",Source=AlarmSource.Fishing}) && !Reminder(new(){Label="Mine"}),"a fishing or own alarm stays in Alarms");
            Check(!Reminder(new(){Label="Vessel",Group="Free Company",Source=AlarmSource.Voyage}),"a vessel alarm set from the Free Company page stays in Alarms");
        }),
        ("Reminder alarms 1159: one drawing for both tools, the Alarms tool without them, counts in the menu",() =>
        {
            var alarms=Read("src/TenshoAssistant/MainWindow.Alarms.cs");
            Check(alarms.Contains("internal static bool IsReminderAlarm(TenshoAlarm alarm)=>DiscordWebhook.ReminderRowOf(alarm).Length>0;"),"one rule, the Discord box's");
            Check(alarms.Contains("var shown=alarms.Where(alarm=>!IsReminderAlarm(alarm)).ToList();") && alarms.Contains("localization.Format(\"Alarms.Title\",shown.Count)"),"the Alarms tool without them, its count too");
            Check(alarms.Contains("var shown=alarms.Where(IsReminderAlarm).ToList();"),"the tab lists them");
            Check(alarms.Split("DrawAlarmGroups(shown,now,remove,added)").Length==3 && alarms.Split("foreach(var section in alarms.GroupBy(AlarmJob)").Length==2,"one drawing, used twice");
            var pages=Read("src/TenshoAssistant/MainWindow.Pages.cs");
            Check(pages.Contains("localization.Format(\"Navigation.Alarms\",AlarmsToolCount)"),"the Alarms count without them");
            Check(pages.Contains("ReminderAlarmCount is var reminderAlarms and >0 ? localization.Format(\"Navigation.RemindersCount\",reminderAlarms):localization.Get(\"Navigation.Reminders\")"),"their number beside Reminders");
            Check(Read("src/TenshoAssistant/MainWindow.Reminders.cs").Contains("(\"Alarms\",localization.Format(\"Reminders.TabAlarms\",ReminderAlarmCount),()=>DrawReminderAlarms()),"),"the tab");
        }),
    ];
}
