namespace TenshoAssistant.Craft;

public sealed record CraftRecipeParameters(int Durability,int Progress,int Quality,int ProgressDivider,int QualityDivider,
    int ProgressModifier,int QualityModifier,int RequiredCraftsmanship,int RequiredControl,int MaxStartingQuality,
    bool Expert=false,bool LevelScaled=false,bool Specialist=false,bool Collectable=false,int RequiredQuality=0);
public sealed record CraftStats(int Level,int Craftsmanship,int Control,int Cp,bool ManipulationAllowed=false);
public static class CraftRecipeQuality
{
    public static int Maximum(bool canHq,bool collectable,int requiredQuality,int baseQuality,int factor)
    {
        if(requiredQuality<0 || baseQuality<0 || factor<0)throw new ArgumentOutOfRangeException(nameof(baseQuality));
        return canHq || collectable || requiredQuality>0 ? checked((int)((long)baseQuality*factor/100)):0;
    }
}
public enum CraftFinish { InProgress, Completed, Broken }
public enum CraftStepCondition {Normal,Good,Excellent,Poor}
public sealed record CraftSimState(int Step,int Progress,int Quality,int Durability,int Cp,int InnerQuiet,
    int Veneration=0,int Innovation=0,int GreatStrides=0,int WasteNot=0,int Manipulation=0,int MuscleMemory=0,int TouchCombo=0,CraftFinish Finish=CraftFinish.InProgress,
    bool TrainedPerfection=false,bool UsedTrainedPerfection=false);
public sealed record CraftSimStep(CraftActionKind Action,int CpCost,int ProgressGain,int QualityGain,CraftSimState After);
public sealed record CraftSimulationResult(uint RecipeId,CraftStats Stats,int StartingQuality,int BaseProgress,int BaseQuality,
    IReadOnlyList<CraftSimStep> Steps,CraftSimState Final,int PlannedActions,int? InvalidAction,string? Error);

public static class CraftSimulation
{
    internal static bool IncreasesQuality(CraftActionKind kind)=>kind is CraftActionKind.BasicTouch or CraftActionKind.StandardTouch or CraftActionKind.AdvancedTouch or CraftActionKind.Byregot or CraftActionKind.Reflect
        or CraftActionKind.PreparatoryTouch or CraftActionKind.PrudentTouch or CraftActionKind.RefinedTouch or CraftActionKind.TrainedEye or CraftActionKind.TrainedFinesse or CraftActionKind.DelicateSynthesis;
    public static string? Unsupported(RecipeDefinition recipe)
    {
        var p=recipe.Simulation;
        if(p is null) return "Paramètres de simulation absents pour cette recette.";
        if(p.Expert || p.LevelScaled || p.Specialist || p.RequiredQuality>0)
            return "Recette experte, ajustée, spécialisée ou à qualité imposée hors périmètre de cette simulation.";
        if(p.Collectable && p.Quality<=0)return "Qualité maximale du collectionnable absente ou incompatible.";
        if(p.Durability<=0 || p.Progress<=0 || p.Quality<0 || p.ProgressDivider<=0 || p.ProgressModifier<=0
            || p.Quality>0 && (p.QualityDivider<=0 || p.QualityModifier<=0) || p.MaxStartingQuality<0 || p.MaxStartingQuality>p.Quality)
            return "Paramètres natifs incomplets ou incompatibles avec le modèle.";
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
        if(stats.Craftsmanship<p.RequiredCraftsmanship || stats.Control<p.RequiredControl) throw new ArgumentException("Habileté ou Contrôle sous le minimum de la recette.",nameof(stats));
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
        if(!Enum.IsDefined(condition) || goodQualityPercent is not (150 or 175)){error="Condition ou bonus favorable non pris en charge.";return null;}
        var spec=CraftActionCatalog.Specs.FirstOrDefault(spec=>spec.Kind==kind);
        var quality=IncreasesQuality(kind);
        var progress=kind is CraftActionKind.BasicSynthesis or CraftActionKind.CarefulSynthesis or CraftActionKind.Groundwork or CraftActionKind.MuscleMemory or CraftActionKind.DelicateSynthesis or CraftActionKind.PrudentSynthesis;
        var cp=spec?.Cp ?? 0;
        if(kind==CraftActionKind.StandardTouch && state.TouchCombo==1 || kind==CraftActionKind.AdvancedTouch && state.TouchCombo==2) cp=18;
        error=spec is null ? "Action non prise en charge." : stats.Level<spec.Level ? "Niveau insuffisant pour cette action."
            : kind==CraftActionKind.Manipulation && !stats.ManipulationAllowed ? "Utilisation de Manipulation non confirmée dans le scénario."
            : (kind is CraftActionKind.MuscleMemory or CraftActionKind.Reflect or CraftActionKind.TrainedEye) && state.Step!=0 ? "Action réservée au premier tour."
            : kind==CraftActionKind.TrainedEye && stats.Level<recipeLevel+10 ? "Main preste exige dix niveaux de plus que la recette."
            : (kind is CraftActionKind.PrudentSynthesis or CraftActionKind.PrudentTouch) && state.WasteNot>0 ? "Action incompatible avec Parcimonie."
            : kind==CraftActionKind.TrainedFinesse && state.InnerQuiet!=10 ? "Dix degrés de Calme intérieur requis."
            : kind==CraftActionKind.TrainedPerfection && state.UsedTrainedPerfection ? "Main suprême utilisable une seule fois par synthèse."
            : kind==CraftActionKind.Byregot && state.InnerQuiet==0 ? "Calme intérieur requis pour Byregot."
            : quality && p.Quality==0 && kind!=CraftActionKind.DelicateSynthesis ? "Cette recette ne possède pas de cible de qualité."
            : state.Cp<cp ? "PS insuffisants pour cette action." : null;
        if(error is not null) return null;
        var durabilityCost=state.TrainedPerfection ? 0:state.WasteNot>0 ? (spec!.Durability+1)/2:spec!.Durability;
        var efficiency=kind switch
        {
            CraftActionKind.BasicSynthesis=>stats.Level>=31 ? 120:100,
            CraftActionKind.CarefulSynthesis=>stats.Level>=82 ? 180:150,
            CraftActionKind.Groundwork=>(stats.Level>=86 ? 360:300)/(state.Durability<durabilityCost ? 2:1),
            CraftActionKind.MuscleMemory or CraftActionKind.Reflect=>300,
            CraftActionKind.BasicTouch or CraftActionKind.DelicateSynthesis or CraftActionKind.PrudentTouch or CraftActionKind.RefinedTouch or CraftActionKind.TrainedFinesse=>100,
            CraftActionKind.StandardTouch=>125,CraftActionKind.AdvancedTouch=>150,CraftActionKind.PreparatoryTouch=>200,CraftActionKind.PrudentSynthesis=>180,
            CraftActionKind.Byregot=>100+20*state.InnerQuiet,_=>0
        };
        var progressEfficiency=kind==CraftActionKind.DelicateSynthesis && stats.Level>=94 ? 150:efficiency;
        var pg=progress ? (int)((long)bp*progressEfficiency*(100+(state.Veneration>0 ? 50:0)+(state.MuscleMemory>0 ? 100:0))/10000):0;
        var conditionPercent=condition switch {CraftStepCondition.Good=>goodQualityPercent,CraftStepCondition.Excellent=>400,CraftStepCondition.Poor=>50,_=>100};
        var qg=quality ? (int)((long)bq*efficiency*(100+10*state.InnerQuiet)*(100+(state.Innovation>0 ? 50:0)+(state.GreatStrides>0 ? 100:0))*conditionPercent/100000000):0;
        if(kind==CraftActionKind.TrainedEye) qg=p.Quality-state.Quality;
        var durability=kind==CraftActionKind.ImmaculateMend ? p.Durability:kind==CraftActionKind.MastersMend ? Math.Min(p.Durability,state.Durability+30):state.Durability-durabilityCost;
        if(durability>0 && state.Manipulation>0 && kind!=CraftActionKind.Manipulation) durability=Math.Min(p.Durability,durability+5);
        var nextProgress=Math.Min(p.Progress,checked(state.Progress+pg)); var nextQuality=Math.Min(p.Quality,checked(state.Quality+qg));
        var iq=quality && stats.Level>=11 ? Math.Min(10,state.InnerQuiet+1):state.InnerQuiet;
        if(kind is CraftActionKind.Reflect or CraftActionKind.PreparatoryTouch || kind==CraftActionKind.RefinedTouch && state.TouchCombo==1) iq=Math.Min(10,state.InnerQuiet+2);
        if(kind==CraftActionKind.Byregot) iq=0;
        var old=state;
        state=new(state.Step+1,nextProgress,nextQuality,Math.Max(0,durability),state.Cp-cp,iq,
            kind==CraftActionKind.Veneration ? 4:Math.Max(0,state.Veneration-1),
            kind==CraftActionKind.Innovation ? 4:Math.Max(0,state.Innovation-1),
            kind==CraftActionKind.GreatStrides ? 3:quality ? 0:Math.Max(0,state.GreatStrides-1),
            kind==CraftActionKind.WasteNot ? 4:kind==CraftActionKind.WasteNot2 ? 8:Math.Max(0,state.WasteNot-1),
            kind==CraftActionKind.Manipulation ? 8:Math.Max(0,state.Manipulation-1),
            kind==CraftActionKind.MuscleMemory ? 5:progress ? 0:Math.Max(0,state.MuscleMemory-1),
            kind==CraftActionKind.BasicTouch ? 1:(kind==CraftActionKind.StandardTouch && state.TouchCombo==1 || kind==CraftActionKind.Observe) ? 2:0,
            nextProgress>=p.Progress ? CraftFinish.Completed:durability<=0 ? CraftFinish.Broken:CraftFinish.InProgress,
            kind==CraftActionKind.TrainedPerfection || state.TrainedPerfection && spec!.Durability==0,
            state.UsedTrainedPerfection || kind==CraftActionKind.TrainedPerfection);
        return new(kind,cp,state.Progress-old.Progress,state.Quality-old.Quality,state);
    }
}
