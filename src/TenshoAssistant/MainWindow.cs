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

internal sealed partial class MainWindow : Window, IDisposable
{
    private enum Page { Home, Items, Gathering, Maps, MacroChain, SortPreview, Collections, BlueMagic, TripleTriad, Market, Quests, Character, MiniCactpot, Settings, Diagnostics, DutyLoot, MonsterLoot, GatheringGp, Fashion, CraftSimulation, CraftLive, Weather, CraftPresets, NpcVendors }
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
        BankInventoryBridge bankBridge, ITextureProvider textures, IGameGui gameGui, DutyCatalogService duties, DutyLootService dutyLoot, MonsterLootService monsterLoot,GpCatalogService gpActions,FashionHintService fashionHints,FashionDataService fashionData,CraftActionCatalogService craftActions,CraftLiveService craftLive,WeatherCatalogService weather,MiniCactpotObservationService cactpotObservation,GatheringObservationService gatheringObservation,ItemUseCatalogService itemUses,InventoryVisualSortService visualSort) : base("Tensho--Assistant###TenshoMain")
    {
        this.configuration = configuration;
        this.save = save;
        this.log = log;
        this.localization = localization;
        this.inventory = inventory;
        this.visualSort=visualSort;
        this.itemCatalog = itemCatalog;
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
        Size = new Vector2(1040, 700);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 480),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void PreDraw()=>FfxivTheme.Push();
    public override void PostDraw()=>FfxivTheme.Pop();

    internal void OpenSettings(){page=Page.Settings;IsOpen=true;}

    public override void Draw()
    {
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
        ImGui.SameLine();
        ImGui.TextDisabled(localization.Format("LocalClock.Header",DateTimeOffset.Now.ToString("HH:mm:ss")));
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
                        switch (page)
                        {
                            case Page.Home: DrawHome(); break;
                            case Page.Items: DrawItems(); break;
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
                            case Page.CraftPresets: DrawCraftPresets(); break;
                            case Page.MacroChain: DrawMacroChain(); break;
                            case Page.SortPreview: DrawSortPreview(); break;
                            case Page.Collections: DrawCollections(); break;
                            case Page.BlueMagic: DrawCollections(true); break;
                            case Page.TripleTriad: DrawTripleTriad(); break;
                            case Page.Market: DrawMarket(); break;
                            case Page.Quests: DrawQuests(); break;
                            case Page.Character: DrawCharacterStats(); break;
                            case Page.MiniCactpot: DrawMiniCactpot(); break;
                            case Page.Settings: DrawSettings(); break;
                            case Page.Diagnostics: DrawDiagnostics(); break;
                        }
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
        ImGui.TextDisabled(localization.Get("Navigation.Workspace"));
        Navigation(localization.Get("Navigation.Home"), Page.Home);
        Navigation(localization.Get("Navigation.Items"), Page.Items);
        ImGui.Spacing();
        ImGui.TextDisabled(localization.Get("Navigation.Modules"));
        Navigation(localization.Get("Navigation.Crafting"), Page.CraftSimulation);
        Navigation(localization.Get("Navigation.Gathering"), Page.Gathering);
        Navigation(localization.Get("Navigation.Collection"), Page.Collections);
        Navigation(localization.Get("Navigation.Quests"), Page.Quests);
        Navigation(localization.Get("Navigation.Map"), Page.Maps);
        Navigation(localization.Get("Navigation.Market"), Page.Market);
        Navigation(localization.Get("Navigation.Automation"), Page.MacroChain);
        Navigation(localization.Get("Navigation.Combat"), Page.Character);
        Navigation(localization.Get("Category.GoldSaucer"), Page.MiniCactpot);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled(localization.Get("Navigation.Preferences"));
        Navigation(localization.Get("Navigation.Settings"), Page.Settings);
        if (developerMode) Navigation(localization.Get("Navigation.Diagnostics"), Page.Diagnostics);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled(localization.Get("Navigation.ContextItem"));
        if (SelectedItem is { } selected)
        {
            DrawItemIcon(selected, new Vector2(32));
            ImGui.TextWrapped(selected.Name);
            if(developerMode)ImGui.TextDisabled($"ID {selected.Id}");
            if (ImGui.SmallButton(localization.Get("Navigation.ViewQuantities"))) { page = Page.Items; focusStocksTab = true; }
            DrawContextTools();
        }
        else if (itemId > 0) ImGui.TextWrapped(localization.Format("BankUi.Item",itemId));
        else ImGui.TextWrapped(localization.Get("Navigation.SelectItem"));
    }

    private void Navigation(string label, Page destination)
    {
        var selectedCategory=page switch
        {
            Page.CraftLive or Page.CraftPresets=>Page.CraftSimulation,
            Page.GatheringGp=>Page.Gathering,
            Page.BlueMagic=>Page.Collections,
            Page.Weather or Page.DutyLoot or Page.MonsterLoot or Page.NpcVendors=>Page.Maps,
            Page.SortPreview=>Page.MacroChain,
            Page.Fashion or Page.TripleTriad=>Page.MiniCactpot,
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

    private void DrawHome()
    {
        Heading(localization.Get("Home.Title"),localization.Get("Home.Description"));
        var items = itemCatalog.State;
        var recipes = recipeCatalog.State;
        UiComponents.StatusBadge(localization.Get(items.Catalog is not null && recipes.Catalog is not null ? "Home.Ready":"Home.Loading"),
            items.Catalog is not null && recipes.Catalog is not null ? Success:Accent);
        ImGui.TextWrapped(localization.Format("Home.Catalogues",items.Catalog?.Count ?? 0,recipes.Catalog?.Count ?? 0));
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Home.Activity"));
        var busy = false;
        if(craftSolve.State.Loading) { ImGui.TextWrapped(localization.Get("CraftSearch.Loading")); busy=true; }
        if(craftChain.State.Loading) { ImGui.TextWrapped(localization.Get("Home.CraftChain")); busy=true; }
        if(cactpotSolve.State.Loading) { ImGui.TextWrapped(localization.Get("Home.Cactpot")); busy=true; }
        if(!busy) ImGui.TextDisabled(localization.Get("Home.Idle"));
        if(SelectedItem is { } contextItem)
        {
            ImGui.Separator();
            ImGui.TextColored(Accent,contextItem.Name);
            if(ImGui.SmallButton(localization.Get("Home.Stocks")+"###HomeStocks")) { page=Page.Items; focusStocksTab=true; }
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("Home.Recipes")+"###HomeRecipes")) { page=Page.Items; focusCraftTab=true; }
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("Navigation.Market")+"###HomeMarket"))page=Page.Market;
        }
        if(items.Error is not null || recipes.Error is not null || market.State.Error is not null || quests.State.Error is not null)
        {
            ImGui.Separator();
            ImGui.TextWrapped(localization.Get("Home.Errors"));
            if(items.Error is not null) ImGui.TextWrapped(localization.Get("Navigation.Items"));
            if(recipes.Error is not null) ImGui.TextWrapped(localization.Get("Navigation.Crafting"));
            if(market.State.Error is not null) ImGui.TextWrapped(localization.Get("Navigation.Market"));
            if(quests.State.Error is not null) ImGui.TextWrapped(localization.Get("Navigation.Quests"));
            if(developerMode && ImGui.SmallButton(localization.Get("Navigation.Diagnostics")))page=Page.Diagnostics;
        }
        ImGui.Separator();
        if(revealHomeTools) { ImGui.SetNextItemOpen(true); revealHomeTools=false; }
        if(ImGui.CollapsingHeader(localization.Get("Plugin.AllTools")+"###HomeTools")) DrawTools();
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
