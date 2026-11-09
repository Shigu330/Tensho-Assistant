using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Market;

/// <summary>
/// I-05: while the game's retainer sale list is open, copies what it shows (framework thread, every half second). Never calls a game function that changes anything.
/// The window is found by name and confirmed by the agent's own window id; the row count is bounded before any row is read. Anything out of shape gives
/// « unavailable », never a partial table. The first lines are logged for the in-game check (docs/evidence/0.508-retainer-sales.md).
/// </summary>
internal sealed class RetainerSalesReader : IDisposable
{
    private const string Window = "RetainerSellList";
    private static readonly RetainerSalesState Closed = new(RetainerSalesStatus.Closed, [], new HashSet<string>(), "", false);
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private readonly Func<bool> enabled;
    private RetainerSalesState state = Closed;
    private DateTimeOffset nextRead;
    private string? lastRaw;
    private int rawLines;
    private string? failure;

    public RetainerSalesState State => Volatile.Read(ref state);

    // X-76 H9 (0.922): developer captures are logged in developer mode only.
    private readonly Func<bool>? developer;
    public RetainerSalesReader(IFramework framework, IGameGui gui, IClientState client, IPlayerState player, IPluginLog log, Func<bool> enabled, Func<bool>? developer = null)
    {this.developer=developer;
        this.framework = framework; this.gui = gui; this.client = client; this.player = player; this.log = log; this.enabled = enabled;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void Publish(RetainerSalesState next) => Volatile.Write(ref state, next);

    private unsafe void OnUpdate(IFramework _)
    {
        if (!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
        var now = DateTimeOffset.UtcNow;
        if (now < nextRead) return;
        nextRead = now + TimeSpan.FromMilliseconds(500);
        try
        {
            if (!enabled() || !client.IsLoggedIn || !player.IsLoaded || gui.GameUiHidden) { if (State.Status != RetainerSalesStatus.Closed) Publish(Closed); return; }
            var handle = gui.GetAddonByName(Window);
            if (handle.IsNull || !handle.IsReady || !handle.IsVisible) { if (State.Status != RetainerSalesStatus.Closed) Publish(Closed); return; }
            var agent = AgentRetainer.Instance();
            var unit = (AtkUnitBase*)handle.Address;
            if (agent == null || agent->RetainerSellListAddonId != unit->Id) { Fail("the agent does not own this window"); return; }
            var count = agent->SellListEntryCount;
            var entries = agent->SellListEntries;
            if (count < 0 || count > RetainerSales.MaximumListings || count > entries.Length) { Fail($"row count {count}"); return; }
            var listings = new List<RetainerListing>(count);
            var raw = new List<string>(count);
            for (var row = 0; row < count; row++)
            {
                ref var entry = ref entries[row];
                var text = entry.UnitPriceText.ToString();
                var (item, hq) = RetainerSales.SplitItem(entry.ItemId);
                if (item == 0 || entry.Quantity is <= 0 or > 9999) { Fail($"row {row} out of shape"); return; }
                listings.Add(new(row, item, hq, entry.Quantity, RetainerSales.ParsePrice(text)));
                raw.Add($"{entry.ItemId}x{entry.Quantity}@{entry.InventorySlot}={text}");
            }
            var own = new HashSet<string>();
            var manager = RetainerManager.Instance();
            if (manager != null && manager->IsReady)
                foreach (ref var retainer in manager->Retainers)
                    if (retainer.RetainerId != 0 && retainer.NameString is { Length: > 0 } name) own.Add(name);
            var home = player.HomeWorld.IsValid ? player.HomeWorld.Value.Name.ExtractText() : "";
            var atHome = player.HomeWorld.RowId != 0 && player.HomeWorld.RowId == player.CurrentWorld.RowId;
            failure = null;
            Publish(new(RetainerSalesStatus.Ready, listings.AsReadOnly(), own, home, atHome));
            var capture = string.Join(";", raw);
            if (developer?.Invoke() == true && capture != lastRaw && rawLines < 40) { lastRaw = capture; rawLines++; log.Information("Retainer sale list: {0} rows: {1}", count, capture); }
        }
        catch (Exception error) { Fail($"{error.GetType().Name}: {error.Message}"); }
    }

    private void Fail(string reason)
    {
        Publish(Closed with { Status = RetainerSalesStatus.Unavailable });
        if (failure != reason) { failure = reason; log.Warning("Retainer sale list unavailable: {0}", reason); }
    }

    public void Dispose() => TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
}
