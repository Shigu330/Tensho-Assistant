using TenshoAssistant.UI;

// 0.1697 (X-605, first part, the user's request of 18/11): the tabs of a family of pages dragged with the mouse, the order kept by tab id and
// given back at load; « put back in order » in Settings › Appearance.
internal static class TabOrder1697Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Tab order 1697: the order kept first, the others in the list's order; nothing kept: the list's order", () =>
        {
            string[] natural = ["Craft", "Presets", "Live", "Macros"];
            Check(TabOrder.Arrange(natural, null, id => id).SequenceEqual(natural) && TabOrder.Arrange(natural, [], id => id).SequenceEqual(natural), "nothing kept");
            Check(TabOrder.Arrange(natural, ["Macros", "Craft"], id => id).SequenceEqual(["Macros", "Craft", "Presets", "Live"]), "kept first, then the list's order");
            Check(TabOrder.Arrange(natural, ["Gone", "Live", "Live"], id => id).SequenceEqual(["Live", "Craft", "Presets", "Macros"]), "a tab no longer drawn and a name twice change nothing else");
        }),
        ("Tab order 1697: the order shown, left to right; kept after a drag with the tabs out of reach at the end", () =>
        {
            Check(TabOrder.Shown([("B", 120f), ("A", 10f), ("C", 300f)]).SequenceEqual(["A", "B", "C"]), "by left edge");
            Check(TabOrder.Keep(["B", "A"], ["A", "Dev", "B", "Dev"]).SequenceEqual(["B", "A", "Dev"]), "a tab out of reach keeps a place, once");
            Check(TabOrder.Keep(["B", "A"], null).SequenceEqual(["B", "A"]), "nothing kept before");
        }),
        ("Tab order 1697: wiring: reorderable bar, places read after each tab, kept when a drag ends, made again otherwise, reset in Settings", () =>
        {
            var header = Read("UI/Navigation/MainWindow.PageHeader.cs");
            Check(header.Contains("ImGuiTabBarFlags.Reorderable", StringComparison.Ordinal) && header.Contains("drag.Places.Add((entry.Page.ToString(),ImGui.GetItemRectMin().X));", StringComparison.Ordinal), "reorderable, places read");
            Check(header.Contains("TabOrder.Arrange(natural,configuration.TabOrders.GetValueOrDefault(key),entry=>entry.Page.ToString())", StringComparison.Ordinal), "drawn in the order kept");
            // X-709 (0.1700): the families' header uses the detail tabs' memory (TabBarDrag): kept when a drag ends, settled otherwise.
            Check(header.Contains("TabBarAfter(key,drag,dragged);", StringComparison.Ordinal) && header.Contains("TrySave(\"TabOrders\");", StringComparison.Ordinal), "kept and saved when a drag ends");
            Check(header.Contains("if(drag.TakeFresh())pageTabs.Reset();", StringComparison.Ordinal), "a new bar (put back in order): the page shown selected");
            Check(Read("MainWindow.Settings.cs").Contains("if(SettingButton(\"Settings.TabOrderReset\",\"Settings.TabOrderResetTip\"))ResetTabOrders();", StringComparison.Ordinal), "put back in order");
            var configuration = Read("Configuration.cs");
            Check(configuration.Contains("public Dictionary<string,List<string>> TabOrders { get; set; } = new();", StringComparison.Ordinal) && configuration.Contains("\"TabOrders\",", StringComparison.Ordinal), "saved, reset with the settings");
        }),
    ];
}
