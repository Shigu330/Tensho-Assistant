# Publishing on the official Dalamud repository (D17)

> *Traduction française plus bas.*

**Since DEC-039 (2026-11-05): no submission.** Tensho stays on its custom repository. This document is the list of the Dalamud rules,
which are still applied; the submission steps are no longer to be done.

Rules re-read on 2026-11-04: [submission](https://dalamud.dev/plugin-publishing/submission),
[approval](https://dalamud.dev/plugin-publishing/approval-process),
[restrictions](https://dalamud.dev/plugin-publishing/restrictions), [AI policy](https://dalamud.dev/plugin-publishing/ai-policy) (no longer followed since DEC-050),
[metadata](https://dalamud.dev/plugin-development/plugin-metadata),
[technical considerations](https://dalamud.dev/plugin-development/technical-considerations), README of
[DalamudPluginsD17](https://github.com/goatcorp/DalamudPluginsD17). How they build: [record](../evidence/0.512-d17-build.md).
List checked against the code on 2026-11-09.

The custom repository: `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json`, updated by `Verify.ps1 -Release`,
run by the agent (DEC-050); the latest release is on the GitHub releases page. Version 1.0.0 marks the end of the global plan on this custom repository; the author decides
it (DEC-039).

Answer received on the Dalamud Discord, 2026-11-05 (hOst1k, question asked by the author): no size limit if the plugin follows the rules;
the bigger it is, the longer the first review; better not to aim for something too complex from the start, Simple Tweaks and « VanillaTweaks » (probably
VanillaPlus) having grown over time. Exact role of this person in the team: not checked. At that date: 57,419 lines of C# in the plugin,
123,519 lines tracked by git. The AI policy also applied then: an « Auto » submission too big for the author to account for may be closed
(no longer followed since DEC-050).

Rest of the same exchange, 2026-11-05: after learning of the 57,000 lines written with an agent, goat answers that this amount of code
cannot be reviewed, new Era adds that many things already exist in other plugins, and MidoriKami asks the approval team (`@dalamud-pac`)
whether there is a size guideline for AI-written code. **A single-block submission to the official repository therefore looked
compromised**; the author then decided not to submit (DEC-039). The custom repository already works and does not wait for this review.
State at that date: 1,120 tests, many of which read the source code without running it; 415 in-game test boxes open against 156 ticked;
first commit on 2026-10-31.

## Former submission steps (not to do since DEC-039)

Kept because several of them are Dalamud rules the code still follows. Points marked **Dalamud rule** need a summary and the author's
agreement (instruction of 2026-11-04), except the AI policy, no longer followed (DEC-050).

| # | Point | Who | State |
|---|---|---|---|
| 1 | Play on the latest version: Dalamud asks that the author has tested. Open boxes in [TESTING](TESTING.md); the in-game tests are phase 6 of the [global plan](GLOBAL-PLAN.md), in small batches at the end of each block of work, grouped by place | author + agent | in progress (phase 6) |
| 2 | **Dalamud rule**: title by job, the only action towards the game without a click (setting off by default) | author | **decided** (DEC-035): kept as it is |
| 3 | Game alarms: nothing to change. The `/alarm` commands always start from a player's click: adding an alarm while the setting is on, or « Send »; the automatic alarms of retainers and vessels never create one | — | done |
| 3 bis | **Dalamud rule**: PvP and competitive content: Tensho stops entirely, with no setting (DEC-038, 0.610) | author | coded, not tried in game |
| 4 | **Dalamud rule**: Beastmaster. The page is a list of information; the team is calculated out of combat (DEC-046). The Crucible of the Unbroken is not switched off (DEC-038, addition of 2026-11-11): Tensho helps there fully, combat included, with the team panel beside the board window; this replaces the limited exception of DEC-047. In a PvE fight Tensho only shows what the player would already know, taken from the game (DEC-055); the highlight of the combo skills is an option, off by default, display only | author | decided (DEC-038, DEC-046, DEC-055); to prove in game |
| 5 | **Dalamud rule**: windows. « If it looks like a window, use the window API. » Everything is converted (0.514, 0.516) except the invisible click zones of the Fashion Report; to check in game (#38) | author | to check in game |
| 6 | **Dalamud rule**: AI disclosure and pull request text written by the author, who must be able to explain the code. Size on 2026-11-09: 608 C# files, 71,194 lines in the plugin | author | **closed** (DEC-050): AI policy no longer followed; the AI disclosure stays in the manifest and the README; the pull request draft left with publish/d17 in 0.1090 (git history) |
| 7 | AI-generated icon, declared. The AI policy preferred a hand-made icon | agent | **closed** (DEC-050): the current icon is kept, still declared |
| 8 | Screenshots for the installer: `images/image1.png` to `image5.png`, 730×380 at most. Optional, none exist; `Verify.ps1 -Release` lists them in `repo.json` when they exist (0.1020) | agent, in game with the author | open |
| 9 | Move to 1.0.0 (author's decision; the release needs `-Major`), Verify, public commit, release `Stable-1.0.0` | agent, on request | at the end |
| 10 | Commit hash and `changelog` in `manifest.toml`, fork of goatcorp/DalamudPluginsD17, branch, `testing/live/TenshoAssistant/` folder, pull request | author | not to do since DEC-039 (publish/d17 draft removed in 0.1090 (git history; regenerate it from the template below if DEC-039 changes)) |

## State of the checklist

| Requirement | State | Where |
|---|---|---|
| Build on their side: Linux, SDK 10.0.101, network off, lock file | **`global.json` fixed in 0.512** (it required 10.0.403, their image would not have built). A CI job uses their image and their command, green since 515a28b; it keeps the network | `global.json`, `.github/workflows/build.yml`, [record](../evidence/0.512-d17-build.md) |
| `packages.lock.json` tracked by git | done | `src/TenshoAssistant/packages.lock.json` |
| NuGet dependencies reviewed by the team | `Raphael.Net` 5.0.0 (MIT, native library), the same version as Craftimizer, already published; `Microsoft.Data.Sqlite` 10.0.14 (MIT) and `SQLitePCLRaw.bundle_e_sqlite3` 2.1.14 (Apache-2.0, native library) for the local data files | `TenshoAssistant.csproj` |
| JSON manifest: Name, Author, Punchline, Description | done; Description rewritten in 0.512: lines cut by hand as the documentation asks, summary on the first line | `src/TenshoAssistant/TenshoAssistant.json` |
| InternalName, AssemblyVersion, DalamudApiLevel left to DalamudPackager | done; checked by `Verify.ps1` | same |
| Deterministic version | done: only `<Version>` of the csproj | `TenshoAssistant.csproj` |
| Normal windows through the Dalamud window system | done in code (0.514, 0.516): main window, map, guide, free-standing panels (`PanelWindow`), panels attached to a game window (`AttachedPanels`). Only exception: the invisible click zones of the Fashion Report, with no background or border | `PanelWindow.cs`, `AttachedPanels.cs`, `MainWindow.FashionWeek.cs` |
| Drawings over the game | `GetForegroundDrawList` for the Mini Cactpot, the Triple Triad advice and the deck reminder; `GetBackgroundDrawList` for the frames on the rows of the game's gathering window and the icons above nearby monsters (information only) | `MainWindow.TriadAdvice.cs`, `MainWindow.TriadDecks.cs`, `MainWindow.TriadRequest.cs`, `MainWindow.MiniCactpot.cs`, `MainWindow.GatheringPanel.cs`, `MainWindow.MonsterIcons.cs` |
| No automatic request to the game server | **One exception kept and declared** (point 2 above, DEC-035). Everything else starts on a click: teleport, applying the sort, macro chain, writing the macros, Try On, equipping a gearset, the hunt train call, game alarms. `/alarm`: one command every half second, never above 30 game alarms | `Collections/TitleChanger.Dalamud.cs`, `Alarms/AlarmService.Dalamud.cs`, `MainWindow.Alarms.cs` |
| No automation (crafting, dialogues, loot…) | compliant: simulator, solver and suggested move without execution; the crafting recorder only reads the actions the player uses | `Craft/CraftRecorder.Dalamud.cs` |
| Combat elements informative only, what the player would already know | parser: none, no boss timer, no mechanic prediction (DEC-009, DEC-055); in a PvE fight, only what the game itself tells (its texts and sheets). Beastmaster page: game data (DEC-032), team calculated out of combat (DEC-046); the highlight of the beastmaster's combo skills on the hotbars (option, off by default) works everywhere in PvE, display only, nothing sent to the game (the user's choice of 20/11). Nearby monsters: name, place and level only, nothing about the fight (`Maps/MapLiveReader.Dalamud.cs`) | `MainWindow.Beastmaster.cs`, `MainWindow.NearbyMonsters.cs` |
| No advantage in competitive content | the whole plugin stops in PvP and ranked content (DEC-038); Triple Triad advice against NPCs only, never in tournaments or against a player (DEC-033) | `Competitive/`, `MainWindow.TriadAdvice.cs` |
| Account ids: only one's own | compliant: only `player.ContentId` (local character) is used as a key; the offers read at the market board keep no seller name or id, only whether an offer is one of the player's retainers'; the Universalis offers carry their retainer's name, kept in memory only (shown, and used to recognise the player's own offers), never in the copy kept on disk (since 0.1840, X-836) | `*.Dalamud.cs` readers, `Market/SeenPriceReader.Dalamud.cs` |
| No personal data sent, HTTPS, host names | compliant. Universalis, Garland Tools, wiki, Teamcraft (one GraphQL query per fishing spot), PaissaDB, GitHub (LuminaSupplemental, GatherBuddy, FFXIVGachaSpreadsheet, gallery and videos of this repository); the eight data sources each have their switch and « Game data and what you observed only » turns them off at once; the gallery and the videos are read on a click only. Exceptions chosen by the player: Discord webhook off by default, Discord hosts only, character name only as an option, address never logged; shared discoveries exchange through GitHub, sending only after the player connects GitHub, own sightings with the day only, to a secret gist and one issue named by the GitHub login, token encrypted on the PC (DPAPI) | [SECURITY](../../SECURITY.md) |
| Downloading and starting a program | **One exception, declared** (DEC-056): on the player's clicks only (Settings › Advanced › Desktop application), Tensho downloads the desktop application from this repository's GitHub release, checks its SHA-256 against the one published in the same release (integrity, not authenticity), unpacks it in the player's folder and opens it; never mandatory, never started without a click. Kept because Tensho is on its own repository (DEC-039); a build with `-p:TenshoDesktopInstall=false` leaves only a link to the release page | `src/TenshoAssistant/Link/DesktopInstaller.cs` |
| Report a problem | the exact text shown first; then, on the player's click only, a public issue sent through the player's own GitHub connection (the discoveries' one, if connected), else the prefilled GitHub form opened in the browser and sent by the player (author's agreement of 2026-11-14) | `Help/ProblemReport.cs`, `Discoveries/GitHubExchange.cs` |
| No telemetry | compliant: none | — |
| Lumina rather than XIVAPI | compliant: game sheets; XIVAPI is not called by the plugin | [evidence](../evidence/0.451-xivapi-v2.md) |
| Icon `images/icon.png` 1:1, 64–512 px | done: 512×512, generated by the agent, declared in the description | `src/TenshoAssistant/images/icon.png` |
| `manifest.toml` in `testing/live/TenshoAssistant/` | not needed since DEC-039 | template below (publish/d17 draft removed in 0.1090, git history) |
| AI disclosure in the pull request | to be written by the author if a submission ever happens | — (the 0.971 draft left with publish/d17 in 0.1090, git history) |
| AI-assisted translations reviewed by speakers | German and Japanese: about thirty keys, not reviewed, declared; English and French complete | `src/TenshoAssistant/Localization/Languages/` |
| Embedded data under a compatible licence, notice kept | done: SubmarineTracker and FFXIVGachaSpreadsheet (MIT, full text in the file), LuminaSupplemental airship and festival tables (GPL-3.0), Character Panel Refined (MIT, in the code), Lotlab FFXIV Gardening Tracker (GPL-3.0), nick75g crossbreeds (MIT); credits in the README and the Credits page | `FreeCompany/Data/`, `Events/Data/`, `Reminders/Data/`, `Character/StatFormulas.cs`, `Credits/CreditCatalog.cs` |

## Dalamud rules, one by one

Text of origin: dalamud.dev, plugin-publishing/restrictions, read on 2026-11-15 (22 rules; the guidance that follows them is not a rule).
Tensho is published on its own repository (DEC-039) and keeps these rules all the same. One row per rule: how Tensho keeps it, and its proof
(decision, file, test). `DalamudRules1392Tests` checks that every rule has its row and its proof in both halves, that the files named exist,
and that the code keeps what the rows say: the functions that act on the game stay in the files named here, the ones said absent stay absent.
A change that touches a rule changes its row in the same version.

| # | Rule | How Tensho keeps it | Proof |
|---|---|---|---|
| 1 | No automatic request to the server; nothing the normal game could not send | What reaches the game starts from the player's click, as a normal game command or through the game's own function: game alarms (`/alarm`), the hunt announcement typed into the chat box, the teleport to an aetheryte, equipping a gearset, Try On, writing the crafting macros into the game's macro list, the macro chain (rule 12) and applying the sort to an open inventory. One exception, declared and off by default: the title by job | DEC-035; `src/TenshoAssistant/Collections/TitleChanger.Dalamud.cs`, `src/TenshoAssistant/Alarms/AlarmService.Dalamud.cs`, `src/TenshoAssistant/Maps/HuntAnnouncer.Dalamud.cs`, `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs`, `src/TenshoAssistant/Gear/GameGearsetReader.Dalamud.cs`, `src/TenshoAssistant/Items/TryOn.Dalamud.cs`, `src/TenshoAssistant/Macros/MacroWriter.Dalamud.cs`, `src/TenshoAssistant/Macros/MacroChainBridge.Dalamud.cs`, `src/TenshoAssistant/Inventory/InventoryVisualSortBridge.Dalamud.cs`; `DalamudRules1392Tests`, `DalamudRulesActs1399Tests` |
| 2 | Combat not augmented, altered or interfered with | In a PvE fight Tensho only shows what the player would already know, taken from the game (its texts and sheets: weaknesses, actions and their effects, preparation reminders): no mechanic prediction, no boss timer, no measure of the damage, no action sent in the player's place. Everything is off in PvP and ranked content (DEC-038) | DEC-055 (replaces DEC-009 in part), DEC-032, DEC-046, DEC-047; `DalamudRulesCombat1575Tests` (nothing a parser or a boss timer needs) |
| 3 | No harm to Square Enix's revenue | Nothing is bought, unlocked or given: shop and Mog Station items are shown as information | `DalamudRules1392Tests` (no purchase function of the game called) |
| 4 | No parsing, raid logging or DPS meter | None: no fight is logged or measured; ACT and IINACT are out of scope | DEC-009, DEC-055; `DalamudRules1392Tests` (no FFLogs), `DalamudRulesCombat1575Tests` (no combat log, no action effect read) |
| 5 | No account id of other players | Only the logged-in character's local id (`player.ContentId`) is a key; at the market board, each offer's retainer id is read only to recognise the player's own offers, never kept (only « one of mine » is). In the chat window, the tooltip of a meal's or a medicine's status gained by another player in range (« X gains the effect of Well Fed ») reads that player's status when the line arrives and for half a second at most after it (D-208, the owner's choice of 21/11/2026 at about 8:15 pm: a meal put on later shows no bonus rather than an older meal), with its remaining time at the line (D-211, the owner's choice of 21/11/2026 at about 10:30 pm: a Well Fed status at its meal's full duration, 1800 s by the game's data, was put on just before the line and is the line's; the time is compared, never kept; a status at its full duration that is the meal shown for that player's line of the 4 seconds before is that older meal: no bonus, X-768), as the game shows it when you target them: the meal's number only, kept in memory with the line, never on disk, never an id (the owner's choice of 21/11/2026 at about 7:10 pm, D-207, asked on this very behaviour; it corrects D-205, which said « read on hover only ») | `src/TenshoAssistant/Market/SeenPriceReader.Dalamud.cs`; `src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs` |
| 6 | No hard dependency on a plugin that breaks the rules | No plugin dependency, no IPC with another plugin; three NuGet packages (Raphael.Net, MIT; Microsoft.Data.Sqlite, MIT; SQLitePCLRaw.bundle_e_sqlite3, Apache-2.0) | `src/TenshoAssistant/TenshoAssistant.csproj`; `DalamudRules1392Tests` (no IPC subscriber) |
| 7 | Not useful only for out-of-spec behaviour | Information first; a game action only on the player's click (each one named in rule 1) | [README](../../README.md); rules 1 and 12; `DalamudRulesActs1399Tests` |
| 8 | No advantage in competitive or PvP content | The whole plugin stops in PvP and ranked content, with no setting, and restarts by itself | DEC-038; `src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs`; `CompetitiveCut610Tests` |
| 9 | No emote or expression looping | None: Tensho sends no emote of its own and loops nothing (the chat box is used only for the alarms and the hunt announcement); a macro of the player's run by the macro chain (rule 12) may hold one, as the player wrote it, once per click | `DalamudRules1392Tests` |
| 10 | No cutscene skipping | None: no game window is answered or closed in the player's place | `DalamudRules1392Tests` (no window callback) |
| 11 | No automatic skipping of dialogue boxes | None: no Talk or Yes/No window is read to answer it | `DalamudRules1392Tests` (no Talk, no SelectYesno) |
| 12 | No automated crafting | Simulator, solver and suggested move, never executed; the rotation recorder only watches the player's own actions (the game's function called first, its result returned unchanged). Crafting macros are written into the game's macro list on the player's click, never run by that. The macro chain, started by the player's click, runs the player's macros one after the other through the game's own macro function, as the player wrote them (each read again and refused if changed; stopped at a locked macro or an interruption, and cancelled when PvP or ranked content starts: it never starts again by itself) | `src/TenshoAssistant/Craft/CraftRecorder.Dalamud.cs`, `src/TenshoAssistant/Macros/MacroWriter.Dalamud.cs`, `src/TenshoAssistant/Macros/MacroChainBridge.Dalamud.cs`; `DalamudRules1392Tests`, `DalamudRulesActs1399Tests` |
| 13 | No automatic loot rolls | None: no loot roll is sent | `DalamudRules1392Tests` (no roll function) |
| 14 | No friend list login or logout alerts | None: the friend list is not read | `DalamudRules1392Tests` (no friend list) |
| 15 | No markers for untelegraphed area attacks | None | DEC-009 |
| 16 | No camera zoom beyond the game's bounds | None: the camera is not touched | `DalamudRules1392Tests` (no camera manager) |
| 17 | No FFLogs integration | None | `DalamudRules1392Tests` (no FFLogs) |
| 18 | No damage parser | None | DEC-009 |
| 19 | No way around the Mog Station (Fantasia) | None | rule 3 |
| 20 | No new combo plugin | None: no action is replaced or combined | rule 12; `DalamudRules1392Tests` (UseAction only watched by the recorder) |
| 21 | No recolouring of area attacks | None | DEC-009 |
| 22 | Nothing in PvP that gives an advantage | The whole plugin stops in PvP (rule 8) | DEC-038; `CompetitiveCut610Tests` |

## `manifest.toml` template (DalamudPluginsD17 repository, `testing/live/TenshoAssistant/manifest.toml`)

```toml
[plugin]
repository = "https://github.com/Shigu330/Tensho-Assistant.git"
commit = "<hash of the public commit to build>"
owners = ["Shigu330"]
project_path = "src/TenshoAssistant"
changelog = "<summary of the version>"
```

Beside it: `images/icon.png` (required), `images/image1.png` to `image5.png` (optional). One pull request per plugin, one branch per
submission; each update = a new pull request that changes at least `commit`. Rebuild a pull request: comment `bleatbot, rebuild`. Moving
from testing to stable: move the folder, without changing the version. Approval: four votes for, one veto is enough, the queue can take
more than a week.

## Decisions already taken

2026-10-31: display name « Tensho Assistant »; AI level « Auto »; icon generated and declared; official repository. 2026-11-02 (DEC-023):
submission at version 1.0.0; German and Japanese kept in the package, partial, falling back to English. 2026-11-04 (DEC-033): frame of the
game windows removed (it was planned before D17); game advice in PvE only. 2026-11-05 (DEC-039): no submission, the custom repository
only, the rules kept. 2026-11-09: the Crucible of the Unbroken team panel as a limited exception (DEC-047); releases by the agent, AI policy no
longer followed (AI disclosure kept), EUPL-1.2 confirmed, current icon kept (DEC-050). Quest icons 71141 / 71151: the setting says « variant n » as long as no in-game record gives their meaning; nothing
blocking.

---

# Publication sur le dépôt officiel Dalamud (D17)

**Depuis DEC-039 (2026-11-05) : pas de soumission.** Tensho reste sur le dépôt personnalisé. Ce document sert de liste des règles
Dalamud, qui restent appliquées ; les étapes de soumission ne sont plus à faire.

Règles relues le 2026-11-04 : [soumission](https://dalamud.dev/plugin-publishing/submission),
[approbation](https://dalamud.dev/plugin-publishing/approval-process),
[restrictions](https://dalamud.dev/plugin-publishing/restrictions), [politique IA](https://dalamud.dev/plugin-publishing/ai-policy) (plus suivie depuis DEC-050),
[métadonnées](https://dalamud.dev/plugin-development/plugin-metadata),
[considérations techniques](https://dalamud.dev/plugin-development/technical-considerations), README de
[DalamudPluginsD17](https://github.com/goatcorp/DalamudPluginsD17). Compilation chez eux : [relevé](../evidence/0.512-d17-build.md).
Liste relue contre le code le 2026-11-09.

Le dépôt personnalisé : `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json`, mis à jour par
`Verify.ps1 -Release`, lancé par l'agent (DEC-050) ; la dernière release est sur la page des releases GitHub. La version 1.0.0 marque la fin du plan global sur ce dépôt
personnalisé ; c'est l'auteur qui la décide (DEC-039).

Réponse reçue sur le Discord de Dalamud, 2026-11-05 (hOst1k, question posée par l'auteur) : pas de limite de taille si le plugin respecte
les règles ; plus il est gros, plus le premier examen est long ; mieux vaut ne pas viser trop complexe dès le départ, Simple Tweaks et
« VanillaTweaks » (sans doute VanillaPlus) ayant grandi avec le temps. Rôle exact de cette personne dans l'équipe : non vérifié. À cette
date : 57 419 lignes de C# dans le plugin, 123 519 lignes suivies par git. La politique IA s'appliquait aussi : une soumission « Auto » trop grosse
pour que l'auteur en réponde peut être fermée (plus suivie depuis DEC-050).

Suite du même échange, 2026-11-05 : après avoir appris les 57 000 lignes écrites avec un agent, goat répond que cette quantité de code
n'est pas examinable, new Era ajoute que beaucoup de choses existent déjà dans d'autres plugins, et MidoriKami demande à l'équipe
d'approbation (`@dalamud-pac`) s'il existe un repère de taille pour du code écrit par IA. **La soumission d'un seul bloc au dépôt
officiel paraissait donc compromise** ; l'auteur a ensuite décidé de ne pas soumettre (DEC-039). Le dépôt personnalisé marche déjà et
n'attend pas cet examen. État à cette date : 1 120 tests, dont beaucoup lisent le code source sans l'exécuter ; 415 cases d'essai en jeu
ouvertes contre 156 cochées ; premier commit le 2026-10-31.

## Anciennes étapes de soumission (plus à faire depuis DEC-039)

Gardées parce que plusieurs sont des règles Dalamud que le code suit toujours. Les points marqués **règle Dalamud** demandent un résumé
et l'accord de l'auteur (consigne du 2026-11-04), sauf la politique IA, plus suivie (DEC-050).

| # | Point | Qui | État |
|---|---|---|---|
| 1 | Jouer sur la dernière version : Dalamud demande que l'auteur ait testé. Cases ouvertes dans [TESTING](TESTING.md) ; les essais en jeu sont la phase 6 du [plan global](GLOBAL-PLAN.md), par petits lots à la fin de chaque bloc de travail, groupés par lieu | auteur + agent | en cours (phase 6) |
| 2 | **Règle Dalamud** : titre selon le job, seule action vers le jeu sans clic (réglage désactivé par défaut) | auteur | **décidé** (DEC-035) : gardé tel quel |
| 3 | Alarmes du jeu : rien à changer. Les commandes `/alarm` partent toujours d'un clic du joueur : ajouter une alarme quand le réglage est actif, ou « Envoyer » ; les alarmes automatiques des servants et des vaisseaux n'en créent jamais | — | fait |
| 3 bis | **Règle Dalamud** : JcJ et contenu compétitif : Tensho se coupe en entier, sans réglage (DEC-038, 0.610) | auteur | codé, pas essayé en jeu |
| 4 | **Règle Dalamud** : dresseur. La page est une liste d'informations ; l'équipe est calculée hors combat (DEC-046). Le Dédale bestial n'est pas coupé (DEC-038, ajout du 2026-11-11) : Tensho y aide entièrement, combat compris, avec le panneau d'équipe à côté de la fenêtre du plateau ; cela remplace l'exception limitée de DEC-047. En combat JcE, Tensho montre seulement ce que le joueur saurait déjà, tiré du jeu (DEC-055) ; le surlignage des techniques de combo est une option, coupée par défaut, affichage seulement | auteur | décidé (DEC-038, DEC-046, DEC-055) ; à prouver en jeu |
| 5 | **Règle Dalamud** : fenêtres. « Si ça ressemble à une fenêtre, utiliser l'API de fenêtres. » Tout est converti (0.514, 0.516) sauf les zones de clic invisibles de la Revue de mode ; à vérifier en jeu (#38) | auteur | à vérifier en jeu |
| 6 | **Règle Dalamud** : déclaration d'IA et texte de la PR écrits par l'auteur, qui doit pouvoir expliquer le code. Taille au 2026-11-09 : 608 fichiers C#, 71 194 lignes dans le plugin | auteur | **fermé** (DEC-050) : politique IA plus suivie ; la déclaration d'IA reste dans le manifeste et le README ; le brouillon de PR est parti avec publish/d17 en 0.1090 (historique git) |
| 7 | Icône générée par IA, déclarée. La politique IA préférait une icône faite main | agent | **fermé** (DEC-050) : l'icône actuelle est gardée, toujours déclarée |
| 8 | Captures pour l'installeur : `images/image1.png` à `image5.png`, 730×380 au plus. Facultatif, aucune n'existe ; `Verify.ps1 -Release` les inscrit dans `repo.json` quand elles existent (0.1020) | agent, en jeu avec l'auteur | ouvert |
| 9 | Passer à 1.0.0 (décision de l'auteur ; la release demande `-Major`), Verify, commit public, release `Stable-1.0.0` | agent, sur demande | à la fin |
| 10 | Hash du commit et `changelog` dans `manifest.toml`, fork de goatcorp/DalamudPluginsD17, branche, dossier `testing/live/TenshoAssistant/`, PR | auteur | plus à faire depuis DEC-039 (brouillon publish/d17 retiré en 0.1090 (historique git ; le refaire depuis le modèle ci-dessous si DEC-039 change)) |

## État de la liste de contrôle

| Exigence | État | Où |
|---|---|---|
| Compilation chez eux : Linux, SDK 10.0.101, réseau coupé, fichier de verrouillage | **`global.json` corrigé en 0.512** (il exigeait 10.0.403, leur image n'aurait pas compilé). Un job de CI reprend leur image et leur commande, vert depuis 515a28b ; il garde le réseau | `global.json`, `.github/workflows/build.yml`, [relevé](../evidence/0.512-d17-build.md) |
| Fichier `packages.lock.json` suivi par git | fait | `src/TenshoAssistant/packages.lock.json` |
| Dépendances NuGet examinées par l'équipe | `Raphael.Net` 5.0.0 (MIT, bibliothèque native), même version que Craftimizer, déjà publié ; `Microsoft.Data.Sqlite` 10.0.14 (MIT) et `SQLitePCLRaw.bundle_e_sqlite3` 2.1.14 (Apache-2.0, bibliothèque native) pour les fichiers de données locaux | `TenshoAssistant.csproj` |
| Manifeste JSON : Name, Author, Punchline, Description | fait ; Description réécrite en 0.512 : lignes coupées à la main comme le demande la documentation, résumé en première ligne | `src/TenshoAssistant/TenshoAssistant.json` |
| InternalName, AssemblyVersion, DalamudApiLevel laissés à DalamudPackager | fait ; contrôlé par `Verify.ps1` | idem |
| Version déterministe | fait : seulement `<Version>` du csproj | `TenshoAssistant.csproj` |
| Fenêtres normales via le système de fenêtres Dalamud | fait en code (0.514, 0.516) : principale, carte, guide, panneaux libres (`PanelWindow`), panneaux accrochés à une fenêtre du jeu (`AttachedPanels`). Seule exception : les zones de clic invisibles de la Revue de mode, sans fond ni bord | `PanelWindow.cs`, `AttachedPanels.cs`, `MainWindow.FashionWeek.cs` |
| Dessins par-dessus le jeu | `GetForegroundDrawList` pour le Mini Cactpot, le conseil et le rappel de deck de Triple Triade ; `GetBackgroundDrawList` pour les cadres sur les lignes de la fenêtre de récolte du jeu et les icônes au-dessus des monstres proches (information seulement) | `MainWindow.TriadAdvice.cs`, `MainWindow.TriadDecks.cs`, `MainWindow.TriadRequest.cs`, `MainWindow.MiniCactpot.cs`, `MainWindow.GatheringPanel.cs`, `MainWindow.MonsterIcons.cs` |
| Pas de requête automatique au serveur du jeu | **Une exception gardée et déclarée** (point 2 ci-dessus, DEC-035). Tout le reste part sur un clic : téléportation, application du tri, chaîne de macros, écriture des macros, cabine d'essayage, équiper une tenue, appel du train de chasse, alarmes du jeu. `/alarm` : une commande toutes les demi-secondes, jamais plus de 30 alarmes du jeu | `Collections/TitleChanger.Dalamud.cs`, `Alarms/AlarmService.Dalamud.cs`, `MainWindow.Alarms.cs` |
| Pas d'automatisation (artisanat, dialogues, butin…) | conforme : simulateur, solveur et coup conseillé sans exécution ; l'enregistreur d'artisanat ne fait que lire les actions qu'utilise le joueur | `Craft/CraftRecorder.Dalamud.cs` |
| Éléments de combat seulement informatifs, ce que le joueur saurait déjà | parser : aucun, ni minuteur de boss, ni prédiction de mécanique (DEC-009, DEC-055) ; en combat JcE, seulement ce que le jeu dit lui-même (ses textes et ses feuilles). Page dresseur : données du jeu (DEC-032), équipe calculée hors combat (DEC-046) ; le surlignage des techniques de combo du dresseur sur les barres d'actions (option, coupée par défaut) marche partout en JcE, affichage seulement, rien n'est envoyé au jeu (choix de l'utilisateur du 20/11). Monstres proches : nom, lieu et niveau seulement, rien sur le combat (`Maps/MapLiveReader.Dalamud.cs`) | `MainWindow.Beastmaster.cs`, `MainWindow.NearbyMonsters.cs` |
| Aucun avantage en contenu compétitif | le plugin entier s'arrête en JcJ et en contenu classé (DEC-038) ; conseil de Triple Triade contre les PNJ seulement, jamais en tournoi ni contre un joueur (DEC-033) | `Competitive/`, `MainWindow.TriadAdvice.cs` |
| Identifiants de compte : seulement le sien | conforme : seul `player.ContentId` (personnage local) sert de clé ; les offres lues au tableau des ventes ne gardent ni nom ni identifiant de vendeur, seulement si une offre est celle d'un servant du joueur ; les offres d'Universalis portent le nom de leur servant, gardé en mémoire seulement (affiché, et utilisé pour reconnaître les offres du joueur), jamais dans la copie gardée sur le disque (depuis 0.1840, X-836) | lecteurs `*.Dalamud.cs`, `Market/SeenPriceReader.Dalamud.cs` |
| Aucune donnée personnelle envoyée, HTTPS, nom d'hôte | conforme. Universalis, Garland Tools, wiki, Teamcraft (une requête GraphQL par lieu de pêche), PaissaDB, GitHub (LuminaSupplemental, GatherBuddy, FFXIVGachaSpreadsheet, galerie et vidéos de ce dépôt) ; les huit sources de données ont chacune leur interrupteur et « Données du jeu et vos observations seulement » les coupe d'un coup ; la galerie et les vidéos ne sont lues que sur un clic. Exceptions choisies par le joueur : webhook Discord désactivé par défaut, hôtes de Discord seulement, nom du personnage seulement sur option, adresse jamais journalisée ; échange des découvertes partagées par GitHub, sans aucun envoi avant la connexion du joueur à GitHub, ses propres observations avec le jour seulement, vers un gist secret et un ticket au nom de son identifiant GitHub, jeton chiffré sur le PC (DPAPI) | [SECURITY](../../SECURITY.md) |
| Télécharger et lancer un programme | **Une exception, déclarée** (DEC-056) : sur les clics du joueur seulement (Paramètres › Avancé › Application de bureau), Tensho télécharge l'application de bureau depuis la release GitHub de ce dépôt, vérifie son SHA-256 contre celui publié dans la même release (intégrité, pas authenticité), la décompresse dans le dossier du joueur et l'ouvre ; jamais obligatoire, jamais lancée sans un clic. Gardée parce que Tensho est sur son propre dépôt (DEC-039) ; une compilation avec `-p:TenshoDesktopInstall=false` ne laisse qu'un lien vers la page de la release | `src/TenshoAssistant/Link/DesktopInstaller.cs` |
| Signaler un problème | le texte exact montré d'abord ; puis, sur le clic du joueur seulement, un ticket public envoyé par sa propre connexion GitHub (celle des découvertes, s'il est connecté), sinon le formulaire GitHub prérempli ouvert dans le navigateur et envoyé par le joueur (accord de l'auteur du 2026-11-14) | `Help/ProblemReport.cs`, `Discoveries/GitHubExchange.cs` |
| Pas de télémétrie | conforme : aucune | — |
| Lumina plutôt que XIVAPI | conforme : feuilles du jeu ; XIVAPI n'est pas appelé par le plugin | [preuve](../evidence/0.451-xivapi-v2.md) |
| Icône `images/icon.png` 1:1, 64–512 px | fait : 512×512, générée par l'agent, déclarée dans la description | `src/TenshoAssistant/images/icon.png` |
| `manifest.toml` dans `testing/live/TenshoAssistant/` | plus nécessaire depuis DEC-039 | modèle ci-dessous (brouillon publish/d17 retiré en 0.1090, historique git) |
| Déclaration d'IA dans la PR | à écrire par l'auteur si une soumission a lieu un jour | — (le brouillon de 0.971 est parti avec publish/d17 en 0.1090, historique git) |
| Traductions assistées par IA relues par des locuteurs | allemand et japonais : une trentaine de clés, non relues, déclarées ; anglais et français complets | `src/TenshoAssistant/Localization/Languages/` |
| Données embarquées sous licence compatible, avis conservé | fait : SubmarineTracker et FFXIVGachaSpreadsheet (MIT, texte complet dans le fichier), tables des dirigeables et des fêtes de LuminaSupplemental (GPL-3.0), Character Panel Refined (MIT, dans le code), FFXIV Gardening Tracker de Lotlab (GPL-3.0), croisements de nick75g (MIT) ; crédits dans le README et la page Crédits | `FreeCompany/Data/`, `Events/Data/`, `Reminders/Data/`, `Character/StatFormulas.cs`, `Credits/CreditCatalog.cs` |

## Règles de Dalamud, une par une

Texte d'origine : dalamud.dev, plugin-publishing/restrictions, lu le 2026-11-15 (22 règles ; les conseils qui suivent ne sont pas des
règles). Tensho est publié sur son propre dépôt (DEC-039) et garde ces règles quand même. Une ligne par règle : comment Tensho la respecte,
et sa preuve (décision, fichier, test). `DalamudRules1392Tests` vérifie que chaque règle a sa ligne et sa preuve dans les deux moitiés, que
les fichiers nommés existent, et que le code tient ce que disent les lignes : les fonctions qui agissent sur le jeu restent dans les fichiers
nommés ici, celles dites absentes restent absentes. Un changement qui touche une règle change sa ligne dans la même version.

| # | Règle | Comment Tensho la respecte | Preuve |
|---|---|---|---|
| 1 | Aucune requête automatique au serveur ; rien que le jeu normal ne pourrait envoyer | Ce qui part vers le jeu part d'un clic du joueur, comme une commande normale du jeu ou par la fonction du jeu lui-même : alarmes du jeu (`/alarm`), annonce de chasse tapée dans la boîte de discussion, téléportation vers un éthérite, équiper une tenue, cabine d'essayage, écriture des macros d'artisanat dans la liste des macros du jeu, chaîne de macros (règle 12) et application du tri à un inventaire ouvert. Une exception, déclarée et coupée par défaut : le titre selon le job | DEC-035 ; `src/TenshoAssistant/Collections/TitleChanger.Dalamud.cs`, `src/TenshoAssistant/Alarms/AlarmService.Dalamud.cs`, `src/TenshoAssistant/Maps/HuntAnnouncer.Dalamud.cs`, `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs`, `src/TenshoAssistant/Gear/GameGearsetReader.Dalamud.cs`, `src/TenshoAssistant/Items/TryOn.Dalamud.cs`, `src/TenshoAssistant/Macros/MacroWriter.Dalamud.cs`, `src/TenshoAssistant/Macros/MacroChainBridge.Dalamud.cs`, `src/TenshoAssistant/Inventory/InventoryVisualSortBridge.Dalamud.cs` ; `DalamudRules1392Tests`, `DalamudRulesActs1399Tests` |
| 2 | Le combat n'est ni augmenté, ni modifié, ni gêné | En combat JcE, Tensho montre seulement ce que le joueur saurait déjà, tiré du jeu (ses textes et ses feuilles : faiblesses, actions et leurs effets, rappels de préparation) : ni prédiction de mécanique, ni minuteur de boss, ni mesure des dégâts, ni action envoyée à la place du joueur. Tout est coupé en JcJ et en contenu classé (DEC-038) | DEC-055 (remplace en partie DEC-009), DEC-032, DEC-046, DEC-047 ; `DalamudRulesCombat1575Tests` (rien de ce qu'un parser ou un minuteur de boss demande) |
| 3 | Aucun tort aux revenus de Square Enix | Rien n'est acheté, débloqué ni donné : les objets des boutiques et de la Mog Station sont montrés en information | `DalamudRules1392Tests` (aucune fonction d'achat du jeu appelée) |
| 4 | Pas d'analyse de combat, de journal de raid ni de compteur de dégâts | Aucun : aucun combat n'est enregistré ni mesuré ; ACT et IINACT hors périmètre | DEC-009, DEC-055 ; `DalamudRules1392Tests` (pas de FFLogs), `DalamudRulesCombat1575Tests` (ni journal de combat, ni effet d'action lu) |
| 5 | Aucun identifiant de compte des autres joueurs | Seul l'identifiant local du personnage connecté (`player.ContentId`) sert de clé ; au marché, l'identifiant du servant de chaque offre est lu seulement pour reconnaître les offres du joueur, jamais gardé (seul « une des miennes » l'est). Dans la fenêtre de chat, l'info-bulle d'un statut de repas ou de remède gagné par un autre joueur à portée (« X bénéficie de l'effet Repu ») lit le statut de ce joueur à l'arrivée de la ligne et une demi-seconde au plus après elle (D-208, choix du propriétaire du 21/11/2026 vers 20 h 15 : un repas posé plus tard ne montre aucun bonus plutôt qu'un ancien repas), avec son temps restant à la ligne (D-211, choix du propriétaire du 21/11/2026 vers 22 h 30 : un statut Repu à la durée pleine de son repas, 1800 s d'après les données du jeu, a été posé juste avant la ligne et c'est celui de la ligne ; le temps est comparé, jamais gardé ; un statut à sa durée pleine qui est le repas montré pour la ligne de ce joueur des 4 secondes d'avant est cet ancien repas : aucun bonus, X-768), comme le jeu le montre quand on le cible : le seul numéro du repas, gardé en mémoire avec la ligne, jamais sur le disque, jamais un identifiant (choix du propriétaire du 21/11/2026 vers 19 h 10, D-207, demandé sur ce comportement même ; il corrige D-205, qui disait « lu au survol seulement ») | `src/TenshoAssistant/Market/SeenPriceReader.Dalamud.cs` ; `src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs` |
| 6 | Aucune dépendance obligatoire à un plugin qui enfreint les règles | Aucune dépendance de plugin, aucun IPC avec un autre plugin ; trois paquets NuGet (Raphael.Net, MIT ; Microsoft.Data.Sqlite, MIT ; SQLitePCLRaw.bundle_e_sqlite3, Apache-2.0) | `src/TenshoAssistant/TenshoAssistant.csproj` ; `DalamudRules1392Tests` (aucun abonné IPC) |
| 7 | Pas utile seulement pour un usage hors des règles | L'information d'abord ; une action dans le jeu seulement sur le clic du joueur (chacune nommée à la règle 1) | [README](../../README.md) ; règles 1 et 12 ; `DalamudRulesActs1399Tests` |
| 8 | Aucun avantage en contenu compétitif ou JcJ | Tout le plugin se coupe en JcJ et en contenu classé, sans réglage, et repart seul | DEC-038 ; `src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs` ; `CompetitiveCut610Tests` |
| 9 | Pas d'emote ni d'expression en boucle | Aucune : Tensho n'envoie aucune emote de lui-même et ne répète rien en boucle (la boîte de discussion ne sert qu'aux alarmes et à l'annonce de chasse) ; une macro du joueur lancée par la chaîne de macros (règle 12) peut en contenir une, telle qu'il l'a écrite, une fois par clic | `DalamudRules1392Tests` |
| 10 | Pas de cinématique passée | Aucune : aucune fenêtre du jeu n'est répondue ni fermée à la place du joueur | `DalamudRules1392Tests` (aucun rappel de fenêtre) |
| 11 | Pas de dialogue passé automatiquement | Aucun : aucune fenêtre Talk ni Oui/Non n'est lue pour y répondre | `DalamudRules1392Tests` (ni Talk, ni SelectYesno) |
| 12 | Pas d'artisanat automatisé | Simulateur, solveur et coup conseillé, jamais exécutés ; l'enregistreur de rotation ne fait que regarder les actions du joueur (la fonction du jeu appelée d'abord, son résultat rendu tel quel). Les macros d'artisanat sont écrites dans la liste des macros du jeu sur le clic du joueur, jamais lancées par là. La chaîne de macros, lancée par le clic du joueur, exécute ses macros l'une après l'autre par la fonction de macro du jeu, telles qu'il les a écrites (chacune relue et refusée si elle a changé ; arrêt sur une macro verrouillée ou une interruption, et annulation quand un contenu JcJ ou classé commence : elle ne repart jamais d'elle-même) | `src/TenshoAssistant/Craft/CraftRecorder.Dalamud.cs`, `src/TenshoAssistant/Macros/MacroWriter.Dalamud.cs`, `src/TenshoAssistant/Macros/MacroChainBridge.Dalamud.cs` ; `DalamudRules1392Tests`, `DalamudRulesActs1399Tests` |
| 13 | Pas de jet de butin automatique | Aucun : aucun jet n'est envoyé | `DalamudRules1392Tests` (aucune fonction de jet) |
| 14 | Pas d'alerte de connexion ou de déconnexion des amis | Aucune : la liste d'amis n'est pas lue | `DalamudRules1392Tests` (pas de liste d'amis) |
| 15 | Pas de marqueur des attaques de zone non signalées | Aucun | DEC-009 |
| 16 | Pas de zoom de caméra au-delà des bornes du jeu | Aucun : la caméra n'est pas touchée | `DalamudRules1392Tests` (pas de gestionnaire de caméra) |
| 17 | Pas d'intégration FFLogs | Aucune | `DalamudRules1392Tests` (pas de FFLogs) |
| 18 | Pas d'analyseur de dégâts | Aucun | DEC-009 |
| 19 | Pas de contournement de la Mog Station (Fantasia) | Aucun | règle 3 |
| 20 | Pas de nouveau plugin de combos | Aucun : aucune action n'est remplacée ni combinée | règle 12 ; `DalamudRules1392Tests` (UseAction seulement regardé par l'enregistreur) |
| 21 | Pas de recoloration des attaques de zone | Aucune | DEC-009 |
| 22 | Rien en JcJ qui donne un avantage | Tout le plugin se coupe en JcJ (règle 8) | DEC-038 ; `CompetitiveCut610Tests` |

## Modèle de `manifest.toml` (dépôt DalamudPluginsD17, `testing/live/TenshoAssistant/manifest.toml`)

```toml
[plugin]
repository = "https://github.com/Shigu330/Tensho-Assistant.git"
commit = "<hash du commit public à construire>"
owners = ["Shigu330"]
project_path = "src/TenshoAssistant"
changelog = "<résumé de la version>"
```

À côté : `images/icon.png` (obligatoire), `images/image1.png` à `image5.png` (facultatifs). Une PR par plugin, une branche par
soumission ; chaque mise à jour = nouvelle PR qui change au moins `commit`. Reconstruire une PR : commentaire `bleatbot, rebuild`.
Passer du test au stable : déplacer le dossier, sans changer de version. Approbation : quatre votes favorables, un veto suffit, la file
peut dépasser une semaine.

## Décisions déjà prises

2026-10-31 : nom affiché « Tensho Assistant » ; niveau d'IA « Auto » ; icône générée et déclarée ; dépôt officiel. 2026-11-02 (DEC-023) :
soumission à la version 1.0.0 ; allemand et japonais gardés dans le paquet, partiels, repli sur l'anglais. 2026-11-04 (DEC-033) : cadre
des fenêtres du jeu retiré (il était prévu avant D17) ; conseils de jeu en JcE seulement. 2026-11-05 (DEC-039) : pas de soumission, le
dépôt personnalisé seulement, les règles gardées. 2026-11-09 : panneau d'équipe du Dédale en exception limitée (DEC-047) ; releases
par l'agent, politique IA plus suivie (déclaration d'IA gardée), EUPL-1.2 confirmée, icône actuelle gardée (DEC-050). Icônes de quête 71141 / 71151 : le réglage dit « variante n » tant qu'aucun relevé en
jeu n'en donne le sens ; rien de bloquant.
