using TenshoAssistant.Diagnostics;

// 0.1245 (X-165, slice 2): Tensho's state in the server bar, for every player, off by default; « only when something is wrong » hides it
// while all is fine. The measures stay on the draw thread (the main window publishes a snapshot once a second, only while the entry is on);
// the entry reads it on the framework thread, is cut in competitive content, and its click opens the settings where the dashboard is shown.
internal static class StateBar1245Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("State bar 1245: shown once read; « only when something is wrong » hides it while all is fine",() =>
        {
            Check(!StateBar.Shown(null,false) && !StateBar.Shown(null,true),"nothing read yet: hidden");
            Check(StateBar.Shown(HealthLevel.Ok,false) && StateBar.Shown(HealthLevel.Watch,false) && StateBar.Shown(HealthLevel.Problem,false),"always");
            Check(!StateBar.Shown(HealthLevel.Ok,true) && StateBar.Shown(HealthLevel.Watch,true) && StateBar.Shown(HealthLevel.Problem,true),"only when something is wrong");
            var defaults=new TenshoAssistant.Configuration();
            Check(!defaults.StateServerBar && !defaults.StateServerBarOnlyProblems,"off by default");
        }),
        ("State bar 1245: snapshot published from the draw, read by the entry; cut in competitive content; settings, texts",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            Check(page.Contains("internal void TickDashboard(){if(DashboardWanted)RefreshDashboard();}") && page.Contains("DashboardWanted=>configuration.StateServerBar || "),"measures read only while the entry (or, since 0.1246, the panel or the link) is on");
            Check(page.Contains("Volatile.Write(ref dashboardSnapshot,") && page.Contains("internal DashboardSnapshot? DashboardState=>Volatile.Read(ref dashboardSnapshot);"),"published whole, read whole");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("(\"StateBar\",()=>{mainWindow.TickDashboard();stateBar?.Invoke()?.DrawTooltip();}),"),"ticked by the plugin's draw");
            var entry=Read("src/TenshoAssistant/Alarms/StateDtrEntry.Dalamud.cs");
            Check(entry.Contains("if(CompetitiveGate.Cut){entry.Shown=false;return;}") && entry.Contains("CompetitiveGate.RemoveAlways(framework,OnUpdate);") && entry.Contains("entry.Remove();"),"cut and removed like the others");
            Check(entry.Contains("var snapshot=configuration.StateServerBar ? state():null;") && entry.Contains("entry.Shown=StateBar.Shown(snapshot?.Overall,configuration.StateServerBarOnlyProblems);"),"shown by the rule");
            Check(Read("src/TenshoAssistant/Plugin.ServerBar.cs").Contains("()=>mainWindow.DashboardState,localization.Get,localization.Format,mainWindow.OpenSettings)"),"in the server bar family, click opens the settings");
            var advanced=Read("src/TenshoAssistant/MainWindow.Advanced.cs");
            Check(advanced.Contains("BoolSetting(\"Advanced.StateServerBar\",") && advanced.Contains("BoolSetting(\"Advanced.StateServerBarProblems\",") && advanced.Contains("                DrawDashboard();"),"settings and the dashboard for every player");
            Check(Read("src/TenshoAssistant/Configuration.cs").Contains("\"PlaceServerBar\", \"StateServerBar\", \"StateServerBarOnlyProblems\", "),"reset with the other settings");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach(var key in new[]{"StateServerBar","StateServerBarTip","StateServerBarProblems","StateServerBarProblemsTip","Bar","BarNoMemory","BarOk","BarWatch","BarProblem"})
                Check(en.Contains($"    \"{key}\": ") && fr.Contains($"    \"{key}\": "),"text: "+key);
            Check(en.Contains("\\nDefault: off.\",\n    \"StateServerBarProblems\"") && fr.Contains("\\nPar défaut : désactivé.\",\n    \"StateServerBarProblems\""),"defaults said");
        }),
    ];
}
