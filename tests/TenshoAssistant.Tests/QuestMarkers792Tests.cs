// 0.792 (X-32): the game's own quest and leve markers, captured to see if they hold the areas while the game map is closed.
internal static class QuestMarkers792Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest markers capture: developer readings only, bounded, nothing used yet",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("private void ReadQuestMarkers()") && capture.Contains("if(count is <0 or >32){text.Append(\" out of range]\");return;}") && capture.Contains("Line(\"questMarkers\",80,now);"),"bounded, limited");
        }),
    ];
}
