// 0.754 (X-08, user report): a closed timed point says when it opens.
internal static class TimedCountdown754Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Timed point: the map tooltip counts down to the opening, or to the closing",() =>
        {
            var map=Read("MainWindow.MapView.cs");
            var tip=map[map.IndexOf("private void DrawPointAvailability")..];
            tip=tip[..tip.IndexOf("if(access is null)return;")];
            Check(tip.Contains("GatheringTime.Next(point.Schedule,now,1)") && tip.Contains("\"LocalClock.CountdownOpen\""),"countdown");
        }),
    ];
}
