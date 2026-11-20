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
            // X-648 (review of 0.1593): the sample really sorted by the rules the preview uses, then the placed stacks counted per bag.
            var sorted=InventorySortRules.Build(containers,scope,InventorySortRules.Default(scope),id=>("Item "+id.ToString("D4"),1u));
            Check(sorted.Slots is { } slots && slots.Count==4*scope.SlotsPerContainer,"one entry per position");
            var per=Enumerable.Range(0,4).Select(bag=>Enumerable.Range(bag*scope.SlotsPerContainer,scope.SlotsPerContainer).Count(slot=>sorted.Slots![slot] is not null)).ToArray();
            Check(per[0]==scope.SlotsPerContainer && per[2]==scope.SlotsPerContainer,"bags 1 and 3 full: "+string.Join(",",per));
            Check(per[1]==count-2*scope.SlotsPerContainer && per[3]==0,"bag 2 holds the rest, bag 4 empty: "+string.Join(",",per));
            Check(Enumerable.Range(scope.SlotsPerContainer,per[1]).All(slot=>sorted.Slots![slot] is not null),"bag 2 filled from its first slot");
            Check(sorted.Stacks.Count==count,"every stack placed once");
            // X-651 (review of 0.1594, M1): the order between the bags, not only their counts. sorted.Stacks follows the slots (bag after
            // bag); the sorted order itself is the preview's, so its 1st stack is in bag 1's first slot, its 36th in bag 3's (bag 3 before
            // bag 2), its 71st in bag 2's.
            var order=InventorySortPreview.Build(containers,scope.Capacity,InventorySortRules.Default(scope).Criteria,id=>("Item "+id.ToString("D4"),1u)).Stacks;
            Check(order.Count==count,"the sorted order holds every stack");
            Check(Equals(sorted.Slots![0],order[0]),"1st sorted stack in bag 1, slot 1");
            Check(Equals(sorted.Slots![2*scope.SlotsPerContainer],order[scope.SlotsPerContainer]),"36th sorted stack in bag 3, slot 1");
            Check(Equals(sorted.Slots![scope.SlotsPerContainer],order[2*scope.SlotsPerContainer]),"71st sorted stack in bag 2, slot 1");
        }),
        ("Sort trial 1593: wiring: the trial picture scrolled to the frames' foot, after the frames",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.SortPreview.cs");
            var scroll=page.IndexOf("if(sortTrial is not null)ImGui.SetScrollHereY(1f);",StringComparison.Ordinal);
            Check(scroll>page.IndexOf("ImGui.BeginChild($\"SortPreviewBag{bag}\"",StringComparison.Ordinal) && scroll<page.IndexOf("if(openSlotMenu)ImGui.OpenPopup(\"SortSlotMenu\");",StringComparison.Ordinal),"after the frames");
        }),
    ];
}
