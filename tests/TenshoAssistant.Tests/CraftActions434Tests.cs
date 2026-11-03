using TenshoAssistant.Craft;

// A-13 (0.434): Final Appraisal, Careful Observation, Heart and Soul, Quick Innovation, Intensive Synthesis, Precise Touch, Tricks of the
// Trade, Rapid Synthesis and Hasty Touch in the simulator (game sheets for level / CP; effects as Craftimizer models them, read only).
internal static class CraftActions434Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    // Base progress 1000×10/100+2 = 102, base quality 1000×10/100+35 = 135 (recipe level 5, no modifier).
    private static RecipeDefinition Recipe(int progress=1000,int quality=5000)=>new(1,2,1,"Fixture",5,[],0,new(40,progress,quality,100,100,100,100,0,0,0));
    private static readonly CraftStats Specialist=new(100,1000,1000,500,false,true);
    private static CraftSimulationResult Run(IEnumerable<CraftActionKind> actions,CraftStats? stats=null,int progress=1000)=>CraftSimulation.Run(Recipe(progress),stats ?? Specialist,0,actions);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("A-13: specs from the game sheets; Daring Touch left out",() =>
        {
            CraftActionSpec Spec(CraftActionKind kind)=>CraftActionCatalog.Specs.Single(spec=>spec.Kind==kind);
            Check(CraftActionCatalog.Specs.Count==35,"26 + 9 actions");
            Check(Spec(CraftActionKind.FinalAppraisal)==new CraftActionSpec(CraftActionKind.FinalAppraisal,19012,42,1,0)
                && Spec(CraftActionKind.IntensiveSynthesis)==new CraftActionSpec(CraftActionKind.IntensiveSynthesis,100315,78,6,10)
                && Spec(CraftActionKind.PreciseTouch)==new CraftActionSpec(CraftActionKind.PreciseTouch,100128,53,18,10)
                && Spec(CraftActionKind.QuickInnovation)==new CraftActionSpec(CraftActionKind.QuickInnovation,100459,96,0,0),"docs/evidence/0.434-craft-actions.md");
            Check(!Enum.GetNames<CraftActionKind>().Contains("DaringTouch"),"Daring Touch: ClassJob 0 in the sheet, chance-based");
        }),
        ("A-13: Final Appraisal stops one short, does not count as a step, keeps the opener",() =>
        {
            var result=Run([CraftActionKind.FinalAppraisal,CraftActionKind.MuscleMemory],progress:300);
            Check(result.Error is null && result.Steps[0].After is { Step:0, Durability:40, FinalAppraisal:5 },"no step, no durability");
            Check(result.Steps[1].After is { Progress:299, FinalAppraisal:0, Finish:CraftFinish.InProgress },"stops at 299 and is spent");
            Check(Run([CraftActionKind.BasicSynthesis,CraftActionKind.FinalAppraisal,CraftActionKind.BasicSynthesis]).Steps[2].After.FinalAppraisal==4,"shortened by steps only");
        }),
        ("A-13: Intensive, Precise and Tricks need a good condition or Heart and Soul; specialist actions need the specialist",() =>
        {
            var normal=Run([CraftActionKind.IntensiveSynthesis]);
            Check(normal.InvalidAction==0 && normal.Error=="Good or Excellent condition, or Heart and Soul, required.","normal condition refused");
            Check(Run([CraftActionKind.HeartAndSoul],Specialist with {Specialist=false}).Error=="Specialist action: tick Specialist in the attributes.","specialist only");
            var hs=Run([CraftActionKind.HeartAndSoul,CraftActionKind.IntensiveSynthesis,CraftActionKind.HeartAndSoul]);
            Check(hs.Steps[1].ProgressGain==408 && hs.Steps[1].After is { HeartAndSoul:false, UsedHeartAndSoul:true } && hs.InvalidAction==2,"400 % progress, spent, once");
            var good=CraftSimulation.Apply(Recipe().Simulation!,Specialist,5,102,135,new CraftSimState(1,0,0,40,500,0,HeartAndSoul:true),CraftActionKind.PreciseTouch,out var error,CraftStepCondition.Good);
            Check(error is null && good!.After.HeartAndSoul && good.QualityGain==303 && good.After.InnerQuiet==2,"good condition: Heart and Soul kept, 150 % × 1.5, two Inner Quiet");
            var tricks=Run([CraftActionKind.Innovation,CraftActionKind.Veneration,CraftActionKind.HeartAndSoul,CraftActionKind.TricksOfTheTrade]);
            Check(tricks.Final.Cp==484 && Run([CraftActionKind.HeartAndSoul,CraftActionKind.TricksOfTheTrade]).Final.Cp==500,"+20 CP, capped at the maximum");
        }),
        ("A-13: Quick Innovation once, for the next step; Rapid Synthesis by level; chance actions out of the search",() =>
        {
            var quick=Run([CraftActionKind.QuickInnovation,CraftActionKind.BasicTouch]);
            Check(quick.Steps[1].QualityGain==202 && quick.Steps[1].After.Innovation==0,"Innovation for the next step only");
            Check(Run([CraftActionKind.QuickInnovation,CraftActionKind.BasicTouch,CraftActionKind.QuickInnovation]).InvalidAction==2
                && Run([CraftActionKind.Innovation,CraftActionKind.QuickInnovation]).InvalidAction==1,"once, never over Innovation");
            Check(Run([CraftActionKind.RapidSynthesis]).Steps[0].ProgressGain==510 && Run([CraftActionKind.RapidSynthesis],Specialist with {Level=62}).Steps[0].ProgressGain==255,"500 % from level 63, else 250 %");
            Check(CraftSimulation.Random(CraftActionKind.RapidSynthesis) && CraftSimulation.Random(CraftActionKind.HastyTouch) && !CraftSimulation.Random(CraftActionKind.PreciseTouch),"chance actions");
            var solver=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Craft/CraftSolver.cs"));
            Check(solver.Contains("!CraftSimulation.Random(spec.Kind) && spec.Kind is not (CraftActionKind.CarefulObservation or CraftActionKind.FinalAppraisal)"),"search leaves them out");
        }),
    ];
}
