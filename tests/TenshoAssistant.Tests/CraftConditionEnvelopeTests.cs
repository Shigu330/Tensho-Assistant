using TenshoAssistant.Craft;

internal static class CraftConditionEnvelopeTests
{
    private static readonly RecipeDefinition Recipe=new(1,2,1,"Menuisier",100,[],0,new(80,10000,10000,100,100,100,100,0,0,0));
    private static readonly CraftStats Stats=new(100,1000,1000,500,true);
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    // Exhaustive path enumeration, with independently written ordinary-condition edges.
    private static IReadOnlyList<CraftRange> Enumerate(RecipeDefinition recipe,CraftStats stats,int quality,CraftActionKind[] actions,int percent)
    {
        var baseline=CraftSimulation.Run(recipe,stats,quality,actions);
        var states=new List<(CraftStepCondition Condition,CraftSimState State)>{(CraftStepCondition.Normal,new(0,0,quality,recipe.Simulation!.Durability,stats.Cp,0))};
        var ranges=new List<CraftRange>();
        foreach(var action in baseline.Steps)
        {
            var next=new List<(CraftStepCondition,CraftSimState)>();var qualities=new List<int>();
            foreach(var (condition,state) in states)
            {
                var step=CraftSimulation.Apply(recipe.Simulation,stats,recipe.Level,baseline.BaseProgress,baseline.BaseQuality,state,action.Action,out _,condition,percent)!;
                qualities.Add(step.After.Quality);
                if(condition==CraftStepCondition.Excellent)next.Add((CraftStepCondition.Poor,step.After));
                else if(condition!=CraftStepCondition.Normal)next.Add((CraftStepCondition.Normal,step.After));
                else {next.Add((CraftStepCondition.Normal,step.After));next.Add((CraftStepCondition.Good,step.After));next.Add((CraftStepCondition.Excellent,step.After));}
            }
            ranges.Add(new(qualities.Min(),qualities.Max()));states=next;
        }
        return ranges;
    }
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Condition envelope starts Normal rather than assigning an impossible first Excellent",()=>
        {
            Check(CraftConditionEnvelope.Evaluate(Recipe,Stats,0,[]).Quality==new CraftRange(0,0),"empty");
            var result=CraftConditionEnvelope.Evaluate(Recipe,Stats,0,[CraftActionKind.BasicTouch]);
            Check(result.Quality==new CraftRange(135,135) && result.Normal.Final.Quality==135,"first normal");
        }),
        ("Excellent buff step can force Poor on the next quality action",()=>
        {
            var result=CraftConditionEnvelope.Evaluate(Recipe,Stats,0,[CraftActionKind.Observe,CraftActionKind.Observe,CraftActionKind.BasicTouch]);
            Check(result.Quality==new CraftRange(67,540) && result.Normal.Final.Quality==135,"forced Poor path and exact one-floor bounds");
        }),
        ("Compressed condition bounds match exhaustive coherent paths including buffs combos and caps",()=>
        {
            CraftActionKind[][] sequences=[
                [CraftActionKind.BasicTouch,CraftActionKind.StandardTouch,CraftActionKind.AdvancedTouch,CraftActionKind.Byregot],
                [CraftActionKind.Reflect,CraftActionKind.Innovation,CraftActionKind.GreatStrides,CraftActionKind.PreparatoryTouch,CraftActionKind.Byregot,CraftActionKind.BasicSynthesis],
                [CraftActionKind.WasteNot,CraftActionKind.BasicTouch,CraftActionKind.RefinedTouch,CraftActionKind.DelicateSynthesis,CraftActionKind.MastersMend,CraftActionKind.BasicTouch,CraftActionKind.Byregot],
                [CraftActionKind.Observe,CraftActionKind.BasicTouch,CraftActionKind.Innovation,CraftActionKind.StandardTouch,CraftActionKind.Observe,CraftActionKind.AdvancedTouch,CraftActionKind.Byregot]
            ];
            foreach(var recipe in new[]{Recipe,Recipe with {Simulation=Recipe.Simulation! with {Quality=300,MaxStartingQuality=100}}})
            foreach(var actions in sequences)
            foreach(var percent in new[]{150,175})
            {
                var result=CraftConditionEnvelope.Evaluate(recipe,Stats,0,actions,percent);var expected=Enumerate(recipe,Stats,0,actions,percent);
                Check(result.Steps.Select(row=>row.Quality).SequenceEqual(expected),"enumeration equality");
                Check(result.Normal.Steps.Select(row=>row.After.Quality).Zip(result.Steps,(value,band)=>value>=band.Quality.Min && value<=band.Quality.Max).All(value=>value),"all-normal path remains inside every band");
            }
        }),
        ("Undeclared tool scenario conservatively includes both modeled favorable factors",()=>
        {
            CraftActionKind[] actions=[CraftActionKind.Reflect,CraftActionKind.Observe,CraftActionKind.BasicTouch,CraftActionKind.StandardTouch,CraftActionKind.AdvancedTouch,CraftActionKind.Byregot];
            var unknown=CraftConditionEnvelope.Evaluate(Recipe,Stats,0,actions);
            var ordinary=CraftConditionEnvelope.Evaluate(Recipe,Stats,0,actions,150);var enhanced=CraftConditionEnvelope.Evaluate(Recipe,Stats,0,actions,175);
            Check(unknown.GoodQualityPercent is null,"no tool fact claimed");
            for(var i=0;i<unknown.Steps.Count;i++)Check(unknown.Steps[i].Quality==new CraftRange(Math.Min(ordinary.Steps[i].Quality.Min,enhanced.Steps[i].Quality.Min),Math.Max(ordinary.Steps[i].Quality.Max,enhanced.Steps[i].Quality.Max)),"bounded by constant-tool scenarios");
        }),
        ("Condition envelope preserves completion tail invalid action and break rather than claiming success",()=>
        {
            var easy=Recipe with {Simulation=Recipe.Simulation! with {Progress=1}};
            var done=CraftConditionEnvelope.Evaluate(easy,Stats,0,[CraftActionKind.BasicSynthesis,CraftActionKind.BasicTouch]);
            Check(done.Normal.Final.Finish==CraftFinish.Completed && done.Normal.PlannedActions==2 && done.Steps.Count==1 && done.Quality==new CraftRange(0,0),"completed tail");
            var invalid=CraftConditionEnvelope.Evaluate(Recipe,Stats,0,[CraftActionKind.Byregot]);
            Check(invalid.Steps.Count==0 && invalid.Normal.InvalidAction==0 && invalid.Normal.Error is not null,"invalid retained");
            var broken=CraftConditionEnvelope.Evaluate(Recipe with {Simulation=Recipe.Simulation! with {Durability=10}},Stats,0,[CraftActionKind.BasicTouch,CraftActionKind.BasicTouch]);
            Check(broken.Normal.Final.Finish==CraftFinish.Broken && broken.Steps.Count==1,"break retained");
        }),
        ("Trained Eye quality remains capped for every envelope condition",()=>
        {
            var result=CraftConditionEnvelope.Evaluate(Recipe with {Level=90},Stats,0,[CraftActionKind.TrainedEye,CraftActionKind.Observe,CraftActionKind.BasicTouch]);
            Check(result.Steps.All(row=>row.Quality==new CraftRange(10000,10000)),"max remains max");
        }),
        ("Condition envelope rejects unsupported tool factors honors cancellation and owns its result",()=>
        {
            var rejected=false;try{CraftConditionEnvelope.Evaluate(Recipe,Stats,0,[],160);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"factor");
            using var source=new CancellationTokenSource();source.Cancel();rejected=false;
            try{CraftConditionEnvelope.Evaluate(Recipe,Stats,0,[],cancellation:source.Token);}catch(OperationCanceledException){rejected=true;}Check(rejected,"cancel");
            var actions=new[]{CraftActionKind.BasicTouch};var result=CraftConditionEnvelope.Evaluate(Recipe,Stats,0,actions);actions[0]=CraftActionKind.Observe;
            Check(result.Normal.Steps[0].Action==CraftActionKind.BasicTouch && result.Steps is not CraftConditionBand[],"detached immutable result");
        })
    };
}
