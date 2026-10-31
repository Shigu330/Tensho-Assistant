# Architecture

## Vue d'ensemble — état au 0.154.0 (2026-10-31, relu contre le code)

Un projet plugin (`src/TenshoAssistant`, .NET 10, Dalamud.NET.Sdk 15) et un exécutable de tests sans dépendance native
(`tests/TenshoAssistant.Tests`, qui inclut les fichiers C# purs du plugin). Les fichiers `*.Dalamud.cs` touchent au jeu ;
`*.Lumina.cs` sont les copies de feuilles Excel partagées avec les sondes de `tools/` ; le reste est du C# pur testé.

### Hôte et cycle de vie

`Plugin` reçoit les services Dalamud par injection, charge `Configuration` v1 (version inconnue refusée sans écrasement), crée
`MainWindow` et enregistre chaque lecteur, chargeur et pont dans `RegistrationScope` (nettoyage en ordre inverse, idempotent, poursuivi
après erreur ; un échec du constructeur déclenche le même nettoyage). Commande `/tensho`, `OpenMainUi`, `OpenConfigUi`.

### Domaines (`src/TenshoAssistant/`)

| Dossier | Rôle |
|---|---|
| `Items/` | catalogue d'objets commun (`ItemCatalog`, déblocages monture/mascotte/orchestrion), survol d'objet, menu contextuel, usages |
| `Inventory/` | inventaire, banque des servants/coffres, tri visuel natif (règles, zones, modèles) |
| `Craft/` | recettes, simulateur, solveur, macros, observation en direct de la synthèse |
| `Gathering/` | points de récolte et de pêche, horaires Éorzéa, météo, PR (GP) |
| `Maps/` | carte Tensho : catalogue de cartes, projection, état en direct, index local, remplacement de la carte du jeu |
| `Npcs/` | marchands PNJ (boutiques gil, échanges) |
| `Collections/`, `Quests/` | collections (montures, mascottes, titres, succès, orchestrion…), quêtes, récompenses, objets de quête |
| `Market/` | prix Universalis (sur demande ou au survol selon les réglages), coût de préparation |
| `Community/`, `Monsters/`, `Duties/`, `Fashion/` | données communautaires : Garland Tools, wiki FFXIV, butin de donjons et de monstres, Revue de mode |
| `MiniGames/`, `Macros/`, `Character/` | Mini Cactpot, Triple Triad, chaîne de macros, statistiques du personnage |
| `Caching/` | cache disque versionné (`DataCache`) et durées par domaine (`CachePreferences`) |
| `Localization/` | langues JSON embarquées, validation |
| `UI/` | thème (palette du jeu), composants partagés, navigation |

`MainWindow` est découpée en partials par outil (`MainWindow.<Outil>.cs`). Le hub et la navigation verticale y vivent ; le répertoire
des outils (`MainWindow.Tools.cs`) liste 18 routes. Les paramètres (`MainWindow.Settings.cs`) ont les onglets Général, Interface,
Modules, Carte, Données et Debug (mode développeur) ; les diagnostics sont séparés (`MainWindow.Diagnostics.cs`).

### Règles de fil d'exécution

- Lecture native sur le thread framework (`IFramework.Update`), par lots bornés (en général 32 lignes / 2 ms), puis copie en
  enregistrements managés immuables publiés atomiquement. `Draw` ne lit ni pointeur ni feuille Excel.
- Chaînes natives lues avec une borne (exemple : `Utf8String` des infobulles de la carte, longueur = `BufUsed − 1`, voir
  [l'extrait du plantage](../evidence/0.133.0-crash-excerpt.log)).
- Les services qui lancent du travail (workers, requêtes) possèdent leur jeton d'annulation et une génération ; les résultats tardifs
  sont rejetés.

### Carte Tensho (`Maps/`, `MainWindow.MapView.cs`, `MainWindow.MapLocalLists.cs`)

- `MapCatalogLoader` copie les cartes et marqueurs (`MapMarkerSource`) ; `MapProjection` convertit monde ↔ texture ↔ écran.
- `MapLiveReader` publie deux fois par seconde : position, drapeau, ALEA, groupe, marqueurs d'événement du jeu (quêtes, mandats),
  courants éthérés et exploration débloqués, combat/instance.
- `MapLocalIndexLoader` construit l'index local par carte (donneurs de quête, points de récolte et de pêche, exploration, courants) ;
  `NpcVendorLoader` les marchands. Les deux passent par le cache disque.
- `NativeMapReplacer` garde l'addon `AreaMap` ouvert hors écran pour que le jeu continue à mettre à jour ses marqueurs, lit la demande
  d'ouverture (`AgentMap`, quête du journal, téléporteur, ALEA) et ouvre la carte Tensho à la place ; la carte du jeu est restaurée à la
  fermeture ou au déchargement.
- Réglages : `MapViewSettings` dans `Configuration` (tailles et priorités d'icônes par carte, suivi du zoom, zones colorées, filtres).

### Sources d'objets (« Comment l'obtenir »)

Ordre : données du jeu (marchands PNJ, quêtes, succès, recettes, récolte), puis Garland Tools (`GarlandItemSourceService`, seulement
si aucune source n'est connue), puis section « Acquisition » du wiki (`WikiItemAcquisitionService`). Le marché est un critère, pas
une source. Les requêtes externes passent par `BoundedHttp` et sont mises en cache.

### Cache disque (`Caching/DataCache.cs`)

`DataCache-v1` dans le dossier de configuration du plugin. Enveloppe versionnée (schéma, famille, portée, version, date) ; écriture
hors du thread framework ; 32 Mo par entrée, 2048 fichiers. À chaque écriture, l'ancienne entrée de même famille et portée est
supprimée ; les catalogues (`catalog-*`) ne sont jamais évincés au nombre. Durées réglables par domaine (`CacheArea` : prix, quêtes,
collections, sources, Revue de mode, stock importé, calculs d'artisanat).

### Interface et langues

`UI/FfxivTheme.cs` porte la palette (suivie sur le thème du jeu) ; PreDraw/PostDraw pour le hub, scopes jetables pour les panneaux.
`LocalizationManager` charge les JSON embarqués (anglais = référence et repli, français complet, allemand/japonais partiels). Les noms
d'objets, actions et lieux viennent du catalogue du jeu. Voir [Localization/README.md](../../Localization/README.md).
Dates affichées en heure locale ; calculs en UTC et heure d'Éorzéa.

La compilation et les tests managés ne valident pas le rendu ImGui ni la lecture native : seul l'essai en jeu le fait
(état par version : [STATUS](../ai/STATUS.md), [CHANGELOG](../../CHANGELOG.md)).

## Historique des domaines et des versions

> Archive : notes de conception écrites au moment de chaque objectif (2026-10-27 → 2026-10-29). Elles restent justes sur les
> principes, mais les noms de versions, compteurs de tests et « état courant » sont ceux de leur date. Le code fait foi.

USE-CONTEXT-004 : ItemContextMenuBridge possède IContextMenu.OnMenuOpened et Logout. Seule la cible Inventory est lue au callback ; closures sans args/target/pointeur. ItemContextMenuSession conserve un jeton normalisé, propriétaire, révision et date, rejette remplacement/logout/désactivation/expiration après deux minutes et remet au plus une navigation au Draw suivant. Draw revalide le contexte puis MainWindow.OpenItemContext réutilise SelectItem et les routes existantes. Pas de poller natif ou réseau supplémentaire.

État au 0.75.0 (2026-10-29, archive) : [cache partagé et péremption réglable](../goals/CACHE-001.md). Prix, quêtes, collections, sources communautaires, Revue de mode et calculs du carnet réutilisent des instantanés persistants datés. Durées indépendantes dans Réglages : 5 min, 1 h, 24 h ou 7 j ; actualisation forcée depuis les outils. Les imports restent manuels et signalent leur péremption ; les observations en direct gardent une invalidation courte. 466/466 tests/package/SHA-256 passants, zéro avertissement/erreur ; validation en jeu de ce cache encore à faire.

QUEST-ITEM-SOURCES-004 : QuestRewardIndex figé construit par worker pur depuis les copies de QuestService ; aucune API native. QuestState sépare chargement/erreur d’index et observation. Publication verrouillée par génération/snapshot/annulation ; nouvelles données/invalidation/clear/fail/dispose annulent et retirent le handle courant, finally libère le jeton. Reader.Dispose termine le service. UI recherche O(1), clipper et navigation SelectQuest existante ; cache de métadonnées lié à Entries pour éviter une reconstruction à chaque frame en fiche ou à la seule publication de l’index.

QUEST-REWARDS-003 : QuestRewardSnapshot immuable s’ajoute à QuestEntry, indépendant du owner/progrès. QuestRewardCopy.Lumina partagé reader/probe décode les RowRef polymorphes et toutes les sous-lignes métier/rang ; référence non Item jamais normalisée en Item. Correspondance Tomestones par clé étrangère, inconnus et parent/subrow/slots conservés. Tables lues/cachées dans le lecteur Quest existant (32/2 ms), erreurs de récompenses isolées de la progression, cleanup étendu. Draw ne consulte que les modèles/Item communs.

COLL-003 : lecteur partagé étendu via partial Completion, 32 lignes/2 ms. CompletionCopy.Lumina copie strictement Title/Achievement/category/kind et récompenses. CollectionAcquisitionEvidence sépare positif, négatif sous couverture stable et inconnu ; offline/invalidation effacent les deux preuves, contradiction refusée avant publication. Le signal Unlock ne copie que RowType et coalesce RequestRefresh ; pending en fin de scan empêche publication, changement de chargement relance, rollback et Dispose couvrent les deux abonnements. UI catégories/fiches liées en cache, clipper, Item/contexte existants.

COLL-002 : CollectionReader ajoute la phase Orchestrion dans ses lots de 32/2 ms, mêmes owner/garde framework/rafraîchissement. OrchestrionCopy.Lumina partage la projection strictement jointe avec CollectionProbe. CollectionCategoryIssue isole les erreurs nouvelles sans effacer les autres catégories ; snapshot readonly et nettoyage existants étendus. ItemDefinition.OrchestrionId et index ForOrchestrion étendent le catalogue commun, copie typée ItemOrchestrionCopy.Lumina ; aucune feuille/RowRef dans Draw. Fiches et Usages naviguent via SelectItem/contexte commun ; aucun lecteur, cache ou réseau supplémentaire.

GATHER-FISH-FORECAST-010 : FishingForecast est un calcul pur borné (1–24 périodes), utilisant WeatherCatalog.Forecast et GatheringTime.Between (clipping/fusion rationnelle des plages, horizon ≤ neuf jours ET). ReportedFields indique la couverture ; bool? ReportedConditionsMatch conserve vrai/faux/inconnu. MainWindow calcule seulement au clic et lie son dernier résultat aux références conditions/catalogue et IDs Item/territoire. Ni réseau, ni lecture native ou worker ajoutés ; pas de promesse de capture.

GATHER-FISH-WEATHER-009 : WeatherDefinition ajoute EnglishName optionnel, WeatherCopy.Lumina partage la copie locale/anglaise par RowId entre loader/probes. Le loader météo existant lit les définitions complètes avant les territoires, lots 32/2 ms ; erreur anglaise isolée. WeatherCatalog construit un index (TerritoryId, nom anglais exact) parmi les poids positifs seulement si métadonnées complètes et identité unique. Aucun fallback global/local approximatif ; Draw récupère nom local et icône sans feuille native.

GATHER-FISH-WINDOWS-008 : FishingConditionParser ajoute une projection optionnelle par variante sans affecter les appâts/monstres en cas d’erreur. Horaires entiers ET normalisés par GatheringSchedule et conversion/cache DrawGatheringTime existants ; listes météo readonly conservant les noms de source. Aucune correspondance météo locale supposée ni calcul d’intersection. Cadres de lieu repliables, lien vers prévisions natives existantes.

GATHER-FISH-SOURCES-007 : FishingSourceParser étend la projection d’objet MonsterItemDocument/MonsterLootService existante, sans second service/cache/transport. Parsing optionnel borné et isolé des références de monstres ; chaînes readonly, index FishingSpot typé pour Draw. La sélection ne fait aucune requête. Consultation/actualisation manuelle, FetchedAt et états inconnus explicites.

GATHER-FISH-006 : FishingNotebookIndex est inclus dans GatheringCatalog, sans second service. FishingNotebookCopy.Lumina projette FishParameter/FishingNoteInfo sur framework, RowId et Item strictement concordants ; champs non interprétés restent inconnus. Le loader existant ajoute la phase bornée et transmet uniquement records/strings/primitives au worker. Draw utilise l’index immuable. Ce carnet natif reste distinct des appâts communautaires.

SORT-TEMPLATES-007 : InventorySortTemplates sépare règles réutilisables (version, GUID, nom, géométrie, critères/zones) des profils appartenant aux personnages/rangements. La validation de contenu est partagée sans propriétaire factice. Un modèle compatible est copié/relié au scope courant dans le brouillon, sans reçu de capture ni application. Configuration v1 ajoute une bibliothèque vide, rollback à la sauvegarde. Verify.ps1 compare aussi les SHA-256 DLL/manifeste du ZIP aux fichiers canoniques.

SORT-FILTERS-006 : InventorySortMetadata transporte propriétés bool? et masque nullable depuis le catalogue Item commun, avec contrôle d’identité et cache par ID durant l’aperçu. Les callbacks nom/catégorie antérieurs restent compatibles, autres propriétés inconnues. EquipmentSlotMap centralise aussi MainHand/OffHand/SoulCrystal ; le loader étend la copie existante, sans nouveau lecteur. Les filtres Oui/Non n’acceptent pas Unknown ; la permutation native reste inchangée.

SORT-PROTECTED-005 ajoute Preserve/ReverseFill facultatifs à la zone. Le moteur valide les indices par InventorySortScope.Extract, retire les piles protégées avant tous les filtres et conserve les trous ; le plan positionné partagé assure la bijection physique. Une inversion change les cibles de la zone filtrée sans inverser les critères. Le bridge natif reste inchangé ; zéro lecture supplémentaire.

SORT-RULES-004 : InventorySortRules fournit profils validés par scope, critères multiples et zones filtrées sans chevauchement. ItemCatalog expose les catégories natives dédupliquées, calculées lors de sa construction. InventorySortResult.Slots conserve les trous ; InventoryVisualSortPlan les transforme en vraies cases physiques vides et vérifie l’ensemble des piles avant la permutation existante. Configuration v1 ajoute des profils indépendants et leurs listes copiées ; sauvegarde distincte du clic d’application, rollback en cas d’erreur.

SORT-SCOPES-003 : InventorySortScope distingue personnage, propriétaire natif, conteneurs et capacité. BankInventoryBridge partage contrôles de fenêtre/chargement et capture directe sans import. Le service exige la référence du snapshot exact publié pour ce scope ; un cache cloné ne suffit pas. Capture/application partagent génération et lifecycle. Inspection deux minutes, recapture fraîche et bon servant/module vérifiés au framework ; géométrie d’aperçu adaptée.

SORT-NATIVE-002 : InventoryVisualSortPlan transforme l’aperçu commun en permutation managée de toutes les cases personnelles, vides incluses. InventoryVisualSortService possède une demande explicite et ses générations. InventoryVisualSortBridge recapture et vérifie au framework.Update avant de permuter les entrées visuelles existantes et marquer HasChanges ; aucun pointeur conservé, aucun transfert physique. Logout/Dispose annulent. La lecture commune actualise l’ordre chaque seconde ; les caches importés ne servent pas de source d’écriture.

Historique 0.5.27 : catalogue Item/Recipe, banque à ordre visuel natif, carnet par métier et préparation sont implémentés ; les sections anciennes ci-dessous décrivent leur historique. La sortie du plugin est imposée dans le csproj à bin/x64/Configuration, y compris lors d'un build direct. Les extensions réutilisent ces services ; le checkpoint actuel et GOAL.md font autorité.

Un projet plugin et un exécutable de tests sans dépendance native. CORE-000 fournit l'hôte ; CORE-001 ajoute le modèle d'inventaire commun.

CRAFT-SIM-001 : les paramètres de simulation sont des valeurs immuables de RecipeDefinition, dans le catalogue partagé. Un loader framework borné vérifie et copie 136 actions métiers, puis publie un catalogue atomique ; pas de pointeur ou ligne native dans le moteur/UI. CraftSimulation retourne un état par étape, limite 100 actions, arrête à la première impossibilité/fin/casse et accepte l'annulation. MainWindow.CraftSimulation possède la séquence et invalide les résultats à l'édition, les attributs copiés au changement de personnage. Pas de worker ni d'exécution native ajoutés.

CRAFT-MACRO-001 : export pur vers pages de 15 lignes maximum à partir des étapes d'une simulation terminée. Noms natifs cités et validés, attentes bornées, pages immuables ; l'UI lie l'export à l'identité du résultat et fournit une copie par bloc. Aucun appel au module de macros ou au bridge de Macro Chain.

CRAFT-SOLVE-001 : transition pure partagée avec le simulateur, recherche par faisceau déterministe bornée, déduplication d'état complet et replay de la proposition. Session propriétaire du worker/CTS/génération ; entrées managées copiées, invalidation owner/recette/scénario/contexte et rejet des publications tardives. Le solveur normal ne lit ni ne commande une synthèse native.

CRAFT-ACTIONS-002 étend le même moteur à 26 actions / 208 variantes natives. CraftSimState conserve deux booléens distincts pour la protection persistante de Main suprême et son utilisation unique ; leur égalité participe à la déduplication de recherche. Les efficacités progression/qualité de Travail minutieux sont séparées. Les règles d'ouverture/niveau/Parcimonie/Calme intérieur précèdent tout débit de ressources.

CRAFT-LIVE-001 ajoute un lecteur framework opt-in à 5 Hz, avec champs nommés de CraftEventHandler/AddonSynthesis et PS/statuts de LocalPlayer. Publication atomique d'un cadre managé immuable, lease propriétaire/génération, contrôles de stabilité/visibilité et bornes de lecture. Les effets bruts et étapes natives/UI restent distincts ; aucun état de solveur n'est fabriqué à partir des champs non établis. Draw ne lit aucun pointeur.

CRAFT-LIVE-002 étend le catalogue natif à douze métadonnées Status et décode les compteurs Param des effets reconnus. StatusesComplete exige une énumération entière de longueur bornée ; doublons/bornes/effets non modélisés restent signalés. L'historique d'une protection consommée est nullable et les procs ne deviennent pas un combo inventé. Les résultats du décodeur sont immuables et ne sont pas injectés dans le solveur.

GATHER-WEATHER-001 : WeatherCatalog immuable/partagé, poids et noms/icônes natifs copiés par territoire, prévisions UTC/ET déterministes et tirages manquants inconnus. Loader framework borné avec désinscription ; cache de présentation par territoire/période/catalogue. Mappy et les sources de récolte ouvrent la même page ; aucune dépendance au réseau ou à un pointeur natif dans Draw.

GATHER-WINDOWS-003 : GatheringTime consomme les horaires normalisés déjà copiés, convertit les bornes rationnelles ET en ticks UTC et fusionne les fenêtres contiguës à minuit. Résultats immuables/calculés ; cache UI borné par seconde et clé d'horaire, fin de fenêtre invalidée immédiatement. Pas de nouveau service natif ou de détection de disponibilité.

CRAFT-PRESETS-001 ajoute une liste typée dans Configuration v1, avec copies d'actions, résolution par IDs et mutations pures bornées. Commit remplace temporairement la liste pour SavePluginConfig puis restaure la référence précédente sur échec. Chargement UI invalide résultat/export/recherche ; aucun attribut ou état natif persisté. Les lignes non résolues restent conservées.

CRAFT-LIVE-003 ajoute des valeurs typées contrôlées et des admissibilités natives à la copie framework. La projection pure d'un coup utilise Apply sur les états possibles de combo/historique et retourne des intervalles, sans choisir un état inconnu. Cache UI par identité de cadre/catalogue et rejet à deux secondes. Les codes Normale du handler (1) et de l'addon (0) restent distincts ; aucune API de coût à paramètres inconnus ou exécution native n'est appelée.

CRAFT-CONDITIONS-004 partage la classification des outils dans ItemCatalog, avec copie anglaise bornée et absence de bonus seulement si déclaration explicitement vide. L’outil principal observé est copié et revérifié, son ID conservé même sans métadonnées. Qualité nullable si le bonus favorable est inconnu ; les autres ressources restent disponibles. Textes visibles décodés en SeString.TextValue avant publication.

CRAFT-CONDITION-RANGE-005 réutilise Apply pour une séquence fixe commencée Normale. Au plus quatre conditions × deux qualités extrêmes, transitions ordinaires forcées et monotonie ; autres ressources vérifiées identiques à la simulation normale. Résultat immuable et cache UI par résultat/scénario d’outil ; aucun worker ou historique natif ajouté. Une influence future sur l’admissibilité ou les ressources provoque un refus explicite au lieu d’un intervalle trompeur.

CACT-LIVE-002 copie neuf GameNumbers du LotteryDaily visible à 5 Hz sur framework, revérifie fenêtre/propriétaire/valeurs et publie un cadre managé immuable. Le service d’observation possède ses leases ; fermeture, erreur, désactivation et changement de personnage invalident les données. MiniCactpotSolveSession possède workers/CTS/générations et rejette succès/erreurs tardifs. L’UI synchronise uniquement un ticket récent jouable et relance le solveur commun lorsque valeurs ou propriétaire changent ; Draw ne lit aucun pointeur. Saisie manuelle et observation utilisent la même session.

GATHER-NATIVE-004 ajoute une observation de huit lignes Gathering, opt-in à 5 Hz sur framework. Nœuds bornés/type/visibilité/texte contrôlés et cadre managé immuable revérifié avant publication. Les identifiants GatheringUiLayout sont partagés avec le probe ULD local. Les taux affichés ne deviennent pas des bases du modèle PR : CopyResources transmet seulement propriétaire, métier/niveau, PR et tentatives cohérentes ; la copie UI invalide résultat et paramètres de base à renseigner. Aucun worker, hook ou commande natifs supplémentaires.

GP-ACTIVE-005 ajoute GpActiveEffects immuable, optionnel dans GpScenario pour conserver le scénario historique. Les valeurs courantes incluent les familles déclarées ; leur nouvelle sélection est exclue, Évangile modifie le supplément et le bonus limité déjà payé n’est ajouté qu’une fois. Les achats d’Abondance commencent après la première tentative déjà bonifiée. Le même moteur construit base/frontière/coût supplémentaire ; aucun service natif ou worker nouveau. Éditions d’effets invalident le résultat ; la copie de ressources remet les paramètres et déclarations manuels à renseigner.

USE-002 ajoute ItemUseCatalog immuable : index inverse de remises, ensembles GC/aquarium et provenance. ItemUseSheetCopy.Lumina est la projection synchrone partagée avec ItemUseProbe ; toutes les RowRef sont résolues/copiées sur framework. Le loader possède abonnement/CTS/worker, copie au plus 32 lignes avec budget doux 2 ms et ne construit les index qu’à partir de valeurs managées. Les tests purs excluent explicitement *.Lumina.cs. L’onglet Usages et les fiches d’outil réutilisent le même contexte et le catalogue de recettes existant ; aucun nouvel inventaire ou action de remise.

USE-HOVER-003 remplace le lecteur privé MarketHoverReader par un seul ItemHoverReader framework. ItemHoverService publie identité/qualité/propriétaire/dates sans dépendance au monde ou à la commercialisation ; normalisation des marqueurs publics centralisée. ItemHoverCoordinator applique séparément la politique du marché, avec RequestHoverAsync bornée au propriétaire/monde attendus. Un seul panneau DrawItemHover reçoit prix/usages selon leurs options ; les usages ne déclenchent pas de réseau. Exceptions, désactivation, disparition, owner et Dispose effacent le contexte ; aucune référence native différée.

`Plugin` reçoit IDalamudPluginInterface, ICommandManager et IPluginLog par injection constructeur Dalamud. Il charge Configuration v1, refuse une version inconnue sans la remplacer, crée MainWindow et possède WindowSystem ainsi que RegistrationScope.

RegistrationScope enregistre uniquement les acquisitions réussies, nettoie dans l'ordre inverse, poursuit après une erreur et rend Dispose idempotent. Un échec du constructeur déclenche le même nettoyage. La commande `/tensho`, Draw, OpenMainUi et OpenConfigUi ont tous une désinscription.

MainWindow affiche l'état du socle et une préférence OpenOnLoad. Sa modification appelle SavePluginConfig immédiatement ; un échec restaure la valeur affichée et inscrit l'erreur dans le journal. La visibilité de la fenêtre reste temporaire. INV-001 ajoute un diagnostic de l'inventaire consommant seulement les snapshots managés ; aucune lecture native dans Draw.

Configuration.cs et RegistrationScope.cs sont partagés par inclusion avec les tests. Configuration.Dalamud.cs ajoute uniquement IPluginConfiguration au plugin. Les tests vérifient le comportement managé ; ils ne simulent pas l'hôte Dalamud ni ImGui.

## Inventaire commun — CORE-001

InventorySnapshot décrit un lot d'observation : ObservedAt normalisé UTC, Source et dictionnaire figé de conteneurs. Chaque InventoryLocation contient des types distincts InventoryOwnerId et InventoryContainerId ; InventoryItem sépare InventoryItemId de IsHighQuality. L'owner doit être non nul, le container peut être zéro. La conversion des identifiants natifs reste la responsabilité de l'adaptateur futur.

InventoryContainerSnapshot copie les piles et additionne les quantités du même objet/qualité avec détection de dépassement. Il conserve InventoryCoverage Unknown/Partial/Complete pour ce conteneur uniquement. Une observation Unknown ne peut contenir de pile. Une liste vide n'est complète que si la couverture a été explicitement déclarée Complete par le producteur ; le modèle ne peut pas prouver cette déclaration.

GetQuantity impose une portée non vide et déduplique les conteneurs demandés. Le résultat contient ObservedQuantity (borne minimale) et ExactQuantity nullable. L'exactitude exige tous les conteneurs demandés complets ; un conteneur hors snapshot est Unknown. Des observations connues mélangées à des inconnues donnent Partial. Propriétaires et qualités ne sont jamais fusionnés implicitement.

InventoryService publie un seul snapshot immuable via échange atomique. Replace remplace le lot entier, sans historique ni fusion ; Clear invalide toute observation. L'ordre de publication fait autorité, pas l'horloge murale. Un consommateur conserve une référence stable au snapshot et à ses métadonnées pendant son calcul. Le producteur devra garantir l'ordre de collecte et vider le service en cas de changement de personnage/déconnexion.

## Acquisition Dalamud — INV-001

Plugin possède maintenant InventoryService et DalamudInventoryReader. Le reader s'abonne à IFramework.Update, IGameInventory.InventoryChanged, Login et Logout via RegistrationScope. Son Dispose désabonne puis ferme le coordinateur et vide le service. Un échec de construction déclenche le même nettoyage. Aucun hook, offset, worker ou timer n'est ajouté.

Sur un callback Update confirmé par IsInFrameworkUpdateThread, le reader vérifie IsLoggedIn, IPlayerState.IsLoaded et ContentId. Il lit Inventory1–Inventory4 (identifiants locaux 0–3), consomme immédiatement chaque ReadOnlySpan<GameInventoryItem> et copie BaseItemId/IsHq/Quantity dans des piles managées. Les spans et les slots ne quittent jamais cette lecture. La portée exclut équipement, cristaux, sacoche, servants et compagnie.

L'API publique locale ne fournit pas de preuve de chargement/complétude des conteneurs. Un span vide devient Unknown, un span non vide devient Partial même si tous les slots sont vides. Aucun Complete n'est produit. Cela permet de présenter une borne minimale sans transformer une absence observée en quantité zéro exacte. Un contrat plus fort nécessitera une preuve supplémentaire, pas un simple changement de label.

Mise à jour INV-002 / 0.1.1 : ce comportement Partial systématique décrit la première version 0.1.0. L'adaptateur ajoute maintenant une preuve distincte depuis FFXIVClientStructs installé (REF-INV-LOAD), sans modifier le contrat de IGameInventory. Sur le même framework thread, InventoryManager.Instance/GetInventoryContainer fournissent l'en-tête typé du sac. IsLoaded, Items non nul, type attendu et Size positif borné autorisent la lecture. Après copie, manager/conteneur/buffer et taille doivent être stables, le sac toujours chargé, et le nombre de slots copiés égal à Size. Ces conditions produisent Complete pour ce sac. Aucun offset numérique n'est ajouté ; les pointeurs restent locaux à Capture.

Un en-tête non lisible avant/après la lecture produit Unknown et aucune pile conservée. Une lecture de taille ou buffer incohérents reste Partial. InventoryReadEvidence exprime et teste les faits managés nécessaires sans conserver d'adresse. La taille maximale 1000 est un garde-fou, pas une hypothèse que tous les sacs ont une taille fixe. Une reprise toutes les cinq secondes s'applique maintenant aux sacs Partial comme Unknown, jusqu'à complétude.

InventoryCaptureCoordinator est managé et testé : un verrou sérialise collecte, publication, invalidation et Dispose ; un compteur de génération rejette une collecte invalidée pendant son exécution. L'identité est relue avant publication ; tout lot d'un autre propriétaire est rejeté. Toute invalidation vide immédiatement le service. Les rafales sont coalescées, avec intervalle minimal de 250 ms entre lectures ; les conteneurs Unknown ou erreurs déclenchent une reprise après cinq secondes. Si tous les sacs ont été observés, seuls de nouveaux événements/changements d'identité déclenchent une nouvelle lecture. L'identité seule est vérifiée chaque frame, sans scan des sacs.

Les exceptions de collecte vident le service et sont journalisées une fois par séquence d'échecs, avec réarmement après succès. L'horloge monotone Stopwatch régit les reprises ; ObservedAt UTC date les données. Aucun historique n'est accumulé.

MainWindow présente quatre états de sac, la date UTC et une requête par identifiant de base/NQ-HQ. Le total est explicitement affiché comme minimum observé, total inconnu. Les domaines Craftimizer/GatherBuddy/SortaKinda consommeront la même instance. L'intégration est compilée mais non encore validée dans un client de jeu.

Depuis 0.1.1, MainWindow affiche aussi l'état « complet » et « Total dans les quatre sacs : X NQ/HQ » uniquement lorsque les quatre conteneurs sont complets. Les captures utilisateur de 0.1.0 valident l'affichage et les quantités d'exemples ; la preuve native ajoutée par 0.1.1 attend son propre essai live.

## Ouverture automatique — 0.1.2

Deux préférences indépendantes : OpenOnLoad ouvre la fenêtre lors de la construction du plugin ; OpenOnLogin l'ouvre sur IClientState.Login. Le callback Login est possédé par RegistrationScope et désabonné au Dispose ; il relit la préférence courante à chaque connexion. Configuration reste en version 1 : les anciens fichiers gardent OpenOnLoad et reçoivent OpenOnLogin=false par défaut. Les deux cases sont sauvegardées ensemble après une modification, et leurs valeurs précédentes sont restaurées si la sauvegarde échoue.

Depuis 0.1.3, le diagnostic affiche ensemble les deux requêtes NQ et HQ du même snapshot, puis leur somme. La case de filtre HQ est supprimée. Le résultat est exact seulement si les quatre sacs sont complets ; il reste minimal ou inconnu selon la couverture sinon. La collecte native et les préférences ne changent pas.

## Catalogue d'objets commun — ITEM-001 / 0.1.4

Plugin possède une seule instance ItemCatalogService et son loader. Le loader extrait RowId, Name.ExtractText() et CanBeHq depuis Lumina Item dans la langue de la feuille. La copie s'effectue par lots au framework (128 lignes maximum, budget souple de deux ms entre deux lignes), puis seules des définitions et chaînes managées sont confiées au worker annulable. Une génération est construite par chargement du plugin, sans nouveau scan à chaque reconnexion. Les noms vides et RowId zéro sont exclus ; un ID dupliqué est une erreur explicite.

ItemCatalog est immuable : dictionnaire figé par ID et tableau privé trié de noms normalisés. Il conserve la source, la langue, les versions des repositories du jeu et le schéma Lumina, plus le numéro de génération. Pas de timestamp par ligne statique. Find est une lecture par ID ; Search normalise casse/accents/espaces, exige tous les mots et renvoie au plus 30 résultats par défaut (limite maximale 100, requête maximale 128 caractères). L'ordre est déterministe, nom normalisé puis ID. La recherche parcourt l'index en mémoire seulement lorsque la requête ou l'instance de catalogue change ; elle ne relit pas Excel.

ItemCatalogService publie atomiquement un état immuable (progression, catalogue ou erreur). Le worker ne publie jamais directement : Update ne consomme son résultat qu'une fois terminé et sous le verrou du loader. Dispose annule le worker, désabonne, vide les références source et le service ; aucune attente bloquante. Les erreurs du worker sont capturées dans son résultat, y compris si le plugin est déchargé avant publication. Les callbacks déjà engagés sont sérialisés avec Dispose.

MainWindow met en cache les résultats (30 maximum), affiche le chargement/erreur et permet une sélection qui renseigne l'identifiant déjà consommé par les totaux NQ/HQ des sacs. Recherche/catalogue restent utilisables même si l'inventaire est momentanément inconnu. Aucun autre service d'objet par domaine n'est ajouté.

## Interface commune — UI-003 / 0.2.0

MainWindow reste une seule fenêtre propriétaire d'un contexte de sélection (itemId et requête). Sa présentation est séparée en fichiers partial : MainWindow.cs (cadre/navigation/accueil), MainWindow.Items.cs (recherche et quantités), MainWindow.Settings.cs (réglages et diagnostics) et MainWindow.Tools.cs (répertoire produit).

Cinq destinations concrètes : Accueil présente l'outil disponible et l'état des données ; Objets & inventaire recherche et affiche NQ/HQ/total ; Tous les outils décrit et filtre le périmètre futur ; Réglages conserve les deux préférences persistantes indépendantes ; Diagnostics expose version, sources, génération et couverture des sacs. Les domaines futurs sont regroupés dans le répertoire, pas enregistrés comme faux modules ni présentés comme fonctionnels.

Le répertoire comporte les 17 références autoritaires, regroupées en Artisanat, Récolte & pêche, Objets & économie, Collections & progression, Exploration & butin, Jeux & Gold Saucer, Personnage & macros. Tous les outils métier sont encore « à venir » ; le catalogue et l'inventaire communs restent des fondations disponibles, pas un dix-huitième plugin prioritaire.

Le cadre réserve 200 pixels à la navigation ; le panneau principal utilise la largeur restante. Chaque panneau défile indépendamment. Taille par défaut 1040 × 700 à la première ouverture, minimum 760 × 480. Le style Dalamud existant est conservé, avec titres or et états disponibles verts. Les tables/children/listes ont des scopes équilibrés protégés par finally. Pas de nouvelle dépendance, service par page, abonnement ou lecture du jeu ; les pages consomment les mêmes ItemCatalogService et InventoryService.

### Répertoire par icônes — UI-004 / 0.2.1

Le répertoire remplace les sections détaillées développées par une grille adaptative de deux à cinq colonnes. PlannedTool conserve une référence d'icône FontAwesome native par outil ; IconButton utilise les polices existantes de Dalamud. La grille montre uniquement icône et nom. Description, domaine et état se trouvent dans une fiche modale commune, ouverte au clic seulement, avec fermeture explicite.

La sélection d'une fiche (selectedTool ou fondation disponible) est distincte de la sélection d'objet itemId ; consulter une fiche ne perd pas l'objet en contexte. La tuile disponible ouvre la page Items via sa fiche ; les 17 références futures restent À venir. La demande d'ouverture de popup est traitée après la table, au même niveau de pile ID que son rendu. Aucun fichier d'image ni texture propriétaire n'est requis.

## Banque manuelle des rangements — BANK-001 / 0.3.0

InventoryNativeReader factorise la lecture typée des conteneurs, sa copie managée et les preuves IsLoaded/type/taille/buffer déjà utilisées pour les sacs. Le lecteur live et les imports consomment cette même implémentation ; aucune logique native par domaine craft/récolte n'est dupliquée. InventoryContainerSnapshot expose son dictionnaire figé de quantités pour la persistance, sans exposer de collection mutable.

BankKey distingue CharacterId, BankStorageKind et StorageId. Le servant utilise son ID propre comme propriétaire des sept conteneurs et comme StorageId ; sacoche standard et extension utilisent le propriétaire personnage et StorageId zéro, distinguées par Kind. BankEntry réutilise InventorySnapshot, sa source et son ObservedAt UTC. BankEntryValidator exige exactement le groupe complet prévu, le bon propriétaire et des quantités compatibles avec les bornes natives (au plus 1000 slots × Int32 par conteneur).

InventoryBankService est la source commune des relevés historiques, distincte du snapshot live InventoryService. État immuable publié atomiquement, chargement/sauvegarde sur un worker avec Channel borné, une demande en attente maximum. Un import remplace seulement sa BankKey ; publication après commit disque. Chargement corrompu bloquant pour les écritures ; échec de sauvegarde transitoire rapporté avec ancien état conservé et retry possible. Dispose ferme la file, termine les tâches publiques et empêche les publications suivantes, sans attendre un appel IO synchrone engagé.

FileInventoryBankStore encode schéma 1, SavedAtUtc distinct d'ObservedAtUtc, IDs et quantités NQ/HQ par conteneur. Fichier limité à 16 MiB, groupes/éléments bornés, propriétés JSON obligatoires et doublons refusés. Save valide le fichier existant, écrit/flush un temporaire propre à l'opération puis remplace atomiquement avec .bak ; Load ne récupère .bak que si le primaire est absent. Un primaire corrompu est préservé et signalé. Un seul writer par chemin est prévu ; coordination multi-processus non implémentée.

BankInventoryBridge observe les fenêtres, positions et identités au framework toutes les 100 ms. Aucun scan d'objet hors demande explicite. Il publie seulement des BankMenuContext managés (clé, nom, position et disponibilité), puis revalide clé/personnage/menu/conteneurs avant et après la copie d'un import demandé. GetActiveRetainer seul est insuffisant car il réfère LastSelectedRetainerId ; l'inventaire visible et les contrôles de stabilité sont requis.

BankImportOverlay dessine les panneaux compagnons même quand la fenêtre principale est fermée. Ils suivent la fenêtre de l'inventaire servant ou chocobo et restent dans le viewport. Le bouton envoie une clé managée au bridge ; il ne lit pas de mémoire native dans Draw. Une extension chocobo non chargée n'est pas importée. Les callbacks appartiennent au plugin ; les panels n'injectent ni hook ni nœud natif.

MainWindow ajoute Banque d'objets et conserve la même recherche/sélection. Les résultats montrent les stocks importés du personnage courant, propriétaires, NQ/HQ et dates, même après fermeture des inventaires. Le cumul sacs + cache est nommé « recensé » avec avertissement de décalage après transferts, jamais « total exact possédé ». Les relevés des autres personnages sont conservés mais non additionnés. Équipements/cristaux/marché des servants restent hors des sept pages ; cette portée est affichée.

## Vue unifiée illustrée — UI-005 / 0.4.0

L'interface Objets & rangements fusionne les vues Objets & inventaire et Banque d'objets de 0.3.0 ; les services sous-jacents restent distincts par nature des données, pas par outil. Le menu latéral et la fiche de l'outil disponible pointent vers cette destination unique. L'import reste manuel et sa gestion se trouve dans une section repliable, sans modifier le fichier de banque ni sa portée.

ItemDefinition contient maintenant IconId, copié depuis Item.Icon au même passage que le nom. MainWindow reçoit ITextureProvider par injection et dessine les icônes du jeu dans les résultats, la fiche et le contexte latéral. Les wraps sont empruntés uniquement pour la frame UI : aucun stockage entre frames, aucun Dispose de texture partagée, aucun worker texture propre au plugin.

ItemHoldings est une projection pure et immuable des snapshots existants, filtrée par personnage et par type de rangement. Pour le personnage, seules les quatre clés 0–3 sont demandées ; un propriétaire étranger ou un conteneur absent reste inconnu. Les relevés servant/chocobo gardent leur date et qualité. Les rubriques non importées produisent des lignes Unknown ; l'extension de sacoche est optionnelle. Les quantités sont additionnées avec contrôle des dépassements ; les lignes retournées sont détachées des collections appelantes.

Les indicateurs HasObservation/HasUnknown/HasCached/IsExact commandent la présentation. Une somme avec cache n'est jamais un total exact au présent, même si toutes ses observations étaient complètes. Le filtre Personnage seul peut être exact. Les groupes et filtres réutilisent le même itemId, sans nouvelle lecture native ou banque par page.

Le résultat projeté est mis en cache par itemId, personnage, références du snapshot live et de BankState, et filtre. Il est recalculé seulement quand ce contexte change. La recherche conserve son cache existant et limite le rendu à 30 résultats avec icônes. À partir de 720 pixels disponibles dans le contenu, recherche (300 pixels) et fiche sont côte à côte ; sinon les blocs sont empilés. La liste de résultats ajuste sa hauteur au nombre d'objets, sans grand panneau vide pour quelques résultats.

## Catalogue de recettes — CRAFT-001A / 0.5.0

RecipeCatalog est un index managé par ResultItemId. Chaque RecipeDefinition conserve son RecipeId, son rendement, le nom du CraftType, le niveau de RecipeLevelTable et la liste d'IDs/quantités d'ingrédients ; deux recettes produisant le même objet restent deux variantes. RecipeCatalogLoader copie Recipe par lots de 128 sur le thread framework, puis construit l'index sur un worker. MainWindow affiche les variantes dans une section repliable de la fiche objet. Cette tranche ne déduit pas encore la possession des ingrédients et ne lance aucune action de craft.
