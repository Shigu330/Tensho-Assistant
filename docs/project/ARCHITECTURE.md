# Architecture

> *Traduction française plus bas.*

## Overview (checked against the code on 2026-11-09)

> A map of the code, not a journal: the detail of each version is in the [CHANGELOG](../../CHANGELOG.md), the in-game state in the
> [global plan](GLOBAL-PLAN.md) and [TESTING](TESTING.md). The code is the reference. The design notes written for each goal from
> 2026-10-27 to 2026-10-29 (the « History of the domains and versions » archive) were removed from this file on 2026-11-08: their names,
> counters and states belonged to their date. They stay readable in git (this file in commit `2da535d`, 0.865.0).

One plugin project (`src/TenshoAssistant`, .NET 10, Dalamud.NET.Sdk 15, a single DLL, plus the native library of the optional Raphael
solver next to it) and a test program without native dependency (`tests/TenshoAssistant.Tests`) that compiles the pure C# files of the
plugin by link and reads the embedded data files from the sources.

| Files | Role |
|---|---|
| `*.Dalamud.cs` | touch the game (Dalamud services, FFXIVClientStructs); never compiled into the tests |
| `*.Lumina.cs` | typed copies of Excel sheets, shared with the probes of `tools/` |
| others | pure C# (models, rules, services); tested when the test project includes them |

### Host and life cycle (`Plugin.cs`)

- `Plugin` receives the Dalamud services by injection, loads `Configuration` v1 (another version raises an error, the file is not
  overwritten), creates the services, `MainWindow`, `MapViewWindow`, `GuideWindow`, the free-standing `PanelWindow`s and `AttachedPanels`
  (panels beside the game's windows, in the same `WindowSystem`), and registers every reader, loader, bridge and network service in
  `RegistrationScope`: cleanup in reverse order, idempotent, continued after an error; a failure of the constructor triggers the same
  cleanup.
- Competitive cut (DEC-038): every framework subscription goes through `Competitive/CompetitiveGate`; while the player is in PvP or
  ranked content (`CompetitiveZones`, from the game's data), the whole plugin stops, with no setting, and starts again after.
- Command `/tensho`: opens the window; `peche`, `pêche`, `fish` or `fishing` open or close the fishing help; `todo <task>` (or `tache`,
  `tâche`) adds a task to Reminders; `wiki <item>` and `lodestone <item>` open the item's page in the browser; `item`, `price`, `gather`
  or `recipe` (also `craft` and the French words) followed by an item name open the first matching item in that tool
  (`MainWindow.CommandRoute`); `selftest` writes the self-check to the log; `linktest` (developer mode) prints test links.
  `OpenMainUi`, `OpenConfigUi` (Settings), opening at login (`OpenOnLogin`). Fonts of the Tensho window built by `UI/FontManager`
  (the game's title font by default), released at unload.
- Panels beside the game's windows go through `Panels?.Show(...)` (`AttachedPanels`), for example: crafting log help, synthesis
  projection, gathering and collectable panels, fishing and spearfishing panels, ocean voyage, item hover, inventory sort button,
  retainer sales, duty finder, Character window, Fashion Report slots, Triple Triad deck request. The bank import panel shows only when
  the automatic capture is off (`BankImportOverlay`).

### Module map (`src/TenshoAssistant/`)

| Folder | Main content |
|---|---|
| `Items/` | shared item catalogue (`ItemCatalog` + loader + cache, mount / minion / orchestrion unlocks), uses (`ItemUseCatalog`), hover (`ItemHover`, `ItemHoverReader`), context menu of the game's inventory (`ItemContextMenuBridge`, `ItemContextMenuSession`), crystals, slots, `TryOn` (the game's Try On window), item notes and wishlist (`ItemNotes`, `Wishlist`), Grand Company supply of the day (`GcSupplyToday`), external links (`ExternalLinks`), search filters |
| `Inventory/` | live inventory (`InventoryService`, `DalamudInventoryReader`), Bank (`InventoryBankService`, `PerCharacterBankStore` + `FileInventoryBankStore`: one file per character; `BankInventoryBridge`: retainers, saddlebags, armoury chest, glamour dresser, armoire), ownership (`ItemHoldings`, `OtherCharacterStocks`), native visual sort (rules, scopes, templates, preview, `InventoryVisualSort` + bridge), currencies (`Currencies`, `CurrencyWatcher`), gil journal (`GilJournal`, `GilWatcher`), storage highlight and report |
| `Market/` | Universalis: summary and hover price (`MarketService`, `MarketWorldReader`, `MarketHover`, `ItemHoverCoordinator`); listings, history and cost for N units (`MarketBoard`, `MarketBoardService`); preparation cost (`PreparationCost`, `PreparationPriceSession`); retainer sales (`RetainerSales` + reader), undercut watch (`Undercut` + watcher), prices seen at the market board (`SeenPrices` + reader), craft profit (`CraftProfit`), currency value (`CurrencyValue`) |
| `Craft/` | recipes and actions (catalogues + loaders), simulator, solver (`CraftSolveSession`) and the optional Raphael solver (`RaphaelCraftSolver`, `RaphaelSequenceCache`), conditions envelope, live synthesis (`CraftLive*`, opt-in), crafting log help (`CraftLog*`), plan and chain (`CraftPlan`, `CraftChain*`), presets, macro export and import, crafting lists (`CraftLists`), crafting recorder (`CraftRecorder`, `CraftRecording`), Teamcraft export link (`TeamcraftExport`), crafting from stock, consumables |
| `Gathering/` | gathering and fishing points (catalogue + cache), time windows (`GatheringSchedule`, `GatheringTimeline`), weather (`WeatherCatalogLoader`, `WeatherForecast`), fishing log and forecasts (`FishingNotebook`, `FishingForecast`, `FishNow`, `FishWindows`), observation of the gathering window, GP (`GpActions`, `GpOptimizer`, `GpNextAction`), Bountiful Yield II estimate (`BountifulEstimate`), collectables, spearfishing, ocean fishing (`OceanFishing`, `OceanVoyage` + reader), notebooks, gathering route, GP regeneration tick |
| `Community/` | bounded network access (`BoundedHttp`) and community sources: Garland Tools (`GarlandClient`, `GarlandItemSources`), wiki (`WikiItemAcquisition`), LuminaSupplemental, GatherBuddy (`GatherBuddyFish`), Teamcraft (`FishBiteTimes`), PaissaDB (`PaissaDb`), fishing parsers (`FishingSources`, `FishingConditions`), community names (`CommunityNames`) |
| `Monsters/`, `Duties/` | monster loot (`MonsterLootService`, `WikiMonsterSources`), blue magic sources (`BlueSpellSources`, `BlueSpellWikiIndex`, `BlueSpellMonsterIndex`), Beastmaster wiki index (`BeastWikiIndex`), meaning of nearby monsters (`NearbyMonsters`, information only); duties (`DutyCatalog`), loot (`DutyLootService`), duty finder panel (`DutyFinderReader`), duty progress (`DutyProgress` + reader) |
| `Fashion/` | Fashion Report: community data (`FashionDataService`), hints (`FashionHintLoader`), the game window (`FashionCheckReader`) |
| `Maps/` | Tensho map: catalogue, markers (`MapMarkerSource`), projection, live state (`MapLiveReader`: position, flag, FATEs, party, nearby objects and monsters, teleport), local index (`MapLocalIndex` + loader), replacement of the game map (`NativeMapReplacer`), bookmarks and place directory, sightseeing from the wiki (`WikiVistas`), map links of the chat (`ChatMapLinks` + watcher), hunt train (`HuntTrain`, `HuntAnnouncer`), map discovery, search, zoom memory, treasure maps, island resources |
| `Npcs/` | NPC vendors (`NpcVendorCatalog` + loader): gil shops, exchanges, free company shops |
| `Quests/`, `Collections/` | quests (reader, rewards and reverse index, quest items, start cities, options not taken); collections, titles, achievements, orchestrion (`CollectionReader` + partial `Completion`, `ExtraCollectionReader`, `BgmReader`), outfits, achievement progress, title by job (`TitleChanger`) |
| `Character/` | character statistics (`CharacterStatsReader`, `StatFormulas`), what Tensho last read for each of the player's characters (`CharacterStatus` + watcher) |
| `Gear/` | gear planner: `GearCatalog` (pieces, rules, materia and caps, `TENSHO-GEAR1:` sharing), `GearLoader`, plan sets, comparison, gearsets (`Gearsets`, `GameGearsetReader`), gear wear, job levels (`JobProgress`), chocobo barding (`ChocoboBarding` + reader) |
| `FreeCompany/` | submarines and airships: `Voyages` (records, merged by house), `VoyageWatcher` (reading the workshop, catalogues), `VoyagePlanner` (best routes), `VesselBuilds` (builds, tiers, wear), airship sectors and planner, voyage history, workshop projects and deliveries, `Data/` (embedded files) |
| `Island/` | island sanctuary: catalogue + loader, reader (on the player's click), workshop planner, materials, pasture animals |
| `Deliveries/`, `GrandCompanies/`, `Societies/`, `Leves/`, `Hunting/` | custom and special deliveries; Grand Companies; allied societies; guildleves (`LeveCatalog`, `LeveProgressReader`); hunting log (`HuntingLog` + reader) and S rank conditions (`SRankConditions`) |
| `Carnivale/`, `Relics/`, `Fields/`, `DeepDungeons/`, `Lexicon/` | Masked Carnivale; relic weapons and armour; Eureka and Bozja (`FieldBoard`) and FATEs; deep dungeons; lexicon of the game's words — each a model and its `*.Dalamud.cs` reader |
| `Events/` | seasonal events (`Festivals`, `FestivalReader`), `Data/festival-names.csv` (embedded) |
| `Discoveries/` | local discoveries base (`DiscoveriesModel`: facts with their provenance, import with merge, export; the player's own facts in `Discoveries.json`, the shared ones in `Shared/`, one compressed file per kind, read only when a page needs that kind), its settings (`DiscoverySettings`), what it notes from the game (`ItemDiscoveryWatcher`, `ItemGains`), and the exchange with other players (`GitHubExchange`, `CommunityDiscoveries`: GitHub sign-in on the player's click, own facts sent to the player's gist with an issue, the common file `community-discoveries/discoveries.json` joined as imported; « Exchange now » or the opt-in daily exchange); game rows and places only, never another player |
| `Competitive/` | PvP and ranked content cut (`CompetitiveGate`, `CompetitiveZones`, DEC-038) |
| `Credits/` | the single list of credits (`CreditCatalog`: the game, libraries, embedded data, online data, rules taken up, references read, tools), shown by `MainWindow.Credits.cs` and checked by the tests (embedded data, network hosts, settings, MIT notices) |
| `Alarms/`, `Reminders/` | Tensho alarms (`TenshoAlarm`, `AlarmService`, the game's `/alarm` command through `GameAlarmCommand`, automatic alarms `AutoAlarms` + watcher, server info bar entries for alarms, reminders and currencies, Discord webhook `DiscordWebhook` (pure rules) and `DiscordNotifier` (queue and sending on a worker), tracked alarms, world clocks); reminders of the calendar (`ResetSchedule`, `GameTimers`), garden (journal, seeds, crossbreeds, chat watcher), squadron planner and reader, hunt bills, personal tasks, Wondrous Tails odds, company benefits, `Data/` (embedded garden files) |
| `MiniGames/` | Mini Cactpot (solver, session, observation); Triple Triad (cards, solver, match simulator and advice, card score, NPCs, observation) |
| `Macros/` | macro chain (`MacroSequence`, `MacroChainService` / `MacroChainBridge`, observation); writing crafting macros into the game (`MacroSlotPlan`, `MacroWriter`) |
| `Caching/` | disk cache (`DataCache`), durations (`CachePreferences`), keys (`CatalogCacheKeys`, `GameCacheIdentity`), validation (`CacheSnapshots`) |
| `Localization/` | `LocalizationManager`, `LocalizationValidator`, `Languages/*.json` (embedded) |
| `Help/`, `Guide/` | help of each page and search (`HelpIndex`), report a problem (`ProblemReport`, prefilled GitHub form), community videos (`CommunityHelpIndex`, read on a click); first-run guide (`GuideFlow`; `GuideWindow.cs`, at the root, in the `WindowSystem`) |
| `Diagnostics/` | self-check at reload in developer mode (`SelfCheck`, `SelfCheckRunner`), page sweep (`PageSweep`, `PageSweepWindow`, DEC-040), slow frame log (`SlowFrame`), blink watch, temporary in-game readings (`ReadingCapture`) |
| `Beastmaster/` | Beastmaster page, the game's data read raw (`BeastCatalog` + loader); window probe (`BeastWindowProbe`), without an advice panel (DEC-032) |
| `MyTools/` | the player's tools and the community gallery, data only (`ToolTemplate`, `ToolTemplateStore`, `ToolGallery` read on a click) |
| `UI/` | theme (`FfxivTheme`, `ThemePalettes`, `GameThemeReader`), fonts (`FontManager`, `FontSettings`), `UiComponents`, `UiMessages`, `InternalMessages`, setting save with rollback (`SettingSave`), the game's visible windows (`GameWindowsReader`, `GameWindowFollower`), shared templates `Components/`, navigation bar `Navigation/` |
| root | `Plugin`, `Configuration` (+ `Configuration.Dalamud.cs`), `CommunitySources` (« game data and what you observed only »), `RegistrationScope`, saved files (`AtomicFile`: temporary file then swap; `BackgroundWriter`: writes off the framework thread, in order; `BackupZip`: zip copy before a folder is emptied), `OnceSet` (bounded « once per event »), `MainWindow.*.cs` (one partial per page or part), `MapViewWindow`, `GuideWindow`, `PanelWindow`, `AttachedPanels`, `BankImportOverlay` |

### Main window

`MainWindow`: a side bar sorted in foldable groups (state kept in `NavigationCollapsed`), « item in context » under the bar, content page
on the right. Every page is one line of `MainWindow.Pages.cs` (`PageEntries`): its group, its label, its draw method and its source; the
navigation, the drawing and the source shown to contributors all read it. Today:

| Group | Pages (pages reached only from tabs in brackets) |
|---|---|
| — | Home, My tools |
| ITEMS | Items (search + item sheet), Storage, Inventory sorting, Market, Lexicon |
| PRODUCTION | Crafting (live synthesis, presets), Gathering (GP planning, collectables), Ocean fishing, Island sanctuary, Custom deliveries, Macro chain |
| WORLD | Map, Weather, FATEs, Alarms, Reminders, Garden, Free company, NPC vendors, Duty loot, Monster loot, Discoveries |
| PROGRESSION | Quests, Events, Collections (Blue magic), Masked Carnivale, Relics, Allied societies, Grand Companies, Special deliveries, Eureka and Bozja, Deep dungeons, Gear, Chocobo, Guildleves, Hunting log, Beastmaster |
| GOLD SAUCER | Fashion Report, Triple Triad, Mini Cactpot |
| PREFERENCES | Help, Settings (General, Interface, Modules, Map, Data, Advanced; Debug in developer mode), Credits, Diagnostics (developer mode) |

The map has its own window (`MapViewWindow`), whose content and open state stay in `MainWindow.MapView.cs`.

Shared templates (`UI/Components/`):

- `ItemCell.cs`: `DrawItemIcon` / `ItemCell`, the item icon of every module: shared item tooltip (one per frame), ✓ / ✗ badge of
  collection items (unknown = no badge), right click = useful tools (`RelevantRoutes`) and grouped sources (`SourceLines`,
  `MainWindow.SourceLinks.cs`), unique menu id (`UniqueMenuId`). The same content (`DrawItemInfoBody`) serves the panel under the game's
  tooltip; the same sources, the Tensho menu of the game's inventory.
- `SettingRow.cs`: setting with tooltip and ↺ button (default read on a new `Configuration`); « Reset everything to default ». A failed
  save puts the previous value back and shows one error line at the bottom of the main window (`UI/SettingSave.cs`).
- `Tooltip.cs`: one tooltip helper (also on greyed controls); `StatusLines.cs`: one look for empty or loading lines.
- `DetailPage.cs`: header then tabs, only the open tab is computed (item sheet, Triple Triad sub-tabs).
- `EntityMenus.cs`: one right-click menu per kind of thing (quest, NPC, monster, place).
- `DataIndexes.cs`: indexes of collections and quests, rebuilt when the list is read again, never at each frame.

### Data flow

```
game data (Lumina sheets, native memory)
  └─ *.Dalamud.cs readers / loaders on IFramework.Update: copy into managed records
       └─ optional worker (index, catalogue) → atomic publication of an immutable state (Volatile / lock, generation)
            └─ Draw (MainWindow, panels): reads the published state, caches its computations, never waits
network sources (Community/, Market/, Fashion/, Maps/WikiVistas, Monsters/) → bounded request → published state + DataCache
player actions (flag removal, teleport, /alarm, macros, sort, import) → request in a queue → run on IFramework.Update
```

- Static catalogues: copied in batches on the framework thread (a few rows or about 2 ms per frame), then indexed on a worker; copy kept in
  the disk cache (`catalog-*`).
- Live reads: each reader has its own rate, written in its code; for example the game's visible windows ten times a second, the alarms
  once a second, the automatic alarms and the character status every 30 seconds, the running festivals every 10 seconds, the discoveries
  ten times a second. Observations of the synthesis, the Mini Cactpot and Triple Triad run only when turned on.
- Native strings read with a bound (example: `Utf8String` of the map tooltips, length = `BufUsed − 1`, see
  [the crash excerpt](../evidence/0.133.0-crash-excerpt.log)); search of addon nodes bounded.

Known gaps to the rule « Draw reads neither pointer nor Excel sheet » (noted on 2026-11-02, not checked again since):

- `MainWindow.Reminders.cs` calls `GameTimers.Read()` (QuestManager, SatisfactionSupplyManager, RetainerManager) from Draw, at most once
  a second and only with the Reminders page open.
- Functions passed by `Plugin.cs` and called from Draw: fishing help (player, territory, « fishing » condition each frame; `FishingSpot`
  table built at the first call), Bountiful Yield II base (`GatheringItem`, `ItemLevel`, built at the first call), job names, dyes and
  Fashion Report themes (`TryGetRow` at the call).
- `TryOn.Item` (`AgentTryon.TryOn`) is called from Draw, on a player's click.
- Placing the flag: `IGameGui.OpenMapWithMapLink` is called from Draw, on a click; removing the flag goes through the queue of
  `MapLiveReader`.
- `MapMarkerSource` reads from Draw, at the first use of each key, the sheets `MapMarker`, `Map` (region and world maps), `EventIconType`
  and `MapSymbol`, then keeps the copy in memory.
- `VoyageWatcher` builds its sector and part catalogues in one go on the framework thread (first pass), not in batches; `GearLoader` and
  `TriadNpcLoader` read the Lumina sheets directly on a worker.

### Background work and cancellation

| Owner | Work | End and cancellation |
|---|---|---|
| catalogue loaders (`ItemCatalogLoader`, `RecipeCatalogLoader`, `GatheringCatalogLoader`, `NpcVendorLoader`, `MapLocalIndexLoader`, `MapCatalogLoader`, `DutyCatalogLoader`, `ItemUseCatalogLoader`, and the others named `*Loader`) | index on a worker after the framework copy | result taken at a later `Update`; `Dispose` unsubscribes and cancels |
| `GearLoader`, `TriadNpcLoader` | catalogue from Lumina on a worker | token cancelled at `Dispose` |
| sessions: `CraftSolveSession`, `CraftChainSession`, `CraftLogSession`, `TriadSolveSession`, `MiniCactpotSolveSession`, `PreparationPriceSession`, rewards index of `QuestService` | computation or requests | generation + token; late result rejected; closed by `MainWindow.Dispose` or by their service |
| `MainWindow` pages: Triple Triad advice and deck request, best routes, best builds | search on a worker, restarted (the old one cancelled) when its inputs change | cancelled by `MainWindow.Dispose` (`MainWindow.Lifetime.cs`) |
| network services (`MarketService`, `MarketBoardService`, `GarlandClient` and `GarlandItemSourceService`, `WikiItemAcquisitionService`, `WikiMonsterSources`, `WikiVistaSource`, `LuminaSupplementalService`, `GatherBuddyFishService`, `FishBiteTimeService`, `FashionDataService`, `PaissaDbService`, `ToolGallery`) | one request at a time or a queue, bounded answer | `Dispose` through `RegistrationScope`; LuminaSupplemental and GatherBuddy wait 5 min after a failure and keep their expired copy if the network fails |
| `InventoryBankService` | reading / writing the bank files (queue bounded to one request) | `Dispose` closes the queue without waiting for an I/O already started |
| `DataCache` | disk reads and writes outside the framework | `Dispose` waits 2 s at most, then cancels |

### Configuration and persistence

- `Configuration` v1 (`Configuration.cs`, pure C# shared with the tests; `Configuration.Dalamud.cs` adds `IPluginConfiguration`), saved
  by `SavePluginConfig`. `MainWindow` writes settings through `SaveSetting`, which puts the former value back if saving fails.
- Content: settings (including `MapViewSettings`, `Cache`, the switches of the community sources, `Discovery`) and some user data
  (alarms, reminders done, vessels read, unlocked sectors, build plans, gear and materia plans, presets, decks, followed quests,
  bookmarks, macro plan, sort profiles and templates, item notes, wishlist, garden beds, personal tasks…).
- Defaults: a new `Configuration` is the reference of the ↺ buttons. « Reset everything to default » (Settings › Advanced,
  `UI/Components/SettingRow.cs`) copies by reflection, from a new configuration, only the properties listed in `ConfigurationReset.Reset`
  (`Configuration.cs`); the Discord webhook address typed is kept. `ConfigurationReset.Kept` keeps the user's data and the interface
  state. A test requires every public property to be in exactly one of the two lists: **every new property of `Configuration` must be
  placed in one of them**.
- Separate files in the plugin's configuration folder, never touched by the reset: `InventoryBank/` (one file per character, schema 1,
  16 MiB at most, bounded entries and containers; written through a temporary file then replaced, with `.bak`), `GilJournal/`,
  `Characters/`, `Discoveries/` (`Discoveries.json`, `Shared/`, `MarketSeen.json`; with the GitHub exchange, `github.json` and the token
  `github.token`, protected for this Windows user by DPAPI), `VoyageHistory.json`, `MyTools.json`, `CraftLists.json`, `Exports/`,
  `DataCache-v1/`, `Backups/` (zip copies taken before the cache or the discoveries are emptied, GitHub token left out); in developer
  mode, `SelfCheckSeen.json` and `PageSweepLast.txt` (see [SECURITY](../../SECURITY.md)). Saved files are written through `AtomicFile`
  (temporary file, then swapped in).

### Caches (`Caching/`)

- `DataCache-v1` in the plugin's configuration folder: one file per family + scope + version, versioned envelope (schema, family, scope,
  version, date); bounded size per entry, in number of files and in total; only the last entry of a family and scope is kept; the
  catalogues (`catalog-*`) are never evicted by number.
- Static catalogues (`catalog-items`, `catalog-recipes`, `catalog-gathering`, `catalog-npc-vendors`, `catalog-map-positions`…): key =
  game data + language + shape of the copy (`CatalogCacheKeys`), never the plugin version.
- Snapshots per character (quests, collections): key `GameCacheIdentity` (plugin version + language + data versions), so read again
  after each new plugin version.
- Durations per area (`CacheArea`, Settings › Data), defaults since 0.750 (X-02): Prices 1 day, Quests 1 h, Collections 7 days, Sources
  « Only when missing, or on Refresh », Fashion Report 7 days, Imported stock 1 day (only flags the age), Crafting computations 7 days,
  Titles and achievements 7 days; choices 5 min, 1 h, 24 h, 7 days or « Only when missing, or on Refresh ». Full record of the sources and
  rates at the time of the audit: [https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/evidence/0.246-cache-audit.md](https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/evidence/0.246-cache-audit.md).

### Embedded resources and external sources

| Data | Where | Source and licence |
|---|---|---|
| languages `Localization/Languages/*.json` | embedded in the DLL; a `Localization/Languages` folder next to the DLL overrides them (translators); Verify refuses a JSON shipped in the package | Tensho |
| `FreeCompany/Data/submarine-loot.json` | embedded; average loot per sector, made by `tools/submarine-loot.py`, read once by `VoyageWatcher` | FFXIVGachaSpreadsheet (MIT, community records) |
| `FreeCompany/Data/submarine-breakpoints.csv` | embedded; thresholds of the sectors (surveillance, retrieval, favour) | SubmarineTracker (MIT, licence notice at the top of the file) |
| `FreeCompany/Data/airship-unlock.csv`, `airship-drop.csv` | embedded; airship sectors | LuminaSupplemental (GPL-3.0, notice in the file) |
| `Events/Data/festival-names.csv` | embedded; English names of seasonal events | LuminaSupplemental (GPL-3.0, notice in the file) |
| `Reminders/Data/garden-seeds.csv`, `garden-crosses.csv` | embedded; growing times and crossbreeds, made by `tools/garden-data.py` | FFXIV Gardening Tracker (GPL-3.0), FFXIV Crossbreed Helper (MIT), notices in the files |
| main stat modifier table (levels 1–100) | in the code (`Character/StatFormulas.cs`) | Character Panel Refined (MIT) |
| `images/icon.png` | copied next to the DLL (not embedded) | Tensho |
| prices, listings, history, data centres | network (bounded `HttpClient`) | Universalis API |
| item sources, loot | network (`BoundedHttp`) | Garland Tools |
| monster loot, acquisition, sightseeing, blue magic, Beastmaster and pasture pages | network (`BoundedHttp`) | ffxiv.consolegameswiki.com |
| duty loot, monsters, FATEs, ventures, shop, desynthesis… | network (GitHub, `BoundedHttp`) | LuminaSupplemental (GPL-3.0) |
| baits, conditions and fishing windows | network (GitHub, `BoundedHttp`) | GatherBuddy (Apache-2.0) |
| bite times | network (GraphQL, `FishBiteTimes`) | Teamcraft public reports (MIT) |
| Fashion Report (`FashionReport.json`) | network (GitHub, `BoundedHttp`) | FFXIVGachaSpreadsheet (MIT) |
| open housing plots | network (`BoundedHttp`) | PaissaDB |
| common discoveries file (`community-discoveries/discoveries.json`) | network (raw.githubusercontent.com), on « Exchange now » or the opt-in daily exchange | Tensho's repository (players' readings) |
| sign-in, gist and issue of the discoveries exchange | network (github.com, api.github.com), only after the player connects GitHub | GitHub |
| alarm, currency and duty-pop posts | network (the player's own webhook, discord.com hosts only) | Discord |

Embedding rule: only data under a licence compatible with GPL v3, with source, licence and copyright in the file
([DEC-021](DECISIONS.md)). References read without copying code: listed on the Credits page (`Credits/CreditCatalog.cs`, « references
read ») and in [REFERENCES.md](REFERENCES.md); credits: [README](../../README.md).

### Tensho map (`Maps/`, `MainWindow.MapView.cs`, `MainWindow.MapLocalLists.cs`)

- `MapCatalogLoader` copies the `Map` sheet (zone maps); `MapMarkerSource` reads on demand, from Draw, `MapMarker`, `Map` (regions and
  world), `MapSymbol` and `EventIconType`, kept in memory (known gap, see above); `MapProjection` converts world ↔ texture ↔ screen.
- `MapLiveReader` publishes position, flag, FATEs, party, the game's event markers (quests, leves), nearby objects, living enemies around
  the player (information only, 100 at most), aether currents and unlocked sightseeing entries, combat / instance; it runs on the
  framework thread the requests of the interface (remove the flag, teleport, teleport costs).
- `MapLocalIndexLoader` builds the local index per map (quest givers, gathering and fishing points, sightseeing, currents);
  `NpcVendorLoader` the vendors. Both go through the disk cache. NPCs placed by an event are shown only during that event.
- `NativeMapReplacer` keeps the `AreaMap` addon open off screen so that the game keeps updating its markers, reads the open request
  (`AgentMap`, journal quest, aetheryte, FATE) and opens the Tensho map instead; the game map is restored when it closes or at unload.
- Layouts: classic (region / zone menus at the top, tool column on the left) or « Menus and tools over the map » (`OverlayControls`): tools
  and names submitted before the map (child windows) to take the click, the map ignores the mouse over them. Edge markers for targets out
  of view (`DrawEdgeMarkers`).
- Right click: the icon's own menu (`RightClickIgnore` sends an icon to the general menu), general menu with tabs (Favourites, Quests,
  FATEs, Gathering, Aetherytes, Vendors, Triple Triad, Aether currents, Sightseeing log).
- Settings: `MapViewSettings` in `Configuration` (global and per-map icon sizes and priorities, coloured areas, view cone, edge markers,
  layout, nearby monster layers, zoom at opening).

### Item sources (« How to get it »)

Order: game data (NPC vendors including free company shops, exchanges, quests, achievements, recipes, gathering), LuminaSupplemental data
(duties, monsters, FATEs, ventures, shop, items from items), then Garland Tools, then the « Acquisition » section of the wiki. « How to get
it » shows Garland only if no other source is known, and the wiki only if Garland knows nothing either; but Garland is asked for almost
every item put in context (selected item, sheet, Tensho and game menus), because the useful tools (`RelevantRoutes`: duties, monsters) and
`SourceLines` (duties, exchanges) read it. Garland and the wiki are turned off in Settings › Advanced. Seals of another Grand Company than
the character's are left out (unknown company = everything shown). The market is a criterion, not a source. The rows are computed once
(`SourceLines`) and shown in the Tensho menu as in the menu of the game's inventory.

### Interface and languages

`UI/FfxivTheme.cs` holds the palette (following the game's theme through `GameThemeReader`, or forced); PreDraw / PostDraw for the
windows, disposable scopes for the panels. `LocalizationManager` loads the embedded JSON (English = reference and fallback, French
complete, German and Japanese partial); a test fails Verify if a setting tooltip named in the code is missing in English or French. Game
data (items, actions, places) comes from the game's catalogue, in the game's language; Tensho's own texts are in the plugin's language.
See [Localization/README.md](../../src/TenshoAssistant/Localization/README.md). Dates shown in local time; computations in UTC and Eorzea time.

The build and the managed tests do not validate the ImGui rendering nor the native reads: only the in-game test does (state per version:
[global plan](GLOBAL-PLAN.md), [TESTING](TESTING.md), [CHANGELOG](../../CHANGELOG.md)).

### Competitive switch-off (`Competitive/`, DEC-038)

Coded in 0.610; the plan of 2026-11-05 was folded here and into DEC-038 in 0.1087 (git history keeps it). Zones and flags read from the
game data: [0.600-competitive-inventory](../evidence/0.600-competitive-inventory.md).

| Level | Content | How it is known |
|---|---|---|
| Game flags | PvP, Lord of Verminion; deep dungeons (content type 21) were cut until 0.1308, no longer since (X-262, DEC-038 completed on 2026-11-13) | `IClientState.IsPvP` (`EnterPvP`, `LeavePvP`), `TerritoryType.IsPvpZone`, `ContentFinderCondition.PvP` (`CompetitiveZones.Build`): new maps are covered without an update; `IsPvP` alone is false at the Battlehall (579) and at Lord of Verminion |
| Arena only | Triple Triad Battlehall (579), whose public hall has PvE matches | `GameMain.IsInPvPInstance` (0.612); the log writes `Competitive arena check` |
| Listed in the code | Triple Triad tournaments (940, 941), Mahjong (831); the Crucible of the Unbroken (1339 to 1343) was listed until 0.1115, no longer cut since (X-137, DEC-038 completed on 2026-11-11) | `CompetitiveZones.Listed`; the labyrinth's territories stay known (`IsLabyrinth`) |
| Outside a zone | Triple Triad against a player in a normal zone | only the Triple Triad part switches off (DEC-038, D5) |

Instances with the game's own map (X-262, 0.1308): in a duty's territory that no map of the sheets covers (`CompetitiveZones.OwnMapInstances`:
the deep dungeons, the Gold Saucer's games, the Diadem's trial, the Final Verse, one extreme trial;
[0.1308-own-map-instances](../evidence/0.1308-own-map-instances.md)), Tensho is not cut but its map window is hidden and the game's map is
left in place (`CompetitiveGate.HasOwnMap`, `MapViewVisible`); both come back on leaving.

What « switched off » means, as coded (`CompetitiveGate`): Tensho stays loaded and one guard, on the framework thread, keeps only the
reading of the zone. The framework subscriptions go through it (a test refuses one that would bypass it), the chat readers too (0.615),
and the reading of market offers during a sale listing (0.926). Display, commands (one line saying Tensho is off), context menu and
server bar are off; the game's map is given back; what was waiting for Discord is dropped; one chat line on entry, nothing on leaving.
Nothing is erased. Still active, without reading the game in combat: the « inventory changed » notice (it only marks a reread for later)
and the change of the system theme setting. In case of doubt (unknown zone during a loading, sheet unreadable after a game update),
Tensho stays off; loaded while already in PvP, it starts off.

Known gaps:

- The hooks stay in place (macro, sequence recording) but let the game's call through without doing anything; they are not removed.
- A web request that has already left finishes normally; no new one leaves. It is not cancelled.
- An inventory sort or a macro write already in progress stops in the middle and resumes on leaving. Not tried.
- An alarm whose time falls during the match does not ring afterwards; one whose advance notice begins during the match and whose
  time falls after rings on leaving.
- If the game sheets become unreadable after an update, Tensho stays off everywhere while connected, with a warning in the log only.

---

# Architecture (français)

## Vue d'ensemble (relue contre le code le 2026-11-09)

> Carte du code, pas un journal : le détail par version est dans le [CHANGELOG](../../CHANGELOG.md), l'état en jeu dans le
> [plan global](GLOBAL-PLAN.md) et [TESTING](TESTING.md). Le code fait foi. Les notes de conception écrites pour chaque objectif du
> 2026-10-27 au 2026-10-29 (l'archive « Historique des domaines et des versions ») ont été retirées de ce fichier le 2026-11-08 : leurs
> noms, compteurs et états étaient ceux de leur date. Elles restent lisibles dans git (ce fichier au commit `2da535d`, 0.865.0).

Un projet plugin (`src/TenshoAssistant`, .NET 10, Dalamud.NET.Sdk 15, une seule DLL, plus à côté la bibliothèque native du solveur
Raphael facultatif) et un programme de tests sans dépendance native (`tests/TenshoAssistant.Tests`) qui compile par lien les fichiers C#
purs du plugin et lit les fichiers de données embarqués depuis les sources.

| Fichiers | Rôle |
|---|---|
| `*.Dalamud.cs` | touchent au jeu (services Dalamud, FFXIVClientStructs) ; jamais compilés dans les tests |
| `*.Lumina.cs` | copies typées de feuilles Excel, partagées avec les sondes de `tools/` |
| autres | C# pur (modèles, règles, services) ; testés quand le projet de tests les inclut |

### Hôte et cycle de vie (`Plugin.cs`)

- `Plugin` reçoit les services Dalamud par injection, charge `Configuration` v1 (une autre version lève une erreur, le fichier n'est pas
  écrasé), crée les services, `MainWindow`, `MapViewWindow`, `GuideWindow`, les `PanelWindow` libres et `AttachedPanels` (panneaux à côté
  des fenêtres du jeu, dans le même `WindowSystem`), et enregistre chaque lecteur, chargeur, pont et service réseau dans
  `RegistrationScope` : nettoyage en ordre inverse, idempotent, poursuivi après erreur ; un échec du constructeur déclenche le même
  nettoyage.
- Coupure compétitive (DEC-038) : chaque abonnement au framework passe par `Competitive/CompetitiveGate` ; tant que le joueur est en JcJ
  ou en contenu classé (`CompetitiveZones`, d'après les données du jeu), le plugin entier s'arrête, sans réglage, et reprend ensuite.
- Commande `/tensho` : ouvre la fenêtre ; `peche`, `pêche`, `fish` ou `fishing` ouvrent ou ferment l'aide à la pêche ; `todo <tâche>` (ou
  `tache`, `tâche`) ajoute une tâche aux Rappels ; `wiki <objet>` et `lodestone <objet>` ouvrent la page de l'objet dans le navigateur ;
  `objet`, `prix`, `recolte` ou `recette` (aussi les mots anglais `item`, `price`, `gather`, `recipe`, `craft`) suivis d'un nom d'objet
  ouvrent le premier objet trouvé dans cet outil (`MainWindow.CommandRoute`) ; `selftest` écrit l'auto-contrôle au journal ; `linktest` (mode
  développeur) affiche des liens d'essai. `OpenMainUi`, `OpenConfigUi` (Paramètres), ouverture à la connexion (`OpenOnLogin`). Polices de
  la fenêtre Tensho construites par `UI/FontManager` (police de titre du jeu par défaut), libérées au déchargement.
- Les panneaux à côté des fenêtres du jeu passent par `Panels?.Show(...)` (`AttachedPanels`), par exemple : aide du carnet de synthèse,
  projection de la synthèse, panneaux de récolte et d'objets de collection, panneaux de pêche et de pêche au harpon, traversée de pêche en
  mer, survol d'objet, bouton de tri de l'inventaire, ventes des servants, outil de mission, fenêtre Personnage, emplacements de la Revue
  de mode, demande de deck de Triple Triade. Le panneau d'import de banque ne s'affiche que si la capture automatique est coupée
  (`BankImportOverlay`).

### Carte des modules (`src/TenshoAssistant/`)

| Dossier | Contenu principal |
|---|---|
| `Items/` | catalogue d'objets commun (`ItemCatalog` + chargeur + cache, déblocages monture / mascotte / orchestrion), usages (`ItemUseCatalog`), survol (`ItemHover`, `ItemHoverReader`), menu contextuel de l'inventaire du jeu (`ItemContextMenuBridge`, `ItemContextMenuSession`), cristaux, emplacements, `TryOn` (cabine d'essayage du jeu), notes d'objets et liste d'envies (`ItemNotes`, `Wishlist`), approvisionnement du jour de la Grande Compagnie (`GcSupplyToday`), liens externes (`ExternalLinks`), filtres de recherche |
| `Inventory/` | inventaire live (`InventoryService`, `DalamudInventoryReader`), Banque (`InventoryBankService`, `PerCharacterBankStore` + `FileInventoryBankStore` : un fichier par personnage ; `BankInventoryBridge` : servants, sacoches, arsenal, coffre à apparence, armoire), possession (`ItemHoldings`, `OtherCharacterStocks`), tri visuel natif (règles, portées, modèles, aperçu, `InventoryVisualSort` + pont), monnaies (`Currencies`, `CurrencyWatcher`), journal des gils (`GilJournal`, `GilWatcher`), mise en évidence et rapport des rangements |
| `Market/` | Universalis : résumé et prix au survol (`MarketService`, `MarketWorldReader`, `MarketHover`, `ItemHoverCoordinator`) ; annonces, historique et coût pour N unités (`MarketBoard`, `MarketBoardService`) ; coût de préparation (`PreparationCost`, `PreparationPriceSession`) ; ventes des servants (`RetainerSales` + lecteur), surveillance des prix cassés (`Undercut` + veilleur), prix vus au tableau des ventes (`SeenPrices` + lecteur), rentabilité des fabrications (`CraftProfit`), valeur des monnaies (`CurrencyValue`) |
| `Craft/` | recettes et actions (catalogues + chargeurs), simulateur, solveur (`CraftSolveSession`) et solveur Raphael facultatif (`RaphaelCraftSolver`, `RaphaelSequenceCache`), enveloppe des conditions, synthèse en direct (`CraftLive*`, sur option), aide du carnet (`CraftLog*`), plan et chaîne (`CraftPlan`, `CraftChain*`), préréglages, export et import de macros, listes d'artisanat (`CraftLists`), enregistreur d'artisanat (`CraftRecorder`, `CraftRecording`), lien d'export Teamcraft (`TeamcraftExport`), fabrication depuis le stock, consommables |
| `Gathering/` | points de récolte et de pêche (catalogue + cache), horaires (`GatheringSchedule`, `GatheringTimeline`), météo (`WeatherCatalogLoader`, `WeatherForecast`), carnet et prévisions de pêche (`FishingNotebook`, `FishingForecast`, `FishNow`, `FishWindows`), observation de la fenêtre de récolte, PR (`GpActions`, `GpOptimizer`, `GpNextAction`), estimation d'Abondance II (`BountifulEstimate`), objets de collection, pêche au harpon, pêche en mer (`OceanFishing`, `OceanVoyage` + lecteur), carnets, itinéraire de récolte, tic de régénération des PR |
| `Community/` | accès réseau borné (`BoundedHttp`) et sources de la communauté : Garland Tools (`GarlandClient`, `GarlandItemSources`), wiki (`WikiItemAcquisition`), LuminaSupplemental, GatherBuddy (`GatherBuddyFish`), Teamcraft (`FishBiteTimes`), PaissaDB (`PaissaDb`), analyseurs de pêche (`FishingSources`, `FishingConditions`), noms de la communauté (`CommunityNames`) |
| `Monsters/`, `Duties/` | butin des monstres (`MonsterLootService`, `WikiMonsterSources`), sources de la magie bleue (`BlueSpellSources`, `BlueSpellWikiIndex`, `BlueSpellMonsterIndex`), index du dresseur sur le wiki (`BeastWikiIndex`), sens des monstres proches (`NearbyMonsters`, information seulement) ; contenus (`DutyCatalog`), butin (`DutyLootService`), panneau de l'outil de mission (`DutyFinderReader`), progression des contenus (`DutyProgress` + lecteur) |
| `Fashion/` | Revue de mode : données de la communauté (`FashionDataService`), indices (`FashionHintLoader`), fenêtre du jeu (`FashionCheckReader`) |
| `Maps/` | carte Tensho : catalogue, marqueurs (`MapMarkerSource`), projection, état live (`MapLiveReader` : position, drapeau, ALÉA, groupe, objets et monstres proches, téléportation), index local (`MapLocalIndex` + chargeur), remplacement de la carte du jeu (`NativeMapReplacer`), favoris et répertoire des lieux, exploration du wiki (`WikiVistas`), liens de carte du chat (`ChatMapLinks` + veilleur), train de chasse (`HuntTrain`, `HuntAnnouncer`), découverte des cartes, recherche, mémoire du zoom, cartes au trésor, ressources de l'île |
| `Npcs/` | marchands PNJ (`NpcVendorCatalog` + chargeur) : boutiques à gils, échanges, boutiques de compagnie libre |
| `Quests/`, `Collections/` | quêtes (lecteur, récompenses et index inverse, objets de quête, cités de départ, options non prises) ; collections, titres, succès, orchestrion (`CollectionReader` + partiel `Completion`, `ExtraCollectionReader`, `BgmReader`), tenues, progression des succès, titre selon le job (`TitleChanger`) |
| `Character/` | statistiques du personnage (`CharacterStatsReader`, `StatFormulas`), ce que Tensho a lu en dernier pour chacun des personnages du joueur (`CharacterStatus` + veilleur) |
| `Gear/` | planificateur d'équipement : `GearCatalog` (pièces, règles, matérias et plafonds, partage `TENSHO-GEAR1:`), `GearLoader`, ensembles de plans, comparaison, ensembles d'équipement (`Gearsets`, `GameGearsetReader`), usure, niveaux des jobs (`JobProgress`), barde de chocobo (`ChocoboBarding` + lecteur) |
| `FreeCompany/` | sous-marins et dirigeables : `Voyages` (relevés, fusion par maison), `VoyageWatcher` (lecture de l'atelier, catalogues), `VoyagePlanner` (meilleurs trajets), `VesselBuilds` (montages, paliers, usure), secteurs et planificateur des dirigeables, historique des voyages, projets et livraisons de l'atelier, `Data/` (fichiers embarqués) |
| `Island/` | Félicité insulaire : catalogue + chargeur, lecteur (sur clic du joueur), planificateur de l'atelier, matériaux, animaux du pâturage |
| `Deliveries/`, `GrandCompanies/`, `Societies/`, `Leves/`, `Hunting/` | commandes et livraisons spéciales ; Grandes compagnies ; peuples alliés ; mandats (`LeveCatalog`, `LeveProgressReader`) ; bestiaire (`HuntingLog` + lecteur) et conditions des rangs S (`SRankConditions`) |
| `Carnivale/`, `Relics/`, `Fields/`, `DeepDungeons/`, `Lexicon/` | Grande Mascarade ; armes et armures reliques ; Eureka et Bozja (`FieldBoard`) et ALÉA ; donjons profonds ; lexique des mots du jeu — chacun un modèle et son lecteur `*.Dalamud.cs` |
| `Events/` | événements saisonniers (`Festivals`, `FestivalReader`), `Data/festival-names.csv` (embarqué) |
| `Discoveries/` | base locale de découvertes (`DiscoveriesModel` : faits avec leur provenance, import avec fusion, export ; les faits du joueur dans `Discoveries.json`, les faits partagés dans `Shared/`, un fichier compressé par sorte, lu seulement quand une page a besoin de cette sorte), ses réglages (`DiscoverySettings`), ce qu'elle relève dans le jeu (`ItemDiscoveryWatcher`, `ItemGains`), et l'échange avec les autres joueurs (`GitHubExchange`, `CommunityDiscoveries` : connexion GitHub sur clic du joueur, ses faits envoyés dans son gist avec un ticket, le fichier commun `community-discoveries/discoveries.json` joint comme importé ; « Échanger maintenant » ou l'échange quotidien sur option) ; des lignes du jeu et des lieux seulement, jamais un autre joueur |
| `Competitive/` | coupure en JcJ et en contenu classé (`CompetitiveGate`, `CompetitiveZones`, DEC-038) |
| `Credits/` | liste unique des crédits (`CreditCatalog` : le jeu, bibliothèques, données intégrées, données en ligne, règles reprises, références lues, outils), affichée par `MainWindow.Credits.cs` et vérifiée par les tests (données intégrées, hôtes réseau, réglages, avis MIT) |
| `Alarms/`, `Reminders/` | alarmes Tensho (`TenshoAlarm`, `AlarmService`, commande `/alarm` du jeu via `GameAlarmCommand`, alarmes automatiques `AutoAlarms` + veilleur, entrées de la barre d'infos du serveur pour les alarmes, les rappels et les monnaies, webhook Discord `DiscordWebhook` (règles pures) et `DiscordNotifier` (file et envoi sur un worker), alarmes suivies, horloges du monde) ; rappels du calendrier (`ResetSchedule`, `GameTimers`), jardinage (journal, graines, croisements, veilleur du chat), planificateur et lecteur de l'escouade, contrats de chasse, tâches personnelles, chances du Carnet mystérieux, avantages de compagnie, `Data/` (fichiers de jardinage embarqués) |
| `MiniGames/` | Mini Cactpot (solveur, session, observation) ; Triple Triade (cartes, solveur, simulateur et conseil de match, score de carte, PNJ, observation) |
| `Macros/` | chaîne de macros (`MacroSequence`, `MacroChainService` / `MacroChainBridge`, observation) ; écriture des macros d'artisanat dans le jeu (`MacroSlotPlan`, `MacroWriter`) |
| `Caching/` | cache disque (`DataCache`), durées (`CachePreferences`), clés (`CatalogCacheKeys`, `GameCacheIdentity`), validation (`CacheSnapshots`) |
| `Localization/` | `LocalizationManager`, `LocalizationValidator`, `Languages/*.json` (embarqués) |
| `Help/`, `Guide/` | aide de chaque page et recherche (`HelpIndex`), signaler un problème (`ProblemReport`, formulaire GitHub prérempli), vidéos de la communauté (`CommunityHelpIndex`, lues sur clic) ; guide de première prise en main (`GuideFlow` ; `GuideWindow.cs`, à la racine, dans le `WindowSystem`) |
| `Diagnostics/` | auto-contrôle au rechargement en mode développeur (`SelfCheck`, `SelfCheckRunner`), passage des pages (`PageSweep`, `PageSweepWindow`, DEC-040), relevé des images lentes (`SlowFrame`), surveillance des clignotements, relevés en jeu temporaires (`ReadingCapture`) |
| `Beastmaster/` | page dresseur, données du jeu lues brutes (`BeastCatalog` + chargeur) ; sonde des fenêtres (`BeastWindowProbe`), sans panneau de conseil (DEC-032) |
| `MyTools/` | outils du joueur et galerie de la communauté, données seulement (`ToolTemplate`, `ToolTemplateStore`, `ToolGallery` lue sur clic) |
| `UI/` | thème (`FfxivTheme`, `ThemePalettes`, `GameThemeReader`), polices (`FontManager`, `FontSettings`), `UiComponents`, `UiMessages`, `InternalMessages`, enregistrement d'un réglage avec retour arrière (`SettingSave`), fenêtres visibles du jeu (`GameWindowsReader`, `GameWindowFollower`), modèles communs `Components/`, barre de navigation `Navigation/` |
| racine | `Plugin`, `Configuration` (+ `Configuration.Dalamud.cs`), `CommunitySources` (« Données du jeu et observées seulement »), `RegistrationScope`, fichiers enregistrés (`AtomicFile` : fichier temporaire puis échange ; `BackgroundWriter` : écritures hors du thread framework, dans l'ordre ; `BackupZip` : copie zip avant de vider un dossier), `OnceSet` (« une fois par événement » borné), `MainWindow.*.cs` (un partiel par page ou partie), `MapViewWindow`, `GuideWindow`, `PanelWindow`, `AttachedPanels`, `BankImportOverlay` |

### Fenêtre principale

`MainWindow` : barre latérale rangée par groupes repliables (état gardé dans `NavigationCollapsed`), « objet en contexte » sous la barre,
page de contenu à droite. Chaque page est une ligne de `MainWindow.Pages.cs` (`PageEntries`) : son groupe, son libellé, sa méthode de
dessin et sa source ; la navigation, le dessin et la source montrée aux contributeurs la lisent tous. Aujourd'hui :

| Groupe | Pages (entre parenthèses, les pages atteintes seulement par des onglets) |
|---|---|
| — | Accueil & outils, Mes outils |
| OBJETS | Objets (recherche + fiche objet), Rangements, Tri de l'inventaire, Marché, Lexique |
| PRODUCTION | Artisanat (synthèse en direct, préréglages), Récolte (planification des PR, objets de collection), Pêche en mer, Félicité insulaire, Commandes spéciales, Enchaînement de macros |
| MONDE | Carte, Météo, ALÉA, Alarmes, Rappels, Jardinage, Compagnie libre, PNJ vendeurs, Butin des contenus, Butin des monstres, Découvertes |
| PROGRESSION | Quêtes, Événements, Collections (magie bleue), La Grande Mascarade, Reliques, Peuples alliés, Grandes compagnies, Livraisons spéciales, Eureka et Bozja, Donjons profonds, Équipement, Chocobo, Mandats, Bestiaire, Dresseur |
| GOLD SAUCER | Revue de mode, Triple Triade, Mini Cactpot |
| PRÉFÉRENCES | Aide, Paramètres (Général, Interface, Modules, Carte, Données, Avancé ; Debug en mode développeur), Crédits, Diagnostics (mode développeur) |

La carte a sa propre fenêtre (`MapViewWindow`), dont le contenu et l'état d'ouverture restent dans `MainWindow.MapView.cs`.

Modèles communs (`UI/Components/`) :

- `ItemCell.cs` : `DrawItemIcon` / `ItemCell`, l'icône d'objet de tous les modules : infobulle d'objet commune (une seule par image),
  pastille ✓ / ✗ des objets de collection (inconnu = pas de pastille), clic droit = outils utiles (`RelevantRoutes`) et sources groupées
  (`SourceLines`, `MainWindow.SourceLinks.cs`), identifiant de menu unique (`UniqueMenuId`). Le même contenu (`DrawItemInfoBody`) sert au
  panneau sous l'infobulle du jeu ; les mêmes sources, au menu Tensho de l'inventaire du jeu.
- `SettingRow.cs` : réglage avec infobulle et bouton ↺ (défaut lu sur une `Configuration` neuve) ; « Tout remettre par défaut ». Un
  enregistrement raté remet l'ancienne valeur et affiche une ligne d'erreur en bas de la fenêtre principale (`UI/SettingSave.cs`).
- `Tooltip.cs` : une seule aide pour les infobulles (aussi sur les commandes grisées) ; `StatusLines.cs` : un seul aspect pour les lignes
  vides ou de chargement.
- `DetailPage.cs` : en-tête puis onglets, seul l'onglet ouvert est calculé (fiche objet, sous-onglets de Triple Triade).
- `EntityMenus.cs` : un menu de clic droit par type de chose (quête, PNJ, monstre, lieu).
- `DataIndexes.cs` : index des collections et des quêtes, refaits quand la liste est relue, jamais à chaque image.

### Flux de données

```
données du jeu (feuilles Lumina, mémoire native)
  └─ lecteurs / chargeurs *.Dalamud.cs sur IFramework.Update : copie en records managés
       └─ worker éventuel (index, catalogue) → publication atomique d'un état immuable (Volatile / verrou, génération)
            └─ Draw (MainWindow, panneaux) : lit l'état publié, met ses calculs en cache, n'attend jamais
sources réseau (Community/, Market/, Fashion/, Maps/WikiVistas, Monsters/) → requête bornée → état publié + DataCache
actions du joueur (retrait du drapeau, téléportation, /alarm, macros, tri, import) → demande en file → exécutée sur IFramework.Update
```

- Catalogues statiques : copie par lots sur le thread framework (quelques lignes ou environ 2 ms par image), puis index sur un worker ;
  copie gardée en cache disque (`catalog-*`).
- Lectures live : chaque lecteur a sa cadence, écrite dans son code ; par exemple les fenêtres visibles du jeu dix fois par seconde, les
  alarmes chaque seconde, les alarmes automatiques et l'état des personnages toutes les 30 secondes, les fêtes en cours toutes les
  10 secondes, les découvertes dix fois par seconde. Les observations de la synthèse, du Mini Cactpot et de la Triple Triade ne tournent
  que si elles sont activées.
- Chaînes natives lues avec une borne (exemple : `Utf8String` des infobulles de la carte, longueur = `BufUsed − 1`, voir
  [l'extrait du plantage](../evidence/0.133.0-crash-excerpt.log)) ; recherche de nœuds d'addon bornée.

Écarts connus à la règle « Draw ne lit ni pointeur ni feuille Excel » (relevés le 2026-11-02, pas revérifiés depuis) :

- `MainWindow.Reminders.cs` appelle `GameTimers.Read()` (QuestManager, SatisfactionSupplyManager, RetainerManager) depuis Draw, au plus
  une fois par seconde et seulement page Rappels ouverte.
- Fonctions passées par `Plugin.cs` et appelées depuis Draw : aide à la pêche (joueur, territoire, condition « pêche » à chaque image ;
  table `FishingSpot` construite au premier appel), base d'Abondance II (`GatheringItem`, `ItemLevel`, construite au premier appel),
  noms de jobs, teintures et thèmes de la Revue de mode (`TryGetRow` à l'appel).
- `TryOn.Item` (`AgentTryon.TryOn`) est appelé depuis Draw, sur un clic du joueur.
- Pose du drapeau : `IGameGui.OpenMapWithMapLink` est appelé depuis Draw, sur un clic ; le retrait du drapeau, lui, passe par la file de
  `MapLiveReader`.
- `MapMarkerSource` lit depuis Draw, au premier usage de chaque clé, les feuilles `MapMarker`, `Map` (cartes de région et du monde),
  `EventIconType` et `MapSymbol`, puis garde la copie en mémoire.
- `VoyageWatcher` construit ses catalogues de secteurs et de pièces en une seule fois sur le thread framework (premier passage), pas par
  lots ; `GearLoader` et `TriadNpcLoader` lisent les feuilles Lumina directement sur un worker.

### Travail en tâche de fond et annulation

| Propriétaire | Travail | Fin et annulation |
|---|---|---|
| chargeurs de catalogues (`ItemCatalogLoader`, `RecipeCatalogLoader`, `GatheringCatalogLoader`, `NpcVendorLoader`, `MapLocalIndexLoader`, `MapCatalogLoader`, `DutyCatalogLoader`, `ItemUseCatalogLoader`, et les autres nommés `*Loader`) | index sur worker après la copie framework | résultat consommé à un `Update` suivant ; `Dispose` désabonne et annule |
| `GearLoader`, `TriadNpcLoader` | catalogue depuis Lumina sur worker | jeton annulé au `Dispose` |
| sessions : `CraftSolveSession`, `CraftChainSession`, `CraftLogSession`, `TriadSolveSession`, `MiniCactpotSolveSession`, `PreparationPriceSession`, index des récompenses de `QuestService` | calcul ou requêtes | génération + jeton ; résultat tardif rejeté ; fermées par `MainWindow.Dispose` ou par leur service |
| pages de `MainWindow` : conseil et demande de deck de Triple Triade, meilleurs trajets, meilleurs montages | recherche sur worker, relancée (l'ancienne annulée) quand ses entrées changent | annulées par `MainWindow.Dispose` (`MainWindow.Lifetime.cs`) |
| services réseau (`MarketService`, `MarketBoardService`, `GarlandClient` et `GarlandItemSourceService`, `WikiItemAcquisitionService`, `WikiMonsterSources`, `WikiVistaSource`, `LuminaSupplementalService`, `GatherBuddyFishService`, `FishBiteTimeService`, `FashionDataService`, `PaissaDbService`, `ToolGallery`) | une requête à la fois ou une file, réponse bornée | `Dispose` par `RegistrationScope` ; LuminaSupplemental et GatherBuddy attendent 5 min après un échec et gardent leur copie expirée si le réseau échoue |
| `InventoryBankService` | lecture / écriture des fichiers de la Banque (file bornée à une demande) | `Dispose` ferme la file sans attendre une E/S engagée |
| `DataCache` | lectures et écritures disque hors framework | `Dispose` attend 2 s au plus, puis annule |

### Configuration et persistance

- `Configuration` v1 (`Configuration.cs`, C# pur partagé avec les tests ; `Configuration.Dalamud.cs` ajoute `IPluginConfiguration`),
  enregistrée par `SavePluginConfig`. `MainWindow` écrit les réglages par `SaveSetting`, qui remet l'ancienne valeur si l'enregistrement
  échoue.
- Contenu : réglages (dont `MapViewSettings`, `Cache`, les interrupteurs des sources de la communauté, `Discovery`) et une partie des
  données de l'utilisateur (alarmes, rappels faits, vaisseaux lus, secteurs débloqués, plans de montage, plans d'équipement et de
  matérias, préréglages, decks, quêtes suivies, favoris, plan de macros, profils et modèles de tri, notes d'objets, liste d'envies,
  parcelles du jardin, tâches personnelles…).
- Défauts : une `Configuration` neuve sert de référence aux boutons ↺. « Tout remettre par défaut » (Paramètres › Avancé,
  `UI/Components/SettingRow.cs`) recopie par réflexion, depuis une configuration neuve, les seules propriétés listées dans
  `ConfigurationReset.Reset` (`Configuration.cs`) ; l'adresse du webhook Discord tapée est gardée. `ConfigurationReset.Kept` garde les
  données de l'utilisateur et l'état de l'interface. Un test exige que chaque propriété publique soit dans exactement une des deux
  listes : **toute nouvelle propriété de `Configuration` doit être placée dans l'une d'elles**.
- Fichiers à part dans le dossier de configuration du plugin, jamais touchés par la remise à zéro : `InventoryBank/` (un fichier par
  personnage, schéma 1, 16 Mio au plus, entrées et conteneurs bornés ; écriture par fichier temporaire puis remplacement, avec `.bak`),
  `GilJournal/`, `Characters/`, `Discoveries/` (`Discoveries.json`, `Shared/`, `MarketSeen.json` ; avec l'échange GitHub, `github.json` et
  le jeton `github.token`, protégé pour cet utilisateur Windows par DPAPI), `VoyageHistory.json`, `MyTools.json`, `CraftLists.json`,
  `Exports/`, `DataCache-v1/`, `Backups/` (copies zip prises avant de vider le cache ou les découvertes, jeton GitHub exclu) ; en mode
  développeur, `SelfCheckSeen.json` et `PageSweepLast.txt` (voir [SECURITY](../../SECURITY.md)). Les fichiers enregistrés sont écrits par
  `AtomicFile` (fichier temporaire, puis échangé d'un coup).

### Caches (`Caching/`)

- `DataCache-v1` dans le dossier de configuration du plugin : un fichier par famille + portée + version, enveloppe versionnée (schéma,
  famille, portée, version, date) ; taille bornée par entrée, en nombre de fichiers et au total ; seule la dernière entrée d'une même
  famille et portée est gardée ; les catalogues (`catalog-*`) ne sont jamais évincés au nombre.
- Catalogues statiques (`catalog-items`, `catalog-recipes`, `catalog-gathering`, `catalog-npc-vendors`, `catalog-map-positions`…) : clé
  = données du jeu + langue + forme de la copie (`CatalogCacheKeys`), jamais la version du plugin.
- Instantanés par personnage (quêtes, collections) : clé `GameCacheIdentity` (version du plugin + langue + versions des données), donc
  relus après chaque nouvelle version du plugin.
- Durées par domaine (`CacheArea`, Paramètres › Données), valeurs par défaut depuis la 0.750 (X-02) : Prix 1 j, Quêtes 1 h, Collections
  7 j, Sources « Seulement si absent, ou sur Actualiser », Revue de mode 7 j, Stock importé 1 j (signale seulement l'ancienneté), Calculs
  d'artisanat 7 j, Titres et succès 7 j ; choix 5 min, 1 h, 24 h, 7 j ou « Seulement si absent, ou sur Actualiser ». Relevé complet des
  sources et cadences au moment de l'audit : [https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/evidence/0.246-cache-audit.md](https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/evidence/0.246-cache-audit.md).

### Ressources embarquées et sources externes

| Donnée | Où | Source et licence |
|---|---|---|
| langues `Localization/Languages/*.json` | embarquées dans la DLL ; un dossier `Localization/Languages` posé à côté de la DLL les remplace (traducteurs) ; Verify refuse un JSON livré dans le paquet | Tensho |
| `FreeCompany/Data/submarine-loot.json` | embarqué ; butin moyen par secteur, produit par `tools/submarine-loot.py`, lu une fois par `VoyageWatcher` | FFXIVGachaSpreadsheet (MIT, relevés de la communauté) |
| `FreeCompany/Data/submarine-breakpoints.csv` | embarqué ; seuils des secteurs (surveillance, récupération, faveur) | SubmarineTracker (MIT, avis de licence en tête du fichier) |
| `FreeCompany/Data/airship-unlock.csv`, `airship-drop.csv` | embarqués ; secteurs des dirigeables | LuminaSupplemental (GPL-3.0, avis dans le fichier) |
| `Events/Data/festival-names.csv` | embarqué ; noms anglais des événements saisonniers | LuminaSupplemental (GPL-3.0, avis dans le fichier) |
| `Reminders/Data/garden-seeds.csv`, `garden-crosses.csv` | embarqués ; durées de pousse et croisements, produits par `tools/garden-data.py` | FFXIV Gardening Tracker (GPL-3.0), FFXIV Crossbreed Helper (MIT), avis dans les fichiers |
| table du modificateur de statistique principale (niveaux 1–100) | dans le code (`Character/StatFormulas.cs`) | Character Panel Refined (MIT) |
| `images/icon.png` | copiée à côté de la DLL (non embarquée) | Tensho |
| prix, annonces, historique, centres de données | réseau (`HttpClient` borné) | API Universalis |
| sources d'objets, butins | réseau (`BoundedHttp`) | Garland Tools |
| butin des monstres, acquisition, exploration, magie bleue, pages du dresseur et du pâturage | réseau (`BoundedHttp`) | ffxiv.consolegameswiki.com |
| butin des contenus, monstres, ALÉA, entreprises, boutique, désynthèse… | réseau (GitHub, `BoundedHttp`) | LuminaSupplemental (GPL-3.0) |
| appâts, conditions et créneaux de pêche | réseau (GitHub, `BoundedHttp`) | GatherBuddy (Apache-2.0) |
| temps de touche | réseau (GraphQL, `FishBiteTimes`) | rapports publics de Teamcraft (MIT) |
| Revue de mode (`FashionReport.json`) | réseau (GitHub, `BoundedHttp`) | FFXIVGachaSpreadsheet (MIT) |
| terrains libres | réseau (`BoundedHttp`) | PaissaDB |
| fichier commun des découvertes (`community-discoveries/discoveries.json`) | réseau (raw.githubusercontent.com), sur « Échanger maintenant » ou l'échange quotidien sur option | dépôt de Tensho (relevés des joueurs) |
| connexion, gist et ticket de l'échange des découvertes | réseau (github.com, api.github.com), seulement après que le joueur a connecté GitHub | GitHub |
| messages des alarmes, des monnaies et de l'outil de mission | réseau (webhook du joueur, hôtes discord.com seulement) | Discord |

Règle d'embarquement : seulement une donnée sous licence compatible avec la GPL v3, avec source, licence et copyright dans le fichier
([DEC-021](DECISIONS.md)). Références lues sans copie de code : listées sur la page Crédits (`Credits/CreditCatalog.cs`, « références
lues ») et dans [REFERENCES.md](REFERENCES.md) ; crédits : [README](../../README.md).

### Carte Tensho (`Maps/`, `MainWindow.MapView.cs`, `MainWindow.MapLocalLists.cs`)

- `MapCatalogLoader` copie la feuille `Map` (cartes des zones) ; `MapMarkerSource` lit à la demande, depuis Draw, `MapMarker`, `Map`
  (régions et monde), `MapSymbol` et `EventIconType`, gardés en mémoire (écart connu, voir plus haut) ; `MapProjection` convertit monde ↔
  texture ↔ écran.
- `MapLiveReader` publie position, drapeau, ALÉA, groupe, marqueurs d'événement du jeu (quêtes, mandats), objets proches, ennemis vivants
  autour du joueur (information seulement, 100 au plus), courants éthérés et repères d'exploration débloqués, combat / instance ; il
  exécute sur le thread framework les demandes de l'interface (retirer le drapeau, téléportation, coûts de téléportation).
- `MapLocalIndexLoader` construit l'index local par carte (donneurs de quête, points de récolte et de pêche, exploration, courants) ;
  `NpcVendorLoader` les marchands. Les deux passent par le cache disque. Les PNJ placés par un événement ne sont montrés que pendant cet
  événement.
- `NativeMapReplacer` garde l'addon `AreaMap` ouvert hors écran pour que le jeu continue à mettre à jour ses marqueurs, lit la demande
  d'ouverture (`AgentMap`, quête du journal, téléporteur, ALÉA) et ouvre la carte Tensho à la place ; la carte du jeu est restaurée à la
  fermeture ou au déchargement.
- Dispositions : classique (menus de région / zone en haut, colonne d'outils à gauche) ou « Menus et outils par-dessus la carte »
  (`OverlayControls`) : outils et noms soumis avant la carte (fenêtres enfants) pour prendre le clic, la carte ignore la souris sur eux.
  Repères au bord pour les cibles hors de la vue (`DrawEdgeMarkers`).
- Clic droit : menu propre à l'icône (`RightClickIgnore` renvoie une icône au menu général), menu général à onglets (Favoris, Quêtes,
  ALÉA, Récolte, Éthérites, Vendeurs, Triple Triade, Courants éthérés, Carnet d'exploration).
- Réglages : `MapViewSettings` dans `Configuration` (tailles et priorités d'icônes globales et par carte, zones colorées, cône de vision,
  repères au bord, disposition, couches des monstres proches, zoom à l'ouverture).

### Sources d'objets (« Comment l'obtenir »)

Ordre : données du jeu (marchands PNJ dont boutiques de compagnie libre, échanges, quêtes, succès, recettes, récolte), données de
LuminaSupplemental (contenus, monstres, ALÉA, entreprises, boutique, objets d'origine), puis Garland Tools, puis section « Acquisition »
du wiki. « Comment l'obtenir » n'affiche Garland que si aucune autre source n'est connue, et le wiki que si Garland ne connaît rien non
plus ; mais Garland est interrogé pour presque chaque objet mis en contexte (objet sélectionné, fiche, menus Tensho et du jeu), car les
outils utiles (`RelevantRoutes` : contenus, monstres) et `SourceLines` (contenus, échanges) le lisent. Garland et le wiki se coupent dans
Paramètres › Avancé. Les sceaux d'une autre Grande Compagnie que celle du personnage sont écartés (compagnie inconnue = tout affiché). Le
marché est un critère, pas une source. Les lignes sont calculées une fois (`SourceLines`) et affichées dans le menu Tensho comme dans le
menu de l'inventaire du jeu.

### Interface et langues

`UI/FfxivTheme.cs` porte la palette (suivie sur le thème du jeu par `GameThemeReader`, ou forcée) ; PreDraw / PostDraw pour les fenêtres,
scopes jetables pour les panneaux. `LocalizationManager` charge les JSON embarqués (anglais = référence et repli, français complet,
allemand et japonais partiels) ; un test fait échouer Verify si une infobulle de réglage citée dans le code manque en anglais ou en
français. Les données du jeu (objets, actions, lieux) viennent du catalogue du jeu, dans la langue du jeu ; les textes propres à Tensho
sont dans la langue du plugin. Voir [Localization/README.md](../../src/TenshoAssistant/Localization/README.md). Dates affichées en heure locale ; calculs en
UTC et heure d'Éorzéa.

La compilation et les tests managés ne valident pas le rendu ImGui ni la lecture native : seul l'essai en jeu le fait (état par version :
[plan global](GLOBAL-PLAN.md), [TESTING](TESTING.md), [CHANGELOG](../../CHANGELOG.md)).

### Coupure en JcJ et en contenu classé (`Competitive/`, DEC-038)

Codée en 0.610 ; le plan du 2026-11-05 a été fondu ici et dans DEC-038 en 0.1087 (l'historique git le garde). Zones et drapeaux lus
dans les données du jeu : [0.600-competitive-inventory](../evidence/0.600-competitive-inventory.md).

| Niveau | Contenu | Comment on le sait |
|---|---|---|
| Drapeaux du jeu | JcJ, Lord of Verminion ; les donjons profonds (type de contenu 21) étaient coupés jusqu'en 0.1308, plus depuis (X-262, DEC-038 complétée le 2026-11-13) | `IClientState.IsPvP` (`EnterPvP`, `LeavePvP`), `TerritoryType.IsPvpZone`, `ContentFinderCondition.PvP` (`CompetitiveZones.Build`) : les nouvelles cartes sont couvertes sans mise à jour ; `IsPvP` seul est faux au Battlehall (579) et à Lord of Verminion |
| Arène seulement | Battlehall de Triple Triade (579), dont la salle publique a des matchs JcE | `GameMain.IsInPvPInstance` (0.612) ; le journal écrit `Competitive arena check` |
| Listé dans le code | Tournois de Triple Triade (940, 941), Mahjong (831) ; le Dédale bestial (1339 à 1343) y était jusqu'en 0.1115, il n'est plus coupé depuis (X-137, DEC-038 complétée le 2026-11-11) | `CompetitiveZones.Listed` ; les territoires du Dédale restent connus (`IsLabyrinth`) |
| Hors d'une zone | Triple Triade contre un joueur dans une zone normale | seule la partie Triple Triade se coupe (DEC-038, D5) |

Instances à carte propre (X-262, 0.1308) : dans le territoire d'un contenu qu'aucune carte des feuilles ne couvre
(`CompetitiveZones.OwnMapInstances` : les donjons profonds, les jeux du Gold Saucer, l'épreuve du Diadème, le Verset final, une épreuve
extrême ; [0.1308-own-map-instances](../evidence/0.1308-own-map-instances.md)), Tensho n'est pas coupé mais sa fenêtre de carte est cachée
et la carte du jeu laissée en place (`CompetitiveGate.HasOwnMap`, `MapViewVisible`) ; tout revient à la sortie.

Ce que « coupé » veut dire, tel que codé (`CompetitiveGate`) : Tensho reste chargé et un seul gardien, sur le thread du framework, ne
garde que la lecture de la zone. Les abonnements au framework passent par lui (un test refuse celui qui le contournerait), les lecteurs
du chat aussi (0.615), et la lecture des offres du marché pendant une mise en vente (0.926). Affichage, commandes (une ligne qui dit que
Tensho est coupé), menu contextuel et barre de serveur sont coupés ; la carte du jeu est rendue ; ce qui attendait Discord est abandonné ;
une ligne dans le chat à l'entrée, rien à la sortie. Rien n'est effacé. Restent actifs, sans lire le jeu en combat : l'avis « inventaire
modifié » (il ne fait que marquer une relecture pour après) et le changement de réglage système du thème. En cas de doute (zone inconnue
pendant un chargement, feuille illisible après une mise à jour du jeu), Tensho reste coupé ; chargé alors qu'on est déjà en JcJ, il
démarre coupé.

Écarts connus :

- Les hooks restent posés (macro, enregistrement de séquence) mais laissent passer l'appel du jeu sans rien faire ; ils ne sont pas retirés.
- Une requête web déjà partie finit normalement ; aucune nouvelle ne part. Elle n'est pas annulée.
- Un tri d'inventaire ou une écriture de macro déjà en cours s'arrête au milieu et reprend à la sortie. Pas essayé.
- Une alarme dont l'heure tombe pendant le match ne sonne pas ensuite ; celle dont le préavis commence pendant le match et dont l'heure
  tombe après sonne à la sortie.
- Si les feuilles du jeu deviennent illisibles après une mise à jour, Tensho reste coupé partout tant qu'on est connecté, avec un
  avertissement dans le journal seulement.
