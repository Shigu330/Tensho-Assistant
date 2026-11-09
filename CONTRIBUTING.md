# Contributing to Tensho Assistant

> *Traduction française plus bas.*

*Short summary at the end of the English part.*

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
(`git config core.hooksPath .githooks`); editor defaults are in `.editorconfig`; the CI workflows (build, CodeQL, Dependabot) are in
`.github/`, with their history in [publish/github](publish/github/README.md).

The script, run from the repository root, builds `TenshoAssistant.sln` in Release, runs the managed tests (`tests/TenshoAssistant.Tests`),
validates the language files, and checks the package (manifest identity, API level 15, version, exact ZIP content, no language file and no
host assembly in the package). It ends with `VERSION x.y.z (DLL x.y.z.0)`. A contribution must leave the project **green, with no
warnings**. Output DLL: `src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll` (to be added to the Dalamud development plugin list,
then run `/tensho`).

The tests read files by relative path (`src/TenshoAssistant/...`): run them from the repository root, as `Verify.ps1` does.

## Project organization

- Code: `src/TenshoAssistant/` (one folder per domain; files ending in `*.Dalamud.cs` touch the game, files ending in `*.Lumina.cs` copy
  game sheets, and the others are pure C# and testable). Pages are `MainWindow.<Page>.cs` partial files, listed in `MainWindow.Pages.cs`;
  shared UI templates are in `UI/Components/` (`ItemCell`, `DetailPage`, `SettingRow`, `EntityMenus`, `DataIndexes`, `Tooltip`,
  `StatusLines`, `ToolButton`). Embedded data
  files are in `FreeCompany/Data/`, `Reminders/Data/`, `Events/Data/` and `Localization/Languages/`. The code map is in
  [docs/project/ARCHITECTURE.md](docs/project/ARCHITECTURE.md).
- Tests: `tests/TenshoAssistant.Tests/` (no-framework test program using `Check(condition, "message")`).
- Probes: `tools/` (read-only console programs over the game files or APIs, e.g. `tools/ShopCurrencyProbe`, `tools/ApiProbe`) and data
  scripts (`tools/submarine-loot.py`, `tools/garden-data.py`; `tools/discoveries-merge.py`, the maintainer's merge of the shared
  discoveries into `community-discoveries/`, see its [README](community-discoveries/README.md)).
- Scripts: `scripts/Verify.ps1` (checks), `scripts/Release.ps1` (git steps, called by `Verify.ps1`), `scripts/New-Tool.ps1` (skeleton
  of a new page), `scripts/Update-ToolGallery.ps1` (community tool gallery), `commit-release.bat` (root).
- Documentation: see [docs/README.md](docs/README.md). History: [CHANGELOG.md](CHANGELOG.md).

## Project rules

- **Never invent game data.** Any data link (Excel sheet, native field, external format) must be proven: use a read-only probe
  (`tools/ShopCurrencyProbe`, `tools/ApiProbe`) or a cited source; the output must be kept in `docs/evidence/` and referenced in the code.
- Game data first; community data only when the game does not have it (see `docs/project/DECISIONS.md`, DEC-011).
- Unknown ≠ zero: display "unknown" instead of a guessed value; cite the source of community data (Garland Tools, FFXIV wiki, Universalis,
  GatherBuddy, Teamcraft, LuminaSupplemental, FFXIVGachaSpreadsheet, SubmarineTracker, and every other source on the Credits page).
- Game memory: read on the framework thread and copied into managed data; nothing heavy in `Draw`; native string reads are bounded.
- Network: requests are bounded in time and size and their results go through the disk cache (`Caching/DataCache.cs`). New community
  sources use `Community/BoundedHttp.cs`; the Universalis clients (`Market/MarketService.cs`, `Market/MarketBoardService.cs`) have their
  own `HttpClient` with a timeout and a response size limit, and the Teamcraft client (`Community/FishBiteTimes.cs`) also goes through
  `BoundedHttp.SendAsync` (POST, read within 1 MiB). Requests start from a player action or a page that needs the data, never in a loop after a
  failure. Each source has its switch in Settings › Advanced, and « Game data and what you observed only » turns them all off at once.
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
- Style: follow the neighboring code (naming, comment density, compact C# style used by the project).
- Licences: the project is under **GPL v3**. Data or code under a compatible licence (MIT, Apache-2.0, BSD, CC0, CC-BY, GPL-3.0, and
  EUPL-1.2 through its compatibility clause, decision DEC-025) may be used with credit in the README « Credits » and on the Credits page
  (`src/TenshoAssistant/Credits/CreditCatalog.cs`) and, for embedded files, the licence notice kept in the file (example:
  `src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv`). References under AGPL (Mappy, SortaKinda…) or without a licence
  (xivgear.app…) are read to understand them, **never copied**.
- Out of scope: ACT/IINACT, parsers, anything that provides an advantage in combat or PvP. Game advice is for PvE only; Beastmaster content
  shows information only (DEC-032, DEC-033).

## Adding a tool

Discuss the idea first with the « Tool proposal » issue form. Then, from the repository root:

```powershell
.\scripts\New-Tool.ps1 -Name RetainerNotes -Category Objects -LabelEn "Retainer notes" -LabelFr "Notes de servants" -DryRun
```

`-Category` is one of the navigation groups: `Objects`, `Production`, `World`, `Progression`, `GoldSaucer`, `Preferences`. Without
`-DryRun`, `scripts/New-Tool.ps1` creates and registers the skeleton:

- `MainWindow.<Name>.cs`: the page, its method `Draw<Name>`;
- `<Name>/<Name>Model.cs`: the pure logic, compiled by the tests. Code that reads the game goes beside it, in a file named `*.Dalamud.cs`;
- `tests/TenshoAssistant.Tests/<Name>Tests.cs`: replace its example;
- the texts in `en.json` and `fr.json`, and one line in the list of pages, `MainWindow.Pages.cs`.

It builds and passes the tests as it is. It changes no version and commits nothing. What is left to you:

1. Write the tool. Prove any game data with a read-only probe (`tools/`), with its output in `docs/evidence/`. Read native memory on the
   framework thread and copy it; nothing heavy in the draw.
2. Give each subscription an owner and a cleanup, registered in `Plugin.cs` with `registrations.Register`.
3. Give each setting a tooltip and a « ↺ » button. Replace the texts of `PageHelp.<Name>Title`, `Purpose`, `Start` and `Empty`: the « ? »
   of your page shows them, and a test refuses a page without them. If the page depends on what the game gives, add it to `HelpIndex` so
   that its help says what is missing.
4. Run `.\scripts\Verify.ps1`, add the `CHANGELOG.md` entry (third version number, x.y.Z) and open the pull request.

A page reached only from another page's tabs has no name in the navigation: give its line `null` as label and its parent as last value,
as `Page.CraftLive` does.

## Help, videos and guides

Each page has a short help in the language files (`PageHelp` section, English first, then French), shown by its « ? » and searched in the
Help page. Keep it to what the page does, how to start and what to do when it stays empty, and say only what the code does. A wrong
sentence is a bug: fix it in a pull request like any text.

To propose a video or a written guide, see [community-help/README.md](community-help/README.md). To propose a tool for the « My tools »
gallery, see [community-templates/README.md](community-templates/README.md). Neither needs code, a version or a CHANGELOG entry.

## Proposing a change

1. One branch per subject; keep each subject small and verifiable.
2. `Verify.ps1` must be green; add new tests for the pure logic.
3. Add an entry in [CHANGELOG.md](CHANGELOG.md) (what, why, evidence, and whether it was "validated in-game" or not), with a `### English`
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
| `commit-release.bat` (double-click) | `-Commit` then `-Release`, stopping before the release if the commit step fails or stops; the release step then stops until the CI of the pushed commit is green: run it again once it is. `commit-release.bat dry` shows the commands only, `commit-release.bat commit` commits only. |
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
- [AI policy](https://dalamud.dev/plugin-publishing/ai-policy): AI use must be **declared** in the pull request (written by a human);
  a submission **entirely generated by AI without real human involvement is rejected**, and twice means a ban; undisclosed AI use in a
  clearly AI-written submission means a ban; Copilot and Auto submissions are reviewed at the maintainers' discretion and may be closed if
  larger than the author can account for; the author must understand, test, and explain the code.

**Current project status:** the code is written with an AI agent (level "Auto") under the direction and in-game testing of its author.
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
`### English` part. The plugin is not submitted to the official Dalamud repository (DEC-039), but its rules and AI policy are still
followed: AI use is disclosed, and the author must understand and test the code (see « Dalamud publication » above).

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
- PowerShell 7 recommandé (`commit-release.bat` se replie sur Windows PowerShell 5.1).

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
workflows de CI (compilation, CodeQL, Dependabot) sont dans `.github/`, avec leur historique dans [publish/github](publish/github/README.md).

Le script, lancé depuis la racine du dépôt, compile `TenshoAssistant.sln` en Release, lance les tests managés
(`tests/TenshoAssistant.Tests`), valide les fichiers de langue et contrôle le paquet (identité du manifeste, niveau d'API 15, version,
contenu exact du ZIP, aucun fichier de langue ni assembly de l'hôte dans le paquet). Il se termine par `VERSION x.y.z (DLL x.y.z.0)`.
Une contribution doit laisser le projet **vert, sans avertissement**. DLL produite : `src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll`
(à ajouter à la liste des plugins de développement de Dalamud, puis lancer `/tensho`).

Les tests lisent des fichiers par chemin relatif (`src/TenshoAssistant/...`) : lancez-les depuis la racine du dépôt, comme le fait
`Verify.ps1`.

## Organisation du projet

- Code : `src/TenshoAssistant/` (un dossier par domaine ; les fichiers en `*.Dalamud.cs` touchent au jeu, ceux en `*.Lumina.cs` copient
  des feuilles du jeu, et les autres sont du C# pur, testable). Les pages sont des fichiers partiels `MainWindow.<Page>.cs`, listés dans
  `MainWindow.Pages.cs` ; les modèles d'interface communs sont dans `UI/Components/` (`ItemCell`, `DetailPage`, `SettingRow`,
  `EntityMenus`, `DataIndexes`, `Tooltip`, `StatusLines`, `ToolButton`). Les fichiers de données embarqués sont dans `FreeCompany/Data/`, `Reminders/Data/`, `Events/Data/` et
  `Localization/Languages/`. La carte du code est dans [docs/project/ARCHITECTURE.md](docs/project/ARCHITECTURE.md).
- Tests : `tests/TenshoAssistant.Tests/` (programme de test sans framework, avec `Check(condition, "message")`).
- Sondes : `tools/` (programmes console en lecture seule sur les fichiers ou les API du jeu, par exemple `tools/ShopCurrencyProbe`,
  `tools/ApiProbe`) et scripts de données (`tools/submarine-loot.py`, `tools/garden-data.py` ; `tools/discoveries-merge.py`, la fusion
  par le mainteneur des découvertes partagées dans `community-discoveries/`, voir son [README](community-discoveries/README.md)).
- Scripts : `scripts/Verify.ps1` (contrôles), `scripts/Release.ps1` (étapes git, appelé par `Verify.ps1`), `scripts/New-Tool.ps1`
  (squelette d'une nouvelle page), `scripts/Update-ToolGallery.ps1` (galerie d'outils de la communauté), `commit-release.bat` (racine).
- Documentation : voir [docs/README.md](docs/README.md). Historique : [CHANGELOG.md](CHANGELOG.md).

## Règles du projet

- **Ne jamais inventer une donnée du jeu.** Tout lien de données (feuille Excel, champ natif, format externe) doit être prouvé : par une
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
  (`Community/FishBiteTimes.cs`) passe aussi par `BoundedHttp.SendAsync` (POST, lu dans 1 Mio). Les requêtes partent d'une action du
  joueur ou d'une page qui a besoin des données, jamais en boucle après un échec. Chaque source a son interrupteur dans
  Paramètres › Avancé, et « Données du jeu et observées seulement » les coupe toutes d'un coup. L'échange des découvertes partagées
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
  seulement ; le contenu du dresseur ne montre que des informations (DEC-032, DEC-033).

## Ajouter un outil

Discutez d'abord de l'idée avec le formulaire de ticket « Tool proposal ». Puis, depuis la racine du dépôt :

```powershell
.\scripts\New-Tool.ps1 -Name RetainerNotes -Category Objects -LabelEn "Retainer notes" -LabelFr "Notes de servants" -DryRun
```

`-Category` est l'un des groupes de la navigation : `Objects`, `Production`, `World`, `Progression`, `GoldSaucer`, `Preferences`. Sans
`-DryRun`, `scripts/New-Tool.ps1` crée et enregistre le squelette :

- `MainWindow.<Name>.cs` : la page, sa méthode `Draw<Name>` ;
- `<Name>/<Name>Model.cs` : la logique pure, compilée par les tests. Le code qui lit le jeu va à côté, dans un fichier nommé
  `*.Dalamud.cs` ;
- `tests/TenshoAssistant.Tests/<Name>Tests.cs` : remplacez son exemple ;
- les textes dans `en.json` et `fr.json`, et une ligne dans la liste des pages, `MainWindow.Pages.cs`.

Il compile et passe les tests tel quel. Il ne change aucune version et ne commite rien. Ce qui vous reste :

1. Écrire l'outil. Prouver toute donnée du jeu par une sonde en lecture seule (`tools/`), avec sa sortie dans `docs/evidence/`. Lire la
   mémoire native sur le thread framework et la copier ; rien de lourd dans le dessin.
2. Donner à chaque abonnement un propriétaire et un nettoyage, enregistrés dans `Plugin.cs` avec `registrations.Register`.
3. Donner à chaque réglage une infobulle et un bouton « ↺ ». Remplacer les textes de `PageHelp.<Name>Title`, `Purpose`, `Start` et
   `Empty` : le « ? » de votre page les montre, et un test refuse une page qui ne les a pas. Si la page dépend de ce que donne le jeu,
   l'ajouter à `HelpIndex` pour que son aide dise ce qui manque.
4. Lancer `.\scripts\Verify.ps1`, ajouter l'entrée de `CHANGELOG.md` (troisième numéro de version, x.y.Z) et ouvrir la pull request.

Une page atteinte seulement par les onglets d'une autre page n'a pas de nom dans la navigation : donnez à sa ligne `null` comme libellé
et sa page parente comme dernière valeur, comme le fait `Page.CraftLive`.

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
| `commit-release.bat` (double-clic) | `-Commit` puis `-Release`, en s'arrêtant avant la release si l'étape de commit échoue ou s'arrête ; l'étape de release s'arrête ensuite tant que la CI du commit poussé n'est pas verte : la relancer quand elle l'est. `commit-release.bat dry` montre seulement les commandes, `commit-release.bat commit` commite seulement. |
| ajouter `-DryRun` | Affiche les commandes git/gh sans les lancer (les contrôles de CI et de push avertissent seulement). |
| ajouter `-Force` | Refait tous les contrôles ; sinon ils sont sautés quand la même version et les mêmes sources (tous les fichiers suivis sauf `*.md`, `docs/`, `repo.json`) ont déjà réussi (tampon dans `artifacts/verify-stamp.json`). |

Conditions : `<Version>` montée dans le csproj et une entrée `## x.y.z` en tête de CHANGELOG.md avec une section `### English` (utilisée
pour le message de commit et les notes de release, qui rassemblent toutes les versions depuis le dernier commit ou la dernière release ;
le script refuse sinon) ; pour `-Release`, GitHub CLI installé (`winget install GitHub.cli`) et connecté une fois (`gh auth login`). La
logique est dans `scripts/Release.ps1`. Sujets des commits : `x.y.z` la première fois, `x.y.z · suite` pour du travail en plus avant la
release, `x.y.z · docs` après elle (documentation seulement).

Dépôt Dalamud personnalisé (installation sans la DLL de développement) :
`https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json` (Dalamud › Settings › Experimental › Custom Plugin
Repositories), mis à jour par chaque `-Release`.

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
- [politique IA](https://dalamud.dev/plugin-publishing/ai-policy) : l'usage de l'IA doit être **déclaré** dans la pull request (écrite
  par un humain) ; une soumission **entièrement générée par IA sans réelle implication humaine est refusée**, et deux fois mènent à un
  bannissement ; un usage non déclaré dans une soumission clairement écrite par IA mène à un bannissement ; les soumissions Copilot et
  Auto sont examinées à la discrétion des mainteneurs et peuvent être fermées si elles dépassent ce dont l'auteur peut répondre ; l'auteur
  doit comprendre, tester et expliquer le code.

**État actuel du projet :** le code est écrit avec un agent IA (niveau « Auto ») sous la direction et avec les essais en jeu de son
auteur. Aucune soumission au dépôt officiel n'est prévue (DEC-039) : le plugin reste sur son dépôt personnalisé, et les règles ci-dessus
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

Compilez et testez avec `./scripts/Verify.ps1` (SDK .NET 10.0) depuis la racine du dépôt ; il doit rester vert, sans avertissement. Ne
jamais inventer une donnée du jeu : prouvez les liens de données par une sonde en lecture seule (`tools/`) et gardez la sortie dans
`docs/evidence/`. Inconnu n'est pas zéro. La mémoire native est lue sur le thread framework et copiée ; les chaînes natives sont lues
avec une borne. Les textes de l'interface sont dans `Localization/Languages/*.json` (l'anglais est la référence, le français complet) ;
chaque réglage a une infobulle. Les références AGPL ou sans licence (Mappy, SortaKinda, xivgear.app) sont lues, jamais copiées ; ce
projet est sous GPL-3.0. Pas d'ACT ni de parser, aucun avantage en combat ou en JcJ. Ajoutez une entrée au [CHANGELOG.md](CHANGELOG.md)
avec une partie `### English`. Le plugin n'est pas soumis au dépôt officiel Dalamud (DEC-039), mais ses règles et sa politique IA sont
toujours suivies : l'usage de l'IA est déclaré, et l'auteur doit comprendre et tester le code (voir « Publication Dalamud » ci-dessus).
