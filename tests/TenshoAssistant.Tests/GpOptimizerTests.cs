using TenshoAssistant.Character;
using TenshoAssistant.Gathering;

internal static class GpOptimizerTests
{
    private static GpActionCatalog Catalog()=>new(GpActionCatalog.Specs.Select(spec=>new GpAction(spec,$"Action {spec.Id}",spec.Id,"Native description fixture")),"fixture");
    private static GpScenario Scenario(int budget=600,int level=100)=>new(16,level,budget,4,1,100,15,3);
    private static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    private static void Reject(Action action) { try { action(); } catch(ArgumentException) { return; } throw new Exception("Invalid GP input accepted."); }
    // Independent oracle: enumerate action subsets, then weight the two successful harvest outcomes.
    private static (int Cost,decimal Expected) Oracle(GpScenario input,GpActionCatalog catalog)
    {
        var candidates=catalog.Actions.Where(action=>action.Spec.JobId==input.JobId && action.Spec.Level<=input.Level
            && (action.Spec.Effect!=GpEffect.Bountiful1 || input.Level<68)).ToArray();
        var best=(Cost:0,Expected:-1m);
        for(var bits=0;bits<(1<<candidates.Length);bits++)
        {
            var selected=candidates.Where((_,index)=>(bits&(1<<index))!=0).ToArray();
            if(selected.Count(action=>action.Spec.Effect==GpEffect.Success)>1 || selected.Count(action=>action.Spec.Effect==GpEffect.Yield)>1) continue;
            if(input.SuccessPercent==0 && selected.Any(action=>action.Spec.Effect==GpEffect.Success)) continue;
            if(input.BoonPercent==0 && selected.Any(action=>action.Spec.Effect is GpEffect.BoonChance1 or GpEffect.BoonChance2)) continue;
            var once=selected.Where(action=>action.Spec.Effect is not (GpEffect.Bountiful1 or GpEffect.Bountiful2)).ToArray();
            var bounty=selected.FirstOrDefault(action=>action.Spec.Effect is GpEffect.Bountiful1 or GpEffect.Bountiful2);
            var success=Math.Min(100,input.SuccessPercent+once.Where(action=>action.Spec.Effect==GpEffect.Success).Sum(action=>action.Spec.Magnitude));
            var boon=Math.Min(100,input.BoonPercent+once.Where(action=>action.Spec.Effect is GpEffect.BoonChance1 or GpEffect.BoonChance2).Sum(action=>action.Spec.Magnitude));
            var normalAmount=input.BaseYield+once.Where(action=>action.Spec.Effect==GpEffect.Yield).Sum(action=>action.Spec.Magnitude);
            var boonAmount=normalAmount+1+once.Where(action=>action.Spec.Effect==GpEffect.BoonYield).Sum(action=>action.Spec.Magnitude);
            for(var count=bounty is null ? 0:1;count<=(bounty is null ? 0:input.Attempts);count++)
            {
                var cost=once.Sum(action=>action.Spec.Cost)+(bounty?.Spec.Cost ?? 0)*count;
                if(cost>input.Budget) continue;
                var yieldNumerator=input.Attempts*(success*(100-boon)*normalAmount+success*boon*boonAmount);
                yieldNumerator+=count*success*100*(bounty?.Spec.Effect==GpEffect.Bountiful2 ? input.BountifulBonus:1);
                var expected=yieldNumerator/10000m;
                if(expected>best.Expected || expected==best.Expected && cost<best.Cost) best=(cost,expected);
            }
        }
        return best;
    }
    public static IEnumerable<(string Name,Action Run)> Cases=>
    [
        ("GP hand example stacks both boon chances and repeated abundance at minimum optimal cost",()=>
        {
            var result=GpOptimizer.Solve(Scenario(),Catalog());
            Check(result.Baseline==4.6m && result.Best.ExpectedYield==18.2m && result.Best.Cost==550,"independent hand expectation and cost");
            Check(result.Best.Actions.Count==3 && result.Best.Actions.Single(use=>use.Action.Spec.Effect==GpEffect.Bountiful2).Count==4,"abundance is repeated before four attempts");
            Check(result.Best.Actions.Any(use=>use.Action.Spec.Effect==GpEffect.BoonChance1) && result.Best.Actions.Any(use=>use.Action.Spec.Effect==GpEffect.BoonChance2),"two boon buffs stack");
            Check(result.NextUpgrade?.Cost==700 && result.NextUpgrade.ExpectedYield==19.6m,"next useful budget beyond current funds");
        }),
        ("GP exhaustive model agrees with an independent subset and outcome oracle",()=>
        {
            var random=new Random(20261028); var catalog=Catalog();
            for(var sample=0;sample<64;sample++)
            {
                var input=new GpScenario(sample%2==0 ? 16u:17u,random.Next(1,101),random.Next(0,1601),random.Next(1,9),random.Next(1,5),random.Next(0,101),random.Next(0,101),random.Next(1,4));
                var actual=GpOptimizer.Solve(input,catalog); var expected=Oracle(input,catalog);
                Check(actual.Best.Cost==expected.Cost && actual.Best.ExpectedYield==expected.Expected,$"oracle mismatch for {input}");
                Check(actual.Best.Cost==actual.Best.Actions.Sum(use=>use.Count*use.Action.Spec.Cost) && actual.Best.Cost<=input.Budget,"no unsupported GP borrowing");
            }
        }),
        ("GP mutually exclusive tiers and level upgrades preserve miner and botanist identities",()=>
        {
            var catalog=Catalog();
            foreach(var job in new uint[]{16,17})
            {
                var old=GpOptimizer.Solve(new(job,67,100,1,1,100,0,3),catalog);
                var newer=GpOptimizer.Solve(new(job,68,100,1,1,100,0,3),catalog);
                Check(old.Best.ExpectedYield==2 && newer.Best.ExpectedYield==4,"abundance I fixed +1, II scenario bonus +3");
                Check(old.Best.Actions.Single().Action.Spec.Effect==GpEffect.Bountiful1 && newer.Best.Actions.Single().Action.Spec.Effect==GpEffect.Bountiful2,"upgraded ability chosen at correct level");
                var full=GpOptimizer.Solve(Scenario(10000) with {JobId=job},catalog);
                Check(full.Best.Actions.All(use=>use.Action.Spec.JobId==job),"no cross-job ability");
                Check(full.Best.Actions.Count(use=>use.Action.Spec.Effect==GpEffect.Yield)<=1 && full.Best.Actions.Count(use=>use.Action.Spec.Effect==GpEffect.Success)<=1,"exclusive tiers never combined");
            }
        }),
        ("GP zero chances caps and free baseline never create impossible bonuses or wasted spending",()=>
        {
            var catalog=Catalog();
            var impossible=GpOptimizer.Solve(Scenario(10000) with {SuccessPercent=0},catalog);
            Check(impossible.Best.Cost==0 && impossible.Best.ExpectedYield==0 && impossible.NextUpgrade is null,"zero success cannot be raised by these actions");
            var capped=GpOptimizer.Solve(Scenario(10000) with {BoonPercent=100},catalog);
            Check(capped.Best.Actions.All(use=>use.Action.Spec.Effect is not (GpEffect.Success or GpEffect.BoonChance1 or GpEffect.BoonChance2)),"already capped percentages need no buff");
            var zero=GpOptimizer.Solve(Scenario(0),catalog);
            Check(zero.Best.Cost==0 && zero.Best.Actions.Count==0 && zero.Best.ExpectedYield==zero.Baseline,"zero budget is observed zero, not unknown");
            var noBoon=GpOptimizer.Solve(Scenario(500) with {BoonPercent=0},catalog);
            Check(noBoon.Best.Actions.All(use=>use.Action.Spec.Effect is not (GpEffect.BoonChance1 or GpEffect.BoonChance2 or GpEffect.BoonYield)),"boon-only actions do not fabricate an initial boon chance");
        }),
        ("GP frontier is strictly improving and preserves immutable recommendations",()=>
        {
            var result=GpOptimizer.Solve(Scenario(),Catalog());
            Check(result.Frontier.Zip(result.Frontier.Skip(1)).All(pair=>pair.First.Cost<pair.Second.Cost && pair.First.ExpectedYield<pair.Second.ExpectedYield),"Pareto thresholds strict in cost and expectation");
            Check(result.Frontier.Last(plan=>plan.Cost<=600)==result.Best,"best is current attainable threshold");
            try { ((IList<GpPlan>)result.Frontier).Clear(); throw new Exception("Mutable frontier"); } catch(NotSupportedException) { }
            try { ((IList<GpActionUse>)result.Best.Actions).Clear(); throw new Exception("Mutable action plan"); } catch(NotSupportedException) { }
        }),
        ("GP catalog fails closed for missing duplicate or changed semantic bindings",()=>
        {
            var source=Catalog().Actions.ToList(); var catalog=new GpActionCatalog(source,"test"); source.Clear();
            Check(catalog.Actions.Count==20,"producer collection detached");
            Reject(()=>new GpActionCatalog(catalog.Actions.Skip(1),"missing"));
            Reject(()=>new GpActionCatalog(catalog.Actions.Skip(1).Append(catalog.Actions.Last()),"duplicate"));
            Reject(()=>new GpActionCatalog(catalog.Actions.Select((action,index)=>index==0 ? action with {Spec=action.Spec with {Cost=1}}:action),"changed cost"));
            var service=new GpCatalogService(); service.Publish(catalog); service.Fail(new Exception("incompatible"));
            Check(service.State.Catalog is null && service.State.Error=="incompatible","invalid action catalog does not leave a partial optimum");
        }),
        ("GP invalid ranges and cancellation never publish a misleading result",()=>
        {
            var catalog=Catalog();
            foreach(var bad in new[]{Scenario() with {JobId=18},Scenario() with {Budget=-1},Scenario() with {Attempts=0},Scenario() with {Attempts=13},Scenario() with {SuccessPercent=101},Scenario() with {BoonPercent=-1},Scenario() with {BountifulBonus=4},Scenario() with {BaseYield=0}})
                Reject(()=>GpOptimizer.Solve(bad,catalog));
            try { GpOptimizer.Solve(Scenario(),catalog,new CancellationToken(true)); throw new Exception("Cancellation ignored"); } catch(OperationCanceledException) { }
        }),
        ("Observed GP remains distinct from the character GP attribute and clears on owner invalidation",()=>
        {
            var stats=new CharacterStatsService();
            stats.Publish(42,"Miner",16,"Mineur",100,new Dictionary<string,int>{{"GatheringPoints",1100}},0,1100);
            Check(stats.State.CurrentGp==0 && stats.State.MaximumGp==1100 && stats.State.Values["GatheringPoints"]==1100,"current zero and maximum retained separately");
            stats.Clear(); Check(stats.State.CurrentGp is null && stats.State.MaximumGp is null && stats.State.CharacterId==0,"no foreign resource evidence after clear");
            Reject(()=>stats.Publish(42,"",16,"",100,new Dictionary<string,int>(),500,400));
            Reject(()=>stats.Publish(42,"",16,"",100,new Dictionary<string,int>(),500,null));
        })
    ];
}
