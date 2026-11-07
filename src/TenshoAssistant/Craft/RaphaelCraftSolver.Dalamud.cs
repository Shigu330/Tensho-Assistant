using Raphael;

namespace TenshoAssistant.Craft;

/// <summary>
/// A-03 (0.465, DEC-028): the optional Raphael solver (Raphael.Net 5.0.0, MIT; raphael-rs, Apache-2.0: docs/evidence/0.451-raphael-study.md). Runs on the search's worker, never on the game's
/// thread, cancellable, one at a time (CraftSolveSession); leaves two processors to the game. Its native library can bring the game down if it fails, which is why it is off by default. The
/// macro it gives is replayed in Tensho's simulator (RaphaelActions.Replay) and proposed only if it holds.
/// </summary>
internal static class RaphaelCraftSolver
{
    public static CraftSolveResult Solve(RecipeDefinition recipe,CraftStats stats,int startingQuality,CraftSolveOptions options,CancellationToken cancellation)
    {
        var parameters=recipe.Simulation ?? throw new ArgumentException("Recipe without simulation data.",nameof(recipe));
        var start=CraftSimulation.Run(recipe,stats,startingQuality,[],cancellation);
        var pool=RaphaelActions.Pool(stats,options.Forbidden,recipe.Level).Select(kind=>(Raphael.Action)(byte)RaphaelActions.Number(kind)!.Value).ToArray();
        var target=Math.Max(0,options.TargetQuality-startingQuality);
        // 0.782 (X-25): never hand the native solver a value it divides by while 0; ArgumentException is the « no Raphael answer » the cache already expects.
        if(RaphaelActions.InputRefusal(start.BaseProgress,start.BaseQuality,parameters.Durability,parameters.Progress,stats.Cp,target) is { } refusal)
            throw new ArgumentException("Raphael not called: "+refusal+".",nameof(recipe));
        var config=new SolverConfig{AllowNonMaxQualitySolutions=true,ThreadCount=(ushort)Math.Clamp(Environment.ProcessorCount-2,1,ushort.MaxValue)};
        var input=new SolverInput
        {
            CP=(ushort)stats.Cp,Durability=(ushort)parameters.Durability,Progress=(ushort)parameters.Progress,Quality=(ushort)target,
            BaseProgressGain=(ushort)start.BaseProgress,BaseQualityGain=(ushort)start.BaseQuality,JobLevel=(byte)stats.Level,
        };
        Raphael.Action[]? found=null;
        using var solver=new Solver(config,input,pool);
        // Called from the native solver: nothing may be thrown back into it.
        solver.OnFinish+=actions=>Volatile.Write(ref found,actions);
        using(cancellation.Register(solver.Cancel))solver.Solve();
        cancellation.ThrowIfCancellationRequested();
        var (result,problem)=RaphaelActions.Replay(recipe,stats,startingQuality,(Volatile.Read(ref found) ?? []).Select(action=>(int)action));
        if(result is null)throw new InvalidOperationException(problem);
        var quality=result.Final.Quality;
        return new(result,options.TargetQuality,quality>=options.TargetQuality,0,result.Steps.Count,false,false,false);
    }
}
