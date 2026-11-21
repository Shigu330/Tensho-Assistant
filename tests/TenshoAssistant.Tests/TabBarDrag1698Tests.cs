using TenshoAssistant.UI;

// 0.1698 (X-605, second part, the user's request of 18/11): the common detail tabs (Alarms, Reminders, garden, Grand Companies…) dragged with
// the mouse, the order kept by bar; what a reorderable bar remembers between frames in one pure class (TabBarDrag).
internal static class TabBarDrag1698Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static void Frame(TabBarDrag drag, params (string Id, float X)[] places) { drag.Places.Clear(); drag.Places.AddRange(places); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Tab bar drag 1698: a drag kept when it ends, not before; nothing kept when the order did not change", () =>
        {
            var drag = new TabBarDrag();
            Frame(drag, ("A", 0), ("B", 100), ("C", 200));
            Check(drag.After(false, null) is null && drag.Generation == 0, "a still bar: nothing");
            Frame(drag, ("A", 120), ("B", 100), ("C", 200));
            Check(drag.After(true, null) is null, "during the drag: nothing kept yet");
            Frame(drag, ("A", 100), ("B", 0), ("C", 200));
            Check(drag.After(false, ["C", "Dev"]) is { } kept && kept.SequenceEqual(["B", "A", "C", "Dev"]) && drag.Generation == 0, "kept when it ends, a tab out of reach at the end, the bar not made again");
            Frame(drag, ("B", 0), ("A", 100), ("C", 200));
            Check(drag.After(false, ["B", "A", "C"]) is null && drag.Generation == 0, "then the bar drawn in that order: nothing more");
            Frame(drag, ("A", 0), ("B", 100));
            drag.After(true, null);
            Frame(drag, ("A", 0), ("B", 100));
            Check(drag.After(false, null) is null, "a drag that ends where it began: nothing kept");
        }),
        // X-709 (0.1700): another order shown settles the same bar in the next frame (TabBarSettle1700Tests); a new bar only to put back in order.
        ("Tab bar drag 1698: another order than the one asked, outside a drag: the bar settles (no new bar); put back in order: a new bar, the tab shown selected once", () =>
        {
            var drag = new TabBarDrag { Shown = "B" };
            Frame(drag, ("A", 0), ("New", 300), ("B", 100));
            Check(drag.After(false, null) is null && drag.Generation == 0, "not made again");
            Check(drag.Settle<string>(["A", "New", "B"], id => id), "settled in the next frame");
            drag.Renew();
            Check(drag.Generation == 1 && drag.TakeFresh() && !drag.TakeFresh(), "put back in order: a new bar, the tab shown selected once");
        }),
        ("Tab bar drag 1698: wiring: the detail tabs reorderable, drawn in the order kept, kept when a drag ends; reset with the families'", () =>
        {
            var detail = Read("UI/Components/DetailPage.cs");
            Check(detail.Contains("ImGui.BeginTabBar(id+\"/\"+drag.Generation,drag.Settle(arranged,tab=>tab.Key) ? ImGuiTabBarFlags.None:ImGuiTabBarFlags.Reorderable)", StringComparison.Ordinal), "reorderable (settling one frame without), its number in its id");
            Check(detail.Contains("TabOrder.Arrange(tabs,configuration.TabOrders.GetValueOrDefault(id),tab=>tab.Key)", StringComparison.Ordinal), "drawn in the order kept");
            Check(detail.Contains("drag.Places.Add((key,ImGui.GetItemRectMin().X));", StringComparison.Ordinal) && detail.Contains("TabBarAfter(id,drag,dragged);", StringComparison.Ordinal)
                && detail.Contains("if(drag.After(dragged,configuration.TabOrders.GetValueOrDefault(id)) is not { } keep)return;", StringComparison.Ordinal), "places read, kept when a drag ends");
            Check(detail.Contains("ImGui.BeginTabItem(label+\"###\"+id+key,", StringComparison.Ordinal), "each tab's id stays its key (the tab open is kept across the update)");
            Check(Read("UI/Navigation/MainWindow.PageHeader.cs").Contains("foreach(var drag in tabBarDrags.Values)drag.Renew();", StringComparison.Ordinal), "put back in order with the families' tabs");
        }),
    ];
}
