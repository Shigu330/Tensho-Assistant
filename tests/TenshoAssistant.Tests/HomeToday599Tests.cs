using TenshoAssistant.Reminders;

// 0.599 (T10): Home's « Today » block and the choice of tiles.
internal static class HomeToday599Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Today: the first names, and how many more",() =>
        {
            Check(HomeToday.Short(["A","B"],3,"(+{0})")=="A, B","all");
            Check(HomeToday.Short(["A","B","C","D","E"],3,"(+{0})")=="A, B, C (+2)","cut");
            Check(HomeToday.Short([],3,"(+{0})")=="","none");
        }),
        ("Today: a box hides or shows a line; nothing hidden is stored as nothing",() =>
        {
            var hidden=HomeToday.Toggle(null,"Quests",false);
            Check(hidden is ["Quests"],"hidden");
            Check(HomeToday.Toggle(hidden,"Quests",false) is ["Quests"],"not twice");
            hidden=HomeToday.Toggle(hidden,"Alarms",false);
            Check(hidden is ["Quests","Alarms"],"another");
            hidden=HomeToday.Toggle(hidden,"Quests",true);
            Check(hidden is ["Alarms"],"shown again");
            Check(HomeToday.Toggle(hidden,"Alarms",true) is null,"none left");
        }),
        ("Today: all shown by default, reset with the settings, nothing read in the frame",() =>
        {
            var settings=new TenshoAssistant.Configuration();
            Check(settings.HomeTodayHidden is null && settings.HomeTilesHidden is null,"everything shown");
            var config=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Configuration.cs"));
            Check(config.Contains("\"HomeTodayHidden\"") && config.Contains("\"HomeTilesHidden\""),"in the reset list");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.HomeToday.cs"));
            Check(view.Contains("TimeSpan.FromSeconds(1)") && !view.Contains("GameTimers.Read"),"once a second, from what is already copied");
            var home=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Home.cs"));
            Check(home.Contains("HomeTiles().Where(tile=>configuration.HomeTilesHidden?.Contains(tile.Target.ToString())!=true)"),"the tiles chosen");
            Check(home.Contains("MeasureMap(\"home today\",DrawHomeToday);"),"on Home");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Reminders.cs")).Contains("KeepHomeTimers(timers);"),"fed by the same copy as the bar");
        }),
    ];
}
