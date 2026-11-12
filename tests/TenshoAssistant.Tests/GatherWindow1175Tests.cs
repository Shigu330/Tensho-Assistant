// 0.1175 (X-134, next step): the gathering window's numbers written to the log in developer mode (up to 200; no text, rule of 0.881), to find which one
// marks the item gathered only once (« RARE »), before Tensho stops proposing GP for its quantity.
internal static class GatherWindow1175Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gather window 1175: « Gathering » among the windows read in developer mode, its numbers up to 200", () =>
        {
            var capture = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("\"Materialize\",\"LetterViewer\",\"Gathering\"];", StringComparison.Ordinal), "the window is read");
            Check(capture.Contains("AppendValues(values,unit,name==\"Gathering\" ? 200:96);", StringComparison.Ordinal), "its numbers, up to 200, never its texts");
            Check(capture.Contains("Line(\"addons\",160,$\"{name}:{all}\",\"addons/\"+name);", StringComparison.Ordinal), "a line only when the values change, bounded per window");
        }),
    ];
}
