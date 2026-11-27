using TenshoAssistant.Beastmaster;

// 0.1544 (X-575 part 4, the user's request of 17/11 around 23:25: « the best beasts for the fights, knowing they come out one at a time:
// weakness first, then the rest, stats and others »): each fight's three in calling order, the best first: the enemies whose weakness it hits,
// then its attack against their resistances, then its defence; the rank counts only through the attributes it gives. Real data: the 1st board
// and the user's beasts of BeastPlan1543Tests. 0.1547 (X-579, the user's request of 18/11: « the strongest monster's weakness first »): the
// enemies hit count by their weight (BeastTeams.Weights), no longer by their number: Pas de Seul before the two succubi, the ogre before
// the two will-o'-wisps.
internal static class BeastCallOrder1544Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static Beast With(uint id, string name, uint auto, int strength, int intelligence)
        => new(id, name, 0, "", "x", "", "", Enumerable.Range(1, 10).Select(_ => new BeastStats(strength, intelligence, 100, 100, 500)).ToArray(), "", auto);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast call order 1544: the six fights of the 1st board, the plan's three called by the weight of the enemies hit (0.1547: the strongest first), then attack, then defence", () =>
        {
            var board = BeastPlan1543Tests.FirstBoard();
            var beasts = BeastPlan1543Tests.Held.Select(BeastPlan1543Tests.Make).ToArray();
            var plan = BeastTeams.Plan(board, beasts, BeastPlan1543Tests.Held.Select(row => row.Id).ToHashSet(), null);
            Check(plan.Battles.Count == 6, "six fights");
            foreach (var battle in plan.Battles)
            {
                var fight = board.Battles[battle.Battle];
                var weights = BeastTeams.Weights(fight.Enemies);
                long Priority(uint id) { var beast = beasts.First(row => row.Id == id); return fight.Enemies.Select((enemy, at) => beast.AutoRow != 0 && enemy.WeaknessRow == beast.AutoRow ? weights[at] : 0L).Sum(); }
                var hits = battle.Team.Select(pick => Priority(pick.Beast)).ToArray();
                Check(hits.Zip(hits.Skip(1)).All(pair => pair.First >= pair.Second), $"fight {battle.Battle}: enemies hit by weight, the most first ({string.Join(",", hits)})");
                var strongest = BeastTeams.Strongest(fight) ?? fight.Enemies[0];
                Check(beasts.First(beast => beast.Id == battle.Team[0].Beast).AutoRow == strongest.WeaknessRow, $"fight {battle.Battle}: the first called hits the strongest enemy's weakness ({strongest.Name})");
            }
            var guardian = plan.Battles.First(battle => battle.Battle == 0).Team.Select(pick => BeastPlan1543Tests.Held.First(row => row.Id == pick.Beast).Auto).ToArray();
            Check(guardian[0] == 8 && guardian.Contains(1u), $"guardian: a piercing beast first (Pas de Seul, the strongest), a fire one (two succubi) after ({string.Join(",", guardian)})");
            var fifth = plan.Battles.First(battle => battle.Battle == 5).Team.Select(pick => BeastPlan1543Tests.Held.First(row => row.Id == pick.Beast).Auto).ToArray();
            Check(fifth[0] == 2 && fifth.Contains(6u), $"fight 5: a wind beast first (the ogre, the strongest), a water one (two will-o'-wisps) after ({string.Join(",", fifth)})");
        }),
        ("Beast call order 1544: the user's ten of the capture: a piercing beast first at the guardian (0.1547), Écureuil called; Salamandre first in fight 5 (no wind beast in the ten)", () =>
        {
            var beasts = BeastPlan1543Tests.Held.Select(BeastPlan1543Tests.Make).ToArray();
            var team = new uint[] { 1, 5, 22, 20, 41, 12, 27, 2, 8, 16 }.Select(id => new BeastTeams.TeamMember(id, 7, 600, 600)).ToArray();
            var fights = BeastTeams.FromTeam(BeastPlan1543Tests.FirstBoard(), null, team, beasts);
            var guardian = fights.First(fight => fight.Battle == 0).Trio;
            Check(guardian[0] is 1 or 22 or 8 && guardian.Contains(2u), $"guardian: Cu sith, Pampa or Acarus first (Pas de Seul), Écureuil called ({string.Join(",", guardian)})");
            Check(fights.First(fight => fight.Battle == 5).Trio[0] == 41, "fight 5: Salamandre first");
            Check(fights.First(fight => fight.Battle == 4).Trio[0] is 20 or 27, "fight 4: an ice beast first");
        }),
        ("Beast call order 1544: with the same weaknesses hit, the better attack against the enemies' resistances is called first", () =>
        {
            var board = BeastPlan1543Tests.FirstBoard();
            // The ice fight: two ice beasts (both hit 2), the stronger magic first; a third that hits nothing comes last whatever its attack.
            var weak = With(1, "Weak ice", 5, 50, 80);
            var strong = With(2, "Strong ice", 5, 50, 200);
            var brute = With(3, "Brute", 7, 400, 400);
            var (picks, hit) = BeastTeams.Best([(weak.At(1)!, weak.AutoRow), (brute.At(1)!, brute.AutoRow), (strong.At(1)!, strong.AutoRow)], board.Battles[4].Enemies);
            Check(hit == 2 && picks.SequenceEqual([2, 0, 1]), $"strong ice, weak ice, then the brute: {string.Join(",", picks)}");
        }),
        ("Beast call order 1544: one text for the plan, the team taken and the fight shown; the plans are made again (rules' version 3)", () =>
        {
            Check(BeastTeams.PlanVersion >= 4, "plans of 0.1543 and 0.1544 made again (calling order, then the strongest first)");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("DrawBeastCallOrder(fight, battle.Team.Select(pick => pick.Beast).ToArray(), catalog);") && page.Contains("DrawBeastCallOrder(battle, fight.Trio, catalog);")
                  && page.Contains("ImGui.TextUnformatted(BeastCallText(fight, place, id, catalog));"), "the plan, the team taken and the fight shown use the calling order");
            Check(!page.Contains("ImGui.TextUnformatted(\"→ \""), "no unordered list left");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"FightReasonHits\": \"{0}. {1}") && text.Contains("\"FightReasonNone\": \"{0}. {1}") && text.Contains("\"CallOrder\": \""), language + ": numbered lines");
            }
        }),
    ];
}
