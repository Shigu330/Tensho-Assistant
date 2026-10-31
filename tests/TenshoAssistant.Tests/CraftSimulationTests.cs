using TenshoAssistant.Craft;

internal static class CraftSimulationTests
{
    private static RecipeDefinition Recipe(CraftRecipeParameters? p=null,int level=100)=>new(1,2,1,"Test",level,[],0,p ?? new(100,100000,100000,100,100,100,100,0,0,50000));
    private static readonly CraftStats Stats=new(100,1000,1000,1000,true);
    private static CraftSimulationResult Run(params CraftActionKind[] actions)=>CraftSimulation.Run(Recipe(),Stats,0,actions);
    private static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
    private static void Throws<T>(Action run) where T:Exception { try {run();} catch(T) {return;} throw new Exception(typeof(T).Name); }
    public static IEnumerable<(string Name,Action Run)> Cases => new (string,Action)[]
    {
        ("Craft base gains truncate before action multipliers and level modifier boundary",()=>
        {
            var p=new CraftRecipeParameters(100,100000,100000,100,100,80,70,0,0,0);
            var same=CraftSimulation.Run(Recipe(p),Stats,0,[CraftActionKind.BasicSynthesis,CraftActionKind.BasicTouch]);
            Check(same.BaseProgress==81 && same.BaseQuality==94 && same.Steps[0].ProgressGain==97 && same.Steps[1].QualityGain==94,"floor 102*.8, 135*.7 before action");
            var over=CraftSimulation.Run(Recipe(p,99),Stats,0,[CraftActionKind.BasicSynthesis]);
            Check(over.BaseProgress==102 && over.BaseQuality==135 && over.Final.Progress==122,"overlevel removes recipe modifier");
        }),
        ("Craft synthesis traits upgrade at native levels",()=>
        {
            foreach(var (level,kind,expected) in new[]{(30,CraftActionKind.BasicSynthesis,102),(31,CraftActionKind.BasicSynthesis,122),
                (81,CraftActionKind.CarefulSynthesis,153),(82,CraftActionKind.CarefulSynthesis,183),(85,CraftActionKind.Groundwork,306),(86,CraftActionKind.Groundwork,367)})
                Check(CraftSimulation.Run(Recipe(),Stats with {Level=level},0,[kind]).Final.Progress==expected,"trait boundary");
        }),
        ("Craft touch combo requires full chain or Observe",()=>
        {
            var chain=Run(CraftActionKind.BasicTouch,CraftActionKind.StandardTouch,CraftActionKind.AdvancedTouch);
            Check(chain.Steps.Select(s=>s.CpCost).SequenceEqual(new[]{18,18,18}) && chain.Final.Quality==563 && chain.Final.InnerQuiet==3,"full combo costs and IQ gain");
            var standalone=Run(CraftActionKind.StandardTouch,CraftActionKind.AdvancedTouch);
            Check(standalone.Steps.Select(s=>s.CpCost).SequenceEqual(new[]{32,46}),"standalone Standard is not discounted Advanced combo");
            Check(Run(CraftActionKind.Observe,CraftActionKind.AdvancedTouch).Steps[1].CpCost==18,"Observe enables discount");
            Check(Run(CraftActionKind.BasicTouch,CraftActionKind.Innovation,CraftActionKind.StandardTouch).Steps[2].CpCost==32,"intervening buff breaks combo");
        }),
        ("Craft additive quality buffs and one-use Great Strides",()=>
        {
            var result=Run(CraftActionKind.Innovation,CraftActionKind.GreatStrides,CraftActionKind.BasicTouch,CraftActionKind.BasicTouch);
            Check(result.Steps[2].QualityGain==337 && result.Steps[3].QualityGain==222,"additive 250%, then 150% with IQ1");
            Check(result.Steps[2].After.GreatStrides==0 && result.Final.Innovation==1,"consumption and subsequent steps");
        }),
        ("Craft progress buffs add and Muscle Memory expires or is consumed",()=>
        {
            var result=Run(CraftActionKind.MuscleMemory,CraftActionKind.Veneration,CraftActionKind.BasicSynthesis,CraftActionKind.BasicSynthesis);
            Check(result.Steps[0].ProgressGain==306 && result.Steps[2].ProgressGain==306 && result.Steps[3].ProgressGain==183,"300% opening then additive 250% and 150%");
            var expire=Run(CraftActionKind.MuscleMemory,CraftActionKind.Observe,CraftActionKind.Observe,CraftActionKind.Observe,CraftActionKind.Observe,CraftActionKind.Observe,CraftActionKind.BasicSynthesis);
            Check(expire.Steps[5].After.MuscleMemory==0 && expire.Steps[6].ProgressGain==122,"five subsequent steps");
        }),
        ("Craft effect duration covers exactly subsequent turns",()=>
        {
            var result=Run(CraftActionKind.Veneration,CraftActionKind.Observe,CraftActionKind.Observe,CraftActionKind.Observe,CraftActionKind.BasicSynthesis,CraftActionKind.BasicSynthesis);
            Check(result.Steps[0].After.Veneration==4 && result.Steps[4].ProgressGain==183 && result.Steps[5].ProgressGain==122,"4 turns plus casting turn");
        }),
        ("Craft Reflect Byregot and Inner Quiet cap",()=>
        {
            var result=Run(CraftActionKind.Reflect,CraftActionKind.Byregot,CraftActionKind.BasicTouch);
            Check(result.Steps[0].QualityGain==405 && result.Steps[0].After.InnerQuiet==2 && result.Steps[1].QualityGain==226 && result.Steps[1].After.InnerQuiet==0 && result.Steps[2].QualityGain==135,"pre-stack efficiency and consumption");
            var p=Recipe().Simulation! with {Durability=1000};
            var cap=CraftSimulation.Run(Recipe(p),Stats,0,Enumerable.Repeat(CraftActionKind.BasicTouch,15));
            Check(cap.Final.InnerQuiet==10,"IQ cap");
            Check(CraftSimulation.Run(Recipe(),Stats with {Level=10},0,[CraftActionKind.BasicTouch]).Final.InnerQuiet==0,"no IQ before level11");
        }),
        ("Craft Waste Not replaces duration and Groundwork uses effective cost",()=>
        {
            var replacement=Run(CraftActionKind.WasteNot2,CraftActionKind.WasteNot,CraftActionKind.BasicTouch);
            Check(replacement.Steps[1].After.WasteNot==4 && replacement.Final.Durability==95,"replacement not stacking");
            var p=Recipe().Simulation! with {Durability=10};
            var full=CraftSimulation.Run(Recipe(p),Stats,0,[CraftActionKind.WasteNot,CraftActionKind.Groundwork]);
            var half=CraftSimulation.Run(Recipe(p),Stats,0,[CraftActionKind.Groundwork]);
            Check(full.Final.Progress==367 && half.Final.Progress==183,"10 effective cost vs20 threshold");
        }),
        ("Craft Manipulation skips cast recast and cannot rescue zero durability",()=>
        {
            var result=Run(CraftActionKind.BasicTouch,CraftActionKind.Manipulation,CraftActionKind.Manipulation,CraftActionKind.BasicTouch,CraftActionKind.Observe,CraftActionKind.MastersMend);
            Check(result.Steps[1].After.Durability==90 && result.Steps[2].After.Durability==90 && result.Steps[3].After.Durability==85 && result.Steps[4].After.Durability==90 && result.Final.Durability==100,"restore after subsequent valid steps and cap");
            var p=Recipe().Simulation! with {Durability=10};
            var broken=CraftSimulation.Run(Recipe(p),Stats,0,[CraftActionKind.Manipulation,CraftActionKind.BasicTouch,CraftActionKind.MastersMend]);
            Check(broken.Final.Finish==CraftFinish.Broken && broken.Final.Durability==0 && broken.Steps.Count==2,"no resurrection at zero");
        }),
        ("Craft completion at zero durability wins and stops future actions",()=>
        {
            var p=Recipe().Simulation! with {Durability=10,Progress=100,Quality=100,MaxStartingQuality=20};
            var done=CraftSimulation.Run(Recipe(p),Stats,20,[CraftActionKind.BasicSynthesis,CraftActionKind.BasicTouch]);
            Check(done.Final.Finish==CraftFinish.Completed && done.Final.Durability==0 && done.Final.Progress==100 && done.Final.Quality==20 && done.Steps.Count==1 && done.PlannedActions==2,"completion and clamping");
        }),
        ("Craft invalid actions leave state unchanged with first failure index",()=>
        {
            foreach(var actions in new[]{new[]{CraftActionKind.Byregot},new[]{CraftActionKind.BasicTouch,CraftActionKind.Reflect},new[]{(CraftActionKind)999}})
            {
                var result=Run(actions);
                Check(result.Error is not null && result.InvalidAction==actions.Length-1 && result.Final.Step==actions.Length-1,"first failure preserved");
            }
            var cp=CraftSimulation.Run(Recipe(),Stats with {Cp=17},0,[CraftActionKind.BasicTouch]);
            Check(cp.Final.Cp==17 && cp.Final.Durability==100 && cp.Steps.Count==0,"no partial debit");
            Check(CraftSimulation.Run(Recipe(),Stats with {ManipulationAllowed=false},0,[CraftActionKind.Manipulation]).InvalidAction==0,"unlock manual");
        }),
        ("Craft unsupported recipes invalid parameters bounds and cancellation fail closed",()=>
        {
            var p=Recipe().Simulation!;
            foreach(var bad in new[]{p with {Expert=true},p with {LevelScaled=true},p with {Specialist=true},p with {Collectable=true,Quality=0},p with {RequiredQuality=1},p with {ProgressDivider=0}})
                Throws<ArgumentException>(()=>CraftSimulation.Run(Recipe(bad),Stats,0,[]));
            Throws<ArgumentException>(()=>CraftSimulation.Run(Recipe(p with {RequiredCraftsmanship=1001}),Stats,0,[]));
            Throws<ArgumentOutOfRangeException>(()=>CraftSimulation.Run(Recipe(),Stats,-1,[]));
            Throws<ArgumentOutOfRangeException>(()=>CraftSimulation.Run(Recipe(),Stats,50001,[]));
            Throws<ArgumentException>(()=>Run(Enumerable.Repeat(CraftActionKind.Observe,101).ToArray()));
            using var cancel=new CancellationTokenSource(); cancel.Cancel();
            Throws<OperationCanceledException>(()=>CraftSimulation.Run(Recipe(),Stats,0,[],cancel.Token));
        }),
        ("Craft immutable sequence result and quality clamp",()=>
        {
            var sequence=new List<CraftActionKind>{CraftActionKind.Reflect};
            var p=Recipe().Simulation! with {Quality=100,MaxStartingQuality=50};
            var result=CraftSimulation.Run(Recipe(p),Stats,50,sequence); sequence.Clear();
            Check(result.Steps.Count==1 && result.Final.Quality==100 && result.Steps[0].QualityGain==50,"copied immutable output and actual capped gain");
            Throws<NotSupportedException>(()=>((IList<CraftSimStep>)result.Steps).Clear());
        }),
        ("Craft action catalog requires all eight native job variants",()=>
        {
            var rows=CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8).Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,"Test",1,"Native description"))).ToArray();
            var catalog=new CraftActionCatalog(rows,"fixture");
            Check(catalog.Find(15,CraftActionKind.Reflect)?.Spec.Cp==6 && catalog.Find(16,CraftActionKind.Reflect) is null,"job scoped");
            Throws<ArgumentException>(()=>new CraftActionCatalog(rows.Skip(1),"bad"));
            var duplicate=rows.ToArray(); duplicate[0]=duplicate[1]; Throws<ArgumentException>(()=>new CraftActionCatalog(duplicate,"bad"));
            var service=new CraftActionCatalogService(); service.Publish(catalog); service.Fail(new Exception("changed")); Check(service.State.Catalog is null,"no stale metadata after failure");
        })
    };
}
