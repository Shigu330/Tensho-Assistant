// 0.842 (X-67): the game map's own markers with flags and second icon, in the developer capture.
internal static class MapMarkers842Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map markers capture: flags and second icon, bounded, on change",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("var count=Math.Min((int)agent->MapMarkerCount,agent->MapMarkers.Length);") && capture.Contains("shown<60"),"bounded");
            Check(capture.Contains("if(now==mapMarkers)return;"),"on change");
        }),
    ];
}
