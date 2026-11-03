using TenshoAssistant;
using TenshoAssistant.Alarms;

// 0.331 (queue step e: alarms, free company, quests, gear, mini-games audit). A 0-minute alarm survives a frame hitch; the game command
// queue, the weather catalogue wait and the Discord categories are pinned in the source (Dalamud code is outside the test project).
internal static class Tools331Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm with no lead: still rings when checked a few seconds late, and only once",() =>
        {
            var alarm = AlarmSchedule.ForWindows("Test",[480],0,0,0)[0];
            var start = DateTimeOffset.UtcNow;
            var occurrence = AlarmSchedule.Next(alarm,start) ?? throw new Exception("no occurrence");
            var late = AlarmSchedule.Check(alarm,occurrence.AddSeconds(5));
            Check(late.Check == AlarmCheck.Fire && late.Occurrence == occurrence,"rings 5 s late: " + late.Check);
            alarm.LastOccurrence = occurrence;
            Check(AlarmSchedule.Check(alarm,occurrence.AddSeconds(20)).Check == AlarmCheck.None,"not twice");
        }),
        ("Alarms: nothing is sent for a deleted alarm, a weather alarm waits for the catalogue, vessel returns follow their Discord category",() =>
        {
            var service = Read("Alarms/AlarmService.Dalamud.cs");
            Check(service.Contains("if (catalog is null && alarm.Weathers.Count + alarm.PreviousWeathers.Count > 0) continue;",StringComparison.Ordinal),"weather alarm not consumed while the catalogue is unknown");
            var send = service.IndexOf("ui->ProcessChatBoxEntry(text)",StringComparison.Ordinal);
            var owner = service.IndexOf("var owner = configuration.Alarms?.FirstOrDefault(alarm => alarm.Id == next.Id)",StringComparison.Ordinal);
            Check(owner > 0 && send > 0 && owner < send,"the alarm is looked up before its /alarm command is sent");
            Check(Read("MainWindow.Reminders.cs").Contains("key==\"Voyages\" ? AlarmSource.Voyage",StringComparison.Ordinal),"vessel returns use the « Free company » Discord category");
        }),
        ("Duty Finder has its own loot service; restore-all resyncs developer mode",() =>
        {
            Check(Read("Plugin.cs").Contains("mainWindow.ConfigureDutyFinder(dutyFinder,dutyFinderLoot);",StringComparison.Ordinal),"separate service, same Garland transport");
            Check(Read("UI/Components/SettingRow.cs").Contains("developerMode",StringComparison.Ordinal),"developer mode follows « Restore all defaults »");
        }),
    ];
}
