# PluginUiTest

> **Test tool, not affiliated with the Dalamud team, does not replace Dalamud.**
> Game data and Dalamud are loaded from your install or the folder you give; nothing is redistributed.

*Version française plus bas.*

"Draw every window of your plugin without the game and tell me what breaks": a console tool for Dalamud plugin creators, on their PC
and in CI (GitHub Actions). You give it your plugin's DLL; it creates the plugin as Dalamud does, opens its main and settings UI, finds
every window of its `WindowSystem`s and draws each one alone for a few frames in a headless ImGui context, then reports, window by window,
what was drawn, what threw (exception, message, the first frame of your code), what blocked, what crashed, and what it did not run
because it can reach game memory. Windows only.

## What it does

1. Loads **only the DLL you name** (a copy of it, see below), never a folder, never a plugin installed by Dalamud (`installedPlugins`).
2. Loads Dalamud's assemblies and its native ImGui (`cimgui.dll`) from your install: `--dalamud=<folder>`, else `DALAMUD_HOME`, else
   `%APPDATA%\XIVLauncher\addon\Hooks\dev` (the folder `Dalamud.NET.Sdk` builds against). Game data from `--sqpack=<folder>`, else
   XIVLauncher's game path, else Steam's default folder; none with `--no-game-data` (CI).
3. Copies your DLL to `%LOCALAPPDATA%\PluginUiTest\copies` and rewrites, in the copy only, the calls that block outside the game:
   `UiBuilder.IconFont`, `IconFontFixedWidth`, `DefaultFont`, `MonoFont` (they wait for Dalamud's font service) point at the tool's font;
   your own calls to `WindowSystem.Draw` (it waits for Dalamud's configuration service) draw through the tool. Your build is not touched.
4. Creates the plugin as Dalamud's IoC does: the `IDalamudPlugin` class, its `[PluginService]` properties and fields filled, its constructor
   given `IDalamudPluginInterface` and the services it asks for (stand-ins, see "Limits"); `Create<T>` and `Inject` work. Its configuration
   folder is a fresh folder of the tool (`%LOCALAPPDATA%\PluginUiTest\config`), never your `pluginConfigs`.
5. Invokes the `OpenMainUi` and `OpenConfigUi` subscribers, finds every `WindowSystem` reachable from the plugin (its fields, a few levels
   down, static fields, and `UiBuilder.Draw`'s targets), and draws each window alone for N frames as Dalamud does: `Update`,
   `DrawConditions`, `PreDraw`, position, size and constraints, `Begin`, `Draw`, `End`, `PostDraw`, then Dalamud's ImGui error recovery.
6. Runs the `UiBuilder.Draw` subscribers that are not a `WindowSystem.Draw` (overlays, your own `Draw`), every window closed.
7. Writes the report (console, `--out=` markdown, `--json=`).

## Safeguards

- **Never a crash of the tool.** All the plugin's code runs in a child process (the same exe). The parent never loads your plugin nor
  Dalamud. A part that runs over `--timeout` is reported **blocked** and the child is stopped; a child that dies (access violation, stack
  overflow) is reported in plain words with the part it was running (**crashed**), and a new child draws the parts not drawn yet. Windows'
  error dialog is switched off in the child.
- **Game memory.** Before a part runs, its IL call graph (Mono.Cecil, read from Dalamud's folder) is walked inside your code (your DLL
  and the other DLLs of its folder that are not Dalamud's) from the constructor, from each window's `Update`, `DrawConditions`, `PreDraw`,
  `Draw`, `PostDraw`, `OnOpen`, and from the `UiBuilder` handlers. A part that can reach FFXIVClientStructs or InteropGenerator members,
  pointer reads or writes (`ldind`, `stind`, `ldobj`, `stobj`, `cpblk`, `initblk` in a method that works with pointers), `calli`,
  `Marshal.Read*`/`Write*`/`Copy`/`PtrToStructure`, or a Dalamud member that hands out raw game addresses (`ISigScanner`,
  `IGameInteropProvider`, `IGameGui.GetAddonByName`, `FindAgentInterface`, any `Address`, any Dalamud member returning a pointer or an
  `IntPtr`) is **skipped** with the first path found, e.g. `MemoryReader.Draw → Dalamud raw game address: IGameGui.GetAddonByName`.
  `--run-anyway=<name>` runs it anyway, still in the child process. If the constructor itself is flagged, the plugin is not created
  (exit 3) unless `--run-anyway=constructor`.
- **No network.** The tool makes no request and opens no socket. In the child, every `HttpClient` request of the plugin is stopped
  before it connects and reported (`blocked: GET https://…`) through .NET's HTTP diagnostics; as a second net the default proxy points at
  a closed local port; raw socket connections are recorded, not blocked (see "Limits").
- **Your files.** Only a copy of your DLL is rewritten; the plugin's configuration goes to a fresh folder of the tool.

## Usage

```powershell
dotnet run -c Release --project tools/PluginUiTest/src/PluginUiTest -- path\to\YourPlugin.dll [options]
```

| Option | What it does |
|---|---|
| `--dalamud=<folder>` | Dalamud's assemblies (default: `DALAMUD_HOME`, else `%APPDATA%\XIVLauncher\addon\Hooks\dev`) |
| `--sqpack=<folder>` | the game's `sqpack` folder (default: XIVLauncher's game path, else Steam's) |
| `--no-game-data` | no game data: `IDataManager` answers absent (as in CI) |
| `--frames=N` | frames per window (default 20) |
| `--timeout=S` | seconds a part may take before it is reported blocked (default 30; the constructor gets twice that) |
| `--out=report.md`, `--json=report.json` | write the report |
| `--run-anyway=A,B` | run these parts even if they can reach game memory: a window name (with or without its `###id`), `constructor`, or `*` |

Measured on the sample plugin of this folder (`samples/SamplePlugin`: a fine window, a window that throws, a window that reads a game
window through `IGameGui` and FFXIVClientStructs):

```text
  CREATED  Plugin constructor: created; UiBuilder subscribers: Draw 1, OpenMainUi 1, OpenConfigUi 0
  RAN      UiBuilder.OpenMainUi → Plugin.ToggleAll: ran
  DRAWN    Fine window
  THREW    Throwing window: InvalidOperationException: Sample failure: this window throws in Draw on purpose. [at ThrowingWindow.Draw]
  SKIPPED  Memory reader: its code can reach game memory: Dalamud raw game address: IGameGui.GetAddonByName. To draw it anyway: --run-anyway="Memory reader" [at MemoryReader.Draw → Dalamud raw game address: IGameGui.GetAddonByName]

FAIL (exit 1): 3 windows and draw handlers: 1 drawn, 0 hidden, 1 threw, 0 blocked, 0 crashed, 1 skipped
```

The report also lists, per part, the Dalamud members reached through the stand-ins, the network requests blocked (the fine window's
`OnOpen` asks `https://example.com/` once: `blocked: GET https://example.com/`) and the plugin's log errors.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | every part drawn, hidden (`DrawConditions` false) or skipped for game memory |
| 1 | something threw, blocked or crashed |
| 2 | usage or refusal (a folder, an installed plugin, no such file, no Dalamud) |
| 3 | the plugin could not be created (no `IDalamudPlugin` class, a service it asks that the tool cannot give, its constructor threw, blocked, crashed, or can reach game memory) |

## In CI (GitHub Actions)

[`docs/github-actions-example.yml`](docs/github-actions-example.yml) is an example for your repository (it is not a workflow of this
one): `windows-latest`, .NET 10, Dalamud's distribution downloaded the way your build already does, your plugin built, PluginUiTest run
with `--no-game-data`, the report uploaded. There is no game data in CI: every sheet your plugin asks is recorded and throws a clear
`GameDataAbsentException`, so code that needs sheets is reported, not drawn.

## Limits

- Windows only (x64), .NET 10; Dalamud API of the installed Dalamud (built and measured with Dalamud.NET.Sdk 15).
- The game-memory analysis is an upper bound of the paths it follows (a branch may never run): calls, constructors, lambdas and
  delegates made on the path, async and iterator bodies, overrides of your own virtual and interface methods. It does not follow
  delegates stored elsewhere and called later, reflection, or native code; the child process is the second safeguard. Delegates handed to
  Dalamud (events, commands, `IFramework.RunOnFrameworkThread`) are not followed: the stand-ins never call them.
- What the stand-ins answer: `IDataManager` reads the game data with Lumina when it is there (English sheets), else answers absent;
  `ITextureProvider` gives game icons read with Lumina (none without game data); `IFramework` is always "on the framework thread" and
  never ticks; `IPluginLog` keeps errors for the report; `ICommandManager` accepts commands (never runs them); `IUiBuilder` records its
  subscribers and gives the tool's font; every other Dalamud interface answers default values (false, 0, null, empty, completed tasks,
  another stand-in for an interface) and records each member reached. Nothing of the game runs: a window that needs the game to show
  something shows its "not available" state.
- Fonts: Dalamud's own fonts from `dalamudAssets\dev\UIRes` when present, else ImGui's default font (icons then draw as `?`).
- Network: requests that skip .NET's HTTP diagnostics and set their own proxy, and raw sockets, are recorded at best, not blocked.
- `Dispose` is not called; the child process ends instead.

## Licence

The tool is part of the Tensho Assistant repository and is under its licence (GPL v3) for now. If it moves to a repository of its own,
the owner will choose that repository's licence then.

---

# PluginUiTest

> **Outil de test, non affilié à l'équipe de Dalamud, ne remplace pas Dalamud.**
> Les données du jeu et Dalamud sont lus dans votre installation ou le dossier que vous donnez ; rien n'est redistribué.

« Dessine chaque fenêtre de ton plugin sans le jeu et dis-moi ce qui casse » : un outil console pour les créateurs de plugins Dalamud,
sur leur PC et en CI (GitHub Actions). Vous lui donnez la DLL de votre plugin ; il crée le plugin comme Dalamud, ouvre son interface
principale et ses réglages, trouve chaque fenêtre de ses `WindowSystem` et dessine chacune seule pendant quelques images dans un contexte
ImGui sans fenêtre, puis dit, fenêtre par fenêtre, ce qui a été dessiné, ce qui a levé une exception (type, message, première ligne de
votre code), ce qui a bloqué, ce qui a planté, et ce qu'il n'a pas lancé parce que cela peut atteindre la mémoire du jeu. Windows seulement.

## Ce qu'il fait

1. Charge **seulement la DLL que vous nommez** (une copie, voir plus bas), jamais un dossier, jamais un plugin installé par Dalamud
   (`installedPlugins`).
2. Charge les assemblys de Dalamud et son ImGui natif (`cimgui.dll`) depuis votre installation : `--dalamud=<dossier>`, sinon
   `DALAMUD_HOME`, sinon `%APPDATA%\XIVLauncher\addon\Hooks\dev` (le dossier contre lequel `Dalamud.NET.Sdk` compile). Les données du jeu
   depuis `--sqpack=<dossier>`, sinon le dossier du jeu de XIVLauncher, sinon celui de Steam ; aucune avec `--no-game-data` (CI).
3. Copie votre DLL dans `%LOCALAPPDATA%\PluginUiTest\copies` et réécrit, dans la copie seulement, les appels qui bloquent hors du jeu :
   `UiBuilder.IconFont`, `IconFontFixedWidth`, `DefaultFont`, `MonoFont` (ils attendent le service des polices de Dalamud) lisent la police
   de l'outil ; vos propres appels à `WindowSystem.Draw` (il attend le service de configuration de Dalamud) dessinent par l'outil. Votre
   build n'est pas touché.
4. Crée le plugin comme l'IoC de Dalamud : la classe `IDalamudPlugin`, ses propriétés et champs `[PluginService]` remplis, son
   constructeur reçoit `IDalamudPluginInterface` et les services qu'il demande (des remplaçants, voir « Limites ») ; `Create<T>` et
   `Inject` marchent. Son dossier de configuration est un dossier neuf de l'outil (`%LOCALAPPDATA%\PluginUiTest\config`), jamais vos
   `pluginConfigs`.
5. Appelle les abonnés de `OpenMainUi` et `OpenConfigUi`, trouve chaque `WindowSystem` joignable depuis le plugin (ses champs, sur
   quelques niveaux, les champs statiques, et les cibles de `UiBuilder.Draw`), et dessine chaque fenêtre seule pendant N images comme
   Dalamud : `Update`, `DrawConditions`, `PreDraw`, position, taille et contraintes, `Begin`, `Draw`, `End`, `PostDraw`, puis la reprise
   d'erreurs ImGui de Dalamud.
6. Lance les abonnés de `UiBuilder.Draw` qui ne sont pas un `WindowSystem.Draw` (surcouches, votre propre `Draw`), toutes les fenêtres
   fermées.
7. Écrit le rapport (console, markdown par `--out=`, `--json=`).

## Protections

- **Jamais de plantage de l'outil.** Tout le code du plugin tourne dans un processus enfant (le même exe). Le parent ne charge jamais
  votre plugin ni Dalamud. Une partie qui dépasse `--timeout` est dite **bloquée** (blocked) et l'enfant est arrêté ; un enfant qui meurt
  (violation d'accès, débordement de pile) est rapporté en clair avec la partie qu'il faisait (**plantée**, crashed), et un nouvel enfant
  dessine les parties pas encore dessinées. La boîte d'erreur de Windows est coupée dans l'enfant.
- **Mémoire du jeu.** Avant qu'une partie tourne, son graphe d'appels IL (Mono.Cecil, lu dans le dossier de Dalamud) est parcouru dans
  votre code (votre DLL et les autres DLL de son dossier qui ne sont pas celles de Dalamud) depuis le constructeur, depuis `Update`,
  `DrawConditions`, `PreDraw`, `Draw`, `PostDraw`, `OnOpen` de chaque fenêtre, et depuis les abonnés de `UiBuilder`. Une partie qui peut
  atteindre des membres de FFXIVClientStructs ou d'InteropGenerator, des lectures ou écritures par pointeur (`ldind`, `stind`, `ldobj`,
  `stobj`, `cpblk`, `initblk` dans une méthode qui manie des pointeurs), `calli`, `Marshal.Read*`/`Write*`/`Copy`/`PtrToStructure`, ou un
  membre de Dalamud qui donne des adresses brutes du jeu (`ISigScanner`, `IGameInteropProvider`, `IGameGui.GetAddonByName`,
  `FindAgentInterface`, tout `Address`, tout membre de Dalamud qui rend un pointeur ou un `IntPtr`) est **sautée** (skipped) avec le
  premier chemin trouvé, par exemple `MemoryReader.Draw → Dalamud raw game address: IGameGui.GetAddonByName`. `--run-anyway=<nom>` la
  lance quand même, toujours dans le processus enfant. Si le constructeur lui-même est signalé, le plugin n'est pas créé (sortie 3), sauf
  avec `--run-anyway=constructor`.
- **Pas de réseau.** L'outil ne fait aucune requête et n'ouvre aucune socket. Dans l'enfant, chaque requête `HttpClient` du plugin est
  arrêtée avant de se connecter et rapportée (`blocked: GET https://…`) par les diagnostics HTTP de .NET ; en second filet, le proxy par
  défaut pointe vers un port local fermé ; les connexions par socket brute sont notées, pas bloquées (voir « Limites »).
- **Vos fichiers.** Seule une copie de votre DLL est réécrite ; la configuration du plugin va dans un dossier neuf de l'outil.

## Utilisation

```powershell
dotnet run -c Release --project tools/PluginUiTest/src/PluginUiTest -- chemin\vers\VotrePlugin.dll [options]
```

| Option | Ce qu'elle fait |
|---|---|
| `--dalamud=<dossier>` | les assemblys de Dalamud (par défaut : `DALAMUD_HOME`, sinon `%APPDATA%\XIVLauncher\addon\Hooks\dev`) |
| `--sqpack=<dossier>` | le dossier `sqpack` du jeu (par défaut : celui de XIVLauncher, sinon celui de Steam) |
| `--no-game-data` | sans données du jeu : `IDataManager` répond absent (comme en CI) |
| `--frames=N` | images par fenêtre (20 par défaut) |
| `--timeout=S` | secondes qu'une partie peut prendre avant d'être dite bloquée (30 par défaut ; le double pour le constructeur) |
| `--out=rapport.md`, `--json=rapport.json` | écrit le rapport |
| `--run-anyway=A,B` | lance ces parties même si elles peuvent atteindre la mémoire du jeu : un nom de fenêtre (avec ou sans son `###id`), `constructor`, ou `*` |

Mesuré sur le plugin d'exemple de ce dossier (`samples/SamplePlugin` : une fenêtre sans souci, une fenêtre qui lève une exception, une
fenêtre qui lit une fenêtre du jeu par `IGameGui` et FFXIVClientStructs) :

```text
  CREATED  Plugin constructor: created; UiBuilder subscribers: Draw 1, OpenMainUi 1, OpenConfigUi 0
  RAN      UiBuilder.OpenMainUi → Plugin.ToggleAll: ran
  DRAWN    Fine window
  THREW    Throwing window: InvalidOperationException: Sample failure: this window throws in Draw on purpose. [at ThrowingWindow.Draw]
  SKIPPED  Memory reader: its code can reach game memory: Dalamud raw game address: IGameGui.GetAddonByName. To draw it anyway: --run-anyway="Memory reader" [at MemoryReader.Draw → Dalamud raw game address: IGameGui.GetAddonByName]

FAIL (exit 1): 3 windows and draw handlers: 1 drawn, 0 hidden, 1 threw, 0 blocked, 0 crashed, 1 skipped
```

Le rapport donne aussi, par partie, les membres de Dalamud atteints par les remplaçants, les requêtes réseau bloquées (le `OnOpen` de la
fenêtre sans souci demande une fois `https://example.com/` : `blocked: GET https://example.com/`) et les erreurs du journal du plugin.

## Codes de sortie

| Code | Sens |
|---|---|
| 0 | chaque partie dessinée, cachée (`DrawConditions` faux) ou sautée pour la mémoire du jeu |
| 1 | une partie a levé une exception, bloqué ou planté |
| 2 | utilisation ou refus (un dossier, un plugin installé, un fichier absent, pas de Dalamud) |
| 3 | le plugin n'a pas pu être créé (pas de classe `IDalamudPlugin`, un service demandé que l'outil ne sait pas donner, son constructeur a levé une exception, bloqué, planté, ou peut atteindre la mémoire du jeu) |

## En CI (GitHub Actions)

[`docs/github-actions-example.yml`](docs/github-actions-example.yml) est un exemple pour votre dépôt (ce n'est pas un workflow de
celui-ci) : `windows-latest`, .NET 10, la distribution de Dalamud téléchargée comme votre build le fait déjà, votre plugin compilé,
PluginUiTest lancé avec `--no-game-data`, le rapport envoyé. Il n'y a pas de données du jeu en CI : chaque feuille que votre plugin demande
est notée et lève une `GameDataAbsentException` claire ; le code qui a besoin des feuilles est donc rapporté, pas dessiné.

## Limites

- Windows seulement (x64), .NET 10 ; l'API de Dalamud est celle du Dalamud installé (construit et mesuré avec Dalamud.NET.Sdk 15).
- L'analyse de la mémoire du jeu est une borne haute des chemins qu'elle suit (une branche peut ne jamais tourner) : appels,
  constructeurs, lambdas et délégués créés sur le chemin, corps async et itérateurs, redéfinitions de vos méthodes virtuelles et
  d'interface. Elle ne suit pas les délégués rangés ailleurs et appelés plus tard, la réflexion ni le code natif ; le processus enfant est
  la seconde protection. Les délégués donnés à Dalamud (événements, commandes, `IFramework.RunOnFrameworkThread`) ne sont pas suivis : les
  remplaçants ne les appellent jamais.
- Ce que répondent les remplaçants : `IDataManager` lit les données du jeu avec Lumina quand elles sont là (feuilles anglaises), sinon
  répond absent ; `ITextureProvider` donne les icônes du jeu lues avec Lumina (aucune sans données du jeu) ; `IFramework` est toujours
  « sur le thread du framework » et ne fait jamais de tick ; `IPluginLog` garde les erreurs pour le rapport ; `ICommandManager` accepte
  les commandes (ne les lance jamais) ; `IUiBuilder` note ses abonnés et donne la police de l'outil ; toute autre interface de Dalamud
  répond des valeurs par défaut (faux, 0, null, vide, tâches finies, un autre remplaçant pour une interface) et note chaque membre
  atteint. Rien du jeu ne tourne : une fenêtre qui a besoin du jeu pour montrer quelque chose montre son état « indisponible ».
- Polices : celles de Dalamud dans `dalamudAssets\dev\UIRes` quand elles sont là, sinon la police par défaut d'ImGui (les icônes
  s'affichent alors en `?`).
- Réseau : les requêtes qui évitent les diagnostics HTTP de .NET et fixent leur propre proxy, et les sockets brutes, sont au mieux notées,
  pas bloquées.
- `Dispose` n'est pas appelé ; le processus enfant se termine à la place.

## Licence

L'outil fait partie du dépôt de Tensho Assistant et suit sa licence (GPL v3) pour l'instant. S'il part dans un dépôt à lui, le
propriétaire choisira alors la licence de ce dépôt.
