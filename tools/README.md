# Tools: read-only probes

Small console programs that read the **installed game files** (and, for `ApiProbe`, the metadata of Dalamud's assemblies) to prove
a fact before the plugin relies on it. They never write to the game, never run game code, and are not part of the plugin package.
Their findings go to `docs/evidence/` and are cited by the code (project rule: unknown is not zero, never invent data).

They are not in `TenshoAssistant.sln`: build and run each one on its own. Most take the game's `sqpack` folder as first argument:

```powershell
dotnet run --project tools/ShopCurrencyProbe -- "C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack"
```

| Probe | What it checks |
|---|---|
| `ApiProbe` | Members of Dalamud, FFXIVClientStructs and Lumina types by reflection on metadata only (`--find-method=`, `--find-struct=`, `--find-excel=`, or a full type name): used before calling a native API. Reads `%AppData%\XIVLauncher\addon\Hooks\dev`. |
| `ShopCurrencyProbe` | The general probe: one block per `PROBE_*` environment variable (shops and currencies, NPC positions, vessel parts, gear and materia, quest genres and start cities, map markers, gathering levels, event icons…). Set the variable(s), run, keep the output as evidence. The list of variables is in the plugin's contributor panel (Diagnostics, developer mode). |
| `CollectionProbe` | The collections projection (mounts, minions, cards…) built from the game sheets, and that it stays usable after the game data is released. |
| `ConfigSerializationProbe` | That the plugin configuration round-trips through Newtonsoft.Json as Dalamud saves it. |
| `CraftSimulationProbe` | The crafting simulator and solver against real recipes (collectables included). |
| `FashionProbe` | Fashion Report sheets (weekly themes, slots) used by the Fashion pages. |
| `FishingNotebookProbe` | The fishing log sheets (spots, fish, notebook order). |
| `GatheringActionsProbe` | Gathering actions and their effects, for the GP planner. |
| `GatheringProbe` | Gathering points and their Eorzea time windows converted to real time. |
| `GatheringRequirementsProbe` | Gathering requirements (levels, perception, unlocks) of points and items. |
| `GatheringUiProbe` | The gathering window's layout values the gathering helper reads. |
| `ItemUseProbe` | Where an item is used (recipes, Grand Company, quests…), for the Uses tab. |
| `QuestRewardsProbe` | Quest reward rows (fixed and optional items, currencies) for the quest and item source projections. |
| `ToolBonusProbe` | Bonuses of gathering and crafting tools. |
| `UldProbe` | Interface layouts (`.uld`) of the game: which exist, the textures they use (`--tex` sizes, `--parts` the cut the game declares). Proves the game frame textures (docs/evidence/0.332-uld-window-frame.md). |
| `WeatherProbe` | Weather rates per territory and the forecast formula, unknown kept unknown. |
| `submarine-loot.py` | Not a probe: rebuilds `src/TenshoAssistant/FreeCompany/Data/submarine-loot.json` from FFXIVGachaSpreadsheet's `Submarines.json` (MIT, notice written into the file). |

Each probe prints `PASS …` / `FAIL …` lines or the raw rows it read. A probe that breaks after a game update is a signal: the fact
it proved may have changed, so the code that relies on it must be checked (see R-14 in `docs/project/USER-REQUESTS.md`).
