# Tools: read-only probes

> *Traduction française plus bas.*

Small console programs that read the **installed game files** (and, for `ApiProbe`, the metadata of Dalamud's assemblies) to prove
a fact before the plugin relies on it. They never write to the game, never run game code, and are not part of the plugin package.
Their findings go to `docs/evidence/` and are cited by the code (project rule: unknown is not zero, never invent data).

They are not in `TenshoAssistant.sln`: build and run each one on its own. Most take the game's `sqpack` folder as first argument:

```powershell
dotnet run --project tools/ShopCurrencyProbe -- "C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack"
```

| Probe | What it checks |
|---|---|
| `ApiProbe` | Members of Dalamud, FFXIVClientStructs and Lumina types by reflection on metadata only (`--find-method=`, `--find-struct=`, `--find-excel=`, or a full type name): used before calling a native API. Reads `%AppData%\XIVLauncher\addon\Hooks\dev`. |
| `discoveries-merge.py` | Maintainer tool (X-77, 0.882): merges the discovery files players shared (zip or json from a « Discoveries » issue) into `community-discoveries/discoveries.json`, with the plugin's validity rules; offline. |
| `DiscoveryLoadProbe` | X-93 (0.938): load test of the discoveries base with invented facts (load, write, read, search times, file size); offline, writes only to a temporary folder. Arguments: the sizes to try. |
| `DiscoveryCheckProbe` | The facts the plugin's parsers recorded in game (`Discoveries.json`) and the market prices seen (`MarketSeen.json`), checked against the game sheets: rows exist (BNpcName, Fate, Item, RetainerTask, Weather, ENpcResident, Quest, SpearfishingItem…), territory and map agree, places on the map, cells, levels, dates, duplicates, FATE layout ranges, FishingSpot and spearfishing GatheringPoint lists, weather forecast at the catch, marketable items and price outliers. Arguments: sqpack folder, then the Discoveries folder; read-only, prints `IMPOSSIBLE` / `SUSPECT` / `NOTE` per fact. |
| `XbmProbe` | Every column of the Beastmaster (XBM) sheets, read raw: sqpack directory, then a language and optionally sheet names. |
| `ShopCurrencyProbe` | The general probe: one block per `PROBE_*` environment variable (shops and currencies, NPC positions, vessel parts, gear and materia, quest genres and start cities, map markers, gathering levels, event icons, ocean fishing sheets with `PROBE_OCEAN`, `PROBE_OCEANLIVE` and `PROBE_OCEANFISH`…). Set the variable(s), run, keep the output as evidence. The list of variables is in the plugin's contributor panel (Diagnostics, developer mode). |
| `CollectionProbe` | The collections projection (mounts, minions, cards…) built from the game sheets, and that it stays usable after the game data is released. |
| `ConfigSerializationProbe` | That the plugin configuration round-trips through Newtonsoft.Json as Dalamud saves it. |
| `CraftSimulationProbe` | The crafting simulator and solver against real recipes (collectables included). |
| `FashionProbe` | Fashion Report sheets (weekly themes, slots) used by the Fashion pages. |
| `FishingNotebookProbe` | The fishing log sheets (spots, fish, notebook order). |
| `GatheringActionsProbe` | Gathering actions and their effects, for the GP planner. |
| `GatheringProbe` | Gathering points and their Eorzea time windows converted to real time. |
| `GatheringRequirementsProbe` | Gathering requirements (levels, perception, unlocks) of points and items. |
| `GatheringUiProbe` | The gathering window's layout values the gathering helper reads. |
| `ItemUseProbe` | Where an item is used (recipes, Grand Company, quests…), for the Uses tab. |
| `QuestRewardsProbe` | Quest reward rows (fixed and optional items, currencies) for the quest and item source projections. |
| `ToolBonusProbe` | Bonuses of gathering and crafting tools. |
| `UldProbe` | Interface layouts (`.uld`) of the game: which exist, the textures they use (`--tex` sizes, `--parts` the cut the game declares). Proves the game frame textures (`docs/evidence/0.332-uld-window-frame.md`). |
| `FateRangeProbe` | Which layout file of a territory holds the range named by `Fate.Location` and `DynamicEvent.LGBMapRange`, with its position. |
| `MapMaskProbe` | A map's texture files and discovery columns; with `--pixels <folder>`, the colours of its masks and a raw copy (P11, `docs/evidence/0.766-map-discovery.md`); `--png <folder>` writes pictures, and `cells.py <folder>` counts each region against the outline (`docs/evidence/0.803-map-discovery-layout.md`). |
| `WeatherProbe` | Weather rates per territory and the forecast formula, unknown kept unknown. |
| `submarine-loot.py` | Not a probe: rebuilds `src/TenshoAssistant/FreeCompany/Data/submarine-loot.json` from FFXIVGachaSpreadsheet's `Submarines.json` (MIT, notice written into the file). |
| `garden-data.py` | Not a probe: rebuilds `src/TenshoAssistant/Reminders/Data/garden-seeds.csv` from FFXIV Gardening Tracker (GPL-3.0) and `garden-crosses.csv` from FFXIV Crossbreed Helper (MIT), with the source, commit and licence notice written into each file. Run it with `python -I`. |

Each probe prints `PASS …` / `FAIL …` lines or the raw rows it read. A probe that breaks after a game update is a signal: the fact
it proved may have changed, so the code that relies on it must be checked (see R-14 in `docs/project/USER-REQUESTS.md`).

---

# Outils : sondes en lecture seule

De petits programmes console qui lisent les **fichiers du jeu installé** (et, pour `ApiProbe`, les métadonnées des assemblys de Dalamud)
pour prouver un fait avant que le plugin s'y fie. Ils n'écrivent jamais dans le jeu, n'exécutent jamais de code du jeu et ne font pas
partie du paquet du plugin. Leurs résultats vont dans `docs/evidence/` et sont cités par le code (règle du projet : inconnu n'est pas zéro,
ne jamais inventer une donnée).

Ils ne sont pas dans `TenshoAssistant.sln` : compilez et lancez chacun à part. La plupart prennent le dossier `sqpack` du jeu comme
premier argument :

```powershell
dotnet run --project tools/ShopCurrencyProbe -- "C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack"
```

| Sonde | Ce qu'elle vérifie |
|---|---|
| `ApiProbe` | Les membres des types de Dalamud, FFXIVClientStructs et Lumina, par réflexion sur les métadonnées seulement (`--find-method=`, `--find-struct=`, `--find-excel=`, ou un nom de type complet) : avant d'appeler une API native. Lit `%AppData%\XIVLauncher\addon\Hooks\dev`. |
| `discoveries-merge.py` | Outil du mainteneur (X-77, 0.882) : fusionne les fichiers de découvertes partagés par les joueurs (zip ou json d'un ticket « Discoveries ») dans `community-discoveries/discoveries.json`, avec les règles de validité du plugin ; hors ligne. |
| `DiscoveryCheckProbe` | Les faits relevés en jeu par les analyseurs du plugin (`Discoveries.json`) et les prix vus au marché (`MarketSeen.json`), vérifiés contre les feuilles du jeu : lignes existantes (BNpcName, Fate, Item, RetainerTask, Weather, ENpcResident, Quest, SpearfishingItem…), territoire et carte cohérents, lieux sur la carte, cellules, niveaux, dates, doublons, zones des ALEA dans la disposition, listes des FishingSpot et des GatheringPoint de pêche au harpon, météo prévue à la prise, objets vendables et prix aberrants. Arguments : dossier sqpack, puis dossier Discoveries ; en lecture seule, affiche `IMPOSSIBLE` / `SUSPECT` / `NOTE` par fait. |
| `XbmProbe` | Toutes les colonnes des feuilles du dresseur (XBM), lues brutes : dossier sqpack, puis une langue et, au choix, des noms de feuilles. |
| `ShopCurrencyProbe` | La sonde générale : un bloc par variable d'environnement `PROBE_*` (boutiques et monnaies, positions des PNJ, pièces de vaisseaux, équipement et matérias, genres de quêtes et cités de départ, marqueurs de carte, niveaux de récolte, icônes d'événements, feuilles de la pêche en mer avec `PROBE_OCEAN`, `PROBE_OCEANLIVE` et `PROBE_OCEANFISH`…). Réglez la ou les variables, lancez, gardez la sortie comme preuve. La liste des variables est dans le panneau contributeur du plugin (Diagnostics, mode développeur). |
| `CollectionProbe` | La projection des collections (montures, mascottes, cartes…) construite à partir des feuilles du jeu, et qu'elle reste utilisable après la libération des données du jeu. |
| `ConfigSerializationProbe` | Que la configuration du plugin fait l'aller-retour par Newtonsoft.Json comme Dalamud l'enregistre. |
| `CraftSimulationProbe` | Le simulateur et le solveur d'artisanat contre de vraies recettes (objets de collection compris). |
| `FashionProbe` | Les feuilles de la Revue de mode (thèmes de la semaine, emplacements) utilisées par les pages de la Revue de mode. |
| `FishingNotebookProbe` | Les feuilles du carnet de pêche (lieux, poissons, ordre du carnet). |
| `GatheringActionsProbe` | Les actions de récolte et leurs effets, pour le planificateur de PR. |
| `GatheringProbe` | Les points de récolte et leurs créneaux en heure d'Éorzéa convertis en heure réelle. |
| `GatheringRequirementsProbe` | Les conditions de récolte (niveaux, perception, déblocages) des points et des objets. |
| `GatheringUiProbe` | Les valeurs de disposition de la fenêtre de récolte que lit l'aide à la récolte. |
| `ItemUseProbe` | Où un objet sert (recettes, Grande Compagnie, quêtes…), pour l'onglet Usages. |
| `QuestRewardsProbe` | Les lignes de récompenses des quêtes (objets fixes et au choix, monnaies) pour les projections des quêtes et des sources d'objets. |
| `ToolBonusProbe` | Les bonus des outils de récolte et d'artisanat. |
| `UldProbe` | Les dispositions d'interface (`.uld`) du jeu : lesquelles existent, les textures qu'elles utilisent (`--tex` tailles, `--parts` le découpage déclaré par le jeu). Prouve les textures du cadre du jeu (`docs/evidence/0.332-uld-window-frame.md`). |
| `FateRangeProbe` | Quel fichier de disposition d'un territoire contient la zone nommée par `Fate.Location` et `DynamicEvent.LGBMapRange`, avec sa position. |
| `MapMaskProbe` | Les fichiers de texture d'une carte et ses colonnes de découverte ; avec `--pixels <dossier>`, les couleurs de ses masques et une copie brute (P11, `docs/evidence/0.766-map-discovery.md`) ; `--png <dossier>` écrit des images, et `cells.py <dossier>` compte chaque région contre le contour (`docs/evidence/0.803-map-discovery-layout.md`). |
| `WeatherProbe` | Les taux de météo par territoire et la formule de prévision, l'inconnu gardé inconnu. |
| `submarine-loot.py` | Pas une sonde : refait `src/TenshoAssistant/FreeCompany/Data/submarine-loot.json` à partir du `Submarines.json` de FFXIVGachaSpreadsheet (MIT, avis écrit dans le fichier). |
| `garden-data.py` | Pas une sonde : refait `src/TenshoAssistant/Reminders/Data/garden-seeds.csv` à partir de FFXIV Gardening Tracker (GPL-3.0) et `garden-crosses.csv` à partir de FFXIV Crossbreed Helper (MIT), avec la source, le commit et l'avis de licence écrits dans chaque fichier. À lancer avec `python -I`. |

Chaque sonde affiche des lignes `PASS …` / `FAIL …` ou les lignes brutes qu'elle a lues. Une sonde qui casse après une mise à jour du jeu
est un signal : le fait qu'elle prouvait a pu changer, donc le code qui s'y fie doit être vérifié (voir R-14 dans
`docs/project/USER-REQUESTS.md`).
