using System.Text.RegularExpressions;
using TenshoAssistant.Alarms;
using TenshoAssistant.UI;

// X-312 (0.1316): master's finding of 14/11 (test T-0.1241-2, Eureka and Bozja › Weather and night): other pages still wrote a delay in minutes
// only (« in 1440 min »), as the alarms did before X-279. Every delay goes through the common format (TimeLeft: « 17 d 2 h », « 3 h 05 »,
// « 45 min »); the ratchet of X-279 is extended to all of src/TenshoAssistant.
internal static class DurationText1316Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Repo()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "TenshoAssistant.sln")) && !Directory.Exists(Path.Combine(folder.FullName, "src", "TenshoAssistant")))
            folder = folder.Parent;
        return folder?.FullName ?? Path.GetFullPath(".");
    }

    // The only places where TotalMinutes may still appear, each with its reason; any new one fails (ratchet).
    private static readonly Dictionary<string, (int Count, string Reason)> Allowed = new()
    {
        ["UI/TimeLeft.cs"] = (3, "the common format itself"),
        ["Alarms/AlarmService.Dalamud.cs"] = (1, "the minutes left handed to the chat line and Discord, written there by TimeLeft.Write"),
        ["Alarms/AlarmDtrEntry.Dalamud.cs"] = (2, "the server info bar: hours from one hour, minutes only below"),
        ["MainWindow.RegenTick.cs"] = (1, "GP full in m:ss, always under an hour (the regeneration of a full bar)"),
    };

    // Texts with a number of minutes still allowed: the common format's own key, and the health curve (360 samples of 10 s: one hour at most).
    private static readonly string[] MinutesKeys = ["LeftMinutes", "CurveAgo"];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-312: rounded up to the minute, rounded to the nearest, and written through a formatter", () =>
        {
            Check(TimeLeft.UpToMinute(TimeSpan.FromSeconds(30)) == TimeSpan.FromMinutes(1), "30 s: 1 min, never « 0 min » while time is left");
            Check(TimeLeft.UpToMinute(TimeSpan.FromMinutes(90)) == TimeSpan.FromMinutes(90), "a whole minute stays");
            Check(TimeLeft.UpToMinute(TimeSpan.FromSeconds(-5)) == TimeSpan.Zero, "passed: zero");
            Check(TimeLeft.NearestMinute(TimeSpan.FromSeconds(89)) == TimeSpan.FromMinutes(1) && TimeLeft.NearestMinute(TimeSpan.FromSeconds(91)) == TimeSpan.FromMinutes(2), "nearest");
            string Format(string key, int first, int second) => $"{key}:{first}:{second}";
            Check(TimeLeft.Write(TimeSpan.FromMinutes(1440), Format) == "Common.LeftDays:1:0", "1440 min: one day");
            Check(TimeLeft.Write(TimeSpan.FromMinutes(90), Format) == "Common.LeftHours:1:30", "90 min: 1 h 30");
            Check(TimeLeft.Write(TimeSpan.FromMinutes(45), Format) == "Common.LeftMinutes:45:0", "45 min stays in minutes");
        }),
        ("X-312: the Discord marker [left] takes the common text; [minutes] stays the number for the players' own messages", () =>
        {
            var settings = new DiscordSettings();
            var at = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.FromHours(1));
            Check(DiscordWebhook.Format(settings, "[label] in [left] ([minutes])", null, "A", 90, at, null, left: "1 h 30") == "A in 1 h 30 (90)", "left given");
            Check(DiscordWebhook.Format(settings, "[label] in [left]", null, "B", 7, at, null) == "B in 7 min", "left not given: the minutes and « min »");
        }),
        ("X-312: no TotalMinutes left in src/TenshoAssistant outside the listed places (ratchet of X-279, extended)", () =>
        {
            var source = Path.Combine(Repo(), "src", "TenshoAssistant");
            var found = new Dictionary<string, int>();
            foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, file).Replace('\\', '/');
                if (relative.StartsWith("bin/", StringComparison.Ordinal) || relative.StartsWith("obj/", StringComparison.Ordinal)) continue;
                var count = Regex.Matches(File.ReadAllText(file), @"\bTotalMinutes\b").Count;
                if (count > 0) found[relative] = count;
            }
            foreach (var (file, count) in found)
                Check(Allowed.TryGetValue(file, out var allowed) && count <= allowed.Count, $"{file}: {count} TotalMinutes; a delay is written by TimeLeft (TimeLeftText, TimeLeftUp, MinutesText, TimeLeft.Write)");
            Check(found.Count >= 1 && found.ContainsKey("UI/TimeLeft.cs"), "the scan found the common format (the folder was read)");
        }),
        ("X-312: no English or French text writes a number of minutes, except the common format and the hour-long health curve", () =>
        {
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.Combine(Repo(), "src", "TenshoAssistant", "Localization", "Languages", language + ".json"));
                var keys = Regex.Matches(text, @"^\s*""([^""]+)"": ""((?:[^""\\]|\\.)*\{\d+(?::[^}]*)?\} ?min\b(?:[^""\\]|\\.)*)""", RegexOptions.Multiline)
                    .Select(match => match.Groups[1].Value).ToArray();
                var extra = keys.Where(key => !MinutesKeys.Contains(key)).ToArray();
                Check(extra.Length == 0, $"{language}: minutes written by hand in {string.Join(", ", extra)}");
                Check(keys.Contains("LeftMinutes"), $"{language}: the scan found the common key");
                Check(text.Contains("[left]", StringComparison.Ordinal) && !text.Contains("[minutes] min", StringComparison.Ordinal), $"{language}: the Discord default message takes [left]");
            }
        }),
        ("X-312: the pages listed by the finding go through the common format", () =>
        {
            var source = Path.Combine(Repo(), "src", "TenshoAssistant");
            string Read(string file) => File.ReadAllText(Path.Combine(source, file));
            Check(Read("MainWindow.EurekaNotorious.cs").Contains("TimeLeftUp(window.Start-now)"), "Eureka notorious");
            Check(Read("MainWindow.MiniCactpot.cs").Contains("TimeLeftUp(gate-now)"), "GATE");
            Check(Read("MainWindow.Home.cs").Contains("TimeLeftText(next.At-DateTimeOffset.UtcNow)"), "Home");
            Check(Read("MainWindow.Weather.cs").Contains("TimeLeftText(entry.StartsAt-now)"), "Weather");
            Check(Read("MainWindow.DesktopData.cs").Contains("TimeLeftUp(item.Occurrence-now)"), "desktop alarm line");
            Check(Read("Plugin.cs").Contains("UI.TimeLeft.Write(TimeSpan.FromMinutes(Math.Max(0,ringMinutes))"), "the alarm's chat line");
        }),
    ];
}
