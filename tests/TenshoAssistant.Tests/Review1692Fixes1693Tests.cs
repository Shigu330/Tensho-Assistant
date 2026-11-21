using TenshoAssistant.Npcs;

// 0.1693 (X-707, review of 0.1687 to 0.1692), M1: the Tensho map's vendor labels (built once per map, for every vendor) take only the name and
// the shop lines (VendorShopLines), no longer the whole NPC tooltip whose places and icon were formatted and thrown away; NpcTips.Lines says
// each line once through a set instead of List.Contains.
internal static class Review1692Fixes1693Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review fixes 1693: M1, the lines of a list: each once, in the order given, blanks out, on a long list too", () =>
        {
            var many = Enumerable.Range(0, 5000).Select(i => "Place " + (i % 250)).Concat(new string?[] { " ", null, " Place 3 " });
            var kept = NpcTips.Lines(many, int.MaxValue, more => "+" + more);
            Check(kept.Count == 250 && kept[0] == "Place 0" && kept[249] == "Place 249" && kept.Distinct().Count() == 250, kept.Count + " lines");
            Check(NpcTips.Lines(["b", " a ", "b", "a", "c", "d"], 2, more => "and " + more + " more").SequenceEqual(["b", "a", "and 2 more"]), "order, trimmed, then how many more");
            Check(NpcTips.Lines(["Shop", "shop"], 9, _ => "").Count == 2, "case kept apart, as before (ordinal)");
        }),
        ("Review fixes 1693: M1, wiring: the map's label builds the shop lines only; the tooltip keeps its places and icon", () =>
        {
            var map = Read("src/TenshoAssistant/MainWindow.MapView.cs");
            var label = map.Split('\n').Single(line => line.Contains("private string VendorLabel(", StringComparison.Ordinal));
            Check(label.Contains("VendorShopLines(vendor,", StringComparison.Ordinal) && !label.Contains("BuildVendorTip", StringComparison.Ordinal) && !label.Contains("VendorLocations", StringComparison.Ordinal), label);
            var tooltip = Read("src/TenshoAssistant/MainWindow.NpcTooltip.cs");
            Check(tooltip.Contains("return NpcTips.Build(NpcName(vendor),VendorIconOf(vendor,shops),places,VendorShopLines(vendor,shops),[]);", StringComparison.Ordinal), "the tooltip: places, icon and the same shop lines");
            Check(tooltip.Contains("private IReadOnlyList<string> VendorShopLines(NpcVendor vendor,", StringComparison.Ordinal), "one place for the shop lines");
            Check(!Read("src/TenshoAssistant/Npcs/NpcTips.cs").Contains("kept.Contains(", StringComparison.Ordinal), "no List.Contains left in the lines");
            Check(TenshoAssistant.Diagnostics.JitWarmup.MapPathMethods.Contains("VendorShopLines"), "warmed with the map's path");
        }),
    ];
}
