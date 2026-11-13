// 0.1274: the dashboard's history (0.1269) was called from the first frame after a load, only to return at once, but the call compiled
// its code on the game's thread: the « StateBar » part of the first frame went from about 4 ms to 7 to 11 ms (dalamud.log, loads of
// 0.1269 to 0.1273). Nothing of the dashboard is called before the start delay any more.
internal static class DashboardFirstFrame1274Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Dashboard first frame 1274: before the start delay the tick returns before any dashboard code is called",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            var tick=page[page.IndexOf("internal void TickDashboard()",StringComparison.Ordinal)..];tick=tick[..tick.IndexOf('\n')];
            Check(tick=="internal void TickDashboard(){if(!DashboardStarted)return;if(DashboardWanted)RefreshDashboard();SampleDashboardHistory();}","the tick: "+tick);
            Check(tick.IndexOf("if(!DashboardStarted)return;",StringComparison.Ordinal)<tick.IndexOf("RefreshDashboard()",StringComparison.Ordinal)
                && tick.IndexOf("if(!DashboardStarted)return;",StringComparison.Ordinal)<tick.IndexOf("SampleDashboardHistory()",StringComparison.Ordinal),"the delay is checked first");
            Check(page.Contains("private bool DashboardStarted=>Environment.TickCount64-dashboardFrom>=0;") && page.Contains("private const long DashboardStartDelayMs=20_000;"),"a field compared with the clock: nothing to compile, twenty seconds");
            // The dashboard opened by the player still reads at once (DashboardStart1251Tests), and the history starts with the delay.
            var sample=page[page.IndexOf("private void SampleDashboardHistory()",StringComparison.Ordinal)..];
            Check(sample[..sample.IndexOf("RefreshDashboard();",StringComparison.Ordinal)].Contains("if(!DashboardStarted || !dashboardHistory.Due(now))return;"),"the sampling keeps its own guard (the dashboard may be drawn before the delay)");
        }),
    ];
}
