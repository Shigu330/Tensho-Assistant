using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Maps;

/// <summary>
/// Copies quest-giver and gathering-point positions in bounded framework batches (2 ms per tick), then indexes on a worker.
/// The rows are kept in DataCache (family catalog-map-positions) until the client data, the language or the copy shape changes,
/// like the item, recipe, gathering and NPC vendor catalogues (docs/goals/CATALOG-CACHE-012.md).
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
        var givers=new Dictionary<uint,MapLocalPosition>();
        foreach(var quest in data.GetExcelSheet<Quest>())
        {
            if(quest.IssuerLocation.RowId!=0 && quest.IssuerLocation.IsValid)
            {
                var level=quest.IssuerLocation.Value;
                // Data = map icon of the quest kind: EventIconType.MapIconAvailable + 1 (65560 → 71021 as seen in game;
                // docs/evidence/0.151.0-questicon-probe.log — other states of the same kind use other offsets).
                var kindIcon=quest.EventIconType.RowId!=0 && quest.EventIconType.IsValid && quest.EventIconType.Value.MapIconAvailable!=0 ? quest.EventIconType.Value.MapIconAvailable+1:0u;
                if(level.Map.RowId!=0){var giver=new MapLocalPosition(MapLocalKind.QuestIssuer,quest.RowId,level.Map.RowId,level.X,level.Z,level.Radius,kindIcon);rows.Add(giver);givers[quest.RowId]=giver;}
            }
            yield return ++count;
        }
        // Sightseeing log vistas (Adventure → Level).
        foreach(var vista in data.GetExcelSheet<Adventure>())
        {
            if(vista.Level.RowId!=0 && vista.Level.IsValid && vista.Level.Value.Map.RowId!=0 && vista.Name.ExtractText() is { Length:>0 } name)
            {
                var level=vista.Level.Value;
                var emote=vista.Emote.RowId!=0 && vista.Emote.IsValid ? vista.Emote.Value.Name.ExtractText():"";
                rows.Add(new(MapLocalKind.Sightseeing,vista.RowId,level.Map.RowId,level.X,level.Z,0,(uint)Math.Max(0,vista.IconDiscovered),name,vista.MinTime,vista.MaxTime,emote));
            }
            yield return ++count;
        }
        // Aether currents: quest ones at the quest giver; field ones where a Level places the EObj that carries them.
        var currents=data.GetExcelSheet<AetherCurrent>();
        foreach(var current in currents)
        {
            if(current.Quest.RowId!=0 && givers.TryGetValue(current.Quest.RowId,out var giver))
                rows.Add(new(MapLocalKind.AetherCurrent,current.RowId,giver.MapId,giver.X,giver.Z,0,current.Quest.RowId));
            yield return ++count;
        }
        var currentObjects=new Dictionary<uint,uint>();
        foreach(var obj in data.GetExcelSheet<EObj>())
        {
            if(obj.Data.RowId!=0 && currents.HasRow(obj.Data.RowId))currentObjects.TryAdd(obj.RowId,obj.Data.RowId);
            yield return ++count;
        }
        foreach(var level in data.GetExcelSheet<Level>())
        {
            if(level.Map.RowId!=0 && currentObjects.TryGetValue(level.Object.RowId,out var currentId))
                rows.Add(new(MapLocalKind.AetherCurrent,currentId,level.Map.RowId,level.X,level.Z,0));
            if((count & 63)==0)yield return count;
            count++;
        }
        // ExportedGatheringPoint shares its row id with GatheringPointBase; X/Y are world X/Z.
        foreach(var point in data.GetExcelSheet<ExportedGatheringPoint>())
        {
            if(point.X!=0 || point.Y!=0)rows.Add(new(MapLocalKind.GatheringPoint,point.RowId,0,point.X,point.Y,point.Radius));
            yield return ++count;
        }
        // FishingSpot X/Z are texture pixels of the territory's map: world = (pixel − 1024) / scale − offset.
        foreach(var spot in data.GetExcelSheet<FishingSpot>())
        {
            if(spot.TerritoryType.RowId!=0 && spot.TerritoryType.IsValid && (spot.X!=0 || spot.Z!=0) && spot.TerritoryType.Value.Map.IsValid)
            {
                var map=spot.TerritoryType.Value.Map.Value;
                if(map.SizeFactor>0)
                {
                    var scale=map.SizeFactor/100f;
                    rows.Add(new(MapLocalKind.FishingSpot,spot.RowId,map.RowId,(spot.X-1024f)/scale-map.OffsetX,(spot.Z-1024f)/scale-map.OffsetY,0));
                }
            }
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
