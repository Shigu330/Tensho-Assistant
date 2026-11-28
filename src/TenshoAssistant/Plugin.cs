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

public sealed partial class Plugin : IDalamudPlugin
{
    private const string Command = "/tensho";
    private readonly RegistrationScope registrations = new();
    // X-01 C1.3 (0.1187): this instance's parts, noted weakly at unload in developer mode (Diagnostics.UnloadWatch).
    private Dictionary<string,WeakReference>? unloadParts;
    private Func<bool> developerAtUnload=()=>false;
    private readonly IPluginLog log;
    // X-92 (0.972): the game data check after a game update, cancelled when the plugin unloads.
    private readonly CancellationTokenSource gameDataCancel=new();
    /// <summary>X-01 C5 (0.1206): when the map names are built off the frame, after the loading's own work (readers ready about 9 s after).</summary>
    internal static readonly TimeSpan MapMarkersWarmDelay=TimeSpan.FromSeconds(12);
    private readonly LocalizationManager localization;
    private readonly MainWindow mainWindow;
    private MapViewWindow? mapWindow;
    private GuideWindow? guideWindow;
    // X-119 step 2 (0.1081): the local link with the desktop application (off by default).
    private Link.LocalLinkFeed? linkFeed;
    // X-163 (0.1155): the alerts that rang in the game, sent to the desktop application by the link (only while it is on).
    private readonly Link.LinkNoticeLog linkNotices=new();
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
    // X-162 (0.1129): the currency and gil entries draw Tensho's tooltip too.
    private Func<Alarms.CurrencyDtrEntry?>? currencyBar;
    private Func<Inventory.GilDtrEntry?>? gilBar;
    // X-165 (0.1245): Tensho's state in the server bar (Plugin.ServerBar.cs).
    private Func<Alarms.StateDtrEntry?>? stateBar;
    private Func<Alarms.InfoDtrEntries?>? infoBar;
    private Market.UndercutWatcher? undercut;
    // 0.349 (g, X-07): the chat, for the « /tensho wiki | lodestone » answers.
    private readonly IChatGui chatGui;
    // X-539: a character logged in and loaded (UI.CharacterGate); Draw and /tensho wait for it.
    private readonly Func<bool> characterReady;
    private Diagnostics.SelfCheckRunner? selfCheck;
    // R-12 (0.414): items whose « Open » chat link handler is registered (command id = item id), all removed on unload.
    private readonly Dictionary<uint,Dalamud.Game.Text.SeStringHandling.Payloads.DalamudLinkPayload> alarmLinks=[];

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commands, IPluginLog log,
        IGameInventory inventory, IPlayerState player, IClientState client, IFramework framework, IDataManager data, IGameGui gameGui,
        ITextureProvider textures, IUnlockState unlocks, IGameInteropProvider interop,IObjectTable objects,IContextMenu contextMenus,IGameConfig gameConfig,ICondition condition,IFateTable fateTable,ITargetManager targets,IPartyList partyList,IChatGui chat,IDtrBar dtrBar,IKeyState keyState,IMarketBoard marketBoardEvents,ISeStringEvaluator seStrings,IAddonLifecycle addonLifecycle)
    {
        // 0.525 (user report: lag when the plugin loads): what the load costs, said in the log, to tell the plugin's own work from the runtime's pauses.
        var loadStarted=System.Diagnostics.Stopwatch.GetTimestamp();var pausesBefore=GC.GetTotalPauseDuration();var gen2Before=GC.CollectionCount(2);
        // X-433 (0.1447): what the process allocates and keeps while Tensho loads (developer mode, said two minutes after the load).
        var allocatedBefore=GC.GetTotalAllocatedBytes();var managedBefore=GC.GetTotalMemory(false);
        this.log = log;
        // 0.1105 (R20, plan 4.20): the version loaded, read by the agent's launch check (the agent's logread.py launch).
        log.Information("Tensho Assistant {0} loaded; logged in: {1}, territory {2}.",typeof(Plugin).Assembly.GetName().Version?.ToString(3) ?? "?",client.IsLoggedIn,client.TerritoryType);
        chatGui = chat;
        // X-539 (17/11): nothing drawn before a character is logged in (title screen, character selection), open state kept.
        characterReady=()=>UI.CharacterGate.Draws(client.IsLoggedIn,player.IsLoaded);
        localization = new LocalizationManager((error, message) => log.Warning(error, message), pluginInterface.AssemblyLocation.DirectoryName);
        dutyLoot = new DutyLootService(garland);
        monsterLoot = new MonsterLootService(garland);
        try
        {
            // DEC-038: decided first: no service starts in PvP or ranked content.
            chatForCut=chat;
            // X-01 C1.3 (0.1187): registered first, so run last at unload: the static hooks given below as closures (developer mode, slow
            // reader line, story weather) hold the constructor's locals, that is the whole instance; a static lives as long as the load
            // context, which stays alive in the game process after an unload (docs/evidence/0.1187-unload-roots.md). Emptied here, the
            // unloaded instance can be collected; a reader that forgot its own framework subscription is released and counted.
            registrations.Register(()=>{},()=>
            {
                var left=TenshoAssistant.Competitive.CompetitiveGate.ReleaseAll(framework);
                if(left>0)log.Warning("{0} framework subscriptions were still registered at unload: released.",left);
                TenshoAssistant.Competitive.CompetitiveGate.ForgetInstance();
                Gathering.WeatherCatalog.StoryWeatherConfirmed=null;
                // 0.1194: the root the measure named: the window's static cache of Raphael's sequences held the window and the data cache.
                MainWindow.ForgetInstance();
            });
            TenshoAssistant.Competitive.CompetitiveWatcher? competitive=null;
            registrations.Register(()=>competitive=new TenshoAssistant.Competitive.CompetitiveWatcher(framework,client,data,log,gameGui,condition),()=>competitive?.Dispose());
            registrations.Register(()=>{},()=>dataCache?.Dispose());
            registrations.Register(() => { }, market.Dispose);
            registrations.Register(() => { }, marketBoard.Dispose);
            registrations.Register(() => { }, garland.Dispose);
            registrations.Register(() => { }, dutyLoot.Dispose);
            registrations.Register(() => { }, monsterLoot.Dispose);
            registrations.Register(() => { }, fashionData.Dispose);
            // X-685 part 2 (0.1745, D-206 a): Tensho's own settings file (pluginConfigs\TenshoAssistant\Settings.json, System.Text.Json), no
            // longer Dalamud's (Newtonsoft kept every unloaded copy of Tensho in the game process); Dalamud's file read once, then left as it was.
            var settingsFile=new SettingsFile<Configuration>(SettingsFile<Configuration>.In(pluginInterface.GetPluginConfigDirectory()),pluginInterface.ConfigFile.FullName,
                (error,message)=>{if(error is null)log.Warning(message);else log.Warning(error,message);});
            var configuration=settingsFile.Load(out var settingsOrigin);
            var newInstall=settingsOrigin==SettingsOrigin.New;
            var keptAside=settingsFile.KeptAside;
            if(settingsOrigin is SettingsOrigin.Legacy)log.Information("Settings moved from Dalamud's settings file to Tensho's own (Settings.json), once; Dalamud's file is left as it was.");
            TenshoAssistant.Competitive.CompetitiveGate.Developer=()=>configuration.DeveloperMode;
            // X-177 C4.2 (0.1200): one thread compiles at a time, in the order of the need: the readers' classes wait until the window
            // exists and its warm-up has compiled the interface's code (bounded by ReaderCompiler.MaxHold; dropped at unload).
            TenshoAssistant.Competitive.CompetitiveGate.Compiler.Hold=()=>mainWindow is not { WarmupEarlyDone:true };
            // X-437 M2 (0.1428): the readers' compiler times its compilations for the log in developer mode only.
            TenshoAssistant.Competitive.CompetitiveGate.Compiler.Measure=static ()=>TenshoAssistant.Competitive.CompetitiveGate.Developer?.Invoke()==true;
            TenshoAssistant.Competitive.CompetitiveGate.SlowSubscription=(name,spent,worst)=>
            {
                log.Information("Slow reader: {0} took {1:0} ms in one frame (worst {2:0} ms; named at most every 5 minutes).",name,spent,worst);
                // X-433 (0.1440): while the runtime is listened to (developer mode, two minutes after a load), the methods the game's thread
                // compiled during that pass, read a moment later (the runtime's events come in a little after).
                if(Diagnostics.GameThreadJit.Current is { } jit)
                {
                    var to=DateTime.UtcNow;
                    var from=to-TimeSpan.FromMilliseconds(spent+5);
                    _=Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_=>
                    {
                        var names=jit.Between(from,to);
                        if(names.Count>0)log.Information("Slow reader: methods the game's thread compiled meanwhile ({0}): {1}.",names.Count,string.Join(", ",names.Take(40)));
                    },TaskScheduler.Default);
                }
            };
            // X-177 (0.1162): a save of the settings made on the framework thread is timed; over 50 ms it is named in the log with what saved
            // (AutoAlarmWatcher took 648 ms in one frame on 12/11 at 11:15, its work being a save of the whole settings), at most every 5 minutes.
            var saveNotes=new Diagnostics.SlowNotes(TimeSpan.FromMinutes(5));
            // X-177 step 2 (0.1164): Dalamud's save is the cost (64 to 71 ms each time, 371 to 648 ms the first time after a load; the text
            // itself about 2 ms once warm): only the text is made here, a checked copy is written off the game's thread (SettingsSaver).
            // X-685 part 2 (0.1745): the text is System.Text.Json's (SettingsJson), the copy written to Tensho's own file, merged with it.
            var saver=new SettingsSaver<Configuration>(configuration,
                live=>SettingsJson.Write(live),
                text=>SettingsJson.Read<Configuration>(text),
                copy=>settingsFile.Save(copy,framework.IsInFrameworkUpdateThread),
                action=>_=framework.RunOnFrameworkThread(action),
                (error,message)=>{if(error is null)log.Warning(message);else log.Warning(error,message);});
            registrations.Register(()=>_=saver.Warm(()=>File.Exists(settingsFile.FilePath) ? File.ReadAllText(settingsFile.FilePath) : null),
                ()=>
                {
                    // X-764 M1 (review of 0.1751): from here every save is the last one: the full wait for another client's lock on any
                    // thread (the game's tenth of a second refused it before, and no save followed), its failure said as a loss.
                    settingsFile.Close();
                    if(saver.Flush(TimeSpan.FromSeconds(5)))
                    {
                        // A write of the worker that failed just before (said « written at the next save »): the next save is this one (the saver,
                        // closed by Flush, saves the live settings at once).
                        if(settingsFile.Failing && settingsFile.ClosingSave is null)saver.Save();
                        return;
                    }
                    // X-184 N22 (0.1207): the live settings were handed to the writer: waited for, and said as it is.
                    if(saver.Settle(TimeSpan.FromSeconds(15)))log.Warning(settingsFile.ClosingSave==false ? "The last settings save took more than 5 s at unload and failed (said above)." : "The last settings save took more than 5 s at unload; the settings were written once it ended.");
                    else log.Warning("The settings were still being written 20 s after the unload began; the last ones may not be on disk.");
                });
            void SaveTimed(string what)
            {
                var started=System.Diagnostics.Stopwatch.GetTimestamp();
                saver.Save();
                var spent=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                if(saveNotes.Note(what,spent,Environment.TickCount64) is { } worst)
                    log.Information("Slow settings save: {0} took {1:0} ms (worst {2:0} ms; named at most every 5 minutes).",what,spent,worst);
            }
            autoBankCapture = () => configuration.AutoBankCapture;
            fontSettings = () => configuration.Fonts;
            if (configuration.Version != 1)
                throw new NotSupportedException($"Unsupported configuration version: {configuration.Version}");
            // 0.750 (X-02): once, the longer cache defaults; saved with the other settings at their next save.
            if (!configuration.CacheLongerDefaults) { configuration.Cache.LongerDefaults(); configuration.CacheLongerDefaults = true; }
            dataCache=new DataCache(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"DataCache-v1"));
            // X-117 (0.1046): what a game patch changed in the catalogue copies, written once to the log when first seen, then at each load.
            dataCache.PatchReported=report=>log.Information("Patch changes: {0}",PatchCatalogDiff.Describe(report));
            _=dataCache.LoadPatchReportsAsync().ContinueWith(task=>
            {
                if(task.IsCompletedSuccessfully)foreach(var report in task.Result)log.Information("Patch changes (kept): {0}",PatchCatalogDiff.Describe(report));
            },TaskScheduler.Default);
            market.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Prices),dataCache);
            marketBoard.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Prices),dataCache);
            garland.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache);
            // 0.324: each source switch stops every request of its source (it used to cover part of it only).
            // X-834: the desktop application sets the same switches from CommunitySources.SwitchFor (SourceSwitches1839Tests keeps both alike).
            garland.Enabled=()=>configuration.CommunityGarland;
            market.Enabled=()=>configuration.CommunityUniversalis;
            // X-815 (0.1847): the items the market sells (tradable and in a market search category, the item catalog's IsMarketable rule);
            // the others are not asked to Universalis, which answers 400 to a list of them. An item the sheet does not have is asked.
            var marketItems=data.GetExcelSheet<Lumina.Excel.Sheets.Item>();
            market.Marketable=id=>marketItems.GetRowOrDefault(id) is not { } row || (!row.IsUntradable && row.ItemSearchCategory.RowId!=0);
            // X-848 M2: the line of a list refused by answers 400, in the log.
            market.Trace=line=>log.Information(line);
            // X-122, D-101: duty and monster loot follow their own use of Garland Tools too.
            dutyLoot.Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.GarlandDutyLoot);
            monsterLoot.Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.GarlandMonsterLoot);
            marketBoard.Enabled=()=>configuration.CommunityUniversalis;
            // X-73.15 (0.871): the region of each data centre from the game sheet, so that Universalis' table is asked only for what the game does not name.
            marketBoard.GameRegions=data.GetExcelSheet<Lumina.Excel.Sheets.WorldDCGroupType>().Where(row=>row.Name.ExtractText().Length>0 && row.Region.RowId!=0)
                .GroupBy(row=>row.Name.ExtractText(),StringComparer.OrdinalIgnoreCase).ToDictionary(group=>group.Key,group=>group.First().Region.RowId,StringComparer.OrdinalIgnoreCase);
            fashionData.ConfigureCache(()=>configuration.Cache.Duration(CacheArea.Fashion),dataCache);
            fashionData.Enabled=()=>configuration.CommunityFashion;
            // X-153 (0.1893): the market family (world reader, prices seen, undercut, retainer market) in Plugin.Market.cs.
            RegisterMarketWorld(framework,player,client,data);
            // X-173 (0.1140): the player's data file, one for every family moved into it (gil journal, achievement progress, voyage history,
            // bank), with the bank and its bridge to the game's inventory: the persistence family (X-153, 0.1239: Plugin.Persistence.cs).
            var (tenshoData,bank,bankBridge)=RegisterPersistence(pluginInterface,configuration,data,inventory,gameGui,player,client,framework);
            bankOverlay = new BankImportOverlay(bank, bankBridge, localization);
            localization.SelectInitialLanguage(configuration.LanguageCode,client.ClientLanguage.ToString());
            mainWindow = new MainWindow(configuration, () => SaveTimed("window"), log, localization,
                inventoryService, itemCatalog, recipeCatalog, gatheringCatalog, collections, market, itemHover, quests, characterStats, maps, macroObservations, macroChain, triadObservation, bank, bankBridge, textures, gameGui, duties, dutyLoot, monsterLoot,gpActions,fashionHints,fashionData,craftActions,craftLive,weather,cactpotObservation,gatheringObservation,itemUses,visualSort) { IsOpen = configuration.OpenOnLoad };
            registrations.Register(() => { }, mainWindow.Dispose);
            mainWindow.TenshoData=tenshoData;   // X-789 (0.1802): its « json » mode said by a banner
            mainWindow.ConfigureDataCache(dataCache);
            // X-153 (0.1934): the game's theme reader in Plugin.GameUi.cs.
            RegisterGameTheme(gameConfig);
            // X-155 (0.1260): Dalamud's « reduced motion » preference, for Tensho's brief highlights.
            mainWindow.ConfigureMotion(()=>pluginInterface.UiBuilder.ShouldUseReducedMotion);
            var wikiSources=new WikiMonsterSources(id=>itemCatalog.State.Catalog?.Find(id)?.EnglishName,()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.WikiMonsters)};
            registrations.Register(()=>{},wikiSources.Dispose);
            mainWindow.ConfigureWikiSources(wikiSources);
            // X-90 (0.994): duty guides from the wiki, by the duty's English name (ContentFinderCondition, English sheet).
            var dutyGuides=new Duties.WikiDutyGuides(id=>data.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>(Dalamud.Game.ClientLanguage.English).GetRowOrDefault(id)?.Name.ExtractText() is { Length:>0 } english
                ? char.ToUpperInvariant(english[0])+english[1..]:null,()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.WikiDutyGuides)};
            registrations.Register(()=>{},dutyGuides.Dispose);
            mainWindow.DutyGuides=dutyGuides;
            mainWindow.InCombatNow=()=>condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            // X-447 D8 (0.1620): the chat window hidden in a cutscene (those that keep the interface too) or in a duty, when chosen.
            mainWindow.InCutsceneNow=()=>condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene]
                || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene78] || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInCutSceneEvent];
            mainWindow.InDutyNow=()=>condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
            // X-447 B13 (0.1655): the chat window's contexts: crafting (the game's crafting states), a city (the zone's TerritoryIntendedUse 0,
            // the game's 19 cities: docs/evidence/0.1655-chat-context-cities.md), read once per zone; a row not read is no city (R08).
            mainWindow.InCraftingNow=()=>condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Crafting] || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.PreparingToCraft];
            uint townTerritory=uint.MaxValue;var inTown=false;
            mainWindow.InTownNow=()=>
            {
                uint territory=client.TerritoryType;
                if(territory==townTerritory)return inTown;
                townTerritory=territory;
                try{inTown=territory!=0 && data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(territory) is { } row && row.TerritoryIntendedUse.RowId==Chat.ChatContexts.TownUse;}
                catch(Exception){inTown=false;}
                return inTown;
            };
            // X-474 (0.1477): a link of Dalamud or of a plugin in Tensho's chat window: the handler Dalamud keeps for it (RegisteredLinkHandlers,
            // the same one a click in the game's chat calls), run on the game's thread on the player's click. X-496 M1 (0.1478): its message
            // is the link's own string (from the link to its end), as Dalamud passes it (docs/evidence/0.1478-dalamud-link-handler.md); a
            // failure is said on the link's tooltip, not only in the log.
            mainWindow.DalamudLinkKnown=(plugin,command)=>chatGui.RegisteredLinkHandlers.ContainsKey((plugin,command));
            // X-484 (0.1482): the game's log filters, in the client's language (docs/evidence/0.1482-chat-log-filters.md).
            mainWindow.ChatFilterSource=()=>data.GetExcelSheet<Lumina.Excel.Sheets.LogFilter>()
                .Select(row=>new Chat.ChatFilterRow(row.RowId,row.LogKind,row.Caster,row.Target,row.Category,row.DisplayOrder,row.Preset,row.Name.ExtractText(),row.Example.ExtractText())).ToList();
            // X-484 (0.1777): the names of Teleport and Return in the client's language (Action rows 5 and 6), for the tabs that hide others' teleports.
            try{Chat.ChatTravel.Names=Chat.ChatTravel.From(Chat.ChatTravel.Actions.Select(id=>data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id)?.Name.ExtractText()));}
            catch(Exception error){log.Warning(error,"Chat window: the names of Teleport and Return could not be read (maybe after a game update): others' teleports are not hidden.");}
            mainWindow.OpenDalamudLink=(plugin,command,data,text)=>
            {
                if(!chatGui.RegisteredLinkHandlers.TryGetValue((plugin,command),out var handler))return false;
                _=framework.RunOnFrameworkThread(()=>
                {
                    // X-498 M2 (0.1480): re-reading the link's string is Tensho's part: a failure there is logged as Tensho's, and the handler
                    // gets the link's text instead; only an error of the handler itself is said to be the plugin's.
                    var message=new Dalamud.Game.Text.SeStringHandling.SeString(new Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload(text));
                    if(data is { Length:>0 })
                        try{message=Dalamud.Game.Text.SeStringHandling.SeString.Parse(data);}
                        catch(Exception error){log.Warning(error,"Tensho could not re-read a link of the chat window ({0}): its text is passed instead.",plugin);}
                    try{handler(command,message);}
                    catch(Exception error){mainWindow.DalamudLinkFailed=plugin;log.Warning(error,"A link of the chat window could not be opened by its plugin ({0}).",plugin);}
                });
                return true;
            };
            // g, C-13 (0.367): monsters that teach a blue magic spell, from the wiki's spell page (English action name), on click only.
            // 0.387 (user report « never gives the monster »): the AozAction → Action link resolved in the client's language, so the wiki
            // was asked for the French page (« Mauvaise_haleine »: missing) and said « no monster ». The name now comes from the English
            // Action sheet by row; family « -2 » drops the empty answers cached under the wrong title.
            var blueWiki=new WikiMonsterSources(id=>data.GetExcelSheet<Lumina.Excel.Sheets.AozAction>().GetRowOrDefault(id) is { Action.RowId:not 0 } aoz
                    ? data.GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.English).GetRowOrDefault(aoz.Action.RowId)?.Name.ExtractText():null,
                ()=>configuration.Cache.Duration(CacheArea.Sources),dataCache,family:"wiki-blue-spells-2"){Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.WikiBlueMagic)};
            registrations.Register(()=>{},blueWiki.Dispose);
            mainWindow.BlueSpellWiki=blueWiki;
            // 0.798 (X-38): the monsters that teach each spell, for the nearby monsters; English monster names matched to BNpcName rows once, off the frame.
            var englishMonsters=new Lazy<IReadOnlyDictionary<string,uint[]>>(()=>data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>(Dalamud.Game.ClientLanguage.English)
                .Select(row=>(Name:row.Singular.ExtractText().Trim(),Id:row.RowId)).Where(row=>row.Name.Length>0)
                .GroupBy(row=>row.Name,StringComparer.OrdinalIgnoreCase).ToDictionary(group=>group.Key,group=>group.Select(row=>row.Id).ToArray(),StringComparer.OrdinalIgnoreCase));
            var blueNearby=new Monsters.BlueSpellMonsterIndex(blueWiki,name=>englishMonsters.Value.TryGetValue(name,out var ids) ? ids:[],()=>CommunitySources.Allowed(configuration,SourceUse.WikiBlueMagic));
            registrations.Register(()=>{},blueNearby.Dispose);
            mainWindow.BlueSpellNearby=blueNearby;
            // 0.804 (X-39): the same English names for the Beastmaster's wiki bestiary.
            mainWindow.EnglishMonsterIds=name=>englishMonsters.Value.TryGetValue(name,out var ids) ? ids:[];
            var beastWiki=new Monsters.BeastWikiIndex(dataCache,()=>CommunitySources.Allowed(configuration,SourceUse.WikiBeastmaster));
            registrations.Register(()=>{},beastWiki.Dispose);
            mainWindow.BeastWiki=beastWiki;
            // X-153 (0.1897): the inventory family (storage highlight, inventory window, gear sets) in Plugin.Inventory.cs.
            RegisterStorageHighlight(configuration,framework,gameGui,inventory);
            // X-43 (0.807): the local base of discoveries; X-153 (0.1250): its family in Plugin.Discoveries.cs (store, GitHub exchange, loading).
            var (discoveries,discoveryFolder)=RegisterDiscoveries(pluginInterface);
            mainWindow.MonsterName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>().GetRowOrDefault(id)?.Singular.ExtractText() ?? "";
            mainWindow.FateName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Fate>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";
            mainWindow.AirshipSectorName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.AirshipExplorationPoint>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";
            mainWindow.SectorName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.SubmarineExploration>().GetRowOrDefault(id)?.Destination.ExtractText() ?? "";
            mainWindow.DutyName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";
            // X-262 (0.1308): deep dungeons and other instances with the game's own map: Tensho's map gives way.
            mainWindow.InOwnMapInstance=()=>Competitive.CompetitiveGate.HasOwnMap(client.TerritoryType);
            // X-266 (0.1307): a vendor placed only by a quest's layer shows on the map while that quest is under way or done.
            var questRows=new Lazy<Lumina.Excel.ExcelSheet<Lumina.Excel.Sheets.Quest>>(()=>data.GetExcelSheet<Lumina.Excel.Sheets.Quest>());
            mainWindow.QuestSeen=id=>Quests.QuestReader.SeenNow(client,player,unlocks,questRows.Value,id);
            // X-263 (0.1305): a retainer venture's name (exploration) or class and level (targeted), from RetainerTask.
            // X-332 (0.1327, review of 0.1318 I3, R07): every venture read once, off the frame (RetainerTask, RetainerTaskRandom and the ClassJob
            // names, which were read again at each frame for each row); until the table is built a venture shows its number.
            var ventures=new Caching.BackgroundTable<uint,Discoveries.VentureRow>(()=>
            {
                var jobs=data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().Select(row=>(row.Abbreviation.ExtractText(),row.Name.ExtractText())).ToArray();
                var randoms=data.GetExcelSheet<Lumina.Excel.Sheets.RetainerTaskRandom>();
                var rows=new Dictionary<uint,Discoveries.VentureRow>();
                foreach(var task in data.GetExcelSheet<Lumina.Excel.Sheets.RetainerTask>())
                {
                    var random=task.IsRandom ? randoms.GetRowOrDefault(task.Task.RowId)?.Name.ExtractText() ?? "":"";
                    var job=task.IsRandom ? "":Discoveries.VentureNames.JobName(task.ClassJobCategory.ValueNullable?.Name.ExtractText() ?? "",jobs);
                    rows[task.RowId]=new Discoveries.VentureRow(task.IsRandom,random,job,task.RetainerLevel);
                }
                return rows;
            },error=>log.Warning(error,"Venture sheets unreadable (game update?)."));
            mainWindow.VentureRowOf=id=>ventures.TryGet(id,out var row) ? row:null;
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
            RegisterSeenPrices(configuration,marketBoardEvents,seenPrices,bankBridge);
            // X-153 (0.1250): the prices seen, then the discoveries, read off the frame (Plugin.Discoveries.cs); nothing is written before it is read.
            LoadDiscoveries(discoveryFolder,discoveries,seenPrices,framework,chat);
            // 0.395 (user request): the wiki's spellbook table (one page for every spell) read when the Blue magic page opens, copy first.
            var blueIndex=new BlueSpellWikiIndex(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache,()=>CommunitySources.Allowed(configuration,SourceUse.WikiBlueMagic));
            registrations.Register(()=>{},blueIndex.Dispose);
            mainWindow.BlueSpellIndex=blueIndex;
            mainWindow.GameNames=new Items.GameNames(data,log);
            Dictionary<string,IReadOnlyList<uint>>? englishPlaces=null;
            mainWindow.EnglishPlaceIds=name=>(englishPlaces??=data.GetExcelSheet<Lumina.Excel.Sheets.PlaceName>(Dalamud.Game.ClientLanguage.English)
                .Where(row=>!row.Name.IsEmpty).GroupBy(row=>row.Name.ExtractText(),StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group=>group.Key,group=>(IReadOnlyList<uint>)group.Select(row=>row.RowId).ToArray(),StringComparer.OrdinalIgnoreCase)).GetValueOrDefault(name.Trim()) ?? [];
            // X-153 (0.1404, seventh slice): the map family (Plugin.Maps.cs), at the same place as before.
            RegisterMaps(framework,client,objects,data,condition,fateTable,partyList);
            var itemSources=new Community.GarlandItemSourceService(garland){Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.GarlandItemSources)};
            registrations.Register(()=>{},itemSources.Dispose);
            mainWindow.ConfigureItemSources(itemSources);
            var wikiItems=new Community.WikiItemAcquisitionService(id=>itemCatalog.State.Catalog?.Find(id)?.EnglishName,()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.WikiItemAcquisition)};
            registrations.Register(()=>{},wikiItems.Dispose);
            mainWindow.ConfigureWikiItems(wikiItems);
            var wikiVistas=new Maps.WikiVistaSource(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.WikiVistas)};
            registrations.Register(()=>{},wikiVistas.Dispose);
            mainWindow.ConfigureWikiVistas(wikiVistas);
            // W-04 (0.685): the island's animals on the wiki, on the player's click only.
            var islandAnimalBook=new Island.IslandAnimalSource(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.WikiIslandAnimals)};
            registrations.Register(()=>{},islandAnimalBook.Dispose);
            mainWindow.IslandAnimalBookSource=islandAnimalBook;
            // X-153 (0.1900): the rest of the map family (game map replacement, housing probe, treasure map) in Plugin.Maps.cs.
            RegisterGameMap(configuration,framework,gameGui,client,condition);
            // X-153 (0.1933): the macro writer in Plugin.Macros.cs.
            RegisterMacroWriter(framework,chat);
            // X-153 (0.1932): the alarm service and its Discord notifier in Plugin.Alarms.cs; the notifier is handed back (currencies, duty pop).
            var discord=RegisterAlarms(configuration,framework,condition,chat,objects,()=>SaveTimed("alarms"));
            // X-153 (0.1412, eighth slice): the gathering and fishing family (Plugin.Gathering.cs), at the same place as before.
            RegisterGathering(pluginInterface,configuration,data,framework,client,objects,condition);
            mainWindow.ConfigureJobNames(job=>data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().TryGetRow(job,out var row) ? row.Name.ExtractText():null);
            var lumina=new Community.LuminaSupplementalService(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>().TryGetRow(id,out var monster) ? monster.Singular.ExtractText():null,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.Fate>().TryGetRow(id,out var fate) ? fate.Name.ExtractText():null,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.ENpcResident>().TryGetRow(id,out var npc) ? npc.Singular.ExtractText():null,
                id=>data.GetExcelSheet<Lumina.Excel.Sheets.Quest>().TryGetRow(id,out var quest) ? quest.Name.ExtractText():null){Enabled=()=>configuration.CommunityLumina};
            registrations.Register(()=>{},lumina.Dispose);
            mainWindow.ConfigureLumina(lumina);
            // X-153 (0.1899): the duties family (Duty Finder, duty catalogue, duty progress) in Plugin.Duties.cs.
            RegisterDutyFinder(configuration,framework,gameGui);
            // X-153 (0.1927): the Triple Triad NPCs' loader in Plugin.MiniGames.cs.
            RegisterTriadNpcs(data,framework,client,player,unlocks);
            // 0.500 (user request): the Beastmaster page; X-153 (0.1886): its family in Plugin.Beastmaster.cs.
            var beasts=RegisterBeastCatalog(data,framework,client,player);
            // X-73.9 (0.863): spells learned and beasts tamed, put down to their monster, for the discoveries base.
            if(itemWatcher is not null){itemWatcher.Beasts=()=>beasts.State.Obtained;itemWatcher.WatchUnlocks(unlocks);itemWatcher.Vistas=()=>mapLive?.State?.Sightseen;
                var blueActions=data.GetExcelSheet<Lumina.Excel.Sheets.AozAction>().Where(row=>row.Action.RowId!=0).GroupBy(row=>row.Action.RowId).ToDictionary(group=>group.Key,group=>group.First().RowId);
                itemWatcher.BlueSpellOfAction=action=>blueActions.GetValueOrDefault(action);
                // X-96 (0.959): the check uses the standard table even where the zone has a story weather, and records whether the game agrees there.
                itemWatcher.ForecastWeather=(territory,at)=>weather.State.Catalog?.Forecast(territory,at,1,true) is { Count:>0 } current ? current[0].Weather?.Id ?? 0:0;
                // 0.1102: an airship sector unlocked (log message 4163), kept only when the sheet names that row.
                itemWatcher.AirshipPointUnlocked=point=>
                {
                    if(configuration.Discovery.Enabled && configuration.Discovery.ItemSources
                       && data.GetExcelSheet<Lumina.Excel.Sheets.AirshipExplorationPoint>().GetRowOrDefault(point) is { } row && !row.Name.IsEmpty)
                        mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.AirshipUnlock,point,0,1,DateTimeOffset.UtcNow);
                };
                itemWatcher.WeatherChecked=(territory,matched)=>
                {
                    if(weather.State.Catalog?.Zone(territory) is not { Individual:true })return;
                    var zones=configuration.WeatherStoryZones ??= [];
                    if(matched==zones.Contains(territory))return;
                    if(matched)zones.Add(territory);else zones.Remove(territory);
                    try{saver.Save();}catch(Exception error){log.Warning(error,"The confirmed story weather zones could not be saved.");}
                };
                Gathering.WeatherCatalog.StoryWeatherConfirmed=territory=>configuration.WeatherStoryZones?.Contains(territory)==true;
                itemWatcher.PreviousWeather=(territory,at)=>weather.State.Catalog?.Forecast(territory,at.AddSeconds(-1400),1) is { Count:>0 } before ? before[0].Weather?.Id ?? 0:0;}
            mainWindow.BeastName=id=>beasts.State.Catalog?.Beasts.FirstOrDefault(beast=>beast.Id==id)?.Name ?? "";
            mainWindow.SpellName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.AozAction>().GetRowOrDefault(id) is { } spell ? data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(spell.Action.RowId)?.Name.ExtractText() ?? "":"";
            mainWindow.GuideNewInstall=newInstall;
            // X-761 (review of 0.1746, I1): the settings file held by another program at load: said in the chat (now, or at the next login),
            // and once more when it can be read again; this session writes nothing to it (SettingsFile.Unseen).
            var settingsNotices=new List<string>();
            void SettingsNotice(string key,params object[] values)=>_=framework.RunOnFrameworkThread(()=>
            {
                var text=values.Length==0 ? localization.Get(key):localization.Format(key,values);
                if(client.IsLoggedIn)chat.Print("[Tensho] "+text);else settingsNotices.Add(text);
            });
            Action settingsNoticeOnLogin=()=>{foreach(var text in settingsNotices)chat.Print("[Tensho] "+text);settingsNotices.Clear();};
            registrations.Register(()=>client.Login+=settingsNoticeOnLogin,()=>client.Login-=settingsNoticeOnLogin);
            if(settingsOrigin is SettingsOrigin.Held or SettingsOrigin.HeldBackup)SettingsNotice(settingsOrigin==SettingsOrigin.HeldBackup ? "Plugin.SettingsHeldBackup" : "Plugin.SettingsHeld");
            settingsFile.FreeAgain=()=>SettingsNotice("Plugin.SettingsFreeAgain");
            // X-763 M1 (review of 0.1746): a settings file that could not be read at load, kept aside: said in the chat, with what was read
            // instead (the previous copy, Dalamud's older file, or new settings).
            var unreadableKey=settingsOrigin switch{SettingsOrigin.Backup when keptAside is not null=>"Plugin.SettingsUnreadableBackup",SettingsOrigin.LegacyAfterUnreadable=>"Plugin.SettingsUnreadableLegacy",SettingsOrigin.NewAfterUnreadable=>"Plugin.SettingsUnreadableNew",_=>null};
            if(unreadableKey is not null)SettingsNotice(unreadableKey,keptAside ?? "?");
            // X-765 M4 (review of 0.1751): its previous copy unreadable too, kept aside under its own name (no longer written over).
            if(settingsFile.BackupKeptAside is { } backupAside)SettingsNotice("Plugin.SettingsBackupKeptAside",backupAside);
            // 0.508 (I-05): the retainer sale list; X-153 (0.1887): its family (and the market tax rates) in Plugin.Market.cs.
            var retainerSales=RegisterRetainerMarket(configuration,framework,gameGui,client,player,marketBoardEvents);
            Help.CommunityHelpIndex? communityHelp=null;
            registrations.Register(()=>mainWindow.CommunityHelp=communityHelp=new Help.CommunityHelpIndex(),()=>communityHelp?.Dispose());
            RegisterHousingProbe(configuration,framework,client,objects);
            RegisterBeastBoard(configuration,data,framework,gameGui,objects);
            // X-153 (0.1894): the game chat's layout probe and its hider, in the chat family (Plugin.Chat.cs).
            RegisterGameChat(configuration,framework,gameGui);
            // 0.494 (user request): the self-check, on its own in developer mode, on request otherwise.
            // X-153 (0.1901): the diagnostics family (self-check, reading capture) in Plugin.Diagnostics.cs.
            RegisterSelfCheck(pluginInterface,configuration,data,framework,client,player);
            mainWindow.ConfigureMarketBoard(marketBoard);
            // X-153 (0.1898): the currency watcher and its alerts, in the inventory family (Plugin.Inventory.cs); the server bar reads it.
            var currencies=RegisterCurrencies(configuration,framework,client,player,data,condition,chat,objects,discord);
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
                    if(options.ChatAllowed(inDuty) || options.SoundAllowed(inDuty))linkNotices.Add(message);
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
            // X-153 (0.1403, sixth slice): the company voyages family (Plugin.Voyages.cs), at the same place as before.
            RegisterVoyages(pluginInterface,configuration,data,framework,client,tenshoData,()=>SaveTimed("voyages"));
            // X-153 (0.1888): the gear family (gear loader, the game's gear sets and job levels, the manuals' Grace) in Plugin.Gear.cs.
            var (gear,gameGearsets,graceWatcher)=RegisterGear(configuration,data,framework,client,player,objects,chat,()=>SaveTimed("grace"));
            // X-153 (0.1895): the field content family (shared FATE ranks, Occult Crescent) in Plugin.Fields.cs.
            RegisterSharedFates(configuration,framework,client,player,gameGui,()=>SaveTimed("shared FATEs"));
            mainWindow.GraceSource=()=>graceWatcher?.Shown;
            // X-153 (0.1928): the desynthesis of an item and the player's desynthesis levels in Plugin.Items.cs.
            RegisterDesynthesis(data,framework,client);
            // X-223 (0.1233): the region of the home world's data centre (WorldRegionGroup row), for the Jumbo Cactpot's draw time.
            mainWindow.HomeRegion=()=>player.HomeWorld.ValueNullable?.DataCenter.ValueNullable?.Region.RowId ?? 0;
            // X-153 (0.1896): the hunting family (S rank kills, hunting log) in Plugin.Hunting.cs.
            RegisterSRankKills(configuration,framework,client,objects,chat,()=>SaveTimed("S rank kills"));
            // X-218 (0.1228): each character's job levels and gear sets kept in TenshoData.db at login and when they change (Storage › Characters).
            var characterJobs=new Character.CharacterJobsData(tenshoData);
            mainWindow.CharacterJobs=characterJobs;
            var characterJobsWriter=new BackgroundWriter((error,message)=>{log.Warning(error,message);mainWindow.NoteWriteFailed(message);});
            Character.CharacterJobs? lastCharacterJobs=null;
            if(gameGearsets is not null)gameGearsets.Copied=read=>
            {
                var id=player.ContentId;
                if(id==0)return;
                var jobs=new Character.CharacterJobs(id,DateTimeOffset.UtcNow,new Dictionary<uint,int>(read.JobLevels),
                    read.Sets.Select(set=>new Character.SavedGearset(set.Number,set.Name.Length>Character.CharacterJobsData.MaxName ? set.Name[..Character.CharacterJobsData.MaxName]:set.Name,set.Job,set.ItemLevel)).ToArray());
                if(jobs.SameContent(lastCharacterJobs))return;
                lastCharacterJobs=jobs;
                characterJobsWriter.Enqueue("The job levels and gear sets of the character could not be saved.",()=>{if(characterJobs.Save(jobs))mainWindow.CharacterJobsChanged();});
            };
            // 0.650 (phase 5): the in-game readings, in developer mode only. Temporary.
            RegisterReadingCapture(configuration,framework,objects,condition,gameGui,chat);
            // X-153 (0.1938): the GP tick (tick bar, GP alert) in Plugin.GpTick.cs; the gathering pages read the reader handed back.
            var regenTick=RegisterGpTick(configuration,framework,objects,condition,chat);
            mainWindow.RegenTick=()=>regenTick?.State;
            // g, R-01 (0.392): ocean fishing timetable from the game's loop (IKDRouteTable) and voyages (IKDRoute), built on first use.
            // X-285 (0.1300): the page loaders that read only the game's sheets, given by one method to the game and the desktop application.
            mainWindow.UseSheetLoaders(data,log);
            // X-153 (0.1417, ninth slice): the ocean fishing family (Plugin.Ocean.cs), at the same place as before.
            RegisterOcean(pluginInterface,configuration,data,framework,client,condition);
            RegisterInventoryWindow(configuration,framework,gameGui);
            // g, M-06 (0.376): repair and materia extraction alert (chat and sound with the alarm options), once per crossing.
            Gear.GearWearWatcher? gearWear=null;
            registrations.Register(()=>gearWear=new Gear.GearWearWatcher(framework,client,()=>gear?.Equipped ?? [],()=>configuration.GearWear ?? new(),log){Crossed=(repair,extract,state)=>
            {
                var options=configuration.AlarmOptions ?? new();
                var inDuty=options.MuteInDuties && condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
                var repairText=repair ? localization.Format("GearWear.AlertRepair",state.ToRepair,state.LowestCondition ?? 0):null;
                var extractText=extract ? localization.Format("GearWear.AlertExtract",state.ToExtract):null;
                if(options.ChatAllowed(inDuty))
                {
                    if(repairText is not null)chat.Print("[Tensho] "+repairText);
                    if(extractText is not null)chat.Print("[Tensho] "+extractText);
                }
                if(options.SoundAllowed(inDuty))Alarms.AlarmService.PlaySound(options.Sound,log);
                if(options.ChatAllowed(inDuty) || options.SoundAllowed(inDuty))
                {
                    if(repairText is not null)linkNotices.Add(repairText);
                    if(extractText is not null)linkNotices.Add(extractText);
                }
            }},()=>gearWear?.Dispose());
            // X-153 (0.1936): the preparation reminder (meal, chocobo companion) in Plugin.Reminders.cs.
            RegisterPrepReminders(configuration,data,framework,client,objects,condition,chat);
            mainWindow.GearCatalog=()=>gear?.Catalog;
            mainWindow.GearCatalogFailed=()=>gear?.CatalogFailed==true;
            mainWindow.EquippedGear=()=>gear?.Equipped ?? [];
            // X-153 (0.1419, tenth slice): the chocobo family (Plugin.Chocobo.cs), at the same place as before.
            RegisterChocobo(data,framework,client,player,unlocks,condition);
            mainWindow.StainName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Stain>().GetRowOrDefault(id)?.Name.ExtractText();
            // X-153 (0.1937): the festivals' reader in Plugin.Events.cs; the Events page reads the reader handed back.
            var festivals=RegisterFestivals(framework,client,data);
            mainWindow.Festivals=()=>festivals;
            RegisterGearsets(framework,player);
            // 0.353 (g, I-12): key items (EventItem) by name, from the game data, the list built once.
            (uint Id,string Name,uint Icon)[]? keyItems=null;
            mainWindow.KeyItemSearch=text=>
            {
                var wanted=text.Trim();
                if(wanted.Length==0)return [];
                keyItems??=data.GetExcelSheet<Lumina.Excel.Sheets.EventItem>().Where(row=>!row.Name.IsEmpty).Select(row=>(row.RowId,row.Name.ExtractText(),(uint)row.Icon)).ToArray();
                return keyItems.Where(item=>SearchText.Contains(item.Name,wanted,StringComparison.CurrentCultureIgnoreCase)).Take(30).ToArray();
            };
            // 0.352 (g, I-11): names of the quests and achievements that unlock shop articles.
            mainWindow.QuestNameOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Quest>().GetRowOrDefault(id)?.Name.ExtractText();
            // 0.911 (X-76): a key item's name (EventItem row), for the hand-ins seen in game.
            mainWindow.KeyItemNameOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.EventItem>().GetRowOrDefault(id)?.Name.ExtractText();
            mainWindow.AchievementNameOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Achievement>().GetRowOrDefault(id)?.Name.ExtractText();
            // 0.351 (g, I-04): gil journal per character, kept 90 days; since 0.1139 (X-173) in TenshoData.db; the files of before (GilJournal/<id>.json)
            // left aside, never read (X-574, 0.1793).
            var gilWatcher=RegisterGilJournal(pluginInterface,framework,player,gameGui,condition,objects,tenshoData);
            // The server bar's entries (alarms, reminders, currencies, gil, X-217's optional entries), once the services they show exist:
            // the server bar family (X-153, 0.1240: Plugin.ServerBar.cs), in the order they were created before.
            RegisterServerBar(dtrBar,framework,gameGui,player,client,inventory,configuration,localization,()=>currencies?.State,()=>gilWatcher?.State?.Entries,()=>gear?.Equipped ?? []);
            mainWindow.StackSizeOf=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(id)?.StackSize ?? 0;
            // X-153 (0.1928): the shop reminder of the items labelled « to sell » and « important » in Plugin.Items.cs.
            RegisterShopReminder(configuration,data,framework,gameGui,player,chat);
            // 0.346 (g, I-07): lowest offer minus 1 copied to the clipboard when the player opens an item's offers (off by default).
            RegisterUndercut(configuration,marketBoardEvents,framework,chat,data);
            mainWindow.WorldName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.World>().GetRowOrDefault(id)?.Name.ExtractText();
            mainWindow.TerritoryName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(id) is { } territory && territory.PlaceName.RowId!=0
                ? territory.PlaceName.Value.Name.ExtractText():null;
            // X-153 (0.1225): the desktop family (local link, installer) is wired in Plugin.Desktop.cs.
            RegisterDesktop(pluginInterface,configuration,framework,client,player,inventory,objects);
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
            // X-153 (0.1930): the character's status file (ventures, counters, retainers) in Plugin.Character.cs.
            RegisterCharacterStatus(pluginInterface,data,framework,player,dailyRoulettes,retainerSales);
            // g, C-03 (0.357): items the armoire takes, for the storage report.
            IReadOnlySet<uint>? cabinetItems=null;
            // g, C-14 (0.363): guildleves (Leve sheet, copied once) and the character's progress, read while the page shows them.
            IReadOnlyList<Leves.LeveDefinition>? leveList=null;
            IReadOnlyList<Leves.LeveDefinition> LeveList()=>leveList??=LeveClientsNamed(data.GetExcelSheet<Lumina.Excel.Sheets.Leve>()
                .Where(row=>!row.Name.IsEmpty && row.ClassJobLevel>0)
                .Select(row=>new Leves.LeveDefinition(row.RowId,row.Name.ExtractText(),row.ClassJobLevel,row.LeveAssignmentType.RowId,
                    row.LeveAssignmentType.ValueNullable?.Name.ExtractText() ?? "",(uint)Math.Max(0,row.LeveAssignmentType.ValueNullable?.Icon ?? 0),
                    row.LeveClient.ValueNullable?.Name.ExtractText() ?? "",row.PlaceNameStartZone.ValueNullable?.Name.ExtractText() ?? "",row.GilReward,row.AllowanceCost,
                    row.LevelLevemete.ValueNullable is { Map.RowId:not 0 } client ? new(client.Map.RowId,client.X,client.Z):null,
                    row.LevelStart.ValueNullable is { Map.RowId:not 0 } start ? new(start.Map.RowId,start.X,start.Z):null))
                .OrderBy(leve=>leve.Level).ThenBy(leve=>leve.Name,StringComparer.CurrentCulture).ToArray());
            // X-404 (0.1372): the sheet's « Client: » label taken off each name once (the page wrote « Client : Client : … »).
            static IReadOnlyList<Leves.LeveDefinition> LeveClientsNamed(Leves.LeveDefinition[] leves)
            {
                var labels=Leves.LeveCatalog.ClientLabels(leves.Select(leve=>leve.Client));
                return labels.Count==0 ? leves:leves.Select(leve=>leve with{Client=Leves.LeveCatalog.WithoutClientLabel(leve.Client,labels)}).ToArray();
            }
            // X-153 (0.1902): the leves family (leve progress) in Plugin.Leves.cs; the Leves page reads the reader handed back.
            var leveReader=RegisterLeveProgress(framework,client,player,unlocks,data,LeveList);
            mainWindow.LeveDefinitions=LeveList;
            // g, M-01 (0.371): automatic alarms at the ventures' end and the vessels' return (on by default, Alarms settings).
            var treasureMap=RegisterTreasureMap(framework,client,data);
            // X-159 (0.1174): the map a treasure hunt's chest is put down to (its item, TreasureHuntRank.ItemName).
            if(itemWatcher is not null)itemWatcher.TreasureMapItem=()=>treasureMap?.Spot is { } held && data.GetExcelSheet<Lumina.Excel.Sheets.TreasureHuntRank>().GetRowOrDefault(held.Rank) is { } rank ? rank.ItemName.RowId:0u;
            // X-153 (0.1443): the chat family (the Tensho chat window's reader, the chat's map links, the link under the mouse) is wired in Plugin.Chat.cs.
            // X-654 (0.1602): the senders of the chat window's messages (in memory) and the game's entries of a player's menu.
            var chatSenders=new Chat.ChatSources();
            mainWindow.ChatSenders=chatSenders;
            // X-679 (0.1658): the senders follow their lines when the chat history put back numbers them again.
            registrations.Register(()=>mainWindow.ChatMessages.Renumbered+=chatSenders.Renumber,()=>mainWindow.ChatMessages.Renumbered-=chatSenders.Renumber);
            RegisterChat(configuration,client,addonLifecycle,framework,player,pluginInterface.GetPluginConfigDirectory(),chatSenders);
            RegisterChatPlayers(configuration,interop,framework,client,objects,targets,partyList,data,chatSenders,gameConfig,seStrings,contextMenus);
            // X-153 (0.1931): the aetherial wheels' reader in Plugin.FreeCompany.cs.
            RegisterAetherialWheels(configuration,data,framework,gameGui,()=>SaveTimed("aetherial wheels"));
            // X-153 (0.1932): the automatic alarms in Plugin.Alarms.cs.
            RegisterAutoAlarms(framework,client,configuration,()=>SaveTimed("automatic alarms"));
            // X-153 (0.1444): the collections family (music, titles, achievement progress, extra collections) is wired in Plugin.Collections.cs.
            RegisterCollections(pluginInterface,configuration,data,framework,client,player,unlocks,tenshoData);
            // g, C-12 (0.364): hunting log (MonsterNote, copied once) and the kills the game holds, read while the page shows them.
            // X-01 C2.2 (0.1191, analysis of 12/11: MonsterIcons took 55 to 133 ms on its first frame in the open air, 5 to 9 ms in town:
            // the first pass with monsters around copied the whole MonsterNote sheet, and the whole English BNpcName sheet for a Blue Mage or
            // a Beastmaster, inside the frame): both are built once on a worker at load; a frame that asks sooner waits for that one build.
            var huntEntries=new Lazy<IReadOnlyList<Hunting.HuntEntry>>(()=>data.GetExcelSheet<Lumina.Excel.Sheets.MonsterNote>().Where(row=>!row.Name.IsEmpty && row.RowId>=10000)
                .Select(row=>new Hunting.HuntEntry(row.RowId,row.Name.ExtractText(),row.MonsterNoteTarget.Select((target,index)=>(target,index))
                    .Where(pair=>pair.target.RowId!=0 && pair.index<row.Count.Count && row.Count[pair.index]>0 && pair.target.ValueNullable is not null)
                    .Select(pair=>new Hunting.HuntTarget(pair.target.Value.BNpcName.ValueNullable?.Singular.ExtractText() ?? "",row.Count[pair.index],
                        string.Join(" / ",pair.target.Value.PlaceNameZone.Where(zone=>zone.RowId!=0).Select(zone=>zone.ValueNullable?.Name.ExtractText() ?? "").Where(name=>name.Length>0)),pair.target.Value.BNpcName.RowId))
                    .ToArray()))
                .Where(entry=>entry.Targets.Count>0).ToArray());
            mainWindow.HuntEntries=()=>huntEntries.Value;
            _=Task.Run(()=>
            {
                try{_=huntEntries.Value.Count;_=englishMonsters.Value.Count;}
                catch(Exception error){log.Warning(error,"Monster sheets not read ahead (perhaps after a game update); they are read when first asked.");}
            });
            RegisterHuntingLog(framework,client);
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
            mainWindow.ReadIsland=catalog=>framework.RunOnFrameworkThread(()=>
            {
                try{return Island.IslandReader.Read(catalog);}
                catch(Exception error){log.Warning(error,"Island unreadable (game update?).");throw;}
            });
            // W-01 (0.675): the custom delivery clients, read when the page opens and on the player's click.
            mainWindow.ReadDeliveries=clients=>framework.RunOnFrameworkThread(()=>
            {
                try
                {
                    var quests=data.GetExcelSheet<Lumina.Excel.Sheets.Quest>();
                    return Deliveries.DeliveryReader.Read(clients,id=>quests.TryGetRow(id,out var quest) && unlocks.IsQuestCompleted(quest));
                }
                catch(Exception error){log.Warning(error,"Custom deliveries unreadable (game update?).");throw;}
            });
            mainWindow.CurrentTerritory=()=>client.TerritoryType;
            // 0.757 (X-12): the aetherytes of each territory, from the game's sheet, for the teleport price of a zone.
            mainWindow.TerritoryAetherytes=LoadSheet("Aetheryte",()=>data.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>().Where(row=>row.IsAetheryte && row.Territory.RowId!=0)
                .GroupBy(row=>row.Territory.RowId).ToDictionary(group=>group.Key,group=>group.Select(row=>row.RowId).ToArray()),new Dictionary<uint,uint[]>());
            // X-92 (0.972): what is new in the game's sheets since the last load (log and self-check).
            var gameDataCheck=Diagnostics.GameDataWatch.Start(data,Path.Combine(pluginInterface.GetPluginConfigDirectory(),"GameDataSnapshot.json"),log,gameDataCancel.Token);
            mainWindow.GameDataCheck=()=>gameDataCheck;
            RegisterOccultCrescent(framework);
            // W-11 (0.714): the Grand Companies, read when the page opens and on the player's click.
            mainWindow.GcFeminine=()=>framework.RunOnFrameworkThread(GrandCompanies.GcReader.Feminine);
            mainWindow.ReadGc=rows=>framework.RunOnFrameworkThread(()=>
            {
                try{return GrandCompanies.GcReader.Read(rows);}
                catch(Exception error){log.Warning(error,"Grand Companies unreadable (game update?).");throw;}
            });
            // X-307 (0.1394): the game's item finder copy of the retainers, read by the self-check on the framework thread.
            mainWindow.RetainerFinder=()=>
            {
                try{return Inventory.RetainerFinderReader.Read();}
                catch(Exception error){log.Warning(error,"Item finder copy of the retainers unreadable (game update?).");return null;}
            };
            // W-10 (0.713): the allied societies, read when the page opens and on the player's click.
            mainWindow.ReadAllied=catalog=>framework.RunOnFrameworkThread(()=>
            {
                try{return Societies.AlliedReader.Read(catalog);}
                catch(Exception error){log.Warning(error,"Allied societies unreadable (game update?).");throw;}
            });
            // W-03 (0.708): the Zodiac weapons, read when the page opens and on the player's click.
            mainWindow.ReadRelics=()=>framework.RunOnFrameworkThread(()=>
            {
                try{return Relics.RelicReader.Read();}
                catch(Exception error){log.Warning(error,"Relic book unreadable (game update?).");throw;}
            });
            // W-02 (0.683): the stages of the Masked Carnivale, read when the page opens and on the player's click.
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
            // 0.449, 0.450 (DEC-027): the garden's seed list and crossbreeds. X-178 (0.1258): built by the window itself
            // (MainWindow.GardenData.cs), so the desktop application gets them the same way.
            mainWindow.ConfigureGarden(data);
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
            // X-153 (0.1936): the garden's watcher (plantings read from the chat) in Plugin.Reminders.cs; the item watcher read when a harvest is seen.
            RegisterGardenWatcher(configuration,data,framework,client,targets,gameGui,()=>itemWatcher);
            mainWindow.SquadronTrainingName=id=>data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyTraining>().GetRowOrDefault(id)?.Name.ExtractText();
            // 0.321: a failing Try On (game function moved) is logged once and its buttons turn off for the session.
            mainWindow.TryOnItem=item=>
            {
                var opened=Items.TryOn.Item(item,player.IsLoaded,error=>log.Warning(error,"Try On is unavailable (game update?)."));
                if(Items.TryOn.Failed)mainWindow.TryOnItem=null;
                return opened;
            };
            mainWindow.PlayerGrandCompany = () => player.IsLoaded ? player.GrandCompany.RowId : 0;
            // X-153 (0.1929): the NPC vendors' catalogue in Plugin.Catalogs.cs.
            RegisterNpcVendors(data,framework);
            itemContextMenu=new ItemContextMenuBridge(configuration,contextMenus,player,client,itemCatalog,log,localization,gameGui){Relevant=mainWindow.RelevantRoutes,Title=mainWindow.RouteTitle,Sources=mainWindow.SourceMenu,
                // 0.826 (X-58): the crafting lists first, then the item's pages.
                Links=id=>[..mainWindow.CraftListAdds(id),..(mainWindow.ExternalLinksFor(id) ?? [])]};
            registrations.Register(()=>{ },itemContextMenu.Dispose);
            InventoryVisualSortBridge? sortBridge=null;
            registrations.Register(()=>sortBridge=new InventoryVisualSortBridge(visualSort,inventory,player,client,framework,log,bankBridge),()=>sortBridge?.Dispose());
            // X-153 (0.1933): the Macro Chain bridge and the macros' observation in Plugin.Macros.cs.
            RegisterMacroReaders(framework,player,client,commands,interop);
            ItemHoverReader? hoverReader = null;
            registrations.Register(() => hoverReader = new ItemHoverReader(configuration, market, itemHover, itemCatalog, gameGui, player, client, framework, log) { InDuty = () => mapLive?.State is { InDuty: true }, Keys = keyState, Developer=()=>configuration.DeveloperMode, },
                () => hoverReader?.Dispose());
            // X-153 (0.1929): the maps' catalogue in Plugin.Catalogs.cs.
            RegisterMapCatalog(data,framework);
            RegisterDutyCatalog(pluginInterface,data,framework,client,unlocks,player);
            // X-153 (0.1930): the character's attributes in Plugin.Character.cs.
            RegisterCharacterStats(data,framework,player,client,objects,gameGui);
            // X-153 (0.1929): the weather and GP actions' catalogues in Plugin.Catalogs.cs.
            RegisterWeatherAndGpCatalogs(data,framework);
            // X-153 (0.1924): the crafting family (actions' loader, live reader, food and medicine, meals, detection, recipe log) in Plugin.Craft.cs.
            RegisterCraftReaders(data,framework,player,client,gameGui,objects);
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
            // X-589 (external audit A02 of 18/11): the competitive cut read again when the line is printed (PaissaDb.Alert).
            mainWindow.PlotAlert=Community.PaissaDb.Alert(()=>Competitive.CompetitiveGate.Cut,action=>framework.RunOnFrameworkThread(action),text=>chat.Print("[Tensho] "+text));
            mainWindow.ChatNotice=text=>chat.Print("[Tensho] "+text);
            // X-13 (0.447): the conductor's call, sent to the chosen channel on click only, on the framework thread.
            mainWindow.SendHuntCall=command=>framework.RunOnFrameworkThread(()=>Maps.HuntAnnouncer.Send(command,log));
            // TEMPLATES-001 (DEC-029): tools made of data, in a file of their own.
            TenshoAssistant.MyTools.ToolTemplateStore? myTools=null;
            registrations.Register(()=>mainWindow.MyToolsStore=myTools=new TenshoAssistant.MyTools.ToolTemplateStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"MyTools.json"),(error,message)=>log.Warning(error,message)),()=>myTools?.Dispose());
            TenshoAssistant.MyTools.ToolGallery? toolGallery=null;
            registrations.Register(()=>mainWindow.MyToolsGallery=toolGallery=new TenshoAssistant.MyTools.ToolGallery(),()=>toolGallery?.Dispose());
            // X-153 (0.1924): the crafting lists, the rotation recorder, Manipulation learned and the craft log reader in Plugin.Craft.cs.
            RegisterCraftLog(pluginInterface,configuration,data,framework,player,client,gameGui,unlocks,interop);
            // X-153 (0.1903): the fashion family (hints' loader, Fashion Report reader, its stains and themes) in Plugin.Fashion.cs.
            RegisterFashion(configuration,data,framework,gameGui);
            // X-153 (0.1925): the quests' and collections' readers in Plugin.Quests.cs.
            RegisterQuests(configuration,data,framework,player,client,unlocks,tenshoData);
            // X-153 (0.1926): the gathering readers (gathering window, spearfishing, collectables, action bar, gathering actions) in Plugin.GatheringReaders.cs.
            RegisterGatheringReaders(data,framework,player,client,gameGui,objects,targets);
            // X-153 (0.1934): the game's open windows and the window follower in Plugin.GameUi.cs.
            RegisterGameWindows(framework,client,gameGui);
            // X-153 (0.1926): what the player may gather, read from the game, in Plugin.GatheringReaders.cs.
            RegisterGatherKnowledge(data,framework,client,player);
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
            // X-153 (0.1927): the Mini Cactpot and Triple Triad readers in Plugin.MiniGames.cs.
            RegisterMiniGameReaders(framework,player,client,gameGui,targets,data);
            // X-153 (0.1928): the items' uses and today's Grand Company request restored, in Plugin.Items.cs.
            RegisterItemUses(configuration,data,framework);
            // X-153 (0.1931): the workshop deliveries' reader in Plugin.FreeCompany.cs.
            RegisterWorkshopDeliveries(framework,gameGui);
            // X-153 (0.1928): the Grand Company supply reader and the items' catalogue in Plugin.Items.cs.
            RegisterItemCatalog(configuration,data,framework,gameGui,client,player,saver);
            // X-153 (0.1929): the recipes' and gathering points' catalogues in Plugin.Catalogs.cs.
            RegisterRecipeAndGatheringCatalogs(data,framework);
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
            // X-153 (0.1249): the panel family (Plugin.Panels.cs), after the main window it draws from.
            RegisterPanels();
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
            // X-01 C1.1 (0.1184): the heap and the load contexts with each load line; the previous instance's context is held weakly in a
            // process-wide slot, read before this one replaces it (measurement only, no collection forced).
            var previousContext=AppDomain.CurrentDomain.GetData(Diagnostics.LoadMemory.PreviousSlot) as WeakReference;
            // X-433 (0.1459): the load's rank in the game process (the frame gaps after a load grow with the heap the reloads leave).
            var loadRank=Diagnostics.LoadMemory.NextRank(AppDomain.CurrentDomain.GetData(Diagnostics.LoadMemory.RankSlot));
            AppDomain.CurrentDomain.SetData(Diagnostics.LoadMemory.RankSlot,loadRank);
            AppDomain.CurrentDomain.SetData(Diagnostics.LoadMemory.PreviousSlot,new WeakReference(System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(Plugin).Assembly)));
            string Memory()
            {
                var contexts=System.Runtime.Loader.AssemblyLoadContext.All.Select(context=>context.Name).ToArray();
                // X-433 (0.1447): with the process's private memory and the machine's (a collection « induced for low memory »).
                return Diagnostics.LoadMemory.Describe(GC.GetGCMemoryInfo().HeapSizeBytes,GC.GetTotalMemory(false),Diagnostics.LoadMemory.Named(contexts,"TenshoAssistant"),contexts.Length,previousContext?.IsAlive,loadRank)
                    +"; "+Diagnostics.MachineMemory.Text(Diagnostics.MachineMemory.Read(),Diagnostics.MachineMemory.ProcessPrivateBytes());
            }
            void SayLoad(string when)=>log.Information("Load {When}: {Milliseconds} ms since the start, runtime pauses {Pauses} ms, {Collections} full collections, on thread {Thread}; {Memory}.",
                when,(long)System.Diagnostics.Stopwatch.GetElapsedTime(loadStarted).TotalMilliseconds,(long)(GC.GetTotalPauseDuration()-pausesBefore).TotalMilliseconds,GC.CollectionCount(2)-gen2Before,framework.IsInFrameworkUpdateThread ? "framework":"other",Memory());
            log.Information("Load context of this instance: {0}.",System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(Plugin).Assembly)?.Name ?? "?");
            // X-01 C1.3 (0.1187): the instances loaded before in this process (weak references in a slot of runtime types only); ten seconds
            // after the load, developer mode, one line each: context alive or not, parts still alive (noted by each instance at its unload).
            var instances=Diagnostics.UnloadWatch.Push(AppDomain.CurrentDomain.GetData(Diagnostics.UnloadWatch.Slot),
                $"{typeof(Plugin).Assembly.GetName().Version?.ToString(3) ?? "?"} loaded at {DateTime.Now.ToString("HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture)}",
                System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(Plugin).Assembly));
            AppDomain.CurrentDomain.SetData(Diagnostics.UnloadWatch.Slot,instances);
            unloadParts=instances[0].Value;
            developerAtUnload=()=>configuration.DeveloperMode;
            SayLoad("created");
            mainWindow.LoadMilliseconds=(long)System.Diagnostics.Stopwatch.GetElapsedTime(loadStarted).TotalMilliseconds;
            // X-433 (0.1422): developer mode, two minutes after the load: the runtime's suspensions of every thread with their reason (the
            // collection pauses are not all of them), to name what the readers wait on about 25 s after a load.
            if(configuration.DeveloperMode)
            {
                var suspensions=Diagnostics.RuntimeSuspensionListener.Open(note=>log.Information(note));
                registrations.Register(()=>{},()=>suspensions.Dispose());
                // X-433 (0.1433): the gaps between the game's frames meanwhile (the interface's Draw, once a frame; not a framework
                // subscription, which would wait for the readers' compiler), whatever ran meanwhile: a stop of every managed thread by a
                // collection freezes the game only if its thread waits too. X-440 (0.1437): Dalamud skips Draw while the plugin interface
                // is hidden (cutscene, gpose, hidden by the player); HideUi ends the run, so that time is not read as a frame gap.
                var gaps=new Diagnostics.FrameGaps();
                // X-444 M4 (0.1446): a competitive cut ends the run too (the first frame after it is not a gap as long as the cut).
                void Gap(){if(TenshoAssistant.Competitive.CompetitiveGate.Cut){gaps.Cut();return;}if(gaps.Frame(System.Diagnostics.Stopwatch.GetElapsedTime(loadStarted).TotalMilliseconds,DateTimeOffset.Now) is { } gapNote)log.Information(gapNote);}
                void GapHidden()=>gaps.Hidden();
                pluginInterface.UiBuilder.Draw+=Gap;
                pluginInterface.UiBuilder.HideUi+=GapHidden;
                registrations.Register(()=>{},()=>{pluginInterface.UiBuilder.Draw-=Gap;pluginInterface.UiBuilder.HideUi-=GapHidden;});
                _=Task.Delay(Diagnostics.RuntimeSuspensionWatch.Length).ContinueWith(_=>
                {
                    pluginInterface.UiBuilder.Draw-=Gap;
                    pluginInterface.UiBuilder.HideUi-=GapHidden;
                    if(Diagnostics.RuntimeSuspensionWatch.End(suspensions))
                    {
                        log.Information(gaps.Summary(Diagnostics.RuntimeSuspensionWatch.Length));
                        log.Information(suspensions.Tracker.Summary(Diagnostics.RuntimeSuspensionWatch.Length));
                        log.Information(suspensions.Types.Summary());
                        log.Information(suspensions.Collections.Summary(Diagnostics.RuntimeSuspensionWatch.Length));
                        log.Information(suspensions.Large.Summary(Diagnostics.RuntimeSuspensionWatch.Length));
                        log.Information(suspensions.Jit.Summary(Diagnostics.RuntimeSuspensionWatch.Length));
                        // X-433 (0.1447): what was allocated meanwhile, by type and thread, and by the whole process.
                        log.Information(suspensions.Allocations.Summary(Diagnostics.RuntimeSuspensionWatch.Length));
                        log.Information(Diagnostics.MachineMemory.AllocatedText(GC.GetTotalAllocatedBytes()-allocatedBefore,managedBefore,GC.GetTotalMemory(false),Diagnostics.RuntimeSuspensionWatch.Length));
                    }
                },TaskScheduler.Default);
            }
            _=Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_=>
            {
                SayLoad("10 s later");
                if(!configuration.DeveloperMode)return;
                // X-177 C4.2 (0.1199): the readers' classes compiled on their own thread, each reader started once its class was ready.
                void SayReaders()
                {
                    if(TenshoAssistant.Competitive.CompetitiveGate.CompilerState is { } readers)
                        log.Information("Readers compiled on their own thread: {0} classes, {1} methods in {2:0} ms, the last one ready {3:0} ms after the first subscribed; {4} still waiting.",
                            readers.Classes,readers.Methods,readers.Compiling,readers.DoneAfter,readers.Waiting);
                    // X-177 (0.1281): of those methods, the ones the readers' classes call in other classes (CallGraph).
                    if(TenshoAssistant.Competitive.CompetitiveGate.CompilerState is not null)
                        log.Information("Readers' callees: {0} methods called from their classes in other classes, compiled with them.",TenshoAssistant.Competitive.CompetitiveGate.CompilerCallees);
                    // X-433 (0.1438): the readers the guard delay started before their class was compiled (each compiles in a frame).
                    if(TenshoAssistant.Competitive.CompetitiveGate.CompilerState is not null)
                        log.Information("Readers started by the guard delay before their class was compiled: {0}.",TenshoAssistant.Competitive.CompetitiveGate.CompilerGuardStarts);
                    // X-433 (0.1424): the longest compilations, to compare with a hitch's time.
                    if(TenshoAssistant.Competitive.CompetitiveGate.CompilerSlowest is { Length:>0 } slowest)
                        log.Information("Readers' longest compilations: {0}.",slowest);
                    // X-433 (0.1425): and the JIT warm-up's, the other thread that compiles while the readers start.
                    if(mainWindow.WarmupSlowest is { Length:>0 } warmup)
                        log.Information("JIT warm-up's longest compilations so far: {0}.",warmup);
                }
                SayReaders();
                // 0.1200: the classes come after the interface's code: said again once they had the time to end.
                if(TenshoAssistant.Competitive.CompetitiveGate.CompilerState?.Waiting>0)_=Task.Delay(TimeSpan.FromSeconds(20)).ContinueWith(_=>SayReaders(),TaskScheduler.Default);
                // 0.1196: one short line per earlier instance: « 0 of N parts alive » when it was collected whole, else its first 24 parts held.
                foreach(var earlier in instances.Skip(1))log.Information("Earlier instance {0}.",Diagnostics.UnloadWatch.Describe(earlier.Key,earlier.Value,24));
                // X-802 (0.1845): the newest earlier instance still alive is no longer taken to read its unload state and the static fields
                // of its assembly (0.1188): that reflection ran in a load context being unloaded, and the game's runtime crashed in it
                // (22/11 13:46, the dump's faulting thread). Its parts alive are named above, by their names only.
            },TaskScheduler.Default);
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
        if(!characterReady()){chatGui.Print("[Tensho] "+localization.Get("Plugin.NeedsCharacter"));return;}
        if (arguments.Trim().ToLowerInvariant() is "peche" or "pêche" or "fish" or "fishing") { mainWindow?.ToggleFishingPanel(); return; }
        // X-403 (0.1434, D-166): « /tensho chat » shows or hides the Tensho chat window.
        if (arguments.Trim().ToLowerInvariant() is "chat") { mainWindow?.ToggleChatWindow(); return; }
        // X-447 B12 (0.1714): « /tensho chat hide » hides every Tensho chat window for a moment, or shows them back.
        if (string.Join(' ', arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant() is "chat hide") { mainWindow?.ToggleChatWindowsHidden(); return; }
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
        // X-539: title screen and character selection: no window nor overlay; their open state is left as it is.
        if(!characterReady())return;
        // 0.745: in developer mode, a slow frame is written to the log with its costliest parts.
        if(!mainWindow.DeveloperMode){DrawFrame();return;}
        var started=System.Diagnostics.Stopwatch.GetTimestamp();var paused=GC.GetTotalPauseDuration();
        try{DrawFrame();}
        finally
        {
            var total=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if(mainWindow.SlowFrames.Report(Dalamud.Bindings.ImGui.ImGui.GetFrameCount(),total,(GC.GetTotalPauseDuration()-paused).TotalMilliseconds) is { } slow)
                log.Information(slow);
            // X-01 (0.1190): the steady state, one line a minute in developer mode: what each part costs on ordinary frames.
            mainWindow.SlowFrames.Steady.Add(Diagnostics.SteadyFrames.Frame,total);
            if(mainWindow.SlowFrames.Steady.Take(Environment.TickCount64) is { } steady)log.Information(steady);
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
            ("ChatLinkMenu",mainWindow.DrawChatLinkMenu),
            ("AlarmBar",()=>alarmBar?.Invoke()?.DrawTooltip()),
            ("ReminderBar",()=>reminderBar?.Invoke()?.DrawTooltip()),
            ("CurrencyBar",()=>currencyBar?.Invoke()?.DrawTooltip()),
            ("GilBar",()=>gilBar?.Invoke()?.DrawTooltip()),
            ("StateBar",()=>{mainWindow.TickDashboard();stateBar?.Invoke()?.DrawTooltip();}),
            ("InfoBar",()=>infoBar?.Invoke()?.DrawTooltip()),
            ("TriadOverlay",mainWindow.DrawTriadOverlay),
            ("TriadRequest",mainWindow.DrawTriadRequestPanel),
            ("TriadDeck",mainWindow.DrawTriadDeckReminder),
            ("Character",mainWindow.DrawCharacterPanel),
            ("DutyFinder",mainWindow.DrawDutyFinderPanel),
            ("FashionCheck",mainWindow.DrawFashionCheckPanel),
            ("Gathering",mainWindow.DrawGatheringPanel),
            ("BeastCombo",mainWindow.DrawBeastComboHighlight),
            ("Collectable",mainWindow.DrawCollectablePanel),
            ("RetainerSales",mainWindow.DrawRetainerSalesPanel),
            ("Spearfishing",mainWindow.DrawSpearfishingPanel),
            ("OceanVoyage",mainWindow.DrawOceanVoyagePanel),
            ("GameLogRequest",mainWindow.ProcessGameLogRequest),
            ("HuntTrain",mainWindow.ObserveHuntTrain),
            ("OccultAlerts",mainWindow.ObserveOccultAlerts),
            ("Garden",mainWindow.ObserveGarden),
            ("WishlistSales",mainWindow.WatchWishlistSales),
            ("PlotWatch",mainWindow.WatchPlots),
            ("Fishing",mainWindow.DrawFishingPanel),
        ];
        // X-01 (0.1279): the overlay pass and the free panels wait until their code is compiled, 6 s at most after the load (X-653): they were
        // 152 to 338 ms of first-run compiling in one frame at each load. The frame where they come back is timed once, in every mode.
        var panelsOpen=mainWindow.PanelsOpen;
        var panelsFirst=panelsOpen && !panelsOpenedLogged ? System.Diagnostics.Stopwatch.GetTimestamp():0;
        // R-14 (0.452): Dalamud calls this whole method inside one catch (UiBuilder.OnDraw): an overlay that threw (a sheet lost after a game
        // update throws on every call) replaced every Tensho window with Dalamud's error window. Each one now fails alone, logged once.
        for(var index=0;index<overlays.Length;index++)
        {
            if(!panelsOpen)break;
            var (name,draw)=overlays[index];
            if(brokenOverlays.Contains(name))continue;
            var started=mainWindow.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp():0;
            try{draw();}
            catch(Exception error){brokenOverlays.Add(name);log.Error(error,"Overlay {0} turned off until the plugin reloads (perhaps after a game update).",name);mainWindow.NotePanelOff(name);}
            // 0.486 (user report): the panels beside the game's windows count too, in developer mode.
            if(started!=0)mainWindow.RecordOverlayDraw(name,System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        // 0.516: the panels attached to a game window have asked above; all the windows are drawn after them.
        attachedPanels?.Apply();
        var windowsStarted=mainWindow.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp():0;
        windows.Draw();
        if(windowsStarted!=0)mainWindow.SlowFrames.Add(Dalamud.Bindings.ImGui.ImGui.GetFrameCount(),"all windows",System.Diagnostics.Stopwatch.GetElapsedTime(windowsStarted).TotalMilliseconds);
        if(panelsFirst!=0){panelsOpenedLogged=true;log.Information(mainWindow.PanelsOpenedText(System.Diagnostics.Stopwatch.GetElapsedTime(panelsFirst).TotalMilliseconds));}
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
    /// <summary>X-01 (0.1279): the line « Panels opened … » is written (once per load).</summary>
    private bool panelsOpenedLogged;

    /// <summary>0.1095: a lazy value built on a worker the first time it is asked; null until it is ready (never built in the frame).</summary>
    private static Func<T?> InBackground<T>(Lazy<T?> lazy) where T:class
    {
        Task? building=null;
        return ()=>
        {
            if(lazy.IsValueCreated)return lazy.Value;
            building??=Task.Run(()=>_=lazy.Value);
            return null;
        };
    }

    public void Dispose()
    {
        // X-01 C1.3 (0.1187): developer mode: the plugin, the window and their fields that hold the plugin's own objects are noted weakly
        // before anything is released; the next loads say which ones are still alive.
        if(unloadParts is { } parts && developerAtUnload())
        {
            Diagnostics.UnloadWatch.Note(parts,"plugin",this,typeof(Plugin).Assembly);
            Diagnostics.UnloadWatch.Note(parts,"window",mainWindow,typeof(Plugin).Assembly);
            // 0.1188: the constructor's captured locals (most services live there, not in fields): the closure object behind this delegate.
            Diagnostics.UnloadWatch.Note(parts,"locals",developerAtUnload.Target,typeof(Plugin).Assembly);
            // 0.1191: and the closure of the constructor's outer scope behind it (its parameters and this).
            Diagnostics.UnloadWatch.Note(parts,"locals0",Diagnostics.UnloadWatch.OuterClosure(developerAtUnload.Target),typeof(Plugin).Assembly);
        }
        gameDataCancel.Cancel();
        try { registrations.Dispose(); }
        catch (AggregateException error) { log.Error(error, "Failed to release some Tensho registrations."); }
        // X-685 (0.1752): once the data files are closed, this copy's own Microsoft.Data.Sqlite factory is released: its prune timer and
        // its two handlers of the process events held this whole load context after the unload, for good (Diagnostics.UnloadRelease).
        log.Information("Unload: {0}.",Diagnostics.UnloadRelease.Sqlite().Text);
        // Developer mode: what still holds this copy among the process's events (none expected; « unknown » on a runtime that keeps them elsewhere).
        if(developerAtUnload())
        {
            var held=Diagnostics.UnloadRelease.ProcessEventsFrom(System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(Plugin).Assembly));
            log.Information("Unload: process event handlers still from this copy: {0}.",held is null ? "unknown":held.Count==0 ? "none":string.Join(", ",held));
        }
        // X-685 (0.1755): the count of full collections at the end of the unload, for the next loads' line (« after N full collection(s) »).
        if(unloadParts is { } unloaded)Diagnostics.UnloadWatch.Unloaded(unloaded,GC.CollectionCount(2));
    }
}
