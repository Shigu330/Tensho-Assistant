using TenshoAssistant.Craft;

// 0.464 (in-game campaign, batch 5): detection for the simulator, list tab, live panel, order of the simulator's sections.
internal static class Craft464Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Food and medicine from the status: ItemFood row, plus 10000 for HQ; other kind, no bonus or nothing active give nothing",() =>
        {
            var food=new CraftConsumable(1,"Food",false,[],52);var draught=new CraftConsumable(2,"Draught",true,[],52);
            CraftConsumable[] list=[food,draught];
            Check(CraftConsumables.FromStatus(list,52,false) is { Hq:false } nq && nq.Item==food,"NQ food");
            Check(CraftConsumables.FromStatus(list,10052,false) is { Hq:true } hq && hq.Item==food,"HQ food");
            Check(CraftConsumables.FromStatus(list,10052,true) is { Hq:true } medicine && medicine.Item==draught,"medicine");
            Check(CraftConsumables.FromStatus(list,53,false) is null && CraftConsumables.FromStatus(list,null,false) is null && CraftConsumables.FromStatus(list,0,false) is null && CraftConsumables.FromStatus(null,52,false) is null,"nothing");
        }),
        ("Simulator: detected once per recipe and job, specialist only once resolved; saving comes after adding actions; green or red against the recipe",() =>
        {
            var ui=Read("MainWindow.CraftSimulation.cs");
            Check(ui.Contains("DetectCraftState(recipe,job,canCopy,CopyStats);"),"detected");
            Check(ui.IndexOf("CraftSimulation.AddAction",StringComparison.Ordinal)<ui.IndexOf("DrawCraftPresetSave();",StringComparison.Ordinal),"save after the palette");
            Check(ui.Contains("simulationCs>=p.RequiredCraftsmanship") && ui.Contains("simulationLevel>=recipe.Level,"),"colours");
            var consumables=Read("MainWindow.CraftConsumables.cs");
            Check(consumables.Contains("if(craftDetectionFor==(recipe.RecipeId,job,worn))return;") && consumables.Contains("if(worn){simulationFood=null;simulationMedicine=null;copy();}"),"once; worn: no double count");
            Check(Read("Craft/CraftDetectionReader.Dalamud.cs").Contains("(nint)PlayerState.MemberFunctionPointers.IsMeisterFlag!=0"),"resolved");
        }),
        ("Crafting list: its own tab, ingredients in a table with icons and the item tooltip",() =>
        {
            Check(Read("MainWindow.CraftSimulation.cs").Contains("if(listsTab) { DrawCraftLists(); return; }"),"tab");
            var lists=Read("MainWindow.CraftLists.cs");
            Check(lists.Contains("ImGui.BeginTable(\"CraftListIngredients\",4,") && lists.Contains("DrawItemIcon(ingredient,new Vector2(24));") && lists.Contains("DrawItemInfoTooltip(itemId);"),"table");
        }),
        ("Live panel: beside the Synthesis window, on by default, reads when its setting or the panel asks, isolated",() =>
        {
            Check(new TenshoAssistant.Configuration().CraftLivePanel && Read("Configuration.cs").Contains("\"CraftLivePanel\""),"setting");
            var ui=Read("MainWindow.CraftProjection.cs");
            Check(ui.Contains("gameGui.GetAddonByName(\"Synthesis\")") && ui.Contains("DrawCraftProjectionBody(state)"),"panel");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("craftLive.SetEnabled(MainWindow.CraftLiveWanted(configuration));") && plugin.Contains("(\"CraftLive\",mainWindow.DrawCraftLivePanel)"),"wired");
        }),
    ];
}
