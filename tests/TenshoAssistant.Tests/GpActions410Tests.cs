using TenshoAssistant.Gathering;
using TenshoAssistant.Craft;

// R-09 (0.410): Solid Reason / Ageless Words (another attempt; Eureka Moment at 50 % from level 90) and Clear Vision / Flora Mastery
// (+15 % on one attempt) in the GP optimizer, only when the scenario asks for them (MoreActions).
internal static class GpActions410Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static GpActionCatalog Catalog()=>new(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,$"Action {spec.Id}",spec.Id,"fixture")),"fixture");

    // Attempt by attempt: every placement of Clear Vision and Bountiful over the attempts (no boon, level below 30: no yield or gift).
    private static decimal Oracle(GpScenario input,GpActionCatalog catalog)
    {
        var available=catalog.Actions.Where(action=>action.Spec.JobId==input.JobId && action.Spec.Level<=input.Level).ToArray();
        var chances=available.Where(action=>action.Spec.Effect==GpEffect.Success).Select(action=>(action.Spec.Cost,action.Spec.Magnitude)).Prepend((0,0)).ToArray();
        var restore=available.Single(action=>action.Spec.Effect==GpEffect.Attempt).Spec.Cost;
        var clear=available.Single(action=>action.Spec.Effect==GpEffect.NextSuccess).Spec.Cost;
        var bounty=available.Single(action=>action.Spec.Effect==GpEffect.Bountiful1).Spec.Cost;
        decimal best=0;
        foreach(var (chanceCost,chance) in chances)
        for(var restored=0;restored<=input.Attempts;restored++)
        {
            var attempts=input.Attempts+restored;
            for(var clearMask=0;clearMask<1<<attempts;clearMask++)
            for(var bountyMask=0;bountyMask<1<<attempts;bountyMask++)
            {
                var cost=chanceCost+restored*restore+System.Numerics.BitOperations.PopCount((uint)clearMask)*clear+System.Numerics.BitOperations.PopCount((uint)bountyMask)*bounty;
                if(cost>input.Budget)continue;
                decimal expected=0;
                for(var attempt=0;attempt<attempts;attempt++)
                {
                    var success=Math.Min(100,input.SuccessPercent+(input.SuccessPercent>0 ? chance:0)+((clearMask>>attempt&1)!=0 && input.SuccessPercent>0 ? 15:0))/100m;
                    expected+=success*(input.BaseYield+((bountyMask>>attempt&1)!=0 ? 1:0));
                }
                best=Math.Max(best,expected);
            }
        }
        return best;
    }

    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static GpActionCatalog CatalogRaw()=>new(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,$"Action {spec.Id}",0,"Text")),"fixture");
    private static GpOptimization Node(int budget,string forbidden="",int attempts=4,int success=100)=>GpOptimizer.Solve(new(16,90,budget,attempts,1,success,0,1,MoreActions:true,Forbidden:forbidden),CatalogRaw());
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("GP actions 0.410: Solid Reason and Clear Vision placed as well as an attempt-by-attempt search",() =>
        {
            var catalog=Catalog();
            foreach(var job in new uint[]{16,17})
            foreach(var attempts in new[]{1,2,3})
            foreach(var success in new[]{0,40,85,100})
            foreach(var budget in new[]{0,150,300,450,700})
            {
                var input=new GpScenario(job,27,budget,attempts,1,success,0,1,MoreActions:true);
                var actual=GpOptimizer.Solve(input,catalog).Best.ExpectedYield;var expected=Oracle(input,catalog);
                Check(actual==expected,$"job {job} attempts {attempts} success {success} budget {budget}: {actual} instead of {expected}");
            }
        }),
        ("GP actions 0.410: Eureka Moment from level 90 counted on average; off, the model before 0.410",() =>
        {
            var catalog=Catalog();
            var eureka=GpOptimizer.Solve(new GpScenario(16,90,300,1,1,100,0,1,MoreActions:true),catalog);
            Check(eureka.Best.ExpectedYield==2.5m && eureka.Best.Actions.Single().Action.Spec.Id==232,"Solid Reason with Eureka: 1 + 1 + 0.5");
            var before=GpOptimizer.Solve(new GpScenario(16,89,300,1,1,100,0,1,MoreActions:true),catalog);
            Check(before.Best.ExpectedYield==2m,"no Eureka below level 90");
            var old=GpOptimizer.Solve(new GpScenario(17,100,700,4,1,90,20,3),catalog);
            Check(old.Best.Actions.All(use=>use.Action.Spec.Effect is not (GpEffect.Attempt or GpEffect.NextSuccess)) && old.Frontier.All(plan=>plan.Actions.All(use=>use.Action.Spec.Effect is not (GpEffect.Attempt or GpEffect.NextSuccess))),
                "MoreActions off: no new action");
            var more=GpOptimizer.Solve(new GpScenario(17,100,700,4,1,90,20,3,MoreActions:true),catalog);
            Check(more.Best.ExpectedYield>=old.Best.ExpectedYield,"more actions never worse");
        }),
        ("GP actions 0.410: the panel and the GP tool use them; Eureka said",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.GatheringPanel.cs"));
            var tool=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.GatheringGp.cs"));
            Check(panel.Contains("MoreActions:true") && panel.Contains("GpUi.Eureka"),"panel");
            Check(tool.Contains("gpMoreActions=true") && tool.Contains("Tip(\"GpUi.MoreActionsTip\")") && tool.Contains("GpUi.Eureka"),"tool");
        }),
        ("GP actions can be forbidden, on both jobs, like macro actions",() =>
        {
            var free=Node(1000);
            Check(free.Best.Actions.Any(use=>use.Action.Spec.Effect==GpEffect.Yield),"the yield action is used when free");
            var key=GpActionRules.Joined(GpActionCatalog.Specs.Where(spec=>spec.Effect==GpEffect.Yield).Select(GpActionRules.Key));
            var without=Node(1000,key);
            Check(without.Best.Actions.All(use=>use.Action.Spec.Effect!=GpEffect.Yield) && without.Frontier.All(plan=>plan.Actions.All(use=>use.Action.Spec.Effect!=GpEffect.Yield)),"never planned");
            var botanist=GpOptimizer.Solve(new(17,90,1000,4,1,100,0,1,MoreActions:true,Forbidden:key),CatalogRaw());
            Check(botanist.Best.Actions.All(use=>use.Action.Spec.Effect!=GpEffect.Yield),"the botanist's twin too");
            Check(GpActionRules.Joined(["b","a","a",""])=="a|b" && GpActionRules.Joined(null)=="","joined");
            Check(GpActionRules.Groups(CatalogRaw()).All(group=>group.Miner is not null && group.Botanist is not null),"each action has its twin");
            Check(Read("MainWindow.Settings.cs").Contains("DrawGpPlanSettings();") && Read("MainWindow.GatheringGp.cs").Contains("Forbidden:GpForbidden"),"in the settings and the GP page");
        }),
    ];
}
