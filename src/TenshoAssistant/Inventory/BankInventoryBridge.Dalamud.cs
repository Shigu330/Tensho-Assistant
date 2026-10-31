using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.Inventory;
using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Inventory;

internal sealed record BankMenuContext(BankKey Key, string Name, string AddonName, Vector2 Anchor, bool Loaded);
internal sealed record BankMenuState(ulong CharacterId, BankMenuContext? Retainer, BankMenuContext? Saddlebag,
    BankMenuContext? Premium, string? Message);

/// <summary>Observes menu anchors on framework, and copies inventory only after an explicit import request.</summary>
internal sealed class BankInventoryBridge : IDisposable
{
    private readonly object gate = new();
    private readonly InventoryBankService bank;
    private readonly IGameInventory inventory;
    private readonly IGameGui gui;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly long started = Stopwatch.GetTimestamp();
    private TimeSpan nextPoll;
    private TimeSpan retainerSince;
    private BankKey? lastRetainer;
    private BankKey? requested;
    private Task<BankImportResult>? importing;
    private bool disposed;
    private BankMenuState state = new(0, null, null, null, null);
    public BankMenuState State => Volatile.Read(ref state);

    public BankInventoryBridge(InventoryBankService bank, IGameInventory inventory, IGameGui gui,
        IPlayerState player, IClientState client, IFramework framework, IPluginLog log)
    {
        this.bank = bank;
        this.inventory = inventory;
        this.gui = gui;
        this.player = player;
        this.client = client;
        this.framework = framework;
        this.log = log;
        framework.Update += OnUpdate;
    }

    public void RequestImport(BankKey key)
    {
        lock (gate)
        {
            if (!disposed && requested is null && importing is null) requested = key;
        }
    }

    private ulong CharacterId => client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;

    // Shared live-window checks, used by explicit sort requests too. Never substitutes the persisted bank cache.
    internal BankMenuContext RequireOpenForSort(BankKey key)
    {
        if(!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)
            throw new InvalidOperationException("La lecture du rangement exige le thread framework.");
        lock(gate)
        {
            if(disposed || CharacterId!=key.CharacterId)throw new InvalidOperationException("Personnage du rangement modifié.");
            var context=key.Kind==BankStorageKind.Retainer ? GetRetainer(CharacterId):GetSaddle(CharacterId,key.Kind);
            if(context?.Key!=key || !context.Loaded || key.Kind==BankStorageKind.Retainer
                && (lastRetainer!=key || Stopwatch.GetElapsedTime(started)-retainerSince<TimeSpan.FromMilliseconds(500)))
                throw new InvalidOperationException("Ouvrez le bon rangement et attendez son chargement complet.");
            return context;
        }
    }

    internal BankEntry CaptureOpenForSort(BankKey key)
    {
        var context=RequireOpenForSort(key);
        var scope=InventorySortScope.FromStorage(key);
        var types=BankStorageScope.ContainerIds(key.Kind).Select(id=>(GameInventoryType)id).ToArray();
        var snapshot=InventoryNativeReader.Capture(inventory,new(scope.InventoryOwner),types,"Dalamud/visual-sort/live/"+key.Kind);
        RequireOpenForSort(key);
        var entry=new BankEntry(key,context.Name,snapshot);BankEntryValidator.Validate(entry);
        return entry;
    }

    private void OnUpdate(IFramework _)
    {
        if (!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
        lock (gate)
        {
            if (disposed) return;
            var now = Stopwatch.GetElapsedTime(started);
            if (now < nextPoll && requested is null && importing?.IsCompleted != true) return;
            nextPoll = now + TimeSpan.FromMilliseconds(100);
            var contextUpdated = false;
            try
            {
                var character = CharacterId;
                var retainer = GetRetainer(character);
                if (retainer?.Key != lastRetainer)
                {
                    lastRetainer = retainer?.Key;
                    retainerSince = now;
                }
                if (retainer is not null && now - retainerSince < TimeSpan.FromMilliseconds(500))
                    retainer = retainer with { Loaded = false };
                var saddle = GetSaddle(character, BankStorageKind.Saddlebag);
                var premium = GetSaddle(character, BankStorageKind.PremiumSaddlebag);
                var message = state.Message;
                if (importing?.IsCompleted == true)
                {
                    message = importing.GetAwaiter().GetResult().Message;
                    importing = null;
                }
                Volatile.Write(ref state, new(character, retainer, saddle, premium, message));
                contextUpdated = true;
                if (requested is not { } key) return;
                requested = null;
                var context = new[] { retainer, saddle, premium }.FirstOrDefault(c => c?.Key == key);
                if (context is null || !context.Loaded || character != key.CharacterId)
                    throw new InvalidOperationException("Inventaire fermé, propriétaire changé ou conteneurs non chargés. Rouvrez le rangement et réessayez.");

                var types = BankStorageScope.ContainerIds(key.Kind).Select(id => (GameInventoryType)id).ToArray();
                var owner = new InventoryOwnerId(key.Kind == BankStorageKind.Retainer ? key.StorageId : key.CharacterId);
                var snapshot = InventoryNativeReader.Capture(inventory, owner, types, $"Dalamud/manual-bank/{key.Kind}");
                var after = key.Kind == BankStorageKind.Retainer ? GetRetainer(CharacterId) : GetSaddle(CharacterId, key.Kind);
                if (CharacterId != key.CharacterId || after?.Key != key || after?.Loaded != true
                    || snapshot.Containers.Values.Any(c => c.Coverage != InventoryCoverage.Complete))
                    throw new InvalidOperationException("Lecture incomplète ou identité modifiée. L'ancien cache est conservé ; réessayez une fois l'inventaire chargé.");
                importing = bank.ImportAsync(new BankEntry(key, context.Name, snapshot));
                Volatile.Write(ref state, state with { Message = "Import en cours…" });
            }
            catch (Exception error)
            {
                if (state.Message != error.Message) log.Warning(error, "Tensho manual inventory import/context failed.");
                Volatile.Write(ref state, contextUpdated ? state with { Message = error.Message }
                    : new(0, null, null, null, error.Message));
            }
        }
    }

    private AtkUnitBasePtr FindVisible(params string[] names)
    {
        if (gui.GameUiHidden) return default;
        foreach (var name in names)
        {
            var addon = gui.GetAddonByName(name);
            if (!addon.IsNull && addon.IsReady && addon.IsVisible) return addon;
        }
        return default;
    }

    private unsafe BankMenuContext? GetRetainer(ulong character)
    {
        if (character == 0) return null;
        var addon = FindVisible("InventoryRetainer", "InventoryRetainerLarge");
        if (addon.IsNull) return null;
        var manager = RetainerManager.Instance();
        if (manager == null || !manager->IsReady) return null;
        var retainer = manager->GetActiveRetainer();
        if (retainer == null || retainer->RetainerId == 0 || retainer->RetainerId != manager->LastSelectedRetainerId) return null;
        var key = new BankKey(character, BankStorageKind.Retainer, retainer->RetainerId);
        return new(key, retainer->NameString, addon.Name, addon.Position + new Vector2(addon.ScaledWidth + 6, 20), IsLoaded(key.Kind));
    }

    private BankMenuContext? GetSaddle(ulong character, BankStorageKind kind)
    {
        if (character == 0) return null;
        var addon = FindVisible("InventoryBuddy", "InventoryBuddy2");
        if (addon.IsNull) return null;
        return new(new BankKey(character, kind, 0), kind == BankStorageKind.Saddlebag ? "Sacoche chocobo" : "Sacoche chocobo supplémentaire",
            addon.Name, addon.Position + new Vector2(addon.ScaledWidth + 6, 20), IsLoaded(kind));
    }

    private static unsafe bool IsLoaded(BankStorageKind kind)
    {
        var manager = InventoryManager.Instance();
        if (manager == null) return false;
        foreach (var id in BankStorageScope.ContainerIds(kind))
        {
            var container = manager->GetInventoryContainer((InventoryType)id);
            if (container == null || !container->IsLoaded || container->Items == null
                || (int)container->Type != id || container->Size is <= 0 or > 1000) return false;
        }
        return true;
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            requested = null;
            framework.Update -= OnUpdate;
            Volatile.Write(ref state, new(0, null, null, null, null));
        }
    }
}
