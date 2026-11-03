// 0.426 (user request): Tensho closes once it has opened a game window (crafting / gathering log, game map from a page); a setting.
internal static class CloseOnGameWindow426Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Close on game window: after a successful opening only, on by default, not for the flag of « Go there »",() =>
        {
            var logs=Read("MainWindow.GameLogs.cs");
            Check(logs.Contains("private void CloseForGameWindow(){if(configuration.CloseOnGameWindow)IsOpen=false;}")
                && logs.Contains("if(result==GameLogOpen.Opened)CloseForGameWindow();"),"only when the game function ran");
            var maps=Read("MainWindow.Maps.cs");
            Check(maps.Contains("OpenMapPoint(map,x,y);if(mapOpenError is null)CloseForGameWindow();return;") && maps.Split("CloseForGameWindow").Length==2,
                "game map opened from a page; the flag set by « Go there » keeps Tensho open");
            var configuration=Read("Configuration.cs");
            Check(configuration.Contains("public bool CloseOnGameWindow { get; set; } = true;") && configuration.Contains("\"CloseOnGameWindow\","),"setting on by default, listed");
        }),
    ];
}
