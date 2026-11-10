# X-116 — client placements, facts against the client, « to recheck » and « to discover » (2026-11-10)

Tools: `tools/ClientSnapshot` (`client-placements.json`, mode `--probe-lgb=all`), `tools/client-snapshot.py` (`diff`, `patch`, `check`,
`discover`), `tools/discoveries-merge.py` (`recheck`, `sync --dry-run`) (README of `tools/`). Game on
disk: `ffxivgame.ver` 2026.11.03.0000.0000 (7.58), read with Lumina 7.0.0.0 without Dalamud (not ready for 7.58 yet). The snapshots stay in
`%LOCALAPPDATA%\TenshoAssistant\client-snapshots` (Square Enix data, never published); the shared discoveries hold no client data.

## Step B — what the layout files place (probe, R06)

`ClientSnapshot <sqpack> <out> --only=placements --probe-lgb=all`: the 596 distinct layouts (`TerritoryType.Bg`, up to `/level/`) and their
files `bg`, `planevent`, `planlive`, `planmap`, `planner` (`sound` and `vfx` hold no placement of use; 17 `planner.lgb` files have another
header and are skipped). Entries per asset type that matter here:

| Asset type | bg | planevent | planlive | planmap | planner | Kept as |
|---|---|---|---|---|---|---|
| EventNPC | 0 | 51 957 | 73 | 1 595 | 1 385 | `enpc` (55 010): ENpcBase row, layer festival |
| EventObject | 6 | 9 704 | 2 732 | 8 504 | 297 | `eobj` (21 243): EObj row |
| EventRange whose instance id is a `Fate.Location` | — | 1 181 | 58 | 142 | — | `fate` (1 701 rows: 1 381 locations, all found) |
| MapRange of `DynamicEvent.LGBMapRange` | — | — | — | 30 | — | `ce` (30, all found) |
| Aetheryte | 0 | 0 | 0 | 197 | 0 | `aetheryte` (197) |
| Treasure | 2 | 0 | 0 | 1 931 | 0 | `treasure` (1 933) |
| ExitRange | 0 | 0 | 0 | 189 | 0 | `exit` (189): destination TerritoryType |
| BattleNPC | 0 | 0 | 0 | 0 | 0 | none: **the client places no monster** |
| Gathering | 0 | 0 | 0 | 0 | 0 | none: gathering points come from `ExportedGatheringPoint` (catalogue) |

Left out on purpose: PopRange (14 839, monster spawn areas without a monster), MapRange (19 794, sub-area names and settings), EventRange
not tied to a FATE, SharedGroup, BG, lights, sounds: no fact or page of Tensho uses them.

Size and time (7.58, French client): `client-placements.json` 10 177 KB, 80 303 rows, 4.0 to 5.0 s; the whole snapshot (5 catalogues,
maps, placements) about 15 s with `client-snapshot.py patch`; the store holds 55 MB for 7.58 and 44 MB for 7.57 (catalogues only).

Move thresholds of `diff` per family, in yalms: NPCs, objects, aetherytes, coffers 1; zone exits 2; FATE and CE ranges 5 (wide areas whose
centre matters at a few yalms). Positions are written to 0.01, so 1 yalm is far above rounding. A change of layout counts as a move. No
patch can be compared yet: 7.57 has only the plugin cache's catalogues (imported before the patch), so the placement families show
« not in both snapshots »; the first real comparison of placements will be 7.58 → 7.59.

## Step C — facts against the client (`client-snapshot.py check`)

Facts read: the community base (`community-discoveries/discoveries.json`, 1 453 facts) and the maintainer's own `Discoveries.json`
(1 772 facts, mostly the same player: both counted). Compared only for the kinds the client also places: NpcPlace (19) with the layout NPCs
of that ENpc in that territory; FateSeen (3) with the FATE's range; FishCatch, FishBite, FishBiteBait (10, 23, 29; IntuitionStart 30 has no
fact yet) with the fishing spots of the map (distance to the edge of the area: `FishingSpot.Radius` × 0.155, as the plugin); SpearShadow (28)
with the spearfishing points (gathering type 5) of the territory. Never MonsterPlace, HuntMarkPlace, MonsterDrop, BlueSpellCaster, MonsterKill
(placed by the server: 0 BattleNPC above).

| Kind | Facts | At their place | Distances in yalms (median, p90, p99, max) |
|---|---|---|---|
| 3 FATE | 212 | 212 | 0.0, 0.0, 0.0, 0.0 (largest 0.05) |
| 19 NPC | 29 | 29 | 0.0, 0.0, 0.0, 0.0 (largest 0.05) |
| 28 spearfishing | 94 | 94 | 0.0 (all inside their point's area) |
| 10 fishing | 102 | 102 | 0.0, 0.0, 35.3, 35.3 |
| 23 fishing | 105 | 105 | 0.0, 0.0, 35.3, 35.3 |
| 29 fishing | 33 | 33 | 0.0, 35.3, 35.3, 35.3 |

The 35.3 is one place of territory 1190, map 860, (-191, 664): 19 catches, 190 yalms from the spot at (-271, 836) whose area is 155: true
catches (the maintainer's own), so the catalogue's area is short there or a spot is missing; not a bad fact. Thresholds (`THRESHOLDS` in
`client-snapshot.py`), above the largest true distance with a margin: NPC 2 (wandering NPCs), FATE 5, fishing 50, spearfishing 20. Result
on 7.58: 575 facts compared, 575 at their place, **0 SUSPECT**, 0 placed only elsewhere, 0 unplaced.

## Step C — « to recheck » and « to discover » on 7.57 → 7.58

- `client-snapshot.py patch` wrote `changes-from-2026.10.16.0000.0000.json`: 4 changed places (1 quest giver, 3 vendors), all in territory
  128 (Limsa Lominsa, upper decks), cells (0, 2); patch day 2026-11-03.
- `discoveries-merge.py recheck --local`: **0 facts to recheck** (no fact of the two files was seen before 2026-11-03 in those cells or the
  next ones). Not compared, missing from 7.57: maps, map markers, aetherytes and the seven placement families.
- `client-snapshot.py discover previous latest`: **3 to discover**, the three items the patch added with no gathering source, shop sale or
  recipe in the client and no fact naming them: 46786, 51268, 52661. The three new vendors have a place in the client, so they are not on
  the list.

## X-113 — nothing new is not merged again

`discoveries-merge.py sync --dry-run` on 2026-11-10: ticket #43, 1 453 facts, 1 refused, « nothing new (same facts as its player file): no
write, no « Merged » comment ». `discoveries-merge.py rebuild`: « discoveries.json: unchanged, 1453 facts from 1 players », the file is not
written (0.1027 had rewritten it for its date alone).

## Automation

The maintainer's routine after a game patch runs `client-snapshot.py patch`, then `check`, `recheck` and `discover`, and keeps a French
summary for the session in game (outside the repository). Self-tests: `client-snapshot.py --selftest` (17), `discoveries-merge.py
--selftest` (5).
