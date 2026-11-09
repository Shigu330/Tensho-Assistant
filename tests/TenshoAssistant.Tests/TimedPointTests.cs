// Test audit of 2026-11-09, batch 2 (0.1021): TimedCountdown754Tests, TimedColours827Tests merged into TimedPointTests; every case is kept.
internal static class TimedPointTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From TimedCountdown754Tests: 0.754 (X-08, user report): a closed timed point says when it opens.
        ("Timed point: the map tooltip counts down to the opening, or to the closing",() =>
        {
            var map=Read("MainWindow.MapView.cs");
            var tip=map[map.IndexOf("private void DrawPointAvailability")..];
            tip=tip[..tip.IndexOf("if(access is null)return;")];
            // 0.906 (X-76): the countdown text is the shared WindowCountdown of the Gathering page.
            Check(tip.Contains("GatheringTime.Next(point.Schedule,now,1)") && tip.Contains("WindowCountdown(next.At(now))"),"countdown");
            var clock=Read("MainWindow.GatheringTime.cs");
            var helper=clock[clock.IndexOf("private string WindowCountdown(")..];
            Check(helper[..helper.IndexOf(';')].Contains("\"LocalClock.CountdownOpen\""),"countdown text");
        }),
        // From TimedColours827Tests: 0.827 (X-59, user request): the timed points' timer alone, green or red.
        ("Timed colours: green open, red closed, the words only on hover",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("ImGui.TextColored(state.Open ? Success:UI.FfxivTheme.Error,state.Text);"),"colours");
            var french=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(french.Contains("\"TimedOpen\": \"{0}:{1:00}:{2:00}\"") && french.Contains("\"TimedClosedHint\""),"timer alone");
        }),
    ];
}
