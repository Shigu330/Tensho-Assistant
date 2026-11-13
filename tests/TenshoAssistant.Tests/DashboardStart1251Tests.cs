// 0.1251 (X-165 follow-up): the dashboard's first reading for the server bar, the panel and the desktop link cost 16 to 17 ms on the first
// frame after every load (« StateBar » in the slow-frame line of 0.1245 to 0.1250, its code compiled on the game's thread). It now waits
// 20 s after the window is built (the JIT warm-up starts at 5 s); the dashboard the player opens reads at once.
internal static class DashboardStart1251Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Dashboard start 1251: no reading in the first 20 s for the bar, the panel and the link; the dashboard opened reads at once",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            Check(page.Contains("private const long DashboardStartDelayMs=20_000;") && page.Contains("private readonly long dashboardFrom=Environment.TickCount64+DashboardStartDelayMs;"),"20 s from the window's creation");
            Check(page.Contains("private bool DashboardStarted=>Environment.TickCount64-dashboardFrom>=0;"),"compared by difference (no overflow, Timers828Tests)");
            Check(page.Contains("internal void TickDashboard(){if(!DashboardStarted)return;if(DashboardWanted)RefreshDashboard();SampleDashboardHistory();}"),"the bar and the link wait");
            Check(page.Contains("if(windowDisposed || !DashboardStarted)return false;"),"the panel waits");
            var draw=page[page.IndexOf("private void DrawDashboard()",StringComparison.Ordinal)..page.IndexOf("private System.Numerics.Vector4 DashboardColour",StringComparison.Ordinal)];
            Check(!draw.Contains("DashboardStarted") && draw.Contains("RefreshDashboard();"),"the dashboard opened by the player reads at once");
            Check(Read("src/TenshoAssistant/Diagnostics/JitWarmup.cs").Contains("public static readonly TimeSpan Delay=TimeSpan.FromSeconds(5);"),"the warm-up starts before the first reading");
        }),
    ];
}
