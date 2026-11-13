using System.Text.Json;
using TenshoAssistant.Alarms;

// X-280 (0.1288): « Edit » on an alarm of « Alarms at a date » did not offer its time: only one-off alarms had the date field, and the
// form also makes daily and weekly ones. Behaviour tests: edit, then read the saved alarm back (same alarm, new time, no copy, nothing lost).
internal static class AlarmEdit1288Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Now = new(2026, 11, 13, 21, 55, 0, TimeSpan.Zero);

    private static List<TenshoAlarm> SaveAndLoad(List<TenshoAlarm> alarms) =>
        JsonSerializer.Deserialize<List<TenshoAlarm>>(JsonSerializer.Serialize(alarms)) ?? throw new Exception("not read back");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-280: a daily alarm's time is changed in place and read back from the save", () =>
        {
            var daily = new TenshoAlarm { Label = "Raid", Group = "Alarms at a date", Repeat = AlarmRepeat.RealDaily, At = Now.AddDays(-3).AddHours(-1), LeadMinutes = 5,
                LastOccurrence = Now.AddHours(-1), LastRung = Now.AddHours(-1), Discord = true, DiscordMessage = "go" };
            var other = new TenshoAlarm { Label = "Other", Repeat = AlarmRepeat.Once, At = Now.AddHours(5) };
            var alarms = new List<TenshoAlarm> { daily, other };
            Check(AlarmSchedule.DateEditable(daily.Repeat), "editable");
            var shown = AlarmSchedule.EditShown(daily, Now);
            Check(shown > Now && shown == AlarmSchedule.Next(daily, Now), "the field starts at its next ringing, a future date");
            var moved = Now.AddDays(1).Date.AddHours(19).AddMinutes(30);
            Check(AlarmSchedule.Move(daily, new DateTimeOffset(moved, TimeSpan.Zero)), "moved");
            var loaded = SaveAndLoad(alarms);
            Check(loaded.Count == 2 && loaded.Count(alarm => alarm.Id == daily.Id) == 1, "same alarm, no copy");
            var back = loaded.Single(alarm => alarm.Id == daily.Id);
            Check(back.Repeat == AlarmRepeat.RealDaily && back.Label == "Raid" && back.Discord && back.DiscordMessage == "go" && back.LeadMinutes == 5, "nothing else lost");
            Check(back.LastOccurrence == default && back.LastRung == default, "rings at the new time");
            var next = AlarmSchedule.Occurrences(back, Now, 3);
            Check(next.Count == 3 && next.All(time => time.TimeOfDay == new TimeSpan(19, 30, 0)) && next[1] - next[0] == TimeSpan.FromDays(1), "every day at 19:30");
            Check(loaded.Single(alarm => alarm.Id == other.Id).At == other.At, "the other alarm untouched");
        }),
        ("X-280: a weekly alarm takes the new weekday; a one-off its new date", () =>
        {
            var weekly = new TenshoAlarm { Repeat = AlarmRepeat.RealWeekly, At = Now.AddDays(-10) };
            var friday = new DateTimeOffset(2026, 11, 20, 20, 0, 0, TimeSpan.Zero);
            Check(AlarmSchedule.Move(weekly, friday) && AlarmSchedule.Occurrences(weekly, Now, 2).SequenceEqual([friday, friday.AddDays(7)]), "weekly");
            var once = new TenshoAlarm { Repeat = AlarmRepeat.Once, At = Now.AddHours(2) };
            Check(AlarmSchedule.EditShown(once, Now) == once.At, "one-off shows its own date");
            Check(AlarmSchedule.Move(once, Now.AddHours(3)) && AlarmSchedule.Next(once, Now) == Now.AddHours(3), "one-off moved");
            Check(!AlarmSchedule.Move(once, Now.AddHours(3)), "same date: nothing to save");
        }),
        ("X-280: alarms without a real date are not moved (Eorzea time, resets)", () =>
        {
            foreach (var repeat in new[] { AlarmRepeat.EorzeaDaily, AlarmRepeat.DailyReset, AlarmRepeat.WeeklyReset })
            {
                var alarm = new TenshoAlarm { Repeat = repeat, At = Now };
                Check(!AlarmSchedule.DateEditable(repeat) && !AlarmSchedule.Move(alarm, Now.AddHours(1)) && alarm.At == Now, repeat.ToString());
            }
        }),
        ("X-280: the edit field uses DateEditable and Move", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Alarms.cs"));
            Check(page.Contains("if(AlarmSchedule.DateEditable(alarm.Repeat))") && page.Contains("AlarmSchedule.Move(alarm,moved)"), "wiring");
        }),
    ];
}
