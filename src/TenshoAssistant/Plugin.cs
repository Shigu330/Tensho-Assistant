using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
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
using TenshoAssistant.Community;
using TenshoAssistant.Monsters;
using TenshoAssistant.Fashion;
using TenshoAssistant.Caching;
using TenshoAssistant.Localization;

namespace TenshoAssistant;

public sealed class Plugin : IDalamudPlugin
{
    private const string Command = "/tensho";
    private readonly RegistrationScope registrations = new();
    private readonly IPluginLog log;
    private readonly LocalizationManager localization;
    private readonly MainWindow mainWindow;
    private MapViewWindow? mapWindow;
    private readonly WindowSystem windows = new("TenshoAssistant");
    private readonly InventoryService inventoryService = new();
    private readonly InventoryVisualSortService visualSort=new();
    private readonly ItemCatalogService itemCatalog = new();
    private readonly RecipeCatalogService recipeCatalog = new();
    private readonly CraftActionCatalogService craftActions = new();
    private readonly CraftLiveService craftLive = new();
    private readonly WeatherCatalogService weather = new();
    private readonly GatheringCatalogService gatheringCatalog = new();
    private readonly GpCatalogService gpActions = new();
    private readonly CollectionService collections = new();
    private readonly MarketService market = new();
    private readonly ItemHoverService itemHover = new();
    private readonly QuestService quests = new();
    private readonly CharacterStatsService characterStats = new();
    private readonly MapCatalogService maps = new();
    private readonly MacroObservationService macroObservations = new();
    private readonly MacroChainService macroChain = new();
    private readonly TriadObservationService triadObservation = new();
    private readonly MiniCactpotObservationService cactpotObservation = new();
    private readonly GatheringObservationService gatheringObservation = new();
    private readonly ItemUseCatalogService itemUses = new();
    private readonly DutyCatalogService duties = new();
    private readonly GarlandClient garland = new();
    private readonly DutyLootService dutyLoot;
    private readonly MonsterLootService monsterLoot;
    private readonly FashionHintService fashionHints=new();
    private readonly FashionDataService fashionData=new();
    private readonly BankImportOverlay bankOverlay;
    private Func<bool> autoBankCapture = () => true;
    private readonly ItemContextMenuBridge? itemContextMenu;
    private readonly DataCache? dataCache;
    private CommandInfo? commandInfo;
    private int commandLocalizationRevision=-1;

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commands, IPluginLog log,
        IGameInventory inventory, IPlayerState player, IClientState client, IFramework framework, IDataManager data, IGameGui gameGui,
        ITextureProvider textures, IUnlockState unlocks, IGameInteropProvider interop,IObjectTable objects,IContextMenu contextMenus,IGameConfig gameConfig,ICondition condition,IFateTable fateTable,ITargetManager targets,IPartyList partyList)
    {
        this.log = log;
        localization = new LocalizationManager((error, message) => log.Warning(error, message), pluginInterface.AssemblyLocation.DirectoryName);
        dutyLoot = new DutyLootService(garland);
        monsterLoot = new MonsterLootService(garland);
        try
        {
            registrations.Register(()=>{},()=>dataCache?.Dispose());
            registrations.Register(() => { }, market.Dispose);
            registrations.Register(() => { }, garland.Dispose);
            registrations.Register(() => { }, dutyLoot.Dispose);
            registrations.Register(() => { }, monsterLoot.Dispose);
            registrations.Register(() => { }, fashionData.Dispose);
            var configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            autoBankCapture = () => configuration.AutoBankCapture;
            if (configuration.Version != 1)
                throw new NotSupportedException($"Unsupported configuration version: {configuration.Version}");
            dataCache=new DataCache(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"DataCache-v1"));
            market.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Prices),dataCache);
            garland.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache);
            fashionData.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Fashion),dataCache);
            MarketWorldReader? marketReader = null;
            registrations.Register(() => marketReader = new MarketWorldReader(market, framework, player, client, log, data), () => marketReader?.Dispose());
            var bank = new InventoryBankService(new FileInventoryBankStore(
                Path.Combine(pluginInterface.GetPluginConfigDirectory(), "InventoryBank.json")));
            registrations.Register(() => { }, bank.Dispose);
            var bankBridge = new BankInventoryBridge(bank, inventory, gameGui, player, client, framework, log, () => configuration.AutoBankCapture);
            registrations.Register(() => { }, bankBridge.Dispose);
            bankOverlay = new BankImportOverlay(bank, bankBridge, localization);
            localization.SelectInitialLanguage(configuration.LanguageCode,client.ClientLanguage.ToString());
            mainWindow = new MainWindow(configuration, () => pluginInterface.SavePluginConfig(configuration), log, localization,
                inventoryService, itemCatalog, recipeCatalog, gatheringCatalog, collections, market, itemHover, quests, characterStats, maps, macroObservations, macroChain, triadObservation, bank, bankBridge, textures, gameGui, duties, dutyLoot, monsterLoot,gpActions,fashionHints,fashionData,craftActions,craftLive,weather,cactpotObservation,gatheringObservation,itemUses,visualSort) { IsOpen = configuration.OpenOnLoad };
            registrations.Register(() => { }, mainWindow.Dispose);
            mainWindow.ConfigureDataCache(dataCache);
            UI.GameThemeReader? themeReader=null;
            registrations.Register(()=>themeReader=new UI.GameThemeReader(gameConfig,log),()=>themeReader?.Dispose());
            mainWindow.ConfigureTheme(()=>themeReader?.GameValue);
            var wikiSources=new WikiMonsterSources(id=>itemCatalog.State.Catalog?.Find(id)?.EnglishName,()=>configuration.Cache.Duration(CacheArea.Sources),dataCache);
            registrations.Register(()=>{},wikiSources.Dispose);
            mainWindow.ConfigureWikiSources(wikiSources);
            var monsterHover=new Monsters.MonsterHoverSources(wikiSources);
            registrations.Register(()=>{},monsterHover.Dispose);
            mainWindow.ConfigureMonsterHover(monsterHover);
            Maps.MapLiveReader? mapLive=null;
            registrations.Register(()=>mapLive=new Maps.MapLiveReader(framework,client,objects,data,condition,fateTable,partyList,log),()=>mapLive?.Dispose());
            var mapLocal=new Maps.MapLocalIndexService();
            Maps.MapLocalIndexLoader? mapLocalLoader=null;
            registrations.Register(()=>mapLocalLoader=new Maps.MapLocalIndexLoader(mapLocal,data,framework,log,dataCache),()=>mapLocalLoader?.Dispose());
            mainWindow.ConfigureMapView(mapLive!,new Maps.MapMarkerSource(data,log),mapLocal);
            var itemSources=new Community.GarlandItemSourceService(garland);
            registrations.Register(()=>{},itemSources.Dispose);
            mainWindow.ConfigureItemSources(itemSources);
            var wikiItems=new Community.WikiItemAcquisitionService(id=>itemCatalog.State.Catalog?.Find(id)?.EnglishName,()=>configuration.Cache.Duration(CacheArea.Sources),dataCache);
            registrations.Register(()=>{},wikiItems.Dispose);
            mainWindow.ConfigureWikiItems(wikiItems);
            var wikiVistas=new Maps.WikiVistaSource(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache);
            registrations.Register(()=>{},wikiVistas.Dispose);
            mainWindow.ConfigureWikiVistas(wikiVistas);
            Maps.NativeMapReplacer? mapReplacer=null;
            registrations.Register(()=>mapReplacer=new Maps.NativeMapReplacer(framework,gameGui,client,()=>configuration.MapView,mainWindow.ShowMapFromGame,()=>mainWindow.MapViewOpen,mainWindow.CloseMapFromGame,log),()=>mapReplacer?.Dispose());
            mainWindow.ConfigureGameMapOpener(()=>mapReplacer?.OpenGameMap());
            var lumina=new Community.LuminaSupplementalService(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>().TryGetRow(id,out var monster) ? monster.Singular.ExtractText():null,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.Fate>().TryGetRow(id,out var fate) ? fate.Name.ExtractText():null);
            registrations.Register(()=>{},lumina.Dispose);
            mainWindow.ConfigureLumina(lumina);
            var dutyFinder=new Duties.DutyFinderService();
            Duties.DutyFinderReader? dutyFinderReader=null;
            registrations.Register(()=>dutyFinderReader=new Duties.DutyFinderReader(dutyFinder,framework,gameGui,log,()=>mainWindow.DeveloperMode),()=>dutyFinderReader?.Dispose());
            mainWindow.ConfigureDutyFinder(dutyFinder);
            var triadNpcs=new TriadNpcService();
            TriadNpcLoader? triadNpcLoader=null;
            registrations.Register(()=>triadNpcLoader=new TriadNpcLoader(triadNpcs,data,framework,client,player,log),()=>triadNpcLoader?.Dispose());
            mainWindow.ConfigureTriadNpcs(triadNpcs);
            var npcVendors=new Npcs.NpcVendorService();
            Npcs.NpcVendorLoader? npcLoader=null;
            registrations.Register(()=>npcLoader=new Npcs.NpcVendorLoader(npcVendors,data,framework,log,dataCache),()=>npcLoader?.Dispose());
            mainWindow.ConfigureNpcVendors(npcVendors);
            itemContextMenu=new ItemContextMenuBridge(configuration,contextMenus,player,client,itemCatalog,log,localization,gameGui);
            registrations.Register(()=>{ },itemContextMenu.Dispose);
            InventoryVisualSortBridge? sortBridge=null;
            registrations.Register(()=>sortBridge=new InventoryVisualSortBridge(visualSort,inventory,player,client,framework,log,bankBridge),()=>sortBridge?.Dispose());
            MacroChainBridge? macroBridge = null;
            registrations.Register(() => macroBridge = new MacroChainBridge(macroChain, framework, player, client, commands, interop, log), () => macroBridge?.Dispose());
            MacroObservationReader? macroObserver = null;
            registrations.Register(() => macroObserver = new MacroObservationReader(macroObservations, framework, player, client, log), () => macroObserver?.Dispose());
            ItemHoverReader? hoverReader = null;
            registrations.Register(() => hoverReader = new ItemHoverReader(configuration, market, itemHover, itemCatalog, gameGui, player, client, framework, log),
                () => hoverReader?.Dispose());
            MapCatalogLoader? mapLoader = null;
            registrations.Register(() => mapLoader = new MapCatalogLoader(maps, data, framework, log), () => mapLoader?.Dispose());
            DutyCatalogLoader? dutyLoader = null;
            registrations.Register(() => dutyLoader = new DutyCatalogLoader(duties, data, framework, log), () => dutyLoader?.Dispose());
            CharacterStatsReader? statsReader = null;
            registrations.Register(() => statsReader = new CharacterStatsReader(characterStats, framework, player, client, log,objects,data,gameGui), () => statsReader?.Dispose());
            GpCatalogLoader? gpLoader=null;
            WeatherCatalogLoader? weatherLoader=null;
            registrations.Register(()=>weatherLoader=new WeatherCatalogLoader(weather,data,framework,log),()=>weatherLoader?.Dispose());
            registrations.Register(()=>gpLoader=new GpCatalogLoader(gpActions,data,framework,log),()=>gpLoader?.Dispose());
            CraftActionCatalogLoader? craftActionLoader=null;
            registrations.Register(()=>craftActionLoader=new CraftActionCatalogLoader(craftActions,data,framework,log),()=>craftActionLoader?.Dispose());
            CraftLiveReader? craftLiveReader=null;
            registrations.Register(()=>craftLiveReader=new CraftLiveReader(craftLive,recipeCatalog,craftActions,itemCatalog,framework,player,client,gameGui,objects,log),()=>craftLiveReader?.Dispose());
            CraftLogReader? craftLogReader=null;
            registrations.Register(()=>craftLogReader=new CraftLogReader(mainWindow.CraftLog,recipeCatalog,configuration,framework,player,client,gameGui,log),()=>craftLogReader?.Dispose());
            FashionHintLoader? fashionLoader=null;
            registrations.Register(()=>fashionLoader=new FashionHintLoader(fashionHints,data,framework,log),()=>fashionLoader?.Dispose());
            QuestReader? questReader = null;
            registrations.Register(() => questReader = new QuestReader(quests, data, framework, player, client, unlocks, log,()=>configuration.Cache.Duration(CacheArea.Quests),dataCache),
                () => questReader?.Dispose());
            CollectionReader? collectionReader = null;
            registrations.Register(() => collectionReader = new CollectionReader(collections, data, framework, player, client, unlocks, log,()=>configuration.Cache.Duration(CacheArea.Collections),dataCache,()=>configuration.Cache.Duration(CacheArea.Completion)),
                () => collectionReader?.Dispose());
            TriadObservationReader? triadReader = null;
            MiniCactpotObservationReader? cactpotReader=null;
            GatheringObservationReader? gatheringReader=null;
            registrations.Register(()=>gatheringReader=new GatheringObservationReader(gatheringObservation,framework,player,client,gameGui,objects,log),()=>gatheringReader?.Dispose());
            registrations.Register(()=>cactpotReader=new MiniCactpotObservationReader(cactpotObservation,framework,player,client,gameGui,log),()=>cactpotReader?.Dispose());
            registrations.Register(() => triadReader = new TriadObservationReader(triadObservation, framework, player, client, gameGui, log, () => targets.Target?.BaseId ?? 0, data, () => mainWindow.DeveloperMode),
                () => triadReader?.Dispose());
            ItemCatalogLoader? catalogLoader = null;
            ItemUseCatalogLoader? usageLoader=null;
            registrations.Register(()=>usageLoader=new ItemUseCatalogLoader(itemUses,data,framework,log),()=>usageLoader?.Dispose());
            registrations.Register(() => catalogLoader = new ItemCatalogLoader(itemCatalog, data, framework, log, dataCache),
                () => catalogLoader?.Dispose());
            RecipeCatalogLoader? recipeLoader = null;
            registrations.Register(() => recipeLoader = new RecipeCatalogLoader(recipeCatalog, data, framework, log, dataCache),
                () => recipeLoader?.Dispose());
            GatheringCatalogLoader? gatheringLoader = null;
            registrations.Register(() => gatheringLoader = new GatheringCatalogLoader(gatheringCatalog, data, framework, log, dataCache),
                () => gatheringLoader?.Dispose());
            DalamudInventoryReader? inventoryReader = null;
            registrations.Register(() => inventoryReader = new DalamudInventoryReader(inventoryService, inventory, player, client, framework, log),
                () => inventoryReader?.Dispose());
            craftLive.SetEnabled(configuration.Observations?.GetValueOrDefault("Craft") == true);
            gatheringObservation.SetEnabled(configuration.Observations?.GetValueOrDefault("Gathering") == true);
            cactpotObservation.SetEnabled(configuration.Observations?.GetValueOrDefault("Cactpot") == true);
            triadObservation.SetEnabled(configuration.Observations?.GetValueOrDefault("Triad") == true);
            macroObservations.SetEnabled(configuration.Observations?.GetValueOrDefault("Macros") == true);
            registrations.Register(() => windows.AddWindow(mainWindow), () => windows.RemoveWindow(mainWindow));
            registrations.Register(() => windows.AddWindow(mapWindow = new MapViewWindow(mainWindow)), () => { if (mapWindow is not null) windows.RemoveWindow(mapWindow); });
            registrations.Register(() =>
            {
                commandInfo=new CommandInfo(OnCommand){HelpMessage=localization.Get("Plugin.CommandHelp")};
                commandLocalizationRevision=localization.Revision;
                if (!commands.AddHandler(Command,commandInfo))
                    throw new InvalidOperationException($"Command already registered: {Command}");
            }, () => commands.RemoveHandler(Command));
            registrations.Register(() => pluginInterface.UiBuilder.Draw += Draw,
                () => pluginInterface.UiBuilder.Draw -= Draw);
            registrations.Register(() => pluginInterface.UiBuilder.OpenMainUi += Open,
                () => pluginInterface.UiBuilder.OpenMainUi -= Open);
            registrations.Register(() => pluginInterface.UiBuilder.OpenConfigUi += OpenConfig,
                () => pluginInterface.UiBuilder.OpenConfigUi -= OpenConfig);
            Action openOnLogin = () =>
            {
                if (configuration.OpenOnLogin) Open();
            };
            registrations.Register(() => client.Login += openOnLogin,
                () => client.Login -= openOnLogin);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void OnCommand(string command, string arguments) => Open();
    private void Open() => mainWindow.IsOpen = true;
    private void OpenConfig()=>mainWindow.OpenSettings();
    private void Draw()
    {
        if(commandInfo is not null && commandLocalizationRevision!=localization.Revision)
        {commandInfo.HelpMessage=localization.Get("Plugin.CommandHelp");commandLocalizationRevision=localization.Revision;}
        if(itemContextMenu?.Take() is { } request)mainWindow.OpenItemContext(request);
        mainWindow.ApplyTheme();
        // The map's open state lives in MainWindow (game map key M, journal); its window follows it.
        if(mapWindow is not null)mapWindow.IsOpen=mainWindow.MapViewOpen;
        windows.Draw();
        if (!autoBankCapture()) bankOverlay.Draw();
        mainWindow.DrawCraftLogHelper();
        mainWindow.DrawItemHover();
        mainWindow.DrawCactpotOverlay();
        mainWindow.DrawTriadOverlay();
        mainWindow.DrawTriadRequestPanel();
        mainWindow.DrawTriadDeckReminder();
        mainWindow.DrawCharacterPanel();
        mainWindow.DrawDutyFinderPanel();
    }

    public void Dispose()
    {
        try { registrations.Dispose(); }
        catch (AggregateException error) { log.Error(error, "Failed to release some Tensho registrations."); }
    }
}
