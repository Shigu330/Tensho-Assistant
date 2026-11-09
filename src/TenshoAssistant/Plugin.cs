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
    // X-92 (0.972): the game data check after a game update, cancelled when the plugin unloads.
    private readonly CancellationTokenSource gameDataCancel=new();
    private readonly LocalizationManager localization;
    private readonly MainWindow mainWindow;
    private MapViewWindow? mapWindow;
    private GuideWindow? guideWindow;
    private PanelWindow[] panelWindows = [];
    private AttachedPanels? attachedPanels;
    private readonly WindowSystem windows = new("TenshoAssistant");
    // X-109 (0.981, DEC-047): the Beastmaster board's team panel, the only window drawn during a cut, and only while the labyrinth's board is open.
    private readonly WindowSystem boardWindows = new("TenshoBeastBoard");
    private BeastBoardWindow? boardWindow;
    private Beastmaster.BeastBoardReader? boardReader;
    private Func<bool> showBoardPanel=()=>false;
    private bool boardPanelBroken;
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
    private readonly MarketBoardService marketBoard = new();
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
    // 0.394: the font settings, read by Draw (the configuration is a constructor local).
    private Func<UI.FontSettings?> fontSettings = () => null;
    private readonly ItemContextMenuBridge? itemContextMenu;
    private readonly DataCache? dataCache;
    private CommandInfo? commandInfo;
    private int commandLocalizationRevision=-1;
    private Func<Alarms.AlarmDtrEntry?>? alarmBar;
    private Func<Alarms.ReminderDtrEntry?>? reminderBar;
    private Market.UndercutWatcher? undercut;
    // 0.349 (g, X-07): the chat, for the « /tensho wiki | lodestone » answers.
    private readonly IChatGui chatGui;
    private Diagnostics.SelfCheckRunner? selfCheck;
    // R-12 (0.414): items whose « Open » chat link handler is registered (command id = item id), all removed on unload.
    private readonly Dictionary<uint,Dalamud.Game.Text.SeStringHandling.Payloads.DalamudLinkPayload> alarmLinks=[];

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commands, IPluginLog log,
        IGameInventory inventory, IPlayerState player, IClientState client, IFramework framework, IDataManager data, IGameGui gameGui,
        ITextureProvider textures, IUnlockState unlocks, IGameInteropProvider interop,IObjectTable objects,IContextMenu contextMenus,IGameConfig gameConfig,ICondition condition,IFateTable fateTable,ITargetManager targets,IPartyList partyList,IChatGui chat,IDtrBar dtrBar,IKeyState keyState,IMarketBoard marketBoardEvents)
    {
        // 0.525 (user report: lag when the plugin loads): what the load costs, said in the log, to tell the plugin's own work from the runtime's pauses.
        var loadStarted=System.Diagnostics.Stopwatch.GetTimestamp();var pausesBefore=GC.GetTotalPauseDuration();var gen2Before=GC.CollectionCount(2);
        this.log = log;
        chatGui = chat;
        localization = new LocalizationManager((error, message) => log.Warning(error, message), pluginInterface.AssemblyLocation.DirectoryName);
        dutyLoot = new DutyLootService(garland);
        monsterLoot = new MonsterLootService(garland);
        try
        {
            // DEC-038: decided first: no service starts in PvP or ranked content.
            chatForCut=chat;
            TenshoAssistant.Competitive.CompetitiveWatcher? competitive=null;
            registrations.Register(()=>competitive=new TenshoAssistant.Competitive.CompetitiveWatcher(framework,client,data,log,gameGui,condition),()=>competitive?.Dispose());
            registrations.Register(()=>{},()=>dataCache?.Dispose());
            registrations.Register(() => { }, market.Dispose);
            registrations.Register(() => { }, marketBoard.Dispose);
            registrations.Register(() => { }, garland.Dispose);
            registrations.Register(() => { }, dutyLoot.Dispose);
            registrations.Register(() => { }, monsterLoot.Dispose);
            registrations.Register(() => { }, fashionData.Dispose);
            var saved = pluginInterface.GetPluginConfig() as Configuration;
            var newInstall = saved is null;
            var configuration = saved ?? new Configuration();
            TenshoAssistant.Competitive.CompetitiveGate.Developer=()=>configuration.DeveloperMode;
            TenshoAssistant.Competitive.CompetitiveGate.SlowSubscription=(name,spent)=>log.Information("Slow reader: {0} took {1:0} ms in one frame (named once).",name,spent);
            autoBankCapture = () => configuration.AutoBankCapture;
            fontSettings = () => configuration.Fonts;
            if (configuration.Version != 1)
                throw new NotSupportedException($"Unsupported configuration version: {configuration.Version}");
            // 0.750 (X-02): once, the longer cache defaults; saved with the other settings at their next save.
            if (!configuration.CacheLongerDefaults) { configuration.Cache.LongerDefaults(); configuration.CacheLongerDefaults = true; }
            dataCache=new DataCache(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"DataCache-v1"));
            market.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Prices),dataCache);
            marketBoard.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Prices),dataCache);
            garland.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache);
            // 0.324: each source switch stops every request of its source (it used to cover part of it only).
            garland.Enabled=()=>configuration.CommunityGarland;
            market.Enabled=()=>configuration.CommunityUniversalis;
            marketBoard.Enabled=()=>configuration.CommunityUniversalis;
            // X-73.15 (0.871): the region of each data centre from the game sheet, so that Universalis' table is asked only for what the game does not name.
            marketBoard.GameRegions=data.GetExcelSheet<Lumina.Excel.Sheets.WorldDCGroupType>().Where(row=>row.Name.ExtractText().Length>0 && row.Region.RowId!=0)
                .GroupBy(row=>row.Name.ExtractText(),StringComparer.OrdinalIgnoreCase).ToDictionary(group=>group.Key,group=>group.First().Region.RowId,StringComparer.OrdinalIgnoreCase);
            fashionData.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Fashion),dataCache);
            fashionData.Enabled=()=>configuration.CommunityFashion;
            MarketWorldReader? marketReader = null;
            registrations.Register(() => marketReader = new MarketWorldReader(market, framework, player, client, log, data), () => marketReader?.Dispose());
            // 0.345 (g, I-03): one file per character (InventoryBank/<id>.json); the former shared file is split once.
            var bank = new InventoryBankService(new PerCharacterBankStore(
                Path.Combine(pluginInterface.GetPluginConfigDirectory(), "InventoryBank"),
                Path.Combine(pluginInterface.GetPluginConfigDirectory(), "InventoryBank.json")));
            registrations.Register(() => { }, bank.Dispose);
            IReadOnlyList<(uint,uint)>? cabinet = null;
            var bankBridge = new BankInventoryBridge(bank, inventory, gameGui, player, client, framework, log, () => configuration.AutoBankCapture,
                () => cabinet ??= data.GetExcelSheet<Lumina.Excel.Sheets.Cabinet>().Where(row => row.Item.RowId != 0).Select(row => (row.RowId, row.Item.RowId)).ToArray(),
                set => data.GetExcelSheet<Lumina.Excel.Sheets.MirageStoreSetItem>().TryGetRow(set, out var outfit)
                    ? new[] { outfit.MainHand.RowId, outfit.OffHand.RowId, outfit.Head.RowId, outfit.Body.RowId, outfit.Hands.RowId, outfit.Legs.RowId,
                        outfit.Feet.RowId, outfit.Earrings.RowId, outfit.Necklace.RowId, outfit.Bracelets.RowId, outfit.Ring.RowId }
                    : []);
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
            var wikiSources=new WikiMonsterSources(id=>itemCatalog.State.Catalog?.Find(id)?.EnglishName,()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>configuration.CommunityWiki};
            registrations.Register(()=>{},wikiSources.Dispose);
            mainWindow.ConfigureWikiSources(wikiSources);
            // X-90 (0.994): duty guides from the wiki, by the duty's English name (ContentFinderCondition, English sheet).
            var dutyGuides=new Duties.WikiDutyGuides(id=>data.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>(Dalamud.Game.ClientLanguage.English).GetRowOrDefault(id)?.Name.ExtractText() is { Length:>0 } english
                ? char.ToUpperInvariant(english[0])+english[1..]:null,()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>configuration.CommunityWiki};
            registrations.Register(()=>{},dutyGuides.Dispose);
            mainWindow.DutyGuides=dutyGuides;
            mainWindow.InCombatNow=()=>condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            // g, C-13 (0.367): monsters that teach a blue magic spell, from the wiki's spell page (English action name), on click only.
            // 0.387 (user report « never gives the monster »): the AozAction → Action link resolved in the client's language, so the wiki
            // was asked for the French page (« Mauvaise_haleine »: missing) and said « no monster ». The name now comes from the English
            // Action sheet by row; family « -2 » drops the empty answers cached under the wrong title.
            var blueWiki=new WikiMonsterSources(id=>data.GetExcelSheet<Lumina.Excel.Sheets.AozAction>().GetRowOrDefault(id) is { Action.RowId:not 0 } aoz
                    ? data.GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.English).GetRowOrDefault(aoz.Action.RowId)?.Name.ExtractText():null,
                ()=>configuration.Cache.Duration(CacheArea.Sources),dataCache,family:"wiki-blue-spells-2"){Enabled=()=>configuration.CommunityWiki};
            registrations.Register(()=>{},blueWiki.Dispose);
            mainWindow.BlueSpellWiki=blueWiki;
            // 0.798 (X-38): the monsters that teach each spell, for the nearby monsters; English monster names matched to BNpcName rows once, off the frame.
            var englishMonsters=new Lazy<IReadOnlyDictionary<string,uint[]>>(()=>data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>(Dalamud.Game.ClientLanguage.English)
                .Select(row=>(Name:row.Singular.ExtractText().Trim(),Id:row.RowId)).Where(row=>row.Name.Length>0)
                .GroupBy(row=>row.Name,StringComparer.OrdinalIgnoreCase).ToDictionary(group=>group.Key,group=>group.Select(row=>row.Id).ToArray(),StringComparer.OrdinalIgnoreCase));
            var blueNearby=new Monsters.BlueSpellMonsterIndex(blueWiki,name=>englishMonsters.Value.TryGetValue(name,out var ids) ? ids:[],()=>configuration.CommunityWiki);
            registrations.Register(()=>{},blueNearby.Dispose);
            mainWindow.BlueSpellNearby=blueNearby;
            // 0.804 (X-39): the same English names for the Beastmaster's wiki bestiary.
            mainWindow.EnglishMonsterIds=name=>englishMonsters.Value.TryGetValue(name,out var ids) ? ids:[];
            var beastWiki=new Monsters.BeastWikiIndex(dataCache,()=>configuration.CommunityWiki);
            registrations.Register(()=>{},beastWiki.Dispose);
            mainWindow.BeastWiki=beastWiki;
            // X-54 (0.820): the storage windows' cells holding what the lists need.
            Inventory.StorageHighlightReader? storageHighlight=null;
            registrations.Register(()=>mainWindow.StorageHighlight=storageHighlight=new Inventory.StorageHighlightReader(framework,gameGui,log,()=>configuration.DeveloperMode,inventory),()=>storageHighlight?.Dispose());
            // X-43 (0.807): the local base of discoveries, read off the frame; nothing is written before it is read.
            var discoveryFolder=Path.Combine(pluginInterface.GetPluginConfigDirectory(),"Discoveries");
            var discoveries=new Discoveries.DiscoveryStore();
            mainWindow.DiscoveryStore=discoveries;mainWindow.DiscoveryFolder=discoveryFolder;
            // X-77.1 (0.883): the exchange of discoveries through GitHub (connection by code, opt-in).
            Discoveries.GitHubExchange? discoveryExchange=null;
            registrations.Register(()=>mainWindow.DiscoveryExchange=discoveryExchange=new Discoveries.GitHubExchange(discoveryFolder),()=>discoveryExchange?.Dispose());
            mainWindow.MonsterName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>().GetRowOrDefault(id)?.Singular.ExtractText() ?? "";
            mainWindow.FateName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Fate>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";
            mainWindow.AirshipSectorName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.AirshipExplorationPoint>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";
            mainWindow.SectorName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.SubmarineExploration>().GetRowOrDefault(id)?.Destination.ExtractText() ?? "";
            mainWindow.DutyName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";
            // X-74 (0.860): where the items got come from, and the fish caught (Discoveries/ItemDiscoveryWatcher).
            HashSet<uint>? fishItems=null;var territoryDuties=new Dictionary<uint,uint>();
            Discoveries.ItemDiscoveryWatcher? itemWatcher=null;
            registrations.Register(()=>itemWatcher=new Discoveries.ItemDiscoveryWatcher(framework,client,gameGui,objects,targets,condition,inventory,discoveries,log,
                ()=>configuration.Discovery,()=>configuration.DeveloperMode && configuration.Discovery.DebugLog,
                territory=>territoryDuties.TryGetValue(territory,out var duty) ? duty:territoryDuties[territory]=data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(territory)?.ContentFinderCondition.RowId ?? 0,
                item=>(fishItems??=data.GetExcelSheet<Lumina.Excel.Sheets.FishParameter>().Where(row=>row.Item.RowType==typeof(Lumina.Excel.Sheets.Item)).Select(row=>row.Item.RowId).ToHashSet()).Contains(item),
                item=>data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(item)?.ItemAction.RowId is > 0,chat),()=>itemWatcher?.Dispose());
            // X-52 (0.817): the prices seen at the market board, in place of Universalis' when newer.
            var seenPrices=new Market.SeenPriceStore();
            mainWindow.SeenPrices=seenPrices;
            // X-73.1 (0.857): then the data center's cheapest offers completed by the worlds visited.
            Dictionary<uint,uint>? worldRegions=null;(uint World,uint[] Worlds) regionWorlds=(0,[]);
            uint[] RegionWorlds(uint home)
            {
                if(home==0)return [];
                if(regionWorlds.World==home)return regionWorlds.Worlds;
                worldRegions??=data.GetExcelSheet<Lumina.Excel.Sheets.World>().Where(row=>row.IsPublic && row.DataCenter.RowId!=0)
                    .ToDictionary(row=>row.RowId,row=>row.DataCenter.ValueNullable?.Region.RowId ?? 0);
                var region=worldRegions.GetValueOrDefault(home);
                regionWorlds=(home,region==0 ? []:worldRegions.Where(pair=>pair.Value==region).Select(pair=>pair.Key).ToArray());
                return regionWorlds.Worlds;
            }
            market.Seen=(world,item,universalis)=>
            {
                if(!configuration.Discovery.Enabled || !configuration.Discovery.MarketPrices)return null;
                var now=DateTimeOffset.UtcNow;
                var quote=seenPrices.Prefer(world,item,universalis,now,configuration.CommunityUniversalis) ?? universalis;
                // X-75.5 (0.879): and the region's, from the worlds of the region seen (World → data centre → region, game sheets).
                return seenPrices.WithRegion(seenPrices.WithDataCenter(quote,market.State.WorldNames?.Keys.ToArray() ?? [],now),RegionWorlds(market.State.WorldId),now);
            };
            Market.SeenPriceReader? seenReader=null;
            registrations.Register(()=>seenReader=new Market.SeenPriceReader(marketBoardEvents,seenPrices,()=>market.State.WorldId,()=>configuration.Discovery.Enabled && configuration.Discovery.MarketPrices,log)
                {OwnRetainers=()=>bankBridge.State.KnownRetainers?.Select(known=>known.Id).ToHashSet()},()=>seenReader?.Dispose());
            _=Task.Run(()=>
            {
                try{if(Market.SeenPriceStore.Read(Path.Combine(discoveryFolder,"MarketSeen.json")) is { } prices)seenPrices.Load(prices);}
                catch(Exception error){log.Warning(error,"The market prices seen could not be read; the file is kept as .broken.");try{File.Move(Path.Combine(discoveryFolder,"MarketSeen.json"),Path.Combine(discoveryFolder,"MarketSeen.json.broken"),true);}catch(Exception){}}
                var saved=Path.Combine(discoveryFolder,"Discoveries.json");
                try{if(Discoveries.DiscoveryFiles.Read(saved) is { } file)discoveries.Load(file);}
                catch(Exception error)
                {
                    // A file that cannot be read is set aside, never overwritten.
                    log.Warning(error,"The discoveries could not be read; the file is kept as .broken.");
                    try{File.Move(saved,saved+".broken",true);}catch(Exception){}
                }
                // X-93 (0.941): the shared discoveries, kept apart by kind, read only when a page asks for their kind (an unreadable file is set aside).
                discoveries.UseSharedSource(kind=>Discoveries.DiscoveryFiles.ReadSharedKind(saved,kind,(path,error)=>log.Warning(error,"Shared discoveries {0} could not be read; the file is kept as .broken.",Path.GetFileName(path))),
                    Discoveries.DiscoveryFiles.SharedKindsOnDisk(saved));
                mainWindow.DiscoveriesLoaded=true;
            });
            // 0.395 (user request): the wiki's spellbook table (one page for every spell) read when the Blue magic page opens, copy first.
            var blueIndex=new BlueSpellWikiIndex(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache,()=>configuration.CommunityWiki);
            registrations.Register(()=>{},blueIndex.Dispose);
            mainWindow.BlueSpellIndex=blueIndex;
            mainWindow.GameNames=new Items.GameNames(data,log);
            Dictionary<string,IReadOnlyList<uint>>? englishPlaces=null;
            mainWindow.EnglishPlaceIds=name=>(englishPlaces??=data.GetExcelSheet<Lumina.Excel.Sheets.PlaceName>(Dalamud.Game.ClientLanguage.English)
                .Where(row=>!row.Name.IsEmpty).GroupBy(row=>row.Name.ExtractText(),StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group=>group.Key,group=>(IReadOnlyList<uint>)group.Select(row=>row.RowId).ToArray(),StringComparer.OrdinalIgnoreCase)).GetValueOrDefault(name.Trim()) ?? [];
            Maps.MapLiveReader? mapLive=null;
            registrations.Register(()=>mapLive=new Maps.MapLiveReader(framework,client,objects,data,condition,fateTable,partyList,log),()=>mapLive?.Dispose());
            var mapLocal=new Maps.MapLocalIndexService();
            Maps.MapLocalIndexLoader? mapLocalLoader=null;
            registrations.Register(()=>mapLocalLoader=new Maps.MapLocalIndexLoader(mapLocal,data,framework,log,dataCache),()=>mapLocalLoader?.Dispose());
            mainWindow.ConfigureMapView(mapLive!,new Maps.MapMarkerSource(data,log),mapLocal);
            var itemSources=new Community.GarlandItemSourceService(garland){Enabled=()=>configuration.CommunityGarland};
            registrations.Register(()=>{},itemSources.Dispose);
            mainWindow.ConfigureItemSources(itemSources);
            var wikiItems=new Community.WikiItemAcquisitionService(id=>itemCatalog.State.Catalog?.Find(id)?.EnglishName,()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>configuration.CommunityWiki};
            registrations.Register(()=>{},wikiItems.Dispose);
            mainWindow.ConfigureWikiItems(wikiItems);
            var wikiVistas=new Maps.WikiVistaSource(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>configuration.CommunityWiki};
            registrations.Register(()=>{},wikiVistas.Dispose);
            mainWindow.ConfigureWikiVistas(wikiVistas);
            // W-04 (0.685): the island's animals on the wiki, on the player's click only.
            var islandAnimalBook=new Island.IslandAnimalSource(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>configuration.CommunityWiki};
            registrations.Register(()=>{},islandAnimalBook.Dispose);
            mainWindow.IslandAnimalBookSource=islandAnimalBook;
            Maps.NativeMapReplacer? mapReplacer=null;
            registrations.Register(()=>mapReplacer=new Maps.NativeMapReplacer(framework,gameGui,client,condition,()=>configuration.MapView,mainWindow.ShowMapFromGame,()=>mainWindow.MapViewOpen,()=>mainWindow.MapViewVisible(),mainWindow.CloseMapFromGame,log),()=>mapReplacer?.Dispose());
            mainWindow.ConfigureGameMapOpener(()=>mapReplacer?.OpenGameMap());
            Macros.MacroWriter? macroWriter=null;
            registrations.Register(()=>macroWriter=new Macros.MacroWriter(framework,log,outcome=>
            {
                var parts=outcome.Split(':');
                // 0.330 (audit): the exact slot list (« Done:first:last:book:list »); occupied slots are skipped, so « first to last » could
                // suggest they were overwritten.
                var text=parts[0]=="Done" ? localization.Format("CraftMacro.Written",parts[4].Replace(",",", ")):localization.Get("CraftMacro.Write"+parts[0]);
                if(parts[0]=="Done")chat.Print("[Tensho] "+text);else chat.PrintError("[Tensho] "+text);
            }),()=>macroWriter?.Dispose());
            mainWindow.ConfigureMacroWriter(macroWriter!);
            // R-12 (0.319): Discord webhook posts on their own worker; registered before the alarm service so it is released after it.
            var discord=new Alarms.DiscordNotifier();
            registrations.Register(()=>{},discord.Dispose);
            mainWindow.ConfigureDiscord(discord);
            // Reminders and vessel returns made before 0.319 have no source: recognized once by their group, for the Discord categories.
            var migrated=false;
            foreach(var alarm in configuration.Alarms ?? [])
                migrated|=Alarms.DiscordWebhook.AssignLegacySource(alarm,localization.Get("Reminders.Title"),localization.Get("FreeCompany.Title"));
            if(migrated)try{pluginInterface.SavePluginConfig(configuration);}catch(Exception error){log.Warning(error,"Alarm sources could not be saved.");}
            Alarms.AlarmService? alarms=null;
            registrations.Register(()=>{},()=>{chat.RemoveChatLinkHandler();alarmLinks.Clear();});
            registrations.Register(()=>alarms=new Alarms.AlarmService(framework,condition,log,configuration,()=>pluginInterface.SavePluginConfig(configuration),
                ()=>weather.State.Catalog,(alarm,line)=>
                {
                    // « label|minutes|local time » from the service, localized here.
                    var parts=line.Split('|');
                    if(parts.Length<3)return;
                    // R-12 (0.414): the game's map link to the alarm's gathering point, and « Open » (Tensho on the item), when known.
                    var message=new Dalamud.Game.Text.SeStringHandling.SeStringBuilder()
                        .AddText("[Tensho] "+localization.Format("Alarms.Ring",string.Join('|',parts[..^2]),parts[^2],parts[^1]));
                    if(mainWindow.AlarmPlace(alarm) is { } place)
                        message.AddText(" ").AddMapLink(place.TerritoryId,place.MapId,place.X,place.Y,0.05f);
                    // 0.774 (X-03, user report and Dalamud's console in verbose): the game never handed the « Open in Tensho » link to Dalamud; it took it for an
                    // item tooltip. The ring now carries the game's own item link: hovering shows the item, a right click opens its menu, where Tensho's entry is.
                    if(alarm.ItemId is > 0 and < uint.MaxValue)
                        // 0.780 (X-03, user report: the right click did nothing on the grey hint, which was not part of the link): the hint is inside the link, so the
                        // whole line opens the item's menu.
                        // 0.812 (X-03, user report 21:01: no menu at all): the game's own item link, its own name, as in 0.774 (validated at 14:29). The 0.780 link
                        // with another text was no longer taken by the game as an item (no menu opened: docs/evidence/0.812-alarm-link.md). The hint stays beside it.
                        message.AddText(" ").AddItemLink(alarm.ItemId,false).AddUiForeground(" "+localization.Get("Alarms.RightClickTensho"),3);
                    chat.Print(message.Build());
                },alarm=>
                {
                    // An item alarm without its job (fish forecast, alarms before 0.203) takes it from the gathering catalogue, as the
                    // Alarms page sorts it, so the Discord category matches; stored on the alarm, saved with its next change.
                    if(alarm.JobId==0 && alarm.ItemId!=0)alarm.JobId=Alarms.AlarmSchedule.JobOfAlarm(alarm,item=>gatheringCatalog.State.Catalog?.ForItem(item));
                    return Alarms.DiscordWebhook.ShouldSend(configuration.Discord,alarm);
                },(alarm,minutes,occurrence)=>
                {
                    // Framework thread: the character's name is read here, only when the player allowed it.
                    var settings=configuration.Discord;
                    var character=settings.IncludeCharacter ? objects.LocalPlayer?.Name.TextValue:null;
                    var content=Alarms.DiscordWebhook.Format(settings,localization.Get("Discord.DefaultTemplate"),alarm.DiscordMessage,alarm.Label,minutes,occurrence.ToLocalTime(),character,mainWindow.AlarmZoneName(alarm.TerritoryId),mainWindow.AlarmItemName(alarm.ItemId));
                    discord.Enqueue(new(Alarms.DiscordWebhook.UrlFor(settings,Alarms.DiscordWebhook.SourceOf(alarm)),Alarms.DiscordWebhook.Payload(settings,content)));
                }),()=>alarms?.Dispose());
            mainWindow.ConfigureAlarms(alarms!);
            if(alarms is not null)alarms.Gathered=mainWindow.AlarmGathered;
            var gatherBuddyFish=new Community.GatherBuddyFishService(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>configuration.CommunityGatherBuddy};
            registrations.Register(()=>{},gatherBuddyFish.Dispose);
            mainWindow.ConfigureGatherBuddyFish(gatherBuddyFish);
            var biteTimes=new Community.FishBiteTimeService(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>configuration.CommunityTeamcraftBites};
            registrations.Register(()=>{},biteTimes.Dispose);
            mainWindow.ConfigureBiteTimes(biteTimes);
            // Fishing help (0.226): the FishingSpot of the player's territory nearest to the player. FishingSpot X/Z are map pixels
            // (0–2048, probe PROBE_FISHSPOT: spot 35 X 668 Z 1120 at scale 200 = map 7.7, 12.2 as Garland): world = (pixel − 1024) / scale − offset.
            // g, R-05 (0.402): fishing, spearfishing and gathering notebooks, read while the Gathering page shows them.
            Gathering.NotebookReader? notebooks=null;
            registrations.Register(()=>notebooks=new Gathering.NotebookReader(framework,client,data,log),()=>notebooks?.Dispose());
            mainWindow.NotebookSource=notebooks;
            mainWindow.NotebookExportFolder=()=>Path.Combine(pluginInterface.GetPluginConfigDirectory(),"Exports");
            // g, R-04 (0.398): the bait in hand for the fishing help.
            Gathering.FishingBaitReader? baitReader=null;
            registrations.Register(()=>baitReader=new Gathering.FishingBaitReader(framework,objects,log),()=>baitReader?.Dispose());
            mainWindow.FishingBait=()=>baitReader?.Bait;
            mainWindow.FishingIntuitionLeft=()=>baitReader?.IntuitionLeft;
            mainWindow.FishingCastAt=()=>baitReader?.CastAt;
            // 0.604 (C35): the player copied on the framework thread, again only when it changed; the two tables built off the frame on first use, nothing
            // shown until they are ready, a failure logged once.
            System.Runtime.CompilerServices.StrongBox<(uint Job,uint Territory,float X,float Z,bool Fishing)?> fishingNowCopy=new(null);
            IFramework.OnUpdateDelegate copyFishingNow=_=>
            {
                var local=objects.LocalPlayer;
                (uint Job,uint Territory,float X,float Z,bool Fishing)? seen=local is null ? null
                    :(local.ClassJob.RowId,(uint)client.TerritoryType,local.Position.X,local.Position.Z,condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Fishing]);
                if(seen!=Volatile.Read(ref fishingNowCopy).Value)Volatile.Write(ref fishingNowCopy,new(seen));
            };
            registrations.Register(()=>TenshoAssistant.Competitive.CompetitiveGate.Add(framework,copyFishingNow),()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,copyFishingNow));
            Task<Dictionary<uint,List<(uint Id,string Name,float X,float Z,uint[] Fish)>>>? fishingSpotsTask=null;
            mainWindow.ConfigureFishingHelp(()=>Volatile.Read(ref fishingNowCopy).Value,(territory,x,z)=>
            {
                fishingSpotsTask??=Task.Run(()=>
                {
                    var spots=new Dictionary<uint,List<(uint Id,string Name,float X,float Z,uint[] Fish)>>();
                    try
                    {
                        foreach(var spot in data.GetExcelSheet<Lumina.Excel.Sheets.FishingSpot>())
                        {
                            if(spot.TerritoryType.RowId==0 || !spot.TerritoryType.IsValid)continue;
                            var map=spot.TerritoryType.Value.Map.Value;var scale=Math.Max(1,(int)map.SizeFactor)/100f;
                            var fish=spot.Item.Where(item=>item.RowId!=0).Select(item=>item.RowId).ToArray();
                            if(fish.Length==0)continue;
                            if(!spots.TryGetValue(spot.TerritoryType.RowId,out var list))spots[spot.TerritoryType.RowId]=list=[];
                            list.Add((spot.RowId,spot.PlaceName.IsValid ? spot.PlaceName.Value.Name.ExtractText():"",(spot.X-1024)/scale-map.OffsetX,(spot.Z-1024)/scale-map.OffsetY,fish));
                        }
                    }
                    catch(Exception error){log.Warning(error,"Fishing spots unavailable (perhaps after a game update).");}
                    return spots;
                });
                if(!fishingSpotsTask.IsCompletedSuccessfully)return null;
                return fishingSpotsTask.Result.TryGetValue(territory,out var candidates) && candidates.Count>0
                    ? candidates.OrderBy(spot=>(spot.X-x)*(spot.X-x)+(spot.Z-z)*(spot.Z-z)).First():null;
            });
            // Bountiful II base of a gathered item (0.224): ItemLevel.Gathering at its GatheringItemLevelConvertTable row, highest of its
            // gathering rows; 0.604: built off the frame on first use (docs/evidence/0.224-bountiful-base.md).
            Task<Dictionary<uint,int>>? bountifulBases=null;
            mainWindow.ConfigureBountifulBase(item=>
            {
                bountifulBases??=Task.Run(()=>
                {
                    var bases=new Dictionary<uint,int>();
                    try
                    {
                        var levels=data.GetExcelSheet<Lumina.Excel.Sheets.ItemLevel>();
                        foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.GatheringItem>())
                            if(row.Item.RowId!=0 && levels.TryGetRow(row.GatheringItemLevel.RowId,out var level) && level.Gathering>0)
                                bases[row.Item.RowId]=Math.Max(bases.GetValueOrDefault(row.Item.RowId),(int)level.Gathering);
                    }
                    catch(Exception error){log.Warning(error,"Bountiful bases unavailable (perhaps after a game update).");}
                    return bases;
                });
                return bountifulBases.IsCompletedSuccessfully && bountifulBases.Result.TryGetValue(item,out var value) ? value:null;
            });
            // 0.515: a node's effects, read once per node (docs/evidence/0.515-node-effects.md). R-14: an unreadable sheet means no effect line, not a broken panel.
            var nodeEffects=new Dictionary<uint,IReadOnlyList<Gathering.GatherNodeEffect>>();
            mainWindow.ConfigureNodeEffects(node=>
            {
                if(nodeEffects.TryGetValue(node,out var known))return known;
                var found=new List<Gathering.GatherNodeEffect>();
                try
                {
                    var bonuses=data.GetExcelSheet<Lumina.Excel.Sheets.GatheringPointBonus>();
                    if(data.GetExcelSheet<Lumina.Excel.Sheets.GatheringPoint>().TryGetRow(node,out var point))
                        foreach(var link in point.GatheringPointBonus)
                            if(link.RowId!=0 && bonuses.TryGetRow(link.RowId,out var bonus) && Gathering.GatherNodeEffects.From(bonus.Condition.RowId,bonus.ConditionValue,bonus.Condition.ValueNullable?.Text.ExtractText(),bonus.BonusType.RowId,bonus.BonusValue,bonus.BonusType.ValueNullable?.Text.ExtractText()) is { } effect)
                                found.Add(effect);
                }
                catch(Exception error){log.Warning(error,"Node effects unavailable (perhaps after a game update).");}
                nodeEffects[node]=found;
                return found;
            });
            mainWindow.ConfigureJobNames(job=>data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().TryGetRow(job,out var row) ? row.Name.ExtractText():null);
            var lumina=new Community.LuminaSupplementalService(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>().TryGetRow(id,out var monster) ? monster.Singular.ExtractText():null,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.Fate>().TryGetRow(id,out var fate) ? fate.Name.ExtractText():null,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.ENpcResident>().TryGetRow(id,out var npc) ? npc.Singular.ExtractText():null,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.Quest>().TryGetRow(id,out var quest) ? quest.Name.ExtractText():null){Enabled=()=>configuration.CommunityLumina};
            registrations.Register(()=>{},lumina.Dispose);
            mainWindow.ConfigureLumina(lumina);
            var dutyFinder=new Duties.DutyFinderService();
            Duties.DutyFinderReader? dutyFinderReader=null;
            registrations.Register(()=>dutyFinderReader=new Duties.DutyFinderReader(dutyFinder,framework,gameGui,log,()=>mainWindow.DeveloperMode),()=>dutyFinderReader?.Dispose());
            var dutyFinderLoot=new DutyLootService(garland);registrations.Register(()=>{},dutyFinderLoot.Dispose);
            mainWindow.ConfigureDutyFinder(dutyFinder,dutyFinderLoot);
            var triadNpcs=new TriadNpcService();
            TriadNpcLoader? triadNpcLoader=null;
            registrations.Register(()=>triadNpcLoader=new TriadNpcLoader(triadNpcs,data,framework,client,player,unlocks,log),()=>triadNpcLoader?.Dispose());
            mainWindow.ConfigureTriadNpcs(triadNpcs);
            // 0.500 (user request): the Beastmaster page.
            var beasts=new Beastmaster.BeastService();
            Beastmaster.BeastCatalogLoader? beastLoader=null;
            registrations.Register(()=>beastLoader=new Beastmaster.BeastCatalogLoader(beasts,data,framework,client,player,log),()=>beastLoader?.Dispose());
            mainWindow.Beasts=beasts;
            // X-73.9 (0.863): spells learned and beasts tamed, put down to their monster, for the discoveries base.
            if(itemWatcher is not null){itemWatcher.Beasts=()=>beasts.State.Obtained;itemWatcher.WatchUnlocks(unlocks);itemWatcher.Vistas=()=>mapLive?.State?.Sightseen;
                var blueActions=data.GetExcelSheet<Lumina.Excel.Sheets.AozAction>().Where(row=>row.Action.RowId!=0).GroupBy(row=>row.Action.RowId).ToDictionary(group=>group.Key,group=>group.First().RowId);
                itemWatcher.BlueSpellOfAction=action=>blueActions.GetValueOrDefault(action);
                // X-96 (0.959): the check uses the standard table even where the zone has a story weather, and records whether the game agrees there.
                itemWatcher.ForecastWeather=(territory,at)=>weather.State.Catalog?.Forecast(territory,at,1,true) is { Count:>0 } current ? current[0].Weather?.Id ?? 0:0;
                itemWatcher.WeatherChecked=(territory,matched)=>
                {
                    if(weather.State.Catalog?.Zone(territory) is not { Individual:true })return;
                    var zones=configuration.WeatherStoryZones ??= [];
                    if(matched==zones.Contains(territory))return;
                    if(matched)zones.Add(territory);else zones.Remove(territory);
                    try{pluginInterface.SavePluginConfig(configuration);}catch(Exception error){log.Warning(error,"The confirmed story weather zones could not be saved.");}
                };
                Gathering.WeatherCatalog.StoryWeatherConfirmed=territory=>configuration.WeatherStoryZones?.Contains(territory)==true;
                itemWatcher.PreviousWeather=(territory,at)=>weather.State.Catalog?.Forecast(territory,at.AddSeconds(-1400),1) is { Count:>0 } before ? before[0].Weather?.Id ?? 0:0;}
            mainWindow.BeastName=id=>beasts.State.Catalog?.Beasts.FirstOrDefault(beast=>beast.Id==id)?.Name ?? "";
            mainWindow.SpellName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.AozAction>().GetRowOrDefault(id) is { } spell ? data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(spell.Action.RowId)?.Name.ExtractText() ?? "":"";
            mainWindow.GuideNewInstall=newInstall;
            // 0.508 (I-05): the retainer sale list. Read only while it is open; Universalis on the panel's button only.
            Market.RetainerSalesReader? retainerSales=null;
            registrations.Register(()=>retainerSales=new Market.RetainerSalesReader(framework,gameGui,client,player,log,()=>configuration.RetainerSalesPanel,()=>configuration.DeveloperMode),()=>retainerSales?.Dispose());
            mainWindow.RetainerSalesSource=()=>retainerSales?.State ?? new(Market.RetainerSalesStatus.Closed,[],new HashSet<string>(),"",false);
            Market.RetainerCompareService? retainerCompare=null;
            registrations.Register(()=>mainWindow.RetainerCompare=retainerCompare=new Market.RetainerCompareService{Enabled=()=>configuration.CommunityUniversalis},()=>retainerCompare?.Dispose());
            Help.CommunityHelpIndex? communityHelp=null;
            registrations.Register(()=>mainWindow.CommunityHelp=communityHelp=new Help.CommunityHelpIndex(),()=>communityHelp?.Dispose());
            registrations.Register(()=>boardReader=new Beastmaster.BeastBoardReader(framework,gameGui,log,()=>mainWindow.Beasts?.State.Catalog?.Beasts,mainWindow.BeastRanksRead),()=>boardReader?.Dispose());
            registrations.Register(()=>{boardWindow=new BeastBoardWindow(mainWindow){Windows=()=>boardReader?.State ?? default};boardWindows.AddWindow(boardWindow);},()=>{if(boardWindow is not null)boardWindows.RemoveWindow(boardWindow);});
            showBoardPanel=()=>configuration.ShowBeastBoardPanel;
            mainWindow.BeastBoardOfTerritory=territory=>mainWindow.Beasts?.State.Catalog?.Boards.FirstOrDefault(board=>data.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>().GetRowOrDefault(board.Content)?.TerritoryType.RowId==territory)?.Id ?? 0;
            Beastmaster.BeastWindowProbe? beastProbe=null;
            registrations.Register(()=>beastProbe=new Beastmaster.BeastWindowProbe(framework,gameGui,log,()=>configuration.DeveloperMode),()=>beastProbe?.Dispose());
            // 0.494 (user request): the self-check, on its own in developer mode, on request otherwise.
            registrations.Register(()=>selfCheck=new Diagnostics.SelfCheckRunner(framework,client,player,log,()=>configuration.DeveloperMode,mainWindow.SelfCheckReady,mainWindow.PrepareSelfCheck,mainWindow.SelfCheckProbes,mainWindow.SelfCheckWindows,
                Path.Combine(pluginInterface.GetPluginConfigDirectory(),"SelfCheckSeen.json"),string.Join(";",data.GameData.Repositories.Values.OrderBy(repository=>repository.Name).Select(repository=>$"{repository.Name}:{repository.Version}"))),()=>selfCheck?.Dispose());
            mainWindow.SelfCheck=selfCheck;
            mainWindow.ConfigureMarketBoard(marketBoard);
            Alarms.AlarmDtrEntry? alarmBar=null;
            registrations.Register(()=>alarmBar=new Alarms.AlarmDtrEntry(dtrBar,framework,gameGui,configuration,localization.Get,mainWindow.OpenAlarms),()=>alarmBar?.Dispose());
            // 0.455 (user request): reminders apart from alarms in the server bar.
            Alarms.ReminderDtrEntry? reminderBar=null;
            registrations.Register(()=>reminderBar=new Alarms.ReminderDtrEntry(dtrBar,framework,gameGui,player,log,configuration,localization.Get,mainWindow.OpenReminders,mainWindow.ReminderTodoLines,mainWindow.TickRemindersFromGame),()=>reminderBar?.Dispose());
            this.reminderBar=()=>reminderBar;
            // 0.344 (g, I-02): currency balances and threshold alerts (chat and sound with the alarm options, Discord in its own category),
            // and their server bar entry. Registered after the Discord notifier, so released before it.
            Inventory.CurrencyWatcher? currencies=null;
            // Framework thread. One chat line and one sound for one alert; silence in duties as for the alarms.
            void CurrencyNotice(string message)
            {
                var options=configuration.AlarmOptions ?? new();
                var inDuty=options.MuteInDuties && condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
                if(options.ChatAllowed(inDuty))chat.Print("[Tensho] "+message);
                if(options.SoundAllowed(inDuty))Alarms.AlarmService.PlaySound(options.Sound,log);
                var settings=configuration.Discord;
                if(Alarms.DiscordWebhook.ShouldSend(settings,Alarms.AlarmSource.Currency))
                    try
                    {
                        var character=settings.IncludeCharacter ? objects.LocalPlayer?.Name.TextValue:null;
                        var content=Alarms.DiscordWebhook.Format(settings,message,message,message,0,DateTimeOffset.Now,character);
                        discord.Enqueue(new(Alarms.DiscordWebhook.UrlFor(settings,Alarms.AlarmSource.Currency),Alarms.DiscordWebhook.Payload(settings,content)));
                    }
                    catch(Exception error){log.Warning("Currency alert could not be queued for Discord ({Error}).",error.GetType().Name);}
            }
            registrations.Register(()=>currencies=new Inventory.CurrencyWatcher(framework,client,player,data,configuration,log)
            {
                Crossed=crossed=>CurrencyNotice(localization.Format("Currencies.Alert",string.Join(" · ",crossed.Select(Alarms.CurrencyDtrEntry.Line)))),
                // 0.606: the weekly limit of the limited tomestones reached.
                WeeklyCapReached=(got,limit)=>CurrencyNotice(localization.Format("Currencies.WeeklyCapAlert",got.ToString("N0"),limit.ToString("N0"))),
            },()=>currencies?.Dispose());
            mainWindow.Currencies=()=>currencies;
            // DEC-038: one chat line on entry, nothing on leaving; what waits for Discord does not leave during the cut.
            TenshoAssistant.Competitive.CompetitiveGate.Changed+=isCut=>
            {
                if(!isCut)return;
                discord.Clear();
                chat.Print("[Tensho] "+localization.Get("Competitive.Off"));
            };
            // 0.366 (g, C-10): duty ready (IClientState.CfPop, framework thread): chat line and sound with the alarm options, Discord in its
            // own category; nothing when the alert is off. Unsubscribed on dispose.
            void OnDutyPop(Lumina.Excel.Sheets.ContentFinderCondition duty)
            {
                try
                {
                    var options=configuration.AlarmOptions ?? new();
                    if(!options.DutyPopAlert)return;
                    var inDuty=options.MuteInDuties && condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
                    var name=duty.Name.ExtractText();
                    var message=localization.Format("DutyPop.Message",name.Length>0 ? name:localization.Get("DutyPop.Roulette"));
                    if(options.ChatAllowed(inDuty))chat.Print("[Tensho] "+message);
                    if(options.SoundAllowed(inDuty))Alarms.AlarmService.PlaySound(options.Sound,log);
                    var settings=configuration.Discord;
                    if(Alarms.DiscordWebhook.ShouldSend(settings,Alarms.AlarmSource.DutyPop))
                    {
                        var character=settings.IncludeCharacter ? objects.LocalPlayer?.Name.TextValue:null;
                        var content=Alarms.DiscordWebhook.Format(settings,message,message,message,0,DateTimeOffset.Now,character);
                        discord.Enqueue(new(Alarms.DiscordWebhook.UrlFor(settings,Alarms.AlarmSource.DutyPop),Alarms.DiscordWebhook.Payload(settings,content)));
                    }
                }
                catch(Exception error){log.Warning("Duty ready alert failed ({Error}).",error.GetType().Name);}
            }
            registrations.Register(()=>client.CfPop+=OnDutyPop,()=>client.CfPop-=OnDutyPop);
            Alarms.CurrencyDtrEntry? currencyBar=null;
            registrations.Register(()=>currencyBar=new Alarms.CurrencyDtrEntry(dtrBar,framework,configuration,()=>currencies?.State,localization.Get,mainWindow.OpenCurrencies),()=>currencyBar?.Dispose());
            this.alarmBar=()=>alarmBar;
            FreeCompany.VoyageWatcher? voyages=null;
            // g, M-10 (0.379): voyage loot history, one local file (VoyageHistory.json), 180 days.
            var voyageHistory=new FreeCompany.VoyageHistoryStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"VoyageHistory.json"),(error,message)=>log.Warning(error,message));
            registrations.Register(()=>voyages=new FreeCompany.VoyageWatcher(framework,data,configuration,()=>pluginInterface.SavePluginConfig(configuration),log){Observed=voyageHistory.Observe,
                // X-73.8 (0.862): each submersible voyage's loot by sector and the sectors it unlocked, for the discoveries base.
                AirshipLoot=(item,sector,count)=>{if(configuration.Discovery.Enabled && configuration.Discovery.ItemSources)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.AirshipDrop,item%1_000_000,sector,count,DateTimeOffset.UtcNow,hq:item>=1_000_000);},
                SectorLoot=(item,sector,count,hq)=>{if(configuration.Discovery.Enabled && configuration.Discovery.ItemSources)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.VoyageDrop,item,sector,count,DateTimeOffset.UtcNow,hq:hq);},
                SectorVisit=(sector,twice,surveillance)=>{if(configuration.Discovery.Enabled && configuration.Discovery.ItemSources)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.VoyageVisit,sector,twice ? 1u:0u,surveillance,DateTimeOffset.UtcNow);},
                SectorUnlock=(unlocked,sector)=>{if(configuration.Discovery.Enabled && configuration.Discovery.ItemSources)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.VoyageUnlock,unlocked,sector,1,DateTimeOffset.UtcNow);}},()=>voyages?.Dispose());
            mainWindow.VoyageHistorySource=voyageHistory;
            mainWindow.SubmarineCatalog=()=>voyages?.Catalog;
            mainWindow.VesselParts=()=>voyages?.PartCatalog;
            mainWindow.SubmarineCatalogFailed=()=>voyages?.CatalogFailed==true;
            mainWindow.VesselPartsFailed=()=>voyages?.PartCatalogFailed==true;
            mainWindow.AirshipCatalog=()=>voyages?.AirshipCatalog;
            mainWindow.AirshipCatalogFailed=()=>voyages?.AirshipCatalogFailed==true;
            // 0.337 (DEC-023 3): airship leg times and distances from the game's own functions, on the framework thread, on request.
            FreeCompany.AirshipVoyageTable? airshipTable=null;
            registrations.Register(()=>airshipTable=new FreeCompany.AirshipVoyageTable(framework,client,log),()=>airshipTable?.Dispose());
            mainWindow.AirshipTable=()=>airshipTable?.State;
            // DEC-043 (0.1006): developer-mode probe of the game's submarine functions against the formula, once per session (log only).
            FreeCompany.SubmarineVoyageProbe? submarineProbe=null;
            registrations.Register(()=>submarineProbe=new FreeCompany.SubmarineVoyageProbe(framework,client,data,log,()=>configuration.DeveloperMode),()=>submarineProbe?.Dispose());
            mainWindow.RequestAirshipTable=(speed,points)=>airshipTable?.Request(speed,points);
            mainWindow.VoyageReadUnavailable=()=>voyages?.ReadUnavailable==true;
            Gear.GearLoader? gear=null;
            registrations.Register(()=>gear=new Gear.GearLoader(framework,data,client,log),()=>gear?.Dispose());
            // g, M-12 (0.383): the game's gear sets and the job levels (ClassJob ExpArrayIndex), read while the Gear page shows them.
            // R-14 (0.452): a sheet read here that no longer loads threw out of this constructor and Dalamud refused the whole plugin; it now
            // turns its own feature off (logged).
            var expIndex=LoadSheet("ClassJob",()=>data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().Where(row=>row.ExpArrayIndex>=0 && !row.Name.IsEmpty).ToDictionary(row=>row.RowId,row=>(int)row.ExpArrayIndex),[]);
            // T21 (0.646): the game's order of the jobs and the experience of each level.
            var jobOrder=LoadSheet("ClassJob",()=>data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().ToDictionary(row=>row.RowId,row=>(int)row.UIPriority),[]);
            var expToNext=LoadSheet("ParamGrow",()=>data.GetExcelSheet<Lumina.Excel.Sheets.ParamGrow>().ToDictionary(row=>(int)row.RowId,row=>row.ExpToNext),[]);
            Gear.GameGearsetReader? gameGearsets=null;
            registrations.Register(()=>gameGearsets=new Gear.GameGearsetReader(framework,client,log,expIndex,jobOrder,expToNext),()=>gameGearsets?.Dispose());
            mainWindow.GearsetSource=gameGearsets;
            // 0.650 (phase 5): the in-game readings, in developer mode only. Temporary.
            Diagnostics.ReadingCapture? readingCapture=null;
            registrations.Register(()=>readingCapture=new Diagnostics.ReadingCapture(framework,objects,condition,gameGui,chat,()=>configuration.DeveloperMode,log),()=>readingCapture?.Dispose());
            // g, X-06 (0.391): gatherer GP regeneration tick (on-screen bar, off by default).
            Gathering.RegenTickReader? regenTick=null;
            // g, R-08 (0.403): GP alert at the threshold (chat line and sound with the alarm options), once per rise.
            registrations.Register(()=>regenTick=new Gathering.RegenTickReader(framework,objects,condition,()=>configuration.GpTickBar || configuration.GatheringPanel,log)
            {
                Alert=()=>configuration.GpAlert ? configuration.GpAlertThreshold:null,
                Reached=(gp,max)=>
                {
                    log.Information("GP alert: {0} / {1}.",gp,max);
                    var options=configuration.AlarmOptions ?? new();
                    var inDuty=options.MuteInDuties && condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
                    if(options.ChatAllowed(inDuty))chat.Print("[Tensho] "+localization.Format("GpTick.Alert",gp,max));
                    if(options.SoundAllowed(inDuty))Alarms.AlarmService.PlaySound(options.Sound,log);
                },
            },()=>regenTick?.Dispose());
            mainWindow.RegenTick=()=>regenTick?.State;
            // g, R-01 (0.392): ocean fishing timetable from the game's loop (IKDRouteTable) and voyages (IKDRoute), built on first use.
            var ocean=new Lazy<Gathering.OceanSchedule?>(()=>
            {
                try
                {
                    var routes=data.GetExcelSheet<Lumina.Excel.Sheets.IKDRoute>().Where(row=>!row.Name.IsEmpty).ToDictionary(row=>row.RowId,row=>new Gathering.OceanRoute(row.RowId,row.Name.ExtractText(),
                        row.Spot.Zip(row.Time).Where(pair=>pair.First.RowId!=0)
                            .Select(pair=>new Gathering.OceanStop(pair.First.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() ?? "",Gathering.OceanSchedule.TimeOf(pair.Second.RowId))).ToArray()));
                    var loop=data.GetExcelSheet<Lumina.Excel.Sheets.IKDRouteTable>().OrderBy(row=>row.RowId).Select(row=>(row.Route.RowId,row.Unknown0)).ToArray();
                    return loop.Length>0 ? new Gathering.OceanSchedule(loop,routes):null;
                }
                catch(Exception error){log.Warning(error,"Ocean fishing timetable unavailable.");return null;}
            });
            mainWindow.OceanTimetable=()=>ocean.Value;
            // g, R-07 (0.418): the voyage in progress and the game's ocean fishing data (routes, places, fish of each spot, missions).
            var oceanVoyageData=new Lazy<Gathering.OceanVoyageData?>(()=>
            {
                try
                {
                    var routes=data.GetExcelSheet<Lumina.Excel.Sheets.IKDRoute>().Where(row=>!row.Name.IsEmpty)
                        .ToDictionary(row=>row.RowId,row=>(row.Spot.Select(spot=>spot.RowId).Where(id=>id!=0).ToArray(),row.Time.Select(time=>time.RowId).ToArray()));
                    var spots=data.GetExcelSheet<Lumina.Excel.Sheets.IKDSpot>().Where(row=>row.PlaceName.RowId!=0)
                        .ToDictionary(row=>row.RowId,row=>new Gathering.OceanSpot(row.RowId,row.PlaceName.ValueNullable?.Name.ExtractText() ?? "",row.SpotMain.RowId,row.SpotSub.RowId));
                    var fishingSpots=data.GetExcelSheet<Lumina.Excel.Sheets.FishingSpot>();
                    var spotFish=new Dictionary<uint,IReadOnlyList<uint>>();var territories=new HashSet<uint>();
                    foreach(var id in spots.Values.SelectMany(spot=>new[]{spot.MainSpot,spot.SpectralSpot}).Where(id=>id!=0).Distinct())
                        if(fishingSpots.GetRowOrDefault(id) is { } fishingSpot)
                        {
                            spotFish[id]=fishingSpot.Item.Select(item=>item.RowId).Where(item=>item!=0).ToArray();
                            if(fishingSpot.TerritoryType.RowId!=0)territories.Add(fishingSpot.TerritoryType.RowId);
                        }
                    // X-85 V3 (0.961): each mission's criterion from the English texts (species words matched to the bonus rows), each fish's species.
                    var words=Gathering.OceanMissionRules.SpeciesWords(data.GetExcelSheet<Lumina.Excel.Sheets.IKDContentBonus>(Dalamud.Game.ClientLanguage.English)
                        .Select(row=>(row.RowId,row.Requirement.ExtractText())));
                    var englishMissions=data.GetExcelSheet<Lumina.Excel.Sheets.IKDPlayerMissionCondition>(Dalamud.Game.ClientLanguage.English);
                    var missions=data.GetExcelSheet<Lumina.Excel.Sheets.IKDPlayerMissionCondition>().Where(row=>!row.Unknown0.IsEmpty)
                        .ToDictionary(row=>row.RowId,row=>new Gathering.OceanMission(row.Unknown0.ExtractText(),row.Unknown1,
                            englishMissions.GetRowOrDefault(row.RowId) is { } english ? Gathering.OceanMissionRules.Parse(english.Unknown0.ExtractText(),words):null));
                    var fishBonuses=new Dictionary<uint,uint[]>();
                    foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.IKDFishParam>())
                        if(row.Fish.ValueNullable?.Item.RowId is { } item and not 0)
                            fishBonuses[item]=row.IKDContentBonus.Select(bonus=>bonus.RowId).Where(id=>id!=0).ToArray();
                    return new Gathering.OceanVoyageData(routes,spots,spotFish,missions){Territories=territories,FishBonuses=fishBonuses};
                }
                catch(Exception error){log.Warning(error,"Ocean fishing voyage data unavailable.");return null;}
            });
            mainWindow.OceanVoyageData=()=>oceanVoyageData.Value;
            OceanVoyageReader? oceanVoyageReader=null;
            registrations.Register(()=>mainWindow.OceanVoyage=oceanVoyageReader=new OceanVoyageReader(framework,client,log){Developer=()=>configuration.DeveloperMode,
                CatchPoints=(item,points)=>{if(configuration.Discovery.Enabled && configuration.Discovery.Fishing)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.FishPoints,item,0,Math.Min(points,100_000),DateTimeOffset.UtcNow,client.TerritoryType);}},()=>oceanVoyageReader?.Dispose());
            // 0.388 (user request): position of the game's inventory window, for the sort button drawn above it.
            Inventory.InventoryWindowReader? inventoryWindow=null;
            registrations.Register(()=>inventoryWindow=new Inventory.InventoryWindowReader(framework,gameGui,()=>configuration.InventorySortButton,log),()=>inventoryWindow?.Dispose());
            mainWindow.InventoryWindow=inventoryWindow;
            // g, M-06 (0.376): repair and materia extraction alert (chat and sound with the alarm options), once per crossing.
            Gear.GearWearWatcher? gearWear=null;
            registrations.Register(()=>gearWear=new Gear.GearWearWatcher(framework,client,()=>gear?.Equipped ?? [],()=>configuration.GearWear ?? new(),log){Crossed=(repair,extract,state)=>
            {
                var options=configuration.AlarmOptions ?? new();
                var inDuty=options.MuteInDuties && condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
                if(options.ChatAllowed(inDuty))
                {
                    if(repair)chat.Print("[Tensho] "+localization.Format("GearWear.AlertRepair",state.ToRepair,state.LowestCondition ?? 0));
                    if(extract)chat.Print("[Tensho] "+localization.Format("GearWear.AlertExtract",state.ToExtract));
                }
                if(options.SoundAllowed(inDuty))Alarms.AlarmService.PlaySound(options.Sound,log);
            }},()=>gearWear?.Dispose());
            mainWindow.GearCatalog=()=>gear?.Catalog;
            mainWindow.GearCatalogFailed=()=>gear?.CatalogFailed==true;
            mainWindow.EquippedGear=()=>gear?.Equipped ?? [];
            // 0.340 (R-11 step 4): the companion chocobo and its bardings, read on the framework thread while the page shows them.
            Gear.ChocoboReader? chocobo=null;
            registrations.Register(()=>chocobo=new Gear.ChocoboReader(framework,client,player,unlocks,data,log),()=>chocobo?.Dispose());
            mainWindow.Chocobo=()=>{chocobo?.Want();return chocobo?.State;};
            mainWindow.ChocoboUnavailable=()=>chocobo?.Unavailable==true;
            mainWindow.StainName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Stain>().GetRowOrDefault(id)?.Name.ExtractText();
            // 0.342 (R-09): running festivals (GameMain), their quests (Quest.Festival) and English names (LuminaSupplemental, embedded).
            Events.FestivalReader? festivals=null;
            registrations.Register(()=>festivals=new Events.FestivalReader(framework,client,data,log),()=>festivals?.Dispose());
            mainWindow.Festivals=()=>festivals;
            // 0.348 (g, I-10): gear sets read on request for the storage report; stack sizes from the game data.
            Inventory.GearsetReader? gearsets=null;
            registrations.Register(()=>gearsets=new Inventory.GearsetReader(framework,player,log),()=>gearsets?.Dispose());
            mainWindow.Gearsets=()=>gearsets;
            // 0.353 (g, I-12): key items (EventItem) by name, from the game data, the list built once.
            (uint Id,string Name,uint Icon)[]? keyItems=null;
            mainWindow.KeyItemSearch=text=>
            {
                var wanted=text.Trim();
                if(wanted.Length==0)return [];
                keyItems??=data.GetExcelSheet<Lumina.Excel.Sheets.EventItem>().Where(row=>!row.Name.IsEmpty).Select(row=>(row.RowId,row.Name.ExtractText(),(uint)row.Icon)).ToArray();
                return keyItems.Where(item=>item.Name.Contains(wanted,StringComparison.CurrentCultureIgnoreCase)).Take(30).ToArray();
            };
            // 0.352 (g, I-11): names of the quests and achievements that unlock shop articles.
            mainWindow.QuestNameOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Quest>().GetRowOrDefault(id)?.Name.ExtractText();
            // 0.911 (X-76): a key item's name (EventItem row), for the hand-ins seen in game.
            mainWindow.KeyItemNameOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.EventItem>().GetRowOrDefault(id)?.Name.ExtractText();
            mainWindow.AchievementNameOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Achievement>().GetRowOrDefault(id)?.Name.ExtractText();
            // 0.351 (g, I-04): gil journal, one file per character (GilJournal/<id>.json), kept 90 days.
            Inventory.GilWatcher? gilWatcher=null;
            registrations.Register(()=>gilWatcher=new Inventory.GilWatcher(framework,player,gameGui,Path.Combine(pluginInterface.GetPluginConfigDirectory(),"GilJournal"),log,
                ()=>mainWindow.GameWindowSource?.Invoke().Select(window=>window.Name).ToArray() ?? [],()=>mainWindow.DeveloperMode),()=>gilWatcher?.Dispose());
            mainWindow.GilJournalState=()=>gilWatcher?.State;
            Inventory.GilDtrEntry? gilBar=null;
            registrations.Register(()=>gilBar=new Inventory.GilDtrEntry(dtrBar,framework,()=>configuration.GilServerBar,()=>gilWatcher?.State?.Entries,localization.Get,mainWindow.OpenGilJournal),()=>gilBar?.Dispose());
            mainWindow.StackSizeOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(id)?.StackSize ?? 0;
            // 0.347 (g, I-08): at an NPC shop, the bags' items labelled « to sell » and « important » (chat; sound if turned on).
            Items.ItemNoteShopReminder? shopReminder=null;
            registrations.Register(()=>shopReminder=new Items.ItemNoteShopReminder(framework,gameGui,configuration,()=>inventoryService.Current,()=>player.IsLoaded ? player.ContentId:0,
                (toSell,important)=>
                {
                    var items=data.GetExcelSheet<Lumina.Excel.Sheets.Item>();
                    string Names(IReadOnlyList<(uint Item,long Quantity)> list)=>string.Join(", ",list.Take(10).Select(entry=>$"{items.GetRowOrDefault(entry.Item)?.Name.ExtractText() ?? $"#{entry.Item}"} ×{entry.Quantity}"))
                        +(list.Count>10 ? " …":"");
                    if(toSell.Count>0)chat.Print("[Tensho] "+localization.Format("ItemNotes.ShopToSell",Names(toSell)));
                    if(important.Count>0)
                    {
                        chat.Print("[Tensho] "+localization.Format("ItemNotes.ShopImportant",Names(important)));
                        if(configuration.ItemNoteImportantSound)Alarms.AlarmService.PlaySound((configuration.AlarmOptions ?? new()).Sound,log);
                    }
                },log),()=>shopReminder?.Dispose());
            // 0.346 (g, I-07): lowest offer minus 1 copied to the clipboard when the player opens an item's offers (off by default).
            registrations.Register(()=>undercut=new Market.UndercutWatcher(marketBoardEvents,framework,configuration,(item,lowest,paste,hq)=>
                chat.Print("[Tensho] "+localization.Format("Undercut.Copied",data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(item)?.Name.ExtractText() ?? $"#{item}",
                    lowest.ToString("N0"),paste.ToString("N0"),hq ? " HQ":"")),log),()=>undercut?.Dispose());
            mainWindow.WorldName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.World>().GetRowOrDefault(id)?.Name.ExtractText();
            mainWindow.TerritoryName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(id) is { } territory && territory.PlaceName.RowId!=0
                ? territory.PlaceName.Value.Name.ExtractText():null;
            // X-89 (0.970): expansions from the game's sheets (TerritoryType.ExVersion, Quest.Expansion), for the item tooltips' origin line.
            mainWindow.TerritoryExpansion=id=>data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(id) is { } expansionTerritory && expansionTerritory.ExVersion.IsValid
                ? expansionTerritory.ExVersion.Value.Name.ExtractText():null;
            mainWindow.QuestExpansion=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Quest>().GetRowOrDefault(id) is { } expansionQuest && expansionQuest.Expansion.IsValid
                ? expansionQuest.Expansion.Value.Name.ExtractText():null;
            // X-109 (0.980): the beast team plans kept on disk.
            mainWindow.BeastTeamsFile=()=>Path.Combine(pluginInterface.GetPluginConfigDirectory(),"BeastTeams.json");
            // X-105 (0.991): the jobs, and which ClassJobCategory rows include each (raw column = job + 1, the sheet's own layout).
            mainWindow.JobList=()=>data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().Where(job=>job.RowId!=0 && !job.Name.IsEmpty)
                .Select(job=>(job.RowId,job.Name.ExtractText() is { Length:>0 } name ? char.ToUpper(name[0],System.Globalization.CultureInfo.CurrentCulture)+name[1..]:"")).Where(job=>job.Item2.Length>0).ToArray();
            mainWindow.JobCategoriesOf=job=>
            {
                var found=new HashSet<uint>();
                try{foreach(var row in data.Excel.GetSheet<Lumina.Excel.RawRow>(null,"ClassJobCategory"))if(row.ReadColumn((int)job+1) is bool allowed && allowed)found.Add(row.RowId);}
                catch(Exception error){log.Warning(error,"Job categories unreadable (perhaps after a game update): the job filter finds nothing.");}
                return found;
            };
            mainWindow.GrandCompanyNameOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.GrandCompany>().GetRowOrDefault(id)?.Name.ExtractText();
            mainWindow.EquippedGearUnavailable=()=>gear?.Unavailable==true;
            // 0.333: names of the squadron's mission and training (Reminders).
            // g, C-02 (0.356): daily roulettes = Duty Finder roulettes with a completion index (docs/evidence/0.356-roulettes.md).
            var dailyRoulettes=LoadSheet("ContentRoulette",()=>data.GetExcelSheet<Lumina.Excel.Sheets.ContentRoulette>()
                .Where(row=>row.IsInDutyFinder && row.CompletionArrayIndex>=0 && !row.Name.IsEmpty && row.RowId<=byte.MaxValue)
                .OrderBy(row=>row.SortKey).Select(row=>new Reminders.RouletteRow((byte)row.RowId,(byte)row.ContentRouletteRoleBonus.RowId)).ToArray(),[]);
            mainWindow.DailyRoulettes=()=>dailyRoulettes;
            // g, M-11 (0.382): challenge log entries (ContentsNote rows with a name; docs/evidence/0.382-counters.md).
            var challengeRows=LoadSheet("ContentsNote",()=>data.GetExcelSheet<Lumina.Excel.Sheets.ContentsNote>().Where(row=>!row.Name.IsEmpty && row.RowId<=int.MaxValue).Select(row=>(int)row.RowId).ToArray(),[]);
            mainWindow.ChallengeRows=()=>challengeRows;
            // g, M-04 (0.374): one status file per character (Characters/<local character id>.json): ventures, counters, « done » boxes.
            Character.CharacterStatusWatcher? characterStatus=null;
            registrations.Register(()=>characterStatus=new Character.CharacterStatusWatcher(framework,player,log,configuration,()=>pluginInterface.SavePluginConfig(configuration),
                ()=>dailyRoulettes,Path.Combine(pluginInterface.GetPluginConfigDirectory(),"Characters")),()=>characterStatus?.Dispose());
            mainWindow.CharacterStatusSource=characterStatus;
            // g, C-03 (0.357): items the armoire takes, for the storage report.
            IReadOnlySet<uint>? cabinetItems=null;
            // g, C-14 (0.363): guildleves (Leve sheet, copied once) and the character's progress, read while the page shows them.
            IReadOnlyList<Leves.LeveDefinition>? leveList=null;
            IReadOnlyList<Leves.LeveDefinition> LeveList()=>leveList??=data.GetExcelSheet<Lumina.Excel.Sheets.Leve>()
                .Where(row=>!row.Name.IsEmpty && row.ClassJobLevel>0)
                .Select(row=>new Leves.LeveDefinition(row.RowId,row.Name.ExtractText(),row.ClassJobLevel,row.LeveAssignmentType.RowId,
                    row.LeveAssignmentType.ValueNullable?.Name.ExtractText() ?? "",(uint)Math.Max(0,row.LeveAssignmentType.ValueNullable?.Icon ?? 0),
                    row.LeveClient.ValueNullable?.Name.ExtractText() ?? "",row.PlaceNameStartZone.ValueNullable?.Name.ExtractText() ?? "",row.GilReward,row.AllowanceCost,
                    row.LevelLevemete.ValueNullable is { Map.RowId:not 0 } client ? new(client.Map.RowId,client.X,client.Z):null,
                    row.LevelStart.ValueNullable is { Map.RowId:not 0 } start ? new(start.Map.RowId,start.X,start.Z):null))
                .OrderBy(leve=>leve.Level).ThenBy(leve=>leve.Name,StringComparer.CurrentCulture).ToArray();
            Leves.LeveProgressReader? leveReader=null;
            registrations.Register(()=>leveReader=new Leves.LeveProgressReader(framework,client,player,unlocks,data,log,LeveList),()=>leveReader?.Dispose());
            mainWindow.LeveDefinitions=LeveList;
            // g, M-01 (0.371): automatic alarms at the ventures' end and the vessels' return (on by default, Alarms settings).
            // g, M-14 (0.384): the deciphered treasure map's spot (game functions checked, TreasureSpot → Level).
            Maps.TreasureMapReader? treasureMap=null;
            registrations.Register(()=>treasureMap=new Maps.TreasureMapReader(framework,client,data,log),()=>treasureMap?.Dispose());
            mainWindow.TreasureMap=()=>treasureMap?.Spot;
            // g, M-03 (0.373): map links of the chat, kept for the Maps page this session (map settings switch, on by default).
            Maps.ChatMapLinkWatcher? chatLinks=null;
            registrations.Register(()=>chatLinks=new Maps.ChatMapLinkWatcher(chatGui,client,log,()=>configuration.MapView.ChatLinks),()=>chatLinks?.Dispose());
            mainWindow.ChatLinkHistory=chatLinks?.History;
            Alarms.AutoAlarmWatcher? autoAlarms=null;
            registrations.Register(()=>autoAlarms=new Alarms.AutoAlarmWatcher(framework,client,log,configuration,()=>pluginInterface.SavePluginConfig(configuration),localization.Get),
                ()=>autoAlarms?.Dispose());
            // g, X-02 (0.370): the music playing now (BGMSystem, memory only) and its roll (BGM file = OrchestrionPath file).
            Collections.BgmReader? bgm=null;
            registrations.Register(()=>bgm=new Collections.BgmReader(framework,client,log),()=>bgm?.Dispose());
            Dictionary<uint,uint>? rollByBgm=null;
            mainWindow.PlayingRoll=()=>
            {
                bgm?.Want();
                if(bgm?.Playing is not { } playing)return null;
                rollByBgm??=BuildRollByBgm();
                return rollByBgm.TryGetValue(playing,out var roll) ? roll:null;
            };
            mainWindow.MusicRead=()=>bgm?.Playing is not null;
            Dictionary<uint,uint> BuildRollByBgm()
            {
                var rolls=data.GetExcelSheet<Lumina.Excel.Sheets.OrchestrionPath>().Where(row=>row.RowId!=0 && !row.File.IsEmpty)
                    .GroupBy(row=>row.File.ExtractText(),StringComparer.OrdinalIgnoreCase).ToDictionary(group=>group.Key,group=>group.First().RowId,StringComparer.OrdinalIgnoreCase);
                return data.GetExcelSheet<Lumina.Excel.Sheets.BGM>().Where(row=>!row.File.IsEmpty)
                    .Select(row=>(row.RowId,Roll:rolls.GetValueOrDefault(row.File.ExtractText()))).Where(pair=>pair.Roll!=0).ToDictionary(pair=>pair.RowId,pair=>pair.Roll);
            }
            // g, X-03 (0.369): wear a title (click), or the job's title on a job change when the player turned it on.
            Collections.TitleChanger? titleChanger=null;
            registrations.Register(()=>mainWindow.TitleChangerSource=titleChanger=new Collections.TitleChanger(framework,client,player,log,
                ()=>configuration.JobTitlesEnabled,()=>configuration.JobTitles),()=>titleChanger?.Dispose());
            // g, C-15 (0.368): achievement progress, read passively; asked from the server only on the player's click.
            Collections.AchievementProgressWatcher? achievementProgress=null;
            registrations.Register(()=>mainWindow.AchievementProgressSource=achievementProgress=new Collections.AchievementProgressWatcher(framework,client,player,log,
                Path.Combine(pluginInterface.GetPluginConfigDirectory(),"AchievementProgress.json"),id=>configuration.TrackedAchievements?.Contains(id)==true,mainWindow.AchievementCompleted),
                ()=>achievementProgress?.Dispose());
            // g, C-04 (0.365): hairstyles, accessories, glasses, bardings, portraits, master books, McGuffins, Occult Crescent lore.
            Collections.ExtraCollectionReader? extraCollections=null;
            registrations.Register(()=>extraCollections=new Collections.ExtraCollectionReader(framework,client,player,unlocks,data,log),()=>extraCollections?.Dispose());
            mainWindow.ExtraCollections=()=>{extraCollections?.Want();return extraCollections?.State;};
            mainWindow.ExtraCollectionsUnavailable=()=>extraCollections?.Unavailable==true;
            // g, C-12 (0.364): hunting log (MonsterNote, copied once) and the kills the game holds, read while the page shows them.
            IReadOnlyList<Hunting.HuntEntry>? huntEntries=null;
            mainWindow.HuntEntries=()=>huntEntries??=data.GetExcelSheet<Lumina.Excel.Sheets.MonsterNote>().Where(row=>!row.Name.IsEmpty && row.RowId>=10000)
                .Select(row=>new Hunting.HuntEntry(row.RowId,row.Name.ExtractText(),row.MonsterNoteTarget.Select((target,index)=>(target,index))
                    .Where(pair=>pair.target.RowId!=0 && pair.index<row.Count.Count && row.Count[pair.index]>0 && pair.target.ValueNullable is not null)
                    .Select(pair=>new Hunting.HuntTarget(pair.target.Value.BNpcName.ValueNullable?.Singular.ExtractText() ?? "",row.Count[pair.index],
                        string.Join(" / ",pair.target.Value.PlaceNameZone.Where(zone=>zone.RowId!=0).Select(zone=>zone.ValueNullable?.Name.ExtractText() ?? "").Where(name=>name.Length>0)),pair.target.Value.BNpcName.RowId))
                    .ToArray()))
                .Where(entry=>entry.Targets.Count>0).ToArray();
            Hunting.HuntingLogReader? huntReader=null;
            registrations.Register(()=>huntReader=new Hunting.HuntingLogReader(framework,client,log),()=>huntReader?.Dispose());
            mainWindow.HuntGrandCompany=()=>huntReader?.GrandCompany;
            mainWindow.HuntSlots=()=>{huntReader?.Want();return huntReader?.State;};
            mainWindow.HuntUnavailable=()=>huntReader?.Unavailable==true;
            mainWindow.LeveProgress=()=>{leveReader?.Want();return leveReader?.State;};
            mainWindow.LeveProgressUnavailable=()=>leveReader?.Unavailable==true;
            // g, C-05 (0.362): glamour dresser outfits (MirageStoreSetItem: outfit item → pieces), read once.
            IReadOnlyList<Collections.OutfitDefinition>? outfits=null;
            mainWindow.OutfitDefinitions=()=>outfits??=data.GetExcelSheet<Lumina.Excel.Sheets.MirageStoreSetItem>()
                .Select(row=>new Collections.OutfitDefinition(row.RowId,new[]{row.MainHand.RowId,row.OffHand.RowId,row.Head.RowId,row.Body.RowId,row.Hands.RowId,
                    row.Legs.RowId,row.Feet.RowId,row.Earrings.RowId,row.Necklace.RowId,row.Bracelets.RowId,row.Ring.RowId}.Where(piece=>piece!=0).Distinct().ToArray()))
                .Where(outfit=>outfit.SetItemId!=0 && outfit.Pieces.Count>1).ToArray();
            // g, C-07 (0.358): how an item looks, for the glamour dresser duplicates.
            mainWindow.ItemLookOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(id) is { } look
                ? new Inventory.ItemLook(look.ModelMain,look.ModelSub,look.EquipSlotCategory.RowId,look.DyeCount):null;
            mainWindow.CabinetItems=()=>cabinetItems??=data.GetExcelSheet<Lumina.Excel.Sheets.Cabinet>().Where(row=>row.Item.RowId!=0).Select(row=>row.Item.RowId).ToHashSet();
            mainWindow.RouletteName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.ContentRoulette>().GetRowOrDefault(id)?.Name.ExtractText();
            mainWindow.IslandPoints=LoadSheet("MJIGatheringItem",()=>Maps.IslandResources.Points(data.GetExcelSheet<Lumina.Excel.Sheets.MJIGatheringItem>().Select(row=>(row.Item.RowId,(int)row.X,(int)row.Y,(int)row.Radius))),[]);
            // W-04 (0.685): the pasture, read when the tab opens and on the player's click.
            mainWindow.ReadPasture=()=>framework.RunOnFrameworkThread(()=>
            {
                try
                {
                    var names=data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>();
                    return Island.PastureReader.Read(id=>names.GetRowOrDefault(id)?.Singular.ExtractText());
                }
                catch(Exception error){log.Warning(error,"Pasture unreadable (game update?).");throw;}
            });
            // The wiki's English words, in the game's language: each sheet read once in each language, off the framework.
            mainWindow.LocalizeIslandAnimals=rows=>Task.Run(()=>
            {
                static Func<string,string?> Words(IEnumerable<(string English,string Own)> pairs)
                {
                    var map=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                    foreach(var (english,own) in pairs)if(english.Length>0 && own.Length>0)map.TryAdd(english,own);
                    return word=>map.GetValueOrDefault(word);
                }
                var npcOwn=data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>();var itemOwn=data.GetExcelSheet<Lumina.Excel.Sheets.Item>();var weatherOwn=data.GetExcelSheet<Lumina.Excel.Sheets.Weather>();
                var wanted=rows.Select(row=>row.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var words=rows.SelectMany(row=>new[]{row.Restraint,row.Common,row.Bonus}).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var npc=Words(data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>(Dalamud.Game.ClientLanguage.English).Where(row=>wanted.Contains(row.Singular.ExtractText())).Select(row=>(row.Singular.ExtractText(),npcOwn.GetRowOrDefault(row.RowId)?.Singular.ExtractText() ?? "")));
                var item=Words(data.GetExcelSheet<Lumina.Excel.Sheets.Item>(Dalamud.Game.ClientLanguage.English).Where(row=>words.Contains(row.Name.ExtractText())).Select(row=>(row.Name.ExtractText(),itemOwn.GetRowOrDefault(row.RowId)?.Name.ExtractText() ?? "")));
                var weather=Words(data.GetExcelSheet<Lumina.Excel.Sheets.Weather>(Dalamud.Game.ClientLanguage.English).Select(row=>(row.Name.ExtractText(),weatherOwn.GetRowOrDefault(row.RowId)?.Name.ExtractText() ?? "")));
                return Island.IslandAnimalBook.Localized(rows,npc,item,weather);
            });
            // 0.684: what stands on the island, to name the game's markers that come with no text; read once, off the framework, on the island only.
            mainWindow.IslandPlacesLoad=()=>
            {
                var objects=data.GetExcelSheet<Lumina.Excel.Sheets.EObjName>();var people=data.GetExcelSheet<Lumina.Excel.Sheets.ENpcResident>();
                return data.GetExcelSheet<Lumina.Excel.Sheets.Level>().Where(row=>row.Territory.RowId==Maps.IslandResources.Territory && row.Type is 45 or 8)
                    .Select(row=>new Maps.IslandPlace(row.X,row.Z,row.Type==45 ? objects.GetRowOrDefault(row.Object.RowId)?.Singular.ExtractText() ?? "":people.GetRowOrDefault(row.Object.RowId)?.Singular.ExtractText() ?? "")).ToArray();
            };
            mainWindow.WorkshopProjectsLoad=()=>FreeCompany.WorkshopProjectsLoader.Read(data);
            mainWindow.CompanyActionNames=LoadSheet("Status",()=>data.GetExcelSheet<Lumina.Excel.Sheets.Status>().Where(row=>row.IsFcBuff).ToDictionary(row=>row.RowId,row=>row.Name.ExtractText()),[]);
            // N-10 (0.671): the island's workshops, read on the player's click.
            mainWindow.IslandCatalogLoad=()=>
            {
                try{return Island.IslandCatalogLoader.Read(data);}
                catch(Exception error){log.Warning(error,"Island sheets unreadable (game update?).");return null;}
            };
            mainWindow.ReadIsland=catalog=>framework.RunOnFrameworkThread(()=>
            {
                try{return Island.IslandReader.Read(catalog);}
                catch(Exception error){log.Warning(error,"Island unreadable (game update?).");throw;}
            });
            // W-01 (0.675): the custom delivery clients, read when the page opens and on the player's click.
            mainWindow.DeliveryClientsLoad=()=>
            {
                try{return Deliveries.DeliveryClientLoader.Read(data);}
                catch(Exception error){log.Warning(error,"Custom delivery sheets unreadable (game update?).");return null;}
            };
            mainWindow.ReadDeliveries=clients=>framework.RunOnFrameworkThread(()=>
            {
                try
                {
                    var quests=data.GetExcelSheet<Lumina.Excel.Sheets.Quest>();
                    return Deliveries.DeliveryReader.Read(clients,id=>quests.TryGetRow(id,out var quest) && unlocks.IsQuestCompleted(quest));
                }
                catch(Exception error){log.Warning(error,"Custom deliveries unreadable (game update?).");throw;}
            });
            // W-14b (0.721): every FATE's place, from the game's zone layouts, off the frame.
            mainWindow.FatesLoad=()=>
            {
                try{return Fields.FieldCatalogLoader.ReadOpenZones(data,log);}
                catch(Exception error){log.Warning(error,"FATE places unreadable (game update?).");return null;}
            };
            mainWindow.CurrentTerritory=()=>client.TerritoryType;
            // 0.757 (X-12): the aetherytes of each territory, from the game's sheet, for the teleport price of a zone.
            mainWindow.TerritoryAetherytes=LoadSheet("Aetheryte",()=>data.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>().Where(row=>row.IsAetheryte && row.Territory.RowId!=0)
                .GroupBy(row=>row.Territory.RowId).ToDictionary(group=>group.Key,group=>group.Select(row=>row.RowId).ToArray()),new Dictionary<uint,uint[]>());
            // W-16 (0.719): the lexicon, from the sheets, off the frame.
            mainWindow.LexiconLoad=()=>
            {
                try{return Lexicon.LexiconLoader.Read(data);}
                catch(Exception error){log.Warning(error,"Lexicon sheets unreadable (game update?).");return null;}
            };
            // W-15 (0.718): the deep dungeons, from the sheets, off the frame.
            mainWindow.DeepDungeonLoad=()=>
            {
                try{return DeepDungeons.DeepDungeonLoader.Read(data);}
                catch(Exception error){log.Warning(error,"Deep dungeon sheets unreadable (game update?).");return null;}
            };
            // W-14 (0.717): Eureka and Bozja, from the game's files, off the frame.
            // X-92 (0.972): what is new in the game's sheets since the last load (log and self-check).
            var gameDataCheck=Diagnostics.GameDataWatch.Start(data,Path.Combine(pluginInterface.GetPluginConfigDirectory(),"GameDataSnapshot.json"),log,gameDataCancel.Token);
            mainWindow.GameDataCheck=()=>gameDataCheck;
            mainWindow.FieldCatalogLoad=()=>
            {
                try{return Fields.FieldCatalogLoader.Read(data,log);}
                catch(Exception error){log.Warning(error,"Eureka and Bozja sheets unreadable (game update?).");return null;}
            };
            // W-11 (0.714): the Grand Companies, read when the page opens and on the player's click.
            mainWindow.GcFeminine=()=>framework.RunOnFrameworkThread(GrandCompanies.GcReader.Feminine);
            mainWindow.GcCatalogLoad=feminine=>
            {
                try{return GrandCompanies.GcCatalogLoader.Read(data,feminine);}
                catch(Exception error){log.Warning(error,"Grand Company sheets unreadable (game update?).");return null;}
            };
            mainWindow.ReadGc=rows=>framework.RunOnFrameworkThread(()=>
            {
                try{return GrandCompanies.GcReader.Read(rows);}
                catch(Exception error){log.Warning(error,"Grand Companies unreadable (game update?).");throw;}
            });
            // W-10 (0.713): the allied societies, read when the page opens and on the player's click.
            mainWindow.AlliedCatalogLoad=()=>
            {
                try{return Societies.AlliedCatalogLoader.Read(data);}
                catch(Exception error){log.Warning(error,"Allied society sheets unreadable (game update?).");return null;}
            };
            mainWindow.ReadAllied=catalog=>framework.RunOnFrameworkThread(()=>
            {
                try{return Societies.AlliedReader.Read(catalog);}
                catch(Exception error){log.Warning(error,"Allied societies unreadable (game update?).");throw;}
            });
            // W-03 (0.708): the Zodiac weapons, read when the page opens and on the player's click.
            mainWindow.RelicCatalogLoad=()=>
            {
                try{return Relics.RelicCatalogLoader.Read(data);}
                catch(Exception error){log.Warning(error,"Relic sheets unreadable (game update?).");return null;}
            };
            mainWindow.ReadRelics=()=>framework.RunOnFrameworkThread(()=>
            {
                try{return Relics.RelicReader.Read();}
                catch(Exception error){log.Warning(error,"Relic book unreadable (game update?).");throw;}
            });
            // W-02 (0.683): the stages of the Masked Carnivale, read when the page opens and on the player's click.
            mainWindow.CarnivaleStagesLoad=()=>
            {
                try{return Carnivale.CarnivaleStageLoader.Read(data);}
                catch(Exception error){log.Warning(error,"Masked Carnivale sheets unreadable (game update?).");return null;}
            };
            mainWindow.ReadCarnivale=stages=>framework.RunOnFrameworkThread(()=>
            {
                try{return Carnivale.CarnivaleReader.Read(stages);}
                catch(Exception error){log.Warning(error,"Masked Carnivale unreadable (game update?).");throw;}
            });
            // N-12 (0.670): names of the S rank conditions, in the game's language.
            mainWindow.SRankMarkName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>().GetRowOrDefault(id)?.Singular.ExtractText();
            mainWindow.SRankZoneName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(id)?.PlaceName.ValueNullable?.Name.ExtractText();
            mainWindow.SRankExpansionName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.ExVersion>().GetRowOrDefault((uint)id)?.Name.ExtractText();
            mainWindow.SRankRefName=item=>item.Kind switch
            {
                Hunting.SRankRefKind.Mob=>data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>().GetRowOrDefault(item.Id)?.Singular.ExtractText(),
                Hunting.SRankRefKind.Item=>data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(item.Id)?.Name.ExtractText(),
                Hunting.SRankRefKind.Minion=>data.GetExcelSheet<Lumina.Excel.Sheets.Companion>().GetRowOrDefault(item.Id)?.Singular.ExtractText(),
                Hunting.SRankRefKind.Fate=>data.GetExcelSheet<Lumina.Excel.Sheets.Fate>().GetRowOrDefault(item.Id)?.Name.ExtractText(),
                Hunting.SRankRefKind.Weather=>data.GetExcelSheet<Lumina.Excel.Sheets.Weather>().GetRowOrDefault(item.Id)?.Name.ExtractText(),
                Hunting.SRankRefKind.Action=>data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(item.Id)?.Name.ExtractText(),
                _=>null,
            };
            mainWindow.SquadronMissionName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyExpedition>().GetRowOrDefault(id)?.Name.ExtractText();
            // 0.449 (DEC-027): the embedded garden seeds (Lotlab), named in the game's language, read once.
            IReadOnlyList<(Reminders.GardenSeed,string)>? gardenSeeds=null;
            mainWindow.GardenSeedCatalog=()=>gardenSeeds??=((Func<IReadOnlyList<(Reminders.GardenSeed,string)>>)(()=>
            {
                try
                {
                    using var stream=typeof(Plugin).Assembly.GetManifestResourceStream("TenshoAssistant.Reminders.Data.garden-seeds.csv")
                        ?? throw new InvalidOperationException("Embedded garden seeds are missing.");
                    using var reader=new StreamReader(stream);
                    var items=data.GetExcelSheet<Lumina.Excel.Sheets.Item>();
                    return Reminders.GardenSeeds.Read(reader)
                        .Select(seed=>(seed,items.GetRowOrDefault(seed.SeedId)?.Name.ExtractText() ?? ""))
                        .Where(pair=>pair.Item2.Length>0).OrderBy(pair=>pair.Item2,StringComparer.CurrentCultureIgnoreCase).ToArray();
                }
                catch(Exception error){log.Warning(error,"Garden seeds unavailable.");return [];}
            }))();
            // 0.450 (DEC-027): the crossbreeds (nick75g, English names linked through the English Item sheet), read once off the frame.
            Task<(Reminders.GardenCrossData,IReadOnlyDictionary<uint,string>)>? gardenCrosses=null;
            mainWindow.GardenCrossCatalog=()=>gardenCrosses??=Task.Run(()=>
            {
                try
                {
                    var english=Reminders.GardenCrosses.EnglishIndex(data.GetExcelSheet<Lumina.Excel.Sheets.Item>(Dalamud.Game.ClientLanguage.English)
                        .Select(row=>(row.RowId,row.Name.ExtractText())));
                    using var stream=typeof(Plugin).Assembly.GetManifestResourceStream("TenshoAssistant.Reminders.Data.garden-crosses.csv")
                        ?? throw new InvalidOperationException("Embedded garden crossbreeds are missing.");
                    using var reader=new StreamReader(stream);
                    var crosses=Reminders.GardenCrosses.Read(reader,english);
                    if(crosses.Unmatched.Count>0)log.Information("Garden crossbreeds: {0} source names not in the English Item sheet: {1}.",crosses.Unmatched.Count,string.Join(", ",crosses.Unmatched));
                    var items=data.GetExcelSheet<Lumina.Excel.Sheets.Item>();
                    IReadOnlyDictionary<uint,string> names=crosses.Seeds.Concat([MainWindow.GardenTopsoil3,MainWindow.GardenTopsoil2]).Distinct().ToDictionary(id=>id,id=>items.GetRowOrDefault(id)?.Name.ExtractText() ?? "");
                    return (crosses,names);
                }
                catch(Exception error){log.Warning(error,"Garden crossbreeds unavailable.");throw;}
            });
            // 0.451 (DEC-027, N-06): the squadron read on click on the framework thread (loaded by the game in the barracks only), and the
            // sheets of the planner, read once: missions with their six requirement sets, attributes by class and level, class names.
            mainWindow.ReadSquadron=()=>framework.RunOnFrameworkThread(()=>
            {
                try{return Reminders.SquadronReader.Read();}
                catch(Exception error){log.Warning(error,"Squadron unreadable (game update?).");throw;}
            });
            Reminders.SquadronSheets? squadronSheets=null;var squadronSheetsFailed=false;
            mainWindow.SquadronSheetData=()=>
            {
                if(squadronSheets is not null || squadronSheetsFailed)return squadronSheets;
                try
                {
                    var missions=data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyExpedition>().Where(row=>!row.Name.IsEmpty)
                        .Select(row=>new Reminders.SquadronMission(row.RowId,row.Name.ExtractText(),row.RequiredLevel,row.GcArmyExpeditionType.RowId,row.PercentBase,
                            row.ExpeditionParams.Select(set=>new Reminders.SquadronRequirement(new(set.RequiredPhysical,set.RequiredMental,set.RequiredTactical),
                                set.PercentPhysicalMet,set.PercentMentalMet,set.PercentTacticalMet,set.PercentAllMet)).ToArray()))
                        .OrderBy(mission=>mission.Type).ThenBy(mission=>mission.Level).ThenBy(mission=>mission.Id).ToArray();
                    var grow=new Dictionary<uint,(IReadOnlyList<Reminders.SquadronStats>,Reminders.SquadronStats?)>();
                    foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyMemberGrow>())
                        if(row.ClassJob.RowId!=0)
                            grow[row.ClassJob.RowId]=(row.MemberParams.Select(level=>new Reminders.SquadronStats(level.Physical,level.Mental,level.Tactical)).ToArray(),
                                new Reminders.SquadronStats(row.Unknown1,row.Unknown2,row.Unknown3));
                    var jobs=data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>();
                    var classNames=grow.Keys.ToDictionary(job=>job,job=>jobs.GetRowOrDefault(job)?.Name.ExtractText() ?? "");
                    squadronSheets=new(missions,grow,classNames);
                }
                catch(Exception error){squadronSheetsFailed=true;log.Warning(error,"Squadron sheets unreadable (game update?).");}
                return squadronSheets;
            };
            mainWindow.SquadronMemberName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.ENpcResident>().GetRowOrDefault(id)?.Singular.ExtractText();
            // 0.460 (DEC-027 follow-up, user request): plantings read from the chat.
            Reminders.GardenChatWatcher? gardenWatcher=null;
            registrations.Register(()=>gardenWatcher=new Reminders.GardenChatWatcher(chatGui,framework,data,client,log,()=>configuration.GardenDetect,
                id=>mainWindow.GardenSeedCatalog?.Invoke().Any(row=>row.Item1.SeedId==id)==true,mainWindow.GardenPlanted,developer:()=>configuration.DeveloperMode,
                target:()=>targets.Target is { ObjectKind:Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj } bedObject ? bedObject.BaseId:0u),()=>gardenWatcher?.Dispose());
            mainWindow.SquadronTrainingName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyTraining>().GetRowOrDefault(id)?.Name.ExtractText();
            // 0.321: a failing Try On (game function moved) is logged once and its buttons turn off for the session.
            mainWindow.TryOnItem=item=>
            {
                var opened=Items.TryOn.Item(item,player.IsLoaded,error=>log.Warning(error,"Try On is unavailable (game update?)."));
                if(Items.TryOn.Failed)mainWindow.TryOnItem=null;
                return opened;
            };
            mainWindow.PlayerGrandCompany = () => player.IsLoaded ? player.GrandCompany.RowId : 0;
            var npcVendors=new Npcs.NpcVendorService();
            Npcs.NpcVendorLoader? npcLoader=null;
            registrations.Register(()=>npcLoader=new Npcs.NpcVendorLoader(npcVendors,data,framework,log,dataCache),()=>npcLoader?.Dispose());
            mainWindow.ConfigureNpcVendors(npcVendors);
            itemContextMenu=new ItemContextMenuBridge(configuration,contextMenus,player,client,itemCatalog,log,localization,gameGui){Relevant=mainWindow.RelevantRoutes,Title=mainWindow.RouteTitle,Sources=mainWindow.SourceMenu,
                // 0.826 (X-58): the crafting lists first, then the item's pages.
                Links=id=>[..mainWindow.CraftListAdds(id),..(mainWindow.ExternalLinksFor(id) ?? [])]};
            registrations.Register(()=>{ },itemContextMenu.Dispose);
            InventoryVisualSortBridge? sortBridge=null;
            registrations.Register(()=>sortBridge=new InventoryVisualSortBridge(visualSort,inventory,player,client,framework,log,bankBridge),()=>sortBridge?.Dispose());
            MacroChainBridge? macroBridge = null;
            registrations.Register(() => macroBridge = new MacroChainBridge(macroChain, framework, player, client, commands, interop, log), () => macroBridge?.Dispose());
            MacroObservationReader? macroObserver = null;
            registrations.Register(() => macroObserver = new MacroObservationReader(macroObservations, framework, player, client, log), () => macroObserver?.Dispose());
            ItemHoverReader? hoverReader = null;
            registrations.Register(() => hoverReader = new ItemHoverReader(configuration, market, itemHover, itemCatalog, gameGui, player, client, framework, log) { InDuty = () => mapLive?.State is { InDuty: true }, Keys = keyState, Developer=()=>configuration.DeveloperMode, },
                () => hoverReader?.Dispose());
            MapCatalogLoader? mapLoader = null;
            registrations.Register(() => mapLoader = new MapCatalogLoader(maps, data, framework, log), () => mapLoader?.Dispose());
            DutyCatalogLoader? dutyLoader = null;
            registrations.Register(() => dutyLoader = new DutyCatalogLoader(duties, data, framework, log), () => dutyLoader?.Dispose());
            // g, C-01 (0.355): unlocked / completed duties, read on the framework thread while the Duty loot page shows them.
            DutyProgressReader? dutyProgress = null;
            registrations.Register(() => dutyProgress = new DutyProgressReader(framework, client, unlocks, data, log, () => duties.State.Catalog, player, Path.Combine(pluginInterface.GetPluginConfigDirectory(), "DutyProgress.json")), () => dutyProgress?.Dispose());
            mainWindow.DutyProgressSource = () => { dutyProgress?.Want(); return dutyProgress?.State; };
            mainWindow.DutyProgressUnavailable = () => dutyProgress?.Unavailable == true;
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
            // A-02 (0.420): crafting food and medicine from the game's sheets (Item › ItemAction › ItemFood), built on first use.
            var craftConsumables=new Lazy<IReadOnlyList<Craft.CraftConsumable>>(()=>
            {
                try
                {
                    var foods=data.GetExcelSheet<Lumina.Excel.Sheets.ItemFood>();var list=new List<Craft.CraftConsumable>();
                    foreach(var item in data.GetExcelSheet<Lumina.Excel.Sheets.Item>().Where(row=>row.ItemAction.RowId!=0 && !row.Name.IsEmpty))
                    {
                        if(item.ItemAction.ValueNullable is not { } action || action.Data.Count<2 || (uint)action.Data[0] is not (Craft.CraftConsumables.WellFed or Craft.CraftConsumables.Medicated)
                           || foods.GetRowOrDefault(action.Data[1]) is not { } food)continue;
                        var bonuses=food.Params.Where(param=>param.BaseParam.RowId is Craft.CraftConsumables.Craftsmanship or Craft.CraftConsumables.Control or Craft.CraftConsumables.Cp)
                            .Select(param=>new Craft.CraftBonus(param.BaseParam.RowId,param.IsRelative,param.Value,param.Max,param.ValueHQ,param.MaxHQ)).ToArray();
                        if(bonuses.Length>0)list.Add(new(item.RowId,item.Name.ExtractText(),(uint)action.Data[0]==Craft.CraftConsumables.Medicated,bonuses,action.Data[1]));
                    }
                    return list.OrderByDescending(item=>item.Bonuses.Sum(bonus=>bonus.MaxHq)).ThenBy(item=>item.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
                }
                catch(Exception error){log.Warning(error,"Crafting food and medicine unavailable.");return [];}
            });
            mainWindow.CraftConsumableList=()=>craftConsumables.Value;
            // 0.464: food, medicine and specialist read from the game for the simulator; a failure leaves them unknown.
            var craftDetectionWarned=false;
            mainWindow.CraftDetectedNow=job=>
            {
                try{return Craft.CraftDetectionReader.Read(objects,job);}
                catch(Exception error){if(!craftDetectionWarned){craftDetectionWarned=true;log.Warning(error,"Crafting food, medicine and specialist could not be read (perhaps after a game update).");}return null;}
            };
            // A-11 (0.421): recipes crafted at least once (QuestManager.IsRecipeComplete), read while a crafting view asks.
            Craft.RecipeLogReader? recipeLog=null;
            registrations.Register(()=>mainWindow.RecipeLog=recipeLog=new Craft.RecipeLogReader(framework,client,log,()=>recipeCatalog.State.Catalog),()=>recipeLog?.Dispose());
            // A-10 (0.422): Macro Chain (Caraxi, InternalName « MacroChain ») loaded: exported pages use its /nextmacro.
            var macroChainCheckedAt=DateTimeOffset.MinValue;var macroChainLoaded=false;
            mainWindow.MacroChainLoaded=()=>
            {
                // Checked at most every 5 seconds (asked from Draw).
                var now=DateTimeOffset.UtcNow;
                if(now>=macroChainCheckedAt){macroChainCheckedAt=now.AddSeconds(5);macroChainLoaded=pluginInterface.InstalledPlugins.Any(plugin=>plugin.InternalName=="MacroChain" && plugin.IsLoaded);}
                return macroChainLoaded;
            };
            // N-01 (0.440): hunt orders' targets (MobHuntOrder subrows → MobHuntTarget) and bill names (MobHuntOrderType › EventItem),
            // in the game's language, built on first use.
            var huntOrders=new Lazy<Dictionary<uint,IReadOnlyList<Reminders.HuntOrderTarget>>>(()=>
            {
                var orders=new Dictionary<uint,IReadOnlyList<Reminders.HuntOrderTarget>>();
                try
                {
                    foreach(var group in data.GetSubrowExcelSheet<Lumina.Excel.Sheets.MobHuntOrder>())
                        orders[group.RowId]=group.Select(order=>new Reminders.HuntOrderTarget(order.SubrowId,Community.CommunityNames.Name("A",order.Target.ValueNullable?.Name.ValueNullable?.Singular.ExtractText()),
                            order.Target.ValueNullable?.Map.RowId ?? 0,order.Target.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() ?? "",order.NeededKills,order.Target.ValueNullable?.FATE.RowId ?? 0)).ToArray();
                }
                catch(Exception error){log.Warning(error,"Hunt orders unavailable.");}
                return orders;
            });
            mainWindow.HuntOrderTargets=row=>huntOrders.Value.GetValueOrDefault(row) ?? [];
            mainWindow.HuntBillName=index=>data.GetExcelSheet<Lumina.Excel.Sheets.MobHuntOrderType>().GetRowOrDefault(index)?.EventItem.ValueNullable?.Name.ExtractText() is { Length:>0 } name ? name:$"#{index}";
            // N-02 (0.443): Wondrous Tails task names: type 0 = an instance (its ContentFinderCondition name), else WeeklyBingoText.
            var bingoDuties=new Lazy<Dictionary<uint,string>>(()=>
            {
                var names=new Dictionary<uint,string>();
                try{foreach(var duty in data.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>())if(duty.ContentLinkType==1 && duty.Content.RowId!=0 && !duty.Name.IsEmpty)names.TryAdd(duty.Content.RowId,duty.Name.ExtractText());}
                catch(Exception error){log.Warning(error,"Wondrous Tails duty names unavailable.");}
                return names;
            });
            mainWindow.BingoTaskName=order=>data.GetExcelSheet<Lumina.Excel.Sheets.WeeklyBingoOrderData>().GetRowOrDefault(order) is { } row
                ? row.Type==0 && bingoDuties.Value.TryGetValue(row.Data.RowId,out var duty) ? Community.CommunityNames.Name("A",duty)
                    :row.Text.ValueNullable?.Description.ExtractText() is { Length:>0 } text ? text:$"#{order}"
                :$"#{order}";
            // X-09 (0.444): residential districts (TerritoryType, intended use 13), in the game's language.
            var housingDistricts=new Lazy<IReadOnlyList<(uint,string)>>(()=>
            {
                try{return data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().Where(row=>row.TerritoryIntendedUse.RowId==13 && row.PlaceName.RowId!=0)
                    .Select(row=>(row.RowId,row.PlaceName.ValueNullable?.Name.ExtractText() ?? "")).Where(row=>row.Item2.Length>0).ToArray();}
                catch(Exception error){log.Warning(error,"Residential districts unavailable.");return [];}
            });
            mainWindow.HousingDistricts=()=>housingDistricts.Value;
            // N-05 (0.445): PaissaDB open plots (read only), its new plots said in the chat while the watch is on.
            var paissa=new Community.PaissaDbService();
            registrations.Register(()=>{},paissa.Dispose);
            mainWindow.Paissa=paissa;
            mainWindow.PlotAlert=text=>framework.RunOnFrameworkThread(()=>chat.Print("[Tensho] "+text));
            mainWindow.ChatNotice=text=>chat.Print("[Tensho] "+text);
            // X-13 (0.447): the conductor's call, sent to the chosen channel on click only, on the framework thread.
            mainWindow.SendHuntCall=command=>framework.RunOnFrameworkThread(()=>Maps.HuntAnnouncer.Send(command,log));
            // A-05 (0.433): crafting lists in a file of their own (like the Bank).
            // TEMPLATES-001 (DEC-029): tools made of data, in a file of their own.
            TenshoAssistant.MyTools.ToolTemplateStore? myTools=null;
            registrations.Register(()=>mainWindow.MyToolsStore=myTools=new TenshoAssistant.MyTools.ToolTemplateStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"MyTools.json"),(error,message)=>log.Warning(error,message)),()=>myTools?.Dispose());
            TenshoAssistant.MyTools.ToolGallery? toolGallery=null;
            registrations.Register(()=>mainWindow.MyToolsGallery=toolGallery=new TenshoAssistant.MyTools.ToolGallery(),()=>toolGallery?.Dispose());
            mainWindow.CraftListStore=new Craft.CraftListStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"CraftLists.json"),(error,message)=>log.Warning(error,message));
            // A-14 (0.436): crafting rotation recorder (read-only hook on UseAction, only while recording).
            Craft.CraftRecorder? craftRecorder=null;
            registrations.Register(()=>mainWindow.CraftRecorder=craftRecorder=new Craft.CraftRecorder(interop,log,()=>craftActions.State.Catalog),()=>craftRecorder?.Dispose());
            // X-11 (0.419): Manipulation learned by the job, from its action row (CraftActionCatalog) and the game's unlock state.
            // 0.468: said once by job in the log, so a wrong detection can be told from the action's data.
            var manipulationLogged=new HashSet<uint>();
            mainWindow.ManipulationLearned=job=>
            {
                if(craftActions.State.Catalog?.Find(job,Craft.CraftActionKind.Manipulation) is not { } manipulation
                    || data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(manipulation.NativeId) is not { } action)return false;
                var unlocked=unlocks.IsActionUnlocked(action);
                if(manipulationLogged.Add(job))log.Information("Manipulation for job {Job}: action {Action}, unlock link {Link}, unlocked {Unlocked}",job,manipulation.NativeId,action.UnlockLink.RowId,unlocked);
                return unlocked;
            };
            registrations.Register(()=>craftLogReader=new CraftLogReader(mainWindow.CraftLog,recipeCatalog,configuration,framework,player,client,gameGui,log),()=>craftLogReader?.Dispose());
            FashionHintLoader? fashionLoader=null;
            registrations.Register(()=>fashionLoader=new FashionHintLoader(fashionHints,data,framework,log),()=>fashionLoader?.Dispose());
            var fashionCheck=new Fashion.FashionCheckService();
            Fashion.FashionCheckReader? fashionCheckReader=null;
            registrations.Register(()=>fashionCheckReader=new Fashion.FashionCheckReader(fashionCheck,framework,gameGui,log,()=>mainWindow.DeveloperMode)
            {
                // X-73.4 (0.861): the outfit judged, for the discoveries base.
                Judged=(theme,slots)=>
                {
                    if(!configuration.Discovery.Enabled || !configuration.Discovery.FashionReport || mainWindow.DiscoveryStore is not { } store)return;
                    var at=DateTimeOffset.UtcNow;
                    foreach(var slot in slots)store.SeeSource(Discoveries.DiscoveryKind.FashionEvaluation,slot.Item,slot.Category,slot.Evaluation,at,theme);
                },
            },()=>fashionCheckReader?.Dispose());
            mainWindow.ConfigureFashionStains(id=>data.GetExcelSheet<Lumina.Excel.Sheets.Stain>().TryGetRow(id,out var stain) ? (stain.Name.ExtractText(),stain.Color):null);
            mainWindow.ConfigureFashionCheck(fashionCheck,row=>data.GetExcelSheet<Lumina.Excel.Sheets.FashionCheckWeeklyTheme>().TryGetRow(row,out var theme) ? theme.Name.ExtractText():null);
            QuestReader? questReader = null;
            registrations.Register(() => questReader = new QuestReader(quests, data, framework, player, client, unlocks, log,()=>configuration.Cache.Duration(CacheArea.Quests),dataCache),
                () => questReader?.Dispose());
            CollectionReader? collectionReader = null;
            registrations.Register(() => collectionReader = new CollectionReader(collections, data, framework, player, client, unlocks, log,()=>configuration.Cache.Duration(CacheArea.Collections),dataCache,()=>configuration.Cache.Duration(CacheArea.Completion)),
                () => collectionReader?.Dispose());
            TriadObservationReader? triadReader = null;
            MiniCactpotObservationReader? cactpotReader=null;
            GatheringObservationReader? gatheringReader=null;
            mainWindow.GatheringSlotRects=()=>gatheringReader?.SlotRects ?? [];
            mainWindow.GatheringAutoGather=()=>gatheringReader?.AutoGather;
            registrations.Register(()=>gatheringReader=new GatheringObservationReader(gatheringObservation,framework,player,client,gameGui,objects,log),()=>gatheringReader?.Dispose());
            CollectableReader? collectableReader=null;
            SpearfishingReader? spearfishingReader=null;
            registrations.Register(()=>mainWindow.Spearfishing=spearfishingReader=new SpearfishingReader(framework,gameGui,client,log,
                // 0.894 (user capture: node 32076 found, list of the zone shown): several points share one GatheringPointBase (32074–32076 → 465) and the catalogue
                // keeps one point per base, so the node is given as its base.
                ()=>(targets.Target is { ObjectKind:Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint } node ? node.BaseId
                    // 0.892: no node targeted: the nearest targetable gathering point within 12 yalms.
                    :objects.LocalPlayer is { } me ? objects.Where(item=>item.ObjectKind==Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint && item.IsTargetable
                        && System.Numerics.Vector3.Distance(item.Position,me.Position)<=12).OrderBy(item=>System.Numerics.Vector3.Distance(item.Position,me.Position))
                        .Select(item=>item.BaseId).FirstOrDefault():0u) is var found and not 0
                    && data.GetExcelSheet<Lumina.Excel.Sheets.GatheringPoint>().GetRowOrDefault(found) is { } pointRow ? pointRow.GatheringPointBase.RowId:0u),()=>spearfishingReader?.Dispose());
            registrations.Register(()=>mainWindow.Collectables=collectableReader=new CollectableReader(framework,gameGui,client,data,log),()=>collectableReader?.Dispose());
            mainWindow.CollectableActionInfo=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id) is { } row ? (row.Name.ExtractText(),(uint)row.Icon,(int)row.PrimaryCostValue):null;
            // 0.480: status 757, Scrutiny (Status sheet, docs/evidence/0.479-collectable-window-values.md).
            mainWindow.CollectableActionStatus=id=>Gathering.CollectableActionState.Status(id);
            mainWindow.GatheringRecastLeft=id=>Gathering.CollectableActionState.RecastLeft(id);
            Gathering.ActionBarLocator? barLocator=null;
            registrations.Register(()=>mainWindow.BarLocator=barLocator=new Gathering.ActionBarLocator(framework,gameGui,log),()=>{mainWindow.BarLocator=null;barLocator?.Dispose();});
            // 0.517 (user report): IsActionUnlocked is false for a row without an unlock link, and 22 of the 24 gathering actions have none: they come with the level, which the plan checks (docs/evidence/0.517-gp-action-unlocks.md).
            var gatheringActionRows=new Dictionary<uint,(string,uint,string)?>();
            mainWindow.GatheringActionRow=id=>
            {
                if(gatheringActionRows.TryGetValue(id,out var known))return known;
                (string,uint,string)? found=null;
                try
                {
                    if(data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id) is { } row)
                        found=(row.Name.ExtractText(),row.Icon,data.GetExcelSheet<Lumina.Excel.Sheets.ActionTransient>().GetRowOrDefault(id)?.Description.ExtractText() ?? "");
                }
                catch(Exception error){log.Warning(error,"Gathering action {Id} unavailable (perhaps after a game update).",id);}
                gatheringActionRows[id]=found;
                return found;
            };
            UI.GameWindowsReader? gameWindows=null;
            registrations.Register(()=>{gameWindows=new UI.GameWindowsReader(framework,client,gameGui,log,()=>mainWindow.DeveloperMode);mainWindow.GameWindowSource=()=>gameWindows.Windows;},()=>{mainWindow.GameWindowSource=null;gameWindows?.Dispose();});
            mainWindow.ItemLinkToChat=id=>chatGui.Print(new Dalamud.Game.Text.SeStringHandling.SeStringBuilder().AddItemLink(id,false).BuiltString);
            UI.GameWindowFollower? gameWindowFollower=null;
            registrations.Register(()=>{gameWindowFollower=new UI.GameWindowFollower(framework,gameGui,log);mainWindow.GameWindowFollow=gameWindowFollower.At;bankOverlay.Follow=gameWindowFollower.At;},()=>{mainWindow.GameWindowFollow=null;bankOverlay.Follow=null;gameWindowFollower?.Dispose();});
            Gathering.GatherKnowledgeReader? gatherKnowledge=null;
            registrations.Register(()=>{gatherKnowledge=new Gathering.GatherKnowledgeReader(data,framework,client,player,log);mainWindow.GatherAccess=gatherKnowledge.Access;mainWindow.GatherKind=gatherKnowledge.Kind;},()=>{mainWindow.GatherAccess=null;mainWindow.GatherKind=null;gatherKnowledge?.Dispose();});
            // X-76 P4 (0.927): the game's own unlock state of an item (null when it teaches nothing, when nobody is logged in, or in PvP).
            mainWindow.ItemUnlockState=id=>
            {
                if(Competitive.CompetitiveGate.Cut || !client.IsLoggedIn || !player.IsLoaded)return null;
                try{return data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(id) is { } row && unlocks.IsItemUnlockable(row) ? unlocks.IsItemUnlocked(row):null;}
                catch(Exception){return null;}
            };
            // X-76 P5 (0.928): targeted ventures by item, read once from RetainerTask (not random) → RetainerTaskNormal.Item.
            var ventureTasks=new Lazy<IReadOnlyDictionary<uint,IReadOnlyList<(int Level,string Jobs)>>>(()=>
            {
                try
                {
                    var normal=data.GetExcelSheet<Lumina.Excel.Sheets.RetainerTaskNormal>();var found=new Dictionary<uint,List<(int Level,string Jobs)>>();
                    foreach(var task in data.GetExcelSheet<Lumina.Excel.Sheets.RetainerTask>())
                    {
                        if(task.IsRandom || task.Task.RowId==0 || normal.GetRowOrDefault(task.Task.RowId) is not { } row || row.Item.RowId==0)continue;
                        if(!found.TryGetValue(row.Item.RowId,out var list))found[row.Item.RowId]=list=[];
                        list.Add((task.RetainerLevel,task.ClassJobCategory.ValueNullable?.Name.ExtractText() ?? ""));
                    }
                    return found.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<(int Level,string Jobs)>)pair.Value.Distinct().OrderBy(task=>task.Level).ToArray());
                }
                catch(Exception error){log.Warning(error,"Retainer venture sheets unavailable (perhaps after a game update).");return new Dictionary<uint,IReadOnlyList<(int Level,string Jobs)>>();}
            });
            mainWindow.VentureTasksOf=id=>ventureTasks.Value.GetValueOrDefault(id) ?? [];
            // X-76 P5 (0.929): the sightseeing log phases of each expansion (AdventureExPhase), read once.
            var vistaPhases=new Lazy<IReadOnlyList<(uint Begin,uint End,int Expansion)>?>(()=>
            {
                try{return data.GetExcelSheet<Lumina.Excel.Sheets.AdventureExPhase>().Select(row=>(row.AdventureBegin.RowId,row.AdventureEnd.RowId,(int)row.Expansion.RowId)).ToArray();}
                catch(Exception error){log.Warning(error,"Sightseeing log phases unavailable (perhaps after a game update).");return null;}
            });
            mainWindow.VistaPhases=()=>vistaPhases.Value;
            // X-88 (0.969): the game's group of an item category, for « add a whole group » in the sorting rules.
            mainWindow.SortCategoryGroup=id=>data.GetExcelSheet<Lumina.Excel.Sheets.ItemUICategory>().GetRowOrDefault(id)?.OrderMajor ?? 0;
            mainWindow.GpActionUnlocked=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id) is { } row && (row.UnlockLink.RowId==0 || unlocks.IsActionUnlocked(row));
            mainWindow.GpTraitsUnlocked=job=>
            {
                var sheet=data.GetExcelSheet<Lumina.Excel.Sheets.Trait>();
                // 0.517: the same for a trait without a quest: it comes with the level.
                return Gathering.GpRegen.Bonus(Gathering.GpRegen.Traits(job).Where(id=>sheet.GetRowOrDefault(id) is { } row && (row.Quest.RowId==0 ? player.GetClassJobLevel(row.ClassJob.Value)>=row.Level:unlocks.IsTraitUnlocked(row))),job);
            };
            mainWindow.ScrutinyActive=()=>objects.LocalPlayer is { } local && local.StatusList.Any(status=>status.StatusId==757);
            // 0.520: Enhanced Solid Reason and Enhanced Ageless Words, Eureka Moment (docs/evidence/0.410-gp-actions.md).
            mainWindow.EurekaReady=()=>objects.LocalPlayer is { } local && local.StatusList.Any(status=>status.StatusId==2765);
            mainWindow.EurekaChanceFor=job=>data.GetExcelSheet<Lumina.Excel.Sheets.Trait>().GetRowOrDefault(job==16 ? 242u:243u) is { } row && (row.Quest.RowId==0 ? player.GetClassJobLevel(row.ClassJob.Value)>=row.Level:unlocks.IsTraitUnlocked(row)) ? Gathering.GpScenario.EurekaChance:0m;
            registrations.Register(()=>cactpotReader=new MiniCactpotObservationReader(cactpotObservation,framework,player,client,gameGui,log),()=>cactpotReader?.Dispose());
            registrations.Register(() => triadReader = new TriadObservationReader(triadObservation, framework, player, client, gameGui, log, () => targets.Target?.BaseId ?? 0, data, () => mainWindow.DeveloperMode),
                () => triadReader?.Dispose());
            ItemCatalogLoader? catalogLoader = null;
            ItemUseCatalogLoader? usageLoader=null;
            registrations.Register(()=>usageLoader=new ItemUseCatalogLoader(itemUses,data,framework,log),()=>usageLoader?.Dispose());
            // 0.469 (user request, in-game campaign): today's Grand Company request, read from the game's supply missions window.
            mainWindow.GcSupply.Restore(configuration.GcSupplySeen);
            // 0.787 (X-28): what the workshop already received, from its delivery window.
            FreeCompany.WorkshopDeliveryReader? workshopDeliveryReader=null;
            registrations.Register(()=>workshopDeliveryReader=new FreeCompany.WorkshopDeliveryReader(framework,gameGui,log,mainWindow.WorkshopDeliverySeen),()=>workshopDeliveryReader?.Dispose());
            Items.GcSupplyReader? gcSupplyReader=null;
            registrations.Register(()=>gcSupplyReader=new Items.GcSupplyReader(framework,gameGui,client,player,data,log,()=>itemUses.State.Catalog,mainWindow.GcSupply,seen=>
            {
                configuration.GcSupplySeen=seen;
                try{pluginInterface.SavePluginConfig(configuration);}catch(Exception error){log.Warning(error,"Today's Grand Company request could not be saved.");}
            },developer:()=>configuration.DeveloperMode),()=>gcSupplyReader?.Dispose());
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
            craftLive.SetEnabled(MainWindow.CraftLiveWanted(configuration));
            gatheringObservation.SetEnabled(MainWindow.GatheringObservationWanted(configuration));
            cactpotObservation.SetEnabled(configuration.Observations?.GetValueOrDefault("Cactpot") == true);
            triadObservation.SetEnabled(configuration.Observations?.GetValueOrDefault("Triad") == true);
            macroObservations.SetEnabled(configuration.Observations?.GetValueOrDefault("Macros") == true);
            registrations.Register(() => windows.AddWindow(mainWindow), () => windows.RemoveWindow(mainWindow));
            // 0.724: developer mode only, once per load, after the self-check: every page drawn out of sight, what it cost and any error in the log.
            Diagnostics.PageSweepWindow? pageSweep=null;
            // 0.748 (X-01): at most one sweep every two hours, whatever the reloads; the time of the last one is kept beside the self-check's memory.
            var sweepFile=Path.Combine(pluginInterface.GetPluginConfigDirectory(),"PageSweepLast.txt");
            string? sweepLast=null;
            try{if(File.Exists(sweepFile))sweepLast=File.ReadAllText(sweepFile);}catch(Exception error) when(error is IOException or UnauthorizedAccessException){log.Warning(error,"Page sweep time could not be read.");}
            var sweepDue=Diagnostics.PageSweep.Due(sweepLast,DateTimeOffset.UtcNow);
            registrations.Register(() => windows.AddWindow(pageSweep = new Diagnostics.PageSweepWindow(configuration.DeveloperMode && sweepDue,
                () => client.IsLoggedIn && player.IsLoaded && selfCheck?.LastLines.Count>0,
                () => !client.IsLoggedIn || !player.IsLoaded || new[]
                {
                    Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat,Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty,Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty56,
                    Dalamud.Game.ClientState.Conditions.ConditionFlag.Crafting,Dalamud.Game.ClientState.Conditions.ConditionFlag.Gathering,Dalamud.Game.ClientState.Conditions.ConditionFlag.Fishing,
                    Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas,Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene,Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInCutSceneEvent,
                }.Any(flag => condition[flag]),
                mainWindow.PageSweepEntries,MainWindow.PageSweepSkipped,log,
                (name,ms)=>mainWindow.SlowFrames.Add(Dalamud.Bindings.ImGui.ImGui.GetFrameCount(),name,ms),
                ()=>{try{File.WriteAllText(sweepFile,DateTimeOffset.UtcNow.ToString("O",System.Globalization.CultureInfo.InvariantCulture));}catch(Exception error) when(error is IOException or UnauthorizedAccessException){log.Warning(error,"Page sweep time could not be saved.");}})), () => { if (pageSweep is not null) windows.RemoveWindow(pageSweep); });
            registrations.Register(() => windows.AddWindow(guideWindow = new GuideWindow(mainWindow)), () => { if (guideWindow is not null) windows.RemoveWindow(guideWindow); guideWindow = null; });
            registrations.Register(() => windows.AddWindow(mapWindow = new MapViewWindow(mainWindow)), () => { if (mapWindow is not null) windows.RemoveWindow(mapWindow); });
            registrations.Register(() => { attachedPanels = new AttachedPanels(windows, mainWindow); mainWindow.Panels = attachedPanels; bankOverlay.Panels = attachedPanels; }, () => { mainWindow.Panels = null; bankOverlay.Panels = null; attachedPanels?.Dispose(); attachedPanels = null; });
            registrations.Register(() => { panelWindows = mainWindow.CreatePanelWindows(); foreach (var panel in panelWindows) windows.AddWindow(panel); }, () => { foreach (var panel in panelWindows) windows.RemoveWindow(panel); panelWindows = []; });
            registrations.Register(() =>
            {
                commandInfo=new CommandInfo(OnCommand){HelpMessage=localization.Get("Plugin.CommandHelp")};
                commandLocalizationRevision=localization.Revision;
                if (!commands.AddHandler(Command,commandInfo))
                    throw new InvalidOperationException($"Command already registered: {Command}");
            }, () => commands.RemoveHandler(Command));
            // 0.394 (user request): titles, sections and text fonts from the settings (TrumpGothic 24 / 19 and Dalamud's font by default).
            registrations.Register(() => { fonts = new UI.FontManager(pluginInterface.UiBuilder, log); fonts.Apply(configuration.Fonts ?? new()); },
                () => { fonts?.Dispose(); fonts = null; });
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
            void SayLoad(string when)=>log.Information("Load {When}: {Milliseconds} ms since the start, runtime pauses {Pauses} ms, {Collections} full collections, on thread {Thread}.",
                when,(long)System.Diagnostics.Stopwatch.GetElapsedTime(loadStarted).TotalMilliseconds,(long)(GC.GetTotalPauseDuration()-pausesBefore).TotalMilliseconds,GC.CollectionCount(2)-gen2Before,framework.IsInFrameworkUpdateThread ? "framework":"other");
            SayLoad("created");
            _=Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_=>SayLoad("10 s later"),TaskScheduler.Default);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    // « /tensho » opens the window; « /tensho peche » (or fish) opens or closes the fishing help (0.228).
    private void OnCommand(string command, string arguments)
    {
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut){chatForCut?.Print("[Tensho] "+localization.Get("Competitive.Off"));return;}
        if (arguments.Trim().ToLowerInvariant() is "peche" or "pêche" or "fish" or "fishing") { mainWindow?.ToggleFishingPanel(); return; }
        // 0.349 (g, X-07): « /tensho wiki <item> » and « /tensho lodestone <item> » open the item's page in the browser.
        var parts = arguments.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        // 0.494: « /tensho selftest » writes the self-check to the log.
        if (arguments.Trim().ToLowerInvariant() is "selftest")
        {
            selfCheck?.Start("command");
            chatGui.Print("[Tensho] " + localization.Get("DiagnosticsUi.SelfCheckStarted"));
            return;
        }
        // 0.836 (X-03, dalamud.log 23:06: the ring printed, then no menu event at all): developer mode, « /tensho linktest » prints the link in four forms, for a
        // right click (item link) or a left click (Tensho's link) on each; the log then says which ones the game handed over.
        if (arguments.Trim().ToLowerInvariant() is "linktest" && mainWindow.DeveloperMode) { PrintLinkTest(); return; }
        // 0.381 (g, X-01): « /tensho todo <task> » adds a free task to Reminders › Your tasks.
        if (parts.Length >= 1 && parts[0].ToLowerInvariant() is "todo" or "tache" or "tâche")
        {
            chatGui.Print("[Tensho] " + (parts.Length > 1 ? mainWindow.AddTaskFromCommand(parts[1]) : localization.Get("PersonalTasks.CommandUsage")));
            return;
        }
        if (parts.Length >= 1 && MainWindow.CommandRoute(parts[0]) is { } route)
        {
            var sheet = parts[0].ToLowerInvariant() is "objet" or "item";
            if (mainWindow?.OpenFromCommand(route, sheet, parts.Length > 1 ? parts[1] : "") is { } failed) chatGui.Print("[Tensho] " + failed);
            return;
        }
        if (parts.Length >= 1 && parts[0].ToLowerInvariant() is "wiki" or "lodestone")
        {
            if (mainWindow?.OpenExternal(parts[0].ToLowerInvariant() == "wiki", parts.Length > 1 ? parts[1] : "") is { } problem) chatGui.Print("[Tensho] " + problem);
            return;
        }
        Open();
    }
    private Dalamud.Game.Text.SeStringHandling.Payloads.DalamudLinkPayload? linkTestPayload;
    private void PrintLinkTest()
    {
        const uint item=12898; // Morel (Morille), the item of the 23:06 report.
        linkTestPayload??=chatGui.AddChatLinkHandler(uint.MaxValue-7,(_,text)=>log.Information("Link test clicked: Tensho link, line {0}.",text.TextValue));
        Dalamud.Game.Text.SeStringHandling.SeString Item(string name)=>new Dalamud.Game.Text.SeStringHandling.SeStringBuilder().AddText($"[Tensho] Test {name} : ").AddItemLink(item,false).Build();
        Dalamud.Game.Text.SeStringHandling.SeString Own(string name)=>new Dalamud.Game.Text.SeStringHandling.SeStringBuilder().AddText($"[Tensho] Test {name} : ").AddUiForeground(500)
            .Add(linkTestPayload).AddText("[Tensho]").Add(Dalamud.Game.Text.SeStringHandling.Payloads.RawPayload.LinkTerminator).AddUiForegroundOff().Build();
        chatGui.Print(Item("A"));
        chatGui.Print(new Dalamud.Game.Text.XivChatEntry{Type=Dalamud.Game.Text.XivChatType.Echo,Message=Item("B")});
        chatGui.Print(Own("C"));
        chatGui.Print(new Dalamud.Game.Text.XivChatEntry{Type=Dalamud.Game.Text.XivChatType.Echo,Message=Own("D")});
        log.Information("Link test printed: A item link (default type), B item link (Echo), C Tensho link (default type), D Tensho link (Echo).");
    }
    private void Open(){if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;mainWindow.IsOpen = true;}
    private void OpenConfig(){if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;mainWindow.OpenSettings();}
    private IChatGui? chatForCut;
    private UI.FontManager? fonts;
    private void Draw()
    {
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut){DrawBeastBoard(TenshoAssistant.Competitive.CompetitiveGate.LabyrinthBoard);return;}
        // 0.745: in developer mode, a slow frame is written to the log with its costliest parts.
        if(!mainWindow.DeveloperMode){DrawFrame();return;}
        var started=System.Diagnostics.Stopwatch.GetTimestamp();var paused=GC.GetTotalPauseDuration();
        try{DrawFrame();}
        finally
        {
            if(mainWindow.SlowFrames.Report(Dalamud.Bindings.ImGui.ImGui.GetFrameCount(),System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,(GC.GetTotalPauseDuration()-paused).TotalMilliseconds) is { } slow)
                log.Information(slow);
        }
    }
    private void DrawFrame()
    {
        var setupStarted=mainWindow.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp():0;
        fonts?.Apply(fontSettings() ?? new());
        if(commandInfo is not null && commandLocalizationRevision!=localization.Revision)
        {commandInfo.HelpMessage=localization.Get("Plugin.CommandHelp");commandLocalizationRevision=localization.Revision;}
        if(itemContextMenu?.Take() is { } request)mainWindow.OpenItemContext(request);
        mainWindow.ApplyTheme();
        // The map's open state lives in MainWindow (game map key M, journal); its window follows it.
        if(mapWindow is not null)mapWindow.IsOpen=mainWindow.MapViewOpen;
        mainWindow.KeepMapZoom();
        mainWindow.KeepCraftStats();
        mainWindow.LinkReminderAlarms();
        if(guideWindow is not null)guideWindow.IsOpen=mainWindow.GuideOpen;
        foreach(var panel in panelWindows)panel.Follow();
        // 0.747 (X-01): the parts the slow-frame line left « not measured ».
        if(setupStarted!=0)mainWindow.SlowFrames.Add(Dalamud.Bindings.ImGui.ImGui.GetFrameCount(),"setup",System.Diagnostics.Stopwatch.GetElapsedTime(setupStarted).TotalMilliseconds);
        overlays??=
        [
            ("Bank",()=>{if(!autoBankCapture())bankOverlay.Draw();}),
            ("CraftLog",mainWindow.DrawCraftLogHelper),
            ("MonsterIcons",mainWindow.DrawMonsterOverheadIcons),
            ("Discoveries",mainWindow.CollectDiscoveries),
            ("StorageHighlight",mainWindow.DrawStorageHighlight),
            ("ItemHover",mainWindow.DrawItemHover),
            ("UndercutClipboard",()=>undercut?.FlushClipboard()),
            ("Cactpot",mainWindow.DrawCactpotOverlay),
            ("CraftLive",mainWindow.DrawCraftLivePanel),
            ("InventorySort",mainWindow.DrawInventorySortButton),
            ("RegenTick",mainWindow.ObserveRegenTick),
            ("AlarmBar",()=>alarmBar?.Invoke()?.DrawTooltip()),
            ("ReminderBar",()=>reminderBar?.Invoke()?.DrawTooltip()),
            ("TriadOverlay",mainWindow.DrawTriadOverlay),
            ("TriadRequest",mainWindow.DrawTriadRequestPanel),
            ("TriadDeck",mainWindow.DrawTriadDeckReminder),
            ("Character",mainWindow.DrawCharacterPanel),
            ("DutyFinder",mainWindow.DrawDutyFinderPanel),
            ("FashionCheck",mainWindow.DrawFashionCheckPanel),
            ("Gathering",mainWindow.DrawGatheringPanel),
            ("Collectable",mainWindow.DrawCollectablePanel),
            ("RetainerSales",mainWindow.DrawRetainerSalesPanel),
            ("Spearfishing",mainWindow.DrawSpearfishingPanel),
            ("OceanVoyage",mainWindow.DrawOceanVoyagePanel),
            ("GameLogRequest",mainWindow.ProcessGameLogRequest),
            ("HuntTrain",mainWindow.ObserveHuntTrain),
            ("Garden",mainWindow.ObserveGarden),
            ("WishlistSales",mainWindow.WatchWishlistSales),
            ("PlotWatch",mainWindow.WatchPlots),
            ("Fishing",mainWindow.DrawFishingPanel),
        ];
        // R-14 (0.452): Dalamud calls this whole method inside one catch (UiBuilder.OnDraw): an overlay that threw (a sheet lost after a game
        // update throws on every call) replaced every Tensho window with Dalamud's error window. Each one now fails alone, logged once.
        for(var index=0;index<overlays.Length;index++)
        {
            var (name,draw)=overlays[index];
            if(brokenOverlays.Contains(name))continue;
            var started=mainWindow.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp():0;
            try{draw();}
            catch(Exception error){brokenOverlays.Add(name);log.Error(error,"Overlay {0} turned off until the plugin reloads (perhaps after a game update).",name);}
            // 0.486 (user report): the panels beside the game's windows count too, in developer mode.
            if(started!=0)mainWindow.RecordOverlayDraw(name,System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        // 0.516: the panels attached to a game window have asked above; all the windows are drawn after them.
        attachedPanels?.Apply();
        var windowsStarted=mainWindow.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp():0;
        windows.Draw();
        if(windowsStarted!=0)mainWindow.SlowFrames.Add(Dalamud.Bindings.ImGui.ImGui.GetFrameCount(),"all windows",System.Diagnostics.Stopwatch.GetElapsedTime(windowsStarted).TotalMilliseconds);
        attachedPanels?.EndFrame();
        DrawBeastBoard(true);
    }
    /// <summary>X-109 (0.981, DEC-047): the board's team panel when the game's board window is open (in a cut, only while the labyrinth's board is).</summary>
    private void DrawBeastBoard(bool allowed)
    {
        if(boardWindow is null)return;
        boardWindow.IsOpen=allowed && !boardPanelBroken && showBoardPanel() && boardReader?.State.Board==true;
        try{boardWindows.Draw();}
        catch(Exception error){boardWindow.IsOpen=false;boardPanelBroken=true;log.Error(error,"The Beastmaster board panel turned off until the plugin reloads.");}
    }
    private (string Name,Action Draw)[]? overlays;
    private T LoadSheet<T>(string name,Func<T> read,T unavailable)
    {
        try{return read();}
        catch(Exception error){log.Warning(error,"Sheet {0} unavailable (perhaps after a game update); its feature stays off.",name);return unavailable;}
    }
    private readonly HashSet<string> brokenOverlays=[];

    public void Dispose()
    {
        gameDataCancel.Cancel();
        try { registrations.Dispose(); }
        catch (AggregateException error) { log.Error(error, "Failed to release some Tensho registrations."); }
    }
}
