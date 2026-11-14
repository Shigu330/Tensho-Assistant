# TenshoAssistant

> *Traduction française plus bas.*

TenshoAssistant is a Dalamud plugin for Final Fantasy XIV that brings everyday tools together in one window, opened with `/tensho`:
items and storage, inventory sorting, market prices, crafting, gathering and fishing, a map that replaces the game map, weather, alarms
and reminders, free company voyages, quests, collections, character stats, a gear planner, Gold Saucer helpers, macro chains,
the island sanctuary, side content (Eureka and Bozja, deep dungeons, relics, Masked Carnivale), a local base of what you discover
while playing, a French–English lexicon of the game's words, your own tools, and diagnostics.

## Install

You need FINAL FANTASY XIV on Windows, started through [XIVLauncher](https://goatcorp.github.io/) with Dalamud (plugin API 15), and the
game up to date.

1. In game, type `/xlsettings`, open Experimental › Custom Plugin Repositories, add
   `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json` with the « + » button, keep it enabled and save.
2. Open the plugin installer (`/xlplugins`), search for « Tensho Assistant » and install it.
3. Type `/tensho`: the window opens, with a short guide the first time. Dalamud updates the plugin like any other.

Questions: [docs/FAQ.md](docs/FAQ.md) (the same as the plugin's Help page), or ask in [Discussions › Q&A](https://github.com/Shigu330/Tensho-Assistant/discussions/categories/q-a). Building from source
and development builds: [CONTRIBUTING.md](CONTRIBUTING.md).

These documents were produced for version <!-- docgen:version -->0.1323<!-- /docgen:version -->. What changed in each version is in
[CHANGELOG.md](CHANGELOG.md); what is validated in game, and what is not yet, is in [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md). Map of all
documents: [docs/README.md](docs/README.md).

## Overview

The sidebar groups the pages in foldable groups. The table is written by `tools/docgen.py` from the page list (`MainWindow.Pages.cs`)
and the help text of each page (the « ? » beside its title):

<!-- docgen:pages-en -->
| Group | Page | What it is for |
|---|---|---|
| — | **Home** | An overview: one tile per main tool with its live state, the next alarm, running calculations and errors. |
|  | **My tools** | Build your own small pages from Tensho's blocks, without code, or install those shared by the community. |
| Items | **Items** | Find an item and see its item page: how to get it, your stocks, crafting, uses, gathering, market, your note. |
|  | **Storage** | Where your items are: character, retainers, saddlebags, armoury, glamour dresser, armoire, company chest; also crystals, currencies with what they buy, a gil journal, a report of what could free space, and your other characters. |
|  | **Inventory sorting** | Preview a sorting rule, then apply it to your storage. Nothing moves until you click. |
|  | **Market** | Prices of the selected item from Universalis: summary, listings for your world, Data Center or region, and sale history. |
|  | **Wishlist** | The items you want to get, in one list: their state (obtained, or the copies you hold), your price for a sale alert and, for the items that are gathered, where to gather them (job, zone, node, hours or weather). |
| Production | **Crafting** | Simulate a recipe step by step, search a sequence, prepare the materials, and make a macro to copy or write into the game. Other tabs: crafting lists (with an on-screen panel of what they still need) and « What can I craft? » from your stocks. |
|  | **Gathering** | Find where and when a resource can be gathered or fished, with your notebooks, an Eorzean clock and the fishing help. |
|  | **Ocean fishing** | The next departures from Limsa Lominsa and Kugane, with their stops and the time of day. |
|  | **Island Sanctuary** | Your workshops' crafts with their estimated value, and the animals of your pasture, from what the game has loaded on your island. |
|  | **Macro chain** | Chain several game macros, one after the other. The sequence only starts when you click. |
| World | **Map** | The Tensho map, with your position, flags, favourites, vistas, gathering points and quests; also chat map links, plots for sale, a hunt train recorder and the S ranks' spawn conditions. |
|  | **Weather** | The weather forecast of a zone, and its next windows. |
|  | **FATEs** | Where each FATE of the open zones takes place, with its level, from the game files. |
|  | **NPC vendors** | Who sells an item, for what price, and where. |
|  | **Duty loot** | What each duty can drop, and how far you have got in it. |
|  | **Monster loot** | Which monsters drop the selected item, and where to find them. |
|  | **Discoveries** | What you discover while playing, kept on your disk only: where monsters, hunt marks and FATEs were seen; where the items you get come from (desynthesis, ventures, duty chests, FATE rewards, monster drops, opened items, voyages); fish caught; Fashion Report judgements; blue magic spells learned, beasts tamed, festivals and vistas seen; market board prices. The hunting log, monster loot and the item hover show what you saw. |
| Tracking | **Alarms** | Alarms on items, gathering points, dates, voyages and missions, with sound, chat line and Discord if you wish. The clocks are here too. |
|  | **Reminders** | The game's timers and counters, with « done » boxes: daily and weekly tasks, retainers, voyages, roulettes, squadron, character status, your own tasks. |
|  | **Garden** | Your beds and pots with their reminders, and seed crossbreeds with a planting plan. |
|  | **Free Company** | Submersibles and airships: rank, route, return, finds, best routes, builds and the history of your voyages; also the materials of the workshop's projects, with an on-screen panel. |
| Progression | **Quests** | The quests of the game with your progress, their rewards and their chains. |
|  | **Events** | The seasonal events running now, with their quests, what you have done and their shops. |
|  | **Collections** | Mounts, minions, emotes, cards, orchestrion rolls, titles, achievements, hairstyles and more, with what you own and how to get the rest; also your wish list. |
|  | **Allied Societies** | Each allied society: your rank and reputation, its place and its currency. |
|  | **Grand Companies** | Your rank and seals in each Grand Company, the next promotion, and what seals buy. |
|  | **Special deliveries** | The delivery quest lines of crafters and gatherers: Crystarium, Studium and Wachumeqimeqi, done or left. |
|  | **Custom Deliveries** | Your custom delivery clients: rank, points and what each one still takes this week; and each client's rewards, from the game's sheets. |
|  | **Gear** | Plan your gear in the style of the Character window: worn pieces, materia, real and planned stats, upgrades, your game gear sets, wear and spiritbond. |
|  | **Chocobo** | Your companion chocobo: what it wears, the bardings unlocked and the items that teach the others. |
|  | **Guildleves** | Guildleves and crafting orders, with your allowances, what is done and what is accepted. |
|  | **Hunting log** | Your hunting logs: rank, monsters and kills, for your class and grand company. |
| Content | **Masked Carnivale** | The stages of the Masked Carnivale, done or left, with their enemies and times. |
|  | **Relics** | Relic weapons and armour, the stages you hold and the Zodiac book in progress. |
|  | **Eureka and Bozja** | Where FATEs, skirmishes and critical engagements take place in these zones, the field records and the lost actions. |
|  | **Deep dungeons** | Prepare before going in: the floors, their state, the items of each dungeon and what their coffers give. |
|  | **Beastmaster** | The beastmaster's beasts, with their attributes at a chosen rank, and the boards of the labyrinth with their enemies and bonuses. |
| Gold Saucer | **Fashion Report** | This week's theme and categories, what you own for each, and the equipment reported by the community. |
|  | **Triple Triad** | Your cards, decks, a simulator, NPCs with their availability and the cards you lack. With the observation on, advice during a match. |
|  | **Mini Cactpot** | The best cell to reveal and the best line of a ticket. You always choose yourself. |
| References | **Lexicon** | Look up in-game terms in French and in English: items, actions, places, monsters, quests and the interface's own words. |
|  | **Help** | All of Tensho's help: search it, read the frequent questions, or open a page. The « ? » beside the title explains the page you are on. |
|  | **Credits** | Who Tensho owes its data and libraries to, with their licences. |
| Preferences | **Settings** | All of Tensho's options, grouped by domain. Each has a tooltip, and the reset button (curved arrow) next to a changed option puts it back to its default. |
|  | **Diagnostics** *(developer mode only)* | For checks and bug reports, in developer mode: what is loaded, what Tensho read, draw times, and the self-check. |
<!-- /docgen:pages-en -->

Chat commands: `/tensho` opens the window; `/tensho fish` (also `fishing`, `peche`, `pêche`) opens or closes the fishing help;
`/tensho todo <task>` adds a free task to Reminders; `/tensho item <item>`, `price`, `gather` and `recipe` (also `objet`, `prix`,
`recolte`, `recette`) open the item in Tensho (item sheet, Market, Gathering, Crafting); `/tensho wiki <item>` and
`/tensho lodestone <item>` open the item's page in the browser; `/tenshonext` is the end-of-page marker of macro chains. Dalamud's « open » and « settings » buttons open the window and its
Settings page.

## Features

Each page has its own help in the plugin: the « ? » beside its title, the Help page and its FAQ ([docs/FAQ.md](docs/FAQ.md)). The table
above says what each page is for. Unknown is not zero: when a value is not known (storage not opened yet, no community data), the plugin
says so instead of guessing. In-game validation of each part: [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md); how the code is
organised: [docs/project/ARCHITECTURE.md](docs/project/ARCHITECTURE.md).

## Desktop app (game closed)

Each release also carries `TenshoAssistant.Desktop.zip`: the same pages in a Windows window, with the game closed (step 1 of the desktop
application; the link to a running game comes later). Unzip it anywhere and start `TenshoAssistant.Desktop.exe`. It needs XIVLauncher with
Dalamud and the game installed (it reads Dalamud's ImGui and the game data where they are installed; nothing of them is shipped); it carries
its own .NET runtime, nothing else to install. It reads your saved Tensho settings without changing them. Live parts (bags, timers, alarms) stay empty and actions are
off, as its status bar says; the map, live synthesis and the Fashion Report stay in the game. The app and the plugin carry the same version
and are released together: the app says at start when the installed plugin's version differs.
It updates itself: at start it asks GitHub for the release of the installed plugin's version (else the latest one), checks the
zip's SHA-256 published in that release, installs it beside and keeps the version before (« Previous version »); it never restarts while
you use it, and « Automatic update » turns it off. The app is not code-signed: the SHA-256 protects against a damaged download, not
against a stolen GitHub account (`SECURITY.md`). Code: `src/TenshoAssistant.Desktop`.
Its own files are in `%LOCALAPPDATA%\TenshoAssistant\` (a copy of Tensho's data, its cache and its versions): they stay when you delete
the app, so delete that folder too to remove everything. Tensho's files: [docs/FAQ.md](docs/FAQ.md) (« Where are my files »).

## Localization

Rules: [Localization/README.md](src/TenshoAssistant/Localization/README.md). In short:

- copy `src/TenshoAssistant/Localization/Languages/en.json`, rename it with the language code, translate the values;
- keep the translation keys unchanged; English is the reference and the fallback for missing keys;
- English and French are complete; German and Japanese only have a few keys;
- since 0.248 the languages are embedded in the DLL; a `Localization/Languages` folder beside the installed DLL overrides them for testing.

## Documentation to consult

- [docs/README.md](docs/README.md) — map of all documents
- [docs/FAQ.md](docs/FAQ.md) — frequent questions (the same as the plugin's Help page); ask a new one in [Discussions › Q&A](https://github.com/Shigu330/Tensho-Assistant/discussions/categories/q-a)
- [CONTRIBUTING.md](CONTRIBUTING.md) — build, tests, project rules, Dalamud publishing rules
- [CHANGELOG.md](CHANGELOG.md) — what changed in each version
- [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md) — the plan, what is done and what is validated in game
- Issues: [our plan](https://github.com/Shigu330/Tensho-Assistant/issues?q=is%3Aissue+label%3Aplan) (label `plan`, pinned) · [player reports](https://github.com/Shigu330/Tensho-Assistant/issues?q=is%3Aissue+is%3Aopen+label%3Aplayer-report) (label `player-report`, open)
- [docs/project/ROADMAP.md](docs/project/ROADMAP.md) · [ARCHITECTURE.md](docs/project/ARCHITECTURE.md) · [TESTING.md](docs/project/TESTING.md) · [REFERENCES.md](docs/project/REFERENCES.md)
- [Localization/README.md](src/TenshoAssistant/Localization/README.md) — localization workflow
- [SECURITY.md](SECURITY.md) — reporting a vulnerability

## AI usage disclosure

This plugin is developed with an AI coding agent (Claude, « Auto » level) under the direction of its author, who defines the features and
tries the changes in game. Each release is built and tested automatically (compilation, tests, the desktop app drawn without a window), then loaded
once in game with its self-check, which its notes say (« checked at launch, not tried in detail yet »); the detailed in-game tests
come after, grouped in campaigns, and [CHANGELOG.md](CHANGELOG.md) says for each version whether it was validated in game. An urgent fix
(game patch, privacy) may go out before that launch check, and its notes say so. Data links are proven with read-only probes
([docs/evidence/](docs/evidence/)). See [CONTRIBUTING.md](CONTRIBUTING.md#publication-dalamud) for the Dalamud rules the project follows. The plugin icon (`src/TenshoAssistant/images/icon.png`)
was also generated by the AI agent.

## Credits

The complete list, with every licence and what each source is used for, is the plugin's **Credits** page (Preferences › Credits,
`src/TenshoAssistant/Credits/CreditCatalog.cs`, kept in step with the code by tests); this section summarizes it.

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd. FINAL FANTASY XIV © SQUARE ENIX CO., LTD. All rights reserved.
Tensho Assistant is not affiliated with nor endorsed by SQUARE ENIX.

Game data comes first: every list is read from the game's own sheets and memory when they have it. Community data is used only when the
game does not have it, always cited.

Framework and tools:

- Final Fantasy XIV and its game data (sheets read through Dalamud / Lumina), Dalamud and FFXIVClientStructs
- Dalamud.NET SDK, .NET SDK and runtime, PowerShell scripts for verification and release
- [Raphael.Net](https://github.com/WorkingRobot/Raphael.Net) by Asriel (MIT, NuGet, native library) and raphael-rs by KonaeAkira (Apache-2.0): the crafting solver
- [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore) (.NET Foundation, MIT, NuGet), [SQLitePCLRaw](https://github.com/ericsink/SQLitePCL.raw)
  by Eric Sink (Apache-2.0, NuGet, native library `e_sqlite3.dll`) and [SQLite](https://sqlite.org/) (public domain): the local discoveries database

Community data used by the plugin (licence compatible with GPL-3.0, or service API):

- [LuminaSupplemental](https://github.com/Critical-Impact/LuminaSupplemental) by Critical-Impact (GPL-3.0): duty loot and item sources, downloaded at runtime; airship unlock tree and possible loot, embedded with its notice (0.336)
- [GatherBuddy](https://github.com/Ottermandias/GatherBuddy) by Ottermandias (Apache-2.0): fishing bait, mooch, bite and window data, downloaded at runtime as data
- [Teamcraft](https://github.com/ffxiv-teamcraft/ffxiv-teamcraft) (MIT): fishing bite times from its public reports
- [FFXIVGachaSpreadsheet](https://github.com/Infiziert90/FFXIVGachaSpreadsheet) by Infi (MIT, crowd-sourced): Fashion Report data (downloaded at runtime) and submarine loot averages (embedded snapshot, `tools/submarine-loot.py`)
- [SubmarineTracker](https://github.com/Infiziert90/SubmarineTracker) by Infi (MIT): submarine sector breakpoints embedded (notice in
  `src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv`), originally from the community « Submersible Loot Spreadsheet »
  (Elena / saerielle); voyage formulas understood by reading its code (original code); wear estimate from the community « Submersibles
  durability damage » sheet that it quotes
- [FFXIV Gardening Tracker](https://github.com/Lotlab/FFXIV-Gardening-Tracker) by Lotlab (GPL-3.0): garden seed times, embedded with its notice; FFXIV Crossbreed Helper by nick75g (MIT): garden crosses, embedded
- level modifier table (main stat, levels 1–100) from Character Panel Refined by Kouzukii (MIT, full notice in `Character/StatFormulas.cs`)
- [Universalis](https://universalis.app/) (market prices and listings), [Garland Tools](https://www.garlandtools.org/) (item, duty and
  monster sources), the [FFXIV Console Games Wiki](https://ffxiv.consolegameswiki.com/) (monster drops, sightseeing log, acquisition;
  island animals and workshop rules),
  [PaissaDB](https://github.com/zhudotexe/FFXIV_PaissaDB) by zhudotexe (open housing plots, read on click or by the optional watch)
- this repository on GitHub (CC0-1.0): the shared discoveries (on « Exchange now » or the optional automatic exchange) and the community tools gallery (read
  on click); [Partake](https://www.partake.gg/) (player events, opened in your browser); Discord (the optional alarm webhook)

Read to understand, never copied (facts only, original code):

- Mappy and SortaKinda (AGPL-3.0-or-later), and the other reference plugins listed in [docs/project/ROADMAP.md](docs/project/ROADMAP.md)
- Craftimizer (writing macros into the game, functional reference)
- HimbeertoniRaidTool (MIT) and Glamourer (Apache-2.0) for the gear planner; xivgear.app (no published licence: read only)
- Argus (licence not confirmed) for vessel builds; its airship breakpoint data comes from unlicensed sources and is not used
- frozen_rabbit_tome (no licence): the Bountiful Yield II rule, taken as a fact with original code and shown as an estimate (a rule, no text copied)

This project is an original plugin implementation, but it relies on these ecosystem components and references for its operation, testing, and validation.

---

# TenshoAssistant

Version française / French version

TenshoAssistant est un plugin Dalamud pour Final Fantasy XIV qui réunit dans une seule fenêtre, ouverte par `/tensho`, des outils du
quotidien : objets et rangements, tri de l'inventaire, prix du marché, artisanat, récolte et pêche, une carte qui remplace celle du jeu,
météo, alarmes et rappels, expéditions de la compagnie libre, quêtes, collections, statistiques du personnage, planificateur
d'équipement, aides du Gold Saucer, enchaînement de macros, la Félicité insulaire, des contenus annexes (Eurêka et Bozja, donjons
profonds, reliques, La Grande Mascarade), une base locale de ce que vous découvrez en jouant, un lexique français–anglais des mots du
jeu, vos propres outils, et des diagnostics.

## Installation

Il vous faut FINAL FANTASY XIV sur Windows, lancé par [XIVLauncher](https://goatcorp.github.io/) avec Dalamud (API des plugins 15), et
le jeu à jour.

1. En jeu, tapez `/xlsettings`, ouvrez Expérimental › Dépôts de plugins personnalisés, ajoutez
   `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json` avec le bouton « + », laissez-le activé et enregistrez.
2. Ouvrez l'installateur de plugins (`/xlplugins`), cherchez « Tensho Assistant » et installez-le.
3. Tapez `/tensho` : la fenêtre s'ouvre, avec un court guide la première fois. Dalamud met le plugin à jour comme les autres.

Questions : [docs/FAQ.md](docs/FAQ.md) (les mêmes que la page Aide du plugin), ou posez-la dans [Discussions › Q&A](https://github.com/Shigu330/Tensho-Assistant/discussions/categories/q-a).
Compiler depuis les sources et versions de développement : [CONTRIBUTING.md](CONTRIBUTING.md).

Ces documents ont été produits pour la version <!-- docgen:version -->0.1323<!-- /docgen:version -->. Les changements de chaque version sont dans
[CHANGELOG.md](CHANGELOG.md) ; ce qui est validé en jeu, et ce qui ne l'est pas encore, dans [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md).
Carte des documents : [docs/README.md](docs/README.md).

## Vue d’ensemble

La barre latérale range les pages en groupes repliables. Le tableau est écrit par `tools/docgen.py` depuis la liste des pages
(`MainWindow.Pages.cs`) et le texte d'aide de chaque page (le « ? » à côté de son titre) :

<!-- docgen:pages-fr -->
| Groupe | Page | À quoi elle sert |
|---|---|---|
| — | **Accueil & outils** | Une vue d'ensemble : une tuile par outil principal avec son état, la prochaine alarme, les calculs en cours et les erreurs. |
|  | **Mes outils** | Construisez vos petites pages avec les blocs de Tensho, sans code, ou installez celles de la communauté. |
| Objets | **Objets** | Trouvez un objet et voyez sa fiche : comment l'obtenir, vos stocks, l'artisanat, les usages, la récolte, le marché, votre note. |
|  | **Rangements** | Où sont vos objets : personnage, servants, sacoches, arsenal, coffre à apparence, armoire, coffre de compagnie ; aussi les cristaux, les monnaies avec ce qu'elles achètent, un journal des gils, un rapport de ce qui pourrait libérer de la place, et vos autres personnages. |
|  | **Tri de l’inventaire** | Prévisualisez une règle de tri, puis appliquez-la à vos rangements. Rien ne bouge sans votre clic. |
|  | **Marché** | Les prix de l'objet choisi, depuis Universalis : résumé, offres de votre monde, centre de données ou région, historique des ventes. |
|  | **Souhaits** | Les objets que vous voulez obtenir, en une liste : leur état (obtenu, ou les exemplaires que vous avez), votre prix pour l'alerte de vente et, pour les objets qui se récoltent, où les récolter (métier, zone, point, heures ou météo). |
| Production | **Artisanat** | Simulez une recette étape par étape, cherchez une séquence, préparez les matières, et faites une macro à copier ou à écrire dans le jeu. Autres onglets : les listes de fabrication (avec un panneau à l'écran de ce qu'il leur manque) et « Que puis-je fabriquer ? » d'après vos stocks. |
|  | **Récolte** | Trouvez où et quand récolter ou pêcher une ressource, avec vos carnets, l'horloge d'Éorzéa et l'aide à la pêche. |
|  | **Pêche en mer** | Les prochains départs de Limsa Lominsa et de Kugane, avec leurs escales et le moment de la journée. |
|  | **Félicité insulaire** | Les créations de vos fabriques avec leur valeur estimée, et les animaux de votre pâturage, à partir de ce que le jeu a chargé sur votre île. |
|  | **Enchaînement de macros** | Enchaînez plusieurs macros du jeu, l'une après l'autre. La suite ne démarre que sur votre clic. |
| Monde | **Carte** | La carte Tensho, avec votre position, drapeaux, favoris, panoramas, points de récolte et quêtes ; aussi les liens de carte du chat, les terrains en vente, un relevé de train de chasse et les conditions d'apparition des rangs S. |
|  | **Météo** | Les prévisions météo d'une zone et ses prochaines fenêtres. |
|  | **ALEA** | Où se tient chaque ALEA des zones ouvertes, avec son niveau, d'après les fichiers du jeu. |
|  | **PNJ vendeurs** | Qui vend un objet, à quel prix et où. |
|  | **Butin des contenus** | Ce que chaque contenu peut donner, et où vous en êtes. |
|  | **Butin des monstres** | Quels monstres donnent l'objet choisi, et où les trouver. |
|  | **Découvertes** | Ce que vous découvrez en jouant, gardé sur votre disque seulement : où ont été vus les monstres, les monstres de chasse et les ALÉA ; d'où viennent les objets obtenus (désynthèse, tâches de servants, coffres de mission, récompenses d'ALÉA, butin de monstres, objets ouverts, voyages) ; les poissons pris ; les jugements de la Fashion Report ; les sorts de mage bleu appris, les bêtes apprivoisées, les fêtes et les panoramas vus ; les prix du tableau des ventes. Le carnet de chasse, le butin des monstres et l'info-bulle des objets montrent ce que vous avez vu. |
| Suivi | **Alarmes** | Des alarmes sur les objets, points de récolte, dates, voyages et missions, avec son, ligne de chat et Discord si vous le souhaitez. Les horloges sont ici aussi. |
|  | **Rappels** | Les minuteurs et compteurs du jeu, avec des cases « fait » : tâches du jour et de la semaine, servants, voyages, roulettes, escouade, état du personnage, vos propres tâches. |
|  | **Jardinage** | Vos parcelles et vos pots avec leurs rappels, et les croisements de graines avec un plan de plantation. |
|  | **Compagnie libre** | Sous-marins et dirigeables : rang, trajet, retour, trouvailles, meilleurs trajets, montages et l'historique de vos voyages ; aussi les matériaux des projets de l'atelier, avec un panneau à l'écran. |
| Progression | **Quêtes** | Les quêtes du jeu avec votre progression, leurs récompenses et leurs enchaînements. |
|  | **Événements** | Les événements saisonniers en cours, avec leurs quêtes, ce que vous avez fait et leurs boutiques. |
|  | **Collections** | Montures, mascottes, emotes, cartes, rouleaux d'orchestrion, titres, succès, coiffures et plus, avec ce que vous avez et comment obtenir le reste ; aussi votre liste de souhaits. |
|  | **Peuples alliés** | Chaque peuple allié : votre rang et votre réputation, son lieu et sa monnaie. |
|  | **Grandes compagnies** | Votre grade et vos sceaux dans chaque grande compagnie, la prochaine promotion et ce que les sceaux achètent. |
|  | **Livraisons spéciales** | Les quêtes de livraison des artisans et récolteurs : Cristarium, académie de Sharlayan et bazar aux Mille Perles, faites ou à faire. |
|  | **Commandes spéciales** | Vos clients des commandes spéciales : rang, points et ce que chacun prend encore cette semaine ; et les récompenses de chaque client, d'après les feuilles du jeu. |
|  | **Équipement** | Préparez votre équipement dans le style de la fenêtre Personnage : pièces portées, matérias, statistiques réelles et prévues, améliorations, ensembles du jeu, usure et symbiose. |
|  | **Chocobo** | Votre chocobo de compagnie : ce qu'il porte, les bardes débloquées et les objets qui apprennent les autres. |
|  | **Mandats** | Les mandats et les mandats d'artisanat, avec vos allocations, ce qui est fait et ce qui est accepté. |
|  | **Bestiaire** | Vos carnets du bestiaire : rang, monstres et victimes, pour votre classe et votre grande compagnie. |
| Contenus | **La Grande Mascarade** | Les niveaux de la Grande Mascarade, réussis ou non, avec leurs ennemis et leurs temps. |
|  | **Reliques** | Les armes et armures reliques, les étapes que vous détenez et le livre du zodiaque en cours. |
|  | **Eureka et Bozja** | Où se tiennent les ALEA, escarmouches et affrontements cruciaux de ces zones, les rapports du front bozjien et les actions oubliées. |
|  | **Donjons profonds** | Pour préparer dehors : les étages, leur état, les objets de chaque donjon et ce que donnent ses coffres. |
|  | **Dresseur** | Les bêtes du dresseur, avec leurs attributs au rang choisi, et les plateaux du Dédale avec leurs ennemis et leurs bonus. |
| Gold Saucer | **Revue de mode** | Le thème et les catégories de la semaine, ce que vous possédez pour chacune, et l'équipement signalé par la communauté. |
|  | **Triple Triade** | Vos cartes, vos decks, un simulateur, les PNJ avec leur disponibilité et les cartes qui vous manquent. Avec l'observation activée, des conseils pendant un match. |
|  | **Mini Cactpot** | La meilleure case à révéler et la meilleure ligne d'un ticket. Vous choisissez toujours vous-même. |
| Références | **Lexique** | Trouver un mot du jeu en français et en anglais : objets, actions, lieux, monstres, quêtes et les mots de l'interface. |
|  | **Aide** | Toute l'aide de Tensho : cherchez-y, lisez les questions fréquentes ou ouvrez une page. Le « ? » à côté du titre explique la page où vous êtes. |
|  | **Crédits** | À qui Tensho doit ses données et ses bibliothèques, avec leurs licences. |
| Préférences | **Paramètres** | Toutes les options de Tensho, groupées par domaine. Chacune a son infobulle, et le bouton de retour au défaut (flèche courbe), à côté d'une option changée, la remet par défaut. |
|  | **Diagnostics** *(mode développeur seulement)* | Pour les vérifications et les rapports de bug, en mode développeur : ce qui est chargé, ce que Tensho a lu, les temps d'affichage et l'auto-contrôle. |
<!-- /docgen:pages-fr -->

Commandes : `/tensho` ouvre la fenêtre ; `/tensho peche` (ou `pêche`, `fish`, `fishing`) ouvre ou ferme l'aide à la pêche ;
`/tensho todo <tâche>` ajoute une tâche libre aux Rappels ; `/tensho objet <objet>`, `prix`, `recolte` et `recette` (ou `item`,
`price`, `gather`, `recipe`) ouvrent l'objet dans Tensho (fiche objet, Marché, Récolte, Artisanat) ; `/tensho wiki <objet>` et `/tensho lodestone <objet>` ouvrent la page de
l'objet dans le navigateur ; `/tenshonext` est le marqueur de fin de page de l'enchaînement de macros. Les boutons « ouvrir » et
« réglages » de Dalamud ouvrent la fenêtre et sa page Paramètres.

## Fonctions

Chaque page a son aide dans le plugin : le « ? » à côté de son titre, la page Aide et sa FAQ ([docs/FAQ.md](docs/FAQ.md)). Le tableau
ci-dessus dit à quoi sert chaque page. Inconnu n'est pas zéro : quand une valeur n'est pas connue (rangement pas encore ouvert, pas de
donnée communautaire), le plugin le dit au lieu de deviner. Validation en jeu de chaque partie :
[docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md) ; organisation du code :
[docs/project/ARCHITECTURE.md](docs/project/ARCHITECTURE.md).

## Application de bureau (jeu fermé)

Chaque release porte aussi `TenshoAssistant.Desktop.zip` : les mêmes pages dans une fenêtre Windows, jeu fermé (étape 1 de l'application de
bureau ; le lien avec le jeu lancé viendra ensuite). Décompressez-le où vous voulez et lancez `TenshoAssistant.Desktop.exe`. Il lui faut
XIVLauncher avec Dalamud et le jeu installés (elle lit l'ImGui de Dalamud et les données du jeu là où ils sont installés ; rien d'eux n'est
livré) ; elle porte son propre runtime .NET, rien d'autre à installer. Elle lit vos réglages Tensho enregistrés sans les changer. Les parties en direct (sacs, minuteurs, alarmes)
restent vides et les actions sont coupées, comme le dit sa barre d'état ; la carte, la synthèse en direct et la Revue de mode restent dans le
jeu. L'application et le plugin portent le même numéro de version et sont publiés ensemble : l'application signale au démarrage une version
différente du plugin installé.
Elle se met à jour seule : au démarrage, elle demande à GitHub la release de la version du plugin installé (sinon la dernière),
vérifie le SHA-256 du zip publié dans cette release, l'installe à côté et garde la version d'avant (« Version d'avant ») ; elle ne
redémarre jamais pendant que vous l'utilisez, et « Mise à jour automatique » la coupe. L'application n'est pas signée : le SHA-256 protège
d'un téléchargement abîmé, pas d'un compte GitHub volé (`SECURITY.md`). Code : `src/TenshoAssistant.Desktop`.
Ses propres fichiers sont dans `%LOCALAPPDATA%\TenshoAssistant\` (une copie des données de Tensho, son cache et ses versions) : ils restent quand
vous supprimez l'application, supprimez donc aussi ce dossier pour tout retirer. Fichiers de Tensho : [docs/FAQ.md](docs/FAQ.md) (« Où sont mes fichiers »).

## Localisation

Règles : [Localization/README.md](src/TenshoAssistant/Localization/README.md). En bref :

- copier `src/TenshoAssistant/Localization/Languages/en.json`, le renommer avec le code de la langue, traduire les valeurs ;
- conserver les clés ; l'anglais est la référence et le secours des clés manquantes ;
- anglais et français complets ; allemand et japonais : quelques clés seulement ;
- depuis 0.248 les langues sont embarquées dans la DLL ; un dossier `Localization/Languages` à côté de la DLL installée les remplace pour les essais.

## Documentation utile

- [docs/README.md](docs/README.md) : carte de tous les documents
- [docs/FAQ.md](docs/FAQ.md) : questions fréquentes (les mêmes que la page Aide du plugin) ; une nouvelle question se pose dans [Discussions › Q&A](https://github.com/Shigu330/Tensho-Assistant/discussions/categories/q-a)
- [CONTRIBUTING.md](CONTRIBUTING.md) : compiler, tester, règles du projet, publication Dalamud
- [CHANGELOG.md](CHANGELOG.md) : changements par version
- [docs/project/GLOBAL-PLAN.md](docs/project/GLOBAL-PLAN.md) : le plan, ce qui est fait et validé en jeu
- Tickets : [notre plan](https://github.com/Shigu330/Tensho-Assistant/issues?q=is%3Aissue+label%3Aplan) (étiquette `plan`, épinglés) · [signalements des joueurs](https://github.com/Shigu330/Tensho-Assistant/issues?q=is%3Aissue+is%3Aopen+label%3Aplayer-report) (étiquette `player-report`, ouverts)
- [Localization/README.md](src/TenshoAssistant/Localization/README.md) : contribution à la localisation
- [SECURITY.md](SECURITY.md) : signaler une vulnérabilité

## Usage de l'IA

Ce plugin est développé avec un agent IA (Claude, niveau « Auto ») sous la direction de son auteur, qui définit les fonctions et essaie
les changements en jeu. Chaque release est compilée et testée automatiquement (compilation, tests, application de bureau dessinée sans fenêtre), puis
chargée une fois en jeu avec son auto-contrôle, ce que disent ses notes (« vérifié au lancement, pas encore essayé en détail ») ; les
essais détaillés en jeu viennent après, groupés en campagnes, et [CHANGELOG.md](CHANGELOG.md) dit pour chaque version si elle a été
validée en jeu. Un correctif urgent (patch du jeu, confidentialité) peut partir avant cette vérification au lancement, et ses notes le disent. Les liens de données sont prouvés par des sondes en lecture seule ([docs/evidence/](docs/evidence/)). L'icône du plugin a aussi été générée par l'agent IA.

## Crédits

La liste complète, avec chaque licence et l'usage de chaque source, est la page **Crédits** du plugin (Préférences › Crédits,
`src/TenshoAssistant/Credits/CreditCatalog.cs`, tenue à jour avec le code par des tests) ; cette section la résume.

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
  « Submersibles durability damage ») ; Raphael.Net (Asriel, MIT) et raphael-rs (KonaeAkira, Apache-2.0) : solveur d'artisanat ; Microsoft.Data.Sqlite (.NET Foundation, MIT), SQLitePCLRaw (Eric Sink, Apache-2.0, bibliothèque native `e_sqlite3.dll`) et SQLite (domaine public) : base locale des découvertes ; FFXIV Gardening Tracker (Lotlab, GPL-3.0 : durées du jardinage, embarquées) et FFXIV Crossbreed Helper (nick75g, MIT : croisements, embarqués) ; table des modificateurs de niveau de Character Panel Refined (Kouzukii, MIT, avis complet dans `Character/StatFormulas.cs`) ;
  [Universalis](https://universalis.app/), [Garland Tools](https://www.garlandtools.org/), le [wiki FFXIV](https://ffxiv.consolegameswiki.com/),
  [PaissaDB](https://github.com/zhudotexe/FFXIV_PaissaDB) de zhudotexe (terrains en vente, lus sur clic ou par la surveillance facultative) ;
  ce dépôt sur GitHub (CC0-1.0 : découvertes partagées, sur « Échanger maintenant » ou par l'échange automatique facultatif, et galerie
  des outils de la communauté, lue sur clic) ; [Partake](https://www.partake.gg/) (événements de joueurs, ouvert dans votre
  navigateur) ; Discord (webhook facultatif des alarmes).
- Lus pour comprendre, jamais copiés : Mappy et SortaKinda (AGPL-3.0-or-later) et les autres références de `docs/project/ROADMAP.md` ;
  Craftimizer (écriture des macros) ; HimbeertoniRaidTool (MIT), Glamourer (Apache-2.0), xivgear.app (sans licence) pour l'équipement ;
  Argus (licence non confirmée) pour les montages. Règle reprise comme fait, avec du code original : frozen_rabbit_tome (sans licence) pour la
  Récolte abondante II, affichée comme estimation.

Liens et détails : section anglaise ci-dessus.
