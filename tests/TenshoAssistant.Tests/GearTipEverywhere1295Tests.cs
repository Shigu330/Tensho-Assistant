using System.Text.RegularExpressions;
using TenshoAssistant.Gear;

// X-294 (0.1295): user request of 14/11 about 0:20 (the worn piece's tooltip had no stats, the Gear list's had them): « the tooltips are not
// harmonised for gear, stats must show everywhere ». The gear part (base stats, comparison with the worn piece) is drawn by the common item
// tooltip, the one way every page draws an item tooltip (R10).
internal static class GearTipEverywhere1295Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Root => Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Root, file)).Replace("\r\n", "\n");
    private static IEnumerable<(string File, string Text)> Sources() =>
        Directory.EnumerateFiles(Root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Select(path => (Path.GetRelativePath(Root, path).Replace('\\', '/'), File.ReadAllText(path)));

    private static GearItem Piece(uint id, Dictionary<int, int> stats, Dictionary<int, int> hqStats, bool canBeHq) =>
        new(id, $"I{id}", 0, 90, 600, GearSlot.Ears, false, 1UL << 43, stats, hqStats, 0, canBeHq, false);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-294: the common item tooltip draws the gear part for every item, between its header and its body; no page adds its own", () =>
        {
            var hover = Read("MainWindow.MarketHover.cs");
            var tooltip = hover[hover.IndexOf("private void DrawItemInfoTooltip(", StringComparison.Ordinal)..hover.IndexOf("private void DrawItemInfoBody(", StringComparison.Ordinal)];
            Check(tooltip.Contains("private void DrawItemInfoTooltip(uint id,bool glamourOnly=false,bool? hq=null)"), "no page hook (Action) in the item tooltip: one way");
            Check(tooltip.Contains("DrawItemGearStats(id,glamourOnly,hq);\n            DrawItemInfoBody(item,id);"), "stats and comparison, then the common body");
        }),
        ("X-294 ratchet: one path for the item tooltip: the gear part, its stats table and the comparison are drawn only by the common parts", () =>
        {
            var gearPart = Sources().Where(source => source.Text.Contains("DrawItemGearStats(")).Select(source => source.File).OrderBy(file => file).ToArray();
            Check(gearPart.SequenceEqual(new[] { "MainWindow.GearCompare.cs", "MainWindow.MarketHover.cs" }), "the gear part lives in the comparator and is called by the item tooltip only: " + string.Join(",", gearPart));
            var compare = Sources().Where(source => source.Text.Contains("GearCompare.Compare(")).Select(source => source.File).ToArray();
            Check(compare.SequenceEqual(new[] { "MainWindow.GearCompare.cs" }), "the comparison is worked out in one file: " + string.Join(",", compare));
            var stats = Sources().Sum(source => Regex.Matches(source.Text, @"GearCompare\.BaseStats\(").Count);
            Check(stats == 1, "a piece's stats for a tooltip are listed in one place: " + stats);
            // Every item tooltip goes through DrawItemInfoTooltip: the common body is drawn by it, the panel under the game's tooltip and the item sheet only.
            var bodies = Sources().Sum(source => Regex.Matches(source.Text, @"\bDrawItemInfoBody\(item,").Count);
            Check(bodies == 3, "DrawItemInfoBody called by the item tooltip, the hover panel and the item sheet only: " + bodies);
            Check(!Read("MainWindow.Gear.cs").Contains("DrawGearPickTooltip"), "the Gear list no longer has its own tooltip");
        }),
        ("X-294: the worn piece of a Gear slot shows its stats with its own HQ state", () =>
        {
            var gear = Read("MainWindow.Gear.cs");
            Check(gear.Contains("DrawItemInfoTooltip(shown.Id,glamourOnly:look is not null,hq:hq)"), "slot piece: known HQ, glamour mark");
        }),
        ("X-294: base stats: NQ or HQ values (base plus the HQ extras), in the stats' order, zeros left out; HQ only when the piece can be HQ", () =>
        {
            var order = new[] { 12, 13, 1, 2, 3, 4, 5, 27, 22, 44, 45, 46, 19, 6, 21, 24 };
            var piece = Piece(1, new() { [6] = 10, [1] = 20, [44] = 0, [27] = 7 }, new() { [6] = 2, [1] = 2, [27] = 1 }, true);
            Check(GearCompare.BaseStats(piece, false, order).SequenceEqual(new[] { (1, 20), (27, 7), (6, 10) }), "NQ: " + string.Join(",", GearCompare.BaseStats(piece, false, order)));
            Check(GearCompare.BaseStats(piece, true, order).SequenceEqual(new[] { (1, 22), (27, 8), (6, 12) }), "HQ: " + string.Join(",", GearCompare.BaseStats(piece, true, order)));
            var noHq = Piece(2, new() { [6] = 10 }, new() { [6] = 89 }, false);
            Check(GearCompare.BaseStats(noHq, true, order).SequenceEqual(new[] { (6, 10) }), "a piece that cannot be HQ keeps its NQ values");
            Check(GearCompare.BaseStats(Piece(3, new(), new(), false), false, order).Count == 0, "no stat, no line");
        }),
    ];
}
