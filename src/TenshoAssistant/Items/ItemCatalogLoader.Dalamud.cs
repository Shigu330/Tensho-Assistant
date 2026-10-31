using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Items;

/// <summary>Copies bounded batches on framework, indexes only managed data on a cancellable worker.</summary>
internal sealed class ItemCatalogLoader : IDisposable
{
    private readonly object gate = new();
    private readonly ItemCatalogService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly CancellationTokenSource cancellation = new();
    private readonly List<ItemDefinition> rows = [];
    private ExcelSheet<Item>? sheet;
    private ExcelSheet<Item>? englishItems;
    private bool englishUnavailable;
    private ItemCatalogMetadata? metadata;
    private Task<(ItemCatalog? Catalog, Exception? Error, bool FromCache)>? build;
    private readonly Caching.DataCache? cache;
    private Task<Caching.CachedValue<ItemCatalogCacheData>?>? cacheRead;
    private bool cacheChecked;
    private ItemDefinition[]? toPersist;
    private int position;
    private bool stopped;
    private bool disposed;

    public ItemCatalogLoader(ItemCatalogService service, IDataManager data, IFramework framework, IPluginLog log, Caching.DataCache? cache = null)
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
                    var result = build.GetAwaiter().GetResult(); // Completion already established; never waits.
                    build = null;
                    if (result.Error is not null)
                    {
                        if (!result.FromCache) throw result.Error;
                        // A rejected disk copy is never published: the next ticks copy the game sheet as before.
                        log.Warning(result.Error, "Tensho item cache rejected; rebuilding from game data.");
                        return;
                    }
                    service.Publish(result.Catalog!);
                    if (toPersist is { } persisted && cache is not null && metadata is { } written)
                    {
                        // Fire and forget: DataCache orders writes off-thread and reports its own failures.
                        var pending = cache.WriteAsync(ItemCatalogCache.Family, ItemCatalogCache.Scope(written), ItemCatalogCache.Version(written),
                            ItemCatalogCache.Snapshot(written, persisted), DateTimeOffset.UtcNow);
                    }
                    toPersist = null;
                    Stop();
                    return;
                }

                if (sheet is null)
                {
                    sheet = data.GetExcelSheet<Item>();
                    var versions = string.Join(";", data.GameData.Repositories.Values.OrderBy(r => r.Name)
                        .Select(r => $"{r.Name}={r.Version}"));
                    metadata = new ItemCatalogMetadata("Lumina.Excel/Item", sheet.Language.ToString(),
                        $"{versions};schema={typeof(Item).Assembly.GetName().Version}", 1);
                }

                if (!cacheChecked && cache is not null)
                {
                    // Disk read, JSON parsing, validation and indexing all stay off the framework thread.
                    var key = metadata!;
                    cacheRead ??= cache.ReadAsync<ItemCatalogCacheData>(ItemCatalogCache.Family, ItemCatalogCache.Scope(key),
                        ItemCatalogCache.Version(key), cancellation.Token);
                    if (!cacheRead.IsCompleted) return;
                    var cached = cacheRead.GetAwaiter().GetResult();
                    cacheRead = null;
                    cacheChecked = true;
                    if (cached?.Value is { } snapshot)
                    {
                        var cachedToken = cancellation.Token;
                        build = Task.Run<(ItemCatalog? Catalog, Exception? Error, bool FromCache)>(() =>
                        {
                            try
                            {
                                if (!ItemCatalogCache.Valid(snapshot, key)) throw new InvalidDataException("Item cache does not match the current game data.");
                                return (ItemCatalog.Build(snapshot.Items, ItemCatalogCache.FromCache(key), cachedToken), null, true);
                            }
                            catch (Exception error) { return (null, error, true); }
                        });
                        return;
                    }
                }

                var started = Stopwatch.GetTimestamp();
                var batchEnd = Math.Min(position + 128, sheet.Count);
                while (position < batchEnd)
                {
                    var row = sheet.GetRowAt(position++);
                    var name = row.Name.ExtractText();
                    if (row.RowId != 0 && !string.IsNullOrWhiteSpace(name))
                    {
                        var uiCategory = row.ItemUICategory.Value;
                        var slots=row.EquipSlotCategory.Value;
                        // The English row gives the community wiki title and the tool bonus wording; unavailable means both stay unknown.
                        Item? english=null;
                        if(!englishUnavailable)
                        {
                            try
                            {
                                englishItems ??= data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.English);
                                if(englishItems.TryGetRow(row.RowId,out var found))english=found;
                            }
                            catch(Exception error){englishUnavailable=true;englishItems=null;log.Warning(error,"English item names unavailable; wiki titles and Good-condition bonus remain unknown.");}
                        }
                        CraftToolDefinition? tool=null;
                        if(slots.MainHand>0 && row.ClassJobUse.RowId is >=8 and <=15)
                            tool=new(row.ClassJobUse.RowId,CraftToolBonuses.FromDeclaredMetadata(english?.Description.ExtractText(),row.ItemSpecialBonus.RowId,row.ItemSpecialBonusParam));
                        var equipment=row.EquipSlotCategory.RowId==0 ? EquipmentSlots.None : EquipmentSlotMap.FromNative(
                            slots.Head,slots.Body,slots.Gloves,slots.Legs,slots.Feet,slots.Ears,slots.Neck,slots.Wrists,slots.FingerL,slots.FingerR,slots.MainHand,slots.OffHand,slots.SoulCrystal);
                        rows.Add(new ItemDefinition(row.RowId, name, row.CanBeHq, row.Icon,
                            uiCategory.RowId, uiCategory.Name.ExtractText(), !row.IsUntradable && row.ItemSearchCategory.RowId != 0,
                            row.AdditionalData.RowType == typeof(TripleTriadCard) ? row.AdditionalData.RowId : 0,equipment,tool,ItemOrchestrionCopy.Identity(row),
                            row.LevelItem.RowId,row.LevelEquip,row.Rarity,row.PriceLow,
                            (row.IsUnique ? ItemTraits.Unique:0)|(row.IsUntradable ? ItemTraits.Untradable:0)|(row.DyeCount>0 ? ItemTraits.Dyeable:0)
                            |(row.ItemRepair.RowId!=0 ? ItemTraits.Repairable:0)|(row.IsCollectable ? ItemTraits.Collectable:0),
                            english?.Name.ExtractText() ?? ""));
                    }
                    if (Stopwatch.GetElapsedTime(started) >= TimeSpan.FromMilliseconds(2)) break;
                }
                service.Progress(position, sheet.Count);
                if (position != sheet.Count) return;

                var managed = rows.ToArray();
                var context = metadata!;
                var token = cancellation.Token;
                sheet = null;englishItems=null;
                rows.Clear();
                if (cache is not null) toPersist = managed;
                build = Task.Run<(ItemCatalog? Catalog, Exception? Error, bool FromCache)>(() =>
                {
                    try { return (ItemCatalog.Build(managed, context, token), null, false); }
                    catch (Exception error) { return (null, error, false); }
                });
            }
            catch (Exception error)
            {
                log.Error(error, "Tensho item catalog could not be built.");
                service.Fail();
                Stop();
            }
        }
    }

    private void Stop()
    {
        stopped = true;
        framework.Update -= OnUpdate;
        sheet = null;
        englishItems=null;
        rows.Clear();
        cacheRead = null;
        toPersist = null;
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            cancellation.Cancel();
            Stop();
            service.Clear();
            // No worker can publish: publication occurs only through the now-removed Update callback.
            cancellation.Dispose();
        }
    }
}
