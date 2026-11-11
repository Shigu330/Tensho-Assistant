# X-76 step 0 — starting point of the full clean-up (2026-11-08, version 0.903)

Measured by a script on the source tree (src/TenshoAssistant, bin and obj left out) before any clean-up, to compare at the end (step 9).
Backup of the repository files before starting: `checkpoints/backup-20261108-1450-x76-start.zip` (local, not in git).

## C# files and lines per folder

| Folder | .cs files | Lines |
|---|---|---|
| (root) | 195 | 32,770 |
| Alarms | 12 | 1,327 |
| Beastmaster | 3 | 328 |
| Caching | 5 | 250 |
| Carnivale | 2 | 88 |
| Character | 5 | 552 |
| Collections | 11 | 1,063 |
| Community | 12 | 1,224 |
| Competitive | 2 | 130 |
| Craft | 39 | 2,967 |
| Credits | 1 | 254 |
| DeepDungeons | 2 | 81 |
| Deliveries | 3 | 178 |
| Diagnostics | 7 | 1,022 |
| Discoveries | 6 | 1,170 |
| Duties | 7 | 524 |
| Events | 2 | 221 |
| Fashion | 5 | 473 |
| Fields | 2 | 126 |
| FreeCompany | 11 | 1,419 |
| Gathering | 41 | 3,166 |
| Gear | 11 | 1,026 |
| GrandCompanies | 2 | 82 |
| Guide | 1 | 80 |
| Help | 3 | 232 |
| Hunting | 3 | 246 |
| Inventory | 32 | 3,306 |
| Island | 7 | 463 |
| Items | 25 | 1,527 |
| Leves | 2 | 121 |
| Lexicon | 2 | 93 |
| Localization | 2 | 249 |
| Macros | 7 | 671 |
| Maps | 25 | 2,856 |
| Market | 18 | 1,819 |
| MiniGames | 15 | 1,829 |
| Monsters | 8 | 819 |
| MyTools | 6 | 653 |
| Npcs | 2 | 540 |
| Quests | 9 | 708 |
| Relics | 2 | 202 |
| Reminders | 15 | 1,051 |
| Societies | 2 | 77 |
| UI | 17 | 1,165 |
| **Total** | **589** | **69,148** |

## Other figures

- LANGS {'de': 40, 'en': 6925, 'fr': 6925, 'ja': 40}
- SETTINGS 263
- HOSTS 27 ['api.github.com', 'claude.com', 'cli.github.com', 'creativecommons.org', 'de.finalfantasyxiv.com', 'discord.com', 'docs.google.com', 'ffxiv.consolegameswiki.com', 'ffxivteamcraft.com', 'fontawesome.com', 'fr.finalfantasyxiv.com', 'gist.github.com', 'github.com', 'gubal.ffxivteamcraft.com', 'jp.finalfantasyxiv.com', 'licenses.nuget.org', 'na.finalfantasyxiv.com', 'raw.githubusercontent.com', 'srankguide.carrd.co', 'support.na.square-enix.com', 'universalis.app', 'www.akhmorning.com', 'www.finalfantasyxiv.com', 'www.garlandtools.org', 'www.partake.gg', 'www.python.org', 'xivgear.app']
- PARTS 20 ['addons', 'airship', 'eventMarkers', 'fashion', 'fishing', 'fishline', 'island', 'islandAgenda', 'islandobjects', 'leves', 'mapMarkers', 'messages', 'monsterEvents', 'npctrade', 'purify', 'questMarkers', 'retainer', 'statuses', 'synthesis', 'workshop']
- NATIVE 39
- PRAGMAS {}
- NOWARN []
- TESTFILES 427

- Tests: 1483 passing in the managed suite (427 test files).
- LANGS = language keys per file; SETTINGS = public get/set properties of Configuration.cs; HOSTS = host names written in the code (links and requests
  together; step 5 sorts them); PARTS = parts of the developer-mode capture; NATIVE = native types read (FFXIVClientStructs); no warning disabled
  (no NoWarn, no pragma).
