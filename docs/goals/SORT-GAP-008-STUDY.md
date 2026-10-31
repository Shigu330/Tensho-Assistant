# SORT-GAP-008-STUDY — Écarts restants avec SortaKinda

2026-10-30. Étude seulement : aucun code modifié. Remplace l'entrée obsolète « NEXT SORT-NATIVE-002-STUDY » du BACKLOG : l'application native du tri existe depuis 0.54 (SORT-NATIVE-002) et a été étendue en 0.55–0.59.

## Source étudiée

MidoriKami/SortaKinda, clone du 2026-10-30 : HEAD `95a1bf9161f4bc595c21094eeaae9fda556f4b4b` (2026-08-29, « Use Framework.Run »). Identique au commit de référence du prompt maître : la baseline devient CURRENT_VERIFIED au 2026-10-30. Licence AGPL v3+ (LICENSE). Faits techniques seulement, aucune reprise de code.

Fichiers lus : Configuration/SystemConfiguration.cs, Classes/SortingController.cs, Extensions/InventoryTypeExtensions.cs, Extensions/InventoryItemExtensions.cs, FilterRules/*.cs, OrderRules/*.cs (liste et évaluation).

## Couverture comparée

| Domaine | SortaKinda | Tensho 0.76 | Écart |
|---|---|---|---|
| Rangements | 4 sacs, arsenal (12 catégories), sacoche + extension ; interception du tri natif aussi pour servant | 4 sacs, sacoche + extension, servant (capture directe) | **Arsenal absent** |
| Ordres | Alphabétique, catégorie UI, quantité, qualité, niveau d'objet, niveau d'équipement, rareté, prix de vente, ID, gearset | Nom, catégorie, quantité, qualité (multi-critères) | Niveau objet, niveau équipement, rareté, prix de vente, ID, gearset |
| Filtres | Tout, armure, arme, classe/job, collectionnable, teinture, niveau équipement, gearset, HQ, ID, niveau objet, catégorie UI, nom, rareté, réparable, unique, non échangeable, prix de vente | Catégorie, qualité, texte, emplacements d'équipement, vendable (type), existe en HQ | Niveaux, rareté, prix, unique, non échangeable, teinture, réparable, collectionnable, classe/job, gearset, ID |
| Déclencheurs | Ajout, retrait, zone, job, connexion et modification de config (actifs par défaut) ; changement, déplacement, fusion, division (désactivés par défaut) ; remplace la commande « Trier » du menu contextuel | Manuel uniquement, après capture et revalidation | Automatismes absents (choix de sécurité actuel) |
| Cases non assignées | Ordre optionnel séparé | Remplissage hors zones selon les critères | Comparable |
| Interface | Peinture des cases sur une grille, préréglages import/export | Zones par plages, modèles réutilisables | Peinture visuelle, import/export |

Faits techniques notés (Lumina Item, via InventoryItemExtensions) : niveau d'objet = LevelItem.RowId ; niveau d'équipement = LevelEquip ; rareté = Rarity (valeurs 1, 2, 3, 4, 7) ; prix de vente = PriceLow ; teinture = DyeCount > 0 ; réparable = ItemRepair.RowId ≠ 0 ; IsUnique ; IsUntradable ; arme = EquipSlotCategory 1, 2 ou 13 ; armure = ItemUICategory 34–38 et 40–43 ; gearset via RaptureGearsetModule (natif). Colonnes à revérifier contre le Lumina installé avant usage (LOCAL_VERIFIED requis).

## Constat prioritaire

Le tri natif de Tensho (0.54–0.59) n'a **jamais été observé en jeu**. Étendre les critères ou ajouter des automatismes avant cette validation empilerait du risque sur une écriture mémoire non confirmée.

## Tranches proposées (ordre recommandé)

1. **SORT-RUNTIME-VALIDATION** — essai utilisateur du protocole existant (SORT-NATIVE-002/SCOPES-003) : aperçu, application, refus après changement, reconnexion. Aucun code.
2. **SORT-ITEM-PROPS-008** — LIGHT. Étendre InventorySortMetadata (copie statique unique par ID) avec niveau d'objet, niveau d'équipement, rareté, prix de vente, unique, non échangeable, teinture, réparable ; nouveaux critères d'ordre et filtres par plages/oui-non ; inconnus distincts de « non ». Aucune lecture native nouvelle.
3. **SORT-ARMOURY-009** — HEAVY. Portée arsenal (sorters par catégorie d'ItemOrderModule), avec la même capture/revalidation que les autres rangements.
4. **SORT-AUTO-010** — HEAVY. Déclencheurs opt-in, désactivés par défaut, toujours via capture fraîche + permutation complète ; jamais pendant un combat, un échange ou une fenêtre de transfert ouverte (à étudier).
5. Plus tard : filtres classe/job et gearset (lecture native), interception du « Trier » natif (hook d'agent), peinture des cases.

SortaKinda reste PARTIAL.
