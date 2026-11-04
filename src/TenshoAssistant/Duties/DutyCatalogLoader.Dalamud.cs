using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Duties;

internal sealed class DutyCatalogLoader : IDisposable
{
    private readonly object gate = new();
    private readonly DutyCatalogService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly CancellationTokenSource cancellation = new();
    private readonly List<DutyDefinition> copied = [];
    private ExcelSheet<InstanceContent>? sheet;
    private Task<DutyCatalog>? build;
    private int position;
    private bool stopped;
    private bool disposed;
    public DutyCatalogLoader(DutyCatalogService service, IDataManager data, IFramework framework, IPluginLog log)
    {
        this.service = service; this.data = data; this.framework = framework; this.log = log;
        try { framework.Update += Update; }
        catch { cancellation.Dispose(); throw; }
    }
    private void Update(IFramework unused)
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
                sheet ??= data.GetExcelSheet<InstanceContent>();
                var end = Math.Min(position + 32, sheet.Count);
                var start = Stopwatch.GetTimestamp();
                while (position < end)
                {
                    var row = sheet.GetRowAt(position++);
                    if (row.RowId != 0 && row.ContentFinderCondition.RowId != 0)
                    {
                        // R-14 (0.507): a dangling link skips this duty, not the whole catalogue.
                        if (row.ContentFinderCondition.ValueNullable is not { } finder) continue;
                        var name = finder.Name.ExtractText();
                        // Content is polymorphic: only a reciprocal regular-instance link is accepted.
                        if (finder.ContentLinkType == 1 && finder.Content.RowId == row.RowId
                            && finder.TerritoryType.RowId != 0 && !string.IsNullOrWhiteSpace(name))
                            copied.Add(new(finder.RowId,row.RowId,finder.TerritoryType.RowId,name,
                                finder.ContentType.ValueNullable?.Name.ExtractText() ?? "",finder.ClassJobLevelRequired,finder.ContentType.ValueNullable?.Icon ?? 0));
                    }
                    if (Stopwatch.GetElapsedTime(start) >= TimeSpan.FromMilliseconds(2)) break;
                }
                service.Progress(position,sheet.Count);
                if (position != sheet.Count) return;
                // 0.492 (R-14): no duty copied means unreadable game data, not « no duty »: the catch publishes the failure.
                if (copied.Count == 0) throw new InvalidDataException("InstanceContent sheet gave no duty.");
                var managed = copied.ToArray(); copied.Clear(); sheet = null;
                var token = cancellation.Token;
                build = Task.Run(() => new DutyCatalog(managed,token));
                _ = build.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception error) { service.Fail(error); log.Warning(error,"Tensho duty catalog unavailable."); Stop(); }
        }
    }
    private void Stop() { stopped = true; framework.Update -= Update; copied.Clear(); sheet = null; }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true; cancellation.Cancel(); if (!stopped) Stop(); service.Clear(); cancellation.Dispose();
        }
    }
}
