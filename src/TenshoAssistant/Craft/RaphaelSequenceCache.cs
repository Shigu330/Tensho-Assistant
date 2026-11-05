using System.Security.Cryptography;
using System.Text.Json;
using TenshoAssistant.Caching;
namespace TenshoAssistant.Craft;

/// <summary>
/// 0.468 (user request, in-game campaign): Raphael's sequences kept on disk for the craft calculations duration, shared by the crafting log and the simulator. Only what Raphael depends on is
/// in the key (recipe, attributes, starting quality, target, forbidden actions), so neither a new plugin version nor another macro option computes again. A kept sequence is replayed in
/// Tensho's simulator before use; one that no longer holds is computed again. <see cref="CraftSolveOptions.Fresh"/> ignores what is kept.
/// </summary>
public sealed class RaphaelSequenceCache(Func<RecipeDefinition,CraftStats,int,CraftSolveOptions,CancellationToken,CraftSolveResult> solve)
{
    public const string Family="raphael-sequences";
    public const string DataVersion="raphael-net-5.0.0";
    private readonly object gate=new();
    private Func<TimeSpan> lifetime=()=>TimeSpan.FromDays(1);
    private DataCache? store;
    public void Configure(Func<TimeSpan> duration,DataCache cache){lock(gate){lifetime=duration;store=cache;}}
    public CraftSolveResult Solve(RecipeDefinition recipe,CraftStats stats,int startingQuality,CraftSolveOptions options,CancellationToken token)
    {
        DataCache? cache;Func<TimeSpan> duration;
        lock(gate){cache=store;duration=lifetime;}
        var key=Key(recipe,stats,startingQuality,options);
        if(!options.Fresh && cache is not null)
        {
            var saved=cache.ReadAsync<CraftActionKind[]>(Family,key,DataVersion,token).GetAwaiter().GetResult();
            if(saved is not null && CacheAge.Fresh(saved.RecordedAt,DateTimeOffset.UtcNow,duration()) && Replay(recipe,stats,startingQuality,options,saved.Value,token) is { } kept)return kept;
        }
        token.ThrowIfCancellationRequested();
        var result=solve(recipe,stats,startingQuality,options,token);
        if(cache is not null && result.Simulation is {Final.Finish:CraftFinish.Completed,Error:null} found)
            _=cache.WriteAsync(Family,key,DataVersion,found.Steps.Select(step=>step.Action).ToArray(),DateTimeOffset.UtcNow);
        return result;
    }
    private static string Key(RecipeDefinition recipe,CraftStats stats,int startingQuality,CraftSolveOptions options)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        recipe.RecipeId,recipe.CraftTypeId,recipe.Level,recipe.Simulation,stats,startingQuality,options.TargetQuality,options.Forbidden
    })));
    private static CraftSolveResult? Replay(RecipeDefinition recipe,CraftStats stats,int startingQuality,CraftSolveOptions options,IReadOnlyList<CraftActionKind> actions,CancellationToken token)
    {
        if(actions.Count==0 || CraftActionRules.UsesForbidden(actions,options.Forbidden))return null;
        try
        {
            var simulation=CraftSimulation.Run(recipe,stats,startingQuality,actions,token);
            if(simulation.InvalidAction is not null || simulation.Error is not null || simulation.Final.Finish!=CraftFinish.Completed)return null;
            return new(simulation,options.TargetQuality,simulation.Final.Quality>=options.TargetQuality,0,simulation.Steps.Count,false,false,false,null,true);
        }
        catch(ArgumentException){return null;}
    }
}
