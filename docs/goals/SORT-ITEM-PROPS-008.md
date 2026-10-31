# SORT-ITEM-PROPS-008 — Propriétés d'objet pour le tri

0.84.0.0 — 2026-10-30 — IMPLEMENTED / COMPILED / TESTED (Verify.ps1 exécuté par l'agent sur le PC, 19:47 : 496/496, 0 avertissement/erreur, langues et paquet verts). VALIDATED_IN_GAME: NO.

Tranche n° 2 de [SORT-GAP-008-STUDY](SORT-GAP-008-STUDY.md), demandée par l'utilisateur le 2026-10-30 (« beaucoup plus de possibilités »).

## Source

Colonnes de `Lumina.Excel.Sheets.Item` vérifiées par réflexion sur `%APPDATA%\XIVLauncher\addon\Hooks\dev\Lumina.Excel.dll` le 2026-10-30 (LOCAL_VERIFIED) : `LevelItem` (RowRef), `LevelEquip` (byte), `Rarity` (byte), `PriceLow` (uint), `IsUnique`, `IsUntradable`, `DyeCount` (byte), `ItemRepair` (RowRef), `IsCollectable`. Copiés une fois dans `ItemDefinition` (`ItemLevel`, `EquipLevel`, `Rarity`, `SellPrice`, `Traits` : drapeaux Unique, Untradable, Dyeable = DyeCount > 0, Repairable = ItemRepair ≠ 0, Collectable). Aucune lecture native nouvelle.

## Comportement

- **Critères de tri** ajoutés : niveau d'objet, niveau d'équipement, rareté, prix de vente PNJ, identifiant. Jusqu'à 6 critères (au lieu de 4). Les valeurs de l'énumération sont ajoutées à la fin, donc les profils enregistrés gardent leur sens.
- **Filtres de zone** (« Filtres avancés ») : plages min–max pour le niveau d'objet, le niveau d'équipement et le prix de vente (0 = pas de limite) ; rareté exacte (blanc, vert, bleu, violet, rose) ; Oui/Non pour unique, non échangeable, teinture possible, réparable, collectionnable.
- **Inconnu ≠ non** : un objet absent du catalogue n'a aucune de ces valeurs ; il ne satisfait aucun filtre actif et se trie après les valeurs connues, dans les deux sens.
- Validation : bornes positives, min ≤ max, rareté parmi 0/1/2/3/4/7, filtres Oui/Non définis. Les zones enregistrées avant 0.84 se chargent sans nouvelle restriction.

## Effet sur le cache

La forme d'`ItemDefinition` change : le cache disque des objets (CATALOG-CACHE-012) est invalidé une seule fois au premier chargement de 0.84, puis réécrit. C'est le comportement prévu de la clé de forme.

## Tests

`InventorySortPropsTests` (4 groupes) : ordre par chaque nouveau critère avec inconnus en dernier dans les deux sens et départage ; plages, rareté et propriétés (Oui/Non, inconnu exclu, combinaisons) ; refus des valeurs invalides, format v1, aller-retour JSON ; copie dans les métadonnées et validation du cache des objets. Rendu ImGui non couvert.

## Protocole en jeu (DLL 0.84.0.0)

1. Critère « Niveau d'objet » décroissant : l'équipement le plus haut d'abord.
2. Zone avec « Filtres avancés » → Rareté Violet, ou Réparable Oui : seuls ces objets y vont.
3. Plage de prix de vente (ex. 1 – 100) : vérifier l'appartenance de quelques objets.
4. Diagnostics : source « Lumina.Excel/Item » au premier lancement de 0.84, puis « · DataCache » au suivant.
