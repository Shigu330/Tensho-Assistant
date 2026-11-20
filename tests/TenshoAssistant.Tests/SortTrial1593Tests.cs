using TenshoAssistant.Inventory;

// 0.1593 (X-647, review of 0.1592, minor M1 on X-394): the sample inventory overflows bag 1, so the sorted stacks show the fill order
// (bag 1 full, then bag 3, then bag 2), and the picture is scrolled to the frames' foot (whole frames in 1280x860).
internal static class SortTrial1593Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sort trial 1593: the sorted sample fills bag 1, then bag 3, then a part of bag 2, bag 4 empty",() =>
        {
            var snapshot=InventorySortTrial.Snapshot(new DateTimeOffset(2026,11,20,10,0,0,TimeSpan.Zero));
            var scope=InventorySortScope.Personal(InventorySortTrial.Character);
            var containers=snapshot.Containers.Values.ToArray();
            Check(containers.All(container=>container.Stacks.Count()<=scope.SlotsPerContainer),"each bag holds what it can");
            var count=containers.Sum(container=>container.Stacks.Count());
            Check(count==InventorySortTrial.Stacks && count>2*scope.SlotsPerContainer && count<3*scope.SlotsPerContainer,"more than two bags, less than three");
            var fill=InventorySortRules.FillOrder(scope);
            var used=fill.Take(count).Select(slot=>slot/scope.SlotsPerContainer).Distinct().ToArray();
            Check(used.SequenceEqual([0,2,1]),"filled 1, 3, then 2");
            Check(fill.Take(count).Count(slot=>slot/scope.SlotsPerContainer==1)==count-2*scope.SlotsPerContainer,"bag 2 holds the rest");
        }),
        ("Sort trial 1593: wiring: the trial picture scrolled to the frames' foot, after the frames",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.SortPreview.cs");
            var scroll=page.IndexOf("if(sortTrial is not null)ImGui.SetScrollHereY(1f);",StringComparison.Ordinal);
            Check(scroll>page.IndexOf("ImGui.BeginChild($\"SortPreviewBag{bag}\"",StringComparison.Ordinal) && scroll<page.IndexOf("if(openSlotMenu)ImGui.OpenPopup(\"SortSlotMenu\");",StringComparison.Ordinal),"after the frames");
        }),
    ];
}
