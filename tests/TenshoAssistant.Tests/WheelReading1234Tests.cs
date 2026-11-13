// 0.1234 (X-225 part 1, user's request of 12/11, K-46): aetherial wheels. The sheet gives the hours per grade (20, 45, 70,
// docs/evidence/0.1234-aetherial-wheels.md); when a wheel was set is read from the stand's window first, in developer mode. Tests: the reading looks
// at windows named « Wheel », numbers only; evidence cited; probe listed; no reminder before the reading (never a guessed time).
internal static class WheelReading1234Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Wheel reading 1234: the developer reading dumps the numbers of a « Wheel » window, never its texts",() =>
        {
            var capture=Source("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs");
            Check(capture.Contains("ReadWheelWindows();") && capture.Contains("if(!name.Contains(\"Wheel\",StringComparison.OrdinalIgnoreCase))continue;")
                  && capture.Contains("AppendValues(values,unit,200);"),"numbers only, windows named Wheel");
            Check(capture.Contains("docs/evidence/0.1234-aetherial-wheels.md") && File.Exists(Path.GetFullPath("docs/evidence/0.1234-aetherial-wheels.md")),"evidence");
        }),
        ("Wheel reading 1234: probe listed in both halves; no wheel reminder before the reading",() =>
        {
            Check(Source("tools/README.md").Split("`PROBE_WHEEL`").Length==3,"PROBE_WHEEL in English and French");
            Check(!Source("src/TenshoAssistant/Localization/Languages/en.json").Contains("wheel is primed",StringComparison.OrdinalIgnoreCase),"nothing shown yet");
        }),
    ];
}
