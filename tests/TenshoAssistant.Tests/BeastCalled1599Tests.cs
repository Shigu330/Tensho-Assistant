using TenshoAssistant.Beastmaster;

// 0.1599 (X-655, the user's report of 20/11 10:45: no « 1 » or « 2 » on the hotbars in the labyrinth with the option on). The log of 20/11
// showed the battlehorns window (XBMActivePet) open only while the team is chosen (10:37:20 to 10:37:40) and closed in the fight (10:38:13):
// the beast fell back to 0 and the highlight stopped silently (docs/evidence/0.1599-beast-called.md). The beast called is now read on the
// field first (the player's familiar), the window only when none is found; where the highlight stops, or which bar and slot carry « 1 » and
// « 2 », is said in the log. R03.1 changed on 20/11 (journal J174): everywhere in PvE.
internal static class BeastCalled1599Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static Beast Make(uint id, string name) => new(id, name, 0, "", "", "", "", [new BeastStats(1, 1, 1, 1, 1)], "", 0);
    private static readonly Beast[] Beasts = [Make(1, "Cu sith"), Make(5, "Opo-opo"), Make(12, "Mandragore"), Make(22, "Pampa")];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast called 1599: the window closed in the fight (beast 0 from it), the familiar on the field gives the beast", () =>
        {
            var read = BeastCalled.Resolve(["Opo-opo"], 0, Beasts);
            Check(read == new BeastCalledRead(5, BeastCalledSource.Familiar), $"Opo-opo on the field: {read}");
            Check(BeastCalled.Resolve(["opo-opo"], 0, Beasts).Beast == 5, "any case");
        }),
        ("Beast called 1599: the familiar wins over the window; the window only when no familiar is found", () =>
        {
            Check(BeastCalled.Resolve(["Pampa"], 5, Beasts) == new BeastCalledRead(22, BeastCalledSource.Familiar), "familiar first");
            Check(BeastCalled.Resolve([], 5, Beasts) == new BeastCalledRead(5, BeastCalledSource.Window), "window when nothing on the field");
            Check(BeastCalled.Resolve(["Chocobo du joueur", ""], 5, Beasts) == new BeastCalledRead(5, BeastCalledSource.Window), "another battle NPC of the player is not a beast");
        }),
        ("Beast called 1599: two different beasts on the field, no catalogue, nothing: none (R08)", () =>
        {
            Check(BeastCalled.Resolve(["Opo-opo", "Pampa"], 0, Beasts) == BeastCalledRead.None, "two beasts");
            Check(BeastCalled.Resolve(["Opo-opo", "Opo-opo"], 0, Beasts).Beast == 5, "the same beast twice");
            Check(BeastCalled.Resolve(["Opo-opo"], 0, null) == BeastCalledRead.None && BeastCalled.Resolve(["Opo-opo"], 0, []) == BeastCalledRead.None, "no catalogue");
            Check(BeastCalled.Resolve([], 0, Beasts) == BeastCalledRead.None, "nothing");
        }),
        ("Beast called 1599: the beast of the user's fight (Opo-opo, Trick Rampant) gives Gale Axe 1 and Mistral Axe 2 (EventTutorialPage 34, 36)", () =>
        {
            var read = BeastCalled.Resolve(["Opo-opo"], 0, Beasts);
            Check(BeastInstinct.Of(read.Beast) == Affinity.Rampant, "Opo-opo's Trick 44943 is Rampant");
            Check(BeastInstinct.Combos(read.Beast) is [{ Skill: 44889, AfterTrick: false, Kind: IntentionalCombo.Sunstrider }, { Skill: 44887, AfterTrick: true, Kind: IntentionalCombo.Moonstalker }],
                "Gale Axe before the Trick (Sunstrider), Mistral Axe after it (Moonstalker, « Assaut létal » in the user's chat)");
        }),
        ("Beast combo trace 1599: one line for each place the highlight stops, and the bars and slots of « 1 » and « 2 »", () =>
        {
            var familiar = new BeastCalledRead(5, BeastCalledSource.Familiar);
            Check(BeastComboTrace.Line(false, true, familiar, 44889, 44887, true, []).Contains("off (option)"), "option off");
            Check(BeastComboTrace.Line(true, false, familiar, 44889, 44887, true, []).Contains("not read (no reader)"), "no reader");
            Check(BeastComboTrace.Line(true, true, BeastCalledRead.None, 0, 0, false, []).Contains("no beast called read"), "no beast");
            Check(BeastComboTrace.Line(true, true, new BeastCalledRead(99, BeastCalledSource.Window), 0, 0, false, []).Contains("beast 99 has no known Trick affinity"), "no affinity");
            Check(BeastComboTrace.Line(true, true, familiar, 44889, 44887, false, []).Contains("hotbars not read yet"), "not read yet");
            Check(BeastComboTrace.Line(true, true, familiar, 44889, 44887, true, []).Contains("on no visible hotbar slot"), "not on the bars");
            var line = BeastComboTrace.Line(true, true, familiar, 44889, 44887, true, [new("_ActionBar", 3, false), new("_ActionBar01", 7, true)]);
            Check(line == "Beast combo highlight: beast 5 (Familiar), skills 44889 (1) and 44887 (2): 1 on _ActionBar slot 3, 2 on _ActionBar01 slot 7.", line);
        }),
        ("Beast called 1599: wired everywhere in PvE, read only, the bars' slots said in the log", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            var start = page.IndexOf("internal void DrawBeastComboHighlight()", StringComparison.Ordinal);
            var body = page[start..page.IndexOf("private readonly Dictionary<uint, byte[]> beastFontIcons", start, StringComparison.Ordinal)];
            Check(!body.Contains("BeastBoardOfTerritory"), "no longer in the labyrinth only (R03.1, J174)");
            Check(body.Contains("BeastCalledNow?.Invoke()") && body.Contains("log.Information(trace)"), "the beast called, the line said");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastFamiliarReader.Dalamud.cs"));
            Check(reader.Contains("npc.OwnerId != player.EntityId") && reader.Contains("CompetitiveGate.Add(framework, OnUpdate)") && !reader.Contains("UseAction") && !reader.Contains("Hook"),
                "the player's own battle NPCs, cut in PvP, nothing sent (R03.3)");
            var locator = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/ActionBarLocator.Dalamud.cs"));
            Check(locator.Contains("Hotbar slots read for the beast combo") && locator.Contains("!first && second,name,index+1)"), "the slots said, bar and slot kept");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("mainWindow.BeastCalledNow=()=>familiarReader?.Called ?? Beastmaster.BeastCalledRead.None;"), "wired");
        }),
    ];
}
