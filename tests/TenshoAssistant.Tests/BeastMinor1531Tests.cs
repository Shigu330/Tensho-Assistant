using TenshoAssistant.Beastmaster;

// 0.1531 (X-562, review of 0.1526, minor findings M2 to M5): a plan kept before 0.1522 still says its assumed ranks; Missed is kept, not worked
// out at each frame; the result's numbers 47 onwards are said in the log; « Magical » is the game's word (Addon 13101).
internal static class BeastMinor1531Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast minor 1531: M2: a plan kept before 0.1522 (no count) counts its beasts whose rank is assumed", () =>
        {
            BeastPick[] team = [new(1, 5, true), new(2, 5, false), new(3, 5, false)];
            var old = new BoardPlan(1, team, [], true);
            Check(BeastTeams.RanksUnknownOf(old) == 2, $"two assumed: {BeastTeams.RanksUnknownOf(old)}");
            Check(BeastTeams.RanksUnknownOf(old with { RanksUnknown = 4 }) == 4, "a count kept is used as it is");
            Check(BeastTeams.RanksUnknownOf(new BoardPlan(1, [new(1, 5, true)], [], false)) == 0, "all known: none");
            var restored = BeastTeams.Read(BeastTeams.Write(new BeastTeams.SavedPlans(new() { [1] = old }, new() { [1] = "k" })));
            Check(restored is { } saved && BeastTeams.RanksUnknownOf(saved.Plans[1]) == 2, "read back from disk: still two");
        }),
        ("Beast minor 1531: M3, M4, M5: Missed kept per plan; the result's numbers 47 onwards in the log; the game's word for a magical attack", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("foreach (var miss in BeastMissed(plan, battle, fight, catalog))") && page.Contains("if (kept.Fights.TryGetValue(battle.Battle, out var known)) return known;"), "kept");
            Check(page.Contains("BeastTeams.RanksUnknownOf(plan) is > 0 and var unknown"), "M2 on the panel");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs"));
            Check(reader.Contains("Beastmaster board result, numbers 47 to 62:"), "M4: said in the log");
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(en.Contains("\"Magical\": \"Magical\",") && fr.Contains("\"Magical\": \"Magique\","), "Addon 13101 in both languages");
        }),
    ];
}
