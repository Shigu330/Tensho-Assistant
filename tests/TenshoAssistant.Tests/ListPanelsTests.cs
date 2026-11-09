// Test audit of 2026-11-09, batch 2 (0.1021): ListAlarms755Tests, ListAlarms832Tests, ListBlink740Tests, ListBlink741Tests, ListBlink742Tests, ListPanels691Tests, ListZones752Tests merged into ListPanelsTests; every case is kept.
using TenshoAssistant.Alarms;
using TenshoAssistant.Diagnostics;
using TenshoAssistant.Inventory;
using TenshoAssistant.Craft;
internal static class ListPanelsTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static readonly DateTimeOffset At=new(2026,11,7,11,0,0,TimeSpan.Zero);
    private static readonly InventoryOwnerId Owner=new(1);
    private static InventoryContainerSnapshot Bag(int id,InventoryCoverage coverage,long quantity)=>
        new(new(Owner,new(id)),coverage,quantity>0 ? [new InventoryStack(new(new(5111),false),quantity,0)]:[]);
    private static InventorySnapshot Snapshot(params InventoryContainerSnapshot[] bags)=>new(DateTimeOffset.UtcNow,"test",bags);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From ListAlarms755Tests: 0.755 (X-09, user request): the alarms of a tracking list's timed points.
        ("List alarms: what is still needed, timed points only, named as the Gathering page names them, never twice",() =>
        {
            var page=Read("MainWindow.ListPanels.cs");
            Check(page.Contains("held.Total>=needed)continue;"),"only what is still needed");
            Check(page.Contains("if(!ListPanelTimedOnly(item))continue;"),"0.772: only what grows nowhere else");
            Check(page.Contains("timing.Kind!=Gathering.GatheringScheduleKind.Limited"),"timed only");
            Check(page.Contains("$\"{name} · {place}\"") && Read("MainWindow.Alarms.cs").Contains("var label=$\"{SelectedItem?.Name ?? itemId.ToString()} · {place}\";"),"same name as the Gathering page");
            Check(page.Contains("known.Label==alarm.Label && known.Repeat==alarm.Repeat && known.EtMinute==alarm.EtMinute"),"existing left out");
            Check(page.Contains("AddAlarms(known.Alarms)"),"through the usual add, with its maximum and the game's copy");
        }),
        // From ListAlarms832Tests: 0.832 (X-62, user report): alarms added from a tracking list still rang with the item gathered (24/2).
        ("List alarms: silent once the item is held as the list needed; other alarms always ring",() =>
        {
            Check(AlarmSchedule.Gathered(new TenshoAlarm{ItemId=5,Needed=2},24),"24/2: silent");
            Check(!AlarmSchedule.Gathered(new TenshoAlarm{ItemId=5,Needed=2},1),"1/2: rings");
            Check(!AlarmSchedule.Gathered(new TenshoAlarm{ItemId=5},24),"not from a list: rings");
            var service=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Alarms/AlarmService.Dalamud.cs"));
            Check(service.Contains("if (check != AlarmCheck.Fire || gathered) continue;") && service.Contains("if (!gathered && settings.RingOnTime"),"ring and on-time skipped");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs")).Contains("alarm.Needed=needed;"),"the list's quantity kept");
        }),
        // From ListBlink740Tests: 0.740 (user report): a list panel line goes back and forth while gathering.
        ("List blink: a value that comes back within three seconds is said, once in thirty seconds; a steady rise is not",() =>
        {
            var watch=new BlinkWatch();
            Check(watch.Observe("CraftList",5,10,30,At) is null && watch.Observe("CraftList",5,10,30,At.AddSeconds(1)) is null,"first and same");
            Check(watch.Observe("CraftList",5,11,30,At.AddSeconds(2)) is null,"one more gathered");
            var back=watch.Observe("CraftList",5,10,30,At.AddSeconds(3));
            Check(back is not null && back.Contains("held 10->11->10") && back.Contains("needed 30->30->30"),back ?? "none");
            Check(watch.Observe("CraftList",5,11,30,At.AddSeconds(4)) is null,"quiet for thirty seconds");
            var rise=new BlinkWatch();
            foreach(var held in new[]{1,2,3,4})Check(rise.Observe("Workshop",7,held,9,At.AddSeconds(held)) is null,"rise");
            var slow=new BlinkWatch();
            slow.Observe("Workshop",8,1,9,At);slow.Observe("Workshop",8,2,9,At.AddSeconds(1));
            Check(slow.Observe("Workshop",8,1,9,At.AddSeconds(10)) is null,"too slow to be a blink");
            var lists=new BlinkWatch();
            lists.Observe("A",9,1,5,At);lists.Observe("B",9,2,5,At);
            Check(lists.Observe("A",9,1,5,At) is null,"each list apart");
            var zero=new BlinkWatch();
            Check(!zero.FellToZero("W",1,0,At) && !zero.FellToZero("W",1,5,At),"from nothing, or a rise");
            Check(zero.FellToZero("W",1,0,At.AddSeconds(1)),"5 to 0");
            zero.FellToZero("W",2,4,At);
            Check(!zero.FellToZero("W",2,0,At.AddSeconds(2)),"once in thirty seconds per list");
        }),
        // From ListBlink741Tests: 0.741 (user report): counts of the list panels fell to 0 for 200 ms at each item gathered.
        ("List blink: stock counts read the last reading; the bag sort still reads only a sure one",() =>
        {
            var items=Read("MainWindow.Items.cs");
            var refresh=items[items.IndexOf("private void RefreshRecipeHoldingsCache()")..];
            refresh=refresh[..refresh.IndexOf("recipeHoldingsLive = live;")];
            // 0.744: the first fix landed on another function that began with the same line; this one feeds the lists and panels.
            Check(refresh.Contains("var live = inventory.Shown;") && !refresh.Contains("inventory.Current"),"lists and panels");
            foreach(var file in new[]{"MainWindow.SortPreview.cs","MainWindow.InventorySortButton.cs"})
                Check(!Read(file).Contains("inventory.Shown"),file+": what acts never reads the display copy");
        }),
        // From ListBlink742Tests: 0.742 (user report): the blink came back after 0.741: a reading made during the change found the bags unreadable.
        ("List blink: an unreadable bag keeps its last complete reading on screen; what acts sees what was read",() =>
        {
            var service=new InventoryService();
            service.Replace(Snapshot(Bag(0,InventoryCoverage.Complete,345),Bag(1,InventoryCoverage.Complete,0)));
            service.Replace(Snapshot(Bag(0,InventoryCoverage.Unknown,0),Bag(1,InventoryCoverage.Complete,0)));
            var shown=service.Shown!.Containers[new(Owner,new(0))];
            Check(shown.Coverage==InventoryCoverage.Complete && shown.Quantities.Values.Sum()==345,"345 stays, not 0");
            Check(service.Current!.Containers[new(Owner,new(0))].Coverage==InventoryCoverage.Unknown,"the sort sees it unknown");
            service.Replace(Snapshot(Bag(0,InventoryCoverage.Complete,346),Bag(1,InventoryCoverage.Complete,0)));
            Check(service.Shown!.Containers[new(Owner,new(0))].Quantities.Values.Sum()==346,"the next complete reading replaces it");
        }),
        ("List blink: nothing is kept from before a clear, and a bag never read stays unknown",() =>
        {
            var service=new InventoryService();
            service.Replace(Snapshot(Bag(0,InventoryCoverage.Complete,345)));
            service.Clear();
            service.Replace(Snapshot(Bag(0,InventoryCoverage.Unknown,0)));
            Check(service.Shown!.Containers.Values.Single().Coverage==InventoryCoverage.Unknown,"after a logout or another character");
            var fresh=Snapshot(Bag(2,InventoryCoverage.Partial,3));
            Check(ReferenceEquals(InventoryService.ForDisplay(Snapshot(Bag(0,InventoryCoverage.Complete,1)),fresh),fresh),"no complete reading of that bag: as read");
        }),
        // From ListPanels691Tests: W-05 (0.691): the on-screen lists of a workshop project and of a preparation.
        ("List panels: both are off by default, created with the others, and their settings are on their pages",() =>
        {
            var configuration=new TenshoAssistant.Configuration();
            Check(!configuration.WorkshopPanel && !configuration.PreparationPanel,"off by default");
            Check(Read("MainWindow.TrackerWindow.cs").Contains("new(this,WorkshopListPanel),new(this,PreparationListPanel)"),"created");
            Check(Read("MainWindow.WorkshopProjects.cs").Contains("BoolSetting(\"FreeCompany.WorkshopPanel\""),"workshop setting");
            Check(Read("MainWindow.CraftPlan.cs").Contains("BoolSetting(\"PlanUi.Panel\""),"preparation setting");
        }),
        ("List panels: the crafting list has its own, with its setting on its tab, and the island's board does not hide a panel",() =>
        {
            Check(Read("MainWindow.CraftLists.cs").Contains("BoolSetting(\"CraftLists.Panel\""),"setting");
            Check(Read("MainWindow.TrackerWindow.cs").Contains("new(this,CraftListListPanel)"),"created");
            Check(!new TenshoAssistant.Configuration().CraftListPanel,"off by default");
            Check(TenshoAssistant.UI.GameWindows.Ignored("MJIHud"),"the board");
            Check(TenshoAssistant.UI.GameWindows.Ignored("JobHudRRP0") && TenshoAssistant.UI.GameWindows.Ignored("JobHudRRP1"),"the job gauges, read in the log");
            Check(!TenshoAssistant.UI.GameWindows.Ignored("MJIAnimalManagement"),"the pasture window stays a window");
        }),
        ("List panels: shown means shown unless the player chooses, and unread stocks are not shown as zero",() =>
        {
            var page=Read("MainWindow.ListPanels.cs");
            Check(System.Text.RegularExpressions.Regex.Matches(page,@"ListPanelsHiddenInCombat").Count==4,"combat, by choice");
            var settings=new TenshoAssistant.Configuration();
            Check(!settings.ListPanelsHideInCombat && !settings.ListPanelsHideBehindGame,"never hidden by default");
            Check(System.Text.RegularExpressions.Regex.Matches(page,@"\(\)=>configuration\.ListPanelsHideBehindGame\);").Count==3,"behind the game, by choice");
            Check(Read("PanelWindow.cs").Contains("panel.StepsAside?.Invoke() != false && lastPosition"),"the window honours it");
            Check(page.Contains("if(!stock.HasObservation)ImGui.TextDisabled("),"unknown is not zero");
            Check(System.Text.RegularExpressions.Regex.Matches(page,@"RefreshRecipeHoldingsCache\(\);").Count==3,"stocks refreshed");
        }),
        // From ListZones752Tests: 0.752 (X-05, user request): the tracking lists' gathered items, by zone.
        ("By zone: the zone with the most comes first and takes its items, the list's order kept inside, no zone last",() =>
        {
            var zones=new Dictionary<string,string[]>{["ore"]=["Kholusia","Lakeland"],["log"]=["Lakeland"],["herb"]=["Lakeland","Il Mheg"],["sand"]=["Kholusia"],["fish"]=[]};
            var groups=BaseMaterials.ByZone(["ore","log","herb","sand","fish"],line=>zones[line]);
            Check(groups.Select(group=>group.Zone).SequenceEqual(["Lakeland","Kholusia",""]),string.Join(",",groups.Select(group=>group.Zone)));
            Check(groups[0].Lines.SequenceEqual(["ore","log","herb"]) && groups[1].Lines.SequenceEqual(["sand"]) && groups[2].Lines.SequenceEqual(["fish"]),"lines");
            Check(BaseMaterials.ByZone(new string[0],_=>new string[0]).Count==0,"empty");
            var tie=BaseMaterials.ByZone(["a","b"],line=>line=="a" ? new[]{"Zeta"}:new[]{"Alpha"});
            Check(tie[0].Zone=="Alpha","ties by name");
        }),
        ("By zone: only for what is gathered, behind a setting",() =>
        {
            var page=Read("MainWindow.ListPanels.cs");
            Check(page.Contains("if(!configuration.ListPanelsByZone || origins.FirstOrDefault(group=>group.Origin==ItemOrigin.Gathering)"),"gathered only, behind its setting");
            Check(page.Contains("if(configuration.ListPanelsBase)lumina?.Request();"),"0.779: the lists ask for the monster source themselves");
        }),
    ];
}
