using TenshoAssistant.Desktop;
using TenshoAssistant.Inventory;

// 0.1591 (X-394, review of 0.1361, important finding 1, R37): the sort preview's expected picture, with a sample inventory: the frames in
// fill order (bags 1, 3, 2, 4) and the sort's line once in developer mode.
internal static class SortTrial1591Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sort trial 1591: the sample inventory is one character's four bags, complete, a stack per slot, in all four bags",() =>
        {
            var snapshot=InventorySortTrial.Snapshot(new DateTimeOffset(2026,11,11,10,0,0,TimeSpan.Zero));
            var scope=InventorySortScope.Personal(InventorySortTrial.Character);
            var containers=snapshot.Containers.Values.ToArray();
            Check(containers.Length==4 && containers.All(container=>container.Location.Owner.Value==scope.InventoryOwner && container.Coverage==InventoryCoverage.Complete),"four complete bags of the character");
            var stacks=containers.SelectMany(container=>container.Stacks.Select(stack=>(Bag:container.Location.Container.Value,stack.Slot,stack.DisplayIndex))).ToArray();
            Check(stacks.Length==InventorySortTrial.Stacks && stacks.Distinct().Count()==stacks.Length,"a slot each");
            Check(stacks.All(stack=>stack.Slot is >=0 and <35 && stack.DisplayIndex==stack.Bag*35+stack.Slot),"places read");
            Check(Enumerable.Range(0,4).All(bag=>stacks.Any(stack=>stack.Bag==bag)),"every bag holds something");
            Check(InventorySortRules.ShownContainers(scope).SequenceEqual([0,2,1,3]),"drawn 1, 3, 2, 4");
            Check(TenshoAssistant.UI.UiStates.DeveloperDetail(InventorySortTrial.Message,InventorySortTrial.Message) is null,"the line once");
        }),
        ("Sort trial 1591: wiring: the picture planned, the trial given before it and taken back after, the page reads it",() =>
        {
            var plan=ReferencePlan.Plan(DesktopPages.AsIs.Concat(DesktopPages.NeedsLink));
            Check(plan.Count(shot=>shot.Id=="SortPreview-trial" && shot.Page=="SortPreview" && shot.Setup=="sort-trial")==1,"planned once");
            var settings=plan.ToList().FindIndex(shot=>ReferencePlan.SettingsSetup(shot.Setup) is not null);
            Check(settings<0 || plan.ToList().FindIndex(shot=>shot.Id=="SortPreview-trial")<settings,"before the Settings tabs (they come last)");
            var references=Read("src/TenshoAssistant.Desktop/References.cs");
            Check(references.Contains("if(shot.Setup==\"sort-trial\")host.Surface.SortTrial(true);") && references.Contains("if(shot.Setup==\"sort-trial\")host.Surface.SortTrial(false);"),"given, taken back");
            var window=Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(window.Contains("SortTrial=DesktopSortTrial,") && window.Contains("if(!inDesktopApp)return;") && window.Contains("developerMode=sortTrialDeveloper;sortTrial=null;"),"the desktop application only, developer mode given back");
            var page=Read("src/TenshoAssistant/MainWindow.SortPreview.cs");
            Check(page.Contains("var character = sortTrial?.Character ?? bankBridge.State.CharacterId;") && page.Contains("sortTrial?.Snapshot ?? inventory.Current"),"the page reads it");
        }),
    ];
}
