using TenshoAssistant.Craft;

// 0.757 (X-11, X-12, user request): zones that fold, here first, then the cheapest teleport.
internal static class ZoneOrder757Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static (string,IReadOnlyList<int>) Zone(string name,int lines)=>(name,Enumerable.Range(0,lines).ToArray());

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Zone order: here first, then the cheapest teleport, unknown prices after, more lines first among equals",() =>
        {
            var costs=new Dictionary<string,uint>{["Kholusia"]=900,["Lakeland"]=300,["Il Mheg"]=300};
            var order=BaseMaterials.OrderZones([Zone("Kholusia",5),Zone("Lakeland",1),Zone("Il Mheg",3),Zone("Rak'tika",4),Zone("Tempest",2)],"Tempest",zone=>costs.TryGetValue(zone,out var gil) ? gil:null);
            Check(order.Select(group=>group.Zone).SequenceEqual(["Tempest","Il Mheg","Lakeland","Kholusia","Rak'tika"]),string.Join(",",order.Select(group=>group.Zone)));
            Check(BaseMaterials.OrderZones([Zone("B",1),Zone("A",1)],null,_=>null).Select(group=>group.Zone).SequenceEqual(["A","B"]),"nothing known: by name");
        }),
        ("Zone order: zones fold, priced from the game's teleport list, asked once a minute",() =>
        {
            var page=Read("MainWindow.ListPanels.cs");
            Check(page.Contains("ImGui.TreeNodeEx($\"{group.Label} ({zoneDone}/{group.Lines.Length}){group.Note}###ListZone{key}{group.Id}\",ImGuiTreeNodeFlags.DefaultOpen)") && page.Contains("ImGui.TreePop();"),"folds");
            Check(page.Contains("now-listTeleportAsked<60000") && page.Contains("mapLive?.RefreshTeleportCosts();"),"priced");
            // 0.758 (X-13): timed points apart, first, folding, and only items that grow nowhere else.
            Check(page.Contains("zones.Add(new(\"Timed\",localization.Get(\"ListPanels.TimedGroup\"),\"\",true,timed));") && page.Contains("var rest=gathered.Except(timed).ToArray();"),"timed group");
            Check(page.Contains("sources.All(source=>source.Timing.Kind==Gathering.GatheringScheduleKind.Limited)"),"timed only");
            // 0.760 (X-01): worked out with the lines, never per frame; zones indexed once per catalogue.
            Check(page.Contains("layout.At!=known.At || layout.Here!=here") && !page.Contains("catalog.Sources.Where(source=>source.Zone==zone)"),"not per frame");
            // 0.761 (X-14): open or closed, once a second, the open point first.
            Check(page.Contains("if(group.Timed && ListPanelTimedState(item) is { } state)") && page.Contains("if(second!=listTimedStates.Second)"),"open or closed");
        }),
    ];
}
