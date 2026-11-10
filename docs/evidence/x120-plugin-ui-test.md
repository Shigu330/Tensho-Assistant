# X-120 — PluginUiTest: every window of a Dalamud plugin drawn without the game (2026-11-10)

> Test tool, not affiliated with the Dalamud team, does not replace Dalamud.
> Game data and Dalamud are loaded from the user's install or the folder given; nothing is redistributed.

A generic test tool for Dalamud plugin creators, after X-118's probe ([x118-desktop-probe.md](x118-desktop-probe.md)) and X-119's
desktop application ([x119-desktop-app.md](x119-desktop-app.md)): "draw every window of your plugin without the game and tell me what
breaks", on a creator's PC and in GitHub CI, laid out to move later to a repository of its own. Everything is under `tools/PluginUiTest/`.
The plugin's source (`src/TenshoAssistant`) is unchanged (game 7.58 installed, Dalamud not ready for it: no logic change).

## What was built

| Part | What it is |
|---|---|
| `src/PluginUiTest.Host` | Class library, namespace `PluginUiTest.Host`: the generic code **moved** (git mv or cut, not copied, R11) out of `src/TenshoAssistant.Desktop`: `DalamudPaths` (Dalamud's folder from `DALAMUD_HOME` or `%APPDATA%\XIVLauncher\addon\Hooks\dev`, its fonts, `installedPlugins`, the sqpack candidates), `DalamudRuntime` (run-time resolution of Dalamud's assemblies and of `cimgui.dll`), `StandIns` (absent recorder, framework, log, texture provider over Lumina, data manager over Lumina, plus `AbsentDataManager` for runs without game data), `ImGuiHeadless` (context, fonts: Dalamud's when present, else ImGui's default), `Rewrite` (UiBuilder's font statics in a temporary copy, Mono.Cecil; option: `WindowSystem.Draw`), `HostWindows` (one window drawn as `WindowSystem` does, without Dalamud's services), `CallGraph` (the IL call graph, used by the app's `--inventory` and by the tool), `Win32Gl` (window and OpenGL 1.1 renderer, generic). |
| `src/TenshoAssistant.Desktop` | Keeps what is Tensho's (pages, `MainWindow`, status bar, loaders, version check, `DesktopPaths` for its own folder and languages). References the library by `ProjectReference`; its package carries `PluginUiTest.Host.dll` (our own DLL, now in Verify's required list). |
| `src/PluginUiTest` | The console exe. `PluginUiTest <YourPlugin.dll> [--dalamud=] [--sqpack=] [--no-game-data] [--frames=N] [--timeout=S] [--out=report.md] [--json=report.json] [--run-anyway=A,B]`. Exit codes 0 all drawn (hidden and skipped do not fail), 1 threw / blocked / crashed, 2 usage or refusal, 3 the plugin could not be created. |
| `samples/SamplePlugin` | A tiny plugin (Dalamud.NET.Sdk 15, built against Hooks\dev, in no solution Verify builds): "Fine window", "Throwing window", "Memory reader" (reads `_ToDoList` through `IGameGui.GetAddonByName(...).Address` and `AtkUnitBase` without a null check). |
| `docs/github-actions-example.yml` | Example workflow for creators (windows-latest, .NET 10, Dalamud's distribution downloaded as their build does, plugin built, tool run with `--no-game-data`, report uploaded). Documentation only, not under `.github/`. |
| `README.md` | English, `---`, French, same headings: what it does, banner, safeguards, usage, exit codes, CI, limits, licence. |

How a plugin is created and drawn: the plugin's DLL is copied to `%LOCALAPPDATA%\PluginUiTest\copies\<name>\<hash>` and, in the copy only,
`UiBuilder.IconFont`/`IconFontFixedWidth`/`DefaultFont`/`MonoFont` and the plugin's calls to `WindowSystem.Draw` are pointed at the tool
(both block outside the game: font service; `WindowSystem.Draw` reads `Service<DalamudConfiguration>`, measured below). The `IDalamudPlugin`
class is made as Dalamud's IoC does (uninitialised object, `[PluginService]` members filled, constructor run on it with
`IDalamudPluginInterface` and service stand-ins; `Create<T>`, `Inject`, `GetOrCreateData`, `RunWhenUiPrepared` served). `OpenMainUi` and
`OpenConfigUi` subscribers are invoked; every `WindowSystem` reachable from the plugin is found (fields a few levels down, static fields,
`UiBuilder.Draw` targets); each window is drawn alone for N frames (`Update`, `DrawConditions`, `PreDraw`, position / size / constraints /
collapse / alpha, `Begin`, `Draw`, `End`, `PostDraw`, `ImGuiP.ErrorCheckEndFrameRecover`, `Render`); then the `UiBuilder.Draw` subscribers
that are not `WindowSystem.Draw`, every window closed.

## Safeguards

| Safeguard | How | Measured |
|---|---|---|
| Never a crash | The parent never loads the plugin nor Dalamud; a child (same exe, `--child`) prints one event per line (`@@PUT {json}`). Over `--timeout` (constructor: twice): **blocked**, child killed (whole tree). Child dies: **crashed** with the exit code in words, the part it was running named, a new child draws the parts not reported yet (`--exclude=`). Windows' error dialog off in the child (`SetErrorMode`). | Throwaway plugin in the session's scratchpad (not kept): `*(int*)0x100000000000=1` in Draw → `CRASHED Crasher: ... access violation (0xC0000005)`; `Thread.Sleep(Infinite)` → `BLOCKED Blocker: did not return within 5 s`; the window after both → `DRAWN After` (table below). |
| Game memory | Before a part runs, IL call graph of the plugin's own code (its DLL and the non-Dalamud DLLs of its folder; Mono.Cecil from Dalamud's folder) from the constructor (and type initializer), each window's `Update`/`DrawConditions`/`PreDraw`/`Draw`/`PostDraw`/`OnOpen`, and the UiBuilder handlers. Flags FFXIVClientStructs and InteropGenerator members; `ldind.*`, `stind.*`, `ldobj`, `stobj`, `cpblk`, `initblk` in a method that works with pointers (pointer parameter, local, return, pinned local, `conv.u`/`conv.i`, member taking or giving a pointer); `calli`; `Marshal.Read*/Write*/Copy/PtrToStructure/StructureToPtr/PtrToString*`; `Unsafe` on pointers; Dalamud's `ISigScanner`, `IGameInteropProvider`, `IGameGui.GetAddonByName/GetAddonById/FindAgentInterface/GetUIModule`, any `Address`, any Dalamud member returning a pointer, `IntPtr` or a `Dalamud.Game.NativeWrapper` type. Delegates handed straight to a Dalamud member (events, `CommandInfo`, `RunOnFrameworkThread`) are not followed (the stand-ins never call them). Flagged parts are **skipped** with the shortest path; `--run-anyway=` runs them in the child. Flagged constructor: exit 3 with the path and the override. The scan reads the rewritten copy (its metadata tokens are those of the loaded assembly: reading the original gave wrong methods, measured during the build). | Sample: `SKIPPED Memory reader ... [at MemoryReader.Draw → Dalamud raw game address: IGameGui.GetAddonByName]`. Tensho: constructor flagged (`Plugin..ctor → NativeMapReplacer.Dispose → IGameGui.GetAddonByName`, an upper bound: that `Dispose` is handed to the plugin's own registration list and runs at unload). |
| No network | The tool makes no request and opens no socket. Child only: a listener of `HttpHandlerDiagnosticListener` records `METHOD url` and throws `NetworkBlockedException` into the request before it connects; `HttpClient.DefaultProxy` points at `127.0.0.1:9` (closed) as a second net; `System.Net.Sockets` `ConnectStart` events recorded, not blocked. | Sample: "Fine window" `OnOpen` asks `https://example.com/` → report column Network: `blocked: GET https://example.com/`, window still drawn. |
| Only the creator's DLL | A folder, a path under `installedPlugins` (the user's XIVLauncher or any `\XIVLauncher\installedPlugins\`), a missing file or a non-DLL: refused, exit 2. Configuration folder: a fresh `%LOCALAPPDATA%\PluginUiTest\config\<name>`, never `pluginConfigs`. | `Refused: C:\Plugins-ff14\src\TenshoAssistant\bin is a folder ...` (exit 2); `Refused: ...\XIVLauncher\installedPlugins\x\1.0\x.dll is a plugin installed by Dalamud (installedPlugins). PluginUiTest never loads installed plugins ...` (exit 2). |

## a) Sample plugin (measured)

Build: `dotnet build tools/PluginUiTest/samples/SamplePlugin -c Release` → `bin\Release\SamplePlugin.dll`. Default run (game data found
in Steam's sqpack, Dalamud 15.0.3.9 from Hooks\dev, Dear ImGui 1.88, Dalamud's fonts), 2 s, exit 1:

```text
  Plugin copy: ...\PluginUiTest\copies\SamplePlugin\...\SamplePlugin.dll (rewritten calls: UiBuilder.IconFont 1)
  CREATED  Plugin constructor: created; UiBuilder subscribers: Draw 1, OpenMainUi 1, OpenConfigUi 0
  RAN      UiBuilder.OpenMainUi → Plugin.ToggleAll: ran
  Window systems: 1 (SamplePlugin: 3 windows)
  DRAWN    Fine window
  THREW    Throwing window: InvalidOperationException: Sample failure: this window throws in Draw on purpose. [at ThrowingWindow.Draw]
  SKIPPED  Memory reader: its code can reach game memory: Dalamud raw game address: IGameGui.GetAddonByName. To draw it anyway: --run-anyway="Memory reader" [at MemoryReader.Draw → Dalamud raw game address: IGameGui.GetAddonByName]

FAIL (exit 1): 3 windows and draw handlers: 1 drawn, 0 hidden, 1 threw, 0 blocked, 0 crashed, 1 skipped
```

Report columns of that run: constructor reached `ICommandManager.AddHandler /sampleui`; Fine window 226 vertices, Network `blocked: GET
https://example.com/`; Throwing window 242 vertices. Same result with `--no-game-data` (1 s, exit 1).

With `--run-anyway="Memory reader"` (3 s, exit 1): `THREW Memory reader: NullReferenceException: Object reference not set to an instance
of an object. [at MemoryReader.Draw]`. The read at `0 + offset` is turned into a NullReferenceException by .NET (the first 64 KB of
the address space), so the child survives; no crash on this window. The crash path itself was measured with a throwaway plugin
(`--timeout=5 --run-anyway=Crasher`, 13 s, exit 1):

| Part | Result | Detail |
|---|---|---|
| Fine | drawn | 58 vertices |
| Crasher | crashed | the drawing process died while it ran: access violation (0xC0000005) …; the other parts were drawn again without it |
| Blocker | blocked | did not return within 5 s: the drawing process was stopped; the other parts were drawn again without it |
| After | drawn | 62 vertices |

Without `--run-anyway`, the same Crasher was skipped: `native pointer write (stind.i4) in Crasher.Draw`.

## b) Tensho Assistant itself (measured)

`src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll` (0.1031.0), plugin source unchanged.

| Run | Time | Exit | Result |
|---|---|---|---|
| default | 4 s | 3 | NOT CREATED: the constructor can reach game memory (`Plugin..ctor → NativeMapReplacer.Dispose → Dalamud raw game address: IGameGui.GetAddonByName`); not run. |
| `--run-anyway=constructor` | 5 s | 0 | created (39 `UiBuilder.IconFont` sites and 2 `WindowSystem.Draw` sites rewritten in the copy); OpenMainUi and OpenConfigUi ran; 2 window systems (TenshoAssistant: 11 windows, TenshoBeastBoard: 1); 2 drawn, 8 hidden, 3 skipped (main window: `native pointer read (ldobj) in MainWindow.DrawSortPreview`; map view: `… GameLogOpener.Gatherable → FFXIVClientStructs: AgentGatheringNote.Instance`; `UiBuilder.Draw → Plugin.Draw`: `… MainWindow.DrawCraftLivePanel → IGameGui.GetAddonByName`). |
| `--run-anyway=*` | 6 s | 0 | 5 drawn (main window 4926 vertices, guide 1860, tracker 218, beast board 1538, `Plugin.Draw` 1860), 8 hidden (`DrawConditions` false: page sweep, map view, task panel, wear bar, GP tick, workshop, preparation and craft lists), 0 threw, 0 blocked, 0 crashed. |
| `--run-anyway=* --no-game-data` | 1 s | 3 | NOT CREATED: `GameDataAbsentException: no game data in this run (IDataManager.GetExcelSheet<WorldDCGroupType>)` in `Plugin..ctor` (the constructor reads a sheet). |

Steps on the way, measured on Tensho and fixed in the tool (not in the plugin): `IDtrBar.Get` answered null and `AlarmDtrEntry..ctor`
threw a NullReferenceException → the tool's absent stand-ins now answer another stand-in for a Dalamud interface (`AbsentProxy.Nested`,
off for the desktop app); `ICommandManager.AddHandler` answered false and the plugin threw « Command /tenshonext already registered » →
an `ICommandManager` stand-in accepts commands; `Plugin.Draw` blocked 30 s in Dalamud's `WindowSystem.Draw` (`Service<DalamudConfiguration>`)
→ the copy's calls to it draw through `HostWindows`.

Compared with Tensho's own headless run (`TenshoAssistant.Desktop --headless`, measured in this session after the move: `PASS headless:
51 pages drawn without exception (31 as is, 17 need the link, 3 in game), 0 threw, 0 blocked`): the generic tool draws each window once
at its current state, so the main window is drawn on its first page only, before any catalogue loads (its `IFramework` never ticks); the
desktop headless run knows Tensho's page list and loaders and draws all 51 pages with the catalogues loaded. The generic tool sees the
other windows (guide, tracker, beast board, panels) that the desktop app does not draw.

## How to reproduce

```powershell
# from C:\Plugins-ff14, .NET 10 SDK, XIVLauncher with Dalamud installed
dotnet build tools/PluginUiTest/samples/SamplePlugin -c Release
dotnet run -c Release --project tools/PluginUiTest/src/PluginUiTest -- tools\PluginUiTest\samples\SamplePlugin\bin\Release\SamplePlugin.dll --out=sample.md
dotnet run -c Release --project tools/PluginUiTest/src/PluginUiTest -- tools\PluginUiTest\samples\SamplePlugin\bin\Release\SamplePlugin.dll "--run-anyway=Memory reader"
dotnet run -c Release --project tools/PluginUiTest/src/PluginUiTest -- src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll "--run-anyway=*" --out=tensho.md
```

## Moved to its own repository (X-124, 2026-11-10)

The tool now lives in [Shigu330/PluginUiTest](https://github.com/Shigu330/PluginUiTest) (public, GPL v3, the user's approval of
2026-11-10). First commit `28f3160`: the library, the tool, the sample, the CI example (now downloading the release zip), tests of the
pure parts (`tests/PluginUiTest.Tests`, taken from this repository's PluginUiTest1032Tests, plus the JSON report's shape) and a JSON
schema of the report (`docs/report.schema.json`, `schemaVersion` 1: camelCase names, outcomes in lower case, `label`, `tool`). Its CI
(`.github/workflows/ci.yml`, windows-latest, Dalamud's distribution downloaded) builds the library, the tool, the sample and the tests,
runs the tests, publishes the zip (nothing of Dalamud in it, checked), runs the tool from the zip on the sample with `--no-game-data` and
checks the outcomes and the schema. First run, measured (run 42893995549, Dalamud.dll 15.0.3.9, ImGui's default font: no Dalamud
assets in CI): tests 4/4; `constructor -> created`, `Fine window -> drawn`, `Throwing window -> threw`, `Memory reader -> skipped`;
exit 1 as expected; the JSON report matches its schema. Rulesets: `main` (no deletion, no force push) and `v*` tags (no deletion, update
or force push), as Tensho's. In this repository only `tools/PluginUiTest/src/PluginUiTest.Host` stays, as a pinned copy (its `.cs` files
the same as at `28f3160`), referenced by the desktop application; the commands below name paths that are now in the other repository.

## What remained before a separate repository (done in X-124, except the wider measures)

- Copy `tools/PluginUiTest` (library, tool, sample, docs) to its own repository; replace the desktop application's `ProjectReference` to
  `PluginUiTest.Host` with a package reference (NuGet) or a pinned copy, so this repository builds without the other one.
- Licence: GPL v3 with Tensho for now; the licence of the separate repository is the owner's choice.
- Its own CI (build the library, the tool and the sample against Dalamud's distribution, run the sample and check the three outcomes).
- Packaging: a `dotnet tool` (or a released zip of the exe) so creators do not build it; a stable JSON schema for the report.
- Wider measures: more plugins (with their own sibling DLLs, ECommons-style helpers), the `###id`-only window names in `--run-anyway`,
  a precise reading of which delegates a constructor only stores (Tensho's constructor is flagged through a `Dispose` it registers).
