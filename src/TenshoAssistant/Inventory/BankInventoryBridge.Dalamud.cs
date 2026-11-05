using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.Inventory;
using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Inventory;

internal sealed record BankMenuContext(BankKey Key, string Name, string AddonName, Vector2 Anchor, bool Loaded, Vector2 AddonAt = default);
internal sealed record BankMenuState(ulong CharacterId, BankMenuContext? Retainer, BankMenuContext? Saddlebag,
    BankMenuContext? Premium, string? Message, IReadOnlyList<(ulong Id, string Name)>? KnownRetainers = null, bool ReadsUnavailable = false,
    BankMenuContext? FreeCompany = null);

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
    // Automatic capture (0.167): each storage is copied once per opening, then again every 30 s while it stays open.
    private readonly Func<bool> autoCapture;
    // Armoire rows (Cabinet sheet: row → item), given by the plugin (0.236).
    private readonly Func<IReadOnlyList<(uint Row, uint Item)>> cabinetRows;
    // Pieces of an outfit stored as one item in the glamour dresser (MirageStoreSetItem: outfit item → pieces), 0.240.
    private readonly Func<uint, IReadOnlyList<uint>> outfitPieces;
    private readonly HashSet<(uint Outfit, string Bits)> loggedOutfits = [];
    private bool outfitSlotsFailed;
    private readonly Dictionary<BankKey, TimeSpan> autoCaptured = [];
    private IReadOnlyList<(ulong Id, string Name)>? knownRetainers;
    // 0.329 (R-14): optional reads that failed once (logged once); the first failure of each posts ReadUnavailable.
    private const string ReadUnavailable = "A storage could not be read (perhaps after a game update); the other storages are still recorded.";
    private readonly HashSet<string> failedReads = [];
    private bool importingSilent;
    private bool disposed;
    private BankMenuState state = new(0, null, null, null, null);
    public BankMenuState State => Volatile.Read(ref state);

    public BankInventoryBridge(InventoryBankService bank, IGameInventory inventory, IGameGui gui,
        IPlayerState player, IClientState client, IFramework framework, IPluginLog log, Func<bool>? autoCapture = null,
        Func<IReadOnlyList<(uint Row, uint Item)>>? cabinetRows = null, Func<uint, IReadOnlyList<uint>>? outfitPieces = null)
    {
        this.cabinetRows = cabinetRows ?? (() => []);
        this.outfitPieces = outfitPieces ?? (_ => []);
        this.autoCapture = autoCapture ?? (() => false);
        this.bank = bank;
        this.inventory = inventory;
        this.gui = gui;
        this.player = player;
        this.client = client;
        this.framework = framework;
        this.log = log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
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
            throw new InvalidOperationException("Reading a storage requires the framework thread.");
        lock(gate)
        {
            if(disposed || CharacterId!=key.CharacterId)throw new InvalidOperationException("Storage character changed.");
            var context=key.Kind==BankStorageKind.Retainer ? GetRetainer(CharacterId):GetSaddle(CharacterId,key.Kind);
            if(context?.Key!=key || !context.Loaded || key.Kind==BankStorageKind.Retainer
                && (lastRetainer!=key || Stopwatch.GetElapsedTime(started)-retainerSince<TimeSpan.FromMilliseconds(500)))
                throw new InvalidOperationException("Open the right storage and wait until it is fully loaded.");
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
            var character = 0UL;
            try
            {
                character = CharacterId;
                // 0.329 (R-14): each optional native read is guarded on its own, so one lost signature no longer hides the others.
                var retainer = Optional(GetRetainer, character, null, "retainer");
                if (retainer?.Key != lastRetainer)
                {
                    lastRetainer = retainer?.Key;
                    retainerSince = now;
                }
                if (retainer is not null && now - retainerSince < TimeSpan.FromMilliseconds(500))
                    retainer = retainer with { Loaded = false };
                var saddle = Optional(c => GetSaddle(c, BankStorageKind.Saddlebag), character, null, "saddlebag");
                var premium = Optional(c => GetSaddle(c, BankStorageKind.PremiumSaddlebag), character, null, "premium saddlebag");
                // 0.350 (g, I-09): the company chest, while its window is open (the game sends its pages only then).
                var company = Optional(GetFreeCompany, character, null, "company chest");
                // Armoury chest (0.235): always in memory once logged in; read like a storage, kept in the Bank, refreshed every 30 s.
                var armoury = character == 0 ? null : new BankMenuContext(new BankKey(character, BankStorageKind.Armoury, 0), "Arsenal", "",
                    Vector2.Zero, Optional(static _ => IsLoaded(BankStorageKind.Armoury), character, false, "armoury"));
                // Glamour dresser and armoire (0.236): known once opened in the game this session (PrismBoxLoaded, IsCabinetLoaded).
                var dresser = character == 0 ? null : new BankMenuContext(new BankKey(character, BankStorageKind.GlamourDresser, 0), "Glamour dresser", "",
                    Vector2.Zero, Optional(static _ => DresserLoaded(), character, false, "glamour dresser"));
                var armoire = character == 0 ? null : new BankMenuContext(new BankKey(character, BankStorageKind.Armoire, 0), "Armoire", "",
                    Vector2.Zero, Optional(static _ => ArmoireLoaded(), character, false, "armoire"));
                // 0.345 (g, I-03): the character's own bags, crystals and currencies, recorded like the armoury so the other characters'
                // can be seen; named after the character (« Name (World) ») for the overview of every character.
                var who = character == 0 ? "" : CharacterLabel();
                BankMenuContext? Own(BankStorageKind kind, string read) => character == 0 || who.Length == 0 ? null
                    : new BankMenuContext(new BankKey(character, kind, 0), who, "", Vector2.Zero, Optional(_ => IsLoaded(kind), character, false, read));
                var bags = Own(BankStorageKind.Bags, "bags");
                var crystals = Own(BankStorageKind.Crystals, "crystals");
                var currencies = Own(BankStorageKind.Currency, "currencies");
                var message = state.Message;
                if (importing?.IsCompleted == true)
                {
                    // Review 0.329: a silent automatic import (armoury, dresser, armoire) only reports a failure, never « saved ».
                    var result = importing.GetAwaiter().GetResult();
                    if (!importingSilent || !result.Success) message = result.Message;
                    importing = null;
                }
                knownRetainers = Optional(ReadKnownRetainers, character, null, "retainer list") ?? (character == state.CharacterId ? knownRetainers : null);
                Volatile.Write(ref state, new(character, retainer, saddle, premium, message, knownRetainers, failedReads.Count > 0, company));
                contextUpdated = true;
                var open = new[] { retainer, saddle, premium, armoury, dresser, armoire, bags, crystals, currencies, company }.Where(context => context is { Loaded: true }).Select(context => context!.Key).ToHashSet();
                foreach (var gone in autoCaptured.Keys.Where(openKey => !open.Contains(openKey)).ToArray()) autoCaptured.Remove(gone);
                // 0.329: the setting covers what its label says (retainers, saddlebags); armoury, dresser and armoire have no import
                // button, so they are recorded whatever the setting.
                var auto = autoCapture();
                if (requested is null && importing is null && !bank.State.Loading && !bank.State.Importing && bank.State.Error is null)
                    foreach (var openKey in open)
                    {
                        var always = openKey.Kind is BankStorageKind.Armoury or BankStorageKind.GlamourDresser or BankStorageKind.Armoire || BankStorageScope.IsCharacterOwn(openKey.Kind);
                        if ((auto || always) && (!autoCaptured.TryGetValue(openKey, out var last)
                            || now - last >= (always ? TimeSpan.FromMinutes(2) : TimeSpan.FromSeconds(30))))
                        { autoCaptured[openKey] = now; requested = openKey; break; }
                    }
                if (requested is not { } key) return;
                requested = null;
                var context = new[] { retainer, saddle, premium, armoury, dresser, armoire, bags, crystals, currencies, company }.FirstOrDefault(c => c?.Key == key);
                if (context is null || !context.Loaded || character != key.CharacterId)
                    throw new InvalidOperationException("Inventory closed, owner changed or containers not loaded. Reopen the storage and try again.");

                var types = BankStorageScope.ContainerIds(key.Kind).Select(id => (GameInventoryType)id).ToArray();
                var owner = new InventoryOwnerId(key.Kind == BankStorageKind.Retainer ? key.StorageId : key.CharacterId);
                var snapshot = key.Kind switch
                {
                    BankStorageKind.GlamourDresser => DresserSnapshot(owner),
                    BankStorageKind.Armoire => ArmoireSnapshot(owner),
                    _ => InventoryNativeReader.Capture(inventory, owner, types, $"Dalamud/manual-bank/{key.Kind}"),
                };
                var after = key.Kind == BankStorageKind.Retainer ? GetRetainer(CharacterId)
                    : key.Kind == BankStorageKind.Armoury || BankStorageScope.IsCharacterOwn(key.Kind) ? (IsLoaded(key.Kind) ? context : null)
                    : key.Kind is BankStorageKind.GlamourDresser or BankStorageKind.Armoire ? context
                    : key.Kind == BankStorageKind.FreeCompanyChest ? GetFreeCompany(CharacterId) : GetSaddle(CharacterId, key.Kind);
                if (CharacterId != key.CharacterId || after?.Key != key || after?.Loaded != true
                    || snapshot.Containers.Values.Any(c => c.Coverage != InventoryCoverage.Complete))
                    throw new InvalidOperationException("Incomplete reading or identity changed. The old cache is kept; try again once the inventory is loaded.");
                importing = bank.ImportAsync(new BankEntry(key, context.Name, snapshot));
                importingSilent = key.Kind is BankStorageKind.Armoury or BankStorageKind.GlamourDresser or BankStorageKind.Armoire || BankStorageScope.IsCharacterOwn(key.Kind);
                if (!importingSilent) Volatile.Write(ref state, state with { Message = "Import en cours…" });
            }
            catch (Exception error)
            {
                if (state.Message != error.Message) log.Warning(error, "Tensho manual inventory import/context failed.");
                // 0.329 (R-14): the character comes from the Dalamud API, not from the failed read; publishing 0 signed it out of every stock.
                // Another character's retainer list is dropped here, or the next poll would take it for this one's.
                if (!contextUpdated && character != state.CharacterId) knownRetainers = null;
                Volatile.Write(ref state, contextUpdated ? state with { Message = error.Message }
                    : new(character, null, null, null, error.Message, knownRetainers, failedReads.Count > 0, null));
            }
        }
    }

    // 0.345: « Name (World) » of the logged-in character (Dalamud IPlayerState), stored as the name of its own storages; empty when not known.
    private bool homeWorldFailed;
    private string CharacterLabel()
    {
        var name = player.CharacterName;
        if (string.IsNullOrWhiteSpace(name)) return "";
        // R-14 (0.507): the World sheet only adds to the label: its failure leaves the name alone, and every storage stays readable.
        string world;
        try { world = player.HomeWorld.IsValid ? player.HomeWorld.Value.Name.ExtractText() : ""; }
        catch (Exception error)
        {
            world = "";
            if (!homeWorldFailed) { homeWorldFailed = true; log.Warning(error, "Home world name unavailable."); }
        }
        return world.Length > 0 ? $"{name} ({world})" : name;
    }

    // An optional native read: a failure (signature lost after a game update) reads as "not open", logged once per read.
    private T Optional<T>(Func<ulong, T> read, ulong character, T fallback, string name)
    {
        try { return read(character); }
        catch (Exception error)
        {
            if (failedReads.Add(name)) { log.Warning(error, "Tensho storage read unavailable ({Read}); other storages stay recorded.", name); }
            return fallback;
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

    // The character's retainers (name, id), known once the summoning bell was opened in this session; null when not available.
    private static unsafe IReadOnlyList<(ulong Id, string Name)>? ReadKnownRetainers(ulong character)
    {
        if (character == 0) return null;
        var manager = RetainerManager.Instance();
        if (manager == null || !manager->IsReady) return null;
        var list = new List<(ulong, string)>();
        foreach (ref var retainer in manager->Retainers)
            if (retainer.RetainerId != 0 && retainer.NameString.Length > 0) list.Add((retainer.RetainerId, retainer.NameString));
        return list.AsReadOnly();
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
        return new(key, retainer->NameString, addon.Name, addon.Position + new Vector2(addon.ScaledWidth + 6, 20), IsLoaded(key.Kind), addon.Position);
    }

    // 0.350 (g, I-09): the company chest window; loaded once every page (and gil, crystals) has been received from the game.
    private BankMenuContext? GetFreeCompany(ulong character)
    {
        if (character == 0) return null;
        var addon = FindVisible("FreeCompanyChest");
        if (addon.IsNull) return null;
        return new(new BankKey(character, BankStorageKind.FreeCompanyChest, 0), "Company chest", addon.Name,
            addon.Position + new Vector2(addon.ScaledWidth + 6, 20), IsLoaded(BankStorageKind.FreeCompanyChest), addon.Position);
    }

    private BankMenuContext? GetSaddle(ulong character, BankStorageKind kind)
    {
        if (character == 0) return null;
        var addon = FindVisible("InventoryBuddy", "InventoryBuddy2");
        if (addon.IsNull) return null;
        return new(new BankKey(character, kind, 0), kind == BankStorageKind.Saddlebag ? "Sacoche chocobo" : "Premium chocobo saddlebag",
            addon.Name, addon.Position + new Vector2(addon.ScaledWidth + 6, 20), IsLoaded(kind), addon.Position);
    }

    private static unsafe bool DresserLoaded() { var mirage = MirageManager.Instance(); return mirage != null && mirage->PrismBoxLoaded; }
    private static unsafe bool ArmoireLoaded() { var ui = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance(); return ui != null && ui->Cabinet.IsCabinetLoaded() && (nint)FFXIVClientStructs.FFXIV.Client.Game.UI.Cabinet.MemberFunctionPointers.IsItemInCabinet != 0; }

    // Glamour dresser items (MirageManager.PrismBoxItemIds: 0 empty, +1 000 000 HQ, +500 000 collectable), one stack per stored piece.
    private unsafe InventorySnapshot DresserSnapshot(InventoryOwnerId owner)
    {
        var mirage = MirageManager.Instance();
        if (mirage == null || !mirage->PrismBoxLoaded) throw new InvalidOperationException("Glamour dresser not loaded.");
        var stacks = new List<InventoryStack>();
        var ids = mirage->PrismBoxItemIds;
        for (var slot = 0; slot < ids.Length; slot++)
        {
            var raw = ids[slot];
            if (raw == 0) continue;
            var hq = raw >= 1_000_000; var id = raw % 500_000;
            if (id == 0) continue;
            stacks.Add(new(new(new(id), hq), 1, slot));
            // A whole outfit is stored as one item: its pieces count as owned too (numbered after the dresser's own slots).
            // 0.491 (R-14): since 7.4 an outfit may be incomplete (FCS MirageManager.IsSetSlotUnlocked; slot = MirageStoreSetItem column,
            // 11 columns, 11 slots). No slot held, or the function unavailable: what the game says is not usable, all the pieces count as before.
            var pieces = outfitPieces(id);
            if (pieces.Count == 0) continue;
            var held = OutfitSlots(mirage, (uint)slot, id, pieces.Count);
            for (var column = 0; column < pieces.Count; column++)
                if (pieces[column] != 0 && (held is null || held[column])) stacks.Add(new(new(new(pieces[column]), false), 1, ids.Length + stacks.Count));
        }
        return new(DateTimeOffset.UtcNow, "Dalamud/MirageManager.PrismBox", [new InventoryContainerSnapshot(new(owner, new(25000)), InventoryCoverage.Complete, stacks)]);
    }

    private unsafe bool[]? OutfitSlots(MirageManager* mirage, uint index, uint outfit, int count)
    {
        var held = new bool[count];
        try
        {
            for (var column = 0; column < count; column++) held[column] = mirage->IsSetSlotUnlocked(index, column);
        }
        catch (Exception error)
        {
            if (!outfitSlotsFailed) { outfitSlotsFailed = true; log.Warning(error, "Outfit slot check unavailable: every piece of a stored outfit counts."); }
            return null;
        }
        var bits = string.Concat(held.Select(value => value ? '1' : '0'));
        if (loggedOutfits.Add((outfit, bits))) log.Information("Dresser outfit {0}: slots held {1}.", outfit, bits);
        return held.Any(value => value) ? held : null;
    }

    // Armoire items: every Cabinet row whose item is stored (UIState.Cabinet.IsItemInCabinet).
    private unsafe InventorySnapshot ArmoireSnapshot(InventoryOwnerId owner)
    {
        var ui = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
        if (ui == null || !ui->Cabinet.IsCabinetLoaded()) throw new InvalidOperationException("Armoire not loaded.");
        var stacks = new List<InventoryStack>();
        // Slots are numbered in order (0.238: Cabinet row ids exceed the stored slot bound of 1000).
        foreach (var (row, item) in cabinetRows())
            if (item != 0 && ui->Cabinet.IsItemInCabinet(row)) stacks.Add(new(new(new(item), false), 1, stacks.Count));
        return new(DateTimeOffset.UtcNow, "Dalamud/UIState.Cabinet", [new InventoryContainerSnapshot(new(owner, new(25001)), InventoryCoverage.Complete, stacks)]);
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
            TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
            Volatile.Write(ref state, new(0, null, null, null, null));
        }
    }
}
