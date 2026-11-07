namespace TenshoAssistant.Craft;

/// <summary>
/// 0.589 (C5): where a synthesis stands against a planned sequence. Position is the plan's action to play next, Expected what the plan has
/// reached at that point. Nothing is played: the player reads it and acts.
/// </summary>
public sealed record CraftFollowView(int Position,int Total,CraftActionKind? Next,CraftSimState Expected,bool Diverged,bool QualityCompared);

public static class CraftFollow
{
    /// <summary>
    /// The game's step counts the actions that take a turn, starting at 1, so step N follows N-1 of them. Actions that take no turn (Final
    /// Appraisal, for example) share a step with the next one: of the plan's positions at that step, the first whose numbers are the game's.
    /// </summary>
    public static CraftFollowView Compare(CraftSimulationResult plan,int maxDurability,int uiStep,int progress,int quality,int durability,int cp,bool qualityKnown)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var done=Math.Max(0,uiStep-1);
        CraftSimState Before(int position)=>position==0 ? new CraftSimState(0,0,plan.StartingQuality,maxDurability,plan.Stats.Cp,0):plan.Steps[position-1].After;
        bool Same(CraftSimState state)=>state.Progress==progress && (!qualityKnown || state.Quality==quality) && state.Durability==durability && state.Cp==cp;
        int? first=null,match=null;
        for(var position=0;position<=plan.Steps.Count && match is null;position++)
        {
            var state=Before(position);
            if(state.Step!=done)continue;
            first??=position;
            if(Same(state))match=position;
        }
        // Past the plan's last step: the game has gone on without it.
        var at=match ?? first ?? plan.Steps.Count;
        var expected=Before(at);
        return new(at,plan.Steps.Count,at<plan.Steps.Count ? plan.Steps[at].Action:null,expected,match is null,qualityKnown);
    }
}

/// <summary>
/// 0.589 (C5): what a follow needs to remember between two readings of the same synthesis: the quality it started from (materials of high
/// quality raise it) and the states other than Normal, which the plan does not count on. A step going back means a new synthesis.
/// </summary>
public sealed class CraftFollowTracker
{
    private uint recipe;
    private int lastStep;
    private readonly List<(int Step,string Name)> conditions=[];
    public int? StartingQuality {get;private set;}

    public void Observe(uint recipeId,int uiStep,int quality,bool normal,string conditionName)
    {
        if(recipeId!=recipe || uiStep==1 || uiStep<lastStep)
        {
            recipe=recipeId;
            conditions.Clear();
            StartingQuality=uiStep==1 ? quality:null;
        }
        lastStep=uiStep;
        if(!normal && !conditions.Any(row=>row.Step==uiStep))conditions.Add((uiStep,conditionName));
    }

    /// <summary>The states already played on: the one of the current step has not changed any number yet.</summary>
    public IReadOnlyList<(int Step,string Name)> Before(int uiStep)=>conditions.Where(row=>row.Step<uiStep).ToArray();
}
