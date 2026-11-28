using TenshoAssistant.Reminders;

// X-226 (the user's request of 12/11, K-15, R04 agreement given): the preparation reminder outside fights and duties: a meal's Well Fed status or
// the chocobo companion about to end, or ended by itself; once per end, never in a fight nor a duty (R03.1); nothing when the player took it
// off (the companion sent away: « use another bunch of gysahl greens », HowToPage row 372).
internal static class PrepReminder1778Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset Start = new(2026, 11, 22, 5, 0, 0, TimeSpan.Zero);

    // Readings every 5 s from `from` seconds left down to `to`, the time going on; the notices said.
    private static List<PrepNotice> Run(PrepTracker tracker, ref DateTimeOffset now, double from, double to, int threshold, bool busy = false)
    {
        var said = new List<PrepNotice>();
        for (var left = from; left >= to; left -= 5)
        {
            if (tracker.Read(now, left, threshold, busy) is { } notice) said.Add(notice);
            now = now.AddSeconds(5);
        }
        return said;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Prep reminder 1778: behaviour: the threshold crossed says it once (« ends in n min »); the end after it says nothing more; a new meal arms it again", () =>
        {
            var meal = new PrepTracker(PrepKind.Meal);
            var now = Start;
            var said = Run(meal, ref now, 30 * 60, 1, 5);
            Check(said.Count == 1 && said[0] == new PrepNotice(PrepKind.Meal, false, 5), "one reminder at 5 min: " + string.Join(", ", said));
            Check(meal.Read(now, null, 5, false) is null, "the end after it: nothing more (one reminder per end)");
            now = now.AddMinutes(1);
            said = Run(meal, ref now, 60 * 60, 4 * 60, 5);
            Check(said.Count == 1 && !said[0].Ended, "a new meal: armed again, one reminder");
            var loaded = new PrepTracker(PrepKind.Meal);
            Check(loaded.Read(Start, 150, 5, false) == new PrepNotice(PrepKind.Meal, false, 3), "Tensho loaded with 2 min 30 s left: said at once, rounded up to 3 min");
        }),
        ("Prep reminder 1778: behaviour: in a fight or a duty nothing is said; after it, the reminder still holding is said once", () =>
        {
            var meal = new PrepTracker(PrepKind.Meal);
            var now = Start;
            Check(Run(meal, ref now, 10 * 60, 2 * 60, 5, busy: true).Count == 0, "in a fight: nothing");
            Check(meal.Read(now, 115, 5, false) == new PrepNotice(PrepKind.Meal, false, 2), "out of it with the meal still ending: said once");
            Check(meal.Read(now.AddSeconds(5), 110, 5, false) is null, "not again");
            var duty = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            Check(Run(duty, ref now, 10 * 60, 5, 5, busy: true).Count == 0 && duty.Read(now, null, 5, true) is null, "the companion ran out in a fight: nothing yet");
            now = now.AddSeconds(40);
            Check(duty.Read(now, null, 5, false) == new PrepNotice(PrepKind.Chocobo, true, 0), "after the fight: « has left », once");
            Check(duty.Read(now.AddSeconds(5), null, 5, false) is null, "not again");
            var eaten = new PrepTracker(PrepKind.Meal);
            now = Start;
            Run(eaten, ref now, 3 * 60, 2 * 60, 5, busy: true);
            Check(eaten.Read(now, 30 * 60, 5, false) is null, "eaten again during the fight: nothing to say after it");
        }),
        ("Prep reminder 1778: behaviour: threshold 0 says only the end; taken off early (sent away, clicked off) says nothing; a loading screen loses nothing", () =>
        {
            var meal = new PrepTracker(PrepKind.Meal);
            var now = Start;
            Check(Run(meal, ref now, 60, 5, 0).Count == 0, "threshold 0: nothing while it lasts");
            Check(meal.Read(now, null, 0, false) == new PrepNotice(PrepKind.Meal, true, 0) && meal.Read(now.AddSeconds(5), null, 0, false) is null, "« has worn off » once");
            var sent = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            Run(sent, ref now, 25 * 60, 20 * 60, 5);
            Check(sent.Read(now, null, 5, false) is null && sent.Read(now.AddMinutes(30), null, 5, false) is null, "sent away with 20 min left: the player's choice, nothing said");
            var loading = new PrepTracker(PrepKind.Meal);
            now = Start;
            Run(loading, ref now, 60, 40, 0);
            // A loading screen of 30 s: no reading, the meal ran out meanwhile (40 s left at the last reading).
            now = now.AddSeconds(30);
            Check(loading.Read(now, null, 0, false) == new PrepNotice(PrepKind.Meal, true, 0), "ran out during a loading screen: still told");
            Check(new PrepTracker(PrepKind.Meal).Read(Start, null, 5, false) is null, "never had one: nothing");
        }),
        ("Prep reminder 1778: settings on by default, thresholds 5 min clamped 0 to 30; wired outside fights and duties, with the alarm options, in Settings › Alarms", () =>
        {
            var settings = new PrepReminderSettings();
            Check(settings.Meal && settings.Chocobo && settings.MealThreshold == 5 && settings.ChocoboThreshold == 5, "defaults");
            Check(new PrepReminderSettings { MealMinutes = -3, ChocoboMinutes = 99 } is { MealThreshold: 0, ChocoboThreshold: PrepReminderSettings.MaximumMinutes }, "clamped");
            var watcher = Read("src/TenshoAssistant/Reminders/PrepReminderWatcher.Dalamud.cs");
            Check(watcher.Contains("condition[ConditionFlag.InCombat] || condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56] || condition[ConditionFlag.BoundByDuty95]", StringComparison.Ordinal), "busy: a fight or a duty");
            Check(watcher.Contains("CompetitiveGate.Add(framework,OnUpdate);", StringComparison.Ordinal) && watcher.Contains("CompetitiveGate.Remove(framework,OnUpdate);", StringComparison.Ordinal), "cut in PvP and ranked content, removed at the end");
            Check(watcher.Contains("if(status.StatusId==WellFed){left=status.RemainingTime;break;}", StringComparison.Ordinal) && watcher.Contains("info.Companion->EntityId is not (0 or NoEntity)", StringComparison.Ordinal), "the meal and the companion out");
            var plugin = Read("src/TenshoAssistant/Plugin.Reminders.cs");
            Check(plugin.Contains("new Reminders.PrepReminderWatcher(framework,client,objects,condition,()=>configuration.PrepReminders ?? new(),log)", StringComparison.Ordinal)
                && plugin.Contains("if(options.ChatAllowed(false))chat.Print(\"[Tensho] \"+text);", StringComparison.Ordinal) && plugin.Contains("()=>prepReminders?.Dispose()", StringComparison.Ordinal), "wired with its end");
            Check(Read("src/TenshoAssistant/MainWindow.Alarms.cs").Contains("DrawPrepReminderSettings();", StringComparison.Ordinal), "with the alarms' settings");
            Check(Read("src/TenshoAssistant/Configuration.cs").Contains("\"PrepReminders\",", StringComparison.Ordinal), "in the settings reset");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "MealEnding", "MealEnded", "ChocoboEnding", "ChocoboEnded", "WellFed", "MealTip", "ChocoboTip" })
                    Check(text.Contains($"\"{key}\": ", StringComparison.Ordinal), $"{language}: {key}");
            }
        }),
    ];
}
