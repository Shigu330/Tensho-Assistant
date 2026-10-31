using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Gathering;

internal sealed class GatheringCatalogLoader : IDisposable
{
    private readonly object gate = new();
    private readonly GatheringCatalogService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly CancellationTokenSource cancellation = new();
    private readonly List<GatheringSource> rows = [];
    private readonly List<FishingNotebookEntry> notebookRows=[];
    private ExcelSheet<FishParameter>? fishParameters;
    private ExcelSheet<FishingNoteInfo>? fishNotes;
    private ExcelSheet<GatheringPoint>? points;
    private ExcelSheet<GatheringItem>? items;
    private ExcelSheet<FishingSpot>? fishing;
    private ExcelSheet<GatheringPointTransient>? transient;
    private ExcelSheet<GatheringRarePopTimeTable>? rareTimes;
    private SubrowExcelSheet<GatheringItemPoint>? itemPoints;
    private int position;
    private bool stopped;
    private bool disposed;
    private Task<(GatheringCatalog? Catalog, Exception? Error, bool FromCache)>? build;
    private readonly Caching.DataCache? cache;
    private Task<Caching.CachedValue<GatheringCatalogCacheData>?>? cacheRead;
    private bool cacheChecked;
    private string? dataVersion;
    private string? cacheVersion;
    private string? cacheScope;
    private GatheringCatalogCacheData? toPersist;

    public GatheringCatalogLoader(GatheringCatalogService service, IDataManager data, IFramework framework, IPluginLog log, Caching.DataCache? cache = null)
    {
        this.service = service;
        this.data = data;
        this.framework = framework;
        this.log = log;
        this.cache = cache;
        try { framework.Update += OnUpdate; }
        catch { cancellation.Dispose(); throw; }
    }

    private void OnUpdate(IFramework _)
    {
        if (!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
        lock (gate)
        {
            if (stopped) return;
            try
            {
                if (build is not null)
                {
                    if (!build.IsCompleted) return;
                    var result = build.GetAwaiter().GetResult();
                    build = null;
                    if (result.Error is not null)
                    {
                        if (!result.FromCache) throw result.Error;
                        // A rejected disk copy is never published: the next ticks copy the game sheets as before.
                        log.Warning(result.Error, "Tensho gathering cache rejected; rebuilding from game data.");
                        return;
                    }
                    service.Publish(result.Catalog!);
                    if (toPersist is { } persisted && cache is not null && cacheScope is not null && cacheVersion is not null)
                    {
                        // Fire and forget: DataCache orders writes off-thread and reports its own failures.
                        var pending = cache.WriteAsync(GatheringCatalogCache.Family, cacheScope, cacheVersion, persisted, DateTimeOffset.UtcNow);
                    }
                    toPersist = null;
                    Stop();
                    return;
                }
                if (dataVersion is null)
                {
                    dataVersion = string.Join(";", data.GameData.Repositories.Values.OrderBy(repository => repository.Name)
                        .Select(repository => $"{repository.Name}={repository.Version}"));
                    cacheScope = data.Language.ToString();
                    cacheVersion = GatheringCatalogCache.Version($"{dataVersion};schema={typeof(GatheringPoint).Assembly.GetName().Version}");
                }
                if (!cacheChecked && cache is not null)
                {
                    // Disk read, JSON parsing, validation and indexing all stay off the framework thread.
                    cacheRead ??= cache.ReadAsync<GatheringCatalogCacheData>(GatheringCatalogCache.Family, cacheScope!, cacheVersion!, cancellation.Token);
                    if (!cacheRead.IsCompleted) return;
                    var cached = cacheRead.GetAwaiter().GetResult();
                    cacheRead = null;
                    cacheChecked = true;
                    if (cached?.Value is { } snapshot)
                    {
                        var expected = dataVersion;
                        var cachedToken = cancellation.Token;
                        build = Task.Run<(GatheringCatalog? Catalog, Exception? Error, bool FromCache)>(() =>
                        {
                            try
                            {
                                if (!GatheringCatalogCache.Valid(snapshot, expected)) throw new InvalidDataException("Gathering cache does not match the current game data.");
                                return (new GatheringCatalog(snapshot.Sources, expected, cachedToken, snapshot.Fishing), null, true);
                            }
                            catch (Exception error) { return (null, error, true); }
                        });
                        return;
                    }
                }
                points ??= data.GetExcelSheet<GatheringPoint>();
                items ??= data.GetExcelSheet<GatheringItem>();
                fishing ??= data.GetExcelSheet<FishingSpot>();
                fishParameters ??= data.GetExcelSheet<FishParameter>();
                fishNotes ??= data.GetExcelSheet<FishingNoteInfo>();
                transient ??= data.GetExcelSheet<GatheringPointTransient>();
                rareTimes ??= data.GetExcelSheet<GatheringRarePopTimeTable>();
                itemPoints ??= data.GetSubrowExcelSheet<GatheringItemPoint>();
                var started = Stopwatch.GetTimestamp();
                var total = points.Count + fishing.Count + items.Count + fishParameters.Count;
                var end = Math.Min(position + 32, total);
                while (position < end)
                {
                    if(position>=points.Count+fishing.Count+items.Count)
                    {
                        var fish=fishParameters.GetRowAt(position++-points.Count-fishing.Count-items.Count);
                        FishingNoteInfo? note=fishNotes.TryGetRow(fish.RowId,out var found) ? found:null;
                        if(FishingNotebookCopy.FromRow(fish,note) is { } entry)notebookRows.Add(entry);
                        if(Stopwatch.GetElapsedTime(started)>=TimeSpan.FromMilliseconds(2))break;
                        continue;
                    }
                    if (position >= points.Count + fishing.Count)
                    {
                        var gatheringItem = items.GetRowAt(position++ - points.Count - fishing.Count);
                        if (gatheringItem.Item.RowId != 0 && gatheringItem.Item.RowType == typeof(Item)
                            && itemPoints.TryGetRow(gatheringItem.RowId, out var directLinks))
                            foreach (var link in directLinks)
                            {
                                if (link.GatheringPoint.RowId == 0) continue;
                                var directPoint = link.GatheringPoint.Value;
                                if (directPoint.GatheringPointBase.RowId == 0 || directPoint.TerritoryType.RowId == 0) continue;
                                var basis = directPoint.GatheringPointBase.Value;
                                var type = basis.GatheringType.Value;
                                var territory = directPoint.TerritoryType.Value;
                                rows.Add(new(gatheringItem.Item.RowId, directPoint.RowId, basis.RowId, type.RowId,
                                    type.Name.ExtractText(), (uint)Math.Max(0, type.IconMain), basis.GatheringLevel,
                                    territory.RowId, territory.PlaceName.Value.Name.ExtractText(), directPoint.PlaceName.Value.Name.ExtractText(),
                                    territory.Map.RowId, false, gatheringItem.IsHidden, ReadSchedule(directPoint)));
                            }
                        if (Stopwatch.GetElapsedTime(started) >= TimeSpan.FromMilliseconds(2)) break;
                        continue;
                    }
                    if (position >= points.Count)
                    {
                        var spot = fishing.GetRowAt(position++ - points.Count);
                        if (spot.RowId != 0 && spot.TerritoryType.RowId != 0)
                        {
                            var territory = spot.TerritoryType.Value;
                            var zone = territory.PlaceName.Value.Name.ExtractText();
                            var place = spot.PlaceName.Value.Name.ExtractText();
                            var job = data.GetExcelSheet<ClassJob>().GetRow(18).Name.ExtractText();
                            foreach (var item in spot.Item)
                                if (item.RowId != 0)
                                    rows.Add(new(item.RowId, spot.RowId, spot.RowId, 100, job, 62118,
                                        spot.GatheringLevel, territory.RowId, zone, place, territory.Map.RowId, true));
                        }
                        if (Stopwatch.GetElapsedTime(started) >= TimeSpan.FromMilliseconds(2)) break;
                        continue;
                    }
                    var point = points.GetRowAt(position++);
                    if (point.GatheringPointBase.RowId != 0 && point.TerritoryType.RowId != 0)
                    {
                        var basis = point.GatheringPointBase.Value;
                        var type = basis.GatheringType.Value;
                        var territory = point.TerritoryType.Value;
                        var zone = territory.PlaceName.Value.Name.ExtractText();
                        var place = point.PlaceName.Value.Name.ExtractText();
                        foreach (var reference in basis.Item)
                        {
                            if (reference.RowId == 0 || reference.RowType != typeof(GatheringItem)) continue;
                            var gatheringItem = items.GetRow(reference.RowId);
                            var item = gatheringItem.Item;
                            if (item.RowId == 0 || item.RowType != typeof(Item)) continue;
                            rows.Add(new(item.RowId, point.RowId, basis.RowId, type.RowId,
                                type.Name.ExtractText(), (uint)Math.Max(0, type.IconMain), basis.GatheringLevel,
                                territory.RowId, zone, place, territory.Map.RowId, false, gatheringItem.IsHidden, ReadSchedule(point)));
                        }
                    }
                    if (Stopwatch.GetElapsedTime(started) >= TimeSpan.FromMilliseconds(2)) break;
                }
                service.Progress(position, total);
                if (position != total) return;
                var managed = rows.ToArray();
                var managedNotebook=notebookRows.ToArray();notebookRows.Clear();fishParameters=null;fishNotes=null;
                var version = dataVersion!;
                var token = cancellation.Token;
                if (cache is not null) toPersist = GatheringCatalogCache.Snapshot(version, managed, managedNotebook);
                rows.Clear();
                points = null;
                items = null;
                fishing = null;
                transient = null;
                rareTimes = null;
                itemPoints = null;
                build = Task.Run<(GatheringCatalog? Catalog, Exception? Error, bool FromCache)>(() =>
                {
                    try { return (new GatheringCatalog(managed, version, token,managedNotebook), null, false); }
                    catch (Exception error) { return (null, error, false); }
                });
            }
            catch (Exception error)
            {
                log.Error(error, "Tensho gathering catalog could not be built.");
                service.Fail(error);
                Stop();
            }
        }
    }
    private GatheringSchedule ReadSchedule(GatheringPoint point)
    {
        // Transient keys refer to physical points, never GatheringPointBase.
        if (point.Type == 8 || transient is null || !transient.TryGetRow(point.RowId, out var timing))
            return GatheringSchedule.Unknown;
        if (timing.GatheringRarePopTimeTable.RowId == 0)
            return GatheringSchedule.FromEphemeral(timing.EphemeralStartTime, timing.EphemeralEndTime);
        if (rareTimes is null || !rareTimes.TryGetRow(timing.GatheringRarePopTimeTable.RowId, out var rare))
            return GatheringSchedule.Unknown;
        var slots = new List<(ushort, ushort)>();
        for (var index = 0; index < rare.StartTime.Count; index++)
            slots.Add((rare.StartTime[index], rare.Duration[index]));
        return GatheringSchedule.FromRare(slots);
    }

    private void Stop()
    {
        stopped = true;
        framework.Update -= OnUpdate;
        rows.Clear();
        notebookRows.Clear();fishParameters=null;fishNotes=null;
        points = null;
        items = null;
        fishing = null;
        transient = null;
        rareTimes = null;
        itemPoints = null;
        cacheRead = null;
        toPersist = null;
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            if (!stopped) { cancellation.Cancel(); Stop(); }
            service.Clear();
            cancellation.Dispose();
        }
    }
}
