using TenshoAssistant.Gathering;

// 0.1675 (X-684, sixth part: gathering actions): the one action tooltip of 0.1674 (MainWindow.ActionTooltip.cs) for the gathering panel's
// next action and plan, the crystal plan, the GP plan and the GP settings: icon, name, level and GP, the game's description.
internal static class GatheringActionTips1675Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gathering action tips 1675: level and GP; every gathering action tooltip uses the one action tooltip",()=>
        {
            Check(GpActionTips.Detail(25,300,"Level {0} · {1} GP")=="Level 25 · 300 GP" && GpActionTips.Detail(1,0,"Niveau {0} · {1} PR")=="Niveau 1 · 0 PR","detail line");
            var panel=Read("src/TenshoAssistant/MainWindow.GatheringPanel.cs");
            Check(panel.Contains("DrawGatheringActionTooltip(action);") && panel.Contains("DrawGatheringActionTooltip(use.Action);")
                  && panel.Contains("DrawGatheringActionTooltip(row.Icon,row.Name,row.Description,action.Level,action.Cost);") && panel.Contains("DrawActionTooltip(row.Icon,row.Name,row.Description,after:"),"the panel: next action, plan, crystals, crystals recasting");
            var gp=Read("src/TenshoAssistant/MainWindow.GatheringGp.cs");
            Check(gp.Contains("DrawGatheringActionTooltip(use.Action);") && gp.Contains("DrawGatheringActionTooltip(metadata);"),"the GP plan");
            Check(Read("src/TenshoAssistant/MainWindow.GatheringPlanSettings.cs").Contains("DrawGatheringActionTooltip(action,"),"the GP settings");
            foreach(var file in new[]{"GatheringPanel","GatheringGp","GatheringPlanSettings"})
                Check(!Read("src/TenshoAssistant/MainWindow."+file+".cs").Contains("WrappedTooltip(action.Description)") && !Read("src/TenshoAssistant/MainWindow."+file+".cs").Contains("WrappedTooltip(use.Action.Description)")
                      && !Read("src/TenshoAssistant/MainWindow."+file+".cs").Contains("WrappedTooltip(row.Description)") && !Read("src/TenshoAssistant/MainWindow."+file+".cs").Contains("WrappedTooltip(metadata.Description)"),file+": no description alone left");
        }),
    ];
}
