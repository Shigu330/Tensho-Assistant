using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Maps;

internal sealed class MapCatalogLoader : IDisposable
{
    private readonly object gate = new();
    private readonly MapCatalogService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly CancellationTokenSource cancellation = new();
    private readonly List<MapDefinition> copied = [];
    private ExcelSheet<Map>? sheet;
    private Task<MapCatalog>? build;
    private int position;
    private bool stopped;
    private bool disposed;
    public MapCatalogLoader(MapCatalogService service, IDataManager data, IFramework framework, IPluginLog log)
    {
        this.service = service; this.data = data; this.framework = framework; this.log = log;
        try { framework.Update += Update; }
        catch { cancellation.Dispose(); throw; }
    }
    private void Update(IFramework unusedFramework)
    {
        lock (gate)
        {
            if (stopped || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            try
            {
                if (build is not null)
                {
                    if (!build.IsCompleted) return;
                    service.Publish(build.GetAwaiter().GetResult()); Stop(); return;
                }
                sheet ??= data.GetExcelSheet<Map>();
                var end = Math.Min(position + 32, sheet.Count);
                var started = Stopwatch.GetTimestamp();
                while (position < end)
                {
                    var row = sheet.GetRowAt(position++);
                    if (row.RowId != 0 && row.TerritoryType.RowId != 0 && row.SizeFactor != 0)
                        copied.Add(new(row.RowId, row.TerritoryType.RowId, row.PlaceNameRegion.Value.Name.ExtractText(),
                            row.PlaceName.Value.Name.ExtractText(), row.PlaceNameSub.Value.Name.ExtractText(), row.MapIndex, row.SizeFactor,row.PlaceName.RowId,
                            MapProjection.TexturePath(row.Id.ExtractText()),row.OffsetX,row.OffsetY,row.MapMarkerRange,row.PlaceNameRegion.RowId,
                            row.PriorityCategoryUI,row.PriorityUI));
                    if (Stopwatch.GetElapsedTime(started) >= TimeSpan.FromMilliseconds(2)) break;
                }
                service.Progress(position, sheet.Count);
                if (position != sheet.Count) return;
                var managed = copied.ToArray();
                copied.Clear(); sheet = null;
                var token = cancellation.Token;
                build = Task.Run(() => new MapCatalog(managed, token));
                // Observe faults even if disposal ends publication before the next callback.
                _ = build.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception error)
            {
                service.Fail(error); log.Warning(error, "Tensho map catalog unavailable."); Stop();
            }
        }
    }
    private void Stop()
    {
        stopped = true; framework.Update -= Update; copied.Clear(); sheet = null;
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true; cancellation.Cancel();
            if (!stopped) Stop();
            service.Clear(); cancellation.Dispose();
        }
    }
}
