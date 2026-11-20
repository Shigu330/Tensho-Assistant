using TenshoAssistant.Beastmaster;
using TenshoAssistant.Gathering;

// 0.1558 (X-585, review of block 0.1540 to 0.1550, minor findings of the beast labyrinth). M1: the 10 with an empty team weigh each fight's
// strongest enemy first (as its three, X-579). M2: a recommended range not seen in the game is said « to be confirmed ». M6: the combo's two
// skills numbered on the bars. M7: every list of beasts in the bestiary's order. M8: the page's help says the bestiary first.
internal static class BeastMinor1558Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const uint Fire = 1, Wind = 2, Lightning = 4, Ice = 5, Water = 6, Blunt = 7, Piercing = 8, Slashing = 9;

    private static Beast[] Beasts() => BeastPlan1543Tests.Held.Select(BeastPlan1543Tests.Make).ToArray();
    private static HashSet<uint> Held() => BeastPlan1543Tests.Held.Select(row => row.Id).ToHashSet();
    private static uint AutoOf(uint id) => BeastPlan1543Tests.Held.First(row => row.Id == id).Auto;

    private static BoardEnemy Enemy(string name, uint weakness, int power)
        => new(name, 0, "w" + weakness, [], new BeastStats(power, power, power, power, power), weakness);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast minor 1558 M1: a fight's rows, strongest enemy first; a lone enemy or enemies all as strong are the strongest", () =>
        {
            var guardian = BeastPlan1543Tests.FirstBoard().Battles[0];
            var (rows, top, _) = BeastTeams.Ranked(guardian.Enemies);
            Check(rows.Select(row => row.Row).SequenceEqual([Piercing, Fire]) && rows[1].Count == 2, "Perforant (Pas de Seul) before Feu (two succubi)");
            Check(top.SetEquals([Piercing]), "the guardian's strongest: Pas de Seul");
            Check(BeastTeams.Ranked([Enemy("lone", Lightning, 1)]).Top.SetEquals([Lightning]), "a lone enemy is its fight's strongest");
            Check(BeastTeams.Ranked([Enemy("a", Fire, 2), Enemy("b", Water, 2)]).Top.SetEquals([Fire, Water]), "all as strong: all strongest");
        }),
        ("Beast minor 1558 M1: seven places on the 1st board: every fight's strongest enemy hit, a weakness of weaker enemies left out", () =>
        {
            var plan = BeastTeams.Plan(BeastPlan1543Tests.FirstBoard(7), Beasts(), Held(), null);
            var rows = plan.Team.Select(pick => AutoOf(pick.Beast)).ToHashSet();
            foreach (var row in new[] { Piercing, Blunt, Lightning, Slashing, Ice, Wind })
                Check(rows.Contains(row), $"the strongest enemy's weakness {row} kept: {string.Join(",", rows)}");
            Check(plan.Left is [{ Reason: LeftReason.NoRoom, Weakness: Fire or Water }], "left: Feu or Eau (two weak enemies), never the guardian's Perforant");
        }),
        ("Beast minor 1558 M1: few places, the guardian's lone strong enemy over two weak ones of another fight (the case of the review)", () =>
        {
            var board = new Board(9, "b", 1, 1, 1, 5,
            [
                new BoardBattle(true, [Enemy("Boss", Lightning, 5)]),
                new BoardBattle(false, [Enemy("Weak 1", Fire, 1), Enemy("Weak 2", Fire, 1)]),
            ], []);
            var plan = BeastTeams.Plan(board, Beasts(), Held(), null);
            Check(plan.Team.Count == 1 && AutoOf(plan.Team[0].Beast) == Lightning, $"one place: the lightning beast (the boss), not fire (two weak): {AutoOf(plan.Team[0].Beast)}");
        }),
        ("Beast minor 1558 M2: the ranges seen in game (1 to 5, 6 to 10) are said plainly, the others « to be confirmed », both languages", () =>
        {
            Board Of(int floor, int limit) => new(1, "b", 1, 1, 10, limit, [], [], 0, floor);
            Check(BeastTeams.RankRangeSeen(Of(1, 5)) && BeastTeams.RankRangeSeen(Of(6, 10)), "the two captures");
            Check(!BeastTeams.RankRangeSeen(Of(11, 15)) && !BeastTeams.RankRangeSeen(Of(16, 20)), "boards 3 and 4 deduced");
            Check(BeastTeams.RankLowKey(Of(1, 5), false) == "BeastUi.RankLow" && BeastTeams.RankLowKey(Of(11, 15), true) == "BeastUi.TeamReadRankLowUnsure", "the keys");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"RankLowUnsure\": ") && text.Contains("\"TeamReadRankLowUnsure\": "), language + ": the two texts");
            }
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("localization.Format(BeastTeams.RankLowKey(board, true), ") && page.Contains("localization.Format(BeastTeams.RankLowKey(board, false), "), "both places use it");
        }),
        ("Beast minor 1558 M6: the bar's rectangle says which action it holds; the combo's skills numbered 1 (before the Trick) and 2 (after)", () =>
        {
            Check(!new ActionBarRect(0, 0, 1, 1).Second && new ActionBarRect(0, 0, 1, 1, true).Second, "Second defaults to false");
            Check(BeastInstinct.Mark(false) == "1" && BeastInstinct.Mark(true) == "2", "1 then 2");
            var locator = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/ActionBarLocator.Dalamud.cs"));
            // X-655 (0.1599): the rectangle also names its bar and slot, for the log.
            Check(locator.Contains("node->Height*node->ScaleY*scale,!first && second,name,index+1)"), "the locator marks the second action");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("var mark = BeastInstinct.Mark(rect.Second);"), "the highlight numbers each slot");
        }),
        ("Beast minor 1558 M7: the team read and the beasts named in a line follow the bestiary's numbers", () =>
        {
            Check(BeastTeams.BookOrder(new uint[] { 41, 2, 13 }).SequenceEqual([2u, 13u, 41u]), "ids");
            var team = new BeastTeams.TeamMember[] { new(22, 5, 1, 1), new(1, 5, 1, 1) };
            Check(BeastTeams.BookOrder(team).Select(member => member.Beast).SequenceEqual([1u, 22u]), "team read");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("foreach (var member in BeastTeams.BookOrder(team))") && page.Contains("BeastTeams.BookOrder(ids).Take(5)") && page.Contains("BeastTeams.BookOrder(lost.Lost)"), "the page's lists");
        }),
        ("Beast minor 1558 M8: the page's help says the bestiary first, both languages", () =>
        {
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json")).Contains("se lit d'abord dans le bestiaire de dresseur du jeu"), "fr");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json")).Contains("is read first in the game's beastmaster bestiary"), "en");
        }),
    ];
}
