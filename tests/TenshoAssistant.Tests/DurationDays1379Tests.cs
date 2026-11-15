using System.Text.RegularExpressions;
using TenshoAssistant.UI;

// X-333 (0.1379, review of 0.1279 to 0.1317, I5; user's request « pas h ou jour »): beyond 24 h a time left or a length says days
// (« 6 j 0 h », never « 144 h 00 »); countdowns to the second say days from a day on; the ratchet of X-312 now also looks at TotalHours.
internal static class DurationDays1379Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
    private static string Format(string key, int first, int second) => $"{key}:{first}:{second}";

    // The only places where TotalHours may still appear, each with its reason; any new one fails (ratchet).
    private static readonly Dictionary<string, (int Count, string Reason)> Allowed = new()
    {
        ["UI/TimeLeft.cs"] = (1, "the countdown's « hh:mm:ss », below a day only"),
        ["Alarms/AlarmDtrEntry.Dalamud.cs"] = (1, "the server bar: hours from one hour, below a day (days first)"),
    };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Duration days 1379: six days are « 6 j 0 h », countdowns say days from a day on", () =>
        {
            Check(TimeLeft.Write(TimeSpan.FromHours(144), Format) == "Common.LeftDays:6:0", "144 h: 6 days 0 hours");
            Check(TimeLeft.Countdown(TimeSpan.FromHours(144), Format) == "Common.LeftDays:6:0", "a countdown of 144 h: days");
            Check(TimeLeft.Countdown(new TimeSpan(2, 3, 4), Format) == "02:03:04", "below a day: hh:mm:ss");
            Check(TimeLeft.Countdown(new TimeSpan(23, 59, 59), Format) == "23:59:59", "just below a day");
            Check(TimeLeft.Countdown(TimeSpan.FromDays(1), Format) == "Common.LeftDays:1:0", "a day");
            Check(TimeLeft.Countdown(TimeSpan.FromSeconds(-3), Format) == "00:00:00", "passed");
        }),
        ("Duration days 1379: the server bar says days from a day on", () =>
        {
            // X-415 M4 (0.1389): through the common text (TimeLeft.Write), no longer TenshoCulture.Days: the same days first, every language.
            string Text(string key) => key switch { "Common.LeftDays" => "{0}d {1}h", "Common.LeftHours" => "{0}h {1:00}", _ => "{0} min" };
            var days = TimeLeft.Remaining(TimeSpan.FromDays(6) + TimeSpan.FromMinutes(5), Text);
            Check(days == "6d 0h", "days: " + days);
            Check(TimeLeft.Remaining(new TimeSpan(2, 15, 30), Text) == "2h 15" && TimeLeft.Remaining(TimeSpan.FromMinutes(45.5), Text) == "45 min"
                && TimeLeft.Remaining(TimeSpan.FromSeconds(20), Text) == "< 1 min", "hours below a day, minutes below an hour, then « < 1 min »");
            Check(Read("src/TenshoAssistant/Alarms/AlarmDtrEntry.Dalamud.cs").Contains("public static string Remaining(TimeSpan left,Func<string,string> text)=>UI.TimeLeft.Remaining(left,text);", StringComparison.Ordinal), "the server bar uses it");
        }),
        ("Duration days 1379: no hour placeholder left in the texts of a time left", () =>
        {
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            var en = Read("src/TenshoAssistant/Localization/Languages/en.json");
            foreach (var key in new[] { "\"TimerLine\": \"{0} : {1}\"", "\"In\": \"Dans {0} ({1})\"", "\"CompanyActionLeft\": \"{0} ({1})\"", "\"Duration\": \"· durée {0}\"",
                "\"CountdownClose\": \"Temps avant la fermeture : {0}\"", "\"TimedOpen\": \"{0}\"", "\"Reset\": \"Prochaine revue : {0} (dans {1})\"" })
                Check(fr.Contains(key, StringComparison.Ordinal), "French: " + key);
            foreach (var key in new[] { "\"TimerLine\": \"{0}: {1}\"", "\"In\": \"In {0} ({1})\"", "\"CompanyActionLeft\": \"{0} ({1})\"", "\"Duration\": \"· duration {0}\"",
                "\"CountdownOpen\": \"Time until opening: {0}\"", "\"TimedClosed\": \"{0}\"", "\"Reset\": \"Next report: {0} (in {1})\"" })
                Check(en.Contains(key, StringComparison.Ordinal), "English: " + key);
            var reminders = Read("src/TenshoAssistant/MainWindow.Reminders.cs");
            Check(reminders.Contains("TimeLeft.SameDay(at,now,TimeZoneInfo.Local) ? \"t\":\"g\"", StringComparison.Ordinal), "the hour alone today");
        }),
        ("Duration days 1379: no TotalHours left in src/TenshoAssistant outside the listed places (ratchet)", () =>
        {
            var source = Path.GetFullPath("src/TenshoAssistant");
            var found = new Dictionary<string, int>();
            foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, file).Replace('\\', '/');
                if (relative.StartsWith("bin/", StringComparison.Ordinal) || relative.StartsWith("obj/", StringComparison.Ordinal)) continue;
                var count = Regex.Matches(File.ReadAllText(file), @"\bTotalHours\b").Count;
                if (count > 0) found[relative] = count;
            }
            foreach (var (file, count) in found)
                Check(Allowed.TryGetValue(file, out var allowed) && count <= allowed.Count, $"{file}: {count} TotalHours; a time left is written by TimeLeft (TimeLeftText, TimeLeft.Countdown, TimeLeft.Write)");
            Check(found.ContainsKey("UI/TimeLeft.cs"), "the scan found the common format (the folder was read)");
        }),
    ];
}
