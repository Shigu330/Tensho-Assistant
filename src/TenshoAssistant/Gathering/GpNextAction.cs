namespace TenshoAssistant.Gathering;

public enum GpNextKind { Action,Gather,OffPlan,Done }
/// <summary>Used: how many of each action the plan has seen spent, by action id.</summary>
/// <summary>Finished: every step of the plan is behind; Leftover: the action comes from the GP left at the end, not from the plan.</summary>
public sealed record GpNextStep(GpNextKind Kind,GpAction? Action,IReadOnlyDictionary<uint,int> Used,bool Finished=false,bool Leftover=false);
/// <summary>A hotbar slot on the screen, copied on the framework thread.</summary>
public readonly record struct ActionBarRect(float X,float Y,float Width,float Height)
{
    public bool Valid=>Width>0 && Height>0 && float.IsFinite(X) && float.IsFinite(Y);
}

/// <summary>
/// 0.728 (user request): the step of a node's plan, worked out from what the game shows: the GP spent since the node opened and the attempts left. The game does not say which action was used:
/// docs/evidence/0.728-gather-next.md. Nothing here acts: the player uses the action.
/// </summary>
public static class GpNextAction
{
    /// <summary>
    /// 0.731 (user report): each gather gives back 6 GP: +6 after one gather, +30 after five, botanist level 100, 2026-11-07. Counted for the gathers done, with a small margin;
    /// the plan made at the opening does not count on it, the end of the node does (<see cref="Leftover"/>).
    /// </summary>
    public const int ReturnPerGather=6,Tolerance=10;
    private const int MaximumSteps=16;

    private static bool PerGather(GpEffect effect)=>effect is GpEffect.Bountiful1 or GpEffect.Bountiful2 or GpEffect.NextSuccess;

    /// <summary>What lasts comes first, in the plan's order; then, attempt by attempt, what only lasts for the next gather. Attempt 0: not tied to one.</summary>
    public static IReadOnlyList<(GpAction Action,int Attempt)> Steps(GpPlan plan)
    {
        var steps=new List<(GpAction,int)>();
        foreach(var use in plan.Actions.Where(use=>!PerGather(use.Action.Spec.Effect)))
            for(var count=0;count<use.Count;count++)steps.Add((use.Action,0));
        var each=plan.Actions.Where(use=>PerGather(use.Action.Spec.Effect)).ToArray();
        var attempts=each.Length==0 ? 0:each.Max(use=>use.Count);
        for(var attempt=1;attempt<=attempts;attempt++)
            foreach(var use in each.Where(use=>use.Count>=attempt))steps.Add((use.Action,attempt));
        return steps;
    }

    /// <summary>The steps whose costs add up to the GP spent, the earliest ones first; null when none do: the player left the plan.</summary>
    private static bool[]? Done(IReadOnlyList<(GpAction Action,int Attempt)> steps,int spent,int tolerance)
    {
        if(steps.Count>MaximumSteps)return null;
        bool[]? best=null;var bestFirst=-1;
        for(var mask=0;mask<1<<steps.Count;mask++)
        {
            var sum=0;
            for(var index=0;index<steps.Count;index++)if((mask>>index&1)!=0)sum+=steps[index].Action.Spec.Cost;
            if(sum<spent || sum>spent+tolerance)continue;
            var first=0;
            while(first<steps.Count && (mask>>first&1)!=0)first++;
            if(first<=bestFirst)continue;
            bestFirst=first;
            best=steps.Select((_,index)=>(mask>>index&1)!=0).ToArray();
        }
        return best;
    }

    public static GpNextStep Next(GpPlan plan,int startGp,int gp,int startAttempts,int attempts)
    {
        var steps=Steps(plan);
        var gathered=Math.Max(0,startAttempts+steps.Count(step=>step.Action.Spec.Effect==GpEffect.Attempt)-attempts);
        var done=Done(steps,Math.Max(0,startGp-gp),Tolerance+ReturnPerGather*gathered);
        if(done is null)return new(GpNextKind.OffPlan,null,new Dictionary<uint,int>());
        var used=new Dictionary<uint,int>();
        var more=0;
        for(var index=0;index<steps.Count;index++)
        {
            if(!done[index])continue;
            used[steps[index].Action.Spec.Id]=used.GetValueOrDefault(steps[index].Action.Spec.Id)+1;
            if(steps[index].Action.Spec.Effect==GpEffect.Attempt)more++;
        }
        if(attempts<=0)return new(GpNextKind.Done,null,used);
        // The gather that comes next; a step meant for an attempt already gone is passed.
        var gather=Math.Max(0,startAttempts+more-attempts)+1;
        for(var index=0;index<steps.Count;index++)
        {
            if(done[index] || steps[index].Attempt!=0 && steps[index].Attempt<gather)continue;
            return steps[index].Attempt>gather ? new(GpNextKind.Gather,null,used):new(GpNextKind.Action,steps[index].Action,used);
        }
        return new(GpNextKind.Gather,null,used,Finished:true);
    }

    /// <summary>
    /// 0.731 (user report): when the plan is behind and attempts remain, the GP left (with what gathers gave back) can still buy something: the same optimizer, from the node as it is now.
    /// What was bought stays active and is not offered again; a success action bought is not bought twice.
    /// </summary>
    public static GpNextStep Leftover(GpNextStep step,GpScenario opened,GpPlan plan,GpActionCatalog catalog,int gp,int attempts,int successNow,int boonNow)
    {
        if(step is not { Kind:GpNextKind.Gather,Finished:true } || attempts<=0)return step;
        var bought=plan.Actions.Where(use=>step.Used.GetValueOrDefault(use.Action.Spec.Id)>0).Select(use=>use.Action.Spec).ToArray();
        var active=new GpActiveEffects(Yield:bought.Any(spec=>spec.Effect==GpEffect.Yield),Gift1:bought.Any(spec=>spec.Effect==GpEffect.BoonChance1),
            Gift2:bought.Any(spec=>spec.Effect==GpEffect.BoonChance2),Tidings:bought.Any(spec=>spec.Effect==GpEffect.BoonYield));
        var forbidden=GpActionRules.Joined((opened.Forbidden ?? "").Split('|').Concat(bought.Where(spec=>spec.Effect==GpEffect.Success).Select(GpActionRules.Key)));
        try
        {
            var again=GpOptimizer.Solve(opened with {Budget=Math.Max(0,gp),Attempts=attempts,SuccessPercent=successNow,BoonPercent=boonNow,Active=active,Forbidden=forbidden},catalog);
            return again.Best.Actions.FirstOrDefault() is { } use ? step with {Kind=GpNextKind.Action,Action=use.Action,Leftover=true}:step;
        }
        catch(ArgumentException){return step;}
    }
}
