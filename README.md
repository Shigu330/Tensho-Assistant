# TenshoAssistant

TenshoAssistant is a Dalamud plugin for Final Fantasy XIV that brings everyday tools together in one window, opened with `/tensho`:
items and storage, inventory sorting, market prices, crafting, gathering and fishing, a map that replaces the game map, weather, alarms
and reminders, free company voyages, quests, collections, character stats, a gear planner, Gold Saucer helpers, macro chains,
the island sanctuary, side content (Eureka and Bozja, deep dungeons, relics, Masked Carnivale), a local base of what you discover
while playing, a French–English lexicon of the game's words, your own tools, and diagnostics.

The current version is the `<Version>` of `src/TenshoAssistant/TenshoAssistant.csproj`. What changed in each version is in
[CHANGELOG.md](CHANGELOG.md); what is validated in game, and what is not yet, is in [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md). Map of all
documents: [docs/README.md](docs/README.md).

## Overview

The sidebar groups the pages in foldable categories (the list is `MainWindow.Pages.cs`):

| Category | Pages |
|---|---|
| — | **Home**: a « Today » block (reminders to do, finished timers, next vessel back, followed quests, next alarms), ten tiles with a live line (crafting, gathering, collections, quests, map, market, alarms, macro chain, gear, Gold Saucer), work in progress and failing modules, the context item, tool directory; **My tools** |
| Items | **Items** (search and item sheet), **Storage**, **Inventory sorting**, **Market**, **Lexicon** |
| Production | **Crafting**, **Gathering**, **Ocean fishing**, **Island sanctuary**, **Custom deliveries**, **Macro chain** |
| World | **Map**, **Weather**, **FATEs**, **Alarms**, **Reminders**, **Garden**, **Free company**, **NPC vendors**, **Duty loot**, **Monster loot**, **Discoveries** |
| Progression | **Quests**, **Events**, **Collections** (with Blue magic), **Masked Carnivale**, **Relics**, **Allied societies**, **Grand Companies**, **Special deliveries**, **Eureka and Bozja**, **Deep dungeons**, **Gear**, **Chocobo**, **Guildleves**, **Hunting log**, **Beastmaster** |
| Gold Saucer | **Fashion Report**, **Triple Triad**, **Mini Cactpot** |
| Preferences | **Help**, **Settings**, **Credits**, **Diagnostics** (developer mode only) |

Chat commands: `/tensho` opens the window; `/tensho fish` (also `fishing`, `peche`, `pêche`) opens or closes the fishing help;
`/tensho todo <task>` adds a free task to Reminders; `/tensho item <item>`, `price`, `gather` and `recipe` (also `objet`, `prix`,
`recolte`, `recette`) open the item in Tensho (item sheet, Market, Gathering, Crafting); `/tensho wiki <item>` and
`/tensho lodestone <item>` open the item's page in the browser; `/tenshonext` is the end-of-page marker of macro chains. Dalamud's « open » and « settings » buttons open the window and its
Settings page.

## Architecture

- `src/TenshoAssistant/Plugin.cs` registers the services, the command, the windows and the panels; every registration has its cleanup.
- `MainWindow.cs` (split into `MainWindow.<Page>.cs` partial files) is the shell and page routing; `MapViewWindow.cs` is the Tensho map,
  `GuideWindow.cs` the first-run guide, `PanelWindow.cs` the free-standing panels (tracker, tasks, wear and GP bars), `AttachedPanels.cs` the panels beside game windows. All are Dalamud `WindowSystem` windows.
- One folder per domain (`Alarms`, `Beastmaster`, `Caching`, `Carnivale`, `Character`, `Collections`, `Community`, `Competitive`,
  `Craft`, `Credits`, `DeepDungeons`, `Deliveries`, `Diagnostics`, `Discoveries`, `Duties`, `Events`, `Fashion`, `Fields`,
  `FreeCompany`, `Gathering`, `Gear`, `GrandCompanies`, `Guide`, `Help`, `Hunting`, `Inventory`, `Island`, `Items`, `Leves`, `Lexicon`,
  `Localization`, `Macros`, `Maps`, `Market`, `MiniGames`, `Monsters`, `MyTools`, `Npcs`, `Quests`, `Relics`, `Reminders`,
  `Societies`). Files ending in `*.Dalamud.cs` touch the game (read on the framework thread, then copied); the others are pure C# and
  tested. `Competitive/` holds the places where the whole plugin stops (PvP zones, deep dungeons and other ranked content).
- `UI/Components/` holds the shared templates: `ItemCell` (item icon and name with the common tooltip and tools menu), `DetailPage`,
  `SettingRow` (tooltip and « restore default » ↺ button), `EntityMenus` (quest, NPC, monster and place menus), `DataIndexes`.
- `Community/` holds the bounded HTTP clients (size and time limits); `Caching/` the disk cache, with a duration per data area.
- Interface text lives in JSON language files embedded in the DLL (`Localization/Languages`).

## Main feature domains

What the code does today. In-game validation of each part is tracked in [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md).

### Items, storage and market

- **Item sheet**: search (with category, level, slot, tradable and market filters; key items by name), then header (stock, last price)
  and tabs Overview, Stocks, Craft (short view, the full preparation is on the Crafting page), Usages, Gathering, Market, Notes (notes
  and labels « important » / « to sell to an NPC », with a reminder when an NPC shop opens); wiki and Lodestone links.
- **Every item icon**, in every module: the common item tooltip and a right-click tools menu with the sources grouped by kind
  (achievements, duties, other items, exchanges, NPC vendors, quests), « Try on » for equipment, « Add to my wishlist », and the
  obtained ✓ / not obtained ✗ mark on collectibles. Grand Company seal sources only show your own company.
- **In the game**, each an option: a Tensho entry in the inventory right-click menu, a panel under the inventory item tooltip (uses, how
  to get it, NPC sale price, comparison with the worn piece, what it teaches and whether you learned it, the retainer venture that
  brings it, its expansion when all its sure sources agree), market prices on hover (last sale, average, sales per day, region's
  cheapest, stack value; Ctrl+Alt refreshes), a « Sort » button inside the game's inventory window.
- **Storage**: Bank (retainers, saddlebags, premium saddlebag, armoury chest, glamour dresser, armoire, free company chest: each known
  once opened in game), Characters (one file per character), Crystals, Currencies (caps, weekly tomestones, cap alerts), « What to do
  with it? » (what a currency buys, its price there, what it sold for on your world, the gil each unit brings), Gil journal (90 days,
  categorized changes), Report (stacks to merge, armoury pieces used by no gear set, armoire candidates, look-alike glamour items),
  Imports.
- **Retainer sales**: beside a retainer's sale list, your items and prices as the game shows them; on a click, a comparison with other sellers through Universalis and a price to copy; the offers you saw at the board on your world are used without a click when newer. Nothing is changed in the game.
- **Inventory sorting**: profiles, templates, rules and protected zones with a preview (up to 256 categories a zone, a whole group of
  the game's categories added in one click); the sort is applied only on click.
- **Market** (Universalis): Summary, Listings (world / data centre / region, quality, quantity and world filters, cheapest set of whole
  listings for N units, tax included) and Sale history; cached on disk, the last known board is shown, dated, when Universalis fails.
  Optional undercut: the lowest offer minus 1 copied to the clipboard (off by default). The prices you see at the game's market board
  are kept per world (Discoveries setting) and used in place of Universalis' when they are newer.
- **Lexicon**: the game's words in French and in English, from its own sheets (items, actions, traits, statuses, places, monsters,
  NPCs, quests, duties, mounts, minions, FATEs, jobs, achievements, emotes, interface); search without case or accents, filter by kind,
  a click copies the word.

### Crafting and macros

- Recipe catalogue, crafting simulator (the game's crafting actions, specialist, food and medicine, expert recipes under the normal condition, forbidden
  actions setting) and rotation search (collectable thresholds as quality targets), live crafting context, presets, rotation recorder,
  pasted macro import.
- **Preparation** tab: craft chain, quantities against stocks, preparation cost and profit, recipe needs against the character
  (« Equip (job) »), recipes never crafted, Teamcraft link, crafting lists of several items, gathering list by zone.
- Macro export, and copy into the game's macros on click (book, first / last slot, prefix, overwrite; `/nextmacro` when Macro Chain is
  loaded); macro helper beside the game's crafting log; the game's crafting log opened on a recipe on click.
- **Macro chain**: macro pages ending with `/tenshonext`, started explicitly, can be stopped.
- **Tracking-list panels** (`MainWindow.ListPanels.cs`), each switched on from its page: the crafting preparation, a crafting list and
  a workshop project, on screen, stock over need. Options shared by the panels: only what is left to fetch, by origin (gathering,
  optionally grouped by gathering zone with the zone's teleport cost; NPC vendors, for the currencies you tick; monsters; other;
  crystals), finished items only, crafting steps left, timed points open or closed with a timer and « Add the alarms » for their
  openings, « Not on you » for items held in another storage, a paw for nearby monsters that drop a needed item (also on the map),
  frames on the cells of an open retainer inventory, chocobo saddlebag or company chest that hold a needed item, hide in combat or
  behind game windows (both off by default).

### Gathering and fishing

- **Gathering** page: resources and zones, timed nodes with Eorzea time windows, GP planning (Bountiful Yield II estimated by a
  community rule, Solid Reason, Clear Vision and their botanist versions), collectables table (thresholds, scrips, those in your bags),
  the three notebooks (✓, hide what is done, CSV export), « Go there » (flag, nearest aetheryte, teleport on confirmation) and « Equip
  (job) » on each spot, the game's gathering log opened on an item.
- Fishing: bait, mooch, windows and weather from GatherBuddy (Garland Tools as fallback), bite times from Teamcraft reports, next real
  windows of a fish with one-off alarms; fishing alarms carry the weather conditions and a map link.
- **Ocean fishing** page: next departures and voyages with their stops, alarm before a departure; the community timetable is checked
  against the voyages you board (« checked in game » or « not checked yet » per port).
- In-game help panels: beside the gathering window, the Collectable window and the spearfishing window; while fishing (fish of the
  nearest spot, bait in hand; `/tensho fish` opens it by hand); during an ocean voyage (stop, spectral current, fish with bait, bite and
  bite time, missions with the fish that count for them, points per fish seen, Fisher's Intuition marks, catches seen just before a
  spectral current). Optional GP tick bar and GP threshold alert.

### Island sanctuary and deliveries

- **Island sanctuary**: Workshop tab, read on click from what the game has loaded once the agenda was opened on your island (rank,
  cycle day, groove, workshops), a table of crafts (hours, themes, popularity, supply and demand, estimated value, materials), a
  proposed agenda for one cycle to copy and enter yourself, with the materials it uses; Pasture tab: your animals as the game holds
  them (mood, food, leavings, care), and the list of island animals (place, Eorzea time, weather, size) read on click from the FFXIV
  Console Games Wiki, with a flag on the island map. Values are estimates; nothing is sent to the game.
- **Custom deliveries**: your clients from what the game holds (place, level, rank, points, this week), and the rewards of each client
  from the game's sheets (three collectability amounts, weekly bonus, objects tied to each rank). This week's requested items are not
  shown.
- **Special deliveries**: the delivery quest lines of crafters and gatherers, one tab per line with done / total, from the quest
  journal; each quest opens its quest sheet.

### Map, weather, alarms and reminders

- **Tensho map**, in its own window, replaces the game map (key M, setting): game icons with size and priority per map, quests and
  leves, FATEs, party, gathering and fishing points with their times, aether currents and sightseeing vistas (collected ones hidden),
  NPC vendors, Triple Triad NPCs, nearby hunt marks coloured by rank, the deciphered treasure map spot, flag (place and remove from every
  menu), teleport with the real gil cost, bookmarks (stars, note, group, `TENSHO-MAP1` sharing code, pasted « Place ( x , y ) » lines),
  local lists by tab (quests, FATEs, gathering, aetherytes, vendors, Triple Triad, sightseeing log, currents), each icon's own
  right-click menu. Also: gathering points by job (Miner, Botanist, Fisher), quests not available yet (a box), nearby monsters for your
  lists, hunting log, FATEs, Blue Mage, missing collectibles and Beastmaster, your leve's targets, Island Sanctuary animals.
- **Maps** page: map links seen in the chat (flag, nearest aetheryte), hunt train recorder (A/S marks met, next flag, copied list and
  conductor call with `<flag>`, optional A/S mark alert), S rank spawn conditions (community knowledge, with where their FATE was seen
  in game), place directory (residential addresses), open plots of your world from PaissaDB
  (on click, optional watch, off by default).
- **Map options** (Settings › Map), each with a tooltip and ↺: menus and tools drawn over the map (off by default) with the zone name
  always shown; markers on the map edge for off-view targets, each kind switchable; position marker and view cone; area colours; hide in
  combat or duty; icon sizes and priorities.
- **Weather**: forecasts by zone; a zone whose weather the game forces until its story is done is forecast only once the game's
  weather was seen to match.
- **FATEs**: every FATE the game's files place in the open zones, running or not, with its level and place; starts on your zone,
  search across all zones; click shows it on the map, right click places the flag or picks the nearest aetheryte. The relic page's
  FATEs open here.
- **Discoveries**: a base kept on your disk (folder « Discoveries »; the shared ones apart in `Discoveries/Shared`, one compressed file
  per kind); the note-taking and its main kinds are settings, on by default. Noted while you play: where monsters live, hunt marks'
  spawn places, FATEs seen, where the items you get come from (desynthesis, aetherial reduction, ventures, duty chests, FATE rewards,
  your own target's drops, opened items), items asked at quest hand-ins, fish caught (bait or mooch, lure, place, weather, Eorzea hour,
  bite strength and time, Fisher's Intuition), spearfishing shadows, ocean fishing points and catches just before a spectral current,
  voyage loot, sectors visited and unlocked, Fashion Report judgements, festivals and vistas, blue magic spells learned or seen cast and
  beasts tamed, shopkeepers' places, counts of monsters defeated and duty chests opened, and the market prices seen at the board on
  each world you visit. Each entry says whether you saw it or imported it; export a dated copy, import files from others, empty the
  base (a zip copy is taken first in the Backups folder). No other player is ever noted. Nothing leaves your PC unless you connect
  GitHub (a code to approve on github.com, after a warning that sharing is public): « Exchange now », or the « Automatic exchange »
  setting (off by default), takes the common discoveries of the repository and sends only what you saw yourself to a secret gist in
  your name, with one issue per player; the maintainer checks and merges them. The places seen show in the Hunting log, Monster loot
  and other pages, the item sources in the item hover panel.
- **Alarms**: Tensho alarms (once at a date, every Eorzea day at an ET time, or at a real local date; weather conditions for fish), chat
  line with links and the game's alarm sound, optional copy into the game's alarm list through `/alarm` (off by default), automatic
  alarms for ventures and vessels, clocks of other time zones, an optional pinned tracking window. Alarm clock in the game's server info
  bar (Settings › General). Optional Discord webhook (off by default): ringing alarms are posted in a channel of your server, with a
  message per category or per alarm group, an optional role mention and a test button; currency cap and « duty ready » alerts too.
- **Reminders**: the game calendar timers (supply missions, custom deliveries, Fashion Report, allied society quests, leve allowances,
  treasure maps, retainer ventures, company voyages, Grand Company squadron) with resets and alarm buttons; Wondrous Tails (tasks, line
  odds), hunt bills (targets and kills), daily roulettes and role bonus, challenge log, Doman Enclave; « done » boxes per character, all
  your characters at a glance, your own tasks (daily, weekly, every n hours, once, counters) with an optional on-screen panel, and a
  squadron mission planner (best parties of four from the game's data, estimate not yet checked in game).
- **Garden**: a journal of your beds and pots (plantings noted automatically from the game's chat lines, or by hand; hours filled from a seed list; harvest and tending reminders), and
  crossbreeding pairs with a planting plan for the seed you want.

### Free company

- **Vessels**: submarines and airships read in the company workshop (every 3 s while you are there) and kept: rank and experience,
  route, return time, last voyage finds, return alarm, voyage history (180 days, CSV).
- **Best routes** (submarines): every 1–5 sector route the rank and range allow, ranked by expected loot value (NPC or market price)
  per voyage or per day, or by experience per day; unlocked-sectors filter. Loot averages are community data (FFXIVGachaSpreadsheet,
  embedded). Airships: sector tree and possible loot (LuminaSupplemental), route planner by distance and duration.
- **Builds**: submarine and airship builds (read / planned / difference stats, cost vs capacity, part rank, repair), a check of read
  vs computed stats, route check (range, duration, breakpoint tiers, wear estimate) and best builds for a route.
- **Workshop**: the materials of each workshop project, part by part and phase by phase, against your stocks as read; what the
  workshop already received is counted once its delivery window was opened; several projects followed as one list, and an optional
  on-screen panel.

### Quests, collections and progression

- **Quests**: the game journal's quest kinds and genres with completion percentages; quests of the other starting cities and options
  not taken are hidden and left out of percentages (a box shows them); followed quests, rewards, item sources; quest menu with flag,
  Tensho map and zone weather; a quest's tooltip gives its expansion.
- **Events**: running festivals, their quests and event shops; a link opens partake.gg (player events) in your browser.
- **Collections**: mounts, minions, emotes, orchestrion (music playing now, favourites), titles (wear, random, per job), achievements
  (progress, tracked ones), Blue magic (teaching monsters sorted by level), hairstyles, fashion accessories, glasses, bardings,
  portraits, master books, outfits, wishlist and more, with the obtained state, a source filter and how to obtain each entry (vendors,
  exchanges, quests, achievements, crafting; Garland Tools and the wiki as fallbacks).
- **Gear** planner: worn gear, one plan per job, stats read / planned / difference and stat tiers, candidates near your level with
  filters, « All jobs (glamour) », materia sockets with caps and overmeld rates, owned ✓ / ✗ and « pieces to obtain » with their sources,
  « Try on », `TENSHO-GEAR1:` codes; the game's gear sets with better pieces owned, random job, wear and spiritbond alerts. A real stats
  (%) panel beside the game's Character window (setting).
- **Chocobo** (bardings), **Guildleves** (every leve, state, allowances), **Hunting log** (class and Grand Company logs, kills read).
- **Beastmaster**: the beasts with their attributes at a chosen rank, and the boards of the labyrinth with their enemies and bonuses.
- **Masked Carnivale**: every stage with its enemies, standard and ideal times, done or not as the game holds it; « only those
  left » box. No advice on the fights.
- **Relics**: the relic weapons of each family (Zodiac, Anima, Eureka, Resistance, Manderville, Phantom), a line per stage, the stage
  reached in your read stocks, and the later families' armour by job or role; the Zodiac book in progress (monsters, dungeons, FATEs
  with a link to the FATEs page, leves) read on click.
- **Allied societies**: each society's expansion, place (flag menu), rank and reputation as the game holds them, its currency in your
  stocks, and the allied society quests left today.
- **Grand Companies**: Grades tab (your grade, seals and next promotion in each company) and « What seals buy » (with a filter for
  what your grade can buy).
- **Duty loot** (also as a panel beside the Duty Finder, with unlocked / completed status and an optional « duty ready » alert),
  **Monster loot**, **NPC vendors** (free company credit shops included, unlock quests and achievements of each article).

### Field content and deep dungeons

- **Eureka and Bozja**, from the game's files: Zones tab (FATEs, skirmishes and critical engagements of each zone with level and
  place; click shows the map, right click the flag and nearest aetheryte), Field records and Lost actions with the game's texts.
  What your character holds there is not read.
- **Deep dungeons**, to get ready outside (Tensho pauses inside them and gives no fight advice): one tab per dungeon with its floors
  (unlocked / completed state from the game, each set opens its duty loot), what is found in the dungeon and its stones and special
  items, with their effects, and its coffers' possible contents from LuminaSupplemental (community source switch).

### Gold Saucer

- **Fashion Report**: weekly theme hints, owned pieces first (glamour dresser and armoire included), panel beside the game's window.
- **Triple Triad**: card notebook (ordinary and Ex. cards, values, sorts, rule-based score), decks, simulation, NPCs and cards (NPCs to
  face, packs to buy, missing cards), match advice (opt-in observation).
- **Mini Cactpot**: exact solver, advice drawn on the game's ticket.

### My tools

- **My tools**: pages you build from Tensho's blocks (lists to tick, reset daily or weekly or never; counters; timers to the game's
  resets; items to gather with held / missing and the cost of what is missing; notes and links), or install from someone else: a pasted `TENSHO1:` line or JSON, or the
  gallery of the plugin's repository, read on click. Each tool is checked and shown in full before it is installed; it holds no code
  and sends nothing to the game. Share as one line (your ticks are not in it), edit, remove.

### Settings, help, diagnostics and localization

- **Help**: a « ? » on every page, a first-visit note, a search across all the help, a first-steps guide you can leave at any step, community videos and guides read on a click, and a form that opens the project's GitHub ticket filled in, in your browser.
- Settings tabs: General (opening, header clock, alarm bar), Interface (language, theme, fonts, item hover and menu options), Modules,
  Map, Data (in-game panels, automatic storage save, cache durations including « Never (on request only) », « Empty the cache… » with a
  zip copy first in the Backups folder), Advanced (« Restore all defaults », developer mode, each community source on / off, PaissaDB
  included, and « Game data and what you observed only » that turns them all off at once, hover prices, game command delay), Debug
  (developer mode). Each setting has a
  tooltip and a ↺ restore-default button. Game data is shown in the game client's language, Tensho's own texts in the plugin language.
- **Folklore** tomes, read or not, are a tab of Collections.
- **Credits**: everything Tensho uses (game, libraries, embedded and online data) with its licence and the setting tied to it.
- Diagnostics (developer mode): draw time of each page (median and maximum of the last 120 frames) and a contributor panel (documents,
  source file of the shown page, data sources, probes, translation completeness).

## Repository structure

```text
.
├── src/TenshoAssistant/      plugin code (one folder per domain; *.Dalamud.cs = game access; UI/Components = shared templates)
├── tests/                    managed tests (pure logic)
├── tools/                    read-only data probes (game sheets, APIs) and data scripts
├── scripts/                  Verify.ps1 (build + tests + localization + package), Release.ps1 (git steps),
│                             New-Tool.ps1 (skeleton of a new page), Update-ToolGallery.ps1 (gallery index)
├── commit-release.bat        Verify -Commit then -Release (maintainer)
├── repo.json                 Dalamud custom repository entry (refreshed by each release)
├── .github/                  CodeQL and build workflows, Dependabot settings, issue and pull request templates
├── publish/d17/              draft for the official Dalamud repository, not submitted: custom repository only (DEC-039,
│                             docs/project/PUBLISHING.md)
├── publish/github/           copies of the files that live in .github/
├── community-help/           help videos and guides listed in the plugin
├── community-templates/      community tool gallery (data only)
├── community-discoveries/    shared discoveries: the common file and each player's merged file (tools/discoveries-merge.py)
├── docs/                     documentation map: docs/README.md
│   ├── project/              scope, roadmap, architecture, testing, references, decisions, publishing
│   └── evidence/             probe outputs cited by the code
├── Localization/README.md    translation guide
├── README.md · CONTRIBUTING.md · SECURITY.md · CHANGELOG.md · LICENSE
└── global.json · TenshoAssistant.sln
```

## Build and verification

Requirements: Windows, .NET SDK 10.0 (`global.json` accepts 10.0.100 and newer), Dalamud.NET.Sdk 15 (restored from NuGet during the build; it compiles against your Dalamud install, or `DALAMUD_HOME`). Without the game: `.\scripts\Verify.ps1 -TestsOnly` (tests and languages only).

```powershell
.\scripts\Verify.ps1                                 # Release build, managed tests, language files, package
.\scripts\Verify.ps1 -DotNet 'C:\path\to\dotnet.exe' # another SDK location
.\scripts\Verify.ps1 -Force                          # run every check again even if this exact state already passed
```

The run must end green with no warnings; it prints `VERSION x.y.z (DLL x.y.z.0)`. Outputs:

- DLL: `src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`
- Package: `src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip`

`-Commit`, `-Release` and `-DryRun` (maintainer only) are described in [CONTRIBUTING.md](CONTRIBUTING.md#versions-and-releases-maintainer).

## Installation in-game

Released versions: add `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json` to Dalamud › Settings ›
Experimental › Custom Plugin Repositories, then install « Tensho Assistant » from the plugin installer. Development builds:

1. Add the absolute DLL path to the Dalamud dev plugins list.
2. Load the plugin.
3. Open the window with:

```text
/tensho
```

4. Validate preferences, reload behavior, and unloading behavior.

## Localization

Rules: [Localization/README.md](Localization/README.md). In short:

- copy `src/TenshoAssistant/Localization/Languages/en.json`, rename it with the language code, translate the values;
- keep the translation keys unchanged; English is the reference and the fallback for missing keys;
- English and French are complete; German and Japanese only have a few keys;
- since 0.248 the languages are embedded in the DLL; a `Localization/Languages` folder beside the installed DLL overrides them for testing.

## Documentation to consult

- [docs/README.md](docs/README.md) — map of all documents
- [CONTRIBUTING.md](CONTRIBUTING.md) — build, tests, project rules, Dalamud publishing rules
- [CHANGELOG.md](CHANGELOG.md) — what changed in each version
- [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md) — the plan, what is done and what is validated in game
- [docs/project/ROADMAP.md](docs/project/ROADMAP.md) · [ARCHITECTURE.md](docs/project/ARCHITECTURE.md) · [TESTING.md](docs/project/TESTING.md) · [REFERENCES.md](docs/project/REFERENCES.md)
- [Localization/README.md](Localization/README.md) — localization workflow
- [SECURITY.md](SECURITY.md) — reporting a vulnerability

## AI usage disclosure

This plugin is developed with an AI coding agent (Claude, « Auto » level) under the direction of its author, who defines the features and
tests every version in game. Data links are proven with read-only probes ([docs/evidence/](docs/evidence/)). See
[CONTRIBUTING.md](CONTRIBUTING.md#publication-dalamud) for the Dalamud AI policy. The plugin icon (`src/TenshoAssistant/images/icon.png`)
was also generated by the AI agent.

## Credits

The complete list, with every licence and what each source is used for, is the plugin's **Credits** page (Preferences › Credits,
`src/TenshoAssistant/Credits/CreditCatalog.cs`, kept in step with the code by tests); this section summarizes it.

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd. FINAL FANTASY XIV © SQUARE ENIX CO., LTD. All rights reserved.
Tensho Assistant is not affiliated with nor endorsed by SQUARE ENIX.

Game data comes first: every list is read from the game's own sheets and memory when they have it. Community data is used only when the
game does not have it, always cited.

Framework and tools:

- Final Fantasy XIV and its game data (sheets read through Dalamud / Lumina), Dalamud and FFXIVClientStructs
- Dalamud.NET SDK, .NET SDK and runtime, PowerShell scripts for verification and release
- [Raphael.Net](https://github.com/WorkingRobot/Raphael.Net) by Asriel (MIT, NuGet, native library) and raphael-rs by KonaeAkira (Apache-2.0): the crafting solver

Community data used by the plugin (licence compatible with GPL-3.0, or service API):

- [LuminaSupplemental](https://github.com/Critical-Impact/LuminaSupplemental) by Critical-Impact (GPL-3.0): duty loot and item sources, downloaded at runtime; airship unlock tree and possible loot, embedded with its notice (0.336)
- [GatherBuddy](https://github.com/Ottermandias/GatherBuddy) by Ottermandias (Apache-2.0): fishing bait, mooch, bite and window data, downloaded at runtime as data
- [Teamcraft](https://github.com/ffxiv-teamcraft/ffxiv-teamcraft) (MIT): fishing bite times from its public reports
- [FFXIVGachaSpreadsheet](https://github.com/Infiziert90/FFXIVGachaSpreadsheet) by Infi (MIT, crowd-sourced): Fashion Report data (downloaded at runtime) and submarine loot averages (embedded snapshot, `tools/submarine-loot.py`)
- [SubmarineTracker](https://github.com/Infiziert90/SubmarineTracker) by Infi (MIT): submarine sector breakpoints embedded (notice in
  `src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv`), originally from the community « Submersible Loot Spreadsheet »
  (Elena / saerielle); voyage formulas understood by reading its code (original code); wear estimate from the community « Submersibles
  durability damage » sheet that it quotes
- [FFXIV Gardening Tracker](https://github.com/Lotlab/FFXIV-Gardening-Tracker) by Lotlab (GPL-3.0): garden seed times, embedded with its notice; FFXIV Crossbreed Helper by nick75g (MIT): garden crosses, embedded
- level modifier table (main stat, levels 1–100) from Character Panel Refined by Kouzukii (MIT, full notice in `Character/StatFormulas.cs`)
- [Universalis](https://universalis.app/) (market prices and listings), [Garland Tools](https://www.garlandtools.org/) (item, duty and
  monster sources), the [FFXIV Console Games Wiki](https://ffxiv.consolegameswiki.com/) (monster drops, sightseeing log, acquisition;
  island animals and workshop rules),
  [PaissaDB](https://github.com/zhudotexe/FFXIV_PaissaDB) by zhudotexe (open housing plots, read on click or by the optional watch)
- this repository on GitHub (CC0-1.0): the shared discoveries (on « Exchange now » or the optional automatic exchange) and the community tools gallery (read
  on click); [Partake](https://www.partake.gg/) (player events, opened in your browser); Discord (the optional alarm webhook)

Read to understand, never copied (facts only, original code):

- Mappy and SortaKinda (AGPL-3.0-or-later), and the other reference plugins listed in [docs/project/ROADMAP.md](docs/project/ROADMAP.md)
- Craftimizer (writing macros into the game, functional reference)
- HimbeertoniRaidTool (MIT) and Glamourer (Apache-2.0) for the gear planner; xivgear.app (no published licence: read only)
- Argus (licence not confirmed) for vessel builds; its airship breakpoint data comes from unlicensed sources and is not used
- frozen_rabbit_tome (no licence): the Bountiful Yield II rule, taken as a fact with original code and shown as an estimate (a rule, no text copied)

This project is an original plugin implementation, but it relies on these ecosystem components and references for its operation, testing, and validation.

## Notes

- The project is a full-feature FFXIV utility plugin and not only an item tracker.
- Unknown is not zero: when a value is not known (storage not opened yet, no community data), the plugin says so instead of guessing.
- Exact module coverage should always be verified against the project documentation rather than inferred from older references.

## Summary

TenshoAssistant is a multi-tool Dalamud plugin intended to centralize useful gameplay support surfaces into a single interface. The current codebase demonstrates a modular architecture with shared catalogs, domain services, and a rich main window that spans inventory, gathering, crafting, quest support, collections, market information, and more. The project documentation remains the definitive reference for maturity, scope, and current status of each feature area.

Traduction française plus bas.

---

# TenshoAssistant

Version française / French version

TenshoAssistant est un plugin Dalamud pour Final Fantasy XIV qui réunit dans une seule fenêtre, ouverte par `/tensho`, des outils du
quotidien : objets et rangements, tri de l'inventaire, prix du marché, artisanat, récolte et pêche, une carte qui remplace celle du jeu,
météo, alarmes et rappels, expéditions de la compagnie libre, quêtes, collections, statistiques du personnage, planificateur
d'équipement, aides du Gold Saucer, enchaînement de macros, la Félicité insulaire, des contenus annexes (Eurêka et Bozja, donjons
profonds, reliques, La Grande Mascarade), une base locale de ce que vous découvrez en jouant, un lexique français–anglais des mots du
jeu, vos propres outils, et des diagnostics.

La version courante est le `<Version>` de `src/TenshoAssistant/TenshoAssistant.csproj`. Les changements de chaque version sont dans
[CHANGELOG.md](CHANGELOG.md) ; ce qui est validé en jeu, et ce qui ne l'est pas encore, dans [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md).
Carte des documents : [docs/README.md](docs/README.md).

## Vue d’ensemble

La barre latérale range les pages en catégories repliables (la liste est `MainWindow.Pages.cs`) :

| Catégorie | Pages |
|---|---|
| — | **Accueil & outils** : un bloc « Aujourd'hui » (rappels à faire, minuteurs terminés, prochain vaisseau de retour, quêtes suivies, prochaines alarmes), dix tuiles avec une ligne en direct (artisanat, récolte, collections, quêtes, carte, marché, alarmes, enchaînement de macros, équipement, Gold Saucer), le travail en cours et les modules en erreur, l'objet en contexte, l'annuaire des outils ; **Mes outils** |
| Objets | **Objets** (recherche et fiche objet), **Rangements**, **Tri de l'inventaire**, **Marché**, **Lexique** |
| Production | **Artisanat**, **Récolte**, **Pêche en mer**, **Félicité insulaire**, **Commandes spéciales**, **Enchaînement de macros** |
| Monde | **Carte**, **Météo**, **ALEA**, **Alarmes**, **Rappels**, **Jardinage**, **Compagnie libre**, **PNJ vendeurs**, **Butin des contenus**, **Butin des monstres**, **Découvertes** |
| Progression | **Quêtes**, **Événements**, **Collections** (avec le grimoire Mage bleu), **La Grande Mascarade**, **Reliques**, **Peuples alliés**, **Grandes compagnies**, **Livraisons spéciales**, **Eureka et Bozja**, **Donjons profonds**, **Équipement**, **Chocobo**, **Mandats**, **Bestiaire**, **Dresseur** |
| Gold Saucer | **Revue de mode**, **Triple Triade**, **Mini Cactpot** |
| Préférences | **Aide**, **Paramètres**, **Crédits**, **Diagnostics** (mode développeur seulement) |

Commandes : `/tensho` ouvre la fenêtre ; `/tensho peche` (ou `pêche`, `fish`, `fishing`) ouvre ou ferme l'aide à la pêche ;
`/tensho todo <tâche>` ajoute une tâche libre aux Rappels ; `/tensho objet <objet>`, `prix`, `recolte` et `recette` (ou `item`,
`price`, `gather`, `recipe`) ouvrent l'objet dans Tensho (fiche objet, Marché, Récolte, Artisanat) ; `/tensho wiki <objet>` et `/tensho lodestone <objet>` ouvrent la page de
l'objet dans le navigateur ; `/tenshonext` est le marqueur de fin de page de l'enchaînement de macros. Les boutons « ouvrir » et
« réglages » de Dalamud ouvrent la fenêtre et sa page Paramètres.

## Architecture actuelle

- `src/TenshoAssistant/Plugin.cs` enregistre les services, la commande, les fenêtres et les panneaux ; chaque enregistrement a son nettoyage.
- `MainWindow.cs` (découpée en fichiers partiels `MainWindow.<Page>.cs`) porte la navigation et les pages ; `MapViewWindow.cs` est la
  carte Tensho, `GuideWindow.cs` le guide de première prise en main, `PanelWindow.cs` les panneaux libres (suivi, tâches, barres d'usure et de PR), `AttachedPanels.cs` les panneaux à côté des fenêtres du jeu. Toutes sont des fenêtres du `WindowSystem` de Dalamud.
- Un dossier par domaine (liste dans la partie anglaise) ; les fichiers `*.Dalamud.cs` touchent le jeu (lecture sur le thread du
  framework, puis copie), les autres sont du C# pur et testé. `Competitive/` tient les lieux où tout le plugin s'arrête (zones JcJ,
  donjons profonds et autres contenus classés).
- `UI/Components/` : modèles communs (`ItemCell`, `DetailPage`, `SettingRow` avec infobulle et bouton ↺, `EntityMenus`, `DataIndexes`).
- `Community/` : clients HTTP bornés (taille et durée) ; `Caching/` : cache disque, une durée par domaine de données.
- Les textes de l'interface sont des fichiers JSON embarqués dans la DLL.

## Domaines fonctionnels présents

Ce que fait le code aujourd'hui. La validation en jeu de chaque partie est suivie dans [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md).

### Objets, rangements et marché

- **Fiche objet** : recherche (filtres de catégorie, niveau, emplacement, échangeable, en vente ; objets clés par nom), puis en-tête
  (stock, dernier prix) et onglets Aperçu, Stocks, Craft (vue courte ; la préparation complète est dans Artisanat), Usages, Récolte,
  Marché, Notes (note et étiquettes « important » / « à vendre au PNJ », rappel à l'ouverture d'une boutique) ; liens wiki et Lodestone.
- **Chaque icône d'objet**, dans tous les modules : infobulle commune et menu d'outils au clic droit, avec les sources groupées par type
  (hauts faits, contenus, autres objets, échanges, PNJ vendeurs, quêtes), « Essayer » pour l'équipement, « Ajouter à ma liste de
  souhaits », marque obtenu ✓ / non obtenu ✗ sur les objets de collection. Les sources en sceaux ne gardent que la Grande Compagnie du
  personnage.
- **Dans le jeu**, chacun sur option : entrée Tensho dans le menu du clic droit de l'inventaire, panneau sous l'infobulle des objets
  (usages, comment l'obtenir, prix de revente au PNJ, comparaison avec la pièce portée, ce qu'il apprend et si vous l'avez déjà
  appris, la tâche de servant qui le rapporte, son extension quand toutes ses sources sûres concordent), prix au survol (dernière vente, moyenne, ventes
  par jour, moins cher de la région, valeur de la pile ; Ctrl+Alt actualise), bouton « Trier » dans la fenêtre d'inventaire du jeu.
- **Rangements** : Banque (servants, sacoches, sacoche premium, arsenal, coffre à apparence, armoire, coffre de compagnie : chacun connu
  après son ouverture en jeu), Personnages (un fichier par personnage), Cristaux, Monnaies (plafonds, mémoquartz de la semaine, alertes),
  « Que faire avec ? » (ce qu'une monnaie achète, son prix dans cette monnaie, ce qu'il s'est vendu sur votre monde, les gils que
  rapporte chaque unité), Gils (journal de 90 jours, changements classés), Rapport (piles à regrouper, pièces d'arsenal sans ensemble,
  objets à ranger dans l'armoire, apparences identiques), Imports.
- **Ventes des servants** : à côté de la liste de vente d'un servant, vos objets et vos prix tels que le jeu les montre ; sur clic, une
  comparaison avec les autres vendeurs par Universalis et un prix à copier ; les offres vues au tableau des ventes sur votre monde
  servent sans clic quand elles sont plus récentes. Rien n'est changé dans le jeu.
- **Tri de l'inventaire** : profils, modèles, règles et zones protégées avec aperçu (jusqu'à 256 catégories par zone, un groupe entier
  de catégories du jeu ajouté en un clic) ; le tri n'est appliqué que sur clic.
- **Marché** (Universalis) : Résumé, Annonces (monde / centre de données / région, filtres, lot le moins cher pour N unités, taxe
  comprise) et Historique des ventes ; cache disque, dernier état connu affiché et daté quand Universalis ne répond pas. Sous-cotation
  facultative : l'offre la plus basse moins 1 copiée dans le presse-papiers (désactivée par défaut). Les prix que vous voyez au tableau
  des ventes du jeu sont gardés par monde (réglage des Découvertes) et remplacent ceux d'Universalis quand ils sont plus récents.
- **Lexique** : les mots du jeu en français et en anglais, d'après ses propres feuilles (objets, actions, traits, états, lieux,
  monstres, PNJ, quêtes, missions, montures, mascottes, ALEA, classes et jobs, hauts faits, emotes, interface) ; recherche sans
  majuscules ni accents, filtre par type, un clic copie le mot.

### Artisanat et macros

- Catalogue de recettes, simulateur (actions d'artisanat du jeu, spécialiste, repas et remède, recettes expertes en condition normale,
  réglage des actions interdites) et recherche de rotation (seuils des collectionnables comme cibles de qualité), artisanat en direct,
  préréglages, enregistreur de rotation, import d'une macro collée.
- Onglet **Préparation** : chaîne de craft, quantités face aux stocks, coût de préparation et bénéfice, prérequis de la recette face au
  personnage (« Équiper (métier) »), recettes jamais fabriquées, lien Teamcraft, listes de fabrication de plusieurs objets, liste de
  récolte par zone.
- Export des macros et copie dans les macros du jeu sur clic (livre, premier / dernier emplacement, préfixe, écrasement ; `/nextmacro`
  quand Macro Chain est chargé) ; aide à côté du carnet d'artisanat du jeu ; carnet du jeu ouvert sur une recette sur clic.
- **Enchaînement de macros** : pages terminées par `/tenshonext`, lancement explicite, arrêt possible.
- **Panneaux des listes de suivi** (`MainWindow.ListPanels.cs`), chacun activé depuis sa page : la préparation d'artisanat, une liste
  de fabrication et un projet de l'atelier, à l'écran, stock sur besoin. Options communes aux panneaux : seulement ce qui reste à
  récupérer, par provenance (récolte, au choix rassemblée par zone de récolte avec le coût de téléportation de la zone ; PNJ vendeurs,
  pour les monnaies cochées ; monstres ; autres ; cristaux), objets finis seulement, étapes de fabrication restantes, points à horaire
  ouverts ou fermés avec un minuteur et « Ajouter les alarmes » pour leurs ouvertures, « Pas sur vous » pour les objets gardés dans un
  autre rangement, une patte pour les monstres proches qui lâchent un objet demandé (aussi sur la carte), un cadre sur les cases d'un
  inventaire de servant, de la sacoche du chocobo ou du coffre de compagnie ouverts qui contiennent un objet demandé, masquage en combat
  ou derrière les fenêtres du jeu (désactivés par défaut).

### Récolte et pêche

- Page **Récolte** : ressources et zones, points à horaire avec créneaux en heure d'Éorzéa, planification des PR (Récolte abondante II
  estimée par une règle communautaire, Raison solide, Vision claire et leurs versions de botaniste), tableau des collectionnables (seuils,
  assignats, ceux de vos sacs), les trois carnets (✓, masquer ce qui est fait, export CSV), « Y aller » (drapeau, éthérite la plus
  proche, téléportation sur confirmation) et « Équiper (métier) » sur chaque lieu, carnet de récolte du jeu ouvert sur un objet.
- Pêche : appâts, pêche au vif, créneaux et météo de GatherBuddy (Garland Tools en secours), temps de touche des relevés Teamcraft,
  prochaines vraies fenêtres d'un poisson avec alarmes uniques ; les alarmes de pêche portent les conditions météo et un lien de carte.
- Page **Pêche en mer** : prochains départs et trajets avec leurs escales, alarme avant un départ ; l'horaire communautaire est
  vérifié avec les voyages où vous embarquez (« vérifié en jeu » ou « pas encore vérifié » pour chaque port).
- Panneaux d'aide en jeu : à côté de la fenêtre de récolte, de la fenêtre Collectionnable et de la fenêtre de harpon ; pendant la pêche
  (poissons du lieu, appât en main ; `/tensho peche` pour l'ouvrir à la main) ; pendant la pêche en mer (escale, courant spectral,
  poissons avec appât, touche et temps de touche, missions avec les poissons qui y comptent, points vus par poisson, marques de
  l'Intuition du pêcheur, prises vues juste avant un courant spectral). Barre du tic de PR et alerte au seuil de PR en option.

### Félicité insulaire et livraisons

- **Félicité insulaire** : onglet Atelier, lu sur clic à partir de ce que le jeu a chargé une fois le carnet de fabrication ouvert sur
  votre île (rang, jour du cycle, enthousiasme, fabriques), un tableau des créations (heures, thèmes, popularité, offre et demande,
  valeur estimée, matériaux), une planification proposée pour un cycle, à copier et à saisir vous-même, avec les matériaux qu'elle
  utilise ; onglet Pâturage : vos animaux tels que le jeu les détient (humeur, nourriture, ressources, soins), et la liste des animaux
  de l'île (lieu, heure d'Éorzéa, météo, taille) lue sur clic sur le wiki FFXIV Console Games, avec un drapeau sur la carte de l'île.
  Les valeurs sont des estimations ; rien n'est envoyé au jeu.
- **Commandes spéciales** : vos clients d'après ce que le jeu détient (lieu, niveau, rang, points, cette semaine), et les récompenses
  de chaque client d'après les feuilles du jeu (trois montants selon la collectabilité, bonus de la semaine, objets liés à chaque rang).
  Les objets demandés cette semaine ne sont pas affichés.
- **Livraisons spéciales** : les quêtes de livraison des artisans et récolteurs, un onglet par série avec faites / total, d'après le
  journal de quêtes ; chaque quête ouvre sa fiche.

### Carte, météo, alarmes et rappels

- **Carte Tensho**, dans sa propre fenêtre, remplace la carte du jeu (touche M, réglage) : icônes du jeu (taille et priorité par carte),
  quêtes et mandats, ALEA, groupe, points de récolte et de pêche avec horaires, courants éthérés et repères d'exploration (masqués une
  fois obtenus), PNJ vendeurs, PNJ de Triple Triade, marques de chasse proches colorées par rang, emplacement de la carte au trésor
  déchiffrée, drapeau (poser et retirer depuis tous les menus), téléportation au coût réel, favoris (étoiles, note, groupe, code de
  partage `TENSHO-MAP1`, lignes « Lieu ( x , y ) » collées), listes locales par onglet (dont le carnet d'exploration), menu propre à
  chaque icône au clic droit. Aussi : points de récolte par métier (Mineur, Botaniste, Pêcheur), quêtes pas encore disponibles (une
  case), monstres proches pour vos listes, le bestiaire, les ALÉA, le mage bleu, les objets de collection manquants et le dresseur, les
  cibles de votre mandat, les animaux de la Félicité insulaire.
- Page **Carte** : liens de carte vus dans le chat (drapeau, éthérite la plus proche), train de chasse (marques A/S rencontrées, drapeau
  suivant, liste et annonce du conducteur copiées avec `<flag>`, alerte des marques A/S en option), conditions d'apparition des rangs S
  (savoir de la communauté, avec l'endroit où leur ALÉA a été vue en jeu), annuaire des lieux (adresses
  résidentielles), terrains en vente de votre monde par PaissaDB (sur clic, surveillance en option, désactivée par défaut).
- **Options de la carte** (Paramètres › Carte, chacune avec infobulle et ↺) : menus et outils par-dessus la carte (désactivé par
  défaut) avec le nom de la zone toujours affiché ; repères au bord de la carte pour les cibles hors de la vue, chaque type réglable ;
  position et cône de vision ; couleurs des zones ; masquage en combat ou en mission ; tailles et priorités des icônes.
- **Météo** : prévisions par zone ; une zone dont le jeu impose la météo jusqu'à la fin de son histoire n'est prévue qu'une fois la
  météo du jeu vue identique.
- **ALEA** : tous les ALEA que les fichiers du jeu placent dans les zones ouvertes, en cours ou non, avec leur niveau et leur lieu ;
  commence sur votre zone, recherche dans toutes les zones ; clic : voir sur la carte, clic droit : drapeau ou éthérite la plus proche.
  Les ALEA de la page des reliques s'ouvrent ici.
- **Découvertes** : une base gardée sur votre disque (dossier « Discoveries » ; les découvertes partagées à part dans
  `Discoveries/Shared`, un fichier compressé par type) ; la prise de notes et ses principaux types sont des réglages, activés par
  défaut. Noté en jouant : où vivent les monstres, où apparaissent les monstres de chasse, les ALÉA vues, d'où viennent les objets
  obtenus (désynthèse, réduction éthérée, tâches de servants, coffres de mission, récompenses d'ALÉA, ce que lâche votre propre cible,
  objets ouverts), les objets demandés lors des remises de quête, les poissons pris (appât ou pêche au vif, leurre, lieu, météo, heure
  d'Éorzéa, force et temps de touche, Intuition du pêcheur), les ombres de la pêche au harpon, les points de la pêche en mer et les
  prises juste avant un courant spectral, le butin des voyages, les secteurs visités et débloqués, les jugements de la Revue de mode,
  les fêtes et les panoramas, les sorts de mage bleu appris ou vus lancés et les bêtes apprivoisées, le lieu des marchands, le nombre
  de monstres vaincus et de coffres de mission ouverts, et les prix vus au tableau des ventes sur chaque monde visité. Chaque entrée dit
  si vous l'avez vue ou importée ; export d'une copie datée, import des fichiers d'autres joueurs, vidage de la base (une copie zip est
  faite avant dans le dossier Backups). Aucun autre joueur n'est jamais noté. Rien ne quitte votre PC sauf si vous connectez GitHub (un
  code à approuver sur github.com, après un avertissement : le partage est public) : « Échanger maintenant », ou le réglage « Échange
  automatique » (désactivé par défaut), récupère les découvertes communes du dépôt et envoie seulement ce que vous avez vu vous-même
  dans un gist secret à votre nom, avec un ticket par joueur ; le mainteneur les vérifie et les fusionne. Les lieux vus s'affichent dans
  le Bestiaire, le Butin des monstres et d'autres pages, les sources d'objets dans le panneau de survol des objets.
- **Alarmes** : alarmes Tensho (une fois à une date, chaque jour d'Éorzéa à une heure ET, ou à une date locale réelle ; conditions météo
  pour les poissons), ligne dans le chat avec liens et son des alarmes du jeu, copie facultative dans la liste d'alarmes du jeu par
  `/alarm` (désactivée par défaut), alarmes automatiques des tâches de servant et des vaisseaux, horloges d'autres fuseaux, fenêtre de
  suivi épinglée en option. Icône d'alarme dans la barre d'informations du serveur (Paramètres › Général). Webhook Discord en option
  (désactivé par défaut) : les alarmes qui sonnent sont envoyées dans un salon de votre serveur, avec un message par catégorie ou par
  groupe d'alarmes, la mention d'un rôle au choix et un bouton de test ; les alertes de plafond des monnaies et « mission prête » aussi.
- **Rappels** : minuteurs du calendrier du jeu (missions de ravitaillement, commandes spéciales, Revue de mode, quêtes des peuples
  alliés, autorisations de mandats, cartes au trésor, tâches de servant, expéditions de la compagnie, escouade de Grande Compagnie) avec
  remises à zéro et alarmes ; Livre d'or (tâches, chances de lignes), contrats de chasse (cibles et victimes), missions aléatoires du
  jour et bonus de rôle, carnet d'objectifs, Enclave de Doman ; cases « fait » par personnage, vue de tous vos personnages, vos propres
  tâches (chaque jour, chaque semaine, toutes les n heures, une fois, compteurs) avec un panneau à l'écran en option, et un planificateur
  des missions d'escouade (meilleurs groupes de quatre d'après les données du jeu, estimation pas encore vérifiée en jeu).
- **Jardinage** : un journal de vos parcelles et pots (plantations notées d'office depuis les lignes du chat du jeu, ou à la main ; durées remplies depuis une liste de graines ;
  rappels de récolte et d'entretien), et les paires de croisement avec un plan de plantation pour la graine voulue.

### Compagnie libre

- **Vaisseaux** : sous-marins et dirigeables lus dans l'atelier de la compagnie (toutes les 3 s sur place) et gardés : rang et
  expérience, trajet, retour, butin du dernier voyage, alarme au retour, historique des voyages (180 jours, CSV).
- **Meilleurs trajets** (sous-marins) : tous les trajets de 1 à 5 secteurs permis par le rang et l'autonomie, classés par valeur du
  butin (prix PNJ ou du marché) par voyage ou par jour, ou par expérience par jour ; filtre des secteurs débloqués. Butin moyen : données
  communautaires (FFXIVGachaSpreadsheet, embarquées). Dirigeables : arbre des secteurs et butin possible (LuminaSupplemental),
  planificateur par distance et durée.
- **Montages** : sous-marins et dirigeables (statistiques lues / prévues / écart, coût et capacité, rang des pièces, réparation),
  contrôle lu / calculé, vérification d'un trajet (autonomie, durée, paliers, usure estimée) et meilleurs montages pour un trajet.
- **Atelier** : les matériaux de chaque projet de l'atelier, pièce par pièce et phase par phase, face à vos stocks relevés ; ce que
  l'atelier a déjà reçu est compté une fois sa fenêtre de livraison ouverte ; plusieurs projets suivis comme une seule liste, et un
  panneau à l'écran en option.

### Quêtes, collections et progression

- **Quêtes** : types et genres du journal du jeu avec pourcentages ; quêtes des autres cités de départ et options non prises masquées
  et hors pourcentages (une case les réaffiche) ; quêtes suivies, récompenses, sources d'objets ; menu de quête (drapeau, carte, météo) ; l'infobulle d'une quête donne son extension.
- **Événements** : fêtes en cours, leurs quêtes et leurs boutiques ; un lien ouvre partake.gg (événements de joueurs) dans votre
  navigateur.
- **Collections** : montures, mascottes, emotes, orchestrion (musique en cours, favoris), titres (porter, au hasard, par job), hauts
  faits (progression, succès suivis), grimoire Mage bleu (monstres triés par niveau), coiffures, accessoires de mode, lunettes, bardes,
  portraits, livres de maître, tenues, liste de souhaits et d'autres, avec l'état obtenu, un filtre par source et comment obtenir chaque
  entrée (vendeurs, échanges, quêtes, hauts faits, artisanat ; Garland Tools et le wiki en dernier recours).
- **Équipement** : équipement porté, un plan par job, statistiques lues / prévues / écart et paliers, pièces proches du niveau avec
  filtres, « Tous les jobs (mirage) », matérias avec plafonds et taux de surmatérialisation, possession ✓ / ✗ et « Pièces à obtenir »
  avec leurs sources, « Essayer », codes `TENSHO-GEAR1:` ; ensembles du jeu avec les pièces possédées meilleures, job au hasard, alertes
  d'usure et de symbiose. Panneau des statistiques réelles (%) à côté de la fenêtre Personnage du jeu (réglage).
- **Chocobo** (bardes), **Mandats** (tous les mandats, état, autorisations), **Bestiaire** (carnets de classe et de Grande Compagnie,
  victimes lues).
- **Dresseur** : les bêtes avec leurs attributs au rang choisi, et les plateaux du labyrinthe avec leurs ennemis et leurs bonus.
- **La Grande Mascarade** : chaque niveau avec ses ennemis, ses temps standard et idéal, réussi ou non tel que le jeu le détient ;
  case « Non réussis seulement ». Aucun conseil sur les combats.
- **Reliques** : les armes reliques de chaque famille (Zodiaque, Anima, Eurêka, Résistance, Manderville, Fantômes), une ligne par
  étape, l'étape atteinte dans vos stocks relevés, et l'armure des familles suivantes par job ou par rôle ; le livre du zodiaque en
  cours (monstres, donjons, ALEA avec un lien vers la page ALEA, mandats) lu sur clic.
- **Peuples alliés** : l'extension, le lieu (menu du drapeau), le rang et la réputation de chaque peuple tels que le jeu les détient,
  sa monnaie dans vos stocks, et les quêtes de peuples alliés restantes aujourd'hui.
- **Grandes compagnies** : onglet Grades (votre grade, vos sceaux et la prochaine promotion dans chaque compagnie) et « Ce que les
  sceaux achètent » (avec un filtre sur ce que votre grade peut acheter).
- **Butin des contenus** (aussi en panneau à côté de l'outil de mission, avec l'état débloqué / terminé et une alerte « mission prête »
  en option), **Butin des monstres**, **PNJ vendeurs** (boutiques de compagnie libre comprises, quêtes et hauts faits qui débloquent
  chaque article).

### Contenus de terrain et donjons profonds

- **Eureka et Bozja**, d'après les fichiers du jeu : onglet Zones (ALEA, escarmouches et affrontements cruciaux de chaque zone avec
  niveau et lieu ; clic : voir sur la carte, clic droit : drapeau et éthérite la plus proche), Rapports du front bozjien et Actions
  oubliées avec les textes du jeu. Ce que votre personnage possède là-bas n'est pas lu.
- **Donjons profonds**, pour préparer dehors (Tensho se met en pause à l'intérieur et ne donne aucun conseil de combat) : un onglet par
  donjon avec ses étages (état débloqué / terminé donné par le jeu, chaque groupe ouvre son butin), ce qu'on y trouve, ses pierres et
  objets particuliers avec leurs effets, et le contenu possible de ses coffres d'après LuminaSupplemental (réglage des sources communautaires).

### Gold Saucer

- **Revue de mode** : indices du thème de la semaine, pièces possédées d'abord (coffre à apparence et armoire compris), panneau à côté
  de la fenêtre du jeu.
- **Triple Triade** : carnet (cartes ordinaires et Ex., valeurs, tris, score par règles), decks, simulation, PNJ et cartes (PNJ à
  affronter, lots à acheter, cartes manquantes), conseil en match (observation sur option).
- **Mini Cactpot** : solveur exact, conseil affiché sur le ticket du jeu.

### Mes outils

- **Mes outils** : des pages que vous composez avec les blocs de Tensho (listes à cocher, remises à zéro chaque jour, chaque semaine
  ou jamais ; compteurs ; minuteurs jusqu'aux remises du jeu ; objets à réunir avec possédé / manque et le coût de ce qui manque ;
  notes et liens), ou que vous installez depuis quelqu'un d'autre : une ligne `TENSHO1:` ou un JSON collés, ou la galerie du dépôt du
  plugin, lue sur clic. Chaque outil est vérifié et montré en entier avant son installation ; il ne contient aucun code et n'envoie
  rien au jeu. Partage en une ligne (vos cases n'y sont pas), modification, retrait.

### Paramètres, aide, diagnostics et localisation

- **Aide** : un « ? » sur chaque page, une note à la première visite, une recherche dans toute l'aide, un guide de premiers pas que
  vous pouvez quitter à chaque étape, des vidéos et guides de la communauté lus sur clic, et un formulaire qui ouvre le ticket GitHub du
  projet, rempli, dans votre navigateur.
- Onglets des paramètres : Général (ouverture, horloge de l'en-tête, barre d'alarme), Interface (langue, thème, polices, survol et menu
  des objets), Modules, Carte, Données (panneaux en jeu, enregistrement des rangements, durées de cache dont « Jamais (seulement sur
  demande) », « Vider le cache… » avec une copie zip faite avant dans le dossier Backups), Avancé (« Tout remettre par défaut », mode
  développeur, chaque source communautaire activée ou non, PaissaDB comprise, et « Données du jeu et observées seulement » qui les coupe
  toutes d'un coup, prix au survol, délai des commandes du jeu), Debug (mode développeur). Chaque réglage a une infobulle et un bouton ↺. Les données du jeu sont affichées dans la langue du
  client, les textes de Tensho dans la langue du plugin.
- Les tomes de **folklore**, lus ou non, sont un onglet des Collections.
- **Crédits** : tout ce qu'utilise Tensho (jeu, bibliothèques, données embarquées et en ligne) avec sa licence et le réglage lié.
- Diagnostics (mode développeur) : temps d'affichage de chaque page (médiane et maximum des 120 dernières images) et panneau
  contributeur (documents, fichier source de la page, sources de données, sondes, avancement des traductions).

## Structure du dépôt

Voir la structure anglaise ci-dessus et la carte des documents [docs/README.md](docs/README.md).

## Build et validation

Prérequis : Windows, SDK .NET 10.0 (`global.json` accepte 10.0.100 et plus), Dalamud.NET.Sdk 15 (restauré depuis NuGet pendant la compilation ; il compile avec votre Dalamud installé, ou `DALAMUD_HOME`). Sans le jeu : `.\scripts\Verify.ps1 -TestsOnly` (tests et langues seulement).

```powershell
.\scripts\Verify.ps1                                  # compilation Release, tests, fichiers de langue, paquet
.\scripts\Verify.ps1 -DotNet 'C:\chemin\dotnet.exe'   # autre emplacement du SDK
.\scripts\Verify.ps1 -Force                           # relancer tous les contrôles même si cet état exact est déjà passé
```

Le résultat doit être vert, sans avertissement ; il affiche `VERSION x.y.z (DLL x.y.z.0)`. Sorties :

- DLL : `src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`
- Paquet : `src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip`

`-Commit`, `-Release` et `-DryRun` (mainteneur) : voir [CONTRIBUTING.md](CONTRIBUTING.md#versions-and-releases-maintainer).

## Installation en jeu

Versions publiées : ajouter `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json` dans Dalamud › Paramètres ›
Expérimental › Dépôts de plugins personnalisés, puis installer « Tensho Assistant ». Versions de développement :

1. Ajouter le chemin de la DLL dans la liste des plugins de développement de Dalamud.
2. Charger le plugin.
3. Ouvrir la fenêtre avec :

```text
/tensho
```

4. Vérifier le comportement des préférences, du rechargement et du déchargement.

## Localisation

Règles : [Localization/README.md](Localization/README.md). En bref :

- copier `src/TenshoAssistant/Localization/Languages/en.json`, le renommer avec le code de la langue, traduire les valeurs ;
- conserver les clés ; l'anglais est la référence et le secours des clés manquantes ;
- anglais et français complets ; allemand et japonais : quelques clés seulement ;
- depuis 0.248 les langues sont embarquées dans la DLL ; un dossier `Localization/Languages` à côté de la DLL installée les remplace pour les essais.

## Documentation utile

- [docs/README.md](docs/README.md) : carte de tous les documents
- [CONTRIBUTING.md](CONTRIBUTING.md) : compiler, tester, règles du projet, publication Dalamud
- [CHANGELOG.md](CHANGELOG.md) : changements par version
- [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md) : le plan, ce qui est fait et validé en jeu
- [Localization/README.md](Localization/README.md) : contribution à la localisation
- [SECURITY.md](SECURITY.md) : signaler une vulnérabilité

## Usage de l'IA

Ce plugin est développé avec un agent IA (Claude, niveau « Auto ») sous la direction de son auteur, qui définit les fonctions et teste chaque
version en jeu. Les liens de données sont prouvés par des sondes en lecture seule ([docs/evidence/](docs/evidence/)). L'icône du plugin a aussi été générée par l'agent IA.

## Crédits

La liste complète, avec chaque licence et l'usage de chaque source, est la page **Crédits** du plugin (Préférences › Crédits,
`src/TenshoAssistant/Credits/CreditCatalog.cs`, tenue à jour avec le code par des tests) ; cette section la résume.

FINAL FANTASY est une marque déposée de Square Enix Holdings Co., Ltd. FINAL FANTASY XIV © SQUARE ENIX CO., LTD. Tous droits réservés.
Tensho Assistant n'est ni affilié à SQUARE ENIX ni approuvé par elle.

Les données du jeu d'abord : chaque liste est lue dans les feuilles et la mémoire du jeu quand il les a. Les données communautaires ne
servent que lorsque le jeu ne les a pas, et sont toujours citées.

- Final Fantasy XIV et ses données (feuilles lues par Dalamud / Lumina), Dalamud et FFXIVClientStructs ; SDK Dalamud.NET, SDK et
  runtime .NET, PowerShell.
- Données communautaires utilisées : LuminaSupplemental (Critical-Impact, GPL-3.0 : butin des contenus et sources d'objets, téléchargé ; arbre de déblocage et butin possible des dirigeables, embarqués avec leur avis) ;
  GatherBuddy (Ottermandias, Apache-2.0 : données de pêche, téléchargées) ; Teamcraft (MIT : temps de touche) ; FFXIVGachaSpreadsheet
  (Infi, MIT : Revue de mode téléchargée, butin moyen des sous-marins embarqué) ; SubmarineTracker (Infi, MIT : seuils des secteurs
  embarqués, avis dans `src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv`, à l'origine la feuille communautaire
  « Submersible Loot Spreadsheet » d'Elena / saerielle ; formules de voyage comprises en lisant son code ; usure d'après la feuille
  « Submersibles durability damage ») ; Raphael.Net (Asriel, MIT) et raphael-rs (KonaeAkira, Apache-2.0) : solveur d'artisanat ; FFXIV Gardening Tracker (Lotlab, GPL-3.0 : durées du jardinage, embarquées) et FFXIV Crossbreed Helper (nick75g, MIT : croisements, embarqués) ; table des modificateurs de niveau de Character Panel Refined (Kouzukii, MIT, avis complet dans `Character/StatFormulas.cs`) ;
  [Universalis](https://universalis.app/), [Garland Tools](https://www.garlandtools.org/), le [wiki FFXIV](https://ffxiv.consolegameswiki.com/),
  [PaissaDB](https://github.com/zhudotexe/FFXIV_PaissaDB) de zhudotexe (terrains en vente, lus sur clic ou par la surveillance facultative) ;
  ce dépôt sur GitHub (CC0-1.0 : découvertes partagées, sur « Échanger maintenant » ou par l'échange automatique facultatif, et galerie
  des outils de la communauté, lue sur clic) ; [Partake](https://www.partake.gg/) (événements de joueurs, ouvert dans votre
  navigateur) ; Discord (webhook facultatif des alarmes).
- Lus pour comprendre, jamais copiés : Mappy et SortaKinda (AGPL-3.0-or-later) et les autres références de `docs/project/ROADMAP.md` ;
  Craftimizer (écriture des macros) ; HimbeertoniRaidTool (MIT), Glamourer (Apache-2.0), xivgear.app (sans licence) pour l'équipement ;
  Argus (licence non confirmée) pour les montages. Règle reprise comme fait, avec du code original : frozen_rabbit_tome (sans licence) pour la
  Récolte abondante II, affichée comme estimation.

Liens et détails : section anglaise ci-dessus.

## Résumé

TenshoAssistant est un plugin Dalamud multi-outils orienté assistance au gameplay. Son architecture actuelle est cohérente avec un système de services partagés, des catalogues centralisés et une interface unifiée qui agrège plusieurs domaines utiles au joueur. La documentation du projet reste la source de vérité pour le périmètre exact, le niveau de maturité et les priorités de chaque fonctionnalité.
