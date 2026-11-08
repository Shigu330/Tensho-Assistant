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
| `ApiProbe` | Members of Dalamud, FFXIVClientStructs and Lumina types by reflection on metadata only (`--find-method=`, `--find-struct=`, `--find-excel=`, `--find-triad`, or a full type name): used before calling a native API. Reads `%AppData%\XIVLauncher\addon\Hooks\dev`. |
| `discoveries-merge.py` | Maintainer tool (X-77, 0.882): builds `community-discoveries/discoveries.json` from what players shared, with the plugin's validity rules. `sync [--dry-run]` reads the open « Discoveries » issues through the GitHub CLI and the gist each one links (network), keeps each player's valid facts in `community-discoveries/players/<issue>.json` and rebuilds (`--dry-run`: report only, nothing written); `rebuild` rebuilds from the player files (offline); `check <file.json\|file.zip\|gist:ID>` validates one file without writing. |
| `DiscoveryLoadProbe` | X-93 (0.938): load test of the discoveries base with invented facts (load, write, read, search times, file size); offline, writes only to a temporary folder. Arguments: the sizes to try (default 100 000, 200 000 and 1 000 000). |
| `DiscoveryCheckProbe` | The facts the plugin's parsers recorded in game (`Discoveries.json`) and the market prices seen (`MarketSeen.json`), checked against the game sheets: rows exist (BNpcName, Fate, Item, RetainerTask, Weather, ENpcResident, Quest, SpearfishingItem…), territory and map agree, places on the map, cells, levels, dates, duplicates, FATE layout ranges, FishingSpot and spearfishing GatheringPoint lists, weather forecast at the catch, marketable items and price outliers. Arguments: sqpack folder, then the Discoveries folder; read-only, prints `IMPOSSIBLE` / `SUSPECT` / `NOTE` per fact. |
| `XbmProbe` | Every column of the Beastmaster (XBM) sheets, read raw: sqpack directory, then a language and optionally sheet names; `PROBE_MACRO=1` prints texts with their macro codes. |
| `ShopCurrencyProbe` | The general probe: one block per `PROBE_*` environment variable (shops and currencies, NPC positions, vessel parts, gear and materia, quest genres and start cities, map markers, gathering levels, event icons, ocean fishing sheets, S rank hunt marks…). Set the variable(s), run, keep the output as evidence. Variables: `PROBE_ACH`, `PROBE_AIRSHIP`, `PROBE_ALARMCMD`, `PROBE_BARDING`, `PROBE_BINGO`, `PROBE_CHOICES`, `PROBE_COLLECTABLES`, `PROBE_CRAFTACTIONS`, `PROBE_CRAFTFOOD`, `PROBE_CRYSTAL`, `PROBE_CURRENCY`, `PROBE_EVENTICONS`, `PROBE_FCC`, `PROBE_FESTIVAL`, `PROBE_FESTIVALNPC`, `PROBE_FISHSPOT`, `PROBE_FOLKLORE`, `PROBE_GATHERBONUS`, `PROBE_GATHERLEVEL`, `PROBE_GATHERTYPE`, `PROBE_GEAR`, `PROBE_GPACTIONS`, `PROBE_GROW`, `PROBE_GTYPE`, `PROBE_HOUSING`, `PROBE_HUNT`, `PROBE_LEGENDS`, `PROBE_LGB`, `PROBE_LOCAL`, `PROBE_LOGS`, `PROBE_MAPS`, `PROBE_MARKERS`, `PROBE_MARKERS_AT`, `PROBE_MATERIARATE`, `PROBE_MELD`, `PROBE_MOBHUNT`, `PROBE_NOTORIOUS`, `PROBE_NPC`, `PROBE_OCEAN`, `PROBE_OCEANFISH`, `PROBE_OCEANLIVE`, `PROBE_QUESTGENRE`, `PROBE_QUESTODD`, `PROBE_RECIPEROWS`, `PROBE_REDUCE`, `PROBE_ROULETTE`, `PROBE_SELLS`, `PROBE_SRANK`, `PROBE_STARTCITY`, `PROBE_SUBROUTES`, `PROBE_TALK`, `PROBE_TRIAD`, `PROBE_UNLOCKS`, `PROBE_VESSELPARTS`, `PROBE_ZONE`. The plugin's contributor panel (Diagnostics, developer mode) explains ten of them and copies an example command. |
| `CollectionProbe` | The collections projection (mounts, minions, cards…) built from the game sheets, and that it stays usable after the game data is released. |
| `ConfigSerializationProbe` | That the plugin configuration round-trips through Newtonsoft.Json as Dalamud saves it. |
| `CraftSimulationProbe` | The crafting simulator and solver against real recipes (collectables included). |
| `FashionProbe` | Fashion Report sheets (weekly themes, slots) used by the Fashion pages; `--fetch` also makes the two fixed public dataset requests. |
| `FishingNotebookProbe` | The fishing log sheets (spots, fish, notebook order). |
| `GatheringActionsProbe` | Gathering actions and their effects, for the GP planner. |
| `GatheringProbe` | Gathering points and their Eorzea time windows converted to real time; one network check at a time on request: `--duty-loot=<finder id>`, `--monster-loot=<item id>`, `--fish-baits=<item id>`. |
| `GatheringRequirementsProbe` | Gathering requirements (levels, perception, unlocks) of points and items; `--compare-source` compares the gathering levels with GatherBuddyReborn's table at a fixed commit (network). |
| `GatheringUiProbe` | The gathering window's layout values the gathering helper reads. |
| `ItemUseProbe` | Where an item is used (recipes, Grand Company, quests…), for the Uses tab. |
| `QuestRewardsProbe` | Quest reward rows (fixed and optional items, currencies) for the quest and item source projections. |
| `ToolBonusProbe` | Bonuses of gathering and crafting tools. |
| `UldProbe` | Interface layouts (`.uld`) of the game: which exist, the textures they use (`--tex` sizes, `--parts` the cut the game declares). Proves the game frame textures (`docs/evidence/0.332-uld-window-frame.md`). |
| `FateRangeProbe` | Which layout file of a territory holds the range named by `Fate.Location` and `DynamicEvent.LGBMapRange`, with its position. Arguments: sqpack folder, a language, then territory rows or `all`. |
| `MapMaskProbe` | A map's texture files and discovery columns; with `--pixels <folder>`, the colours of its masks and a raw copy (P11, `docs/evidence/0.766-map-discovery.md`); `--png <folder>` writes pictures, and `cells.py <folder>` counts each region against the outline (`docs/evidence/0.803-map-discovery-layout.md`). Other modes: `--tex <game path> <out.png>` (any game texture as a picture), `--item <name>` (an item's gathering points), `--markers <place>` (a zone map's fixed markers and aetherytes), `--lgb <territory row>` and `--npc <id>` (event layers and the NPCs they place). |
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
| `ApiProbe` | Les membres des types de Dalamud, FFXIVClientStructs et Lumina, par réflexion sur les métadonnées seulement (`--find-method=`, `--find-struct=`, `--find-excel=`, `--find-triad`, ou un nom de type complet) : avant d'appeler une API native. Lit `%AppData%\XIVLauncher\addon\Hooks\dev`. |
| `discoveries-merge.py` | Outil du mainteneur (X-77, 0.882) : construit `community-discoveries/discoveries.json` à partir de ce que les joueurs ont partagé, avec les règles de validité du plugin. `sync [--dry-run]` lit les tickets « Discoveries » ouverts par la CLI GitHub et le gist que chacun indique (réseau), garde les faits valides de chaque joueur dans `community-discoveries/players/<ticket>.json` puis reconstruit (`--dry-run` : rapport seul, rien n'est écrit) ; `rebuild` reconstruit à partir des fichiers des joueurs (hors ligne) ; `check <fichier.json\|fichier.zip\|gist:ID>` valide un fichier sans rien écrire. |
| `DiscoveryLoadProbe` | X-93 (0.938) : essai de charge de la base de découvertes avec des faits inventés (temps de chargement, d'écriture, de lecture et de recherche, taille du fichier) ; hors ligne, n'écrit que dans un dossier temporaire. Arguments : les tailles à essayer (par défaut 100 000, 200 000 et 1 000 000). |
| `DiscoveryCheckProbe` | Les faits relevés en jeu par les analyseurs du plugin (`Discoveries.json`) et les prix vus au marché (`MarketSeen.json`), vérifiés contre les feuilles du jeu : lignes existantes (BNpcName, Fate, Item, RetainerTask, Weather, ENpcResident, Quest, SpearfishingItem…), territoire et carte cohérents, lieux sur la carte, cellules, niveaux, dates, doublons, zones des ALÉA dans la disposition, listes des FishingSpot et des GatheringPoint de pêche au harpon, météo prévue à la prise, objets vendables et prix aberrants. Arguments : dossier sqpack, puis dossier Discoveries ; en lecture seule, affiche `IMPOSSIBLE` / `SUSPECT` / `NOTE` par fait. |
| `XbmProbe` | Toutes les colonnes des feuilles du dresseur (XBM), lues brutes : dossier sqpack, puis une langue et, au choix, des noms de feuilles ; `PROBE_MACRO=1` affiche les textes avec leurs codes de macro. |
| `ShopCurrencyProbe` | La sonde générale : un bloc par variable d'environnement `PROBE_*` (boutiques et monnaies, positions des PNJ, pièces de vaisseaux, équipement et matérias, genres de quêtes et cités de départ, marqueurs de carte, niveaux de récolte, icônes d'événements, feuilles de la pêche en mer, monstres de rang S…). Réglez la ou les variables, lancez, gardez la sortie comme preuve. Variables : `PROBE_ACH`, `PROBE_AIRSHIP`, `PROBE_ALARMCMD`, `PROBE_BARDING`, `PROBE_BINGO`, `PROBE_CHOICES`, `PROBE_COLLECTABLES`, `PROBE_CRAFTACTIONS`, `PROBE_CRAFTFOOD`, `PROBE_CRYSTAL`, `PROBE_CURRENCY`, `PROBE_EVENTICONS`, `PROBE_FCC`, `PROBE_FESTIVAL`, `PROBE_FESTIVALNPC`, `PROBE_FISHSPOT`, `PROBE_FOLKLORE`, `PROBE_GATHERBONUS`, `PROBE_GATHERLEVEL`, `PROBE_GATHERTYPE`, `PROBE_GEAR`, `PROBE_GPACTIONS`, `PROBE_GROW`, `PROBE_GTYPE`, `PROBE_HOUSING`, `PROBE_HUNT`, `PROBE_LEGENDS`, `PROBE_LGB`, `PROBE_LOCAL`, `PROBE_LOGS`, `PROBE_MAPS`, `PROBE_MARKERS`, `PROBE_MARKERS_AT`, `PROBE_MATERIARATE`, `PROBE_MELD`, `PROBE_MOBHUNT`, `PROBE_NOTORIOUS`, `PROBE_NPC`, `PROBE_OCEAN`, `PROBE_OCEANFISH`, `PROBE_OCEANLIVE`, `PROBE_QUESTGENRE`, `PROBE_QUESTODD`, `PROBE_RECIPEROWS`, `PROBE_REDUCE`, `PROBE_ROULETTE`, `PROBE_SELLS`, `PROBE_SRANK`, `PROBE_STARTCITY`, `PROBE_SUBROUTES`, `PROBE_TALK`, `PROBE_TRIAD`, `PROBE_UNLOCKS`, `PROBE_VESSELPARTS`, `PROBE_ZONE`. Le panneau contributeur du plugin (Diagnostics, mode développeur) en explique dix et copie une commande d'exemple. |
| `CollectionProbe` | La projection des collections (montures, mascottes, cartes…) construite à partir des feuilles du jeu, et qu'elle reste utilisable après la libération des données du jeu. |
| `ConfigSerializationProbe` | Que la configuration du plugin fait l'aller-retour par Newtonsoft.Json comme Dalamud l'enregistre. |
| `CraftSimulationProbe` | Le simulateur et le solveur d'artisanat contre de vraies recettes (objets de collection compris). |
| `FashionProbe` | Les feuilles de la Revue de mode (thèmes de la semaine, emplacements) utilisées par les pages de la Revue de mode ; `--fetch` fait aussi les deux requêtes fixes aux jeux de données publics. |
| `FishingNotebookProbe` | Les feuilles du carnet de pêche (lieux, poissons, ordre du carnet). |
| `GatheringActionsProbe` | Les actions de récolte et leurs effets, pour le planificateur de PR. |
| `GatheringProbe` | Les points de récolte et leurs créneaux en heure d'Éorzéa convertis en heure réelle ; une vérification réseau à la fois, sur demande : `--duty-loot=<id de mission>`, `--monster-loot=<id d'objet>`, `--fish-baits=<id d'objet>`. |
| `GatheringRequirementsProbe` | Les conditions de récolte (niveaux, perception, déblocages) des points et des objets ; `--compare-source` compare les niveaux de récolte à la table de GatherBuddyReborn à un commit fixe (réseau). |
| `GatheringUiProbe` | Les valeurs de disposition de la fenêtre de récolte que lit l'aide à la récolte. |
| `ItemUseProbe` | Où un objet sert (recettes, Grande Compagnie, quêtes…), pour l'onglet Usages. |
| `QuestRewardsProbe` | Les lignes de récompenses des quêtes (objets fixes et au choix, monnaies) pour les projections des quêtes et des sources d'objets. |
| `ToolBonusProbe` | Les bonus des outils de récolte et d'artisanat. |
| `UldProbe` | Les dispositions d'interface (`.uld`) du jeu : lesquelles existent, les textures qu'elles utilisent (`--tex` tailles, `--parts` le découpage déclaré par le jeu). Prouve les textures du cadre du jeu (`docs/evidence/0.332-uld-window-frame.md`). |
| `FateRangeProbe` | Quel fichier de disposition d'un territoire contient la zone nommée par `Fate.Location` et `DynamicEvent.LGBMapRange`, avec sa position. Arguments : dossier sqpack, une langue, puis des lignes de territoire ou `all`. |
| `MapMaskProbe` | Les fichiers de texture d'une carte et ses colonnes de découverte ; avec `--pixels <dossier>`, les couleurs de ses masques et une copie brute (P11, `docs/evidence/0.766-map-discovery.md`) ; `--png <dossier>` écrit des images, et `cells.py <dossier>` compte chaque région contre le contour (`docs/evidence/0.803-map-discovery-layout.md`). Autres modes : `--tex <chemin du jeu> <sortie.png>` (toute texture du jeu en image), `--item <nom>` (les points de récolte d'un objet), `--markers <lieu>` (les marqueurs fixes et les éthérites d'une carte de zone), `--lgb <ligne de territoire>` et `--npc <id>` (les couches d'événement et les PNJ qu'elles placent). |
| `WeatherProbe` | Les taux de météo par territoire et la formule de prévision, l'inconnu gardé inconnu. |
| `submarine-loot.py` | Pas une sonde : refait `src/TenshoAssistant/FreeCompany/Data/submarine-loot.json` à partir du `Submarines.json` de FFXIVGachaSpreadsheet (MIT, avis écrit dans le fichier). |
| `garden-data.py` | Pas une sonde : refait `src/TenshoAssistant/Reminders/Data/garden-seeds.csv` à partir de FFXIV Gardening Tracker (GPL-3.0) et `garden-crosses.csv` à partir de FFXIV Crossbreed Helper (MIT), avec la source, le commit et l'avis de licence écrits dans chaque fichier. À lancer avec `python -I`. |

Chaque sonde affiche des lignes `PASS …` / `FAIL …` ou les lignes brutes qu'elle a lues. Une sonde qui casse après une mise à jour du jeu
est un signal : le fait qu'elle prouvait a pu changer, donc le code qui s'y fie doit être vérifié (voir R-14 dans
`docs/project/USER-REQUESTS.md`).
