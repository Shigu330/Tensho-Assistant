namespace TenshoAssistant.Craft;

public sealed record CraftNativeNumbers(uint Progress,uint MaxProgress,uint Quality,uint MaxQuality,uint Durability,uint MaxDurability,uint Step,uint ResultItemId,uint Condition,bool Expert);
public sealed record CraftNativeAction(CraftActionKind Kind,uint NativeId,uint Status);
public sealed record CraftLiveTool(uint ItemId,uint JobId,int? GoodQualityPercent);
public readonly record struct CraftRange(int Min,int Max)
{
    public override string ToString()=>Min==Max ? Min.ToString():$"{Min}–{Max}";
}
public sealed record CraftActionProjection(CraftActionMetadata Metadata,CraftRange Progress,CraftRange? Quality,CraftRange Durability,CraftRange CpCost,CraftRange InnerQuiet,
    bool Completed,bool Broken,int Alternatives);
public sealed record CraftProjectionResult(IReadOnlyList<CraftActionProjection> Actions,IReadOnlyList<string> Issues);

/// <summary>Projects one action across compatible unknown combo/unique-action states; never executes or resolves a rotation.</summary>
public static class CraftLiveProjection
{
    public static CraftProjectionResult Build(CraftLiveState observation,CraftActionCatalog catalog,DateTimeOffset now)
    {
        var issues=new List<string>();var rows=new List<CraftActionProjection>();
        CraftProjectionResult Result()=>new(Array.AsReadOnly(rows.ToArray()),Array.AsReadOnly(issues.ToArray()));
        if(!observation.Enabled || observation.Owner==0 || observation.Status!=CraftLiveStatusKind.Observed || observation.Frame is not { } frame || observation.ObservedAt is not { } date
            || now<date || now-date>TimeSpan.FromSeconds(2)) {issues.Add("Observation absente ou trop ancienne.");return Result();}
        var raw=frame.Raw;
        if(frame.Animating){issues.Add("Action ou animation en cours.");return Result();}
        if(frame.Recipe is not { } recipe){issues.Add("Identité de recette non établie.");return Result();}
        if(CraftSimulation.Unsupported(recipe) is { } unsupported){issues.Add(unsupported);return Result();}
        // Handler conditions are one-based; Synthesis AtkValues conditions are zero-based.
        if(raw.NumbersNative is not { } native || native.Step is <1 or >101 || native.ResultItemId!=recipe.ResultItemId || native.Condition>3 || raw.Condition!=native.Condition+1 || native.Expert)
        {issues.Add("Valeurs natives typées absentes, incohérentes ou condition hors des quatre états ordinaires.");return Result();}
        var condition=(CraftStepCondition)native.Condition;var goodPercent=150;var goodKnown=true;
        if(condition==CraftStepCondition.Good)
        {
            if(raw.Tool is not {ItemId:>0,GoodQualityPercent:150 or 175} tool || tool.JobId!=raw.JobId)
            {issues.Add("Bonus d'outil favorable inconnu : les gains de qualité concernés restent inconnus, les autres ressources restent projetées.");goodKnown=false;}
            else goodPercent=tool.GoodQualityPercent!.Value;
        }
        var p=recipe.Simulation!;
        if(native.MaxProgress!=p.Progress || native.MaxQuality!=p.Quality || native.MaxDurability!=p.Durability || frame.Progress!=native.Progress || (frame.Quality!=native.Quality && !(p.Quality==0 && native.Quality==0))
            || frame.Durability!=native.Durability || frame.UiStep!=native.Step || native.Progress>=native.MaxProgress || native.Durability==0)
        {issues.Add("Ressources affichées, valeurs natives et recette non concordantes ou synthèse terminée.");return Result();}
        var effects=CraftObservedEffects.Decode(frame,catalog);
        if(!effects.CountersKnown){issues.AddRange(effects.Issues);return Result();}
        if(!raw.ActionsComplete || raw.ActionsNative is not { } actions || actions.Count!=CraftActionCatalog.Specs.Count || actions.Select(action=>action.Kind).Distinct().Count()!=CraftActionCatalog.Specs.Count
            || actions.Any(action=>catalog.Find(raw.JobId,action.Kind)?.NativeId!=action.NativeId))
        {issues.Add("Disponibilité des actions native non établie pour ce métier.");return Result();}
        int Counter(uint id)=>effects.Effects.FirstOrDefault(effect=>effect.Metadata.Id==id)?.Counter ?? 0;
        var protection=effects.Effects.Any(effect=>effect.Metadata.Id==3813);
        CraftSimulationResult initial;
        try {initial=CraftSimulation.Run(recipe,raw.Stats with {Cp=(int)raw.Cp,ManipulationAllowed=true},0,[]);}
        catch(ArgumentException error){issues.Add(error.Message);return Result();}
        var combos=native.Step==1 ? new[]{0}:new[]{0,1,2};
        var usages=effects.UsedTrainedPerfection is { } used ? new[]{used}:new[]{false,true};
        foreach(var action in actions.Where(action=>action.Status==0))
        {
            var alternatives=new List<CraftSimStep>();
            foreach(var combo in combos)
            foreach(var usage in usages)
            {
                var state=new CraftSimState((int)native.Step-1,(int)native.Progress,(int)native.Quality,(int)native.Durability,(int)raw.Cp,Counter(251),
                    Counter(2226),Counter(2189),Counter(254),Math.Max(Counter(252),Counter(257)),Counter(1164),Counter(2191),combo,CraftFinish.InProgress,protection,usage);
                // Native availability narrows impossible resource/history alternatives; no hidden alternative is chosen as fact.
                var next=CraftSimulation.Apply(p,initial.Stats,recipe.Level,initial.BaseProgress,initial.BaseQuality,state,action.Kind,out _,condition,goodPercent);
                if(next is not null)alternatives.Add(next);
            }
            if(alternatives.Count==0){issues.Add($"{catalog.Find(raw.JobId,action.Kind)?.Name} : disponible côté client, aucune transition compatible dans le modèle.");continue;}
            CraftRange Range(Func<CraftSimStep,int> value)=>new(alternatives.Min(value),alternatives.Max(value));
            var qualityKnown=goodKnown || p.Quality==0 || !CraftSimulation.IncreasesQuality(action.Kind) || action.Kind==CraftActionKind.TrainedEye;
            rows.Add(new(catalog.Find(raw.JobId,action.Kind)!,Range(step=>step.After.Progress),qualityKnown ? Range(step=>step.After.Quality):null,Range(step=>step.After.Durability),
                Range(step=>step.CpCost),Range(step=>step.After.InnerQuiet),alternatives.All(step=>step.After.Finish==CraftFinish.Completed),
                alternatives.All(step=>step.After.Finish==CraftFinish.Broken),alternatives.Count));
        }
        return Result();
    }
}
