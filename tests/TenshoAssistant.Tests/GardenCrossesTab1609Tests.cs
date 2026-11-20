// 0.1609 (X-603, user request of 18/11, D-186): the Garden page's crossbreeding planner in its own tab of the page's tab template; nothing
// lost: the planner whole in the tab (seed picker, gives / uses, pairs and plan, source and unmatched lines), the forecasts on the beds kept
// in the Garden tab.
internal static class GardenCrossesTab1609Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden crosses tab 1609: three tabs, Garden, Crossbreeding, Alarms, in the page's tab template",()=>
        {
            var page=Read("MainWindow.Garden.cs");
            var garden=page.IndexOf("(\"Garden\",localization.Get(\"Garden.TabGarden\"),DrawGardenJournal)",StringComparison.Ordinal);
            var crosses=page.IndexOf("(\"Crosses\",localization.Get(\"Garden.Crosses\"),DrawGardenCrosses)",StringComparison.Ordinal);
            var alarms=page.IndexOf("(\"Alarms\",localization.Format(\"Garden.TabAlarms\"",StringComparison.Ordinal);
            Check(page.Contains("DetailTabs(\"GardenTabs\",") && garden>0 && crosses>garden && alarms>crosses,"order "+garden+","+crosses+","+alarms);
            Check(page.Split("DrawGardenCrosses").Length==2,"drawn by its tab only");
        }),
        ("Garden crosses tab 1609: the planner whole in the tab, no folding block; the beds' forecasts stay on the grid",()=>
        {
            var crosses=Read("MainWindow.GardenCrosses.cs");
            var start=crosses.IndexOf("private void DrawGardenCrosses()",StringComparison.Ordinal);
            var end=crosses.IndexOf("private void DrawGardenPairs(",start,StringComparison.Ordinal);
            var body=crosses[start..end];
            Check(!body.Contains("UiComponents.Header("),"no folding block in the tab");
            foreach(var part in new[]{"Garden.CrossesHelp","Garden.CrossesGives","Garden.CrossesUses","##GardenCrossSeed","DrawGardenPairs(data,Name)","Garden.CrossesSource","Garden.CrossesUnmatched","Garden.CrossesLoading","Garden.CrossesUnavailable"})
                Check(body.Contains(part),"kept: "+part);
            var page=Read("MainWindow.Garden.cs");
            Check(page.Contains("var forecasts=GardenGridForecasts(planted);") && page.Contains("Garden.ForecastSource"),"the forecasts on the beds kept");
        }),
    ];
}
