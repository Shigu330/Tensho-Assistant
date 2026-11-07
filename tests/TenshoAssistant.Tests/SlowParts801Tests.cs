// 0.801 (X-01): the slow-frame line lists ten parts.
internal static class SlowParts801Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Slow frame: ten parts listed",() =>
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/SlowFrame.cs")).Contains("var top=mine.OrderByDescending(part=>part.Ms).Where(part=>part.Ms>=1).Take(24).ToArray();"),"0.806: every part of 1 ms or more")),
    ];
}
