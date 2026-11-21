using TenshoAssistant.Reminders;

// X-729 (0.1727, review of 0.1721 to 0.1726, I1): a bed ripe by the calculation only is never said « ripe » in the server bar's « finished »
// entry nor on the home page: counted apart (GardenExpected) with « ripe by the calculation, to confirm in game »; « ripe » once the game says
// so. X-731 (M3): the home page's deadline of a harvest to come says « ripe (calculation) », as the garden's alarm list. Behaviour test: the
// carrots of 18/11 to 21/11 replayed (planted 12:00:20 UTC, 72 h; ripe by the calculation at 12:00:20 UTC on 21/11, said ripe by the game at 13:01).
internal static class GardenBar1727Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static DateTimeOffset Utc(int day, int hour, int minute, int second = 0) => new(2026, 11, day, hour, minute, second, TimeSpan.Zero);
    private static GardenBed Carrot(int bed) => GardenJournal.Plant("Home 2-17 · patch 1, bed " + bed, "Krakka Root", 72, 24, Utc(18, 12, 0, 20), 8182);
    private static IReadOnlyList<(FinishedKind Kind, int Count)> Count(GardenBed[] beds, DateTimeOffset now) => ReminderCounts.Finished(null, null, null, null, beds, now);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Garden bar 1727: the carrots of 21/11: ripe by the calculation counted apart, « ripe » only once the game says so", () =>
        {
            var six = Carrot(6); var two = Carrot(2);
            Check(Count([six, two], Utc(21, 11, 59)).Count == 0, "before the calculated time: nothing");
            Check(Count([six, two], Utc(21, 12, 5)).SequenceEqual([(FinishedKind.GardenExpected, 2)]), "the user's screenshot (13:05 local): two beds ripe by the calculation, none « ripe »");
            GardenJournal.Examined([six, two], "Home 2-17", 1, 6, GardenGameState.Ripe, Utc(21, 13, 1));
            Check(Count([six, two], Utc(21, 13, 2)).SequenceEqual([(FinishedKind.Garden, 1), (FinishedKind.GardenExpected, 1)]), "the game said bed 6 ripe: one ripe, one still by the calculation");
            GardenJournal.Examined([six, two], "Home 2-17", 1, 2, GardenGameState.Growing, Utc(21, 13, 3));
            Check(Count([six, two], Utc(21, 13, 4)).SequenceEqual([(FinishedKind.Garden, 1)]), "said growing after the calculated time: unknown, counted in neither");
        }),
        ("Garden bar 1727: every finished kind has its words in en and fr; the bar and the home page take them by kind", () =>
        {
            foreach (var language in new[] { "en", "fr" })
            {
                var json = Read("Localization/Languages/" + language + ".json");
                foreach (var kind in Enum.GetNames<FinishedKind>()) Check(json.Contains("    \"Bar" + kind + "\": \"", StringComparison.Ordinal), language + ": Bar" + kind);
            }
            var fr = Read("Localization/Languages/fr.json"); var en = Read("Localization/Languages/en.json");
            Check(fr.Contains("\"BarGarden\": \"Emplacements mûrs : {0}\"", StringComparison.Ordinal) && fr.Contains("\"BarGardenExpected\": \"Emplacements mûrs selon le calcul, à confirmer en jeu : {0}\"", StringComparison.Ordinal), "fr");
            Check(en.Contains("\"BarGardenExpected\": \"Garden beds ripe by the calculation, to confirm in game: {0}\"", StringComparison.Ordinal), "en");
            Check(Read("Alarms/ReminderDtrEntry.Dalamud.cs").Contains("text(\"Reminders.Bar\"+item.Kind)", StringComparison.Ordinal) && Read("MainWindow.HomeToday.cs").Contains("localization.Format(\"Reminders.Bar\"+item.Kind,item.Count)", StringComparison.Ordinal), "by kind");
        }),
        ("Garden bar 1727: X-731: the home page's harvest deadline says « ripe (calculation) », as the garden's alarm list", () =>
        {
            var fr = Read("Localization/Languages/fr.json"); var en = Read("Localization/Languages/en.json");
            Check(fr.Contains("\"DeadlineRipe\": \"{0} : mûr (calcul)\"", StringComparison.Ordinal) && fr.Contains("\"AlarmRipe\": \"Mûr (calcul)\"", StringComparison.Ordinal), "fr: the same words");
            Check(en.Contains("\"DeadlineRipe\": \"{0}: ripe (calculation)\"", StringComparison.Ordinal) && en.Contains("\"AlarmRipe\": \"Ripe (calculation)\"", StringComparison.Ordinal), "en: the same words");
            var bed = Carrot(4);
            Check(GardenJournal.Alarms([bed], Utc(21, 11, 0)).Any(alarm => alarm.Kind == GardenState.Ripe && alarm.At == GardenJournal.RipeAt(bed)), "the harvest to come is at the calculated time");
        }),
    ];
}
