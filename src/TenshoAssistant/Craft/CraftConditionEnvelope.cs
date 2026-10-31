namespace TenshoAssistant.Craft;

public sealed record CraftConditionBand(int Step,CraftRange Quality);
public sealed record CraftConditionEnvelopeResult(CraftSimulationResult Normal,IReadOnlyList<CraftConditionBand> Steps,int? GoodQualityPercent)
{
    public CraftRange Quality=>Steps.Count==0 ? new(Normal.StartingQuality,Normal.StartingQuality):Steps[^1].Quality;
}

/// <summary>Conservative quality envelope of a fixed sequence, starting Normal; no probabilities or native state.</summary>
public static class CraftConditionEnvelope
{
    internal static IReadOnlyList<CraftStepCondition> Next(CraftStepCondition condition)=>condition switch
    {
        CraftStepCondition.Normal=>[CraftStepCondition.Normal,CraftStepCondition.Good,CraftStepCondition.Excellent],
        CraftStepCondition.Excellent=>[CraftStepCondition.Poor],
        CraftStepCondition.Good or CraftStepCondition.Poor=>[CraftStepCondition.Normal],
        _=>throw new ArgumentOutOfRangeException(nameof(condition))
    };

    public static CraftConditionEnvelopeResult Evaluate(RecipeDefinition recipe,CraftStats stats,int startingQuality,IEnumerable<CraftActionKind> sequence,
        int? goodQualityPercent=null,CancellationToken cancellation=default)
    {
        if(goodQualityPercent is not (null or 150 or 175))throw new ArgumentOutOfRangeException(nameof(goodQualityPercent));
        var normal=CraftSimulation.Run(recipe,stats,startingQuality,sequence,cancellation);
        var p=recipe.Simulation!;
        var states=new Dictionary<CraftStepCondition,CraftRange>{{CraftStepCondition.Normal,new(startingQuality,startingQuality)}};
        var bands=new List<CraftConditionBand>();var before=new CraftSimState(0,0,startingQuality,p.Durability,stats.Cp,0);
        foreach(var baseline in normal.Steps)
        {
            cancellation.ThrowIfCancellationRequested();
            var next=new Dictionary<CraftStepCondition,CraftRange>();var minimum=int.MaxValue;var maximum=0;
            foreach(var (condition,quality) in states)
            {
                // Quality cannot influence eligibility or other resources for this deterministic action set.
                // Keep only extrema per condition: every quality transition is monotone, including caps and Trained Eye.
                foreach(var value in new[]{quality.Min,quality.Max}.Distinct())
                foreach(var percent in goodQualityPercent is { } known ? new[]{known}:new[]{150,175})
                {
                    var step=CraftSimulation.Apply(p,stats,recipe.Level,normal.BaseProgress,normal.BaseQuality,before with {Quality=value},baseline.Action,out _,condition,percent);
                    if(step is null || step.After with {Quality=baseline.After.Quality}!=baseline.After)
                        throw new InvalidOperationException("Une condition affecte les ressources ou l’admissibilité : enveloppe non prise en charge.");
                    var after=step.After.Quality;minimum=Math.Min(minimum,after);maximum=Math.Max(maximum,after);
                    foreach(var following in Next(condition))
                        next[following]=next.TryGetValue(following,out var previous) ? new(Math.Min(previous.Min,after),Math.Max(previous.Max,after)):new(after,after);
                }
            }
            bands.Add(new(baseline.After.Step,new(minimum,maximum)));states=next;before=baseline.After;
        }
        return new(normal,Array.AsReadOnly(bands.ToArray()),goodQualityPercent);
    }
}
