// 0.687 (user request): the Reminders page in tabs.
internal static class ReminderTabs687Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Reminders: every section has its tab, once",() =>
        {
            var page=Read("MainWindow.Reminders.cs");
            foreach(var section in new[]{"DrawReminderTimers(timers,now)","DrawPersonalTasks()","DrawDailyRoulettes(timers)","DrawSquadronPlanner()","DrawCharacterStatuses()"})
            {
                var first=page.IndexOf(section,StringComparison.Ordinal);
                Check(first>=0,section);
                Check(page.LastIndexOf(section,StringComparison.Ordinal)==first,section+" once");
            }
        }),
        ("Leves: the tooltip says what the columns say, for everyone, and the id stays for developers",() =>
        {
            var page=Read("MainWindow.Leves.cs");
            Check(page.Contains("if(ImGui.IsItemHovered())WrappedTooltip(LeveTooltip(leve));"),"for everyone");
            Check(page.Contains("if(developerMode)lines.Add($\"Leve {leve.Id}\");"),"the id");
        }),
    ];
}
