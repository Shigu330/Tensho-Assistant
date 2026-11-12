using TenshoAssistant.Reminders;

// 0.1165 (X-179, user's finding of 2026-11-12: « manual tending does not update the alarms »): the game's tending line does not say which bed; the
// title of the game's menu on the plant does (« Potager 1, emplacement 6 », Addon 6420). Behaviour tests: one tending = one bed only, never another
// patch nor another garden; a title too old, or from elsewhere, is ignored.
internal static class GardenMenu1165Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    // Addon 6420 as the client gives it (Lumina's ToMacroString), read on 2026-11-12 in the four languages.
    private const string French = "Potager <num(lnum1)>, emplacement <num(lnum2)>";
    private const string English = "<ordinal(lnum2)> Bed, <ordinal(lnum1)> Patch";
    private const string German = "Beet <num(lnum1)>, Furche <num(lnum2)>";
    private const string Japanese = "畑<num(lnum1)>の畝<num(lnum2)>";

    private static GardenBed Bed(string place) => new() { Place = place, Seed = "Seed", PlantedAt = DateTimeOffset.UnixEpoch, GrowHours = 100, TendHours = 24 };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Garden menu 1165: the menu title gives the patch and the bed in every language, by the row's own numbers", () =>
        {
            Check(GardenMenuTitle.From(French)?.Read("Potager 1, emplacement 6") == (1, 6), "French");
            Check(GardenMenuTitle.From(English)?.Read("6th Bed, 1st Patch") == (1, 6) && GardenMenuTitle.From(English)?.Read("2nd Bed, 3rd Patch") == (3, 2), "English: bed first, ordinals");
            Check(GardenMenuTitle.From(German)?.Read("Beet 2, Furche 8") == (2, 8), "German");
            Check(GardenMenuTitle.From(Japanese)?.Read("畑3の畝1") == (3, 1), "Japanese");
            var french = GardenMenuTitle.From(French)!;
            Check(french.Read("Potager 4, emplacement 6") is null && french.Read("Potager 1, emplacement 9") is null && french.Read("Potager 1, emplacement 0") is null, "outside 1-3 and 1-8: nothing");
            Check(french.Read("Mettre de l'engrais") is null && french.Read("Le potager 1, emplacement 6 de Bob") is null && french.Read("") is null && french.Read(null) is null, "another text: nothing");
        }),
        ("Garden menu 1165: a row the game changed is not used (no guess)", () =>
        {
            Check(GardenMenuTitle.From(null) is null && GardenMenuTitle.From("") is null, "no row");
            Check(GardenMenuTitle.From("Potager <num(lnum1)>") is null, "the bed's number missing");
            Check(GardenMenuTitle.From("Potager <num(lnum1)>, emplacement <num(lnum1)>") is null, "the same number twice");
            Check(GardenMenuTitle.From("Potager <num(lnum1)>, <sheet(Item,lnum2,0)>") is null, "another macro");
        }),
        ("Garden menu 1165: the bed seen is used once, at the same place, within 30 s", () =>
        {
            var memory = new GardenMenuMemory();
            var at = new DateTimeOffset(2026, 11, 12, 11, 21, 30, TimeSpan.Zero);
            memory.Seen(1, 6, "Shirogane 2-17", at);
            Check(memory.Take("Shirogane 2-17", at.AddSeconds(3)) == (1, 6), "the line just after: that bed");
            Check(memory.Take("Shirogane 2-17", at.AddSeconds(4)) is null, "used once");
            memory.Seen(1, 6, "Shirogane 2-17", at);
            Check(memory.Take("Shirogane 2-17", at.AddSeconds(31)) is null, "too old: ignored");
            memory.Seen(1, 6, "Shirogane 2-17", at);
            Check(memory.Take("Brumée 3-5", at.AddSeconds(2)) is null, "another garden: ignored");
            memory.Seen(1, 6, "Shirogane 2-17", at);
            Check(memory.Take("Shirogane 2-17", at.AddSeconds(-1)) is null, "a clock gone back: ignored");
            memory.Seen(1, 6, null, at);
            Check(memory.Take(null, at.AddSeconds(1)) is null, "an unknown place: ignored");
        }),
        ("Garden menu 1165: one tending = one bed only, never another patch nor another garden", () =>
        {
            var here = "Shirogane 2-17";
            var beds = new List<GardenBed>
            {
                Bed($"{here} · potager 1, emplacement 6"), Bed($"{here} · potager 1, emplacement 3"), Bed($"{here} · potager 2, emplacement 6"),
                Bed("Brumée 3-5 · potager 1, emplacement 6"), Bed($"{here} · pot"),
            };
            var target = GardenJournal.CareTargets(beds, here, (1, 6));
            Check(target.Length == 1 && target[0] == beds[0], "the bed of the menu alone");
            Check(GardenJournal.CareTargets(beds, here, (2, 6)).Single() == beds[2], "the other patch's bed 6 when it is the one");
            Check(GardenJournal.CareTargets(beds, here, (3, 1)).Length == 0, "a bed not noted: nothing (never another one)");
            Check(GardenJournal.CareTargets(beds, "Brumée 3-5", (1, 6)).Single() == beds[3], "the other garden's own bed only");
            Check(GardenJournal.CareTargets(beds, here, null).Length == 4, "without the menu: all the beds here, the caller acts on one only");
            Check(GardenJournal.CareTargets(beds, null, (1, 6)).Length == 0, "no place: nothing");
        }),
        ("Garden menu 1165: wiring (menu read in housing, used for tending, uprooting and harvest; the whole-patch button)", () =>
        {
            var watcher = Read("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs");
            Check(watcher.Contains("ReadMenu(now);") && watcher.Contains("GetRowOrDefault(GardenMenuTitle.AddonRow)?.Text.ToMacroString()") && watcher.Contains("!InHousing()"), "the menu title read in housing, from Addon 6420");
            Check(watcher.Contains("if(menuMemory.Take(place,DateTimeOffset.UtcNow) is { } slot)found=found with{Patch=slot.Patch,Bed=slot.Bed};") && watcher.Contains("var slot=menuMemory.Take(place,at);"), "tending, uprooting and harvest take it");
            var page = Read("src/TenshoAssistant/MainWindow.Garden.cs");
            Check(page.Contains("GardenJournal.CareTargets(configuration.GardenBeds,zone,menu)") && page.Contains("Tip(\"Garden.TendPatchTip\");"), "the page uses it; the patch button");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("gui:gameGui,developer:()=>configuration.DeveloperMode),()=>gardenWatcher?.Dispose());"), "the watcher gets the game's windows");
        }),
    ];
}
