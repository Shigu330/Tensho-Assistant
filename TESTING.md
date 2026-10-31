# Vérifications

PNJ vendeurs 0.92–0.99 : liens InclusionShop, liens observés (Prêtresse à la robe noire, Officier de gestion du Maelstrom), monnaies codées par CostType, positions planevent/planner avec contrôle d'en-tête LGB. Verify.ps1 agent 509/509 à chaque version (checkpoints/0.9x.0-*). VALIDATED_IN_GAME: YES — rapport utilisateur 2026-10-31 « tous es ok ».

PNJ vendeurs 0.89 (liens FateShop) : Verify.ps1 agent 2026-10-30 20:33 — DLL 0.89.0.0, 509/509 ; test de lien FateShop ajouté. VALIDATED_IN_GAME: NO. Survol 0.87 (monstres wiki) et icône Gil : VALIDATED_IN_GAME (capture utilisateur).

PNJ vendeurs 0.88 : [NPC-VENDORS-014](docs/NPC-VENDORS-014.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 20:27 — DLL 0.88.0.0, 509/509, zéro avertissement/erreur. Quatre groupes NpcVendorTests (liens boutique, monnaies, coordonnées, ordre et cache disque) ; colonnes Lumina vérifiées par réflexion ; lecteur de feuilles et rendu non couverts. VALIDATED_IN_GAME: NO.

Wiki source principale des monstres 0.87 : [HOVER-TOOLTIP-013](docs/HOVER-TOOLTIP-013.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 20:16 — DLL 0.87.0.0, 505/505, zéro avertissement/erreur. MonsterHoverSourcesTests réécrits pour le wiki ; WikiMonsterTests : titre issu du jeu sans Garland, redirections, page absente, candidats. Survol 0.85/0.86 : accroche, stock et moins cher du centre de données VALIDATED_IN_GAME (capture utilisateur) ; 0.87 VALIDATED_IN_GAME: NO.

Monstres au survol 0.86 : [HOVER-TOOLTIP-013](docs/HOVER-TOOLTIP-013.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 20:07 — DLL 0.86.0.0, 504/504, zéro avertissement/erreur. Quatre groupes MonsterHoverSourcesTests (copie disque sans HTTP, attente de 1,5 s puis requête unique et écriture du cache, mode cache seul et attente après échec, copie expirée visible puis actualisée). VALIDATED_IN_GAME: NO.

Survol accroché à l’infobulle 0.85 : [HOVER-TOOLTIP-013](docs/HOVER-TOOLTIP-013.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 20:00 — DLL 0.85.0.0, 500/500, zéro avertissement/erreur. Quatre groupes MarketHoverDetailsTests ; CacheTests marché passé en clé v2 ; placement et rendu non couverts. VALIDATED_IN_GAME: NO.

Propriétés d’objet pour le tri 0.84 : [SORT-ITEM-PROPS-008](docs/SORT-ITEM-PROPS-008.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 19:47 — DLL 0.84.0.0, 496/496, zéro avertissement/erreur. Quatre groupes InventorySortPropsTests ; colonnes Lumina vérifiées localement par réflexion. VALIDATED_IN_GAME: NO.

Cache disque récolte/pêche 0.83 : [CATALOG-CACHE-012](docs/CATALOG-CACHE-012.md). COMPILED/TESTED : Verify.ps1 agent 19:43 — DLL 0.83.0.0, 492/492. Trois groupes GatheringCatalogCacheTests (horaires exacts et refus des fenêtres non normalisées, aller-retour DataCache réel, clé/validation). VALIDATED_IN_GAME: NO.

Cache disque du catalogue des recettes 0.82 : [CATALOG-CACHE-012](docs/CATALOG-CACHE-012.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 19:40 — DLL 0.82.0.0, 489/489, zéro avertissement/erreur. Trois groupes RecipeCatalogCacheTests (aller-retour DataCache réel avec ingrédients et paramètres de simulation, clé version du jeu/forme et refus des lignes corrompues, clé des objets inchangée). VALIDATED_IN_GAME: NO.

Cache disque du catalogue des objets 0.81 : [CATALOG-CACHE-012](docs/CATALOG-CACHE-012.md). COMPILED/TESTED : Verify.ps1 exécuté par l’agent sur le PC le 2026-10-30 19:34 — DLL 0.81.0.0, 486/486, langues et paquet verts, zéro avertissement/erreur. Trois groupes ItemCatalogCacheTests (aller-retour DataCache réel, clé version du jeu/langue/forme, refus des contenus corrompus) ; boucle du chargeur non exécutée par les tests. VALIDATED_IN_GAME: YES — rapport utilisateur 2026-10-30 (« tous est ok »), SORT-ZONE-SELECT-011 0.77–0.80 compris.

Sélection multiple des zones de tri 0.77 : [SORT-ZONE-SELECT-011](docs/SORT-ZONE-SELECT-011.md). COMPILED/TESTED : Verify.ps1 exécuté par l’agent sur le PC le 2026-10-30 — DLL 0.77.0.0, 481/481, langues et paquet verts, zéro avertissement/erreur. Quatre groupes InventorySortSelectionTests ; menu de clic et rendu non couverts. 0.78 (priorité des catégories) : 482/482 (+1 groupe : ordre de liste, ordre interne par critères, débordement, champ historique, remplissage inversé), DLL 0.78.0.0, agent 2026-10-30 19:20. 0.79 (cases restantes partagées) : 483/483, DLL 0.79.0.0, agent 2026-10-30 19:24. 0.80 (filtre de recherche des catégories, UI seule, non couvert par les tests) : 483/483, DLL 0.80.0.0, agent 19:29. VALIDATED_IN_GAME: PARTIAL — captures utilisateur 0.77/0.78 : catégories multiples et numérotées, rendu OK ; cases vides réservées signalées (corrigé par l'option 0.79) ; clic et persistance non encore observés.

Progression des sources de quête 0.76 : [QUEST-ITEM-PROGRESS-007](docs/QUEST-ITEM-PROGRESS-007.md). COMPILED/TESTED : Verify.ps1 exécuté par l’utilisateur le 2026-10-30 (assembly 0.75.0.0 avant passage à 0.76.0) — 477/477 tests, langues et paquet verts, zéro avertissement/erreur. Six groupes : jointure par ID complet, relevés hors ligne/non datés/en lecture/en erreur, ligne absente, filtre après compteurs et recherche, cycle du service (invalidation, changement de personnage, cache, clear), lecture seule. VALIDATED_IN_GAME: PARTIAL — Captures utilisateur 2026-10-30 18:43 (objet « Pièce d’argent allagoise ») : colonne Progression, compteurs 1 en cours · 173 terminées · 127 non confirmées (301/301), filtres Toutes et En cours, séquence affichée ; relevé issu du cache disque daté 29-10-26 21:40. Observation : la séquence native 255 était affichée brute (corrigée en « séquence finale (255) »). Verify.ps1 relancé par l’agent sur le PC le 2026-10-30 18:54 : DLL 0.76.0.0, 477/477, 0 avertissement/erreur. VALIDATED_IN_GAME: YES — rapport utilisateur 2026-10-30 : DLL 0.76.0.0 chargée, libellés « séquence finale (255) » / « 1 variante » et « Non confirmée » après déconnexion OK (non observé directement par l’agent).

Menu contextuel 0.53 : [USE-CONTEXT-004](docs/USE-CONTEXT-004.md). Verify.ps1 : 349/349 tests/package, compilation sans avertissement/erreur. Six groupes supplémentaires : routes/qualités et remise unique ; remplacement de menus ; revalidation au dispatch ; identité/limites ; logout/reconnexion/Dispose ; option v1 indépendante. ConfigSerializationProbe : trois modes Newtonsoft passants. Signatures IContextMenu et source primaire contrôlées ; callback et rendu natifs NOT VALIDATED_IN_GAME. Aucun essai projection/Macro Chain redemandé.

État courant 0.75.0 (2026-10-29) : [cache partagé et péremption réglable](docs/CACHE-001.md). Prix, quêtes, collections, sources communautaires, Revue de mode et calculs du carnet réutilisent des instantanés persistants datés. Durées indépendantes dans Réglages : 5 min, 1 h, 24 h ou 7 j ; actualisation forcée depuis les outils. Les imports restent manuels et signalent leur péremption ; les observations en direct gardent une invalidation courte. 466/466 tests/package/SHA-256 passants, zéro avertissement/erreur ; validation en jeu de ce cache encore à faire.

Index objet → quêtes 0.68 : huit groupes nouveaux, 434/434 tests/package verts, journal checkpoints/0.68.0-verify.log. Copies/variantes/IDs/couverture, bornes/annulation, worker réel, réponses tardives même/nouveau personnage, invalidate/clear/fail/dispose et erreurs isolées. Probe de production : 3 496 Items/8 288 associations, tous ensembles et variantes comparés à la projection directe ; Potion = 25 quêtes candidates, owner/progression hors ligne non confirmés. GatheringProbe recompilé. UI/callbacks en jeu non observés ; détails docs/QUEST-ITEM-SOURCES-004.md.

Récompenses quête 0.67 : cinq groupes nouveaux, 426/426 tests/package verts, journal checkpoints/0.67.0-verify.log. Propriété/readonly/owner, alternatives non sommées, quantités et références inconnues, clé de monnaie ambiguë et bornes. QuestRewardsProbe : 5 373 snapshots/9 313 lignes, tous Item/teintes résolus ; 37 tables métier/538 sous-lignes ; quatre quantités et quinze monnaies directes inconnues conservées. Types mémoquartz 1→28 et 3→49 prouvés, pas une jointure par numéro de ligne. GatheringProbe recompilé. Aucun rendu/choix/réception en jeu déclaré. Détails : docs/QUEST-REWARDS-003.md.

Titres/succès 0.66 : cinq groupes nouveaux, 421/421 tests/package verts, journal checkpoints/0.66.0-verify.log. Table de vérité chargement/positif, perte de couverture, owner/invalidation, contradiction atomique, récompense distincte de l’acquisition. CollectionProbe : 885 titres/530 variantes de genre, 4 003 succès/83 catégories, 192 récompenses Item/891 Title, identités/icônes et rejets de jointures étrangers vérifiés ; orchestrion toujours passant. API/UnlockDelegate source inspectées ; callbacks/chargement/acquisitions en jeu NOT VALIDATED_IN_GAME. Détails : docs/COLL-003.md.

Orchestrion 0.65 : quatre groupes supplémentaires, 416/416 tests/package verts, journal checkpoints/0.65.0-verify.log. CollectionProbe avec projections de production : 886 morceaux/12 catégories/886 liens Item d’identités distinctes, icônes présentes, UI/catégories étrangères et propriétaires hors ligne refusés ; modèles utilisables après disposal GameData. API IUnlockState et source primaire inspectées. Callbacks, acquisitions réelles et navigation UI non validés en jeu ; essais craft/Macro Chain non redemandés. Détails : docs/COLL-002.md.

Prévisions pêche 0.64 : sept groupes nouveaux, 412/412 tests/package verts, journal checkpoints/0.64.0-verify.log. Oracle indépendant BigInteger/horaire sur environ 5 000 dates, périodes précédente/courante, couverture partielle, logique ternaire, alternatives, minuit/ticks, bornes/copies/annulation. Probes réels avec fixture 2026-10-29 12:00 UTC : 8753, météo + précédente et horaire absent, un segment 13:13:20–13:36:40 ; 4915, horaire seul, quatre segments. Aucun rendu/prise/ciel observé. Détails : docs/GATHER-FISH-FORECAST-010.md.

Météos pêche 0.63 : quatre groupes nouveaux, 405/405 tests/package verts. WeatherProbe : 214 lignes, 43 noms globalement ambigus, copies anglaises absentes/étrangères refusées et prévisions antérieures passantes. GatheringProbe réel 8753 : spot 107/territoire 137, noms source Clear Skies/Rain/Showers reliés à Weather 1/7/8 et icônes françaises. Aucun runtime déclaré. Voir docs/GATHER-FISH-WEATHER-009.md.

Conditions pêche 0.62 : cinq groupes supplémentaires et 401/401 tests/package verts. Oracle indépendant sur 600 couples d’heures ; inconnus/vides, erreurs isolées, copies, UTC/minuit et météo précédente. Probes réels 4915 (10–18 ET), 8753 (Clear Skies après Rain/Showers), 7685 (during absent préservé). FashionProbe compilé ; aucun rendu en jeu validé. Détails : docs/GATHER-FISH-WINDOWS-008.md.

Carnet pêche 0.60 : [GATHER-FISH-006](docs/GATHER-FISH-006.md). Verify.ps1 : 389/389 tests/package/SHA-256, zéro avertissement/erreur ; quatre groupes identité/variantes, codes, copies/conflits et lifecycle. FishingNotebookProbe partage la projection : 2 461 fiches et notes concordantes, 296 tomes, aucun Item/spot non résolu ; 491 codes météo inconnus, notes absentes/étrangères rejetées. GatheringProbe antérieur exécuté sans option réseau et passant. Rendu/framework en jeu non observés ; essais craft/Macro Chain différés.

Modèles/livraison 0.59 : [SORT-TEMPLATES-007](docs/SORT-TEMPLATES-007.md). Verify.ps1 : 385/385 tests/package, zéro avertissement/erreur ; six groupes bibliothèque/rebinding/invalides/mutations/v1 et modèle → permutation d’une autre sacoche. SHA-256 des entrées DLL/manifeste du ZIP identiques aux fichiers canoniques, entrées uniques et dépendances hôte exclues. Newtonsoft None/Auto/Objects avec bibliothèque imbriquée passants. Aucune validation runtime ; aucun essai craft/Macro Chain redemandé.

Propriétés 0.58 : [SORT-FILTERS-006](docs/SORT-FILTERS-006.md). Verify.ps1 : 379/379 tests/package, zéro avertissement/erreur ; cinq groupes pour AND/inconnus/masques/identité/cache/JSON. Newtonsoft None/Auto/Objects et FashionProbe sans --fetch passants : 20 238 Wearable, 7 804 MainHand, 984 OffHand, 31 SoulCrystal, 51 vêtements bloquant Head ; OffHand négatifs exclus. API installée confirmée. Rendu/application/persistance en jeu non observés ; aucun essai craft/Macro Chain redemandé.

Protection/inversion 0.57 : [SORT-PROTECTED-005](docs/SORT-PROTECTED-005.md). Verify.ps1 : 374/374 tests/package, zéro avertissement/erreur ; cinq groupes dont 20 scènes multi-scopes vérifiant positions vides/occupées et bijection physique/quantités. Newtonsoft None/Auto/Objects avec zones protégées et inversées passants. Rendu/application/persistance natifs NOT VALIDATED_IN_GAME ; aucun essai craft/Macro Chain redemandé.

Profils/zones 0.56 : [SORT-RULES-004](docs/SORT-RULES-004.md). Verify.ps1 : 369/369 tests/package, zéro avertissement/erreur ; huit groupes nouveaux, 24 scènes multi-scopes avec oracle de coordonnées/quantités et trous réels, refus de positions forgées/chevauchement/place insuffisante. ConfigSerializationProbe : trois modes Newtonsoft avec profils imbriqués passants. Rendu/bridge mémoire/persistance en jeu non observés ; protocoles conservés, aucun test craft/Macro Chain redemandé.

Scopes 0.55 : [SORT-SCOPES-003](docs/SORT-SCOPES-003.md). Verify.ps1 : 361/361 tests/package, zéro avertissement/erreur ; six groupes nouveaux dont 144 scénarios (trois rangements × six remplissages × quatre critères × deux sens) avec oracle indépendant. Cache sans reçu direct, propriétaires/scopes, capture tardive, fraîcheur et lifecycle vérifiés. Fenêtres/bridge mémoire/rendu/persistance NOT VALIDATED_IN_GAME ; aucun test craft/Macro Chain redemandé.

Tri personnel 0.54 : [SORT-NATIVE-002](docs/SORT-NATIVE-002.md). Verify.ps1 : 355/355 tests/package, zéro avertissement/erreur. Six groupes nouveaux dont 48 scénarios avec oracle de coordonnées, refus des mutations et cycle de demande. SDK/source primaire inspectés ; bridge d’écriture natif, rendu et persistance VALIDATED_IN_GAME (rapport utilisateur 2026-10-30, DLL 0.76.0.0, protocole SORT-RUNTIME-VALIDATION de GOAL.md, « tout est OK »). Protocole dans le document, aucun essai craft/Macro Chain redemandé.

Survol 0.52 : [USE-HOVER-003](docs/USE-HOVER-003.md). Verify.ps1 : 343/343 tests/package verts, zéro avertissement/erreur. Neuf groupes nouveaux : identité commune, propriétaire/âge/stabilité, horloge/lifetime, usages seuls sans HTTP, activation et cache, réponse tardive annulée, portée propriétaire/monde, backoff/priorité manuelle et options v1. Transport HTTP simulé ; aucune validation de survol ou rendu réelle. ConfigSerializationProbe (Newtonsoft None/Auto/Objects) et GatheringProbe exécutés. Protocole utilisateur conservé dans le document ; pas de nouvel essai craft/Macro Chain demandé.

Usages 0.51 : [USE-002](docs/USE-002.md). Verify.ps1 : 334/334 tests/package verts, zéro avertissement/erreur. Sept groupes nouveaux pour index, variantes/lignes, copies, validité, tri/couverture, annulation et service. ItemUseProbe partage la projection Lumina de production : 1 240 mandats, 2 516 candidats GC, 263 poissons, 3 280 références Item et icônes de mandats résolus ; aucun lien de mandat perdu. Rendu et chargement via Dalamud non observés. Protocole dans le document ; aucun essai craft/Macro Chain redemandé.

PR0.50 : [GP-ACTIVE-005](docs/GP-ACTIVE-005.md). Sept groupes nouveaux, dont128 scénarios couvrant16 combinaisons de familles × 4 bonus limités × 2 métiers contre un oracle indépendant par tentative. Verify.ps1 :327/327 tests/package verts, zéro avertissement/erreur ; probes natifs vérifient les anciens scénarios et un état explicitement déclaré (base 23 →32 objets pour300 PR). Capture des effets natifs et rendu de cette UI non validés. Protocole conservé dans le document ; aucun test projection/Macro Chain redemandé.

Point de récolte 0.49 : [GATHER-NATIVE-004](docs/GATHER-NATIVE-004.md). Verify.ps1 : 320/320 tests/package verts, zéro avertissement/erreur ; GatheringUiProbe confirme les huit lignes, types de nœuds et limites dans Gathering.uld installé. Copie stricte, inconnus, ressources, âge, propriétaire et leases testés. Rendu/capture en jeu non validés. Protocole conservé : activer le panneau Point de récolte actuel dans GatherBuddy ou PR, comparer objets/taux/tentatives, sélectionner un objet puis copier les ressources ; vérifier que les bases restent à renseigner et la copie impossible en récolte rapide. Pas de nouvel essai projection/Macro Chain demandé.

## GATHER-001A — 0.6.0

Projection 0.46 et enveloppe 0.47 : voir [conditions/bonus](docs/CRAFT-CONDITIONS-004.md) et [séquence fixe](docs/CRAFT-CONDITION-RANGE-005.md). Verify.ps1 : 302/302 tests, zéro avertissement/erreur, package vert. ToolBonusProbe : 928 outils dont 136 bonus améliorés, 519 inconnus gardés inconnus. Les bornes de séquence sont comparées à une énumération exhaustive indépendante des transitions sur petits scénarios, y compris buffs, combos, plafonds et fin/casse. L’utilisateur a différé le test projection le 2026-10-29 ; Macro Chain aussi différé, ne pas redemander.

Mini Cactpot 0.48 : [CACT-LIVE-002](docs/CACT-LIVE-002.md). 311/311 tests et package verts ; copies/bornes/zéros, leases, propriétaire, fermeture/erreur, âge, worker réel et résultats/erreurs tardifs testés. Mode natif non observé en jeu. Protocole conservé : activer l’observation dans Mini Cactpot, ouvrir un ticket, comparer les chiffres après chaque révélation, vérifier choix de ligne après quatre chiffres et retrait du conseil à la fermeture. Aucun clic automatique. Aucun nouveau test projection/Macro Chain demandé.

Séquences 0.44.0 : voir [CRAFT-PRESETS-001](docs/CRAFT-PRESETS-001.md), 283/283 tests, roundtrip JSON managé, résolution exacte/bornes et rollback ajout/retrait. Persistance/rechargement réels dans Dalamud encore à vérifier ; aucun test natif déclaré réalisé.

Fenêtres 0.43.0 : voir [GATHER-WINDOWS-003](docs/GATHER-WINDOWS-003.md), 280/280 tests, oracle sur deux jours ET et probe du point30999 (09–12 ET → époque+1575/+2100 s). Horloge/frontières/minuit et inconnus testés ; comparaison runtime différée.

Météo 0.42.0 : voir [GATHER-WEATHER-001](docs/GATHER-WEATHER-001.md), 275/275 tests, oracle BigInteger/500 dates, frontières et inconnus, probe 1 290 territoires/72 météos/icônes. Empyrée 90/100 conserve l'inconnu. Présentation et comparaison avec le jeu différées.

Effets observés 0.41.0 : voir [CRAFT-LIVE-002](docs/CRAFT-LIVE-002.md), 270/270 tests, douze statuts natifs/icônes vérifiés ; Param, couverture, doublons, conflits et utilisation unique inconnue testés. Comparaison des compteurs/combos/étapes en synthèse d'essai encore requise ; aucun conseil natif déduit.

Observation 0.40.0 : voir [CRAFT-LIVE-001](docs/CRAFT-LIVE-001.md), 265/265 tests et métadonnées natives vérifiées. Activation temporaire explicite ; valeurs/effets partiels et aucun conseil. Capture en synthèse d'essai non réalisée ; protocole conservé sans redemander Macro Chain.

Actions 0.39.0 : voir [CRAFT-ACTIONS-002](docs/CRAFT-ACTIONS-002.md), 260/260 tests, 208 variantes natives vérifiées et huit groupes d'interactions avancées. Contrôle natif niveau 100 à attributs fictifs : proposition terminée mais sous la cible, correctement distinguée ; aucune garantie d'optimalité ou validation en jeu.

Solveur 0.38.0 : voir [CRAFT-SOLVE-001](docs/CRAFT-SOLVE-001.md), 252/252 tests, oracle indépendant sur 36 scénarios, témoin faisable multi-effets, annulation/invalidation et probe natif à attributs fictifs. Propositions à comparer en synthèse d'essai, aucune activation native validée.

Export 0.37.0 : voir [CRAFT-MACRO-001](docs/CRAFT-MACRO-001.md), 243/243 tests et probe des noms natifs pour huit métiers ; copie par bloc seulement, essai dans les macros du jeu différé.

Historique : les vérifications ci-dessous restent datées de leurs versions. Pour la simulation 0.36.0, voir [CRAFT-SIM-001](docs/CRAFT-SIM-001.md) : Verify.ps1 239/239, probe 136 actions/13 892 recettes et scénario Sirop de bouleau ; comparaison en jeu différée.

Sources statiques depuis GatheringItem, GatheringPointBase, GatheringPoint, GatheringItemPoint (objets cachés), TerritoryType/PlaceName/Map et FishingSpot. Accès depuis GatherBuddy, onglet Récolte, et bouton Lieux de chaque ingrédient préparé. Filtre par type, métier/niveau/zone/lieu, copie et ouverture du centre de la carte de zone. Ce repère ne représente pas les coordonnées exactes du nœud. Météo, horaires, prérequis et pêche sous-marine non couverts.

COMPILED/TESTED: Verify.ps1 vert, 66/66 tests et package 0.6.0.0 vérifié. Index managé, séparation des feuilles de pêche/récolte, zones distinctes, annulation, erreurs/clear testés.
LOCAL_DATA_VERIFIED: GatheringProbe sur le sqpack installé : 5 857 points ; 11 186 liens ordinaires, 2 420 liens pêche ; 4 199 sources dédupliquées après inclusion des liens directs. Marron sombre 12893 : Coupe niveau 55, Avant-pays dravanien, Terres fumantes, IsHidden=true. Sève de bouleau 12891 : Coupe, Écume des cieux d'Abalathia, Voor Sian Siran.
VALIDATED_IN_GAME: NON — lecture des fichiers et tests ne prouvent pas le rendu/clic en jeu.

## Sortie de compilation unifiée — 2026-10-27

Le csproj impose PlatformTarget x64 et OutputPath vers bin/x64/Configuration. Build direct du csproj et build de la solution vérifiés : tous deux produisent C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll. Verify.ps1 : 63/63 tests et package validés. Aucun changement fonctionnel ; version conservée 0.5.27. L'ancien dossier bin/Release (artefacts obsolètes) est archivé dans bin/legacy-Release-20261027, récupération possible ; aucune source ou banque supprimée.

## Confirmation préparation — 2026-10-27

Retour utilisateur « c'est ok relis les .md et continue le goal » après livraison 0.5.27 : préparation acceptée en jeu dans son périmètre courant. Pas de nouvelle capture ; ne pas étendre cette confirmation aux cas limites, erreurs ou changements de personnage. Catalogue, carnet et préparation clos ; GATHER-001A est le prochain objectif prêt. Aucune modification du code dans cette reprise documentaire.

## Préparation fabrication — 0.5.27

Outil intégré à Produit par et à la fiche sélectionnée du carnet Utilisé dans ; accès depuis Craftimizer. Quantité à produire 1..999999, nombre de crafts arrondi au rendement, quantité produite et surplus. Tableau des ingrédients : requis, stock recensé NQ+HQ, écart à compléter. Cristaux et imports sont inclus via ItemHoldings. Stocks inconnus restent Inconnu/À vérifier ; caches manuels signalés. La copie vers le presse-papiers conserve ces réserves. Aucun craft automatique ou simulation ajouté.

BUILD: Verify.ps1 Release 0.5.27.0, zéro avertissement/erreur.
TESTS: 63/63 ; deux nouveaux groupes couvrent arrondi au rendement, fusion des ingrédients répétés, NQ+HQ, pas de manque négatif, inconnus/caches et entrées invalides.
RUNTIME: nouveau panneau à vérifier en jeu.

CARNET_CONFIRMATION: Capture codex-clipboard-68433d3a-b304-4103-b5d5-5ab624267e21.png, v0.5.26 et retour « c'est ok ». Objet Marron sombre, six recettes, icône Cuisine, liste de résultats et recette sélectionnée Marrons glacés collectionnables avec cinq ingrédients et 51 crafts visibles.

## Carnet Utilisé dans — 0.5.26

Carnet affichant exclusivement les recettes ForIngredient de l'objet en contexte. Filtres métier via icônes de classe natives, correspondance CraftType 0..7 vers ClassJob 8..15 et icônes encadrées 62108..62115. Libellés métier issus du jeu, bouton Tous, recherche de recette, liste de résultats à gauche, fiche sélectionnée à droite. La fiche reprend ingrédients cliquables, NQ/HQ, rendement et nombre de crafts possibles. Les variantes sont sélectionnées par RecipeId.

BUILD: Verify.ps1 Release 0.5.26.0, zéro avertissement/erreur. TESTS: 61/61 et package validés ; identité métier conservée dans l'index inverse testée.
RUNTIME: contrôle du nouveau carnet requis en jeu.

BANQUE_CONFIRMATION: Capture utilisateur codex-clipboard-79f1aa8b-bc78-4fa2-810c-9e9ee37d91f0.png et retour « c'est ok » : filtre Appât de pêche sous Global, seul Personnage · Sac 3 visible. Ceci valide le masquage des sacs sans résultat ; aucune preuve supplémentaire de persistance des imports n'est inférée.

## Banque finalisation 0.5.25

Bouton Réinitialiser : efface catégorie, qualité et texte. Les piles NQ restent trouvables même si une pile HQ du même objet existe dans un autre sac. Une seule règle de filtrage est utilisée pour la présence de résultats et le rendu des cases. Les sacs filtrés sans résultat sont masqués, les autres restent à leurs positions internes. Les piles identiques ont des identifiants UI par case. Fonds des cases vides visibles. Les relevés importés affichent leur heure UTC ; indication de cache manuel et erreurs de chargement visibles.

BUILD: Release 0.5.25.0 compilée, zéro avertissement/erreur. TESTS: 61/61 et package vérifiés.
RUNTIME: validation finale à exécuter par l'utilisateur ; aucune confirmation visuelle 0.5.25 encore reçue.

## Banque — cadres filtrés 0.5.24

Un filtre de catégorie, qualité ou texte masque les sacs sans case correspondante.
Les cadres visibles se regroupent sur deux colonnes ; noms et positions internes restent conservés.
Sans filtre, les sacs vides restent affichés. Aucun résultat affiche un message explicite.
Verify.ps1 : compilation Release réussie, 61/61 tests et package validés. Rendu en jeu à confirmer.

## Banque — ordre visuel natif 0.5.23

Les essais précédents ligne/colonne ne validaient pas le tri de l'interface et sont abandonnés.
La capture lit désormais ItemOrderModuleSorter.Items : chaque position visuelle référence Page et Slot physiques.
La projection rassemble tous les conteneurs d'une origine puis découpe en pages visuelles de 35 cases
(4 personnage, 5 servant, 2 sacoche). Les filtres laissent des trous et ne compactent pas les cases.
Les imports sauvegardent les stacks séparés, leur slot physique et leur index visuel ; les quantités
agrégées restent contrôlées lors du chargement. Les anciens imports restent lisibles pour les quantités,
mais demandent une actualisation pour afficher leur ordre. Global inclut aussi le personnage.
Le personnage est relu au plus une fois par seconde pour détecter un changement de tri sans événement de quantité.

BUILD: Verify.ps1 Release 0.5.23.0, 0 avertissement/erreur.
TESTS: 61/61 ; tests nouveaux : permutation entre sacs avec trous/HQ, sauvegarde/rechargement de piles séparées et de leur ordre.
RUNTIME: NON VALIDÉ — recharger 0.5.23, comparer personnage trié, puis réimporter une sacoche et un servant.


Date : 2026-10-27 Europe/Brussels. Étape : CORE-000.

COMPILED: YES — solution Release, .NET SDK 10.0.403, Dalamud.NET.Sdk 15.0.0, assemblies Hooks/dev ; 0 avertissement, 0 erreur.

TESTED: YES — `scripts/Verify.ps1` terminé avec code 0. Cinq tests managés passent :

1. Paramètres par défaut et aller-retour JSON (Version et OpenOnLoad).
2. Nettoyage des acquisitions dans l'ordre inverse, Dispose idempotent et refus d'acquisition après Dispose.
3. Nettoyage des acquisitions déjà réussies après un échec d'initialisation, sans libérer une ressource non acquise.
4. Poursuite du nettoyage si une libération lève une exception.
5. Trois cycles d'abonnement/désabonnement sans callback conservé après déchargement.

Paquet : identité TenshoAssistant, API 15, version 0.1.0.0 ; ZIP contenant DLL et manifest ; absence des assemblies Dalamud/FFXIVClientStructs/Lumina/ImGui de l'hôte.

Les tests exécutent les fichiers managés de production Configuration.cs et RegistrationScope.cs. Ils n'instancient pas Plugin, WindowSystem, le sérialiseur de configuration Dalamud ni les bindings natifs ImGui. Les branches UI d'erreur de sauvegarde et la résolution des services nécessitent un essai dans l'hôte.

VALIDATED_IN_GAME: NO — aucun client FFXIV n'a été piloté et aucun chargement de plugin n'a été effectué.

## Smoke test en jeu restant

1. Ajouter `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll` aux plugins de développement Dalamud, puis charger.
2. `/tensho` ouvre la fenêtre ; fermer puis ouvrir depuis l'entrée principale et l'entrée de configuration Dalamud.
3. Cocher l'ouverture au chargement ; recharger : la fenêtre s'ouvre. Décocher ; recharger : elle reste fermée et `/tensho` l'ouvre.
4. Recharger/décharger plusieurs fois, vérifier l'absence de double callback, de commande orpheline ou d'erreur dans les logs.
5. Vérifier un conflit de commande : l'initialisation échoue proprement sans supprimer la commande de l'autre propriétaire.
6. Tester la sauvegarde/relecture via l'hôte et, dans un environnement de test, son échec : préférence restaurée et erreur visible/journalisée. Une version de configuration inconnue doit empêcher le chargement sans écrasement.

## Reproduire les tests automatiques

```powershell
Set-Location C:\Plugins-ff14
.\scripts\Verify.ps1
```

Ce projet utilise un petit exécutable de tests ; `dotnet test` seul ne l'exécute pas. Le script vérifie les codes de sortie et le paquet. Aucun benchmark ou test métier n'est revendiqué.

## CORE-001 — 2026-10-27

COMPILED: YES — Verify.ps1, Release, zéro avertissement et zéro erreur.
TESTED: YES — 13/13 tests managés (les cinq du socle plus huit groupes de tests inventaire), paquet vérifié.
VALIDATED_IN_GAME: NO — ce GOAL n'effectue aucune lecture du jeu.

Les tests exécutent directement InventorySnapshot.cs et InventoryService.cs de production. Ils vérifient : absence zéro uniquement dans une portée complète ; quantités partielles minimales ; séparation propriétaire/conteneur/NQ-HQ et agrégation ; portée mixte et déduplication ; copie des entrées et métadonnées UTC/source ; remplacement/clear sans rétention de l'ancien propriétaire ; rejets des entrées invalides/contradictoires ; dépassements arithmétiques détectés.

Le premier build des tests a révélé une ambiguïté C# entre new() cible et un argument params tableau. Les constructeurs des appels concernés ont été explicités ; le script complet a ensuite réussi. Aucune modification de l'API Dalamud nécessaire.

Ces tests établissent la sémantique managée, pas la complétude d'une future lecture native. Ils ne mesurent ni la contention multithread ni les performances sur un inventaire réel. L'adaptateur devra justifier toute déclaration Complete avec une preuve locale.

## INV-001 — 2026-10-27

COMPILED: YES — build Release via Verify.ps1, zéro avertissement/erreur.
TESTED: YES — 20/20 tests (13 existants plus sept groupes du coordinateur) ; paquet vérifié.
VALIDATED_IN_GAME: NO — un processus FFXIV et XIVLauncher ont été observés, mais aucun chargement ni interaction avec ce plugin n'a été effectué.

Les nouveaux tests exécutent InventoryCaptureCoordinator de production avec des fonctions de lecture managées : capture initiale, regroupement et délai de 250 ms, invalidation immédiate, déconnexion/changement de propriétaire, reprise Unknown après cinq secondes, erreurs avec limitation des logs et récupération, changement/invalidation pendant une collecte, rejet d'un lot pour un autre propriétaire, Dispose pendant une collecte et absence de travail après Dispose.

La compilation lie l'adaptateur aux véritables signatures installées de IGameInventory/IFramework/IPlayerState/IClientState. ApiProbe utilise MetadataLoadContext pour inspecter ces métadonnées sans exécuter le jeu. Les tests ne créent pas de GameInventoryItem natif et ne prouvent pas l'exactitude des observations live, la livraison des événements dans le client ou le comportement graphique.

### RUNTIME-001 : protocole d'acceptation restant

1. Charger la DLL Release comme plugin de développement et ouvrir `/tensho` ; vérifier les logs de chargement.
2. Personnage connecté : vérifier quatre sacs diagnostiqués, date UTC présente, états Partial ou Unknown uniquement. Aucune affirmation Complete/total exact ne doit apparaître.
3. Saisir l'identifiant de base d'un objet présent dans les sacs, comparer la quantité observée avec les piles visibles ; basculer HQ et vérifier la séparation. Un objet absent garde un total inconnu.
4. Déplacer ou scinder une pile dans les sacs ; vérifier une nouvelle date d'observation et le nouveau minimum. Les sacoches/servants restent exclus.
5. Vérifier que les sacs indisponibles restent Unknown puis sont relus après chargement, sans boucle d'erreurs dans les logs.
6. Se déconnecter : diagnostic inconnu, aucune quantité de l'ancien personnage. Reconnecter un autre personnage : ses observations remplacent intégralement les précédentes.
7. Recharger/décharger trois fois ; vérifier l'absence de callbacks et de commande orphelins. Tester aussi la préférence d'ouverture au chargement du socle.

Consigner les résultats réels et les logs d'erreur avant de marquer VALIDATED_IN_GAME. Les étapes qui modifient des piles et changent de personnage sont à effectuer au moment choisi par l'utilisateur.

### RUNTIME-001 : première preuve reçue

Source : capture utilisateur codex-clipboard-4f4c69b5-0d04-4b7f-822f-9dab6081658e.png, reçue le 2026-10-27. Analyse de l'image fournie, aucune interaction avec le client par l'agent.

VALIDATED_IN_GAME: PARTIAL — fenêtre Tensho--Assistant affichée dans FFXIV ; diagnostic présent, date d'observation affichée 2026-10-27 16:46:03 UTC ; sacs 1–4 tous « partiel » et avertissement de couverture visible. Cela atteste l'affichage et une première observation, sans prouver l'exactitude du contenu.

Champ identifiant à 0, HQ décoché et préférence d'ouverture décochée. Aucune requête de quantité n'est visible. La capture ne prouve ni l'emploi de /tensho pour ouvrir, ni la propreté des logs, ni une actualisation, ni le comportement Unknown, ni la persistance, ni les rechargements ou changements de personnage. Ces vérifications restent ouvertes.

Prochain test utile : saisir l'identifiant de base d'un objet connu dans les sacs, comparer NQ/HQ, déplacer une pile puis vérifier la nouvelle observation et la quantité minimale affichée.

### RUNTIME-001 : comparaison Potion 4551 reçue

Source : trois captures utilisateur reçues le 2026-10-27 : codex-clipboard-354c09fa-2577-4020-9612-c5ec4ce0e301.png (requête NQ), codex-clipboard-090999d5-1dab-478e-a563-d297d51cd574.png (requête HQ), codex-clipboard-6546417c-066f-43c8-83c8-8c39027687df.png (icône de pile).

Résultat visuel : ID 4551, HQ décoché → quantité observée 5 ; HQ coché → quantité observée 0 ; la pile montrée porte le compteur 5. Les deux requêtes conservent « minimum ; total inconnu ». Concordance visuelle sur cet objet et distinction des deux résultats NQ/HQ observées.

Limites : les captures ne recensent pas tous les sacs et ne démontrent pas une absence globale de HQ. Aucun exemple de quantité HQ positive n'a été testé. Elles ne montrent ni déplacement de pile ni nouvelle date, ni déconnexion/rechargement/persistance. VALIDATED_IN_GAME reste PARTIAL.

Prochain test : déplacer la pile normale dans un autre emplacement des quatre sacs. La date d'observation doit se renouveler et le minimum agrégé doit rester 5 si aucune potion n'est ajoutée/retirée. Puis poursuivre le protocole lifecycle au moment choisi par l'utilisateur.

### RUNTIME-001 : quantité HQ positive confirmée

Source : capture utilisateur codex-clipboard-0b675a1c-196f-487c-a3d2-fff41ba28eac.png, reçue le 2026-10-27, et message « ça fonctionne mais total inconnu alors que 0 nq 156 hq ».

Résultat visible : ID 12892 et HQ coché, quantité observée 156 ; l'utilisateur confirme 156 HQ et 0 NQ. Le résultat NQ est rapporté par l'utilisateur, sans capture NQ jointe. La quantité HQ positive concorde donc avec son inventaire déclaré.

Observation de présentation : « minimum ; total inconnu » reste affiché. Cause vérifiée dans le code : DalamudInventoryReader produit uniquement Partial/Unknown, sans vérifier le chargement natif des conteneurs ; MainWindow reprend ce statut. Ce libellé ne signifie pas que le compteur ne sait pas lire 156. Pour annoncer un total exact des quatre sacs, il faut vérifier leur complétude réelle ; les autres rangements restent une portée distincte.

VALIDATED_IN_GAME reste PARTIAL : quantités de deux objets concordantes, mais aucun nouveau résultat explicite sur l'horodatage après déplacement, la persistance ou le lifecycle.

## INV-002 — version 0.1.1, 2026-10-27

Consigne utilisateur appliquée : compilation et tests avant tout nouvel essai demandé.

COMPILED: YES — Verify.ps1 terminé avec code 0, Release, zéro avertissement/erreur.
TESTED: YES — 25/25 tests, paquet vérifié ; concordance version du manifest source, du manifest produit et des métadonnées DLL (0.1.1.0).
VALIDATED_IN_GAME: NO pour les nouvelles vérifications de complétude. Les captures antérieures restent des preuves de 0.1.0, pas de la version corrigée.

Cinq nouveaux tests managés : en-têtes chargés/stables et nombre complet de slots ; en-têtes manquants/non chargés/type incorrect/tailles invalides ; span tronqué ou vide et buffer/taille instables ; quatre sacs complets avec exactement 0 NQ/156 HQ et exclusion d'un rangement inconnu ; reprise d'un sac Partial jusqu'à Complete puis arrêt des scans sans événement. Les tests coordinateur précédents utilisent désormais des lots Complete pour le cas stabilisé, et conservent un scénario Unknown avec reprise.

La compilation vérifie l'usage des véritables membres FFXIVClientStructs installés ; les tests managés vérifient la décision de couverture. Ils n'exécutent pas les appels natifs à InventoryManager.

Essai à demander après ce build réussi : recharger la DLL 0.1.1, ouvrir /tensho, vérifier les quatre sacs « complet », saisir 12892 puis vérifier 156 HQ et 0 NQ si l'inventaire n'a pas changé. L'affichage doit annoncer « Total dans les quatre sacs » et exclure les autres rangements. Si un sac reste inconnu/partiel, ne pas forcer Complete : rapporter son état et les éventuelles erreurs Dalamud.

### INV-002 : résultat live reçu

Source : capture utilisateur codex-clipboard-82c08f54-cd4e-45ea-8616-ae48cd6cf3aa.png reçue le 2026-10-27, avec le message « et total bon », en réponse à l'essai de la DLL 0.1.1.

VALIDATED_IN_GAME: YES pour l'affichage des quatre sacs complets (capture) et le total correct (confirmation utilisateur). La capture montre seulement les états des sacs, pas le nombre ni le filtre de qualité ; les valeurs du total ne sont donc pas relues directement sur cette image. Le numéro de version n'est pas visible : attribution à 0.1.1 par le contexte de l'essai et le nouvel état « complet » absent de 0.1.0.

La correction de complétude/total est validée dans ce périmètre. RUNTIME-001 reste partiel : persistance, rechargements répétés, déconnexion/changement de personnage et actualisation horodatée restent à confirmer séparément. Aucune modification du code ni nouvelle compilation nécessaire pour consigner cette preuve.

## Retour sur actualisation, persistance et reconnexion

L'utilisateur a répondu « tous est OK sauf que une fois deco reco je doit l'ouvrir moi meme » au protocole en trois étapes : déplacement avec renouvellement de date/total stable, persistance après désactivation/réactivation, puis déconnexion/reconnexion. Actualisation et persistance sont donc confirmées par déclaration utilisateur ; réouverture automatique à la reconnexion signalée manquante. Le code 0.1.1 ne prévoyait l'ouverture qu'au chargement du plugin.

## UI-001 — version 0.1.2

Demande complémentaire explicite : séparer ouverture au chargement et ouverture à la connexion. Implémentation finale : deux booléens persistants indépendants, deux cases distinctes, abonnement Login libéré avec le plugin. L'option de connexion est désactivée par défaut pour les anciens fichiers de configuration.

COMPILED: YES — Verify.ps1 réexécuté après la séparation, Release 0.1.2.0, zéro avertissement/erreur.
TESTED: YES — 26/26 tests ; les quatre combinaisons des préférences sont vérifiées par aller-retour JSON et un test vérifie la compatibilité d'un ancien fichier ne contenant pas OpenOnLogin. Paquet et versions source/manifest/DLL concordants.

VALIDATED_IN_GAME: Confirmation utilisateur « Tous est OK » reçue après l'implémentation et le build des deux options. La déclaration est consignée comme acceptation du comportement demandé ; aucune capture de version, trace d'événement ou log supplémentaire n'a été fourni. Les tests automatisés ne déclenchent pas l'événement natif Login.

Le protocole utilisateur courant est accepté. Les cas avancés non explicitement exercés (échec de sauvegarde forcé, conflit volontaire de commande, rechargements répétés avec instrumentation, conteneur non chargé provoqué, second personnage) restent des vérifications complémentaires avant publication ; aucune réussite de ces cas n'est revendiquée.

## UI-002 — version 0.1.3 : NQ, HQ et total simultanés

Demande utilisateur : afficher un total HQ/NQ. MainWindow présente désormais NQ, HQ et NQ + HQ ensemble pour l'objet recherché ; le filtre HQ est remplacé par cet affichage simultané. Les deux requêtes utilisent la même instance immuable de snapshot et la même portée. Les lectures incomplètes restent explicitement des minimums, et Unknown n'est pas présenté comme zéro.

COMPILED: YES — Verify.ps1 terminé avec code 0 après modification, Release 0.1.3.0, zéro avertissement/erreur.
TESTED: YES — 26/26 tests existants, paquet et concordance des versions vérifiés. Les tests existants couvrent la séparation NQ/HQ et la complétude ; ils ne rendent pas la nouvelle présentation ImGui.
VALIDATED_IN_GAME: NO pour le nouvel affichage simultané. Essai ciblé après rechargement : ID 12892, vérifier NQ 0, HQ 156, total 156 si inventaire inchangé ; aucun filtre HQ à manipuler.

## ITEM-001 — version 0.1.4

Suite au retour « parfait continue », ajout de la recherche par nom et ID. Ce retour est une acceptation de la présentation précédente ; aucune nouvelle capture du rendu 0.1.3 n'est disponible.

COMPILED: YES — Verify.ps1 exécuté après intégration : Release 0.1.4.0, zéro avertissement/erreur.
TESTED: YES — 31/31 tests managés et paquet/version vérifiés. Cinq groupes nouveaux couvrent casse/accents/mots dans un ordre différent, ID exact/inconnu, résultats bornés/déterministes, copie/source/version/génération et annulation/états publication-erreur. Les tests exécutent ItemCatalog et ItemCatalogService de production ; les données des exemples sont des fixtures, pas une extraction du jeu.
VALIDATED_IN_GAME: NO pour le chargement du catalogue Lumina et l'interface de recherche.

Les signatures locales Item.Name/RowId/CanBeHq, ExcelSheet.Count/GetRowAt/Language et Repository.Version sont vérifiées par métadonnées/XML et compilation. Le premier sondage de métadonnées a utilisé un namespace Repository incorrect ; le type Lumina.Data.Repository a ensuite été inspecté avec succès. Cela n'a pas affecté le build du plugin.

Le loader copie au maximum 128 lignes par Update et s'arrête entre deux lignes après un budget souple de deux millisecondes. Une lecture individuelle de page Excel peut dépasser ce budget ; aucune mesure de latence réelle n'est revendiquée. L'indexation ne reçoit que des ItemDefinition managés et métadonnées copiées, avec annulation au Dispose. Publication exclusivement par Update après achèvement vérifié du worker. Désabonnement à la fin/erreur ; aucun nouvel abonnement au Login pour ces données statiques.

Essai ITEM-001-RUNTIME après le build réussi : recharger 0.1.4, ouvrir /tensho et attendre la fin du chargement du catalogue. Chercher « sirop bouleau », sélectionner « Sirop de bouleau — 12892 » et vérifier nom, identifiant, NQ/HQ/total. Chercher ensuite « potion », et tester 12892 directement dans la recherche. Les autres essais déjà acceptés ne sont pas à répéter. Si une erreur ou un chargement durable survient, conserver le message affiché et les logs Dalamud.

### ITEM-001-RUNTIME : résultat reçu

Source : capture utilisateur codex-clipboard-e454bd2e-7f41-4788-87bf-0ced362301f8.png reçue le 2026-10-27, accompagnée de « tous est OK ».

VALIDATED_IN_GAME: YES dans le périmètre visible : catalogue chargé de 50 880 objets, langue French ; recherche « sirop » donnant plusieurs résultats nommés ; « Sirop de bouleau — 12892 » sélectionné ; champ ID 12892 et nom cohérents ; quatre sacs complets ; NQ 0, HQ 156, total NQ + HQ 156. Horodatage de l'inventaire affiché : 2026-10-27 17:12:56 UTC. Les deux options d'ouverture sont distinctes et visibles.

L'utilisateur confirme globalement l'essai. La capture n'affiche pas une recherche par ID, le test « potion », les performances de construction ni les scénarios d'erreur/annulation ; ces comportements ne sont pas revendiqués comme directement observés par l'agent. Le protocole demandé est accepté sur la base de la capture et du retour utilisateur. Aucun changement de code pour cette consignation, donc aucun nouveau build requis.

## UI-003 — version 0.2.0 : espace de travail commun

Demande utilisateur : améliorer l'interface en tenant compte des différents outils. Fenêtre organisée en cinq pages réelles : Accueil, Objets & inventaire, Tous les outils, Réglages, Diagnostics. Navigation latérale permanente, objet sélectionné conservé entre les pages et quantités en trois colonnes. Les détails techniques des sources sont dans Diagnostics ; les préférences indépendantes dans Réglages. Un seul répertoire regroupe les 17 références futures dans sept domaines ; il ne simule pas des modules métier utilisables.

COMPILED: YES — Verify.ps1, Release 0.2.0.0, zéro avertissement/erreur.
TESTED: YES — 31/31 tests managés existants et paquet/version vérifiés. Vérification statique supplémentaire : les 17 noms de référence du répertoire UI correspondent exactement aux 17 lignes autoritaires de ROADMAP.md. Les services de données et le lifecycle ne sont pas modifiés.
VALIDATED_IN_GAME: NO pour cette nouvelle disposition. Aucun rendu ou test d'interaction native n'a été exécuté par l'agent. Les tests managés ne rendent pas les pages ImGui.

Le premier build a identifié l'absence de ImGuiChildFlags dans le binding installé. L'appel BeginChild avec le paramètre bool de bordure a compilé et la vérification complète a ensuite réussi. Les scopes Begin/End des tables, enfants et listes sont protégés par finally.

Taille de première utilisation : 1040 × 700 ; taille minimale 760 × 480. Les dimensions déjà enregistrées par ImGui restent respectées. Les deux panneaux défilent séparément ; recherche et sélection ne sont pas réinitialisées par le changement de page. Aucun nouvel event, worker, timer ou accès Excel/natif n'est ajouté par cette refonte.

Essai UI-003-RUNTIME après le build réussi : recharger 0.2.0, ouvrir /tensho, vérifier l'Accueil et son bouton d'accès aux objets. Dans Objets & inventaire, chercher puis sélectionner le sirop de bouleau et vérifier NQ/HQ/total. Aller dans Réglages puis revenir : l'objet doit rester sélectionné. Vérifier le répertoire Tous les outils et son filtre, puis Diagnostics. Redimensionner la fenêtre et vérifier l'accès aux pages/quantités. Ne pas recommencer les essais de données déjà acceptés au-delà de ce contrôle de présentation.

## UI-004 — version 0.2.1 : icônes et détails au clic

Demande utilisateur : dans Tous les outils, afficher des icônes et les détails seulement après un clic. La page présente désormais une grille de deux à cinq colonnes selon la largeur. Chaque tuile montre une icône FontAwesome fournie par Dalamud et le nom de référence de l'outil ; les descriptions, domaines et états individuels ne sont affichés que dans une fiche modale après clic. Aucun détail sur simple survol.

Les 17 références prévues sont conservées, chacune avec une icône distincte. Une tuile séparée Obj​​ets & inventaire représente la fondation disponible ; sa fiche dispose d'un bouton Ouvrir l'outil. Les fiches des 17 modules futurs indiquent À venir et ne simulent pas leur disponibilité. Le filtre reste applicable aux noms, références et domaines.

COMPILED: YES — Verify.ps1 réexécuté après la dernière correction, Release 0.2.1.0, zéro avertissement/erreur.
TESTED: YES — 31/31 tests existants, paquet et versions vérifiés ; comparaison statique des 17 références UI avec ROADMAP.md réussie. Les services de données sont inchangés.
VALIDATED_IN_GAME: NO pour la grille et les fiches. Les tests ne rendent pas ImGui et ne déclenchent pas de clic natif.

Vérifications de code : IconButton(string, FontAwesomeIcon, Vector2) et les 18 glyphes utilisés existent dans l'installation locale. L'ouverture de popup est différée après EndTable pour conserver le même niveau de pile d'identifiants que BeginPopupModal. Les scopes EndTable/EndPopup sont protégés par finally ; aucune police, texture, tâche ou ressource externe supplémentaire créée.

Essai ciblé après build : recharger 0.2.1, ouvrir /tensho puis Tous les outils. Vérifier la grille sans descriptions, cliquer une icône future pour afficher sa fiche, fermer et essayer une autre icône. Cliquer Obj​​ets & inventaire puis Ouvrir l'outil pour rejoindre la recherche. Vérifier le filtre et le redimensionnement. Les autres données déjà validées ne sont pas à retester.

## BANK-001 — version 0.3.0

Demande utilisateur : inventaires servant/chocobo, commandes attachées à leurs fenêtres pour import manuel dans une banque persistante, consultation dans les recherches.

COMPILED: YES — Verify.ps1 exécuté après toutes les modifications de code, Release 0.3.0.0, zéro avertissement/erreur.
TESTED: YES — 43/43 tests managés, paquet et concordance versions vérifiés. Douze nouveaux groupes de tests banque couvrent les groupes de conteneurs, séparation servant/personnage/NQ-HQ, remplacement sans double compte, import vide complet, rejets partiels/mauvais propriétaire/conteneurs inattendus, métadonnées UTC et JSON, fichiers corrompus/version inconnue/doublons/quantités impossibles, sauvegarde atomique et secours .bak, erreurs IO avec rollback et retry, chargement/import en cours et absence de publication après Dispose.
VALIDATED_IN_GAME: NO pour les imports servant/chocobo, les noms d'addons retenus et l'ancrage des panneaux. Les tests ne déclenchent pas les véritables menus ni les lectures natives de ces rangements.

La banque et le store ont été implémentés et testés par un worker de développement ; le parent a inspecté les trois fichiers de production et les tests, intégré l'adaptateur/UI, ajouté les bornes de quantités et exécuté la suite complète avec la toolchain .NET 10 locale. Les résultats ciblés du worker ne remplacent pas cette validation complète.

### Portée et sémantique

- Servant : sept pages 10000–10006, clé personnage + identifiant servant ; hors équipements, cristaux, objets en vente et gils.
- Chocobo : sacoche standard 4000/4001 ; extension 4100/4101 importée séparément seulement si disponible et chargée.
- Import explicite uniquement ; il remplace le dernier relevé du même rangement après sauvegarde réussie. Un groupe incomplet ou changeant n'écrase pas l'ancien relevé.
- Les sacs du joueur restent en direct ; les autres stocks sont datés. Le cumul « recensé » mêle stocks actuels et caches et peut être périmé après un transfert. Aucun total global exact de tous les rangements n'est revendiqué.
- JSON dans le dossier de configuration du plugin, InventoryBank.json, schéma 1 ; remplacement atomique et sauvegarde .bak du précédent fichier. Un fichier présent corrompu n'est jamais remplacé silencieusement ; imports bloqués pour le protéger.

### Lifecycle et limites des preuves

Lecture des contextes/positions toutes les 100 ms sur le framework, sans scan des objets tant qu'aucun import n'est demandé. RetainerManager.GetActiveRetainer désigne le dernier servant sélectionné : le code exige aussi inventaire visible, identité stable 500 ms, conteneurs chargés et contrôle avant/après copie. Cette combinaison reste à confirmer en jeu, notamment lors du passage d'un servant à l'autre.

Les panneaux d'import sont des fenêtres ImGui compagnons ancrées à la position gérée du menu, pas une modification de ses nœuds natifs. Draw ne conserve ni lit aucun pointeur natif. Une fermeture entre clic et collecte est revalidée au framework. Les noms InventoryRetainer/InventoryRetainerLarge/InventoryBuddy correspondent aux types locaux inspectés ; InventoryBuddy2 est un candidat de repli, sans observation runtime à ce stade.

Chargement et sauvegarde sont sérialisés sur un worker à file bornée, hors framework. Dispose ferme les futures demandes, termine les tâches publiques sans bloquer et empêche toute publication tardive. Une opération IO synchrone déjà entrée peut finir ; Completion représente la durée de vie publique, pas un arrêt forcé de l'IO. L'essai de rechargement doit attendre le message de sauvegarde avant de désactiver le plugin. Pas de test de crash machine, écriture multi-processus ou annulation forcée d'un appel système revendiqué.

### BANK-001-RUNTIME : essai demandé après build

1. Recharger la DLL 0.3.0. Ouvrir l'inventaire d'un servant : un panneau « TENSHO · Banque d'objets » doit suivre la fenêtre. Attendre son chargement et cliquer Importer / actualiser. Attendre « Import sauvegardé dans la banque ».
2. Fermer puis ouvrir un autre servant et importer. Vérifier que les noms/quantités restent distincts et qu'une nouvelle actualisation remplace le relevé sans doubler les nombres.
3. Ouvrir la sacoche chocobo, importer ; si l'extension est accessible/chargée, l'importer avec son bouton séparé. Ne pas conclure zéro pour une extension indisponible.
4. Fermer les inventaires, ouvrir /tensho → Banque d'objets ou Objets & inventaire, rechercher un objet connu des stocks importés ; vérifier emplacement, NQ/HQ et date. Vérifier que le cache reste consultable sans inventaire servant/chocobo ouvert.
5. Après confirmation de sauvegarde, désactiver/réactiver le plugin : les relevés doivent rester présents. Vérifier, au moment choisi, qu'un changement de personnage n'additionne pas les banques des deux personnages.

Si aucun bouton n'apparaît, vérifier dans Diagnostics les noms de fenêtres détectées et fournir la capture de l'inventaire concerné. Un résultat manquant n'autorise pas à forcer un import ni à retirer les vérifications de chargement.

### BANK-001 : preuve partielle reçue avec la demande de refonte

Source : capture utilisateur codex-clipboard-55b7452f-db39-4732-aca5-13ee28923f23.png, datée du 2026-10-27, fenêtre indiquant v0.3.0. Recherche « cordial », objet Breuvage cordial ID 6141, sacs complets : 182 NQ, 0 HQ, total 182, observation 17:58:29 UTC. Ligne cache Sacoche chocobo : 0 NQ, 26 HQ, total 26, dernier import 2026-10-27 17:58:37 UTC. Cumul recensé affiché : 208.

VALIDATED_IN_GAME: PARTIAL — présence et consultation d'un relevé chocobo et calcul du cumul visibles. La capture ne montre pas le bouton attaché, le processus de collecte, un servant, la réimportation ou un rechargement ; ces points ne sont pas revendiqués comme validés.

## UI-006 — version 0.4.1 : correction des tuiles d'outils

Capture utilisateur codex-clipboard-38906016-8368-4642-9c88-e8522f6f5170.png : boutons très larges et glyphes coupés à gauche dans Tous les outils ; icône du Breuvage cordial visible dans le contexte latéral. Cette preuve ne valide pas encore la page groupée des rangements.

Correction : dimensions positives des boutons (72 × 64 au maximum), dessin du glyphe à 28 pixels centré dans le rectangle réel, légende centrée et grille de 2 à 10 colonnes selon la largeur. Police empruntée à Dalamud uniquement pendant Draw ; fiches toujours ouvertes au clic.

COMPILED: YES — Verify.ps1 Release 0.4.1.0, zéro avertissement/erreur.
TESTED: YES — 55/55 tests, identité/version/contenu du paquet vérifiés, code de sortie 0.
VALIDATED_IN_GAME: YES — retour utilisateur après livraison 0.4.1 : « tous est OK continue ». Correction de l'interface acceptée ; aucune preuve spécifique supplémentaire concernant les imports natifs ou leur persistance n'est déduite de cette confirmation.

## CRAFT-001A — version 0.5.0 : recettes et ingrédients

COMPILED: YES — Verify.ps1 Release 0.5.0.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés. Les tests recette couvrent les variantes d'un résultat, les IDs d'ingrédients, le rendement et les lignes invalides.
LOCAL_VERIFIED: Recipe expose ItemResult, Ingredient, AmountResult, AmountIngredient, CraftType et RecipeLevelTable dans les assemblies locales ; CraftType expose Name et RecipeLevelTable expose ClassJobLevel.
VALIDATED_IN_GAME: NO. Essai demandé après compilation : recharger la DLL 0.5.0, sélectionner le sirop de bouleau, ouvrir « Recettes qui produisent cet objet », puis comparer les lignes affichées au carnet de recettes. Signaler toute différence de recette, rendement, métier, niveau ou ingrédient avec une capture.

Le catalogue est construit une seule fois sur le thread framework par lots de 128 lignes puis indexé sur un worker. Les données managées conservent seulement les IDs et valeurs utiles ; les variantes ne sont pas fusionnées. La possession des ingrédients n'est pas encore affichée dans cette tranche afin de ne pas transformer une absence d'observation en zéro.

## CRAFT-001A-UI — version 0.5.2 : panneau recette et chaîne illustrés

Le bloc recette est maintenant un panneau dédié inspiré de l'interface FF14 : icône du résultat, séparation visuelle, métier/niveau/rendement, icônes des ingrédients et boutons d'accès aux outils liés. Les sections « Produit par » et « Utilisé dans » sont séparées et minimisables. Cliquer sur l'icône ou le nom d'un ingrédient le place dans la recherche commune et empile l'objet précédent dans un historique ; le bouton Retour remonte la chaîne. Le résultat d'une recette liée est lui-même cliquable. Les boutons Craftimizer, Item Use, GatherBuddy et Price Insight ouvrent Tous les outils avec le filtre correspondant ; tous les futurs outils restent accessibles dans le répertoire général.

COMPILED: YES — Verify.ps1 Release 0.5.2.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier après rechargement le rendu du panneau, les icônes, les deux sections, un clic ingrédient et Retour.

## CRAFT-001A-STOCKS — version 0.5.3

Chaque ingrédient affiche désormais la disponibilité observée « NQ … · HQ … ». Les données sont calculées depuis les snapshots de sacs et les caches de banque déjà chargés, avec « stock inconnu » ou « partiel » lorsque la couverture ne permet pas une affirmation complète. Le cache d'affichage est invalidé quand le personnage, le snapshot live ou la banque changent.

COMPILED: YES — Verify.ps1 Release 0.5.3.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier les quantités affichées pour les ingrédients et l'absence de faux zéro.

## UI-005-ORGANIZATION — version 0.5.4

La page Objets & rangements est organisée en quatre onglets : Recherche, Stocks, Craft lié et Imports. L'objet sélectionné reste partagé entre les onglets ; la recherche ne pousse plus les tableaux de stocks, le craft et la gestion des imports dans une seule colonne. La séparation est vérifiable sans modifier les données ni la banque.

COMPILED: YES — Verify.ps1 Release 0.5.4.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier la navigation entre les quatre onglets avec le sirop de bouleau sélectionné.

## CRAFT-001A-UI-FIX — version 0.5.5

Les noms d'ingrédients utilisent maintenant une ligne sélectionnable alignée à gauche au lieu d'un bouton centré étiré. L'icône garde une taille carrée fixe ; la quantité de recette et la disponibilité NQ/HQ restent à droite.

COMPILED: YES — Verify.ps1 Release 0.5.5.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le rendu des lignes avec le sirop de bouleau.

## CRAFT-001A-UI-FIX-2 — version 0.5.6

La zone de sélection d'un ingrédient est maintenant limitée à la largeur du nom et d'une marge de 20 pixels. L'espace de quantité et de disponibilité reste hors de la zone de recherche.

COMPILED: YES — Verify.ps1 Release 0.5.6.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le survol et le clic autour du nom, de la quantité et du stock.

## UI-003-TOOLS — version 0.5.7

Accueil et Tous les outils partagent maintenant une seule destination : « Accueil & outils ». Elle conserve la grille d'icônes, le filtre et les fiches détaillées au clic. Les liens depuis le craft reviennent dans cette même page avec le filtre de l'outil concerné. Objets & rangements, Réglages et Diagnostics restent des destinations séparées.

COMPILED: YES — Verify.ps1 Release 0.5.7.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le menu fusionné et un lien Craftimizer depuis Craft lié.

## CRAFT-001A-STOCKS-2 — version 0.5.8

Les lignes d'ingrédients indiquent maintenant `OK` quand NQ+HQ couvre la quantité requise, ou `Manque X` quand le stock observé est insuffisant. Un stock inconnu reste `stock inconnu` et un stock partiel reste `partiel`.

COMPILED: YES — Verify.ps1 Release 0.5.8.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier un ingrédient possédé et un ingrédient manquant.

## CRAFT-001A-CRAFTABLE — version 0.5.9

Chaque recette affiche maintenant le nombre de crafts possibles, calculé comme le minimum des stocks disponibles divisés par les quantités requises. Une couverture complète affiche un nombre exact ; une couverture partielle affiche `≥ X` car un rangement inconnu peut contenir davantage ; sans observation, le résultat reste inconnu.

COMPILED: YES — Verify.ps1 Release 0.5.9.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le nombre affiché pour le sirop de bouleau et ses recettes liées.

## CRAFT-001A-CRYSTALS — version 0.5.10

Le lecteur live capture maintenant les quatre sacs et `GameInventoryType.Crystals` (2001). Les stocks de craft demandent explicitement ce rangement en plus des sacs et des caches ; l'affichage général des quatre sacs reste inchangé. Le diagnostic nomme séparément la ligne Cristaux.

COMPILED: YES — Verify.ps1 Release 0.5.10.0, zéro avertissement/erreur.
TESTED: YES — 58/58 tests, paquet et versions vérifiés, dont un test de projection avec cristaux.
VALIDATED_IN_GAME: NO — vérifier une recette contenant des cristaux et la variation du nombre de crafts possibles.

## UI-ITEMS-CRYSTALS — version 0.5.11

Un cinquième onglet « Cristaux » est ajouté à Objets & rangements. Il reprend le principe FF14 de grille compacte avec icône, quantité et infobulle ; les quantités HQ éventuelles sont affichées après la quantité NQ. Cliquer sur une icône sélectionne le cristal dans la recherche commune. Le rangement 2001 est distinct des quatre sacs et reste source du calcul de craft.

COMPILED: YES — Verify.ps1 Release 0.5.11.0, zéro avertissement/erreur.
TESTED: YES — 58/58 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier la grille, les quantités, l'infobulle et le clic vers Recherche.

## UI-ITEMS-CRYSTALS-GRID — version 0.5.12

L'onglet Cristaux est organisé en tableau : les six éléments Feu, Glace, Vent, Terre, Foudre et Eau sont en colonnes ; Éclats, Cristaux et Agrégats sont en lignes. Les noms localisés du catalogue français servent uniquement à classer l'affichage ; les IDs et quantités restent ceux du rangement natif. Les éléments non classés sont conservés sous une section séparée.

COMPILED: YES — Verify.ps1 Release 0.5.12.0, zéro avertissement/erreur.
TESTED: YES — 58/58 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le tableau avec les quantités montrées dans la capture.

## UI-ITEMS-CRYSTALS-WATER — version 0.5.13

La classification des éléments accepte maintenant les formes françaises « de X », « d'X » et « d’X ». Les éclats, cristaux et agrégats d'eau ne doivent plus apparaître dans Cristaux non classés.

COMPILED: YES — Verify.ps1 Release 0.5.13.0, zéro avertissement/erreur.
TESTED: YES — 58/58 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier la colonne Eau après rechargement.

## UI-ITEMS-CRYSTALS-CLASSIFIER — version 0.5.14

La classification des cristaux est isolée dans `CrystalClassifier`, avec normalisation des accents et des apostrophes. Les tests couvrent « Éclat d'eau », « Cristal de foudre », « Agrégat de feu » et le rejet d'un objet sans élément.

COMPILED: YES — Verify.ps1 Release 0.5.14.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le résultat dans le client.

## CRAFT-001A-YIELD — version 0.5.15

Chaque recette affiche désormais séparément le nombre de crafts possibles et la quantité finale produite, calculée avec le rendement de la recette. Une borne partielle conserve le signe `≥` sur les deux valeurs.

COMPILED: YES — Verify.ps1 Release 0.5.15.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier une recette à rendement supérieur à 1.

## CRAFT-001A-NAVIGATION — version 0.5.16

Le panneau Craft lié affiche maintenant un fil `PARCOURS` quand plusieurs objets ont été traversés. Chaque niveau précédent est cliquable et reconstruit l'historique restant ; le bouton Retour continue de remonter d'un seul niveau.

COMPILED: YES — Verify.ps1 Release 0.5.16.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier une chaîne de plusieurs ingrédients et le clic sur un niveau du fil.

## UI-ITEMS-TAB-ORDER — version 0.5.17

L'ordre des onglets est maintenant Recherche, Stocks, Craft lié, Cristaux, Imports. Cristaux se trouve juste avant Imports comme demandé.

COMPILED: YES — Verify.ps1 Release 0.5.17.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier l'ordre visuel des onglets.

## UI-ITEMS-TAB-ORDER-FIX — version 0.5.18

L'identifiant ImGui de la barre d'onglets est versionné afin d'abandonner l'état visuel persistant de l'ancien ordre. Le code soumet désormais Recherche, Stocks, Craft lié, Cristaux, Imports dans cet ordre.

COMPILED: YES — Verify.ps1 Release 0.5.18.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier après déchargement/rechargement complet.

## CRAFT-001A-DIAGNOSTICS — version 0.5.19

Diagnostics affiche maintenant l'état du catalogue de recettes : préparation, progression, erreur ou nombre total de recettes et source de données. Cet affichage n'invente aucune recette et reprend l'état publié par le loader existant.

COMPILED: YES — Verify.ps1 Release 0.5.19.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier la ligne Catalogue de recettes dans Diagnostics.

RUNTIME_EVIDENCE: Capture utilisateur codex-clipboard-a550722d-0718-4b88-951d-08981b984d0d.png — Diagnostics affiche Plugin 0.5.19, catalogue Item 50 880 objets en français, catalogue recettes 13 892 recettes, quatre sacs complets et Cristaux complets. Cette capture valide le chargement des catalogues et la couverture du rangement cristaux ; elle ne valide pas encore l'exactitude d'une recette affichée dans le panneau Craft lié.

RUNTIME_EVIDENCE_CRAFT: Capture utilisateur codex-clipboard-ca1ae29d-eb65-4082-876e-e4452498701a.png — onglet Craft lié chargé sur « Marrons glacés collectionnables », fil « Sirop de bouleau › Marrons glacés collectionnables », recette Cuisine niveau 60, rendement 1, cinq ingrédients avec icônes/stocks et « ≥ 51 craft(s) → ≥ 51 objet(s) ». La borne est cohérente avec la couverture partielle affichée.

RUNTIME_EVIDENCE_BANK: Capture utilisateur codex-clipboard-5206a2cd-c29f-4b5c-bca7-e949e23414a8.png — onglet Stocks montre Personnage en direct, Sacoche chocobo, Sacoche chocobo supplémentaire et deux servants Shirune/Zet-suna avec cache manuel, dates UTC et message « Import sauvegardé dans la banque ». Cette preuve valide les imports et leur séparation par rangement ; un transfert ultérieur nécessite toujours un nouvel import manuel.

## USE-001 — version 0.5.20

La fiche Item Use du répertoire ouvre maintenant Objets & rangements pour l'objet conservé en contexte, avec la section Utilisé dans du Craft lié. La capacité reste PARTIAL : elle couvre les usages présents dans le catalogue Recipe et ne revendique pas encore les usages hors recettes.

COMPILED: YES — Verify.ps1 Release 0.5.20.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le bouton depuis la fiche Item Use avec le sirop de bouleau sélectionné.

RUNTIME_EVIDENCE_USE: Capture utilisateur codex-clipboard-a020a8da-a6b6-4f3e-8e63-edddd1b4bb3a.png — Accueil & outils affiche Item Use, l'objet en contexte « Sève de bouleau » est conservé, la fiche indique Fondation disponible et le bouton « Ouvrir les usages de l'objet » est visible. Le clic final vers Craft lié reste à vérifier séparément.

## USE-001-NAVIGATION — version 0.5.21

Le bouton Item Use demande maintenant à Objets & rangements de sélectionner directement l'onglet Craft lié à l'ouverture de la page. L'objet en contexte reste inchangé.

COMPILED: YES — Verify.ps1 Release 0.5.21.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le clic final depuis la fiche Item Use.

## USE-001-PREVIEW — version 0.5.22

La fiche Item Use affiche les recettes connues qui utilisent l'objet en contexte, avec icône, nom, métier et niveau. Chaque résultat est cliquable et ouvre directement cet objet dans Craft lié. La quantité de crafts possibles reste affichée dans Craft lié.

COMPILED: YES — Verify.ps1 Release 0.5.22.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier l'aperçu et le clic d'un résultat depuis la fiche Item Use.

## BANK-001 — version 0.5.22

Un outil Banque d'objets est disponible dans Accueil & outils. Il reprend maintenant une grille de cases inspirée de l'inventaire FF14 : icônes larges, quantité sous chaque case et tooltip détaillé au survol. Il regroupe les objets présents dans les imports servant et chocobo du personnage connecté, avec NQ, HQ, total et rangements sources. Une recherche et un clic sur l'icône placent l'objet dans le contexte commun ; les onglets Recherche, Stocks et Craft lié peuvent ensuite être utilisés.

COMPILED: YES — Verify.ps1 Release 0.5.22.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — ouvrir Banque d'objets après un import, vérifier les icônes et sélectionner une ligne.

## UI-005 — version 0.4.0 : icônes et rangements regroupés

Demande utilisateur : vraies icônes d'objets et interface groupée pour l'inventaire et les autres rangements. Une seule destination Objets & rangements remplace les deux vues séparées. Recherche illustrée et fiche de l'objet ; synthèse NQ/HQ/total recensé ; rubriques repliables Personnage, Chocobo, Servants ; filtres Tous/Personnage/Chocobo/Servants ; gestion des imports dans une section repliable.

COMPILED: YES — dernière exécution de Verify.ps1, Release 0.4.0.0, zéro avertissement/erreur.
TESTED: YES — 55/55 tests, paquet et versions vérifiés. Douze nouveaux groupes testent ItemHoldings de production : cas 182 NQ + 26 HQ = 208 sans exactitude globale, servants distincts, portée des quatre sacs seulement, isolation des personnages, personnage non connecté, sacs manquants, rangements non importés, zéro importé complet, extension optionnelle, couverture partielle, filtres, immutabilité et dépassements arithmétiques. Le test catalogue vérifie aussi la conservation d'IconId en recherche et lookup.
VALIDATED_IN_GAME: NO pour les nouveaux rendus d'icônes et la disposition 0.4.0. Les anciennes captures ne prouvent pas ces nouveaux chemins.

L'icône provient de Lumina Item.Icon, copiée comme identifiant managé dans le catalogue. ITextureProvider fournit une texture partagée pour la frame via TryGetFromGameIcon/TryGetWrap, sans attente, cache local de wraps ou ressource possédée à libérer. Si l'icône n'est pas encore disponible, son emplacement reste réservé ; la recherche et la sélection continuent de fonctionner.

Le modèle de regroupement a été implémenté et testé séparément par un worker, puis entièrement inspecté par le parent et testé avec le plugin intégré. Les essais isolés ne remplacent pas les 55 tests du projet principal. La dernière vérification complète a terminé avec code 0 ; un premier appel groupé avait fini sur le code 1 de rg parce que les anciens symboles Page.Bank/DrawBankHoldings n'existaient plus, alors que le build et ses tests avaient déjà réussi.

Le calcul utilise toujours le même personnage, les snapshots live et la banque persistante existante. Seul le filtre Personnage avec quatre sacs complets peut être déclaré exact au présent ; les stocks incluant du cache restent recensés à leurs dates et peuvent être périmés. Les sources jamais importées restent inconnues, sans faux zéro. Aucune migration ni modification des imports/persistance nécessaires.

Essai UI-005-RUNTIME après compilation : recharger 0.4.0, ouvrir Objets & rangements, chercher « cordial ». Vérifier icônes dans les résultats et la fiche sélectionnée. Contrôler les rubriques Personnage/Chocobo/Servants et les filtres ; le cas de la capture doit donner 182 NQ, 26 HQ, total recensé 208 si les données n'ont pas changé. L'affichage Personnage seul doit montrer le total exact des sacs ; les sources inconnues ne doivent pas devenir zéro. Vérifier une fenêtre large (recherche à gauche, fiche à droite) puis étroite (blocs empilés). Les imports manuels restent accessibles dans leur section et dans les panneaux attachés aux inventaires.
