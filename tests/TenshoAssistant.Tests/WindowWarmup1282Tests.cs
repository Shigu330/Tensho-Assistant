using System.Reflection;
using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1282 (X-01): the main window open at a load (or soon after) drew a first frame of 401 to 412 ms (page Home 182 to 204 ms, dalamud.log of
// 13/11 at 21:14 and 21:21): its code waited for the warm-up's rest, compiled last (26 s). The warm-up now compiles, right after the panels'
// part, the window's frame and its first page with what they call (JitWarmup.WindowFirst, WindowCap).

internal static class Window1282Panel
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Draw(int value) => Window1282Shared.Value(value);
}
internal static class Window1282Frame
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int DrawMain(int value) => Window1282Shared.Value(value) + Window1282Page.Value(value);
}
internal static class Window1282Shared
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Value(int value) => value + 5;
}
internal static class Window1282Page
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Value(int value) => value * 5;
}

internal static class WindowWarmup1282Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private sealed class MainWindow
    {
        public void DrawMain() { }
        public void DrawHomeToday() { Action later = () => DrawMain(); later(); }
        public void DrawSettingsPage() { }
        public void DrawTrackerContents() { }
    }
    private sealed class Other { public void DrawMain() { } }
    private static MethodBase Of(Type type, string name) => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Window warm-up 1282: the window's frame and its first page are in the window part, their lambdas too; another page, a panel or another class are not", () =>
        {
            Check(JitWarmup.WindowFirst(Of(typeof(MainWindow), "DrawMain")) && !JitWarmup.WindowFirst(Of(typeof(MainWindow), "DrawHomeToday")), "the frame; a page is added by the window (0.1283)");
            var lambda = typeof(MainWindow).GetNestedTypes(BindingFlags.NonPublic).SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Concat(typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)).FirstOrDefault(method => method.Name.Contains("<DrawHomeToday>", StringComparison.Ordinal));
            Check(lambda is not null && !JitWarmup.WindowFirst(lambda), "a lambda of a page: the page's own closure takes it");
            Check(!JitWarmup.WindowFirst(Of(typeof(MainWindow), "DrawSettingsPage")), "another page waits for the rest");
            Check(!JitWarmup.WindowFirst(Of(typeof(MainWindow), "DrawTrackerContents")), "a panel is the first part's");
            Check(!JitWarmup.WindowFirst(Of(typeof(Other), "DrawMain")), "not outside the window");
        }),
        ("Window warm-up 1282: every name of the window part is still declared in the window's sources", () =>
        {
            var sources = string.Concat(Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "MainWindow*.cs").Select(File.ReadAllText));
            foreach (var name in JitWarmup.WindowMethods)
                Check(sources.Contains(" " + name + "(", StringComparison.Ordinal), "declared: " + name);
            Check(JitWarmup.WindowCap == 1500 && JitWarmup.FirstCap == 2000, "bounded: panels 2000, window 1500");
        }),
        ("Window warm-up 1282: behaviour: panels' part, then the window's part without what the panels took, then the rest; nothing compiled twice", () =>
        {
            using var warmup = JitWarmup.Start(() => [typeof(Window1282Panel), typeof(Window1282Frame)], TimeSpan.Zero, early: _ => true,
                firstPart: method => method.Name == "Draw", windowPart: method => method.Name == "DrawMain");
            Check(warmup.Wait(TimeSpan.FromSeconds(30)), "the warm-up ends");
            Check(warmup.FirstPartMethods == 2, $"the panel and the shared callee ({warmup.FirstPartMethods})");
            Check(warmup.WindowPartMethods == 2, $"the frame and the page's callee, the shared one left out ({warmup.WindowPartMethods})");
            Check(warmup.Prepared == 4, $"four methods in all, none twice ({warmup.Prepared})");
            Check(warmup.WindowReadyAfter >= warmup.FirstReadyAfter && warmup.FirstReadyAfter >= 0, "the window part after the panels'");
        }),
        ("Window warm-up 1283: behaviour: the window part stops at WindowCap new methods, the panels' ones not counted; the page the window opens on is added", () =>
        {
            Check(JitWarmup.WindowCap == 1500, "1500 new methods at most");
            var source = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/JitWarmup.cs"));
            Check(source.Contains("if(WindowPartMethods>=WindowCap)break;", StringComparison.Ordinal), "counted on the new ones");
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.cs"));
            Check(window.Contains("var firstPage = PageEntryFor(page)?.Draw.Method.MethodHandle;", StringComparison.Ordinal), "the page the window opens on, by its handle");
        }),
        ("Window warm-up 1282: wiring: the window starts its warm-up with the window part after the panels'; the warm-up line says when it was ready", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.cs"));
            var first = window.IndexOf("firstPart: Diagnostics.JitWarmup.OverlayFirst,", StringComparison.Ordinal);
            var second = window.IndexOf("windowPart: method => Diagnostics.JitWarmup.WindowFirst(method) || firstPage is { } shown && method.MethodHandle == shown,", StringComparison.Ordinal);
            Check(first > 0 && second > first, "panels, then the window");
            Check(window.Contains("Window part (X-01, 0.1282): {done.WindowPartMethods} methods ready {done.WindowReadyAfter:F0} ms after the load", StringComparison.Ordinal), "said in the warm-up line");
        }),
    ];
}
