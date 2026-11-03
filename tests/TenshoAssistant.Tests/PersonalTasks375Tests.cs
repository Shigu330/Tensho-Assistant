using TenshoAssistant.Reminders;

// 0.375 (g, M-05): personal tasks — day and week follow the game's resets, « every n hours » after it was done, counters up to their
// target; the on-screen panel lists the pending tasks marked for it, is off by default and hidden in combat.
internal static class PersonalTasks375Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Personal tasks: daily and weekly come back at the game's resets, every n hours after it was done",() =>
        {
            var now = new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            var lastDaily = ResetSchedule.PreviousDaily(now,ResetSchedule.DailyHour);
            var daily = new PersonalTask{Period=TaskPeriod.Daily,DoneAt=lastDaily.AddMinutes(1)};
            Check(PersonalTasks.IsDone(daily,now) && PersonalTasks.ComesBack(daily,now)==ResetSchedule.NextDaily(now,ResetSchedule.DailyHour),"done since the reset");
            daily.DoneAt=lastDaily.AddMinutes(-1);
            Check(!PersonalTasks.IsDone(daily,now) && PersonalTasks.ComesBack(daily,now) is null,"done before the reset: to do again");
            var weekly = new PersonalTask{Period=TaskPeriod.Weekly,DoneAt=ResetSchedule.PreviousWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour).AddHours(1)};
            Check(PersonalTasks.IsDone(weekly,now),"weekly");
            var hours = new PersonalTask{Period=TaskPeriod.EveryHours,Hours=18,DoneAt=now.AddHours(-17)};
            Check(PersonalTasks.IsDone(hours,now) && PersonalTasks.ComesBack(hours,now)==now.AddHours(1),"every 18 h");
            hours.Hours=0;Check(!PersonalTasks.IsDone(hours,now),"hours clamped to at least 1");
            PersonalTasks.Mark(hours,false,now);Check(hours.DoneAt is null,"unticked");
        }),
        ("Personal tasks: counters move one step between 0 and their target; names checked; pending list for the panel",() =>
        {
            var now = DateTimeOffset.UtcNow;
            var counter = new PersonalTask{Period=TaskPeriod.Counter,Target=2};
            PersonalTasks.Mark(counter,false,now);Check(counter.Count==0,"not below 0");
            PersonalTasks.Mark(counter,true,now);PersonalTasks.Mark(counter,true,now);PersonalTasks.Mark(counter,true,now);
            Check(counter.Count==2 && PersonalTasks.IsDone(counter,now),"stops at the target");
            Check(PersonalTasks.CleanName("  FATE  ")=="FATE" && PersonalTasks.CleanName(" ") is null && PersonalTasks.CleanName(new string('x',61)) is null
                && PersonalTasks.CleanName("a\nb") is null,"names");
            var hidden = new PersonalTask{Name="h",OnScreen=false};var shown = new PersonalTask{Name="s"};
            Check(PersonalTasks.Pending([counter,hidden,shown],now).SequenceEqual([shown]),"pending, marked for the panel, not done");
        }),
        ("Personal tasks: panel off by default, hidden in combat, tasks kept by « restore every setting »",() =>
        {
            Check(!new TenshoAssistant.Configuration().TaskPanel,"off by default");
            Check(TenshoAssistant.ConfigurationReset.Kept.Contains("PersonalTasks") && TenshoAssistant.ConfigurationReset.Reset.Contains("TaskPanel"),"lists");
            Check(Read("MainWindow.PersonalTasks.cs").Contains("!configuration.TaskPanel || mapLive?.State is { InCombat:true }",StringComparison.Ordinal),"hidden in combat");
        }),
    ];
}
