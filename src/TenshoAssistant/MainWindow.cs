using System.Numerics;
using Dalamud.Bindings.ImGui;
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
    private enum Page { Home, Items, Gathering, Maps, MacroChain, SortPreview, Collections, BlueMagic, TripleTriad, Market, Quests, Chocobo, Events, MiniCactpot, Settings, Diagnostics, DutyLoot, MonsterLoot, GatheringGp, Fashion, CraftSimulation, CraftLive, Weather, CraftPresets, NpcVendors, Alarms, Storage, Reminders, FreeCompany, Gear, Credits, Leves, HuntingLog, OceanFishing, Garden }
    // Read from the active theme each time: the game theme can change while the plugin runs.
    private static Vector4 Accent => FfxivTheme.Gold;
    private static Vector4 Success => FfxivTheme.Success;
    private static Vector4 Muted => FfxivTheme.TextMuted;
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
    private readonly InventoryBankService bank;
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
        this.save = save;
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
    }

    // 0.394 (user request): the chosen text font for the whole window, pushed after the theme and popped first.
    private IDisposable? bodyFontScope;
    public override void PreDraw(){FfxivTheme.Push();bodyFontScope=TenshoAssistant.UI.UiComponents.BodyFont is { Available:true } font ? font.Push():null;}
    public override void PostDraw(){bodyFontScope?.Dispose();bodyFontScope=null;FfxivTheme.Pop();}

    internal void OpenSettings(){page=Page.Settings;IsOpen=true;}
    internal void OpenAlarms(){page=Page.Alarms;IsOpen=true;}
    internal void OpenReminders(){page=Page.Reminders;IsOpen=true;}
    // 0.344 (g, I-02): from the currency entry of the server bar.
    internal void OpenCurrencies(){page=Page.Storage;focusCurrencyTab=true;IsOpen=true;}

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
        if(!lastPageDirty)return;
        try{save();lastPageDirty=false;}
        catch(Exception error){log.Warning(error,"The last page used could not be saved.");}
    }

    public override void OnClose()=>SaveLastPage();

    public override void Draw()
    {
        RememberPage();
        DrawGameFrame();
        monsterLoot.SelectItem((uint)Math.Max(0,itemId));
        ValidateGpCopy();
        ValidateCraftCopy();
        ValidateCraftSearch();
        craftChain.ValidateContext(bankBridge.State.CharacterId, (uint)Math.Max(0, itemId), craftPlanTarget, recipeCatalog.State.Catalog);
        triadSolve.ValidateContext(bankBridge.State.CharacterId, triadRevision);
        preparationPrices.ValidateContext(bankBridge.State.CharacterId,market.State.WorldId,(uint)Math.Max(0,itemId),craftPlanTarget,costBasisIndex,costQualityIndex,craftChainChoiceRevision);
        ImGui.TextColored(Accent, localization.Get("Plugin.Title").ToUpperInvariant());
        ImGui.SameLine();
        ImGui.TextDisabled($"v{BuildVersion}");
        if(configuration.ShowHeaderClock){ImGui.SameLine();ImGui.TextDisabled(localization.Format("LocalClock.Header",DateTimeOffset.Now.ToString("HH:mm:ss")));}
        if(configuration.ShowHeaderEorzeaTime)
        {
            var eorzea=Gathering.GatheringTime.MinuteOfDay(DateTimeOffset.UtcNow);
            ImGui.SameLine();ImGui.TextDisabled(localization.Format("LocalClock.HeaderEt",eorzea/60,eorzea%60));
        }
        ImGui.TextDisabled(localization.Get("Plugin.Subtitle"));
        ImGui.Separator();

        var height = Math.Max(260, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing());
        if (ImGui.BeginTable("Workspace", 2, ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                ImGui.TableSetupColumn("Navigation", ImGuiTableColumnFlags.WidthFixed, 200);
                ImGui.TableSetupColumn("Contenu", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                var showNavigation = ImGui.BeginChild("NavigationPane", new Vector2(0, height), true);
                try { if (showNavigation) DrawNavigation(); }
                finally { ImGui.EndChild(); }

                ImGui.TableSetColumnIndex(1);
                var showContent = ImGui.BeginChild("ContentPane", new Vector2(0, height), true);
                try
                {
                    if (showContent)
                    {
                        DrawCategoryNavigation();
                        // 0.290 (R-06 step 6): draw time of each page, averaged, shown in Diagnostics (developer mode).
                        var drawStarted = System.Diagnostics.Stopwatch.GetTimestamp();
                        var drawnPage = page;
                        switch (page)
                        {
                            case Page.Home: DrawHome(); break;
                            case Page.Items: DrawItems(); break;
                            case Page.Storage: DrawStorage(); break;
                            case Page.Reminders: DrawReminders(); break;
                            case Page.Garden: DrawGardenPage(); break;
                            case Page.FreeCompany: DrawFreeCompany(); break;
                            case Page.Gathering: DrawGatheringBrowser(); break;
                            case Page.Maps: DrawMaps(); break;
                            case Page.DutyLoot: DrawDutyLoot(); break;
                            case Page.MonsterLoot: DrawMonsterLoot(); break;
                            case Page.NpcVendors: DrawNpcVendors(); break;
                            case Page.GatheringGp: DrawGatheringGp(); break;
                            case Page.Fashion: DrawFashion(); break;
                            case Page.CraftSimulation: DrawCraftSimulation(); break;
                            case Page.CraftLive: DrawCraftLive(); break;
                            case Page.Weather: DrawWeather(); break;
                            case Page.Alarms: DrawAlarmsPage(); break;
                            case Page.CraftPresets: DrawCraftPresets(); break;
                            case Page.MacroChain: DrawMacroChain(); break;
                            case Page.SortPreview: DrawSortPreview(); break;
                            case Page.Collections: DrawCollections(); break;
                            case Page.BlueMagic: DrawCollections(true); break;
                            case Page.TripleTriad: DrawTripleTriad(); break;
                            case Page.Market: DrawMarket(); break;
                            case Page.Quests: DrawQuests(); break;
                            case Page.Chocobo: DrawChocoboPage(); break;
                            case Page.Leves: DrawLeves(); break;
                            case Page.HuntingLog: DrawHuntingLog(); break;
                            case Page.OceanFishing: DrawOceanFishing(); break;
                            case Page.Events: DrawEventsPage(); break;
                            case Page.Gear: DrawGearPlanner(); break;
                            case Page.MiniCactpot: DrawMiniCactpot(); break;
                            case Page.Settings: DrawSettings(); break;
                            case Page.Diagnostics: DrawDiagnostics(); break;
                            case Page.Credits: DrawCredits(); break;
                        }
                        var drawMs = System.Diagnostics.Stopwatch.GetElapsedTime(drawStarted).TotalMilliseconds;
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
        ImGui.TextDisabled("/tensho");
        ImGui.SameLine();
        ImGui.TextDisabled(localization.Get("UiFragments.Footer"));
    }

    private void DrawNavigation()
    {
        // R-06 step 2 (0.264): pages grouped by category, each category folds (kept in the configuration); the hub pages of the map
        // and the Gold Saucer are now direct entries, the sub-pages of crafting, gathering and collections stay in their own tab bar.
        Navigation(localization.Get("Navigation.Home"), Page.Home);
        if (NavigationCategory("Objects", Page.Items, Page.Storage, Page.SortPreview, Page.Market))
        {
            Navigation(localization.Get("Navigation.Items"), Page.Items);
            Navigation(localization.Get("Navigation.Storage"), Page.Storage);
            Navigation(localization.Get("Category.Sorting"), Page.SortPreview);
            Navigation(localization.Get("Navigation.Market"), Page.Market);
        }
        if (NavigationCategory("Production", Page.CraftSimulation, Page.Gathering, Page.OceanFishing, Page.MacroChain))
        {
            Navigation(localization.Get("Navigation.Crafting"), Page.CraftSimulation);
            Navigation(localization.Get("Navigation.Gathering"), Page.Gathering);
            Navigation(localization.Get("Navigation.OceanFishing"), Page.OceanFishing);
            Navigation(localization.Get("Category.MacroChain"), Page.MacroChain);
        }
        if (NavigationCategory("World", Page.Maps, Page.Weather, Page.Alarms, Page.Reminders, Page.Garden, Page.FreeCompany, Page.NpcVendors, Page.DutyLoot, Page.MonsterLoot))
        {
            Navigation(localization.Get("Navigation.Map"), Page.Maps);
            Navigation(localization.Get("Category.Weather"), Page.Weather);
            Navigation(localization.Format("Navigation.Alarms",configuration.Alarms?.Count ?? 0), Page.Alarms);
            Navigation(localization.Get("Navigation.Reminders"), Page.Reminders);
            Navigation(localization.Get("Navigation.Garden"), Page.Garden);
            Navigation(localization.Get("Navigation.FreeCompany"), Page.FreeCompany);
            Navigation(localization.Get("Category.NpcVendors"), Page.NpcVendors);
            Navigation(localization.Get("Category.DutyLoot"), Page.DutyLoot);
            Navigation(localization.Get("Category.MonsterLoot"), Page.MonsterLoot);
        }
        // 0.341 (user request): the Character page is gone (the Gear page shows the same real values); the chocobo has its own page.
        if (NavigationCategory("Progression", Page.Quests, Page.Events, Page.Collections, Page.Gear, Page.Chocobo, Page.Leves, Page.HuntingLog))
        {
            Navigation(localization.Get("Navigation.Quests"), Page.Quests);
            Navigation(localization.Get("Navigation.Events"), Page.Events);
            Navigation(localization.Get("Navigation.Collection"), Page.Collections);
            Navigation(localization.Get("Navigation.Gear"), Page.Gear);
            Navigation(localization.Get("Navigation.Chocobo"), Page.Chocobo);
            Navigation(localization.Get("Navigation.Leves"), Page.Leves);
            Navigation(localization.Get("Navigation.HuntingLog"), Page.HuntingLog);
        }
        if (NavigationCategory("GoldSaucer", Page.Fashion, Page.TripleTriad, Page.MiniCactpot))
        {
            Navigation(localization.Get("Category.Fashion"), Page.Fashion);
            Navigation(localization.Get("Category.Triad"), Page.TripleTriad);
            Navigation(localization.Get("Category.Cactpot"), Page.MiniCactpot);
        }
        if (NavigationCategory("Preferences", Page.Settings, Page.Credits, Page.Diagnostics))
        {
            Navigation(localization.Get("Navigation.Settings"), Page.Settings);
            // R-13 (0.320): everything Tensho uses, shown to every player.
            Navigation(localization.Get("Navigation.Credits"), Page.Credits);
            if (developerMode) Navigation(localization.Get("Navigation.Diagnostics"), Page.Diagnostics);
        }
        ImGui.Spacing();
        ImGui.Separator();
        using (TenshoAssistant.UI.UiComponents.PushSectionFont()) ImGui.TextDisabled(localization.Get("Navigation.ContextItem"));
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

    private static Page NavigationPage(Page current)=>current switch
    {
        Page.CraftLive or Page.CraftPresets=>Page.CraftSimulation,
        Page.GatheringGp=>Page.Gathering,
        Page.BlueMagic=>Page.Collections,
        _=>current,
    };

    private void Navigation(string label, Page destination)
    {
        var selectedCategory=page switch
        {
            Page.CraftLive or Page.CraftPresets=>Page.CraftSimulation,
            Page.GatheringGp=>Page.Gathering,
            Page.BlueMagic=>Page.Collections,
            _=>page,
        };
        if (UiComponents.NavigationItem(label, selectedCategory == destination))
            page = destination;
    }

    private ItemDefinition? SelectedItem => itemId > 0 ? itemCatalog.State.Catalog?.Find((uint)itemId) : null;

    private static void Heading(string title, string description)
    {
        UiComponents.SectionHeader(title, description);
    }

    private void DrawCatalogStatus()
    {
        var state = itemCatalog.State;
        if (state.Catalog is { } catalog)
            ImGui.TextColored(Success, localization.Format("UiFragments.CatalogReady",catalog.Count.ToString("N0"),catalog.Metadata.Language));
        else ImGui.TextWrapped(state.Error ?? (state.TotalRows > 0
            ? localization.Format("UiFragments.CatalogProgress",state.RowsCopied.ToString("N0"),state.TotalRows.ToString("N0"))
            : localization.Get("UiFragments.CatalogLoading")));
    }
}
