using TenshoAssistant.UI;

// 0.1700 (X-709, review of 0.1693 to 0.1698, M1): a reorderable bar whose tabs change (the item sheet's Craft, Uses, Gathering… depend on
// the item) or which shows another order than the one drawn is settled in the same bar (one frame without Reorderable: ImGui then sorts its
// tabs in the order drawn, docs/evidence/0.1700-tab-bar-settle.md), no longer made again with a new id (new child windows each time).
internal static class TabBarSettle1700Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static void Frame(TabBarDrag drag, params (string Id, float X)[] places) { drag.Places.Clear(); drag.Places.AddRange(places); }
    private static bool Settle(TabBarDrag drag, params string[] ids) => drag.Settle(ids, id => id);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Tab bar settle 1700: the item sheet's tabs change with the item: the bar settles once, never a new bar; a tab gone or the same tabs: nothing", () =>
        {
            var drag = new TabBarDrag();
            Check(!Settle(drag, "Overview", "Stocks", "Note", "Market"), "first frame of the bar: nothing to settle");
            Frame(drag, ("Overview", 0), ("Stocks", 80), ("Note", 150), ("Market", 210));
            Check(drag.After(false, null) is null, "drawn in order");
            Check(Settle(drag, "Overview", "Stocks", "Note", "Craft", "Market"), "an item with a recipe: Craft comes, the bar settles");
            Frame(drag, ("Overview", 0), ("Stocks", 80), ("Note", 150), ("Craft", 0), ("Market", 210));
            Check(drag.After(false, null) is null && drag.Generation == 0, "the settling frame (Craft not drawn yet, an empty place): no new bar");
            Check(!Settle(drag, "Overview", "Stocks", "Note", "Craft", "Market"), "next frame: reorderable again");
            Frame(drag, ("Overview", 0), ("Stocks", 80), ("Note", 150), ("Craft", 210), ("Market", 270));
            Check(drag.After(false, null) is null && drag.Generation == 0 && !Settle(drag, "Overview", "Stocks", "Note", "Craft", "Market"), "the order drawn: nothing more");
            Frame(drag, ("Overview", 0), ("Stocks", 80), ("Note", 150), ("Craft", 210), ("Market", 270));
            Check(!Settle(drag, "Overview", "Stocks", "Note", "Market"), "a tab gone: nothing to settle");
            Check(!drag.TakeFresh(), "no new bar, so nothing selected by force");
        }),
        ("Tab bar settle 1700: another order shown than the one drawn: settled next frame; settling twice in vain: a new bar, the tab shown selected once", () =>
        {
            var drag = new TabBarDrag { Shown = "B" };
            Frame(drag, ("A", 0), ("C", 200), ("B", 100));
            Check(drag.After(false, null) is null && drag.Generation == 0, "first miss: no new bar");
            Check(Settle(drag, "A", "C", "B"), "settled in the next frame");
            Frame(drag, ("A", 0), ("C", 200), ("B", 100));
            Check(drag.After(false, null) is null && drag.Generation == 0 && !Settle(drag, "A", "C", "B"), "the settling frame itself is not judged");
            Frame(drag, ("A", 0), ("C", 200), ("B", 100));
            Check(drag.After(false, null) is null && drag.Generation == 0 && Settle(drag, "A", "C", "B"), "second miss: settled again");
            Frame(drag, ("A", 0), ("C", 200), ("B", 100));
            drag.After(false, null);
            Check(!Settle(drag, "A", "C", "B"), "after the settling frame");
            Frame(drag, ("A", 0), ("C", 200), ("B", 100));
            Check(drag.After(false, null) is null && drag.Generation == 1 && drag.TakeFresh(), "third miss: a new bar, the tab shown selected once");
            Frame(drag, ("A", 0), ("C", 100), ("B", 200));
            Check(drag.After(false, null) is null && !Settle(drag, "A", "C", "B") && drag.Generation == 1, "back in order: nothing more");
        }),
        ("Tab bar settle 1700: never settled during a drag (the order shown then is the player's); kept when it ends", () =>
        {
            var drag = new TabBarDrag();
            Frame(drag, ("A", 0), ("B", 100));
            drag.After(false, null);
            Frame(drag, ("A", 120), ("B", 100));
            Check(drag.After(true, null) is null, "a drag under way");
            Check(!Settle(drag, "A", "B", "New"), "a tab coming during a drag: not settled");
            Frame(drag, ("A", 100), ("B", 0));
            Check(drag.After(false, null) is { } kept && kept.SequenceEqual(["B", "A"]) && drag.Generation == 0, "the drag ends: the order shown kept, the same bar");
        }),
        ("Tab bar settle 1700: wiring: the detail tabs and the families' header settle the same bar; one memory (TabBarDrag) and one way to keep a drag", () =>
        {
            var detail = Read("UI/Components/DetailPage.cs");
            var header = Read("UI/Navigation/MainWindow.PageHeader.cs");
            Check(detail.Contains("drag.Settle(arranged,tab=>tab.Key) ? ImGuiTabBarFlags.None:ImGuiTabBarFlags.Reorderable", StringComparison.Ordinal), "detail tabs settle");
            Check(header.Contains("drag.Settle(arranged,entry=>entry.Page.ToString()) ? ImGuiTabBarFlags.None:ImGuiTabBarFlags.Reorderable", StringComparison.Ordinal), "families' header settles");
            Check(header.Contains("if(!tabBarDrags.TryGetValue(key,out var drag))tabBarDrags[key]=drag=new();", StringComparison.Ordinal) && !header.Contains("pageTabGeneration", StringComparison.Ordinal)
                && !header.Contains("pageTabPlaces", StringComparison.Ordinal) && !header.Contains("pageTabDragging", StringComparison.Ordinal), "the header's own copy of the memory gone");
            Check(header.Contains("TabBarAfter(key,drag,dragged);", StringComparison.Ordinal) && detail.Contains("TabBarAfter(id,drag,dragged);", StringComparison.Ordinal)
                && System.Text.RegularExpressions.Regex.Matches(detail + header, "TrySave\\(\"TabOrders\"\\)").Count == 2, "one way to keep a drag (and the reset)");
            Check(Read("UI/TabBarDrag.cs").Contains("docs/evidence/0.1700-tab-bar-settle.md", StringComparison.Ordinal), "the evidence cited");
        }),
    ];
}
