# Références

## Sources externes utilisées par le code — état au 0.315.0 (2026-11-02, relu contre le code ; remplace l'état au 0.154.0)

Ordre (DEC-011) : données du jeu d'abord (Lumina / sqpack, structures FFXIVClientStructs), puis sources communautaires. Une source
communautaire absente ou désactivée donne « inconnu », jamais une valeur devinée. Interrupteurs : Paramètres › Avancé › Sources
communautaires (LuminaSupplemental, Garland, wiki, GatherBuddy, Teamcraft, prix au survol Universalis).

### Lues à l'exécution (réseau)

| Service | Licence | Adresse appelée | Fichier | Déclenchement | Cache |
|---|---|---|---|---|---|
| Universalis | API publique (licence non relevée) | `universalis.app/api/v2/aggregated/{monde}/{objet}` ; `/api/v2/{monde, centre ou région}/{objet}?listings=…&entries=…` ; `/api/v2/data-centers` | `Market/MarketService.cs`, `Market/MarketBoardService.cs` | prix : clic ou survol (option) ; annonces et historique : page Marché seulement (0.270) | « Prix » ; annonces aussi sur disque `market-board` (0.281) |
| Garland Tools | MIT (code, REF-QUEST-REWARDS-003) | `garlandtools.org/db/doc/{route}/{id}.json` (objets, donjons, monstres) | `Community/GarlandClient.cs`, `Duties/DutyLootService.cs`, `Monsters/MonsterLootService.cs` | sources d'objet seulement si aucune source du jeu n'est connue ; butin des contenus et des monstres | « Sources » (`sources`) ; butin 30 min en mémoire |
| Wiki FFXIV (consolegameswiki) | licence du contenu non relevée | API `parse` et `index.php?action=raw` | `Monsters/WikiMonsterSources.cs`, `Community/WikiItemAcquisition.cs`, `Maps/WikiVistas.cs` | survol ou consultation ; carnet d'exploration au survol d'un repère | « Sources » (`wiki-items`, `wiki-monsters`, `wiki-vistas`) |
| LuminaSupplemental (Critical-Impact) | GPL-3.0 | `raw.githubusercontent.com/Critical-Impact/LuminaSupplemental/main/src/LuminaSupplemental.Excel/Generated/*.csv` | `Community/LuminaSupplemental.cs` | premier usage : butin des contenus (coffres, boss), sources d'objets (monstres, ALEA, entreprises, ItemSupplement, boutique, PNJ) | « Sources » (`lumina-supplemental`) |
| GatherBuddy (Ottermandias) | Apache-2.0 | `api.github.com/repos/Ottermandias/GatherBuddy/contents/…/Data/Fish` puis `raw.githubusercontent.com/…/Data/Fish/Data*.cs` | `Community/GatherBuddyFish.cs` | aide à la pêche, Récolte (poissons) ; fichiers lus comme données (REF-FISH-GATHERBUDDY) | « Sources » (`gatherbuddy-fish`) |
| Teamcraft (temps de touche) | MIT (code) ; relevés publics de ses utilisateurs, licence des données non relevée | `gubal.ffxivteamcraft.com/graphql` | `Community/FishBiteTimes.cs` | une requête par lieu de pêche (0.231) | « Sources » (`teamcraft-bites`) |
| FFXIVGachaSpreadsheet (Infi) | MIT | `raw.githubusercontent.com/Infiziert90/FFXIVGachaSpreadsheet/refs/heads/master/website/static/data/` (`FashionReport.json`, `LastUpdate.json`) | `Fashion/FashionDataService.cs` | onglet Revue de mode | « Revue de mode » (`fashion`) |

Source écartée : EorzeaWorld (`eorzeaworld.com`, relevé le 2026-11-06). Sa page À propos interdit l'usage de ses données dans les plugins, addons et outils tiers que les règles de FFXIV interdisent, et la reprise de ses contenus et scripts sans autorisation préalable ; ses pages de contenu répondent par une vérification humaine, à ne pas contourner. Aucune de ses données dans le plugin, sauf accord écrit de son auteur.

Bornes : Garland, wiki, LuminaSupplemental, GatherBuddy et Revue de mode passent par `Community/BoundedHttp.cs` (taille et durée
bornées) ; Universalis a ses propres bornes (`MarketService` 1 Mo / 12 s ; `MarketBoardService` 4 Mo / 15 s, une requête à la fois,
3 s d'écart) ; Teamcraft : délai 20 s, réponse refusée au-delà d'un million de caractères (contrôle fait après la lecture). Cache
disque commun `Caching/DataCache.cs`, durées dans Paramètres › Données (dont « Jamais (seulement sur demande) », 0.281) ; 5 min
d'attente après un échec et copie expirée en secours (0.246). Aucune ne contacte les serveurs du jeu. Détail :
[0.246-cache-audit.md](../evidence/0.246-cache-audit.md).

### Embarquées dans la DLL (données communautaires sous licence compatible, DEC-021)

| Donnée | Source et licence | Fichier embarqué | Avis de licence | Depuis |
|---|---|---|---|---|
| Seuils de butin des secteurs de sous-marin (143 lignes : T2 / T3, Normal / Optimal, faveur) | SubmarineTracker `Data/Sectors.cs` MapBreakpoints, commit 04c54d770f815cd416bbade1ac5b178b551a32b5, MIT (c) 2023 Infi ; à l'origine feuille « Submersible Loot Spreadsheet » (Elena / saerielle, sans licence) | `FreeCompany/Data/submarine-breakpoints.csv` | texte MIT complet en tête du fichier ; README | 0.315 |
| Butin moyen par secteur de sous-marin (55 Ko depuis 0.335, pools par palier) | FFXIVGachaSpreadsheet `website/static/data/Submarines.json` (relevés communautaires), MIT (c) 2023 Infi ; téléchargé le 2026-11-02, SHA-256 ca900aef1a12425699f870c4ab273795fff4ed2b36f6f5a4abf581205353d44c | `FreeCompany/Data/submarine-loot.json`, produit par `tools/submarine-loot.py` | ligne « Source » (licence et copyright) dans le fichier, sans le texte MIT complet ; README | 0.309 |
| Arbre de déblocage des secteurs de dirigeable (24 lignes) et butin possible (261 lignes, sans quantité ni taux) | LuminaSupplemental `src/LuminaSupplemental.Excel/Generated/AirshipUnlock.csv` et `AirshipDrop.csv`, commit bd265c2b39868387937ae5b0c038593af997e0f9, GPL-3.0 (Critical-Impact) ; SHA-256 dans l'en-tête de chaque fichier | `FreeCompany/Data/airship-unlock.csv`, `FreeCompany/Data/airship-drop.csv` | avis GPL-3.0 en tête des fichiers (même licence que Tensho, texte complet : LICENSE) ; README | 0.336 |
| Modificateur de statistique principale, niveaux 1 à 100 | Character Panel Refined (Kouzukii) `LevelModifiers.cs`, MIT | table dans `Character/StatFormulas.cs` | commentaire de source (MIT) sans le texte de licence ; README, Paramètres › Avancé | 0.184 |

Les fichiers de données sont des `EmbeddedResource` (`TenshoAssistant.csproj`) : rien n'est livré à côté de la DLL (DEC-017).

### Règles communautaires reprises comme faits (code original, présentées comme estimations, DEC-022)

| Règle | Source | Code | Statut |
|---|---|---|---|
| Bonus de Récolte abondante II (+3 / +2 / +1 selon ⌊base × 1,1⌋ et ⌊base × 0,9⌋) | frozen_rabbit_tome (sans licence, fait relevé) ; GatheringOptimizer (MIT) laisse la valeur manuelle | outil PR, « estimé », modifiable | 0.224 ; [preuve](../evidence/0.224-bountiful-base.md) ; à valider en jeu |
| Distance et durée d'un trajet de sous-marin (étapes, relevés, 12 h fixes, 5 secteurs au plus) | SubmarineTracker (MIT) `SubExplPretty.cs`, `Data/Voyage.cs`, lus | `FreeCompany/VoyagePlanner.cs` | 0.309 ; durée réelle non vérifiée en jeu |
| Usure : (335 + rang du secteur − rang de la pièce) × 7 par secteur, 30 000 par pièce | feuille « Submersibles durability damage », reprise par SubmarineTracker | `FreeCompany/VesselBuilds.cs` | 0.315 ; estimation |
| Coût total des pièces ≤ capacité | outils communautaires (SubmarineTracker, Argus), cohérente avec 11 montages de dirigeables publiés | `FreeCompany/VesselBuilds.cs` | 0.315 ; non vérifiée en jeu sous le rang 50 |
| Nom court d'un montage (« SSUC++ ») | convention communautaire | `FreeCompany/VesselBuilds.cs` | 0.315 |
| Mirage : toute armure ou tout accessoire, quel que soit le job ou le niveau ; armes et outils liés au job | patch 7.4 (décembre 2025) : presse (PC Gamer, Game Informer) et communiqué du patch, cités en CHANGELOG 0.311 ; pas de sonde des données du jeu | `Gear/GearCatalog.cs` (`AllJobs`), DEC-020 | 0.311 ; page validée en jeu (rapport utilisateur 2026-11-02, 0.310–0.314) |

### Références lues, sans copie de code

Lecture des faits uniquement : Mappy (AGPL-3.0, carte hôte hors écran et liste des icônes survolées), SortaKinda (AGPL-3.0),
AvantGarde (AGPL-3.0, REF-FASHION-AVANTGARDE), GatherBuddy (Apache-2.0), Garland Tools (MIT), FFXIVClientStructs (MIT selon
[0.307-fc-voyages.md](../evidence/0.307-fc-voyages.md) ; `AgentMap`, `MapMarkerData`, `Utf8String`, `PlayerState`, atelier de la
compagnie, `MirageManager`, `UIState.Cabinet`, `AgentTryon`). Depuis 0.235 : SubmarineTracker (MIT, formules 0.309 ; ses seuils
sont embarqués en 0.315, voir ci-dessus), AutoRetainer (0.307, non copié, licence non relevée), Argus (MIT, licence non consignée dans la preuve ; 0.315, règle
« coût ≤ capacité » ; ses seuils de dirigeables viennent de sources sans licence et ne sont pas repris), HimbeertoniRaidTool (MIT,
0.310), Glamourer (Apache-2.0, 0.310), xivgear.app (aucune licence publiée : lu seulement, 0.310). Allagan Tools n'est pas utilisé
(DEC-013). Les preuves de lecture des feuilles du jeu sont dans [docs/evidence/](../evidence/).

## Fiches 0.184 → 0.315 (relues contre le code le 2026-11-02)

> Les fiches 0.213 (REF-FASHION-AVANTGARDE) et 0.227 (REF-FISH-GATHERBUDDY) sont à la fin de l'archive ci-dessous.

### REF-FC-VOYAGES (0.307, 0.309, 0.315) — données du jeu
SOURCE: FFXIVClientStructs (MIT) : `HousingManager.Instance()->WorkshopTerritory` (nul hors de l'atelier),
`HousingWorkshopSubmersibleSubData` (rang, retour, expérience, nom, secteurs du trajet, butin, statistiques Base et Bonus, clés de
pièces), `HousingWorkshopAirshipSubData` (totaux), `HousingManager.IsSubmarineExplorationUnlocked(byte)`. Feuilles :
SubmarineExploration, SubmarineRank, SubmarinePart, SubmarineSpecCategory, AirshipExplorationPart / Point / Level /
ParamType ; pièce ↔ objet par `Item.FilterGroup` 36 / 28 et `Item.AdditionalData`.
EVIDENCE: [0.307-fc-voyages.md](../evidence/0.307-fc-voyages.md), [0.309-submarine-routes.md](../evidence/0.309-submarine-routes.md),
[0.315-vessel-builds.md](../evidence/0.315-vessel-builds.md) (`tools/ApiProbe`, `tools/ShopCurrencyProbe` PROBE_SUBROUTES /
PROBE_VESSELPARTS, lecture seule).
CODE: `FreeCompany/VoyageWatcher.Dalamud.cs` (lecture dans l'atelier), `Voyages.cs`, `VoyagePlanner.cs`, `VesselBuilds.cs`.
LIMITS (non vérifié en jeu) : moment où le jeu remplit ces données (entrée de l'atelier ou panneau des expéditions) ; Base = somme
des pièces et Bonus = bonus de rang (la page affiche ⚠ en cas d'écart) ; durée réelle d'un voyage ; lecture des secteurs débloqués.

### REF-FC-SUBMARINETRACKER (0.309 lu, 0.315 données embarquées)
REPOSITORY: https://github.com/Infiziert90/SubmarineTracker — MIT, (c) 2023 Infi.
COMMIT_OR_VERSION: 04c54d770f815cd416bbade1ac5b178b551a32b5 pour la copie des seuils (0.315) ; commit de la lecture 0.309 non consigné.
FILES_READ: `SubExplPretty.cs` (distance et durée d'une étape), `Data/Voyage.cs` (12 h fixes, ordre de distance minimale, 5 secteurs
au plus), `Data/Sectors.cs` (MapBreakpoints).
USAGE: formules comprises, code Tensho original (0.309) ; MapBreakpoints copiés tels quels avec l'avis MIT (0.315), affichés comme
« seuils communautaires ». Écart voulu : un secteur sans seuil rend le trajet « inconnu » (SubmarineTracker renvoie 0).
LIMITS: contre les minimums observés de FFXIVGachaSpreadsheet, 75 secteurs sur 143 identiques ; la valeur embarquée est sous toutes les
observations dans 19 secteurs (liste dans la preuve 0.315) : un palier « atteint » n'y est confirmé par aucun relevé.

### REF-FC-GACHASPREADSHEET (0.309)
REPOSITORY: https://github.com/Infiziert90/FFXIVGachaSpreadsheet — MIT, (c) 2023 Infi.
USAGE: (1) à l'exécution, `FashionReport.json` pour la Revue de mode (avant 0.235) ; (2) embarqué en 0.309 : `Submarines.json`
(143 secteurs, environ 200 millions d'objets relevés, empreinte ci-dessus) réduit par `tools/submarine-loot.py` au nombre de relevés et
au total vu par objet, tous paliers confondus. Valeur d'un secteur = Σ (Total ÷ Records) × `Item.PriceLow` (données du jeu).
LIMITS: le butin n'est pas dans les données du jeu (décidé côté serveur) ; valeur moyenne sur tous les paliers ; aucun relevé
public équivalent pour les dirigeables.

### REF-FC-ARGUS (0.315)
LICENSE: MIT selon l'équipe de recherche 0.315, mais ni la licence, ni le dépôt, ni le commit ne sont consignés dans la preuve (à relever) ; lu seulement.
USAGE: confirmation de la règle « coût ≤ capacité ». Ses seuils de dirigeables viennent de sources sans licence : non repris, d'où
l'absence de planificateur de trajets de dirigeable (l'arbre de déblocage des dirigeables de LuminaSupplemental, GPL-3.0, est
embarqué depuis 0.336, DEC-023).

### REF-GEAR-R11 (0.310 → 0.314) — données du jeu
SOURCE: Item (EquipSlotCategory, ClassJobCategory, LevelEquip, LevelItem, MateriaSlotCount, IsAdvancedMeldingPermitted,
BaseParam / valeurs), EquipSlotCategory, ClassJobCategory, BaseParam (pourcentages par emplacement), ItemLevel, Materia ;
`InventoryItem.GetMateriaId` / `GetMateriaGrade` ; `AgentTryon.TryOn` (statique).
EVIDENCE: [0.310-gear-sheets.md](../evidence/0.310-gear-sheets.md), [0.312-materia.md](../evidence/0.312-materia.md) (PROBE_GEAR,
PROBE_MELD, lecture seule).
RULES: plafond = arrondi(ItemLevel[statistique] × BaseParam[emplacement] / 1000), égal à la plus haute statistique secondaire de la
pièce (34830 : 111 ; 43047 : 374) ; grades VI, VIII, X et XII exclus en surmatérialisation ; mirage depuis 7.4 (tableau ci-dessus).
READ_ONLY: HimbeertoniRaidTool (MIT), Glamourer (Apache-2.0), xivgear.app (aucune licence) — idées seulement ; dépôts, commits et
fichiers lus non consignés.
LIMITS: taux de réussite de la surmatérialisation non modélisé ; plafond d'une pièce couvrant plusieurs emplacements (pourcentage du
torse) non vérifié en jeu. Pages 0.310–0.314 validées en jeu (rapport utilisateur 2026-11-02).

### REF-BANK-STORAGES (0.235 → 0.240) — données du jeu
SOURCE: `InventoryType` de l'arsenal (3500, 3200–3203, 3205–3209, 3300, 3400 ; ids vérifiés par réflexion de FFXIVClientStructs) ;
`MirageManager.PrismBoxLoaded` / `PrismBoxItemIds` (+1 000 000 HQ, +500 000 collectionnable) ; `UIState.Cabinet.IsCabinetLoaded` /
`IsItemInCabinet` avec la feuille Cabinet ; feuille MirageStoreSetItem (pièces d'une tenue, 0.240).
CODE: `Inventory/BankInventoryBridge.Dalamud.cs`, `Inventory/BankModels.cs` (conteneurs virtuels 25000 et 25001).
LIMITS: coffre et armoire connus seulement après leur ouverture en jeu ; 0.234–0.240 à valider en jeu. Sans autre plugin (DEC-013).

### REF-MARKET-BOARD (0.270, 0.281)
SOURCE: API Universalis, champs sondés le 2026-11-02 ([0.270-universalis-listings.md](../evidence/0.270-universalis-listings.md)) :
`listings[]`, `recentHistory[]`, vitesses de vente ; portée monde sans `worldName`. Règle de jeu : une annonce s'achète en entier.
CODE: `Market/MarketBoard.cs`, `Market/MarketBoardService.cs`.
STATUS: validé en jeu (rapport utilisateur 2026-11-02, 0.270–0.271).

### REF-FISH-TEAMCRAFT (0.231)
REPOSITORY: ffxiv-teamcraft (MIT) ; API GraphQL publique de son client (`apps/client/src/app/apollo-client-factory.ts`), requête
« BiteTimesPerFishPerSpotQuery » (`apps/client/src/app/pages/db/service/fish-data.gql.ts`).
USAGE: `Community/FishBiteTimes.cs` garde, par poisson, les secondes avec au moins 3 relevés et 2 % des relevés du poisson ;
intervalle et seconde la plus fréquente affichés. Preuve : [0.231-teamcraft-bite-times.md](../evidence/0.231-teamcraft-bite-times.md).
STATUS: validé en jeu (capture 2026-11-01).

### REF-CHAR-CPR-LEVELMOD (0.184)
REPOSITORY: Character Panel Refined (Kouzukii), `CharacterPanelRefined/LevelModifiers.cs`, MIT, lu le 2026-10-31.
USAGE: modificateur principal par niveau (absent des feuilles du jeu) embarqué dans `Character/StatFormulas.cs` ; colonnes secondaire
et diviseur concordantes avec ParamGrow aux niveaux 50/70/80/90/100 ([0.182.0-paramgrow-probe.log](../evidence/0.182.0-paramgrow-probe.log)).
STATUS: validé en jeu (« c'est ok », 2026-10-31).

### Preuves de données du jeu 0.246 → 0.300 (sondes en lecture seule)

| Version | Preuve | Fait établi |
|---|---|---|
| 0.246 | [0.246-r05-window-order.md](../evidence/0.246-r05-window-order.md) | Dalamud dessine les fenêtres des plugins après l'interface du jeu, sans API pour les intercaler (DEC-014) |
| 0.278 | [0.278-triad-ex-cards.md](../evidence/0.278-triad-ex-cards.md) | `TripleTriadCardResident.UIPriority` ≠ 0 marque les 15 cartes Ex. |
| 0.283 | [0.283-quest-journal-sections.md](../evidence/0.283-quest-journal-sections.md) | `Quest.JournalGenre` → `JournalCategory` → `JournalSection` |
| 0.285 | [0.285-quest-start-cities.md](../evidence/0.285-quest-start-cities.md) | racines « Une visite s'impose » par cité de départ, chaînes `PreviousQuest` |
| 0.286 | [0.286-quest-alternatives.md](../evidence/0.286-quest-alternatives.md) | `QuestLock` et `PreviousQuestJoin` (une seule quête précédente suffit) |
| 0.292 | [0.292-draw-times.md](../evidence/0.292-draw-times.md) | temps d'affichage relevés en jeu par l'utilisateur (médianes sous 0,5 ms) |
| 0.295 | [0.295-fcc-shops.md](../evidence/0.295-fcc-shops.md) | `FccShop` : objet, coût en crédits de compagnie, rang requis |
| 0.300 | [0.300-calendar-resets.md](../evidence/0.300-calendar-resets.md) | heures de remise à zéro du Calendrier du jeu (capture utilisateur) |

## Fiches de référence (archive)

> Fiches écrites à la date indiquée dans chaque `VERIFIED_AT` ; commits, compteurs de tests et statuts sont ceux de cette date.

## REF-QUEST-CURRENCIES-005-STUDY

DOMAIN: QuestTracker / identités de monnaie.
NAME: Quinze monnaies de quête sans Item direct, hypothèses de compagnie écartées.
REPOSITORY: Sources primaires de REF-QUEST-REWARDS-003 ; Critical-Impact/InventoryTools et API locale.
COMMIT_OR_VERSION: SaintCoinach 0b22b78f97951bb66bfa78fb561aa4c7bf7af7cd ; Garland 04cadd2e1e0de86c20aa9303faa082c7971f8d8b ; InventoryTools 70a9f410d9a8ab97bef6bcf795415ea2d7c75097 ; fichiers installés du 2026-10-29.
LICENSE: Licences consignées dans REF-QUEST-REWARDS-003/REF-INVENTORY ; faits ciblés uniquement, aucune implémentation copiée.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED census ; identité UNKNOWN ; runtime UNKNOWN.
SOURCE: [définition Quest](https://github.com/xivapi/SaintCoinach/blob/0b22b78f97951bb66bfa78fb561aa4c7bf7af7cd/SaintCoinach/Definitions/Quest.json), [renderer InventoryTools](https://github.com/Critical-Impact/InventoryTools/blob/70a9f410d9a8ab97bef6bcf795415ea2d7c75097/InventoryTools/Logic/ItemRenderers/ItemQuestSourceRenderer.cs).
FILES_READ: Trois fichiers InventoryTools cités dans l’étude ; définitions de monnaie/GC, métadonnées GrandCompany/Item/Quest et recherche de bindings ; census natif détaillé.
TECHNICAL_FACTS: CurrencyReward et GCTypeReward nuls dans quinze cas ; neuf prérequis GC explicites, six sans GC. Les monnaies directes connues ont Item.GrandCompany nul et catégorie Autre ; pas de relation unique de résolution démontrée.
LOCAL_COMPATIBILITY: Probe étendu passant, production 0.68 inchangée ; quantités conservées et couverture partielle explicite, pas d’Item inventé.
NOTES: [Étude et limites](../goals/QUEST-CURRENCIES-005-STUDY.md). Inconnu local, aucun blocage global ; poursuivre l’audit de base.

## REF-QUEST-ITEM-SOURCES-004

DOMAIN: QuestTracker / Item Use / snapshots communs.
NAME: Index inverse managé des récompenses de quête.
REPOSITORY: Modèles/projections originaux Tensho ; sources primaires REF-QUEST-REWARDS-003 réutilisées.
COMMIT_OR_VERSION: 0.68.0.0, données natives installées du 2026-10-29 ; aucune nouvelle source externe.
LICENSE: Code du projet ; licences des sources de faits consignées dans REF-QUEST-REWARDS-003, aucune implémentation externe copiée.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: [projection et limites](../goals/QUEST-REWARDS-003.md), [index/lifecycle](../goals/QUEST-ITEM-SOURCES-004.md).
FILES_READ: QuestService/Reader, QuestRewards, usages/fiches et tests existants ; snapshots natifs via QuestRewardsProbe.
TECHNICAL_FACTS: Item/quête groupés sans perdre les variantes, indices complets et couverture explicite ; worker managé, génération/snapshot/annulation empêchent une publication obsolète, erreurs d’index distinctes de progression.
LOCAL_COMPATIBILITY: 434 tests/package verts ; 3 496 Items/8 288 associations et chaque variante concordent avec la projection directe ; Potion 25 quêtes candidates ; hors personnage sans progression confirmée.
NOTES: Pas de disponibilité, choix ou obtention déduits ; quinze monnaies non reliées et quatre quantités inconnues conservées. UI/callbacks en jeu non observés.

## REF-QUEST-REWARDS-003

DOMAIN: QuestTracker / Item et contexte communs.
NAME: Récompenses natives polymorphes et variantes.
REPOSITORY: ufx/GarlandTools ; xivapi/SaintCoinach ; API et fichiers natifs installés.
COMMIT_OR_VERSION: Garland 04cadd2e1e0de86c20aa9303faa082c7971f8d8b ; SaintCoinach 0b22b78f97951bb66bfa78fb561aa4c7bf7af7cd ; données locales du 2026-10-29.
LICENSE: MIT Garland déjà vérifiée ; WTFPL v2 SaintCoinach vérifiée dans LICENSE. Faits ciblés, aucune implémentation/base importée.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED projection ; sources primaires relues aux commits épinglés ; runtime UNKNOWN.
SOURCE: [Quests Garland](https://github.com/ufx/GarlandTools/blob/04cadd2e1e0de86c20aa9303faa082c7971f8d8b/Garland.Data/Modules/Quests.cs), [QuestRewards](https://github.com/xivapi/SaintCoinach/blob/0b22b78f97951bb66bfa78fb561aa4c7bf7af7cd/SaintCoinach/Xiv/QuestRewards.cs), [définition Quest](https://github.com/xivapi/SaintCoinach/blob/0b22b78f97951bb66bfa78fb561aa4c7bf7af7cd/SaintCoinach/Definitions/Quest.json).
FILES_READ: Quests.cs, QuestRewards/Item/ItemGroup/GroupType, définitions Quest/BeastRankBonus et licence ; API Quest/ClassJobReward/BeastRankBonus/TomestonesItem/SubrowExcelSheet et données natives.
TECHNICAL_FACTS: Sept slots fixes natifs, quatre par métier ; récompenses Item/table métier/bonus rang distinctes. Mémoquartz lié par Tomestones, pas numéro de ligne. Alternatives/conditions conservées sans maximum/cumul ni réception inférée.
LOCAL_COMPATIBILITY: 426 tests/package ; 5 373 snapshots/9 313 lignes projetées, identités Item/teinte résolues ; inconnus 4 quantités/15 monnaies conservés.
NOTES: [Contrats et limites](../goals/QUEST-REWARDS-003.md). Rang/genre/métier actuels, choix, disponibilité et bonus non calculés.

## REF-COLL-003

DOMAIN: Collections / preuve de chargement et récompenses.
NAME: Titres et succès natifs, acquisition à trois états.
REPOSITORY: goatcorp/Dalamud ; Lumina et fichiers du jeu installés.
COMMIT_OR_VERSION: Source 1e514694497ae448fca8e8a46f7b63642ad1e5ad relue ; API native et sqpack installés le 2026-10-29.
LICENSE: AGPL selon REF-DALAMUD historique ; faits ciblés, aucune implémentation native copiée.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED signatures/projection ; source primaire relue au commit épinglé ; runtime UNKNOWN.
SOURCE: [UnlockState](https://github.com/goatcorp/Dalamud/blob/1e514694497ae448fca8e8a46f7b63642ad1e5ad/Dalamud/Game/UnlockState/UnlockState.cs).
FILES_READ: getters Title/Achievement, propriétés de chargement, OnLogin/Logout/AgentUpdate et detours de signal ; métadonnées Title/Achievement/Category/Kind/Target et UnlockDelegate ; données natives via CollectionProbe.
TECHNICAL_FACTS: Une acquisition individuelle positive peut précéder la liste complète ; négatif non interprétable sans couverture. TitleList.DataReceived, Achievement.IsLoaded et identités de getter distinctes. Récompense Title/Item ne prouve pas possession/réclamation.
LOCAL_COMPATIBILITY: 421 tests/package verts ; 885 titres/4 003 succès, 83 catégories, 192 récompenses Item/891 Title et icônes résolues, jointures étrangères refusées.
NOTES: [Contrat et limites](../goals/COLL-003.md). Aucun chargement/callback/état de joueur observé en jeu ; disponibilité cachée et progression numérique non calculées.

## REF-COLL-002

DOMAIN: Collections / Item et contexte communs.
NAME: Catalogue et confirmations d’orchestrion.
REPOSITORY: goatcorp/Dalamud ; Lumina/FFXIVClientStructs et fichiers du jeu installés.
COMMIT_OR_VERSION: Source Dalamud 1e514694497ae448fca8e8a46f7b63642ad1e5ad relue ; métadonnées installées le 2026-10-29.
LICENSE: AGPL v3 selon REF-DALAMUD historique pour le code étudié ; faits ciblés, aucune implémentation native copiée.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED signatures/projection ; fichier primaire relu au commit épinglé ; runtime UNKNOWN.
SOURCE: [UnlockState](https://github.com/goatcorp/Dalamud/blob/1e514694497ae448fca8e8a46f7b63642ad1e5ad/Dalamud/Game/UnlockState/UnlockState.cs), [contrat](https://github.com/goatcorp/Dalamud/blob/1e514694497ae448fca8e8a46f7b63642ad1e5ad/Dalamud/Plugin/Services/IUnlockState.cs).
FILES_READ: UnlockState.cs getter Orchestrion et chemin Item ; métadonnées IUnlockState, Orchestrion/Uiparam/Category, Item/AdditionalData ; fichiers natifs via CollectionProbe.
TECHNICAL_FACTS: Acquisition par Orchestrion.RowId, Item lié par AdditionalData typé Orchestrion ; 886 identités Item/morceau différentes, 12 catégories natives. Pas de drapeau orchestrion dédié exposé ; négatif non confirmé. UI/catégorie étrangères ne fournissent pas de métadonnées.
LOCAL_COMPATIBILITY: 416 tests/package verts ; 886 projections/liens/icônes vérifiés hors jeu ; reader/service/contextes existants étendus, erreurs de catégorie isolées.
NOTES: [Contrat et limites](../goals/COLL-002.md). Aucun état de joueur ni interaction native validés ; types supplémentaires conservés à l’étude.

## REF-GATHER-FISH-FORECAST-010

DOMAIN: GatherBuddy / prévisions et horloge communes.
NAME: Intersection des seuls critères de pêche renseignés.
REPOSITORY: ufx/GarlandTools pour la sémantique ; modèles WeatherCatalog/GatheringTime originaux et fichiers natifs installés.
COMMIT_OR_VERSION: 04cadd2e1e0de86c20aa9303faa082c7971f8d8b ; données locales et réponses API consultées le 2026-10-29.
LICENSE: MIT vérifiée pour les sources primaires étudiées ; aucune implémentation/base externe copiée.
VERIFIED_AT: 2026-10-29.
STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: [consommateur horaire et période précédente](https://github.com/ufx/GarlandTools/blob/04cadd2e1e0de86c20aa9303faa082c7971f8d8b/Garland.Web/bell/gt.bell.js), [source 8753](https://www.garlandtools.org/db/doc/item/fr/3/8753.json), [source 4915](https://www.garlandtools.org/db/doc/item/fr/3/4915.json).
FILES_READ: Références primaires déjà vérifiées en 0.62/0.63 ; WeatherForecast, GatheringTimeline, FishingConditions ; réponses/probes natifs ciblés.
TECHNICAL_FACTS: Heure ET et période météo précédente distinctes ; critères absents non assimilés à sans restriction. Les segments compatibles n’établissent que les critères rapportés, avec couverture explicite ; inconnus et incompatibilité distincts.
LOCAL_COMPATIBILITY: 412 tests/package verts ; oracle indépendant et probes réels avec fixtures datées pour météo/précédente 8753 et horaire seul 4915.
NOTES: [Contrat et limites](../goals/GATHER-FISH-FORECAST-010.md). Pas de capture/prérequis/météo forcée observés ; code natif 2 inchangé.

## REF-GATHER-FISH-WEATHER-009

DOMAIN: GatherBuddy / météo native partagée.
NAME: Correspondance des noms anglais de source vers Weather natif dans un territoire.
REPOSITORY: ufx/GarlandTools ; API et fichiers natifs installés.
COMMIT_OR_VERSION: 04cadd2e1e0de86c20aa9303faa082c7971f8d8b ; assemblages et sqpack installés le 2026-10-29.
LICENSE: MIT vérifiée pour le code Garland étudié ; faits ciblés, aucune base/implémentation externe copiée.
VERIFIED_AT: 2026-10-29.
STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: [Weather.cs](https://github.com/ufx/GarlandTools/blob/04cadd2e1e0de86c20aa9303faa082c7971f8d8b/Garland.Data/Modules/Weather.cs), [conditions de pêche](https://github.com/ufx/GarlandTools/blob/04cadd2e1e0de86c20aa9303faa082c7971f8d8b/Garland.Data/Modules/FishingSpots.cs), [document 8753](https://www.garlandtools.org/db/doc/item/fr/3/8753.json).
FILES_READ: Sources primaires déjà consultées en 0.62 ; Weather/French et English, TerritoryType/WeatherRate locaux ; WeatherCatalogLoader et projection commune/probes.
TECHNICAL_FACTS: Noms anglais de la source ; 43 labels natifs globalement ambigus, dont Clear Skies IDs 1/91/209. Correspondance exacte unique seulement dans les taux positifs du territoire ; métadonnées manquantes ou ambiguïté locale conservées inconnues.
LOCAL_COMPATIBILITY: 405 tests/package ; WeatherProbe 214 lignes et contrôles antérieurs passants ; consultation réelle 8753 reliée au territoire 137 et Weather 1/7/8, noms/icônes natifs.
NOTES: [Contrat et limites](../goals/GATHER-FISH-WEATHER-009.md). Pas d’intersection/disponibilité ni observation du ciel ; code natif 2 toujours inconnu.

## REF-GATHER-FISH-WINDOWS-008

DOMAIN: GatherBuddy / horaires et météo partagés.
NAME: Heures ET et conditions communautaires par lieu.
REPOSITORY: ufx/GarlandTools.
COMMIT_OR_VERSION: 04cadd2e1e0de86c20aa9303faa082c7971f8d8b ; documents API consultés le 2026-10-29.
LICENSE: MIT vérifiée pour le code étudié ; aucune implémentation/base redistribuée.
VERIFIED_AT: 2026-10-29.
STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: [générateur](https://github.com/ufx/GarlandTools/blob/04cadd2e1e0de86c20aa9303faa082c7971f8d8b/Garland.Data/Modules/FishingSpots.cs), [consommateur horaire/météo](https://github.com/ufx/GarlandTools/blob/04cadd2e1e0de86c20aa9303faa082c7971f8d8b/Garland.Web/bell/gt.bell.js), [document Mahi-Mahi](https://www.garlandtools.org/db/doc/item/fr/3/4915.json), [transition](https://www.garlandtools.org/db/doc/item/fr/3/8753.json).
FILES_READ: FishingSpots.cs, Weather.cs, gt.bell.js, fish.js et lignes ciblées du TSV ; exemples API 7685,7686,7690,7705,4915,8753.
TECHNICAL_FACTS: during en heures entières ET ; début inclus/fin exclue, minuit circulaire ; transition = météo de la période précédente, weather = alternatives courantes. Absents non déduits ; divergence source épinglée/API 7685 conservée inconnue.
LOCAL_COMPATIBILITY: 401 tests/package verts, probes 4915 et 8753 liés aux lieux/appâts natifs, calcul horaire UTC partagé ; FashionProbe compilé.
NOTES: [Contrat et limites](../goals/GATHER-FISH-WINDOWS-008.md). Noms météo non encore reliés aux IDs locaux, pas d’intersection/disponibilité, code natif 2 inconnu.

## REF-GATHER-FISH-SOURCES-007

DOMAIN: GatherBuddy / sources d’objet communes.
NAME: Appâts et chaînes communautaires par lieu.
REPOSITORY: ufx/GarlandTools ; FFXIV-CombatReborn/GatherBuddyReborn ; xivapi/SaintCoinach.
COMMIT_OR_VERSION: GarlandTools 04cadd2e1e0de86c20aa9303faa082c7971f8d8b ; GatherBuddy a204f2f45cb98302dbe938921255c3a6d617c775 ; définition SaintCoinach consultée le 2026-10-29.
LICENSE: MIT vérifiée dans LICENSE pour le code Garland consulté, Apache-2.0 vérifiée pour GatherBuddy ; faits ciblés, aucune implémentation ni base complète redistribuée.
VERIFIED_AT: 2026-10-29.
STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: [générateur de pêche](https://github.com/ufx/GarlandTools/blob/04cadd2e1e0de86c20aa9303faa082c7971f8d8b/Garland.Data/Modules/FishingSpots.cs), [document objet](https://www.garlandtools.org/db/doc/item/fr/3/4869.json).
FILES_READ: FishingSpots.cs, gt.fishing.js, LICENSE ; FishingNoteInfo.json SaintCoinach ; réponse item 4869 et exemple 7902 ; Fish/CatchData de GatherBuddy déjà lus.
TECHNICAL_FACTS: alternatives de chaînes ordonnées par lieu, appât initial puis intermédiaires. Le drapeau météo natif 2 n’est pas expliqué : tests >0 et ==1 divergent selon les producteurs ; Unknown conservé.
LOCAL_COMPATIBILITY: 396 tests/package, sept groupes nouveaux ; consultation réelle 4869 avec 19 lieux natifs et deux IDs d’appâts résolus. Service/cache/transport d’objet existants partagés.
NOTES: [Contrat et limites](../goals/GATHER-FISH-SOURCES-007.md). Champs météo/horaires non interprétés, aucune disponibilité garantie ; rendu/interaction natifs non observés.

## REF-GATHER-FISH-006

DOMAIN: GatherBuddy / catalogue partagé.
NAME: Références natives du carnet de pêche.
REPOSITORY: FFXIV-CombatReborn/GatherBuddyReborn.
COMMIT_OR_VERSION: a204f2f45cb98302dbe938921255c3a6d617c775 ; API Lumina et sqpack installés.
LICENSE: Apache-2.0 selon référence historique REF-GATHER ; faits ciblés uniquement, aucune base ou implémentation copiée.
VERIFIED_AT: 2026-10-29.
STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: [Classes/Fish.cs](https://github.com/FFXIV-CombatReborn/GatherBuddyReborn/blob/a204f2f45cb98302dbe938921255c3a6d617c775/GatherBuddy.GameData/Classes/Fish.cs).
FILES_READ: Fish.cs, Fish.CatchData.cs, FishRestrictions.cs, Data/Fish/DataBase.cs, Data2.0.cs ; métadonnées FishParameter/FishingNoteInfo/GatheringSubCategory installées.
TECHNICAL_FACTS: note par FishParameter.RowId, Item standard identique ; code 1 signalé, 0 non signalé, autres inconnus dans Tensho. Appâts/conditions détaillées enrichis par des données supplémentaires dans le projet de référence. Code météo 2 non expliqué, pas assimilé à une absence.
LOCAL_COMPATIBILITY: 389 tests/package ; projection originale partagée avec probe : 2 461 fiches et notes concordantes, 296 tomes, 491 restrictions météo inconnues. GatheringProbe antérieur passant.
NOTES: [Contrat et limites](../goals/GATHER-FISH-006.md). Staticité distincte de prises/déblocages/disponibilité ; rendu/framework en jeu non observés.

## REF-SORT-TEMPLATES-007

DOMAIN: SortaKinda / configuration commune / livraison.
NAME: Modèles de règles sans identité d’observation.
COMMIT_OR_VERSION: Tensho 0.59.0.0, configuration et modèles v1.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: implémentation managée originale, validation commune et contrôles de livraison locaux ; aucune API native ajoutée.
TECHNICAL_FACTS: géométrie exacte, rebind au scope courant seulement, copie de listes et absence de données d’observation. Chargement en brouillon sans capture/application. ZIP contrôlé par SHA-256 contre DLL/manifeste canoniques.
LOCAL_COMPATIBILITY: 385 tests/package, six groupes de modèles et Newtonsoft trois modes passants ; sortie canonique 0.59.0.0.
NOTES: [Contrat et limites](../goals/SORT-TEMPLATES-007.md). Rendu/application/persistance natifs non observés ; 17 domaines PARTIAL.

## REF-SORT-FILTERS-006

DOMAIN: SortaKinda / catalogue Item commun / équipement.
NAME: Filtres de propriétés et slots natifs.
COMMIT_OR_VERSION: Tensho 0.58.0.0 ; bindings Lumina installés et fichiers du jeu locaux.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: ItemCatalogLoader, Item/EquipSlotCategory installés ; ApiProbe et FashionProbe sans --fetch. Implémentation originale.
TECHNICAL_FACTS: champs MainHand/OffHand/SoulCrystal sbyte, seuls positifs affectent le masque. CanBeHq et classement marché statiques ne prouvent ni capacité du personnage ni vente de la pile. Métadonnées manquantes restent inconnues.
LOCAL_COMPATIBILITY: 379 tests/package ; Newtonsoft trois modes ; probe natif : 20 238 Wearable, 7 804 MainHand, 984 OffHand, 31 SoulCrystal et 51 vêtements bloquant Head, OffHand négatifs exclus.
NOTES: [Contrat et limites](../goals/SORT-FILTERS-006.md). Pas de réseau ajouté ; capture/rendu/application native non observés.

## REF-SORT-PROTECTED-005

DOMAIN: SortaKinda / aperçu commun.
NAME: Protection de positions et remplissage inversé.
COMMIT_OR_VERSION: Tensho 0.57.0.0 ; configuration v1 compatible.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: modèle managé original sur les indices visuels déjà observés par InventoryNativeReader ; aucune nouvelle API native ou implémentation externe importée.
TECHNICAL_FACTS: protection avant sélection par filtres, cases vides conservées comme vides, permutation complète avec vraies cases physiques vides ; indices manquants ou ambigus refusés. Inversion de remplissage indépendante du sens de tri.
LOCAL_COMPATIBILITY: 374 tests/package, cinq groupes dont 20 scènes multi-scopes avec oracles ; Newtonsoft trois modes et anciens defaults v1 passants.
NOTES: [Contrat et limites](../goals/SORT-PROTECTED-005.md). Rendu/écriture/persistance natifs non observés ; 17 domaines PARTIAL.

## REF-SORT-RULES-004

DOMAIN: SortaKinda / catalogue commun.
NAME: Profils et aperçu positionné.
COMMIT_OR_VERSION: Tensho 0.56.0.0 ; configuration v1 conservée.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: implémentation managée originale sur ItemCatalog, InventorySortPreview et permutation existants ; aucune nouvelle API native ou formule externe supposée.
TECHNICAL_FACTS: catégories et noms issus des ItemDefinition natifs partagés ; trous représentés explicitement puis affectés à de vraies cases physiques vides, toutes les piles vérifiées. Règles et sauvegarde par scope distinctes de l’application.
LOCAL_COMPATIBILITY: 369 tests/package, huit groupes supplémentaires dont 24 scènes multi-scopes ; Newtonsoft None/Auto/Objects passants avec listes imbriquées et v1 historique.
NOTES: [Contrat et limites](../goals/SORT-RULES-004.md). Affichage et application native non observés ; les 17 domaines restent PARTIAL.

## REF-SORT-SCOPES-003

DOMAIN: SortaKinda / banque commune.
NAME: Scopes natifs et capture directe.
COMMIT_OR_VERSION: plugin 0.55.0.0, bindings installés API 15 ; source externe de permutation REF-SORT-NATIVE-002 inchangée.
VERIFIED_AT: 2026-10-29.
STATUS: LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: BankStorageScope, InventoryNativeReader et métadonnées ItemOrderModule/GetActiveRetainerSorter/ActiveRetainerId déjà inspectées ; compilation des accès directs.
TECHNICAL_FACTS: 4 × 35 personnel, 2 × 35 par sacoche, 7 × 25 servant ; propriétaire natif du servant distinct du personnage. Fenêtre/chargement/identités revérifiés au framework, reçu de capture directe indépendant d’un cache importé.
LOCAL_COMPATIBILITY: 361 tests/package, dont 144 scénarios des scopes et contrôles de cache/identité/fraîcheur/lifecycle ; zéro avertissement/erreur.
NOTES: [Contrat et limites](../goals/SORT-SCOPES-003.md). Détection de fenêtre, capture réelle, écriture et persistance non observées en jeu. Aucun code externe importé.

## REF-SORT-NATIVE-002

DOMAIN: SortaKinda / inventaire commun.
NAME: Ordre visuel natif.
REPOSITORY: MidoriKami/SortaKinda.
COMMIT_OR_VERSION: 95a1bf9161f4bc595c21094eeaae9fda556f4b4b.
LICENSE: AGPL v3 ou ultérieure, LICENSE lu ; faits d’API seulement, implémentation originale.
VERIFIED_AT: 2026-10-29.
STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: [SortingController](https://github.com/MidoriKami/SortaKinda/blob/95a1bf9161f4bc595c21094eeaae9fda556f4b4b/SortaKinda/Classes/SortingController.cs).
FILES_READ: SortingController.cs, ItemOrderModuleSorterExtensions.cs, InventoryTypeExtensions.cs, ItemSlotInfo.cs, SortaKinda.cs, LICENSE ; métadonnées installées ItemOrderModule/Sorter/ItemEntry/PreviousOrder/SortFunction/StdVector.
TECHNICAL_FACTS: ordre visuel par Page/Slot d’entrées ; permutation des valeurs entières, HasChanges après traitement. Le code de référence utilise des workers et hooks, qui ne sont pas repris.
LOCAL_COMPATIBILITY: bridge original sur framework.Update, 140 cases personnelles, gardes owner/âge/couverture/permutation/ordre/pointeurs ; compilation et 355 tests/package passants.
NOTES: application native, rendu et persistance non observés ; pas de revendication REPLACEABLE. [Contrat et limites](../goals/SORT-NATIVE-002.md).

## REF-USE-CONTEXT-004

DOMAIN: Item Use / contexte partagé.
NAME: Menu contextuel d’inventaire Dalamud.
REPOSITORY: goatcorp/Dalamud.
COMMIT_OR_VERSION: 1e514694497ae448fca8e8a46f7b63642ad1e5ad ; SDK local API 15.
LICENSE: AGPL v3 ; faits d’API seulement, code original.
VERIFIED_AT: 2026-10-29.
STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED ; runtime UNKNOWN.
SOURCE: [MenuTargetInventory](https://github.com/goatcorp/Dalamud/blob/1e514694497ae448fca8e8a46f7b63642ad1e5ad/Dalamud/Game/Gui/ContextMenu/MenuTargetInventory.cs).
FILES_READ: MenuTargetInventory.cs, GameInventoryItem.cs, MenuItem.cs, MenuOpenedArgs.cs, MenuArgs.cs, ContextMenu.cs ; métadonnées IContextMenu/IMenuOpenedArgs/IMenuItemClickedArgs installées.
TECHNICAL_FACTS: TargetItem construit une copie GameInventoryItem du slot ciblé ; lire la cible au callback. OpenSubmenu est fourni à OnClicked ; la branche AddonContextSub n’émet pas un nouvel OnMenuOpened. Les closures de Tensho ne conservent que des valeurs managées normalisées.
LOCAL_COMPATIBILITY: compilation 0.53 sans avertissement/erreur, 349 tests/package et Newtonsoft trois modes passants.
NOTES: contrôles propriétaire/révision/âge et option indépendants ; absence de validation native observée. [Contrat et protocole](../goals/USE-CONTEXT-004.md).

## REF-USE-HOVER-003

STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED — 2026-10-29. [Preuves et limites](../goals/USE-HOVER-003.md).
SOURCE: goatcorp/Dalamud ItemUtil commit1e514694497ae448fca8e8a46f7b63642ad1e5ad, marqueurs NQ/collectionnable500000/HQ1000000 et IDs EventItem non soustraits ; métadonnées installées confirmées. Normalisation standard originale, rejet explicite des IDs spéciaux ≥2000000 ; aucun parseur de nom ou modulo.
VALIDATION: 343/343 tests/package, HTTP simulé et portée/priorités ; Newtonsoft v1 trois modes et GatheringProbe avec données du jeu passants. Aucun rendu/survol natif nouveau observé.

## REF-USE-002

STATUS: LOCAL_VERIFIED / CURRENT_VERIFIED — 2026-10-29. [Preuves et limites](../goals/USE-002.md).
SOURCE: PunishedPineapple/ItemUse commit2e7dfc5f7eb122aea5b8705706d4bd8c5000f42d, ItemCategorizer (AGPL-3.0, faits seulement). Feuilles CraftLeve/Leve/GCSupplyDuty/AquariumFish et métadonnées installées ; projection originale partagée avec le probe.
FACTS: 1 240 mandats liés, 54 à lignes multiples, paramètres Repeats0/2/5 conservés sans interprétation ; 2 516 candidats GC, 263 poissons. Référence statique distincte de demande/acceptation actuelle.
VALIDATION: 334/334 tests/package, 3 280 Item et icônes de mandat résolus sur sqpack ; aucun rendu ou chargement Dalamud observé.

## REF-GP-ACTIVE-005

STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED —2026-10-29. [Preuves et limites](../goals/GP-ACTIVE-005.md).
SOURCE: LazyGatherer0bb8ec9a47dc4521411c705221cdeda361015d75, familles Yield/Gift/Tidings et tentatives Abondance ; faits ciblés uniquement, AGPL, aucune implémentation importée. Actions/ActionTransient installées et probe commun ; StatusGainSelf vide reste une association inconnue.
FACTS: familles permanentes déjà actives non sélectionnées à nouveau ; supplément Évangile2, bonus limité appliqué une fois. Modèle déclaré et oracle originaux ; aucun état natif inventé.
VALIDATION:327/327 tests/package, oracle128 scénarios et probes MIN/BTN avec actions du jeu passants. Rendu et effets réels non validés.

## REF-GATHER-NATIVE-004

STATUS: LOCAL_VERIFIED / CURRENT_VERIFIED — 2026-10-29. [Preuves et limites](../goals/GATHER-NATIVE-004.md).
SOURCE: signatures GUI installées ; FFXIVClientStructs AddonGathering/AtkResNode 385b0821bc4573b77fbde84c930774eeca8283ec ; LazyGatherer GatheringItemComponent/GatheringController 0bb8ec9a47dc4521411c705221cdeda361015d75 pour faits de lecture seulement (AGPL, aucune implémentation importée).
FACTS: Gathering.uld installé contient 8 lignes CheckBox composant1010, textes10/16 et icône31→composant1005, texte quantité7. Probe utilise GatheringUiLayout de production ; 35/12 nœuds, borne256. Textes vides/tirets/malformés restent inconnus ; taux affichés pas déduits comme bases.
VALIDATION: 320/320 tests/package verts et GatheringUiProbe passant ; aucune capture ou validation visuelle native.

## REF-CACT-LIVE-002

Voir aussi [CACT-LIVE-002](../goals/CACT-LIVE-002.md) : métadonnées locales AddonLotteryDaily et source FFXIVClientStructs 385b0821bc4573b77fbde84c930774eeca8283ec vérifiées ; indexeurs neuf entiers en ordre ligne/colonne. Référence historique MidoriKami/EzMiniCactpot 63173afb3eff69aa64a3edc613a377c4053d26fa pour GameNumbers et retrait en fin de ticket ; aucune implémentation importée, pas de fichier LICENSE trouvé dans cet arbre. Ancienne URL daemitus 404. API compilée et 311 tests/package verts ; native runtime non validé. Statuts LOCAL_VERIFIED / HISTORICAL_VERIFIED, 2026-10-29.

## REF-CRAFT-CONDITIONS-004 / RANGE-005

STATUS: CURRENT_VERIFIED / LOCAL_VERIFIED — 2026-10-29. Sources et limites dans [CRAFT-CONDITIONS-004](../goals/CRAFT-CONDITIONS-004.md) et [CRAFT-CONDITION-RANGE-005](../goals/CRAFT-CONDITION-RANGE-005.md).
SOURCE: Craftimizer commit 1d4a2c67ea47a25d68701e8e69c8744dbbd56d86, Gearsets et Simulator (StepCondition, facteurs qualité), lus depuis le dépôt primaire ; métadonnées et sqpack locaux.
FACTS: description anglaise connue des outils × 1,75 ; 136 marqueurs dans 928 outils principaux, aucun ailleurs. Favorable/Mauvaise → Normale ; Excellente → Mauvaise ; à partir de Normale, enveloppe conservatrice Normale/Favorable/Excellente sans probabilité. Bornes par condition comparées à une énumération exhaustive sur petits scénarios.
VALIDATION: 302/302 tests/package verts. L’observation de l’outil réellement équipé et le rendu en jeu restent non validés ; tests projection et Macro Chain différés.

## REF-CRAFT-LIVE-003

DOMAIN: Craft ; projection immédiate native expérimentale.
NAME: Valeurs Synthesis typées et admissibilité des actions.
REPOSITORY: WorkingRobot/Craftimizer ; aers/FFXIVClientStructs ; assemblies locales API 15.
COMMIT_OR_VERSION: 1d4a2c67ea47a25d68701e8e69c8744dbbd56d86 ; ActionManager 385b0821bc4573b77fbde84c930774eeca8283ec.
LICENSE: Sources étudiées pour les faits ; implémentation originale, aucun hook ou offset repris.
VERIFIED_AT: 2026-10-29.
STATUS: CURRENT_VERIFIED pour schéma/conventions étudiés ; LOCAL_VERIFIED pour signatures ; runtime UNKNOWN, test différé par l'utilisateur.
SOURCE: [preuves et limites](../goals/CRAFT-LIVE-003.md).
FILES_READ: SynthesisValues, Condition, SynthHelper, Hooks ; ActionManager et métadonnées AtkValue/ActionType/CraftCondition.
TECHNICAL_FACTS: Normale addon0 et handler1, étape UI moins un ; GetActionStatus sans contrôle de recharge ; GetActionCost non utilisé faute de sens établi des quatre paramètres additionnels. Projection d'états possibles, sans rotation complète.
LOCAL_COMPATIBILITY: Verify.ps1 0.45.0.0, 290/290 tests/package verts ; GatheringProbe et CraftSimulationProbe passant.
NOTES: Le test de projection et Macro Chain sont différés par l'utilisateur ; pas de nouvelle demande. Réserve de quota utilisateur 5 %.

## REF-GATHER-WEATHER-001

DOMAIN: Gathering / Maps ; météo de zones.
NAME: Poids météo natifs et prévisions des périodes ET.
REPOSITORY: FFXIV-CombatReborn/GatherBuddyReborn ; feuilles natives installées.
COMMIT_OR_VERSION: a204f2f45cb98302dbe938921255c3a6d617c775 ; Lumina locale API 15.
LICENSE: Apache-2.0 pour la référence ; faits étudiés, implémentation originale.
VERIFIED_AT: 2026-10-28.
STATUS: CURRENT_VERIFIED pour calcul/taux ; LOCAL_VERIFIED pour jointures et icônes ; runtime UNKNOWN.
SOURCE: [preuves et limites](../goals/GATHER-WEATHER-001.md).
FILES_READ: WeatherManager.Computation.cs, CumulativeWeatherRates.cs ; WeatherRate, Weather, TerritoryType via ApiProbe et sqpack.
TECHNICAL_FACTS: Période huit heures ET, mélange 32 bits et taux cumulés ; 1290 territoires/72 météos, 24 zones individuelles ; Empyrée total90 conservé inconnu pour la masse manquante.
LOCAL_COMPATIBILITY: Verify.ps1 0.42.0.0, 275/275 tests/package verts ; WeatherProbe passant sans avertissement.
NOTES: Prévision calculée, pas météo native observée ; événement et prérequis de ressources hors couverture.

## REF-CRAFT-LIVE-002

DOMAIN: Craft ; effets observés.
NAME: Identités de statuts, Param, compteurs et historique unique.
REPOSITORY: WorkingRobot/Craftimizer ; feuilles natives et assemblies installées.
COMMIT_OR_VERSION: 1d4a2c67ea47a25d68701e8e69c8744dbbd56d86 ; API 15 locale.
LICENSE: MIT pour la source étudiée ; faits utilisés, aucun hook/code repris.
VERIFIED_AT: 2026-10-28.
STATUS: CURRENT_VERIFIED pour Param/IDs/convention étudiée ; LOCAL_VERIFIED pour douze statuts/icônes ; runtime UNKNOWN.
SOURCE: [preuves et limites](../goals/CRAFT-LIVE-002.md).
FILES_READ: SynthHelper, SimulatorUtils, SynthesisValues ; StatusList.Length, Status.Param et feuilles Status du jeu.
TECHNICAL_FACTS: Compteurs via Param, douze IDs reconnus et icônes natives, présence de Main suprême distincte de son historique. Le lecteur primaire suit ses ActionStates via actions ; les procs natifs ne sont pas convertis sans preuve.
LOCAL_COMPATIBILITY: Verify.ps1 0.41.0.0, 270/270 tests et package verts ; probe douze statuts et 208 actions passant.
NOTES: Aucun état complet de conseil reconstruit ; test runtime différé, autres domaines peuvent avancer.

## REF-CRAFT-LIVE-001

DOMAIN: Craft ; observation native.
NAME: CraftEventHandler, AddonSynthesis et PS courants.
REPOSITORY: Assemblies locales Dalamud/FFXIVClientStructs installées ; aucune copie de code externe.
COMMIT_OR_VERSION: API 15 ; signatures et champs vérifiés via ApiProbe.
LICENSE: Métadonnées inspectées, pas d'assembly hôte redistribuée.
VERIFIED_AT: 2026-10-28.
STATUS: LOCAL_VERIFIED pour disponibilité des accès ; runtime UNKNOWN.
SOURCE: [preuves et protocole](../goals/CRAFT-LIVE-001.md).
FILES_READ: Types EventFramework/CraftEventHandler/CraftCondition/CraftFlags, AddonSynthesis/CraftEffect, AtkTextNode/AtkResNode/Utf8String, IObjectTable/ICharacter/IBattleChara et statuts ; lecteur expérimental existant comme convention lifecycle.
TECHNICAL_FACTS: Handler présent contrairement au CraftingManager supposé ; champs nommés recette/étape/condition, textes et effets UI, CurrentCp/MaxCp. Paramètres temporels/charges de statuts non interprétés.
LOCAL_COMPATIBILITY: Verify.ps1 0.40.0.0, 265/265 tests et package verts ; aucune capture du client en cours réalisée.
NOTES: Lecture seule temporaire désactivée par défaut ; aucun conseil ou état natif complet revendiqué.

## REF-CRAFT-ACTIONS-002

DOMAIN: Craft ; actions normales avancées.
NAME: Neuf actions, traits, Calme intérieur et protection de solidité.
REPOSITORY: WorkingRobot/Craftimizer.
COMMIT_OR_VERSION: 1d4a2c67ea47a25d68701e8e69c8744dbbd56d86 ; feuilles natives installées API 15.
LICENSE: MIT ; faits étudiés, implémentation originale.
VERIFIED_AT: 2026-10-28.
STATUS: LOCAL_VERIFIED pour 208 métadonnées/icônes ; CURRENT_VERIFIED pour contraintes/traits du modèle ; runtime UNKNOWN.
SOURCE: [table d'actions, preuves et limites](../goals/CRAFT-ACTIONS-002.md).
FILES_READ: DelicateSynthesis, PreparatoryTouch, PrudentSynthesis/Touch, RefinedTouch, TrainedEye/Finesse/Perfection, ImmaculateMend ; Simulator et Effects.
TECHNICAL_FACTS: Trait 94 séparé, deux charges/combo, interdiction Parcimonie, ouverture/niveau et dix charges, restauration totale, protection persistante consommée une seule fois.
LOCAL_COMPATIBILITY: Verify.ps1 0.39.0.0, 260/260 tests et package verts ; probe 208 variantes, recette niveau100 et replay passant.
NOTES: Proposition niveau100 sous cible explicitement rapportée ; ne prouve ni impossibilité ni capacité du personnage connecté. Pas de lecture native de synthèse.

## REF-CRAFT-MACRO-001

DOMAIN: Craft ; export manuel.
NAME: Commandes localisées, capacité et attentes proposées.
REPOSITORY: WorkingRobot/Craftimizer.
COMMIT_OR_VERSION: 1d4a2c67ea47a25d68701e8e69c8744dbbd56d86.
LICENSE: MIT ; faits étudiés, implémentation originale.
VERIFIED_AT: 2026-10-28.
STATUS: CURRENT_VERIFIED ; métadonnées d'actions LOCAL_VERIFIED ; activation en jeu UNKNOWN.
SOURCE: [preuves et limites](../goals/CRAFT-MACRO-001.md).
FILES_READ: MacroCopy.cs, BaseAction.cs, BaseBuffAction.cs, Observe.cs, MastersMend.cs, MuscleMemory.cs, Reflect.cs ; lecteur natif de macros existant.
TECHNICAL_FACTS: 15 lignes, /ac avec nom natif et attente ; effets 2s, autres actions modélisées 3s. Convention d'export, pas garantie de latence.
LOCAL_COMPATIBILITY: Release 0.37.0.0, 243/243 tests et package verts ; probe des huit métiers passant.
NOTES: Copie texte par bloc uniquement ; aucune exécution ou écriture native.

## REF-CRAFT-SIM-001

DOMAIN: Craft ; simulation manuelle normale.
NAME: Paramètres Recipe et actions déterministes.
REPOSITORY: WorkingRobot/Craftimizer ; ffxiv-teamcraft/simulator.
COMMIT_OR_VERSION: 1d4a2c67ea47a25d68701e8e69c8744dbbd56d86 ; 74e167a05ba279526d2ddd457a048e234bedbad9 ; assemblies locales API 15.
LICENSE: Craftimizer MIT ; sources étudiées pour les faits, aucun code externe embarqué.
VERIFIED_AT: 2026-10-28.
STATUS: LOCAL_VERIFIED pour champs/jointures/actions/icônes ; CURRENT_VERIFIED pour comparaison des formules de ces commits ; runtime UNKNOWN.
SOURCE: [dossier et preuves](../goals/CRAFT-SIM-001.md).
FILES_READ: Craftimizer SimulationInput et actions/effets ; Teamcraft quality-action, progress-action, AdvancedTouch ; Lumina Recipe/RecipeLevelTable/CraftAction/Action/ActionTransient du jeu installé.
TECHNICAL_FACTS: Bases tronquées avant action, buffs additifs, combos conditionnels, durées et arrêts ; 136 variantes natives vérifiées ; 11 361 recettes acceptées / 2 531 explicitement exclues du modèle initial.
LOCAL_COMPATIBILITY: Release 0.36.0.0, Verify.ps1 239/239 et package verts ; CraftSimulationProbe passant.
NOTES: Condition normale uniquement, attributs manuels/copiés, pas de taux HQ ni de synthèse commandée ; voir périmètre exact et test différé.

État courant 0.19.0 (2026-10-28) : [base multi-outils](../goals/BASE-TOOLS-001.md), [sous-recettes](../goals/CRAFT-CHAIN-001.md), [cartes](../goals/MAP-001A.md), [favoris de cartes](../goals/MAP-002.md), [prix au survol](../goals/MARKET-HOVER-001.md) et [séquences de macros](../goals/MACRO-001A.md). 119/119 tests et package vérifiés. L'utilisateur testera Macro Chain plus tard ; validation native différée, sans acceptation supposée.

REF-ID: REF-GATHER-LOCAL-20261027
DOMAIN: Récolte/pêche
SOURCE: Assemblies Lumina installées, sqpack du jeu installé lu par tools/GatheringProbe.
STATUS: LOCAL_VERIFIED — compilation API ; LOCAL_DATA_VERIFIED — jointures exécutées sur fichiers du jeu.
VERIFIED_AT: 2026-10-27 Europe/Brussels
FILES_READ: Métadonnées GatheringItem, GatheringItemPoint (subrows), GatheringPointBase, GatheringPoint, GatheringType, TerritoryType, PlaceName, FishingSpot et Map ; données locales des mêmes feuilles.
TECHNICAL_FACTS: Les RowRef non typées sont contrôlées par RowType. GatheringItemPoint.RowId identifie GatheringItem et ses subrows référencent GatheringPoint ; cette liaison fournit les sources des objets cachés absents de GatheringPointBase.Item. FishingSpot.Item est directement typé Item.
NOTES: Pas de code de plugin externe copié. Carte ouverte au centre de zone uniquement ; aucune position exacte ou disponibilité déduite. Preuves de comptage dans TESTING.md.

REF-ID: INVENTORY-VISUAL-ORDER-20261027
DOMAIN: Inventory
NAME: ItemOrderModule et ItemOrderModuleSorter
SOURCE: https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Misc/ItemOrderModule.cs
VERIFIED_AT: 2026-10-27 Europe/Brussels
STATUS: LOCAL_VERIFIED — champs présents dans l'assembly installé et accès compilé ; runtime à confirmer.
TECHNICAL_FACTS: InventorySorter, SaddleBagSorter, PremiumSaddleBagSorter et GetActiveRetainerSorter exposent Items ; chaque entrée référence Page et Slot physiques. Cette permutation est distincte de l'ordre physique IGameInventory.
NOTES: L'affirmation précédente selon laquelle changer ligne/colonne suffisait était incorrecte. La projection consomme désormais cette table ; aucune écriture native.


## REF-MACRO-BRIDGE-20261028

REF-ID: REF-MACRO-BRIDGE-20261028
DOMAIN: Macro Chain
NAME: Adaptateur local à marqueur final et appel possédé
REPOSITORY: Sources originales Tensho ; références primaires https://github.com/Caraxi/MacroChain et https://github.com/aers/FFXIVClientStructs
COMMIT_OR_VERSION: Assemblies locales API 15 compilées en 0.18.0 ; lectures externes du 2026-10-27 conservées dans docs/goals/MACRO-001A.md
LICENSE: Aucune implémentation externe copiée ; SDK/assemblies hôte non redistribués
VERIFIED_AT: 2026-10-28 Europe/Brussels
STATUS: LOCAL_VERIFIED pour signatures/compilation ; NATIVE_BEHAVIOR_UNVERIFIED
SOURCE: ApiProbe, Dalamud.xml local et sources de l'adaptateur
FILES_READ: RaptureMacroModule.Macro/GetMacro, Utf8String, RaptureShellModule.ExecuteMacro/MacroLines/MacroName/MacroCurrentLine, IGameInteropProvider.HookFromAddress, IFramework.IsInFrameworkUpdateThread
TECHNICAL_FACTS: Macro est un type natif avec 15 lignes ; livres 0/1 et index 0–99. Hook à adresse fournie par les membres typés, aucun offset/signature ajouté. Les noms/lignes copiés au framework sont managés. IsInFrameworkUpdateThread vérifie le thread du jeu, pas seulement la portée du callback Update.
LOCAL_COMPATIBILITY: 0.18.0 compilée et 114 tests/package vérifiés ; scopes, commandes et hook possédés. Aucun appel au jeu effectué pendant ces tests.
NOTES: Indice de ligne au callback du marqueur encore à confirmer. Le protocole accepte la ligne du pied ou sa consommation immédiate ; tout désaccord arrête la suite. Aucune réussite d'action de macro n'est déduite du marqueur.

BASELINE_REFERENCE_DATE: 2026-10-27

Les onze blocs historiques ci-dessous sont transcrits du prompt maître, et non revérifiés sur leurs dépôts. Les licences sont celles rapportées par cette source. Aucune source de plugin externe n'a été copiée. FILES_READ distingue ce qui a réellement été lu ici des concepts rapportés historiquement.

## REF-DALAMUD

REF-ID: REF-DALAMUD
DOMAIN: Socle
NAME: Dalamud
REPOSITORY: https://github.com/goatcorp/Dalamud
COMMIT_OR_VERSION: 1e514694497ae448fca8e8a46f7b63642ad1e5ad
LICENSE: AGPL v3 (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : IFramework ; IUnlockState ; IGameInventory ; IAddonLifecycle ; WindowSystem ; GameInventory.
TECHNICAL_FACTS: IFramework expose IsInFrameworkUpdateThread, RunOnTick, RunOnFrameworkThread. Copier ReadOnlySpan<GameInventoryItem> avant traitement différé. Indicateurs de chargement unlocks : false peut rester inconnu. InventoryChanged invalide, ne prouve pas la complétude.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-SAMPLE

REF-ID: REF-SAMPLE
DOMAIN: Bootstrap
NAME: SamplePlugin
REPOSITORY: https://github.com/goatcorp/SamplePlugin
COMMIT_OR_VERSION: b8477daaa678a5dd72cdcd4a32bc249d3c23a598
LICENSE: AGPL v3 (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : Plugin ; configuration ; fenêtres.
TECHNICAL_FACTS: IDalamudPlugin, PluginService, ICommandManager, WindowSystem, UiBuilder.Draw/OpenConfigUi/OpenMainUi, IPluginConfiguration/SavePluginConfig, Dispose. SDK observé 15.0.0, sans présumer de sa validité actuelle.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-CRAFT

REF-ID: REF-CRAFT
DOMAIN: Craftimizer #5
NAME: Craftimizer
REPOSITORY: https://github.com/WorkingRobot/Craftimizer
COMMIT_OR_VERSION: 1d4a2c67ea47a25d68701e8e69c8744dbbd56d86
LICENSE: MIT (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : RecipeData ; RecipeNote ; WKSRecipeNotebook ; simulateur ; solver.
TECHNICAL_FACTS: Joindre Recipe/RecipeLevelTable/Item/seuils collectables. Qualité initiale selon HQ, niveaux objets et MaterialQualityFactor. Byregot/Inner Quiet, Innovation/Great Strides, étapes et aléatoire. Solver annulable avec parallélisme borné, génétique/MCTS/Raphael ; pas d'exhaustivité universelle démontrée. Offsets à revérifier avant usage.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-ARTISAN

REF-ID: REF-ARTISAN
DOMAIN: Craft secondaire
NAME: Artisan
REPOSITORY: https://github.com/PunishXIV/Artisan
COMMIT_OR_VERSION: 247c2df1fad7eb83fa7af584e8340f4f987b12de
LICENSE: BSD-3-Clause (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : CraftState ; StepState ; Simulator ; Calculations ; ISolver ; TrackConditions ; RecipeWindowUI.
TECHNICAL_FACTS: Calculs selon facteurs/diviseurs RecipeLevelTable. Séparer simulation et sélection d'action. Référence secondaire, pas un plugin supplémentaire.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-GATHER

REF-ID: REF-GATHER
DOMAIN: GatherBuddy #8
NAME: GatherBuddyReborn
REPOSITORY: https://github.com/FFXIV-CombatReborn/GatherBuddyReborn
COMMIT_OR_VERSION: a204f2f45cb98302dbe938921255c3a6d617c775
LICENSE: Apache-2.0 (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : GameData ; Gatherable ; TimeInterval ; BitfieldUptime ; UptimeManager ; WeatherManager.
TECHNICAL_FACTS: Feuilles Item, GatheringItem, GatheringItemPoint, GatheringPoint, GatheringPointBase, Aetheryte, TerritoryType, Weather, WeatherRate, FishingSpot. MIN/BTN, niveau, étoiles, lieu. Distinguer Item/Gathering/GatheringPoint IDs et Always/Never/Invalid. Existence statique ne prouve pas les prérequis personnage.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-SORT

REF-ID: REF-SORT
DOMAIN: SortaKinda #16
NAME: SortaKinda
REPOSITORY: https://github.com/MidoriKami/SortaKinda
COMMIT_OR_VERSION: 95a1bf9161f4bc595c21094eeaae9fda556f4b4b
LICENSE: AGPL v3+ (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : Règles ; slot sets ; configuration ; InventoryRenderer ; preview.
TECHNICAL_FACTS: Filtrage, ordre, slot sets, configuration globale/personnage, aperçu. Ne pas coupler sauvegarde et action de tri native. Étudier le comportement actuel lors de l'intégration.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.
PREVIOUS: 95a1bf9161f4bc595c21094eeaae9fda556f4b4b HISTORICAL_VERIFIED
PREVIOUS_VERIFIED_AT: 2026-10-27
CURRENT: HEAD inchangé 95a1bf9161f4bc595c21094eeaae9fda556f4b4b (commit du 2026-08-29), CURRENT_VERIFIED
CURRENT_VERIFIED_AT: 2026-10-30
EVIDENCE: clone git --depth 1 du dépôt public ; LICENSE AGPL v3+ ; FilterRules, OrderRules, SortingController, SystemConfiguration, InventoryItemExtensions lus.
DECISION: écarts consignés dans docs/goals/SORT-GAP-008-STUDY.md ; colonnes Lumina à confirmer localement avant usage.
REASON: préparer les tranches SortaKinda restantes sans recopier de code.

## REF-INVENTORY

REF-ID: REF-INVENTORY
DOMAIN: Inventaire secondaire
NAME: InventoryTools
REPOSITORY: https://github.com/Critical-Impact/InventoryTools
COMMIT_OR_VERSION: 70a9f410d9a8ab97bef6bcf795415ea2d7c75097
LICENSE: GPL v3 (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : ItemSearchService ; ItemObtainabilityService ; CraftListTrackingService ; ListFilterService.
TECHNICAL_FACTS: Portée propriétaire/servant/conteneur/NQ-HQ. Absence dans snapshot incomplet ne signifie pas quantité zéro. Service d'inventaire commun.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-SHINIES

REF-ID: REF-SHINIES
DOMAIN: Collections secondaire
NAME: XIV-Shinies
REPOSITORY: https://github.com/XIV-Shinies/xiv-shinies-plugin
COMMIT_OR_VERSION: e6f4341200f11db6d1bc827eee7fa16ad7aec55a
LICENSE: MIT (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : CollectorRegistry ; CollectResult ; CollectorRunner ; ExcelUnlockCollector ; GameThread ; QuestSequenceCollector ; CollectionStatusMemory.
TECHNICAL_FACTS: Collectors par domaine, raisons de skip, couverture explicite, erreurs isolées et thread approprié. Vide ne signifie pas complet.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-TOMESTONE

REF-ID: REF-TOMESTONE
DOMAIN: Collections secondaire
NAME: Tomestone
REPOSITORY: https://github.com/TomestoneGG/Dalamud.Tomestone
COMMIT_OR_VERSION: d9e69687d4453899d8d419ab3964383afa6e58a5
LICENSE: AGPL v3 (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : BuddyEquip ; Achievement ; Companion ; Mount ; Orchestrion ; TripleTriadCard ; CharaMakeCustomize ; Item ; AozAction.
TECHNICAL_FACTS: Domaines candidats de collection. Candidat de collection ne prouve jamais la possession.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-QUEST

REF-ID: REF-QUEST
DOMAIN: Quêtes secondaire
NAME: ffxiv-todo
REPOSITORY: https://github.com/datamachine/ffxiv-todo
COMMIT_OR_VERSION: ef5f5bab3b0b2c601a14d6c00d2088f1a40f1894
LICENSE: MIT (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : Catalogue ; unlock resolver ; PreviousQuest ; overrides.
TECHNICAL_FACTS: Préserver Unknown. Prérequis ALL/ANY et AND/OR à distinguer selon preuve ; ne pas simplifier arbitrairement.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-SNAPSHOT

REF-ID: REF-SNAPSHOT
DOMAIN: Snapshots secondaire
NAME: Tsukimichi
REPOSITORY: https://github.com/xenofei/Tsukimichi
COMMIT_OR_VERSION: a82116abee83a0fbf0356f3eaa29d5bb9fd800c1
LICENSE: MIT (observation historique fournie)
VERIFIED_AT: 2026-10-27
STATUS: HISTORICAL_VERIFIED
SOURCE: docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître seulement dans cette session ; fichiers/concepts rapportés : GameStateReader ; SnapshotService ; CharacterSnapshot ; QuestRecord ; QuestQuery ; SnapshotDiff ; DailyOfferBook ; GameResets ; JsonSnapshotStore.
TECHNICAL_FACTS: Toute donnée dynamique importante doit exprimer fraîcheur et portée ; séparer observation, acquisition réseau, calcul et mise à jour.
LOCAL_COMPATIBILITY: UNKNOWN pour ces commits historiques ; le build du bootstrap est consigné séparément.
NOTES: Conserver uniquement les faits techniques. Pas de conclusion produit issue d'un autre projet.

## REF-SCOPE

REF-ID: REF-SCOPE
DOMAIN: Périmètre
NAME: Liste autoritaire des 17 plugins
REPOSITORY: N/A — prompt utilisateur
COMMIT_OR_VERSION: SHA256 76B22632FEDCCA6D127FBDFE848AC87C9BC75663D0C36B238F985B23F2B38B9C
LICENSE: N/A — instructions utilisateur ; aucune licence de redistribution supposée
VERIFIED_AT: 2026-10-27
STATUS: LOCAL_VERIFIED
SOURCE: Deux pièces jointes utilisateur identiques, copie docs/ai/PROMPT-MAITRE.md
FILES_READ: Prompt maître complet ; empreintes des deux fichiers vérifiées
TECHNICAL_FACTS: 17 plugins prioritaires ; CORE-000 limité au bootstrap ; contexte et checkpoints persistants.
LOCAL_COMPATIBILITY: Appliqué à ROADMAP.md ; tous les plugins restent NOT_STARTED après le socle.
NOTES: Sources actuelles des autres plugins : NEEDS_STUDY lors de leur GOAL, sans étude globale initiale.

## REF-LOCAL-001

REF-ID: REF-LOCAL-001
DOMAIN: Toolchain et bootstrap
NAME: Environnement local compilé
REPOSITORY: N/A — assemblies installées
COMMIT_OR_VERSION: Dalamud 15.0.3.9 ; commit 30509c3b4fd00edd9a3d65b7bbf16433d84be03b ; SDK Dalamud 15.0.0 ; .NET SDK 10.0.403 / runtime 10.0.14
LICENSE: Licences de l'installation dans Hooks/15.0.3.9/licenses.txt ; non réauditées
VERIFIED_AT: 2026-10-27 Europe/Brussels
STATUS: LOCAL_VERIFIED
SOURCE: dotnet --info ; cache NuGet ; Hooks/dev et Hooks/15.0.3.9 ; build Release et tests
FILES_READ: Sdk/Sdk.props, Sdk/Sdk.targets, Sdk/SdkPackageVersions.props ; Dalamud.xml (sections interfaces/commande/UI) ; Dalamud.runtimeconfig.json ; version.json du dossier stable ; commit_hash.txt dev/stable ; cibles DalamudPackager
TECHNICAL_FACTS: SDK Dalamud cible net10.0-windows/C#14/x64, résout Hooks/dev par défaut et autorise DALAMUD_HOME. .NET du PATH est 9.0.302 ; SDK 10.0.403 disponible dans C:/ia-code/.tools/dotnet10. Stable et dev rapportent le même commit. version.json stable rapporte jeu 2026.10.16.0000.0000 et runtime 10.0.14. Le plugin compile avec injection constructeur, IDalamudPlugin, AddHandler, UiBuilder et WindowSystem.
LOCAL_COMPATIBILITY: COMPILED Release sans avertissement ni erreur ; 5/5 tests managés. Aucun test d'exécution Dalamud/FFXIV.
NOTES: Aucune hypothèse de compatibilité d'offsets, d'inventaire ou d'unlocks. Ne pas recopier d'assemblies hôte dans le paquet.

## REF-DOTNET-DOCS

REF-ID: REF-DOTNET-DOCS
DOMAIN: Outillage
NAME: Documentation Microsoft installation Windows
REPOSITORY: https://learn.microsoft.com/en-us/dotnet/core/install/windows
COMMIT_OR_VERSION: Page consultée le 2026-10-27
LICENSE: Documentation Microsoft ; pas de code repris
VERIFIED_AT: 2026-10-27
STATUS: CURRENT_VERIFIED
SOURCE: Recherche web ciblée pendant la localisation du SDK
FILES_READ: Section installation Windows (résultat de recherche)
TECHNICAL_FACTS: Les SDK peuvent être installés ou extraits dans un dossier explicite.
LOCAL_COMPATIBILITY: Installation devenue inutile : SDK existant trouvé localement.
NOTES: La preuve locale, et non la version citée par la page, détermine global.json.

## REF-INV-MODEL

REF-ID: REF-INV-MODEL
DOMAIN: Fondation d'inventaire partagée
NAME: Sémantique du snapshot managé CORE-001
REPOSITORY: N/A — sources originales locales Tensho--Assistant
COMMIT_OR_VERSION: CORE-001
LICENSE: Code original local ; aucune source externe copiée
VERIFIED_AT: 2026-10-27 Europe/Brussels
STATUS: LOCAL_VERIFIED
SOURCE: Sources managées et Verify.ps1 exécuté avec succès
FILES_READ: src/TenshoAssistant/Inventory/InventorySnapshot.cs ; InventoryService.cs ; tests/TenshoAssistant.Tests/InventoryTests.cs
TECHNICAL_FACTS: Snapshots figés, types d'identifiants distincts, requêtes à portée explicite, borne minimale et exactitude nullable, couverture par conteneur, publication atomique du dernier lot et invalidation totale. Huit tests inventaire passent, 13/13 avec le socle.
LOCAL_COMPATIBILITY: Compilation Release .NET 10.0.403 et SDK Dalamud 15.0.0 ; aucune lecture native ni validation en jeu.
NOTES: Vérification du code original, pas de nouvelle validation des références historiques. Les garanties de lecture/complétude de IGameInventory restent à étudier dans INV-001.

## REF-INV-API

REF-ID: REF-INV-API
DOMAIN: Acquisition des sacs et lifecycle
NAME: Contrat public local IGameInventory / identité / framework
REPOSITORY: N/A — assemblies installées ; dépôt associé https://github.com/goatcorp/Dalamud
COMMIT_OR_VERSION: Hooks/dev, commit 30509c3b4fd00edd9a3d65b7bbf16433d84be03b (installation consignée REF-LOCAL-001)
LICENSE: Assemblies de l'installation existante ; aucun code de Dalamud copié
VERIFIED_AT: 2026-10-27 Europe/Brussels
STATUS: LOCAL_VERIFIED
SOURCE: Dalamud.xml installé ; inspection de métadonnées via tools/ApiProbe ; compilation du nouvel adaptateur
FILES_READ: Dalamud.xml sections IGameInventory, IPlayerState ; métadonnées Dalamud.dll : GameInventoryType, GameInventoryItem, IGameInventory et InventoryChangelogDelegate, IFramework et OnUpdateDelegate, IClientState et LogoutDelegate ; sources locales de l'adaptateur et du coordinateur
TECHNICAL_FACTS: GetInventoryItems(GameInventoryType) retourne ReadOnlySpan<GameInventoryItem>. Inventory1–4 valent 0–3. Les slots exposent IsEmpty, ContainerType, BaseItemId, IsHq et Quantity (Int32). InventoryChanged reçoit IReadOnlyCollection<InventoryEventArgs>. Update reçoit IFramework ; Logout reçoit deux Int32. IPlayerState expose IsLoaded et ContentId ; IsLoaded peut être vrai sans objet LocalPlayer. Aucun indicateur public de complétude/chargement de conteneur dans IGameInventory inspecté.
LOCAL_COMPATIBILITY: Adaptateur compilé avec injection des services ; coordinateur managé testé, 20/20 tests au total. Runtime inconnu ; aucun Complete émis. Le contrat public seul ne justifie pas une preuve d'absence à partir d'un span vide.
NOTES: XML et métadonnées ne prouvent pas le comportement live. Span vide classé Unknown, non vide Partial. Les événements ne sont utilisés que pour invalider ; les données d'événement ne sont pas conservées. La source historique reste inchangée.

## REF-INV-LOAD

REF-ID: REF-INV-LOAD
DOMAIN: Chargement des sacs et preuve de complétude
NAME: InventoryManager / InventoryContainer locaux
REPOSITORY: N/A — assembly installée FFXIVClientStructs.dll
COMMIT_OR_VERSION: SHA256 F5A2C68CCECD3E199BDA572050E98902B2E7594066C95DC7826C2D4297CD0564
LICENSE: Assembly de l'installation existante ; aucun code externe copié
VERIFIED_AT: 2026-10-27 Europe/Brussels
STATUS: LOCAL_VERIFIED
SOURCE: Métadonnées de Hooks/dev/FFXIVClientStructs.dll via ApiProbe ; compilation de l'adaptateur contre cette assembly
FILES_READ: Métadonnées de InventoryManager et InventoryContainer ; XML installé ; InventoryReadEvidence.cs et tests associés
TECHNICAL_FACTS: InventoryManager.Instance() retourne InventoryManager* ; GetInventoryContainer(InventoryType) retourne InventoryContainer*. InventoryContainer expose Items (InventoryItem*), Type, Size (Int32) et IsLoaded (Boolean). Ces membres typés permettent de croiser chargement, identité du conteneur, stabilité du buffer et nombre de slots lus. Aucun offset numérique nouveau n'est utilisé.
LOCAL_COMPATIBILITY: Build Release 0.1.1.0 réussi ; 25/25 tests managés et paquet vérifiés. Appels natifs ajoutés non encore validés en jeu.
NOTES: Complète REF-INV-API, qui reste correct sur l'absence d'indicateur dans IGameInventory seul. La sémantique native est celle des membres installés ; sa confirmation live reste RUNTIME-001.

PREVIOUS: L'adaptateur 0.1.0 émet uniquement Partial/Unknown faute de preuve de chargement via IGameInventory.
PREVIOUS_VERIFIED_AT: 2026-10-27
CURRENT: 0.1.1 émet Complete pour un sac uniquement si en-têtes chargés valides avant/après, buffer stable et slots lus égaux à Size ; sinon Partial/Unknown.
CURRENT_VERIFIED_AT: 2026-10-27 Europe/Brussels
EVIDENCE: Membres de l'assembly installée, code compilé et tests InventoryReadEvidenceTests.
DECISION: Utiliser cette preuve supplémentaire pour un total exact des quatre sacs ; aucun total global de tous les rangements.
REASON: Corriger le libellé toujours inconnu sans transformer un span vide en preuve de quantité zéro.

## REF-ITEM-API

REF-ID: REF-ITEM-API
DOMAIN: Catalogue Item partagé
NAME: Source Lumina Item et métadonnées de génération
REPOSITORY: N/A — assemblies Dalamud/Lumina/Lumina.Excel installées
COMMIT_OR_VERSION: Installation REF-LOCAL-001 ; schéma Lumina.Excel et versions repositories capturés dans chaque ItemCatalogMetadata lors du chargement
LICENSE: Assemblies existantes ; aucun code externe repris, catalogue construit depuis les données locales du jeu
VERIFIED_AT: 2026-10-27 Europe/Brussels
STATUS: LOCAL_VERIFIED
SOURCE: Métadonnées via tools/ApiProbe, documentation XML locale et compilation Release 0.1.4
FILES_READ: Item.RowId/Name/CanBeHq dans Lumina.Excel.dll ; Lumina.GameData.Repositories et Lumina.Data.Repository.Name/Version dans Lumina.dll ; XML IDataManager.GetExcelSheet et ExcelSheet.Count/GetRowAt/Language ; ReadOnlySeString.ExtractText
TECHNICAL_FACTS: GetRowAt utilise un index séquentiel distinct de RowId. Name est une ReadOnlySeString extraite immédiatement en string. La feuille expose sa langue ; les repositories exposent des versions copiables. Les définitions/index publiés ne contiennent ni feuille, ligne Lumina ni pointeur natif. La construction peut donc travailler uniquement sur les données managées copiées.
LOCAL_COMPATIBILITY: Plugin 0.1.4 compilé, 31/31 tests managés et paquet verts. Aucun chargement réel de cette nouvelle source dans le client n'a encore été observé.
NOTES: Le budget de copie framework est souple ; ni latence réelle ni compatibilité d'une future version du jeu ne sont revendiquées. La langue/génération du catalogue ne doit pas être confondue avec la possession d'un objet dans l'inventaire.

RUNTIME_EVIDENCE_AT: 2026-10-27 Europe/Brussels
RUNTIME_EVIDENCE: Capture utilisateur codex-clipboard-e454bd2e-7f41-4788-87bf-0ced362301f8.png et retour « tous est OK ». Catalogue chargé : 50 880 objets, French ; recherche sirop, sélection du résultat 12892 et diagnostic 0 NQ/156 HQ/total 156 visibles. Source réelle et rendu confirmés dans ce périmètre ; ne pas extrapoler aux performances ou scénarios d'erreur. Détails dans TESTING.md.

## REF-UI-ICONS

REF-ID: REF-UI-ICONS
DOMAIN: Interface / répertoire des outils
NAME: IconButton et FontAwesome natifs Dalamud
REPOSITORY: N/A — installation locale Dalamud, voir REF-LOCAL-001
COMMIT_OR_VERSION: Assembly installée utilisée par le build 0.2.1 ; commit Dalamud consigné dans REF-LOCAL-001
LICENSE: Ressources déjà fournies par l'hôte ; aucun asset externe copié ou redistribué
VERIFIED_AT: 2026-10-27 Europe/Brussels
STATUS: LOCAL_VERIFIED
SOURCE: Documentation XML Dalamud et métadonnées de FontAwesomeIcon via ApiProbe
FILES_READ: Dalamud.xml, ImGuiComponents.IconButton(string, FontAwesomeIcon, Vector2) ; champs utilisés de FontAwesomeIcon ; MainWindow.Tools.cs
TECHNICAL_FACTS: Un bouton icône peut être créé à partir d'un identifiant, d'un glyphe FontAwesome et d'une taille Vector2. Les 17 glyphes des modules futurs et BoxOpen existent localement. La police est gérée par l'hôte.
LOCAL_COMPATIBILITY: Compilation Release 0.2.1 sans avertissement ; 31/31 tests et paquet verts. Rendu et clics dans le client encore à valider.
NOTES: L'ouverture et le rendu d'une popup ImGui doivent utiliser le même niveau de pile ID ; le code ouvre la fiche hors de la portée de la table.

## REF-BANK-API

REF-ID: REF-BANK-API
DOMAIN: Inventaires servant/chocobo et import manuel
NAME: Contextes, identités et positions des menus d'inventaire
REPOSITORY: N/A — assemblies locales Dalamud et FFXIVClientStructs, installation REF-LOCAL-001
COMMIT_OR_VERSION: Assemblies installées contre lesquelles 0.3.0 compile ; FFXIVClientStructs SHA256 consigné dans REF-INV-LOAD
LICENSE: Assemblies existantes ; aucun code externe ou asset copié
VERIFIED_AT: 2026-10-27 Europe/Brussels
STATUS: LOCAL_VERIFIED
SOURCE: Métadonnées via ApiProbe, documentation XML locale et compilation du nouvel adaptateur
FILES_READ: RetainerManager et Retainer (RetainerId, NameString, IsReady), XML GetActiveRetainer/LastSelectedRetainerId ; IGameGui.GetAddonByName ; AtkUnitBasePtr.IsNull/IsReady/IsVisible/Position/ScaledWidth ; types AddonInventoryRetainer, AddonInventoryRetainerLarge, AddonInventoryBuddy ; groupes GameInventoryType déjà inspectés
TECHNICAL_FACTS: GetActiveRetainer retourne le servant référencé par LastSelectedRetainerId, pas une preuve autonome de menu ouvert. RetainerPage1–7 correspondent aux IDs 10000–10006 ; sacoches 4000/4001 et premium 4100/4101. Le wrapper d'addon fournit visibilité et coordonnées copiables. Le lecteur natif commun réutilise IsLoaded/type/taille/buffer stables avant de publier un groupe complet.
LOCAL_COMPATIBILITY: Build 0.3.0 réussi, 43/43 tests managés et paquet vérifiés. Banque/save/recherche managés testés ; menus, identités réelles et conteneurs servant/chocobo non encore validés dans le client.
NOTES: Les noms runtime InventoryRetainer/InventoryRetainerLarge/InventoryBuddy sont choisis à partir des types locaux ; InventoryBuddy2 est un candidat de repli. Cette correspondance et l'ancrage restent UNKNOWN côté observation runtime, à confirmer par BANK-001-RUNTIME. Aucun attribut de nom d'addon exploitable n'a été observé sur ces types lors du sondage. Aucun offset numérique ajouté, aucune requête serveur ou action de déplacement d'objet exécutée.

RUNTIME_EVIDENCE_AT: 2026-10-27 Europe/Brussels
RUNTIME_EVIDENCE: Capture 55b7452f-db39-4732-aca5-13ee28923f23 : v0.3.0, cache Sacoche chocobo affiché avec 26 HQ et date UTC, sacs avec 182 NQ, cumul recensé 208. Cela confirme la consultation d'un import chocobo dans la recherche, pas l'ancrage, les servants ou la conservation après reload. Détails dans TESTING.md.

## REF-ITEM-TEXTURES

REF-ID: REF-ITEM-TEXTURES
DOMAIN: Catalogue Item et icônes UI
NAME: Item.Icon / ITextureProvider / ISharedImmediateTexture
REPOSITORY: N/A — assemblies locales Lumina.Excel et Dalamud
COMMIT_OR_VERSION: Installation REF-LOCAL-001, API compilée en 0.4.0 ; catalogue garde les versions de données/schema dans ses métadonnées
LICENSE: Assets du jeu chargés par l'hôte existant ; aucun fichier d'image copié ou redistribué
VERIFIED_AT: 2026-10-27 Europe/Brussels
STATUS: LOCAL_VERIFIED
SOURCE: Métadonnées Item et interfaces textures, documentation XML locale, compilation et tests du catalogue
FILES_READ: Item.Icon ; GameIconLookup (IconId/ItemHq/HiRes), ITextureProvider.TryGetFromGameIcon, ISharedImmediateTexture.TryGetWrap, IDalamudTextureWrap.Handle/Size ; documentation des durées de vie et usage au thread UI
TECHNICAL_FACTS: L'icône de base est un identifiant distinct d'Item.RowId. Lookup demandé haute résolution, sans variante HQ imposée. TryGetWrap fournit un wrap valide pour la frame seulement, sans attente ; ne pas le conserver entre frames. Propriété de la ressource conservée par l'hôte. Le code transmet Handle à ImGui.Image et réserve la taille si la texture n'est pas encore disponible.
LOCAL_COMPATIBILITY: Release 0.4.0 compilée sans avertissement ; 55/55 tests et paquet verts. IconId est préservé par recherche/lookup dans les tests. Aucun rendu d'icône de cette version encore observé en jeu.
NOTES: Les fixtures de tests vérifient la transmission d'un numéro d'icône, pas l'existence ou l'apparence d'un asset. Aucune texture n'est chargée dans le worker du catalogue.

## REF-CRAFT-LOCAL-API

REF-ID: REF-CRAFT-LOCAL-API
DOMAIN: Catalogue de recettes
STATUS: LOCAL_VERIFIED / 2026-10-27 Europe/Brussels
SOURCE: ApiProbe sur les assemblies Dalamud/Lumina locales, puis compilation de l'adaptateur 0.5.0
TECHNICAL_FACTS: Recipe fournit ItemResult, Ingredient, AmountResult, AmountIngredient, CraftType et RecipeLevelTable. CraftType fournit Name ; RecipeLevelTable fournit ClassJobLevel. Les RowRef sont résolus par RowId via les feuilles correspondantes.
LIMITS: Les valeurs et IDs sont copiés en données managées ; aucune validation de l'apparence ou de la complétude du catalogue n'est revendiquée avant l'essai en jeu.


## REF-FASHION-AVANTGARDE (0.213)
REPOSITORY: github.com/NeNeppie/AvantGarde — licence AGPL-3.0 : lu pour comprendre, aucun code copié (Tensho est GPL v3, code original).
FILES_READ: AvantGarde.cs, UI/MainWindow.cs, UI/SlotWindow.cs, Managers/DataManager.cs, Utilities/EnumItemSlot.cs (2026-10-31).
TECHNICAL_FACTS: fenêtre du jeu « FashionCheck » ; catégorie de l'emplacement n = AtkValue chaîne à l'indice 2 + 11 × n ; 11 emplacements
(arme, tête, torse, mains, jambes, pieds, oreilles, cou, poignets, bague droite, bague gauche) ; semaines depuis le 30/01/2018 8 h UTC,
FashionCheckWeeklyTheme = semaine + 9 ; données d'équipement FFXIVGachaSpreadsheet (FashionReport.json, déjà utilisé par Tensho).
UX reprise (idée seulement) : liste par emplacement, possédés en premier, non possédés grisés, tri par popularité, panneau près de la fenêtre.
LIMITS: lecture AtkValues à confirmer en jeu (journal du mode développeur) ; teintures non reprises ; coffre à apparence et armoire non lus.

## REF-FISH-GATHERBUDDY (0.227)
REPOSITORY: github.com/Ottermandias/GatherBuddy — licence Apache-2.0 (réutilisation permise avec mention).
FILES_READ: GatherBuddy.GameData/Data/Fish/Data*.cs (36 fichiers, 1 183 instructions « data.Apply(…) », lus le 2026-11-01).
USAGE: fichiers téléchargés à l'exécution et lus comme données (`Community/GatherBuddyFish.cs`, analyseur original, rien n'est compilé ni
copié dans Tensho) : Bait, Mooch, Bite (HookSet, BiteType), Time (minutes ET), Weather / Transition (ids de la feuille Weather), Lure,
Predators, Snag. Crédit dans README et dans l'aide à la pêche. Garland reste la source de secours.
