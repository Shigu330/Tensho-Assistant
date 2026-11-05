namespace TenshoAssistant.Gathering;

// Current persistent values include declared paid effects; BaseYield excludes boon and the limited first-attempt bonus.
public sealed record GpActiveEffects(bool Yield=false,bool Gift1=false,bool Gift2=false,bool Tidings=false,int FirstAttemptBonus=0,bool Other=false)
{
    public static GpActiveEffects None {get;}=new();
}
// R-09 (0.410): MoreActions adds Solid Reason / Ageless Words (one more attempt each; from level 90, trait « Enhanced Solid Reason »:
// 50 % chance of Eureka Moment, i.e. a free attempt with Wise to the World, counted on average) and Clear Vision / Flora Mastery (+15 %
// on one attempt, placed on the Bountiful attempts first). Off, the model is the one before 0.410.
public sealed record GpScenario(uint JobId,int Level,int Budget,int Attempts,int BaseYield,int SuccessPercent,int BoonPercent,int BountifulBonus,GpActiveEffects? Active=null,bool MoreActions=false,string Forbidden="")
{
    public const int EurekaLevel=90;
    public const decimal EurekaChance=0.5m;
    public GpActiveEffects Effects=>Active ?? GpActiveEffects.None;
}
public sealed record GpActionUse(GpAction Action,int Count);
public sealed record GpPlan(int Cost,decimal ExpectedYield,IReadOnlyList<GpActionUse> Actions);
public sealed record GpOptimization(GpScenario Scenario,decimal Baseline,GpPlan Best,GpPlan? NextUpgrade,IReadOnlyList<GpPlan> Frontier);

public static class GpOptimizer
{
    public static GpOptimization Solve(GpScenario input,GpActionCatalog catalog,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(catalog);
        if(input.JobId is not (16 or 17) || input.Level is <1 or >100 || input.Budget is <0 or >10000 || input.Attempts is <1 or >12
            || input.BaseYield is <1 or >20 || input.SuccessPercent is <0 or >100 || input.BoonPercent is <0 or >100 || input.BountifulBonus is <1 or >3)
            throw new ArgumentOutOfRangeException(nameof(input));
        var active=input.Effects;
        if(active.Other)throw new ArgumentException("An effect outside the model was declared: no optimum available for this scenario.",nameof(input));
        if(active.FirstAttemptBonus is <0 or >3 || active.FirstAttemptBonus>0 && input.Level<24 || active.FirstAttemptBonus>1 && input.Level<68
            || active.Yield && (input.Level<30 || input.BaseYield<2) || active.Gift1 && input.Level<15 || active.Gift2 && input.Level<50 || active.Tidings && input.Level<81)
            throw new ArgumentException("Declared effect incompatible with the level, the yield or the bonus range.",nameof(input));
        var minimumBoon=1+(active.Gift1 ? 10:0)+(active.Gift2 ? 30:0);
        if(input.BoonPercent>0 && input.BoonPercent<minimumBoon)
            throw new ArgumentException("The declared extra rate does not include the bonuses already active.",nameof(input));
        cancellation.ThrowIfCancellationRequested();
        var available=catalog.Actions.Where(action=>action.Spec.JobId==input.JobId && action.Spec.Level<=input.Level && !GpActionRules.Forbidden(input.Forbidden,action.Spec)).ToArray();
        var chances=available.Where(action=>action.Spec.Effect==GpEffect.Success && input.SuccessPercent>0).Cast<GpAction?>().Prepend(null).ToArray();
        var yields=available.Where(action=>action.Spec.Effect==GpEffect.Yield && !active.Yield).Cast<GpAction?>().Prepend(null).ToArray();
        var gift1=available.FirstOrDefault(action=>action.Spec.Effect==GpEffect.BoonChance1 && input.BoonPercent>0 && !active.Gift1);
        var gift2=available.FirstOrDefault(action=>action.Spec.Effect==GpEffect.BoonChance2 && input.BoonPercent>0 && !active.Gift2);
        var tidings=available.FirstOrDefault(action=>action.Spec.Effect==GpEffect.BoonYield && !active.Tidings);
        // 0.517: II comes with a quest: without it, the first one is still there.
        var bountiful=(input.Level>=68 ? available.FirstOrDefault(action=>action.Spec.Effect==GpEffect.Bountiful2):null) ?? available.FirstOrDefault(action=>action.Spec.Effect==GpEffect.Bountiful1);
        var restore=input.MoreActions ? available.FirstOrDefault(action=>action.Spec.Effect==GpEffect.Attempt):null;
        var clear=input.MoreActions && input.SuccessPercent>0 ? available.FirstOrDefault(action=>action.Spec.Effect==GpEffect.NextSuccess):null;
        var eureka=input.Level>=GpScenario.EurekaLevel ? GpScenario.EurekaChance:0m;
        // Plans far above the budget are not needed (the next upgrade is the first plan above it): repeated actions stop at budget + 600.
        var reach=input.Budget+600;
        var maxRestore=restore is null ? 0:Math.Min(input.Attempts,reach/restore.Spec.Cost);
        var bestAtCost=new Dictionary<int,GpPlan>();
        foreach(var chance in chances)
        foreach(var yield in yields)
        for(var mask=0;mask<8;mask++)
        {
            cancellation.ThrowIfCancellationRequested();
            if(((mask&1)!=0 && gift1 is null) || ((mask&2)!=0 && gift2 is null) || ((mask&4)!=0 && tidings is null)) continue;
            var prefix=new[]{chance,yield,(mask&1)!=0 ? gift1:null,(mask&2)!=0 ? gift2:null,(mask&4)!=0 ? tidings:null}
                .OfType<GpAction>().ToArray();
            var prefixCost=prefix.Sum(action=>action.Spec.Cost);
            var success=Math.Min(100,input.SuccessPercent+(chance?.Spec.Magnitude ?? 0))/100m;
            var clearSuccess=Math.Min(100,input.SuccessPercent+(chance?.Spec.Magnitude ?? 0)+(clear?.Spec.Magnitude ?? 0))/100m;
            var boon=Math.Min(100,input.BoonPercent+((mask&1)!=0 ? gift1!.Spec.Magnitude:0)+((mask&2)!=0 ? gift2!.Spec.Magnitude:0))/100m;
            var ordinary=input.BaseYield+(yield?.Spec.Magnitude ?? 0)+boon*(1+(active.Tidings ? 1:0)+((mask&4)!=0 ? tidings!.Spec.Magnitude:0));
            for(var restored=0;restored<=maxRestore;restored++)
            {
                var guaranteed=input.Attempts+restored;
                var attempts=guaranteed+restored*eureka;
                var purchasableAttempts=guaranteed-(active.FirstAttemptBonus>0 ? 1:0);
                var maxClear=clear is null || clearSuccess<=success ? 0:Math.Min(guaranteed,reach/clear.Spec.Cost);
                for(var cleared=0;cleared<=maxClear;cleared++)
                for(var count=0;count<=(bountiful is null ? 0:purchasableAttempts);count++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var cost=prefixCost+restored*(restore?.Spec.Cost ?? 0)+cleared*(clear?.Spec.Cost ?? 0)+count*(bountiful?.Spec.Cost ?? 0);
                    if(restored+cleared>0 && cost>reach)continue;
                    var bonus=bountiful?.Spec.Effect==GpEffect.Bountiful2 ? input.BountifulBonus:1;
                    // Clear Vision goes on the Bountiful attempts first, then on the attempt with the paid first bonus.
                    var overlap=Math.Min(count,cleared);
                    var firstClear=active.FirstAttemptBonus>0 && cleared>overlap;
                    var expected=ordinary*(cleared*clearSuccess+(attempts-cleared)*success)
                        +bonus*(overlap*clearSuccess+(count-overlap)*success)+active.FirstAttemptBonus*(firstClear ? clearSuccess:success);
                    if(bestAtCost.TryGetValue(cost,out var old) && old.ExpectedYield>=expected) continue;
                    var actions=prefix.Select(action=>new GpActionUse(action,1)).ToList();
                    if(restored>0) actions.Add(new(restore!,restored));
                    if(cleared>0) actions.Add(new(clear!,cleared));
                    if(count>0) actions.Add(new(bountiful!,count));
                    bestAtCost[cost]=new(cost,expected,Array.AsReadOnly(actions.ToArray()));
                }
            }
        }
        var frontier=new List<GpPlan>(); decimal maximum=-1;
        foreach(var plan in bestAtCost.Values.OrderBy(plan=>plan.Cost))
            if(plan.ExpectedYield>maximum) { frontier.Add(plan); maximum=plan.ExpectedYield; }
        var best=frontier.Last(plan=>plan.Cost<=input.Budget);
        var next=frontier.FirstOrDefault(plan=>plan.Cost>input.Budget && plan.ExpectedYield>best.ExpectedYield);
        var baseline=input.SuccessPercent/100m*(input.Attempts*(input.BaseYield+input.BoonPercent/100m*(active.Tidings ? 2:1))+active.FirstAttemptBonus);
        return new(input,baseline,best,next,Array.AsReadOnly(frontier.ToArray()));
    }
}
