// 0.755 (X-09, user request): the alarms of a tracking list's timed points.
internal static class ListAlarms755Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("List alarms: what is still needed, timed points only, named as the Gathering page names them, never twice",() =>
        {
            var page=Read("MainWindow.ListPanels.cs");
            Check(page.Contains("held.Total>=needed)continue;"),"only what is still needed");
            Check(page.Contains("timing.Kind!=Gathering.GatheringScheduleKind.Limited"),"timed only");
            Check(page.Contains("$\"{name} · {place}\"") && Read("MainWindow.Alarms.cs").Contains("var label=$\"{SelectedItem?.Name ?? itemId.ToString()} · {place}\";"),"same name as the Gathering page");
            Check(page.Contains("known.Label==alarm.Label && known.Repeat==alarm.Repeat && known.EtMinute==alarm.EtMinute"),"existing left out");
            Check(page.Contains("AddAlarms(known.Alarms)"),"through the usual add, with its maximum and the game's copy");
        }),
    ];
}
