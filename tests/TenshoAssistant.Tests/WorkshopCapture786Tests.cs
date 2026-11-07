// 0.786 (X-28): the workshop's delivery window, captured in developer mode before anything is counted.
internal static class WorkshopCapture786Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Workshop capture: read in the developer readings, nothing counted yet",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("private void ReadWorkshop()") && capture.Contains("\"SubmarinePartsMenu\"") && capture.Contains("Line(\"workshop\",60,now);"),"captured, limited");
            Check(!File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs")).Contains("Delivered"),"not counted before the layout is known");
        }),
    ];
}
