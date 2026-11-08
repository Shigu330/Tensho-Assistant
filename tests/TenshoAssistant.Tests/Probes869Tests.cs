// 0.869 (X-73.13): developer-mode readings for fishing, aetherial reduction and hand-ins, before any parser relies on them.
internal static class Probes869Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Probes: fishing line, reduction and hand-in read in the developer capture only, bounded",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("if(job==18)ReadFishingLine(local);") && capture.Contains("events->EventHandlerModule.FishingEventHandler"),"fishing line");
            Check(capture.Contains("Line(\"fishline\",400,") && capture.Contains("Line(\"purify\",60,") && capture.Contains("Line(\"npctrade\",60,"),"bounded");
            Check(capture.Contains("if(disposed || !enabled())return;"),"developer mode only");
        }),
    ];
}
