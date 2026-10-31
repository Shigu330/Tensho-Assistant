using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Maps;

/// <summary>
/// Copies quest-giver and gathering-point positions in bounded framework batches (2 ms per tick), then indexes on a worker.
/// The rows are kept in DataCache (family catalog-map-positions) until the client data, the language or the copy shape changes,
/// like the item, recipe, gathering and NPC vendor catalogues (docs/CATALOG-CACHE-012.md).
/// </summary>
internal sealed class MapLocalIndexLoader : IDisposable
{
    private readonly object gate=new();
    private readonly MapLocalIndexService service;private readonly IDataManager data;private readonly IFramework framework;private readonly IPluginLog log;
    private readonly Caching.DataCache? cache;
    private readonly CancellationTokenSource cancellation=new();
    private readonly List<MapLocalPosition> rows=[];
    private IEnumerator<int>? copy;
    private Task<(MapLocalIndex? Index,Exception? Error,bool FromCache)>? build;
    private Task<Caching.CachedValue<MapLocalIndexCacheData>?>? cacheRead;
    private bool cacheChecked,stopped,disposed;
    private string? dataVersion,cacheVersion,cacheScope;
    private MapLocalIndexCacheData? toPersist;

    public MapLocalIndexLoader(MapLocalIndexService service,IDataManager data,IFramework framework,IPluginLog log,Caching.DataCache? cache=null)
    {
        this.service=service;this.data=data;this.framework=framework;this.log=log;this.cache=cache;
        try{framework.Update+=OnUpdate;}catch{cancellation.Dispose();throw;}
    }

    private void OnUpdate(IFramework _)
    {
        if(!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        lock(gate)
        {
            if(stopped)return;
            try
            {
                if(build is not null)
                {
                    if(!build.IsCompleted)return;
                    var result=build.GetAwaiter().GetResult();build=null;
                    if(result.Error is not null)
                    {
                        if(!result.FromCache)throw result.Error;
                        log.Warning(result.Error,"Tensho map position cache rejected; rebuilding from game data.");
                        return;
                    }
                    service.Publish(result.Index!);
                    if(toPersist is { } persisted && cache is not null && cacheScope is not null && cacheVersion is not null)
                    {var pending=cache.WriteAsync(MapLocalIndexCache.Family,cacheScope,cacheVersion,persisted,DateTimeOffset.UtcNow);}
                    Stop();return;
                }
                if(dataVersion is null)
                {
                    dataVersion=string.Join(";",data.GameData.Repositories.Values.OrderBy(r=>r.Name).Select(r=>$"{r.Name}={r.Version}"));
                    cacheScope=data.Language.ToString();
                    cacheVersion=MapLocalIndexCache.Version($"{dataVersion};schema={typeof(Level).Assembly.GetName().Version}");
                }
                if(!cacheChecked && cache is not null)
                {
                    // Disk read, JSON parsing, validation and indexing all stay off the framework thread.
                    cacheRead??=cache.ReadAsync<MapLocalIndexCacheData>(MapLocalIndexCache.Family,cacheScope!,cacheVersion!,cancellation.Token);
                    if(!cacheRead.IsCompleted)return;
                    var cached=cacheRead.GetAwaiter().GetResult();cacheRead=null;cacheChecked=true;
                    if(cached?.Value is { } snapshot)
                    {
                        var expected=dataVersion;
                        build=Task.Run<(MapLocalIndex?,Exception?,bool)>(()=>
                        {
                            try
                            {
                                if(!MapLocalIndexCache.Valid(snapshot,expected))throw new InvalidDataException("Map position cache does not match the current game data.");
                                return (new MapLocalIndex(snapshot.Positions),null,true);
                            }
                            catch(Exception error){return (null,error,true);}
                        });
                        return;
                    }
                }
                copy??=Copy().GetEnumerator();
                var started=Stopwatch.GetTimestamp();var progress=0;var done=false;
                while(Stopwatch.GetElapsedTime(started)<TimeSpan.FromMilliseconds(2))
                {
                    if(!copy.MoveNext()){done=true;break;}
                    progress=copy.Current;
                }
                service.Progress(progress);
                if(!done)return;
                copy.Dispose();copy=null;
                var copied=Array.AsReadOnly(rows.ToArray());var version=dataVersion!;rows.Clear();
                if(cache is not null)toPersist=new(version,copied);
                build=Task.Run<(MapLocalIndex?,Exception?,bool)>(()=>
                {
                    try{return (new MapLocalIndex(copied),null,false);}
                    catch(Exception error){return (null,error,false);}
                });
            }
            catch(Exception error)
            {
                log.Error(error,"Tensho map positions could not be built.");
                service.Fail(error);Stop();
            }
        }
    }

    // Each yield is one copied row; the value is a running count for progress only.
    private IEnumerable<int> Copy()
    {
        var count=0;
        foreach(var quest in data.GetExcelSheet<Quest>())
        {
            if(quest.IssuerLocation.RowId!=0 && quest.IssuerLocation.IsValid)
            {
                var level=quest.IssuerLocation.Value;
                if(level.Map.RowId!=0)rows.Add(new(MapLocalKind.QuestIssuer,quest.RowId,level.Map.RowId,level.X,level.Z,level.Radius));
            }
            yield return ++count;
        }
        // ExportedGatheringPoint shares its row id with GatheringPointBase; X/Y are world X/Z.
        foreach(var point in data.GetExcelSheet<ExportedGatheringPoint>())
        {
            if(point.X!=0 || point.Y!=0)rows.Add(new(MapLocalKind.GatheringPoint,point.RowId,0,point.X,point.Y,point.Radius));
            yield return ++count;
        }
    }

    private void Stop(){stopped=true;framework.Update-=OnUpdate;copy?.Dispose();copy=null;cacheRead=null;toPersist=null;rows.Clear();}
    public void Dispose()
    {
        lock(gate)
        {
            if(disposed)return;
            disposed=true;cancellation.Cancel();if(!stopped)Stop();service.Clear();cancellation.Dispose();
        }
    }
}
