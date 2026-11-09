// 0.832 (X-62, user report): alarms added from a tracking list still rang with the item gathered (24/2).
using TenshoAssistant.Alarms;
internal static class ListAlarms832Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("List alarms: silent once the item is held as the list needed; other alarms always ring",() =>
        {
            Check(AlarmSchedule.Gathered(new TenshoAlarm{ItemId=5,Needed=2},24),"24/2: silent");
            Check(!AlarmSchedule.Gathered(new TenshoAlarm{ItemId=5,Needed=2},1),"1/2: rings");
            Check(!AlarmSchedule.Gathered(new TenshoAlarm{ItemId=5},24),"not from a list: rings");
            var service=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Alarms/AlarmService.Dalamud.cs"));
            Check(service.Contains("if (check != AlarmCheck.Fire || gathered) continue;") && service.Contains("if (!gathered && settings.RingOnTime"),"ring and on-time skipped");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs")).Contains("alarm.Needed=needed;"),"the list's quantity kept");
        }),
    ];
}
