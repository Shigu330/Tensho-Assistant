namespace TenshoAssistant.Craft;

/// <summary>
/// A-03 (0.465, DEC-028): Tensho's crafting actions and the numbers of Raphael.Net 5.0.0's action enum (raphael-bindings/src/lib.rs, docs/evidence/0.451-raphael-study.md). The pool is the
/// same as Tensho's search (level, Manipulation, forbidden actions, nothing left to chance, nothing that needs a condition), and specialist actions only for a specialist.
/// </summary>
public static class RaphaelActions
{
    /// <summary>
    /// 0.782 (X-25, the game froze on 2026-11-07 15:18:45): Raphael's native code divides by the base quality gain (quality_upper_bound_solver/state.rs:59,
    /// « attempt to divide by zero ») and by the others; a panic there cannot be caught and stops the game. Any value it cannot take is refused here, before the
    /// call: zero gains (a recipe without quality, a stat read as 0), no durability or progress, or a number past what its 16-bit fields hold. Null: safe to call.
    /// </summary>
    public static string? InputRefusal(int baseProgress,int baseQuality,int durability,int progress,int cp,int quality)
    {
        if(baseProgress<=0)return "base progress gain is 0";
        if(baseQuality<=0)return "base quality gain is 0";
        if(durability<5)return "durability below 5";
        if(progress<=0)return "progress is 0";
        if(cp<0 || quality<0)return "negative CP or quality";
        if(new[]{baseProgress,baseQuality,durability,progress,cp,quality}.Any(value=>value>ushort.MaxValue))return "a value past 65535";
        return null;
    }

    private static readonly IReadOnlyDictionary<CraftActionKind,int> Numbers=new Dictionary<CraftActionKind,int>
    {
        [CraftActionKind.BasicSynthesis]=0,[CraftActionKind.BasicTouch]=1,[CraftActionKind.MastersMend]=2,[CraftActionKind.Observe]=3,[CraftActionKind.WasteNot]=5,
        [CraftActionKind.Veneration]=6,[CraftActionKind.StandardTouch]=7,[CraftActionKind.GreatStrides]=8,[CraftActionKind.Innovation]=9,[CraftActionKind.WasteNot2]=10,
        [CraftActionKind.Byregot]=11,[CraftActionKind.MuscleMemory]=13,[CraftActionKind.CarefulSynthesis]=14,[CraftActionKind.Manipulation]=15,[CraftActionKind.PrudentTouch]=16,
        [CraftActionKind.AdvancedTouch]=17,[CraftActionKind.Reflect]=18,[CraftActionKind.PreparatoryTouch]=19,[CraftActionKind.Groundwork]=20,[CraftActionKind.DelicateSynthesis]=21,
        [CraftActionKind.TrainedEye]=23,[CraftActionKind.HeartAndSoul]=24,[CraftActionKind.PrudentSynthesis]=25,[CraftActionKind.TrainedFinesse]=26,[CraftActionKind.RefinedTouch]=27,
        [CraftActionKind.QuickInnovation]=28,[CraftActionKind.ImmaculateMend]=29,[CraftActionKind.TrainedPerfection]=30,
    };

    public static int? Number(CraftActionKind kind)=>Numbers.TryGetValue(kind,out var number) ? number:null;

    /// <summary>The Tensho action of a Raphael number, or null for one the simulator does not model (the macro is then refused).</summary>
    public static CraftActionKind? Kind(int number)
    {
        foreach(var (kind,value) in Numbers)if(value==number)return kind;
        return null;
    }

    /// <summary>
    /// The actions Raphael may use for these attributes. Raphael does not know the recipe's level: 0.466 (in-game campaign): Trained Eye, which needs ten levels more than the recipe, is left out
    /// here, or it gave a first step the game and the simulator refuse. Heart and Soul only serves actions that are not in the pool.
    /// </summary>
    public static IReadOnlyList<CraftActionKind> Pool(CraftStats stats,string forbidden,int recipeLevel)
        =>CraftActionCatalog.Specs.Where(spec=>Numbers.ContainsKey(spec.Kind) && spec.Level<=stats.Level
                && spec.Kind!=CraftActionKind.HeartAndSoul
                && (spec.Kind!=CraftActionKind.TrainedEye || stats.Level>=recipeLevel+10)
                && (spec.Kind!=CraftActionKind.Manipulation || stats.ManipulationAllowed)
                && (spec.Kind!=CraftActionKind.QuickInnovation || stats.Specialist)
                && !CraftSimulation.Random(spec.Kind) && !CraftActionRules.Forbidden(forbidden,spec.Kind))
            .Select(spec=>spec.Kind).ToArray();

    /// <summary>
    /// Raphael's macro checked by Tensho's own simulator before it is proposed; null with the reason when it does not hold (an action Tensho does not model or that its simulator
    /// refuses, or a synthesis not finished).
    /// </summary>
    public static (CraftSimulationResult? Result,string? Problem) Replay(RecipeDefinition recipe,CraftStats stats,int startingQuality,IEnumerable<int> numbers)
    {
        var kinds=new List<CraftActionKind>();
        foreach(var number in numbers)
        {
            if(Kind(number) is not { } kind)return (null,$"Raphael used action {number}, which Tensho does not model.");
            kinds.Add(kind);
        }
        if(kinds.Count==0)return (null,"Raphael found no sequence.");
        var result=CraftSimulation.Run(recipe,stats,startingQuality,kinds);
        if(result.InvalidAction is { } invalid)return (null,$"Tensho's simulator refused step {invalid+1} of Raphael's sequence: {result.Error}");
        return result.Final.Finish==CraftFinish.Completed ? (result,null):(null,"Raphael's sequence does not finish the synthesis in Tensho's simulator.");
    }
}
