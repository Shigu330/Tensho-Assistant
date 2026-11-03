# Backlog ciblé

## Ouvert — état au 0.315.0 (2026-11-02, relu contre le CHANGELOG et le code)

Les points qui attendent une décision ou un relevé en jeu sont regroupés dans la [ROADMAP](ROADMAP.md), section « À décider / à
revoir » ; les tests en jeu en attente, dans sa section « Tests à faire en jeu ». L'ordre de passage est dans
[STATUS](../ai/STATUS.md) (« Prochaine action »).

Carte (MAP-001, plan : [MAP-016-PLAN](../goals/MAP-016-PLAN.md))
- Icônes des commissionnaires (ex. commises de Rowena) : corrigé en 0.155 (MAP-017), validé en jeu 2026-10-31.
- Repères d'exploration : **clos** (était « en pause » au 0.154) ; météo du wiki citée, affichée et utilisée par les alarmes depuis 0.201.
- Icône de type de quête approximative (71141 calculé contre 71151 vu en jeu) ; correspondance `ObjectiveId` en partie non prouvée.
  Toujours ouvert au 0.315 : relevé en jeu attendu (seul point « avant D17 » restant).
- Type de marqueur des mandats (repères au bord de la carte, 0.298) : inconnu, à relever en jeu avec un mandat accepté.
- Cadres et textures du jeu tirés des ULD (R-06 étape 6) : possible plus tard, à décider.

Objets et collections
- Sources encore inconnues pour certains objets (Alsvid, PNJ du Certificat végétal) : inconnu documenté (décision 2026-11-01) ;
  autres types de collection à couvrir (COLL-001).
- Marchands PNJ : monnaies codées et InclusionShop déjà traités (ligne périmée au 0.154) ; boutiques de compagnie libre (FccShop)
  **faites en 0.295**, validées en jeu. Ligne close.
- Injection du texte dans l'infobulle native du jeu : **abandonnée** (décision utilisateur 2026-11-01).
- Monnaies de quête sans objet direct : inconnu documenté ([QUEST-CURRENCIES-005-STUDY](../goals/QUEST-CURRENCIES-005-STUDY.md)).

Inventaire et tri : arsenal, coffre à apparence et armoire lus comme rangements (0.235–0.240, à valider en jeu) ; leur tri
(SORT-ARMOURY-009, HEAVY) et SORT-AUTO-010 (opt-in) : après publication. Cache disque des petits catalogues (cartes, donjons,
usages) non fait, gain faible.

Récolte : bonus d'Abondance **fait en 0.224** comme estimation (règle communautaire citée, affichée « estimé », modifiable), à
valider en jeu ; l'ancienne note « ne pas inventer de formule » reste vraie : aucune formule officielle. Pêche en mer non couverte
(pas de source de données utilisable).

Compagnie libre, Rappels, Équipement (R-07, R-08, R-11) : restes et choix ouverts dans la ROADMAP (« À décider / à revoir ») :
table des seuils de sous-marin, arbre de déblocage des dirigeables (LuminaSupplemental, GPL-3.0), planificateur de trajets de
dirigeable, règle de capacité sous le rang 50, escouade et Planeurs d'Ehcatl, étape 4 de R-11. R-09 (événements) : pas commencé.

Publication (PUBLISH-018, voir [PUBLISHING](PUBLISHING.md)) : soumission D17 éventuelle (décision de l'utilisateur). Côté code fait
(0.156–0.157) : icône `src/TenshoAssistant/images/icon.png` (générée par IA, déclarée), `manifest.toml` prêt, brouillon de PR avec la
déclaration d'IA. L'ancienne mention « il manque `images/icon.png`, `manifest.toml` et la déclaration d'IA » est périmée depuis 0.157.
Reste à l'auteur : commit et push, hash du commit dans `manifest.toml`, texte de PR et déclaration réécrits par lui (voir
[CONTRIBUTING](../../CONTRIBUTING.md#publication-dalamud)) ; traductions allemande et japonaise partielles, non relues.

Les 17 lignes de la [ROADMAP](ROADMAP.md) restent PARTIAL.

## Revue des points ouverts (2026-11-01, avant publication D17)
Décision utilisateur : publier après résolution ; l'injection de texte dans l'infobulle native du jeu n'est **pas** faite.
Suivi au 0.315 (2026-11-02) ajouté en fin de chaque décision : seul le point des icônes de quête reste ouvert.

| Point | Constat | Décision |
|---|---|---|
| Bonus d'Abondance (Récolte abondante II, actions 272/273) | GatheringOptimizer (MIT) le demande à la main ; frozen_rabbit_tome (sans licence, fait relevé seulement) : +3 si récolte ≥ ⌊base × 1,1⌋, +2 si ≥ ⌊base × 0,9⌋, sinon +1, base = valeur « Gathering » de l'item level du point (table item-level de Teamcraft, issue de la feuille du jeu ItemLevel). Règle communautaire, non officielle. | **Faire** : préremplir le bonus avec cette règle (stat de récolte lue, item level du point par sonde), affiché « estimé », modifiable. → **FAIT 0.224** ([preuve](../evidence/0.224-bountiful-base.md)), à valider en jeu. |
| Revue de mode : coffre à apparence et armoire | Lisibles dans le client (dresser d'apparence, armoire) ; donne un « possédé » plus juste. | **Faire** (sonde avant usage). → **FAIT 0.235–0.240** (élargi à l'arsenal, lu sans autre plugin, relié à la Banque), à valider en jeu. |
| Vendeurs : boutiques de compagnie libre | Feuille FccShop non lue (InclusionShop et monnaies codées déjà traitées, ligne du backlog périmée). | **Faire** (petit). → **FAIT 0.295** ([preuve](../evidence/0.295-fcc-shops.md)), validé en jeu. |
| Icônes de quête : nom des variantes, 71141 vs 71151 | Sens des variantes absent des données ; seule l'observation en jeu le donne. | **Faire seulement avec un relevé en jeu** de l'utilisateur ; sinon laisser « variante n ». → **Ouvert** : aucun relevé au 0.315. |
| Carte : cible de quête du journal qui reste (demande 2026-11-01, R-01) | L'icône « Cible indiquée par le jeu (journal) » ne pouvait pas être retirée. | **Faire**. → **FAIT 0.242–0.243** (retrait au clic droit, puis automatique), à valider en jeu. |
| Carte : « flèches boussole » vers une cible hors de la vue (demande 2026-11-01) | — | **Faire**. → **FAIT 0.297** sous forme d'icônes au bord de la carte, un réglage par type ; validé en jeu. |
| Revue de mode : score | Aucune donnée de score par pièce dans les sources. | **Ne pas faire** (serait inventé). |
| Suppression d'une alarme du jeu depuis Tensho | Pas de sous-commande /alarm par alarme (seulement « clear » = tout) ; DeleteAlarm(index) non prouvé. | **Ne pas faire** (risque d'effacer la mauvaise alarme) ; rappel manuel conservé. |
| Sources inconnues restantes (Alsvid, PNJ du Certificat végétal) | Absentes du jeu, de Garland et de LuminaSupplemental. | **Ne pas faire** : inconnu documenté. |
| Repères d'exploration (météo du wiki) | Météo du wiki déjà affichée et utilisée par les alarmes (0.201). | **Clos** (source wiki citée). |
| Compléments des 17 outils (rotation adaptative, tri d'arsenal, règles Triad natives…) | Gros chantiers, non nécessaires pour publier. | **Après publication**, au cas par cas. → **Repris** dans le plan c) ([GAPS](GAPS.md), étape g de la [ROADMAP](ROADMAP.md)) : rotation adaptative A-04 retenue, tri de l'arsenal I-14 plus tard ; règles natives de Triple Triade non relevées par la comparaison, restent ici. |
| Injection dans l'infobulle native | — | **Abandonné** (décision utilisateur). |

## Archive

> Entrées historiques (2026-10-27 → 2026-10-30), de la plus récente à la plus ancienne ; leurs « NEXT » et compteurs sont ceux de leur date.

- QUEST-ITEM-PROGRESS-007 CLOSE (2026-10-30) : TESTED 477/477 + VALIDATED_IN_GAME rapporté par l’utilisateur sur la DLL 0.76.0.0 (déconnexion, séquence 255, « 1 variante » inclus). SORT-RUNTIME-VALIDATION CLOSE (tri natif validé en jeu, rapport utilisateur 2026-10-30). SORT-ZONE-SELECT-011 TESTED en 0.77 (multi-catégories, objets choisis, termes « ; », clic sur l'aperçu) ; validation en jeu à faire. Suite : SORT-ITEM-PROPS-008.
- CATALOG-CACHE-012 : objets 0.81 (validé en jeu), recettes 0.82, récolte/pêche 0.83 (TESTED). Map, Duty, ItemUse non faits (petits catalogues, gain faible).
- NPC-VENDORS-014 TESTED en 0.88 (outil PNJ vendeurs, drapeau carte, survol). Suites possibles : table vérifiée des monnaies codées hors mémoquartz (assignats), InclusionShop, boutiques de compagnie libre.
- HOVER-TOOLTIP-013 TESTED en 0.85 (panneau sous l'infobulle native, moins cher du centre de données, stock/obtention/usages). Option future HEAVY : injection du texte dans l'infobulle native.
- SORT-ITEM-PROPS-008 TESTED en 0.84 (niveaux, rareté, prix, ID, unique/non échangeable/teinture/réparable/collectionnable). Suite SortaKinda : SORT-ARMOURY-009 (HEAVY), SORT-AUTO-010 opt-in.

- QUEST-CURRENCIES-005-STUDY clôturée sur UNKNOWN documenté : quinze monnaies sans Item/clé GC de récompense, aucune relation unique trouvée ; quantités conservées, pas de compagnie inventée. Voir docs/goals/QUEST-CURRENCIES-005-STUDY.md. BASE-AUDIT-003 terminé : cohérence/livraison 0.69 et diagnostics améliorés ; suite indépendante selon quota, sans blocage global.

- BASE-AUDIT-002 terminé dans son périmètre de contrôle : 17 références présentes, 17 couvertures PARTIAL, 71 liens locaux/61 Markdown résolus, DLL et paquet 0.59.0.0 concordants. Voir docs/goals/BASE-AUDIT-002.md. Prochaine étude indépendante : catalogue GatherBuddy/pêche, après contrôle du quota ; ne pas inventer les appâts/prérequis absents.

- USE-CONTEXT-004 IMPLEMENTED / TESTED en 0.53 : menu d’inventaire opt-in, cinq routes partagées, garde de propriétaire/révision/âge, aucune requête réseau par le menu. Rendu natif à confirmer ; liens de discussion et cibles Default hors de cette tranche.
- SORT-NATIVE-002-STUDY OBSOLÈTE (2026-10-30) : application native livrée en 0.54–0.59. Remplacée par docs/goals/SORT-GAP-008-STUDY.md : 1) validation en jeu du tri natif (jamais observé), 2) SORT-ITEM-PROPS-008 (LIGHT, propriétés Item statiques), 3) SORT-ARMOURY-009, 4) SORT-AUTO-010 opt-in.

État au 0.75.0 (2026-10-29, archive) : [cache partagé et péremption réglable](../goals/CACHE-001.md). Prix, quêtes, collections, sources communautaires, Revue de mode et calculs du carnet réutilisent des instantanés persistants datés. Durées indépendantes dans Réglages : 5 min, 1 h, 24 h ou 7 j ; actualisation forcée depuis les outils. Les imports restent manuels et signalent leur péremption ; les observations en direct gardent une invalidation courte. 466/466 tests/package/SHA-256 passants, zéro avertissement/erreur ; validation en jeu de ce cache encore à faire.

- GATHER-FISH-006 IMPLEMENTED / TESTED en 0.60 : carnet natif, 2 461 fiches, restrictions et tomes ; 389 tests/package et probes natifs passants. 491 codes météo restent inconnus.
- GATHER-FISH-SOURCES-007 IMPLEMENTED / TESTED en 0.61 : appâts/chaînes par lieu, cache/transport communs, 396 tests et consultation réelle 4869 passants. Code météo natif 2 non expliqué, Unknown conservé.
- GATHER-FISH-WINDOWS-008 IMPLEMENTED / TESTED en 0.62 : conditions par lieu, plages horaires ET/UTC, météo précédente distincte ; 401 tests et probes réels passants. Disponibilité non déduite.
- GATHER-FISH-WEATHER-009 IMPLEMENTED / TESTED en 0.63 : noms/icônes météo natifs par territoire, ambiguïtés et métadonnées incomplètes inconnues ; 405 tests et probes réels passants.
- GATHER-FISH-FORECAST-010 IMPLEMENTED / TESTED en 0.64 : plages des critères renseignés, couverture et logique ternaire explicites ; 412 tests et probes réels/fixtures datées passants. Pas de capture garantie.
- COLL-002 IMPLEMENTED / TESTED en 0.65 : orchestrion natif, confirmations positives, catégories et liens Item/contexte bidirectionnels, erreurs de catégorie isolées ; 416 tests et probe natif passants.
- COLL-003 IMPLEMENTED / TESTED en 0.66 : titres/succès, catégories/fiches/récompenses, preuve d’acquisition à trois états et refresh coalescé ; 421 tests et probe natif passants, aucun runtime revendiqué.
- QUEST-REWARDS-003 IMPLEMENTED / TESTED en 0.67 : récompenses conditionnelles natives et lien Item/stocks ; 426 tests et probe 5 373 quêtes/9 313 lignes passants. Quatre quantités, quinze monnaies et association de rang courant inconnus explicitement.
- QUEST-ITEM-SOURCES-004 IMPLEMENTED / TESTED en 0.68 : index partagé Item → quêtes, variantes/couverture conservées et publication atomique/cancellable ; 434 tests, 3 496 Items/8 288 associations vérifiées.
- NEXT QUEST-CURRENCIES-005-STUDY : quinze monnaies sans Item direct, clés GC et variantes sur preuve ; ne pas choisir compagnie/monnaie par convention. Les anciennes entrées NEXT ci-dessous sont historiques ; GOAL.md détermine la suite courante.

- SORT-TEMPLATES-007 IMPLEMENTED / TESTED en 0.59 : bibliothèque commune bornée, géométrie compatible, chargement en brouillon, copies sans observations ; 385 tests/package, Newtonsoft trois modes et ZIP identique aux sorties canoniques.
- NEXT BASE-AUDIT-002 : audit ciblé du périmètre des 17 outils, documents/chemins et limites. Compteur à vérifier avant toute nouvelle grosse tranche ; réserve 5 %.

- SORT-FILTERS-006 IMPLEMENTED / TESTED en 0.58 : équipement, marché et potentiel HQ, inconnus explicites, 379 tests/package, Newtonsoft et probe natif passants.
- NEXT SORT-TEMPLATES-007 : modèles de règles indépendants du personnage, géométrie compatible, chargement en brouillon sans application native.

- SORT-PROTECTED-005 IMPLEMENTED / TESTED en 0.57 : positions protégées et inversion, refus des indices absents/ambigus, 374 tests/package et Newtonsoft trois modes.
- NEXT SORT-FILTERS-006 : équipement/marché depuis les métadonnées Item existantes ; ne pas transformer une absence de métadonnées en false.

- SORT-RULES-004 IMPLEMENTED / TESTED en 0.56 : profils par scope, quatre critères, 32 zones réservées avec catégorie native/qualité/recherche, aperçu positionné et refus des pertes ; 369 tests/package et Newtonsoft trois modes.
- NEXT SORT-PROTECTED-005 : zones préservées à leurs positions actuelles et remplissage inversé, avec garde sur les indices visuels et sans nouveau moteur natif.

- SORT-SCOPES-003 IMPLEMENTED / TESTED en 0.55 : lecture directe distincte de la banque, sacoches/servant, scope exact et recapture avant application. 361 tests/package ; runtime non observé.
- NEXT SORT-RULES-004 : zones de cases et filtres des catégories natives, critères multiples ; aperçu/permutation et application explicite partagés.

- SORT-NATIVE-002 IMPLEMENTED / TESTED en 0.54 : application visuelle explicite des quatre sacs, snapshot récent revalidé, permutation intégrale. 355 tests/package ; natif/rendu/persistance non observés.
- NEXT SORT-SCOPES-003 : capturer en direct les sacoches/servants ouverts avant de proposer l’application au rangement ; le cache importé seul ne permet aucune écriture.

MISE À JOUR 2026-10-27 / 0.5.27 : les anciens états READY/TODO de CRAFT-001A ci-dessous sont dépassés. Catalogue, carnet et préparation implémentés, 63 tests réussis et confirmation utilisateur reçue. Cet ancien checkpoint est clos ; GOAL.md décrit la tranche actuelle. Les scénarios runtime non observés restent conservés dans TESTING.md.

- CORE-001 DONE : snapshot managé et InventoryService avec portée propriétaire/conteneurs/NQ-HQ, couverture et observations ; huit tests inventaire passent.
- INV-001 DONE (implémentation/tests) : adaptateur des quatre sacs intégré, lecture framework, invalidation et snapshots Partial/Unknown. Aucun Complete supposé.
- INV-002 DONE (implémentation/tests, 0.1.1) : preuve de chargement/stabilité et lecture complète, affichage d'un total exact des quatre sacs si complet ; nouvel essai live requis.
- RUNTIME-001 : protocole utilisateur courant accepté ; cas avancés de déchargement/erreur/second personnage conservés dans TESTING.md, sans réussite supposée.
- ITEM-001 DONE côté code/tests : catalogue Item partagé, métadonnées de génération et recherche par nom/ID reliée au diagnostic des sacs.
- ITEM-001-RUNTIME DONE : catalogue français 50 880 objets, recherche/sélection et NQ/HQ/total confirmés par capture et retour utilisateur.
- CRAFT-001A READY : catalogue Recipe + RecipeLevelTable + ingrédients ; première tranche recette → ingrédients, basée sur le même catalogue Item.
- UI-003 DONE côté code/tests : espace commun avec navigation, répertoire des 17 outils, réglages et diagnostics séparés. UI-003-RUNTIME ACTIVE : contrôle visuel 0.2.0 avant reprise de CRAFT-001A.
- UI-004 DONE côté code/tests : répertoire remplacé par une grille d'icônes natives ; détails seulement dans une fiche après clic. L'essai UI-003-RUNTIME doit utiliser la nouvelle DLL 0.2.1.
- BANK-001 DONE côté code/tests : cache persistant par personnage/servant/sacoche, imports manuels attachés aux menus et résultats de recherche datés. BANK-001-RUNTIME ACTIVE : vérifier ancrage/import/réimport/persistance en 0.3.0 avant reprise des recettes.
- Banque : équipements/cristaux/marché des servants et éventuelle suppression manuelle d'un ancien relevé restent des extensions séparées, sans prétendre être inclus dans le relevé des sept pages.
- UI-005 DONE côté code/tests : icônes réelles et regroupement unifié Personnage/Chocobo/Servants. UI-005-RUNTIME ACTIVE : vérifier visuellement 0.4.0. La capture 0.3.0 prouve un cache chocobo consultable mais ne valide pas encore les imports servant ou leur attache/persistance.
- CRAFT-001 TODO après ITEM-001 : catalogue Recipe + RecipeLevelTable + ingrédients ; chemin recette → besoins.
- Runtime CORE-000 à effectuer : chargement, commande, préférence, rechargement/déchargement et logs dans un client FFXIV approprié.
- Les phases suivantes de chaque plugin restent dans ROADMAP.md. Les dépôts non fournis doivent être identifiés et leurs licences vérifiées au moment de leur étude ciblée (NEEDS_STUDY).
- Combat/ACT/IINACT : retiré du projet (décision utilisateur 2026-10-31).
