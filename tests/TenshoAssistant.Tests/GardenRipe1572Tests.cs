using TenshoAssistant.Reminders;

// 0.1572 (X-581, user report of 18/11 about 00:35: four carrots said « Ripe » that were not): « Ripe » only when the game said so, or when the
// planting time plus the growing time has passed and the game has not said since that the crop was still growing; else « Unknown ». The game's
// state line of a bed (sheet CmnDefHousingGardeningPlant_00151, docs/evidence/0.1572-garden-ripe.md) is read when the bed's menu opens.
internal static class GardenRipe1572Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static DateTimeOffset Utc(int day, int hour, int minute) => new(2026, 11, day, hour, minute, 0, TimeSpan.Zero);

    // The eight beds of the capture: apples on the odd beds (planted 16/11 22:05 UTC, 120 h), carrots on the even ones (14/11 22:55 UTC, 72 h).
    private static List<GardenBed> Capture()
    {
        var beds = new List<GardenBed>();
        foreach (var number in new[] { 1, 3, 5, 7 })
            beds.Add(new() { Place = "Home 2-17 · patch 1, bed " + number, Seed = "Glowing apple", SeedId = 7731, PlantedAt = Utc(16, 22, 5), GrowHours = 120, TendHours = 48, TendedAt = Utc(17, 22, 8) });
        foreach (var number in new[] { 2, 4, 6, 8 })
            beds.Add(new() { Place = "Home 2-17 · patch 1, bed " + number, Seed = "Krakka root", SeedId = 8182, PlantedAt = Utc(14, 22, 55), GrowHours = 72, TendHours = 24, TendedAt = Utc(17, 22, 8) });
        return beds;
    }

    private static GardenBed Bed(List<GardenBed> beds, int number) => beds.Single(bed => GardenJournal.Slot(bed)!.Bed == number);

    private static GardenTalk FrenchTalk() => GardenTalk.From(
    [
        ("TEXT_CMNDEFHOUSINGGARDENINGPLANT_00151_CARE", ["Entretenir"]),
        ("TEXT_CMNDEFHOUSINGGARDENINGPLANT_00151_TALK_DEAD", ["Le semis ", null, " s'est fané..."]),
        ("TEXT_CMNDEFHOUSINGGARDENINGPLANT_00151_TALK_VIGOROUS", ["Le semis ", null, " se porte bien."]),
        ("TEXT_CMNDEFHOUSINGGARDENINGPLANT_00151_TALK_DEPRESSED", ["Le semis ", null, " n'a pas l'air en grande forme..."]),
        ("TEXT_CMNDEFHOUSINGGARDENINGPLANT_00151_TALK_RIPE", [null, " est arrivé", null, " à maturité", null, "!"]),
    ])!;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Garden ripe 1572: the eight beds of the capture, the game having said « growing » after the expected time", () =>
        {
            var beds = Capture();
            var now = Utc(17, 23, 35);
            foreach (var number in new[] { 2, 4, 6, 8 })
                Check(GardenJournal.State(Bed(beds, number), now) == GardenState.Expected, "without the game's word, bed " + number + " is ripe by the calculation only, to confirm in game (X-720)");
            foreach (var number in new[] { 2, 4, 6, 8 })
                Check(GardenJournal.Examined(beds, "Home 2-17", 1, number, GardenGameState.Growing, Utc(17, 23, 13)) == Bed(beds, number), "the line goes to bed " + number);
            foreach (var number in new[] { 2, 4, 6, 8 })
            {
                var bed = Bed(beds, number);
                Check(GardenJournal.State(bed, now) == GardenState.Unknown, "bed " + number + " is not said ripe: " + GardenJournal.State(bed, now));
                Check(GardenView.Left(bed, now) is null && GardenView.Badge(bed, now) == GardenBadge.Unknown, "bed " + number + ": no time left, an unknown mark");
            }
            foreach (var number in new[] { 1, 3, 5, 7 })
            {
                var bed = Bed(beds, number);
                Check(GardenJournal.State(bed, now) == GardenState.Growing && GardenView.Left(bed, now) == (3, 22, 30), "bed " + number + ": growing, 3 d 22 h left");
            }
            Check(!GardenJournal.Due(beds, now).Any(due => due.State == GardenState.Ripe), "no « ripe » notice for a bed the game said growing");
        }),
        ("Garden ripe 1572: « Ripe » when the game says so, even before the expected time; « growing » before the expected time changes nothing", () =>
        {
            var beds = Capture();
            var apple = Bed(beds, 1);
            Check(GardenJournal.Examined(beds, "Home 2-17", 1, 1, GardenGameState.Growing, Utc(18, 0, 0)) == apple, "said growing before the expected time");
            Check(GardenJournal.State(apple, Utc(21, 23, 0)) == GardenState.Expected, "said growing before the expected time: ripe by the calculation once the time has passed, to confirm (X-720)");
            GardenJournal.Examined(beds, "Home 2-17", 1, 1, GardenGameState.Ripe, Utc(20, 12, 0));
            Check(GardenJournal.State(apple, Utc(20, 12, 1)) == GardenState.Ripe && GardenView.Growth(apple, Utc(20, 12, 1)) == 1 && GardenView.Left(apple, Utc(20, 12, 1)) is null, "the game says ripe: ripe at once");
            Check(!GardenJournal.Alarms(beds, Utc(20, 12, 1)).Any(alarm => alarm.Bed == apple && alarm.Kind == GardenState.Ripe), "no harvest alarm to come for a bed already ripe");
            var carrot = Bed(beds, 2);
            GardenJournal.Examined(beds, "Home 2-17", 1, 2, GardenGameState.Wilting, Utc(17, 23, 13));
            Check(GardenJournal.State(carrot, Utc(17, 23, 35)) == GardenState.Unknown, "in need of care after the expected time: unknown");
            GardenJournal.Examined(beds, "Home 2-17", 1, 2, GardenGameState.Ripe, Utc(18, 11, 50));
            Check(GardenJournal.State(carrot, Utc(18, 11, 51)) == GardenState.Ripe, "then said ripe: ripe");
            GardenJournal.Examined(beds, "Home 2-17", 1, 4, GardenGameState.Withered, Utc(16, 0, 0));
            Check(GardenJournal.State(Bed(beds, 4), Utc(16, 0, 1)) == GardenState.Unknown, "withered: unknown, never ripe");
        }),
        ("Garden ripe 1572: a line for a bed not noted, another place or no state changes nothing; a new planting forgets the game's word", () =>
        {
            var beds = Capture();
            Check(GardenJournal.Examined(beds, "Home 2-17", 2, 1, GardenGameState.Growing, Utc(18, 0, 0)) is null, "patch 2 is not noted");
            Check(GardenJournal.Examined(beds, "Elsewhere 1-1", 1, 1, GardenGameState.Growing, Utc(18, 0, 0)) is null, "another place");
            Check(GardenJournal.Examined(beds, "Home 2-17", 1, 1, GardenGameState.None, Utc(18, 0, 0)) is null, "nothing said");
            Check(beds.All(bed => bed.GameState == GardenGameState.None), "no bed changed");
            GardenJournal.Examined(beds, "Home 2-17", 1, 2, GardenGameState.Growing, Utc(17, 23, 13));
            var replanted = GardenJournal.Detected(beds, "Home 2-17 · patch 1, bed 2", "Krakka root", 72, 24, Utc(18, 12, 0), 8182)!;
            Check(Bed(replanted, 2).GameState == GardenGameState.None && Bed(replanted, 2).GameStateAt is null, "the new planting starts without the game's word");
        }),
        ("Garden ripe 1572: the game's state lines read from the sheet's rows, in French and in English", () =>
        {
            var talk = FrenchTalk();
            Check(talk.Read("", "Le semis de carotte de Krakka se porte bien.") == GardenGameState.Growing, "growing");
            Check(talk.Read("", "Le semis de carotte de Krakka n'a pas l'air en grande forme...") == GardenGameState.Wilting, "in need of care");
            Check(talk.Read("", "La carotte de Krakka est arrivée à maturité\u00a0!") == GardenGameState.Ripe, "ripe (non-breaking space)");
            Check(talk.Read("", "La carotte de Krakka est arrivée à maturité !") == GardenGameState.Ripe, "ripe (space)");
            Check(talk.Read("", "Le semis de pomme luisante s'est fané...") == GardenGameState.Withered, "withered");
            Check(talk.Read("", "Le semis gagne en vigueur!") == GardenGameState.None, "the tending line is no state");
            Check(talk.Read("Ann Doe", "Le semis de carotte de Krakka se porte bien.") == GardenGameState.None, "a player typing it never counts");
            var english = GardenTalk.From(
            [
                ("X_TALK_DEAD", [null, null, "This crop is beyond hope."]),
                ("X_TALK_VIGOROUS", [null, null, "This crop is doing well."]),
                ("X_TALK_DEPRESSED", [null, null, "This crop has seen better days..."]),
                ("X_TALK_RIPE", [null, null, "This crop is ready to be harvested."]),
            ])!;
            Check(english.Read("", "Krakka Root\nThis crop is ready to be harvested.") == GardenGameState.Ripe, "English ripe");
            Check(english.Read("", "Krakka Root\nThis crop has seen better days...") == GardenGameState.Wilting, "English in need of care");
            Check(GardenTalk.From([("X_TALK_VIGOROUS", ["a ", null])]) is null, "a sheet without the four states (a game update): no rule, nothing guessed");
        }),
        ("Garden ripe 1572: the line goes to the menu that opens just after it at the same place, once", () =>
        {
            var memory = new GardenTalkMemory();
            var at = Utc(19, 22, 8);
            memory.Said(GardenGameState.Wilting, "Home 2-17", at);
            Check(memory.Opened("Home 2-17", at.AddMilliseconds(80)) == GardenGameState.Wilting, "the menu 80 ms after the line (measured 65 to 85 ms)");
            Check(memory.Opened("Home 2-17", at.AddMilliseconds(3000)) == GardenGameState.None, "the next menu without its own line: nothing");
            memory.Said(GardenGameState.Growing, "Home 2-17", at);
            Check(memory.Opened("Home 2-17", at.AddSeconds(2)) == GardenGameState.None, "a menu more than a second later: not that line's");
            memory.Said(GardenGameState.Growing, "Home 2-17", at);
            Check(memory.Opened("Elsewhere 1-1", at.AddMilliseconds(80)) == GardenGameState.None, "another place: nothing");
            memory.Said(GardenGameState.Growing, "Home 2-17", at);
            Check(memory.Opened("Home 2-17", at.AddMilliseconds(-10)) == GardenGameState.None, "a menu before the line: nothing");
        }),
        ("Garden ripe 1572: wiring of the watcher, the plugin and the page", () =>
        {
            var watcher = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs"));
            Check(watcher.Contains("Rules()?.Talk?.Read(sender,text)") && watcher.Contains("talkMemory.Said(said,Place(),DateTimeOffset.UtcNow,text);"), "the state line is kept");
            Check(watcher.Contains("talkMemory.Opened(place,now,out var line)") && watcher.Contains("examined?.Invoke(place,slot.Patch,slot.Bed,said,line);"), "the menu that opens takes it");
            Check(watcher.Contains("data.Excel.GetSheet<Lumina.Excel.RawRow>(null,GardenTalk.Sheet)"), "the rules come from the game's sheet");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("examined:(place,patch,bed,said,line)=>mainWindow.GardenExamined(place,patch,bed,said,line)),()=>gardenWatcher?.Dispose());"), "the plugin tells the page");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.GardenView.cs"));
            Check(page.Contains("else if(state==GardenState.Unknown){ImGui.TextColored(Muted,localization.Get(\"Garden.StateUnknown\"));if(TipHovered())TipText(GardenUnknownText(bed));}"), "« Unknown » with its reason under the mouse");
        }),
    ];
}
