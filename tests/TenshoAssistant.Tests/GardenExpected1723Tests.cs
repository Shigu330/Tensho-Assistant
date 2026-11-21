using TenshoAssistant.Reminders;

// X-720 (0.1723, the user's report of 21/11): « Ripe » only when the game says so; past the calculated time without it, « ripe by the
// calculation, to confirm in game » (Expected), the reminder and the desktop bell the same; the probe's line. Behaviour tests (R28.14): the
// carrots of 18/11 to 21/11 replayed (planted 12:00:20 UTC, 72 h; the game said growing at 12:57:42 UTC, ripe at about 13:01 UTC).
internal static class GardenExpected1723Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static DateTimeOffset Utc(int day, int hour, int minute, int second = 0) => new(2026, 11, day, hour, minute, second, TimeSpan.Zero);
    private static GardenBed Carrot(int bed) => GardenJournal.Plant("Home 2-17 · patch 1, bed " + bed, "Krakka Root", 72, 24, Utc(18, 12, 0, 20), 8182);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Garden expected 1723: the carrots of 21/11: ripe by the calculation at 72 h, to confirm; « Ripe » only when the game says so", () =>
        {
            var six = Carrot(6);
            Check(GardenJournal.State(six, Utc(21, 11, 59)) is GardenState.Growing or GardenState.ToTend, "before the calculated time: not ripe");
            Check(GardenJournal.State(six, Utc(21, 12, 5)) == GardenState.Expected, "the user's screenshot (13:05 local): ripe by the calculation, not « Ripe »");
            Check(GardenView.Badge(six, Utc(21, 12, 5)) == GardenBadge.Expected && GardenView.Left(six, Utc(21, 12, 5)) is null, "its own mark, no time left");
            var due = GardenJournal.Due([six], Utc(21, 12, 5));
            Check(due is [(_, GardenState.Expected)] && six.RipeSaid && GardenJournal.NoticeKey(due[0].State) == "Garden.ExpectedNotice", "the reminder says « should be ripe by the calculation »");
            Check(GardenJournal.HarvestCandidates([six], Utc(21, 12, 5)).SequenceEqual([six]), "a harvest may be of it");
            GardenJournal.Examined([six], "Home 2-17", 1, 6, GardenGameState.Growing, Utc(21, 12, 57, 42));
            Check(GardenJournal.State(six, Utc(21, 12, 58)) == GardenState.Unknown, "the game said growing at 13:57:42 local: unknown (0.1572)");
            GardenJournal.Examined([six], "Home 2-17", 1, 6, GardenGameState.Ripe, Utc(21, 13, 1));
            Check(GardenJournal.State(six, Utc(21, 13, 1)) == GardenState.Ripe && GardenView.Badge(six, Utc(21, 13, 1)) == GardenBadge.Ripe, "the game says ripe: « Ripe »");
            Check(GardenJournal.Due([six], Utc(21, 13, 2)).Count == 0, "said once: not again when the game confirms");
            Check(GardenJournal.NoticeKey(GardenState.Ripe) == "Garden.RipeNotice" && GardenJournal.NoticeKey(GardenState.ToTend) == "Garden.TendNotice", "the other notices");
        }),
        ("Garden expected 1723: the desktop bell rings a bed ripe by the calculation once, as such", () =>
        {
            var bell = new GardenBell();
            var two = Carrot(2);
            var rung = bell.Due([two], Utc(21, 12, 5));
            Check(rung is [(_, GardenState.Expected)] && !two.RipeSaid, "rung as expected, the bed not changed");
            Check(bell.Due([two], Utc(21, 12, 6)).Count == 0, "once");
            GardenJournal.Examined([two], "Home 2-17", 1, 2, GardenGameState.Ripe, Utc(21, 13, 1));
            Check(bell.Due([two], Utc(21, 13, 2)).Count == 0, "the game's « ripe » afterwards: not rung again");
        }),
        ("Garden expected 1723: the probe's line: patch and bed, seed, planting to the second, growing time and source, gap, no place name", () =>
        {
            var four = Carrot(4);
            var line = GardenJournal.Describe(four, Utc(21, 12, 57, 42), "embedded data, Lotlab, not the game");
            Check(line.StartsWith("patch 1, bed 4, seed item 8182, planted ", StringComparison.Ordinal) && line.Contains("growing 72 h (embedded data, Lotlab, not the game), tending 24 h", StringComparison.Ordinal)
                && line.Contains("(+57 min now)", StringComparison.Ordinal) && line.Contains("game None", StringComparison.Ordinal) && line.EndsWith("shown Expected", StringComparison.Ordinal), line);
            Check(!line.Contains("Home", StringComparison.Ordinal) && line.Contains(":00:20 ", StringComparison.Ordinal), "never the place's name; the planting to the second");
            Check(GardenJournal.Describe(four, Utc(21, 11, 0), "x").Contains("(-61 min now)", StringComparison.Ordinal), "before: a negative gap");
        }),
        ("Garden expected 1723: wiring: the page, the notices, the count, the probe, en and fr", () =>
        {
            var garden = Read("MainWindow.Garden.cs");
            Check(garden.Contains("ChatNotice?.Invoke(localization.Format(GardenJournal.NoticeKey(state),bed.Seed,bed.Place));", StringComparison.Ordinal), "the chat notice");
            Check(garden.Contains("GardenState.Expected=>localization.Format(\"Garden.Expected\",ripe)", StringComparison.Ordinal), "the journal's line");
            Check(garden.Contains("if(developerMode)log.Information(\"Garden planting noted: {0}.\",", StringComparison.Ordinal) && garden.Contains("if(developerMode)log.Information(\"Garden bed state kept: {0}.\",", StringComparison.Ordinal), "the probe, developer mode only");
            var view = Read("MainWindow.GardenView.cs");
            Check(view.Contains("else if(state==GardenState.Expected){ImGui.TextColored(Accent,localization.Get(\"Garden.StateExpectedShort\"));TipText(localization.Get(\"Garden.ExpectedWhy\"));}", StringComparison.Ordinal)
                && view.Contains("GardenBadge.Expected=>(Accent,background,FontAwesomeIcon.HourglassEnd),", StringComparison.Ordinal), "the grid: « Ripe? » and its mark");
            Check(Read("Reminders/ReminderCounts.cs").Contains("GardenJournal.State(bed,now) is GardenState.Ripe or GardenState.Expected", StringComparison.Ordinal), "counted among the beds to look at");
            var fr = Read("Localization/Languages/fr.json");
            Check(fr.Contains("\"StateExpected\": \"Mûr selon le calcul, à confirmer en jeu\"", StringComparison.Ordinal) && fr.Contains("\"AlarmRipe\": \"Mûr (calcul)\"", StringComparison.Ordinal), "fr");
            Check(Read("Localization/Languages/en.json").Contains("\"ExpectedNotice\": \"Garden: {0} should be ripe by the calculation, to confirm in game — {1}.\"", StringComparison.Ordinal), "en");
        }),
    ];
}
