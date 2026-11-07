using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Diagnostics;
using TenshoAssistant.Quests;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// Self-check (0.494, user request): what each part of the plugin has read, as numbers. Probes run on the framework thread, one per frame
// (SelfCheckRunner); they read the published states, plus the game's counters that the Reminders page also reads there.
internal sealed partial class MainWindow
{
    internal SelfCheckRunner? SelfCheck { get; set; }

    /// <summary>The catalogues have finished, well or badly: a pass now says something about them.</summary>
    internal bool SelfCheckReady()
    {
        var items = itemCatalog.State; var recipes = recipeCatalog.State; var gathered = gatheringCatalog.State;
        var places = maps.State; var duty = duties.State; var vendors = npcVendors?.State; var triad = triadNpcs?.State;
        StartSelfCheckPages();
        return (items.Catalog is not null || items.Error is not null) && (recipes.Catalog is not null || recipes.Error is not null)
            && (gathered.Catalog is not null || gathered.Error is not null) && (places.Catalog is not null || places.Error is not null)
            && (duty.Catalog is not null || duty.Error is not null) && (vendors is null || vendors.Catalog is not null || vendors.Error is not null)
            && (triad is null || !triad.Loading) && !quests.State.Loading && !collections.State.Loading
            && selfCheckPages!.All(page => page.Read.IsCompleted);
    }

    /// <summary>Readers that read only while a page asks are asked first.</summary>
    internal void PrepareSelfCheck()
    {
        StartSelfCheckPages();
        GearsetSource?.Want();
        Currencies?.Invoke()?.Want();
        // These ask their reader when called.
        _ = Chocobo?.Invoke(); _ = ExtraCollections?.Invoke(); _ = HuntSlots?.Invoke(); _ = LeveProgress?.Invoke(); _ = DutyProgressSource?.Invoke();
    }

    internal IReadOnlyList<(string Domain, Func<SelfCheckLine> Probe)> SelfCheckProbes(bool loggedIn)
    {
        SelfCheckLine Personal(string domain, Func<SelfCheckLine> read) => loggedIn ? read() : Diagnostics.SelfCheck.NotRead(domain, "not logged in");
        (string, Func<SelfCheckLine>) Own(string domain, Func<SelfCheckLine> read) => (domain, () => Personal(domain, read));
        return
        [
            .. SelfCheckPageProbes(),
            ("items", () => { var state = itemCatalog.State; return Diagnostics.SelfCheck.Catalog("items", state.Catalog?.Count, state.RowsCopied, state.TotalRows, state.Error); }),
            ("recipes", () => { var state = recipeCatalog.State; return Diagnostics.SelfCheck.Catalog("recipes", state.Catalog?.Count, state.RowsCopied, state.TotalRows, state.Error); }),
            ("gathering", () => { var state = gatheringCatalog.State; return Diagnostics.SelfCheck.Catalog("gathering", state.Catalog?.Count, state.Copied, state.Total, state.Error); }),
            ("maps", () => { var state = maps.State; return Diagnostics.SelfCheck.Catalog("maps", state.Catalog?.Entries.Count, state.Copied, state.Total, state.Error); }),
            ("duties", () => { var state = duties.State; return Diagnostics.SelfCheck.Catalog("duties", state.Catalog?.Entries.Count, state.Copied, state.Total, state.Error); }),
            ("vendors", SelfCheckVendors),
            ("gear data", () => Diagnostics.SelfCheck.Built("gear data", GearCatalog?.Invoke() is not null, GearCatalogFailed?.Invoke() == true)),
            ("submarine data", () => Diagnostics.SelfCheck.Built("submarine data", SubmarineCatalog?.Invoke() is not null, SubmarineCatalogFailed?.Invoke() == true)),
            ("airship data", () => Diagnostics.SelfCheck.Built("airship data", AirshipCatalog?.Invoke() is not null, AirshipCatalogFailed?.Invoke() == true)),
            Own("character", SelfCheckCharacter),
            Own("inventory", SelfCheckInventory),
            Own("bank", SelfCheckBank),
            Own("quests", SelfCheckQuests),
            Own("collections", SelfCheckCollections),
            Own("extra collections", () => ExtraCollectionsUnavailable?.Invoke() == true ? Diagnostics.SelfCheck.Failed("extra collections", "read failed")
                : ExtraCollections?.Invoke() is { } extra ? Counted("extra collections", extra.Count, $"{extra.Count(entry => entry.ConfirmedUnlocked)}/{extra.Count} obtained")
                : Diagnostics.SelfCheck.NotRead("extra collections", "not read yet")),
            Own("triad", SelfCheckTriad),
            ("beastmaster", SelfCheckBeastmaster),
            Own("map", SelfCheckMap),
            Own("gear sets", () => GearsetSource is not { } source ? Diagnostics.SelfCheck.NotRead("gear sets", "no reader")
                : source.State is { } sets ? Counted("gear sets", sets.Sets.Count, $"{sets.Sets.Count} sets, {sets.JobLevels.Count} job levels")
                : source.Unavailable ? Diagnostics.SelfCheck.Failed("gear sets", "read failed") : Diagnostics.SelfCheck.NotRead("gear sets", "not read yet")),
            Own("worn gear", () => EquippedGearUnavailable?.Invoke() == true ? Diagnostics.SelfCheck.Failed("worn gear", "read failed")
                : EquippedGear?.Invoke() is { } worn ? Counted("worn gear", worn.Count, $"{worn.Count} pieces") : Diagnostics.SelfCheck.NotRead("worn gear", "no reader")),
            Own("hunting log", () => HuntUnavailable?.Invoke() == true ? Diagnostics.SelfCheck.Failed("hunting log", "read failed")
                : HuntSlots?.Invoke() is { } slots ? Counted("hunting log", slots.Count, $"{slots.Count} logs") : Diagnostics.SelfCheck.NotRead("hunting log", "not read yet")),
            Own("leves", () => LeveProgressUnavailable?.Invoke() == true ? Diagnostics.SelfCheck.Failed("leves", "read failed")
                : LeveProgress?.Invoke() is { Completed: { } done } leves ? Diagnostics.SelfCheck.Ok("leves", $"{done.Count} done, {leves.Accepted.Count} accepted, allowances {leves.Allowances?.ToString() ?? "unknown"}")
                : Diagnostics.SelfCheck.NotRead("leves", "not read yet")),
            Own("duty progress", () => DutyProgressUnavailable?.Invoke() == true ? Diagnostics.SelfCheck.Failed("duty progress", "read failed")
                : DutyProgressSource?.Invoke() is { } progress ? Counted("duty progress", progress.ByInstance.Count, $"{progress.ByInstance.Count} duties") : Diagnostics.SelfCheck.NotRead("duty progress", "not read yet")),
            Own("chocobo", () => ChocoboUnavailable?.Invoke() == true ? Diagnostics.SelfCheck.Failed("chocobo", "read failed")
                : Chocobo?.Invoke() is { } chocobo ? Diagnostics.SelfCheck.Ok("chocobo", $"rank {chocobo.Rank}, {chocobo.Unlocked?.Count.ToString() ?? "unknown"} bardings") : Diagnostics.SelfCheck.NotRead("chocobo", "not read yet")),
            Own("currencies", () => Currencies?.Invoke()?.State is not { } money ? Diagnostics.SelfCheck.NotRead("currencies", "no reader")
                : money.Unavailable ? Diagnostics.SelfCheck.Failed("currencies", "read failed") : money.Reading is null ? Diagnostics.SelfCheck.NotRead("currencies", "not read yet") : Diagnostics.SelfCheck.Ok("currencies", "read")),
            Own("reminders", SelfCheckReminders),
            .. SelfCheckWindows(),
        ];
    }

    // 0.495 (user report): named after what is watched. The Gathering log has no line: Tensho only opens it, it reads nothing from it.
    // 0.496: published states only, cheap enough to be watched once a second (SelfCheckRunner).
    internal IReadOnlyList<(string Domain, Func<SelfCheckLine> Probe)> SelfCheckWindows() => selfCheckWindows ??=
    [
        ("crafting log", SelfCheckCraftingLog),
        ("synthesis", () => Observed("synthesis", craftLive.State.Enabled, craftLive.State.Status.ToString(), "no synthesis running")),
        ("gathering node", () => Observed("gathering node", gatheringObservation.State.Enabled, gatheringObservation.State.Status.ToString(), "no node open")),
        ("triad match", () => Observed("triad match", triadObservation.State.Enabled, triadObservation.State.Status.ToString(), "no match running")),
        ("cactpot ticket", () => Observed("cactpot ticket", cactpotObservation.State.Enabled, cactpotObservation.State.Status.ToString(), "no ticket open")),
    ];
    private IReadOnlyList<(string Domain, Func<SelfCheckLine> Probe)>? selfCheckWindows;

    private static SelfCheckLine Counted(string domain, int count, string detail)
        => count > 0 ? Diagnostics.SelfCheck.Ok(domain, detail) : Diagnostics.SelfCheck.Suspect(domain, detail);

    private static SelfCheckLine Observed(string domain, bool enabled, string status, string closed)
        => Diagnostics.SelfCheck.Window(domain, enabled, status, closed);

    private SelfCheckLine SelfCheckCraftingLog()
    {
        if (!configuration.CraftLogHelperEnabled) return Diagnostics.SelfCheck.NotRead("crafting log", "help off");
        if (craftLog.Error is { } error) return Diagnostics.SelfCheck.Failed("crafting log", error);
        return craftLog.Observation is { } seen && seen.Fresh(DateTimeOffset.UtcNow) ? Diagnostics.SelfCheck.Ok("crafting log", $"recipe {seen.RecipeId}") : Diagnostics.SelfCheck.NotRead("crafting log", "not open");
    }

    private SelfCheckLine SelfCheckVendors()
    {
        if (npcVendors?.State is not { } state) return Diagnostics.SelfCheck.NotRead("vendors", "no reader");
        if (state.Error is not null) return Diagnostics.SelfCheck.Failed("vendors", state.Error);
        if (state.Catalog is not { } catalog) return Diagnostics.SelfCheck.NotRead("vendors", $"loading {state.Copied}/{state.Total}");
        return catalog.VendorCount > 0 && catalog.SaleCount > 0 ? Diagnostics.SelfCheck.Ok("vendors", $"{catalog.VendorCount} vendors, {catalog.SaleCount} sales")
            : Diagnostics.SelfCheck.Suspect("vendors", $"{catalog.VendorCount} vendors, {catalog.SaleCount} sales");
    }

    private SelfCheckLine SelfCheckCharacter()
    {
        var stats = characterStats.State;
        if (stats.CharacterId == 0) return Diagnostics.SelfCheck.NotRead("character", "not read yet");
        var detail = $"job {stats.JobId}, level {stats.Level}, {stats.Values.Count} values";
        return stats.Level > 0 && stats.Values.Count > 0 ? Diagnostics.SelfCheck.Ok("character", detail) : Diagnostics.SelfCheck.Suspect("character", detail);
    }

    private SelfCheckLine SelfCheckInventory()
    {
        if (inventory.Current is not { } snapshot) return Diagnostics.SelfCheck.NotRead("inventory", "not read yet");
        var containers = snapshot.Containers.Values.ToArray();
        var complete = containers.Count(container => container.Coverage == Inventory.InventoryCoverage.Complete);
        var stacks = containers.Sum(container => container.Stacks.Count);
        return Counted("inventory", stacks, $"{containers.Length} containers, {complete} complete, {stacks} stacks");
    }

    private SelfCheckLine SelfCheckBank()
    {
        var state = bank.State;
        if (state.Error is not null) return Diagnostics.SelfCheck.Failed("bank", state.Error);
        return state.Loading ? Diagnostics.SelfCheck.NotRead("bank", "loading") : Diagnostics.SelfCheck.Ok("bank", $"{state.Entries.Count} saved containers");
    }

    private SelfCheckLine SelfCheckQuests()
    {
        var state = quests.State;
        if (state.Error is not null) return Diagnostics.SelfCheck.Failed("quests", state.Error);
        if (state.Loading) return Diagnostics.SelfCheck.NotRead("quests", "loading");
        var done = state.Entries.Count(entry => entry.Progress == QuestProgress.Complete);
        var active = state.Entries.Count(entry => entry.Progress == QuestProgress.Active);
        return Counted("quests", done, $"{done} done, {active} active, of {state.Entries.Count}");
    }

    private SelfCheckLine SelfCheckCollections()
    {
        var state = collections.State;
        if (state.Error is not null) return Diagnostics.SelfCheck.Failed("collections", state.Error);
        if (state.Loading) return Diagnostics.SelfCheck.NotRead("collections", "loading");
        var kinds = state.Entries.GroupBy(entry => entry.Kind).OrderBy(group => group.Key)
            // Titles and achievements are known only once the game has loaded them: no answer either way is « not read », not « none obtained ».
            .Select(group => group.Key is CollectionKind.Title or CollectionKind.Achievement && !group.Any(entry => entry.ConfirmedUnlocked || entry.NegativeConfirmed)
                ? $"{group.Key} not read ({group.Count()})" : $"{group.Key} {group.Count(entry => entry.ConfirmedUnlocked)}/{group.Count()}");
        return Counted("collections", state.Entries.Count(entry => entry.ConfirmedUnlocked), string.Join(", ", kinds));
    }

    private SelfCheckLine SelfCheckTriad()
    {
        if (triadNpcs?.State is not { } state) return Diagnostics.SelfCheck.NotRead("triad", "no reader");
        if (state.Error is not null) return Diagnostics.SelfCheck.Failed("triad", state.Error);
        if (state.Catalog is not { } catalog) return Diagnostics.SelfCheck.NotRead("triad", "loading");
        if (state.BeatenUnavailable) return Diagnostics.SelfCheck.Failed("triad", "beaten list or quests: read failed");
        if (state.Beaten is not { } beaten || state.QuestsDone is not { } done) return Diagnostics.SelfCheck.NotRead("triad", "not read yet");
        var locked = catalog.Npcs.Count(npc => npc.Available(done) == false);
        // An NPC already beaten cannot have been out of reach: if one reads « quest needed », the reading of its quests is wrong.
        var contradictions = catalog.Npcs.Count(npc => beaten.Contains(npc.MatchId) && npc.Available(done) == false);
        var detail = $"{catalog.Npcs.Count} npcs, {catalog.Npcs.Count - locked} available, {locked} quest needed, {beaten.Count} beaten, {contradictions} beaten but quest needed";
        return catalog.Npcs.Count > 0 && locked < catalog.Npcs.Count && contradictions == 0 ? Diagnostics.SelfCheck.Ok("triad", detail) : Diagnostics.SelfCheck.Suspect("triad", detail);
    }

    private SelfCheckLine SelfCheckBeastmaster()
    {
        if (Beasts?.State is not { } state) return Diagnostics.SelfCheck.NotRead("beastmaster", "no reader");
        if (state.Error is not null) return Diagnostics.SelfCheck.Failed("beastmaster", state.Error);
        if (state.Catalog is not { } catalog) return Diagnostics.SelfCheck.NotRead("beastmaster", "loading");
        var ranks = catalog.Beasts.Count(beast => beast.Ranks.Count > 0);
        var obtained = state.ObtainedUnavailable ? "read failed" : state.Obtained?.Count.ToString() ?? "not read yet";
        var detail = $"{catalog.Beasts.Count} beasts, {ranks} with attributes, {obtained} obtained, {catalog.Boards.Count} boards, {catalog.Boards.Sum(board => board.Battles.Sum(battle => battle.Enemies.Count))} enemies";
        return catalog.Beasts.Count > 0 && ranks == catalog.Beasts.Count && catalog.Boards.Count > 0 && !state.ObtainedUnavailable ? Diagnostics.SelfCheck.Ok("beastmaster", detail) : Diagnostics.SelfCheck.Suspect("beastmaster", detail);
    }

    private SelfCheckLine SelfCheckMap()
    {
        if (mapLive?.State is not { } state) return Diagnostics.SelfCheck.NotRead("map", "not read yet");
        var detail = $"map {state.MapId}, currents {state.UnlockedCurrents?.Count.ToString() ?? "unknown"}, vistas {state.Sightseen?.Count.ToString() ?? "unknown"}";
        return state.MapId == 0 ? Diagnostics.SelfCheck.Suspect("map", detail) : Diagnostics.SelfCheck.Ok("map", detail);
    }

    private SelfCheckLine SelfCheckReminders()
    {
        var timers = GameTimers.Read(DailyRoulettes?.Invoke(), ChallengeRows?.Invoke());
        var read = new[] { timers.AlliedQuestsLeft is not null, timers.LeveAllowances is not null, timers.DeliveriesLeft is not null, timers.Roulettes is not null,
            timers.HuntBillsHeld is not null, timers.ChallengeLog is not null, timers.DomanEnclave is not null };
        return Counted("reminders", read.Count(value => value), $"{read.Count(value => value)}/{read.Length} counters read");
    }

    private void DrawSelfCheck()
    {
        if (!TenshoAssistant.UI.UiComponents.Header(localization.Get("DiagnosticsUi.SelfCheck"))) return;
        ImGui.PushTextWrapPos(0);ImGui.TextDisabled(localization.Get("DiagnosticsUi.SelfCheckHelp"));ImGui.PopTextWrapPos();
        if (SelfCheck is not { } runner) return;
        ImGui.BeginDisabled(runner.Running);
        try { if (ImGui.SmallButton(localization.Get("DiagnosticsUi.SelfCheckRun"))) runner.Start("button"); }
        finally { ImGui.EndDisabled(); }
        Tip("DiagnosticsUi.SelfCheckRunTip");
        foreach (var line in runner.LastLines)
        {
            var color = line.Status switch { SelfCheckStatus.Ok or SelfCheckStatus.Seen => Success, SelfCheckStatus.NotRead => Muted, _ => Accent };
            ImGui.TextColored(color, SelfCheckLine.Label(line.Status));
            ImGui.SameLine();
            ImGui.TextWrapped($"{line.Domain}: {line.Detail}");
        }
    }
}
