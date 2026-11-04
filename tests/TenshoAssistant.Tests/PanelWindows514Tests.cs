// Issue #38: the free-standing panels go through the Dalamud window system, and their settings keep deciding.
internal static class PanelWindows514Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Panels: the four free-standing ones no longer open an ImGui window themselves, and keep their ids",() =>
        {
            foreach(var (file,id) in new[]{("MainWindow.TrackerWindow.cs","###TenshoTracker"),("MainWindow.PersonalTasks.cs","###TenshoTaskPanel"),("MainWindow.GearWear.cs","###TenshoWearBar"),("MainWindow.RegenTick.cs","###TenshoGpTick")})
            {
                var code=Read(file);
                Check(!code.Contains("ImGui.Begin(\"") && !code.Contains("ImGui.Begin(localization") && !code.Contains("ImGui.End()"),file+": no window of its own");
                Check(code.Contains("new(\""+code.Split("new(\"")[1].Split('"')[0]+"\",\""+id+"\""),file+": the id keeps the saved position");
            }
        }),
        ("Panels: added to the window system, the setting copied before each draw, the close button written back at once",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("panelWindows = mainWindow.CreatePanelWindows()") && plugin.Contains("windows.AddWindow(panel)") && plugin.Contains("windows.RemoveWindow(panel)"),"owned and cleaned up");
            Check(plugin.IndexOf("panel.Follow();",StringComparison.Ordinal)>=0 && plugin.IndexOf("panel.Follow();",StringComparison.Ordinal)<plugin.IndexOf("windows.Draw();",StringComparison.Ordinal),"followed before the draw");
            foreach(var gone in new[]{"DrawTaskPanel","DrawWearBar","DrawRegenTickBar","DrawTrackerWindow"})Check(!plugin.Contains(gone),"drawn once only: "+gone);
            var window=Read("PanelWindow.cs");
            Check(window.Contains("if (!IsOpen) panel.Close?.Invoke();"),"close written back in PostDraw, or Follow would undo it");
            Check(window.Contains("RespectCloseHotkey = false;"),"Escape does not turn a setting off");
            Check(window.Contains("catch (Exception error) { TurnOff(error); }") && window.Contains("owner.RecordOverlayDraw(panel.Name,"),"fails alone, measured in Diagnostics");
        }),
        ("Panels: the GP measure still runs while the bar is off; closing twice saves once",() =>
        {
            Check(Read("Plugin.cs").Contains("(\"RegenTick\",mainWindow.ObserveRegenTick)"),"every frame");
            Check(Read("MainWindow.RegenTick.cs").IndexOf("measuredBase!=configuration.GpRegenBase",StringComparison.Ordinal)>Read("MainWindow.RegenTick.cs").IndexOf("internal void ObserveRegenTick()",StringComparison.Ordinal),"in the observer");
            Check(Read("MainWindow.TrackerWindow.cs").Contains("if(!configuration.TrackerWindow)return;") && Read("MainWindow.PersonalTasks.cs").Contains("if(!configuration.TaskPanel)return;"),"guarded");
        }),
        ("Windows: the guide and the map write their close button back too",() =>
        {
            Check(Read("GuideWindow.cs").Contains("if (!IsOpen) owner.CloseGuide();"),"guide");
            Check(Read("MapViewWindow.cs").Contains("if (!IsOpen) owner.CloseMapFromGame();"),"map");
        }),
    ];
}
