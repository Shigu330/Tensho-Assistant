# Roadmap of the 17 plugins

> *Traduction française plus bas.*

State on 2026-11-08 (version 0.865): the work plan now lives in the [global plan](GLOBAL-PLAN.md), and the history of each version in
[CHANGELOG.md](../../CHANGELOG.md). This file keeps the authoritative list of the 17 reference plugins and what Tensho covers of each, the
user requests R-01 to R-14, the pages added outside the 17 plugins, the open questions and the decisions taken along the way. The
17 rows stay PARTIAL: no complete in-game validation. Release: the custom repository, see [PUBLISHING](PUBLISHING.md) (no submission to
the official repository, DEC-039).

The step queue a) to h) (2026-11-02 to 2026-11-04) and the foundation notes (2026-10-27 to 2026-10-28) were history; they were removed from
this file on 2026-11-08 and stay readable in git (this file at version 0.865.0, commit `2da535d`).

Authoritative source: the master prompt, §4 (agent file `docs/ai/PROMPT-MAITRE.md`, outside the repository). Every row is kept. State
describes the work; Coverage describes the capacity actually validated. TODO/READY/ACTIVE/BLOCKED/DONE; NOT_STARTED/PARTIAL/FUNCTIONAL/
REPLACEABLE. NEEDS_STUDY is a note of uncertainty, not a removal. No arbitrary percentage.

## Main matrix

The « Work » column was written at 0.315 (2026-11-02) and completed until 0.512; what came later is in the [CHANGELOG](../../CHANGELOG.md)
and the [global plan](GLOBAL-PLAN.md). « Validated » = report or screenshot of the user; the other versions named are to be validated in
game. The goal sheets these rows named (`docs/goals/`) are agent files, outside the repository.

| # | ID | Plugin | Author(s) | Description | State | Coverage | Work | Evidence | Group |
|---|---|---|---|---|---|---|---|---|---|
| 1 | FASHION-001 | Avant-Garde | NeNeppie | Valid gear for the Fashion Report hints | ACTIVE | PARTIAL | Manual directory, flagged pieces, filters/stock/context and provenance tested; buttons on the slots of the game window and dyes of the week validated in game (0.213–0.223); « owned » with armoury chest, glamour dresser and armoire read from the game (0.235–0.240, to validate); « Try on » (Try On window) on right click of any gear (0.310, validated). Score per piece not done: no data (decision of 2026-11-01) | — | C |
| 2 | BLU-001 | Blue Mage Helper | Infi, Sl0nderman, harbingerftw | Blue Mage spellbook: spells, acquisition and progress | ACTIVE | PARTIAL | Native spellbook, numbers distinct from ids, confirmed acquisitions; build/tests green, runtime to confirm | — | C |
| 3 | CHAR-001 | Character Panel Refined | Kouzukii | Useful character statistics | ACTIVE | PARTIAL | Native attributes copied and invalidated; real combat values (`Character/StatFormulas.cs`: the game's ParamGrow, community formulas, main modifier taken as data from Character Panel Refined, MIT) in a panel beside the game's Character window, validated in game (0.182–0.184); taken up for the plan in the Gear page (R-11, 0.310, validated) | [ParamGrow probe](../evidence/0.182.0-paramgrow-probe.log) | D |
| 4 | COLL-001 | Collections | Seventh | Discovery, progress and interactions with collections | ACTIVE | PARTIAL | Mounts/minions/emotes/cards/orchestrion/titles/achievements; categories/sheets, rewards and context, three-state acquisition for dedicated lists; obtained/not obtained state in the item tooltip, right click to the collection and « How to get it » (vendors, exchanges, quests, achievements, crafting, gathering, then Garland Tools and the wiki) validated in game (0.146). ✓/✗ badge on every collection item icon (0.275) and cards/scrolls in « Not obtained only » (0.241), to validate; Triple Triad Ex. cards (0.278, validated). Other types and remaining unknown sources to complete | — | B |
| 5 | CRAFT-001 | Craftimizer | Asriel | Craft simulation, assisted macros and suggestions during a craft | ACTIVE | PARTIAL | Recipes/preparation validated; simulation/export/search/sequences, ordinary projection and macro help beside the game's crafting log (selection, attributes, comparison and copies) compiled/tested; ordinary/initial HQ collectables and priority to expectations integrated; copying macros into the game's macros, slot forecast and chaining validated in game (0.191–0.196); adaptive rotation (A-04, 0.509) and the optional Raphael solver (A-03, 0.465) added later; special recipes to complete | — | A |
| 6 | DUTY-001 | Duty Loot Preview | grittyfrog | Preview of duty loot | ACTIVE | PARTIAL | Catalogue and manual lookup, cache/cancellation, provenance and context tested; panel beside the game's duty finder (0.185–0.186, selection validated), loot of recent duties from LuminaSupplemental (0.188–0.190, validated), loot ✓/✗ and roulette (0.207–0.208, validated); cards and scrolls ✓/✗ (R-02, 0.241, to validate); partial data | — | C |
| 7 | CACT-001 | ezMiniCactpot | daemitus, Minmoose, MidoriKami | Best choice at the Mini Cactpot | ACTIVE | PARTIAL | Exact manual solver; opt-in observed mode and recomputation per ticket, owner worker/generation; solution drawn over the game window `LotteryDaily` (0.158), validated in game (report of 2026-11-01) | — | D |
| 8 | GATHER-001 | GatherBuddy | Ottermandias | Gathering and fishing | ACTIVE | PARTIAL | Sources/maps, opt-in MIN/BTN observation, notebook and explicit baits/conditions; local weather and bounded computation of the given criteria. In-game gathering and fishing help panels (0.225–0.233), GatherBuddy fishing data first then Garland (0.227), old GatherBuddy files (0.229, validated), Teamcraft bite times and timer (0.231, validated), fishing alarms with weather (0.253–0.254); ocean fishing calendar and help during the voyage (0.392 and later); the rest to validate | [spot coordinates](../evidence/0.226-fishing-spot-coordinates.md), [Teamcraft bite times](../evidence/0.231-teamcraft-bite-times.md) | A |
| 9 | USE-001 | Item Use | PunishedPineapple | Common uses of an item | ACTIVE | PARTIAL | Tab, opt-in hover and Inventory menu (0.207, validated); recipes/leves/GC/aquarium/orchestrion and quest sources; Uses tab of the item sheet (0.272); Tensho menu of the game's inventory and panel under its tooltip aligned with Tensho (0.256–0.257, to validate); other uses/availability/links to complete | — | C |
| 10 | GP-001 | LazyGatherer | Hiroa | GP optimisation for gathering | ACTIVE | PARTIAL | Manual optimiser with families already paid and limited bonus; observation/copy of resources (tested at the time); Bountiful Yield II bonus prefilled by a cited community rule, shown « estimated » and editable (0.224, to validate); native decoder/regeneration/procs/collectables and runtime to complete | [bountiful base](../evidence/0.224-bountiful-base.md) | D |
| 11 | MACRO-001 | Macro Chain | Caraxi | Running macros one after the other | ACTIVE | PARTIAL | Persistent plan, end-marker protocol, stop/identity and managed tests; plan filled after copying the macros into the game, and chaining, validated in game (0.196, « macro chain works ») | — | D |
| 12 | MAP-001 | Mappy | MidoriKami, harbingerftw | Versatile in-game map | ACTIVE | PARTIAL | Tensho map replacing the game map (M, journal quest, aetherytes): game icons with size/priority per map, quests and leves, FATEs, party, timed gathering/fishing, aether currents, sightseeing (wiki weather), vendors, flag, teleport at the real cost, lists per tab. Validated in game: 0.100–0.156 (including the host map off screen, coloured areas, zoom, Rowena's houses), map variants (0.171–0.175), quest icons (0.211), settings and view cone (0.287–0.289), edge markers and « Remove the flag » (0.297–0.299), menus and tools over the map (0.303–0.306). To validate: R-01 (0.242–0.243), R-03 (0.244), R-04 (0.245), zoom background (0.308). Unknown: marker type of leves, meaning of the quest icon variants | [event icons](../evidence/0.211-event-icons.md) | C |
| 13 | LOOT-001 | Monster Loot Hunter | Sylf | Material sources on monsters | ACTIVE | PARTIAL | Material/monster/zone, sheets, cache/rate/cancellation and context tested; LuminaSupplemental (MobDrop) first (0.190, validated), then the FFXIV wiki and Garland for the places; partial data | — | B |
| 14 | MARKET-001 | Price Insight | Kouzukii | Market price when hovering an item | ACTIVE | PARTIAL | Manual lookup and opt-in hover NQ/HQ/world, attached to the tooltip, stock and cheapest of the data centre validated in game (0.85–0.86); Market page enriched (R-06 step 4: Universalis listings, filters, cost for N units, history, column sort) validated in game (0.270–0.271); fallback on the disk cache if Universalis fails (0.281, to validate) | [Universalis listings](../evidence/0.270-universalis-listings.md) | B |
| 15 | QUEST-001 | QuestTracker | isaiahcat, keifufu | Quest progress and completion | ACTIVE | PARTIAL | Sheets/links/levels/zones/map, conditional native rewards and reverse Item → quest index outside the draw; observed progress joined to an item's sources (0.76, validated); quest types of the journal sections, percentages, start cities and options not taken (0.283–0.286, validated). Currencies/ranks not proven; availability to complete | [journal sections](../evidence/0.283-quest-journal-sections.md), [start cities](../evidence/0.285-quest-start-cities.md), [alternatives](../evidence/0.286-quest-alternatives.md) | B |
| 16 | SORT-001 | SortaKinda | MidoriKami | Inventory customisation, rules, preview and application | ACTIVE | PARTIAL | Reusable profiles and templates, multiple criteria, protected/filtered zones, filling in both directions and visual application after direct capture; native sort validated in game (2026-10-30). Armoury chest read as a storage (0.235, to validate) but not sorted: armoury sort and automatic sort left for later (I-14) | — | A |
| 17 | TRIAD-001 | TriadBuddy | MgAl2O4 | Triple Triad solver | ACTIVE | PARTIAL | Card list/deck/manual solver tested; TRIAD-003 finished: advice drawn during a match (0.160), % per deck and deck generation at the challenge window (0.161), Tensho decks (0.162) validated in game; card list (menus, values, sort, estimated score, Ex. cards) validated (0.265–0.269, 0.276–0.279). Native rules not covered | [Ex. cards](../evidence/0.278-triad-ex-cards.md) | C |

The domain IDs name the overall capacity, cut into small goals when it is started. CRAFT-001 starts with the catalogue, then simulation,
macros and suggestions; GATHER-001 with the places, then time windows/weather/fishing; SORT-001 with the preview before any native
application. The other capacities follow the same principle, without declaring the plugin covered from the first slice.

## User requests R-01 to R-14 (state at 0.512)

Detail of the requests: [USER-REQUESTS](USER-REQUESTS.md). « Validated » = report or screenshot of the user; otherwise, to validate in
game. The plans R-06 and R-11 are agent files (`docs/goals/`), outside the repository.

| No. | Subject | Done | Validated in game | Left |
|---|---|---|---|---|
| R-01 | Map: journal quest target that stayed | 0.242 (removed on right click), 0.243 (removed when the quest is no longer active) | no | — |
| R-02 | Duty loot: cards and scrolls ✓/✗ and « Not obtained » filter | 0.241 | no | — |
| R-03 | Map: « Triple Triad » tab on right click (NPCs of the zone) | 0.244 | no | — |
| R-04 | Map: right click on an icon = its own menu | 0.245; mouse button of the icon settings 0.298 | 0.297–0.299 (« Remove the flag » in these menus, mouse button) | — |
| R-05 | Window order | closed without code (2026-11-01, [evidence](../evidence/0.246-r05-window-order.md)) | — | — |
| R-06 | Interface overhaul | steps 1 (0.247–0.263), 2 (0.264), 4 (0.270–0.271); 3 mostly (item sheet 0.272, Storage 0.280); 5 in part (↺ 0.249–0.262, « Reset everything to default » 0.258, tooltips on every setting 0.287, map settings 0.288–0.289); 6: lightness (0.290–0.292) and the game's title font (0.293–0.294) | 0.265–0.271, 0.276–0.279, 0.287–0.294 | useful missing settings (step 5, point d); game frames and textures (step 6, removed in 0.510, DEC-033); `FilteredTable` removed from the plan (DEC-023) |
| R-07 | Free company | page and expeditions 0.307, best routes 0.309, builds 0.315; DEC-023 follow-up done: flagged sectors 0.334, market prices and tiers 0.335, airships 0.336–0.337; history 0.379 | no | to validate in game |
| R-08 | Reminders | page 0.300, icon and tooltip in the server bar 0.300–0.302, expeditions 0.307, squadron 0.333; then step g (M-01 to M-05, N-01 to N-03) | no | Ehcatl Gliders left out (nothing readable, 0.334); to validate in game |
| R-09 | Seasonal events | Events page (0.342): running, quests, shops, names | 0.342 | to validate; no future dates (no reliable source) |
| R-10 | Cache audit | 0.246 ([audit](../evidence/0.246-cache-audit.md)), checked again 0.281 (« Never » duration) | — | — |
| R-11 | Gear | step 1 (0.310), glamour (0.311), materia and caps (0.312–0.313), sources and ownership (0.314), vessel builds (0.315) | 0.310–0.314 | 0.315 and 0.340 to validate; step 4: chocobo barding DONE 0.340, Chocobo page 0.341 (squadron left out, recommended default, decision at the end) |
| R-12 | Discord webhook for alarms ([request](USER-REQUESTS.md)) | 0.319 | no | to validate in game; used later by C-10, I-02, M-01 |
| R-13 | Full « Credits » page in the plugin ([request](USER-REQUESTS.md)) | 0.320 | no | to validate in game |
| R-14 | Resistance to FFXIV updates, in the logic of the code ([request](USER-REQUESTS.md)) | rule for new code (0.319); audit and fixes 0.452–0.454, 0.491–0.492, 0.507; self-check 0.494–0.496 | by the self-check, for what it reads | — |

## Pages outside the 17 plugins

The navigation of the code today (`MainWindow.Pages.cs`, version 0.865) has these pages besides those of the 17 plugins: Storage, Lexicon;
Ocean fishing, Island sanctuary, Custom deliveries; Weather, FATEs, Alarms, Reminders, Garden, Free company, NPC vendors, Discoveries;
Events, Masked Carnivale, Relics, Allied societies, Grand Companies, Special deliveries, Eureka and Bozja, Deep dungeons, Gear, Chocobo,
Guildleves, Hunting log, Beastmaster; Help, Credits, My tools. The Character page was removed in 0.341 (the Gear page shows the same real
values; the panel beside the game's Character window stays).

## To decide / to review

Points still open, as noted at 0.512: each waits for an in-game record or is settled in a step of the plan. No value is guessed meanwhile.
ONBOARD-001 and TEMPLATES-001 are agent goal sheets, outside the repository.

| Point | Source | Finding | Record or step expected |
|---|---|---|---|
| Getting started, help, videos, issues | ONBOARD-001 | The 8 slices coded (0.502 to 0.506), DEC-031 | In-game validation; a release for the video list |
| Community templates | TEMPLATES-001, [DEC-029](DECISIONS.md) | Finished on the code side in 0.476 | In-game validation; gallery visible after a release |
| « Cost ≤ capacity » rule below rank 50 | [0.315-vessel-builds](../evidence/0.315-vessel-builds.md), « Cost ≤ capacity » | Rule of the community tools. Consistent with 11 published airship builds; not checked in game below rank 50. | In-game record on a vessel below rank 50. |
| Marker type of leves | CHANGELOG 0.298 | The log shows types 0, 3 (quests), 4 and 6; no record says which one is a leve. | In-game record: leve accepted, map open, developer mode. |
| Flag icon 60561 | `MainWindow.MapView.cs` (`FlagIcon`); CHANGELOG 0.102 (« assumed »); map audit 0.328 | The flag icon id is not proven in docs/evidence (placing the flag is validated in game). | In-game record: flag placed, `AgentMap.FlagMapMarkers[0].MapMarker.IconId` read on the framework (probe), log in docs/evidence. |
| Quest icons 71141 / 71151 | [BACKLOG](BACKLOG.md); [0.211-event-icons](../evidence/0.211-event-icons.md) | The meaning of the variants is not in the game data: the settings show « variant n ». | In-game record, otherwise keep « variant n ». |
| Estimated Bountiful bonus (0.224) | [BACKLOG](BACKLOG.md), « Review of open points »; [0.224-bountiful-base](../evidence/0.224-bountiful-base.md) | Unofficial community rule, shown « estimated » and editable. | To compare in game with the real bonus. |
| Materia: piece on several slots | [0.312-materia](../evidence/0.312-materia.md), « Not checked in game » | A piece covering several slots takes the body's percentage. | In-game record (cap shown against the game). |
| Useful missing settings | R-06 step 5; audit of point d) (2026-11-02) | DONE 0.327 (re-read 2026-11-03): number of search results, market scope remembered, Triple Triad helpers, automatic computation of the crafting log, hover hidden during a duty, chat line silenced during a duty, opening page, parts of the hover panel. | In-game tests. |

### Decisions left for the end (agent's provisional choice, to confirm)

Batch of 2026-11-03 ([DEC-026](DECISIONS.md)): « Monsters on hover » removed (0.447); inventory 250 ms: DEC-004 kept, sort editor kept on
screen (0.447); line endings: nothing to normalize; GitHub: protections, Dependabot and CI build turned on (0.447); memoquartz: 80 %
confirmed; choices of g) (G-PROPOSALS) confirmed as a block. The other rows remain to confirm during the in-game tests.

| Point | Provisional choice | Where to change it |
|---|---|---|
| R-12 Discord: defaults | Sending off; every category ticked; message « ⏰ [label] : dans [minutes] min ([time]) »; name « Tensho »; character name not sent. Since 0.778 (X-23), each new alarm has its Discord box off. | Settings › Alarms › Discord |
| R-12 Discord: rate | Two seconds at least between two messages, a queue of 20 messages (the oldest dropped past that), three tries at most on « 429 ». | `Alarms/DiscordNotifier.cs` |
| Fashion Report: source turned off (0.321) | No more download; a recent copy kept on disk stays shown. | `Fashion/FashionDataService.cs` |
| « Reset everything to default » (0.321) | The folded groups of the navigation are kept (interface state); the map settings, including the icon sizes and priorities per map, are reset. | `Configuration.cs` (ConfigurationReset) |
| New order of Release.ps1 (0.323) | Checks, tag and GitHub release with the zip, then main (repo.json) last; resumes if the tag exists without a release. | `scripts/Release.ps1` |
| GitHub Actions and Dependabot (0.323) | **Turned on in 0.447** (DEC-026); job building like Dalamud added in 0.512. | `.github/`, [publish/github](../../publish/github/README.md) |
| Protections of the GitHub repository (0.323) | **Turned on in 0.447** (DEC-026): force push and deletion forbidden on main and Devloppement, Stable-* tags protected. | GitHub settings |
| Line endings eol=lf (0.323) | **Nothing to normalize** (DEC-026). | `.gitattributes` |
| Code of conduct, CODEOWNERS, pre-push hook (0.323) | Not created (code of conduct, CODEOWNERS); hook ready, inactive (`git config core.hooksPath .githooks` to turn it on). | `.githooks/pre-push` |
| R-13 Credits: partial settings | Settled provisionally in 0.324 (recommended choice applied): the Garland and wiki switches cut their whole source, Universalis has its own switch (on by default) with « price on hover » below it; copies kept stay shown. To confirm or change at the end. | `Configuration.cs`, services of `Plugin.cs` |
| R-12 Discord: alarms written in the game's list | They ring in the game and are still sent to Discord. | `Alarms/AlarmService.Dalamud.cs` (Ring) |
| Tooltip test (0.325) | Ratchet (a list of exceptions that only shrinks) and regular expressions, without dependency; no zero tolerance and no Roslyn. | `tests/TenshoAssistant.Tests/TooltipRatchet325Tests.cs`, `tooltip-baseline.txt` |
| Step g): proposals and provisional choices | Kept along g) in [G-PROPOSALS](G-PROPOSALS.md): provisional choices applied, ideas found while coding, gaps left for an in-game session. | [G-PROPOSALS](G-PROPOSALS.md) |
| Currency alerts (I-02, 0.344) | Agent's provisional choices: threshold 80 % of the cap for all; without a Grand Company, no seal alert; a currency already above at load is listed without a message; no reminder at login. Crystals, leves and gil not followed. To confirm or change. | `Inventory/Currencies.cs`, `MainWindow.Currencies.cs` |
| Refresh key of the hovered price (I-01, 0.343) | Agent's provisional choice: Ctrl+Alt during the hover (the F keys and letters are taken by the game), adjustable (can be turned off). | `MainWindow.MarketHover.cs`, `Items/ItemHoverReader.Dalamud.cs` |
| Map, items, crafting and gathering, other tools: what to add (step e) | Proposals of the audit of 2026-11-02, partly done since; each proposal has its « State » line. | [MAP-PROPOSALS](MAP-PROPOSALS.md), [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md), [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md), [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md) |
| Inventory cleared 250 ms at each change (audit 0.329, defect 7) | Not fixed: the fix contradicts DEC-004 (« atomic invalidation, coalesced collection 250 ms ») and its tests. Recommended: keep DEC-004 and let the sort editor keep the last complete list (never for the preview or the sort). | `MainWindow.SortPreview.cs`; DEC-004 |
| Frame of the game windows (0.332) | **Removed in 0.510** (DEC-033, user's decision). | — |
| Squadron in R-11 step 4 (probe 0.332) | A squadron member's gear is only an appearance (glamour, dyes), without stats: nothing to plan. Applied in 0.340 (barding done, squadron not planned); to confirm at the end. | [0.332-squadron-probe](../evidence/0.332-squadron-probe.md) |
| « Monsters on hover » without effect (noted in 0.327) | **Removed in 0.447** (DEC-026). | — |
| Immediate deletions (0.325) | Two-click confirmation for « Delete all » of an alarm group only; elsewhere (alarm, deck, storage template, crafting sequence), a tooltip says the deletion is immediate. | `MainWindow.Alarms.cs` |

### Decided on 2026-11-02 ([DEC-023](DECISIONS.md))

| Point | Decision |
|---|---|
| Submarine loot threshold table | Keep the SubmarineTracker table; on the 19 sectors where it is below every record, the tier carries the note « not confirmed by the records ». |
| Value of submarine routes | Add the market price (Universalis) beside the NPC price, and the surveillance / retrieval tiers if the source splits the records by tier. |
| Airship unlock tree | LuminaSupplemental data (GPL-3.0) embedded in the DLL, with the licence notice in the file. |
| Airship route planner | Distance and duration only; the loot is shown « unknown », never estimated. |
| Materia: overmelding rate | Read the success rate from the game data and show it per advanced slot. |
| Character page removed (0.341, user request) | The Gear page shows the same real values. The panel beside the game's Character window is kept (decided by the user on 2026-11-02). |
| Other gear (R-11 step 4) | Chocobo barding: DONE 0.340 ([evidence](../evidence/0.340-chocobo-barding.md)). Squadron: appearance only, left out. |
| Reminders: squadron, Ehcatl Gliders | Probe a readable time in the game's memory (FFXIVClientStructs); otherwise leave it aside. |
| Seasonal events (R-09) | DONE 0.342: game data only (running events, quests, NPCs and shops of the layers); names from the guide NPC (French) or LuminaSupplemental; no future dates. |
| Game frames and textures (ULD) | Frame removed in 0.510 (DEC-033). |
| D17 publication | Replaced by DEC-039 (2026-11-05): no submission, the custom repository only. |
| German and Japanese translations | Kept in the package (partial, English fallback). |
| Tone of the French texts | « vous » everywhere (done 0.317, automatic test). |
| Ocean fishing | Calendar (« Ocean fishing » page) and help during the voyage; game data (IKDRoute, IKDRouteTable, IKDSpot). |
| Additions to the 17 tools | Taken into plan c): adaptive rotation (A-04), armoury and automatic sort (I-14, later). |
| EUPL-1.2 licence | Compatible with GPL v3 through its clause (notice required): Tourist, Glamaholic, Quest Map, Better Party Finder (DEC-025). |
| `FilteredTable` template | Removed from the plan (agent's choice, rule « no premature abstraction »): to create at the first real repeated need. |

## In-game tests

The current list: the boxes of [TESTING.md](TESTING.md). The guided in-game campaign of the whole plugin is phase 6 of the
[global plan](GLOBAL-PLAN.md). Records still asked for (data unknown, see « To decide »): marker type of an accepted leve; quest icon
variants 71141 / 71151.

## Complementary domain

Combat/ACT/IINACT: removed from the project (user decision 2026-10-31).

---

# Feuille de route des 17 plugins

État au 2026-11-08 (version 0.865) : le plan de travail vit maintenant dans le [plan global](GLOBAL-PLAN.md), et l'historique de chaque
version dans [CHANGELOG.md](../../CHANGELOG.md). Ce fichier garde la liste qui fait foi des 17 plugins de référence et ce que Tensho
couvre de chacun, les demandes de l'utilisateur R-01 à R-14, les pages ajoutées hors des 17 plugins, les questions ouvertes et les
décisions prises en route. Les 17 lignes restent PARTIAL : aucune validation en jeu complète. Diffusion : le dépôt personnalisé, voir
[PUBLISHING](PUBLISHING.md) (pas de soumission au dépôt officiel, DEC-039).

La file d'étapes a) à h) (2026-11-02 au 2026-11-04) et les notes de fondation (2026-10-27 au 2026-10-28) étaient de l'historique ; elles
ont été retirées de ce fichier le 2026-11-08 et restent lisibles dans git (ce fichier à la version 0.865.0, commit `2da535d`).

Source qui fait foi : le prompt maître, §4 (fichier de l'agent `docs/ai/PROMPT-MAITRE.md`, hors du dépôt). Toutes les lignes sont
conservées. State décrit le travail ; Coverage décrit la capacité réellement validée. TODO/READY/ACTIVE/BLOCKED/DONE ;
NOT_STARTED/PARTIAL/FUNCTIONAL/REPLACEABLE. NEEDS_STUDY est une note d'incertitude, pas une suppression. Aucun pourcentage arbitraire.

## Matrice principale

La colonne « Travail » a été écrite au 0.315 (2026-11-02) et complétée jusqu'au 0.512 ; la suite est dans le
[CHANGELOG](../../CHANGELOG.md) et le [plan global](GLOBAL-PLAN.md). « Validé » = rapport ou capture de l'utilisateur ; les autres
versions citées sont à valider en jeu. Les fiches d'objectifs que citaient ces lignes (`docs/goals/`) sont des fichiers de l'agent, hors
du dépôt.

| # | ID | Plugin | Auteur(s) | Description | State | Coverage | Travail | Preuves | Groupe |
|---|---|---|---|---|---|---|---|---|---|
| 1 | FASHION-001 | Avant-Garde | NeNeppie | Équipements valides associés aux indices de la Revue de mode | ACTIVE | PARTIAL | Répertoire manuel, pièces signalées, filtres/stock/contexte et provenance testés ; boutons sur les cases de la fenêtre du jeu et teintures de la semaine validés en jeu (0.213–0.223) ; « possédé » avec arsenal, coffre à apparence et armoire lus dans le jeu (0.235–0.240, à valider) ; « Essayer » (cabine d'essayage) au clic droit de tout équipement (0.310, validé). Score par pièce non fait : aucune donnée (décision du 2026-11-01) | — | C |
| 2 | BLU-001 | Blue Mage Helper | Infi, Sl0nderman, harbingerftw | Grimoire du mage bleu : sorts, acquisition et progression | ACTIVE | PARTIAL | Grimoire natif, numéros distincts des identifiants, acquisitions confirmées ; compilation/tests verts, rendu en jeu à confirmer | — | C |
| 3 | CHAR-001 | Character Panel Refined | Kouzukii | Statistiques utiles du personnage | ACTIVE | PARTIAL | Attributs natifs copiés et invalidés ; valeurs réelles de combat (`Character/StatFormulas.cs` : ParamGrow du jeu, formules de la communauté, modificateur principal repris comme données de Character Panel Refined, MIT) dans un panneau à côté de la fenêtre Personnage du jeu, validées en jeu (0.182–0.184) ; reprises pour le plan dans la page Équipement (R-11, 0.310, validée) | [sonde ParamGrow](../evidence/0.182.0-paramgrow-probe.log) | D |
| 4 | COLL-001 | Collections | Seventh | Découverte, progression et interactions avec les collections | ACTIVE | PARTIAL | Montures/mascottes/emotes/cartes/orchestrion/titres/succès ; catégories/fiches, récompenses et contexte, acquisition à trois états pour les listes dédiées ; état obtenu/non obtenu dans l'infobulle des objets, clic droit vers la collection et « Comment l'obtenir » (marchands, échanges, quêtes, succès, artisanat, récolte, puis Garland Tools et le wiki) validés en jeu (0.146). Pastille ✓/✗ sur toute icône d'objet de collection (0.275) et cartes/rouleaux dans « Non obtenus seulement » (0.241), à valider ; cartes Ex. de Triple Triade (0.278, validé). Autres types et sources inconnues restantes à compléter | — | B |
| 5 | CRAFT-001 | Craftimizer | Asriel | Simulation d'artisanat, macros assistées et suggestions pendant la fabrication | ACTIVE | PARTIAL | Recettes/préparation validées ; simulation/export/recherche/séquences, projection ordinaire et aide macro près du carnet du jeu (sélection, attributs, comparaison et copies) compilées/testées ; objets de collection ordinaires/HQ initial et priorité aux attentes intégrés ; copie des macros dans les macros du jeu, prévision des emplacements et enchaînement validés en jeu (0.191–0.196) ; rotation adaptative (A-04, 0.509) et solveur Raphael facultatif (A-03, 0.465) ajoutés ensuite ; recettes spéciales à compléter | — | A |
| 6 | DUTY-001 | Duty Loot Preview | grittyfrog | Aperçu du butin des contenus | ACTIVE | PARTIAL | Catalogue et consultation manuelle, cache/annulation, provenance et contexte testés ; panneau à côté de l'outil de mission du jeu (0.185–0.186, sélection validée), butin des contenus récents par LuminaSupplemental (0.188–0.190, validé), butin ✓/✗ et mission aléatoire (0.207–0.208, validés) ; cartes et rouleaux ✓/✗ (R-02, 0.241, à valider) ; données partielles | — | C |
| 7 | CACT-001 | ezMiniCactpot | daemitus, Minmoose, MidoriKami | Meilleur choix au Mini Cactpot | ACTIVE | PARTIAL | Solveur manuel exact ; mode observé sur option et recalcul par ticket, worker propriétaire/génération ; solution superposée à la fenêtre du jeu `LotteryDaily` (0.158), validée en jeu (rapport du 2026-11-01) | — | D |
| 8 | GATHER-001 | GatherBuddy | Ottermandias | Récolte et pêche | ACTIVE | PARTIAL | Sources/cartes, observation MIN/BTN sur option, carnet et appâts/conditions explicites ; météo localisée et calcul borné des critères renseignés. Panneaux d'aide à la récolte et à la pêche en jeu (0.225–0.233), données de pêche de GatherBuddy d'abord puis Garland (0.227), anciens fichiers GatherBuddy (0.229, validé), temps de touche Teamcraft et chrono (0.231, validé), alarmes de pêche avec météo (0.253–0.254) ; calendrier de la pêche en mer et aide pendant la traversée (0.392 et suivantes) ; le reste à valider | [coordonnées des lieux](../evidence/0.226-fishing-spot-coordinates.md), [temps de touche Teamcraft](../evidence/0.231-teamcraft-bite-times.md) | A |
| 9 | USE-001 | Item Use | PunishedPineapple | Usages courants d'un objet | ACTIVE | PARTIAL | Onglet, survol sur option et menu Inventory (0.207, validé) ; recettes/mandats/GC/aquarium/orchestrion et sources de quêtes ; onglet Usages de la fiche objet (0.272) ; menu Tensho de l'inventaire du jeu et panneau sous son infobulle alignés sur Tensho (0.256–0.257, à valider) ; autres usages/disponibilité/liens à compléter | — | C |
| 10 | GP-001 | LazyGatherer | Hiroa | Optimisation des PR de récolte | ACTIVE | PARTIAL | Optimiseur manuel avec familles déjà payées et bonus limité ; observation/copie des ressources (testés à l'époque) ; bonus de Récolte abondante II prérempli par une règle de la communauté citée, affiché « estimé » et modifiable (0.224, à valider) ; décodeur natif/régénération/procs/objets de collection et rendu en jeu à compléter | [base d'Abondance](../evidence/0.224-bountiful-base.md) | D |
| 11 | MACRO-001 | Macro Chain | Caraxi | Exécution de macros successives | ACTIVE | PARTIAL | Plan persistant, protocole de marqueur de fin, arrêt/identité et tests managés ; plan rempli après copie des macros dans le jeu, et enchaînement, validés en jeu (0.196, « macro chain fonctionne ») | — | D |
| 12 | MAP-001 | Mappy | MidoriKami, harbingerftw | Carte en jeu polyvalente | ACTIVE | PARTIAL | Carte Tensho remplaçant la carte du jeu (M, quête du journal, téléporteurs) : icônes du jeu à taille/priorité par carte, quêtes et mandats, ALEA, groupe, récolte/pêche à horaires, courants, exploration (météo du wiki), marchands, drapeau, téléportation au coût réel, listes par onglet. Validé en jeu : 0.100–0.156 (dont carte hôte hors écran, zones colorées, zoom, commises de Rowena), variantes de carte (0.171–0.175), icônes de quêtes (0.211), réglages et cône de vision (0.287–0.289), repères au bord de la carte et « Retirer le drapeau » (0.297–0.299), menus et outils par-dessus la carte (0.303–0.306). À valider : R-01 (0.242–0.243), R-03 (0.244), R-04 (0.245), fond du zoom (0.308). Inconnus : type de marqueur des mandats, sens des variantes d'icônes de quête | [icônes d'événements](../evidence/0.211-event-icons.md) | C |
| 13 | LOOT-001 | Monster Loot Hunter | Sylf | Sources de matériaux sur les monstres | ACTIVE | PARTIAL | Matériau/monstre/zone, fiches, cache/cadence/annulation et contexte testés ; LuminaSupplemental (MobDrop) en tête (0.190, validé), puis le wiki FFXIV et Garland pour les lieux ; données partielles | — | B |
| 14 | MARKET-001 | Price Insight | Kouzukii | Prix du marché au survol d'un objet | ACTIVE | PARTIAL | Consultation manuelle et survol sur option NQ/HQ/monde, accroché à l'infobulle, stock et moins cher du centre de données validés en jeu (0.85–0.86) ; page Marché enrichie (R-06 étape 4 : annonces Universalis, filtres, coût pour N unités, historique, tri des colonnes) validée en jeu (0.270–0.271) ; repli sur le cache disque si Universalis échoue (0.281, à valider) | [annonces Universalis](../evidence/0.270-universalis-listings.md) | B |
| 15 | QUEST-001 | QuestTracker | isaiahcat, keifufu | Progression et complétion des quêtes | ACTIVE | PARTIAL | Fiches/liens/niveaux/zones/carte, récompenses natives conditionnelles et index inverse Objet → quête hors du dessin ; progression observée jointe aux sources d'un objet (0.76, validé) ; types de quêtes des sections du journal, pourcentages, cités de départ et options non prises (0.283–0.286, validés). Monnaies/rangs non prouvés ; disponibilité à compléter | [sections du journal](../evidence/0.283-quest-journal-sections.md), [cités de départ](../evidence/0.285-quest-start-cities.md), [options](../evidence/0.286-quest-alternatives.md) | B |
| 16 | SORT-001 | SortaKinda | MidoriKami | Personnalisation de l'inventaire, règles, aperçu et application | ACTIVE | PARTIAL | Profils et modèles réutilisables, critères multiples, zones protégées/filtrées, remplissage dans les deux sens et application visuelle après capture directe ; tri natif validé en jeu (2026-10-30). Arsenal lu comme rangement (0.235, à valider) mais pas trié : tri de l'arsenal et tri automatique laissés pour plus tard (I-14) | — | A |
| 17 | TRIAD-001 | TriadBuddy | MgAl2O4 | Solveur de Triple Triade | ACTIVE | PARTIAL | Carnet/deck/solveur manuel testés ; TRIAD-003 terminé : conseil superposé en match (0.160), % par deck et génération de deck à la fenêtre de défi (0.161), decks Tensho (0.162) validés en jeu ; carnet (menus, valeurs, tri, score estimé, cartes Ex.) validé (0.265–0.269, 0.276–0.279). Règles natives non couvertes | [cartes Ex.](../evidence/0.278-triad-ex-cards.md) | C |

Les ID métier désignent la capacité globale, découpée en petits objectifs au moment de l'activation. CRAFT-001 commence par le
catalogue, puis simulation, macros et suggestions ; GATHER-001 par les lieux, puis horaires/météo/pêche ; SORT-001 par l'aperçu avant
toute application native. Les autres capacités suivent le même principe, sans déclarer le plugin couvert dès la première tranche.

## Demandes utilisateur R-01 à R-14 (état au 0.512)

Détail des demandes : [USER-REQUESTS](USER-REQUESTS.md). « Validé » = rapport ou capture de l'utilisateur ; sinon, à valider en jeu. Les
plans R-06 et R-11 sont des fichiers de l'agent (`docs/goals/`), hors du dépôt.

| N° | Sujet | Fait | Validé en jeu | Reste |
|---|---|---|---|---|
| R-01 | Carte : cible de quête du journal qui restait | 0.242 (retrait au clic droit), 0.243 (retrait quand la quête n'est plus active) | non | — |
| R-02 | Butin des contenus : cartes et rouleaux ✓/✗ et filtre « Non obtenus » | 0.241 | non | — |
| R-03 | Carte : onglet « Triple Triad » au clic droit (PNJ de la zone) | 0.244 | non | — |
| R-04 | Carte : clic droit sur une icône = son propre menu | 0.245 ; bouton souris des réglages d'icônes 0.298 | 0.297–0.299 (« Retirer le drapeau » dans ces menus, bouton souris) | — |
| R-05 | Ordre des fenêtres | clos sans code (2026-11-01, [preuve](../evidence/0.246-r05-window-order.md)) | — | — |
| R-06 | Refonte de l'interface | étapes 1 (0.247–0.263), 2 (0.264), 4 (0.270–0.271) ; 3 pour l'essentiel (fiche objet 0.272, Rangements 0.280) ; 5 en partie (↺ 0.249–0.262, « Tout remettre par défaut » 0.258, infobulles de tous les réglages 0.287, réglages de carte 0.288–0.289) ; 6 : légèreté (0.290–0.292) et police de titre du jeu (0.293–0.294) | 0.265–0.271, 0.276–0.279, 0.287–0.294 | réglages utiles manquants (étape 5, point d) ; cadres et textures du jeu (étape 6, retirés en 0.510, DEC-033) ; `FilteredTable` retiré du plan (DEC-023) |
| R-07 | Compagnie libre | page et expéditions 0.307, meilleurs trajets 0.309, montages 0.315 ; suite DEC-023 faite : secteurs signalés 0.334, prix du marché et paliers 0.335, dirigeables 0.336–0.337 ; historique 0.379 | non | à valider en jeu |
| R-08 | Rappels | page 0.300, icône et infobulle dans la barre du serveur 0.300–0.302, expéditions 0.307, escouade 0.333 ; puis étape g (M-01 à M-05, N-01 à N-03) | non | Planeurs d'Ehcatl laissés (rien de lisible, 0.334) ; à valider en jeu |
| R-09 | Événements saisonniers | page Événements (0.342) : en cours, quêtes, boutiques, noms | 0.342 | à valider ; pas de dates à venir (aucune source fiable) |
| R-10 | Audit des caches | 0.246 ([audit](../evidence/0.246-cache-audit.md)), revérifié 0.281 (durée « Jamais ») | — | — |
| R-11 | Équipement | étape 1 (0.310), mirage (0.311), matérias et plafonds (0.312–0.313), sources et possession (0.314), montages de vaisseaux (0.315) | 0.310–0.314 | 0.315 et 0.340 à valider ; étape 4 : barde de chocobo FAITE 0.340, page Chocobo 0.341 (escouade retirée, défaut recommandé, décision de la fin) |
| R-12 | Webhook Discord pour les alarmes ([demande](USER-REQUESTS.md)) | 0.319 | non | à valider en jeu ; utilisé ensuite par C-10, I-02, M-01 |
| R-13 | Page « Crédits » complète dans le plugin ([demande](USER-REQUESTS.md)) | 0.320 | non | à valider en jeu |
| R-14 | Résistance aux mises à jour de FF14, dans la logique du code ([demande](USER-REQUESTS.md)) | règle pour le nouveau code (0.319) ; audit et corrections 0.452–0.454, 0.491–0.492, 0.507 ; auto-contrôle 0.494–0.496 | par l'auto-contrôle, pour ce qu'il lit | — |

## Pages hors des 17 plugins

La navigation du code aujourd'hui (`MainWindow.Pages.cs`, version 0.865) a ces pages en plus de celles des 17 plugins : Rangements,
Lexique ; Pêche en mer, Félicité insulaire, Commandes spéciales ; Météo, ALEA, Alarmes, Rappels, Jardinage, Compagnie libre, PNJ vendeurs,
Découvertes ; Événements, La Grande Mascarade, Reliques, Peuples alliés, Grandes compagnies, Livraisons spéciales, Eureka et Bozja, Donjons
profonds, Équipement, Chocobo, Mandats, Bestiaire, Dresseur ; Aide, Crédits, Mes outils. La page Personnage a été retirée en 0.341 (la page
Équipement montre les mêmes valeurs réelles ; le panneau à côté de la fenêtre Personnage du jeu reste).

## À décider / à revoir

Points encore ouverts, notés au 0.512 : chacun attend un relevé en jeu ou se règle dans une étape du plan. Aucune valeur n'est devinée en
attendant. ONBOARD-001 et TEMPLATES-001 sont des fiches d'objectifs de l'agent, hors du dépôt.

| Point | Source | Constat | Relevé ou étape attendus |
|---|---|---|---|
| Prise en main, aide, vidéos, tickets | ONBOARD-001 | Les 8 lots codés (0.502 à 0.506), DEC-031 | Validation en jeu ; une release pour la liste de vidéos |
| Modèles de la communauté | TEMPLATES-001, [DEC-029](DECISIONS.md) | Terminés côté code en 0.476 | Validation en jeu ; galerie visible après une release |
| Règle « coût ≤ capacité » sous le rang 50 | [0.315-vessel-builds](../evidence/0.315-vessel-builds.md), « Coût ≤ capacité » | Règle des outils de la communauté. Cohérente avec 11 montages de dirigeables publiés ; non vérifiée en jeu sous le rang 50. | Relevé en jeu sur un vaisseau de rang inférieur à 50. |
| Type de marqueur des mandats | CHANGELOG 0.298 | Le journal montre les types 0, 3 (quêtes), 4 et 6 ; aucun relevé ne dit lequel désigne un mandat. | Relevé en jeu : mandat accepté, carte ouverte, mode développeur. |
| Icône du drapeau 60561 | `MainWindow.MapView.cs` (`FlagIcon`) ; CHANGELOG 0.102 (« supposée ») ; audit de la carte 0.328 | L'identifiant de l'icône du drapeau n'est pas prouvé dans docs/evidence (la pose du drapeau, elle, est validée en jeu). | Relevé en jeu : drapeau posé, `AgentMap.FlagMapMarkers[0].MapMarker.IconId` lu sur le framework (sonde), journal dans docs/evidence. |
| Icônes de quête 71141 / 71151 | [BACKLOG](BACKLOG.md) ; [0.211-event-icons](../evidence/0.211-event-icons.md) | Le sens des variantes n'est pas dans les données du jeu : les réglages affichent « variante n ». | Relevé en jeu, sinon garder « variante n ». |
| Bonus d'Abondance estimé (0.224) | [BACKLOG](BACKLOG.md), « Revue des points ouverts » ; [0.224-bountiful-base](../evidence/0.224-bountiful-base.md) | Règle de la communauté non officielle, affichée « estimé » et modifiable. | À comparer en jeu avec le bonus réel. |
| Matérias : pièce sur plusieurs emplacements | [0.312-materia](../evidence/0.312-materia.md), « Non vérifié en jeu » | Une pièce qui couvre plusieurs emplacements prend le pourcentage du torse. | Relevé en jeu (plafond affiché contre le jeu). |
| Réglages utiles manquants | R-06 étape 5 ; audit du point d) (2026-11-02) | FAIT 0.327 (relu le 2026-11-03) : nombre de résultats de recherche, portée du marché mémorisée, aides Triple Triad, calcul automatique du carnet, survol masqué en mission, silence en mission de la ligne de discussion, page d'ouverture, parties du panneau au survol. | Tests en jeu. |

### Décisions reportées à la fin (choix provisoire de l'agent, à confirmer)

Lot du 2026-11-03 ([DEC-026](DECISIONS.md)) : « Monstres au survol » retiré (0.447) ; inventaire 250 ms : DEC-004 gardée, éditeur du tri
gardé à l'écran (0.447) ; fins de ligne : rien à normaliser ; GitHub : protections, Dependabot et compilation en CI activés (0.447) ;
mémoquartz : 80 % confirmé ; choix de g) (G-PROPOSALS) confirmés en bloc. Les autres lignes restent à confirmer pendant les tests en jeu.

| Point | Choix provisoire | Où le changer |
|---|---|---|
| R-12 Discord : valeurs par défaut | Envoi désactivé ; toutes les catégories cochées ; message « ⏰ [label] : dans [minutes] min ([time]) » ; nom « Tensho » ; nom du personnage non envoyé. Depuis la 0.778 (X-23), chaque nouvelle alarme a sa case Discord décochée. | Réglages › Alarmes › Discord |
| R-12 Discord : débit | Deux secondes au moins entre deux messages, file de 20 messages (les plus anciens écartés au-delà), trois essais au plus sur « 429 ». | `Alarms/DiscordNotifier.cs` |
| Revue de mode : source coupée (0.321) | Plus aucun téléchargement ; une copie récente gardée sur disque reste affichée. | `Fashion/FashionDataService.cs` |
| « Tout remettre par défaut » (0.321) | Les groupes repliés de la navigation sont gardés (état de l'interface) ; les réglages de la carte, dont les tailles et priorités d'icônes par carte, sont remis à zéro. | `Configuration.cs` (ConfigurationReset) |
| Nouvel ordre de Release.ps1 (0.323) | Contrôles, tag et release GitHub avec le zip, puis main (repo.json) en dernier ; reprise si le tag existe sans release. | `scripts/Release.ps1` |
| GitHub Actions et Dependabot (0.323) | **Activés en 0.447** (DEC-026) ; job de compilation à la manière de Dalamud ajouté en 0.512. | `.github/`, [publish/github](../../publish/github/README.md) |
| Protections du dépôt GitHub (0.323) | **Activées en 0.447** (DEC-026) : push forcé et suppression interdits sur main et Devloppement, tags Stable-* protégés. | réglages GitHub |
| Fins de ligne eol=lf (0.323) | **Rien à normaliser** (DEC-026). | `.gitattributes` |
| Code de conduite, CODEOWNERS, hook pre-push (0.323) | Non créés (code de conduite, CODEOWNERS) ; hook prêt, inactif (`git config core.hooksPath .githooks` pour l'activer). | `.githooks/pre-push` |
| R-13 Crédits : réglages partiels | Résolu à titre provisoire en 0.324 (choix recommandé appliqué) : les interrupteurs Garland et wiki coupent toute leur source, Universalis a son propre interrupteur (activé par défaut) avec « prix au survol » en dessous ; les copies gardées restent affichées. À confirmer ou à changer à la fin. | `Configuration.cs`, services de `Plugin.cs` |
| R-12 Discord : alarmes écrites dans la liste du jeu | Elles sonnent dans le jeu et partent quand même sur Discord. | `Alarms/AlarmService.Dalamud.cs` (Ring) |
| Test des infobulles (0.325) | Cliquet (liste d'exceptions qui ne fait que diminuer) et expressions régulières, sans dépendance ; pas de tolérance zéro ni de Roslyn. | `tests/TenshoAssistant.Tests/TooltipRatchet325Tests.cs`, `tooltip-baseline.txt` |
| Étape g) : propositions et choix provisoires | Tenus au fil de g) dans [G-PROPOSALS](G-PROPOSALS.md) : choix provisoires appliqués, idées repérées en codant, écarts laissés pour une séance en jeu. | [G-PROPOSALS](G-PROPOSALS.md) |
| Alertes de monnaies (I-02, 0.344) | Choix provisoires de l'agent : seuil de 80 % du plafond pour toutes ; sans Grande Compagnie, pas d'alerte de sceaux ; une monnaie déjà au-dessus au chargement est listée sans message ; pas de rappel à la connexion. Cristaux, mandats et gils non suivis. À confirmer ou changer. | `Inventory/Currencies.cs`, `MainWindow.Currencies.cs` |
| Touche d'actualisation du prix survolé (I-01, 0.343) | Choix provisoire de l'agent : Ctrl+Alt pendant le survol (les touches F et les lettres sont prises par le jeu), réglable (désactivable). | `MainWindow.MarketHover.cs`, `Items/ItemHoverReader.Dalamud.cs` |
| Carte, objets, artisanat et récolte, autres outils : quoi ajouter (étape e) | Propositions de l'audit du 2026-11-02, en partie faites depuis ; chaque proposition a sa ligne « État ». | [MAP-PROPOSALS](MAP-PROPOSALS.md), [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md), [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md), [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md) |
| Inventaire vidé 250 ms à chaque changement (audit 0.329, défaut 7) | Non corrigé : la correction contredit DEC-004 (« invalidation atomique, collecte coalescée 250 ms ») et ses tests. Recommandé : garder DEC-004 et faire garder à l'éditeur de tri la dernière liste complète (jamais pour l'aperçu ni le tri). | `MainWindow.SortPreview.cs` ; DEC-004 |
| Cadre des fenêtres du jeu (0.332) | **Retiré en 0.510** (DEC-033, décision de l'utilisateur). | — |
| Escouade dans R-11 étape 4 (sonde 0.332) | L'équipement d'un membre d'escouade n'est qu'une apparence (mirage, teintures), sans caractéristiques : rien à planifier. Appliqué en 0.340 (barde faite, escouade non planifiée) ; à confirmer à la fin. | [0.332-squadron-probe](../evidence/0.332-squadron-probe.md) |
| « Monstres au survol » sans effet (relevé en 0.327) | **Retiré en 0.447** (DEC-026). | — |
| Suppressions immédiates (0.325) | Confirmation en deux clics pour « Tout supprimer » d'un groupe d'alarmes seulement ; ailleurs (alarme, deck, modèle de rangement, séquence d'artisanat), une infobulle dit que la suppression est immédiate. | `MainWindow.Alarms.cs` |

### Décidé le 2026-11-02 ([DEC-023](DECISIONS.md))

| Point | Décision |
|---|---|
| Table des seuils de butin des sous-marins | Garder la table SubmarineTracker ; sur les 19 secteurs où elle est sous toutes les observations, le palier porte la mention « non confirmé par les relevés ». |
| Valeur des trajets de sous-marin | Ajouter le prix du marché (Universalis) à côté du prix PNJ, et les paliers de surveillance / récupération si la source sépare les relevés par palier. |
| Arbre de déblocage des dirigeables | Données de LuminaSupplemental (GPL-3.0) embarquées dans la DLL, avec l'avis de licence dans le fichier. |
| Planificateur de trajets de dirigeable | Distance et durée seulement ; le butin est affiché « inconnu », jamais estimé. |
| Matérias : taux de surmatérialisation | Lire le taux de réussite dans les données du jeu et l'afficher par emplacement avancé. |
| Page Personnage retirée (0.341, demande de l'utilisateur) | La page Équipement montre les mêmes valeurs réelles. Le panneau à côté de la fenêtre Personnage du jeu est gardé (décidé par l'utilisateur le 2026-11-02). |
| Autres équipements (R-11 étape 4) | Barde de chocobo : FAITE 0.340 ([preuve](../evidence/0.340-chocobo-barding.md)). Escouade : apparence seulement, retirée. |
| Rappels : escouade, Planeurs d'Ehcatl | Sonder une heure lisible dans la mémoire du jeu (FFXIVClientStructs) ; sinon laisser de côté. |
| Événements saisonniers (R-09) | FAIT 0.342 : données du jeu seulement (événements en cours, quêtes, PNJ et boutiques des calques) ; noms par le PNJ guide (français) ou LuminaSupplemental ; pas de dates à venir. |
| Cadres et textures du jeu (ULD) | Cadre retiré en 0.510 (DEC-033). |
| Publication D17 | Remplacée par DEC-039 (2026-11-05) : pas de soumission, le dépôt personnalisé seulement. |
| Traductions allemande et japonaise | Gardées dans le paquet (partielles, repli anglais). |
| Ton des textes français | Vouvoiement partout (fait 0.317, test automatique). |
| Pêche en mer | Calendrier (page « Pêche en mer ») et aide pendant la traversée ; données du jeu (IKDRoute, IKDRouteTable, IKDSpot). |
| Compléments des 17 outils | Repris dans le plan c) : rotation adaptative (A-04), tri de l'arsenal et automatique (I-14, plus tard). |
| Licence EUPL-1.2 | Compatible avec la GPL v3 par sa clause (avis requis) : Tourist, Glamaholic, Quest Map, Better Party Finder (DEC-025). |
| Modèle `FilteredTable` | Retiré du plan (choix de l'agent, règle « pas d'abstraction prématurée ») : à créer au premier besoin réel répété. |

## Tests en jeu

La liste à jour : les cases de [TESTING.md](TESTING.md). La campagne guidée en jeu de tout le plugin est la phase 6 du
[plan global](GLOBAL-PLAN.md). Relevés encore demandés (données inconnues, voir « À décider ») : type de marqueur d'un mandat accepté ;
variantes d'icônes de quête 71141 / 71151.

## Domaine complémentaire

Combat/ACT/IINACT : retiré du projet (décision de l'utilisateur, 2026-10-31).
