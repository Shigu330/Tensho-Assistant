using TenshoAssistant.FreeCompany;
using TenshoAssistant.Reminders;

// 0.455 (in-game campaign, batch 1): gil tables, the minus sign, squadron automatic alarms, reminders apart from alarms in the server bar.
internal static class Campaign455Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Finished reminders: ventures, vessels, squadron and ripe beds counted, unknown or running left out",() =>
        {
            var now=new DateTimeOffset(2026,11,4,12,0,0,TimeSpan.Zero);
            Check(ReminderCounts.Finished(null,null,null,null,null,now).Count==0,"nothing known");
            var ripe=GardenJournal.Plant("Pot","Seed",1,0,now.AddHours(-2));
            var growing=GardenJournal.Plant("Pot","Seed",5,0,now.AddHours(-2));
            var list=ReminderCounts.Finished([now.AddHours(-1),now.AddHours(1),null],[new VoyageVessel{ReturnAt=now.AddMinutes(-1)},new VoyageVessel{ReturnAt=now.AddMinutes(1)},new VoyageVessel()],
                (7,now.AddSeconds(-1)),(0,now.AddSeconds(-1)),[ripe,growing],now);
            Check(list.Count==4 && list.Contains((FinishedKind.Ventures,1)) && list.Contains((FinishedKind.Voyages,1)) && list.Contains((FinishedKind.SquadronMission,1)) && list.Contains((FinishedKind.Garden,1)),"counted");
            Check(ReminderCounts.Finished(null,null,(7,now.AddSeconds(1)),(7,null),null,now).Count==0,"running or no end");
        }),
        ("Server bar: alarms keep the number switched on, reminders have their own entry, icon, setting and count finished ones",() =>
        {
            Check(Read("Alarms/AlarmDtrEntry.Dalamud.cs").Contains("SeIconChar.Clock.ToIconString()+\" \"+enabled.Length+time"),"alarms: the number switched on (user's choice, 2026-11-04)");
            var reminder=Read("Alarms/ReminderDtrEntry.Dalamud.cs");
            Check(reminder.Contains("bar.Get(\"Tensho Assistant (Reminders)\")") && reminder.Contains("SeIconChar.QuestRepeatable") && reminder.Contains("if((!configuration.ReminderServerBar && !configuration.ReminderFinishedServerBar) || !player.IsLoaded)"),"own entry");
            Check(new TenshoAssistant.Configuration().ReminderServerBar && Read("Configuration.cs").Contains("\"ReminderServerBar\","),"setting on by default, reset");
            Check(Read("Plugin.cs").Contains("(\"ReminderBar\",()=>reminderBar?.Invoke()?.DrawTooltip())"),"tooltip drawn, isolated");
        }),
        ("Squadron: automatic alarms at the end of the mission and of the training",() =>
        {
            var watcher=Read("Alarms/AutoAlarmWatcher.Dalamud.cs");
            Check(watcher.Contains("new(\"squadron:mission\",") && watcher.Contains("new(\"squadron:training\",") && watcher.Contains("if(player!=null && player->IsLoaded)"),"events");
        }),
        ("Gil journal: tables; the minus sign the game font shows as « = » is gone from the texts",() =>
        {
            var ui=Read("MainWindow.GilJournal.cs");
            Check(ui.Contains("BeginDataTable(\"GilTotals\"+source,4)") && ui.Contains("BeginDataTable(\"GilLatest\",5)") && !ui.Contains("GilJournal.Line"),"tables");
            foreach(var language in new[]{"de","en","fr","ja"})Check(!Read($"Localization/Languages/{language}.json").Contains('−'),language);
        }),
    ];
}
