using TenshoAssistant.Beastmaster;

// 0.1522 (X-549, the user's report of 17/11 in the Beast Labyrinth): the panel follows the board the game is on, ranks read at a board's limit are
// floors, and the « ranks assumed » line counts the team's beasts only. Real data: the game's team window of 17/11 10:09 (dalamud.log, developer probe).
internal static class BeastBoardRead1522Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // The team window's texts as the probe wrote them (« Formation de l'équipe », 1st board, limit 5): each beast's attributes, its name, its rank.
    private const string TeamWindow = "Formation de l'équipe;Commencer le combat;Choisissez une équipe de bêtes pour vous accompagner dan;83;RÉS MAG;95;INT;125;VIT;88;RÉS PHY;154;FOR;Exp;608/608;PV;Cu sith;5;76;RÉS MAG;95;INT;137;VIT;76;RÉS PHY;154;FOR;Exp;667/667;PV;Opo-opo;5;76;RÉS MAG;95;INT;137;VIT;76;RÉS PHY;154;FOR;Exp;667/667;PV;Pampa;5;76;RÉS MAG;134;INT;137;VIT;76;RÉS PHY;40;FOR;Exp;667/667;PV;Piège-volant;5;76;RÉS MAG;134;INT;137;VIT;76;RÉS PHY;40;FOR;Exp;667/667;PV;Salamandre;5;98;RÉS MAG;95;INT;102;VIT;98;RÉS PHY;154;FOR;Exp;496/496;PV;Mandragore;5;83;RÉS MAG;122;INT;98;VIT;97;RÉS PHY;122;FOR;Exp;477/477;PV;Puk;3;100;RÉS MAG;128;INT;105;VIT;86;RÉS PHY;128;FOR;Exp;511/511;PV;Apkallu;4;83;RÉS MAG;83;INT;137;VIT;83;RÉS PHY;134;FOR;Exp;667/667;PV;Acarus;5;83;RÉS MAG;83;INT;137;VIT;83;RÉS PHY;134;FOR;Exp;667/667;PV;Mante;5;Exp;PV;Exp;PV;Exp;PV;Équipe conseillée;Bestiaire de dresseur;10/10";
    private static readonly string[] Names = ["Cu sith", "Opo-opo", "Pampa", "Piège-volant", "Salamandre", "Mandragore", "Puk", "Apkallu", "Acarus", "Mante"];

    private static Beast[] Beasts(int strong = 100)
    {
        BeastStats[] Ranks(int value) => Enumerable.Range(1, 25).Select(_ => new BeastStats(value, value, value, value, value)).ToArray();
        return Names.Select((name, index) => new Beast((uint)index + 1, name, 242000u + (uint)index + 1, "", "", "", "", Ranks(strong))).ToArray();
    }

    // The five boards as the game's sheets give them (XBMContent: team size, rank limit; ContentFinderCondition 1088 to 1092).
    private static Board[] Boards()
    {
        var enemy = new BoardEnemy("Chevalier du plateau", 0, "Contondant", [], new BeastStats(2, 1, 2, 2, 3), 7);
        return [.. new[] { (10, 5), (12, 10), (14, 15), (12, 20), (15, 25) }.Select((board, index) =>
            new Board((uint)index + 1, $"Board {index + 1}", 30, 30, board.Item1, board.Item2, [new BoardBattle(false, [enemy])], [], 1088u + (uint)index))];
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast board read 1522: the board the game is on: the labyrinth's territory first, then the game's window (either number), else the list", () =>
        {
            var boards = Boards();
            var (inside, source) = BeastTeams.Current(boards, 1, 1089);
            Check(inside?.Id == 1 && source == BoardSource.Labyrinth, "in the labyrinth, its territory's board, whatever the window says");
            Check(BeastTeams.Current(boards, 0, 1088) is ({ Id: 1 }, BoardSource.Window), "before entering, the window's ContentFinderCondition row");
            Check(BeastTeams.Current(boards, 0, 2) is ({ Id: 2 }, BoardSource.Window), "or its XBMContent row");
            Check(BeastTeams.Current(boards, 0, 0) is (null, BoardSource.Chosen) && BeastTeams.Current(boards, 0, 777) is (null, BoardSource.Chosen), "nothing read, or a number of no board: the list");
            Check(BeastTeams.Current(boards, 9, 0) is (null, BoardSource.Chosen), "a territory board not in the catalog");
            Check(boards[0].TeamSize == 10 && boards[0].RankLimit == 5, "the 1st board takes 10 beasts at rank 5 at most (the game showed 10/10)");
        }),
        ("Beast board read 1522: the real team window: ranks at the board's limit are floors, never put over a rank known higher", () =>
        {
            var beasts = Beasts();
            var read = BeastTeams.ParseRanks(TeamWindow.Split(';'), beasts);
            Check(read.Count == 10 && read[7] == 3 && read[8] == 4 && read[1] == 5, $"ten ranks read: {string.Join(",", read.Select(pair => $"{pair.Key}:{pair.Value}"))}");
            // Cu sith was rank 7 in the board's result (XBMResult « after » ranks), the window shows it brought down to 5.
            var (ranks, floors) = BeastTeams.MergeRanks(new Dictionary<uint, int> { [1] = 7 }, null, read, 5);
            Check(ranks[1] == 7 && !floors.Contains(1), "a rank known higher stays");
            Check(ranks[2] == 5 && floors.Contains(2), "a rank read at the limit is at least that");
            Check(ranks[7] == 3 && !floors.Contains(7) && ranks[8] == 4, "a rank read below the limit is the beast's own");
            var (again, floorsAgain) = BeastTeams.MergeRanks(ranks, floors, new Dictionary<uint, int> { [2] = 6 }, 10);
            Check(again[2] == 6 && !floorsAgain.Contains(2), "read below the limit later: exact, no longer a floor");
            var (unknownLimit, noFloor) = BeastTeams.MergeRanks(null, null, read, 0);
            Check(unknownLimit[1] == 5 && noFloor.Count == 0, "board not known: taken as read");
            Check(BeastTeams.Key(null, ranks, floors) != BeastTeams.Key(null, ranks, null), "the floors are part of the plan's key");
        }),
        ("Beast board read 1522: a floor is known on a board whose limit is not above it; « ranks assumed » counts the team only", () =>
        {
            var beasts = Beasts();
            var boards = Boards();
            var ranks = Names.Select((_, index) => ((uint)index + 1, 5)).ToDictionary(pair => pair.Item1, pair => pair.Item2);
            var floors = new HashSet<uint>(ranks.Keys);
            var held = new HashSet<uint>(ranks.Keys);
            var first = BeastTeams.Plan(boards[0], beasts, held, ranks, floors);
            Check(first.Team.Count == 10 && first.RanksUnknown == 0 && !first.RanksAssumed && first.Team.All(pick => pick.Rank == 5 && pick.RankKnown), "1st board: every rank known (5, the limit)");
            var second = BeastTeams.Plan(boards[1], beasts, held, ranks, floors);
            Check(second.RanksUnknown == 10 && second.RanksAssumed && second.Team.All(pick => pick.Rank == 10 && !pick.RankKnown), "2nd board (limit 10): « at least 5 » is not known");
            // Eleven beasts held, ten known and strong, one not read and weak even at the limit: the team is the ten, nothing assumed.
            var weak = new Beast(11, "Weak", 242011, "", "", "", "", Enumerable.Range(1, 25).Select(_ => new BeastStats(1, 1, 1, 1, 1)).ToArray());
            var plan = BeastTeams.Plan(boards[0], [.. beasts, weak], new HashSet<uint>(held) { 11 }, ranks, floors);
            Check(plan.Team.All(pick => pick.Beast != 11) && plan.RanksUnknown == 0 && !plan.RanksAssumed, "a beast outside the team whose rank is not read is not said");
        }),
        ("Beast board read 1522: the panel and the page follow the board read; the reader reads the windows' board", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("var (read, source) = CurrentBeastBoard(catalog);") && !page.Contains("if (TenshoAssistant.Competitive.CompetitiveGate.Cut)"), "the panel reads the board, not only during a cut");
            Check(page.Contains("FollowBeastBoard(catalog);\n        var board = DrawBeastBoardChoice(catalog);".Replace("\n", Environment.NewLine)) || page.Contains("FollowBeastBoard(catalog);\r\n        var board = DrawBeastBoardChoice(catalog);") || page.Contains("FollowBeastBoard(catalog);\n        var board = DrawBeastBoardChoice(catalog);"), "the page's list follows it");
            Check(page.Contains("BeastTeams.MergeRanks(plans.Ranks, plans.Floors, ranks, limit)") && page.Contains("\"BeastUi.TeamRanksAssumed\", unknown, board.RankLimit"), "ranks merged as floors; the team's unknown ranks said");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs"));
            Check(reader.Contains("AgentXBMStageDetailList.Instance()") && reader.Contains("stage->ContentId") && reader.Contains("team->ContentId"), "the reader reads the agents' board");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("mainWindow.BeastBoardContent=()=>boardReader?.State.Content ?? 0;"), "wired");
        }),
    ];
}
