using System.Security.Cryptography;
using System.Text.Json;
using TenshoAssistant.Caching;
namespace TenshoAssistant.Craft;

public sealed record CraftLogInput(ulong Owner,RecipeDefinition Recipe,CraftStats Stats,CraftActionCatalog Catalog,
    IReadOnlyList<CraftPreset> Presets,CraftSolveOptions Options,int ExtraWait,bool Echo,long ObservationRevision=0,int StartingQuality=0,bool Chain=false);
public sealed record CraftLogMacro(string Name,CraftSimulationResult Simulation,CraftMacroExport Export);
public sealed record CraftLogResult(CraftLogMacro? Saved,CraftLogMacro? Suggested,bool TargetReached,string? SearchIssue);
public sealed record CraftLogState(CraftLogInput? Input,bool Loading,CraftLogResult? Result,string? Error,bool FromCache=false,DateTimeOffset? CalculatedAt=null);

public sealed class CraftLogSession : IDisposable
{
    private readonly object gate=new();
    private readonly Func<CraftLogInput,CancellationToken,CraftLogResult> build;
    private CancellationTokenSource? cancellation;
    private long generation;
    private bool disposed;
    private Func<TimeSpan> cacheLifetime=()=>TimeSpan.FromDays(1);
    private DataCache? disk;
    public void ConfigureCache(Func<TimeSpan> lifetime,DataCache store){lock(gate){cacheLifetime=lifetime;disk=store;}}
    private CraftLogState state=new(null,false,null,null);
    public CraftLogState State=>Volatile.Read(ref state);
    public CraftLogSession(Func<CraftLogInput,CancellationToken,CraftLogResult>? build=null)=>this.build=build ?? Build;
    public Task StartAsync(CraftLogInput input,bool force=false)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(input.Owner==0)throw new ArgumentException("A current character is required.");
        input=input with {Recipe=input.Recipe with {Ingredients=Array.AsReadOnly(input.Recipe.Ingredients.ToArray())},
            Presets=Array.AsReadOnly(input.Presets.Take(CraftPresets.Capacity).Where(CraftPresets.Valid)
                .Select(row=>row with {Actions=Array.AsReadOnly(row.Actions.ToArray())}).ToArray())};
        lock(gate)
        {
            if(disposed)return Task.CompletedTask;
            CancelCore();var ticket=generation;var source=new CancellationTokenSource();cancellation=source;
            var token=source.Token;state=new(input,true,null,null);
            return Task.Run(async()=>
            {
                try
                {
                    var key=CacheKey(input);CraftLogResult? result=null;var fromCache=false;var date=DateTimeOffset.UtcNow;
                    if(!force && disk is { } cacheStore)
                    {
                        var saved=await cacheStore.ReadAsync<CraftLogResult>("craft-calculations",key,"craft-model-v3",token).ConfigureAwait(false);
                        if(saved is not null && CacheAge.Fresh(saved.RecordedAt,DateTimeOffset.UtcNow,cacheLifetime()) && ValidCached(input,saved.Value))
                        {result=saved.Value;date=saved.RecordedAt;fromCache=true;}
                    }
                    token.ThrowIfCancellationRequested();if(result is null){result=build(input,token);date=DateTimeOffset.UtcNow;}
                    lock(gate)if(!disposed && ticket==generation && !token.IsCancellationRequested)
                    {
                        state=new(input,false,result,null,fromCache,date);
                        if(!fromCache && disk is { } writeStore)_=writeStore.WriteAsync("craft-calculations",key,"craft-model-v3",result,date);
                    }
                }
                catch(Exception error)
                {
                    lock(gate)if(!disposed && ticket==generation && !token.IsCancellationRequested)state=new(input,false,null,error.Message);
                }
                finally{lock(gate){if(ReferenceEquals(cancellation,source))cancellation=null;source.Dispose();}}
            });
        }
    }
    private static string CacheKey(CraftLogInput input)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        input.Owner,input.Recipe,input.Stats,input.StartingQuality,input.Options,input.ExtraWait,input.Echo,
        input.Presets,Version=input.Catalog.DataVersion,ModelVersion=typeof(CraftSimulation).Assembly.GetName().Version?.ToString(),
        Actions=CraftActionCatalog.Specs.Select(spec=>input.Catalog.Find(input.Recipe.CraftTypeId+8,spec.Kind)).ToArray()
    })));
    private static bool ValidCached(CraftLogInput input,CraftLogResult result)
    {
        try
        {
            foreach(var macro in new[]{result.Saved,result.Suggested})
            {
                if(macro is null)continue;
                var simulation=CraftSimulation.Run(input.Recipe,input.Stats,input.StartingQuality,macro.Simulation.Steps.Select(step=>step.Action));
                var export=CraftMacroExporter.Build(simulation,input.Recipe.CraftTypeId+8,input.Catalog,input.ExtraWait,input.Echo,input.Chain);
                if(simulation.RecipeId!=macro.Simulation.RecipeId || simulation.Final!=macro.Simulation.Final || simulation.StartingQuality!=macro.Simulation.StartingQuality || simulation.Stats!=macro.Simulation.Stats
                    || export.Actions!=macro.Export.Actions || export.WaitSeconds!=macro.Export.WaitSeconds
                    || !export.Pages.Select(page=>page.Text).SequenceEqual(macro.Export.Pages.Select(page=>page.Text)))return false;
            }
            return result.TargetReached==(result.Suggested is { } proposed && proposed.Simulation.Final.Quality>=input.Options.TargetQuality);
        }
        catch(Exception){return false;}
    }
    public static CraftLogResult Build(CraftLogInput input,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        if(CraftSimulation.Unsupported(input.Recipe) is { } issue)throw new ArgumentException(issue);
        CraftLogMacro? saved=null;var job=input.Recipe.CraftTypeId+8;
        foreach(var preset in input.Presets.Take(CraftPresets.Capacity))
        {
            token.ThrowIfCancellationRequested();
            if(!CraftPresets.Valid(preset) || preset.RecipeId!=input.Recipe.RecipeId || preset.ResultItemId!=input.Recipe.ResultItemId
                || preset.CraftTypeId!=input.Recipe.CraftTypeId)continue;
            try
            {
                var simulation=CraftSimulation.Run(input.Recipe,input.Stats,input.StartingQuality,preset.Actions,token);
                if(simulation.Final.Finish!=CraftFinish.Completed || simulation.Error is not null)continue;
                var export=CraftMacroExporter.Build(simulation,job,input.Catalog,input.ExtraWait,input.Echo,input.Chain);
                if(CraftSolver.BetterCompletion(simulation.Final,export.WaitSeconds,saved?.Simulation.Final,saved?.Export.WaitSeconds ?? 0,input.Options.TargetQuality))
                    saved=new(preset.Name,simulation,export);
            }
            catch(ArgumentException){ /* An incompatible saved scenario must not prevent the new search. */ }
        }
        var result=CraftSolver.Solve(input.Recipe,input.Stats,input.StartingQuality,input.Options with {ExtraWait=input.ExtraWait},token);
        var suggested=result.Simulation is {Final.Finish:CraftFinish.Completed,Error:null} valid
            ? new CraftLogMacro("Macro proposée",valid,CraftMacroExporter.Build(valid,job,input.Catalog,input.ExtraWait,input.Echo,input.Chain)):null;
        token.ThrowIfCancellationRequested();
        return new(saved,suggested,result.TargetReached,suggested is null ? "Aucune synthèse terminée trouvée dans les limites de recherche."
            :result.TargetReached ? null:"La séquence termine sous la qualité cible.");
    }
    private void CancelCore(){generation++;cancellation?.Cancel();state=new(null,false,null,null);}
    public void Cancel(){lock(gate)if(!disposed)CancelCore();}
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;CancelCore();}}
}
