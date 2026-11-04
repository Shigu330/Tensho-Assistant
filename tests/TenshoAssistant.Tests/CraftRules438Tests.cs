using TenshoAssistant.Craft;

// 0.438 (user request): forbidden crafting actions, applied wherever actions are proposed; « Equip (job) » in the crafting log panel.
internal static class CraftRules438Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Forbidden actions: a value key, unknown names dropped; the search never uses them",() =>
        {
            var key=CraftActionRules.Key(["TrainedEye","Manipulation","Nothing","Manipulation"]);
            Check(key=="Manipulation,TrainedEye" && CraftActionRules.Key(null)=="","sorted, distinct, valid only");
            Check(CraftActionRules.Forbidden(key,CraftActionKind.TrainedEye) && !CraftActionRules.Forbidden(key,CraftActionKind.BasicTouch) && !CraftActionRules.Forbidden("",CraftActionKind.TrainedEye),"lookup");
            Check(CraftActionRules.UsesForbidden([CraftActionKind.BasicTouch,CraftActionKind.TrainedEye],key) && !CraftActionRules.UsesForbidden([CraftActionKind.BasicTouch],key),"sequence");
            var recipe=new RecipeDefinition(1,2,1,"Fixture",5,[],0,new(40,200,500,100,100,100,100,0,0,0));
            var stats=new CraftStats(100,1000,1000,400,true);
            var free=CraftSolver.Solve(recipe,stats,0,new(500,6,256,50000));
            var forbidden=CraftActionRules.Key(free.Simulation!.Steps.Select(step=>step.Action.ToString()));
            var constrained=CraftSolver.Solve(recipe,stats,0,new(500,6,256,50000,Forbidden:forbidden));
            Check(constrained.Simulation is null || !constrained.Simulation.Steps.Any(step=>CraftActionRules.Forbidden(forbidden,step.Action)),"search avoids every forbidden action");
        }),
        ("Forbidden actions reach every interface; crafting log panel offers the job's gear set",() =>
        {
            Check(Read("Craft/CraftLogSession.cs").Contains("CraftActionRules.UsesForbidden(preset.Actions,input.Options.Forbidden))continue;"),"saved sequences of the assistant");
            Check(Read("MainWindow.CraftLog.cs").Contains("craftMacroBuffWait,craftForbidden),") && Read("MainWindow.CraftSolver.cs").Contains("key.BuffExtraWait,key.Forbidden,key.Raphael));"),"assistant and search");
            Check(Read("MainWindow.CraftSimulation.cs").Contains("ImGui.BeginDisabled(forbidden ||") && Read("MainWindow.CraftSimulation.cs").Contains("CraftRules.SequenceUsesForbidden"),"manual simulator");
            Check(Read("MainWindow.CraftProjection.cs").Contains("Where(row=>!CraftActionRules.Forbidden(forbiddenKey,row.Metadata.Spec.Kind))"),"live synthesis");
            Check(Read("MainWindow.CraftLog.cs").Contains("if(wrongJob)DrawEquipRecipeJob(context.Recipe!,\"CraftLogEquip\");"),"crafting log panel: equip the job");
        }),
    ];
}
