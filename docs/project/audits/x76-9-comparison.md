# X-76 step 9 — comparison with the starting point (2026-11-09, version 0.956)

Starting point: docs/project/audits/x76-0-baseline.md (2026-11-08, version 0.903). Same script ideas (source tree src/TenshoAssistant, bin and obj left out).

| Figure | 0.903 (start) | 0.956 (now) | Note |
|---|---|---|---|
| C# files | 589 | 606 | new shared components (status lines, tool button, atomic file, background writer, once set, backup zip, community sources…) and pure parts split from Dalamud readers for tests |
| C# lines | 69,148 | 70,818 | +2.4 % for 53 versions (0.904–0.956) adding features (ocean fishing V1–V2, airship route, fishing facts, discoveries load handling) |
| Language keys en / fr | 6,925 / 6,925 | 6,905 / 6,905 | duplicates removed (H8), keys never asked removed (step 1); en and fr still equal |
| Language keys de / ja | 40 / 40 | 35 / 35 | partial files, as planned |
| Hosts written in the code | 27 | 27 | same list; every one sorted in docs/project/audits/0.949-network-rules.md |
| Developer capture parts | 20 | 20 | same parts (the proven ones stay until their readers replace them) |
| Warnings disabled | none | none | no pragma, no NoWarn |
| Test files / cases | 427 / 1,483 | 475 / 1,584 | every version added its test; 0 duplicate case names |

(« Settings » are not compared: the start counted the plain get/set properties of Configuration.cs, 263; the settings inventory of step 3 has 301 rows for
the 134 setting properties and the fields of their objects — docs/project/SETTINGS.md.)

## C# files and lines per folder (now)

| Folder | .cs files | Lines |
|---|---|---|
| (root) | 203 | 33,376 |
| Alarms | 12 | 1,320 |
| Beastmaster | 3 | 329 |
| Caching | 6 | 303 |
| Carnivale | 2 | 88 |
| Character | 5 | 582 |
| Collections | 11 | 1,071 |
| Community | 12 | 1,226 |
| Competitive | 2 | 130 |
| Craft | 39 | 2,962 |
| Credits | 1 | 257 |
| DeepDungeons | 2 | 81 |
| Deliveries | 3 | 178 |
| Diagnostics | 7 | 1,021 |
| Discoveries | 6 | 1,547 |
| Duties | 7 | 526 |
| Events | 2 | 226 |
| Fashion | 5 | 474 |
| Fields | 2 | 126 |
| FreeCompany | 11 | 1,478 |
| Gathering | 41 | 3,256 |
| Gear | 11 | 1,053 |
| GrandCompanies | 2 | 82 |
| Guide | 1 | 80 |
| Help | 3 | 226 |
| Hunting | 3 | 246 |
| Inventory | 32 | 3,307 |
| Island | 7 | 463 |
| Items | 25 | 1,537 |
| Leves | 2 | 121 |
| Lexicon | 2 | 93 |
| Localization | 2 | 249 |
| Macros | 7 | 671 |
| Maps | 26 | 2,930 |
| Market | 18 | 1,819 |
| MiniGames | 15 | 1,829 |
| Monsters | 8 | 838 |
| MyTools | 6 | 650 |
| Npcs | 2 | 557 |
| Quests | 9 | 708 |
| Relics | 2 | 202 |
| Reminders | 15 | 1,053 |
| Societies | 2 | 77 |
| UI | 24 | 1,470 |

## Steps

- 0 baseline, 1 dead code (0.904), 2 duplicates and harmonisation H1–H10 (0.905–0.925), 2c parsers P1–P10 and E1–E5 (0.911–0.938), 3 settings (0.944–0.954),
  4 tooltips (0.947), 5 network and Dalamud rules (0.949), 6 robustness and performance (0.950–0.956), 7 tests (survey: 475 files, 1,587 cases, no
  duplicate; 374 files check source text by design, since Dalamud-bound code cannot run without the game; new tests keep to behaviour where the code is
  pure), 8 repository (ignore rules, tools list, CI and CodeQL green, no open code alert).
- Left for the author: the guided in-game pass of every page (one point at a time), the proposals in docs/project/G-PROPOSALS.md « À revoir avec
  l'utilisateur », then the documents (X-91) and 1.0.0.
