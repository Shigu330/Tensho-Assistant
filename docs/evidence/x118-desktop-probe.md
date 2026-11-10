# X-118 — the plugin's own pages in a desktop window, without Dalamud (2026-11-10)

Plan 4 of the desktop application (decision D-134: the same ImGui pages outside the game, from the same code), step 0: a probe before any
plugin code. Tool: `tools/DesktopProbe` (README of `tools/`). Built plugin: TenshoAssistant 0.1029.0 (`src/TenshoAssistant/bin/x64/Release`).
Installed Dalamud: `addon/Hooks/dev`, revision 7665 (commit 30509c3b), Dalamud.Bindings.ImGui 1.0.0.0 over its own `cimgui.dll`
(Dear ImGui 1.88). Game on disk: `ffxivgame.ver` 2026.11.03.0000.0000 (7.58), read with Lumina; the game was not running and Dalamud was
not loaded. Nothing of Dalamud or of the game is copied into the repository: the probe loads them where they are installed.

## Answer: yes, a real page of the plugin shows in a desktop window

| Point | Result | Proof |
|---|---|---|
| Dalamud's ImGui bindings outside Dalamud | **yes** | `Dalamud.Bindings.ImGui.dll` and `cimgui.dll` load in a plain .NET 10 process (the native library resolved from `Hooks/dev`); context, font atlas (1024×1024), frames, draw lists, Dalamud's own error recovery (`ImGuiP.ErrorCheckEndFrameRecover`). No other binding needed: the pages are compiled against these. |
| Window engine | **yes, no package** | Win32 window + OpenGL 1.1 (`user32`, `gdi32`, `opengl32`, part of Windows), renderer written in the probe (textured triangles, a scissor per command, as Dear ImGui's OpenGL 2 example does; not copied). No download, no licence question for the engine. Mouse, wheel, characters and editing keys go to ImGui's input queue. |
| The plugin's real window | **yes** | `TenshoAssistant.MainWindow` built by its own constructor from the built DLL (34 parameters): the plugin's services created as the plugin creates them, Dalamud services as stand-ins (`IDataManager` over Lumina, `IFramework`, `IPluginLog`, `ITextureProvider`; any other interface answers defaults and records each member reached). Drawn as Dalamud's WindowSystem does: `PreDraw`, `Begin`, `Draw`, `End`, `PostDraw` (theme, navigation, header, footer). |
| Weather page | **yes** | Map and weather catalogues built by the plugin's own loaders on Lumina; Coerthas Central Highlands (territory 155): forecast table, 12 periods, weather icons, alarm bells, French texts. Capture: [x118-desktop-weather.png](x118-desktop-weather.png). |
| Lexicon page | **yes** | The plugin's `LexiconLoader.Read` on the stand-in `IDataManager`, as `Plugin.cs` does: 117,228 words; search « cristal »: 366 words, 8 pages. Capture: [x118-desktop-lexicon.png](x118-desktop-lexicon.png). |
| Input reaches the page code | **yes** | A scripted click on « Récolte » in the navigation (`--click=60,356`) moved the window from Weather to Gathering: 2,939 resources listed with their icons. |
| Game icons | **yes** | `ITextureProvider.TryGetFromGameIcon` answered from `ui/icon/XXX000/XXXXXX_hr1.tex` (normal size as fallback) read by Lumina (`TexFile.ImageData`, BGRA) and uploaded as an OpenGL texture whose name is the `ImTextureID`. Weather capture: 1,440 icon draws served, 0 missing; Gathering: 1,792 served, 0 missing. |
| Fonts | **yes, with one seam** | Dalamud's own free fonts from its installed assets (`dalamudAssets/dev/UIRes`): Noto Sans CJK JP Medium (text) with Font Awesome Free Solid merged (private use area), both under the SIL Open Font Licence. The plugin's icon glyphs (bells, arrows, check marks) draw. **But** `UiBuilder.IconFont` (and `IconFontFixedWidth`, `DefaultFont`, `MonoFont`) **blocks forever** outside Dalamud: `InterfaceManager.WhenFontsReady` waits for `Service<InterfaceManager>`, which never starts (measured: the first frame hung in `NavigationCategory`; IL of the getter read with `--il`). The probe points those calls at its own font in a temporary copy of the plugin (Mono.Cecil): **39 call sites**, all `UiBuilder.IconFont`. In the application this is one seam in the plugin's code, not a rewrite (see « What is missing »). The game's own fonts (`common/font/*.fdt`) were not tried. |
| All 51 pages | **drawn, 0 exception** | `--headless --loaders=all`: the 12 catalogue loaders that need only Lumina (maps, weather, duties, GP, craft actions, fashion hints, item uses, items, recipes, gathering, NPC vendors, map index) all published; each page drawn for 20 frames: **51 of 51 without exception**, 0 Dalamud member reached through the absent stand-ins while drawing (clicks not exercised). |

## Inventory of the 51 pages (measured)

Two measures, both in the probe: the IL call graph of each page's draw method (`--inventory`, from `MainWindow.PageEntries`, the single page
list), and the headless draw above. The call graph follows the plugin's own code and stops at the other pages' draw methods and at the
game-link classes, the 155 types declared in the plugin's `*.Dalamud.cs` files (project convention: game-bound code lives there) plus
`Plugin`. « Can reach » is an upper bound: a branch may never run.

- **No page touches native game state itself**: 0 FFXIVClientStructs type reached from any page's own code (all native reads are behind
  game-link classes).
- **As is (31 pages)**: no game-link class of their own; they draw from the plugin's services (catalogues from Lumina, saved files, service
  states). Their live parts (bags, notebooks, collections, character) stay empty, as « log in » lines, until the local link (step 2) fills
  the same service states.
- **Needs a stand-in over the link or the files (17 pages)**: the page code calls a game-link class itself; 29 such classes. Most common:
  `AlarmService` (10 pages: the alarm buttons), `CollectableReader` (4), `GameAlarm` (3), then `CharacterStatusWatcher`,
  `GameTimerSnapshot`, `SquadronTimer`, `AchievementProgressWatcher`, `TitleChanger`, `SelfCheckRunner` (2 each), 20 others in one page each.
- **Stays in the game (3 pages, user's choice)**: Maps, CraftLive, Fashion (the map, live crafting, the Fashion Report zones). The attached
  panels and the gathering helper are not pages.
- **Shared item tools (39 pages)**: the item cell's menu reaches `BankInventoryBridge`, `GameLogOpener`, `GameGearsetReader` and the map
  marker classes: actions done by the plugin in the game (step 3), off or answered by the link in the application.
- **Dalamud interface pieces**: fonts (`IFontHandle`, the plugin's title, section and body fonts: absent, the default font is used) 50
  pages; game icons (`ITextureProvider`) 35 pages, replaced (above); `UiBuilder.IconFont` 26 pages, 39 sites, needs the seam.
- **Filled by `Plugin.cs` after the window is built**: 177 properties and delegates of `MainWindow` (sheet loaders such as the lexicon,
  readers of the game such as bags, timers, gear). The probe set one (`LexiconLoad`); the others leave their part of a page in its
  « loading » or « unavailable » state. The application sets the sheet ones as `Plugin.cs` does; the game ones come with the link.

| Page | Class | Page-specific game-link classes reached | Shared item tools (map, game log, gearset, bank menu) | Dalamud services reached | Dalamud UI | Headless (12 catalogues loaded) |
|---|---|---|---|---|---|---|
| Home | needs the link | AlarmService, CharacterStatusWatcher, GameAlarm, GameTimerSnapshot, SquadronTimer | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (6226 vertices, 0 icons) |
| Items | needs the link | AlarmService, CollectableReader, RecipeLogReader, RecipeLogState | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (3400 vertices, 0 icons) |
| Storage | needs the link | CurrencyWatchState, CurrencyWatcher, GearsetItems, GearsetReader, GilJournalState | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (4972 vertices, 0 icons) |
| SortPreview | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (3520 vertices, 0 icons) |
| Market | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (3468 vertices, 0 icons) |
| CraftSimulation | needs the link | CollectableReader, CraftRecorder, MacroWriter | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (5000 vertices, 0 icons) |
| CraftLive | in game (user choice) | - | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (5256 vertices, 0 icons) |
| CraftPresets | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (3974 vertices, 0 icons) |
| Gathering | needs the link | AlarmService, CollectableReader, NotebookReader | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (6608 vertices, 320 icons) |
| GatheringGp | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (6968 vertices, 0 icons) |
| GatheringCollectables | needs the link | CollectableReader | no | - | fonts | drawn (3564 vertices, 0 icons) |
| OceanFishing | needs the link | AlarmService | no | - | IconFont, fonts | drawn (3188 vertices, 0 icons) |
| IslandSanctuary | needs the link | IslandAnimalSource, IslandAnimalState | yes | IGameGui | fonts | drawn (3296 vertices, 0 icons) |
| MacroChain | as is (game closed) | - | no | ITextureProvider | icons, fonts | drawn (6052 vertices, 0 icons) |
| Maps | in game (user choice) | - | yes | IGameGui | IconFont, fonts | drawn (5012 vertices, 0 icons) |
| Weather | needs the link | AlarmService | no | ITextureProvider | icons, IconFont, fonts | drawn (7678 vertices, 240 icons) |
| Fates | as is (game closed) | - | yes | IGameGui | fonts | drawn (2864 vertices, 0 icons) |
| NpcVendors | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (2940 vertices, 0 icons) |
| DutyLoot | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (5992 vertices, 340 icons) |
| MonsterLoot | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (3668 vertices, 0 icons) |
| Alarms | as is (game closed) | - | no | - | - | drawn (5008 vertices, 0 icons) |
| Reminders | needs the link | AlarmService, CharacterStatusWatcher, GameTimerSnapshot, GameTimers, RouletteDaily, SquadronTimer, TreasureMapSpot, WondrousTails | no | IGameGui | IconFont, fonts | drawn (9932 vertices, 0 icons) |
| Garden | as is (game closed) | - | no | - | IconFont, fonts | drawn (3440 vertices, 0 icons) |
| FreeCompany | needs the link | AirshipTableState, AlarmService | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (4772 vertices, 0 icons) |
| Quests | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (5534 vertices, 0 icons) |
| Events | needs the link | FestivalReader | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (3556 vertices, 0 icons) |
| Collections | needs the link | AchievementProgressWatcher, TitleChanger | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (7010 vertices, 0 icons) |
| BlueMagic | needs the link | AchievementProgressWatcher, TitleChanger | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (4744 vertices, 0 icons) |
| AlliedSocieties | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (2904 vertices, 0 icons) |
| GrandCompanies | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (3068 vertices, 0 icons) |
| SpecialDeliveries | as is (game closed) | - | yes | IGameGui | fonts | drawn (3012 vertices, 0 icons) |
| CustomDeliveries | as is (game closed) | - | no | - | fonts | drawn (3216 vertices, 0 icons) |
| Gear | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (3920 vertices, 0 icons) |
| Chocobo | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (3492 vertices, 0 icons) |
| Leves | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (4024 vertices, 0 icons) |
| HuntingLog | as is (game closed) | - | yes | IGameGui | IconFont, fonts | drawn (2872 vertices, 0 icons) |
| MaskedCarnivale | as is (game closed) | - | no | - | fonts | drawn (2960 vertices, 0 icons) |
| RelicWeapons | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (3016 vertices, 0 icons) |
| FieldContent | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (3044 vertices, 0 icons) |
| DeepDungeons | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (2980 vertices, 0 icons) |
| Beastmaster | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (2968 vertices, 0 icons) |
| Fashion | in game (user choice) | - | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (4660 vertices, 0 icons) |
| TripleTriad | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (6256 vertices, 0 icons) |
| MiniCactpot | as is (game closed) | - | no | - | fonts | drawn (4478 vertices, 0 icons) |
| Lexicon | as is (game closed) | - | no | - | fonts | drawn (2972 vertices, 0 icons) |
| Help | needs the link | AlarmService, GameAlarm, SelfCheckRunner | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (5394 vertices, 0 icons) |
| Settings | needs the link | AlarmService, MapEventMarker, MapFateMarker | yes | ITextureProvider | icons, IconFont, fonts | drawn (6244 vertices, 0 icons) |
| Credits | as is (game closed) | - | no | - | fonts | drawn (10072 vertices, 0 icons) |
| Diagnostics | needs the link | AlarmService, GameAlarm, SelfCheckRunner | yes | IGameGui, ITextureProvider | icons, IconFont, fonts | drawn (8108 vertices, 0 icons) |
| MyTools | as is (game closed) | - | yes | IGameGui, ITextureProvider | icons, fonts | drawn (3324 vertices, 0 icons) |
| Discoveries | as is (game closed) | - | yes | IGameGui | IconFont, fonts | drawn (5292 vertices, 0 icons) |

Headless run (`--headless --loaders=all`, 20 frames per page): 51 pages drawn, 0 threw; vertices per page between 2,840 and 10,072;
icons asked: Weather 240, Gathering 320, the other pages 0 without a selected item.

## What is missing (not blocking)

1. **Font seam in the plugin** (after the post-patch release, plugin code): the 39 `UiBuilder.IconFont` sites read one property of the
   plugin's UI components instead, set by `Plugin.cs` from `UiBuilder` and by the application from its atlas. Without it, the first
   navigation frame blocks outside Dalamud. A ratchet test can forbid new direct uses (R10).
2. **The other window properties**: the 177 set by `Plugin.cs` (sheet loaders now, game readers with the link).
3. **Saved settings**: the probe uses a new `Configuration`; the application reads the plugin's saved file
   (`pluginConfigs/TenshoAssistant.json`), read-only at first (step 1).
4. **Platform pieces of the probe's window**: clipboard (copy buttons do nothing), DPI scale (`ImGuiHelpers.GlobalScale` set to 1), a
   few keys, the cursor shapes, several windows (a second ImGui window, as Dalamud's floating windows, needs viewports or one OS window
   per Tensho window).
5. **Actions** (flag, teleport request, alarms, lists): not exercised; through the plugin, on a click only (step 3, R04).

## Licences

Window engine and renderer: the probe's own code, GPL v3 with the project. Dear ImGui: MIT. `Dalamud.Bindings.ImGui` and `cimgui.dll` are
Dalamud's (AGPL-3.0 per its repository, to be re-read before an application is published): the AGPL v3 and the GPL v3 allow combining
(section 13 of each), the same situation as the plugin, which already links them. Loaded from the user's Dalamud install, nothing
redistributed by the probe. Noto Sans CJK JP and Font Awesome Free: SIL Open Font Licence 1.1, read from Dalamud's assets. Game data and
icons: read from the installed client, never stored in the repository (the two captures show the plugin's pages).

## Reproduce

X-119: the probe became the desktop application `src/TenshoAssistant.Desktop` (same options; see [x119-desktop-app.md](x119-desktop-app.md)); the commands below are those of 0.1030.

```powershell
.\scripts\Verify.ps1   # builds src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll
dotnet build -c Release tools/DesktopProbe
$probe = "tools\DesktopProbe\bin\Release\net10.0-windows\DesktopProbe.exe"
& $probe --page=Weather --zone=155 --capture=weather.png --frames=120
& $probe --page=Lexicon --field=lexiconQuery=cristal --capture=lexicon.png --frames=150
& $probe --headless --loaders=all --out=headless.md
& $probe --inventory --inventory-out=inventory.md
```

Without `--capture` the window stays open and can be used with the mouse until it is closed.
