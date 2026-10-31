using System.Numerics;
using TenshoAssistant.Character;
using TenshoAssistant.Craft;

internal static class CraftOptimizationTests
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static RecipeDefinition Recipe()=>new(123,456,1,"Menuiserie",100,[],0,new(80,100,1000,100,100,100,100,0,0,1000));
    private static CraftActionCatalog Catalog()=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,spec.Kind+" "+job,1,"Native"))),"fixture");
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Craft log uses observed starting quality and rejects missing incompatible or out of range UI numbers",()=>
        {
            var now=DateTimeOffset.UtcNow;var recipe=Recipe();var catalog=RecipeCatalog.Build([recipe],"fixture");
            var observed=new CraftLogObservation(1,123,456,0,new(0,0),new(500,500),now,8,StartingQuality:600,MaximumQuality:1000);
            var stats=new CharacterStatsState(1,"Artisan",8,"Menuisier",100,now,new Dictionary<string,int>{{"Craftsmanship",1000},{"Control",1000},{"CraftingPoints",500}});
            Check(CraftLogContext.Resolve(observed,stats,catalog,now) is {Issue:null,StartingQuality:600},"HQ material contribution comes from actual crafting log number");
            foreach(var invalid in new[]{observed with {StartingQuality=null},observed with {MaximumQuality=999},observed with {StartingQuality=1001},observed with {StartingQuality=-1}})
                Check(CraftLogContext.Resolve(invalid,stats,catalog,now).Stats is null,"never silently assume zero or foreign starting quality");
            Check(CraftLiveFrame.ParseNumber("6\u202f000")==6000 && CraftLiveFrame.ParseNumber("6.000")==6000 && CraftLiveFrame.ParseNumber("6/12") is null,"native number parsing preserves grouping but rejects composite display text");
        }),
        ("Saved and generated macros share starting quality and select shortest waits before surplus quality",()=>
        {
            var recipe=Recipe();var fast=CraftPresets.Create(recipe,"Rapide",[CraftActionKind.BasicSynthesis]);
            var surplus=CraftPresets.Create(recipe,"Qualité superflue",[CraftActionKind.Reflect,CraftActionKind.BasicSynthesis]);
            var result=CraftLogSession.Build(new(1,recipe,new(100,1000,1000,500),Catalog(),new[]{surplus,fast},new(700,4,64,10000),2,true,StartingQuality:800));
            Check(result.Saved is {Name:"Rapide",Simulation.StartingQuality:800,Export.WaitSeconds:5},"once target reached extra quality never outweighs faster saved macro");
            Check(result.Suggested is {Simulation.StartingQuality:800,Export.WaitSeconds:5} && result.Suggested.Simulation.Final.Quality>=800,"worker search and export include material quality and extra waits");
        }),
        ("Completion ordering prioritizes target then execution waits then action count",()=>
        {
            var shortActions=new CraftSimState(5,100,800,10,100,0,Finish:CraftFinish.Completed);
            var faster=shortActions with {Step=6};
            Check(CraftSolver.BetterCompletion(faster,14,shortActions,15,700),"faster wins even with more actions");
            Check(!CraftSolver.BetterCompletion(shortActions,15,faster,14,700),"fewer actions cannot win against faster completion");
            Check(CraftSolver.BetterCompletion(shortActions,14,faster,14,700),"actions decide equal waiting durations");
            Check(!CraftSolver.BetterCompletion(faster with {Quality=600},10,shortActions,15,700),"fast failure to reach quality never outranks success");
        }),
        ("Search waiting-time optimum agrees with exhaustive four-action level21 enumeration",()=>
        {
            var recipe=Recipe() with {Level=21,Simulation=new(40,200,500,100,100,100,100,0,0,0)};
            foreach(var cp in new[]{50,100})foreach(var extra in new[]{0,2})
            {
                var stats=new CraftStats(21,1000,1000,cp);var terminals=new List<(int Wait,int Steps)>();
                var kinds=CraftActionCatalog.Specs.Where(spec=>spec.Level<=21).Select(spec=>spec.Kind).ToArray();
                void Visit(CraftActionKind[] sequence)
                {
                    var simulation=CraftSimulation.Run(recipe,stats,0,sequence);
                    if(simulation.Error is not null || simulation.Final.Finish==CraftFinish.Broken)return;
                    if(simulation.Final.Finish==CraftFinish.Completed)
                    {
                        if(simulation.Final.Quality>=300)terminals.Add((sequence.Sum(kind=>(kind is CraftActionKind.GreatStrides or CraftActionKind.Veneration or CraftActionKind.WasteNot ? 2:3)+extra),sequence.Length));
                        return;
                    }
                    if(sequence.Length<4)foreach(var kind in kinds)Visit(sequence.Append(kind).ToArray());
                }
                Visit([]);var best=terminals.OrderBy(row=>row.Wait).ThenBy(row=>row.Steps).First();
                var found=CraftSolver.Solve(recipe,stats,0,new(300,4,512,50000,extra));
                Check(found.TargetReached && !found.NodeLimitReached && found.WaitSeconds==best.Wait
                    && found.Simulation?.Steps.Count==best.Steps,$"independent enumeration: cp={cp} extra={extra} expected={best.Wait}/{best.Steps} actual={found.WaitSeconds}/{found.Simulation?.Steps.Count} pruned={found.BeamPruned} limit={found.NodeLimitReached}");
            }
        }),
        ("Changing HQ starting quality invalidates same-owner same-recipe worker result",()=>
        {
            var input=new CraftLogInput(1,Recipe(),new(100,1000,1000,500),Catalog(),[],new(700,4,64,10000),0,true);
            using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
            using var session=new CraftLogSession((current,_)=>{if(current.StartingQuality==0){entered.Set();release.Wait();}return new(null,null,false,current.StartingQuality.ToString());});
            var old=session.StartAsync(input);
            try
            {
                Check(entered.Wait(TimeSpan.FromSeconds(5)),"old worker started");
                var next=input with {StartingQuality=600};Check(next!=input,"material quality participates in helper cache key");
                session.StartAsync(next).GetAwaiter().GetResult();var expected=session.State;release.Set();old.GetAwaiter().GetResult();
                Check(ReferenceEquals(expected,session.State) && session.State.Result?.SearchIssue=="600","late zero-quality calculation cannot overwrite HQ proposal");
            }
            finally{release.Set();old.GetAwaiter().GetResult();}
        }),
    };
}
