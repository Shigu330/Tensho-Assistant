// X-735 (review of block 0.1727 to 0.1733, minor M1): the tracker's alarm gestures without the item list knowing the item.
internal static class ReviewMinors1735Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1735: M1: a tracker row whose item is unknown keeps its alarm's gestures, in a menu of its own", () =>
        {
            var tracker = Read("MainWindow.TrackerWindow.cs");
            var unknown = tracker.IndexOf("if(item is null)\n", StringComparison.Ordinal);
            var common = tracker.IndexOf("ItemLinksMenu(UniqueMenuId($\"{id}{row.Id}\"),item,first:", StringComparison.Ordinal);
            Check(unknown > 0 && common > unknown, "the unknown item handled before the common menu (which draws nothing without an item)");
            var block = tracker[unknown..common];
            Check(block.Contains("if(alarm is not null && ImGui.BeginPopupContextItem(UniqueMenuId($\"{id}{row.Id}\")))", StringComparison.Ordinal)
                && block.Contains("ImGui.TextColored(Accent,row.Label);ImGui.Separator();TrackerAlarmMenu(alarm,map,place);", StringComparison.Ordinal)
                && block.Contains("finally{ImGui.EndPopup();}", StringComparison.Ordinal) && block.Contains("return;", StringComparison.Ordinal), "the alarm's name, then its gestures; the popup always closed");
            Check(Read("MainWindow.DutyFinder.cs").Contains("if(item is null || !ImGui.BeginPopupContextItem(id))return;", StringComparison.Ordinal), "the common menu still needs an item (why the tracker does it apart)");
        }),
    ];
}
