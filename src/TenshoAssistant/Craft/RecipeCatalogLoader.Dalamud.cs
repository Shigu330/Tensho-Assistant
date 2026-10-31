using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Craft;

internal sealed class RecipeCatalogLoader : IDisposable
{
    private readonly object gate = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly RecipeCatalogService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly List<RecipeDefinition> rows = [];
    private ExcelSheet<Recipe>? sheet;
    private ExcelSheet<RecipeLevelTable>? levelSheet;
    private ExcelSheet<CraftType>? craftSheet;
    private int position;
    private Task<(RecipeCatalog? Catalog, Exception? Error, bool FromCache)>? build;
    private readonly Caching.DataCache? cache;
    private Task<Caching.CachedValue<Caching.CatalogCacheData<RecipeDefinition>>?>? cacheRead;
    private bool cacheChecked;
    private string? dataVersion;
    private string? cacheVersion;
    private string? cacheScope;
    private RecipeDefinition[]? toPersist;
    private bool stopped;
    private bool disposed;

    public RecipeCatalogLoader(RecipeCatalogService service, IDataManager data, IFramework framework, IPluginLog log, Caching.DataCache? cache = null)
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
        lock (gate) { ReadUpdate(); }
    }

    private void ReadUpdate()
    {
        if (!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading || stopped) return;
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
                    log.Warning(result.Error, "Tensho recipe cache rejected; rebuilding from game data.");
                    return;
                }
                service.Publish(result.Catalog!);
                if (toPersist is { } persisted && cache is not null && cacheScope is not null && cacheVersion is not null)
                {
                    // Fire and forget: DataCache orders writes off-thread and reports its own failures.
                    var pending = cache.WriteAsync(RecipeCatalogCache.Family, cacheScope, cacheVersion,
                        RecipeCatalogCache.Snapshot(dataVersion!, persisted), DateTimeOffset.UtcNow);
                }
                toPersist = null;
                Stop();
                return;
            }

            if (dataVersion is null)
            {
                dataVersion = string.Join(";", data.GameData.Repositories.Values.OrderBy(r => r.Name)
                    .Select(r => $"{r.Name}={r.Version}"));
                cacheScope = data.Language.ToString();
                cacheVersion = RecipeCatalogCache.Version($"{dataVersion};schema={typeof(Recipe).Assembly.GetName().Version}");
            }

            if (!cacheChecked && cache is not null)
            {
                // Disk read, JSON parsing, validation and indexing all stay off the framework thread.
                cacheRead ??= cache.ReadAsync<Caching.CatalogCacheData<RecipeDefinition>>(RecipeCatalogCache.Family, cacheScope!, cacheVersion!, cancellation.Token);
                if (!cacheRead.IsCompleted) return;
                var cached = cacheRead.GetAwaiter().GetResult();
                cacheRead = null;
                cacheChecked = true;
                if (cached?.Value is { } snapshot)
                {
                    var expected = dataVersion;
                    var cachedToken = cancellation.Token;
                    build = Task.Run<(RecipeCatalog? Catalog, Exception? Error, bool FromCache)>(() =>
                    {
                        try
                        {
                            if (!RecipeCatalogCache.Valid(snapshot, expected)) throw new InvalidDataException("Recipe cache does not match the current game data.");
                            return (RecipeCatalog.Build(snapshot.Rows, expected, cachedToken), null, true);
                        }
                        catch (Exception error) { return (null, error, true); }
                    });
                    return;
                }
            }

            sheet ??= data.GetExcelSheet<Recipe>();
            levelSheet ??= data.GetExcelSheet<RecipeLevelTable>();
            craftSheet ??= data.GetExcelSheet<CraftType>();
            var end = Math.Min(position + 128, sheet.Count);
            var started = Stopwatch.GetTimestamp();
            while (position < end)
            {
                var row = sheet.GetRowAt(position++);
                if (row.RowId != 0 && row.ItemResult.RowId != 0)
                {
                    var ingredients = new List<RecipeIngredient>();
                    for (var i = 0; i < row.Ingredient.Count && i < row.AmountIngredient.Count; i++)
                    {
                        var itemId = row.Ingredient[i].RowId;
                        var amount = row.AmountIngredient[i];
                        if (itemId != 0 && amount > 0) ingredients.Add(new(itemId, amount));
                    }
                    var table=levelSheet.GetRow(row.RecipeLevelTable.RowId);
                    var level = table.ClassJobLevel;
                    var craftType = craftSheet.GetRow(row.CraftType.RowId).Name.ExtractText();
                    var simulation=RecipeSimulationCopy.Copy(row,table);
                    rows.Add(new RecipeDefinition(row.RowId, row.ItemResult.RowId, row.AmountResult,
                        craftType, level, ingredients, row.CraftType.RowId,simulation));
                }
                if (Stopwatch.GetElapsedTime(started) >= TimeSpan.FromMilliseconds(2)) break;
            }
            service.Progress(position, sheet.Count);
            if (position != sheet.Count) return;
            var managed = rows.ToArray();
            var version = dataVersion!;
            var token = cancellation.Token;
            sheet = null; levelSheet = null; craftSheet = null; rows.Clear();
            if (cache is not null) toPersist = managed;
            build = Task.Run<(RecipeCatalog? Catalog, Exception? Error, bool FromCache)>(() =>
            {
                try { return (RecipeCatalog.Build(managed, version, token), null, false); }
                catch (Exception error) { return (null, error, false); }
            });
        }
        catch (Exception error)
        {
            log.Error(error, "Tensho recipe catalog could not be built.");
            service.Fail(error);
            Stop();
        }
    }

    private void Stop()
    {
        if (stopped) return;
        stopped = true;
        framework.Update -= OnUpdate;
        sheet = null;
        levelSheet = null; craftSheet = null;
        rows.Clear();
        cacheRead = null;
        toPersist = null;
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true; cancellation.Cancel();
            Stop(); service.Clear(); cancellation.Dispose();
        }
    }
}
