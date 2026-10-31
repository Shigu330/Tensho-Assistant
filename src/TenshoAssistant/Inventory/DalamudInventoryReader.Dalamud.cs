using System.Diagnostics;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Inventory;

internal sealed class DalamudInventoryReader : IDisposable
{
    private static readonly GameInventoryType[] PlayerContainers =
        [GameInventoryType.Inventory1, GameInventoryType.Inventory2, GameInventoryType.Inventory3,
            GameInventoryType.Inventory4, GameInventoryType.Crystals];
    private readonly RegistrationScope registrations = new();
    private readonly InventoryCaptureCoordinator coordinator;
    private readonly IFramework framework;
    private readonly long startedAt = Stopwatch.GetTimestamp();
    private TimeSpan lastOrderRefresh;

    public DalamudInventoryReader(InventoryService service, IGameInventory inventory,
        IPlayerState player, IClientState client, IFramework framework, IPluginLog log)
    {
        this.framework = framework;
        coordinator = new InventoryCaptureCoordinator(service,
            () => client.IsLoggedIn && player.IsLoaded && player.ContentId != 0
                ? new InventoryOwnerId(player.ContentId) : null,
            owner => Capture(inventory, owner),
            error => log.Error(error, "Tensho inventory capture failed; state cleared, retry in five seconds."));
        try
        {
            registrations.Register(() => inventory.InventoryChanged += OnInventoryChanged,
                () => inventory.InventoryChanged -= OnInventoryChanged);
            registrations.Register(() => client.Login += OnLogin, () => client.Login -= OnLogin);
            registrations.Register(() => client.Logout += OnLogout, () => client.Logout -= OnLogout);
            registrations.Register(() => framework.Update += OnUpdate, () => framework.Update -= OnUpdate);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private static InventorySnapshot Capture(IGameInventory inventory, InventoryOwnerId owner) =>
        InventoryNativeReader.Capture(inventory, owner, PlayerContainers, "Dalamud.IGameInventory/player-bags+crystals");

    private void OnInventoryChanged(IReadOnlyCollection<InventoryEventArgs> _) => coordinator.Invalidate();
    private void OnLogin() => coordinator.Invalidate();
    private void OnLogout(int type, int code) => coordinator.Invalidate();

    private void OnUpdate(IFramework _)
    {
        if (framework.IsInFrameworkUpdateThread && !framework.IsFrameworkUnloading)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            if (elapsed - lastOrderRefresh >= TimeSpan.FromSeconds(1))
            {
                lastOrderRefresh = elapsed;
                coordinator.Invalidate();
            }
            coordinator.Tick(elapsed);
        }
    }

    public void Dispose()
    {
        try { registrations.Dispose(); }
        finally { coordinator.Dispose(); }
    }
}
