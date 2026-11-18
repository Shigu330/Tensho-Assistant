using TenshoAssistant.Beastmaster;

// 0.1547 (X-579, the user's request of 18/11 around 00:25, capture of the Guardian's call order in 0.1546: « careful, it takes every monster,
// even those the boss summons, while the weaknesses must be of the strongest monster first »). The sheets mark no enemy as summoned or as the
// leader (docs/evidence/0.1547-beast-strongest.md): each enemy weighs by the sum of its stars (XBMBattleDetail columns 1 to 5). Real data: the
// 1st board's Guardian (XBMBattleDetail 6: Pas de Seul 2, 5, 3, 5, 5 = 20, Piercing; the two succubi 7 each, Fire).
internal static class BeastStrongest1547Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const uint Fire = 1, Blunt = 7, Piercing = 8;

    private static (BeastStats, uint) Beast(uint auto, int strength = 100) => (new BeastStats(strength, strength, 100, 100, 500), auto);

    private static BoardBattle Guardian() => BeastPlan1543Tests.FirstBoard().Battles[0];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast strongest 1547: the Guardian's strongest is Pas de Seul (20 stars); hitting it weighs more than hitting both succubi", () =>
        {
            var guardian = Guardian();
            Check(BeastTeams.Strongest(guardian)?.Name == "Pas de Seul", "Pas de Seul the strongest");
            Check(BeastTeams.Power(guardian.Enemies[0]) == 20 && BeastTeams.Power(guardian.Enemies[1]) == 7 && BeastTeams.Power(guardian.Enemies[2]) == 7, "20, 7, 7");
            var weights = BeastTeams.Weights(guardian.Enemies);
            Check(weights[0] > weights[1] + weights[2] && weights[1] == weights[2], $"weights {string.Join(",", weights)}");
        }),
        ("Beast strongest 1547: the Guardian's three: a piercing beast called first, then the fire one; with one place only, the piercing one", () =>
        {
            var enemies = Guardian().Enemies;
            var (picks, hit) = BeastTeams.Best([Beast(Fire), Beast(Blunt, 400), Beast(Piercing), Beast(Piercing)], enemies);
            Check(hit == 3 && (picks[0] is 2 or 3) && picks.Contains(0), $"piercing first, fire called, 3 of 3: {string.Join(",", picks)} ({hit})");
            var (one, oneHit) = BeastTeams.Best([Beast(Fire), Beast(Piercing)], enemies, 1);
            Check(one.SequenceEqual([1]) && oneHit == 1, $"one place: the piercing one (Pas de Seul) over the fire one (two succubi): {string.Join(",", one)}");
            var (none, noneHit) = BeastTeams.Best([Beast(Blunt, 400), Beast(Fire)], enemies);
            Check(none[0] == 1 && noneHit == 2, "no piercing beast: the fire one first, it still hits the succubi");
        }),
        ("Beast strongest 1547: enemies all as strong (stars not read): the count of before, the beast hitting the most first", () =>
        {
            var enemies = Guardian().Enemies.Select(enemy => enemy with { Stars = null }).ToArray();
            Check(BeastTeams.Weights(enemies).All(weight => weight == 1) && BeastTeams.Strongest(Guardian() with { Enemies = enemies }) is null, "weights 1, no strongest");
            var (picks, _) = BeastTeams.Best([Beast(Piercing), Beast(Fire)], enemies);
            Check(picks[0] == 1, "fire (two enemies) first when no one is stronger");
        }),
        ("Beast strongest 1547: the reason names the strongest first; the panel marks it, both languages", () =>
        {
            var fight = new BoardBattle(false, [Guardian().Enemies[1], Guardian().Enemies[0] with { WeaknessRow = Fire, Weakness = "Feu" }]);
            var beast = BeastPlan1543Tests.Make((2, "Écureuil", Fire));
            Check(BeastTeams.Hits(fight, beast).SequenceEqual(["Pas de Seul", "Succube mage du plateau"]), "strongest first whatever the sheet's order");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("name == strongest ? localization.Format(\"BeastUi.Strongest\", name) : name"), "the call text marks the strongest");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"Strongest\": \"{0} ("), language + ": the mark");
            }
        }),
    ];
}
