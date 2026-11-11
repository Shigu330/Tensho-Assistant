// 0.880 (X-75.6): the community ocean timetable and weather formula checked against what the game says, counted and logged.
internal static class GameChecks880Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game checks: one ocean check per voyage (boarding counted in the slot), weather 10 s after entering an open zone",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs"));
            Check(panel.Contains("if(frame.RouteId==0 || frame.RouteId==oceanRouteChecked || (OceanTimetableNow ?? OceanTimetable)?.Invoke() is not { } timetable)return;")
                && panel.Contains("(DateTimeOffset.UtcNow+OceanSchedule.Boarding-OceanSchedule.LoopStart)/OceanSchedule.Step"),"ocean");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("weatherCheckAt=duty==0 ? now+10_000:-100_000;") && watcher.Contains("if(expected==0 || actual==0)return;"),"weather");
        }),
    ];
}
