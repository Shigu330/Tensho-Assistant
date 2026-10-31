using TenshoAssistant.Craft;

internal static class CraftSolverTests
{
    private static RecipeDefinition Recipe(int progress=200,int quality=500)=>new(1,2,1,"Fixture",5,[],0,new(40,progress,quality,100,100,100,100,0,0,0));
    private static readonly CraftStats Stats=new(5,1000,1000,54);
    private static void Check(bool condition,string why) {if(!condition)throw new Exception(why);}
    private static void Throws<T>(Action run) where T:Exception {try{run();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static CraftSolveInput Input(int revision=1)=>new(42,revision,Recipe(),Stats,0,new(135,5,512,100000));
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Craft search agrees with independent complete low-level action enumeration",()=>
        {
            foreach(var cs in new[]{400,700,1000})
            foreach(var progress in new[]{100,200,300})
            foreach(var target in new[]{0,135,270,405})
            {
                var terminals=new List<(int Steps,int Quality,int Cp)>();
                // Level5 has just Basic Synthesis and Basic Touch in this action set; no IQ or traits.
                // Independent integer fixture: progress=CS/10+2, quality=135, action costs 10 durability and 0/18 CP.
                void Enumerate(int steps,int p,int q,int durability,int cp)
                {
                    if(p>=progress) {terminals.Add((steps,Math.Min(500,q),cp));return;}
                    if(durability<=0 || steps==5)return;
                    Enumerate(steps+1,p+cs/10+2,q,durability-10,cp);
                    if(cp>=18)Enumerate(steps+1,p,q+135,durability-10,cp-18);
                }
                Enumerate(0,0,0,40,54);
                var reached=terminals.Where(row=>row.Quality>=target).ToArray();
                var result=CraftSolver.Solve(Recipe(progress),Stats with {Craftsmanship=cs},0,new(target,5,512,100000));
                Check(!result.BeamPruned && !result.NodeLimitReached,"small fixture exhaustive before stopping at achieved layer");
                Check(result.TargetReached==(reached.Length>0),"target feasibility agrees with independent enumeration");
                if(reached.Length>0)
                {
                    var steps=reached.Min(row=>row.Steps); var cp=reached.Where(row=>row.Steps==steps).Max(row=>row.Cp);
                    Check(result.Simulation!.Steps.Count==steps && result.Simulation.Final.Cp==cp,"shortest observed layer then CP");
                }
                else if(terminals.Count>0) Check(result.Simulation?.Final.Quality==terminals.Max(row=>row.Quality),"best completed below target retained");
                else Check(result.Simulation is null,"no finished route");
            }
        }),
        ("Craft search returns an inspectable replayable sequence including zero-durability completion",()=>
        {
            var result=CraftSolver.Solve(Recipe(),Stats,0,new(270,5,512,100000));
            Check(result.TargetReached && result.Simulation is {Steps.Count:4} && result.Simulation.Final.Durability==0,"two touches two synthesis");
            var replay=CraftSimulation.Run(Recipe(),Stats,0,result.Simulation!.Steps.Select(step=>step.Action));
            Check(replay.Final==result.Simulation.Final && replay.Error is null,"shared transition replay");
        }),
        ("Craft search reports below-target completion separately from no completion",()=>
        {
            var below=CraftSolver.Solve(Recipe(),Stats with {Cp=0},0,new(500,5,128,100000));
            Check(!below.TargetReached && below.Simulation?.Final.Finish==CraftFinish.Completed && below.Simulation.Final.Quality==0,"can finish without quality");
            var none=CraftSolver.Solve(Recipe(10000),Stats with {Cp=0},0,new(0,5,128,100000));
            Check(!none.TargetReached && none.Simulation is null,"no route found, not a claim of universal impossibility");
        }),
        ("Craft search node depth and beam limits are strict and visible",()=>
        {
            var budget=CraftSolver.Solve(Recipe(),Stats,0,new(500,10,128,1));
            Check(budget.Expanded==1 && budget.NodeLimitReached,"one transition budget");
            var depth=CraftSolver.Solve(Recipe(1000),Stats,0,new(500,1,128,100000));
            Check(depth.Depth==1 && depth.DepthLimitReached,"one expansion layer");
            var width=CraftSolver.Solve(Recipe(),Stats,0,new(500,5,1,100000));
            Check(width.BeamPruned,"beam pruning explicit");
        }),
        ("Craft search deterministic results and stat unlock bounds",()=>
        {
            var recipe=Recipe(2000,2000) with {Level=100,Simulation=Recipe().Simulation! with {Durability=80,Progress=2000,Quality=2000}};
            var stats=new CraftStats(100,1000,1000,500,false);
            var a=CraftSolver.Solve(recipe,stats,0,new(1000,30,64,30000)); var b=CraftSolver.Solve(recipe,stats,0,new(1000,30,64,30000));
            Check(a.Expanded==b.Expanded && a.TargetReached==b.TargetReached && a.Simulation?.Final==b.Simulation?.Final,"deterministic search");
            if(a.Simulation is { } simulation)
            {
                Check(!simulation.Steps.Any(step=>step.Action==CraftActionKind.Manipulation),"manual unlock respected");
                Check(simulation.Steps.Select(step=>step.Action).SequenceEqual(b.Simulation!.Steps.Select(step=>step.Action)),"same ordering");
            }
            foreach(var options in new[]{new CraftSolveOptions(-1),new(501),new(0,0),new(0,101),new(0,1,0),new(0,1,513),new(0,1,1,0),new(0,1,1,500001)})
                Throws<ArgumentOutOfRangeException>(()=>CraftSolver.Solve(Recipe(),Stats,0,options));
            using var cancel=new CancellationTokenSource(); cancel.Cancel();
            Throws<OperationCanceledException>(()=>CraftSolver.Solve(Recipe(),Stats,0,new(0),cancel.Token));
        }),
        ("Craft search finds a nontrivial quality route with buffs and durability planning",()=>
        {
            var recipe=Recipe() with {Level=100,Simulation=Recipe().Simulation! with {Durability=80,Progress=1000,Quality=2000}};
            var stats=new CraftStats(100,1000,1000,500,false);
            var witness=CraftSimulation.Run(recipe,stats,0,[CraftActionKind.Reflect,CraftActionKind.Innovation,CraftActionKind.BasicTouch,
                CraftActionKind.StandardTouch,CraftActionKind.AdvancedTouch,CraftActionKind.GreatStrides,CraftActionKind.Byregot,
                CraftActionKind.WasteNot,CraftActionKind.Groundwork,CraftActionKind.Groundwork,CraftActionKind.Groundwork]);
            Check(witness.Final.Finish==CraftFinish.Completed && witness.Final.Quality>=2000,"explicit feasible witness");
            var result=CraftSolver.Solve(recipe,stats,0,new(2000,40,128,100000));
            Check(result.TargetReached && result.Simulation?.Final.Finish==CraftFinish.Completed,"bounded search finds an actual quality and progress solution");
        }),
        ("Craft search invalidation rejects late publication after owner and revision changes",()=>
        {
            using var entered=new ManualResetEventSlim(); using var release=new ManualResetEventSlim();
            using var session=new CraftSolveSession((input,_)=>{entered.Set();Check(release.Wait(5000),"release");return CraftSolver.Solve(input.Recipe,input.Stats,0,input.Options);});
            var task=session.StartAsync(Input()); Check(entered.Wait(5000),"worker entered");
            session.ValidateContext(99,1,1); release.Set(); task.GetAwaiter().GetResult();
            Check(session.State.Input is null && session.State.Result is null && !session.State.Loading,"owner change invalidated late worker");
            using var normal=new CraftSolveSession(); normal.StartAsync(Input()).GetAwaiter().GetResult();
            Check(normal.State.Result is not null && normal.State.CalculatedAt is not null,"published result timestamp");
            normal.ValidateContext(42,2,1); Check(normal.State.Result is null,"revision invalidated");
        }),
        ("Craft search newer worker wins and disposal clears state",()=>
        {
            using var entered=new ManualResetEventSlim(); using var release=new ManualResetEventSlim();
            var session=new CraftSolveSession((input,_)=>
            {if(input.Revision==1){entered.Set();Check(release.Wait(5000),"release");}return CraftSolver.Solve(input.Recipe,input.Stats,0,input.Options);});
            var old=session.StartAsync(Input()); Check(entered.Wait(5000),"entered");
            session.StartAsync(Input(2)).GetAwaiter().GetResult(); release.Set(); old.GetAwaiter().GetResult();
            Check(session.State.Input?.Revision==2 && session.State.Result is not null,"new result kept");
            session.Dispose(); Check(session.State.Result is null,"dispose clears"); session.StartAsync(Input()).GetAwaiter().GetResult(); Check(session.State.Input is null,"no restart disposed");
        }),
        ("Craft search input ingredients copied and cancellation errors owned",()=>
        {
            var ingredients=new List<RecipeIngredient>{new(2,1)}; var input=Input() with {Recipe=Recipe() with {Ingredients=ingredients}};
            using var entered=new ManualResetEventSlim(); using var release=new ManualResetEventSlim();
            using var session=new CraftSolveSession((copied,token)=>
            {entered.Set();Check(release.Wait(5000),"release");Check(copied.Recipe.Ingredients.Count==1,"managed input copy");token.ThrowIfCancellationRequested();return CraftSolver.Solve(copied.Recipe,copied.Stats,0,copied.Options);});
            var task=session.StartAsync(input);Check(entered.Wait(5000),"entered");ingredients.Clear();session.Cancel();release.Set();task.GetAwaiter().GetResult();
            Check(!session.State.Loading && session.State.Result is null && session.State.Error is null,"cancel rejects late error");
            using var invalid=new CraftSolveSession();invalid.StartAsync(Input() with {Stats=Stats with {Level=0}}).GetAwaiter().GetResult();
            Check(!invalid.State.Loading && invalid.State.Error is not null,"worker validation surfaced");
        })
    };
}
