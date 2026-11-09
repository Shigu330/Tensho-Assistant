// 0.368 (g, C-15): achievement progress read passively, asked only on a click (address checked, 3 s apart), tracked list kept on reset.
internal static class AchievementProgress368Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Achievement progress: passive read, request on click only with its address checked, tracked list kept",() =>
        {
            var watcher = Read("Collections/AchievementProgressWatcher.Dalamud.cs");
            Check(watcher.Contains("(nint)Achievement.MemberFunctionPointers.RequestAchievementProgress!=0",StringComparison.Ordinal)
                && watcher.Contains("TimeSpan.FromSeconds(3)",StringComparison.Ordinal),"request guarded and spaced");
            Check(watcher.Contains("if(player.ContentId!=character){character=player.ContentId;progress=all.TryGetValue(character,out var saved) ? new(saved):[];}",StringComparison.Ordinal),"per character (0.987: that character's saved progress)");
            var page = Read("MainWindow.AchievementProgress.cs");
            Check(page.Contains("ImGui.SmallButton(localization.Get(\"AchievementProgress.Ask\")",StringComparison.Ordinal)
                && Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories)
                    .Sum(file => System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file),@"AchievementProgressSource\?\.Request\(").Count) == 1,"the server is asked from one button only");
            Check(Read("Configuration.cs").Contains("\"TrackedAchievements\",",StringComparison.Ordinal),"tracked list kept on reset");
        }),
    ];
}
