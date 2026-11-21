using TenshoAssistant.Reminders;

// X-721 (0.1725, the user's request of 21/11): the server bar's « Left to do » entry is hidden at 0 and comes back by itself at 1; its setting
// off: never (behaviour test of the rule the entry follows at each reading, then the wiring).
internal static class TodoBar1725Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Todo bar 1725: hidden at 0, shown at 1 and back by itself, never with the setting off", () =>
        {
            var readings = new[] { 2, 1, 0, 0, 1 };
            var shown = readings.Select(count => ReminderCounts.TodoShown(true, count)).ToArray();
            Check(shown.SequenceEqual([true, true, false, false, true]), "2, 1: shown; 0: hidden; 1 again: back by itself");
            Check(readings.All(count => !ReminderCounts.TodoShown(false, count)), "the setting off: never");
        }),
        ("Todo bar 1725: wiring and the setting's tooltip in en and fr", () =>
        {
            Check(Read("Alarms/ReminderDtrEntry.Dalamud.cs").Contains("todoBounds=Show(todoEntry,ReminderCounts.TodoShown(configuration.ReminderServerBar,todoLines.Count),", StringComparison.Ordinal), "the entry follows the rule");
            Check(Read("Localization/Languages/fr.json").Contains("Cachée tant qu'il n'y a rien à faire, elle revient seule dès qu'il y a quelque chose.", StringComparison.Ordinal)
                && Read("Localization/Languages/en.json").Contains("Hidden while nothing is left to do, back by itself as soon as something is.", StringComparison.Ordinal), "said in the tooltip");
        }),
    ];
}
