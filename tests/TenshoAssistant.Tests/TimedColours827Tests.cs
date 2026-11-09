// 0.827 (X-59, user request): the timed points' timer alone, green or red.
internal static class TimedColours827Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Timed colours: green open, red closed, the words only on hover",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("ImGui.TextColored(state.Open ? Success:UI.FfxivTheme.Error,state.Text);"),"colours");
            var french=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(french.Contains("\"TimedOpen\": \"{0}:{1:00}:{2:00}\"") && french.Contains("\"TimedClosedHint\""),"timer alone");
        }),
    ];
}
