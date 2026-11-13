using TenshoAssistant.Alarms;
using TenshoAssistant.Localization;

// X-295 (0.1296): user request of 14/11 about 0:25 (Alarms › dated alarms, an alarm named « test »): its block was titled « Dated alarms »;
// with several personal alarms they would all bear that title. Each dated alarm is a block of its own titled with its name.
internal static class AlarmDatedTitle1296Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file)).Replace("\r\n", "\n");
    private static readonly string[] Dated = ["Dated alarms", "Alarmes à date"];
    private static TenshoAlarm At(string label, string group = "Alarmes à date") =>
        new() { Label = label, Group = group, Repeat = AlarmRepeat.Once, At = new DateTimeOffset(2026, 12, 5, 21, 0, 0, TimeSpan.Zero) };
    private static string Fallback(TenshoAlarm alarm) => "Alarme à date · " + alarm.At.ToString("yyyy-MM-dd HH:mm");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-295: two dated alarms named « A » and « B » make two blocks titled « A » and « B »", () =>
        {
            var a = At("A"); var b = At("B");
            Check(AlarmGroups.Key(a, Dated) != AlarmGroups.Key(b, Dated), "two blocks");
            Check(AlarmGroups.DatedTitle(a, Fallback) == "A" && AlarmGroups.DatedTitle(b, Fallback) == "B", "titled with their names");
            var groups = new[] { a, b, At("A") }.GroupBy(alarm => AlarmGroups.Key(alarm, Dated)).Count();
            Check(groups == 3, "each dated alarm is a block of its own, even with the same name: " + groups);
        }),
        ("X-295: an empty name falls back to « Dated alarm · » and its date", () =>
        {
            Check(AlarmGroups.DatedTitle(At(""), Fallback) == "Alarme à date · 2026-12-05 21:00", "empty name");
            Check(AlarmGroups.DatedTitle(At("   "), Fallback) == "Alarme à date · 2026-12-05 21:00", "blank name");
            Check(AlarmGroups.DatedTitle(At("  test "), Fallback) == "test", "name trimmed");
        }),
        ("X-295: a dated alarm written in another language is still recognised; the other blocks keep their keys", () =>
        {
            Check(AlarmGroups.IsDated(At("x", "Dated alarms"), Dated) && AlarmGroups.IsDated(At("x"), Dated), "English and French groups");
            Check(AlarmGroups.Key(new TenshoAlarm { ItemId = 5, Group = "Alarmes à date" }, Dated) == "item:5", "an item alarm stays grouped by item");
            Check(AlarmGroups.Key(At("Rappels", "Rappels"), Dated) == "Rappels" && !AlarmGroups.IsDated(At("Rappels", "Rappels"), Dated), "another group stays one block");
            Check(AlarmGroups.Key(new TenshoAlarm { Label = "Fleur · 08:00 ET" }, Dated) == "Fleur", "a daily ET alarm stays grouped by its name");
        }),
        ("X-295: the group text is known in every language (stored alarms of another language)", () =>
        {
            var texts = new LocalizationManager((error, message) => { }, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).Translations("WorldClocks.Group");
            Check(texts.Contains("Dated alarms") && texts.Contains("Alarmes à date"), "English and French: " + string.Join(" | ", texts));
        }),
        ("X-295: the Alarms page groups and titles through AlarmGroups; a new dated alarm without a name gets the fallback", () =>
        {
            var alarms = Read("MainWindow.Alarms.cs");
            Check(alarms.Contains("private string AlarmGroupKey(TenshoAlarm alarm)=>AlarmGroups.Key(alarm,DatedAlarmGroups);"), "one key");
            Check(alarms.Contains(":AlarmGroups.IsDated(first,DatedAlarmGroups) ? AlarmGroups.DatedTitle(first,DatedAlarmFallback)"), "dated title");
            Check(Read("MainWindow.WorldClocks.cs").Contains("alarm.Label=alarmManualLabel.Trim().Length>0 ? alarmManualLabel.Trim():DatedAlarmFallback(alarm);"), "fallback name at creation");
        }),
    ];
}
