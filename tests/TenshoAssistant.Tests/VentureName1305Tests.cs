using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1305 (X-263, user request of 13/11 around 12:40, a picture « Seen in game: Zelkova log ← venture no. 584 »): a venture discovery
// names the venture as the game's sheets do: RetainerTask 584 is an exploration (IsRandom, Task 30084 → RetainerTaskRandom « Woodland
// Exploration XXI », probed on 14/11), a targeted venture reads its class and level, and the number shows only when the sheets give nothing.
internal static class VentureName1305Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly (string,string)[] Jobs=[("GLA","gladiator"),("MIN","miner"),("BTN","botanist"),("FSH","fisher"),("","")];
    private static string Text(string language,(string Key,object[] Args) said)
    {
        using var json=JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json"));
        var parts=said.Key.Split('.');
        return string.Format(json.RootElement.GetProperty(parts[0]).GetProperty(parts[1]).GetString()!,said.Args);
    }
    private static string Shown(string language,uint id,VentureRow? row)
    {
        using var json=JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json"));
        return string.Format(json.RootElement.GetProperty("DiscoveriesUi").GetProperty("FromVenture").GetString()!,"Zelkova log",Text(language,VentureNames.Describe(id,row)));
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Venture name 1305: an exploration by its name, a targeted venture by class and level, the number when the sheets give nothing",() =>
        {
            Check(VentureNames.Describe(584,new VentureRow(true,"Exploration en forêt XXI","",65)) is (VentureNames.Named,["Exploration en forêt XXI"]),"exploration");
            Check(VentureNames.Describe(10,new VentureRow(false,"","botaniste",70)) is (VentureNames.Job,["botaniste",70]),"targeted");
            Check(VentureNames.Describe(584,null) is (VentureNames.Number,[584u]),"no row");
            Check(VentureNames.Describe(584,new VentureRow(true,"","",65)).Key==VentureNames.Number,"an exploration without a name");
            Check(VentureNames.Describe(10,new VentureRow(false,"","",70)).Key==VentureNames.Number && VentureNames.Describe(10,new VentureRow(false,"","miner",0)).Key==VentureNames.Number,"a targeted venture without class or level");
            Check(Shown("fr",584,new VentureRow(true,"Exploration en forêt XXI","",65))=="Zelkova log ← tâche : Exploration en forêt XXI","fr: exploration");
            Check(Shown("fr",10,new VentureRow(false,"","botaniste",70))=="Zelkova log ← tâche : tâche de botaniste niv. 70","fr: targeted");
            Check(Shown("fr",584,null)=="Zelkova log ← tâche : n° 584","fr: number");
            Check(Shown("en",584,new VentureRow(true,"Woodland Exploration XXI","",65))=="Zelkova log ← venture: Woodland Exploration XXI","en: exploration");
            Check(Shown("en",10,new VentureRow(false,"","botanist",70))=="Zelkova log ← venture: botanist venture, level 70","en: targeted");
            Check(Shown("en",584,null)=="Zelkova log ← venture: #584","en: number");
        }),
        ("Venture name 1305: the class is the ClassJob whose abbreviation is the category's name; a category of several classes keeps its name",() =>
        {
            Check(VentureNames.JobName("BTN",Jobs)=="botanist" && VentureNames.JobName("min",Jobs)=="miner","one class");
            Check(VentureNames.JobName("Disciples of War or Magic",Jobs)=="Disciples of War or Magic","several classes");
            Check(VentureNames.JobName("",Jobs)=="","no category");
        }),
        ("Venture name 1305: the discoveries page and the plugin use the sheets' venture name, not the bare number",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Discoveries.cs");
            Check(page.Contains("localization.Format(\"DiscoveriesUi.FromVenture\",DiscoveryItemName(fact.Subject),DiscoveryVentureName(fact.Detail))",StringComparison.Ordinal)
                && page.Contains("VentureNames.Describe(id,VentureRowOf?.Invoke(id))",StringComparison.Ordinal),"page");
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("mainWindow.VentureRowOf=",StringComparison.Ordinal) && plugin.Contains("RetainerTaskRandom>().GetRowOrDefault(task.Task.RowId)",StringComparison.Ordinal),"plugin");
        }),
    ];
}
