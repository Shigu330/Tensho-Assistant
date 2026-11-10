# X-119 — the desktop application, step 1: the plugin's pages with the game closed (2026-11-10)

Plan 4 of the desktop application (decision D-134), step 1, after X-118's probe ([x118-desktop-probe.md](x118-desktop-probe.md)). The
probe `tools/DesktopProbe` was moved (git mv, no copy) into a real application, `src/TenshoAssistant.Desktop`, built, drawn and packaged
with the plugin by `scripts/Verify.ps1` and published next to `latest.zip` by `scripts/Release.ps1` (R33). Steps 2 to 4 (live link with the
game, actions, per-page settings) are not done. The plugin's source is unchanged except its language files (a `Desktop` section) and one
FAQ entry (`Help/Faq.cs`, « Desktop »).

## What was built

| Part | What it does |
|---|---|
| Window | Win32 + OpenGL 1.1 (`user32`, `gdi32`, `opengl32`, part of Windows), X-118's renderer; the plugin's real `MainWindow` fills it above a status bar. Started by a double-click, the console Windows opens is let go (`FreeConsole`). |
| Dalamud and game (R05) | Nothing shipped: every Dalamud, Lumina, ImGui, HexaGen, Mono.Cecil and Newtonsoft reference is `Private=false`, resolved at run time from `DALAMUD_HOME`, else `%APPDATA%\XIVLauncher\addon\Hooks\dev` (as the plugin build); `cimgui.dll` from there too; fonts from `dalamudAssets\dev\UIRes`; game data from the user's sqpack (`--sqpack=`, else `GamePath` of `launcherConfigV3.json`, else Steam's default folder). |
| Plugin | In development `src/TenshoAssistant/bin/x64/Release`; in the package, our own plugin files (those of `latest.zip`) under `plugin\`. A copy goes to `%LOCALAPPDATA%\TenshoAssistant\Desktop\plugin\<hash>` (never the repository nor the installed plugin). |
| Catalogues | The plugin's 12 loaders (maps, weather, duties, GP, craft actions, fashion hints, item uses, items, recipes, gathering, NPC vendors, map index) ticked as the game's framework would, once per frame in the window; cache in `%LOCALAPPDATA%\TenshoAssistant\Desktop\cache`. The lexicon as `Plugin.cs` does. |
| Settings | The plugin's saved settings (`pluginConfigs\TenshoAssistant.json`) read as Dalamud reads them (Newtonsoft from the Hooks folder, type names), read-only: the save action does nothing. Fallback (or `--new-settings`): a new `Configuration`. Text language from the settings, else Windows' display language; game data language from it (fr, de, ja, else English). |
| Page policy | `DesktopPages.cs`, one pure class: InGame = Maps, CraftLive, Fashion (hidden from the navigation; a link to one shows Home with a line saying it stays in the game); NeedsLink = the 17 pages of X-118's inventory; AsIs = the 31 others. An unclassified page fails the headless run and a test (R33). |
| Status bar | A small ImGui window at the bottom: « Game not linked: the live parts of this page (…) stay empty; actions are off » on a NeedsLink page, the in-game redirect line, the catalogues still loading, the version check and the app's version. Texts in `en.json` and `fr.json` (`Desktop`), read through the plugin's `LocalizationManager`. |
| Version | One source: the app's `<Version>` is read by MSBuild from the plugin's csproj. At start, the installed plugin is looked for in `installedPlugins\TenshoAssistant\<version>` and the developer locations of `dalamudConfig.json`, and compared: same / different (both numbers, « updated together from the same release ») / not found; printed and shown in the status bar. |
| Fonts (temporary) | `UiBuilder.IconFont` blocks outside Dalamud (X-118): the plugin copy is rewritten with Mono.Cecil (39 sites) to read the app's font. To be replaced by a real seam in the plugin after the post-7.58 release. |
| Diagnostics | X-118's options kept: `--headless [--step=A,B]`, `--inventory`, `--api=Type[:filter]`, `--il=Type::Method`, `--page=`, `--zone=`, `--field=`, `--click=`, `--capture=<png>`. |

## Headless run (Verify.ps1)

`TenshoAssistant.Desktop --headless`: the 12 loaders, then every page classified and drawn for 20 frames without a window (the pages that
stay in the game too: they must not throw), a watchdog (a loader tick over 120 s or a frame over 30 s fails), the status bar's texts
checked (a missing key fails). Exit 0 only when every page is drawn; 4 = SKIP (no game data or no Dalamud install: Verify prints
`SKIP desktop headless`, `-Release` refuses it). Report: `artifacts/desktop-headless.md`.

Result: **PASS headless: 51 pages drawn without exception (31 as is, 17 need the link, 3 in game), 0 threw, 0 blocked, every page classified**

Loaders: MapCatalogLoader: done in 0,1 s, 29444 framework ticks; WeatherCatalogLoader: done in 0,0 s, 48 framework ticks; DutyCatalogLoader: done in 0,1 s, 147391 framework ticks; GpCatalogLoader: done in 0,0 s, 6 framework ticks; CraftActionCatalogLoader: done in 0,0 s, 18 framework ticks; FashionHintLoader: done in 0,0 s, 34 framework ticks; ItemUseCatalogLoader: done in 0,1 s, 73029 framework ticks; ItemCatalogLoader: done in 1,0 s, 3134782 framework ticks; RecipeCatalogLoader: done in 0,5 s, 1910047 framework ticks; GatheringCatalogLoader: done in 0,3 s, 1000482 framework ticks; NpcVendorLoader: done in 0,6 s, 2134242 framework ticks; MapLocalIndexLoader: done in 0,1 s, 279853 framework ticks.

| Page | Kind | Drawn without exception (20 frames) | Vertices (max) | Icons asked | Exception |
|---|---|---|---|---|---|
| Home | NeedsLink | yes | 5822 | 0 | - |
| Items | NeedsLink | yes | 3492 | 0 | - |
| Gathering | NeedsLink | yes | 6776 | 340 | - |
| Maps | InGame | yes | 4304 | 0 | - |
| MacroChain | AsIs | yes | 5920 | 0 | - |
| SortPreview | AsIs | yes | 3208 | 0 | - |
| Collections | NeedsLink | yes | 6826 | 0 | - |
| BlueMagic | NeedsLink | yes | 4916 | 0 | - |
| TripleTriad | AsIs | yes | 5728 | 0 | - |
| Market | AsIs | yes | 3044 | 0 | - |
| Quests | AsIs | yes | 5266 | 0 | - |
| Chocobo | AsIs | yes | 3152 | 0 | - |
| Events | NeedsLink | yes | 3632 | 0 | - |
| MiniCactpot | AsIs | yes | 4162 | 0 | - |
| Settings | NeedsLink | yes | 6566 | 0 | - |
| Diagnostics | NeedsLink | yes | 8940 | 0 | - |
| DutyLoot | AsIs | yes | 6148 | 380 | - |
| MonsterLoot | AsIs | yes | 3492 | 0 | - |
| GatheringGp | AsIs | yes | 6864 | 0 | - |
| Fashion | InGame | yes | 4280 | 0 | - |
| CraftSimulation | NeedsLink | yes | 4680 | 0 | - |
| CraftLive | InGame | yes | 5508 | 0 | - |
| Weather | NeedsLink | yes | 7988 | 240 | - |
| CraftPresets | AsIs | yes | 4226 | 0 | - |
| NpcVendors | AsIs | yes | 2844 | 0 | - |
| Alarms | AsIs | yes | 5084 | 0 | - |
| Storage | NeedsLink | yes | 4596 | 0 | - |
| Reminders | NeedsLink | yes | 9708 | 0 | - |
| FreeCompany | NeedsLink | yes | 5306 | 0 | - |
| Gear | AsIs | yes | 3328 | 0 | - |
| Credits | AsIs | yes | 11116 | 0 | - |
| Leves | AsIs | yes | 3712 | 0 | - |
| HuntingLog | AsIs | yes | 2556 | 0 | - |
| OceanFishing | NeedsLink | yes | 3220 | 0 | - |
| Garden | AsIs | yes | 3116 | 0 | - |
| MyTools | AsIs | yes | 2992 | 0 | - |
| GatheringCollectables | NeedsLink | yes | 4240 | 0 | - |
| Beastmaster | AsIs | yes | 2588 | 0 | - |
| Help | NeedsLink | yes | 6084 | 0 | - |
| IslandSanctuary | NeedsLink | yes | 3228 | 0 | - |
| CustomDeliveries | AsIs | yes | 2696 | 0 | - |
| MaskedCarnivale | AsIs | yes | 2692 | 0 | - |
| RelicWeapons | AsIs | yes | 2732 | 0 | - |
| AlliedSocieties | AsIs | yes | 2668 | 0 | - |
| GrandCompanies | AsIs | yes | 2720 | 0 | - |
| SpecialDeliveries | AsIs | yes | 2588 | 0 | - |
| FieldContent | AsIs | yes | 2616 | 0 | - |
| DeepDungeons | AsIs | yes | 2656 | 0 | - |
| Lexicon | AsIs | yes | 2600 | 0 | - |
| Fates | AsIs | yes | 2588 | 0 | - |
| Discoveries | AsIs | yes | 3452 | 0 | - |

## Version check output

```text
Version check: same version, plugin 0.1030.0 installed (src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll) and this app 0.1030.0.
```

Here the developer location of `dalamudConfig.json` (`src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`) holds the same build;
no release of the plugin is installed through the custom repository on this PC.

## Capture

[x119-desktop-weather.png](x119-desktop-weather.png): taken by the app itself (`--new-settings --page=Weather --zone=155 --capture=…`),
Coerthas Central Highlands, no player data (new settings); the status bar names the page's live part (alarms) and the version check.

## R05

The package `TenshoAssistant.Desktop.zip` is the app published self-contained for win-x64 (195 files, 37 MB zipped): our `TenshoAssistant.Desktop.exe`
and `.dll`, Microsoft's .NET runtime (MIT, so a double-click works on a PC that has only XIVLauncher: nothing else to install), and under
`plugin/` the plugin's `TenshoAssistant.dll`, `TenshoAssistant.json`, `Raphael.Net.dll`, `raphael_bindings.dll` (as in `latest.zip`).
Verify requires those files, refuses any Dalamud, Lumina, ImGui, cimgui, HexaGen, Mono.Cecil, Newtonsoft, MetadataLoadContext or game
file in it, compares the app and the plugin entries with the build output and both versions with the plugin's. The headless check runs
the packaged exe itself (its own runtime, its `plugin/` folder): what is released is what is drawn.

Double-click test: the zip extracted to an empty folder, `TenshoAssistant.Desktop.exe --new-settings --page=Weather --zone=155
--capture=…` started with `DOTNET_ROOT` unset (no global .NET 10 on this PC): the app started on its own runtime, the 12 catalogues loaded and the capture was taken (Weather, 1,392 icon draws served, 0 missing). Started from a shell here; the console release on a real double-click is not observed by this test.

## How to run

```powershell
.\scripts\Verify.ps1   # builds the plugin, publishes the app to src\TenshoAssistant.Desktop\bin\package, draws every page headless, zips it
src\TenshoAssistant.Desktop\bin\package\TenshoAssistant.Desktop.exe                       # the window
src\TenshoAssistant.Desktop\bin\package\TenshoAssistant.Desktop.exe --headless --out=h.md  # the check
```

From the package: unzip anywhere, double-click `TenshoAssistant.Desktop.exe` (XIVLauncher with Dalamud and the game installed).
