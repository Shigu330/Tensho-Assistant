using TenshoAssistant.Craft;

// 0.752 (X-05, user request): the tracking lists' gathered items, by zone.
internal static class ListZones752Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
            Check(page.Contains("origin==ItemOrigin.Gathering && configuration.ListPanelsByZone"),"gathered only");
        }),
    ];
}
