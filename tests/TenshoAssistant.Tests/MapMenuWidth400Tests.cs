using System.Text.RegularExpressions;

// 0.400 (user report: right-click map menu, tabs cut): the lists are as wide as their tab names (at most 90 % of the screen), the tabs
// scroll instead of being cut, and the width list names exactly the tabs drawn.
internal static class MapMenuWidth400Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map menu lists: width from the tab names, scrolling tabs, same tabs in the width list and in the bar",() =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapLocalLists.cs"));
            Check(code.Contains("ImGui.BeginTabBar(\"MapLocalTabs\",ImGuiTabBarFlags.FittingPolicyScroll)",StringComparison.Ordinal),"scrolling tabs");
            var drawn = Regex.Matches(code,@"BeginTabItem\(localization\.Get\(""([^""]+)""\)\)").Select(match=>match.Groups[1].Value).ToArray();
            var listed = Regex.Match(code,@"var tabs=new\[\]\{([^}]*)\}").Groups[1].Value.Split(',').Select(key=>key.Trim().Trim('"')).ToArray();
            Check(drawn.Length==8 && drawn.SequenceEqual(listed) && !drawn.Contains("MapLocal.Sightseeing"),"width list = tabs drawn");
        }),
    ];
}
