namespace TenshoAssistant.Craft;

public sealed record CraftRecipeParameters(int Durability,int Progress,int Quality,int ProgressDivider,int QualityDivider,
    int ProgressModifier,int QualityModifier,int RequiredCraftsmanship,int RequiredControl,int MaxStartingQuality,
    bool Expert=false,bool LevelScaled=false,bool Specialist=false,bool Collectable=false,int RequiredQuality=0,
    // 0.600 (C14): shown on the recipe (Craft/RecipeFacts.cs), not used by the simulation.
    bool CanHq=false,bool QuickSynth=false,int QuickSynthCraftsmanship=0,int QuickSynthControl=0,string MasterBook="",uint RequiredItem=0,string RequiredStatus="",
    // C6 (0.666): the quality thresholds of a Cosmic Exploration mission (0 = none).
    int MissionLow=0,int MissionMid=0,int MissionHigh=0);
// A-13 (0.434): Specialist = the job is a specialist (Careful Observation, Heart and Soul, Quick Innovation, specialist recipes).
public sealed record CraftStats(int Level,int Craftsmanship,int Control,int Cp,bool ManipulationAllowed=false,bool Specialist=false);
public static class CraftRecipeQuality
{
    /// <summary>C6 (0.666, rule read in Craftimizer, MIT; docs/evidence/0.666-adjusted-recipes.md): an adjusted recipe's durability starts from 80, not from its level table.</summary>
    public static int Durability(bool levelScaled,int tableDurability,int factor)=>(levelScaled ? 80:tableDurability)*factor/100;
    /// <summary>C6 (0.667, read in the game's files): a mission's thresholds are percents of the maximum quality (20, 40, 70).</summary>
    public static int MissionThreshold(int maximum,int percent)=>checked((int)((long)maximum*percent/100));
    public static int Maximum(bool canHq,bool collectable,int requiredQuality,int baseQuality,int factor)
    {
        if(requiredQuality<0 || baseQuality<0 || factor<0)throw new ArgumentOutOfRangeException(nameof(baseQuality));
        return canHq || collectable || requiredQuality>0 ? checked((int)((long)baseQuality*factor/100)):0;
    }
}
public enum CraftFinish { InProgress, Completed, Broken }
// A-07 (0.435): the expert recipes' conditions, in Craftimizer's order (MIT, read only): Centered (success only), Sturdy and Robust (half
// durability), Pliant (half CP), Malleable (progress × 1.5), Primed (buffs set this step last two more steps), Good Omen (next step Good).
public enum CraftStepCondition {Normal,Good,Excellent,Poor,Centered,Sturdy,Pliant,Malleable,Primed,GoodOmen,Robust}
public sealed record CraftSimState(int Step,int Progress,int Quality,int Durability,int Cp,int InnerQuiet,
    int Veneration=0,int Innovation=0,int GreatStrides=0,int WasteNot=0,int Manipulation=0,int MuscleMemory=0,int TouchCombo=0,CraftFinish Finish=CraftFinish.InProgress,
    bool TrainedPerfection=false,bool UsedTrainedPerfection=false,
    int FinalAppraisal=0,bool HeartAndSoul=false,bool UsedHeartAndSoul=false,bool UsedQuickInnovation=false,int CarefulObservations=0);
public sealed record CraftSimStep(CraftActionKind Action,int CpCost,int ProgressGain,int QualityGain,CraftSimState After);
public sealed record CraftSimulationResult(uint RecipeId,CraftStats Stats,int StartingQuality,int BaseProgress,int BaseQuality,
    IReadOnlyList<CraftSimStep> Steps,CraftSimState Final,int PlannedActions,int? InvalidAction,string? Error);

public static class CraftSimulation
{
    /// <summary>Actions the game allows only on Good or Excellent, or with Heart and Soul (docs/evidence/0.434-craft-actions.md).</summary>
    internal static bool NeedsFavourable(CraftActionKind kind)=>kind is CraftActionKind.IntensiveSynthesis or CraftActionKind.PreciseTouch or CraftActionKind.TricksOfTheTrade;
    internal static bool IncreasesQuality(CraftActionKind kind)=>kind is CraftActionKind.BasicTouch or CraftActionKind.StandardTouch or CraftActionKind.AdvancedTouch or CraftActionKind.Byregot or CraftActionKind.Reflect
        or CraftActionKind.PreparatoryTouch or CraftActionKind.PrudentTouch or CraftActionKind.RefinedTouch or CraftActionKind.TrainedEye or CraftActionKind.TrainedFinesse or CraftActionKind.DelicateSynthesis
        or CraftActionKind.PreciseTouch or CraftActionKind.HastyTouch;
    /// <summary>A-13 (0.434): actions that succeed only by chance (Rapid Synthesis 50 %, Hasty Touch 60 %); the simulator assumes success.</summary>
    public static bool Random(CraftActionKind kind)=>kind is CraftActionKind.RapidSynthesis or CraftActionKind.HastyTouch;
    /// <summary>A-13 (0.434): actions that do not count as a step (no durability, buffs not shortened, Manipulation not ticked).</summary>
    internal static bool NonStep(CraftActionKind kind)=>kind is CraftActionKind.FinalAppraisal or CraftActionKind.CarefulObservation or CraftActionKind.HeartAndSoul or CraftActionKind.QuickInnovation;
    public static string? Unsupported(RecipeDefinition recipe)
    {
        var p=recipe.Simulation;
        if(p is null) return "Simulation parameters missing for this recipe.";
        if(p.Collectable && p.Quality<=0)return "Collectable maximum quality missing or incompatible.";
        if(p.Durability<=0 || p.Progress<=0 || p.Quality<0 || p.ProgressDivider<=0 || p.ProgressModifier<=0
            || p.Quality>0 && (p.QualityDivider<=0 || p.QualityModifier<=0) || p.MaxStartingQuality<0 || p.MaxStartingQuality>p.Quality)
            return "Native parameters incomplete or incompatible with the model.";
        return null;
    }
    public static CraftSimulationResult Run(RecipeDefinition recipe,CraftStats stats,int startingQuality,IEnumerable<CraftActionKind> sequence,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(recipe); ArgumentNullException.ThrowIfNull(stats); ArgumentNullException.ThrowIfNull(sequence);
        cancellation.ThrowIfCancellationRequested();
        if(Unsupported(recipe) is { } unsupported) throw new ArgumentException(unsupported,nameof(recipe));
        var p=recipe.Simulation!;
        if(stats.Level is <1 or >100 || stats.Craftsmanship is <0 or >100000 || stats.Control is <0 or >100000 || stats.Cp is <0 or >10000
            || startingQuality<0 || startingQuality>p.MaxStartingQuality) throw new ArgumentOutOfRangeException(nameof(stats));
        if(stats.Craftsmanship<p.RequiredCraftsmanship || stats.Control<p.RequiredControl) throw new ArgumentException("Craftsmanship or Control below the recipe's minimum.",nameof(stats));
        if(p.Specialist && !stats.Specialist) throw new ArgumentException("Specialist recipe: tick Specialist in the attributes.",nameof(stats));
        var actions=sequence.Take(101).ToArray(); if(actions.Length>100) throw new ArgumentException("100 actions maximum.",nameof(sequence));
        // Single-precision base gain and truncation reproduce the verified game-model baseline.
        var progressBase=stats.Craftsmanship*10f/p.ProgressDivider+2;
        var qualityBase=p.Quality==0 ? 0:stats.Control*10f/p.QualityDivider+35;
        if(stats.Level<=recipe.Level) { progressBase*=p.ProgressModifier*.01f; qualityBase*=p.QualityModifier*.01f; }
        var bp=(int)progressBase; var bq=(int)qualityBase;
        var state=new CraftSimState(0,0,startingQuality,p.Durability,stats.Cp,0);
        var steps=new List<CraftSimStep>(); int? invalid=null; string? error=null;
        for(var index=0;index<actions.Length && state.Finish==CraftFinish.InProgress;index++)
        {
            cancellation.ThrowIfCancellationRequested();
            var step=Apply(p,stats,recipe.Level,bp,bq,state,actions[index],out error);
            if(step is null) { invalid=index; break; }
            state=step.After; steps.Add(step);
        }
        return new(recipe.RecipeId,stats,startingQuality,bp,bq,Array.AsReadOnly(steps.ToArray()),state,actions.Length,invalid,error);
    }

    // Shared transition: the manual simulator and search must obey exactly the same rules.
    internal static CraftSimStep? Apply(CraftRecipeParameters p,CraftStats stats,int recipeLevel,int bp,int bq,CraftSimState state,CraftActionKind kind,out string? error,
        CraftStepCondition condition=CraftStepCondition.Normal,int goodQualityPercent=150)
    {
        if(!Enum.IsDefined(condition) || goodQualityPercent is not (150 or 175)){error="Favourable condition or bonus not supported.";return null;}
        var spec=CraftActionCatalog.Specs.FirstOrDefault(spec=>spec.Kind==kind);
        var quality=IncreasesQuality(kind);
        var progress=kind is CraftActionKind.BasicSynthesis or CraftActionKind.CarefulSynthesis or CraftActionKind.Groundwork or CraftActionKind.MuscleMemory or CraftActionKind.DelicateSynthesis or CraftActionKind.PrudentSynthesis
            or CraftActionKind.IntensiveSynthesis or CraftActionKind.RapidSynthesis;
        var nonStep=NonStep(kind);
        var favourable=condition is CraftStepCondition.Good or CraftStepCondition.Excellent;
        var needsFavourable=NeedsFavourable(kind);
        var cp=spec?.Cp ?? 0;
        if(kind==CraftActionKind.StandardTouch && state.TouchCombo==1 || kind==CraftActionKind.AdvancedTouch && state.TouchCombo==2) cp=18;
        // A-07 (0.435): Pliant halves the CP cost, rounded up.
        if(condition==CraftStepCondition.Pliant) cp=(cp+1)/2;
        error=spec is null ? "Action non prise en charge." : stats.Level<spec.Level ? "Level too low for this action."
            : kind==CraftActionKind.Manipulation && !stats.ManipulationAllowed ? "Use of Manipulation not confirmed in the scenario."
            : (kind is CraftActionKind.MuscleMemory or CraftActionKind.Reflect or CraftActionKind.TrainedEye) && state.Step!=0 ? "Action reserved for the first step."
            : kind==CraftActionKind.TrainedEye && stats.Level<recipeLevel+10 ? "Trained Eye needs ten levels more than the recipe."
            : (kind is CraftActionKind.PrudentSynthesis or CraftActionKind.PrudentTouch) && state.WasteNot>0 ? "Action incompatible with Waste Not."
            : kind==CraftActionKind.TrainedFinesse && state.InnerQuiet!=10 ? "Ten stacks of Inner Quiet required."
            : kind==CraftActionKind.TrainedPerfection && state.UsedTrainedPerfection ? "Trained Perfection can be used only once per synthesis."
            : kind==CraftActionKind.Byregot && state.InnerQuiet==0 ? "Inner Quiet required for Byregot."
            : (kind is CraftActionKind.CarefulObservation or CraftActionKind.HeartAndSoul or CraftActionKind.QuickInnovation) && !stats.Specialist ? "Specialist action: tick Specialist in the attributes."
            : kind==CraftActionKind.HeartAndSoul && state.UsedHeartAndSoul ? "Heart and Soul can be used only once per synthesis."
            : kind==CraftActionKind.QuickInnovation && (state.UsedQuickInnovation || state.Innovation>0) ? "Quick Innovation: once per synthesis, without Innovation active."
            : kind==CraftActionKind.CarefulObservation && state.CarefulObservations>=3 ? "Careful Observation can be used three times per synthesis."
            : needsFavourable && !favourable && !state.HeartAndSoul ? "Good or Excellent condition, or Heart and Soul, required."
            : quality && p.Quality==0 && kind!=CraftActionKind.DelicateSynthesis ? "This recipe has no quality target."
            : state.Cp<cp ? "Not enough CP for this action." : null;
        if(error is not null) return null;
        // Waste Not, and (A-07, 0.435) Sturdy or Robust, each halve the cost; rounded up once at the end (as Craftimizer).
        var durabilityHalves=(state.WasteNot>0 ? 1:0)+(condition is CraftStepCondition.Sturdy or CraftStepCondition.Robust ? 1:0);
        var durabilityCost=nonStep || state.TrainedPerfection ? 0:(spec!.Durability+(1<<durabilityHalves)-1)>>durabilityHalves;
        var efficiency=kind switch
        {
            CraftActionKind.BasicSynthesis=>stats.Level>=31 ? 120:100,
            CraftActionKind.CarefulSynthesis=>stats.Level>=82 ? 180:150,
            CraftActionKind.Groundwork=>(stats.Level>=86 ? 360:300)/(state.Durability<durabilityCost ? 2:1),
            CraftActionKind.MuscleMemory or CraftActionKind.Reflect=>300,
            CraftActionKind.BasicTouch or CraftActionKind.DelicateSynthesis or CraftActionKind.PrudentTouch or CraftActionKind.RefinedTouch or CraftActionKind.TrainedFinesse=>100,
            CraftActionKind.StandardTouch=>125,CraftActionKind.AdvancedTouch=>150,CraftActionKind.PreparatoryTouch=>200,CraftActionKind.PrudentSynthesis=>180,
            CraftActionKind.Byregot=>100+20*state.InnerQuiet,
            CraftActionKind.IntensiveSynthesis=>400,CraftActionKind.PreciseTouch=>150,CraftActionKind.HastyTouch=>100,
            CraftActionKind.RapidSynthesis=>stats.Level>=63 ? 500:250,_=>0
        };
        var progressEfficiency=kind==CraftActionKind.DelicateSynthesis && stats.Level>=94 ? 150:efficiency;
        var progressCondition=condition==CraftStepCondition.Malleable ? 150:100;
        var pg=progress ? (int)((long)bp*progressEfficiency*(100+(state.Veneration>0 ? 50:0)+(state.MuscleMemory>0 ? 100:0))*progressCondition/1000000):0;
        var conditionPercent=condition switch {CraftStepCondition.Good=>goodQualityPercent,CraftStepCondition.Excellent=>400,CraftStepCondition.Poor=>50,_=>100};
        var qg=quality ? (int)((long)bq*efficiency*(100+10*state.InnerQuiet)*(100+(state.Innovation>0 ? 50:0)+(state.GreatStrides>0 ? 100:0))*conditionPercent/100000000):0;
        if(kind==CraftActionKind.TrainedEye) qg=p.Quality-state.Quality;
        var durability=kind==CraftActionKind.ImmaculateMend ? p.Durability:kind==CraftActionKind.MastersMend ? Math.Min(p.Durability,state.Durability+30):state.Durability-durabilityCost;
        if(!nonStep && durability>0 && state.Manipulation>0 && kind!=CraftActionKind.Manipulation) durability=Math.Min(p.Durability,durability+5);
        var nextProgress=Math.Min(p.Progress,checked(state.Progress+pg)); var nextQuality=Math.Min(p.Quality,checked(state.Quality+qg));
        // Final Appraisal: the progress that would complete the synthesis stops one short, once.
        var finalAppraisal=kind==CraftActionKind.FinalAppraisal ? 5+(condition==CraftStepCondition.Primed ? 2:0):nonStep ? state.FinalAppraisal:Math.Max(0,state.FinalAppraisal-1);
        if(state.FinalAppraisal>0 && pg>0 && nextProgress>=p.Progress){nextProgress=p.Progress-1;finalAppraisal=0;}
        var iq=quality && stats.Level>=11 ? Math.Min(10,state.InnerQuiet+1):state.InnerQuiet;
        if(kind is CraftActionKind.Reflect or CraftActionKind.PreparatoryTouch or CraftActionKind.PreciseTouch || kind==CraftActionKind.RefinedTouch && state.TouchCombo==1) iq=Math.Min(10,state.InnerQuiet+2);
        if(kind==CraftActionKind.Byregot) iq=0;
        var old=state;
        // Buffs are shortened by the actions that count as a step only (A-13, 0.434).
        int Tick(int value)=>nonStep ? value:Math.Max(0,value-1);
        // A-07 (0.435): under Primed, a buff set by this action lasts two more steps.
        var primed=condition==CraftStepCondition.Primed ? 2:0;
        var cpAfter=kind==CraftActionKind.TricksOfTheTrade ? Math.Min(stats.Cp,state.Cp-cp+20):state.Cp-cp;
        var heartAndSoul=kind==CraftActionKind.HeartAndSoul || state.HeartAndSoul && !(needsFavourable && !favourable);
        state=new(nonStep ? state.Step:state.Step+1,nextProgress,nextQuality,Math.Max(0,durability),cpAfter,iq,
            kind==CraftActionKind.Veneration ? 4+primed:Tick(state.Veneration),
            kind==CraftActionKind.Innovation ? 4+primed:kind==CraftActionKind.QuickInnovation ? 1+primed:Tick(state.Innovation),
            kind==CraftActionKind.GreatStrides ? 3+primed:quality ? 0:Tick(state.GreatStrides),
            kind==CraftActionKind.WasteNot ? 4+primed:kind==CraftActionKind.WasteNot2 ? 8+primed:Tick(state.WasteNot),
            kind==CraftActionKind.Manipulation ? 8+primed:Tick(state.Manipulation),
            kind==CraftActionKind.MuscleMemory ? 5+primed:progress ? 0:Tick(state.MuscleMemory),
            nonStep ? state.TouchCombo:kind==CraftActionKind.BasicTouch ? 1:(kind==CraftActionKind.StandardTouch && state.TouchCombo==1 || kind==CraftActionKind.Observe) ? 2:0,
            nextProgress>=p.Progress ? CraftFinish.Completed:durability<=0 ? CraftFinish.Broken:CraftFinish.InProgress,
            kind==CraftActionKind.TrainedPerfection || state.TrainedPerfection && spec!.Durability==0,
            state.UsedTrainedPerfection || kind==CraftActionKind.TrainedPerfection,
            finalAppraisal,heartAndSoul,state.UsedHeartAndSoul || kind==CraftActionKind.HeartAndSoul,state.UsedQuickInnovation || kind==CraftActionKind.QuickInnovation,
            state.CarefulObservations+(kind==CraftActionKind.CarefulObservation ? 1:0));
        return new(kind,cp,state.Progress-old.Progress,state.Quality-old.Quality,state);
    }
}
