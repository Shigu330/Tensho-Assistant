# X-115 — client map database, first comparison 7.57 → 7.58 (2026-11-10)

Tool: `tools/client-snapshot.py` with `tools/ClientSnapshot` (README of `tools/`). Game on disk: `ffxivgame.ver` 2026.11.03.0000.0000
(7.58); Dalamud 15.0.3.9 does not support it yet, so the plugin could not run: the snapshot was read without Dalamud.

## Method and proof that both sides have the same shape

- 7.57 (2026.10.16.0000.0000): the five `catalog-*` files of the plugin cache built on the 7.57 client (copied before the patch),
  imported as they are (`import-cache`).
- 7.58: `ClientSnapshot` loads the built plugin (TenshoAssistant 0.1027.0.0) and runs its own loaders (`MapLocalIndexLoader`,
  `NpcVendorLoader`, `GatheringCatalogLoader`, `ItemCatalogLoader`, `RecipeCatalogLoader`) on a Lumina `GameData`, through stand-ins of
  `IDataManager` (a class: `DispatchProxy` cannot emit `GetExcelSheet<T>`'s self-referencing constraint, `TypeLoadException`),
  `IFramework` and `IPluginLog`; they write through the plugin's `DataCache`.
- Checked on the files: same family, scope (`French`), value keys, and the same version string after the game versions
  (`schema=7.0.0.0|copy=…|<row shape>`), which the plugin itself uses to refuse a cache of another shape. Rows unchanged by the patch
  compare equal field by field (gathering 16 280, fish 2 461, recipes 13 892: 0 changes).
- Sheet layouts: `patch-watch.py sheets` (Lumina.Excel 7.0.0.0 of `Hooks\dev` against the 7.58 data): the 243 sheets Tensho reads are
  all `ok`, none added or removed, so the 7.58 rows are not misread.

## Counts (rows before → after; added / removed / moved / changed)

| Family | 7.57 | 7.58 | + | − | moved | changed |
|---|---|---|---|---|---|---|
| map positions | 7 345 | 7 346 | 1 | 0 | 0 | 0 |
| NPC vendors | 920 | 923 | 3 | 0 | 0 | 0 |
| shop sales | 39 275 | 39 277 | 2 | 0 | 0 | 0 |
| gathering sources | 16 280 | 16 280 | 0 | 0 | 0 | 0 |
| fish | 2 461 | 2 461 | 0 | 0 | 0 | 0 |
| items | 50 880 | 50 883 | 3 | 0 | 0 | 0 |
| recipes | 13 892 | 13 892 | 0 | 0 | 0 | 0 |
| maps / markers / aetherytes | not cached in 7.57 | 1 231 / 8 266 / 239 | — | — | — | — |

The additions are one seasonal event: quest 70913 (giver on map 11, Limsa Lominsa upper decks), three NPC vendors 1054717–1054719 at
the same place, two sales of shop 1770885, items 46786, 51268 and 52661.

## Expected by the 7.58 forecast

- Beastmaster (XBM) trainer: nothing new in these catalogues (no NPC, vendor or position added under that name); the 7.58 changes
  forecast for XBM are in game windows, which this tool does not read.
- North Horn (Map 1135 and 1244, territory 1346): both maps are in the 7.58 snapshot; the six positions on them are unchanged since
  7.57. The 7.57 maps were not cached, so whether the maps themselves changed cannot be said for this patch (from now on, `snapshot`
  keeps maps, markers and aetherytes for each version).
