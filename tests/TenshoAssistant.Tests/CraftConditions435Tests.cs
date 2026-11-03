using TenshoAssistant.Craft;

// A-07 (0.435): expert recipes simulated (normal condition at every step), and the expert conditions' effects in the transition.
internal static class CraftConditions435Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly CraftRecipeParameters P=new(80,5000,5000,100,100,100,100,0,0,0,Expert:true);
    private static readonly CraftStats Stats=new(100,1000,1000,500,true);
    private static CraftSimStep Step(CraftActionKind kind,CraftStepCondition condition,CraftSimState? state=null)
        =>CraftSimulation.Apply(P,Stats,5,102,135,state ?? new CraftSimState(1,0,0,80,500,0),kind,out var error,condition) ?? throw new Exception(error);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("A-07: Pliant halves CP, Sturdy and Robust halve durability (with Waste Not too), Malleable × 1.5 progress",() =>
        {
            Check(Step(CraftActionKind.Innovation,CraftStepCondition.Pliant).CpCost==9 && Step(CraftActionKind.Observe,CraftStepCondition.Pliant).CpCost==4,"18 → 9, 7 → 4 (rounded up)");
            Check(Step(CraftActionKind.BasicSynthesis,CraftStepCondition.Sturdy).After.Durability==75 && Step(CraftActionKind.BasicSynthesis,CraftStepCondition.Robust).After.Durability==75,"10 → 5");
            Check(Step(CraftActionKind.BasicSynthesis,CraftStepCondition.Sturdy,new CraftSimState(1,0,0,80,500,0,WasteNot:3)).After.Durability==77,"10 → 2.5 → 3");
            Check(Step(CraftActionKind.Groundwork,CraftStepCondition.Sturdy).After.Durability==70,"20 → 10");
            Check(Step(CraftActionKind.BasicSynthesis,CraftStepCondition.Malleable).ProgressGain==183 && Step(CraftActionKind.BasicSynthesis,CraftStepCondition.Normal).ProgressGain==122,"122 × 1.5");
        }),
        ("A-07: Primed lengthens the buffs set this step; Centered and Good Omen change nothing here; expert recipes simulated",() =>
        {
            Check(Step(CraftActionKind.Innovation,CraftStepCondition.Primed).After.Innovation==6 && Step(CraftActionKind.Manipulation,CraftStepCondition.Primed).After.Manipulation==10,"+2");
            Check(Step(CraftActionKind.BasicTouch,CraftStepCondition.Centered).QualityGain==Step(CraftActionKind.BasicTouch,CraftStepCondition.Normal).QualityGain
                && Step(CraftActionKind.BasicTouch,CraftStepCondition.GoodOmen).QualityGain==135,"no gain change");
            var recipe=new RecipeDefinition(1,2,1,"Fixture",5,[],0,P);
            Check(CraftSimulation.Unsupported(recipe) is null && CraftSimulation.Run(recipe,Stats,0,[CraftActionKind.BasicSynthesis]).Error is null,"expert recipe simulated");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftSimulation.cs")).Contains("if(p.Expert)ImGui.TextColored(Accent,localization.Get(\"CraftSimulation.ExpertNormal\"));"),"said on screen");
        }),
    ];
}
