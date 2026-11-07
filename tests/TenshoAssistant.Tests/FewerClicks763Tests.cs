using TenshoAssistant.Gathering;

// 0.763 (X-16, user request): the same result with fewer clicks.
internal static class FewerClicks763Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static GpAction Act(uint id,int cost,GpEffect effect)=>new(new(id,17,50,cost,effect,0),"a"+id,0,"");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fewer clicks: same yield, the plan with fewer uses wins, even if dearer; a better yield still wins; over the GP never",() =>
        {
            var bountiful=Act(273,100,GpEffect.Bountiful2);var yield=Act(222,400,GpEffect.Yield);var gift=Act(21178,50,GpEffect.BoonChance1);
            GpPlan perGather=new(200,18m,[new(bountiful,2)]);GpPlan lasting=new(400,18m,[new(yield,1)]);GpPlan better=new(450,18.5m,[new(yield,1),new(gift,1)]);
            Check(GpOptimizer.Pick([perGather,lasting],500)==lasting,"one click rather than two");
            Check(GpOptimizer.Pick([perGather,lasting,better],500)==better,"a better result first");
            Check(GpOptimizer.Pick([perGather,lasting],300)==perGather,"what the GP allows");
            Check(GpOptimizer.Pick([new(100,10m,[new(gift,2)]),new(50,10m,[new(gift,2)])],500).Cost==50,"same clicks: the cheapest");
            Check(GpOptimizer.Pick([new(0,8m,[]),perGather],50).Actions.Count==0,"nothing to buy");
        }),
        ("Fewer clicks: the real optimizer keeps its yield, never fewer GP than the old choice would allow",() =>
        {
            var catalog=new GpActionCatalog(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,"a"+spec.Id,1,"d")).ToArray(),"test");
            var scenario=new GpScenario(17,100,823,4,1,100,60,3,MoreActions:true);
            var solved=GpOptimizer.Solve(scenario,catalog);
            Check(solved.Best.Cost<=823 && solved.Frontier.Where(plan=>plan.Cost<=823).Max(plan=>plan.ExpectedYield)-solved.Best.ExpectedYield<=GpOptimizer.YieldTie,"same best yield");
        }),
    ];
}
