using TenshoAssistant.UI;

// 0.1702 (X-605, last part, the user's request of 18/11: « and if possible the side bar's entries »): the side bar's entries reordered with
// the mouse inside their group (ListDrag, pure), kept by group, put back with the pages' tabs.
internal static class SideBarDrag1702Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    // Three rows of 26 px: A 0-26, B 26-52, C 52-78; the row B pressed and dragged when `drag` names it.
    private static void Frame(ListDrag list, string? drag = null)
    {
        list.Clear();
        string[] ids = ["A", "B", "C"];
        for (var index = 0; index < ids.Length; index++) list.Row(ids[index], 10, index * 26, 200, index * 26 + 26, ids[index] == drag);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Side bar drag 1702: an entry dragged below another moves there; to the top; released where it was: nothing", () =>
        {
            var list = new ListDrag();
            Frame(list);
            Check(list.Dragged is null && list.Drop(40) is null, "a click without a drag: nothing");
            Frame(list, "B");
            Check(list.Dragged == "B", "pressed and dragged: B is moved");
            Frame(list);
            Check(list.Line(60) is { } line && line.Y == 52 && line.Left == 10 && line.Right == 200, "held above C's middle: the line at C's top");
            Check(list.Line(75)?.Y == 78, "below C's middle: the line after C");
            Check(list.Drop(75) is { } below && below.SequenceEqual(["A", "C", "B"]) && list.Dragged is null, "released below C: A, C, B; the drag ended");
            Frame(list, "B");
            Check(list.Drop(5) is { } top && top.SequenceEqual(["B", "A", "C"]), "released above A's middle: B first");
            Frame(list, "B");
            Check(list.Drop(30) is null && list.Drop(50) is null, "released on itself: nothing moves");
        }),
        ("Side bar drag 1702: the place from the mouse's height; no row, or the row gone: nothing kept, the drag ends", () =>
        {
            var list = new ListDrag();
            Frame(list);
            Check(list.Target(-5) == 0 && list.Target(12) == 0 && list.Target(14) == 1 && list.Target(100) == 3, "before A, before B, at the end");
            Check(list.Line(10) is null, "no drag: no line");
            Frame(list, "C");
            list.Clear();
            Check(list.Line(10) is null && list.Drop(10) is null && list.Dragged is null, "the group folded during the drag: nothing");
            Frame(list, "A");
            list.Clear();
            list.Row("B", 10, 0, 200, 26, false);
            Check(list.Drop(40) is null, "the row dragged no longer drawn: nothing");
        }),
        ("Side bar drag 1702: wiring: each group in the order kept, no page opened at the end of a drag, the line and the order kept, the reset says it", () =>
        {
            var window = Read("MainWindow.cs");
            Check(window.Contains("var key=\"SideBar.\"+(group.Category ?? \"Group\"+index);", StringComparison.Ordinal)
                && window.Contains("TabOrder.Arrange(group.Entries,configuration.TabOrders.GetValueOrDefault(key),entry=>entry.Page.ToString())", StringComparison.Ordinal), "each group in the order kept");
            Check(window.Contains("if(FamilyTarget(entry.Page) is { } target)Navigation(entry.Label!(),entry.Page,target);", StringComparison.Ordinal) && window.Contains("sideBarRows=drag;", StringComparison.Ordinal)
                && window.Contains("if (!rows.KeepsClick(root.ToString())) clicked = false;", StringComparison.Ordinal), "rows told by Navigation; no page opened by a drag of another row (X-711)");
            Check(window.Contains("ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left)", StringComparison.Ordinal) && window.Contains("SideBarAfter(key,drag);", StringComparison.Ordinal), "rows told, after each group");
            Check(window.Contains("if(drag.Drop(mouse) is { } order)KeepOrder(key,TenshoAssistant.UI.TabOrder.Keep(order,configuration.TabOrders.GetValueOrDefault(key)));", StringComparison.Ordinal), "kept by the one way (KeepOrder)");
            Check(Read("UI/Components/DetailPage.cs").Contains("private void KeepOrder(string id,List<string> keep)", StringComparison.Ordinal), "one way to keep an order");
            Check(Read("Localization/Languages/en.json").Contains("and the entries of the side bar (inside their group)", StringComparison.Ordinal)
                && Read("Localization/Languages/fr.json").Contains("et les entrées de la barre de côté (dans leur groupe)", StringComparison.Ordinal), "the reset's help says it");
            Check(Read("UI/Navigation/MainWindow.PageHeader.cs").Contains("configuration.TabOrders=new Dictionary<string,List<string>>();", StringComparison.Ordinal), "the reset clears the side bar's orders too (one dictionary)");
        }),
    ];
}
