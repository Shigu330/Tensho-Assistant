// 0.361 (g, C-11): the whole sightseeing log; « possible now » never claims a weather it does not know.
internal static class VistaLog361Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sightseeing log: every zone, unknown weather kept as « time only », rebuilt at most once a minute",() =>
        {
            var page = Read("MainWindow.VistaLog.cs");
            Check(page.Contains("index.All(MapLocalKind.Sightseeing)",StringComparison.Ordinal),"every vista");
            // 0.1128 (X-158): unknown weather is VistaWindow's TimeOnly (VistaWindow1128Tests), kept by the filter.
            Check(page.Contains("row.Now.State!=VistaOpen.Closed",StringComparison.Ordinal),"unknown weather is not « impossible »");
            Check(page.Contains("var minute=DateTimeOffset.UtcNow.ToUnixTimeSeconds()/60;",StringComparison.Ordinal)
                && page.Contains("if(!Equals(key,vistaLogKey))",StringComparison.Ordinal),"rows cached by sources and minute");
            Check(Read("MainWindow.MapLocalLists.cs").Contains("DrawVistaLog(live)",StringComparison.Ordinal),"tab of the map lists");
        }),
    ];
}
