// X-86 (0.966): locked quest markers can be hidden, from the map settings and the right-click menu; on by default.
internal static class LockedQuests966Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Locked quests 966: setting on by default, filter on the markers, box in the menu and the settings",() =>
        {
            Check(new TenshoAssistant.MapViewSettings().ShowLockedQuests,"on by default");
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("if(settings.ShowLockedQuests || mapMarkers?.QuestIcons.TryGetValue(marker.Icon,out var questIcon)!=true || !questIcon.Locked)"),"filter");
            Check(System.Text.RegularExpressions.Regex.Matches(map,"SettingBox\\(\"MapView.ShowLockedQuests\"").Count==2,"menu and settings");
        }),
    ];
}
