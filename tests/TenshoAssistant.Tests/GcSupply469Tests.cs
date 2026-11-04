using TenshoAssistant.Items;

// 0.469 (user requests, in-game campaign): today's Grand Company request read from the game's window; icons in the crafting log panel.
internal static class GcSupply469Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static readonly DateTimeOffset Today=new(2026,11,3,20,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Grand Company request: nothing seen is « not observed »; seen today, an item is requested or only not listed",() =>
        {
            var today=new GcSupplyToday();
            Check(today.Status(1,Today,10)==GcSupplyStatus.NotObserved,"nothing seen");
            Check(!today.Observe(1,Today,[]) && today.Status(1,Today,10)==GcSupplyStatus.NotObserved,"a window without a known item proves nothing");
            Check(today.Observe(1,Today,[10]) && today.Status(1,Today,10)==GcSupplyStatus.Requested && today.Status(1,Today,11)==GcSupplyStatus.NotListed,"seen");
            Check(today.Observe(1,Today,[11]) && !today.Observe(1,Today,[10,11]) && today.Status(1,Today,11)==GcSupplyStatus.Requested,"another tab adds, the same one does not");
        }),
        ("Grand Company request: starts empty at the daily reset and for another character",() =>
        {
            var today=new GcSupplyToday();today.Observe(1,Today,[10]);
            Check(today.Status(1,Today.AddDays(1),10)==GcSupplyStatus.NotObserved,"next day");
            Check(today.Status(2,Today,10)==GcSupplyStatus.NotObserved,"other character");
            Check(today.Observe(1,Today.AddDays(1),[20]) && today.Status(1,Today.AddDays(1),10)==GcSupplyStatus.NotListed,"yesterday's items are gone");
        }),
        ("Grand Company request: kept through a reload; its copy cannot change the state",() =>
        {
            var first=new GcSupplyToday();first.Observe(1,Today,[10]);
            var saved=first.Seen!;saved.Items.Add(99);
            Check(first.Status(1,Today,99)==GcSupplyStatus.NotListed,"copy");
            var second=new GcSupplyToday();second.Restore(first.Seen);
            Check(second.Status(1,Today,10)==GcSupplyStatus.Requested,"restored");
            var empty=new GcSupplyToday();empty.Restore(null);empty.Restore(new(0,Today,[10]));
            Check(empty.Seen is null,"no character, nothing restored");
        }),
        ("Grand Company request: only a text that is exactly a delivery item's name counts",() =>
        {
            var names=GcSupplyToday.NameIndex([(10,"Chondrite"),(11,"Bois de mélèze"),(12,""),(13,null),(0,"Rien")]);
            Check(names.Count==2,"index");
            Check(GcSupplyToday.Match(["Chondrite","Bois de mélèze ","chondrite",null,"Livraison de Chondrite x3","Rien"],names).SequenceEqual([10u,11u]),"exact, without the game's glyphs");
            Check(GcSupplyToday.Normalize(" Chondrite\n")=="Chondrite" && GcSupplyToday.Normalize(null)=="","normalize");
        }),
        ("Wiring: the game's window read only, every line says the real status; panel icons, crafting bonus only",() =>
        {
            var reader=Read("Items/GcSupplyReader.Dalamud.cs");
            Check(reader.Contains("\"ContentsInfoDetail\"") && reader.Contains("framework.Update-=Update;") && !reader.Contains("Fire"),"read only, with its cleanup");
            Check(Read("Plugin.cs").Contains("new Items.GcSupplyReader(") && Read("Configuration.cs").Contains("\"GcSupplySeen\""),"registered, kept");
            foreach(var file in new[]{"MainWindow.MarketHover.cs","MainWindow.Tools.cs","MainWindow.ItemUses.cs"})Check(Read(file).Contains("GcSupplyLine("),file);
            var panel=Read("MainWindow.CraftLog.cs");
            Check(panel.Contains("ImGui.Checkbox(\"###CraftLogManipulation\"") && panel.Contains("Find(job,CraftActionKind.Manipulation)") && panel.Contains("DrawDetectedConsumables(craftLogDetected)"),"panel");
            var consumables=Read("MainWindow.CraftConsumables.cs");
            Check(consumables.Contains("DrawItemInfoTooltip(known.Item.ItemId)") && !consumables.Contains("DetectedOther"),"icon and tooltip; no other food is named");
        }),
        ("Crafting log panel: in sections, a quality bar, the explanations in a tooltip; nothing says Manipulation is excluded when the box allows it",() =>
        {
            var panel=Read("MainWindow.CraftLog.cs");
            Check(panel.Contains("\"CraftLogUi.SectionState\"") && panel.Contains("\"CraftLogUi.SectionTarget\"") && panel.Contains("\"CraftLogUi.SectionResult\"") && panel.Contains("ImGui.BeginTable(\"###CraftLogFigures\",4,"),"sections");
            Check(panel.Contains("ImGui.ProgressBar(maximum>0") && panel.Contains("if(result.Saved is not null)DrawCraftLogMacro("),"result");
            foreach(var language in new[]{"en","fr"})
            {
                var texts=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")))!["CraftLogUi"]!;
                Check(!texts["Assumptions"]!.GetValue<string>().Contains("Manipulation"),"assumptions: "+language);
            }
        }),
    ];
}
