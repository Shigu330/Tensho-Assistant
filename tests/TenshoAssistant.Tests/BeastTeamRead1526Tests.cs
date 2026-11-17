using TenshoAssistant.Beastmaster;

// 0.1526 (X-550, the user's request of 17/11 in the Beast Labyrinth): the three of each fight are taken among the team the player chose in the
// game's team window, beasts down left out, low HP said, and the advised beasts missing said with those that take their place. Real data: the
// team window of 17/11 10:09 (the same texts as BeastBoardRead1522Tests) and the 1st board's fights 1 and 4 (BeastWeakness1523Tests).
internal static class BeastTeamRead1526Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const string TeamWindow = "Formation de l'équipe;Commencer le combat;Choisissez une équipe de bêtes pour vous accompagner dan;83;RÉS MAG;95;INT;125;VIT;88;RÉS PHY;154;FOR;Exp;608/608;PV;Cu sith;5;76;RÉS MAG;95;INT;137;VIT;76;RÉS PHY;154;FOR;Exp;667/667;PV;Opo-opo;5;76;RÉS MAG;95;INT;137;VIT;76;RÉS PHY;154;FOR;Exp;667/667;PV;Pampa;5;76;RÉS MAG;134;INT;137;VIT;76;RÉS PHY;40;FOR;Exp;667/667;PV;Piège-volant;5;76;RÉS MAG;134;INT;137;VIT;76;RÉS PHY;40;FOR;Exp;667/667;PV;Salamandre;5;98;RÉS MAG;95;INT;102;VIT;98;RÉS PHY;154;FOR;Exp;496/496;PV;Mandragore;5;83;RÉS MAG;122;INT;98;VIT;97;RÉS PHY;122;FOR;Exp;477/477;PV;Puk;3;100;RÉS MAG;128;INT;105;VIT;86;RÉS PHY;128;FOR;Exp;511/511;PV;Apkallu;4;83;RÉS MAG;83;INT;137;VIT;83;RÉS PHY;134;FOR;Exp;667/667;PV;Acarus;5;83;RÉS MAG;83;INT;137;VIT;83;RÉS PHY;134;FOR;Exp;667/667;PV;Mante;5;Exp;PV;Exp;PV;Équipe conseillée;Bestiaire de dresseur;10/10";
    private const uint Ice = 5, Blunt = 7, Piercing = 8, Slashing = 9, Water = 6;
    private static readonly (uint Id, string Name, uint Auto)[] Rows =
    [
        (1, "Cu sith", Piercing), (5, "Opo-opo", Blunt), (22, "Pampa", Piercing), (20, "Piège-volant", Ice), (41, "Salamandre", Water),
        (12, "Mandragore", Blunt), (14, "Puk", Piercing), (24, "Apkallu", Piercing), (8, "Acarus", Piercing), (16, "Mante", Slashing), (27, "Uragnite", Ice),
    ];
    private static Beast[] Beasts() => Rows.Select(row => new Beast(row.Id, row.Name, 0, "", "", "", "", Enumerable.Range(1, 10).Select(_ => new BeastStats(100, 100, 100, 100, 500)).ToArray(), "", row.Auto)).ToArray();

    private static BoardEnemy Enemy(string name, uint weakness) => new(name, 0, "", [], new BeastStats(2, 2, 2, 2, 3), weakness);
    private static Board Board() => new(1, "1er plateau", 30, 30, 10, 5,
        [new BoardBattle(false, [Enemy("Chevalier du plateau", Blunt), Enemy("Évêque du plateau", Blunt)]), new BoardBattle(false, [Enemy("Sarcopte du plateau", Ice), Enemy("Petit acarus du plateau", Ice)])], []);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast team read 1526: the real team window gives the ten taken, their rank and their HP", () =>
        {
            var team = BeastTeams.ParseTeam(TeamWindow.Split(';'), Beasts());
            Check(team.Count == 10 && team[0] == new BeastTeams.TeamMember(1, 5, 608, 608) && team[6] == new BeastTeams.TeamMember(14, 3, 477, 477), "ten beasts in the window's order: " + string.Join(", ", team));
            Check(team.All(member => !member.Down && !member.Low), "all at full HP");
            var hurt = BeastTeams.ParseTeam(TeamWindow.Replace("608/608;PV;Cu sith", "0/608;PV;Cu sith").Replace("667/667;PV;Opo-opo", "150/667;PV;Opo-opo").Split(';'), Beasts());
            Check(hurt[0].Down && !hurt[0].Low && hurt[1].Low && !hurt[1].Down, "0 HP: down; under 30 %: low");
            var noHp = BeastTeams.ParseTeam(TeamWindow.Replace("608/608;PV;Cu sith", "PV;PV;Cu sith").Split(';'), Beasts());
            Check(noHp[0] is { Hp: -1, MaxHp: 0, Down: false }, "HP not read: unknown, never « down »");
        }),
        ("Beast team read 1526: the three of each fight are taken among the team read, beasts down left out, the advised ones missing said", () =>
        {
            var beasts = Beasts();
            var board = Board();
            var team = BeastTeams.ParseTeam(TeamWindow.Split(';'), beasts);
            // The advised plan called Uragnite (ice), not taken, in the ice fight, and Opo-opo and Mandragore in the blunt fight.
            var advised = new BoardPlan(1, [], [new BattlePlan(0, [new(5, 5, true), new(12, 5, true), new(1, 5, true)], 2, 2), new BattlePlan(1, [new(27, 5, true), new(20, 5, true), new(1, 5, true)], 2, 2)], false);
            var fights = BeastTeams.FromTeam(board, advised, team, beasts);
            Check(fights.Count == 2 && fights[0].Hit == 2 && fights[0].Trio.Contains(5u) && fights[0].Trio.Contains(12u) && fights[0].Lost.Count == 0, "blunt fight: Opo-opo and Mandragore, nothing missing");
            Check(fights[1].Trio.Contains(20u) && fights[1].Hit == 2 && fights[1].Lost.SequenceEqual([27u]) && !fights[1].Instead.Contains(20u) && fights[1].Instead.Count == 1, "ice fight: Uragnite missing, one beast in its place");
            var down = team.Select(member => member.Beast == 20 ? member with { Hp = 0 } : member).ToArray();
            var without = BeastTeams.FromTeam(board, advised, down, beasts);
            Check(!without[1].Trio.Contains(20u) && without[1].Hit == 0 && without[1].Lost.SequenceEqual([27u, 20u]), "Piège-volant down: not called, said missing too");
            Check(BeastTeams.FromTeam(board, null, team, beasts).All(fight => fight.Lost.Count == 0 && fight.Trio.Count == 3), "no advised plan: the three alone");
        }),
        ("Beast team read 1526: read with the team window, shown first on the panel", () =>
        {
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs"));
            Check(reader.Contains("team = BeastTeams.ParseTeam(texts, known);"), "read with the ranks");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("if (BeastTeamRead?.Invoke() is { Count: > 0 } team)") && page.Contains("BeastTeams.FromTeam(board, plan, team, catalog.Beasts)"), "drawn before the advised plan");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("mainWindow.BeastTeamRead=()=>boardReader?.State.Team;"), "wired");
        }),
    ];
}
