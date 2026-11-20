using TenshoAssistant.Beastmaster;

// 0.1661 (X-692, the user's tests T-0.1526-1 and -2 of 20/11 in the Beast Labyrinth: in front of « Formation de l'équipe » the panel showed the
// advised beasts, Mantis, Opo-opo and Pampa, which the player does not have, and never the team read). Real data: the game's team window of
// 20/11 22:04:23 (dalamud.log, read byte by byte): each rank comes after the game's glyph U+E036, which the reading did not expect.
// docs/evidence/0.1661-beast-team-rank-glyph.md.
internal static class BeastTeamGlyph1661Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const string TeamWindow = "Formation de l'équipe;Commencer le combat;Sélectionner les bêtes que vous souhaitez appeler en \u0002\u0010\u0001\u0003com;83;RÉS MAG;60;INT;125;VIT;97;RÉS PHY;60;FOR;Exp;608/608;PV;Chimère;3;78;RÉS MAG;50;INT;113;VIT;91;RÉS PHY;50;FOR;Exp;550/550;PV;Mouton;1;91;RÉS MAG;48;INT;113;VIT;74;RÉS PHY;25;FOR;Exp;550/550;PV;Pugil;1;78;RÉS MAG;122;INT;110;VIT;78;RÉS PHY;37;FOR;Exp;535/535;PV;Dodo;3;80;RÉS MAG;48;INT;100;VIT;99;RÉS PHY;25;FOR;Exp;487/487;PV;Coblyn;1;99;RÉS MAG;50;INT;100;VIT;85;RÉS PHY;25;FOR;Exp;487/487;PV;Mégalocrabe;1;83;RÉS MAG;122;INT;98;VIT;97;RÉS PHY;122;FOR;Exp;477/477;PV;Puk;3;78;RÉS MAG;80;INT;100;VIT;91;RÉS PHY;40;FOR;Exp;487/487;PV;Crabe;1;81;RÉS MAG;90;INT;109;VIT;95;RÉS PHY;43;FOR;Exp;530/530;PV;Gluant;4;100;RÉS MAG;128;INT;105;VIT;86;RÉS PHY;128;FOR;Exp;511/511;PV;Apkallu;4;Exp;PV;Exp;PV;Exp;PV;Exp;PV;Exp;PV;Exp;PV;Exp;PV;Bêtes assignées;10/10";

    private const uint Ice = 5, Blunt = 7, Piercing = 8, Slashing = 9, Water = 6, Fire = 2;
    // The names as the client's Pet sheet writes them (French, lower case; XbmProbe of 21/11); the player's ten, then the three advised.
    private static readonly (uint Id, string Name, uint Auto)[] Rows =
    [
        (38, "chimère", Fire), (3, "mouton", Blunt), (4, "pugil", Water), (6, "dodo", Piercing), (7, "coblyn", Blunt), (9, "mégalocrabe", Slashing),
        (14, "puk", Piercing), (15, "crabe", Water), (17, "gluant", Water), (24, "apkallu", Piercing),
        (16, "mante", Slashing), (5, "opo-opo", Blunt), (22, "pampa", Ice),
    ];
    private static readonly uint[] Ten = [38, 3, 4, 6, 7, 9, 14, 15, 17, 24];
    private static Beast[] Beasts() => Rows.Select(row => new Beast(row.Id, row.Name, 0, "", "", "", "", Enumerable.Range(1, 10).Select(_ => new BeastStats(100, 100, 100, 100, 500)).ToArray(), "", row.Auto)).ToArray();
    private static BoardEnemy Enemy(string name, uint weakness) => new(name, 0, "", [], new BeastStats(2, 2, 2, 2, 3), weakness);
    private static Board Board() => new(1, "1er plateau", 30, 30, 10, 5,
        [new BoardBattle(false, [Enemy("Chevalier du plateau", Blunt), Enemy("Évêque du plateau", Blunt)]), new BoardBattle(false, [Enemy("Sarcopte du plateau", Ice), Enemy("Petit acarus du plateau", Ice)])], []);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast team glyph 1661: the real window of 20/11 gives the player's ten beasts, their rank after the game's glyph, their HP", () =>
        {
            var team = BeastTeams.ParseTeam(TeamWindow.Split(';'), Beasts());
            Check(team.Select(member => member.Beast).SequenceEqual(Ten), "the ten, in the window's order: " + string.Join(", ", team));
            Check(team.Select(member => member.Rank).SequenceEqual([3, 1, 1, 3, 1, 1, 3, 1, 4, 4]), "the ranks after the glyph");
            Check(team[0] == new BeastTeams.TeamMember(38, 3, 608, 608) && team[9] == new BeastTeams.TeamMember(24, 4, 511, 511) && team.All(member => !member.Down && !member.Low), "HP read, all up");
            var ranks = BeastTeams.ParseRanks(TeamWindow.Split(';'), Beasts());
            Check(ranks.Count == 10 && ranks[38] == 3 && ranks[17] == 4 && !ranks.ContainsKey(16), "the ranks merged too, only the beasts shown");
        }),
        ("Beast team glyph 1661: a beast down or low on HP is said; the three of each fight are taken among the team, never the advised beasts the player lacks", () =>
        {
            var beasts = Beasts();
            var hurt = BeastTeams.ParseTeam(TeamWindow.Replace("608/608;PV;Chimère", "0/608;PV;Chimère").Replace("550/550;PV;Mouton", "100/550;PV;Mouton").Split(';'), beasts);
            Check(hurt[0].Down && hurt[1].Low && !hurt[1].Down, "down, low");
            // The advised plan called Mantis, Opo-opo and Pampa (the capture of 20/11 22:04), none of them taken.
            var advised = new BoardPlan(1, [], [new BattlePlan(0, [new(16, 5, true), new(5, 5, true), new(22, 5, true)], 2, 2), new BattlePlan(1, [new(16, 5, true), new(5, 5, true), new(22, 5, true)], 2, 2)], false);
            var fights = BeastTeams.FromTeam(Board(), advised, hurt, beasts);
            Check(fights.Count == 2 && fights.All(fight => fight.Trio.Count == 3 && fight.Trio.All(id => Ten.Contains(id) && id != 38)), "three of the player's own, the beast down left out");
            Check(fights.All(fight => fight.Lost.OrderBy(id => id).SequenceEqual(new uint[] { 5, 16, 22 }) && fight.Instead.Count > 0), "the advised ones missing said, with those in their place");
            Check(fights[0].Trio.Any(id => id is 3 or 7) && fights[0].Hit == 2, "the blunt fight: Mouton or Coblyn (blunt) hits both weaknesses");
            Check(fights[1].Hit == 0, "the ice fight: no ice beast in the team (Pampa is not the player's), none hit, not hidden");
        }),
        ("Beast team glyph 1661: a rank is the number after the game's glyphs and spaces, nothing else", () =>
        {
            Check(BeastTeams.TryRank("3", out var a) && a == 3 && BeastTeams.TryRank("3", out var b) && b == 3 && BeastTeams.TryRank("  12 ", out var c) && c == 12, "read");
            Check(!BeastTeams.TryRank("", out _) && !BeastTeams.TryRank("3a", out _) && !BeastTeams.TryRank("-3", out _) && !BeastTeams.TryRank("Exp", out _) && !BeastTeams.TryRank("", out _), "refused");
        }),
        ("Beast team glyph 1661: a window naming beasts with no team read is told apart (said once in the log); the reading is said when it changes", () =>
        {
            var beasts = Beasts();
            var unread = TeamWindow.Replace("", "Rang ").Split(';');
            var none = BeastTeams.ParseTeam(unread, beasts);
            Check(none.Count == 0 && BeastTeams.NamesWithoutTeam(unread, beasts, none), "names, no team: said");
            Check(!BeastTeams.NamesWithoutTeam(TeamWindow.Split(';'), beasts, BeastTeams.ParseTeam(TeamWindow.Split(';'), beasts)) && !BeastTeams.NamesWithoutTeam(["Formation de l'équipe"], beasts, []), "read, or no beast named: nothing said");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs"));
            Check(reader.Contains("if (!teamWarned && BeastTeams.NamesWithoutTeam(texts, known, team))") && reader.Contains("Beastmaster team window read: {0} beasts, {1} down, {2} low."), "logged");
        }),
    ];
}
