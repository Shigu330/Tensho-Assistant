using TenshoAssistant.Maps;

// 0.1694 (X-684, twelfth part, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one gathering point tooltip
// (DrawGatheringTooltip, body DrawGatheringBody, facts in Maps/GatheringTips): title, job and kind of point, place, opening read live, items
// with « and n more »; the Tensho map's icons under the cursor (a copy by hand before), the list's points without a position (names only
// before) go through it; the spearfishing helper's fish names carry the common item tooltip.
internal static class GatheringTips1694Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gathering tips 1694: the facts: title trimmed, detail parts joined once, blank place out, each item once", () =>
        {
            var facts = GatheringTips.Build(" Mining · Lv. 50 ", ["Miner", null, " ", "Legendary point", "Miner"], " ", [(5u, "Ore"), (6u, "Shard"), (5u, "Ore")]);
            Check(facts.Title == "Mining · Lv. 50" && facts.Detail == "Miner · Legendary point" && facts.Place is null, facts.Title + " | " + facts.Detail);
            Check(facts.Items.Select(item => item.Id).SequenceEqual([5u, 6u]) && facts.Items[1].Name == "Shard", "each item once, in the game's order");
            Check(GatheringTips.Build("Logging", [null, ""], "Central Shroud", []).Detail is null, "no detail: none said");
        }),
        ("Gathering tips 1694: how many items are left out", () =>
        {
            var items = Enumerable.Range(1, 20).Select(i => ((uint)i, "Item " + i));
            var facts = GatheringTips.Build("Fishing", [], null, items);
            Check(GatheringTips.More(facts, GatheringTips.MaxItems) == 4 && GatheringTips.More(facts, GatheringTips.MaxListItems) == 12, "16 then 8 shown");
            Check(GatheringTips.More(GatheringTips.Build("Fishing", [], null, items.Take(3)), GatheringTips.MaxItems) == 0, "none left out");
        }),
        ("Gathering tips 1694: wiring: one body; the map's icons under the cursor, the list with and without a position; no copy by hand; the fish names", () =>
        {
            var body = Read("src/TenshoAssistant/MainWindow.GatheringTooltip.cs");
            Check(body.Contains("private void DrawGatheringTooltip(MapLocalGatheringPoint point,string? after=null)", StringComparison.Ordinal) && body.Contains("using(TooltipScope.Begin())", StringComparison.Ordinal), "the tooltip");
            Check(body.Contains("private void DrawGatheringBody(MapLocalGatheringPoint point,int max)", StringComparison.Ordinal) && body.Contains("DrawPointAvailability(point);", StringComparison.Ordinal), "the body, opening read live");
            Check(body.Contains("if(gatheringTips.TryGetValue(point,out var known))return known;", StringComparison.Ordinal) && body.Contains("gatheringTips.Count>=64", StringComparison.Ordinal), "built once per point, bounded");
            Check(body.Contains("\"MapLocal.NoPosition\":\"MapLocal.ClickFlag\"", StringComparison.Ordinal), "a point without a position says so");
            var map = Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(map.Contains("if(hit.Point is { } point)DrawGatheringBody(point,GatheringTips.MaxListItems);", StringComparison.Ordinal), "the icons under the cursor");
            Check(!map.Contains("foreach(var id in point.Items.Take(", StringComparison.Ordinal) && !map.Contains("private void DrawGatheringTooltip(", StringComparison.Ordinal), "no copy by hand left in the map");
            var lists = Read("src/TenshoAssistant/MainWindow.MapLocalLists.cs");
            Check(lists.Split("if(RichTipHovered())DrawGatheringTooltip(point);").Length == 3 && !lists.Contains("localization.Get(\"MapLocal.NoPosition\")", StringComparison.Ordinal), "the list, with and without a position");
            var spear = Read("src/TenshoAssistant/MainWindow.SpearfishingPanel.cs");
            Check(spear.Contains("DrawItemInfoTooltip(row.Fish);", StringComparison.Ordinal) && !spear.Contains("WrappedTooltip(name+\" — \"", StringComparison.Ordinal), "the fish's name: the common item tooltip, the name not written again");
        }),
    ];
}
