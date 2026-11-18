using TenshoAssistant.Beastmaster;

// 0.1550 (X-136, the user's request of 11/11: « in combat, highlight the Instinct II combo skills according to the beast called (option, off
// by default) »; R03.2: the labyrinth is not cut, combat included; R03.3: nothing is sent to the game). The beast: the game's « Cornes
// d'appel » window (XBMActivePet), probe lines of 17/11 23:41 (value 4 « Mante », value 19 44965 = 44933 + 2 × 16), docs/evidence/0.1550-beast-combo-highlight.md.
internal static class BeastComboHighlight1550Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static Beast Make(uint id, string name) => new(id, name, 0, "", "", "", "", [new BeastStats(1, 1, 1, 1, 1)], "", 0);
    private static readonly Beast[] Beasts = [Make(1, "Cu sith"), Make(16, "Mante"), Make(22, "Pampa")];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast combo highlight 1550: the window's Trick and name give the beast; anything that does not agree gives none", () =>
        {
            Check(BeastInstinct.FromWindow(44965, "Mante", Beasts) == 16, "Mante: Trick 44965, value 4 « Mante » (the probe's line)");
            Check(BeastInstinct.FromWindow(44935, "Cu sith", Beasts) == 1, "Cu sith: Trick 44935 (the probe's line of 18/11 00:02)");
            Check(BeastInstinct.FromWindow(44965, "Pampa", Beasts) == 0, "name and Trick of two beasts");
            Check(BeastInstinct.FromWindow(44966, "Mante", Beasts) == 0 && BeastInstinct.FromWindow(44933, "", Beasts) == 0 && BeastInstinct.FromWindow(0, "Mante", Beasts) == 0, "not a Trick");
            Check(BeastInstinct.FromWindow(44933 + 2 * 51, "Mante", Beasts) == 0, "beyond the fifty beasts");
        }),
        ("Beast combo highlight 1550: the two skills of the Cu sith's combos (its Trick is Rampant: the Volant skill before, the Durant one after, as the game's tutorial)", () =>
        {
            var combos = BeastInstinct.Combos(1);
            Check(combos is [{ Skill: 44889, AfterTrick: false }, { Skill: 44887, AfterTrick: true }], $"Cu sith: {string.Join(", ", combos)}");
        }),
        ("Beast combo highlight 1550: an option off by default, in the labyrinth only, read and drawn without sending anything", () =>
        {
            Check(!new TenshoAssistant.Configuration().HighlightBeastCombos, "off by default");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("if (!configuration.HighlightBeastCombos || BeastBarLocator is not { } located) return;")
                  && page.Contains("if ((BeastBoardOfTerritory?.Invoke(CurrentTerritory?.Invoke() ?? 0) ?? 0) == 0) return;")
                  && page.Contains("located.Want(before.Skill, after.Skill);"), "option, labyrinth, both skills asked");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs"));
            Check(reader.Contains("gui.GetAddonByName(\"XBMActivePet\")") && reader.Contains("BeastInstinct.FromWindow(trick, name, known)"), "the window read");
            var locator = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/ActionBarLocator.Dalamud.cs"));
            Check(!locator.Contains("UseAction") && !locator.Contains("Hook") && !reader.Contains("UseAction"), "nothing sent to the game (R03.3)");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("(\"BeastCombo\",mainWindow.DrawBeastComboHighlight)") && plugin.Contains("mainWindow.BeastActive=()=>boardReader?.ActiveBeast ?? 0;"), "drawn each frame, wired");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"ComboHighlight\": \"") && text.Contains(language == "en" ? "Default: off." : "Par défaut : désactivé."), language);
            }
        }),
    ];
}
