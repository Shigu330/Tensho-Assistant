using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Market;

internal sealed class MarketWorldReader : IDisposable
{
    private readonly MarketService service;
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private readonly IDataManager data;
    private readonly object gate = new();
    private bool disposed;
    private long retryAt;
    private uint worldId;
    private ulong character;
    public MarketWorldReader(MarketService service, IFramework framework, IPlayerState player, IClientState client, IPluginLog log, IDataManager data)
    {
        this.service = service; this.framework = framework; this.player = player; this.client = client; this.log = log; this.data = data;
        framework.Update += Update;
    }
    private void Update(IFramework _)
    {
        lock (gate)
        {
            if (disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            if (!client.IsLoggedIn || !player.IsLoaded || player.ContentId == 0)
            { Clear(); return; }
            if (Stopwatch.GetTimestamp() < retryAt) return;
            try
            {
                var owner = player.ContentId;
                var world = player.CurrentWorld;
                if (world.RowId == 0) { Clear(); return; }
                if (character == owner && worldId == world.RowId) return;
                var row = world.Value;
                var name = row.Name.ExtractText();
                // Names copied once per world change; the hover only reads this managed map.
                var dataCenter = row.DataCenter.RowId;
                var dataCenterName = dataCenter == 0 ? "" : row.DataCenter.Value.Name.ExtractText();
                var names = new Dictionary<uint, string>();
                if (dataCenter != 0)
                    foreach (var candidate in data.GetExcelSheet<World>())
                        if (candidate.IsPublic && candidate.DataCenter.RowId == dataCenter) names[candidate.RowId] = candidate.Name.ExtractText();
                if (!client.IsLoggedIn || !player.IsLoaded || player.ContentId != owner || player.CurrentWorld.RowId != world.RowId)
                { Clear(); return; }
                service.SetWorld(owner, world.RowId, name, dataCenterName, names);
                character = owner;
                worldId = world.RowId;
            }
            catch (Exception error)
            {
                Clear();
                retryAt = Stopwatch.GetTimestamp() + 5 * Stopwatch.Frequency;
                log.Warning(error, "Tensho current market world unavailable; retry in five seconds.");
            }
        }
    }
    private void Clear()
    {
        character = 0; worldId = 0;
        service.SetWorld(0, 0, "");
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            framework.Update -= Update;
            Clear();
        }
    }
}
