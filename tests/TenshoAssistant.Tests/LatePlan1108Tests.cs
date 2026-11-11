// 0.1108 (X-129, user capture of 2026-11-11): a hidden item revealed after GP was spent gets a plan from the GP and attempts left then.
using TenshoAssistant.Gathering;

internal static class LatePlan1108Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Late plan: an item read after GP was spent starts from what was left then; the others from the node as it opened",() =>
        {
            Check(GatheringNodes.PlanStart(false,900,6,300,4)==(900,6),"read at the opening: the node as it opened");
            Check(GatheringNodes.PlanStart(true,900,6,300,4)==(300,4),"revealed later: the GP and attempts left then");
            Check(GatheringNodes.PlanStart(true,900,6,300,null)==(300,null),"attempts unread then: unknown, never the opening's");
        }),
        ("Late plan: the panel plans a late item instead of « no plan », keeps each slot's first GP and attempts, and says so",() =>
        {
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("(slot.SuccessPercent,slot.BoonPercent,start.Frozen,slot.DisplayedQuantity,(int)frame.Gp,frame.Attempts)"),"GP and attempts kept at the first read");
            Check(panel.Contains("var (planGp,planAttempts)=GatheringNodes.PlanStart(first.Late,start.Gp,start.Attempts,first.Gp,first.Attempts);"),"the plan's start");
            Check(!panel.Contains("missing.Count==0 && !first.Late"),"a late item is no longer refused");
            Check(panel.Contains("new GpScenario(start.JobId,start.Level,planGp,attempts,"),"solved from what was left");
            Check(panel.Contains("localization.Format(\"GatheringPanel.PlanLate\",plan.Scenario.Budget,plan.Scenario.Attempts)"),"said under the plan");
        }),
    ];
}
