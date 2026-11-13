using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;
using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;
using TenshoAssistant.Collections;
using TenshoAssistant.Market;
using TenshoAssistant.Quests;
using TenshoAssistant.Character;
using TenshoAssistant.Maps;
using TenshoAssistant.Macros;
using TenshoAssistant.MiniGames;
using TenshoAssistant.Duties;
using TenshoAssistant.Monsters;
using TenshoAssistant.Fashion;
using TenshoAssistant.Localization;
using TenshoAssistant.UI;

namespace TenshoAssistant;

// Main Tensho window (/tensho). This file holds the shell: header (title, version, optional local and Eorzea clocks), the left
// navigation grouped by category (Objects, Production, World, Progression, Gold Saucer, Preferences; folds kept in the
// configuration, 0.264), the context item with its tool icons, and the page switch with a per-page draw-time ring shown in
// Diagnostics (0.290–0.292). Each page or tool is a partial file MainWindow.<Tool>.cs; services are created in Plugin.cs.
internal sealed partial class MainWindow : Window, IDisposable
{
    private enum Page { Home, Items, Gathering, Maps, MacroChain, SortPreview, Collections, BlueMagic, TripleTriad, Market, Quests, Chocobo, Events, MiniCactpot, Settings, Diagnostics, DutyLoot, MonsterLoot, GatheringGp, Fashion, CraftSimulation, CraftLive, Weather, CraftPresets, NpcVendors, Alarms, Storage, Reminders, FreeCompany, Gear, Credits, Leves, HuntingLog, OceanFishing, Garden, MyTools, GatheringCollectables, Beastmaster, Help, IslandSanctuary, CustomDeliveries, MaskedCarnivale, RelicWeapons, AlliedSocieties, GrandCompanies, SpecialDeliveries, FieldContent, DeepDungeons, Lexicon, Fates, Discoveries, Wishlist }
    // 0.1220 (X-261): numbers shown in the culture of Tensho's language, never Windows' (TenshoCulture).
    private static System.Globalization.CultureInfo Numbers => TenshoAssistant.Localization.TenshoCulture.Current;
    // Read from the active theme each time: the game theme can change while the plugin runs.
    private static Vector4 Accent => FfxivTheme.Gold;
    private static Vector4 Success => FfxivTheme.Success;
    private static Vector4 Muted => FfxivTheme.TextMuted;
    private static Vector4 Text => FfxivTheme.Text;
    private static Vector4 Error => FfxivTheme.Error;
    private static Vector4 Warning => FfxivTheme.Warning;
    // Background of a chosen toggle button (as the map's buttons): readable with the theme's text on light themes too.
    private static Vector4 Selected => FfxivTheme.Selected;
    private static readonly string BuildVersion = typeof(Plugin).Assembly.GetName().Version?.ToString(3) ?? "?";
    private readonly Configuration configuration;
    private readonly Action save;
    private readonly IPluginLog log;
    private readonly LocalizationManager localization;
    private readonly InventoryService inventory;
    private readonly InventoryVisualSortService visualSort;
    private readonly ItemCatalogService itemCatalog;
    private readonly RecipeCatalogService recipeCatalog;
    private readonly CraftActionCatalogService craftActions;
    private readonly CraftLiveService craftLive;
    private readonly WeatherCatalogService weather;
    private readonly GatheringCatalogService gatheringCatalog;
    private readonly CollectionService collections;
    private readonly MarketService market;
    private readonly ItemHoverService itemHover;
    private readonly QuestService quests;
    private readonly CharacterStatsService characterStats;
    private readonly MapCatalogService maps;
    private readonly MacroObservationService macroObservations;
    private readonly MacroChainService macroChain;
    private readonly TriadObservationService triadObservation;
    private readonly MiniCactpotObservationService cactpotObservation;
    private readonly GatheringObservationService gatheringObservation;
    private readonly ItemUseCatalogService itemUses;
    private readonly DutyCatalogService duties;
    private readonly DutyLootService dutyLoot;
    private readonly MonsterLootService monsterLoot;
    private readonly GpCatalogService gpActions;
    private readonly FashionHintService fashionHints;
    private readonly FashionDataService fashionData;
    private string questQuery = string.Empty;
    private string questCategory = string.Empty;
    private int questFilter = 1;
    private CollectionKind collectionKind;
    // Tab to bring forward when another tool opens a collection entry (0.142).
    private CollectionKind? focusCollectionKind;
    private string collectionQuery = string.Empty;
    private int collectionFilter;
    private uint selectedBlueSpell;
    private bool focusGatherTab;
    private uint? gatheringTypeFilter;
    private BankKey? sortPreviewOrigin;
    // X-119 (0.1131): replaced by the desktop application when it reads its copy of the bank (UseDesktopData); the plugin sets it once.
    private InventoryBankService bank;
    private readonly BankInventoryBridge bankBridge;
    private readonly ITextureProvider textures;
    private readonly IGameGui gameGui;
    private Page page;
    private string? saveError;
    private bool developerMode;
    private bool ShowIds=>developerMode || configuration.ShowIds;
    internal bool DeveloperMode => developerMode;
    private bool windowDisposed;
    private bool revealHomeTools;
    private int itemId;
    private string itemQuery = string.Empty;
    private string toolQuery = string.Empty;
    private string bankQuery = string.Empty;
    private BankKey? bankOriginFilter;
    private bool bankPersonalFilter;
    private string bankCategoryFilter = string.Empty;
    private int bankQualityFilter;
    private bool focusCraftTab;
    private bool focusStocksTab;
    private bool focusBankTab;
    // 0.292: last 120 draw times of each page (ring); Diagnostics shows the median (steady cost) and the maximum (spikes such as a
    // first icon load), a moving average mixed both and kept the spike of a page left just after opening it.
    private readonly Dictionary<Page, (double[] Samples, int Next, int Count)> pageDrawMs = [];
    private readonly Dictionary<string, (double[] Samples, int Next, int Count)> overlayDrawMs = [];
    internal readonly Diagnostics.SlowFrame SlowFrames = new();
    internal void RecordOverlayDraw(string name, double milliseconds)
    {
        SlowFrames.Add(ImGui.GetFrameCount(), name, milliseconds);
        // 0.487 (user report: times appeared then went): a panel whose window is closed costs next to nothing, and its zeros pushed its real times out within 2 seconds. Only frames it really drew count.
        if (milliseconds < 0.02) return;
        var ring = overlayDrawMs.TryGetValue(name, out var known) ? known : (Samples: new double[120], Next: 0, Count: 0);
        ring.Samples[ring.Next] = milliseconds;
        overlayDrawMs[name] = (ring.Samples, (ring.Next + 1) % ring.Samples.Length, Math.Min(ring.Count + 1, ring.Samples.Length));
    }
    private ItemCatalog? searchedCatalog;
    private IReadOnlyList<ItemDefinition> searchResults = [];
    private readonly Stack<uint> craftHistory = [];
    private string craftBookFilter = string.Empty;
    private string craftBookQuery = string.Empty;
    private uint craftBookRecipe;
    private int craftPlanTarget = 1;
    private readonly Dictionary<uint, ItemHoldingsSummary> recipeHoldingsCache = [];
    private InventorySnapshot? recipeHoldingsLive;
    private BankState? recipeHoldingsBank;
    private ulong recipeHoldingsCharacter;

    public MainWindow(Configuration configuration, Action save, IPluginLog log, LocalizationManager localization,
        InventoryService inventory, ItemCatalogService itemCatalog, RecipeCatalogService recipeCatalog, GatheringCatalogService gatheringCatalog, CollectionService collections, MarketService market, ItemHoverService itemHover, QuestService quests, CharacterStatsService characterStats, MapCatalogService maps, MacroObservationService macroObservations, MacroChainService macroChain, TriadObservationService triadObservation, InventoryBankService bank,
        BankInventoryBridge bankBridge, ITextureProvider textures, IGameGui gameGui, DutyCatalogService duties, DutyLootService dutyLoot, MonsterLootService monsterLoot,GpCatalogService gpActions,FashionHintService fashionHints,FashionDataService fashionData,CraftActionCatalogService craftActions,CraftLiveService craftLive,WeatherCatalogService weather,MiniCactpotObservationService cactpotObservation,GatheringObservationService gatheringObservation,ItemUseCatalogService itemUses,InventoryVisualSortService visualSort) : base("Tensho Assistant###TenshoMain")
    {
        this.configuration = configuration;
        // X-180 (0.1168): refused while the desktop application draws this window (its copy would be overwritten from the game).
        this.save = TenshoAssistant.UI.DesktopReadOnly.Guard(() => inDesktopApp, save);
        this.log = log;
        this.localization = localization;
        this.inventory = inventory;
        this.visualSort=visualSort;
        this.itemCatalog = itemCatalog;
        developerMode = configuration.DeveloperMode;
        this.recipeCatalog = recipeCatalog;
        this.craftActions = craftActions;
        this.craftLive = craftLive;
        this.weather = weather;
        this.gatheringCatalog = gatheringCatalog;
        this.collections = collections;
        this.market = market;
        this.itemHover = itemHover;
        this.quests = quests;
        this.characterStats = characterStats;
        this.maps = maps;
        this.macroObservations = macroObservations;
        this.macroChain = macroChain;
        this.triadObservation = triadObservation;
        this.cactpotObservation = cactpotObservation;
        this.gatheringObservation = gatheringObservation;
        this.itemUses = itemUses;
        this.duties = duties;
        this.dutyLoot = dutyLoot;
        this.monsterLoot = monsterLoot;
        this.gpActions=gpActions;
        this.fashionHints=fashionHints; this.fashionData=fashionData;
        this.bank = bank;
        this.bankBridge = bankBridge;
        this.textures = textures;
        this.gameGui = gameGui;
        // 0.327 (queue step e): Home as before, or the last page used when that setting is on (developerMode is known here).
        page = InitialPage();
        Size = new Vector2(1040, 700);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 480),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        // X-01 (0.1050): the window's code compiled ahead off the frame (Diagnostics/JitWarmup.cs): the first frame of a page was mostly JIT.
        jitWarmup = Diagnostics.JitWarmup.Start(() => Diagnostics.JitWarmup.FirstThenAssembly(typeof(MainWindow)), Diagnostics.JitWarmup.Delay, done => log.Information(FormattableString.Invariant(
            $"JIT warm-up: {done.Prepared} methods of the window and the plugin compiled ahead in {done.Elapsed.TotalMilliseconds:F0} ms, {done.Skipped} skipped, {done.EarlyPrepared} of the map and the overlays first. Window part (X-01, 0.1282): {done.WindowPartMethods} methods ready {done.WindowReadyAfter:F0} ms after the load; panels' part ready {done.FirstReadyAfter:F0} ms.")),
            // X-177 C4.2 (0.1200): the rest waits for the readers' classes (one thread compiles at a time: interface, readers, rest).
            holdRest: static ()=>TenshoAssistant.Competitive.CompetitiveGate.CompilerBusy,
            // X-01 (0.1279): the free panels' and the bars' code first, with what it calls: they wait for it (PanelGate).
            firstPart: Diagnostics.JitWarmup.OverlayFirst,
            // X-01 (0.1282): then the window's frame and its first page, with what they call.
            windowPart: Diagnostics.JitWarmup.WindowFirst,
            early: Diagnostics.JitWarmup.EarlyMethod);
        panelGate = new Diagnostics.PanelGate(() => jitWarmup.FirstPartDone);
    }
    /// <summary>X-01 (0.1279): the free panels, the bars and the overlay pass are drawn only once their code is compiled (3 s at most after
    /// the load; Diagnostics/PanelGate.cs).</summary>
    internal bool PanelsOpen => panelGate.Open;
    private readonly Diagnostics.PanelGate panelGate;
    /// <summary>X-01 (0.1279): the line of the frame where the panels came back, written once by the plugin.</summary>
    internal string PanelsOpenedText(double frameMs) => FormattableString.Invariant(
        $"Panels opened {panelGate.OpenedAfter} ms after the load ({(panelGate.ByBound ? "at the bound" : "their code compiled")}: {jitWarmup.Prepared} of {jitWarmup.FirstPartMethods} methods with what they call compiled by then, all ready {jitWarmup.FirstReadyAfter:F0} ms after the load, -1 = not yet); their first frame {frameMs:F0} ms.");
    /// <summary>X-177 C4.2 (0.1200): the interface's code (map, overlays) is compiled: the readers' classes may be compiled next.</summary>
    internal bool WarmupEarlyDone=>jitWarmup.EarlyDone;

    private readonly Diagnostics.JitWarmup jitWarmup;

    // 0.394 (user request): the chosen text font for the whole window, pushed after the theme and popped first.
    private IDisposable? bodyFontScope;
    // 0.498 (user report): a window collapsed by its title bar stayed collapsed when something asked for it. Anything that opens the window or
    // changes its page unfolds it; held two frames, then given back to the player, who can collapse it again.
    private int unfoldFrames;
    private Page? lastShownPage;
    public new bool IsOpen
    {
        get=>base.IsOpen;
        set{if(value)unfoldFrames=2;base.IsOpen=value;}
    }
    private void UnfoldWhenAsked()
    {
        if(lastShownPage is { } shown && shown!=page)unfoldFrames=2;
        lastShownPage=page;
        if(unfoldFrames>0){unfoldFrames--;Collapsed=false;CollapsedCondition=ImGuiCond.Always;}
        else Collapsed=null;
    }
    public override void PreDraw(){UnfoldWhenAsked();if(inDesktopApp)ApplyTheme();FfxivTheme.Push();bodyFontScope=TenshoAssistant.UI.UiComponents.BodyFont is { Available:true } font ? font.Push():null;}
    public override void PostDraw(){bodyFontScope?.Dispose();bodyFontScope=null;FfxivTheme.Pop();}

    internal void OpenSettings(){page=Page.Settings;IsOpen=true;}
    internal void OpenAlarms(){page=Page.Alarms;IsOpen=true;}
    internal void OpenReminders(){page=Page.Reminders;IsOpen=true;}
    // 0.344 (g, I-02): from the currency entry of the server bar.
    internal void OpenCurrencies(){page=Page.Storage;focusCurrencyTab=true;IsOpen=true;}
    // X-190 (0.1178): Storage › What to do with it, for one currency (the island's coins).
    internal void OpenCurrencyValue(uint currency){page=Page.Storage;ChooseCurrencyValue(currency);focusCurrencyValueTab=true;IsOpen=true;}
    /// <summary>X-107 (0.986): the server bar's gil entry opens the storage page (gil journal tab).</summary>
    internal void OpenGilJournal(){page=Page.Storage;IsOpen=true;}
    /// <summary>X-217 (0.1226): from the server bar's gear condition and place entries.</summary>
    internal void OpenGear(){page=Page.Gear;IsOpen=true;}
    internal void OpenMap(){page=Page.Maps;IsOpen=true;}

    // 0.327 (queue step e): opening page. Off (default): Home when the plugin loads, as before (the reopened window keeps its page
    // during a session). On: the page shown is kept in memory as it changes and written to disk only when the window closes or the
    // plugin unloads, never on each page change.
    private Page? rememberedPage;
    private bool lastPageDirty;

    private Page InitialPage()
    {
        if(!configuration.OpenOnLastPage || !Enum.TryParse<Page>(configuration.LastPage,out var saved) || !Enum.IsDefined(saved)
            || (saved==Page.Diagnostics && !developerMode))return Page.Home;
        rememberedPage=saved;
        return saved;
    }

    private void RememberPage()
    {
        if(!configuration.OpenOnLastPage || page==rememberedPage)return;
        rememberedPage=page;
        configuration.LastPage=page.ToString();
        lastPageDirty=true;
    }

    private void SaveLastPage()
    {
        RememberPage();
        if(!lastPageDirty || inDesktopApp)return;
        try{save();lastPageDirty=false;}
        catch(Exception error){log.Warning(error,"The last page used could not be saved.");}
    }

    public override void OnClose()=>SaveLastPage();

    public override void Draw()
    {
        var started = DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        try { DrawMain(); }
        finally { if (started != 0) SlowFrames.Add(ImGui.GetFrameCount(), "main window", System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds); }
    }

    private void DrawMain()
    {
        RememberPage();
        OpenGuideOnce();
        ValidateDrawContext();
        // X-154 (0.1252): the navigation column follows Dalamud's scale and folds behind a button in a narrow window (or by the setting);
        // folded, the header names the page instead of the plugin (the window's title bar already names it).
        var scale = Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale;
        var folded = NavigationLayout.Folded(configuration.NavigationMode, ImGui.GetWindowWidth(), scale);
        var height = Math.Max(260, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing()*3);
        if (folded)
        {
            if (ToolButton(FontAwesomeIcon.Bars, "Navigation.Open", true)) ImGui.OpenPopup("NavigationPopup");
            DrawNavigationPopup(scale, height);
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            UiComponents.SubTitle(PageEntryFor(NavigationPage(page))?.Label?.Invoke() ?? localization.Get("Plugin.Title"));
        }
        else ImGui.TextColored(Accent, localization.Get("Plugin.Title").ToUpperInvariant());
        ImGui.SameLine();
        ImGui.TextDisabled($"v{BuildVersion}");
        if(configuration.ShowHeaderClock){ImGui.SameLine();ImGui.TextDisabled(localization.Format("LocalClock.Header",DateTimeOffset.Now.ToString("HH:mm:ss")));}
        if(configuration.ShowHeaderEorzeaTime)
        {
            var eorzea=Gathering.GatheringTime.MinuteOfDay(DateTimeOffset.UtcNow);
            ImGui.SameLine();ImGui.TextDisabled(localization.Format("LocalClock.HeaderEt",eorzea/60,eorzea%60));
        }
        DrawPageHelpButton();
        ImGui.TextDisabled(localization.Get("Plugin.Subtitle"));
        ImGui.Separator();

        height = Math.Max(260, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing());
        if (ImGui.BeginTable(folded ? "WorkspaceFolded" : "Workspace", folded ? 1 : 2, ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                if (!folded) ImGui.TableSetupColumn("Navigation", ImGuiTableColumnFlags.WidthFixed, NavigationLayout.Width(scale));
                ImGui.TableSetupColumn("Contenu", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                if (!folded)
                {
                    ImGui.TableSetColumnIndex(0);
                    var showNavigation = ImGui.BeginChild("NavigationPane", new Vector2(0, height), true);
                    try { if (showNavigation) DrawNavigation(); }
                    finally { ImGui.EndChild(); }
                }

                ImGui.TableSetColumnIndex(folded ? 0 : 1);
                var showContent = ImGui.BeginChild("ContentPane", new Vector2(0, height), true);
                try
                {
                    if (showContent)
                    {
                        DrawCategoryNavigation();
                        DrawHelpBanner();
                        // 0.290 (R-06 step 6): draw time of each page, averaged, shown in Diagnostics (developer mode).
                        var drawStarted = System.Diagnostics.Stopwatch.GetTimestamp();
                        var drawnPage = page;
                        PageEntryFor(page)?.Draw();
                        var drawMs = System.Diagnostics.Stopwatch.GetElapsedTime(drawStarted).TotalMilliseconds;
                        if (DeveloperMode) SlowFrames.Add(ImGui.GetFrameCount(), "page " + drawnPage, drawMs);
                        var ring = pageDrawMs.TryGetValue(drawnPage, out var known) ? known : (Samples: new double[120], Next: 0, Count: 0);
                        ring.Samples[ring.Next] = drawMs;
                        pageDrawMs[drawnPage] = (ring.Samples, (ring.Next + 1) % ring.Samples.Length, Math.Min(ring.Count + 1, ring.Samples.Length));
                    }
                }
                finally { ImGui.EndChild(); }
            }
            finally { ImGui.EndTable(); }
        }
        ImGui.Separator();
        // X-76 H3 (0.914): a failed save is said here, whatever the page; else the usual footer.
        if(DrawSaveError())return;
        // X-127 (0.1106): « added to your wishlist », with a button that opens it.
        if(DrawWishAdded())return;
        ImGui.TextDisabled("/tensho");
        ImGui.SameLine();
        // 0.1221 (X-259, first-player review G8): the footer says how retainers and chocobo are really saved (AutoBankCapture).
        ImGui.TextDisabled(localization.Get(configuration.AutoBankCapture ? "UiFragments.Footer":"UiFragments.FooterManual"));
    }

    private void ValidateDrawContext()
    {
        monsterLoot.SelectItem((uint)Math.Max(0,itemId));
        ValidateGpCopy();
        ValidateCraftCopy();
        ValidateCraftSearch();
        craftChain.ValidateContext(bankBridge.State.CharacterId, (uint)Math.Max(0, itemId), craftPlanTarget, recipeCatalog.State.Catalog);
        triadSolve.ValidateContext(bankBridge.State.CharacterId, triadRevision);
        preparationPrices.ValidateContext(bankBridge.State.CharacterId,market.State.WorldId,(uint)Math.Max(0,itemId),craftPlanTarget,costBasisIndex,costQualityIndex,craftChainChoiceRevision);
    }

    // X-154 (0.1252): the folded navigation, in a popup under its button: the same list as the column (DrawNavigation, drawn once per
    // frame, here or there); choosing a page closes it.
    private void DrawNavigationPopup(float scale, float height)
    {
        if (!ImGui.BeginPopup("NavigationPopup")) return;
        try
        {
            var before = page;
            var show = ImGui.BeginChild("NavigationPopupPane", new Vector2(NavigationLayout.Width(scale), height), false);
            try { if (show) DrawNavigation(); }
            finally { ImGui.EndChild(); }
            if (page != before) ImGui.CloseCurrentPopup();
        }
        finally { ImGui.EndPopup(); }
    }

    private void DrawNavigation()
    {
        // R-06 step 2 (0.264): pages grouped by category, each category folds (kept in the configuration); the hub pages of the map
        // and the Gold Saucer are now direct entries, the sub-pages of crafting, gathering and collections stay in their own tab bar.
        // 0.470 (TEMPLATES-001): the pages and their order come from one list, MainWindow.Pages.cs.
        // X-220 (0.1230): one search over pages, items, quests and maps, above the list.
        DrawQuickSearch();
        foreach(var group in PageGroups)
            if(group.Category is null || NavigationCategory(group.Category,group.Pages))
                foreach(var entry in group.Entries)
                    if(entry.Visible?.Invoke()!=false && ListedHere(entry.Page))Navigation(entry.Label!(),entry.Page);
        ImGui.Spacing();
        ImGui.Separator();
        // 0.1038 (T-0.912-1): the theme's text colour, readable in the light theme too.
        using (TenshoAssistant.UI.UiComponents.PushSectionFont()) ImGui.TextColored(Text,localization.Get("Navigation.ContextItem"));
        if (SelectedItem is { } selected)
        {
            DrawItemIcon(selected, new Vector2(32));
            ImGui.TextWrapped(selected.Name);
            if(ShowIds)ImGui.TextDisabled($"ID {selected.Id}");
            if (ImGui.SmallButton(localization.Get("Navigation.ViewQuantities"))) { page = Page.Items; focusStocksTab = true; }
            DrawContextTools();
        }
        else if (itemId > 0) ImGui.TextWrapped(localization.Format("BankUi.Item",itemId));
        else ImGui.TextWrapped(localization.Get("Navigation.SelectItem"));
    }

    // X-119 step 4 (0.1158): a page chosen for the other side only is not listed here (it still opens from a link or a command).
    private bool ListedHere(Page page)=>inDesktopApp ? Desktop.DesktopPages.InAppNavigation(configuration.PagePlaces,page.ToString())
        : Desktop.DesktopPages.InGameNavigation(configuration.PagePlaces,page.ToString());

    private Page NavigationPage(Page current)=>PageEntryFor(current)?.Parent ?? current;

    private void Navigation(string label, Page destination)
    {
        if (UiComponents.NavigationItem(label, NavigationPage(page) == destination))
            page = destination;
    }

    private ItemDefinition? SelectedItem => itemId > 0 ? itemCatalog.State.Catalog?.Find((uint)itemId) : null;

    private static void Heading(string title, string description)
    {
        UiComponents.SectionHeader(title, description);
    }

    // One state mark for every page: check = the game confirms it (owned, done, unlocked), cross = it says no, question mark = not read yet.
    // Drawn from the icon font: « ✗ » is missing from the game font (0.341, it showed as « = »).
    private static void DrawStateMark(bool? state)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        try
        {
            var icon=state switch{true=>FontAwesomeIcon.Check,false=>FontAwesomeIcon.Times,_=>FontAwesomeIcon.Question};
            ImGui.TextColored(state switch{true=>Success,false=>Muted,_=>Accent},icon.ToIconString());
        }
        finally{ImGui.PopFont();}
    }

    private void DrawCatalogStatus()
    {
        var state = itemCatalog.State;
        if (state.Catalog is { } catalog)
            ImGui.TextColored(Success, localization.Format("UiFragments.CatalogReady",catalog.Count.ToString("N0"),catalog.Metadata.Language));
        else ImGui.TextWrapped(state.Error ?? (state.TotalRows > 0
            ? localization.Format("UiFragments.CatalogProgress",state.RowsCopied.ToString("N0",Numbers),state.TotalRows.ToString("N0",Numbers))
            : localization.Get("UiFragments.CatalogLoading")));
    }
}
