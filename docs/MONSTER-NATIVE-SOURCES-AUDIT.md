# Native monster source investigation — 2026-10-30

Status: LOCAL_VERIFIED metadata inspection, not an in-game validation.

The installed Dalamud development bindings were inspected with the existing
ApiProbe tool; no live game state was read and no native reader was added.

```powershell
dotnet run --project tools/ApiProbe -c Release -- --find-excel=Drop --find-excel=Loot --find-excel=BNpc --find-excel=Mob --find-excel=RetainerTask
dotnet run --project tools/ApiProbe -c Release -- Lumina.Excel.Sheets.BNpcBase Lumina.Excel.Sheets.BNpcName Lumina.Excel.Sheets.MobHuntTarget Lumina.Excel.Sheets.RetainerTaskNormal --find-excel=ItemSource --find-excel=Treasure
```

## Verified fields

- BNpcName: localized Singular/Plural and grammar fields. No Item reference.
- BNpcBase: appearance, model, equipment, behavior and flags, including unknown
  fields. No named Item/drop relation. Unknown fields are not interpreted.
- MobHuntTarget: Name → BNpcName, Map, PlaceName, FATE and icon. These establish
  hunt target identity/location, not an Item acquisition relationship.
- RetainerTaskNormal: Item, GatheringLog, FishingLog and Quantity. An Item reward
  from a retainer venture does not identify the monster that drops that Item.
- Type discovery found LootModeType but no sheet name containing Drop or
  ItemSource. This is bounded discovery, not proof that every client resource has
  been exhaustively analyzed.

## Consequence for Item 44106

The earlier Garland French and English Item responses omitted `drops` and mob
partials. The inspected native tables cannot independently recover this missing
drop association. Do not assign a monster by matching rroneek names, assume that
BNpcName and BNpcBase IDs coincide, or treat a retainer reward as monster evidence.

The existing UI must retain the explicit unknown association state. Native names
and hunt locations can only enrich an independently verified monster identity.
Before implementing enrichment, establish exact ID mapping and coverage using
copied client data and tests. A general drop catalogue would require a verified
additional source or supported client relation; it is not implemented here.

## Next work

Continue the UI refactor independently. Keep potential native monster enrichment
in the backlog until an explicit Item → monster relation is established. No new
parser, combat hook, packet reader or speculative lookup was introduced.
