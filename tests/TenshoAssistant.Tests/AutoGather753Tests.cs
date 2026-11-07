using TenshoAssistant.Gathering;

// 0.753 (X-06, user request): the plan when the game gathers automatically.
internal static class AutoGather753Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static readonly GpActionCatalog Catalog=new(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,"a"+spec.Id,1,"d")).ToArray(),"test");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Auto gather: one use at most of an action for one gather, no attempt given back, lasting effects kept",() =>
        {
            var hand=new GpScenario(17,100,823,4,1,100,60,3,MoreActions:true);
            var auto=hand with {AutoGather=true};
            var byHand=GpOptimizer.Solve(hand,Catalog).Best;
            var alone=GpOptimizer.Solve(auto,Catalog).Best;
            Check(byHand.Actions.Any(use=>use.Action.Spec.Effect is GpEffect.Bountiful2 && use.Count>1),"by hand, the bonus on several gathers");
            Check(alone.Actions.All(use=>use.Action.Spec.Effect is not (GpEffect.Bountiful1 or GpEffect.Bountiful2 or GpEffect.NextSuccess) || use.Count<=1),"one use at most");
            Check(alone.Actions.All(use=>use.Action.Spec.Effect!=GpEffect.Attempt),"no attempt given back");
            Check(alone.ExpectedYield<=byHand.ExpectedYield && alone.Cost<=823,"never better than by hand, within the GP");
        }),
        ("Auto gather: read from the game's box, the plan says so, no end-of-node replan",() =>
        {
            Check(Read("Gathering/GatheringObservationReader.Dalamud.cs").Contains("quick->IsChecked ? 1:0"),"read");
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("AutoGather:auto") && panel.Contains("GatheringPanel.AutoGatherPlan") && panel.Contains("!optimization.Scenario.AutoGather"),"used");
        }),
    ];
}
