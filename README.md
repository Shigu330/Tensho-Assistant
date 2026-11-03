# TenshoAssistant

TenshoAssistant is a Dalamud plugin for Final Fantasy XIV that brings everyday tools together in one window, opened with `/tensho`:
items and storage, inventory sorting, market prices, crafting, gathering and fishing, a map that replaces the game map, weather, alarms
and reminders, free company voyages, quests, collections, character stats, a gear planner, Gold Saucer helpers, macro chains and
diagnostics.

The current version is the `<Version>` of `src/TenshoAssistant/TenshoAssistant.csproj`. What changed in each version is in
[CHANGELOG.md](CHANGELOG.md); what is validated in game, and what is not yet, is in [docs/ai/STATUS.md](docs/ai/STATUS.md). Map of all
documents: [docs/README.md](docs/README.md).

## Overview

The sidebar groups the pages in foldable categories (`MainWindow.cs`):

| Category | Pages |
|---|---|
| — | **Home**: ten tiles with a live line (crafting, gathering, collections, quests, map, market, alarms, macro chain, character, Gold Saucer), work in progress, next alarm, the context item, tool directory |
| Items | **Items** (search and item sheet), **Storage**, **Inventory sorting**, **Market** |
| Production | **Crafting**, **Gathering**, **Macro chain** |
| World | **Map**, **Weather**, **Alarms**, **Reminders**, **Free company**, **NPC vendors**, **Duty loot**, **Monster loot** |
| Progression | **Quests**, **Collections** (with Blue magic), **Character**, **Gear** |
| Gold Saucer | **Fashion Report**, **Triple Triad**, **Mini Cactpot** |
| Preferences | **Settings**, **Diagnostics** (developer mode only) |

Chat commands: `/tensho` opens the window; `/tensho fish` (also `fishing`, `peche`, `pêche`) opens or closes the fishing help;
`/tenshonext` is the end-of-page marker of macro chains. Dalamud's « open » and « settings » buttons open the window and its Settings page.

## Architecture

- `src/TenshoAssistant/Plugin.cs` registers the services, the command, the windows and the panels; every registration has its cleanup.
- `MainWindow.cs` (split into `MainWindow.<Page>.cs` partial files) is the shell and page routing; `MapViewWindow.cs` is the Tensho map.
  Both are Dalamud `WindowSystem` windows.
- One folder per domain (`Alarms`, `Caching`, `Character`, `Collections`, `Community`, `Craft`, `Duties`, `Fashion`, `FreeCompany`,
  `Gathering`, `Gear`, `Inventory`, `Items`, `Localization`, `Macros`, `Maps`, `Market`, `MiniGames`, `Monsters`, `Npcs`, `Quests`,
  `Reminders`). Files ending in `*.Dalamud.cs` touch the game (read on the framework thread, then copied); the others are pure C# and tested.
- `UI/Components/` holds the shared templates: `ItemCell` (item icon and name with the common tooltip and tools menu), `DetailPage`,
  `SettingRow` (tooltip and « restore default » ↺ button), `EntityMenus` (quest, NPC, monster and place menus), `DataIndexes`.
- `Community/` holds the bounded HTTP clients (size and time limits); `Caching/` the disk cache, with a duration per data area.
- Interface text lives in JSON language files embedded in the DLL (`Localization/Languages`).

## Main feature domains

What the code does today. In-game validation of each part is tracked in [docs/ai/STATUS.md](docs/ai/STATUS.md).

### Items, storage and market

- **Item sheet**: search, then header (stock, last price) and tabs Overview, Stocks, Craft (Produced by / Used in), Usages, Gathering,
  Market; only the tabs with content are shown.
- **Every item icon**, in every module: the common item tooltip and a right-click tools menu with the sources grouped by kind
  (achievements, duties, other items, exchanges, NPC vendors, quests), « Try on » for equipment, and the obtained ✓ / not obtained ✗
  mark on collectibles. Grand Company seal sources only show your own company.
- **In the game**, each an option: a Tensho entry in the inventory right-click menu, a panel under the inventory item tooltip (uses and
  how to get it), market prices on hover (off by default).
- **Storage**: Bank (retainers, saddlebags, premium saddlebag, armoury chest, glamour dresser, armoire: each known once opened in game),
  Crystals, Imports; retainers and saddlebags saved when opened (setting), otherwise an import button beside the game's window.
- **Inventory sorting**: profiles, templates, rules and protected zones with a preview; the sort is applied only on click.
- **Market** (Universalis): Summary, Listings (world / data centre / region, quality, quantity and world filters, cheapest set of whole
  listings for N units, tax included) and Sale history; cached on disk, the last known board is shown, dated, when Universalis fails.

### Crafting and macros

- Recipe catalogue, crafting simulator and rotation search, live crafting context, presets, craft chain and preparation cost.
- Macro export, and copy into the game's macros on click (book, first / last slot, prefix, overwrite); macro helper beside the game's
  crafting log.
- **Macro chain**: macro pages ending with `/tenshonext`, started explicitly, can be stopped.

### Gathering and fishing

- **Gathering** page: resources and zones, timed nodes with Eorzea time windows, GP planning with an estimated Bountiful Yield II bonus
  (community rule, editable).
- Fishing: bait, mooch, windows and weather from GatherBuddy (Garland Tools as fallback), bite times from Teamcraft reports, fishing
  notebook and forecast; fishing alarms carry the weather conditions. Ocean fishing is not covered (no source).
- In-game help panels: beside the gathering window (miner / botanist: items, boon, stock, estimated Bountiful, advised GP plan) and
  while fishing (fish of the nearest spot with bait, window and weather; `/tensho fish` opens it by hand).

### Map, weather, alarms and reminders

- **Tensho map**, in its own window, replaces the game map (key M, setting): game icons with size and priority per map, quests and
  leves, FATEs, party, gathering and fishing points with their times, aether currents and sightseeing vistas (collected ones hidden),
  NPC vendors, Triple Triad NPCs, flag (place and remove from every menu), teleport with the real gil cost, bookmarks, local lists by tab
  (quests, FATEs, gathering, aetherytes, vendors, Triple Triad, vistas, currents), each icon's own right-click menu.
- **Map options** (Settings › Map), each with a tooltip and ↺: menus and tools drawn over the map (off by default) with the zone name
  always shown; markers on the map edge for off-view targets (your character, flag, journal target, quests, leves and events, FATEs,
  open timed nodes, party), each kind switchable; position marker size, view cone length, opacity and colour; area colours; hide in
  combat or duty; icon sizes and priorities.
- **Weather**: forecasts by zone.
- **Alarms**: Tensho alarms (once at a date, or every Eorzea day at an ET time; weather conditions for fish), chat line and the game's alarm sound, optional copy into
  the game's alarm list through `/alarm` (off by default). Alarm clock in the game's server info bar with the time left before the next
  alarm and Tensho's own tooltip (Settings › General). Optional Discord webhook (off by default): ringing alarms are posted in a
  channel of your server, with a message per category or per alarm group, an optional role mention and a test button.
- **Reminders**: the game calendar timers (supply missions, custom deliveries, Fashion Report, allied society quests, leve allowances,
  treasure maps with a « map opened » button, retainer ventures, company voyages) with resets, counters read from the game, « done »
  boxes and alarm buttons. Not yet: Grand Company squadron, Ehcatl deliveries.

### Free company

- **Vessels**: submarines and airships read in the company workshop (every 3 s while you are there) and kept: rank and experience,
  route, return time, last voyage finds, return alarm.
- **Best routes** (submarines): every 1–5 sector route the rank and range allow, ranked by expected loot value per voyage or per day, or
  by experience per day; unlocked-sectors filter. Loot averages are community data (FFXIVGachaSpreadsheet, embedded); values are NPC
  sell prices. No airship planner: no public loot data under a compatible licence.
- **Builds**: submarine and airship builds (read / planned / difference stats, cost vs capacity, part rank, repair), a check of read
  vs computed stats, route check (range, duration, breakpoint tiers, wear estimate) and best builds for a route.

### Quests, collections and progression

- **Quests**: the game journal's quest kinds and genres with completion percentages; quests of the other starting cities and options
  not taken are hidden and left out of percentages (a box shows them); followed quests, rewards, item sources; quest menu with flag,
  Tensho map and zone weather.
- **Collections**: mounts, minions, emotes, orchestrion, titles, achievements and Blue magic, with the obtained state and how to obtain
  each entry (vendors, exchanges, quests, achievements, crafting; Garland Tools and the wiki as fallbacks).
- **Character**: the game's attributes, and a real stats (%) panel beside the game's Character window (setting).
- **Gear** planner: worn gear, one plan per job, stats read / planned / difference, candidates near your level with filters, « All jobs
  (glamour) » filter, materia sockets and editor with the per-piece caps, owned ✓ / ✗ and « pieces to obtain » with their sources,
  « Try on » (the game's Try On window), sharing and importing a `TENSHO-GEAR1:` code.
- **Duty loot** (also as a panel beside the Duty Finder), **Monster loot**, **NPC vendors** (free company credit shops included).

### Gold Saucer

- **Fashion Report**: weekly theme hints, owned pieces first (glamour dresser and armoire included), panel beside the game's window.
- **Triple Triad**: card notebook (ordinary and Ex. cards, values, sorts, rule-based score), decks, simulation, NPCs and cards (NPCs to
  face, packs to buy, missing cards), match advice (opt-in observation).
- **Mini Cactpot**: exact solver, advice drawn on the game's ticket.

### Settings, diagnostics and localization

- Settings tabs: General (opening, header clock, alarm bar), Interface (language, theme, item hover and menu options), Modules, Map,
  Data (in-game panels, automatic storage save, cache durations including « Never (on request only) »), Advanced (« Restore all
  defaults », developer mode, community sources on / off, hover prices, game command delay), Debug (developer mode). Each setting has a
  tooltip and a ↺ restore-default button.
- Diagnostics (developer mode): draw time of each page (median and maximum of the last 120 frames) and a contributor panel (documents,
  source file of the shown page, data sources, probes, translation completeness).

## Repository structure

```text
.
├── src/TenshoAssistant/      plugin code (one folder per domain; *.Dalamud.cs = game access; UI/Components = shared templates)
├── tests/                    managed tests (pure logic)
├── tools/                    read-only data probes (game sheets, APIs) and data scripts
├── scripts/                  Verify.ps1 (build + tests + localization + package), Release.ps1 (git steps)
├── commit-release.bat        Verify -Commit then -Release (maintainer)
├── repo.json                 Dalamud custom repository entry (refreshed by each release)
├── publish/d17/              draft submission for the official Dalamud repository
├── docs/                     documentation map: docs/README.md
│   ├── ai/                   agent steering (rules, status, goal)
│   ├── project/              scope, roadmap, architecture, testing, references, decisions, publishing
│   ├── goals/                one document per goal
│   └── evidence/             probe outputs cited by the code
├── Localization/README.md    translation guide
├── README.md · CONTRIBUTING.md · SECURITY.md · CHANGELOG.md · CLAUDE.md · LICENSE
└── global.json · TenshoAssistant.sln
```

## Build and verification

Requirements: Windows, .NET SDK 10.0.403 (`global.json`), Dalamud.NET.Sdk 15 (restored from NuGet during the build; it compiles against your Dalamud install, or `DALAMUD_HOME`). Without the game: `.\scripts\Verify.ps1 -TestsOnly` (tests and languages only).

```powershell
.\scripts\Verify.ps1                                 # Release build, managed tests, language files, package
.\scripts\Verify.ps1 -DotNet 'C:\path\to\dotnet.exe' # another SDK location
.\scripts\Verify.ps1 -Force                          # run every check again even if this exact state already passed
```

The run must end green with no warnings; it prints `VERSION x.y.z (DLL x.y.z.0)`. Outputs:

- DLL: `src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`
- Package: `src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip`

`-Commit`, `-Release` and `-DryRun` (maintainer only) are described in [CONTRIBUTING.md](CONTRIBUTING.md#versions-and-releases-maintainer).

## Installation in-game

Released versions: add `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json` to Dalamud › Settings ›
Experimental › Custom Plugin Repositories, then install « Tensho Assistant » from the plugin installer. Development builds:

1. Add the absolute DLL path to the Dalamud dev plugins list.
2. Load the plugin.
3. Open the window with:

```text
/tensho
```

4. Validate preferences, reload behavior, and unloading behavior.

## Localization

Rules: [Localization/README.md](Localization/README.md). In short:

- copy `src/TenshoAssistant/Localization/Languages/en.json`, rename it with the language code, translate the values;
- keep the translation keys unchanged; English is the reference and the fallback for missing keys;
- English and French are complete; German and Japanese only have a few keys;
- since 0.248 the languages are embedded in the DLL; a `Localization/Languages` folder beside the installed DLL overrides them for testing.

## Documentation to consult

- [docs/README.md](docs/README.md) — map of all documents
- [CONTRIBUTING.md](CONTRIBUTING.md) — build, tests, project rules, Dalamud publishing rules
- [CHANGELOG.md](CHANGELOG.md) — what changed in each version
- [docs/ai/STATUS.md](docs/ai/STATUS.md) — current status, what is validated in game, next action
- [docs/project/ROADMAP.md](docs/project/ROADMAP.md) · [ARCHITECTURE.md](docs/project/ARCHITECTURE.md) · [TESTING.md](docs/project/TESTING.md) · [REFERENCES.md](docs/project/REFERENCES.md)
- [Localization/README.md](Localization/README.md) — localization workflow
- [SECURITY.md](SECURITY.md) — reporting a vulnerability

## AI usage disclosure

This plugin is developed with an AI coding agent (Claude, « Auto » level) under the direction of its author, who defines the features and
tests every version in game. Data links are proven with read-only probes ([docs/evidence/](docs/evidence/)). See
[CONTRIBUTING.md](CONTRIBUTING.md#publication-dalamud) for the Dalamud AI policy. The plugin icon (`src/TenshoAssistant/images/icon.png`)
was also generated by the AI agent.

## Credits

The complete list, with every licence and what each source is used for, is the plugin's **Credits** page (Preferences › Credits,
72 entries, `src/TenshoAssistant/Credits/CreditCatalog.cs`, kept in step with the code by tests); this section summarizes it.

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd. FINAL FANTASY XIV © SQUARE ENIX CO., LTD. All rights reserved.
Tensho Assistant is not affiliated with nor endorsed by SQUARE ENIX.

Game data comes first: every list is read from the game's own sheets and memory when they have it. Community data is used only when the
game does not have it, always cited.

Framework and tools:

- Final Fantasy XIV and its game data (sheets read through Dalamud / Lumina), Dalamud and FFXIVClientStructs
- Dalamud.NET SDK, .NET SDK and runtime, PowerShell scripts for verification and release

Community data used by the plugin (licence compatible with GPL-3.0, or service API):

- [LuminaSupplemental](https://github.com/Critical-Impact/LuminaSupplemental) by Critical-Impact (GPL-3.0): duty loot and item sources, downloaded at runtime; airship unlock tree and possible loot, embedded with its notice (0.336)
- [GatherBuddy](https://github.com/Ottermandias/GatherBuddy) by Ottermandias (Apache-2.0): fishing bait, mooch, bite and window data, downloaded at runtime as data
- [Teamcraft](https://github.com/ffxiv-teamcraft/ffxiv-teamcraft) (MIT): fishing bite times from its public reports
- [FFXIVGachaSpreadsheet](https://github.com/Infiziert90/FFXIVGachaSpreadsheet) by Infi (MIT, crowd-sourced): Fashion Report data (downloaded at runtime) and submarine loot averages (embedded snapshot, `tools/submarine-loot.py`)
- [SubmarineTracker](https://github.com/Infiziert90/SubmarineTracker) by Infi (MIT): submarine sector breakpoints embedded (notice in
  `src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv`), originally from the community « Submersible Loot Spreadsheet »
  (Elena / saerielle); voyage formulas understood by reading its code (original code); wear estimate from the community « Submersibles
  durability damage » sheet that it quotes
- level modifier table (main stat, levels 1–100) from Character Panel Refined by Kouzukii (MIT, full notice in `Character/StatFormulas.cs`)
- [Universalis](https://universalis.app/) (market prices and listings), [Garland Tools](https://www.garlandtools.org/) (item, duty and
  monster sources), the [FFXIV Console Games Wiki](https://ffxiv.consolegameswiki.com/) (monster drops, sightseeing log, acquisition)

Read to understand, never copied (facts only, original code):

- Mappy and SortaKinda (AGPL-3.0-or-later), and the other reference plugins listed in [docs/project/ROADMAP.md](docs/project/ROADMAP.md)
- Craftimizer (writing macros into the game, functional reference)
- HimbeertoniRaidTool (MIT) and Glamourer (Apache-2.0) for the gear planner; xivgear.app (no published licence: read only)
- Argus (licence not confirmed) for vessel builds; its airship breakpoint data comes from unlicensed sources and is not used
- frozen_rabbit_tome (no licence): the Bountiful Yield II rule, taken as a fact with original code and shown as an estimate (a rule, no text copied)

This project is an original plugin implementation, but it relies on these ecosystem components and references for its operation, testing, and validation.

## Notes

- The project is a full-feature FFXIV utility plugin and not only an item tracker.
- Unknown is not zero: when a value is not known (storage not opened yet, no community data), the plugin says so instead of guessing.
- Exact module coverage should always be verified against the project documentation rather than inferred from older references.

## Summary

TenshoAssistant is a multi-tool Dalamud plugin intended to centralize useful gameplay support surfaces into a single interface. The current codebase demonstrates a modular architecture with shared catalogs, domain services, and a rich main window that spans inventory, gathering, crafting, quest support, collections, market information, and more. The project documentation remains the definitive reference for maturity, scope, and current status of each feature area.

---

# TenshoAssistant

Version française / French version

TenshoAssistant est un plugin Dalamud pour Final Fantasy XIV qui réunit dans une seule fenêtre, ouverte par `/tensho`, des outils du
quotidien : objets et rangements, tri de l'inventaire, prix du marché, artisanat, récolte et pêche, une carte qui remplace celle du jeu,
météo, alarmes et rappels, expéditions de la compagnie libre, quêtes, collections, statistiques du personnage, planificateur
d'équipement, aides du Gold Saucer, enchaînement de macros et diagnostics.

La version courante est le `<Version>` de `src/TenshoAssistant/TenshoAssistant.csproj`. Les changements de chaque version sont dans
[CHANGELOG.md](CHANGELOG.md) ; ce qui est validé en jeu, et ce qui ne l'est pas encore, dans [docs/ai/STATUS.md](docs/ai/STATUS.md).
Carte des documents : [docs/README.md](docs/README.md).

## Vue d’ensemble

La barre latérale range les pages en catégories repliables (`MainWindow.cs`) :

| Catégorie | Pages |
|---|---|
| — | **Accueil & outils** : dix tuiles avec une ligne en direct (artisanat, récolte, collections, quêtes, carte, marché, alarmes, enchaînement de macros, personnage, Gold Saucer), le travail en cours, la prochaine alarme, l'objet en contexte, l'annuaire des outils |
| Objets | **Objets** (recherche et fiche objet), **Rangements**, **Tri de l'inventaire**, **Marché** |
| Production | **Artisanat**, **Récolte**, **Enchaînement de macros** |
| Monde | **Carte**, **Météo**, **Alarmes**, **Rappels**, **Compagnie libre**, **PNJ vendeurs**, **Butin des contenus**, **Butin des monstres** |
| Progression | **Quêtes**, **Collections** (avec le grimoire Mage bleu), **Personnage**, **Équipement** |
| Gold Saucer | **Revue de mode**, **Triple Triade**, **Mini Cactpot** |
| Préférences | **Paramètres**, **Diagnostics** (mode développeur seulement) |

Commandes : `/tensho` ouvre la fenêtre ; `/tensho peche` (ou `pêche`, `fish`, `fishing`) ouvre ou ferme l'aide à la pêche ;
`/tenshonext` est le marqueur de fin de page de l'enchaînement de macros. Les boutons « ouvrir » et « réglages » de Dalamud ouvrent la
fenêtre et sa page Paramètres.

## Architecture actuelle

- `src/TenshoAssistant/Plugin.cs` enregistre les services, la commande, les fenêtres et les panneaux ; chaque enregistrement a son nettoyage.
- `MainWindow.cs` (découpée en fichiers partiels `MainWindow.<Page>.cs`) porte la navigation et les pages ; `MapViewWindow.cs` est la
  carte Tensho. Les deux sont des fenêtres du `WindowSystem` de Dalamud.
- Un dossier par domaine ; les fichiers `*.Dalamud.cs` touchent le jeu (lecture sur le thread du framework, puis copie), les autres sont
  du C# pur et testé.
- `UI/Components/` : modèles communs (`ItemCell`, `DetailPage`, `SettingRow` avec infobulle et bouton ↺, `EntityMenus`, `DataIndexes`).
- `Community/` : clients HTTP bornés (taille et durée) ; `Caching/` : cache disque, une durée par domaine de données.
- Les textes de l'interface sont des fichiers JSON embarqués dans la DLL.

## Domaines fonctionnels présents

Ce que fait le code aujourd'hui. La validation en jeu de chaque partie est suivie dans [docs/ai/STATUS.md](docs/ai/STATUS.md).

### Objets, rangements et marché

- **Fiche objet** : recherche, puis en-tête (stock, dernier prix) et onglets Aperçu, Stocks, Craft (Produit par / Utilisé dans),
  Usages, Récolte, Marché ; seuls les onglets qui ont un contenu s'affichent.
- **Chaque icône d'objet**, dans tous les modules : infobulle commune et menu d'outils au clic droit, avec les sources groupées par type
  (hauts faits, contenus, autres objets, échanges, PNJ vendeurs, quêtes), « Essayer » pour l'équipement, marque obtenu ✓ / non obtenu ✗
  sur les objets de collection. Les sources en sceaux ne gardent que la Grande Compagnie du personnage.
- **Dans le jeu**, chacun sur option : entrée Tensho dans le menu du clic droit de l'inventaire, panneau sous l'infobulle des objets
  (usages, comment l'obtenir), prix au survol (désactivé par défaut).
- **Rangements** : Banque (servants, sacoches, sacoche premium, arsenal, coffre à apparence, armoire : chacun connu après son ouverture
  en jeu), Cristaux, Imports ; servants et sacoches enregistrés à l'ouverture (réglage), sinon bouton d'import à côté de la fenêtre du jeu.
- **Tri de l'inventaire** : profils, modèles, règles et zones protégées avec aperçu ; le tri n'est appliqué que sur clic.
- **Marché** (Universalis) : Résumé, Annonces (monde / centre de données / région, filtres, lot le moins cher pour N unités, taxe
  comprise) et Historique des ventes ; cache disque, dernier état connu affiché et daté quand Universalis ne répond pas.

### Artisanat et macros

- Catalogue de recettes, simulateur et recherche de rotation, artisanat en direct, préréglages, chaîne de craft et coût de préparation.
- Export des macros et copie dans les macros du jeu sur clic (livre, premier / dernier emplacement, préfixe, écrasement) ; aide à côté
  du carnet d'artisanat du jeu.
- **Enchaînement de macros** : pages terminées par `/tenshonext`, lancement explicite, arrêt possible.

### Récolte et pêche

- Page **Récolte** : ressources et zones, points à horaire avec créneaux en heure d'Éorzéa, planification des PR avec bonus estimé de
  Récolte abondante II (règle communautaire, modifiable).
- Pêche : appâts, pêche au vif, créneaux et météo de GatherBuddy (Garland Tools en secours), temps de touche des relevés Teamcraft, carnet
  et prévision ; les alarmes de pêche portent les conditions météo. Pêche en mer non couverte (aucune source).
- Panneaux d'aide en jeu : à côté de la fenêtre de récolte (mineur / botaniste) et pendant la pêche (`/tensho peche` pour l'ouvrir à la main).

### Carte, météo, alarmes et rappels

- **Carte Tensho**, dans sa propre fenêtre, remplace la carte du jeu (touche M, réglage) : icônes du jeu (taille et priorité par carte),
  quêtes et mandats, ALEA, groupe, points de récolte et de pêche avec horaires, courants éthérés et repères d'exploration (masqués une
  fois obtenus), PNJ vendeurs, PNJ de Triple Triade, drapeau (poser et retirer depuis tous les menus), téléportation au coût réel,
  favoris, listes locales par onglet, menu propre à chaque icône au clic droit.
- **Options de la carte** (Paramètres › Carte, chacune avec infobulle et ↺) : menus et outils par-dessus la carte (désactivé par
  défaut) avec le nom de la zone toujours affiché ; repères au bord de la carte pour les cibles hors de la vue (personnage, drapeau,
  cible du journal, quêtes, mandats et événements, ALEA, points à horaire ouverts, groupe), chaque type réglable ; taille de la
  position, longueur, opacité et couleur du cône de vision ; couleurs des zones ; masquage en combat ou en mission ; tailles et
  priorités des icônes.
- **Météo** : prévisions par zone.
- **Alarmes** : alarmes Tensho (une fois à une date, ou chaque jour d'Éorzéa à une heure ET ; conditions météo pour les poissons), ligne dans le chat et son des alarmes du
  jeu, copie facultative dans la liste d'alarmes du jeu par `/alarm` (désactivée par défaut). Icône d'alarme dans la barre
  d'informations du serveur avec le temps restant avant la prochaine alarme et l'infobulle de Tensho (Paramètres › Général).
  Webhook Discord en option (désactivé par défaut) : les alarmes qui sonnent sont envoyées dans un salon de votre serveur, avec un
  message par catégorie ou par groupe d'alarmes, la mention d'un rôle au choix et un bouton de test.
- **Rappels** : minuteurs du calendrier du jeu (missions de ravitaillement, commandes spéciales, Revue de mode, quêtes des peuples
  alliés, autorisations de mandats, cartes au trésor avec bouton « Carte ouverte », tâches de servant, expéditions de la compagnie)
  avec remises à zéro, compteurs lus dans le jeu, cases « fait » et alarmes. Pas encore : escouade de Grande Compagnie, livraisons des
  Planeurs d'Ehcatl.

### Compagnie libre

- **Vaisseaux** : sous-marins et dirigeables lus dans l'atelier de la compagnie (toutes les 3 s sur place) et gardés : rang et
  expérience, trajet, retour, butin du dernier voyage, alarme au retour.
- **Meilleurs trajets** (sous-marins) : tous les trajets de 1 à 5 secteurs permis par le rang et l'autonomie, classés par valeur du
  butin par voyage ou par jour, ou par expérience par jour ; filtre des secteurs débloqués. Butin moyen : données communautaires
  (FFXIVGachaSpreadsheet, embarquées) ; valeur = prix de revente au PNJ. Pas de planificateur pour les dirigeables (aucun relevé sous
  licence compatible).
- **Montages** : sous-marins et dirigeables (statistiques lues / prévues / écart, coût et capacité, rang des pièces, réparation),
  contrôle lu / calculé, vérification d'un trajet (autonomie, durée, paliers, usure estimée) et meilleurs montages pour un trajet.

### Quêtes, collections et progression

- **Quêtes** : types et genres du journal du jeu avec pourcentages ; quêtes des autres cités de départ et options non prises masquées
  et hors pourcentages (une case les réaffiche) ; quêtes suivies, récompenses, sources d'objets ; menu de quête (drapeau, carte, météo).
- **Collections** : montures, mascottes, emotes, orchestrion, titres, hauts faits et grimoire Mage bleu, avec l'état obtenu et comment
  obtenir chaque entrée (vendeurs, échanges, quêtes, hauts faits, artisanat ; Garland Tools et le wiki en dernier recours).
- **Personnage** : attributs du jeu, et panneau des statistiques réelles (%) à côté de la fenêtre Personnage du jeu (réglage).
- **Équipement** : équipement porté, un plan par job, statistiques lues / prévues / écart, pièces proches du niveau avec filtres, filtre
  « Tous les jobs (mirage) », emplacements et éditeur de matérias avec plafonds par pièce, possession ✓ / ✗ et « Pièces à obtenir »
  avec leurs sources, « Essayer » (cabine d'essayage du jeu), partage et import d'un code `TENSHO-GEAR1:`.
- **Butin des contenus** (aussi en panneau à côté de l'outil de mission), **Butin des monstres**, **PNJ vendeurs** (boutiques de
  compagnie libre comprises).

### Gold Saucer

- **Revue de mode** : indices du thème de la semaine, pièces possédées d'abord (coffre à apparence et armoire compris), panneau à côté
  de la fenêtre du jeu.
- **Triple Triade** : carnet (cartes ordinaires et Ex., valeurs, tris, score par règles), decks, simulation, PNJ et cartes (PNJ à
  affronter, lots à acheter, cartes manquantes), conseil en match (observation sur option).
- **Mini Cactpot** : solveur exact, conseil affiché sur le ticket du jeu.

### Paramètres, diagnostics et localisation

- Onglets des paramètres : Général (ouverture, horloge de l'en-tête, barre d'alarme), Interface (langue, thème, survol et menu des
  objets), Modules, Carte, Données (panneaux en jeu, enregistrement des rangements, durées de cache dont « Jamais (seulement sur demande) »),
  Avancé (« Tout remettre par défaut », mode développeur, sources communautaires, prix au survol, délai des commandes du jeu), Debug
  (mode développeur). Chaque réglage a une infobulle et un bouton ↺.
- Diagnostics (mode développeur) : temps d'affichage de chaque page (médiane et maximum des 120 dernières images) et panneau
  contributeur (documents, fichier source de la page, sources de données, sondes, avancement des traductions).

## Structure du dépôt

Voir la structure anglaise ci-dessus et la carte des documents [docs/README.md](docs/README.md).

## Build et validation

Prérequis : Windows, SDK .NET 10.0.403 (`global.json`), Dalamud.NET.Sdk 15 (restauré depuis NuGet pendant la compilation ; il compile avec votre Dalamud installé, ou `DALAMUD_HOME`). Sans le jeu : `.\scripts\Verify.ps1 -TestsOnly` (tests et langues seulement).

```powershell
.\scripts\Verify.ps1                                  # compilation Release, tests, fichiers de langue, paquet
.\scripts\Verify.ps1 -DotNet 'C:\chemin\dotnet.exe'   # autre emplacement du SDK
.\scripts\Verify.ps1 -Force                           # relancer tous les contrôles même si cet état exact est déjà passé
```

Le résultat doit être vert, sans avertissement ; il affiche `VERSION x.y.z (DLL x.y.z.0)`. Sorties :

- DLL : `src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`
- Paquet : `src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip`

`-Commit`, `-Release` et `-DryRun` (mainteneur) : voir [CONTRIBUTING.md](CONTRIBUTING.md#versions-and-releases-maintainer).

## Installation en jeu

Versions publiées : ajouter `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json` dans Dalamud › Paramètres ›
Expérimental › Dépôts de plugins personnalisés, puis installer « Tensho Assistant ». Versions de développement :

1. Ajouter le chemin de la DLL dans la liste des plugins de développement de Dalamud.
2. Charger le plugin.
3. Ouvrir la fenêtre avec :

```text
/tensho
```

4. Vérifier le comportement des préférences, du rechargement et du déchargement.

## Localisation

Règles : [Localization/README.md](Localization/README.md). En bref :

- copier `src/TenshoAssistant/Localization/Languages/en.json`, le renommer avec le code de la langue, traduire les valeurs ;
- conserver les clés ; l'anglais est la référence et le secours des clés manquantes ;
- anglais et français complets ; allemand et japonais : quelques clés seulement ;
- depuis 0.248 les langues sont embarquées dans la DLL ; un dossier `Localization/Languages` à côté de la DLL installée les remplace pour les essais.

## Documentation utile

- [docs/README.md](docs/README.md) : carte de tous les documents
- [CONTRIBUTING.md](CONTRIBUTING.md) : compiler, tester, règles du projet, publication Dalamud
- [CHANGELOG.md](CHANGELOG.md) : changements par version
- [docs/ai/STATUS.md](docs/ai/STATUS.md) : état, validé en jeu, prochaine action
- [Localization/README.md](Localization/README.md) : contribution à la localisation
- [SECURITY.md](SECURITY.md) : signaler une vulnérabilité

## Usage de l'IA

Ce plugin est développé avec un agent IA (Claude, niveau « Auto ») sous la direction de son auteur, qui définit les fonctions et teste chaque
version en jeu. Les liens de données sont prouvés par des sondes en lecture seule ([docs/evidence/](docs/evidence/)). L'icône du plugin a aussi été générée par l'agent IA.

## Crédits

La liste complète, avec chaque licence et l'usage de chaque source, est la page **Crédits** du plugin (Préférences › Crédits,
72 entrées, `src/TenshoAssistant/Credits/CreditCatalog.cs`, tenue à jour avec le code par des tests) ; cette section la résume.

FINAL FANTASY est une marque déposée de Square Enix Holdings Co., Ltd. FINAL FANTASY XIV © SQUARE ENIX CO., LTD. Tous droits réservés.
Tensho Assistant n'est ni affilié à SQUARE ENIX ni approuvé par elle.

Les données du jeu d'abord : chaque liste est lue dans les feuilles et la mémoire du jeu quand il les a. Les données communautaires ne
servent que lorsque le jeu ne les a pas, et sont toujours citées.

- Final Fantasy XIV et ses données (feuilles lues par Dalamud / Lumina), Dalamud et FFXIVClientStructs ; SDK Dalamud.NET, SDK et
  runtime .NET, PowerShell.
- Données communautaires utilisées : LuminaSupplemental (Critical-Impact, GPL-3.0 : butin des contenus et sources d'objets, téléchargé ; arbre de déblocage et butin possible des dirigeables, embarqués avec leur avis) ;
  GatherBuddy (Ottermandias, Apache-2.0 : données de pêche, téléchargées) ; Teamcraft (MIT : temps de touche) ; FFXIVGachaSpreadsheet
  (Infi, MIT : Revue de mode téléchargée, butin moyen des sous-marins embarqué) ; SubmarineTracker (Infi, MIT : seuils des secteurs
  embarqués, avis dans `src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv`, à l'origine la feuille communautaire
  « Submersible Loot Spreadsheet » d'Elena / saerielle ; formules de voyage comprises en lisant son code ; usure d'après la feuille
  « Submersibles durability damage ») ; table des modificateurs de niveau de Character Panel Refined (Kouzukii, MIT, avis complet dans `Character/StatFormulas.cs`) ;
  [Universalis](https://universalis.app/), [Garland Tools](https://www.garlandtools.org/), le [wiki FFXIV](https://ffxiv.consolegameswiki.com/).
- Lus pour comprendre, jamais copiés : Mappy et SortaKinda (AGPL-3.0-or-later) et les autres références de `docs/project/ROADMAP.md` ;
  Craftimizer (écriture des macros) ; HimbeertoniRaidTool (MIT), Glamourer (Apache-2.0), xivgear.app (sans licence) pour l'équipement ;
  Argus (licence non confirmée) pour les montages. Règle reprise comme fait, avec du code original : frozen_rabbit_tome (sans licence) pour la
  Récolte abondante II, affichée comme estimation.

Liens et détails : section anglaise ci-dessus.

## Résumé

TenshoAssistant est un plugin Dalamud multi-outils orienté assistance au gameplay. Son architecture actuelle est cohérente avec un système de services partagés, des catalogues centralisés et une interface unifiée qui agrège plusieurs domaines utiles au joueur. La documentation du projet reste la source de vérité pour le périmètre exact, le niveau de maturité et les priorités de chaque fonctionnalité.
