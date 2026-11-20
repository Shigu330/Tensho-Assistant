using TenshoAssistant.Beastmaster;

// 0.1543 (X-575, the user's report of 17/11 around 23:00 in the labyrinth, two captures of the panel on the 1st board, 0.1542: « I do not
// understand why there is orange if I have the beasts »). Cause: the plan kept in BeastTeams.json was made before 0.1523 (magical beasts hit
// nothing then) and its key held only the beasts held and their ranks: it was never made again. And with an empty team the 10 were the beasts
// chosen in the most fights, not those covering the board's weaknesses. Real data (game 2026.11.03, docs/evidence/0.1543-beast-plan.md): the
// 1st board in the sheets' order (XBMContentBattle 1: the guardian first, then fights 1 to 5), the user's 37 beasts held (BeastTeams.json of
// 17/11) with each one's automatic attack (XBMPet column 5, Action.AttackType and Action.Aspect).
internal static class BeastPlan1543Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const uint Fire = 1, Wind = 2, Earth = 3, Lightning = 4, Ice = 5, Water = 6, Blunt = 7, Piercing = 8, Slashing = 9;

    private static BoardEnemy Enemy(string name, uint weakness, string weak, int strength, int intelligence, int physical, int magical, int vitality)
        => new(name, 0, weak, [], new BeastStats(strength, intelligence, physical, magical, vitality), weakness);

    internal static Board FirstBoard(int teamSize = 10) => new(1, "1er plateau", 30, 30, teamSize, 5,
    [
        new BoardBattle(true, [Enemy("Pas de Seul", Piercing, "Perforant", 2, 5, 3, 5, 5), Enemy("Succube mage du plateau", Fire, "Feu", 1, 2, 1, 1, 2), Enemy("Succube épéiste du plateau", Fire, "Feu", 2, 1, 1, 1, 2)]),
        new BoardBattle(false, [Enemy("Chevalier du plateau", Blunt, "Contondant", 2, 1, 2, 2, 3), Enemy("Évêque du plateau", Blunt, "Contondant", 1, 2, 2, 2, 2)]),
        new BoardBattle(false, [Enemy("Archidémon du plateau", Lightning, "Foudre", 3, 1, 3, 2, 4)]),
        new BoardBattle(false, [Enemy("Piscodémon du plateau", Slashing, "Tranchant", 1, 3, 2, 3, 4)]),
        new BoardBattle(false, [Enemy("Sarcopte du plateau", Ice, "Glace", 3, 3, 3, 2, 4), Enemy("Petit acarus du plateau", Ice, "Glace", 1, 1, 2, 1, 1)]),
        new BoardBattle(false, [Enemy("Ogre du plateau", Wind, "Vent", 4, 3, 4, 2, 4), Enemy("Feu follet du plateau", Water, "Eau", 1, 1, 1, 1, 1), Enemy("Grand feu follet du plateau", Water, "Eau", 1, 3, 1, 1, 2)]),
    ], []);

    // The user's beasts held (BeastTeams.json of 17/11: 1 to 31, 34, 35, 36, 38, 40, 41), each with its automatic attack read in the sheets.
    internal static readonly (uint Id, string Name, uint Auto)[] Held =
    [
        (1, "Cu sith", Piercing), (2, "Écureuil", Fire), (3, "Mouton", Blunt), (4, "Pugil", Water), (5, "Opo-opo", Blunt), (6, "Dodo", Earth),
        (7, "Coblyn", Lightning), (8, "Acarus", Piercing), (9, "Mégalocrabe", Water), (10, "Frelon", Piercing), (11, "Vautour", Wind),
        (12, "Mandragore", Blunt), (13, "Gespenst", Lightning), (14, "Puk", Piercing), (15, "Crabe", Water), (16, "Mante", Slashing), (17, "Gluant", 0),
        (18, "Dullahan", Slashing), (19, "Chauve-souris", 0), (20, "Piège-volant", Ice), (21, "Ziz", Ice), (22, "Pampa", Piercing), (23, "Golem", Earth),
        (24, "Apkallu", Piercing), (25, "Adamankhélone", Lightning), (26, "Buffle", Blunt), (27, "Uragnite", Ice), (28, "Ver géant", Earth),
        (29, "Spriggan", Fire), (30, "Goobbue", Blunt), (31, "Crapaud géant", Blunt), (34, "Raptor", Ice), (35, "Draconide", Fire), (36, "Tréant", Earth),
        (38, "Chimère", Slashing), (40, "Fantôme", Wind), (41, "Salamandre", Water),
    ];

    internal static Beast Make((uint Id, string Name, uint Auto) row)
        => new(row.Id, row.Name, 0, "", row.Auto == 0 ? "" : "x", "", "", Enumerable.Range(1, 10).Select(_ => new BeastStats(100, 100, 100, 100, 500)).ToArray(), "", row.Auto);

    private static Beast[] Beasts() => Held.Select(Make).ToArray();
    private static HashSet<uint> HeldIds(params uint[] without) => Held.Select(row => row.Id).Where(id => !without.Contains(id)).ToHashSet();
    private static uint AutoOf(uint id) => Held.First(row => row.Id == id).Auto;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast plan 1543: empty team, the user's beasts: the 10 cover every weakness of the 1st board (Foudre, Feu and Eau included), nothing left out", () =>
        {
            var plan = BeastTeams.Plan(FirstBoard(), Beasts(), HeldIds(), null);
            Check(plan.Team.Count == 10, "ten");
            var rows = plan.Team.Select(pick => AutoOf(pick.Beast)).ToHashSet();
            foreach (var (row, name) in new[] { (Lightning, "Foudre"), (Fire, "Feu"), (Water, "Eau"), (Wind, "Vent"), (Ice, "Glace"), (Blunt, "Contondant"), (Piercing, "Perforant"), (Slashing, "Tranchant") })
                Check(rows.Contains(row), name + " covered by the ten: " + string.Join(", ", plan.Team.Select(pick => Held.First(beast => beast.Id == pick.Beast).Name)));
            Check(plan.Team.Any(pick => pick.Beast is 7 or 13 or 25), "the Archidémon's Foudre: Coblyn, Gespenst or Adamankhélone");
            var hits = plan.Battles.Select(battle => $"{battle.WeaknessesHit}/{battle.Enemies}").ToArray();
            Check(plan.Battles.All(battle => battle.WeaknessesHit == battle.Enemies), "every enemy's weakness hit in each fight: " + string.Join(" ", hits));
            Check(plan.Left is { Count: 0 }, "nothing left out");
            var guardian = plan.Battles.First(battle => battle.Battle == 0).Team.Select(pick => AutoOf(pick.Beast)).ToArray();
            Check(guardian.Contains(Fire) && guardian.Contains(Piercing), "the guardian: a fire beast and a piercing one");
        }),
        ("Beast plan 1543: a weakness is left out only when no beast held hits it, or the places are too few; the plan says which and why", () =>
        {
            var none = BeastTeams.Plan(FirstBoard(), Beasts(), HeldIds(7, 13, 25), null);
            Check(none.Left is [{ Weakness: Lightning, Name: "Foudre", Reason: LeftReason.NoneHeld }], "no lightning beast held: Foudre, none held");
            Check(none.Battles.Where(battle => battle.Battle != 2).All(battle => battle.WeaknessesHit == battle.Enemies) && none.Battles.First(battle => battle.Battle == 2).WeaknessesHit == 0,
                "every other weakness still hit");
            // Eight weaknesses on the board, seven places: one weakness of a single enemy is left out, said as « no room ».
            var seven = BeastTeams.Plan(FirstBoard(7), Beasts(), HeldIds(), null);
            Check(seven.Team.Count == 7 && seven.Left is [{ Reason: LeftReason.NoRoom }] && seven.Left[0].Weakness is Fire or Water,
                "seven places: one weakness of one enemy left out for want of room: " + string.Join(", ", (seven.Left ?? []).Select(left => left.Name)));
            // X-585 M1 (0.1558): each fight's strongest enemy first, so the weakness left is one of two weak enemies (Feu or Eau): ten of twelve hit.
            Check(seven.Battles.Sum(battle => battle.WeaknessesHit) == 10 && seven.Battles.Sum(battle => battle.Enemies) == 12, "ten enemies of twelve hit");
        }),
        ("Beast plan 1543: chosen team, the user's ten of the capture: the guardian 3 of 3 with Écureuil, fight 4 counted, fight 5 with Salamandre", () =>
        {
            var beasts = Beasts();
            var team = new uint[] { 1, 5, 22, 20, 41, 12, 27, 2, 8, 16 }.Select(id => new BeastTeams.TeamMember(id, 7, 600, 600)).ToArray();
            var fights = BeastTeams.FromTeam(FirstBoard(), null, team, beasts);
            var guardian = fights.First(fight => fight.Battle == 0);
            Check(guardian.Trio.Contains(2u) && guardian.Hit == 3, $"guardian: Écureuil called, 3 of 3 ({guardian.Hit}: {string.Join(",", guardian.Trio)})");
            var ice = fights.First(fight => fight.Battle == 4);
            Check(ice.Trio.Any(id => id is 20 or 27) && ice.Hit == 2, $"fight 4: an ice beast called and counted, 2 of 2 ({ice.Hit})");
            var water = fights.First(fight => fight.Battle == 5);
            Check(water.Trio.Contains(41u) && water.Hit == 2, $"fight 5: Salamandre called, the two will-o'-wisps hit ({water.Hit}; no wind beast in the ten)");
            // The trio of the capture (Cu sith, Pampa, Acarus) left Feu: Écureuil, in the team, hits it; that line is grey now, not orange.
            var stale = new BeastPick[] { new(1, 5, true), new(22, 5, true), new(8, 5, true) };
            var missed = BeastTeams.Missed(FirstBoard().Battles[0], stale, team.Select(member => new BeastPick(member.Beast, 5, true)).ToArray(), beasts, HeldIds());
            Check(missed is [{ Weakness: Fire, InTeam: [2] }], "the old trio misses Feu, Écureuil in the team would hit it");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("if (miss.InTeam.Count > 0) ImGui.TextDisabled(localization.Format(\"BeastUi.MissInTeam\""), "orange only when no beast brought hits the weakness");
        }),
        ("Beast plan 1543: with the plan made by the new rules, no orange line on the 1st board when every weakness is held", () =>
        {
            var beasts = Beasts();
            var held = HeldIds();
            var plan = BeastTeams.Plan(FirstBoard(), beasts, held, null);
            var board = FirstBoard();
            foreach (var battle in plan.Battles)
                Check(BeastTeams.Missed(board.Battles[battle.Battle], battle.Team, plan.Team, beasts, held).Count == 0, $"fight {battle.Battle}: no weakness missed");
        }),
        ("Beast plan 1543: a plan kept from older rules or other game data is made again (its key holds the mark of both)", () =>
        {
            var beasts = Beasts();
            var catalog = new BeastCatalog(beasts, [FirstBoard()]);
            var mark = BeastTeams.Mark(catalog);
            Check(mark.StartsWith($"v{BeastTeams.PlanVersion}:", StringComparison.Ordinal) && BeastTeams.PlanVersion >= 2, "the rules' version in the mark");
            Check(BeastTeams.Mark(new BeastCatalog(Held.Select(Make).ToArray(), [FirstBoard()])) == mark, "the same data: the same mark");
            // Before 0.1523 the magical beasts had no element: the same catalog with Écureuil at 0 is other data.
            var before = new BeastCatalog(beasts.Select(beast => beast.Id == 2 ? beast with { AutoRow = 0 } : beast).ToArray(), [FirstBoard()]);
            Check(BeastTeams.Mark(before) != mark, "a beast's attack changed: another mark");
            Check(BeastTeams.Mark(new BeastCatalog(beasts, [FirstBoard(12)])) != mark, "a board changed: another mark");
            var held = HeldIds();
            var old = BeastTeams.Key(held, null);
            Check(BeastTeams.Key(held, null, null, mark) != old && BeastTeams.Key(held, null, null, mark).StartsWith(mark + "|", StringComparison.Ordinal), "the key of 0.1542 no longer matches: the plan is made again");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("BeastTeams.Key(state.Obtained, BeastRanks, floors, state.Catalog is { } marked ? BeastMarkOf(marked) : \"\")") && page.Contains("BeastTeams.Mark(catalog)"),
                "the panel's key holds the catalog's mark, worked out once per catalog");
        }),
        ("Beast plan 1543: a plan file of 0.1542 (no « Left ») still reads; the panel says the weaknesses left out in both languages", () =>
        {
            var plan = BeastTeams.Plan(FirstBoard(), Beasts(), HeldIds(7, 13, 25), null);
            var back = BeastTeams.Read(BeastTeams.Write(new(new() { [1] = plan }, new() { [1] = "k" })));
            Check(back?.Plans[1].Left is [{ Weakness: Lightning, Reason: LeftReason.NoneHeld }], "Left kept on disk");
            var old = BeastTeams.Read("{\"Plans\":{\"1\":{\"Board\":1,\"Team\":[],\"Battles\":[],\"RanksAssumed\":false}},\"Keys\":{\"1\":\"k\"}}");
            Check(old is not null && old.Plans[1].Left is null, "an older file: no Left, read");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("foreach (var left in plan.Left ?? [])") && page.Contains("\"BeastUi.LeftNoneHeld\" : \"BeastUi.LeftNoRoom\""), "drawn under the ten");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"LeftNoneHeld\": \"{0}") && text.Contains("\"LeftNoRoom\": \"{0}") && text.Contains("{1}"), language + ": both texts");
            }
        }),
    ];
}
