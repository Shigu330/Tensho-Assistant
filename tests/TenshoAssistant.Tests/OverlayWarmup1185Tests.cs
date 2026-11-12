using System.Reflection;
using System.Text.RegularExpressions;
using TenshoAssistant.Diagnostics;

// 0.1185 (X-01 C2.1, analysis of the slow frames of 12/11): the overlays' methods are compiled in the warm-up's early phase with the map's,
// so their first frame after a load finds them compiled; every method of Plugin's overlay table is covered (a new overlay fails here).
internal static class OverlayWarmup1185Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class MainWindow
    {
        public void DrawGatheringPanel() { }
        public void DrawTrackerContents() { }
        public void WearBarVisible() { }
        public void DrawSettingsPage() { }
        public Action Lambda() => () => DrawGatheringPanel();
    }
    private sealed class PanelWindow { public void Draw() { } }
    private sealed class Other { public void DrawGatheringPanel() { } }

    private static MethodBase Of(Type type, string name) => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public)!;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Overlay warm-up 1185: overlay, panel and bar methods of the window are early; a page's method and another type's are not", () =>
        {
            Check(JitWarmup.OverlayFirst(Of(typeof(MainWindow), "DrawGatheringPanel")), "an overlay of the table");
            Check(JitWarmup.OverlayFirst(Of(typeof(MainWindow), "DrawTrackerContents")) && JitWarmup.OverlayFirst(Of(typeof(MainWindow), "WearBarVisible")), "the tracker and a bar");
            Check(JitWarmup.OverlayFirst(Of(typeof(PanelWindow), "Draw")), "the free panels' window");
            Check(!JitWarmup.OverlayFirst(Of(typeof(MainWindow), "DrawSettingsPage")), "a page is not an overlay");
            Check(!JitWarmup.OverlayFirst(Of(typeof(Other), "DrawGatheringPanel")), "another type is not the window");
            var lambda = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Concat(typeof(MainWindow).GetNestedTypes(BindingFlags.NonPublic)
                .SelectMany(nested => nested.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))).FirstOrDefault(method => method.Name.Contains("<Lambda>", StringComparison.Ordinal));
            Check(lambda is not null && JitWarmup.EarlyMethod(Of(typeof(MainWindow), "DrawGatheringPanel")), "early phase covers overlays");
            var methods = new[] { Of(typeof(MainWindow), "DrawSettingsPage"), Of(typeof(MainWindow), "DrawGatheringPanel"), Of(typeof(Other), "DrawGatheringPanel") };
            Check(JitWarmup.EarlyFirst(methods, JitWarmup.EarlyMethod).First() == methods[1], "overlays first, none lost");
        }),
        ("Overlay warm-up 1185: every method of Plugin's overlay table is in the early list, and the window starts the warm-up with it", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var named = Regex.Matches(plugin, @"\(""[A-Za-z]+"",mainWindow\.([A-Za-z]+)\)").Select(match => match.Groups[1].Value).Distinct().ToArray();
            Check(named.Length >= 25, "overlays read: " + named.Length);
            var missing = named.Where(name => !JitWarmup.OverlayMethods.Contains(name)).ToArray();
            Check(missing.Length == 0, "overlays missing from JitWarmup.OverlayMethods: " + string.Join(", ", missing));
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.cs")).Contains("Diagnostics.JitWarmup.EarlyMethod);", StringComparison.Ordinal), "the window's warm-up uses it");
        }),
    ];
}
