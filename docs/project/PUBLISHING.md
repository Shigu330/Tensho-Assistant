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
run by the agent (DEC-050); the latest release is on the GitHub releases page. Version 1.0.0 marks the end of the global plan on this custom repository; the user decides
it (DEC-039).

Answer received on the Dalamud Discord, 2026-11-05 (hOst1k, question asked by the author): no size limit if the plugin follows the rules;
the bigger it is, the longer the first review; better not to aim too complex from the start, Simple Tweaks and « VanillaTweaks » (probably
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
| 1 | Play on the latest version: Dalamud asks that the author has tested. Open boxes in [TESTING](TESTING.md); the guided in-game campaign is phase 6 of the [global plan](GLOBAL-PLAN.md) | author + agent | to do (phase 6) |
| 2 | **Dalamud rule**: title by job, the only action towards the game without a click (setting off by default) | author | **decided** (DEC-035): kept as it is |
| 3 | Game alarms: nothing to change. The `/alarm` commands always start from a player's click: adding an alarm while the setting is on, or « Send »; the automatic alarms of retainers and vessels never create one | — | done |
| 3 bis | **Dalamud rule**: PvP and competitive content: Tensho stops entirely, with no setting (DEC-038, 0.610) | author | coded, not tried in game |
| 4 | **Dalamud rule**: Beastmaster. The page is a list of information. The Labyrinth panel dropped by DEC-038 comes back as a limited exception (DEC-047, X-109): the team panel in front of the Beastmaster Labyrinth board, between fights only, off from the start of each fight | author | decided (DEC-047); to prove in game |
| 5 | **Dalamud rule**: windows. « If it looks like a window, use the window API. » Everything is converted (0.514, 0.516) except the invisible click zones of the Fashion Report; to check in game (#38) | author | to check in game |
| 6 | **Dalamud rule**: AI disclosure and pull request text written by the author, who must be able to explain the code. Size on 2026-11-09: 608 C# files, 71,194 lines in the plugin | author | **closed** (DEC-050): AI policy no longer followed; the AI disclosure stays in the manifest and the README; draft in `publish/d17/PULL_REQUEST.md` |
| 7 | AI-generated icon, declared. The AI policy preferred a hand-made icon | agent | **closed** (DEC-050): the current icon is kept, still declared |
| 8 | Screenshots for the installer: `images/image1.png` to `image5.png`, 730×380 at most. Optional, none exist; `Verify.ps1 -Release` lists them in `repo.json` when they exist (0.1020) | agent, in game with the author | open |
| 9 | Move to 1.0.0 (author's decision; the release needs `-Major`), Verify, public commit, release `Stable-1.0.0` | agent, on request | at the end |
| 10 | Commit hash and `changelog` in `manifest.toml`, fork of goatcorp/DalamudPluginsD17, branch, copy of `publish/d17/testing/live/TenshoAssistant/`, pull request | author | not to do since DEC-039 |

## State of the checklist

| Requirement | State | Where |
|---|---|---|
| Build on their side: Linux, SDK 10.0.101, network off, lock file | **`global.json` fixed in 0.512** (it required 10.0.403, their image would not have built). A CI job uses their image and their command, green since 515a28b; it keeps the network | `global.json`, `.github/workflows/build.yml`, [record](../evidence/0.512-d17-build.md) |
| `packages.lock.json` tracked by git | done | `src/TenshoAssistant/packages.lock.json` |
| NuGet dependency reviewed by the team | `Raphael.Net` 5.0.0 (MIT, native library); the same version as Craftimizer, already published | `TenshoAssistant.csproj` |
| JSON manifest: Name, Author, Punchline, Description | done; Description rewritten in 0.512: lines cut by hand as the documentation asks, summary on the first line | `src/TenshoAssistant/TenshoAssistant.json` |
| InternalName, AssemblyVersion, DalamudApiLevel left to DalamudPackager | done; checked by `Verify.ps1` | same |
| Deterministic version | done: only `<Version>` of the csproj | `TenshoAssistant.csproj` |
| Normal windows through the Dalamud window system | done in code (0.514, 0.516): main window, map, guide, free-standing panels (`PanelWindow`), panels attached to a game window (`AttachedPanels`). Only exception: the invisible click zones of the Fashion Report, with no background or border | `PanelWindow.cs`, `AttachedPanels.cs`, `MainWindow.FashionWeek.cs` |
| Drawings over the game | `GetForegroundDrawList` for the Mini Cactpot, the Triple Triad advice and the deck reminder; `GetBackgroundDrawList` for the frames on the rows of the game's gathering window and the icons above nearby monsters (information only) | `MainWindow.TriadAdvice.cs`, `MainWindow.TriadDecks.cs`, `MainWindow.TriadRequest.cs`, `MainWindow.MiniCactpot.cs`, `MainWindow.GatheringPanel.cs`, `MainWindow.MonsterIcons.cs` |
| No automatic request to the game server | **One exception kept and declared** (point 2 above, DEC-035). Everything else starts on a click: teleport, applying the sort, macro chain, writing the macros, Try On, equipping a gearset, the hunt train call, game alarms. `/alarm`: one command every half second, never above 30 game alarms | `Collections/TitleChanger.Dalamud.cs`, `Alarms/AlarmService.Dalamud.cs`, `MainWindow.Alarms.cs` |
| No automation (crafting, dialogues, loot…) | compliant: simulator, solver and suggested move without execution; the crafting recorder only reads the actions the player uses | `Craft/CraftRecorder.Dalamud.cs` |
| Combat elements informative only, what the player would already know | parser: none (DEC-009). Beastmaster page: game data (DEC-032). Nearby monsters: name, place and level only, nothing about the fight (`Maps/MapLiveReader.Dalamud.cs`) | `MainWindow.Beastmaster.cs`, `MainWindow.NearbyMonsters.cs` |
| No advantage in competitive content | the whole plugin stops in PvP and ranked content (DEC-038); Triple Triad advice against NPCs only, never in tournaments or against a player (DEC-033) | `Competitive/`, `MainWindow.TriadAdvice.cs` |
| Account ids: only one's own | compliant: only `player.ContentId` (local character) is used as a key; the offers read at the market board keep no seller name or id, only whether an offer is one of the player's retainers' | `*.Dalamud.cs` readers, `Market/SeenPriceReader.Dalamud.cs` |
| No personal data sent, HTTPS, host names | compliant. Universalis, Garland Tools, wiki, Teamcraft (one GraphQL query per fishing spot), PaissaDB, GitHub (LuminaSupplemental, GatherBuddy, FFXIVGachaSpreadsheet, gallery and videos of this repository); the eight data sources each have their switch and « Game data and what you observed only » turns them off at once; the gallery and the videos are read on a click only. Exceptions chosen by the player: Discord webhook off by default, Discord hosts only, character name only as an option, address never logged; shared discoveries exchange through GitHub, sending only after the player connects GitHub, own sightings with the day only, to a secret gist and one issue named by the GitHub login, token encrypted on the PC (DPAPI) | [SECURITY](../../SECURITY.md) |
| Report a problem | prefilled GitHub form, opened in the browser; nothing is sent by the plugin | `Help/ProblemReport.cs` |
| No telemetry | compliant: none | — |
| Lumina rather than XIVAPI | compliant: game sheets; XIVAPI is not called by the plugin | [evidence](../evidence/0.451-xivapi-v2.md) |
| Icon `images/icon.png` 1:1, 64–512 px | done: 512×512, generated by the agent, declared in the description | `src/TenshoAssistant/images/icon.png`, `publish/d17/…/images/icon.png` |
| `manifest.toml` in `testing/live/TenshoAssistant/` | ready, **commit hash not filled** (not needed since DEC-039) | `publish/d17/testing/live/TenshoAssistant/manifest.toml` |
| AI disclosure in the pull request | « Auto » level draft (0.971); **to be rewritten by the author** if a submission ever happens | `publish/d17/PULL_REQUEST.md` |
| AI-assisted translations reviewed by speakers | German and Japanese: 35 keys, not reviewed, declared; English and French complete | `src/TenshoAssistant/Localization/Languages/` |
| Embedded data under a compatible licence, notice kept | done: SubmarineTracker and FFXIVGachaSpreadsheet (MIT, full text in the file), LuminaSupplemental airship and festival tables (GPL-3.0), Character Panel Refined (MIT, in the code), Lotlab FFXIV Gardening Tracker (GPL-3.0), nick75g crossbreeds (MIT); credits in the README and the Credits page | `FreeCompany/Data/`, `Events/Data/`, `Reminders/Data/`, `Character/StatFormulas.cs`, `Credits/CreditCatalog.cs` |

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
only, the rules kept. 2026-11-09: the Labyrinth team panel as a limited exception (DEC-047); releases by the agent, AI policy no
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
personnalisé ; c'est l'utilisateur qui la décide (DEC-039).

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
| 1 | Jouer sur la dernière version : Dalamud demande que l'auteur ait testé. Cases ouvertes dans [TESTING](TESTING.md) ; la campagne guidée en jeu est la phase 6 du [plan global](GLOBAL-PLAN.md) | auteur + agent | à faire (phase 6) |
| 2 | **Règle Dalamud** : titre selon le job, seule action vers le jeu sans clic (réglage désactivé par défaut) | auteur | **décidé** (DEC-035) : gardé tel quel |
| 3 | Alarmes du jeu : rien à changer. Les commandes `/alarm` partent toujours d'un clic du joueur : ajouter une alarme quand le réglage est actif, ou « Envoyer » ; les alarmes automatiques des servants et des vaisseaux n'en créent jamais | — | fait |
| 3 bis | **Règle Dalamud** : JcJ et contenu compétitif : Tensho se coupe en entier, sans réglage (DEC-038, 0.610) | auteur | codé, pas essayé en jeu |
| 4 | **Règle Dalamud** : dresseur. La page est une liste d'informations. Le panneau du Dédale abandonné par DEC-038 revient en exception limitée (DEC-047, X-109) : le panneau d'équipe devant le plateau du Dédale bestial, entre les combats seulement, coupé dès le début de chaque combat | auteur | décidé (DEC-047) ; à prouver en jeu |
| 5 | **Règle Dalamud** : fenêtres. « Si ça ressemble à une fenêtre, utiliser l'API de fenêtres. » Tout est converti (0.514, 0.516) sauf les zones de clic invisibles de la Revue de mode ; à vérifier en jeu (#38) | auteur | à vérifier en jeu |
| 6 | **Règle Dalamud** : déclaration d'IA et texte de la PR écrits par l'auteur, qui doit pouvoir expliquer le code. Taille au 2026-11-09 : 608 fichiers C#, 71 194 lignes dans le plugin | auteur | **fermé** (DEC-050) : politique IA plus suivie ; la déclaration d'IA reste dans le manifeste et le README ; brouillon dans `publish/d17/PULL_REQUEST.md` |
| 7 | Icône générée par IA, déclarée. La politique IA préférait une icône faite main | agent | **fermé** (DEC-050) : l'icône actuelle est gardée, toujours déclarée |
| 8 | Captures pour l'installeur : `images/image1.png` à `image5.png`, 730×380 au plus. Facultatif, aucune n'existe ; `Verify.ps1 -Release` les inscrit dans `repo.json` quand elles existent (0.1020) | agent, en jeu avec l'auteur | ouvert |
| 9 | Passer à 1.0.0 (décision de l'auteur ; la release demande `-Major`), Verify, commit public, release `Stable-1.0.0` | agent, sur demande | à la fin |
| 10 | Hash du commit et `changelog` dans `manifest.toml`, fork de goatcorp/DalamudPluginsD17, branche, copie de `publish/d17/testing/live/TenshoAssistant/`, PR | auteur | plus à faire depuis DEC-039 |

## État de la liste de contrôle

| Exigence | État | Où |
|---|---|---|
| Compilation chez eux : Linux, SDK 10.0.101, réseau coupé, fichier de verrouillage | **`global.json` corrigé en 0.512** (il exigeait 10.0.403, leur image n'aurait pas compilé). Un job de CI reprend leur image et leur commande, vert depuis 515a28b ; il garde le réseau | `global.json`, `.github/workflows/build.yml`, [relevé](../evidence/0.512-d17-build.md) |
| Fichier `packages.lock.json` suivi par git | fait | `src/TenshoAssistant/packages.lock.json` |
| Dépendance NuGet examinée par l'équipe | `Raphael.Net` 5.0.0 (MIT, bibliothèque native) ; même version que Craftimizer, déjà publié | `TenshoAssistant.csproj` |
| Manifeste JSON : Name, Author, Punchline, Description | fait ; Description réécrite en 0.512 : lignes coupées à la main comme le demande la documentation, résumé en première ligne | `src/TenshoAssistant/TenshoAssistant.json` |
| InternalName, AssemblyVersion, DalamudApiLevel laissés à DalamudPackager | fait ; contrôlé par `Verify.ps1` | idem |
| Version déterministe | fait : seulement `<Version>` du csproj | `TenshoAssistant.csproj` |
| Fenêtres normales via le système de fenêtres Dalamud | fait en code (0.514, 0.516) : principale, carte, guide, panneaux libres (`PanelWindow`), panneaux accrochés à une fenêtre du jeu (`AttachedPanels`). Seule exception : les zones de clic invisibles de la Revue de mode, sans fond ni bord | `PanelWindow.cs`, `AttachedPanels.cs`, `MainWindow.FashionWeek.cs` |
| Dessins par-dessus le jeu | `GetForegroundDrawList` pour le Mini Cactpot, le conseil et le rappel de deck de Triple Triade ; `GetBackgroundDrawList` pour les cadres sur les lignes de la fenêtre de récolte du jeu et les icônes au-dessus des monstres proches (information seulement) | `MainWindow.TriadAdvice.cs`, `MainWindow.TriadDecks.cs`, `MainWindow.TriadRequest.cs`, `MainWindow.MiniCactpot.cs`, `MainWindow.GatheringPanel.cs`, `MainWindow.MonsterIcons.cs` |
| Pas de requête automatique au serveur du jeu | **Une exception gardée et déclarée** (point 2 ci-dessus, DEC-035). Tout le reste part sur un clic : téléportation, application du tri, chaîne de macros, écriture des macros, cabine d'essayage, équiper un ensemble d'équipement, appel du train de chasse, alarmes du jeu. `/alarm` : une commande toutes les demi-secondes, jamais plus de 30 alarmes du jeu | `Collections/TitleChanger.Dalamud.cs`, `Alarms/AlarmService.Dalamud.cs`, `MainWindow.Alarms.cs` |
| Pas d'automatisation (artisanat, dialogues, butin…) | conforme : simulateur, solveur et coup conseillé sans exécution ; l'enregistreur d'artisanat ne fait que lire les actions qu'utilise le joueur | `Craft/CraftRecorder.Dalamud.cs` |
| Éléments de combat seulement informatifs, ce que le joueur saurait déjà | parser : aucun (DEC-009). Page dresseur : données du jeu (DEC-032). Monstres proches : nom, lieu et niveau seulement, rien sur le combat (`Maps/MapLiveReader.Dalamud.cs`) | `MainWindow.Beastmaster.cs`, `MainWindow.NearbyMonsters.cs` |
| Aucun avantage en contenu compétitif | le plugin entier s'arrête en JcJ et en contenu classé (DEC-038) ; conseil de Triple Triade contre les PNJ seulement, jamais en tournoi ni contre un joueur (DEC-033) | `Competitive/`, `MainWindow.TriadAdvice.cs` |
| Identifiants de compte : seulement le sien | conforme : seul `player.ContentId` (personnage local) sert de clé ; les offres lues au tableau des ventes ne gardent ni nom ni identifiant de vendeur, seulement si une offre est celle d'un servant du joueur | lecteurs `*.Dalamud.cs`, `Market/SeenPriceReader.Dalamud.cs` |
| Aucune donnée personnelle envoyée, HTTPS, nom d'hôte | conforme. Universalis, Garland Tools, wiki, Teamcraft (une requête GraphQL par lieu de pêche), PaissaDB, GitHub (LuminaSupplemental, GatherBuddy, FFXIVGachaSpreadsheet, galerie et vidéos de ce dépôt) ; les huit sources de données ont chacune leur interrupteur et « Données du jeu et observées seulement » les coupe d'un coup ; la galerie et les vidéos ne sont lues que sur un clic. Exceptions choisies par le joueur : webhook Discord désactivé par défaut, hôtes de Discord seulement, nom du personnage seulement sur option, adresse jamais journalisée ; échange des découvertes partagées par GitHub, qui n'envoie qu'après la connexion du joueur à GitHub, ses propres observations avec le jour seulement, vers un gist secret et un ticket au nom de son identifiant GitHub, jeton chiffré sur le PC (DPAPI) | [SECURITY](../../SECURITY.md) |
| Signaler un problème | formulaire GitHub prérempli, ouvert dans le navigateur ; rien n'est envoyé par le plugin | `Help/ProblemReport.cs` |
| Pas de télémétrie | conforme : aucune | — |
| Lumina plutôt que XIVAPI | conforme : feuilles du jeu ; XIVAPI n'est pas appelé par le plugin | [preuve](../evidence/0.451-xivapi-v2.md) |
| Icône `images/icon.png` 1:1, 64–512 px | fait : 512×512, générée par l'agent, déclarée dans la description | `src/TenshoAssistant/images/icon.png`, `publish/d17/…/images/icon.png` |
| `manifest.toml` dans `testing/live/TenshoAssistant/` | prêt, **hash du commit non renseigné** (plus nécessaire depuis DEC-039) | `publish/d17/testing/live/TenshoAssistant/manifest.toml` |
| Déclaration d'IA dans la PR | brouillon niveau « Auto » (0.971) ; **à réécrire par l'auteur** si une soumission a lieu un jour | `publish/d17/PULL_REQUEST.md` |
| Traductions assistées par IA relues par des locuteurs | allemand et japonais : 35 clés, non relues, déclarées ; anglais et français complets | `src/TenshoAssistant/Localization/Languages/` |
| Données embarquées sous licence compatible, avis conservé | fait : SubmarineTracker et FFXIVGachaSpreadsheet (MIT, texte complet dans le fichier), tables des dirigeables et des fêtes de LuminaSupplemental (GPL-3.0), Character Panel Refined (MIT, dans le code), FFXIV Gardening Tracker de Lotlab (GPL-3.0), croisements de nick75g (MIT) ; crédits dans le README et la page Crédits | `FreeCompany/Data/`, `Events/Data/`, `Reminders/Data/`, `Character/StatFormulas.cs`, `Credits/CreditCatalog.cs` |

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
