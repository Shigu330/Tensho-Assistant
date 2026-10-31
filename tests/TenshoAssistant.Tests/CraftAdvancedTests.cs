using TenshoAssistant.Craft;

internal static class CraftAdvancedTests
{
    private static RecipeDefinition Recipe(int level=100,int durability=500,int quality=100000)=>new(1,2,1,"Fixture",level,[],0,new(durability,100000,quality,100,100,100,100,0,0,0));
    private static readonly CraftStats Stats=new(100,1000,1000,1000,true);
    private static CraftSimulationResult Run(params CraftActionKind[] sequence)=>CraftSimulation.Run(Recipe(),Stats,0,sequence);
    private static void Check(bool condition,string why) {if(!condition)throw new Exception(why);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Advanced craft Delicate Synthesis gains independently upgrade progress at94",()=>
        {
            foreach(var level in new[]{93,94})
            {
                var result=CraftSimulation.Run(Recipe(),Stats with {Level=level},0,[CraftActionKind.DelicateSynthesis]);
                Check(result.Final.Progress==(level==93?102:153) && result.Final.Quality==135 && result.Final.InnerQuiet==1 && result.Final.Cp==968 && result.Final.Durability==490,"separate efficiencies");
            }
            var both=Run(CraftActionKind.MuscleMemory,CraftActionKind.Veneration,CraftActionKind.Innovation,CraftActionKind.GreatStrides,CraftActionKind.DelicateSynthesis);
            Check(both.Steps[4].ProgressGain==382 && both.Steps[4].QualityGain==337 && both.Final.MuscleMemory==0 && both.Final.GreatStrides==0,"consume both one-use effects independently");
            var noQuality=CraftSimulation.Run(Recipe(quality:0),Stats,0,[CraftActionKind.DelicateSynthesis]);
            Check(noQuality.Error is null && noQuality.Final.Progress==153 && noQuality.Final.Quality==0 && noQuality.Final.InnerQuiet==1,"progress and IQ remain valid with quality capped at zero");
        }),
        ("Advanced craft Preparatory and Refined Touch stack bonuses respect exact combo",()=>
        {
            var prep=Run(CraftActionKind.PreparatoryTouch);
            Check(prep.Final.InnerQuiet==2 && prep.Final.Quality==270 && prep.Final.Durability==480 && prep.Final.Cp==960,"preparatory two stacks 20durability 40CP");
            var combo=Run(CraftActionKind.BasicTouch,CraftActionKind.RefinedTouch,CraftActionKind.AdvancedTouch);
            Check(combo.Steps[1].After.InnerQuiet==3 && combo.Steps[1].QualityGain==148 && combo.Steps[2].CpCost==46,"Refined bonus after Basic, no Advanced discount");
            Check(Run(CraftActionKind.RefinedTouch).Final.InnerQuiet==1,"no bonus without Basic");
            Check(Run(CraftActionKind.BasicTouch,CraftActionKind.Observe,CraftActionKind.RefinedTouch).Final.InnerQuiet==2,"Observe breaks refined combo");
        }),
        ("Advanced craft Prudent actions save durability but reject either Waste Not",()=>
        {
            var result=Run(CraftActionKind.PrudentSynthesis,CraftActionKind.PrudentTouch);
            Check(result.Final.Durability==490 && result.Final.Progress==183 && result.Final.Quality==135 && result.Final.Cp==957,"five durability each with distinct costs");
            foreach(var buff in new[]{CraftActionKind.WasteNot,CraftActionKind.WasteNot2})
            foreach(var action in new[]{CraftActionKind.PrudentSynthesis,CraftActionKind.PrudentTouch})
            {
                var rejected=Run(buff,action);
                Check(rejected.InvalidAction==1 && rejected.Final==rejected.Steps[0].After,"no resource debit on incompatible action");
            }
        }),
        ("Advanced craft Trained Eye requires opening ten level gap and grants one IQ",()=>
        {
            var gap=CraftSimulation.Run(Recipe(90),Stats,0,[CraftActionKind.TrainedEye]);
            Check(gap.Final.Quality==100000 && gap.Final.InnerQuiet==1 && gap.Final.Cp==750 && gap.Final.Durability==500,"maximum quality no durability cost");
            Check(CraftSimulation.Run(Recipe(91),Stats,0,[CraftActionKind.TrainedEye]).InvalidAction==0,"nine levels insufficient");
            Check(CraftSimulation.Run(Recipe(90),Stats,0,[CraftActionKind.Observe,CraftActionKind.TrainedEye]).InvalidAction==1,"first step only");
            Check(CraftSimulation.Run(Recipe(69),Stats with {Level=79},0,[CraftActionKind.TrainedEye]).InvalidAction==0,"level80 unlock required");
        }),
        ("Advanced craft Trained Finesse requires ten IQ and consumes neither stacks nor durability",()=>
        {
            Check(Run(CraftActionKind.TrainedFinesse).InvalidAction==0,"no IQ rejected");
            var sequence=Enumerable.Repeat(CraftActionKind.PreparatoryTouch,5).Append(CraftActionKind.TrainedFinesse).ToArray();
            var result=Run(sequence);
            Check(result.Steps[5].After.InnerQuiet==10 && result.Steps[5].After.Durability==400 && result.Steps[5].CpCost==32 && result.Steps[5].QualityGain==270,"ten stacks preserved");
        }),
        ("Advanced craft Immaculate Mend restores full durability including with Manipulation",()=>
        {
            var result=Run(CraftActionKind.PreparatoryTouch,CraftActionKind.PreparatoryTouch,CraftActionKind.PreparatoryTouch,CraftActionKind.ImmaculateMend);
            Check(result.Final.Durability==500 && result.Steps[3].CpCost==112,"full repair exceeds30");
            var manipulation=Run(CraftActionKind.Manipulation,CraftActionKind.BasicTouch,CraftActionKind.ImmaculateMend);
            Check(manipulation.Final.Durability==500,"cannot exceed max");
        }),
        ("Advanced craft Trained Perfection persists until positive base durability cost once per craft",()=>
        {
            var result=Run(CraftActionKind.TrainedPerfection,CraftActionKind.Observe,CraftActionKind.Innovation,CraftActionKind.ImmaculateMend,CraftActionKind.Groundwork,CraftActionKind.BasicTouch,CraftActionKind.TrainedPerfection);
            Check(result.Steps[3].After.TrainedPerfection && result.Steps[3].After.UsedTrainedPerfection,"zero cost actions do not consume pending protection");
            Check(!result.Steps[4].After.TrainedPerfection && result.Steps[4].After.Durability==500 && result.Steps[5].After.Durability==490,"one protected cost then normal cost");
            Check(result.InvalidAction==6 && result.Final==result.Steps[5].After,"one use even after consumption");
            var thin=CraftSimulation.Run(Recipe(durability:10),Stats,0,[CraftActionKind.TrainedPerfection,CraftActionKind.Groundwork]);
            Check(thin.Final.Durability==10 && thin.Final.Progress==367,"zero effective Groundwork cost avoids half efficiency");
            var incompatible=Run(CraftActionKind.TrainedPerfection,CraftActionKind.WasteNot,CraftActionKind.PrudentSynthesis);
            Check(incompatible.InvalidAction==2 && incompatible.Final.TrainedPerfection,"protection does not bypass forbidden WasteNot");
        }),
        ("Advanced craft solver replay and macro waits include all new actions",()=>
        {
            var recipe=Recipe(90,80,2000) with {Simulation=Recipe(90,80,2000).Simulation! with {Progress=200}};
            var result=CraftSolver.Solve(recipe,Stats,0,new(2000,20,128,100000));
            Check(result.TargetReached && result.Simulation?.Final.Finish==CraftFinish.Completed,"advanced palette search succeeds");
            Check(CraftMacroExporter.BaseWait(CraftActionKind.TrainedPerfection)==3 && CraftMacroExporter.BaseWait(CraftActionKind.ImmaculateMend)==3,"not shared buff timing2");
            var a=new CraftSimState(1,0,0,80,100,0,TrainedPerfection:true,UsedTrainedPerfection:true);
            Check(a!=a with {TrainedPerfection=false} && a!=a with {UsedTrainedPerfection=false},"both protection flags distinguish search states");
        })
    };
}
