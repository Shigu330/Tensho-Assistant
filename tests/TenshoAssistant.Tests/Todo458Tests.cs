using TenshoAssistant.Reminders;

// 0.458 (in-game campaign): the Reminders icon shows what is left to do; the Reminders page chooses it with its « Bar » boxes.
internal static class Todo458Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Left to do: rows not ticked since their reset, without those hidden; a tick before the reset counts as not done",() =>
        {
            var now=new DateTimeOffset(2026,11,4,12,0,0,TimeSpan.Zero);
            var all=ReminderCounts.Todo(_=>null,null,now);
            Check(all.SequenceEqual(ReminderCounts.CheckedRows),"everything to do");
            Check(!ReminderCounts.Todo(row=>row=="Supply" ? now:null,null,now).Contains("Supply"),"ticked now");
            Check(ReminderCounts.Todo(row=>row=="Supply" ? now.AddDays(-2):null,null,now).Contains("Supply"),"ticked before the reset");
            Check(!ReminderCounts.Todo(_=>null,["Fashion"],now).Contains("Fashion"),"hidden");
            Check(ReminderCounts.LastReset("Supply",now)==ResetSchedule.PreviousDaily(now,ResetSchedule.SupplyHour) && ReminderCounts.LastReset("Allied",now)==ResetSchedule.PreviousDaily(now,ResetSchedule.DailyHour)
                && ReminderCounts.LastReset("Fashion",now)==ResetSchedule.PreviousWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour),"same resets as the page");
            Check(ReminderCounts.BarRows.Contains("Ventures") && ReminderCounts.BarRows.Contains("SquadronTraining"),"finishing timers can be hidden");
        }),
        ("Reminders page: one « Bar » box per row, saved; the icon's number is everything left; same rows as the page's done boxes",() =>
        {
            var page=Read("MainWindow.Reminders.cs");
            Check(page.Contains("ImGui.BeginTable(\"Reminders\",6,") && page.Contains("ImGui.Checkbox(\"##Bar\"+key,ref inBar)") && page.Contains("PersonalTasks.Pending(configuration.PersonalTasks,now)"),"boxes and tasks");
            var entry=Read("Alarms/ReminderDtrEntry.Dalamud.cs");
            Check(entry.Contains("(finished.Sum(item=>item.Count)+left.Count)") && entry.Contains("configuration.ReminderBarHidden?.Contains(item.Kind.ToString())!=true"),"count and filter");
            Check(Read("Configuration.cs").Contains("\"ReminderBarHidden\","),"reset with the settings");
            foreach(var row in ReminderCounts.CheckedRows)Check(page.Contains("Row(\""+row+"\","),"row on the page: "+row);
        }),
    ];
}
