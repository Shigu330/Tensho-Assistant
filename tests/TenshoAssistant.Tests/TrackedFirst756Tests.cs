// 0.756 (X-10, user request): the gathering help starts on an item the lists need.
internal static class TrackedFirst756Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Tracked first: once per node, the item the lists need most, only when they need one; a new node asks again",() =>
        {
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("gatheringPanelStart=start=new(owner,frame);\n            gatheringTrackedPicked=false;") || panel.Contains("gatheringPanelStart=start=new(owner,frame);\r\n            gatheringTrackedPicked=false;"),"reset per node");
            Check(panel.Contains("OrderByDescending(slot=>TrackedNeed(slot.ItemId))") && panel.Contains("TrackedNeed(wanted.ItemId)>0)SelectItem(wanted.ItemId);"),"most needed, if any");
        }),
    ];
}
