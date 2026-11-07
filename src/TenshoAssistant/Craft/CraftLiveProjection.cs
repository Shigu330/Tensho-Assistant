namespace TenshoAssistant.Craft;

public sealed record CraftNativeNumbers(uint Progress,uint MaxProgress,uint Quality,uint MaxQuality,uint Durability,uint MaxDurability,uint Step,uint ResultItemId,uint Condition,bool Expert);
public sealed record CraftNativeAction(CraftActionKind Kind,uint NativeId,uint Status);
public sealed record CraftLiveTool(uint ItemId,uint JobId,int? GoodQualityPercent);
public readonly record struct CraftRange(int Min,int Max)
{
    public override string ToString()=>Min==Max ? Min.ToString():$"{Min}–{Max}";
}
public sealed record CraftActionProjection(CraftActionMetadata Metadata,CraftRange Progress,CraftRange? Quality,CraftRange Durability,CraftRange CpCost,CraftRange InnerQuiet,
    bool Completed,bool Broken,int Alternatives,bool OnlyInThisCondition=false);
/// <summary>0.497 (user report): Condition = the state of the synthesis the rows were computed with; GoodPercent = the Good quality bonus, null when the tool's is unknown.</summary>
public sealed record CraftProjectionResult(IReadOnlyList<CraftActionProjection> Actions,IReadOnlyList<string> Issues,CraftStepCondition? Condition=null,int? GoodPercent=null,CraftSuggestInput? Suggest=null);

/// <summary>Projects one action across compatible unknown combo/unique-action states; never executes or resolves a rotation.</summary>
public static class CraftLiveProjection
{
    public static CraftProjectionResult Build(CraftLiveState observation,CraftActionCatalog catalog,DateTimeOffset now)
    {
        var issues=new List<string>();var rows=new List<CraftActionProjection>();
        CraftStepCondition? seenCondition=null;int? seenGood=null;CraftSuggestInput? suggest=null;
        CraftProjectionResult Result()=>new(Array.AsReadOnly(rows.ToArray()),Array.AsReadOnly(issues.ToArray()),seenCondition,seenGood,suggest);
        if(!observation.Enabled || observation.Owner==0 || observation.Status!=CraftLiveStatusKind.Observed || observation.Frame is not { } frame || observation.ObservedAt is not { } date
            || now<date || now-date>TimeSpan.FromSeconds(2)) {issues.Add("Observation missing or too old.");return Result();}
        var raw=frame.Raw;
        if(frame.Animating){issues.Add("Action or animation in progress.");return Result();}
        if(frame.Recipe is not { } recipe){issues.Add("Recipe identity not established.");return Result();}
        if(CraftSimulation.Unsupported(recipe) is { } unsupported){issues.Add(unsupported);return Result();}
        // Handler conditions are one-based; Synthesis AtkValues conditions are zero-based.
        if(raw.NumbersNative is not { } native || native.Step is <1 or >101 || native.ResultItemId!=recipe.ResultItemId || native.Condition>3 || raw.Condition!=native.Condition+1 || native.Expert)
        {issues.Add("Typed native values missing, inconsistent, or condition outside the four ordinary states.");return Result();}
        var condition=(CraftStepCondition)native.Condition;var goodPercent=150;var goodKnown=true;
        if(condition==CraftStepCondition.Good)
        {
            if(raw.Tool is not {ItemId:>0,GoodQualityPercent:150 or 175} tool || tool.JobId!=raw.JobId)
            {issues.Add("Favourable tool bonus unknown: the affected quality gains stay unknown, the other resources are still projected.");goodKnown=false;}
            else goodPercent=tool.GoodQualityPercent!.Value;
        }
        seenCondition=condition;seenGood=goodKnown ? goodPercent:null;
        var p=recipe.Simulation!;
        // 0.784 (X-26, user report: no next move on a recipe where HQ is impossible): the game still shows that recipe's quality bar (2 500), Tensho keeps 0 on purpose
        // (CraftRecipeQuality.Maximum: nothing to seek); only that gap is accepted.
        var qualityAgrees=native.MaxQuality==p.Quality || p.Quality==0 && !p.CanHq && !p.Collectable && p.RequiredQuality==0;
        if(native.MaxProgress!=p.Progress || !qualityAgrees || native.MaxDurability!=p.Durability || frame.Progress!=native.Progress || (frame.Quality!=native.Quality && !(p.Quality==0 && native.Quality==0))
            || frame.Durability!=native.Durability || frame.UiStep!=native.Step || native.Progress>=native.MaxProgress || native.Durability==0)
        {issues.Add("Displayed resources, native values and recipe do not agree, or synthesis finished.");return Result();}
        var effects=CraftObservedEffects.Decode(frame,catalog);
        if(!effects.CountersKnown){issues.AddRange(effects.Issues);return Result();}
        if(!raw.ActionsComplete || raw.ActionsNative is not { } actions || actions.Count!=CraftActionCatalog.Specs.Count || actions.Select(action=>action.Kind).Distinct().Count()!=CraftActionCatalog.Specs.Count
            || actions.Any(action=>catalog.Find(raw.JobId,action.Kind)?.NativeId!=action.NativeId))
        {issues.Add("Native action availability not established for this job.");return Result();}
        int Counter(uint id)=>effects.Effects.FirstOrDefault(effect=>effect.Metadata.Id==id)?.Counter ?? 0;
        var protection=effects.Effects.Any(effect=>effect.Metadata.Id==3813);
        CraftSimulationResult initial;
        try {initial=CraftSimulation.Run(recipe,raw.Stats with {Cp=(int)raw.Cp,ManipulationAllowed=true},0,[]);}
        catch(ArgumentException error){issues.Add(error.Message);return Result();}
        var combos=native.Step==1 ? new[]{0}:new[]{0,1,2};
        var usages=effects.UsedTrainedPerfection is { } used ? new[]{used}:new[]{false,true};
        // A-04 (0.509): what a suggestion starts from. The combo is unknown here: taken as none, which never counts on a cheaper cost. Not without the tool's bonus.
        if(goodKnown)
            suggest=new(recipe,initial.Stats,new CraftSimState((int)native.Step-1,(int)native.Progress,(int)native.Quality,(int)native.Durability,(int)raw.Cp,Counter(251),
                Counter(2226),Counter(2189),Counter(254),Math.Max(Counter(252),Counter(257)),Counter(1164),Counter(2191),0,CraftFinish.InProgress,protection,usages[0],
                Counter(2190),effects.Effects.Any(effect=>effect.Metadata.Id==2665)),condition,goodPercent,Array.AsReadOnly(actions.Where(action=>action.Status==0).Select(action=>action.Kind).ToArray()));
        foreach(var action in actions.Where(action=>action.Status==0))
        {
            var alternatives=new List<CraftSimStep>();
            foreach(var combo in combos)
            foreach(var usage in usages)
            {
                // A-13 (0.434): Final Appraisal (2190) and Heart and Soul (2665) as observed; the uses of the specialist actions are not
                // shown by the game: taken as not used (the game's own availability then refuses what is spent).
                var state=new CraftSimState((int)native.Step-1,(int)native.Progress,(int)native.Quality,(int)native.Durability,(int)raw.Cp,Counter(251),
                    Counter(2226),Counter(2189),Counter(254),Math.Max(Counter(252),Counter(257)),Counter(1164),Counter(2191),combo,CraftFinish.InProgress,protection,usage,
                    Counter(2190),effects.Effects.Any(effect=>effect.Metadata.Id==2665));
                // Native availability narrows impossible resource/history alternatives; no hidden alternative is chosen as fact.
                var next=CraftSimulation.Apply(p,initial.Stats,recipe.Level,initial.BaseProgress,initial.BaseQuality,state,action.Kind,out _,condition,goodPercent);
                if(next is not null)alternatives.Add(next);
            }
            if(alternatives.Count==0){issues.Add($"{catalog.Find(raw.JobId,action.Kind)?.Name}: available client-side, no compatible transition in the model.");continue;}
            CraftRange Range(Func<CraftSimStep,int> value)=>new(alternatives.Min(value),alternatives.Max(value));
            var qualityKnown=goodKnown || p.Quality==0 || !CraftSimulation.IncreasesQuality(action.Kind) || action.Kind==CraftActionKind.TrainedEye;
            rows.Add(new(catalog.Find(raw.JobId,action.Kind)!,Range(step=>step.After.Progress),qualityKnown ? Range(step=>step.After.Quality):null,Range(step=>step.After.Durability),
                Range(step=>step.CpCost),Range(step=>step.After.InnerQuiet),alternatives.All(step=>step.After.Finish==CraftFinish.Completed),
                alternatives.All(step=>step.After.Finish==CraftFinish.Broken),alternatives.Count,
                CraftSimulation.NeedsFavourable(action.Kind) && condition is CraftStepCondition.Good or CraftStepCondition.Excellent));
        }
        return Result();
    }
}
