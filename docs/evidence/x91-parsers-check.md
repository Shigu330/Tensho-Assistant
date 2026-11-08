# X-91 — last check of the parsers (2026-11-09, version 0.971)

User request (« a faire » document): check one last time that every parser does what was planned. Method: every parser item of the plan
(GLOBAL-PLAN rows X-73 to X-75.8, X-76 2c list in `docs/evidence/x76-2bc-plan.md`) compared with the code and the versions that coded it.

| Planned | Status | Where |
|---|---|---|
| X-73.1 to X-73.18: places, FATEs, desynthesis, ventures, chests, FATE rewards, monster drops, item from item, fishing, Fashion Report, submersibles, blue magic, beastmaster, festivals, vistas, vendors, lures, transitions, bites, reduction, quest hand-ins, spearfishing | coded (0.855–0.896) | `Discoveries/ItemDiscoveryWatcher.Dalamud.cs`, kinds 1 to 28 |
| X-73.12 rest: island animals | coded (0.901, island animals on the map; 0.932 size and reward from MJIAnimals) | `Maps/MapLiveReader.Dalamud.cs` |
| X-73.12 rest: vendors without a game place | coded (0.872, NpcPlace) | kind 19 |
| X-73.12 rest: comparison of the player's retainers | left to Universalis (offers seen carry no retainer) | decision in the row |
| X-75.5 rest: FATE places seen on the S rank page | **missing, coded in 0.971** (FateSeen facts of the condition's FATE, with their map coordinates) | `MainWindow.SRankConditions.cs` |
| X-75.7: spearfishing, airships, submarine results, overmeld rates, mail, Fashion Report preview, garden crosses, collectability, bountiful | coded (0.881 probes; 0.896 spearfishing; 0.899 overmeld rates match the sheets; 0.903 letters; 0.919 airship route; 0.934 submarine sectors) | see the GLOBAL-PLAN rows X-81 to X-84 |
| X-75.7: open shop's id | not probed (structure unknown), left | X-75.7 row |
| X-75.8: PaissaDB, hook-only data, Mog Station, future dates, worlds never visited | stay with the community sources (Dalamud rule or no game data) | X-75.8 row |
| X-76 P1 to P9 | coded (0.911–0.936) | `docs/evidence/x76-2bc-plan.md` « Done » |
| X-76 P9 retainers (ClassJob, Level, MarketExpire) | waits for an in-game reading | reminder table « Liste des servants » |
| X-76 P10 duty-end rewards (DutyCompleted, ChestObjectId) | chest counts coded (0.935); rewards wait for an in-game reading | reminder table « Fin d'une mission » |
| X-76 P6 swimbait (cast flag 32) | waits for an in-game reading | reminder table « Pêche avec un leurre nageur » |
| X-85 / X-93 fishing additions: FishBiteBait, Intuition, voyage visits, monster kills, duty chests, spectral current, fish points | coded (0.933–0.962) | kinds 29 to 36 |
| Deferred list of x76-2bc-plan.md (custom delivery request, island stocks, Fashion Report best score, accepted-now quest markers, daily quests, GC supply, bountiful checks, field content) | deferred by decision (unproven fields or the user's request for Occult Crescent) | x76-2bc-plan.md « Deferred parser items » |

Result: one planned item was not coded (FATE places on the S rank page), coded in 0.971; everything else is coded, waits for a named
in-game reading (reminder table), or was left on purpose with its reason written in the plan.
