// 0.751 (X-04, user request): the items the tracking lists need, at the node.
internal static class TrackedNeed751Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Tracked need: the crafting list once, the workshop and the preparation added, only lists worked out lately",() =>
        {
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("return Math.Max(GatherListNeed(item),Panel(\"CraftList\"))+Panel(\"Workshop\")+Panel(\"Preparation\");"),"sum");
            Check(panel.Contains("if(ListPanelLinesFor(key) is not { } known)return 0;") && Read("MainWindow.ListPanels.cs").Contains("if(!ListPanelOn(key) || !listPanelSources.TryGetValue(key,out var source))return null;"),"0.764: folded counts, closed does not");
            Check(!panel.Contains("GetForegroundDrawList") && panel.Contains("ImGui.GetBackgroundDrawList()"),"0.764: frames under Tensho's windows");
            var lists=Read("MainWindow.ListPanels.cs");
            foreach(var key in new[]{"Workshop","CraftList","Preparation"})Check(lists.Contains($"listPanelSources.TryAdd(\"{key}\","),"0.765: "+key+" known without drawing");
            Check(panel.Contains("configuration.GatheringListHighlight && GatheringSlotRects?.Invoke()"),"setting");
            var reader=Read("Gathering/GatheringObservationReader.Dalamud.cs");
            Check(reader.Contains("slotRects=Rects(addon);") && !reader.Contains("FireCallback"),"read only, with the slots");
        }),
    ];
}
