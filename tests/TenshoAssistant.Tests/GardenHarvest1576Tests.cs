using TenshoAssistant.Reminders;

// 0.1576 (X-628, review of 0.1570 to 0.1572): M1 a withered bed is never a harvest's candidate; M2 the game's own words of a bed's state line
// are kept and shown under « Unknown » (a bed in need of care is not said « still growing »).
internal static class GardenHarvest1576Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static DateTimeOffset Utc(int day, int hour, int minute) => new(2026, 11, day, hour, minute, 0, TimeSpan.Zero);
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    private static GardenBed Bed(int number, GardenGameState state, DateTimeOffset? said = null) => new()
    {
        Place = "Home 2-17 · patch 1, bed " + number, Seed = "Krakka root", SeedId = 8182, PlantedAt = Utc(14, 22, 55), GrowHours = 72,
        GameState = state, GameStateAt = said,
    };

    public static IReadOnlyList<(string Name, Action Run)> Cases =>
    [
        ("Garden harvest 1576: a withered bed is never a harvest's candidate (M1)", () =>
        {
            var now = Utc(18, 12, 0);
            var ripe = Bed(1, GardenGameState.Ripe, Utc(18, 11, 0));
            var withered = Bed(2, GardenGameState.Withered, Utc(18, 11, 0));
            var unknown = Bed(3, GardenGameState.Growing, Utc(18, 0, 13));
            var growing = Bed(4, GardenGameState.None);
            growing.PlantedAt = Utc(18, 0, 0);
            Check(GardenJournal.State(withered, now) == GardenState.Unknown, "withered stays « Unknown » on the page");
            Check(GardenJournal.HarvestCandidates([ripe, withered], now).SequenceEqual([ripe]), "ripe and withered: the ripe one alone (no « several beds »)");
            Check(GardenJournal.HarvestCandidates([withered], now).Length == 0, "withered alone: none (nothing removed, the player is told)");
            Check(GardenJournal.HarvestCandidates([unknown, withered, growing], now).SequenceEqual([unknown]), "said growing after the expected time: still a candidate");
        }),
        ("Garden harvest 1576: the game's words kept with the state, one line, never another bed's (M2)", () =>
        {
            var memory = new GardenTalkMemory();
            var at = Utc(19, 22, 8);
            memory.Said(GardenGameState.Wilting, "Home 2-17", at, "Krakka Root\nThis crop has seen better days...");
            Check(memory.Opened("Home 2-17", at.AddMilliseconds(80), out var line) == GardenGameState.Wilting && line == "Krakka Root\nThis crop has seen better days...", "the line goes with its state");
            Check(memory.Opened("Home 2-17", at.AddMilliseconds(90), out var none) == GardenGameState.None && none is null, "used once");
            memory.Said(GardenGameState.Growing, "Home 2-17", at, "x");
            Check(memory.Opened("Elsewhere 1-1", at.AddMilliseconds(80), out var other) == GardenGameState.None && other is null, "another place: no words");
            var beds = new List<GardenBed> { Bed(2, GardenGameState.None) };
            GardenJournal.Examined(beds, "Home 2-17", 1, 2, GardenGameState.Wilting, Utc(17, 23, 13), "Krakka Root\nThis crop has seen\tbetter days...");
            Check(beds[0].GameStateText == "Krakka Root This crop has seen better days...", "kept as one line: " + beds[0].GameStateText);
            Check(GardenJournal.GameLine(new string('a', 300))!.Length == 200 && GardenJournal.GameLine(" \n ") is null && GardenJournal.GameLine(null) is null, "at most 200 characters; empty: null");
            GardenJournal.Examined(beds, "Home 2-17", 1, 2, GardenGameState.Growing, Utc(18, 0, 0));
            Check(beds[0].GameStateText is null, "a later line without words does not keep the old ones");
        }),
        ("Garden harvest 1576: the page shows the game's words, and « in need of care » for Wilting", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.Garden.cs");
            Check(page.Contains("var ripe=GardenJournal.HarvestCandidates(here,DateTimeOffset.UtcNow);"), "the harvest takes the candidates");
            Check(page.Contains("bed.GameStateText is { Length:>0 } line ? localization.Format(\"Garden.UnknownSaid\",said,line)") && page.Contains("GardenGameState.Wilting=>\"Garden.UnknownWilting\""), "the game's words, else Wilting's own text");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read("src/TenshoAssistant/Localization/Languages/" + language + ".json");
                Check(text.Contains("\"UnknownSaid\": ") && text.Contains("\"UnknownWilting\": ") && text.Contains("\"UnknownAfter\": "), language + ": the texts");
            }
            Check(!Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("le semis poussait encore, après"), "the old text that said « still growing » for every state is gone");
        }),
    ];
}
