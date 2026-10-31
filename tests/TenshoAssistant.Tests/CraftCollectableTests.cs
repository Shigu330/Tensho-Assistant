using System.Numerics;
using TenshoAssistant.Character;
using TenshoAssistant.Craft;

internal static class CraftCollectableTests
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static void Throws<T>(Action action) where T:Exception{try{action();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static RecipeDefinition Recipe()=>new(123,456,1,"Menuiserie",100,[],0,new(80,100,10000,100,100,100,100,0,0,0,Collectable:true));
    private static readonly CraftStats Stats=new(100,1000,1000,500);
    private static CraftActionCatalog Catalog()=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,spec.Kind+" "+job,1,"Native"))),"fixture");
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Collectible quality projection preserves quality without inferring HQ eligibility",()=>
        {
            Check(CraftRecipeQuality.Maximum(false,true,0,12345,97)==11974,"collectible without HQ retains scaled quality with truncation");
            Check(CraftRecipeQuality.Maximum(false,false,0,12345,97)==0,"ordinary non quality recipe remains zero");
            Check(CraftRecipeQuality.Maximum(true,false,0,12345,97)==11974 && CraftRecipeQuality.Maximum(false,false,10,12345,97)==11974,"existing HQ and mandatory quality projection unchanged");
            Throws<ArgumentOutOfRangeException>(()=>CraftRecipeQuality.Maximum(false,true,0,-1,100));
            Throws<OverflowException>(()=>CraftRecipeQuality.Maximum(false,true,0,int.MaxValue,101));
        }),
        ("Ordinary collectibles reuse deterministic transitions search and macro export",()=>
        {
            var recipe=Recipe();var normal=recipe with {Simulation=recipe.Simulation! with {Collectable=false}};
            CraftActionKind[] sequence=[CraftActionKind.Reflect,CraftActionKind.GreatStrides,CraftActionKind.Byregot,CraftActionKind.BasicSynthesis];
            var collect=CraftSimulation.Run(recipe,Stats,0,sequence);var ordinary=CraftSimulation.Run(normal,Stats,0,sequence);
            Check(collect.Final==ordinary.Final && collect.Steps.SequenceEqual(ordinary.Steps) && collect.Final.Finish==CraftFinish.Completed,"collectible status does not alter ordinary action physics");
            var solved=CraftSolver.Solve(recipe,Stats,0,new(600,4,64,10000));
            Check(solved.Simulation is {Final.Finish:CraftFinish.Completed},"collector not rejected by search");
            var macro=CraftMacroExporter.Build(solved.Simulation!,8,Catalog());
            Check(macro.Actions==solved.Simulation!.Steps.Count && macro.Pages.All(page=>page.Lines.Count<=15),"completed collectible can be exported without changing line capacity");
        }),
        ("Great Strides doubles Byregot quality before rounding and consumes both effects on collectibles",()=>
        {
            var recipe=Recipe();var combined=CraftSimulation.Run(recipe,Stats,0,[CraftActionKind.Reflect,CraftActionKind.GreatStrides,CraftActionKind.Byregot]);
            var plain=CraftSimulation.Run(recipe,Stats,0,[CraftActionKind.Reflect,CraftActionKind.Byregot]);
            var iq=combined.Steps[0].After.InnerQuiet;
            var expected=(int)((BigInteger)combined.BaseQuality*(100+20*iq)*(100+10*iq)*2/10000);
            Check(combined.Error is null && combined.Steps[2].QualityGain==expected && expected>plain.Steps[1].QualityGain,"native modeled combo including IQ potency, with one final rounding");
            Check(combined.Final.InnerQuiet==0 && combined.Final.GreatStrides==0 && combined.Steps[1].After.GreatStrides==3,"Byregot consumes IQ and the next quality action consumes Great Strides");
            Check(CraftMacroExporter.BaseWait(CraftActionKind.GreatStrides)==2 && CraftMacroExporter.BaseWait(CraftActionKind.Byregot)==3,"export retains buff and action waits");
        }),
        ("Collectible support never promotes expert adjusted specialist mandatory or missing quality recipes",()=>
        {
            var recipe=Recipe();var p=recipe.Simulation!;
            Check(CraftSimulation.Unsupported(recipe) is null,"ordinary collectible accepted");
            foreach(var excluded in new[]{p with {Expert=true},p with {LevelScaled=true},p with {Specialist=true},p with {RequiredQuality=10},p with {Quality=0}})
                Check(CraftSimulation.Unsupported(recipe with {Simulation=excluded}) is not null,"special case remains explicitly unsupported");
        }),
        ("Inner Quiet gains single double combo and capped stacks before a maximum potency Byregot",()=>
        {
            var recipe=Recipe() with {Simulation=Recipe().Simulation! with {Durability=200,Quality=100000}};
            CraftActionKind[] sequence=[CraftActionKind.Reflect,CraftActionKind.BasicTouch,CraftActionKind.RefinedTouch,CraftActionKind.StandardTouch,
                CraftActionKind.PreparatoryTouch,CraftActionKind.PreparatoryTouch,CraftActionKind.BasicTouch,CraftActionKind.GreatStrides,CraftActionKind.Byregot];
            var result=CraftSimulation.Run(recipe,Stats,0,sequence);
            Check(result.Error is null && result.Steps.Select(step=>step.After.InnerQuiet).SequenceEqual(new[]{2,3,5,6,8,10,10,10,0}),"double gains, combo bonus, cap ten, buff grants no stack and Byregot consumes all");
            var expected=(int)((BigInteger)result.BaseQuality*300*200*2/10000);
            Check(result.Steps[^1].QualityGain==expected && result.Final.GreatStrides==0,"Byregot potency is capped at 300 before IQ quality multiplier and Great Strides");
            var noCombo=CraftSimulation.Run(recipe,Stats,0,[CraftActionKind.Reflect,CraftActionKind.RefinedTouch]);
            Check(noCombo.Final.InnerQuiet==3,"Refined Touch without Basic Touch combo grants only one stack");
            var noQuiet=CraftSimulation.Run(recipe,Stats,0,[CraftActionKind.Byregot]);
            Check(noQuiet.InvalidAction==0 && noQuiet.Final.Step==0 && noQuiet.Error is not null,"Byregot cannot execute without Inner Quiet");
        }),
        ("Crafting log accepts collectible identity and produces a compatible saved and suggested macro",()=>
        {
            var recipe=Recipe();var now=DateTimeOffset.UtcNow;
            var observation=new CraftLogObservation(1,123,456,0,new(0,0),new(500,500),now,8,StartingQuality:0,MaximumQuality:10000);
            var stats=new CharacterStatsState(1,"Artisan",8,"Menuisier",100,now,new Dictionary<string,int>{{"Craftsmanship",1000},{"Control",1000},{"CraftingPoints",500}});
            var context=CraftLogContext.Resolve(observation,stats,RecipeCatalog.Build([recipe],"fixture"),now);
            Check(context.Issue is null && context.Stats is not null,"same native collector selection guard as ordinary recipe");
            var preset=CraftPresets.Create(recipe,"Collection",[CraftActionKind.Reflect,CraftActionKind.GreatStrides,CraftActionKind.Byregot,CraftActionKind.BasicSynthesis]);
            var result=CraftLogSession.Build(new(1,recipe,context.Stats!,Catalog(),new[]{preset},new(600,4,64,10000),0,true));
            Check(result.Saved?.Name=="Collection" && result.Suggested is not null,"helper comparisons and automatic suggestion support collectors together");
        }),
    };
}
