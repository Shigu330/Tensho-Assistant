// 0.881 (X-75.7): new developer-mode probes, numbers only.
internal static class Probes881Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Probes 0.881: windows' numbers only (no text), on change, bounded; fashion and island animals on change",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            var windows=capture[capture.IndexOf("private void ReadWindows()",StringComparison.Ordinal)..capture.IndexOf("private void ReadFashion()",StringComparison.Ordinal)];
            Check(!windows.Contains("AtkValueType.String"),"no text from windows");
            Check(windows.Contains("Math.Min((int)unit->AtkValuesCount,96)") && windows.Contains("if(windowValues.GetValueOrDefault(name)==all)continue;"),"bounded, on change");
            Check(capture.Contains("if(all==fashionSeen)return;") && capture.Contains("if(all==islandObjects)return;"),"on change");
        }),
    ];
}
