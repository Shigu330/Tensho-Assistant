using TenshoAssistant.Gathering;

internal static class GpActiveEffectsTests
{
    private static GpActionCatalog Catalog()=>new(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,$"Action{spec.Id}",spec.Id,"fixture")),"fixture");
    private static GpScenario Scenario(GpActiveEffects? active=null)=>new(16,100,600,4,3,100,100,3,active);
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(GpScenario scenario)
    {var rejected=false;try{GpOptimizer.Solve(scenario,Catalog());}catch(ArgumentException){rejected=true;}Check(rejected,"inconsistent active scenario accepted");}
    // Enumerate subsets independently, then sum success/failure and boon/no-boon outcomes per attempt.
    private static (int Cost,decimal Expected) Oracle(GpScenario scenario,GpActionCatalog catalog)
    {
        var candidates=catalog.Actions.Where(action=>action.Spec.JobId==scenario.JobId && action.Spec.Level<=scenario.Level
            && (action.Spec.Effect!=GpEffect.Bountiful1 || scenario.Level<68)).ToArray();
        var active=scenario.Effects;var best=(Cost:0,Expected:-1m);
        for(var mask=0;mask<(1<<candidates.Length);mask++)
        {
            var selected=candidates.Where((_,index)=>(mask&(1<<index))!=0).ToArray();
            bool Contains(GpEffect effect)=>selected.Any(action=>action.Spec.Effect==effect);
            if(selected.Count(action=>action.Spec.Effect==GpEffect.Success)>1 || selected.Count(action=>action.Spec.Effect==GpEffect.Yield)>1)continue;
            if(active.Yield && Contains(GpEffect.Yield) || active.Gift1 && Contains(GpEffect.BoonChance1) || active.Gift2 && Contains(GpEffect.BoonChance2) || active.Tidings && Contains(GpEffect.BoonYield))continue;
            if(scenario.SuccessPercent==0 && Contains(GpEffect.Success) || scenario.BoonPercent==0 && (Contains(GpEffect.BoonChance1) || Contains(GpEffect.BoonChance2)))continue;
            var bounty=selected.SingleOrDefault(action=>action.Spec.Effect is GpEffect.Bountiful1 or GpEffect.Bountiful2);
            var prefix=selected.Where(action=>action!=bounty).ToArray();
            var firstPurchasable=active.FirstAttemptBonus>0 ? 1:0;
            var success=Math.Min(100,scenario.SuccessPercent+prefix.Where(action=>action.Spec.Effect==GpEffect.Success).Sum(action=>action.Spec.Magnitude));
            var boon=Math.Min(100,scenario.BoonPercent+prefix.Where(action=>action.Spec.Effect is GpEffect.BoonChance1 or GpEffect.BoonChance2).Sum(action=>action.Spec.Magnitude));
            for(var count=bounty is null ? 0:1;count<=(bounty is null ? 0:scenario.Attempts-firstPurchasable);count++)
            {
                var cost=prefix.Sum(action=>action.Spec.Cost)+(bounty?.Spec.Cost ?? 0)*count;if(cost>scenario.Budget)continue;
                decimal expected=0;
                for(var attempt=0;attempt<scenario.Attempts;attempt++)
                {
                    var ordinary=scenario.BaseYield+prefix.Where(action=>action.Spec.Effect==GpEffect.Yield).Sum(action=>action.Spec.Magnitude);
                    if(attempt==0)ordinary+=active.FirstAttemptBonus;
                    if(bounty is not null && attempt>=firstPurchasable && attempt<firstPurchasable+count)ordinary+=bounty.Spec.Effect==GpEffect.Bountiful1 ? 1:scenario.BountifulBonus;
                    var extra=(active.Tidings ? 2:1)+prefix.Where(action=>action.Spec.Effect==GpEffect.BoonYield).Sum(action=>action.Spec.Magnitude);
                    expected+=(success*(100-boon)*ordinary+success*boon*(ordinary+extra))/10000m;
                }
                if(expected>best.Expected || expected==best.Expected && cost<best.Cost)best=(cost,expected);
            }
        }
        return best;
    }
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("GP paid effects contribute to baseline once and cost no new GP",()=>
        {
            var input=Scenario(new(Yield:true,Gift1:true,Tidings:true,FirstAttemptBonus:2)) with {Budget=0,SuccessPercent=50,BoonPercent=25};
            var result=GpOptimizer.Solve(input,Catalog());
            Check(result.Baseline==8m && result.Best.ExpectedYield==8m && result.Best.Cost==0 && result.Best.Actions.Count==0,"0.5 * (4*(3+0.25*2)+2)");
        }),
        ("GP limited paid bonus cannot be rebought on the same only attempt",()=>
        {
            var input=Scenario(new(true,true,true,true,3)) with {Attempts=1,Budget=10000};
            var result=GpOptimizer.Solve(input,Catalog());
            Check(result.Baseline==8 && result.Best.ExpectedYield==8 && result.Best.Cost==0,"3 ordinary +2 boon +3 one-off; all paid families already active");
        }),
        ("GP new abundance starts after the already paid first attempt",()=>
        {
            var input=Scenario(new(true,true,true,true,3)) with {Budget=300};
            var result=GpOptimizer.Solve(input,Catalog());var use=result.Best.Actions.Single();
            Check(result.Baseline==23 && result.Best.ExpectedYield==32 && result.Best.Cost==300,"4*(3+2)+3 then three new bonuses of3");
            Check(use.Action.Spec.Effect==GpEffect.Bountiful2 && use.Count==3,"no fourth purchase or recast of active family");
        }),
        ("GP paid first bonus and future bonus can be different values",()=>
        {
            var input=Scenario(new(Yield:true,FirstAttemptBonus:1)) with {Budget=300,BoonPercent=0,BaseYield=2};
            var result=GpOptimizer.Solve(input,Catalog());
            Check(result.Baseline==9 && result.Best.ExpectedYield==18 && result.Best.Actions.Single().Count==3,"one paid +1 then three future +3 bonuses");
        }),
        ("GP all active-family combinations match independent per-attempt outcome oracle",()=>
        {
            var random=new Random(20261029);var catalog=Catalog();
            for(var sample=0;sample<128;sample++)
            {
                var bits=sample%16;var active=new GpActiveEffects((bits&1)!=0,(bits&2)!=0,(bits&4)!=0,(bits&8)!=0,sample/16%4);
                var minimumBoon=1+(active.Gift1 ? 10:0)+(active.Gift2 ? 30:0);
                var input=new GpScenario(sample/64==0 ? 16u:17u,100,random.Next(0,1001),random.Next(1,7),random.Next(active.Yield ? 2:1,5),random.Next(0,101),sample%11==0 ? 0:random.Next(minimumBoon,101),random.Next(1,4),active);
                var result=GpOptimizer.Solve(input,catalog);var oracle=Oracle(input,catalog);
                Check(result.Best.Cost==oracle.Cost && result.Best.ExpectedYield==oracle.Expected,$"oracle sample {sample}");
                Check(result.Best.Cost==result.Best.Actions.Sum(use=>use.Count*use.Action.Spec.Cost),"past costs not recharged");
                Check(result.Frontier.Zip(result.Frontier.Skip(1)).All(pair=>pair.First.Cost<pair.Second.Cost && pair.First.ExpectedYield<pair.Second.ExpectedYield),"strict frontier");
                foreach(var threshold in result.Frontier.Take(3))
                {
                    var expected=Oracle(input with {Budget=threshold.Cost},catalog);
                    Check(threshold.Cost==expected.Cost && threshold.ExpectedYield==expected.Expected,"threshold itself feasible and efficient");
                }
            }
        }),
        ("GP explicit empty effects preserve the existing model and immutable result",()=>
        {
            var input=new GpScenario(16,100,600,4,1,100,15,3);var old=GpOptimizer.Solve(input,Catalog());var explicitEmpty=GpOptimizer.Solve(input with {Active=new()},Catalog());
            Check(old.Best.Cost==explicitEmpty.Best.Cost && old.Best.ExpectedYield==explicitEmpty.Best.ExpectedYield && old.Baseline==explicitEmpty.Baseline && old.NextUpgrade?.Cost==explicitEmpty.NextUpgrade?.Cost,"legacy unchanged");
            var initial=new GpActiveEffects(Yield:true);var copy=GpOptimizer.Solve(Scenario(initial),Catalog());initial=initial with {Other=true};
            Check(copy.Scenario.Effects.Yield && !copy.Scenario.Effects.Other,"immutable declared effects");
        }),
        ("GP active effects require compatible levels amounts and complete modeled influences",()=>
        {
            foreach(var effects in new[]{new GpActiveEffects(FirstAttemptBonus:-1),new(FirstAttemptBonus:4),new(Other:true)})Reject(Scenario(effects));
            Reject(Scenario(new(Yield:true)) with {BaseYield=1});
            foreach(var (level,effects) in new[]{(29,new GpActiveEffects(Yield:true)),(14,new(Gift1:true)),(49,new(Gift2:true)),(80,new(Tidings:true)),(23,new(FirstAttemptBonus:1)),(67,new(FirstAttemptBonus:2))})
                Reject(Scenario(effects) with {Level=level});
            foreach(var (boon,effects) in new[]{(10,new GpActiveEffects(Gift1:true)),(30,new(Gift2:true)),(40,new(Gift1:true,Gift2:true))})
                Reject(Scenario(effects) with {BoonPercent=boon});
            var valid=GpOptimizer.Solve(Scenario(new(Gift1:true,Gift2:true,Tidings:true)) with {BoonPercent=0},Catalog());
            Check(valid.Baseline==12,"zero boon row remains zero despite actor-wide declared gifts");
        })
    };
}
