using TenshoAssistant.Gear;

// X-290 (0.1294): user request of 13/11 about 23:30 (Gear page, search of an off-hand piece, levels 40 to 45): the stat differences written at
// the end of each line went past the list and were cut (« +4 I », « -5 Détermination »). They leave the list for the piece's tooltip, with its
// stats, drawn by the hover comparator's own parts (one way, R10).
internal static class GearPickTip1294Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    private static GearItem Piece(uint id, int itemLevel, Dictionary<int, int> stats) =>
        new(id, $"I{id}", 0, 45, itemLevel, GearSlot.OffHand, false, 1UL << 43, stats, new Dictionary<int, int>(), 0, false, false);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-290: the list writes no stat difference on its lines; only the level line, which goes to the next line when it does not fit", () =>
        {
            var gear = Read("MainWindow.Gear.cs");
            var pick = gear[gear.IndexOf("private void DrawGearPick(", StringComparison.Ordinal)..gear.IndexOf("private readonly record struct GearPickKey", StringComparison.Ordinal)];
            Check(!pick.Contains("Gear.Param") && !pick.Contains("in differences"), "no difference text in the list");
            Check(pick.Contains("SameLineOrNext(localization.Format(\"Gear.LevelLine\"") && pick.Contains("DrawItemInfoTooltip(candidate.Id,glamourOnly:glamourOnly)"), "level line wraps; tooltip on hover");
            Check(!gear.Contains("PickDifferences"), "the old three differences are gone");
        }),
        ("X-290: one way to draw stats: the comparator and the list's tooltip share DrawStatPairs and the comparison", () =>
        {
            var compare = Read("MainWindow.GearCompare.cs");
            Check(compare.Split("ImGui.BeginTable(").Length - 1 == 1 && compare.Contains("DrawStatPairs(\"HoverCompare\""), "one stats table, used by the comparator");
            Check(compare.Contains("DrawStatPairs(\"GearTipStats\"") && compare.Contains("DrawHoverComparisons(comparisons)") && compare.Contains("GearCompare.Compare(piece,hq,"), "the item tooltip's gear part uses the same parts (X-294)");
            var hover = Read("MainWindow.MarketHover.cs").Replace("\r\n", "\n");
            Check(hover.Contains("DrawItemGearStats(id,glamourOnly,hq);\n            DrawItemInfoBody(item,id);"), "the common item tooltip keeps its body (levels, stock, how to get it)");
        }),
        ("X-290: the tooltip's comparison: a level 45 off-hand against the worn one, gains and losses per stat, base values", () =>
        {
            var worn = Piece(1, 40, new() { [6] = 10, [44] = 5 });
            var candidate = Piece(2, 45, new() { [6] = 14, [27] = 3 });
            var found = new Dictionary<uint, GearItem> { [1] = worn, [2] = candidate };
            var result = GearCompare.Compare(candidate, false, [new EquippedPiece(GearSlot.OffHand, 1, false, [])], id => found.GetValueOrDefault(id), [12, 13, 1, 2, 3, 4, 5, 27, 22, 44, 45, 46, 19, 6, 21, 24]);
            Check(result.Count == 1 && result[0].ItemLevelDelta == 5, "one worn piece, +5 item levels");
            Check(result[0].Deltas.SequenceEqual(new[] { (27, 3), (44, -5), (6, 4) }), "every changed stat, in the stats' order: " + string.Join(",", result[0].Deltas));
        }),
    ];
}
