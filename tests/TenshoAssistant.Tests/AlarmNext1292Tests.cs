using TenshoAssistant.UI;

// X-279 (0.1292): user report of 13/11 about 21:55 (Alarms › Alarms at a date): « next 00:00 (in 24631 min) », the hour alone for an alarm of
// 1/12. The time left is written in the common format (Common.LeftDays / LeftHours / LeftMinutes), with the date when not today.
internal static class AlarmNext1292Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Root(string file) => File.ReadAllText(Path.GetFullPath(file));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-279: 24631 minutes are 17 days 2 hours, written with the days key; hours and minutes below a day", () =>
        {
            Check(TimeLeft.Split(TimeSpan.FromMinutes(24631)) == (17, 2, 31), "17 d 2 h 31");
            Check(TimeLeft.Text(TimeLeft.Split(TimeSpan.FromMinutes(24631))) == ("Common.LeftDays", 17, 2), "days key");
            Check(TimeLeft.Text(TimeLeft.Split(TimeSpan.FromMinutes(185))) == ("Common.LeftHours", 3, 5), "3 h 05");
            Check(TimeLeft.Text(TimeLeft.Split(TimeSpan.FromSeconds(2730))) == ("Common.LeftMinutes", 45, 0), "45 min");
            Check(TimeLeft.Split(TimeSpan.FromMinutes(-3)) == (0, 0, 0), "passed: zero, never negative");
        }),
        ("X-279: the date is given when the ringing is not today (time zone of the PC)", () =>
        {
            var paris = TimeZoneInfo.CreateCustomTimeZone("Test+1", TimeSpan.FromHours(1), "Test+1", "Test+1");
            var now = new DateTimeOffset(2026, 11, 13, 21, 55, 0, TimeSpan.FromHours(1));
            Check(TimeLeft.SameDay(now.AddHours(1), now, paris), "22:55 the same day");
            Check(!TimeLeft.SameDay(new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.FromHours(1)), now, paris), "1/12: another day");
            Check(!TimeLeft.SameDay(now.AddHours(2.1), now, paris), "past midnight: another day");
        }),
        ("X-279: the alarm pages write no minutes by hand, and the texts take the common time left", () =>
        {
            var page = Root("src/TenshoAssistant/MainWindow.Alarms.cs");
            Check(!page.Contains("TotalMinutes") && page.Split("AlarmNextText(").Length - 1 == 3, "no TotalMinutes, one helper used twice");
            Check(!Root("src/TenshoAssistant/MainWindow.AlarmLinks.cs").Contains("TotalMinutes"), "links page");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Root($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(text.Contains(language == "en" ? "\"Next\": \"next {0} (in {1})\"" : "\"Next\": \"prochaine {0} (dans {1})\""), language + ": no « min » after the time left");
                Check(text.Contains("\"LeftDays\": \"{0}") && text.Contains("\"LeftHours\": \"{0}"), language + ": the common keys");
            }
        }),
    ];
}
