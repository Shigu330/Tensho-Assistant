// 0.825 (X-57): the game map's markers with their place, captured in developer mode.
internal static class EventMarkers825Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Event markers capture: bounded, written when it changes",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("private void ReadEventMarkers()") && capture.Contains("if(count is <0 or >256)return;") && capture.Contains("Line(\"eventMarkers\",60,now);"),"bounded");
        }),
    ];
}
