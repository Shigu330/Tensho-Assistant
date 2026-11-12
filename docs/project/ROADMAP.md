# Roadmap of the 17 plugins

> *Traduction française plus bas.*

Since 2026-11-09, the work plan now lives in the [global plan](GLOBAL-PLAN.md), and the history of each version in
[CHANGELOG.md](../../CHANGELOG.md). This file keeps the authoritative list of the 17 reference plugins and what Tensho covers of each, the
user requests R-01 to R-14, the pages added outside the 17 plugins, the open questions and the decisions taken along the way. The
17 rows stay PARTIAL: no complete in-game validation. Release: the custom repository, see [PUBLISHING](PUBLISHING.md) (no submission to
the official repository, DEC-039).

The step queue a) to h) (2026-11-02 to 2026-11-04) and the foundation notes (2026-10-27 to 2026-10-28) were history; they were removed from
this file on 2026-11-08 and stay readable in git (this file in commit `2da535d`, 0.865.0).

Origin: the master prompt, §4 (maintainer's notes, outside the repository; a historical source since 2026-11-09, the user's
decisions and [DECISIONS](DECISIONS.md) prevail). Every row is kept. State
describes the work; Coverage describes the capacity actually validated. TODO/READY/ACTIVE/BLOCKED/DONE; NOT_STARTED/PARTIAL/FUNCTIONAL/
REPLACEABLE. NEEDS_STUDY is a note of uncertainty, not a removal. No arbitrary percentage.

## Main matrix

The « Work » column was written at 0.315 (2026-11-02) and completed until 0.512; what came later is in the [CHANGELOG](../../CHANGELOG.md)
and the [global plan](GLOBAL-PLAN.md). « Validated » = report or screenshot of the user; the other versions named are to be validated in
game. The goal sheets these rows named (`docs/goals/`) are agent files, outside the repository.

| # | ID | Plugin | Author(s) | Description | State | Coverage | Work | Evidence | Group |
|---|---|---|---|---|---|---|---|---|---|
| 1 | FASHION-001 | Avant-Garde | NeNeppie | Valid gear for the Fashion Report hints | ACTIVE | PARTIAL | Manual directory, flagged pieces, filters/stock/context and provenance tested; buttons on the slots of the game window and dyes of the week validated in game (0.213–0.223); « owned » with armoury chest, glamour dresser and armoire read from the game (0.235–0.240, to validate); « Try on » (Try On window) on right click of any gear (0.310, validated). Score per piece not done: no data (decision of 2026-11-01) | — | C |
| 2 | BLU-001 | Blue Mage Helper | Infi, Sl0nderman, harbingerftw | Blue Mage spellbook: spells, acquisition and progress | ACTIVE | PARTIAL | Native spellbook, numbers distinct from ids, confirmed acquisitions; build/tests green, runtime to confirm | — | C |
| 3 | CHAR-001 | Character Panel Refined | Kouzukii | Useful character statistics | ACTIVE | PARTIAL | Native attributes copied and invalidated; real combat values (`Character/StatFormulas.cs`: the game's ParamGrow, community formulas, main modifier taken as data from Character Panel Refined, MIT) in a panel beside the game's Character window, validated in game (0.182–0.184); taken up for the plan in the Gear page (R-11, 0.310, validated) | [ParamGrow probe](../evidence/0.182.0-paramgrow-probe.log) | D |
| 4 | COLL-001 | Collections | Seventh | Discovery, progress and interactions with collections | ACTIVE | PARTIAL | Mounts/minions/emotes/cards/orchestrion/titles/achievements; categories/sheets, rewards and context, three-state acquisition for dedicated lists; obtained/not obtained state in the item tooltip, right click to the collection and « How to get it » (vendors, exchanges, quests, achievements, crafting, gathering, then Garland Tools and the wiki) validated in game (0.146). ✓/✗ badge on every collection item icon (0.275) and cards/scrolls in « Not obtained only » (0.241), to validate; Triple Triad Ex. cards (0.278, validated). Other types and remaining unknown sources to complete | — | B |
| 5 | CRAFT-001 | Craftimizer | Asriel | Craft simulation, assisted macros and suggestions during a craft | ACTIVE | PARTIAL | Recipes/preparation validated; simulation/export/search/sequences, ordinary projection and macro help beside the game's crafting log (selection, attributes, comparison and copies) compiled/tested; ordinary/initial HQ collectables and priority to expectations integrated; copying macros into the game's macros, slot forecast and chaining validated in game (0.191–0.196); adaptive rotation (A-04, 0.509) and the optional Raphael solver (A-03, 0.465) added later; special recipes to complete | — | A |
| 6 | DUTY-001 | Duty Loot Preview | grittyfrog | Preview of duty loot | ACTIVE | PARTIAL | Catalogue and manual lookup, cache/cancellation, provenance and context tested; panel beside the game's duty finder (0.185–0.186, selection validated), loot of recent duties from LuminaSupplemental (0.188–0.190, validated), loot ✓/✗ and roulette (0.207–0.208, validated); cards and scrolls ✓/✗ (R-02, 0.241, to validate); partial data | — | C |
| 7 | CACT-001 | ezMiniCactpot | daemitus, Minmoose, MidoriKami | Best choice at the Mini Cactpot | ACTIVE | PARTIAL | Exact manual solver; opt-in observed mode and recomputation per ticket, owner worker/generation; solution drawn over the game window `LotteryDaily` (0.158), validated in game (report of 2026-11-01) | — | D |
| 8 | GATHER-001 | GatherBuddy | Ottermandias | Gathering and fishing | ACTIVE | PARTIAL | Sources/maps, opt-in MIN/BTN observation, notebook and explicit baits/conditions; local weather and bounded computation of the given criteria. In-game gathering and fishing help panels (0.225–0.233), GatherBuddy fishing data first then Garland (0.227), old GatherBuddy files (0.229, validated), Teamcraft bite times and timer (0.231, validated), fishing alarms with weather (0.253–0.254); ocean fishing calendar and help during the voyage (0.392 and later); the rest to validate | [spot coordinates](../evidence/0.226-fishing-spot-coordinates.md), [Teamcraft bite times](../evidence/0.231-teamcraft-bite-times.md) | A |
| 9 | USE-001 | Item Use | PunishedPineapple | Common uses of an item | ACTIVE | PARTIAL | Tab, opt-in hover and Inventory menu (0.207, validated); recipes/leves/GC/aquarium/orchestrion and quest sources; Uses tab of the item sheet (0.272); Tensho menu of the game's inventory and panel under its tooltip aligned with Tensho (0.256–0.257, to validate); other uses/availability/links to complete | — | C |
| 10 | GP-001 | LazyGatherer | Hiroa | GP optimisation for gathering | ACTIVE | PARTIAL | Manual optimiser with families already paid and limited bonus; observation/copy of resources (tested at the time); Bountiful Yield II bonus prefilled by a cited community rule, shown « estimated » and editable (0.224, to validate); native decoder/regeneration/procs/collectables and runtime to complete | [bountiful base](../evidence/0.224-bountiful-base.md) | D |
| 11 | MACRO-001 | Macro Chain | Caraxi | Running macros one after the other | ACTIVE | PARTIAL | Persistent plan, end-marker protocol, stop/identity and managed tests; plan filled after copying the macros into the game, and chaining, validated in game (0.196, « macro chain works ») | — | D |
| 12 | MAP-001 | Mappy | MidoriKami, harbingerftw | Versatile in-game map | ACTIVE | PARTIAL | Tensho map replacing the game map (M, journal quest, aetherytes): game icons with size/priority per map, quests and leves, FATEs, party, timed gathering/fishing, aether currents, sightseeing (wiki weather), vendors, flag, teleport at the real cost, lists per tab. Validated in game: 0.100–0.156 (including the host map off screen, coloured areas, zoom, Rowena's houses), map variants (0.171–0.175), quest icons (0.211), settings and view cone (0.287–0.289), edge markers and « Remove the flag » (0.297–0.299), menus and tools over the map (0.303–0.306). To validate: R-01 (0.242–0.243), R-03 (0.244), R-04 (0.245), zoom background (0.308). Unknown: marker type of leves, meaning of the quest icon variants | [event icons](../evidence/0.211-event-icons.md) | C |
| 13 | LOOT-001 | Monster Loot Hunter | Sylf | Material sources on monsters | ACTIVE | PARTIAL | Material/monster/zone, sheets, cache/rate/cancellation and context tested; LuminaSupplemental (MobDrop) first (0.190, validated), then the FFXIV wiki and Garland for the places; partial data | — | B |
| 14 | MARKET-001 | Price Insight | Kouzukii | Market price when hovering an item | ACTIVE | PARTIAL | Manual lookup and opt-in hover NQ/HQ/world, attached to the tooltip, stock and cheapest of the data centre validated in game (0.85–0.86); Market page enriched (R-06 step 4: Universalis listings, filters, cost for N units, history, column sort) validated in game (0.270–0.271); fallback on the disk cache if Universalis fails (0.281, to validate) | [Universalis listings](../evidence/0.270-universalis-listings.md) | B |
| 15 | QUEST-001 | QuestTracker | isaiahcat, keifufu | Quest progress and completion | ACTIVE | PARTIAL | Sheets/links/levels/zones/map, conditional native rewards and reverse Item → quest index outside the draw; observed progress joined to an item's sources (0.76, validated); quest types of the journal sections, percentages, start cities and options not taken (0.283–0.286, validated). Currencies/ranks not proven; availability to complete | [journal sections](../evidence/0.283-quest-journal-sections.md), [start cities](../evidence/0.285-quest-start-cities.md), [alternatives](../evidence/0.286-quest-alternatives.md) | B |
| 16 | SORT-001 | SortaKinda | MidoriKami | Inventory customisation, rules, preview and application | ACTIVE | PARTIAL | Reusable profiles and templates, multiple criteria, protected/filtered zones, filling in both directions and visual application after direct capture; native sort validated in game (2026-10-30). Armoury chest read as a storage (0.235, to validate) but not sorted: armoury sort and automatic sort left for later (I-14) | — | A |
| 17 | TRIAD-001 | TriadBuddy | MgAl2O4 | Triple Triad solver | ACTIVE | PARTIAL | Card list/deck/manual solver tested; TRIAD-003 finished: advice drawn during a match (0.160), % per deck and deck generation at the challenge window (0.161), Tensho decks (0.162) validated in game; card list (menus, values, sort, estimated score, Ex. cards) validated (0.265–0.269, 0.276–0.279). Native rules not covered | [Ex. cards](../evidence/0.278-triad-ex-cards.md) | C |

The domain IDs name the overall capacity, cut into small goals when it is started. CRAFT-001 starts with the catalogue, then simulation,
macros and suggestions; GATHER-001 with the places, then time windows/weather/fishing; SORT-001 with the preview before any native
application. The other capacities follow the same principle, without declaring the plugin covered from the first slice.

## User requests R-01 to R-14

This table is the only list of the requests R-01 to R-14 (their detailed wording was in USER-REQUESTS, removed in 0.1088; git history keeps it); a new request goes to the [global plan](GLOBAL-PLAN.md) as an X-NN point ([IDS](IDS.md)). « Validated » = report or screenshot of the user; otherwise, to validate in
game. The plans R-06 and R-11 are agent files (`docs/goals/`), outside the repository.

| No. | Subject | Done | Validated in game | Left |
|---|---|---|---|---|
| R-01 | Map: journal quest target that stayed | 0.242 (removed on right click), 0.243 (removed when the quest is no longer active) | no | — |
| R-02 | Duty loot: cards and scrolls ✓/✗ and « Not obtained » filter | 0.241 | no | — |
| R-03 | Map: « Triple Triad » tab on right click (NPCs of the zone) | 0.244 | no | — |
| R-04 | Map: right click on an icon = its own menu | 0.245; mouse button of the icon settings 0.298 | 0.297–0.299 (« Remove the flag » in these menus, mouse button) | — |
| R-05 | Window order | closed without code (2026-11-01, [evidence](../evidence/0.246-r05-window-order.md)) | — | — |
| R-06 | Interface overhaul | steps 1 (0.247–0.263), 2 (0.264), 4 (0.270–0.271); 3 mostly (item sheet 0.272, Storage 0.280); 5 in part (↺ 0.249–0.262, « Reset everything to default » 0.258, tooltips on every setting 0.287, map settings 0.288–0.289); 6: lightness (0.290–0.292) and the game's title font (0.293–0.294) | 0.265–0.271, 0.276–0.279, 0.287–0.294 | useful missing settings (step 5, point d); game frames and textures (step 6, removed in 0.510, DEC-033); `FilteredTable` removed from the plan (DEC-023) |
| R-07 | Free company | page and expeditions 0.307, best routes 0.309, builds 0.315; DEC-023 follow-up done: flagged sectors 0.334, market prices and tiers 0.335, airships 0.336–0.337; history 0.379 | no | to validate in game |
| R-08 | Reminders | page 0.300, icon and tooltip in the server bar 0.300–0.302, expeditions 0.307, squadron 0.333; then step g (M-01 to M-05, N-01 to N-03) | no | Ehcatl Gliders left out (nothing readable, 0.334); to validate in game |
| R-09 | Seasonal events | Events page (0.342): running, quests, shops, names | 0.342 | to validate; no future dates (no reliable source) |
| R-10 | Cache audit | 0.246 ([audit](../evidence/0.246-cache-audit.md)), second check in 0.281 (« Never » duration) | — | — |
| R-11 | Gear | step 1 (0.310), glamour (0.311), materia and caps (0.312–0.313), sources and ownership (0.314), vessel builds (0.315) | 0.310–0.314 | 0.315 and 0.340 to validate; step 4: chocobo barding DONE 0.340, Chocobo page 0.341 (squadron left out, recommended default, decision at the end) |
| R-12 | Discord webhook for alarms | 0.319 | no | to validate in game; used later by C-10, I-02, M-01 |
| R-13 | Full « Credits » page in the plugin | 0.320 | no | to validate in game |
| R-14 | Resistance to FFXIV updates, in the logic of the code | rule for new code (0.319); audit and fixes 0.452–0.454, 0.491–0.492, 0.507; self-check 0.494–0.496 | by the self-check, for what it reads | — |

## Pages outside the 17 plugins

Every page of the code, by group, is in the table of the [README](../../README.md) (« Overview »), written from the page list
(`MainWindow.Pages.cs`) by `tools/docgen.py` at every version. The Character page was removed in 0.341 (the Gear page shows the same real
values; the panel beside the game's Character window stays). Since 0.882 the Discoveries page also exchanges the players'
in-game readings through GitHub, on the player's click or the opt-in daily exchange (no data taken from websites).

## To decide / to review

<!-- gen:decisions:en -->
Open points, produced by script from the agent's source of decisions. Choices already taken: [DECISIONS](DECISIONS.md); the former tables: [archive](archive/DECISIONS-EN-ATTENTE-2026-11-09.md).

| N° | Point | Choice applied, or record awaited | Where |
|---|---|---|---|
| D-83 | Getting started, help, videos, issues: The 8 slices coded (0.502 to 0.506), DEC-031 | In-game validation; a release for the video list | ONBOARD-001 |
| D-84 | Community templates: Finished on the code side in 0.476 | In-game validation; gallery visible after a release | TEMPLATES-001, [DEC-029](DECISIONS.md) |
| D-85 | « Cost ≤ capacity » rule below rank 50: Rule of the community tools. Consistent with 11 published airship builds; not checked in game below rank 50. | In-game record on a vessel below rank 50. | [0.315-vessel-builds](../evidence/0.315-vessel-builds.md), « Coût ≤ capacité » |
| D-87 | Flag icon 60561: The flag icon id is not proven in docs/evidence (placing the flag is validated in game). | In-game record: flag placed, `AgentMap.FlagMapMarkers[0].MapMarker.IconId` read on the framework (probe), log in docs/evidence. | `MainWindow.MapView.cs` (`FlagIcon`) ; CHANGELOG 0.102 (« supposée ») ; audit de la carte 0.328 |
| D-88 | Quest icons 71141 / 71151: The meaning of the variants is not in the game data: the settings show « variant n ». | In-game record, otherwise keep « variant n ». | BACKLOG (removed in 0.1088, git history) ; [0.211-event-icons](../evidence/0.211-event-icons.md) |
| D-89 | Estimated Bountiful bonus (0.224): Unofficial community rule, shown « estimated » and editable. | To compare in game with the real bonus. | BACKLOG, « Revue des points ouverts » (removed in 0.1088, git history) ; [0.224-bountiful-base](../evidence/0.224-bountiful-base.md) |
| D-90 | Materia: piece on several slots: A piece covering several slots takes the body's percentage. | In-game record (cap shown against the game). | [0.312-materia](../evidence/0.312-materia.md), « Non vérifié en jeu » |
| D-140 | X-172 : cache des données téléchargées laissé en fichiers (essai docs/evidence/x172-sqlite-trial.md : écriture 4 fois plus lente en SQLite, lecture un peu plus lente, même taille, place rendue seulement après VACUUM) ; les découvertes passent en SQLite | Cache en fichiers comme aujourd'hui (un seul fichier SQLite, celui des découvertes) |  |
<!-- /gen:decisions:en -->

## In-game tests

The current list: the boxes of [TESTING.md](TESTING.md). The guided in-game campaign of the whole plugin is phase 6 of the
[global plan](GLOBAL-PLAN.md). Records still asked for (data unknown, see « To decide »): marker type of an accepted leve; quest icon
variants 71141 / 71151.

## Complementary domain

Combat/ACT/IINACT: removed from the project (user decision 2026-10-31).

---

# Feuille de route des 17 plugins

Depuis le 2026-11-09, le plan de travail vit maintenant dans le [plan global](GLOBAL-PLAN.md), et l'historique de chaque
version dans [CHANGELOG.md](../../CHANGELOG.md). Ce fichier garde la liste qui fait foi des 17 plugins de référence et ce que Tensho
couvre de chacun, les demandes de l'utilisateur R-01 à R-14, les pages ajoutées hors des 17 plugins, les questions ouvertes et les
décisions prises en route. Les 17 lignes restent PARTIAL : aucune validation en jeu complète. Diffusion : le dépôt personnalisé, voir
[PUBLISHING](PUBLISHING.md) (pas de soumission au dépôt officiel, DEC-039).

La file d'étapes a) à h) (2026-11-02 au 2026-11-04) et les notes de fondation (2026-10-27 au 2026-10-28) étaient de l'historique ; elles
ont été retirées de ce fichier le 2026-11-08 et restent lisibles dans git (ce fichier au commit `2da535d`, 0.865.0).

Origine : le prompt maître, §4 (notes du mainteneur, hors du dépôt ; source historique depuis le 2026-11-09, les décisions de
l'utilisateur et [DECISIONS](DECISIONS.md) priment). Toutes les lignes sont
conservées. State décrit le travail ; Coverage décrit la capacité réellement validée. TODO/READY/ACTIVE/BLOCKED/DONE ;
NOT_STARTED/PARTIAL/FUNCTIONAL/REPLACEABLE. NEEDS_STUDY est une note d'incertitude, pas une suppression. Aucun pourcentage arbitraire.

## Matrice principale

La colonne « Travail » a été écrite au 0.315 (2026-11-02) et complétée jusqu'au 0.512 ; la suite est dans le
[CHANGELOG](../../CHANGELOG.md) et le [plan global](GLOBAL-PLAN.md). « Validé » = rapport ou capture de l'utilisateur ; les autres
versions citées sont à valider en jeu. Les fiches d'objectifs que citaient ces lignes (`docs/goals/`) sont des fichiers de l'agent, hors
du dépôt.

| # | ID | Plugin | Auteur(s) | Description | State | Coverage | Travail | Preuves | Groupe |
|---|---|---|---|---|---|---|---|---|---|
| 1 | FASHION-001 | Avant-Garde | NeNeppie | Équipements valides associés aux indices de la Revue de mode | ACTIVE | PARTIAL | Répertoire manuel, pièces signalées, filtres/stock/contexte et provenance testés ; boutons sur les cases de la fenêtre du jeu et teintures de la semaine validés en jeu (0.213–0.223) ; « possédé » avec arsenal, coffre à apparence et armoire lus dans le jeu (0.235–0.240, à valider) ; « Essayer » (cabine d'essayage) au clic droit de tout équipement (0.310, validé). Score par pièce non fait : aucune donnée (décision du 2026-11-01) | — | C |
| 2 | BLU-001 | Blue Mage Helper | Infi, Sl0nderman, harbingerftw | Grimoire du mage bleu : sorts, acquisition et progression | ACTIVE | PARTIAL | Grimoire natif, numéros distincts des identifiants, acquisitions confirmées ; compilation/tests verts, rendu en jeu à confirmer | — | C |
| 3 | CHAR-001 | Character Panel Refined | Kouzukii | Statistiques utiles du personnage | ACTIVE | PARTIAL | Attributs natifs copiés et invalidés ; valeurs réelles de combat (`Character/StatFormulas.cs` : ParamGrow du jeu, formules de la communauté, modificateur principal repris comme données de Character Panel Refined, MIT) dans un panneau à côté de la fenêtre Personnage du jeu, validées en jeu (0.182–0.184) ; reprises pour le plan dans la page Équipement (R-11, 0.310, validée) | [sonde ParamGrow](../evidence/0.182.0-paramgrow-probe.log) | D |
| 4 | COLL-001 | Collections | Seventh | Découverte, progression et interactions avec les collections | ACTIVE | PARTIAL | Montures/mascottes/emotes/cartes/orchestrion/titres/succès ; catégories/fiches, récompenses et contexte, acquisition à trois états pour les listes dédiées ; état obtenu/non obtenu dans l'infobulle des objets, clic droit vers la collection et « Comment l'obtenir » (marchands, échanges, quêtes, succès, artisanat, récolte, puis Garland Tools et le wiki) validés en jeu (0.146). Pastille ✓/✗ sur toute icône d'objet de collection (0.275) et cartes/rouleaux dans « Non obtenus seulement » (0.241), à valider ; cartes Ex. de Triple Triade (0.278, validé). Autres types et sources inconnues restantes à compléter | — | B |
| 5 | CRAFT-001 | Craftimizer | Asriel | Simulation d'artisanat, macros assistées et suggestions pendant la fabrication | ACTIVE | PARTIAL | Recettes/préparation validées ; simulation/export/recherche/séquences, projection ordinaire et aide macro près du carnet du jeu (sélection, attributs, comparaison et copies) compilées/testées ; objets de collection ordinaires/HQ initial et priorité aux attentes intégrés ; copie des macros dans les macros du jeu, prévision des emplacements et enchaînement validés en jeu (0.191–0.196) ; rotation adaptative (A-04, 0.509) et solveur Raphael facultatif (A-03, 0.465) ajoutés ensuite ; recettes spéciales à compléter | — | A |
| 6 | DUTY-001 | Duty Loot Preview | grittyfrog | Aperçu du butin des contenus | ACTIVE | PARTIAL | Catalogue et consultation manuelle, cache/annulation, provenance et contexte testés ; panneau à côté de l'outil de mission du jeu (0.185–0.186, sélection validée), butin des contenus récents par LuminaSupplemental (0.188–0.190, validé), butin ✓/✗ et mission aléatoire (0.207–0.208, validés) ; cartes et rouleaux ✓/✗ (R-02, 0.241, à valider) ; données partielles | — | C |
| 7 | CACT-001 | ezMiniCactpot | daemitus, Minmoose, MidoriKami | Meilleur choix au Mini Cactpot | ACTIVE | PARTIAL | Solveur manuel exact ; mode observé sur option et recalcul par ticket, worker propriétaire/génération ; solution superposée à la fenêtre du jeu `LotteryDaily` (0.158), validée en jeu (rapport du 2026-11-01) | — | D |
| 8 | GATHER-001 | GatherBuddy | Ottermandias | Récolte et pêche | ACTIVE | PARTIAL | Sources/cartes, observation MIN/BTN sur option, carnet et appâts/conditions explicites ; météo localisée et calcul borné des critères renseignés. Panneaux d'aide à la récolte et à la pêche en jeu (0.225–0.233), données de pêche de GatherBuddy d'abord puis Garland (0.227), anciens fichiers GatherBuddy (0.229, validé), temps de touche Teamcraft et chrono (0.231, validé), alarmes de pêche avec météo (0.253–0.254) ; calendrier de la pêche en mer et aide pendant la traversée (0.392 et suivantes) ; le reste à valider | [coordonnées des lieux](../evidence/0.226-fishing-spot-coordinates.md), [temps de touche Teamcraft](../evidence/0.231-teamcraft-bite-times.md) | A |
| 9 | USE-001 | Item Use | PunishedPineapple | Usages courants d'un objet | ACTIVE | PARTIAL | Onglet, survol sur option et menu Inventory (0.207, validé) ; recettes/mandats/GC/aquarium/orchestrion et sources de quêtes ; onglet Usages de la fiche objet (0.272) ; menu Tensho de l'inventaire du jeu et panneau sous son infobulle alignés sur Tensho (0.256–0.257, à valider) ; autres usages/disponibilité/liens à compléter | — | C |
| 10 | GP-001 | LazyGatherer | Hiroa | Optimisation des PR de récolte | ACTIVE | PARTIAL | Optimiseur manuel avec familles déjà payées et bonus limité ; observation/copie des ressources (testés à l'époque) ; bonus de Récolte abondante II prérempli par une règle de la communauté citée, affiché « estimé » et modifiable (0.224, à valider) ; décodeur natif/régénération/procs/objets de collection et rendu en jeu à compléter | [base d'Abondance](../evidence/0.224-bountiful-base.md) | D |
| 11 | MACRO-001 | Macro Chain | Caraxi | Exécution de macros successives | ACTIVE | PARTIAL | Plan persistant, protocole de marqueur de fin, arrêt/identité et tests managés ; plan rempli après copie des macros dans le jeu, et enchaînement, validés en jeu (0.196, « macro chain fonctionne ») | — | D |
| 12 | MAP-001 | Mappy | MidoriKami, harbingerftw | Carte en jeu polyvalente | ACTIVE | PARTIAL | Carte Tensho remplaçant la carte du jeu (M, quête du journal, téléporteurs) : icônes du jeu à taille/priorité par carte, quêtes et mandats, ALÉA, groupe, récolte/pêche à horaires, courants, exploration (météo du wiki), marchands, drapeau, téléportation au coût réel, listes par onglet. Validé en jeu : 0.100–0.156 (dont carte hôte hors écran, zones colorées, zoom, commises de Rowena), variantes de carte (0.171–0.175), icônes de quêtes (0.211), réglages et cône de vision (0.287–0.289), repères au bord de la carte et « Retirer le drapeau » (0.297–0.299), menus et outils par-dessus la carte (0.303–0.306). À valider : R-01 (0.242–0.243), R-03 (0.244), R-04 (0.245), fond du zoom (0.308). Inconnus : type de marqueur des mandats, sens des variantes d'icônes de quête | [icônes d'événements](../evidence/0.211-event-icons.md) | C |
| 13 | LOOT-001 | Monster Loot Hunter | Sylf | Sources de matériaux sur les monstres | ACTIVE | PARTIAL | Matériau/monstre/zone, fiches, cache/cadence/annulation et contexte testés ; LuminaSupplemental (MobDrop) en tête (0.190, validé), puis le wiki FFXIV et Garland pour les lieux ; données partielles | — | B |
| 14 | MARKET-001 | Price Insight | Kouzukii | Prix du marché au survol d'un objet | ACTIVE | PARTIAL | Consultation manuelle et survol sur option NQ/HQ/monde, accroché à l'infobulle, stock et moins cher du centre de données validés en jeu (0.85–0.86) ; page Marché enrichie (R-06 étape 4 : annonces Universalis, filtres, coût pour N unités, historique, tri des colonnes) validée en jeu (0.270–0.271) ; repli sur le cache disque si Universalis échoue (0.281, à valider) | [annonces Universalis](../evidence/0.270-universalis-listings.md) | B |
| 15 | QUEST-001 | QuestTracker | isaiahcat, keifufu | Progression et complétion des quêtes | ACTIVE | PARTIAL | Fiches/liens/niveaux/zones/carte, récompenses natives conditionnelles et index inverse Objet → quête hors du dessin ; progression observée jointe aux sources d'un objet (0.76, validé) ; types de quêtes des sections du journal, pourcentages, cités de départ et options non prises (0.283–0.286, validés). Monnaies/rangs non prouvés ; disponibilité à compléter | [sections du journal](../evidence/0.283-quest-journal-sections.md), [cités de départ](../evidence/0.285-quest-start-cities.md), [options](../evidence/0.286-quest-alternatives.md) | B |
| 16 | SORT-001 | SortaKinda | MidoriKami | Personnalisation de l'inventaire, règles, aperçu et application | ACTIVE | PARTIAL | Profils et modèles réutilisables, critères multiples, zones protégées/filtrées, remplissage dans les deux sens et application visuelle après capture directe ; tri natif validé en jeu (2026-10-30). Arsenal lu comme rangement (0.235, à valider) mais pas trié : tri de l'arsenal et tri automatique laissés pour plus tard (I-14) | — | A |
| 17 | TRIAD-001 | TriadBuddy | MgAl2O4 | Solveur de Triple Triade | ACTIVE | PARTIAL | Carnet/deck/solveur manuel testés ; TRIAD-003 terminé : conseil superposé en match (0.160), % par deck et génération de deck à la fenêtre de défi (0.161), decks Tensho (0.162) validés en jeu ; carnet (menus, valeurs, tri, score estimé, cartes Ex.) validé (0.265–0.269, 0.276–0.279). Règles natives non couvertes | [cartes Ex.](../evidence/0.278-triad-ex-cards.md) | C |

Les ID métier désignent la capacité globale, découpée en petits objectifs au moment de l'activation. CRAFT-001 commence par le
catalogue, puis simulation, macros et suggestions ; GATHER-001 par les lieux, puis horaires/météo/pêche ; SORT-001 par l'aperçu avant
toute application native. Les autres capacités suivent le même principe, sans déclarer le plugin couvert dès la première tranche.

## Demandes utilisateur R-01 à R-14

Ce tableau est la seule liste des demandes R-01 à R-14 (leur libellé détaillé était dans USER-REQUESTS, retiré en 0.1088 ; l'historique git le garde) ; une nouvelle demande va dans le [plan global](GLOBAL-PLAN.md) comme point X-NN ([IDS](IDS.md)). « Validé » = rapport ou capture de l'utilisateur ; sinon, à valider en jeu. Les
plans R-06 et R-11 sont des fichiers de l'agent (`docs/goals/`), hors du dépôt.

| N° | Sujet | Fait | Validé en jeu | Reste |
|---|---|---|---|---|
| R-01 | Carte : cible de quête du journal qui restait | 0.242 (retrait au clic droit), 0.243 (retrait quand la quête n'est plus active) | non | — |
| R-02 | Butin des contenus : cartes et rouleaux ✓/✗ et filtre « Non obtenus » | 0.241 | non | — |
| R-03 | Carte : onglet « Triple Triad » au clic droit (PNJ de la zone) | 0.244 | non | — |
| R-04 | Carte : clic droit sur une icône = son propre menu | 0.245 ; bouton souris des réglages d'icônes 0.298 | 0.297–0.299 (« Retirer le drapeau » dans ces menus, bouton souris) | — |
| R-05 | Ordre des fenêtres | clos sans code (2026-11-01, [preuve](../evidence/0.246-r05-window-order.md)) | — | — |
| R-06 | Refonte de l'interface | étapes 1 (0.247–0.263), 2 (0.264), 4 (0.270–0.271) ; 3 pour l'essentiel (fiche objet 0.272, Rangements 0.280) ; 5 en partie (↺ 0.249–0.262, « Tout remettre par défaut » 0.258, infobulles de tous les réglages 0.287, réglages de carte 0.288–0.289) ; 6 : légèreté (0.290–0.292) et police de titre du jeu (0.293–0.294) | 0.265–0.271, 0.276–0.279, 0.287–0.294 | réglages utiles manquants (étape 5, point d) ; cadres et textures du jeu (étape 6, retirés en 0.510, DEC-033) ; `FilteredTable` retiré du plan (DEC-023) |
| R-07 | Compagnie libre | page et expéditions 0.307, meilleurs trajets 0.309, montages 0.315 ; suite DEC-023 faite : secteurs signalés 0.334, prix du marché et paliers 0.335, dirigeables 0.336–0.337 ; historique 0.379 | non | à valider en jeu |
| R-08 | Rappels | page 0.300, icône et infobulle dans la barre du serveur 0.300–0.302, expéditions 0.307, escouade 0.333 ; puis étape g (M-01 à M-05, N-01 à N-03) | non | Planeurs d'Ehcatl laissés (rien de lisible, 0.334) ; à valider en jeu |
| R-09 | Événements saisonniers | page Événements (0.342) : en cours, quêtes, boutiques, noms | 0.342 | à valider ; pas de dates à venir (aucune source fiable) |
| R-10 | Audit des caches | 0.246 ([audit](../evidence/0.246-cache-audit.md)), revérifié 0.281 (durée « Jamais ») | — | — |
| R-11 | Équipement | étape 1 (0.310), mirage (0.311), matérias et plafonds (0.312–0.313), sources et possession (0.314), montages de vaisseaux (0.315) | 0.310–0.314 | 0.315 et 0.340 à valider ; étape 4 : barde de chocobo FAITE 0.340, page Chocobo 0.341 (escouade retirée, défaut recommandé, décision de la fin) |
| R-12 | Webhook Discord pour les alarmes | 0.319 | non | à valider en jeu ; utilisé ensuite par C-10, I-02, M-01 |
| R-13 | Page « Crédits » complète dans le plugin | 0.320 | non | à valider en jeu |
| R-14 | Résistance aux mises à jour de FF14, dans la logique du code | règle pour le nouveau code (0.319) ; audit et corrections 0.452–0.454, 0.491–0.492, 0.507 ; auto-contrôle 0.494–0.496 | par l'auto-contrôle, pour ce qu'il lit | — |

## Pages hors des 17 plugins

Toutes les pages du code, par groupe, sont dans le tableau du [README](../../README.md) (« Vue d'ensemble »), écrit depuis la liste
des pages (`MainWindow.Pages.cs`) par `tools/docgen.py` à chaque version. La page Personnage a été retirée en 0.341 (la page
Équipement montre les mêmes valeurs réelles ; le panneau à côté de la fenêtre Personnage du jeu reste). Depuis la 0.882, la page
Découvertes échange aussi les relevés en jeu des joueurs par GitHub, sur clic du joueur ou par l'échange quotidien sur option (aucune
donnée prise sur des sites web).

## À décider / à revoir

<!-- gen:decisions:fr -->
Points ouverts, produits par script depuis la source des décisions de l'agent. Choix déjà faits : [DECISIONS](DECISIONS.md) ; anciens tableaux : [archive](archive/DECISIONS-EN-ATTENTE-2026-11-09.md).

| N° | Point | Choix appliqué, ou relevé attendu | Où |
|---|---|---|---|
| D-83 | Prise en main, aide, vidéos, tickets : Les 8 lots codés (0.502 à 0.506), DEC-031 | Validation en jeu ; une release pour la liste de vidéos | ONBOARD-001 |
| D-84 | Modèles de la communauté : Terminés côté code en 0.476 | Validation en jeu ; galerie visible après une release | TEMPLATES-001, [DEC-029](DECISIONS.md) |
| D-85 | Règle « coût ≤ capacité » sous le rang 50 : Règle des outils de la communauté. Cohérente avec 11 montages de dirigeables publiés ; non vérifiée en jeu sous le rang 50. | Relevé en jeu sur un vaisseau de rang inférieur à 50. | [0.315-vessel-builds](../evidence/0.315-vessel-builds.md), « Coût ≤ capacité » |
| D-87 | Icône du drapeau 60561 : L'identifiant de l'icône du drapeau n'est pas prouvé dans docs/evidence (la pose du drapeau, elle, est validée en jeu). | Relevé en jeu : drapeau posé, `AgentMap.FlagMapMarkers[0].MapMarker.IconId` lu sur le framework (sonde), journal dans docs/evidence. | `MainWindow.MapView.cs` (`FlagIcon`) ; CHANGELOG 0.102 (« supposée ») ; audit de la carte 0.328 |
| D-88 | Icônes de quête 71141 / 71151 : Le sens des variantes n'est pas dans les données du jeu : les réglages affichent « variante n ». | Relevé en jeu, sinon garder « variante n ». | BACKLOG (retiré en 0.1088, historique git) ; [0.211-event-icons](../evidence/0.211-event-icons.md) |
| D-89 | Bonus d'Abondance estimé (0.224) : Règle de la communauté non officielle, affichée « estimé » et modifiable. | À comparer en jeu avec le bonus réel. | BACKLOG, « Revue des points ouverts » (retiré en 0.1088, historique git) ; [0.224-bountiful-base](../evidence/0.224-bountiful-base.md) |
| D-90 | Matérias : pièce sur plusieurs emplacements : Une pièce qui couvre plusieurs emplacements prend le pourcentage du torse. | Relevé en jeu (plafond affiché contre le jeu). | [0.312-materia](../evidence/0.312-materia.md), « Non vérifié en jeu » |
| D-140 | X-172 : cache des données téléchargées laissé en fichiers (essai docs/evidence/x172-sqlite-trial.md : écriture 4 fois plus lente en SQLite, lecture un peu plus lente, même taille, place rendue seulement après VACUUM) ; les découvertes passent en SQLite | Cache en fichiers comme aujourd'hui (un seul fichier SQLite, celui des découvertes) |  |
<!-- /gen:decisions:fr -->

## Tests en jeu

La liste à jour : les cases de [TESTING.md](TESTING.md). La campagne guidée en jeu de tout le plugin est la phase 6 du
[plan global](GLOBAL-PLAN.md). Relevés encore demandés (données inconnues, voir « À décider ») : type de marqueur d'un mandat accepté ;
variantes d'icônes de quête 71141 / 71151.

## Domaine complémentaire

Combat/ACT/IINACT : retiré du projet (décision de l'utilisateur, 2026-10-31).
