using TenshoAssistant.Beastmaster;

// 0.1529 (X-486, the user's request of 16/11 at the Beast Labyrinth): the fights the game's board window shows come first, one block each, with
// the three beasts for it and why; the whole board is folded. Real data: the 1st board (XBMContentBattle 1, XBMBattleDetail rows 1 to 6, game
// 2026.11.03; the guardian first in the sheet) and the user's beasts of 17/11.
internal static class BeastFightNow1529Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const uint Fire = 1, Wind = 2, Lightning = 4, Ice = 5, Water = 6, Blunt = 7, Piercing = 8, Slashing = 9;

    private static BoardEnemy Enemy(string name, uint weakness, string weak, params BoardAction[] actions)
        => new(name, 0, weak, actions, new BeastStats(2, 1, 2, 2, 3), weakness);

    private static Board FirstBoard() => new(1, "1er plateau", 30, 30, 10, 5,
    [
        new BoardBattle(true, [Enemy("Pas de Seul", Piercing, "Perforant"), Enemy("Succube mage du plateau", Fire, "Feu"), Enemy("Succube épéiste du plateau", Fire, "Feu")], 6),
        new BoardBattle(false, [Enemy("Chevalier du plateau", Blunt, "Contondant", new BoardAction("Taillade", "Simple", "Ennemi", "Tranchant")), Enemy("Évêque du plateau", Blunt, "Contondant")], 1),
        new BoardBattle(false, [Enemy("Archidémon du plateau", Lightning, "Foudre")], 2),
        new BoardBattle(false, [Enemy("Piscodémon du plateau", Slashing, "Tranchant")], 3),
        new BoardBattle(false, [Enemy("Sarcopte du plateau", Ice, "Glace"), Enemy("Petit acarus du plateau", Ice, "Glace")], 4),
        new BoardBattle(false, [Enemy("Ogre du plateau", Wind, "Vent"), Enemy("Feu follet du plateau", Water, "Eau")], 5),
    ], []);

    private static Beast Make(uint id, string name, uint auto, string attack)
        => new(id, name, 0, "", attack, "", "", Enumerable.Range(1, 10).Select(_ => new BeastStats(100, 100, 100, 100, 500)).ToArray(), "", auto);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast fight now 1529: the fights the board window lists are the board's own; all of them or none: no fight of its own", () =>
        {
            var board = FirstBoard();
            Check(BeastTeams.FightsShown(board, new HashSet<uint> { 1 }).SequenceEqual([1]), "stage 1 lists the 1st fight (XBMBattleDetail 1): the board's 2nd row (the guardian comes first)");
            Check(BeastTeams.FightsShown(board, new HashSet<uint> { 3, 2 }).SequenceEqual([2, 3]), "two fights, in the board's order");
            Check(BeastTeams.FightsShown(board, new HashSet<uint> { 1, 2, 3, 4, 5, 6 }).Count == 0, "the whole board listed: no fight of its own");
            Check(BeastTeams.FightsShown(board, new HashSet<uint> { 1, 2, 3, 4, 5, 6, 77 }).Count == 0, "the whole board and more");
            Check(BeastTeams.FightsShown(board, new HashSet<uint>()).Count == 0 && BeastTeams.FightsShown(board, null).Count == 0, "nothing read");
            Check(BeastTeams.FightsShown(board, new HashSet<uint> { 42 }).Count == 0, "another board's fights only: none");
            var old = new Board(9, "x", 1, 1, 10, 5, [new BoardBattle(false, [])], []);
            Check(BeastTeams.FightsShown(old, new HashSet<uint> { 0, 1 }).Count == 0, "a fight whose row is not known is never matched");
        }),
        ("Beast fight now 1529: why a beast is called: the enemies whose weakness its automatic attack hits", () =>
        {
            var board = FirstBoard();
            var opo = Make(5, "Opo-opo", Blunt, "Contondant");
            var cu = Make(1, "Cu sith", Piercing, "Perforant");
            var flytrap = Make(20, "Piège-volant", Ice, "Glace");
            Check(BeastTeams.Hits(board.Battles[1], opo).SequenceEqual(["Chevalier du plateau", "Évêque du plateau"]), "blunt hits both");
            Check(BeastTeams.Hits(board.Battles[1], cu).Count == 0, "piercing hits neither");
            Check(BeastTeams.Hits(board.Battles[4], flytrap).SequenceEqual(["Sarcopte du plateau", "Petit acarus du plateau"]), "ice in the ice fight");
            Check(BeastTeams.Hits(board.Battles[0], cu).SequenceEqual(["Pas de Seul"]), "the guardian's piercing weakness");
            Check(BeastTeams.Hits(board.Battles[1], null).Count == 0 && BeastTeams.Hits(board.Battles[1], Make(19, "Chauve-souris", 0, "")).Count == 0, "unknown or unaspected: none");
        }),
        ("Beast fight now 1529: the catalog keeps each fight's row and each action's damage; the panel shows the fight first, the board folded", () =>
        {
            var catalog = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastCatalog.cs"));
            Check(catalog.Contains("Array.AsReadOnly(enemies.GetValueOrDefault(battle.UInt(0)) ?? []), battle.UInt(0))"), "a fight keeps its XBMBattleDetail row");
            Check(catalog.Contains("targets.GetValueOrDefault(row.UInt(1), \"\"), Auto(row.UInt(0)), Effect(row.UInt(3))))") /* 0.1559: the effect after the damage */, "an action's damage from its Action row");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs"));
            Check(reader.Contains("var entries = stage->Entries;") && reader.Contains("entries[index].BattleDetailId") && reader.Contains("Beastmaster board window entries:"), "the reader reads the window's fights, said in the log");
            Check(!reader.Contains("GetSelectedEntryIndex"), "no function of the game called");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("var now = BeastTeams.FightsShown(board, BeastFightsShown?.Invoke());") && page.Contains("foreach (var index in now) DrawBeastFightNow(board, index, plan, taken, catalog);"), "the fights shown first");
            Check(page.Contains("UiComponents.TreeNode(localization.Get(\"BeastUi.WholeBoard\") + \"###BeastWholeBoard\")"), "the whole board folded under its own node");
            var plugin = (File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"))+File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Beastmaster.cs")));
            Check(plugin.Contains("mainWindow.BeastFightsShown=()=>boardReader?.State.Shown;"), "wired");
        }),
    ];
}
