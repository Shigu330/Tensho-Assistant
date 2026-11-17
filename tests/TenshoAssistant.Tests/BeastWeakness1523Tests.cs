using TenshoAssistant.Beastmaster;

// 0.1523 (X-553, the user's report of 17/11: « the help does not seem to make good teams for weakness first »): a magical beast's automatic
// attack has its element, so it hits elemental weaknesses; a weakness none of the three hits is said, with who would. Real data: the 1st board
// (XBMContentBattle 1, XBMBattleDetail rows 1 to 6, game 2026.11.03) and the user's team of 17/11 (the game's team window).
internal static class BeastWeakness1523Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const uint Fire = 1, Wind = 2, Lightning = 4, Ice = 5, Water = 6, Blunt = 7, Piercing = 8, Slashing = 9;

    private static BoardEnemy Enemy(string name, uint weakness, string weak, int strength, int intelligence, int physical, int magical, int vitality)
        => new(name, 0, weak, [], new BeastStats(strength, intelligence, physical, magical, vitality), weakness);

    // The 1st board as the sheets give it: fights 1 to 5, then the guardian (XBMBattleDetail 6: Pas de Seul and two succubi).
    private static Board FirstBoard() => new(1, "1er plateau", 30, 30, 10, 5,
    [
        new BoardBattle(false, [Enemy("Chevalier du plateau", Blunt, "Contondant", 2, 1, 2, 2, 3), Enemy("Évêque du plateau", Blunt, "Contondant", 1, 2, 2, 2, 2)]),
        new BoardBattle(false, [Enemy("Archidémon du plateau", Lightning, "Foudre", 3, 1, 3, 2, 4)]),
        new BoardBattle(false, [Enemy("Piscodémon du plateau", Slashing, "Tranchant", 1, 3, 2, 3, 4)]),
        new BoardBattle(false, [Enemy("Sarcopte du plateau", Ice, "Glace", 3, 3, 3, 2, 4), Enemy("Petit acarus du plateau", Ice, "Glace", 1, 1, 2, 1, 1)]),
        new BoardBattle(false, [Enemy("Ogre du plateau", Wind, "Vent", 4, 3, 4, 2, 4), Enemy("Feu follet du plateau", Water, "Eau", 1, 1, 1, 1, 1), Enemy("Grand feu follet du plateau", Water, "Eau", 1, 3, 1, 1, 2)]),
        new BoardBattle(true, [Enemy("Pas de Seul", Piercing, "Perforant", 2, 5, 3, 5, 5), Enemy("Succube mage du plateau", Fire, "Feu", 1, 2, 1, 1, 2), Enemy("Succube épéiste du plateau", Fire, "Feu", 2, 1, 1, 1, 2)]),
    ], []);

    // The user's ten (team window of 17/11) with their automatic attack as the game's beast book shows it, and a few beasts held outside the team.
    private static readonly (uint Id, string Name, uint Auto)[] Team =
    [
        (1, "Cu sith", Piercing), (5, "Opo-opo", Blunt), (22, "Pampa", Piercing), (20, "Piège-volant", Ice), (41, "Salamandre", Water),
        (12, "Mandragore", Blunt), (14, "Puk", Piercing), (24, "Apkallu", Piercing), (8, "Acarus", Piercing), (16, "Mante", Slashing),
    ];
    private static readonly (uint Id, string Name, uint Auto)[] Outside = [(7, "Coblyn", Lightning), (13, "Gespenst", Lightning), (11, "Vautour", Wind), (19, "Chauve-souris", 0)];

    private static Beast Make((uint Id, string Name, uint Auto) row)
        => new(row.Id, row.Name, 0, "", row.Auto == 0 ? "" : "x", "", "", Enumerable.Range(1, 10).Select(_ => new BeastStats(100, 100, 100, 100, 500)).ToArray(), "", row.Auto);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast weakness 1523: a magical attack's element is its XBMElement row, each matched against the game's beast book", () =>
        {
            Check(BeastCatalog.ElementOfAspect(1) == Fire && BeastCatalog.ElementOfAspect(2) == Ice && BeastCatalog.ElementOfAspect(3) == Wind
                  && BeastCatalog.ElementOfAspect(4) == 3 && BeastCatalog.ElementOfAspect(5) == Lightning && BeastCatalog.ElementOfAspect(6) == Water, "fire, ice, wind, earth, lightning, water");
            Check(BeastCatalog.ElementOfAspect(7) == 0 && BeastCatalog.ElementOfAspect(0) == 0 && BeastCatalog.ElementOfAspect(9) == 0, "unaspected (« Neutre ») and unknown: none");
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastCatalog.cs"));
            Check(code.Contains("5 => ElementOfAspect(aspect?.Invoke(action) ?? 0)"), "the catalog gives a magical beast its element");
            var loader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastCatalogLoader.Dalamud.cs"));
            Check(loader.Contains("aspect: id => actions.TryGetRow(id, out var action) ? action.Aspect : 0"), "the loader reads Action.Aspect");
        }),
        ("Beast weakness 1523: on the real 1st board, the user's ten put the weaknesses first: Piège-volant (ice) and Salamandre (water) now count", () =>
        {
            var board = FirstBoard();
            var team = Team.Select(Make).ToArray();
            var input = team.Select(beast => (beast.At(5)!, beast.AutoRow)).ToArray();
            var covered = board.Battles.Select(fight => BeastTeams.Best(input, fight.Enemies).Hit).ToArray();
            Check(covered.SequenceEqual([2, 0, 1, 2, 2, 1]), $"enemies whose weakness is hit, fights 1 to 5 and the guardian: {string.Join(",", covered)}");
            var fourth = BeastTeams.Best(input, board.Battles[3].Enemies).Picks.Select(index => team[index].Name).ToArray();
            Check(fourth.Contains("Piège-volant"), "the ice fight calls the ice beast (before, a magical beast hit nothing): " + string.Join(", ", fourth));
            // Before 0.1523 every magical beast counted 0: the ice and water fights hit nothing (the user's capture: 0 of 2, 0 of 3).
            var old = team.Select(beast => (beast.At(5)!, beast.AutoRow is Blunt or Piercing or Slashing ? beast.AutoRow : 0u)).ToArray();
            Check(BeastTeams.Best(old, board.Battles[3].Enemies).Hit == 0 && BeastTeams.Best(old, board.Battles[4].Enemies).Hit == 0, "the old reading missed them");
        }),
        ("Beast weakness 1523: a weakness none of the three hits is said, with the team's beasts, then the beasts held, that would hit it", () =>
        {
            var board = FirstBoard();
            var team = Team.Select(Make).ToArray();
            var beasts = team.Concat(Outside.Select(Make)).ToArray();
            var picks = team.Select(beast => new BeastPick(beast.Id, 5, true)).ToArray();
            BeastPick[] Trio(params string[] names) => names.Select(name => picks[Array.FindIndex(team, beast => beast.Name == name)]).ToArray();
            var held = new HashSet<uint>(beasts.Select(beast => beast.Id).Where(id => id != 13));
            var lightning = BeastTeams.Missed(board.Battles[1], Trio("Cu sith", "Opo-opo", "Pampa"), picks, beasts, held);
            Check(lightning.Count == 1 && lightning[0] is { Weakness: Lightning, Name: "Foudre", InTeam.Count: 0 } && lightning[0].Held.SequenceEqual([7u]), "lightning: none in the team, the Coblyn held would (the Gespenst is not held)");
            Check(BeastTeams.Missed(board.Battles[1], Trio("Cu sith", "Opo-opo", "Pampa"), picks, beasts, null)[0].Held.SequenceEqual([7u, 13u]), "beasts held not read: every beast that would");
            var ice = BeastTeams.Missed(board.Battles[3], Trio("Cu sith", "Opo-opo", "Pampa"), picks, beasts, held);
            Check(ice.Count == 1 && ice[0].InTeam.SequenceEqual([20u]), "ice: Piège-volant, in the team, would hit it");
            var fifth = BeastTeams.Missed(board.Battles[4], Trio("Salamandre", "Cu sith", "Pampa"), picks, beasts, held);
            Check(fifth.Count == 1 && fifth[0].Name == "Vent" && fifth[0].Held.SequenceEqual([11u]), "fight 5: water hit (said once for both wisps), wind missed, the Vautour would");
            Check(BeastTeams.Missed(board.Battles[0], Trio("Opo-opo", "Mandragore", "Cu sith"), picks, beasts, held).Count == 0, "every weakness hit: nothing said");
        }),
        ("Beast weakness 1523: the panel says it and no longer says it turns off in a fight", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("BeastTeams.Missed(fight, battle.Team, plan.Team, catalog.Beasts, Beasts?.State.Obtained)") && page.Contains("\"BeastUi.MissNone\""), "the missed weaknesses drawn");
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(!fr.Contains("ce panneau se coupe quand un combat commence") && fr.Contains("le Dédale bestial n'est pas coupé, combats compris") && en.Contains("the Crucible of the Unbroken is not cut, fights included"), "R03.2: not cut, fights included");
        }),
    ];
}
