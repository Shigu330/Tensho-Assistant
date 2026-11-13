using TenshoAssistant.Diagnostics;

// 0.1270 (X-264, slice 2): in game the dashboard's curves stay behind a click: the gauge clicked shows its last hour under the gauges.
internal static class Curves1270Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Curves 1270: the history says the time it covers",() =>
        {
            var history=new HealthHistory();
            Check(history.SpanMs==0,"empty");
            history.Add(new(5_000,HealthLevel.Ok,null,null,null,null,0,0,0,0,0));
            Check(history.SpanMs==0,"one point covers no time");
            history.Add(new(15_000,HealthLevel.Ok,null,null,null,null,0,0,0,0,0));history.Add(new(65_000,HealthLevel.Ok,null,null,null,null,0,0,0,0,0));
            Check(history.SpanMs==60_000,"from the oldest point kept to the last");
        }),
        ("Curves 1270: in game a curve is drawn only for the gauge clicked, inside the opened dashboard",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            var gauges=page[page.IndexOf("private void DrawDashboardGauges(",StringComparison.Ordinal)..];
            Check(gauges.Contains("if(dashboardCurve is { } chosen)") && gauges.Split("CurvePlot(").Length==2,"one curve at most, the chosen one");
            Check(gauges.Contains("else ImGui.TextDisabled(localization.Get(\"Dashboard.CurveHint\"));"),"the way to the curves is said");
            Check(gauges.Contains("if(history.Count<2)ImGui.TextDisabled(localization.Get(\"Dashboard.CurveEmpty\"));"),"no curve from a single point");
            foreach(var name in new[]{"Memory","Page","Frame","Requests"})
                Check(gauges.Contains($"dashboardCurve=dashboardCurve==\"{name}\" ? null:\"{name}\";"),name+": a click shows, a second click hides");
            Check(gauges.Contains("history.PerMinute(sample=>sample.Requests),rateTop"),"requests: the rate per minute, on the gauge's own scale");
            var component=Read("src/TenshoAssistant/UI/Components/Gauges.cs");
            var plot=component[component.IndexOf("private void CurvePlot(",StringComparison.Ordinal)..component.IndexOf("private static int GaugesPerLine()",StringComparison.Ordinal)];
            Check(plot.Contains("GaugeMath.Curve(values,min,max,top)") && plot.Contains("TipText(localization.Format(\"Dashboard.CurveSummary\""),"the pure layout, and the figures on hover");
            Check(!plot.Contains("new Vector4("),"theme colours only");
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                foreach(var key in new[]{"CurveHint","CurveEmpty","CurveAgo","CurveNow","CurveSummary"})Check(text.Contains("\n    \""+key+"\": \""),$"{file}: Dashboard.{key}");
            }
        }),
    ];
}
