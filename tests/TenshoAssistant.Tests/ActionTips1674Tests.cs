using TenshoAssistant.Craft;

// 0.1674 (X-684, fifth part: actions and statuses, crafting first): one action tooltip (MainWindow.ActionTooltip.cs) for the Crafting
// simulator's palette, the craft log's steps, the recorder, the projection, the Manipulation box and the live crafting effects: icon,
// name, level, CP and durability, the game's description, then what the caller adds.
internal static class ActionTips1674Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Action tips 1674: level and CP, the durability only when the action costs any",()=>
        {
            Check(CraftActionTips.Detail(new CraftActionSpec(CraftActionKind.Manipulation,4574,65,96,0),"Level {0} · {1} CP"," · {0} durability")=="Level 65 · 96 CP","no durability cost: not said");
            Check(CraftActionTips.Detail(new CraftActionSpec(CraftActionKind.Manipulation,100001,1,0,10),"Niveau {0} · {1} PS"," · {0} de solidité")=="Niveau 1 · 0 PS · 10 de solidité","durability cost said");
        }),
        ("Action tips 1674: the crafting tooltips use the one action tooltip",()=>
        {
            var simulation=Read("src/TenshoAssistant/MainWindow.CraftSimulation.cs");
            Check(simulation.Split("DrawCraftActionTooltip(action,after:forbidden ? localization.Get(\"CraftRules.ForbiddenTip\"):null);").Length==3
                  && !simulation.Contains("TipText(action.Description);") && !simulation.Contains("\"SimulationHelp.Action\""),"the simulator's icon and name");
            Check(Read("src/TenshoAssistant/MainWindow.CraftLog.cs").Contains("DrawCraftActionTooltip(metadata,$\"{index+1}. {metadata.Name}\");")
                  && Read("src/TenshoAssistant/MainWindow.CraftLog.cs").Contains("DrawCraftActionTooltip(skill,after:manipulationText);"),"the craft log's steps and Manipulation");
            Check(Read("src/TenshoAssistant/MainWindow.CraftRecorder.cs").Contains("DrawCraftActionTooltip(metadata,$\"{index+1}. {metadata.Name}\");"),"the recorder");
            Check(Read("src/TenshoAssistant/MainWindow.CraftProjection.cs").Contains("DrawCraftActionTooltip(row.Metadata,after:"),"the projection");
            Check(Read("src/TenshoAssistant/MainWindow.CraftLive.cs").Contains("DrawActionTooltip(icon,effect.Metadata.Name,effect.Metadata.Description);"),"the live effects");
        }),
    ];
}
