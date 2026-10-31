# TenshoAssistant

TenshoAssistant is a Dalamud plugin for Final Fantasy XIV designed to centralize gameplay support tools into a single, coherent interface. The project currently exposes a multi-page assistant UI accessible through `/tensho`, bringing together inventory tracking, crafting workflows, gathering data, map navigation, quest context, market information, collections, macros, mini-games, and diagnostics.

The repository shows a modular architecture with shared service layers, catalog stores, and a central window orchestrating multiple gameplay domains. The exact maturity and coverage of each feature area are tracked in [docs/ai/STATUS.md](docs/ai/STATUS.md) and [CHANGELOG.md](CHANGELOG.md); the documentation map is [docs/README.md](docs/README.md).

## Overview

TenshoAssistant is not just an inventory helper. The current codebase implements a unified assistant with a main window and a set of feature modules built around shared item and catalog data.

The primary UI includes sections such as:

- Home
- Items & inventory
- Gathering & maps
- Quests
- Market
- Collections
- Crafting / recipes / simulations
- Macro Chain
- Mini-games / Gold Saucer
- Settings
- Diagnostics

This repository is organized as a C# project built around the Dalamud ecosystem, with a central plugin, service-oriented business logic, and documentation for development status and roadmap.

## Architecture

The main entry point is `src/TenshoAssistant/MainWindow.cs`.

The UI is organized around:

- a left-side navigation panel
- a content area that changes based on the active page
- shared state for selected item context and runtime configuration
- integration with Dalamud services (`IPluginLog`, `ITextureProvider`, `IGameGui`, windowing, etc.)

The implementation follows a service-driven structure:

- `MainWindow` handles the UI shell and page routing
- domain services manage inventory, crafting, gathering, market, quests, collections, maps, etc.
- catalog services act as the shared source of data for item definitions, recipes, market context, and maps
- localization and diagnostics are integrated directly into the plugin runtime

## Main feature domains

### Inventory and items

- item catalog and stock tracking
- item search by name or ID
- quantity display and contextual inventory views
- links to crafting, quest, and market-related pages

### Crafting and preparation

- recipe catalog and craft actions
- live crafting context
- preparation and stock comparison flows
- item-oriented planning and simulation

### Gathering, maps, and weather

- gathering catalog and point tracking, timed nodes with Eorzea windows
- **Tensho map** replacing the game map (key M): game icons with per-map size and priority, quests and leves, FATEs, party,
  gathering and fishing points, aether currents, sightseeing vistas, NPC vendors, flag, teleport with real gil cost, local lists by tab
- weather and environmental context
- gathering-related observation workflows

### Quests, collections, and progression

- quest-related metadata and source tracking
- collection tracking with « how to obtain » (vendors, exchanges, quests, achievements, crafting, Garland Tools and wiki as fallbacks)
- duty and monster loot data
- progression-oriented integration with game data

### Market and pricing

- market service integration
- contextual price validation
- comparisons tied to crafting and inventory preparation

### Macros, mini-games, and utility modules

- macro observation and chaining
- mini-game helpers
- Gold Saucer-related tooling
- fashion and visual context support

### Diagnostics and localization

- `LocalizationManager`
- developer mode and debug pages
- translation reload support
- JSON-backed language files under `src/TenshoAssistant/Localization/Languages`

## Repository structure

```text
.
├── src/TenshoAssistant/      plugin code (one folder per domain; *.Dalamud.cs = game access)
├── tests/                    managed tests (pure logic)
├── tools/                    read-only data probes (game sheets, APIs)
├── scripts/Verify.ps1        build + tests + localization + package
├── docs/                     documentation map: docs/README.md
│   ├── ai/                   agent steering (rules, status, goal)
│   ├── project/              scope, roadmap, architecture, testing, references, decisions
│   ├── goals/                one document per delivered goal
│   └── evidence/             probe outputs cited by the code
├── Localization/README.md    translation guide
├── README.md · CONTRIBUTING.md · CHANGELOG.md · CLAUDE.md · LICENSE
└── global.json · TenshoAssistant.sln
```

## Build and verification

The repository includes a central verification script:

```powershell
.\scripts\Verify.ps1
```

For a custom .NET SDK location:

```powershell
.\scripts\Verify.ps1 -DotNet 'C:\path\to\dotnet.exe'
```

The project documentation indicates these are the expected output paths:

- DLL: `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`
- Package: `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip`

This script is the main validation path for build quality and project checks.

## Installation in-game

1. Add the absolute DLL path to the Dalamud dev plugins list.
2. Load the plugin.
3. Open the window with:

```text
/tensho
```

4. Validate preferences, reload behavior, and unloading behavior.

## Localization

The project supports JSON-based localization. The rules are documented in `Localization/README.md`:

- copy `src/TenshoAssistant/Localization/Languages/en.json`
- create a localized file for the language (for example `fr.json`, `de.json`, `ja.json`)
- keep the translation keys unchanged
- use English as the reference language
- allow fallback to English if a key is missing

## Documentation to consult

- [docs/README.md](docs/README.md) — map of all documents
- [CONTRIBUTING.md](CONTRIBUTING.md) — build, tests, project rules, Dalamud publishing rules
- [CHANGELOG.md](CHANGELOG.md) — what changed in each version
- [docs/ai/STATUS.md](docs/ai/STATUS.md) — current status, what is validated in game, next action
- [docs/project/ROADMAP.md](docs/project/ROADMAP.md) · [ARCHITECTURE.md](docs/project/ARCHITECTURE.md) · [TESTING.md](docs/project/TESTING.md)
- [Localization/README.md](Localization/README.md) — localization workflow

## AI usage disclosure

This plugin is developed with an AI coding agent (Claude, « Auto » level) under the direction of its author, who defines the features and
tests every version in game. Data links are proven with read-only probes ([docs/evidence/](docs/evidence/)). See
[CONTRIBUTING.md](CONTRIBUTING.md#publication-dalamud) for the Dalamud AI policy. The plugin icon (`src/TenshoAssistant/images/icon.png`)
was also generated by the AI agent.

## Credits

This project depends on and integrates with the following tools and ecosystems:

- Final Fantasy XIV and its game data, content, and in-game systems
- Dalamud, the plugin framework used to run the addon in-game
- Dalamud.NET SDK and Dalamud API bindings used by the project
- .NET SDK and runtime used to build and validate the plugin
- PowerShell scripts used for project verification and build automation
- community documentation, references, and game data sources used for feature implementation and validation:
  [Garland Tools](https://www.garlandtools.org/) (item sources), the [FFXIV Console Games Wiki](https://ffxiv.consolegameswiki.com/)
  (monster drops, sightseeing log, acquisition), [Universalis](https://universalis.app/) (market prices)
- reference plugins studied without copying code: Mappy, SortaKinda (AGPL) and the other reference plugins listed in `docs/project/ROADMAP.md`
- level modifier table (main stat, levels 1–100) from Character Panel Refined by Kouzukii (MIT)
- duty loot and item source data from [LuminaSupplemental](https://github.com/Critical-Impact/LuminaSupplemental) by Critical-Impact (GPL-3.0)
- the localization and translation workflow used for UI strings and language files

This project is an original plugin implementation, but it relies on these ecosystem components and references for its operation, testing, and validation.

## Notes

- The project is a full-feature FFXIV utility plugin and not only an item tracker.
- The UI is multi-page and domain-oriented.
- The repository is active and structured around shared services, cached data, and modular gameplay assistance.
- Exact module coverage should always be verified against the project documentation rather than inferred from older references.

## Summary

TenshoAssistant is a multi-tool Dalamud plugin intended to centralize useful gameplay support surfaces into a single interface. The current codebase demonstrates a modular architecture with shared catalogs, domain services, and a rich main window that spans inventory, gathering, crafting, quest support, collections, market information, and more. The project documentation remains the definitive reference for maturity, scope, and current status of each feature area.

---

# TenshoAssistant

Version française / French version

TenshoAssistant est un plugin Dalamud pour Final Fantasy XIV conçu pour regrouper dans une interface unique un ensemble d’outils d’assistance au gameplay : inventaire, artisanat, récolte, cartes, quêtes, collections, marché, macros, mini-jeux et diagnostics.

Le dépôt montre une architecture modulaire orientée services, avec des catalogues partagés, un point d’entrée principal et une interface multi-pages qui coordonne plusieurs domaines fonctionnels. Le niveau de maturité de chaque fonction est suivi dans [docs/ai/STATUS.md](docs/ai/STATUS.md) et [CHANGELOG.md](CHANGELOG.md) ; carte des documents : [docs/README.md](docs/README.md).

## Vue d’ensemble

TenshoAssistant ne se limite pas à un simple aide-mémoire d’inventaire. Le code actuel met en place un assistant unifié avec une fenêtre principale (`MainWindow`) et plusieurs modules thématiques qui partagent des données et des services.

La fenêtre principale expose notamment :

- Accueil
- Objets & inventaire
- Récolte & cartes
- Quêtes
- Marché
- Collections
- Crafting / recettes / simulations
- Macro Chain
- Mini-jeux / Gold Saucer
- Paramètres
- Diagnostics

## Architecture actuelle

Le point d’entrée principal est `src/TenshoAssistant/MainWindow.cs`.

L’interface est structurée autour de :

- une navigation latérale
- un espace de contenu changeant selon la page active
- un contexte d’item sélectionné partagé
- l’intégration avec les services Dalamud (`IPluginLog`, `ITextureProvider`, `IGameGui`, etc.)

L’implémentation suit une logique orientée services :

- `MainWindow` orchestre l’interface et la navigation
- les services métier gèrent les domaines de gameplay
- les catalogues partagés centralisent les données d’objets, recettes, marchés et cartes
- la localisation et les diagnostics sont intégrés au runtime

## Domaines fonctionnels présents

### Inventaire et objets

- recherche d’objets par nom ou ID
- affichage des stocks et du contexte de quantité
- navigation liée à l’objet sélectionné
- intégration avec crafting, marché et quêtes

### Crafting

- catalogues de recettes
- actions de craft
- simulations et préparation
- comparaison de stocks / planification

### Récolte, cartes et météo

- points de récolte et navigation de zone, points à horaire
- **carte Tensho** qui remplace la carte du jeu (touche M) : icônes du jeu (taille, priorité, par carte), quêtes et mandats, ALEA, groupe,
  points de récolte et de pêche, courants éthérés, repères d'exploration, vendeurs, drapeau, téléportation au coût réel, listes par onglet
- cartes et contexte géographique
- données météo et conditions de récolte

### Quêtes, collections et progression

- gestion des progrès et des sources d’objets
- suivi des collections
- données de loot et de contenu

### Marché et prix

- services de marché
- contexte de prix et comparaison de stock
- intégration avec les objets et la préparation

### Macros, mini-jeux et utilitaires

- observation de macro
- chaînes de macro
- mini-jeux et Gold Saucer
- aide de contexte pour le personnage et le style

### Diagnostics et localisation

- mode développeur
- page de diagnostics
- rechargement des traductions
- support des fichiers JSON de langue

## Structure du dépôt

Voir la structure anglaise ci-dessus et la carte des documents [docs/README.md](docs/README.md).

## Build et validation

Script de validation principal :

```powershell
.\scripts\Verify.ps1
```

Avec un SDK spécifique :

```powershell
.\scripts\Verify.ps1 -DotNet 'C:\chemin\dotnet.exe'
```

Sorties attendues documentées :

- DLL : `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`
- Paquet : `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant\latest.zip`

## Installation en jeu

1. Ajouter le chemin de la DLL dans la liste des plugins de développement de Dalamud.
2. Charger le plugin.
3. Ouvrir la fenêtre avec :

```text
/tensho
```

4. Vérifier le comportement des préférences, du rechargement et du déchargement.

## Localisation

La localisation est gérée via des fichiers JSON et documentée dans `Localization/README.md` :

- copier `src/TenshoAssistant/Localization/Languages/en.json`
- créer la variante locale (par exemple `fr.json`)
- conserver les clés existantes
- garder l’anglais comme langue de référence
- permettre les fallbacks si une clé manque

## Documentation utile

- [docs/README.md](docs/README.md) : carte de tous les documents
- [CONTRIBUTING.md](CONTRIBUTING.md) : compiler, tester, règles du projet, publication Dalamud
- [CHANGELOG.md](CHANGELOG.md) : changements par version
- [docs/ai/STATUS.md](docs/ai/STATUS.md) : état, validé en jeu, prochaine action
- [Localization/README.md](Localization/README.md) : contribution à la localisation

## Usage de l'IA

Ce plugin est développé avec un agent IA (Claude, niveau « Auto ») sous la direction de son auteur, qui définit les fonctions et teste chaque
version en jeu. Les liens de données sont prouvés par des sondes en lecture seule ([docs/evidence/](docs/evidence/)). L'icône du plugin a aussi été générée par l'agent IA.

## Crédits

Ce projet dépend des outils et écosystèmes suivants :

- Final Fantasy XIV et ses données de jeu, contenus et systèmes in-game
- Dalamud, le framework de plugin utilisé pour exécuter l’addon en jeu
- SDK .NET et runtime .NET utilisés pour compiler et valider le plugin
- PowerShell, utilisé dans les scripts de vérification et de build
- données communautaires : [Garland Tools](https://www.garlandtools.org/), le [wiki FFXIV](https://ffxiv.consolegameswiki.com/), [Universalis](https://universalis.app/)
- plugins de référence étudiés sans copie de code : Mappy, SortaKinda (AGPL) et les autres références de `docs/project/ROADMAP.md`
- le workflow de localisation et les fichiers de langue utilisés pour les libellés d’interface

Ce projet est une implémentation originale, mais il s’appuie sur ces composants et références de l’écosystème pour son fonctionnement et sa validation.

## Résumé

TenshoAssistant est un plugin Dalamud multi-outils orienté assistance au gameplay. Son architecture actuelle est cohérente avec un système de services partagés, des catalogues centralisés et une interface unifiée qui agrège plusieurs domaines utiles au joueur. La documentation du projet reste la source de vérité pour le périmètre exact, le niveau de maturité et les priorités de chaque fonctionnalité.

