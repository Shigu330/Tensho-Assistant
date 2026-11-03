using TenshoAssistant.Reminders;

// 0.381 (g, X-01): free tasks — « Once » stays done until removed, added last to the enum, « /tensho todo » adds one.
internal static class FreeTasks381Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Free tasks: once done they stay done; the command adds a free task, refused when the name is bad or the list full",() =>
        {
            Check((int)TaskPeriod.Once==4,"added last: saved numbers keep their meaning");
            var now = DateTimeOffset.UtcNow;
            var task = PersonalTasks.FromCommand("  Acheter des potions ",0) ?? throw new Exception("not added");
            Check(task.Name=="Acheter des potions" && task.Period==TaskPeriod.Once && !PersonalTasks.IsDone(task,now),"added, to do");
            PersonalTasks.Mark(task,true,now);
            Check(PersonalTasks.IsDone(task,now.AddDays(30)) && PersonalTasks.ComesBack(task,now) is null,"stays done");
            Check(PersonalTasks.FromCommand(" ",0) is null && PersonalTasks.FromCommand("x",PersonalTasks.Capacity) is null,"refused");
            Check(Read("Plugin.cs").Contains("mainWindow.AddTaskFromCommand(parts[1])",StringComparison.Ordinal),"command wired");
        }),
    ];
}
