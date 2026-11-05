# Contributing to Tensho Assistant

*Short summary at the end.*

## Prerequisites

- Windows, Final Fantasy XIV with XIVLauncher/Dalamud (development plugins enabled), for the full check and to try the plugin.
- .NET SDK **10.0** (`global.json` accepts 10.0.100 and newer: the Dalamud repository builds with 10.0.101). Dalamud.NET.Sdk 15 is restored from NuGet during the build, but
  the Dalamud assemblies it compiles against come from your Dalamud install (`%AppData%\XIVLauncher\addon\Hooks\dev`), or from the folder
  named by the `DALAMUD_HOME` environment variable (for example an extracted Dalamud distribution).
- PowerShell 7 recommended (`commit-release.bat` falls back to Windows PowerShell 5.1).

## Build and test

```powershell
.\scripts\Verify.ps1
# If the SDK is installed elsewhere:
.\scripts\Verify.ps1 -DotNet 'C:\path\to\dotnet.exe'
# Run every check again, even if this exact source state already passed (Dalamud or SDK updates also rerun it):
.\scripts\Verify.ps1 -Force
# Without the game or Dalamud: builds and runs the tests (pure logic) and the language checks only, no plugin, no package.
.\scripts\Verify.ps1 -TestsOnly
```

Without `-DotNet`, the script uses `dotnet` from the PATH (or `DOTNET_ROOT`); it first looks for two SDK folders used by the
maintainer's tooling and falls back to the PATH when they are absent.

Other help for contributors: the read-only probes that prove game facts are described in [tools/README.md](tools/README.md); issue and
pull request templates are in `.github/`; an optional pre-push hook runs `Verify.ps1` before every push
(`git config core.hooksPath .githooks`); editor defaults are in `.editorconfig`; drafts of CI workflows wait in
[publish/github](publish/github/README.md) (not active).

The script, run from the repository root, builds `TenshoAssistant.sln` in Release, runs the managed tests (`tests/TenshoAssistant.Tests`),
validates the language files, and checks the package (manifest identity, API level 15, version, exact ZIP content, no language file and no
host assembly in the package). It ends with `VERSION x.y.z (DLL x.y.z.0)`. A contribution must leave the project **green, with no
warnings**. Output DLL: `src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll` (to be added to the Dalamud development plugin list,
then run `/tensho`).

The tests read files by relative path (`src/TenshoAssistant/...`): run them from the repository root, as `Verify.ps1` does.

## Project organization

- Code: `src/TenshoAssistant/` (one folder per domain; files ending in `*.Dalamud.cs` affect the game, while the others are pure C# and testable).
  Pages are `MainWindow.<Page>.cs` partial files; shared UI templates are in `UI/Components/` (`ItemCell`, `DetailPage`, `SettingRow`,
  `EntityMenus`, `DataIndexes`). Embedded data files are in `FreeCompany/Data/` and `Localization/Languages/`.
- Tests: `tests/TenshoAssistant.Tests/` (no-framework test program using `Check(condition, "message")`).
- Probes: `tools/` (read-only console programs over the game files or APIs, e.g. `tools/ShopCurrencyProbe`, `tools/ApiProbe`) and data
  scripts (`tools/submarine-loot.py`).
- Scripts: `scripts/Verify.ps1` (checks), `scripts/Release.ps1` (git steps, called by `Verify.ps1`), `commit-release.bat` (root).
- Documentation: see [docs/README.md](docs/README.md). History: [CHANGELOG.md](CHANGELOG.md).

## Project rules

- **Never invent game data.** Any data link (Excel sheet, native field, external format) must be proven: use a read-only probe
  (`tools/ShopCurrencyProbe`, `tools/ApiProbe`) or a cited source; the output must be kept in `docs/evidence/` and referenced in the code.
- Game data first; community data only when the game does not have it (see `docs/project/DECISIONS.md`, DEC-011).
- Unknown ≠ zero: display "unknown" instead of a guessed value; cite the source of community data (Garland Tools, FFXIV wiki, Universalis,
  GatherBuddy, Teamcraft, LuminaSupplemental, FFXIVGachaSpreadsheet, SubmarineTracker).
- Game memory: read on the framework thread and copied into managed data; nothing heavy in `Draw`; native string reads are bounded.
- Network: requests are bounded in time and size and their results go through the disk cache (`Caching/DataCache.cs`). New community
  sources use `Community/BoundedHttp.cs`; the Universalis clients (`Market/MarketService.cs`, `Market/MarketBoardService.cs`) have their
  own `HttpClient` with a timeout and a response size limit, and the Teamcraft client (`Community/FishBiteTimes.cs`) also goes through
  `BoundedHttp.SendAsync` (POST, read within 1 MiB). Requests start from a player action or a page that needs the data, never in a loop after a
  failure.
- Every subscription, task, or window has an owner and cleanup (proper plugin shutdown/unsubscription).
- Interface: text belongs in `src/TenshoAssistant/Localization/Languages/*.json` (English is the reference, French must be complete); no hardcoded text.
- Settings: each setting has a tooltip and a ↺ « restore default » button (`SettingRow`, `BoolSetting`, `DefaultButton`). A test fails
  `Verify.ps1` when a setting tooltip named in the code (`Tip`, `BoolSetting`, map settings) is missing in English or French.
- Style: follow the neighboring code (naming, comment density, compact C# style used by the project).
- Licences: the project is under **GPL v3**. Data or code under a compatible licence (MIT, Apache-2.0, BSD, CC0, CC-BY, GPL-3.0, and
  EUPL-1.2 through its compatibility clause, decision DEC-025) may be
  used with credit in the README « Credits » and, for embedded files, the licence notice kept in the file (example:
  `src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv`). References under AGPL (Mappy, SortaKinda…) or without a licence
  (xivgear.app…) are read to understand them, **never copied**.
- Out of scope: ACT/IINACT, parsers, anything that provides an advantage in combat or PvP. Game advice is for PvE only; Beastmaster content shows information only (DEC-032, DEC-033).

## Adding a tool

Discuss the idea first with the « Tool proposal » issue form. Then, from the repository root:

```powershell
.\scripts\New-Tool.ps1 -Name RetainerNotes -Category Objects -LabelEn "Retainer notes" -LabelFr "Notes de servants" -DryRun
```

Without `-DryRun`, `scripts/New-Tool.ps1` creates and registers the skeleton:

- `MainWindow.<Name>.cs`: the page, its method `Draw<Name>`;
- `<Name>/<Name>Model.cs`: the pure logic, compiled by the tests. Code that reads the game goes beside it, in a file named `*.Dalamud.cs`;
- `tests/TenshoAssistant.Tests/<Name>Tests.cs`: replace its example;
- the texts in `en.json` and `fr.json`, and one line in the list of pages, `MainWindow.Pages.cs`.

It builds and passes the tests as it is. It changes no version and commits nothing. What is left to you:

1. Write the tool. Prove any game data with a read-only probe (`tools/`), with its output in `docs/evidence/`. Read native memory on the framework thread and copy it; nothing heavy in the draw.
2. Give each subscription an owner and a cleanup, registered in `Plugin.cs` with `registrations.Register`.
3. Give each setting a tooltip and a « ↺ » button. Replace the texts of `PageHelp.<Name>Title`, `Purpose`, `Start` and `Empty`: the « ? » of your page shows them, and a test refuses a page without them. If the page depends on what the game gives, add it to `HelpIndex` so that its help says what is missing.
4. Run `.\scripts\Verify.ps1`, add the `CHANGELOG.md` entry (third version number, x.y.Z) and open the pull request.

A page reached only from another page's tabs has no name in the navigation: give its line `null` as label and its parent as last value, as `Page.CraftLive` does.

## Help, videos and guides

Each page has a short help in the language files (`PageHelp` section, English first, then French), shown by its « ? » and searched in the Help page. Keep it to what the page does, how to start and what to do when it stays empty, and say only what the code does. A wrong sentence is a bug: fix it in a pull request like any text.

To propose a video or a written guide, see [community-help/README.md](community-help/README.md). It needs no code, no version and no CHANGELOG entry.

## Proposing a change

1. One branch per subject; keep each subject small and verifiable.
2. `Verify.ps1` must be green; add new tests for the pure logic.
3. Add an entry in [CHANGELOG.md](CHANGELOG.md) (what, why, evidence, and whether it was "validated in-game" or not), with a `### English`
   summary.
4. Pull request describing the in-game test performed.

## Versions and releases (maintainer)

Branches: work happens on **`Devloppement`**; **`main`** only receives releases. Tags: `Stable-x.y.z`.

Version numbers (`X.Y.Z`, decision DEC-024):

| Number | Meaning | Who raises it |
|---|---|---|
| `X` (first) | Major milestones, e.g. `1.0.0` = the end of the global plan, on the custom repository. | The maintainer only. |
| `Y` (second) | Versions made by the maintainer (with the AI agent); the third number goes back to `0` (`0.317.0` → `0.318.0`). | The maintainer. |
| `Z` (third) | Community versions: changes contributed from outside (`0.317.0` → `0.317.1`). | The maintainer when merging; a pull request may propose it. |

| Command (from the repository root) | What it does after a green build/test/package check |
|---|---|
| `.\scripts\Verify.ps1` | Checks only (Release build, tests, languages, package). |
| `.\scripts\Verify.ps1 -Commit` | On `Devloppement`: commits every change as `x.y.z` with the CHANGELOG entry as description, pushes `Devloppement`. |
| `.\scripts\Verify.ps1 -Release` | From a clean `Devloppement`: commits `repo.json`, sets the local `main` to `origin/main`, merges `Devloppement` into it and tags `Stable-x.y.z` (local steps); then pushes the tag, creates the GitHub release with `latest.zip` and the CHANGELOG notes, pushes `main` (which carries `repo.json`) last, and brings `Devloppement` up to date. Run again after a failure: when the tag exists but the GitHub release or `main` is missing, only the missing steps run. Stops (exit code 2) when the release is complete. |
| `commit-release.bat` (double-click) | `-Commit` then `-Release`, stopping before the release if the commit step fails or stops; `commit-release.bat dry` shows the commands only, `commit-release.bat commit` commits only. |
| add `-DryRun` | Prints the git/gh commands without running them. |
| add `-Force` | Runs every check again; otherwise checks are skipped when the same version and sources (all tracked files except `*.md`, `docs/`, `repo.json`) already passed (stamp in `artifacts/verify-stamp.json`). |

Requirements: `<Version>` raised in the csproj and a `## x.y.z` entry at the top of CHANGELOG.md with a `### English` section (used for
the commit message and the release notes, which gather every version since the last commit or release; the script refuses otherwise); for
`-Release`, GitHub CLI installed (`winget install GitHub.cli`) and logged in once (`gh auth login`). The logic is in `scripts/Release.ps1`.
Commit subjects: `x.y.z` the first time, `x.y.z · suite` for more work before the release, `x.y.z · docs` after it (documentation only).

Custom Dalamud repository (install without the dev DLL): `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json`
(Dalamud › Settings › Experimental › Custom Plugin Repositories), refreshed by each `-Release`.

<a id="publication-dalamud"></a>

## Dalamud publication

The official repository ([DalamudPluginsD17](https://github.com/goatcorp/DalamudPluginsD17), [rules](https://dalamud.dev/plugin-publishing/submission))
requires, among other things (restrictions and AI policy re-read on 2026-11-04):

- a public Git repository built from a specific commit; `manifest.toml` in `testing/live/<plugin>/` (repository, commit, owners, project_path, changelog);
- `images/icon.png` sized 64×64 to 512×512 (preview PNGs up to 730×380, optional); `DalamudPackager`; deterministic version and `AssemblyVersion`
  (no timestamps); windows via the Dalamud window system; a detailed project status: [docs/project/PUBLISHING.md](docs/project/PUBLISHING.md);
- [restrictions](https://dalamud.dev/plugin-publishing/restrictions): no automatic requests to the game server without player action, no
  automated gameplay (crafting, dialogues, loot…), nothing that augments, alters or interferes with combat (combat-related plugins contact the approval team before submitting), no advantage in any competitive or PvP environment, no parsing, no bypassing the Mog Station, no collection of other players' account ids;
- [AI policy](https://dalamud.dev/plugin-publishing/ai-policy): AI use must be **declared** in the pull request (written by a human);
  a submission **entirely generated by AI without real human involvement is rejected**, and twice means a ban; undisclosed AI use in a clearly AI-written submission means a ban; Copilot and Auto submissions are reviewed at the maintainers' discretion and may be closed if larger than the author can account for; the author must understand, test, and explain the code.

**Current project status:** the code is written with an AI agent (level "Auto") under the direction and in-game testing of its author.
No submission to the official repository is planned (DEC-039): the plugin stays on its custom repository, and the rules above are still followed. The state of every requirement: [docs/project/PUBLISHING.md](docs/project/PUBLISHING.md).
How the Dalamud repository builds a plugin (Linux, SDK 10.0.101, no network, lock file) and what the `Build like the Dalamud repository`
CI job checks: [docs/evidence/0.512-d17-build.md](docs/evidence/0.512-d17-build.md). When you add code, keep these true:

- the whole plugin stops in PvP and ranked content, with no setting (DEC-038): every framework subscription goes through `CompetitiveGate`, and a test refuses one that does not;
- external requests are triggered only by player action, an opened page or a setting, and each source has its switch;
- actions that reach the game start on a click; one setting, off by default, acts without one (title by job), kept and declared in the submission (DEC-035); game alarms are sent only from a click (adding an alarm with the setting on, or « Send »), never by the automatic alarms;
- a window that looks like a window uses the Dalamud window system: a free-standing panel through `PanelWindow`, one attached to a game window through `Panels?.Show(...)` (`AttachedPanels`);
- the game map remains hidden and unmodified when the Tensho map replaces it.

---

## Summary

Build and test with `./scripts/Verify.ps1` (.NET SDK 10.0) from the repository root; it must stay green with no warnings. Never invent
game data: prove data links with a read-only probe (`tools/`) and keep the output in `docs/evidence/`. Unknown is not zero. Native memory is
read on the framework thread and copied; native strings are read with a bound. UI text lives in `Localization/Languages/*.json` (English is
the reference, French complete); every setting has a tooltip. AGPL or unlicensed references (Mappy, SortaKinda, xivgear.app) are read,
never copied; this project is GPL-3.0. No ACT/parsers, no combat or PvP advantage. Add a [CHANGELOG.md](CHANGELOG.md) entry with a
`### English` part. Official Dalamud publishing follows D17 rules and the AI policy: AI use must be disclosed, fully AI-generated
submissions are rejected, and the author must understand and test the code (see « Dalamud publication » above).
