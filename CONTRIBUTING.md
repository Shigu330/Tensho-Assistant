# Contributing to Tensho Assistant

> *Traduction française plus bas.*

*Short summary at the end of the English part.*

## Prerequisites

- Windows, Final Fantasy XIV with XIVLauncher/Dalamud (development plugins enabled), for the full check and to try the plugin.
- .NET SDK **10.0** (`global.json` accepts 10.0.100 and newer: the Dalamud repository builds with 10.0.101). Dalamud.NET.Sdk 15 is restored from NuGet during the build, but
  the Dalamud assemblies it compiles against come from your Dalamud install (`%AppData%\XIVLauncher\addon\Hooks\dev`), or from the folder
  named by the `DALAMUD_HOME` environment variable (for example an extracted Dalamud distribution).
- PowerShell 7 recommended (`scripts/commit-release.bat` falls back to Windows PowerShell 5.1).

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
(`git config core.hooksPath .githooks`); editor defaults are in `.editorconfig`; the GitHub workflows (build, CodeQL, player report labels) and the Dependabot settings are in
`.github/`.

The script, run from the repository root, builds `TenshoAssistant.sln` in Release, runs the managed tests (`tests/TenshoAssistant.Tests`),
validates the language files, and checks the package (manifest identity, API level 15, version, exact ZIP content, no language file and no
host assembly in the package). It ends with `VERSION x.y.z (DLL x.y.z.0)`. A contribution must leave the project **green, with no
warnings**. Output DLL: `src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll` (to be added to the Dalamud development plugin list,
then run `/tensho`).

The tests read files by relative path (`src/TenshoAssistant/...`): run them from the repository root, as `Verify.ps1` does.

Outputs:

- DLL: `src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`
- Package: `src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip`
- Desktop app: `src\TenshoAssistant.Desktop\bin\TenshoAssistant.Desktop.zip`, same version; Verify draws every page of it without a window
  (`artifacts\desktop-headless.md`), or prints `SKIP desktop headless` without the game data or XIVLauncher (`-Release` refuses that)

Development builds in game:

1. Add the absolute DLL path to the Dalamud dev plugins list (`/xlsettings` › Experimental).
2. Load the plugin, then open the window with `/tensho`.
3. Check preferences, reloading and unloading.

## Project organisation

```text
.
├── src/TenshoAssistant/      plugin code (one folder per domain; *.Dalamud.cs = game access; UI/Components = shared templates);
│                             Localization/README.md: translation guide, next to the language files
├── src/TenshoAssistant.Desktop/  desktop application: the plugin's pages with the game closed (built and drawn by Verify.ps1)
├── tests/                    managed tests (pure logic)
├── tools/                    read-only data probes (game sheets, APIs) and data scripts
├── scripts/                  Verify.ps1 (build + tests + localization + package), Release.ps1 (git steps),
│                             New-Tool.ps1 (skeleton of a new page), Update-ToolGallery.ps1 (gallery index)
├── repo.json                 Dalamud custom repository entry (refreshed by each release)
├── .github/                  CodeQL and build workflows, Dependabot settings, issue and pull request templates
├── community-help/           help videos and guides listed in the plugin
├── community-templates/      community tool gallery (data only)
├── community-discoveries/    shared discoveries: the common file and each player's merged file (tools/discoveries-merge.py)
├── docs/                     documentation map: docs/README.md
│   ├── project/              scope, roadmap, architecture, testing, references, decisions, publishing
│   └── evidence/             probe outputs cited by the code
├── README.md · CONTRIBUTING.md · SECURITY.md · CHANGELOG.md · LICENSE
└── global.json · TenshoAssistant.sln
```

- Code: `src/TenshoAssistant/` (one folder per domain; files ending in `*.Dalamud.cs` touch the game, files ending in `*.Lumina.cs` copy
  game sheets, and the others are pure C# and testable). Pages are `MainWindow.<Page>.cs` partial files, listed in `MainWindow.Pages.cs`;
  shared UI templates are in `UI/Components/` (`ItemCell`, `DetailPage`, `SettingRow`, `EntityMenus`, `DataIndexes`, `Tooltip`,
  `StatusLines`, `ToolButton`, `DeleteButton`, `Gauges`). Embedded data
  files are in `FreeCompany/Data/`, `Reminders/Data/`, `Events/Data/` and `Localization/Languages/`. The code map is in
  [docs/project/ARCHITECTURE.md](docs/project/ARCHITECTURE.md).
- Tests: `tests/TenshoAssistant.Tests/` (no-framework test program using `Check(condition, "message")`).
- Probes: `tools/` (read-only console programs over the game files or APIs, e.g. `tools/ShopCurrencyProbe`, `tools/ApiProbe`) and data
  scripts (`tools/submarine-loot.py`, `tools/garden-data.py`; `tools/discoveries-merge.py`, the maintainer's merge of the shared
  discoveries into `community-discoveries/`, see its [README](community-discoveries/README.md)).
- Scripts: `scripts/Verify.ps1` (checks), `scripts/Release.ps1` (git steps, called by `Verify.ps1`), `scripts/New-Tool.ps1` (skeleton
  of a new page), `scripts/Update-ToolGallery.ps1` (community tool gallery), `scripts/Get-Dalamud.ps1` (checked Dalamud download for the CI), `scripts/LaunchProof.ps1` (launch proof read by the release), `scripts/commit-release.bat`.
- Documentation: see [docs/README.md](docs/README.md). History: [CHANGELOG.md](CHANGELOG.md).

## Project rules

- **Never invent game data.** Any data link (Excel sheet, native field, external format) must be proven: use a read-only probe
  (`tools/ShopCurrencyProbe`, `tools/ApiProbe`) or a cited source; the output must be kept in `docs/evidence/` and referenced in the code.
- Game data first; community data only when the game does not have it (see `docs/project/DECISIONS.md`, DEC-011).
- Unknown ≠ zero: display « unknown » instead of a guessed value; cite the source of community data (Garland Tools, FFXIV wiki, Universalis,
  GatherBuddy, Teamcraft, LuminaSupplemental, FFXIVGachaSpreadsheet, SubmarineTracker, and every other source on the Credits page).
- Game memory: read on the framework thread and copied into managed data; nothing heavy in `Draw`; native string reads are bounded.
- Network: requests are bounded in time and size and their results go through the disk cache (`Caching/DataCache.cs`). New community
  sources use `Community/BoundedHttp.cs`; the Universalis clients (`Market/MarketService.cs`, `Market/MarketBoardService.cs`) have their
  own `HttpClient` with a timeout and a response size limit, and the Teamcraft client (`Community/FishBiteTimes.cs`) also goes through
  `BoundedHttp.SendAsync` (POST, read within 1 MiB). Requests start from a player action or a page that needs the data, never in a loop after a
  failure. Each source has its switch in Settings › Data and sources, and « Game data and what you observed only » turns them all off at once.
  The shared discoveries exchange (`Discoveries/GitHubExchange.cs`) sends something only after the player connects GitHub.
- Every subscription, task, or window has an owner and cleanup (proper plugin shutdown/unsubscription).
- Interface: text belongs in `src/TenshoAssistant/Localization/Languages/*.json` (English is the reference, French must be complete); no hardcoded text.
  Code, comments and log lines are in English; French stays in the code only as game data to recognise (a test checks it).
- Settings: each setting has a tooltip and a ↺ « restore default » button (`SettingRow`, `BoolSetting`, `DefaultButton`). A test fails
  `Verify.ps1` when a setting tooltip named in the code (`Tip`, `BoolSetting`, map settings) is missing in English or French, or when the
  « Default » line of an on/off setting's tooltip does not state its real default. A setting that only matters when another is on is
  indented under it and greyed while that one is off.
- Shared helpers: tooltips through `Tip` / `TipText` (`UI/Components/Tooltip.cs`), « nothing here » and « loading » lines through
  `EmptyState` (`UI/Components/StatusLines.cs`), saved files through `AtomicFile.Write` (temporary file, then swapped in).
- Style: follow the neighbouring code (naming, comment density, compact C# style used by the project).
- Licences: the project is under **GPL v3**. Data or code under a compatible licence (MIT, Apache-2.0, BSD, CC0, CC-BY, GPL-3.0, and
  EUPL-1.2 through its compatibility clause, decision DEC-025) may be used with credit in the README « Credits » and on the Credits page
  (`src/TenshoAssistant/Credits/CreditCatalog.cs`) and, for embedded files, the licence notice kept in the file (example:
  `src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv`). References under AGPL (Mappy, SortaKinda…) or without a licence
  (xivgear.app…) are read to understand them, **never copied**.
- Out of scope: ACT/IINACT, parsers, anything that provides an advantage in combat or PvP. Game advice is for PvE only; Beastmaster content
  shows game information and a team calculated out of combat (DEC-032, DEC-033, DEC-046); the Crucible of the Unbroken is not switched
  off, Tensho helps there fully, combat included (DEC-038, addition of 2026-11-11); the combo highlight is an option, off by default.

## Adding a tool

Discuss the idea first with the « Tool proposal » issue form. Then, from the repository root:

```powershell
.\scripts\New-Tool.ps1 -Name RetainerNotes -Category Objects -LabelEn "Retainer notes" -LabelFr "Notes de servants" -DryRun
```

`-Category` is one of the navigation groups: `Objects`, `Production`, `World`, `Tracking`, `Progression`, `Content`, `GoldSaucer`, `References`, `Preferences`. Without
`-DryRun`, `scripts/New-Tool.ps1` creates and registers the skeleton:

- `MainWindow.<Name>.cs`: the page, its method `Draw<Name>`;
- `<Name>/<Name>Model.cs`: the pure logic, compiled by the tests. Code that reads the game goes beside it, in a file named `*.Dalamud.cs`;
- `tests/TenshoAssistant.Tests/<Name>Tests.cs`: replace its example;
- the texts in `en.json` and `fr.json` (its name, its title and the help of its « ? »), and one line in the list of pages,
  `MainWindow.Pages.cs`; with `-Parent <Page>`, the page is a tab of that family (its `Tab` and `About`, no title of its own);
- a stop of the guide's visit for every player (`Guide/GuideFlow.cs`) and the kind `AsIs` in the desktop application
  (`DesktopPages.cs`): review both. The Home page's list of tools takes the page from the list of pages and its help; README's table
  of pages is produced by `tools/docgen.py write --blocks`, which the script runs when Python is there.

It changes no version and commits nothing. The skeleton passes the tests as it is (`NewTool1615Tests` runs the script on a copy). Then
follow the list below: the tests refuse a page that misses one of its places.

### Adding a page: where everything is declared

1. **The page.** One name in `enum Page` (`MainWindow.cs`) and one line in the list of pages, `MainWindow.Pages.cs`: its navigation group,
   its name (`Navigation.<Name>`), its `Draw<Name>` method and where its code is. A page of a family (one title and a tab bar in the page
   header, as Crafting or Gathering) names its family as `Parent` and gives its tab name in `Tab` and its description in `About`; a page
   alone draws its own title with `Heading(title, description)`.
2. **Its template.** Blocks of a page go in tabs with `DetailTabs(id, [(key, label, draw), …], ref focus)` (as Storage, Alarms and
   Garden), not in a tab bar written by hand. Tables open with `UiComponents.BeginDataTable` and title their columns with
   `UiComponents.HeadersRow`; numbers right-aligned with `NumberCell`. Empty, waiting and failed states: `EmptyState`, `StateLine`,
   `DrawDataError`. After each control, its tooltip: `Tip("Section.KeyTip")`. Settings: `BoolSetting`, `IntSetting`, `SliderSetting`,
   `ComboSetting` (a default, a « Default: » line and a « ↺ » button). Icon buttons `ToolButton`, game icons `DrawGameIcon`, colours from
   the theme (`Accent`, `Muted`, `Success`, `Warning`, `Error`). Tests refuse the forms these replace, and the number of tab bars and
   tables still written by hand can only go down.
3. **Its texts.** English and French in `Localization/Languages/en.json` and `fr.json` (English is the original; French says « vous »;
   a word of the game is taken from the French client). The help of its « ? »: `PageHelp.<Name>Title`, `Purpose`, `Start` and `Empty`.
   A question players will ask goes in `Help/Faq.cs`. If the page depends on what the game gives, add it to `HelpIndex` so that its help
   says what is missing.
4. **The guide and the Home page.** A stop of the guide's visit (`Guide/GuideFlow.cs`) or a line in its `LeftOut` with the reason; the
   Home page's list of tools, or a line in `DirectoryLeftOut` (`MainWindow.Tools.cs`) with the reason (`PageCoverageTests`).
5. **The desktop application.** Its kind in `src/TenshoAssistant.Desktop/DesktopPages.cs` (`InGame`, `NeedsLink` or `AsIs`, and
   `Editing` when it changes saved data, greyed there between `DesktopEditBegin` and `DesktopEditEnd`). `Verify.ps1` draws every page
   without the game; a page not classified stops it.
6. **Its tests.** The logic in a pure class outside `MainWindow` (the tests do not compile `MainWindow`), tested in
   `tests/TenshoAssistant.Tests/<Name>Tests.cs`: a static class with a static `Cases`, found by itself. Code that reads the game goes in a
   `*.Dalamud.cs` file: read native memory on the framework thread and copy it, nothing heavy in the draw, and prove any game data with a
   read-only probe (`tools/`) whose output goes in `docs/evidence/`. Give each subscription an owner and a cleanup, registered in
   `Plugin.cs` with `registrations.Register`. A page coded without the game adds its reading to `MainWindow.SelfCheckPages.cs`.
7. **Finish.** Run `.\scripts\Verify.ps1`, add the `CHANGELOG.md` entry (third version number, x.y.Z) and open the pull request.

## Help, videos and guides

Each page has a short help in the language files (`PageHelp` section, English first, then French), shown by its « ? » and searched in the
Help page. Keep it to what the page does, how to start and what to do when it stays empty, and say only what the code does. A wrong
sentence is a bug: fix it in a pull request like any text.

To propose a video or a written guide, see [community-help/README.md](community-help/README.md). To propose a tool for the « My tools »
gallery, see [community-templates/README.md](community-templates/README.md). Neither needs code, a version or a CHANGELOG entry.

## Proposing a change

1. One branch per subject; keep each subject small and verifiable.
2. `Verify.ps1` must be green; add new tests for the pure logic.
3. Add an entry in [CHANGELOG.md](CHANGELOG.md) (what, why, evidence, and whether it was « validated in game » or not), with a `### English`
   summary.
4. Pull request towards `Devloppement`, describing the in-game test performed.

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
| `.\scripts\Verify.ps1 -Release` | From a clean, pushed `Devloppement` whose GitHub CI (`Build and test`) is green for that commit, with no other workflow failed: commits `repo.json` and tags it `Stable-x.y.z` (local steps); then pushes the tag, creates the GitHub release with `latest.zip` and the CHANGELOG notes, moves `main` (which carries `repo.json`) to the tagged commit last, as a fast-forward (no merge commit, no branch change), and pushes `Devloppement`. Stops (exit code 2) while the CI runs or has failed, and when one of the maintainer's working files (the 0.802 block of `.gitignore`) or a Discord webhook address would be published. Run again after a failure: when the tag exists but the GitHub release or `main` is missing, only the missing steps run. Stops (exit code 2) when the release is complete. |
| add `-Hotfix "<reason>"` (with `-Release`) | Urgent fix (game patch, privacy) released before its in-game tests: the reason and « not validated in game », in English then French, head the release notes. |
| `scripts/commit-release.bat` (double-click) | `-Commit` then `-Release`, stopping before the release if the commit step fails or stops; the release step then stops until the CI of the pushed commit is green: run it again once it is. `commit-release.bat dry` shows the commands only, `commit-release.bat commit` commits only. |
| add `-DryRun` | Prints the git/gh commands without running them (the CI and push checks only warn). |
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
  automated gameplay (crafting, dialogues, loot…), nothing that augments, alters or interferes with combat (combat-related plugins contact
  the approval team before submitting), no advantage in any competitive or PvP environment, no parsing, no bypassing the Mog Station, no
  collection of other players' account ids;
- [AI policy](https://dalamud.dev/plugin-publishing/ai-policy), for information only: this project no longer follows it, as it submits
  nothing (DEC-050), and its AI use stays disclosed. For a submission, the policy says: AI use must be **declared** in the pull request (written by a human);
  a submission **entirely generated by AI without real human involvement is rejected**, and twice means a ban; undisclosed AI use in a
  clearly AI-written submission means a ban; Copilot and Auto submissions are reviewed at the maintainers' discretion and may be closed if
  larger than the author can account for; the author must understand, test, and explain the code.

**Current project status:** the code is written with an AI agent (level « Auto ») under the direction of its author, who tries the changes
in game. Each release is built and tested automatically (compilation, tests, the desktop app drawn without a window), then loaded
once in game with its self-check, which its notes say (« checked at launch, not tried in detail yet »); the detailed in-game tests
come after, grouped in campaigns, and CHANGELOG.md says for each version whether it was validated in game. An urgent fix
(game patch, privacy) may go out before that launch check, and its notes say so. Release notes are one line per version in players' words (the « For players: » line of each
CHANGELOG entry); the details stay in CHANGELOG.md (DEC-050).
No submission to the official repository is planned (DEC-039): the plugin stays on its custom repository, and the rules above are still
followed. The state of every requirement: [docs/project/PUBLISHING.md](docs/project/PUBLISHING.md).
How the Dalamud repository builds a plugin (Linux, SDK 10.0.101, no network, lock file) and what the `Build like the Dalamud repository`
CI job checks: [docs/evidence/0.512-d17-build.md](docs/evidence/0.512-d17-build.md). When you add code, keep these true:

- the whole plugin stops in PvP and ranked content, with no setting (DEC-038): every framework subscription goes through
  `CompetitiveGate`, and a test refuses one that does not;
- external requests are triggered only by player action, an opened page or a setting, and each source has its switch;
- actions that reach the game start on a click; one setting, off by default, acts without one (title by job), kept and declared (DEC-035);
  game alarms are sent only from a click (adding an alarm with the setting on, or « Send »), never by the automatic alarms;
- a window that looks like a window uses the Dalamud window system: a free-standing panel through `PanelWindow`, one attached to a game
  window through `Panels?.Show(...)` (`AttachedPanels`);
- the game map remains hidden and unmodified when the Tensho map replaces it.

## Summary

Build and test with `./scripts/Verify.ps1` (.NET SDK 10.0) from the repository root; it must stay green with no warnings. Never invent
game data: prove data links with a read-only probe (`tools/`) and keep the output in `docs/evidence/`. Unknown is not zero. Native memory is
read on the framework thread and copied; native strings are read with a bound. UI text lives in `Localization/Languages/*.json` (English is
the reference, French complete); every setting has a tooltip. AGPL or unlicensed references (Mappy, SortaKinda, xivgear.app) are read,
never copied; this project is GPL-3.0. No ACT/parsers, no combat or PvP advantage. Add a [CHANGELOG.md](CHANGELOG.md) entry with a
`### English` part. The plugin is not submitted to the official Dalamud repository (DEC-039), but its rules are still followed,
except the AI policy (DEC-050); AI use stays disclosed (see « Dalamud publication » above).

---

# Contribuer à Tensho Assistant

*Résumé court à la fin de la partie française.*

## Prérequis

- Windows, Final Fantasy XIV avec XIVLauncher/Dalamud (plugins de développement activés), pour le contrôle complet et pour essayer le
  plugin.
- SDK .NET **10.0** (`global.json` accepte 10.0.100 et plus : le dépôt Dalamud compile avec 10.0.101). Dalamud.NET.Sdk 15 est restauré
  depuis NuGet pendant la compilation, mais les assemblys Dalamud contre lesquels il compile viennent de votre installation Dalamud
  (`%AppData%\XIVLauncher\addon\Hooks\dev`), ou du dossier nommé par la variable d'environnement `DALAMUD_HOME` (par exemple une
  distribution Dalamud extraite).
- PowerShell 7 recommandé (`scripts/commit-release.bat` se replie sur Windows PowerShell 5.1).

## Compiler et tester

```powershell
.\scripts\Verify.ps1
# Si le SDK est installé ailleurs :
.\scripts\Verify.ps1 -DotNet 'C:\chemin\vers\dotnet.exe'
# Refaire tous les contrôles, même si cet état exact des sources a déjà réussi (une mise à jour de Dalamud ou du SDK les relance aussi) :
.\scripts\Verify.ps1 -Force
# Sans le jeu ni Dalamud : compile et lance les tests (logique pure) et les contrôles de langues seulement, sans plugin ni paquet.
.\scripts\Verify.ps1 -TestsOnly
```

Sans `-DotNet`, le script utilise `dotnet` du PATH (ou `DOTNET_ROOT`) ; il cherche d'abord deux dossiers de SDK utilisés par les outils
du mainteneur et se replie sur le PATH quand ils sont absents.

Autres aides pour les contributeurs : les sondes en lecture seule qui prouvent les faits du jeu sont décrites dans
[tools/README.md](tools/README.md) ; les modèles de tickets et de pull requests sont dans `.github/` ; un hook pre-push facultatif lance
`Verify.ps1` avant chaque push (`git config core.hooksPath .githooks`) ; les réglages de l'éditeur sont dans `.editorconfig` ; les
workflows GitHub (compilation, CodeQL, étiquettes des rapports de joueurs) et les réglages de Dependabot sont dans `.github/`.

Le script, lancé depuis la racine du dépôt, compile `TenshoAssistant.sln` en Release, lance les tests managés
(`tests/TenshoAssistant.Tests`), valide les fichiers de langue et contrôle le paquet (identité du manifeste, niveau d'API 15, version,
contenu exact du ZIP, aucun fichier de langue ni assembly de l'hôte dans le paquet). Il se termine par `VERSION x.y.z (DLL x.y.z.0)`.
Une contribution doit laisser le projet **vert, sans avertissement**. DLL produite : `src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll`
(à ajouter à la liste des plugins de développement de Dalamud, puis lancer `/tensho`).

Les tests lisent des fichiers par chemin relatif (`src/TenshoAssistant/...`) : lancez-les depuis la racine du dépôt, comme le fait
`Verify.ps1`.

Sorties :

- DLL : `src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`
- Paquet : `src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip`
- Application de bureau : `src\TenshoAssistant.Desktop\bin\TenshoAssistant.Desktop.zip`, même version ; Verify en dessine chaque page sans
  fenêtre (`artifacts\desktop-headless.md`), ou affiche `SKIP desktop headless` sans les données du jeu ou XIVLauncher (`-Release` le refuse)

Versions de développement en jeu :

1. Ajouter le chemin complet de la DLL dans la liste des plugins de développement de Dalamud (`/xlsettings` › Expérimental).
2. Charger le plugin, puis ouvrir la fenêtre avec `/tensho`.
3. Vérifier les préférences, le rechargement et le déchargement.

## Organisation du projet

Arborescence du dépôt : le bloc de la partie anglaise ci-dessus.

- Code : `src/TenshoAssistant/` (un dossier par domaine ; les fichiers en `*.Dalamud.cs` touchent au jeu, ceux en `*.Lumina.cs` copient
  des feuilles du jeu, et les autres sont du C# pur, testable). Les pages sont des fichiers partiels `MainWindow.<Page>.cs`, listés dans
  `MainWindow.Pages.cs` ; les modèles d'interface communs sont dans `UI/Components/` (`ItemCell`, `DetailPage`, `SettingRow`,
  `EntityMenus`, `DataIndexes`, `Tooltip`, `StatusLines`, `ToolButton`, `DeleteButton`, `Gauges`). Les fichiers de données embarqués sont dans `FreeCompany/Data/`, `Reminders/Data/`, `Events/Data/` et
  `Localization/Languages/`. La carte du code est dans [docs/project/ARCHITECTURE.md](docs/project/ARCHITECTURE.md).
- Tests : `tests/TenshoAssistant.Tests/` (programme de test sans framework, avec `Check(condition, "message")`).
- Sondes : `tools/` (programmes console en lecture seule sur les fichiers ou les API du jeu, par exemple `tools/ShopCurrencyProbe`,
  `tools/ApiProbe`) et scripts de données (`tools/submarine-loot.py`, `tools/garden-data.py` ; `tools/discoveries-merge.py`, la fusion
  par le mainteneur des découvertes partagées dans `community-discoveries/`, voir son [README](community-discoveries/README.md)).
- Scripts : `scripts/Verify.ps1` (contrôles), `scripts/Release.ps1` (étapes git, appelé par `Verify.ps1`), `scripts/New-Tool.ps1`
  (squelette d'une nouvelle page), `scripts/Update-ToolGallery.ps1` (galerie d'outils de la communauté), `scripts/Get-Dalamud.ps1` (téléchargement vérifié de Dalamud pour la CI), `scripts/LaunchProof.ps1` (preuve de lancement lue par la release), `scripts/commit-release.bat`.
- Documentation : voir [docs/README.md](docs/README.md). Historique : [CHANGELOG.md](CHANGELOG.md).

## Règles du projet

- **Ne jamais inventer de donnée du jeu.** Tout lien de données (feuille Excel, champ natif, format externe) doit être prouvé : par une
  sonde en lecture seule (`tools/ShopCurrencyProbe`, `tools/ApiProbe`) ou une source citée ; la sortie est gardée dans `docs/evidence/`
  et citée dans le code.
- Données du jeu d'abord ; données de la communauté seulement quand le jeu ne les a pas (voir `docs/project/DECISIONS.md`, DEC-011).
- Inconnu ≠ zéro : afficher « inconnu » plutôt qu'une valeur devinée ; citer la source des données de la communauté (Garland Tools, wiki
  FFXIV, Universalis, GatherBuddy, Teamcraft, LuminaSupplemental, FFXIVGachaSpreadsheet, SubmarineTracker, et toutes les autres sources de
  la page Crédits).
- Mémoire du jeu : lue sur le thread framework et copiée dans des données managées ; rien de lourd dans `Draw` ; les lectures de chaînes
  natives sont bornées.
- Réseau : les requêtes sont bornées en temps et en taille et leurs résultats passent par le cache disque (`Caching/DataCache.cs`). Les
  nouvelles sources de la communauté utilisent `Community/BoundedHttp.cs` ; les clients Universalis (`Market/MarketService.cs`,
  `Market/MarketBoardService.cs`) ont leur propre `HttpClient` avec un délai et une limite de taille de réponse, et le client Teamcraft
  (`Community/FishBiteTimes.cs`) passe aussi par `BoundedHttp.SendAsync` (POST, réponse lue dans la limite de 1 Mio). Les requêtes partent d'une action du
  joueur ou d'une page qui a besoin des données, jamais en boucle après un échec. Chaque source a son interrupteur dans
  Paramètres › Données et sources, et « Données du jeu et vos observations seulement » les coupe toutes d'un coup. L'échange des découvertes partagées
  (`Discoveries/GitHubExchange.cs`) n'envoie quelque chose qu'après la connexion du joueur à GitHub.
- Chaque abonnement, tâche ou fenêtre a un propriétaire et un nettoyage (arrêt et désabonnement propres du plugin).
- Interface : le texte va dans `src/TenshoAssistant/Localization/Languages/*.json` (l'anglais est la référence, le français doit être
  complet) ; aucun texte en dur. Le code, les commentaires et les lignes du journal sont en anglais ; le français ne reste dans le code
  que comme donnée du jeu à reconnaître (un test le contrôle).
- Réglages : chaque réglage a une infobulle et un bouton ↺ « remettre par défaut » (`SettingRow`, `BoolSetting`, `DefaultButton`). Un test
  fait échouer `Verify.ps1` quand une infobulle de réglage nommée dans le code (`Tip`, `BoolSetting`, réglages de la carte) manque en
  anglais ou en français, ou quand la ligne « Par défaut » de l'infobulle d'un réglage oui/non ne dit pas sa vraie valeur par défaut. Un
  réglage qui ne compte que quand un autre est actif est mis en retrait sous lui et grisé tant que celui-ci est coupé.
- Outils communs : les infobulles par `Tip` / `TipText` (`UI/Components/Tooltip.cs`), les lignes « rien ici » et « chargement » par
  `EmptyState` (`UI/Components/StatusLines.cs`), les fichiers enregistrés par `AtomicFile.Write` (fichier temporaire, puis échangé).
- Style : suivre le code voisin (noms, densité des commentaires, style C# compact du projet).
- Licences : le projet est sous **GPL v3**. Des données ou du code sous une licence compatible (MIT, Apache-2.0, BSD, CC0, CC-BY, GPL-3.0,
  et EUPL-1.2 par sa clause de compatibilité, décision DEC-025) peuvent être utilisés avec un crédit dans la partie « Credits » du README
  et sur la page Crédits (`src/TenshoAssistant/Credits/CreditCatalog.cs`) et, pour les fichiers embarqués, l'avis de licence gardé dans le
  fichier (exemple : `src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv`). Les références sous AGPL (Mappy, SortaKinda…) ou
  sans licence (xivgear.app…) sont lues pour les comprendre, **jamais copiées**.
- Hors périmètre : ACT/IINACT, parsers, tout ce qui donne un avantage en combat ou en JcJ. Les conseils de jeu sont pour le JcE
  seulement ; le contenu du dresseur montre des informations du jeu et une équipe calculée hors combat (DEC-032, DEC-033, DEC-046) ; le
  Dédale bestial n'est pas coupé, Tensho y aide entièrement, combat compris (DEC-038, ajout du 2026-11-11) ; le surlignage des combos est une
  option, coupée par défaut.

## Ajouter un outil

Discutez d'abord de l'idée avec le formulaire de ticket « Tool proposal ». Puis, depuis la racine du dépôt :

```powershell
.\scripts\New-Tool.ps1 -Name RetainerNotes -Category Objects -LabelEn "Retainer notes" -LabelFr "Notes de servants" -DryRun
```

`-Category` est l'un des groupes de la navigation : `Objects`, `Production`, `World`, `Tracking`, `Progression`, `Content`, `GoldSaucer`, `References`, `Preferences`. Sans
`-DryRun`, `scripts/New-Tool.ps1` crée et enregistre le squelette :

- `MainWindow.<Name>.cs` : la page, sa méthode `Draw<Name>` ;
- `<Name>/<Name>Model.cs` : la logique pure, compilée par les tests. Le code qui lit le jeu va à côté, dans un fichier nommé
  `*.Dalamud.cs` ;
- `tests/TenshoAssistant.Tests/<Name>Tests.cs` : remplacez son exemple ;
- les textes dans `en.json` et `fr.json` (son nom, son titre et l'aide de son « ? »), et une ligne dans la liste des pages,
  `MainWindow.Pages.cs` ; avec `-Parent <Page>`, la page est un onglet de cette famille (son `Tab` et son `About`, sans titre à elle) ;
- un arrêt de la visite du guide pour tous les joueurs (`Guide/GuideFlow.cs`) et le genre `AsIs` dans l'application de bureau
  (`DesktopPages.cs`) : à revoir tous les deux. La liste des outils de l'accueil prend la page dans la liste des pages et son aide ; le
  tableau des pages du README est produit par `tools/docgen.py write --blocks`, que le script lance quand Python est là.

Il ne change aucune version et ne commite rien. Le squelette passe les tests tel quel (`NewTool1615Tests` lance le script sur une copie).
Suivez ensuite la liste ci-dessous : les tests refusent une page à qui manque l'une de ses places.

### Ajouter une page : où tout se déclare

1. **La page.** Un nom dans `enum Page` (`MainWindow.cs`) et une ligne dans la liste des pages, `MainWindow.Pages.cs` : son groupe de
   navigation, son nom (`Navigation.<Name>`), sa méthode `Draw<Name>` et l'endroit de son code. Une page d'une famille (un titre et une
   barre d'onglets dans l'en-tête de page, comme Artisanat ou Récolte) nomme sa famille en `Parent` et donne son nom d'onglet en `Tab` et
   sa description en `About` ; une page seule dessine son titre avec `Heading(titre, description)`.
2. **Son gabarit.** Les blocs d'une page vont dans des onglets avec `DetailTabs(id, [(clé, libellé, dessin), …], ref focus)` (comme
   Rangements, Alarmes et Jardin), pas dans une barre d'onglets écrite à la main. Les tableaux s'ouvrent avec
   `UiComponents.BeginDataTable` et titrent leurs colonnes avec `UiComponents.HeadersRow` ; les nombres à droite avec `NumberCell`. États
   vide, en attente et en échec : `EmptyState`, `StateLine`, `DrawDataError`. Après chaque contrôle, son infobulle :
   `Tip("Section.CléTip")`. Réglages : `BoolSetting`, `IntSetting`, `SliderSetting`, `ComboSetting` (un défaut, une ligne « Par
   défaut : » et un bouton « ↺ »). Boutons à icône `ToolButton`, icônes du jeu `DrawGameIcon`, couleurs du thème (`Accent`, `Muted`,
   `Success`, `Warning`, `Error`). Des tests refusent les formes qu'ils remplacent, et le nombre de barres d'onglets et de tableaux encore
   écrits à la main ne peut que baisser.
3. **Ses textes.** Anglais et français dans `Localization/Languages/en.json` et `fr.json` (l'anglais est l'original ; le français dit
   « vous » ; un mot du jeu se prend dans le client français). L'aide de son « ? » : `PageHelp.<Name>Title`, `Purpose`, `Start` et
   `Empty`. Une question que les joueurs poseront va dans `Help/Faq.cs`. Si la page dépend de ce que donne le jeu, l'ajouter à
   `HelpIndex` pour que son aide dise ce qui manque.
4. **Le guide et l'accueil.** Un arrêt de la visite du guide (`Guide/GuideFlow.cs`) ou une ligne dans son `LeftOut` avec la raison ; la
   liste des outils de l'accueil, ou une ligne dans `DirectoryLeftOut` (`MainWindow.Tools.cs`) avec la raison (`PageCoverageTests`).
5. **L'application de bureau.** Son genre dans `src/TenshoAssistant.Desktop/DesktopPages.cs` (`InGame`, `NeedsLink` ou `AsIs`, et
   `Editing` quand elle change des données enregistrées, grisées là entre `DesktopEditBegin` et `DesktopEditEnd`). `Verify.ps1` dessine
   chaque page sans le jeu ; une page non classée l'arrête.
6. **Ses tests.** La logique dans une classe pure hors de `MainWindow` (les tests ne compilent pas `MainWindow`), testée dans
   `tests/TenshoAssistant.Tests/<Name>Tests.cs` : une classe statique avec un `Cases` statique, trouvée seule. Le code qui lit le jeu va
   dans un fichier `*.Dalamud.cs` : lire la mémoire native sur le thread framework et la copier, rien de lourd dans le dessin, et prouver
   toute donnée du jeu par une sonde en lecture seule (`tools/`) dont la sortie va dans `docs/evidence/`. Donner à chaque abonnement un
   propriétaire et un nettoyage, enregistrés dans `Plugin.cs` avec `registrations.Register`. Une page codée sans le jeu ajoute sa
   lecture à `MainWindow.SelfCheckPages.cs`.
7. **Finir.** Lancer `.\scripts\Verify.ps1`, ajouter l'entrée de `CHANGELOG.md` (troisième numéro de version, x.y.Z) et ouvrir la pull
   request.

## Aide, vidéos et guides

Chaque page a une courte aide dans les fichiers de langue (section `PageHelp`, l'anglais d'abord, puis le français), montrée par son « ? »
et cherchée dans la page Aide. Tenez-vous-en à ce que fait la page, comment commencer et quoi faire quand elle reste vide, et ne dites
que ce que fait le code. Une phrase fausse est un bogue : corrigez-la dans une pull request comme tout texte.

Pour proposer une vidéo ou un guide écrit, voir [community-help/README.md](community-help/README.md). Pour proposer un outil pour la
galerie de « Mes outils », voir [community-templates/README.md](community-templates/README.md). Ni l'un ni l'autre ne demande de code,
de version ni d'entrée dans le CHANGELOG.

## Proposer un changement

1. Une branche par sujet ; chaque sujet petit et vérifiable.
2. `Verify.ps1` doit être vert ; ajoutez des tests pour la logique pure.
3. Ajoutez une entrée dans [CHANGELOG.md](CHANGELOG.md) (quoi, pourquoi, preuves, et si c'est « validé en jeu » ou non), avec un résumé
   `### English`.
4. Pull request vers `Devloppement`, qui décrit l'essai fait en jeu.

## Versions et releases (mainteneur)

Branches : le travail se fait sur **`Devloppement`** ; **`main`** ne reçoit que les releases. Tags : `Stable-x.y.z`.

Numéros de version (`X.Y.Z`, décision DEC-024) :

| Numéro | Sens | Qui le monte |
|---|---|---|
| `X` (premier) | Grandes étapes, par exemple `1.0.0` = la fin du plan global, sur le dépôt personnalisé. | Le mainteneur seulement. |
| `Y` (deuxième) | Versions faites par le mainteneur (avec l'agent IA) ; le troisième numéro revient à `0` (`0.317.0` → `0.318.0`). | Le mainteneur. |
| `Z` (troisième) | Versions de la communauté : changements apportés de l'extérieur (`0.317.0` → `0.317.1`). | Le mainteneur à la fusion ; une pull request peut le proposer. |

| Commande (depuis la racine du dépôt) | Ce qu'elle fait après un contrôle vert (compilation, tests, paquet) |
|---|---|
| `.\scripts\Verify.ps1` | Contrôles seulement (compilation Release, tests, langues, paquet). |
| `.\scripts\Verify.ps1 -Commit` | Sur `Devloppement` : commite tous les changements sous `x.y.z` avec l'entrée du CHANGELOG comme description, pousse `Devloppement`. |
| `.\scripts\Verify.ps1 -Release` | Depuis une `Devloppement` propre et poussée, dont la CI GitHub (`Build and test`) est verte pour ce commit, sans autre workflow en échec : commite `repo.json` et pose le tag `Stable-x.y.z` (étapes locales) ; puis pousse le tag, crée la release GitHub avec `latest.zip` et les notes du CHANGELOG, avance `main` (qui porte `repo.json`) jusqu'au commit du tag en dernier, en avance rapide (sans commit de fusion ni changement de branche), et pousse `Devloppement`. S'arrête (code de sortie 2) tant que la CI tourne ou a échoué, et quand un des fichiers de travail du mainteneur (le bloc 0.802 de `.gitignore`) ou une adresse de webhook Discord serait publié. À relancer après un échec : quand le tag existe mais que la release GitHub ou `main` manque, seules les étapes manquantes s'exécutent. S'arrête (code de sortie 2) quand la release est complète. |
| ajouter `-Hotfix "<raison>"` (avec `-Release`) | Correctif urgent (patch du jeu, confidentialité) publié avant ses essais en jeu : la raison et « non validé en jeu », en anglais puis en français, en tête des notes de la release. |
| `scripts/commit-release.bat` (double-clic) | `-Commit` puis `-Release`, en s'arrêtant avant la release si l'étape de commit échoue ou s'arrête ; l'étape de release s'arrête ensuite tant que la CI du commit poussé n'est pas verte : la relancer quand elle l'est. `commit-release.bat dry` montre seulement les commandes, `commit-release.bat commit` commite seulement. |
| ajouter `-DryRun` | Affiche les commandes git/gh sans les lancer (les contrôles de CI et de push avertissent seulement). |
| ajouter `-Force` | Refait tous les contrôles ; sinon ils sont sautés quand la même version et les mêmes sources (tous les fichiers suivis sauf `*.md`, `docs/`, `repo.json`) ont déjà réussi (tampon dans `artifacts/verify-stamp.json`). |

Conditions : `<Version>` montée dans le csproj et une entrée `## x.y.z` en tête de CHANGELOG.md avec une section `### English` (utilisée
pour le message de commit et les notes de release, qui rassemblent toutes les versions depuis le dernier commit ou la dernière release ;
le script refuse sinon) ; pour `-Release`, GitHub CLI installé (`winget install GitHub.cli`) et connecté une fois (`gh auth login`). La
logique est dans `scripts/Release.ps1`. Sujets des commits : `x.y.z` la première fois, `x.y.z · suite` pour du travail en plus avant la
release, `x.y.z · docs` après elle (documentation seulement).

Dépôt Dalamud personnalisé (installation sans la DLL de développement) :
`https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json` (`/xlsettings` › Expérimental › Dépôts de plugins personnalisés), mis à jour par chaque `-Release`.

## Publication Dalamud

Le dépôt officiel ([DalamudPluginsD17](https://github.com/goatcorp/DalamudPluginsD17), [règles](https://dalamud.dev/plugin-publishing/submission))
demande entre autres (restrictions et politique IA relues le 2026-11-04) :

- un dépôt Git public compilé à partir d'un commit précis ; `manifest.toml` dans `testing/live/<plugin>/` (repository, commit, owners,
  project_path, changelog) ;
- `images/icon.png` de 64×64 à 512×512 (aperçus PNG jusqu'à 730×380, facultatifs) ; `DalamudPackager` ; version et `AssemblyVersion`
  déterministes (sans horodatage) ; fenêtres par le système de fenêtres de Dalamud ; l'état détaillé du projet :
  [docs/project/PUBLISHING.md](docs/project/PUBLISHING.md) ;
- [restrictions](https://dalamud.dev/plugin-publishing/restrictions) : aucune requête automatique au serveur du jeu sans action du joueur,
  aucun jeu automatisé (artisanat, dialogues, butin…), rien qui augmente, modifie ou gêne le combat (les plugins liés au combat
  contactent l'équipe d'approbation avant de soumettre), aucun avantage en environnement compétitif ou JcJ, aucun parsing, aucun
  contournement de la Mog Station, aucune collecte des identifiants de compte des autres joueurs ;
- [politique IA](https://dalamud.dev/plugin-publishing/ai-policy), pour information seulement : ce projet ne la suit plus, puisqu'il ne
  soumet rien (DEC-050), et son usage de l'IA reste déclaré. Pour une soumission, elle dit : l'usage de l'IA doit être **déclaré** dans la pull request (écrite
  par un humain) ; une soumission **entièrement générée par IA sans réelle implication humaine est refusée**, et une deuxième fois mène à un
  bannissement ; un usage non déclaré dans une soumission clairement écrite par IA mène à un bannissement ; les soumissions Copilot et
  Auto sont examinées à la discrétion des mainteneurs et peuvent être fermées si elles dépassent ce dont l'auteur peut répondre ; l'auteur
  doit comprendre, tester et expliquer le code.

**État actuel du projet :** le code est écrit avec un agent IA (niveau « Auto ») sous la direction de son auteur, qui essaie les
changements en jeu. Chaque release est compilée et testée automatiquement (compilation, tests, application de bureau dessinée sans fenêtre), puis
chargée une fois en jeu avec son auto-contrôle, ce que disent ses notes (« vérifié au lancement, pas encore essayé en détail ») ; les
essais détaillés en jeu viennent après, groupés en campagnes, et CHANGELOG.md dit pour chaque version si elle a été
validée en jeu. Un correctif urgent (patch du jeu, confidentialité) peut partir avant cette vérification au lancement, et ses notes le disent. Les notes de release tiennent en une ligne par version, en mots de joueur (la ligne
« Pour les joueurs : » de chaque entrée du CHANGELOG) ; le détail reste dans CHANGELOG.md (DEC-050). Aucune soumission au dépôt officiel n'est prévue (DEC-039) : le plugin reste sur son dépôt personnalisé, et les règles ci-dessus
sont toujours suivies. L'état de chaque exigence : [docs/project/PUBLISHING.md](docs/project/PUBLISHING.md). Comment le dépôt Dalamud
compile un plugin (Linux, SDK 10.0.101, sans réseau, fichier de verrouillage) et ce que contrôle le job de CI
`Build like the Dalamud repository` : [docs/evidence/0.512-d17-build.md](docs/evidence/0.512-d17-build.md). Quand vous ajoutez du code,
gardez ceci vrai :

- le plugin entier s'arrête en JcJ et en contenu classé, sans réglage (DEC-038) : chaque abonnement au framework passe par
  `CompetitiveGate`, et un test refuse celui qui n'y passe pas ;
- les requêtes extérieures ne partent que d'une action du joueur, d'une page ouverte ou d'un réglage, et chaque source a son
  interrupteur ;
- les actions qui atteignent le jeu partent d'un clic ; un réglage, désactivé par défaut, agit sans clic (titre selon le job), gardé et
  déclaré (DEC-035) ; les alarmes du jeu ne partent que d'un clic (ajouter une alarme avec le réglage actif, ou « Envoyer »), jamais des
  alarmes automatiques ;
- une fenêtre qui ressemble à une fenêtre utilise le système de fenêtres de Dalamud : un panneau libre par `PanelWindow`, un panneau
  accroché à une fenêtre du jeu par `Panels?.Show(...)` (`AttachedPanels`) ;
- la carte du jeu reste cachée et non modifiée quand la carte Tensho la remplace.

## Résumé

Compilez et testez avec `./scripts/Verify.ps1` (SDK .NET 10.0) depuis la racine du dépôt ; il doit rester vert, sans avertissement. N'inventez
jamais de donnée du jeu : prouvez les liens de données par une sonde en lecture seule (`tools/`) et gardez la sortie dans
`docs/evidence/`. Inconnu n'est pas zéro. La mémoire native est lue sur le thread framework et copiée ; les chaînes natives sont lues
avec une borne. Les textes de l'interface sont dans `Localization/Languages/*.json` (l'anglais est la référence, le français complet) ;
chaque réglage a une infobulle. Les références AGPL ou sans licence (Mappy, SortaKinda, xivgear.app) sont lues, jamais copiées ; ce
projet est sous GPL-3.0. Pas d'ACT ni de parser, aucun avantage en combat ou en JcJ. Ajoutez une entrée au [CHANGELOG.md](CHANGELOG.md)
avec une partie `### English`. Le plugin n'est pas soumis au dépôt officiel Dalamud (DEC-039), mais ses règles sont toujours suivies,
sauf la politique IA (DEC-050) ; l'usage de l'IA reste déclaré (voir « Publication Dalamud » ci-dessus).
