# User requests to plan

> *Traduction française plus bas.*

List taken from `ISSUES.md` (2026-11-01), sorted by nature then by module. Each request says what is wrong and the expected result.
A request that has been dealt with is recorded in [CHANGELOG.md](../../CHANGELOG.md) with its number (e.g. "R-03") and marked DONE or CLOSED here
(the line is kept as history). The order of work is set in the maintainer's status file ("Next action").
Overview, points to decide and tests to run: [ROADMAP](ROADMAP.md). State reread at 0.512 (2026-11-04).

## Fixes

| No. | Module | Problem observed | Expected result |
|---|---|---|---|
| R-01 (DONE 0.242–0.243: removal by right click, then automatic when the quest is no longer active; to validate in game) | Map | Targeting a quest from the journal or from the active quest list does not behave well (target icon that stays, impossible to remove). | Review quest targeting: target followed and removed correctly. Study how Mappy works (read only, AGPL licence). |
| R-02 (DONE 0.241; to validate in game) | Duty loot (mission panel) | The "Not obtained only" filter does not show Triple Triad cards or orchestrion rolls, and these have no ✓/✗ badge. | Cards and rolls taken into account by the filter and marked ✓/✗ like mounts and minions. |
| R-03 (DONE 0.244; to validate in game) | Map (right click) | The Triple Triad NPCs of the zone are not in the local lists of the right click. | A "Triple Triad" tab or section with the NPCs of the zone (flag, beaten or not). |
| R-04 (DONE 0.245; per-icon setting as a mouse button 0.298, validated in game with 0.297–0.299) | Map (right click) | A right click on a map icon does not directly open the menu of that icon. | The right click on an icon opens its menu; a list of icons to ignore (adjustable) lets the click through to the general menu. |

## Interface

| No. | Module | Request | Expected result |
|---|---|---|---|
| R-05 (CLOSED 2026-11-01: nothing to code, user decision; see docs/evidence/0.246-r05-window-order.md) | All windows | Tensho's windows go above everything. | If Dalamud allows it, a display order close to that of the game's windows (stay behind a game window brought to the front). To study: feasibility first. |
| R-06 (DONE for the most part 0.247–0.294, plan R-06 (maintainer's notes, not in the repository); remaining: useful missing settings, frames and textures of the game) | Items and Storage (Bank); widened on 2026-11-01 to the whole interface | Interface to review to be clearer and more practical. | A review of the ergonomics of these two modules (layout, filters, readability), proposed before being coded. Done: common templates, sidebar by categories, full item sheet, separate Storage page, enriched market, tooltip and ↺ on every setting, measured lightness, the game's title font. |

## New functions

| No. | Module | Request | Expected result |
|---|---|---|---|
| R-07 (DONE: Free company and expeditions page 0.307, best submarine routes 0.309, builds 0.315, follow-up DEC-023 0.334–0.337, history 0.379; to validate in game) | Free company (new) | Help for free company projects: submarines, airships… | Study of the useful existing plugins (references, licences), then a module: routes, returns, loot and ranks. Remaining: no route planner for airships (no data under a compatible licence); loot value without breakpoints, at the NPC price. |
| R-08 (DONE: Reminders page 0.300, icon and tooltip in the server bar 0.300–0.302, company expeditions 0.307, squadron 0.333; Planeurs d'Ehcatl left, nothing readable 0.334; to validate in game) | Reminders (new) | Reminders for daily, weekly and other tasks. | A list of tasks per period (daily / weekly reset), tickable, with a reminder, linked to the Alarms module. Remaining: Grand Company squadron, deliveries of the Planeurs d'Ehcatl. |
| R-11 (DONE: steps 1 to 3 0.310–0.315, step 4 chocobo barding 0.340, plan R-11 (maintainer's notes, not in the repository); 0.310–0.314 validated in game, 0.315 and 0.340 to validate) | Gear (new) | Gear planner in the style of the Character window, gear close to the level, filters (exact level outside the tiers…), real stats, try-on everywhere, sharing of builds; same style for submarines / airships and any useful gear. | Gear page, fitting room, build codes; then sources / ownership, materia, vessel builds. Done: all of that (glamour and materia caps in addition). Remaining: step 4 (other gear), to decide. |
| R-09 (DONE 0.342: Events page; to validate in game) | Events (new) | Follow the active seasonal events (Halloween, etc.). | A module listing the ongoing events, their dates, quests and rewards, with what is already obtained. |
| R-14 (DONE 0.452–0.454, 0.491–0.492, 0.507, self-check 0.494–0.496; rule applied to the new code, the maintainer's rules) | The whole plugin (technical) | « Il faudrait ajouter une étape pour tester notre résistance aux updates FF14 et y pallier un maximum » ("We should add a step to test our resistance to FF14 updates and make up for it as much as possible"), then « vers la toute fin et directement dans la logique des codes » ("towards the very end and directly in the logic of the code") (2026-11-02). | A pass over all the code: a survey of each native dependency (FFXIVClientStructs structures, game windows and nodes, AtkValues indices, hooks, Lumina sheet columns); in the code, a guard for each (checks of shape and size, known game version, clean degradation into "unavailable after a game update", never a crash or a wrong value); self-check at load visible in Diagnostics; tests that simulate a changed piece of data; procedure to follow after a patch (docs). |
| R-13 (DONE 0.320: Preferences › Credits; to validate in game) | Credits (new page) | « Réserve une section complète dans le plugin pour les crédits de ce qu'on aurait utilisé dans tout le code » ("Reserve a full section in the plugin for the credits of what we would have used throughout the code") (2026-11-02). | A "Credits" page in the plugin, for everyone (not only in developer mode): libraries (Dalamud, FFXIVClientStructs, Lumina…), embedded data (file, licence, copyright), data read online (service, licence, setting that turns it off), references read without copying (AGPL or no licence), community rules taken over as estimates, notice on the game's data; drawn from a single list in the code, aligned with the "Credits" section of the README, with a test that checks that each embedded piece of data has its credit. |
| R-12 (DONE 0.319: Settings › Alarms › Discord; to validate in game) | Alarms, Reminders, Free company | « Prévois dans une todo list un hook Discord pour envoyer les alarmes avec message sur un serveur Discord ; voir plus si possible ou intéressant » ("Plan in a todo list a Discord hook to send the alarms with a message to a Discord server; see more if possible or interesting") (2026-11-02). | "Discord webhook" setting, off by default: URL entered by the user, sending of Tensho's alarms (gathering, fishing, reminders, vessel returns) with a message editable per alarm, "Send a test" button. To study next: other useful events (submarine and airship returns, calendar resets, market price alert), role mention, several webhooks. Constraints: the webhook URL treated as a secret (never in the logs nor in the copied Diagnostics report), no character data sent without an explicit choice, sending outside rendering (worker) and Discord's rate limit respected, compliance with Dalamud's publication rules to check (network sending only at the user's request), comparable existing plugins to note during c). |

## Technical checks

| No. | Domain | Request | Expected result |
|---|---|---|---|
| R-10 (DONE 0.246, see docs/evidence/0.246-cache-audit.md; rechecked 0.281 with the "Never" duration) | Caches | Check all the caches. | An audit: no tool queries the community sources (Garland, wiki, LuminaSupplemental, GatherBuddy, Teamcraft, Universalis) or the game data too often; durations and rates documented. |

## Requests of 2026-11-02 (STATUS queue)

They have no R number; the letter is the one in STATUS ("Next action"), to be dealt with one at a time, in this order.

| Letter | Domain | Request | Expected result |
|---|---|---|---|
| b) (DONE 0.316; decisions 0.317, DEC-023) | Documentation | Tour of the `.md` files and of the tool descriptions in the code; roadmap completed. | Documents consistent with the code; undecided points and tests to run listed in the [ROADMAP](ROADMAP.md). |
| c) (DONE 0.318: docs/project/GAPS.md, choice DEC-025) | Comparison | What Tensho lacks compared with the existing plugins; widened on 2026-11-02: « pas que plugin de référence mais ceux du dépôt Dalamud (voir repo non officiel si ça respecte les règles Dalamud upload) » ("not only reference plugins but those of the Dalamud repository (see unofficial repos if they respect the Dalamud upload rules)"); then « ajoute une phase décision avant aligner avec roadmap pour que je choisisse ce qu'on rajoute » ("add a decision phase before aligning with the roadmap so that I choose what we add"). | A plan: missing function, source plugins (17 references, official repository, unofficial repositories that comply with the rules), licence (read only if AGPL or no licence), data source; then the user's choice, and only after that the roadmap. |
| d) (DONE 0.321–0.326) | Code | Settings that are missing; development tools for the open source community; « regarde aussi ce que tu pourrais faire pour améliorer notre git » ("also look at what you could do to improve our git") (2026-11-02). | Survey of the hard-coded values to make adjustable; development tools and improvements of the git / GitHub repository proposed (nothing is pushed or set on GitHub without agreement). |
| e) (DONE 0.327–0.332) | All the tools | General improvements. | The map first, then the other tools. |

---

# Demandes utilisateur à planifier

Liste reprise de `ISSUES.md` (2026-11-01), classée par nature puis par module. Chaque demande dit ce qui ne va pas et le résultat attendu.
Une demande traitée est reportée dans [CHANGELOG.md](../../CHANGELOG.md) avec son numéro (ex. « R-03 ») et marquée FAIT ou CLOS ici
(la ligne est gardée comme historique). L'ordre de passage est fixé dans [docs/ai/STATUS.md](../ai/STATUS.md) (« Prochaine action »).
Vue d'ensemble, points à décider et tests à faire : [ROADMAP](ROADMAP.md). État relu au 0.512 (2026-11-04).

## Corrections

| N° | Module | Problème constaté | Résultat attendu |
|---|---|---|---|
| R-01 (FAIT 0.242–0.243 : retrait par clic droit, puis automatique quand la quête n'est plus active ; à valider en jeu) | Carte | Cibler une quête depuis le journal ou la liste des quêtes actives ne se comporte pas bien (icône de cible qui reste, impossible à retirer). | Revoir le ciblage de quête : cible suivie et retirée correctement. Étudier le fonctionnement de Mappy (lecture seule, licence AGPL). |
| R-02 (FAIT 0.241 ; à valider en jeu) | Butin des contenus (panneau de mission) | Le filtre « Non obtenus seulement » ne montre pas les cartes de Triple Triad ni les rouleaux d'orchestrion, et ceux-ci n'ont pas la pastille ✓/✗. | Cartes et rouleaux pris en compte par le filtre et marqués ✓/✗ comme les montures et mascottes. |
| R-03 (FAIT 0.244 ; à valider en jeu) | Carte (clic droit) | Les PNJ de Triple Triad de la zone ne figurent pas dans les listes locales du clic droit. | Un onglet ou une section « Triple Triad » avec les PNJ de la zone (drapeau, état battu ou non). |
| R-04 (FAIT 0.245 ; réglage par icône en bouton souris 0.298, validé en jeu avec 0.297–0.299) | Carte (clic droit) | Un clic droit sur une icône de la carte n'ouvre pas directement le menu de cette icône. | Le clic droit sur une icône ouvre son menu ; une liste d'icônes à ignorer (réglable) laisse passer le clic au menu général. |

## Interface

| N° | Module | Demande | Résultat attendu |
|---|---|---|---|
| R-05 (CLOS 2026-11-01 : rien à coder, décision utilisateur ; voir docs/evidence/0.246-r05-window-order.md) | Toutes les fenêtres | Les fenêtres de Tensho passent au-dessus de tout. | Si Dalamud le permet, un ordre d'affichage proche de celui des fenêtres du jeu (rester derrière une fenêtre du jeu mise au premier plan). À étudier : faisabilité d'abord. |
| R-06 (FAIT pour l'essentiel 0.247–0.294, plan docs/goals/R-06.md ; reste : réglages utiles manquants, cadres et textures du jeu) | Objets et Rangements (Banque) ; élargie le 2026-11-01 à toute l'interface | Interface à revoir pour être plus claire et plus pratique. | Une révision de l'ergonomie de ces deux modules (disposition, filtres, lisibilité), proposée avant d'être codée. Fait : modèles communs, barre latérale par catégories, fiche objet complète, page Rangements séparée, marché enrichi, infobulle et ↺ sur chaque réglage, légèreté mesurée, police de titre du jeu. |

## Nouvelles fonctions

| N° | Module | Demande | Résultat attendu |
|---|---|---|---|
| R-07 (FAIT : page Compagnie libre et expéditions 0.307, meilleurs trajets de sous-marin 0.309, montages 0.315, suite DEC-023 0.334–0.337, historique 0.379 ; à valider en jeu) | Compagnie libre (nouveau) | Aide pour les projets de compagnie libre : sous-marins, dirigeables… | Étude des plugins existants utiles (références, licences), puis un module : trajets, retours, butins et rangs. Reste : pas de planificateur de trajets pour les dirigeables (aucune donnée sous licence compatible) ; valeur du butin sans paliers, au prix PNJ. |
| R-08 (FAIT : page Rappels 0.300, icône et infobulle dans la barre du serveur 0.300–0.302, expéditions de la compagnie 0.307, escouade 0.333 ; Planeurs d'Ehcatl laissés, rien de lisible 0.334 ; à valider en jeu) | Rappels (nouveau) | Rappels des tâches quotidiennes, hebdomadaires et autres. | Une liste de tâches par période (réinitialisation quotidienne / hebdomadaire), cochables, avec rappel, reliée au module Alarmes. Reste : escouade de Grande Compagnie, livraisons des Planeurs d'Ehcatl. |
| R-11 (FAIT : étapes 1 à 3 0.310–0.315, étape 4 barde de chocobo 0.340, plan docs/goals/R-11.md ; 0.310–0.314 validées en jeu, 0.315 et 0.340 à valider) | Équipement (nouveau) | Planificateur d'équipement style fenêtre Personnage, équipement proche du niveau, filtres (niveau exact hors paliers…), stats réelles, essayage partout, partage des builds ; même style pour sous-marins / dirigeables et tout équipement utile. | Page Équipement, cabine d'essayage, codes de build ; puis sources / possession, matérias, montages de vaisseaux. Fait : tout cela (mirage et plafonds de matérias en plus). Reste : étape 4 (autres équipements), à décider. |
| R-09 (FAIT 0.342 : page Événements ; à valider en jeu) | Événements (nouveau) | Suivre les événements saisonniers actifs (Halloween, etc.). | Un module listant les événements en cours, leurs dates, quêtes et récompenses, avec ce qui est déjà obtenu. |
| R-14 (FAIT 0.452–0.454, 0.491–0.492, 0.507, auto-contrôle 0.494–0.496 ; règle appliquée au nouveau code, docs/ai/RULES.md) | Tout le plugin (technique) | « Il faudrait ajouter une étape pour tester notre résistance aux updates FF14 et y pallier un maximum », puis « vers la toute fin et directement dans la logique des codes » (2026-11-02). | Passage sur tout le code : relevé de chaque dépendance native (structures FFXIVClientStructs, fenêtres et nœuds du jeu, index d'AtkValues, hooks, colonnes de feuilles Lumina) ; dans le code, garde de chacune (contrôles de forme et de taille, version du jeu connue, dégradation propre en « indisponible après une mise à jour du jeu », jamais de plantage ni de valeur fausse) ; auto-vérification au chargement visible dans Diagnostics ; tests qui simulent une donnée changée ; marche à suivre après un patch (docs). |
| R-13 (FAIT 0.320 : Préférences › Crédits ; à valider en jeu) | Crédits (nouvelle page) | « Réserve une section complète dans le plugin pour les crédits de ce qu'on aurait utilisé dans tout le code » (2026-11-02). | Une page « Crédits » dans le plugin, pour tout le monde (pas seulement en mode développeur) : bibliothèques (Dalamud, FFXIVClientStructs, Lumina…), données embarquées (fichier, licence, copyright), données lues en ligne (service, licence, réglage qui les coupe), références lues sans copie (AGPL ou sans licence), règles communautaires reprises comme estimations, avis sur les données du jeu ; tirée d'une seule liste dans le code, alignée avec la section « Credits » du README, avec un test qui vérifie que chaque donnée embarquée a son crédit. |
| R-12 (FAIT 0.319 : Réglages › Alarmes › Discord ; à valider en jeu) | Alarmes, Rappels, Compagnie libre | « Prévois dans une todo list un hook Discord pour envoyer les alarmes avec message sur un serveur Discord ; voir plus si possible ou intéressant » (2026-11-02). | Réglage « Webhook Discord », désactivé par défaut : URL saisie par l'utilisateur, envoi des alarmes Tensho (récolte, pêche, rappels, retours de vaisseaux) avec un message modifiable par alarme, bouton « Envoyer un test ». À étudier ensuite : autres événements utiles (retours de sous-marins et dirigeables, remises à zéro du calendrier, alerte de prix du marché), mention d'un rôle, plusieurs webhooks. Contraintes : URL du webhook traitée comme un secret (jamais dans les journaux ni dans le rapport copié de Diagnostics), aucune donnée de personnage envoyée sans choix explicite, envoi hors du rendu (worker) et limite de débit de Discord respectée, conformité aux règles de publication Dalamud à vérifier (envoi réseau seulement à la demande de l'utilisateur), plugins existants comparables à relever pendant c). |

## Vérifications techniques

| N° | Domaine | Demande | Résultat attendu |
|---|---|---|---|
| R-10 (FAIT 0.246, voir docs/evidence/0.246-cache-audit.md ; revérifié 0.281 avec la durée « Jamais ») | Caches | Vérifier tous les caches. | Un audit : aucun outil ne sollicite trop souvent les sources communautaires (Garland, wiki, LuminaSupplemental, GatherBuddy, Teamcraft, Universalis) ni les données du jeu ; durées et cadences documentées. |

## Demandes du 2026-11-02 (file d'attente de STATUS)

Elles n'ont pas de numéro R ; la lettre est celle de STATUS (« Prochaine action »), à traiter une à la fois, dans cet ordre.

| Lettre | Domaine | Demande | Résultat attendu |
|---|---|---|---|
| b) (FAIT 0.316 ; décisions 0.317, DEC-023) | Documentation | Tour des `.md` et des descriptions des outils dans le code ; feuille de route complétée. | Documents conformes au code ; points indécis et tests à faire listés dans la [ROADMAP](ROADMAP.md). |
| c) (FAIT 0.318 : docs/project/GAPS.md, choix DEC-025) | Comparaison | Ce qui manque à Tensho par rapport aux plugins existants ; élargi le 2026-11-02 : « pas que plugin de référence mais ceux du dépôt Dalamud (voir repo non officiel si ça respecte les règles Dalamud upload) » ; puis « ajoute une phase décision avant aligner avec roadmap pour que je choisisse ce qu'on rajoute ». | Un plan : fonction manquante, plugins sources (17 références, dépôt officiel, dépôts non officiels conformes aux règles), licence (lu seulement si AGPL ou sans licence), source des données ; puis choix de l'utilisateur, et seulement ensuite la feuille de route. |
| d) (FAIT 0.321–0.326) | Code | Réglages qui manquent ; outils de développement pour la communauté open source ; « regarde aussi ce que tu pourrais faire pour améliorer notre git » (2026-11-02). | Relevé des valeurs figées à rendre réglables ; outils de développement et améliorations du dépôt git / GitHub proposés (rien n'est poussé ni réglé sur GitHub sans accord). |
| e) (FAIT 0.327–0.332) | Tous les outils | Améliorations générales. | La carte d'abord, puis les autres outils. |
