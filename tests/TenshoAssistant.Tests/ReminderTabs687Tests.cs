using TenshoAssistant.UI;
// 0.687 (user request): the Reminders page in tabs.
internal static class ReminderTabs687Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    private static string ReadLf(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
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
        ("Leves: a right click goes through the aetheryte question, for the client and for the leve",() =>
        {
            var page=Read("MainWindow.Leves.cs");
            Check(page.Contains("DrawLeveMenu(leve);") && page.Contains("BeginPopupContextItem"),"menu");
            Check(page.Contains("AskNearestAetheryteAt(map,MapProjection.PixelFromWorld(new Vector3(place.X,0,place.Z),map),leve.Name,flag:true)"),"the existing question, no teleport of its own");
            Check(!page.Contains("Telepo."),"no direct call");
        }),
        ("Leves: the tooltip says what the columns say, for everyone, and the id stays for developers",() =>
        {
            var page=Read("MainWindow.Leves.cs");
            Check(page.Contains("if(ImGui.IsItemHovered())WrappedTooltip(LeveTooltip(leve));"),"for everyone");
            Check(page.Contains("if(developerMode)lines.Add($\"Leve {leve.Id}\");"),"the id");
        }),
        ("0.558: a reminder ticks itself only when the game reads its counter at 0",() =>
        {
            Check(TenshoAssistant.Reminders.ReminderCounts.DoneByGame(0,0).SequenceEqual(["Allied","Deliveries"]),"both at 0");
            Check(TenshoAssistant.Reminders.ReminderCounts.DoneByGame(null,null).Count==0,"unknown says nothing");
            Check(TenshoAssistant.Reminders.ReminderCounts.DoneByGame(3,1).Count==0,"still to do");
            Check(new TenshoAssistant.Configuration().RemindersFromGame,"on by default");
            Check(ReadLf("Alarms/ReminderDtrEntry.Dalamud.cs").Contains("fromGame?.Invoke(timers);"),"also from the server bar");
        }),
    ];
}
