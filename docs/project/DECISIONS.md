# Lasting decisions

> *Traduction française plus bas.*

Index produced by `tools/docgen.py` (one row per DEC, its problem):

<!-- docgen:decisions-en -->
| DEC | Date | Problem |
|---|---|---|
| DEC-001 | 2026-10-27 | Choose the real toolchain of the new plugin. |
| DEC-002 | 2026-10-27 | Check the lifecycle without running the game. |
| DEC-003 | 2026-10-27 | Avoid confusing absence in a partial inventory with a quantity of zero. |
| DEC-004 | 2026-10-27 | Read the bags without over-interpreting completeness and without keeping native memory. |
| DEC-005 | 2026-10-27 | The counter matches the declared inventory but always shows "total unknown" in 0.1.0. |
| DEC-006 | 2026-10-27 | The diagnostic window must prepare an interface shared by the different tools, at the user's request. |
| DEC-007 | 2026-10-27 | Include retainers and chocobo in searches when their inventories are closed, with a voluntary refresh from their menus. |
| DEC-008 | 2026-10-27 | The interface separates bags and bank too much, and the search does not show the real item icons. |
| DEC-009 | 2026-10-31 | ACT/IINACT and a Combat domain were listed as a complementary study; git repository added with the checkpoint archives tracked. |
| DEC-010 | 2026-10-31 | Backups (checkpoint zips) and the flat documentation became heavy and hard to read once the repository was on GitHub; contributors also had to be welcomed. |
| DEC-011 | 2026-10-31 | Order and nature of the sources shown for "How to get it"; place of the FATE tool. |
| DEC-012 | 2026-11-01 | Fix what remains to be done before the D17 publication and what will not be done. |
| DEC-013 | 2026-11-01 | Count the armoury chest, the glamour dresser and the armoire in the stocks and in the "owned" of the Fashion Report. |
| DEC-014 | 2026-11-01 | Tensho windows go above the game windows (request R-05). |
| DEC-015 | 2026-11-01 | After a network failure, LuminaSupplemental and GatherBuddy restarted the download at every frame; an expired copy was lost offline; the market listings had no disk cache. |
| DEC-016 | 2026-11-01 | Reduced and full tooltips stacked for the same item, right-click menus duplicated or different depending on the place. |
| DEC-017 | 2026-11-01 | The package shipped a copy of the language files next to the DLL. |
| DEC-018 | 2026-11-01 | Settings without help or a way back to the default; "restore default settings" must cover all settings without losing the user's data. |
| DEC-019 | 2026-11-02 | Several views turned unread data into "none", "not obtained" or 0. |
| DEC-020 | 2026-11-02 | The user asks to try the gear of the other jobs: « on peut mirager tout le stuff maintenant (vérifie) » ("we can glamour all the gear now (check)"). |
| DEC-021 | 2026-11-02 | Submersible loot and thresholds are not in the game data (decided server side). |
| DEC-022 | 2026-11-01 | Several calculations rely on rules absent from the game data (community or specific to Tensho). |
| DEC-023 | 2026-11-02 | "To decide / to review" points of the roadmap (0.316), settled by the user. |
| DEC-024 | 2026-11-02 | Plugin version numbering rule (`<Version>` of the csproj, tags `Stable-x.y.z`). |
| DEC-025 | 2026-11-02 | Which gaps of plan c) to add to Tensho, in what order, and with which precise choices. |
| DEC-026 | 2026-11-03 | Decisions kept for the end (G-PROPOSALS, ROADMAP "Decisions postponed to the end"): which to settle now, and how. |
| DEC-027 | 2026-11-03 | Sources of N-06 (squadron) and N-14 (crossbreeding), DEC-026 point 11; research: docs/evidence/0.448-n06-n14-sources.md. |
| DEC-028 | 2026-11-03 | A-03 / A-04: native Raphael dependency (licence, Plogon offline build, size), and suggestion solver during crafting. |
| DEC-029 | 2026-11-04 | Scope and order of the customisable chat (CHAT-001) and of the community templates (TEMPLATES-001). |
| DEC-030 | 2026-11-04 | The customisable chat decided in DEC-029 (points C1 to C12, delivered in 0.471, phases 0 and 1). |
| DEC-031 | 2026-11-04 | Plan for first-time use, help, videos and tickets (ONBOARD-001); ten decisions G1 to G10. GitHub had not been kept up to date since 0.454. |
| DEC-032 | 2026-11-04 | Beastmaster team advice facing the Dalamud rules (combat, competitive content); the "Dédale bestial" has rankings (XBMRanking window). |
| DEC-033 | 2026-11-04 | Six open decisions: release, advice during a Triple Triad match, timing of the retainer sales request, I-15, N-10, frame of the game windows. |
| DEC-034 | 2026-11-04 | The build of the Dalamud repository uses SDK 10.0.101 on Linux, network cut; global.json required 10.0.403 (original DEC), Tensho would not have built on their side, and it had never been built on Linux. |
| DEC-035 | 2026-11-04 | Title by job: on an option off by default, the title changes without a click when the job changes, while Dalamud forbids automatic server requests without a player action. |
| DEC-036 | 2026-11-04 | The GP plan on the crafting list (0.478, 0.479) proposed nothing on a node with 5 attempts and 809 GP: the first node seen counted as normal, and its GP stayed kept. |
| DEC-037 | 2026-11-05 | Review of all proposals with the user: 100 points kept for before 1.0.0, 12 rejected ([GLOBAL-PLAN](GLOBAL-PLAN.md)). |
| DEC-038 | 2026-11-05 | Tensho runs everywhere in PvP and in ranked content; only the Triple Triad advice is turned off there (DEC-033). |
| DEC-039 | 2026-11-05 | On 2026-11-05, on the Dalamud Discord, several team members judge Tensho too big to be reviewed and largely already covered by other plugins. |
| DEC-040 | 2026-11-07 | The sweep of the self-check pages (0.724, fixed in 0.726) opens each page without a click; some then ask for a community source enabled in the settings (44 requests on the first sweep, 2 on the next thanks to the cache). |
| DEC-041 | 2026-11-08 | The "to do" document asks for content tools (Eureka, Bozja, Deep Dungeons, "Île Lunule", Savage raids) with strategy boards; DEC-009 removed the Combat domain (only exception: the Beastmaster, information only, PvE, DEC-032). |
| DEC-042 | 2026-11-08 | The format of the discoveries file has no room for HQ, per-value counters or denominators (future loot rates); changing it after 1.0.0 would force migrating every player's files. |
| DEC-043 | 2026-11-08 | Submersible voyage durations calculated by a community formula (SubmarineTracker, MIT); the game has its own calculation functions. |
| DEC-044 | 2026-11-08 | Sea fishing (X-85): which bait triggers the spectral current, if the game data do not say. |
| DEC-045 | 2026-11-09 | Review of the G-PROPOSALS proposals "To review with the user" (points 1 to 8). |
| DEC-046 | 2026-11-09 | Beastmaster team maximiser (request of 2026-11-09) while DEC-032 excluded a calculated "best team" without a new decision (the "Dédale bestial" has rankings). |
| DEC-047 | 2026-11-09 | Help choosing the 3 beasts of each "Dédale bestial" encounter: the board screen is inside the instance (reading of 2026-11-09: "Competitive cut on: territory 1339" at entry, right after the mission window), where DEC-038 turns all of Tensho off. |
| DEC-048 | 2026-11-10 | Proposals 9 (X-101, "wish on sale" alert, automatic network call) and 10 (X-100, grouping of tools and settings) of G-PROPOSALS. |
| DEC-049 | 2026-11-10 | Proposal 11 of G-PROPOSALS (X-40): other uses of nearby monsters. |
| DEC-050 | 2026-11-09 | Topics entrusted to the agent on 2026-11-09: releases, Dalamud AI policy, EUPL-1.2 licence, icon. Some DECs and public documents still said the opposite: commits made by the desktop application (DEC-009, DEC-010), release after a game session (DEC-033), AI policy followed (DEC-010, DEC-039), EUPL "to be accepted by the user" (GAPS), icon "to keep or replace" (PUBLISHING, point 7). |
| DEC-051 | 2026-11-11 | No decision recorded that the agent's working files left the repository in 0.802, nor that the agent makes the commits; DEC-010 still said that the user makes the commits (GitHub Desktop) and that no zip is made any more. |
<!-- /docgen:decisions-en -->

ID: DEC-001
Timestamp: 2026-10-27 Europe/Brussels
Problem: Choose the real toolchain of the new plugin.
Evidence: Local Dalamud.NET.Sdk 15.0.0 targets net10.0-windows/C#14; SDK 10.0.403 found outside the PATH; build succeeded against Hooks/dev.
Options: Use the .NET 9 from the PATH; install an SDK; use the existing .NET 10 SDK.
Decision: global.json pins .NET 10.0.403 and Dalamud.NET.Sdk 15.0.0; Verify.ps1 accepts an explicit path.
Reason: Local compatibility shown, no installation needed.
Consequences: Use the script or the right executable; no old code reused.
References: REF-LOCAL-001, REF-SAMPLE

ID: DEC-002
Timestamp: 2026-10-27 Europe/Brussels
Problem: Check the lifecycle without running the game.
Evidence: The callbacks and the command are the only resources of the base.
Options: Introduce a module framework; handle each unsubscribe on its own; small stack of acquisitions.
Decision: RegistrationScope with reverse release, continuing after an error, and tests that run without a test NuGet.
Reason: Covers partial failures and reloads without premature business infrastructure.
Consequences: The smoke test inside Dalamud stays manual and mandatory before publication; the JSON test covers the managed parameters.
References: REF-LOCAL-001

ID: DEC-003
Timestamp: 2026-10-27 Europe/Brussels
Problem: Avoid confusing absence in a partial inventory with a quantity of zero.
Evidence: REF-INVENTORY, REF-SNAPSHOT and eight tests of the managed model CORE-001.
Options: Integer quantity only; nullable quantity only; observed lower bound and explicit coverage per container.
Decision: Immutable snapshot with owner/container scope; query on an explicit scope, result with a nullable ExactQuantity plus a minimum ObservedQuantity and Coverage. Owners and qualities stay distinct. InventoryService keeps the last batch only, atomic publication, no implicit merge.
Reason: Lets future needs calculations say what is missing as to knowledge as well as to quantity, without announcing false zeros.
Consequences: The adapter must prove Complete, normalise the game identifiers and guarantee the publication order. Clear invalidates the state. No global completeness and no character ownership is inferred from an empty collection.
References: REF-INVENTORY, REF-SNAPSHOT, REF-INV-MODEL

ID: DEC-004
Timestamp: 2026-10-27 Europe/Brussels
Problem: Read the bags without over-interpreting completeness and without keeping native memory.
Evidence: REF-INV-API; build and seven coordinator tests.
Options: Infer Complete from the span length; use new offsets; stay Partial/Unknown through the public contract.
Decision: Four bags only; read on a confirmed framework Update, immediate copy of primitives, owner checked before/after. Empty span Unknown, non-empty Partial. Atomic invalidation, collection coalesced at 250 ms, Unknown/error retries after five seconds, invalidated publications rejected.
Reason: The installed contract allows observation but does not prove container completeness. This choice avoids offsets and false zeros while giving a real diagnostic to validate.
Consequences: Future calculations must respect the nullable ExactQuantity. The adapter does not yet allow claiming the total owned; separate live validation RUNTIME-001.
References: REF-INV-API, REF-INV-MODEL

ID: DEC-005
Timestamp: 2026-10-27 Europe/Brussels
Problem: The counter matches the declared inventory but always shows "total unknown" in 0.1.0.
Evidence: User screenshots Potion 4551 and syrup 12892; REF-INV-LOAD; 25 tests passing after the change.
Options: Change only the text; keep Partial everywhere; check loading through the installed local structures.
Decision: Extend DEC-004 with the typed InventoryManager/InventoryContainer members. Complete requires a loaded header, valid type and size, a buffer stable before/after and all slots read. Limit the total claim to the four bags; retry Partial reads like Unknown. Deliver a built 0.1.1 before asking for a test.
Reason: Gives Complete a verifiable meaning and allows an exact zero quantity in a fully read scope, without claiming to cover the other storages.
Consequences: New native path to confirm in game; no live validation of 0.1.1 can be inferred from the 0.1.0 screenshots.
References: REF-INV-LOAD, TESTING.md section INV-002

ID: DEC-006
Timestamp: 2026-10-27 Europe/Brussels
Problem: The diagnostic window must prepare an interface shared by the different tools, at the user's request.
Evidence: Functions available and validated up to 0.1.4; the 17 business modules are still to build; empty business tabs are forbidden in the master prompt.
Options: Seventeen placeholder tabs; navigation through existing spaces and a unified directory of the future scope.
Decision: Five real pages with a shared item selection. The seven future domains are described in a single filterable directory of the 17 references, all marked as coming. Settings and technical provenance isolated in their own pages. Keep the user's Dalamud style.
Reason: Prepares the arrival of the tools without fake features, duplicated services or diagnostics mixed with everyday use.
Consequences: Version 0.2.0; visual check required. CRAFT-001A stays READY and its plan is kept in the maintainer's notes (not in the repository).
References: ROADMAP.md, TESTING.md UI-003, ARCHITECTURE.md

ID: DEC-007
Timestamp: 2026-10-27 Europe/Brussels
Problem: Include retainers and chocobo in searches when their inventories are closed, with a voluntary refresh from their menus.
Evidence: Explicit user request; REF-BANK-API; managed bank and persistence tested; bag reading API already validated and factored out.
Options: Permanent scans/refreshes; manual import of complete, dated snapshots; infer zero from inventories that are not loaded.
Decision: Shared bank per character and storage, manual import only from a verified visible menu. Native copy on the framework, serialised persistence off the framework, replacement by key and previous cache kept on error. Anchored companion ImGui panels, no native node injection. Cache and live totals explicitly distinguished.
Reason: Keeps the scope, avoids double counting on re-imports and makes searches possible outside the retainer/chocobo context without claiming a live view of closed storages.
Consequences: The user must re-import after a transfer. The seven retainer pages are covered, not equipment/crystals/market. Chocobo extension independent and conditional. Addon names/attachment and identity on retainer change need a live test after the build.
References: REF-BANK-API, TESTING.md BANK-001, ARCHITECTURE.md Bank

ID: DEC-008
Timestamp: 2026-10-27 Europe/Brussels
Problem: The interface separates bags and bank too much, and the search does not show the real item icons.
Evidence: User request and screenshot 0.3.0: 182 NQ in bags and 26 HQ in the chocobo cache, shown in two long sections.
Options: Keep two pages with duplicates; common view and grouped projection; simulate a current total owned from the caches.
Decision: A single Items & storages destination with game icons, filters and Character/Chocobo/Retainers headings. Pure tested projection reusing snapshots and bank. Caches stay dated and the sum is only counted, not exact. The full Character filter is the only exact total at present.
Reason: Gathers the locations around the item while keeping freshness and scope explicit.
Consequences: No bank migration and no new native read; images borrowed from the host provider. Old Bank page merged, import management collapsible; new visual check required in 0.4.0.
References: REF-ITEM-TEXTURES, TESTING.md UI-005, ARCHITECTURE.md unified view

ID: DEC-009
Timestamp: 2026-10-31 Europe/Brussels
Problem: ACT/IINACT and a Combat domain were listed as a complementary study; git repository added with the checkpoint archives tracked.
Evidence: User request 2026-10-31: « retire tout ce qui touche à ACT, on va pas le faire » ("remove everything about ACT, we are not doing it"); « j'ai ajouté un repo git, pense à faire le nécessaire pour gitignore » ("I added a git repo, remember to do what is needed for gitignore").
Options: Keep ACT as an optional study; remove it from the scope. Keep the zips in git; ignore them.
Decision: ACT/IINACT and Combat removed from the project (docs updated, no code concerned). Archives checkpoints/*.zip ignored by git (kept on disk); logs and verification notes stay tracked.
Reason: Scope decision by the user; git history already keeps every version of the code.
Consequences: The zips are still created at each version but outside the repository. Commits are made automatically by the desktop application, not by the agent. Zips already in the local history are not rewritten.
References: .gitignore, the maintainer's notes (not in the repository); modified by DEC-050 (commits and releases by the agent)

ID: DEC-010
Timestamp: 2026-10-31 Europe/Brussels
Problem: Backups (checkpoint zips) and the flat documentation became heavy and hard to read once the repository was on GitHub; contributors also had to be welcomed.
Evidence: User request 2026-10-31: « les backup ne sont plus utile si on utilise github » ("backups are no longer useful if we use github"), « réorganiser les md … suivi complet et reprise IA complète » ("reorganise the md files … full tracking and full AI resumption"), « prendre en compte dev par la communauté open source » ("take open-source community development into account"), D17 rules.
Options: Keep checkpoints/ tracked; ignore it entirely. Documents at the root; arrangement by audience.
Decision: `checkpoints/` entirely ignored by git (local), no more zip per version; evidence cited by the code in `docs/evidence/`; history per version in `CHANGELOG.md`; documents sorted into the maintainer's notes (not in the repository) and `docs/project/`, with the map `docs/README.md`; `CONTRIBUTING.md` (rules, licences, Dalamud publication and AI policy). Replaces the consequence "zips created at each version" of DEC-009.
Reason: Git history keeps the code of each version; one entry per audience (player, contributor, agent) makes resumption safer.
Consequences: The user makes the commits (GitHub Desktop) (amended by DEC-050 and DEC-051: the agent makes the commits; local zip backups before a large change). Zips already in the git history are removed from it only on explicit request (history rewrite). The big documents in `docs/project/` keep their historical part, marked as an archive.
References: .gitignore, CHANGELOG.md, docs/README.md, CONTRIBUTING.md, the maintainer's notes (not in the repository); modified by DEC-050 (AI policy no longer followed, commits by the agent) and DEC-051 (agent files out of git since 0.802, commits by the agent)

ID: DEC-011
Timestamp: 2026-10-31 Europe/Brussels
Problem: Order and nature of the sources shown for "How to get it"; place of the FATE tool.
Evidence: User feedback 0.123–0.146: « achetable au marché est un critère pas une source » ("buyable on the market is a criterion, not a source"), « ne pas utiliser Garland si on connaît déjà » ("do not use Garland if we already know"), « retire l'outil ALEA en gardant la partie faite par la carte » ("remove the FATE tool, keeping the part done by the map").
Options: Query everything in parallel; game data first, then community fallback.
Decision: Game data first (vendors, exchanges, quests, achievements, crafting, gathering); Garland Tools only if no source is known; then the wiki "Acquisition" section, translated into the plugin's language. The market is a criterion, not a source. The FATE tool is removed (0.123); the FATEs stay on the map.
Reason: Fewer external requests, game data first and verifiable.
Consequences: Some sources stay "unknown"; do not infer them.
References: docs/project/ARCHITECTURE.md (Item sources), CHANGELOG.md 0.123–0.150

ID: DEC-012
Timestamp: 2026-11-01 Europe/Brussels (before 0.224)
Problem: Fix what remains to be done before the D17 publication and what will not be done.
Evidence: Review of the open points with the user (docs/project/BACKLOG.md, "Review of open points (2026-11-01)"); sources checked per point (GatheringOptimizer MIT, frozen_rabbit_tome without licence, FccShop sheet, /alarm subcommands).
Options: Handle everything before publication; publish as is; sort point by point.
Decision: Do: estimated Bountiful bonus (0.224), glamour dresser and armoire in "owned" (0.235–0.240), free company shops (0.295), quest icons 71141 / 71151 only with an in-game reading. Do not do: Fashion Report score (no data, it would be invented), deleting a game alarm from Tensho (DeleteAlarm not proven, risk of erasing the wrong one), remaining unknown sources (unknown documented). Dropped: injecting text into the game's native tooltip.
Reason: Deliver only what is proven by game data or a cited source; avoid risky actions on the game state.
Consequences: The "Do not do" items are not reopened without a new source or a new request. The additions to the 17 tools come after publication, case by case.
References: docs/project/BACKLOG.md, the maintainer's notes (not in the repository) ("PLAN BEFORE PUBLICATION"), CHANGELOG.md 0.224, 0.235–0.240, 0.295; publication setting replaced by DEC-039 (custom repository, no submission)

ID: DEC-013
Timestamp: 2026-11-01 Europe/Brussels (0.236)
Problem: Count the armoury chest, the glamour dresser and the armoire in the stocks and in the "owned" of the Fashion Report.
Evidence: User decision noted in CHANGELOG 0.236 (« ne pas dépendre d'un autre plugin » ("do not depend on another plugin"), Allagan Tools set aside); APIs checked by reflection on FFXIVClientStructs (InventoryType of the armoury chest, MirageManager.PrismBoxItemIds, UIState.Cabinet).
Options: Rely on Allagan Tools; read the game directly.
Decision: Direct game read on the framework thread, copy into the Bank (armoury chest as 12 containers; glamour dresser and armoire as virtual containers 25000 and 25001), re-read every 2 min at most; glamour dresser and armoire "unknown" until they have been opened in game.
Reason: No dependency on another plugin being installed; same model as the other storages (DEC-007).
Consequences: Reminder "open it once…" while the glamour dresser or the armoire is not read; a stored outfit counts by its pieces (MirageStoreSetItem sheet, 0.240); limits of the Bank file noted in 0.238–0.239. 0.234–0.240 to validate in game.
References: CHANGELOG.md 0.235–0.240, src/TenshoAssistant/Inventory/BankInventoryBridge.Dalamud.cs, docs/project/REFERENCES.md REF-BANK-STORAGES

ID: DEC-014
Timestamp: 2026-11-01 Europe/Brussels (0.246)
Problem: Tensho windows go above the game windows (request R-05).
Evidence: docs/evidence/0.246-r05-window-order.md: Dalamud draws all plugin ImGui windows after the game interface, with no API to interleave them; the Dalamud title bar menu (pin, click-through, opacity) already exists.
Options: A. automatic fade (Tensho window almost transparent when a game window overlaps it); B. shortcut to hide / show; C. nothing more.
Decision: C, user decision; R-05 closed with no code.
Reason: A real display order is impossible; the Dalamud tool is enough.
Consequences: When a game tooltip is hidden by a Tensho window, Tensho draws its own (server information bar, 0.300–0.301).
References: docs/project/USER-REQUESTS.md (R-05), CHANGELOG.md 0.301

ID: DEC-015
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.246, 0.270, 0.281)
Problem: After a network failure, LuminaSupplemental and GatherBuddy restarted the download at every frame; an expired copy was lost offline; the market listings had no disk cache.
Evidence: docs/evidence/0.246-cache-audit.md (read from the code on 2026-11-01, updated 0.281); docs/evidence/0.270-universalis-listings.md.
Options: Fix case by case; one common rule for all sources.
Decision: 5 min wait after a failure; the expired copy is used when the network fails; each source reads the duration of its cache zone (Settings › Data), with "Never (only on request)"; responses bounded in size and duration. Universalis: listings and history read from the Market page only, one request at a time, 3 s apart, 4 MB at most, last reading shown and dated on failure, no request for an item that cannot be sold.
Reason: Never hit a source in a loop; keep dated data rather than nothing.
Consequences: Any new network source follows the same rule (BoundedHttp or equivalent bounds, dated disk cache, cache zone). The Teamcraft response is only checked after reading (Community/FishBiteTimes.cs), to be reviewed.
References: src/TenshoAssistant/Community/BoundedHttp.cs, src/TenshoAssistant/Caching/DataCache.cs, src/TenshoAssistant/Market/MarketBoardService.cs, CHANGELOG.md 0.246, 0.270, 0.281

ID: DEC-016
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.247–0.269, 0.315)
Problem: Reduced and full tooltips stacked for the same item, right-click menus duplicated or different depending on the place.
Evidence: In-game feedback (screenshots) 0.250, 0.251, 0.252, 0.269; plan in the maintainer's notes (not in the repository), step 1.
Options: Fix list by list; common templates.
Decision: Common templates (src/TenshoAssistant/UI/Components/ItemCell.cs, EntityMenus.cs, DetailPage.cs, SettingRow.cs). Every item icon gives the full tooltip and the menu of tools and sources; a single item tooltip per frame (ItemCell.cs) and a single tooltip per route line (0.315); each attached menu has a unique, stable identifier (UniqueMenuId, 0.269); one menu per kind of thing (quest, NPC, monster, place); same content for the Tensho tooltip and the panel under the game's inventory tooltip (DrawItemInfoBody, 0.257), same sources for both right-click menus (SourceLines, 0.256).
Reason: Same behaviour everywhere, less duplicated code, less work per frame.
Consequences: A new list reuses these templates; no "name only" tooltip and no second overlapping menu. No injection into the native tooltip (DEC-012).
References: CHANGELOG.md 0.247–0.269, 0.315; the maintainer's notes (not in the repository)

ID: DEC-017
Timestamp: 2026-11-01/02 Europe/Brussels (0.248; exact date not recorded)
Problem: The package shipped a copy of the language files next to the DLL.
Evidence: User request: « tout doit être dans la même DLL » ("everything must be in the same DLL") (CHANGELOG 0.248).
Options: Keep the files alongside; embed everything in the DLL.
Decision: Languages and data embedded in the DLL (EmbeddedResource); the package contains only TenshoAssistant.dll, TenshoAssistant.json and TenshoAssistant.deps.json; Verify.ps1 refuses a shipped language file. A Localization/Languages folder placed by hand next to the DLL is still read as an override, to test a translation.
Reason: Installation and update in a single file.
Consequences: The embedded community data (0.309, 0.315) follow the same rule (src/TenshoAssistant/TenshoAssistant.csproj).
References: scripts/Verify.ps1 (check 0.248), Localization/README.md, CHANGELOG.md 0.248

ID: DEC-018
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.249, 0.258, 0.287, 0.289, 0.315)
Problem: Settings without help or a way back to the default; "restore default settings" must cover all settings without losing the user's data.
Evidence: User clarification (0.258); defect found during 0.315: "Restore all defaults" erased the vessels read, the gear plans and the materia plans.
Options: Reset the whole configuration; reset only the settings, saved data kept.
Decision: Each setting has its tooltip (English and French; a test makes Verify fail if it is missing) and a ↺ button when it differs from the default, read from a fresh configuration. "Restore all defaults" (with confirmation) resets the properties listed in ConfigurationReset.Reset (Configuration.cs, since 0.321); ConfigurationReset.Kept keeps version, language, alarms, reminders done, treasure map, macro plan, map favourites, crafting presets, Triple Triad decks, tracked quests, readings, profiles and sort templates; since 0.315 also vessels read, gear plans, materia, unlocked sectors, build plans; since 0.321 the interface state (collapsed navigation groups); a test requires every public property to be in exactly one of the two lists. The bank (separate file) is not touched. Default values of the map taken from the user's settings (0.289); configurations already saved unchanged.
Reason: A return to the default must never destroy what the user has read or prepared.
Consequences: Every new Configuration property goes into ConfigurationReset.Kept (data) or ConfigurationReset.Reset (settings), in Configuration.cs; since 0.321 a test requires every public property to be in exactly one of the two lists.
References: src/TenshoAssistant/UI/Components/SettingRow.cs, tests/TenshoAssistant.Tests/Program.cs (setting tooltips), CHANGELOG.md 0.249, 0.258, 0.287, 0.289, 0.315

ID: DEC-019
Timestamp: 2026-11-02 Europe/Brussels (0.275–0.315)
Problem: Several views turned unread data into "none", "not obtained" or 0.
Evidence: Lasting rule (the maintainer's notes (not in the repository), DEC-003); cases found in 0.275, 0.282, 0.285, 0.296, 0.298, 0.315; SubmarineTracker returns 0 for a sector without a threshold (docs/evidence/0.315-vessel-builds.md).
Options: Default value (0, cross, hidden tab); explicit "unknown" state.
Decision: Unknown stays unknown in the interface: no ✓/✗ badge while the collections are not read (0.275); a catalogue not yet loaded keeps its tab (0.282); without a confirmed quest, no quest of another city is hidden (0.285); Grand Company unknown or none: all seals stay shown (0.296); guildleve marker type not guessed, to be read in game (0.298); thresholds of a route "unknown" if a sector is missing, never 0, build name "?" if a piece is missing, company not read = no filter on unlocked sectors (0.315).
Reason: A guessed value is worse than a flagged gap.
Consequences: The pure model tests cover these cases (VesselBuilds, QuestStartCities…); every new screen distinguishes read, absent and unknown.
References: src/TenshoAssistant/FreeCompany/VesselBuilds.cs (RouteBreakpoint), src/TenshoAssistant/MainWindow.SourceLinks.cs, CHANGELOG.md 0.275–0.315

ID: DEC-020
Timestamp: 2026-11-02 Europe/Brussels (0.311)
Problem: The user asks to try the gear of the other jobs: « on peut mirager tout le stuff maintenant (vérifie) » ("we can glamour all the gear now (check)").
Evidence: Patch 7.4 (December 2025): any armour or accessory can be used as a glamour, whatever the job and level; weapons and tools stay tied to the job (press: PC Gamer, Game Informer; patch notes, cited in CHANGELOG 0.311). Rule not probed in the game data.
Options: Keep the job lists; open all pieces without distinction; open armour and accessories only, flagged.
Decision: "All jobs (glamour)" filter, off by default: armour and accessories of all jobs and levels; weapons stay those of the job; a piece the character cannot wear is flagged "Glamour", can be tried on and shared, and the statistics keep the worn piece.
Reason: Follow the current game rule without distorting the planned statistics.
Consequences: If the game rule changes, update GearCatalog (AllJobs); page validated in game by the user (report 2026-11-02, 0.310–0.314).
References: src/TenshoAssistant/Gear/GearCatalog.cs, src/TenshoAssistant/Configuration.cs, the maintainer's notes (not in the repository), step 1b

ID: DEC-021
Timestamp: 2026-11-02 Europe/Brussels (0.309, 0.315)
Problem: Submersible loot and thresholds are not in the game data (decided server side).
Evidence: docs/evidence/0.309-submarine-routes.md, docs/evidence/0.315-vessel-builds.md; MIT licences read (SubmarineTracker, FFXIVGachaSpreadsheet, (c) 2023 Infi).
Options: Download at run time; embed a dated copy; do not offer the feature.
Decision: Embed only data under a licence compatible with the GPL v3 (MIT so far), with the source, the commit or SHA-256 fingerprint, the licence and the copyright in the file (full MIT text in submarine-breakpoints.csv; "Source" line in submarine-loot.json), credit in the README; values kept as they are; transformation by a versioned script (tools/submarine-loot.py). Data without a licence (xivgear.app, Argus airship thresholds, frozen_rabbit_tome) is read only, never embedded.
Reason: The only way to offer these tools without inventing; licence and credit respected.
Consequences: No airship loot (no data under a compatible licence); distance-and-duration planner and unlock tree from LuminaSupplemental (GPL-3.0, embedded with notice) decided on 2026-11-02 (DEC-023); updating = new download and new fingerprint. The thresholds originally come from a sheet without a licence (Elena / saerielle), taken over by SubmarineTracker under MIT.
References: README.md (Credits), src/TenshoAssistant/FreeCompany/Data/, docs/project/REFERENCES.md (REF-FC-SUBMARINETRACKER, REF-FC-GACHASPREADSHEET)

ID: DEC-022
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.224, 0.277, 0.315)
Problem: Several calculations rely on rules absent from the game data (community or specific to Tensho).
Evidence: docs/evidence/0.224-bountiful-base.md, docs/evidence/0.315-vessel-builds.md; CHANGELOG 0.277 (card score).
Options: Present them as data; flag them as estimates, with their source.
Decision: "Community" or "estimated" label in the interface and tooltips, source cited, value editable when it serves as an input: sector thresholds ("community thresholds"), wear ("community estimate"), "cost ≤ capacity" rule ("community tools"), Bountiful bonus ("estimated", editable), Triple Triad card score ("Tensho estimate", formula in tooltip). The vessel statistics read are compared with the calculation: a gap is flagged (⚠), not silently corrected.
Reason: The user must know what comes from the game and what is an estimate.
Consequences: A community rule not checked in game stays so in the interface until a reading; see docs/project/REFERENCES.md "Community rules taken over as facts".
References: src/TenshoAssistant/Localization/Languages/fr.json (VesselBuilds.Sources, VesselBuilds.SurveillanceTip, VesselBuilds.Mismatch, TriadSort.ScoreHelp), src/TenshoAssistant/FreeCompany/VesselBuilds.cs

ID: DEC-023
Timestamp: 2026-11-02 Europe/Brussels (0.317)
Problem: "To decide / to review" points of the roadmap (0.316), settled by the user.
Evidence: docs/project/ROADMAP.md ("Decided on 2026-11-02"); docs/evidence/0.315-vessel-builds.md; docs/evidence/0.309-submarine-routes.md; docs/evidence/0.312-materia.md; fr.json at 0.316 (44 "vous", 14 texts with "tu").
Options: Those listed in ROADMAP at 0.316, one question per point.
Decision: (1) Submersible thresholds: keep the SubmarineTracker table and flag the 19 sectors where it is below all observations ("not confirmed by readings"). (2) Airship unlock tree: LuminaSupplemental data (GPL-3.0) embedded in the DLL with the licence notice. (3) Airships: route planner by distance and duration, loot shown as "unknown". (4) Value of submersible voyages: market price (Universalis) in addition to the NPC price, and surveillance / retrieval tiers if the source separates readings by tier (probe first). (5) R-11 step 4: chocobo barding and squadron (probe first for the squadron). (6) Reminders for the squadron and the Ehcatl gliders: probe a readable time in the game memory, otherwise leave aside. (7) R-09: after the queue c) → f), game data probed first. (8) Materia: overmelding success rate read from the game data (sheet to probe). (9) D17 publication: at version 1.0.0, moment set by the author. (10) German and Japanese translations: kept in the package, English fallback. (11) Game frames and textures (ULD): before D17. (12) French texts: formal "vous".
Reason: User answers of 2026-11-02 (« D'abord les décisions » ("Decisions first")).
Consequences: DEC-021 changes on two points: the GPL-3.0 unlock tree is embedded (same licence as Tensho, notice in the file) and an airship planner without loot is offered. The remaining roadmap points wait for an in-game reading or a step of the queue. The `FilteredTable` model is removed from the plan (agent's choice, rule "no premature abstraction"; to create at the first real repeated need). The formal "vous" is guarded by an automatic test.
References: docs/project/ROADMAP.md, the maintainer's notes (not in the repository), docs/project/PUBLISHING.md, tests/TenshoAssistant.Tests/Program.cs ("French texts address the player with « vous »"); submission part replaced by DEC-039

ID: DEC-024
Timestamp: 2026-11-02 Europe/Brussels (0.318)
Problem: Plugin version numbering rule (`<Version>` of the csproj, tags `Stable-x.y.z`).
Evidence: User message of 2026-11-02: « 0.. = numéro de version des grosses étapes du genre pour le premier release Dalamud ; .0. = les versions que je fais (nous) ; ..0 = version de la communauté » ("0.. = version number of the big milestones, like for the first Dalamud release; .0. = the versions I make (we make); ..0 = community version").
Options: Free numbering; classic SemVer (major, minor, patch); the user's rule.
Decision: X.y.z = big milestones, on the user's decision only (1.0.0 = first publication on the official Dalamud repository, DEC-023); x.Y.0 = versions of the user and the agent (the third number goes back to 0); x.y.Z = community versions (outside contributions, number set by the maintainer at merge, a PR may propose it).
Reason: User decision; the current numbering (0.317.0) already follows it.
Consequences: The agent raises only the second number; never the first without a request; the third is reserved for contributions. Documented in the maintainer's notes, not in the repository ("With each version delivered") and CONTRIBUTING.md ("Versions and releases").
References: the maintainer's notes (not in the repository), CONTRIBUTING.md, src/TenshoAssistant/TenshoAssistant.csproj; modified by DEC-050 (releases by the agent, `-Major` reserved to the user); meaning of 1.0.0 replaced by DEC-039 (end of the global plan on the custom repository)

ID: DEC-025
Timestamp: 2026-11-02 Europe/Brussels (0.318)
Problem: Which gaps of plan c) to add to Tensho, in what order, and with which precise choices.
Evidence: docs/project/GAPS.md (103 gaps: 17 reference plugins, the official Dalamud repository of 500 plugins, unofficial repositories of 658 plugins judged rule by rule); docs/evidence/0.318-plugin-gaps.json (function-by-function comparison, licences read, cross-check).
Options: For each gap: add, later or no; order by value or by domain; precise choices now or at each addition.
Decision: 101 gaps kept out of 103; later: I-14 (armoury chest sort and automatic sort) and X-05 (NPC bubbles in the chat). Order: domain by domain, Inventory and market → Collections → World → Gathering → Crafting → New domains; within a domain, high value and small size first. Precise choices settled now ("Choice" column of GAPS.md), notably: optimal crafting solver offered in two versions chosen by a setting (original C# in the DLL, native Raphael); if Macro Chain is active, Tensho relies on it; Universalis only on hover or on request for retainer prices and sales; one file per character, never an Account ID; deadline alarms created by default (Tensho alarms, never /alarm by default); sea fishing in the Gathering page, full help at once; Tensho's own collectable rotations according to turn-in thresholds; seasonal events from game data only; Faux Hollows: game data first, then a compatible licence, otherwise readings; PaissaDB accepted as regular monitoring (setting), read-only. EUPL-1.2 licence: compatible with the GPL v3 (notice required).
Reason: User's choices, batch by batch in the conversation (2026-11-02).
Consequences: Step g) of the queue (docs/project/ROADMAP.md, the maintainer's notes (not in the repository)), after R-12, d), e) and f); each gap is a separately verifiable version. Any function that acts on its own stays switchable off and respects the Dalamud rules noted in GAPS.md (no server request without a player action, data read only after the player opens it). The Raphael solver adds a native file outside the DLL: exception wanted by the user, optional.
References: docs/project/GAPS.md, docs/project/ROADMAP.md (step g), docs/evidence/0.318-plugin-gaps.json, CONTRIBUTING.md (licences); EUPL-1.2 confirmed by DEC-050

ID: DEC-026
Timestamp: 2026-11-03 Europe/Brussels (0.447)
Problem: Decisions kept for the end (G-PROPOSALS, ROADMAP "Decisions postponed to the end"): which to settle now, and how.
Evidence: User answers of 2026-11-03, in three batches, in the conversation.
Options: For each point: the agent's recommended choice, or the noted alternative.
Decision: (1) The provisional choices of G-PROPOSALS are confirmed as a block (their alternatives stay ideas). (2) X-04: a simple link to partake.gg (Events page), Tensho reads nothing from it. (3) X-13: the hunt train conductor's announcement can be sent to the chat (/sh, /y, /p, /fc, /ls1, /cwl1) on click, in addition to the copy (alternative chosen by the user, against the recommended default). (4) The "Monsters on hover" setting, without effect since the panel redesign, is removed with its code. (5) Stable release from 0.431 to 0.448 after this batch. (6) Line endings: normalisation done (nothing to change: the repository is already LF). (7) GitHub: main and Devloppement branches protected (no force push or deletion), Stable-* tags protected, Dependabot and the build with tests in CI enabled. (8) Inventory (defect 7 of audit 0.329): DEC-004 kept, the sort rule editor stays displayed during the re-read. (9) Tomestones: 80 % threshold like the other currencies. (10) Raphael (A-03, A-04): studied in session (licence, Plogon build, size) before any code. (11) N-06 and N-14: look for a community source, licence checked and submitted before coding. (12) N-08: gardening log with durations typed by the player.
Reason: User's choice (2026-11-03).
Consequences: 0.447 (points 2, 3, 4, 8), 0.448 (point 12); GitHub and CI outside the plugin; issue #24 updated; G-PROPOSALS and ROADMAP marked "DECIDED 2026-11-03". Sending to the chat (point 3) is the only Tensho function that writes into a channel visible to other players: click only, channel from a fixed list, text cleaned, 5 s apart, checked again before the game call.
References: docs/project/G-PROPOSALS.md, docs/project/ROADMAP.md, tests/TenshoAssistant.Tests/Decisions447Tests.cs, GitHub issue #24

ID: DEC-027
Timestamp: 2026-11-03 Europe/Brussels (0.449)
Problem: Sources of N-06 (squadron) and N-14 (crossbreeding), DEC-026 point 11; research: docs/evidence/0.448-n06-n14-sources.md.
Options: N-06 from the game alone with a probe, or wait for in-game tests; N-14 with the MIT pairs of nick75g, or no N-14; N-08 prefilled durations (Lotlab GPL-3.0) or typed only.
Decision: (1) N-06: coded from the game sheets (GcArmyExpedition, GcArmyMemberGrow, GcArmyTraining) and FFXIVClientStructs, with an in-game probe; the percentage stays "not validated in game" until the probe. (2) N-14: MIT pairs from nick75g/FFXIV-Crossbreed-Helper, linked to items by the English names of the Item sheet and shown in the game language ("correlation to French", user's choice); names not found are discarded and counted. (3) N-08: growth and care durations prefilled from Lotlab/FFXIV-Gardening-Tracker (GPL-3.0), always editable.
Reason: User's choice (2026-11-03); the squadron success rule is in the game sheets; only these two sources have a compatible licence.
Consequences: 0.449 (point 3), 0.450 (point 2), 0.451 (point 1); data embedded with their licence notice, credits added; no network request.
References: docs/evidence/0.448-n06-n14-sources.md, docs/project/G-PROPOSALS.md

ID: DEC-028
Timestamp: 2026-11-03 Europe/Brussels (ultracode session, after Stable-0.451.0)
Problem: A-03 / A-04: native Raphael dependency (licence, Plogon offline build, size), and suggestion solver during crafting.
Evidence: docs/evidence/0.451-raphael-study.md (Raphael.Net 5.0.0 MIT, raphael-rs Apache-2.0, Craftimizer published on D17 with the same version pinned by packages.lock.json, raphael_bindings.dll win-x64 566,784 bytes).
Options: Raphael optional or C# solver only; A-04 with the Tensho solver (exact, from the observed state) or Raphael rerun (approximate).
Decision: (1) A-03: Raphael.Net 5.0.0 added as an option, setting "Solver: Tensho / Raphael", Tensho by default; each Raphael macro replayed in CraftSimulation before being offered; calculation off the game thread, cancellable, one at a time; MIT and Apache-2.0 notices, credits. (2) A-04: suggestions during crafting with Tensho's C# solver, from the observed state (condition and effects included), display only.
Reason: User's choice (2026-11-03, ultracode session).
Consequences: Order of the session: R-14 and A-14 first, then I-05, I-15, N-10, A-03 / A-04; package checked (raphael_bindings.dll in latest.zip).
References: docs/evidence/0.451-raphael-study.md, docs/project/G-PROPOSALS.md, GitHub issue #28

ID: DEC-029
Timestamp: 2026-11-04 Europe/Brussels (decision session during the in-game campaign)
Problem: Scope and order of the customisable chat (CHAT-001) and of the community templates (TEMPLATES-001).
Evidence: CHAT-001 (document removed with the chat, DEC-030; see git), the maintainer's notes (not in the repository) (survey of Chat 2, Simple Tweaks, TidyChat, NoSoliciting, Chat Bubbles, WoW addons, Dalamud rules).
Options: Decision tables of the two plans, with recommendations.
Decision: Chat: C1 full replacement (against the layered recommendation); C2 right away (against the recommendation); C3 warn without blocking if Chat 2 is active; C4 do not ask the Dalamud team (against the recommendation); C5 hooks as needed, with fallback; C6 history in local files, off by default; C7 colour by job only if the game shows it; C8 Discord per chosen rule, name hidden; C9 external translation and emoticons optional, off (service to be chosen, against the recommendation); C10 local rules and shared list; C11 original code; C12 collapsed sections and "essentials" view. Templates: T1 both, code first; T2 right away (against the recommendation); T3 page registry right away (against the recommendation); T4 folder in this repository; T5 CC0; T6 gallery in the plugin, on click; T7 automatic validation and review; T8 settings on an allow list; T9 seven blocks in the first version. T10 (AI box in the pull request): recommendation applied, the template already asked for it.
Reason: User's choice, 2026-11-04.
Consequences: The in-game test campaign (batches 6 to 10) comes after this work, or is interleaved with it. Accepted risks: possible duplicate of Chat 2 at the 1.0.0 publication without prior question; input hooks to maintain; page registry redone during the campaign. Red lines unchanged: no automatic sending, original code, no combat. Still to choose for C9: the translation service and the emoticon service, at phase 8.
References: CHAT-001 (document removed, see git), the maintainer's notes (not in the repository)

ID: DEC-030
Timestamp: 2026-11-04 Europe/Brussels
Problem: The customisable chat decided in DEC-029 (points C1 to C12, delivered in 0.471, phases 0 and 1).
Evidence: User request: « retire tout ce qui se rattache au chat, je prendrai Chat 2 si j'en ai besoin » ("remove everything related to the chat, I will take Chat 2 if I need it").
Options: Keep the code without pursuing it, or remove it.
Decision: Customisable chat abandoned and removed: page, window, settings, texts, tests and the CHAT-001 plan. Cancels points C1 to C12 of DEC-029 and the "chat" point of T8. The functions that read chat lines for something else stay: map links (ChatMapLinkWatcher), gardening detection (GardenChatWatcher), hunt train announcement and alarms.
Reason: User's choice, 2026-11-04.
Consequences: 0.472. The old plan and the code stay in the git history (commit bf30b72, 0.471.0). The community templates (TEMPLATES-001) continue.
References: docs/project/DECISIONS.md (DEC-029), the maintainer's notes (not in the repository)

ID: DEC-031
Timestamp: 2026-11-04 Europe/Brussels
Problem: Plan for first-time use, help, videos and tickets (ONBOARD-001); ten decisions G1 to G10. GitHub had not been kept up to date since 0.454.
Evidence: User answer: « oui pour tous mais tu ne gere pas completement le git issues est pas a jour » ("yes to all but you do not fully manage git, issues are not up to date"); « je parlais demander mon accord quand tu as besoin par rapport au regle dalamud » ("I meant ask for my agreement when you need it about the dalamud rules").
Options: The plan's recommended choices, or change them one by one.
Decision: All recommended choices: help before the guide; a single offer to existing players; sources shown, defaults unchanged; tour according to the jobs; prefilled GitHub form, no token and no server; YouTube first; videos reviewed by the user; first-visit banner enabled with an option; English and French; after the Beastmaster advice, before D17. The issues are kept up to date at each version. A Dalamud rule in play: summary and the user's agreement.
Reason: User decision.
Consequences: Issues #11, #12, #15, #16, #30 closed on report or proof; BST-001, ONBOARD-001 and two follow-up issues open; ultracode label removed. Exception to DEC-009 for the Beastmaster team advice: not decided, subject to the user's agreement.
References: the maintainer's notes (not in the repository)

ID: DEC-032
Timestamp: 2026-11-04 Europe/Brussels
Problem: Beastmaster team advice facing the Dalamud rules (combat, competitive content); the "Dédale bestial" has rankings (XBMRanking window).
Evidence: User answer: « Je ne veux pas pour jcj mais que pour pve » ("I do not want it for PvP but only for PvE"), after the summary of the rules.
Options: Information only; no advice; calculated team.
Decision: Information panel for the "Dédale bestial" (PvE): enemies, actions, weaknesses once confirmed, what the team covers, bonuses. Nothing in PvP. No calculated "best team" without a new decision. Limited exception to DEC-009: neither ACT nor combat measurement.
Reason: User's choice, with the recommended choice for the form. PvE does not remove the rules: the one on combat also applies in PvE, and rankings may count as competitive content.
Consequences: Contact the Dalamud approval team before 1.0.0 (issue #31), and remove the panel if it refuses.
References: docs/evidence/0.500-xbm-columns.md, issue #34

ID: DEC-033
Timestamp: 2026-11-04 Europe/Brussels
Problem: Six open decisions: release, advice during a Triple Triad match, timing of the retainer sales request, I-15, N-10, frame of the game windows.
Evidence: User answer: « Cadre fenêtre jeu à retirer rendu horrible » ("Game window frame to remove, it looks awful"); « Conseil que pve » ("Advice only in PvE"); « Reste OK je suis toujours au boulot donc pas de jeu » ("The rest is OK, I am still at work so no game").
Options: The agent's advice, or change it one by one.
Decision: Frame of the game windows removed. Game advice in PvE only: Triple Triad against an NPC, never in a tournament or against a player. The rest, agent's advice: release after a game session on the latest version; retainer sales, Universalis on click; I-15 during an in-game session with the user; N-10 left aside, only if it is useful to the user.
Reason: User decision.
Consequences: Territories 579, 940 and 941 turn the advice off (docs/evidence/0.510-triad-competitive.md). "Advice during a match" point of PUBLISHING handled; the Dédale panel is still to be asked of the team before 1.0.0.
References: docs/project/G-PROPOSALS.md, docs/project/PUBLISHING.md, issue #24, issue #31; release rule modified by DEC-050

ID: DEC-034
Timestamp: 2026-11-04 Europe/Brussels
Problem: The build of the Dalamud repository uses SDK 10.0.101 on Linux, network cut; global.json required 10.0.403 (original DEC), Tensho would not have built on their side, and it had never been built on Linux.
Evidence: docs/evidence/0.512-d17-build.md
Options: Wait for the PR to find out; relax global.json and build like them in CI.
Decision: global.json accepts 10.0.100 and above (latestFeature); Verify.ps1 takes the most recent local SDK 10.x; job "Build like the Dalamud repository" in build.yml. Agent's choice, no effect on the game.
Reason: User request to list what is missing for 1.0.0; safe correction that is easy to undo.
Consequences: The local SDK stays 10.0.403. A new image at Dalamud is followed in the survey. The job keeps the network: only a real PR proves the network-cut build.
References: docs/project/PUBLISHING.md, issue #31

ID: DEC-035
Timestamp: 2026-11-04 Europe/Brussels
Problem: Title by job: on an option off by default, the title changes without a click when the job changes, while Dalamud forbids automatic server requests without a player action.
Evidence: User answer after the summary of the rule: « On garde juste comme ça on verra après publication dalamud » ("We keep it just like that, we will see after the dalamud publication"). src/TenshoAssistant/Collections/TitleChanger.Dalamud.cs
Options: Click only (agent's advice); keep and present in the PR.
Decision: Kept as is, with no code change, and declared in the plugin description and the PR.
Reason: User decision.
Consequences: The team may ask to remove it: it would then have to be done before approval. To be reviewed after publication.
References: docs/project/PUBLISHING.md, issue #31

ID: DEC-036
Timestamp: 2026-11-04 Europe/Brussels
Problem: The GP plan on the crafting list (0.478, 0.479) proposed nothing on a node with 5 attempts and 809 GP: the first node seen counted as normal, and its GP stayed kept.
Evidence: User screenshot; answers: « tampis on va retirer la lecture de la liste de recolte on fera que par node pas sur une serie de node » ("never mind, we will remove the reading of the gathering list, we will only do it per node, not on a series of nodes"), « si il faut quand meme effet du lieu pour ameliorer sur une node » ("if we still need the place effect to improve on a node"), « si 5/5 recolte au lieu de 4 c'est pas la meme que bonus recolte supplementaire » ("if 5/5 gathering instead of 4 is not the same as extra gathering bonus"), « il faut prendre en compte tous les effet pas que celui la » ("we must take all the effects into account, not only that one").
Options: Fix the bonus detection and keep the series; a single node.
Decision: Plan for the current node only. All the place effects read from the game sheets: named, marked as already counted for active attempts; if inactive, what is missing and what they would bring when it can be estimated. The crafting list line stays, for information (« tu peux laisser le tooltip de la liste de fabrication c'est pratique » ("you can leave the crafting list tooltip, it is handy")).
Reason: User decision.
Consequences: The bonus and seconds-between-nodes settings disappear; the list one now only controls its line. "GP per tick" stays for the GP bar.
References: docs/evidence/0.515-node-effects.md

ID: DEC-037
Timestamp: 2026-11-05 Europe/Brussels
Problem: Review of all proposals with the user: 100 points kept for before 1.0.0, 12 rejected ([GLOBAL-PLAN](GLOBAL-PLAN.md)).
Evidence: User answers to the review; question « Où places-tu la 1.0.0 dans le plan ? » ("Where do you place 1.0.0 in the plan?"), answer « Après tout le plan » ("After the whole plan").
Options: After phase 2 (recommended by the agent: earlier publication, less code to examine); after the whole plan.
Decision: 1.0.0 and the submission come after the whole global plan, in-game test campaign included.
Reason: User decision: « Si c'est refuser pour la taille je pourrais rien y faire mais je veux faire un plugin complet » ("If it is refused for its size I can do nothing about it, but I want to make a complete plugin").
Consequences: Publication comes later and the submission is bigger to examine; the Dalamud team may refuse it if it judges it too heavy (PUBLISHING, point 6). Everything that requires playing comes last, one point at a time.
References: docs/project/GLOBAL-PLAN.md; submission part replaced by DEC-039

ID: DEC-038
Timestamp: 2026-11-05 Europe/Brussels
Problem: Tensho runs everywhere in PvP and in ranked content; only the Triple Triad advice is turned off there (DEC-033).
Evidence: User request: « prevoir que en pvp le plugin se deactive et reactive apres pvp pas de reglages fais obligatoirement pareil pour match classer triad ou dresseur ou autre tous ce qui pourrais etre detecter par SE ou cree un impact sur des classements fais un plan pas de code pour ca pour le moment » ("plan for the plugin to turn itself off in pvp and back on after pvp, no settings, make it mandatory, same for ranked matches, triad or beastmaster or anything else that could be detected by SE or creates an impact on rankings, make a plan, no code for this for now"). Dalamud rule: no advantage in a competitive or PvP environment.
Options: One cut per tool, or a cut of the whole plugin.
Decision: The whole plugin turns itself off and restarts by itself in PvP and in ranked content, mandatory, with no setting. Addition of 2026-11-08 (user's agreement, « y a pas de vendeur en pvp donc oui » ("there is no vendor in pvp so yes")): reading the market offers during a sale listing (UndercutWatcher), the only thing left active, is turned off too (0.926). Plan only for now. Choices D1 to D5 of the checklist of 2026-11-05: see the plan. Dédale panel of DEC-032 abandoned.
Reason: User decision.
Consequences: docs/project/COMPETITIVE-OFF-PLAN.md, docs/evidence/0.600-competitive-inventory.md. Coded in 0.610 on the user's go-ahead.
References: DEC-032, DEC-033, CONTRIBUTING.md, docs/project/PUBLISHING.md

ID: DEC-039
Timestamp: 2026-11-05 Europe/Brussels
Problem: On 2026-11-05, on the Dalamud Discord, several team members judge Tensho too big to be reviewed and largely already covered by other plugins.
Evidence: Exchange noted in docs/project/PUBLISHING.md. User decision: « on continue tampis pour dalamud mais on tiens toujours a leurs consigne et regles » ("we continue, too bad for dalamud, but we still keep to their instructions and rules").
Options: Split into small plugins; do without Dalamud; continue on the custom repository.
Decision: No submission to the official repository. Tensho continues on the custom repository, and the plan continues. The Dalamud rules and instructions stay applied as if the plugin were submitted. Replaces the submission part of DEC-023 and DEC-037.
Reason: User decision.
Consequences: 1.0.0 marks the end of the global plan on the custom repository, without a D17 PR; the user still decides it. docs/project/PUBLISHING.md stays the list of rules to keep, the submission steps no longer have to be done. A small separate plugin remains possible one day, on his request. Splitting Tensho into several plugins: studied as a draft on 2026-11-06 and abandoned by the user (« on va pas le faire » ("we are not doing it")), branch removed.
References: DEC-023, DEC-037, docs/project/PUBLISHING.md; modified by DEC-050 (AI policy no longer followed)

ID: DEC-040
Timestamp: 2026-11-07 Europe/Brussels
Problem: The sweep of the self-check pages (0.724, fixed in 0.726) opens each page without a click; some then ask for a community source enabled in the settings (44 requests on the first sweep, 2 on the next thanks to the cache).
Evidence: docs/evidence/0.724-page-sweep.md. User's choice: « Oui, en automatique et que pour dev » ("Yes, automatic and only for dev").
Options: Automatic in developer mode; only on request; automatic with no request at all.
Decision: Automatic, in developer mode only, with the requests the pages make when they open, within the user's settings. The Market page stays excluded (Universalis). Outside developer mode, nothing runs.
Reason: User decision.
Consequences: The sweep summary gives the number of requests and their hosts. A test checks that the window only opens in developer mode. 0.748 (X-01, heaviness reported by the user): at most one sweep every two hours, whatever the reloads.
References: DEC-038, docs/evidence/0.724-page-sweep.md

ID: DEC-041
Timestamp: 2026-11-08 Europe/Brussels
Problem: The "to do" document asks for content tools (Eureka, Bozja, Deep Dungeons, "Île Lunule", Savage raids) with strategy boards; DEC-009 removed the Combat domain (only exception: the Beastmaster, information only, PvE, DEC-032).
Evidence: User answer to the batch of decisions of 2026-11-08: « On peut être beaucoup plus permissif, mais que pour préparer les combats, pas en combat, tant que FF14 ne peut pas nous détecter ; je suis ok, pareil pour autre contenu » ("We can be much more permissive, but only to prepare fights, not in combat, as long as FF14 cannot detect us; I am ok, same for other content").
Options: Widen the exception to fight preparation; keep combat out.
Decision: Exception widened: information to prepare fights and content (mechanics, order, written positions, objectives, progress), in PvE only. Nothing during a fight (display hidden in combat), nothing reads combat data (no ACT/IINACT, no combat log), no automated action, everything off in PvP and in ranked content (DEC-038). The Dalamud rules stay the criterion (PUBLISHING.md), beyond what the game can detect.
Reason: User decision, framed by the Dalamud rules kept (DEC-039).
Consequences: X-90 may plan preparation boards; summary and the user's agreement before each tool that touches a Dalamud rule.
References: DEC-009, DEC-032, DEC-033, DEC-038, DEC-039, docs/project/GLOBAL-PLAN.md (X-90)

ID: DEC-042
Timestamp: 2026-11-08 Europe/Brussels
Problem: The format of the discoveries file has no room for HQ, per-value counters or denominators (future loot rates); changing it after 1.0.0 would force migrating every player's files.
Evidence: Plan X-76 2c (step E1, docs/project/audits/x76-2bc-plan.md); user answer: « Fais-le au plus vite » ("Do it as soon as possible").
Options: Before 1.0.0; later.
Decision: Format fixed right now: optional fields added, format version raised, reading of old files kept, merge tool updated.
Reason: User decision.
Consequences: Done before X-85.
References: DEC-039, tools/discoveries-merge.py

ID: DEC-043
Timestamp: 2026-11-08 Europe/Brussels
Problem: Submersible voyage durations calculated by a community formula (SubmarineTracker, MIT); the game has its own calculation functions.
Evidence: X-75.6; user answer: « Oui si lecture pure » ("Yes if pure read").
Options: Game functions if they only calculate; keep the formula.
Decision: The game functions are called only if they only calculate (no action, nothing sent to the server, no state changed), on the framework thread; otherwise the formula stays, checked against the game.
Reason: User decision.
Consequences: A probe first proves the nature of the function; proof in docs/evidence.
References: X-75.6, docs/project/audits/0.874-probe-coverage.md

ID: DEC-044
Timestamp: 2026-11-08 Europe/Brussels
Problem: Sea fishing (X-85): which bait triggers the spectral current, if the game data do not say.
Evidence: User answer: « Vos relevés, puis la commu » ("Your readings, then the community").
Options: Readings first then a marked community source; readings only.
Decision: The user's in-game readings first; meanwhile, a community source shown as such, turned off by the "game and observed data only" setting (X-76 step 3).
Reason: User decision.
Consequences: A parser records the spectral currents observed and the bait of the cast.
References: X-85, X-76

ID: DEC-045
Timestamp: 2026-11-09 Europe/Brussels
Problem: Review of the G-PROPOSALS proposals "To review with the user" (points 1 to 8).
Evidence: User answers of 2026-11-09 (checkboxes).
Options: For each point, the recommended proposal or the other choice (G-PROPOSALS).
Decision: 1 PvP cut: left as it is today (jobs started not cancelled, Discord mission alert kept). 2 position in the shared discoveries: exact everywhere, after explanation (useful for other players' maps; it may tell where you were fishing on a given day). 3 settings: the agent proposes a plan of tabs and sections, nothing changes before agreement. 4 provisional choices: already confirmed by DEC-026; alternatives reviewed. 5 retainer sales: Universalis comparison on click. 6 collection fish: seen in the fishing help; add the targeted collectability and the scrips to it. 7 agent files: in French (at the agent's choice, answers in French). 8 content tools: wiki mechanics and strategies (GFDL, on click, credited, Wiki switch), written notes and arena diagram in the Tensho window, out of combat, never over the game, no marker placed in the game; browser links for sites without a licence; deep dungeon bestiary with the wiki notes, outside the dungeon only.
Reason: User decisions.
Consequences: X-97 (fishing help), X-90 (wiki, notes, diagram, bestiary), settings plan to propose.
References: G-PROPOSALS, DEC-026, DEC-038, DEC-041, docs/project/CONTENT-TOOLS.md

ID: DEC-046
Timestamp: 2026-11-09 Europe/Brussels
Problem: Beastmaster team maximiser (request of 2026-11-09) while DEC-032 excluded a calculated "best team" without a new decision (the "Dédale bestial" has rankings).
Evidence: User answer after the reminder of the rule: « équipe calculée tant que ce n'est pas pendant le combat, je pense que c'est ok » ("calculated team as long as it is not during the fight, I think it is ok").
Options: Weakness coverage only; calculated team out of combat; nothing more.
Decision: Calculated team allowed, prepared out of combat: from the Tensho page, before entering, based on the weaknesses of the enemies met (readings) and the beasts owned; result in the disk cache. Nothing during a fight, nothing inside the Dédale (DEC-038 turns Tensho off in ranked content), no automated action, PvE only. Completes DEC-032 and DEC-041.
Reason: User decision, Dalamud rule recalled beforehand (custom repository, DEC-039).
Consequences: Point X-109 of the plan; the calculation stays a preparation aid, shown as such.
References: DEC-032, DEC-038, DEC-039, DEC-041, issue #34

ID: DEC-047
Timestamp: 2026-11-09 Europe/Brussels
Problem: Help choosing the 3 beasts of each "Dédale bestial" encounter: the board screen is inside the instance (reading of 2026-11-09: "Competitive cut on: territory 1339" at entry, right after the mission window), where DEC-038 turns all of Tensho off.
Evidence: User answer after the reminder of DEC-038 and of the Dalamud rule (rankings, advantage in competitive content): cut "at the start of each fight" ("au début de chaque combat").
Options: Cut at entry, everything prepared beforehand; cut at the start of each fight (exception).
Decision: Limited exception to DEC-038 for the "Dédale bestial": in its territories, all of Tensho stays off except the Beastmaster team panel and the minimal reading it requires, active only in front of the board, between fights; turned off from the start of each fight and during any fight. No automated action, nothing that reads the fight. The rest of ranked content is unchanged.
Reason: User decision, Dalamud rule recalled beforehand (custom repository, DEC-039).
Consequences: X-109: the moment a fight starts has to be proven in game (development reading allowed by this exception) before enabling the panel; when in doubt, off.
References: DEC-032, DEC-038, DEC-039, DEC-041, DEC-046

ID: DEC-048
Timestamp: 2026-11-10 Europe/Brussels
Problem: Proposals 9 (X-101, "wish on sale" alert, automatic network call) and 10 (X-100, grouping of tools and settings) of G-PROPOSALS.
Evidence: User answers of 2026-11-10 (checkboxes), after the reminder of the Dalamud rule for the automatic call.
Options: X-101 automatic off by default, button only, nothing; X-100 proposed grouping or current layout; settings in tabs + search, search only, nothing.
Decision: X-101: setting off by default; when enabled: at most one check every 30 min, only wishes with a price, 20 items per pass, chat line when a price falls below the user's own; Universalis already declared. X-100: navigation in groups Items, Production, World, Tracking, Progress, Duties, Gold Saucer, References, Preferences (Home and My tools at the top); Settings in tabs General, Panels beside the game, Map, Tracking and notifications, Data and sources, Advanced, Debugging, with a search across all settings; each setting also stays next to its tool.
Reason: User decisions (recommended choices).
Consequences: X-100 and X-101 to code.
References: G-PROPOSALS (9, 10, "Proposal X-100"), DEC-039

ID: DEC-049
Timestamp: 2026-11-10 Europe/Brussels
Problem: Proposal 11 of G-PROPOSALS (X-40): other uses of nearby monsters.
Evidence: User answer: « Les trois » ("All three").
Options: a) all crafting lists, b) relic book targets, c) wish drops; only a); nothing.
Decision: All three, each with its own setting (map layer, icon above the head), on by default. a) extends the "your lists" layer (X-35), which only counted the list shown in a panel.
Reason: User decision.
Consequences: X-40 coded in 0.1002.
References: G-PROPOSALS no. 11, X-35, X-40

ID: DEC-050
Timestamp: 2026-11-09 Europe/Brussels (0.1020)
Problem: Topics entrusted to the agent on 2026-11-09: releases, Dalamud AI policy, EUPL-1.2 licence, icon. Some DECs and public documents still said the opposite: commits made by the desktop application (DEC-009, DEC-010), release after a game session (DEC-033), AI policy followed (DEC-010, DEC-039), EUPL "to be accepted by the user" (GAPS), icon "to keep or replace" (PUBLISHING, point 7).
Evidence: User requests of 2026-11-09 (plan of the work system, approved): the agent manages git entirely, releases, icon and licences included; Dalamud AI policy removed, the AI declaration of the manifest stays (it is true); release when the plugin has changed, the checks are green and what changed is validated in game, urgent fix (game patch, privacy) without waiting and marked "not validated in game"; 1.0.0 remains the user's decision. EUPL v1.2 (Commission implementing decision (EU) 2017/863, https://eur-lex.europa.eu/eli/dec_impl/2017/863/oj), read on 2026-11-09: Article 5 ("compatibility clause") allows distributing a derivative work that combines the work and a work under a compatible licence under that compatible licence; the appendix lists the GNU GPL v. 2 and v. 3; the same article asks to keep copyright notices intact and to attach the licence.
Options: Releases by the user only; releases by the agent within safeguards. AI policy followed or not. EUPL accepted, refused or left pending. Icon kept or redone.
Decision: 1) Releases by the agent, only through `Verify.ps1 -Release`: plugin changed, checks redone in full (build, tests, languages, package), CI of the pushed commit green, work files and webhook address absent, `repo.json` re-read before its commit, in-game tests of the versions since the last release all validated; otherwise urgent fix only (`-Hotfix "<reason>"`, notes "not validated in game"). main moves forward by fast-forward only; a rollback is done by a revert and a higher version, never by deleting a tag. A new first number (1.0.0 first) requires `-Major`, reserved to the user. Dependabot updates merged by script (Dependabot author checked, `.github/` files only, CI green); human PRs: the user. 2) Dalamud AI policy no longer followed as a rule (no submission, DEC-039); the AI declaration of the manifest and the README stays; the other Dalamud rules stay applied. 3) EUPL-1.2: DEC-025 confirmed; code under EUPL-1.2 may enter Tensho under GPL v3 through the compatibility clause, with its copyright notices kept and the credit (README, Credits page). 4) Current icon (generated by the agent, declared) kept.
Reason: User decisions (points 1 and 2, and delegation of points 3 and 4); official EUPL text read for point 3; for point 4, nothing requires redoing it: it is declared, and the preference for a hand-made icon came from the AI policy that was removed.
Consequences: Modifies DEC-009 and DEC-010 (commits and releases made by the agent, no longer by the desktop application or the user), DEC-024 (releases by the agent; the first number stays with the user, guarded by `-Major`; 1.0.0 marks the end of the global plan on the custom repository, DEC-039), DEC-033 ("release after a game session" becomes point 1 above), DEC-039 and DEC-010 (the AI policy leaves the rules followed). PUBLISHING: points 6 and 7 closed, point 4 aligned with DEC-047. GAPS: EUPL-1.2 "compatible with notice" everywhere. CONTRIBUTING and README say who tests in game and when a release goes out.
References: DEC-009, DEC-010, DEC-024, DEC-025, DEC-033, DEC-039, DEC-047, scripts/Release.ps1, scripts/Verify.ps1, docs/project/PUBLISHING.md, docs/project/GAPS.md, CONTRIBUTING.md, README.md

ID: DEC-051
Timestamp: 2026-11-11 Europe/Brussels (0.1082)
Problem: No decision recorded that the agent's working files left the repository in 0.802, nor that the agent makes the commits; DEC-010 still said that the user makes the commits (GitHub Desktop) and that no zip is made any more.
Evidence: User request of 2026-11-07 (CHANGELOG 0.802.0): remove from the repository the content made for the AI agent; the files stay on the author's disk. User agreement of 2026-11-03, widened on 2026-11-09 (DEC-050): the agent manages git (commits and pushes on Devloppement at each version). User decision of 2026-11-11 (audit of 2026-11-09, finding A039): record it in a DEC.
Options: Leave DEC-010 as it is; record the split and the commits in a new DEC that amends DEC-010.
Decision: 1) The agent's working files (instructions, settings, rules, status, notes, its own scripts) are not tracked by git since 0.802 and are ignored by `.gitignore` (block "0.802"); they live on the author's disk only, with local backups. A tracked file never points to them; `scripts/Release.ps1` refuses to publish them. 2) The agent makes the commits and pushes on the work branch at each version, and the releases (DEC-050); never a forced push, a history rewrite or a deleted tag or branch. 3) Local zip backups stay outside git (`checkpoints/` ignored, DEC-010): before a large change, not at each version.
Reason: The repository holds what players and contributors use; the agent's notes are its own and change at every turn. Recording it in a DEC removes the contradiction with DEC-010.
Consequences: Modifies DEC-010 (commits by the agent, not by the user; local backups before a large change). The documents' map (`docs/README.md`) and CONTRIBUTING describe only tracked files.
References: DEC-009, DEC-010, DEC-050, .gitignore, scripts/Release.ps1, CHANGELOG.md (0.802.0)

---

# Décisions durables

Index produit par `tools/docgen.py` (une ligne par DEC, son problème) :

<!-- docgen:decisions -->
| DEC | Date | Problème |
|---|---|---|
| DEC-001 | 2026-10-27 | Choisir la toolchain réelle du nouveau plugin. |
| DEC-002 | 2026-10-27 | Vérifier le lifecycle sans exécuter le jeu. |
| DEC-003 | 2026-10-27 | Éviter de confondre absence dans un inventaire partiel et quantité zéro. |
| DEC-004 | 2026-10-27 | Lire les sacs sans surinterpréter la complétude ni conserver la mémoire native. |
| DEC-005 | 2026-10-27 | Le compteur concorde avec l'inventaire déclaré, mais affiche toujours « total inconnu » dans 0.1.0. |
| DEC-006 | 2026-10-27 | La fenêtre de diagnostic doit préparer une interface commune aux différents outils, à la demande utilisateur. |
| DEC-007 | 2026-10-27 | Inclure servants et chocobo dans les recherches quand leurs inventaires sont fermés, avec actualisation volontaire depuis leurs menus. |
| DEC-008 | 2026-10-27 | L'interface sépare trop les sacs et la banque, et la recherche n'affiche pas les icônes réelles des objets. |
| DEC-009 | 2026-10-31 | ACT/IINACT et un domaine Combat figuraient comme étude complémentaire ; dépôt git ajouté avec les archives des points de contrôle suivies. |
| DEC-010 | 2026-10-31 | Les sauvegardes (zips de points de contrôle) et la documentation à plat devenaient lourdes et peu lisibles une fois le dépôt sur GitHub ; il fallait aussi accueillir des contributeurs. |
| DEC-011 | 2026-10-31 | Ordre et nature des sources affichées pour « Comment l'obtenir » ; place de l'outil ALEA. |
| DEC-012 | 2026-11-01 | Fixer ce qui reste à faire avant la publication D17 et ce qui ne sera pas fait. |
| DEC-013 | 2026-11-01 | Compter l'arsenal, le coffre à apparence et l'armoire dans les stocks et le « possédé » de la Revue de mode. |
| DEC-014 | 2026-11-01 | Les fenêtres de Tensho passent au-dessus des fenêtres du jeu (demande R-05). |
| DEC-015 | 2026-11-01 | Après un échec réseau, LuminaSupplemental et GatherBuddy relançaient le téléchargement à chaque image ; une copie expirée était perdue hors ligne ; les annonces du marché n'avaient pas de cache disque. |
| DEC-016 | 2026-11-01 | Infobulles réduites et complètes empilées pour un même objet, menus de clic droit en double ou différents selon l'endroit. |
| DEC-017 | 2026-11-01 | Le paquet livrait une copie des fichiers de langue à côté de la DLL. |
| DEC-018 | 2026-11-01 | Des réglages sans aide ni retour au défaut ; « remettre les réglages par défaut » doit viser tous les réglages sans perdre les données de l'utilisateur. |
| DEC-019 | 2026-11-02 | Plusieurs vues transformaient une donnée non lue en « aucun », « non obtenu » ou 0. |
| DEC-020 | 2026-11-02 | L'utilisateur demande d'essayer l'équipement des autres jobs : « on peut mirager tout le stuff maintenant (vérifie) ». |
| DEC-021 | 2026-11-02 | Le butin et les seuils des sous-marins ne sont pas dans les données du jeu (décidés côté serveur). |
| DEC-022 | 2026-11-01 | Plusieurs calculs reposent sur des règles absentes des données du jeu (communautaires ou propres à Tensho). |
| DEC-023 | 2026-11-02 | Points « À décider / à revoir » de la feuille de route (0.316), tranchés par l'utilisateur. |
| DEC-024 | 2026-11-02 | Règle de numérotation des versions du plugin (`<Version>` du csproj, tags `Stable-x.y.z`). |
| DEC-025 | 2026-11-02 | Quels manques du plan c) ajouter à Tensho, dans quel ordre, et avec quels choix précis. |
| DEC-026 | 2026-11-03 | Décisions gardées pour la fin (G-PROPOSALS, ROADMAP « Décisions reportées à la fin ») : lesquelles trancher maintenant, et comment. |
| DEC-027 | 2026-11-03 | Sources de N-06 (escouade) et N-14 (croisements), DEC-026 point 11 ; recherche : docs/evidence/0.448-n06-n14-sources.md. |
| DEC-028 | 2026-11-03 | A-03 / A-04 : dépendance native Raphael (licence, build hors ligne de Plogon, taille), et solveur des suggestions pendant la synthèse. |
| DEC-029 | 2026-11-04 | Portée et ordre du chat personnalisable (CHAT-001) et des modèles communautaires (TEMPLATES-001). |
| DEC-030 | 2026-11-04 | Le chat personnalisable décidé en DEC-029 (points C1 à C12, livré en 0.471, phases 0 et 1). |
| DEC-031 | 2026-11-04 | Plan de première prise en main, aide, vidéos et tickets (ONBOARD-001) ; dix décisions G1 à G10. GitHub n'était plus tenu à jour depuis 0.454. |
| DEC-032 | 2026-11-04 | Conseil d'équipe du dresseur face aux règles Dalamud (combat, contenu compétitif) ; le Dédale bestial a des classements (fenêtre XBMRanking). |
| DEC-033 | 2026-11-04 | Six décisions ouvertes : release, conseil en match de Triple Triade, moment de la requête des ventes de servants, I-15, N-10, cadre des fenêtres du jeu. |
| DEC-034 | 2026-11-04 | La compilation du dépôt Dalamud utilise le SDK 10.0.101 sous Linux, réseau coupé ; global.json exigeait 10.0.403 (DEC d'origine), Tensho n'aurait pas compilé chez eux, et il n'avait jamais été compilé sous Linux. |
| DEC-035 | 2026-11-04 | Titre selon le job : sur option désactivée par défaut, le titre change sans clic au changement de job, alors que Dalamud interdit les requêtes automatiques au serveur sans action du joueur. |
| DEC-036 | 2026-11-04 | Le plan de PR sur la liste de fabrication (0.478, 0.479) ne proposait rien sur un nœud à 5 tentatives avec 809 PR : le premier nœud vu comptait comme normal, et ses PR restaient gardés. |
| DEC-037 | 2026-11-05 | Revue de toutes les propositions avec l'utilisateur : 100 points gardés pour avant la 1.0.0, 12 rejetés ([GLOBAL-PLAN](GLOBAL-PLAN.md)). |
| DEC-038 | 2026-11-05 | Tensho tourne partout en JcJ et en contenu classé ; seuls les conseils de Triple Triade y sont coupés (DEC-033). |
| DEC-039 | 2026-11-05 | Le 2026-11-05, sur le Discord de Dalamud, plusieurs membres de l'équipe jugent Tensho trop gros pour être relu et en grande partie déjà couvert par d'autres plugins. |
| DEC-040 | 2026-11-07 | Le passage des pages de l'auto-contrôle (0.724, corrigé en 0.726) ouvre chaque page sans clic ; certaines demandent alors une source communautaire activée dans les réglages (44 requêtes au premier passage, 2 au suivant grâce au cache). |
| DEC-041 | 2026-11-08 | Le document « a faire » demande des outils de contenus (Eurêka, Bozja, donjons profonds, île Lunule, raids fatals) avec des tableaux stratégiques ; DEC-009 a retiré le domaine Combat (seule exception : le dresseur, informations seulement, JcE, DEC-032). |
| DEC-042 | 2026-11-08 | Le format du fichier des découvertes n'a pas de place pour le HQ, des compteurs par valeur ou des dénominateurs (taux de butin futurs) ; le changer après la 1.0.0 obligerait à migrer les fichiers de tous les joueurs. |
| DEC-043 | 2026-11-08 | Durées des voyages de sous-marins calculées par une formule communautaire (SubmarineTracker, MIT) ; le jeu a ses propres fonctions de calcul. |
| DEC-044 | 2026-11-08 | Pêche en mer (X-85) : quel appât déclenche le courant spectral, si les données du jeu ne le disent pas. |
| DEC-045 | 2026-11-09 | Revue des propositions de G-PROPOSALS « À revoir avec l'utilisateur » (points 1 à 8). |
| DEC-046 | 2026-11-09 | Maximiseur d'équipe du dresseur (demande du 2026-11-09) alors que DEC-032 excluait une « meilleure équipe » calculée sans nouvelle décision (le Dédale bestial a des classements). |
| DEC-047 | 2026-11-09 | Aide au choix des 3 bêtes de chaque affrontement du Dédale bestial : l'écran du plateau est dans l'instance (relevé du 2026-11-09 : « Competitive cut on: territory 1339 » à l'entrée, juste après la fenêtre de mission), où DEC-038 coupe tout Tensho. |
| DEC-048 | 2026-11-10 | Propositions 9 (X-101, alerte « souhait en vente », appel réseau automatique) et 10 (X-100, regroupement des outils et des réglages) de G-PROPOSALS. |
| DEC-049 | 2026-11-10 | Proposition 11 de G-PROPOSALS (X-40) : autres usages des monstres proches. |
| DEC-050 | 2026-11-09 | Sujets confiés à l'agent le 2026-11-09 : releases, politique IA de Dalamud, licence EUPL-1.2, icône. Des DEC et des documents publics disaient encore le contraire : commits faits par l'application de bureau (DEC-009, DEC-010), release après une séance de jeu (DEC-033), politique IA suivie (DEC-010, DEC-039), EUPL « à accepter par l'utilisateur » (GAPS), icône « à garder ou remplacer » (PUBLISHING, point 7). |
| DEC-051 | 2026-11-11 | Aucune décision ne notait que les fichiers de travail de l'agent ont quitté le dépôt en 0.802, ni que l'agent fait les commits ; DEC-010 disait encore que l'utilisateur fait les commits (GitHub Desktop) et qu'aucun zip n'est plus fait. |
<!-- /docgen:decisions -->

DEC-ID: DEC-001
Timestamp: 2026-10-27 Europe/Brussels
Problem: Choisir la toolchain réelle du nouveau plugin.
Evidence: Dalamud.NET.Sdk local 15.0.0 cible net10.0-windows/C#14 ; SDK 10.0.403 trouvé hors PATH ; compilation réussie contre Hooks/dev.
Options: Utiliser .NET 9 du PATH ; installer un SDK ; utiliser le SDK .NET 10 existant.
Decision: global.json épingle .NET 10.0.403 et Dalamud.NET.Sdk 15.0.0 ; Verify.ps1 accepte un chemin explicite.
Reason: Compatibilité locale démontrée, aucune installation nécessaire.
Consequences: Utiliser le script ou le bon exécutable ; aucun ancien code repris.
References: REF-LOCAL-001, REF-SAMPLE

DEC-ID: DEC-002
Timestamp: 2026-10-27 Europe/Brussels
Problem: Vérifier le lifecycle sans exécuter le jeu.
Evidence: Les callbacks et la commande sont les seules ressources du socle.
Options: Introduire un framework de modules ; gérer chaque unsubscribe isolément ; petite pile d'acquisitions.
Decision: RegistrationScope avec libération inverse, poursuite après erreur et tests exécutables sans NuGet de test.
Reason: Couvre les échecs partiels et les rechargements sans infrastructure métier prématurée.
Consequences: Le smoke test dans Dalamud reste manuel et obligatoire avant publication ; le test JSON porte sur les paramètres managés.
References: REF-LOCAL-001

DEC-ID: DEC-003
Timestamp: 2026-10-27 Europe/Brussels
Problem: Éviter de confondre absence dans un inventaire partiel et quantité zéro.
Evidence: REF-INVENTORY, REF-SNAPSHOT et huit tests du modèle managé CORE-001.
Options: Quantité entière seule ; quantité nullable seule ; borne observée et couverture explicite par conteneur.
Decision: Snapshot immuable avec portée propriétaire/conteneur ; requête sur portée explicite, résultat ExactQuantity nullable plus ObservedQuantity minimale et Coverage. Les propriétaires et les qualités restent distincts. InventoryService conserve le dernier lot seulement, publication atomique, sans fusion implicite.
Reason: Permet aux futurs calculs de besoins d'exprimer ce qui manque en connaissance autant qu'en quantité, sans annoncer de faux zéros.
Consequences: L'adaptateur doit prouver Complete, normaliser les identifiants du jeu et garantir l'ordre de publication. Clear invalide l'état. Aucune complétude globale ni propriété personnage déduite d'une collection vide.
References: REF-INVENTORY, REF-SNAPSHOT, REF-INV-MODEL

DEC-ID: DEC-004
Timestamp: 2026-10-27 Europe/Brussels
Problem: Lire les sacs sans surinterpréter la complétude ni conserver la mémoire native.
Evidence: REF-INV-API ; compilation et sept tests du coordinateur.
Options: Déduire Complete de la longueur du span ; employer de nouveaux offsets ; rester Partial/Unknown via le contrat public.
Decision: Quatre sacs seulement ; lecture sur Update confirmé framework, copie immédiate de primitives, propriétaire contrôlé avant/après. Span vide Unknown, non vide Partial. Invalidation atomique, collecte coalescée 250 ms, reprises Unknown/erreur cinq secondes, rejet des publications invalidées.
Reason: Le contrat installé permet l'observation mais ne prouve pas la complétude des conteneurs. Ce choix évite offsets et faux zéros tout en fournissant un diagnostic réel à valider.
Consequences: Les calculs futurs doivent respecter ExactQuantity nullable. L'adaptateur ne permet pas encore d'affirmer le total possédé ; validation live séparée RUNTIME-001.
References: REF-INV-API, REF-INV-MODEL

DEC-ID: DEC-005
Timestamp: 2026-10-27 Europe/Brussels
Problem: Le compteur concorde avec l'inventaire déclaré, mais affiche toujours « total inconnu » dans 0.1.0.
Evidence: Captures utilisateur Potion 4551 et sirop 12892 ; REF-INV-LOAD ; 25 tests passants après modification.
Options: Changer seulement le texte ; garder Partial systématique ; vérifier le chargement via les structures locales installées.
Decision: Étendre DEC-004 avec les membres typés InventoryManager/InventoryContainer. Complete exige un en-tête chargé, type et taille valides, buffer stable avant/après et tous les slots lus. Limiter l'affirmation de total aux quatre sacs ; réessayer les lectures Partial comme Unknown. Livrer 0.1.1 compilée avant de solliciter un test.
Reason: Donne un sens vérifiable à Complete et permet une quantité zéro exacte dans une portée entièrement lue, sans prétendre couvrir les autres rangements.
Consequences: Nouveau chemin natif à confirmer en jeu ; aucune validation live de 0.1.1 ne peut être déduite des captures de 0.1.0.
References: REF-INV-LOAD, TESTING.md section INV-002

DEC-ID: DEC-006
Timestamp: 2026-10-27 Europe/Brussels
Problem: La fenêtre de diagnostic doit préparer une interface commune aux différents outils, à la demande utilisateur.
Evidence: Fonctions disponibles validées jusqu'à 0.1.4 ; les 17 modules métier restent à construire ; interdiction des onglets métier vides dans le prompt maître.
Options: Dix-sept onglets placeholders ; navigation par espaces existants et répertoire unifié du périmètre futur.
Decision: Cinq pages réelles avec sélection d'objet partagée. Les sept domaines futurs sont décrits dans un seul répertoire filtrable des 17 références, toutes marquées à venir. Réglages et provenance technique isolés dans leurs pages dédiées. Conserver le style Dalamud de l'utilisateur.
Reason: Prépare l'arrivée des outils sans fausse fonctionnalité, dupliquer les services ou mélanger les diagnostics avec l'usage courant.
Consequences: Version 0.2.0 ; contrôle visuel requis. CRAFT-001A reste READY et son plan est conservé dans les notes du mainteneur (hors du dépôt).
References: ROADMAP.md, TESTING.md UI-003, ARCHITECTURE.md

DEC-ID: DEC-007
Timestamp: 2026-10-27 Europe/Brussels
Problem: Inclure servants et chocobo dans les recherches quand leurs inventaires sont fermés, avec actualisation volontaire depuis leurs menus.
Evidence: Demande explicite utilisateur ; REF-BANK-API ; banque managée et persistance testées ; API de lecture des sacs déjà validée et factorisée.
Options: Scans/rafraîchissements permanents ; import manuel de snapshots complets et datés ; déduire zéro des inventaires non chargés.
Decision: Banque commune par personnage et rangement, import manuel uniquement depuis un menu visible vérifié. Copie native au framework, persistance sérialisée hors framework, remplacement par clé et conservation du cache antérieur sur erreur. Panneaux ImGui compagnons ancrés, sans injection de nœud natif. Totaux cache et live explicitement distingués.
Reason: Préserve la portée, évite le double comptage des réimports et rend les recherches possibles hors contexte servant/chocobo sans prétendre posséder une vue live des rangements fermés.
Consequences: L'utilisateur doit réimporter après transfert. Les sept pages servant sont couvertes, pas équipement/cristaux/marché. Extension chocobo indépendante et conditionnelle. Noms d'addons/attache et identité au changement de servant nécessitent un essai live après compilation.
References: REF-BANK-API, TESTING.md BANK-001, ARCHITECTURE.md Banque

DEC-ID: DEC-008
Timestamp: 2026-10-27 Europe/Brussels
Problem: L'interface sépare trop les sacs et la banque, et la recherche n'affiche pas les icônes réelles des objets.
Evidence: Demande et capture utilisateur 0.3.0 : 182 NQ en sacs et 26 HQ en cache chocobo, affichés dans deux sections longues.
Options: Conserver deux pages avec doublons ; vue commune et projection groupée ; simuler un total possédé actuel à partir des caches.
Decision: Une destination Objets & rangements avec icônes du jeu, filtres et rubriques Personnage/Chocobo/Servants. Projection pure testée réutilisant snapshots et banque. Les caches restent datés et le cumul est seulement recensé. Le filtre Personnage complet est le seul total exact au présent.
Reason: Réunit les emplacements autour de l'objet tout en conservant la fraîcheur et la portée explicites.
Consequences: Pas de migration de banque ni nouvelle lecture native ; images empruntées au provider de l'hôte. Ancienne page Banque fusionnée, gestion des imports repliable ; nouveau contrôle visuel requis en 0.4.0.
References: REF-ITEM-TEXTURES, TESTING.md UI-005, ARCHITECTURE.md vue unifiée

DEC-ID: DEC-009
Timestamp: 2026-10-31 Europe/Brussels
Problem: ACT/IINACT et un domaine Combat figuraient comme étude complémentaire ; dépôt git ajouté avec les archives des points de contrôle suivies.
Evidence: Demande utilisateur 2026-10-31 : « retire tout ce qui touche à ACT, on va pas le faire » ; « j'ai ajouté un repo git, pense à faire le nécessaire pour gitignore ».
Options: Garder ACT en étude facultative ; le retirer du périmètre. Garder les zips dans git ; les ignorer.
Decision: ACT/IINACT et Combat retirés du projet (docs mises à jour, aucun code concerné). Archives checkpoints/*.zip ignorées par git (gardées sur disque) ; logs et notes de vérification restent suivis.
Reason: Décision de périmètre de l'utilisateur ; l'historique git conserve déjà chaque version du code.
Consequences: Les zips restent créés à chaque version mais hors dépôt. Les commits sont faits automatiquement par l'application de bureau, pas par l'agent. Les zips déjà présents dans l'historique local ne sont pas réécrits.
References: .gitignore, notes du mainteneur (hors du dépôt) ; modifiée par DEC-050 (commits et releases par l'agent)

DEC-ID: DEC-010
Timestamp: 2026-10-31 Europe/Brussels
Problem: Les sauvegardes (zips de points de contrôle) et la documentation à plat devenaient lourdes et peu lisibles une fois le dépôt sur GitHub ; il fallait aussi accueillir des contributeurs.
Evidence: Demande utilisateur 2026-10-31 : « les backup ne sont plus utile si on utilise github », « réorganiser les md … suivi complet et reprise IA complète », « prendre en compte dev par la communauté open source », règles D17.
Options: Garder checkpoints/ suivi ; l'ignorer entièrement. Documents à la racine ; rangement par public.
Decision: `checkpoints/` entièrement ignoré par git (local), plus de zip par version ; preuves citées par le code dans `docs/evidence/` ; historique par version dans `CHANGELOG.md` ; documents rangés dans les notes du mainteneur (hors du dépôt) et `docs/project/`, avec la carte `docs/README.md` ; `CONTRIBUTING.md` (règles, licences, publication Dalamud et politique IA). Remplace la conséquence « zips créés à chaque version » de DEC-009.
Reason: L'historique git conserve le code de chaque version ; une entrée par public (joueur, contributeur, agent) rend la reprise plus sûre.
Consequences: L'utilisateur fait les commits (GitHub Desktop) (modifié par DEC-050 et DEC-051 : l'agent fait les commits ; sauvegardes zip locales avant une grosse modification). Les zips déjà présents dans l'historique git n'en sont retirés que sur demande explicite (réécriture d'historique). Les gros documents de `docs/project/` gardent leur partie historique, marquée comme archive.
References: .gitignore, CHANGELOG.md, docs/README.md, CONTRIBUTING.md, notes du mainteneur (hors du dépôt) ; modifiée par DEC-050 (politique IA plus suivie, commits par l'agent) et DEC-051 (fichiers de l'agent hors git depuis 0.802, commits par l'agent)

DEC-ID: DEC-011
Timestamp: 2026-10-31 Europe/Brussels
Problem: Ordre et nature des sources affichées pour « Comment l'obtenir » ; place de l'outil ALEA.
Evidence: Retours utilisateur 0.123–0.146 : « achetable au marché est un critère pas une source », « ne pas utiliser Garland si on connaît déjà », « retire l'outil ALEA en gardant la partie faite par la carte ».
Options: Tout interroger en parallèle ; données du jeu puis repli communautaire.
Decision: Données du jeu d'abord (marchands, échanges, quêtes, succès, artisanat, récolte) ; Garland Tools seulement si aucune source n'est connue ; puis section « Acquisition » du wiki, traduite dans la langue du plugin. Le marché est un critère, pas une source. L'outil ALEA est retiré (0.123) ; les ALEA restent sur la carte.
Reason: Moins de requêtes externes, données du jeu prioritaires et vérifiables.
Consequences: Certaines sources restent « inconnues » ; ne pas les déduire.
References: docs/project/ARCHITECTURE.md (Sources d'objets), CHANGELOG.md 0.123–0.150

DEC-ID: DEC-012
Timestamp: 2026-11-01 Europe/Brussels (avant 0.224)
Problem: Fixer ce qui reste à faire avant la publication D17 et ce qui ne sera pas fait.
Evidence: Revue des points ouverts avec l'utilisateur (docs/project/BACKLOG.md, « Revue des points ouverts (2026-11-01) ») ; sources vérifiées par point (GatheringOptimizer MIT, frozen_rabbit_tome sans licence, feuille FccShop, sous-commandes /alarm).
Options: Tout traiter avant publication ; publier en l'état ; trier point par point.
Decision: Faire : bonus d'Abondance estimé (0.224), coffre à apparence et armoire dans « possédé » (0.235–0.240), boutiques de compagnie libre (0.295), icônes de quête 71141 / 71151 seulement avec un relevé en jeu. Ne pas faire : score de la Revue de mode (aucune donnée, serait inventé), suppression d'une alarme du jeu depuis Tensho (DeleteAlarm non prouvé, risque d'effacer la mauvaise), sources inconnues restantes (inconnu documenté). Abandonné : injection de texte dans l'infobulle native du jeu.
Reason: Ne livrer que ce qui est prouvé par les données du jeu ou une source citée ; éviter les actions à risque sur l'état du jeu.
Consequences: Les « Ne pas faire » ne sont pas rouverts sans nouvelle source ou nouvelle demande. Les compléments des 17 outils viennent après la publication, au cas par cas.
References: docs/project/BACKLOG.md, notes du mainteneur (hors du dépôt) (« PLAN AVANT PUBLICATION »), CHANGELOG.md 0.224, 0.235–0.240, 0.295 ; cadre de publication remplacé par DEC-039 (dépôt personnalisé, pas de soumission)

DEC-ID: DEC-013
Timestamp: 2026-11-01 Europe/Brussels (0.236)
Problem: Compter l'arsenal, le coffre à apparence et l'armoire dans les stocks et le « possédé » de la Revue de mode.
Evidence: Décision de l'utilisateur notée en CHANGELOG 0.236 (« ne pas dépendre d'un autre plugin », Allagan Tools écarté) ; API vérifiées par réflexion de FFXIVClientStructs (InventoryType de l'arsenal, MirageManager.PrismBoxItemIds, UIState.Cabinet).
Options: S'appuyer sur Allagan Tools ; lire le jeu directement.
Decision: Lecture directe du jeu sur le thread framework, copie dans la Banque (arsenal en 12 conteneurs ; coffre et armoire en conteneurs virtuels 25000 et 25001), relue toutes les 2 min au plus ; coffre et armoire « inconnus » tant qu'ils n'ont pas été ouverts en jeu.
Reason: Aucune dépendance à l'installation d'un autre plugin ; même modèle que les autres rangements (DEC-007).
Consequences: Rappel « ouvrez une fois… » tant que le coffre ou l'armoire ne sont pas lus ; une tenue rangée compte par ses pièces (feuille MirageStoreSetItem, 0.240) ; limites du fichier de la Banque relevées en 0.238–0.239. 0.234–0.240 à valider en jeu.
References: CHANGELOG.md 0.235–0.240, src/TenshoAssistant/Inventory/BankInventoryBridge.Dalamud.cs, docs/project/REFERENCES.md REF-BANK-STORAGES

DEC-ID: DEC-014
Timestamp: 2026-11-01 Europe/Brussels (0.246)
Problem: Les fenêtres de Tensho passent au-dessus des fenêtres du jeu (demande R-05).
Evidence: docs/evidence/0.246-r05-window-order.md : Dalamud dessine toutes les fenêtres ImGui des plugins après l'interface du jeu, sans API pour les intercaler ; le menu de barre de titre de Dalamud (épingler, clic traversant, opacité) existe déjà.
Options: A. retrait automatique (fenêtre Tensho presque transparente quand une fenêtre du jeu la chevauche) ; B. raccourci pour masquer / réafficher ; C. rien de plus.
Decision: C, décision de l'utilisateur ; R-05 clos sans code.
Reason: Un vrai ordre d'affichage est impossible ; l'outil de Dalamud suffit.
Consequences: Quand une infobulle du jeu est cachée par une fenêtre Tensho, Tensho dessine la sienne (barre d'informations du serveur, 0.300–0.301).
References: docs/project/USER-REQUESTS.md (R-05), CHANGELOG.md 0.301

DEC-ID: DEC-015
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.246, 0.270, 0.281)
Problem: Après un échec réseau, LuminaSupplemental et GatherBuddy relançaient le téléchargement à chaque image ; une copie expirée était perdue hors ligne ; les annonces du marché n'avaient pas de cache disque.
Evidence: docs/evidence/0.246-cache-audit.md (relevé dans le code le 2026-11-01, mise à jour 0.281) ; docs/evidence/0.270-universalis-listings.md.
Options: Corriger au cas par cas ; règle commune à toutes les sources.
Decision: 5 min d'attente après un échec ; la copie expirée sert quand le réseau échoue ; chaque source lit la durée de sa zone de cache (Paramètres › Données), avec « Jamais (seulement sur demande) » ; réponses bornées en taille et en durée. Universalis : annonces et historique lus depuis la page Marché seulement, une requête à la fois, 3 s d'écart, 4 Mo au plus, dernière lecture affichée et datée en cas d'échec, aucune requête pour un objet non vendable.
Reason: Ne jamais solliciter une source en boucle ; garder une donnée datée plutôt que rien.
Consequences: Toute nouvelle source réseau suit la même règle (BoundedHttp ou bornes équivalentes, cache disque daté, zone de cache). La réponse de Teamcraft n'est contrôlée qu'après la lecture (Community/FishBiteTimes.cs), à revoir.
References: src/TenshoAssistant/Community/BoundedHttp.cs, src/TenshoAssistant/Caching/DataCache.cs, src/TenshoAssistant/Market/MarketBoardService.cs, CHANGELOG.md 0.246, 0.270, 0.281

DEC-ID: DEC-016
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.247–0.269, 0.315)
Problem: Infobulles réduites et complètes empilées pour un même objet, menus de clic droit en double ou différents selon l'endroit.
Evidence: Retours en jeu (captures) 0.250, 0.251, 0.252, 0.269 ; plan des notes du mainteneur (hors du dépôt), étape 1.
Options: Corriger liste par liste ; modèles communs.
Decision: Modèles communs (src/TenshoAssistant/UI/Components/ItemCell.cs, EntityMenus.cs, DetailPage.cs, SettingRow.cs). Toute icône d'objet donne l'infobulle complète et le menu des outils et des sources ; une seule infobulle d'objet par image (ItemCell.cs) et une seule infobulle par ligne de trajet (0.315) ; chaque menu attaché a un identifiant unique et stable (UniqueMenuId, 0.269) ; un menu par type de chose (quête, PNJ, monstre, lieu) ; même contenu pour l'infobulle Tensho et le panneau sous l'infobulle de l'inventaire du jeu (DrawItemInfoBody, 0.257), mêmes sources pour les deux menus de clic droit (SourceLines, 0.256).
Reason: Même comportement partout, moins de code dupliqué, moins de travail par image.
Consequences: Une nouvelle liste réutilise ces modèles ; pas d'infobulle « nom seul » ni de second menu superposé. Pas d'injection dans l'infobulle native (DEC-012).
References: CHANGELOG.md 0.247–0.269, 0.315 ; notes du mainteneur (hors du dépôt)

DEC-ID: DEC-017
Timestamp: 2026-11-01/02 Europe/Brussels (0.248 ; date exacte non relevée)
Problem: Le paquet livrait une copie des fichiers de langue à côté de la DLL.
Evidence: Demande de l'utilisateur : « tout doit être dans la même DLL » (CHANGELOG 0.248).
Options: Garder les fichiers à côté ; tout intégrer à la DLL.
Decision: Langues et données intégrées à la DLL (EmbeddedResource) ; le paquet contient seulement TenshoAssistant.dll, TenshoAssistant.json et TenshoAssistant.deps.json ; Verify.ps1 refuse un fichier de langue livré. Un dossier Localization/Languages posé à la main à côté de la DLL reste lu en surcharge, pour tester une traduction.
Reason: Installation et mise à jour en un seul fichier.
Consequences: Les données communautaires embarquées (0.309, 0.315) suivent la même règle (src/TenshoAssistant/TenshoAssistant.csproj).
References: scripts/Verify.ps1 (contrôle 0.248), Localization/README.md, CHANGELOG.md 0.248

DEC-ID: DEC-018
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.249, 0.258, 0.287, 0.289, 0.315)
Problem: Des réglages sans aide ni retour au défaut ; « remettre les réglages par défaut » doit viser tous les réglages sans perdre les données de l'utilisateur.
Evidence: Précision de l'utilisateur (0.258) ; défaut trouvé pendant 0.315 : « Tout remettre par défaut » effaçait les vaisseaux lus, les plans d'équipement et les plans de matérias.
Options: Réinitialiser toute la configuration ; réinitialiser les seuls réglages, données enregistrées gardées.
Decision: Chaque réglage a son infobulle (anglais et français ; un test fait échouer Verify si elle manque) et un bouton ↺ quand il diffère du défaut, lu sur une configuration neuve. « Tout remettre par défaut » (avec confirmation) remet les propriétés listées dans ConfigurationReset.Reset (Configuration.cs, depuis 0.321) ; ConfigurationReset.Kept garde version, langue, alarmes, rappels faits, carte au trésor, plan de macros, favoris de carte, préréglages d'artisanat, decks de Triple Triad, quêtes suivies, relevés, profils et modèles de tri ; depuis 0.315 aussi vaisseaux lus, plans d'équipement, matérias, secteurs débloqués, plans de montages ; depuis 0.321 l'état de l'interface (groupes de navigation repliés) ; un test exige que chaque propriété publique soit dans exactement une des deux listes. La banque (fichier à part) n'est pas touchée. Valeurs par défaut de la carte reprises des réglages de l'utilisateur (0.289) ; configurations déjà enregistrées inchangées.
Reason: Un retour au défaut ne doit jamais détruire ce que l'utilisateur a lu ou préparé.
Consequences: Toute nouvelle propriété de Configuration va dans ConfigurationReset.Kept (données) ou ConfigurationReset.Reset (réglages), dans Configuration.cs ; depuis 0.321 un test exige que chaque propriété publique soit dans exactement une des deux listes.
References: src/TenshoAssistant/UI/Components/SettingRow.cs, tests/TenshoAssistant.Tests/Program.cs (infobulles des réglages), CHANGELOG.md 0.249, 0.258, 0.287, 0.289, 0.315

DEC-ID: DEC-019
Timestamp: 2026-11-02 Europe/Brussels (0.275–0.315)
Problem: Plusieurs vues transformaient une donnée non lue en « aucun », « non obtenu » ou 0.
Evidence: Règle durable (notes du mainteneur, hors du dépôt ; DEC-003) ; cas relevés en 0.275, 0.282, 0.285, 0.296, 0.298, 0.315 ; SubmarineTracker renvoie 0 pour un secteur sans seuil (docs/evidence/0.315-vessel-builds.md).
Options: Valeur par défaut (0, croix, onglet masqué) ; état « inconnu » explicite.
Decision: Inconnu reste inconnu dans l'interface : pas de pastille ✓/✗ tant que les collections ne sont pas lues (0.275) ; un catalogue pas encore chargé garde son onglet (0.282) ; sans quête confirmée, aucune quête d'une autre cité n'est masquée (0.285) ; Grande Compagnie inconnue ou aucune : tous les sceaux restent affichés (0.296) ; type de marqueur des mandats non deviné, à relever en jeu (0.298) ; seuils d'un trajet « inconnus » si un secteur manque, jamais 0, nom de montage « ? » si une pièce manque, compagnie non lue = pas de filtre des secteurs débloqués (0.315).
Reason: Une valeur devinée est pire qu'un manque signalé.
Consequences: Les tests des modèles purs couvrent ces cas (VesselBuilds, QuestStartCities…) ; tout nouvel écran distingue lu, absent et inconnu.
References: src/TenshoAssistant/FreeCompany/VesselBuilds.cs (RouteBreakpoint), src/TenshoAssistant/MainWindow.SourceLinks.cs, CHANGELOG.md 0.275–0.315

DEC-ID: DEC-020
Timestamp: 2026-11-02 Europe/Brussels (0.311)
Problem: L'utilisateur demande d'essayer l'équipement des autres jobs : « on peut mirager tout le stuff maintenant (vérifie) ».
Evidence: Patch 7.4 (décembre 2025) : toute armure ou tout accessoire peut servir de mirage, quels que soient le job et le niveau ; armes et outils restent liés au job (presse : PC Gamer, Game Informer ; communiqué du patch, cités en CHANGELOG 0.311). Règle non sondée dans les données du jeu.
Options: Garder les listes du job ; ouvrir toutes les pièces sans distinction ; ouvrir armures et accessoires seulement, marqués.
Decision: Filtre « Tous les jobs (mirage) », désactivé par défaut : armures et accessoires de tous les jobs et niveaux ; les armes restent celles du job ; une pièce que le personnage ne peut pas porter est marquée « Mirage », essayable et partageable, et les statistiques gardent la pièce portée.
Reason: Suivre la règle du jeu actuelle sans fausser les statistiques prévues.
Consequences: Si la règle du jeu change, mettre à jour GearCatalog (AllJobs) ; page validée en jeu par l'utilisateur (rapport 2026-11-02, 0.310–0.314).
References: src/TenshoAssistant/Gear/GearCatalog.cs, src/TenshoAssistant/Configuration.cs, notes du mainteneur (hors du dépôt), étape 1b

DEC-ID: DEC-021
Timestamp: 2026-11-02 Europe/Brussels (0.309, 0.315)
Problem: Le butin et les seuils des sous-marins ne sont pas dans les données du jeu (décidés côté serveur).
Evidence: docs/evidence/0.309-submarine-routes.md, docs/evidence/0.315-vessel-builds.md ; licences MIT lues (SubmarineTracker, FFXIVGachaSpreadsheet, (c) 2023 Infi).
Options: Télécharger à l'exécution ; embarquer une copie datée ; ne pas proposer la fonction.
Decision: Embarquer seulement une donnée sous licence compatible avec la GPL v3 (MIT à ce jour), avec dans le fichier la source, le commit ou l'empreinte SHA-256, la licence et le copyright (texte MIT complet dans submarine-breakpoints.csv ; ligne « Source » dans submarine-loot.json), crédit dans le README ; valeurs gardées telles quelles ; transformation par un script versionné (tools/submarine-loot.py). Une donnée sans licence (xivgear.app, seuils de dirigeables d'Argus, frozen_rabbit_tome) est lue seulement, jamais embarquée.
Reason: Seule façon d'offrir ces outils sans inventer ; licence et crédit respectés.
Consequences: Pas de butin de dirigeable (aucune donnée sous licence compatible) ; planificateur par distance et durée et arbre de déblocage de LuminaSupplemental (GPL-3.0, embarqué avec avis) décidés le 2026-11-02 (DEC-023) ; mettre à jour = nouveau téléchargement et nouvelle empreinte. Les seuils viennent à l'origine d'une feuille sans licence (Elena / saerielle), reprise par SubmarineTracker sous MIT.
References: README.md (Credits), src/TenshoAssistant/FreeCompany/Data/, docs/project/REFERENCES.md (REF-FC-SUBMARINETRACKER, REF-FC-GACHASPREADSHEET)

DEC-ID: DEC-022
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.224, 0.277, 0.315)
Problem: Plusieurs calculs reposent sur des règles absentes des données du jeu (communautaires ou propres à Tensho).
Evidence: docs/evidence/0.224-bountiful-base.md, docs/evidence/0.315-vessel-builds.md ; CHANGELOG 0.277 (score de carte).
Options: Les présenter comme des données ; les signaler comme estimations, avec leur source.
Decision: Libellé « communautaire » ou « estimé » dans l'interface et les infobulles, source citée, valeur modifiable quand elle sert d'entrée : seuils de secteurs (« seuils communautaires »), usure (« estimation communautaire »), règle « coût ≤ capacité » (« outils communautaires »), bonus d'Abondance (« estimé », modifiable), score de carte de Triple Triade (« estimation Tensho », formule en infobulle). Les statistiques de vaisseau lues sont comparées au calcul : un écart est signalé (⚠), pas corrigé en silence.
Reason: L'utilisateur doit savoir ce qui vient du jeu et ce qui est une estimation.
Consequences: Une règle communautaire non vérifiée en jeu le reste dans l'interface jusqu'à un relevé ; voir docs/project/REFERENCES.md « Règles communautaires reprises comme faits ».
References: src/TenshoAssistant/Localization/Languages/fr.json (VesselBuilds.Sources, VesselBuilds.SurveillanceTip, VesselBuilds.Mismatch, TriadSort.ScoreHelp), src/TenshoAssistant/FreeCompany/VesselBuilds.cs

DEC-ID: DEC-023
Timestamp: 2026-11-02 Europe/Brussels (0.317)
Problem: Points « À décider / à revoir » de la feuille de route (0.316), tranchés par l'utilisateur.
Evidence: docs/project/ROADMAP.md (« Décidé le 2026-11-02 ») ; docs/evidence/0.315-vessel-builds.md ; docs/evidence/0.309-submarine-routes.md ; docs/evidence/0.312-materia.md ; fr.json au 0.316 (44 « vous », 14 textes au « tu »).
Options: Celles listées dans ROADMAP au 0.316, une question par point.
Decision: (1) Seuils des sous-marins : garder la table SubmarineTracker et signaler les 19 secteurs où elle est sous toutes les observations (« non confirmé par les relevés »). (2) Arbre de déblocage des dirigeables : données de LuminaSupplemental (GPL-3.0) embarquées dans la DLL avec l'avis de licence. (3) Dirigeables : planificateur de trajets par distance et durée, butin affiché « inconnu ». (4) Valeur des trajets de sous-marin : prix du marché (Universalis) en plus du prix PNJ, et paliers de surveillance / récupération si la source sépare les relevés par palier (sonde d'abord). (5) R-11 étape 4 : barde de chocobo et escouade (sonde d'abord pour l'escouade). (6) Rappels de l'escouade et des Planeurs d'Ehcatl : sonder une heure lisible dans la mémoire du jeu, sinon laisser de côté. (7) R-09 : après la file c) → f), données du jeu sondées d'abord. (8) Matérias : taux de réussite de la surmatérialisation lu dans les données du jeu (feuille à sonder). (9) Publication D17 : à la version 1.0.0, moment fixé par l'auteur. (10) Traductions allemande et japonaise : gardées dans le paquet, repli anglais. (11) Cadres et textures du jeu (ULD) : avant D17. (12) Textes français : vouvoiement.
Reason: Réponses de l'utilisateur du 2026-11-02 (« D'abord les décisions »).
Consequences: DEC-021 change sur deux points : l'arbre de déblocage GPL-3.0 est embarqué (même licence que Tensho, avis dans le fichier) et un planificateur de dirigeable sans butin est proposé. Les points restants de la feuille de route attendent un relevé en jeu ou une étape de la file. Le modèle `FilteredTable` est retiré du plan (choix de l'agent, règle « pas d'abstraction prématurée » ; à créer au premier besoin réel répété). Le vouvoiement est gardé par un test automatique.
References: docs/project/ROADMAP.md, notes du mainteneur (hors du dépôt), docs/project/PUBLISHING.md, tests/TenshoAssistant.Tests/Program.cs (« French texts address the player with « vous » ») ; partie soumission remplacée par DEC-039

DEC-ID: DEC-024
Timestamp: 2026-11-02 Europe/Brussels (0.318)
Problem: Règle de numérotation des versions du plugin (`<Version>` du csproj, tags `Stable-x.y.z`).
Evidence: Message de l'utilisateur du 2026-11-02 : « 0.. = numéro de version des grosses étapes du genre pour le premier release Dalamud ; .0. = les versions que je fais (nous) ; ..0 = version de la communauté ».
Options: Numérotation libre ; SemVer classique (majeur, mineur, correctif) ; règle de l'utilisateur.
Decision: X.y.z = grandes étapes, sur décision de l'utilisateur seulement (1.0.0 = première publication sur le dépôt officiel Dalamud, DEC-023) ; x.Y.0 = versions de l'utilisateur et de l'agent (le troisième nombre revient à 0) ; x.y.Z = versions de la communauté (contributions extérieures, numéro fixé par le mainteneur à la fusion, une PR peut le proposer).
Reason: Décision de l'utilisateur ; la numérotation actuelle (0.317.0) la suit déjà.
Consequences: L'agent monte seulement le deuxième nombre ; jamais le premier sans demande ; le troisième est réservé aux contributions. Documenté dans les notes du mainteneur, hors du dépôt (« À chaque version livrée ») et CONTRIBUTING.md (« Versions and releases »).
References: notes du mainteneur (hors du dépôt), CONTRIBUTING.md, src/TenshoAssistant/TenshoAssistant.csproj ; modifiée par DEC-050 (releases par l'agent, `-Major` réservé à l'utilisateur) ; sens de la 1.0.0 remplacé par DEC-039 (fin du plan global sur le dépôt personnalisé)

DEC-ID: DEC-025
Timestamp: 2026-11-02 Europe/Brussels (0.318)
Problem: Quels manques du plan c) ajouter à Tensho, dans quel ordre, et avec quels choix précis.
Evidence: docs/project/GAPS.md (103 manques : 17 plugins de référence, dépôt officiel Dalamud de 500 plugins, dépôts non officiels de 658 plugins jugés règle par règle) ; docs/evidence/0.318-plugin-gaps.json (comparaison fonction par fonction, licences lues, contre-vérification).
Options: Pour chaque manque : ajouter, plus tard ou non ; ordre par valeur ou par domaine ; choix précis maintenant ou à chaque ajout.
Decision: 101 manques retenus sur 103 ; plus tard : I-14 (tri de l'arsenal et tri automatique) et X-05 (bulles des PNJ dans le chat). Ordre : domaine par domaine, Inventaire et marché → Collections → Monde → Récolte → Artisanat → Nouveaux domaines ; dans un domaine, valeur haute et petite taille d'abord. Choix précis tranchés maintenant (colonne « Choix » de GAPS.md), notamment : solveur d'artisanat optimal proposé en deux versions au choix par un réglage (C# original dans la DLL, Raphael natif) ; si Macro Chain est actif, Tensho s'appuie sur lui ; Universalis seulement au survol ou à la demande pour les prix et les ventes des servants ; un fichier par personnage, jamais d'Account ID ; alarmes d'échéance créées d'office (alarmes Tensho, jamais /alarm d'office) ; pêche en mer dans la page Récolte, aide complète d'un coup ; rotations de collectionnables propres à Tensho selon les seuils de remise ; événements saisonniers par les données du jeu seulement ; Faux Hollows : données du jeu d'abord, puis licence compatible, sinon relevés ; PaissaDB accepté en surveillance régulière (réglage), lecture seule. Licence EUPL-1.2 : compatible avec la GPL v3 (avis requis).
Reason: Choix de l'utilisateur, lot par lot dans la conversation (2026-11-02).
Consequences: Étape g) de la file (docs/project/ROADMAP.md, notes du mainteneur hors du dépôt), après R-12, d), e) et f) ; chaque manque est une version vérifiable à part. Toute fonction qui agit d'office reste désactivable et respecte les règles Dalamud relevées dans GAPS.md (pas de requête serveur sans action du joueur, données lues seulement après ouverture par le joueur). Le solveur Raphael ajoute un fichier natif hors de la DLL : exception voulue par l'utilisateur, en option.
References: docs/project/GAPS.md, docs/project/ROADMAP.md (étape g), docs/evidence/0.318-plugin-gaps.json, CONTRIBUTING.md (licences) ; EUPL-1.2 confirmée par DEC-050

DEC-ID: DEC-026
Timestamp: 2026-11-03 Europe/Brussels (0.447)
Problem: Décisions gardées pour la fin (G-PROPOSALS, ROADMAP « Décisions reportées à la fin ») : lesquelles trancher maintenant, et comment.
Evidence: Réponses de l'utilisateur du 2026-11-03, en trois lots, dans la conversation.
Options: Pour chaque point : le choix recommandé par l'agent, ou l'alternative notée.
Decision: (1) Les choix provisoires de G-PROPOSALS sont confirmés en bloc (leurs alternatives restent des idées). (2) X-04 : un simple lien vers partake.gg (page Événements), Tensho n'en lit rien. (3) X-13 : l'annonce du conducteur du train de chasse peut être envoyée au chat (/sh, /y, /p, /fc, /ls1, /cwl1) sur clic, en plus de la copie (alternative choisie par l'utilisateur, contre le défaut recommandé). (4) Le réglage « Monstres au survol », sans effet depuis la refonte du panneau, est retiré avec son code. (5) Release Stable de 0.431 à 0.448 après ce lot. (6) Fins de ligne : normalisation faite (rien à changer : le dépôt est déjà en LF). (7) GitHub : branches main et Devloppement protégées (pas de push forcé ni de suppression), tags Stable-* protégés, Dependabot et la compilation avec tests en CI activés. (8) Inventaire (défaut 7 de l'audit 0.329) : DEC-004 gardée, l'éditeur de règles du tri reste affiché pendant la relecture. (9) Mémoquartz : seuil de 80 % comme les autres monnaies. (10) Raphael (A-03, A-04) : étudié en séance (licence, build Plogon, taille) avant tout code. (11) N-06 et N-14 : chercher une source communautaire, licence vérifiée et soumise avant de coder. (12) N-08 : journal de jardinage avec des durées tapées par le joueur.
Reason: Choix de l'utilisateur (2026-11-03).
Consequences: 0.447 (points 2, 3, 4, 8), 0.448 (point 12) ; GitHub et CI hors du plugin ; issue #24 mise à jour ; G-PROPOSALS et ROADMAP marqués « DÉCIDÉ 2026-11-03 ». L'envoi au chat (point 3) est la seule fonction de Tensho qui écrit dans un canal visible des autres joueurs : clic seulement, canal parmi une liste fixe, texte nettoyé, 5 s d'écart, vérifié de nouveau avant l'appel au jeu.
References: docs/project/G-PROPOSALS.md, docs/project/ROADMAP.md, tests/TenshoAssistant.Tests/Decisions447Tests.cs, GitHub issue #24

DEC-ID: DEC-027
Timestamp: 2026-11-03 Europe/Brussels (0.449)
Problem: Sources de N-06 (escouade) et N-14 (croisements), DEC-026 point 11 ; recherche : docs/evidence/0.448-n06-n14-sources.md.
Options: N-06 depuis le jeu seul avec sonde, ou attendre les tests en jeu ; N-14 avec les paires MIT de nick75g, ou pas de N-14 ; N-08 durées pré-remplies (Lotlab GPL-3.0) ou tapées seulement.
Decision: (1) N-06 : codé depuis les feuilles du jeu (GcArmyExpedition, GcArmyMemberGrow, GcArmyTraining) et FFXIVClientStructs, avec une sonde en jeu ; le pourcentage reste « non validé en jeu » jusqu'à la sonde. (2) N-14 : paires MIT de nick75g/FFXIV-Crossbreed-Helper, reliées aux objets par les noms anglais de la feuille Item et affichées dans la langue du jeu (« corrélation vers le fr », choix de l'utilisateur) ; noms non trouvés écartés et comptés. (3) N-08 : durées de pousse et d'entretien pré-remplies depuis Lotlab/FFXIV-Gardening-Tracker (GPL-3.0), toujours modifiables.
Reason: Choix de l'utilisateur (2026-11-03) ; la règle de réussite de l'escouade est dans les feuilles du jeu ; seules ces deux sources ont une licence compatible.
Consequences: 0.449 (point 3), 0.450 (point 2), 0.451 (point 1) ; données embarquées avec leur avis de licence, crédits ajoutés ; aucune requête réseau.
References: docs/evidence/0.448-n06-n14-sources.md, docs/project/G-PROPOSALS.md

DEC-ID: DEC-028
Timestamp: 2026-11-03 Europe/Brussels (séance ultracode, après Stable-0.451.0)
Problem: A-03 / A-04 : dépendance native Raphael (licence, build hors ligne de Plogon, taille), et solveur des suggestions pendant la synthèse.
Evidence: docs/evidence/0.451-raphael-study.md (Raphael.Net 5.0.0 MIT, raphael-rs Apache-2.0, Craftimizer publié sur D17 avec la même version figée par packages.lock.json, raphael_bindings.dll win-x64 566 784 octets).
Options: Raphael en option ou solveur C# seul ; A-04 avec le solveur Tensho (exact, depuis l'état observé) ou Raphael relancé (approximatif).
Decision: (1) A-03 : Raphael.Net 5.0.0 ajouté en option, réglage « Solveur : Tensho / Raphael », Tensho par défaut ; chaque macro Raphael rejouée dans CraftSimulation avant d'être proposée ; calcul hors du thread du jeu, annulable, un seul à la fois ; avis MIT et Apache-2.0, crédits. (2) A-04 : suggestions pendant la synthèse avec le solveur C# de Tensho, depuis l'état observé (condition et effets compris), affichage seulement.
Reason: Choix de l'utilisateur (2026-11-03, séance ultracode).
Consequences: Ordre de la séance : R-14 et A-14 d'abord, puis I-05, I-15, N-10, A-03 / A-04 ; package vérifié (raphael_bindings.dll dans latest.zip).
References: docs/evidence/0.451-raphael-study.md, docs/project/G-PROPOSALS.md, GitHub issue #28

DEC-ID: DEC-029
Timestamp: 2026-11-04 Europe/Brussels (séance de décisions pendant la campagne en jeu)
Problem: Portée et ordre du chat personnalisable (CHAT-001) et des modèles communautaires (TEMPLATES-001).
Evidence: CHAT-001 (document retiré avec le chat, DEC-030 ; voir git), notes du mainteneur (hors du dépôt) (relevé de Chat 2, Simple Tweaks, TidyChat, NoSoliciting, Chat Bubbles, addons WoW, règles de Dalamud).
Options: Tableaux de décisions des deux plans, avec recommandations.
Decision: Chat : C1 remplacement complet (contre la recommandation par couches) ; C2 tout de suite (contre la recommandation) ; C3 prévenir sans bloquer si Chat 2 est actif ; C4 ne pas demander à l'équipe Dalamud (contre la recommandation) ; C5 hooks au besoin, avec repli ; C6 historique en fichiers locaux, désactivé par défaut ; C7 couleur par métier seulement si le jeu la montre ; C8 Discord par règle choisie, nom masqué ; C9 traduction et émoticônes externes sur option, désactivées (service à choisir, contre la recommandation) ; C10 règles locales et liste partagée ; C11 code original ; C12 sections repliées et vue « essentiels ». Modèles : T1 les deux, code d'abord ; T2 tout de suite (contre la recommandation) ; T3 registre des pages tout de suite (contre la recommandation) ; T4 dossier dans ce dépôt ; T5 CC0 ; T6 galerie dans le plugin, sur clic ; T7 validation automatique et relecture ; T8 réglages en liste blanche ; T9 sept blocs en première version. T10 (case IA dans la pull request) : recommandation appliquée, le modèle la demandait déjà.
Reason: Choix de l'utilisateur, 2026-11-04.
Consequences: La campagne de tests en jeu (lots 6 à 10) passe après ces travaux, ou s'y intercale. Risques acceptés : doublon de Chat 2 possible à la publication 1.0.0 sans question préalable ; hooks de saisie à maintenir ; registre des pages refait pendant la campagne. Lignes rouges inchangées : aucun envoi automatique, code original, pas de combat. Reste à choisir pour C9 : le service de traduction et celui des émoticônes, à la phase 8.
References: CHAT-001 (document retiré, voir git), notes du mainteneur (hors du dépôt)

DEC-ID: DEC-030
Timestamp: 2026-11-04 Europe/Brussels
Problem: Le chat personnalisable décidé en DEC-029 (points C1 à C12, livré en 0.471, phases 0 et 1).
Evidence: Demande de l'utilisateur : « retire tout ce qui se rattache au chat, je prendrai Chat 2 si j'en ai besoin ».
Options: Garder le code sans le poursuivre, ou le retirer.
Decision: Chat personnalisable abandonné et retiré : page, fenêtre, réglages, textes, tests et plan CHAT-001. Annule les points C1 à C12 de DEC-029 et le point « chat » de T8. Les fonctions qui lisent les lignes du chat pour autre chose restent : liens de carte (ChatMapLinkWatcher), détection du jardinage (GardenChatWatcher), annonce de train de chasse et alarmes.
Reason: Choix de l'utilisateur, 2026-11-04.
Consequences: 0.472. L'ancien plan et le code restent dans l'historique git (commit bf30b72, 0.471.0). Les modèles communautaires (TEMPLATES-001) continuent.
References: docs/project/DECISIONS.md (DEC-029), notes du mainteneur (hors du dépôt)

DEC-ID: DEC-031
Timestamp: 2026-11-04 Europe/Brussels
Problem: Plan de première prise en main, aide, vidéos et tickets (ONBOARD-001) ; dix décisions G1 à G10. GitHub n'était plus tenu à jour depuis 0.454.
Evidence: Réponse de l'utilisateur : « oui pour tous mais tu ne gere pas completement le git issues est pas a jour » ; « je parlais demander mon accord quand tu as besoin par rapport au regle dalamud ».
Options: Les choix recommandés du plan, ou les changer un par un.
Decision: Tous les choix recommandés : l'aide avant le guide ; une proposition unique aux joueurs existants ; sources présentées, défauts inchangés ; visite selon les métiers ; formulaire GitHub prérempli, sans jeton ni serveur ; YouTube d'abord ; vidéos relues par l'utilisateur ; bandeau de première visite activé avec option ; anglais et français ; après le conseil Dresseur, avant D17. Les issues sont tenues à chaque version. Une règle Dalamud en jeu : résumé et accord de l'utilisateur.
Reason: Décision de l'utilisateur.
Consequences: Issues #11, #12, #15, #16, #30 fermées sur rapport ou preuve ; BST-001, ONBOARD-001 et deux issues de suivi ouvertes ; étiquette ultracode retirée. Exception à DEC-009 pour le conseil d'équipe du dresseur : pas décidée, soumise à l'accord de l'utilisateur.
References: notes du mainteneur (hors du dépôt)

DEC-ID: DEC-032
Timestamp: 2026-11-04 Europe/Brussels
Problem: Conseil d'équipe du dresseur face aux règles Dalamud (combat, contenu compétitif) ; le Dédale bestial a des classements (fenêtre XBMRanking).
Evidence: Réponse de l'utilisateur : « Je ne veux pas pour jcj mais que pour pve », après le résumé des règles.
Options: Informations seulement ; pas de conseil ; équipe calculée.
Decision: Panneau d'informations pour le Dédale bestial (JcE) : ennemis, actions, faiblesses une fois confirmées, ce que l'équipe couvre, bonus. Rien en JcJ. Pas de « meilleure équipe » calculée sans nouvelle décision. Exception limitée à DEC-009 : ni ACT ni mesure de combat.
Reason: Choix de l'utilisateur, avec le choix recommandé pour la forme. Le JcE n'écarte pas les règles : celle sur le combat vaut aussi en JcE, et les classements peuvent compter comme contenu compétitif.
Consequences: Contacter l'équipe d'approbation de Dalamud avant la 1.0.0 (issue #31), et retirer le panneau si elle refuse.
References: docs/evidence/0.500-xbm-columns.md, issue #34

DEC-ID: DEC-033
Timestamp: 2026-11-04 Europe/Brussels
Problem: Six décisions ouvertes : release, conseil en match de Triple Triade, moment de la requête des ventes de servants, I-15, N-10, cadre des fenêtres du jeu.
Evidence: Réponse de l'utilisateur : « Cadre fenêtre jeu à retirer rendu horrible » ; « Conseil que pve » ; « Reste OK je suis toujours au boulot donc pas de jeu ».
Options: Les conseils de l'agent, ou les changer un par un.
Decision: Cadre des fenêtres du jeu retiré. Conseils de jeu en JcE seulement : Triple Triade contre un PNJ, jamais en tournoi ni contre un joueur. Reste, conseils de l'agent : release après une session de jeu sur la dernière version ; ventes des servants, Universalis sur clic ; I-15 lors d'une séance en jeu avec l'utilisateur ; N-10 laissé de côté, seulement s'il sert à l'utilisateur.
Reason: Décision de l'utilisateur.
Consequences: Les territoires 579, 940 et 941 coupent les conseils (docs/evidence/0.510-triad-competitive.md). Point « Conseil en match » de PUBLISHING traité ; le panneau du Dédale reste à demander à l'équipe avant la 1.0.0.
References: docs/project/G-PROPOSALS.md, docs/project/PUBLISHING.md, issue #24, issue #31 ; règle de release modifiée par DEC-050

DEC-ID: DEC-034
Timestamp: 2026-11-04 Europe/Brussels
Problem: La compilation du dépôt Dalamud utilise le SDK 10.0.101 sous Linux, réseau coupé ; global.json exigeait 10.0.403 (DEC d'origine), Tensho n'aurait pas compilé chez eux, et il n'avait jamais été compilé sous Linux.
Evidence: docs/evidence/0.512-d17-build.md
Options: Attendre la PR pour le découvrir ; assouplir global.json et compiler comme eux en CI.
Decision: global.json accepte 10.0.100 et plus (latestFeature) ; Verify.ps1 prend le SDK 10.x local le plus récent ; job « Build like the Dalamud repository » dans build.yml. Choix de l'agent, sans effet sur le jeu.
Reason: Demande de l'utilisateur de relever ce qui manque pour la 1.0.0 ; correction sûre et facile à défaire.
Consequences: Le SDK local reste 10.0.403. Une nouvelle image chez Dalamud se suit dans le relevé. Le job garde le réseau : seule une vraie PR prouve la compilation réseau coupé.
References: docs/project/PUBLISHING.md, issue #31

DEC-ID: DEC-035
Timestamp: 2026-11-04 Europe/Brussels
Problem: Titre selon le job : sur option désactivée par défaut, le titre change sans clic au changement de job, alors que Dalamud interdit les requêtes automatiques au serveur sans action du joueur.
Evidence: Réponse de l'utilisateur après le résumé de la règle : « On garde juste comme ça on verra après publication dalamud ». src/TenshoAssistant/Collections/TitleChanger.Dalamud.cs
Options: Clic seulement (conseil de l'agent) ; garder et présenter dans la PR.
Decision: Gardé tel quel, sans changement de code, et déclaré dans la description du plugin et la PR.
Reason: Décision de l'utilisateur.
Consequences: L'équipe peut demander de le retirer : ce serait alors à faire avant l'approbation. À revoir après la publication.
References: docs/project/PUBLISHING.md, issue #31

DEC-ID: DEC-036
Timestamp: 2026-11-04 Europe/Brussels
Problem: Le plan de PR sur la liste de fabrication (0.478, 0.479) ne proposait rien sur un nœud à 5 tentatives avec 809 PR : le premier nœud vu comptait comme normal, et ses PR restaient gardés.
Evidence: Capture de l'utilisateur ; réponses : « tampis on va retirer la lecture de la liste de recolte on fera que par node pas sur une serie de node », « si il faut quand meme effet du lieu pour ameliorer sur une node », « si 5/5 recolte au lieu de 4 c'est pas la meme que bonus recolte supplementaire », « il faut prendre en compte tous les effet pas que celui la ».
Options: Corriger la détection du bonus et garder la série ; un seul nœud.
Decision: Plan pour le seul nœud en cours. Tous les effets du lieu lus dans les feuilles du jeu : nommés, marqués déjà comptés pour les tentatives actives ; s'ils sont inactifs, ce qui manque et ce qu'ils rapporteraient quand c'est estimable. La ligne de la liste de fabrication reste, à titre d'information (« tu peux laisser le tooltip de la liste de fabrication c'est pratique »).
Reason: Décision de l'utilisateur.
Consequences: Les réglages de bonus et de secondes entre nœuds disparaissent ; celui de la liste ne commande plus que sa ligne. « PR par tic » reste pour la barre de PR.
References: docs/evidence/0.515-node-effects.md

DEC-ID: DEC-037
Timestamp: 2026-11-05 Europe/Brussels
Problem: Revue de toutes les propositions avec l'utilisateur : 100 points gardés pour avant la 1.0.0, 12 rejetés ([GLOBAL-PLAN](GLOBAL-PLAN.md)).
Evidence: Réponses de l'utilisateur à la revue ; question « Où places-tu la 1.0.0 dans le plan ? », réponse « Après tout le plan ».
Options: Après la phase 2 (recommandé par l'agent : publication plus tôt, moins de code à examiner) ; après tout le plan.
Decision: La 1.0.0 et la soumission viennent après tout le plan global, campagne de tests en jeu comprise.
Reason: Décision de l'utilisateur : « Si c'est refuser pour la taille je pourrais rien y faire mais je veux faire un plugin complet ».
Consequences: La publication arrive plus tard et la soumission est plus grosse à examiner ; l'équipe Dalamud peut la refuser si elle la juge trop lourde (PUBLISHING, point 6). Tout ce qui demande de jouer vient en dernier, un point à la fois.
References: docs/project/GLOBAL-PLAN.md ; partie soumission remplacée par DEC-039

DEC-ID: DEC-038
Timestamp: 2026-11-05 Europe/Brussels
Problem: Tensho tourne partout en JcJ et en contenu classé ; seuls les conseils de Triple Triade y sont coupés (DEC-033).
Evidence: Demande de l'utilisateur : « prevoir que en pvp le plugin se deactive et reactive apres pvp pas de reglages fais obligatoirement pareil pour match classer triad ou dresseur ou autre tous ce qui pourrais etre detecter par SE ou cree un impact sur des classements fais un plan pas de code pour ca pour le moment ». Règle Dalamud : pas d'avantage en environnement compétitif ou JcJ.
Options: Une coupure par outil, ou une coupure de tout le plugin.
Decision: Tout le plugin se coupe et repart seul en JcJ et en contenu classé, obligatoirement, sans réglage. Complément du 2026-11-08 (accord de l'utilisateur, « y a pas de vendeur en pvp donc oui ») : la lecture des offres du marché pendant une mise en vente (UndercutWatcher), seule restée active, est coupée aussi (0.926). Plan seulement pour le moment. Choix D1 à D5 de la liste à cocher du 2026-11-05 : voir le plan. Panneau du Dédale de DEC-032 abandonné.
Reason: Décision de l'utilisateur.
Consequences: docs/project/COMPETITIVE-OFF-PLAN.md, docs/evidence/0.600-competitive-inventory.md. Codé en 0.610 sur le feu vert de l'utilisateur.
References: DEC-032, DEC-033, CONTRIBUTING.md, docs/project/PUBLISHING.md

DEC-ID: DEC-039
Timestamp: 2026-11-05 Europe/Brussels
Problem: Le 2026-11-05, sur le Discord de Dalamud, plusieurs membres de l'équipe jugent Tensho trop gros pour être relu et en grande partie déjà couvert par d'autres plugins.
Evidence: Échange noté dans docs/project/PUBLISHING.md. Décision de l'utilisateur : « on continue tampis pour dalamud mais on tiens toujours a leurs consigne et regles ».
Options: Découper en petits plugins ; se passer de Dalamud ; continuer sur le dépôt personnalisé.
Decision: Pas de soumission au dépôt officiel. Tensho continue sur le dépôt personnalisé, et le plan continue. Les règles et consignes de Dalamud restent appliquées comme si le plugin était soumis. Remplace la partie soumission de DEC-023 et DEC-037.
Reason: Décision de l'utilisateur.
Consequences: La 1.0.0 marque la fin du plan global sur le dépôt personnalisé, sans PR D17 ; c'est toujours l'utilisateur qui la décide. docs/project/PUBLISHING.md reste la liste des règles à tenir, les étapes de soumission ne sont plus à faire. Un petit plugin séparé reste possible un jour, sur sa demande. Découper Tensho en plusieurs plugins : étudié en brouillon le 2026-11-06 et abandonné par l'utilisateur (« on va pas le faire »), branche retirée.
References: DEC-023, DEC-037, docs/project/PUBLISHING.md ; modifiée par DEC-050 (politique IA plus suivie)

DEC-ID: DEC-040
Timestamp: 2026-11-07 Europe/Brussels
Problem: Le passage des pages de l'auto-contrôle (0.724, corrigé en 0.726) ouvre chaque page sans clic ; certaines demandent alors une source communautaire activée dans les réglages (44 requêtes au premier passage, 2 au suivant grâce au cache).
Evidence: docs/evidence/0.724-page-sweep.md. Choix de l'utilisateur : « Oui, en automatique et que pour dev ».
Options: Automatique en mode développeur ; seulement sur demande ; automatique sans aucune requête.
Decision: Automatique, en mode développeur seulement, avec les requêtes que les pages font à leur ouverture, dans les réglages de l'utilisateur. La page Marché reste exclue (Universalis). Hors mode développeur, rien ne se lance.
Reason: Décision de l'utilisateur.
Consequences: Le résumé du passage donne le nombre de requêtes et leurs hôtes. Un test vérifie que la fenêtre ne s'ouvre qu'en mode développeur. 0.748 (X-01, lourdeur signalée par l'utilisateur) : au plus un passage toutes les deux heures, quels que soient les rechargements.
References: DEC-038, docs/evidence/0.724-page-sweep.md

DEC-ID: DEC-041
Timestamp: 2026-11-08 Europe/Brussels
Problem: Le document « a faire » demande des outils de contenus (Eurêka, Bozja, donjons profonds, île Lunule, raids fatals) avec des tableaux stratégiques ; DEC-009 a retiré le domaine Combat (seule exception : le dresseur, informations seulement, JcE, DEC-032).
Evidence: Réponse de l'utilisateur au lot de décisions du 2026-11-08 : « On peut être beaucoup plus permissif, mais que pour préparer les combats, pas en combat, tant que FF14 ne peut pas nous détecter ; je suis ok, pareil pour autre contenu ».
Options: Élargir l'exception à la préparation des combats ; garder le combat dehors.
Decision: Exception élargie : des informations pour préparer les combats et les contenus (mécaniques, ordre, placements écrits, objectifs, progression), en JcE seulement. Rien pendant un combat (affichage masqué en combat), rien ne lit les données de combat (pas d'ACT/IINACT, pas de journal de combat), aucune action automatisée, tout coupé en JcJ et en contenu classé (DEC-038). Les règles Dalamud restent le critère (PUBLISHING.md), au-delà de ce que le jeu peut détecter.
Reason: Décision de l'utilisateur, encadrée par les règles Dalamud gardées (DEC-039).
Consequences: X-90 peut prévoir des tableaux de préparation ; résumé et accord de l'utilisateur avant chaque outil qui touche une règle Dalamud.
References: DEC-009, DEC-032, DEC-033, DEC-038, DEC-039, docs/project/GLOBAL-PLAN.md (X-90)

DEC-ID: DEC-042
Timestamp: 2026-11-08 Europe/Brussels
Problem: Le format du fichier des découvertes n'a pas de place pour le HQ, des compteurs par valeur ou des dénominateurs (taux de butin futurs) ; le changer après la 1.0.0 obligerait à migrer les fichiers de tous les joueurs.
Evidence: Plan X-76 2c (étape E1, docs/project/audits/x76-2bc-plan.md) ; réponse de l'utilisateur : « Fais-le au plus vite ».
Options: Avant la 1.0.0 ; plus tard.
Decision: Format fixé tout de suite : champs facultatifs ajoutés, version du format montée, lecture des anciens fichiers gardée, outil de fusion mis à jour.
Reason: Décision de l'utilisateur.
Consequences: Fait avant X-85.
References: DEC-039, tools/discoveries-merge.py

DEC-ID: DEC-043
Timestamp: 2026-11-08 Europe/Brussels
Problem: Durées des voyages de sous-marins calculées par une formule communautaire (SubmarineTracker, MIT) ; le jeu a ses propres fonctions de calcul.
Evidence: X-75.6 ; réponse de l'utilisateur : « Oui si lecture pure ».
Options: Fonctions du jeu si elles ne font que calculer ; garder la formule.
Decision: Les fonctions du jeu sont appelées seulement si elles ne font que calculer (aucune action, aucun envoi au serveur, aucun état modifié), sur le fil du framework ; sinon la formule reste, contrôlée contre le jeu.
Reason: Décision de l'utilisateur.
Consequences: Une sonde prouve d'abord la nature de la fonction ; preuve dans docs/evidence.
References: X-75.6, docs/project/audits/0.874-probe-coverage.md

DEC-ID: DEC-044
Timestamp: 2026-11-08 Europe/Brussels
Problem: Pêche en mer (X-85) : quel appât déclenche le courant spectral, si les données du jeu ne le disent pas.
Evidence: Réponse de l'utilisateur : « Vos relevés, puis la commu ».
Options: Relevés d'abord puis source communautaire marquée ; relevés seulement.
Decision: Les relevés en jeu de l'utilisateur d'abord ; en attendant, une source communautaire indiquée comme telle, coupée par le réglage « données du jeu et observées seulement » (X-76 étape 3).
Reason: Décision de l'utilisateur.
Consequences: Un parseur note les courants spectraux observés et l'appât du lancer.
References: X-85, X-76

DEC-ID: DEC-045
Timestamp: 2026-11-09 Europe/Brussels
Problem: Revue des propositions de G-PROPOSALS « À revoir avec l'utilisateur » (points 1 à 8).
Evidence: Réponses de l'utilisateur du 2026-11-09 (cases à cocher).
Options: Pour chaque point, la proposition recommandée ou l'autre choix (G-PROPOSALS).
Decision: 1 coupure JcJ : laissée comme aujourd'hui (travaux lancés non annulés, alerte Discord de mission gardée). 2 position dans les découvertes partagées : exacte partout, après explication (utile aux cartes des autres joueurs ; elle peut dire où vous pêchiez tel jour). 3 réglages : l'agent propose un plan des onglets et sections, rien ne change avant accord. 4 choix provisoires : déjà confirmés par DEC-026 ; revue des alternatives. 5 ventes des servants : comparaison Universalis sur clic. 6 poissons de collection : vus dans l'aide à la pêche ; y ajouter la collectionnabilité visée et les assignats. 7 fichiers de l'agent : en français (au choix de l'agent, réponses en français). 8 outils de contenus : mécaniques et stratégies du wiki (GFDL, sur clic, créditées, interrupteur Wiki), notes écrites et schéma d'arène dans la fenêtre Tensho, hors combat, jamais par-dessus le jeu, aucun marqueur posé dans le jeu ; liens navigateur pour les sites sans licence ; bestiaire des donjons profonds avec les notes du wiki, hors du donjon seulement.
Reason: Décisions de l'utilisateur.
Consequences: X-97 (aide à la pêche), X-90 (wiki, notes, schéma, bestiaire), plan des réglages à proposer.
References: G-PROPOSALS, DEC-026, DEC-038, DEC-041, docs/project/CONTENT-TOOLS.md

DEC-ID: DEC-046
Timestamp: 2026-11-09 Europe/Brussels
Problem: Maximiseur d'équipe du dresseur (demande du 2026-11-09) alors que DEC-032 excluait une « meilleure équipe » calculée sans nouvelle décision (le Dédale bestial a des classements).
Evidence: Réponse de l'utilisateur après le rappel de la règle : « équipe calculée tant que ce n'est pas pendant le combat, je pense que c'est ok ».
Options: Couverture des faiblesses seulement ; équipe calculée hors combat ; rien de plus.
Decision: Équipe calculée permise, préparée hors combat : depuis la page Tensho, avant d'entrer, d'après les faiblesses des ennemis rencontrés (relevés) et les bêtes possédées ; résultat en cache disque. Rien pendant un combat, rien à l'intérieur du Dédale (DEC-038 coupe Tensho en contenu classé), aucune action automatisée, JcE seulement. Complète DEC-032 et DEC-041.
Reason: Décision de l'utilisateur, règle Dalamud rappelée avant (dépôt personnalisé, DEC-039).
Consequences: Point X-109 du plan ; le calcul reste une aide de préparation, affichée comme telle.
References: DEC-032, DEC-038, DEC-039, DEC-041, issue #34

DEC-ID: DEC-047
Timestamp: 2026-11-09 Europe/Brussels
Problem: Aide au choix des 3 bêtes de chaque affrontement du Dédale bestial : l'écran du plateau est dans l'instance (relevé du 2026-11-09 : « Competitive cut on: territory 1339 » à l'entrée, juste après la fenêtre de mission), où DEC-038 coupe tout Tensho.
Evidence: Réponse de l'utilisateur après le rappel de DEC-038 et de la règle Dalamud (classements, avantage en contenu compétitif) : couper « au début de chaque combat ».
Options: Coupure à l'entrée, tout préparé avant ; coupure au début de chaque combat (exception).
Decision: Exception limitée à DEC-038 pour le Dédale bestial : dans ses territoires, tout Tensho reste coupé sauf le panneau d'équipe du dresseur et la lecture minimale qu'il demande, actifs seulement devant le plateau, entre les combats ; coupés dès le début de chaque combat et pendant tout combat. Aucune action automatisée, rien qui lise le combat. Le reste du contenu classé est inchangé.
Reason: Décision de l'utilisateur, règle Dalamud rappelée avant (dépôt personnalisé, DEC-039).
Consequences: X-109 : le moment du début de combat est à prouver en jeu (relevé de développement autorisé par cette exception) avant d'activer le panneau ; en doute, coupé.
References: DEC-032, DEC-038, DEC-039, DEC-041, DEC-046

DEC-ID: DEC-048
Timestamp: 2026-11-10 Europe/Brussels
Problem: Propositions 9 (X-101, alerte « souhait en vente », appel réseau automatique) et 10 (X-100, regroupement des outils et des réglages) de G-PROPOSALS.
Evidence: Réponses de l'utilisateur du 2026-11-10 (cases à cocher), après le rappel de la règle Dalamud pour l'appel automatique.
Options: X-101 automatique coupé par défaut, bouton seulement, rien ; X-100 regroupement proposé ou disposition actuelle ; réglages en onglets + recherche, recherche seule, rien.
Decision: X-101 : réglage coupé par défaut ; activé : au plus une vérification toutes les 30 min, seulement les souhaits avec un prix, 20 objets par passage, ligne de discussion quand un prix passe sous le sien ; Universalis déjà déclaré. X-100 : navigation en groupes Objets, Production, Monde, Suivi, Progression, Contenus, Gold Saucer, Références, Préférences (Accueil et Mes outils en tête) ; Paramètres en onglets Général, Panneaux à côté du jeu, Carte, Suivi et notifications, Données et sources, Avancé, Débogage, avec une recherche parmi tous les réglages ; chaque réglage reste aussi à côté de son outil.
Reason: Décisions de l'utilisateur (choix recommandés).
Consequences: X-100 et X-101 à coder.
References: G-PROPOSALS (9, 10, « Proposition X-100 »), DEC-039

DEC-ID: DEC-049
Timestamp: 2026-11-10 Europe/Brussels
Problem: Proposition 11 de G-PROPOSALS (X-40) : autres usages des monstres proches.
Evidence: Réponse de l'utilisateur : « Les trois ».
Options: a) toutes les listes d'artisanat, b) cibles du livre de relique, c) butin des souhaits ; seulement a) ; rien.
Decision: Les trois, chacun avec son réglage (couche de la carte, icône au-dessus de la tête), activés par défaut. a) étend la couche « vos listes » (X-35) qui ne comptait que la liste affichée dans un panneau.
Reason: Décision de l'utilisateur.
Consequences: X-40 codé en 0.1002.
References: G-PROPOSALS n° 11, X-35, X-40

DEC-ID: DEC-050
Timestamp: 2026-11-09 Europe/Brussels (0.1020)
Problem: Sujets confiés à l'agent le 2026-11-09 : releases, politique IA de Dalamud, licence EUPL-1.2, icône. Des DEC et des documents publics disaient encore le contraire : commits faits par l'application de bureau (DEC-009, DEC-010), release après une séance de jeu (DEC-033), politique IA suivie (DEC-010, DEC-039), EUPL « à accepter par l'utilisateur » (GAPS), icône « à garder ou remplacer » (PUBLISHING, point 7).
Evidence: Demandes de l'utilisateur du 2026-11-09 (plan du système de travail, approuvé) : l'agent gère git entièrement, releases, icône et licences comprises ; politique IA de Dalamud retirée, la déclaration d'IA du manifeste reste (elle dit vrai) ; release quand le plugin a changé, que les contrôles sont verts et que ce qui a changé est validé en jeu, correctif urgent (patch du jeu, confidentialité) sans attendre et marqué « not validated in game » ; la 1.0.0 reste la décision de l'utilisateur. EUPL v1.2 (décision d'exécution (UE) 2017/863 de la Commission, https://eur-lex.europa.eu/eli/dec_impl/2017/863/oj), lue le 2026-11-09 : l'article 5 (« clause de compatibilité ») permet de distribuer une œuvre dérivée qui combine l'œuvre et une œuvre sous licence compatible selon cette licence compatible ; l'annexe liste la GNU GPL v. 2 et v. 3 ; le même article demande de garder intacts les avis de droit d'auteur et de joindre la licence.
Options: Releases par l'utilisateur seulement ; releases par l'agent dans des garde-fous. Politique IA suivie ou non. EUPL acceptée, refusée ou laissée en attente. Icône gardée ou refaite.
Decision: 1) Releases par l'agent, seulement par `Verify.ps1 -Release` : plugin changé, contrôles refaits en entier (compilation, tests, langues, paquet), CI du commit poussé verte, fichiers de travail et adresse de webhook absents, `repo.json` relu avant son commit, essais en jeu des versions depuis la dernière release tous validés ; sinon correctif urgent seulement (`-Hotfix "<raison>"`, notes « not validated in game »). main avance en avance rapide seulement ; un retour arrière se fait par revert et une version supérieure, jamais en supprimant un tag. Un nouveau premier numéro (1.0.0 d'abord) demande `-Major`, réservé à l'utilisateur. Mises à jour Dependabot fusionnées par script (auteur Dependabot vérifié, fichiers de `.github/` seulement, CI verte) ; PR humaines : l'utilisateur. 2) Politique IA de Dalamud plus suivie comme règle (aucune soumission, DEC-039) ; la déclaration d'IA du manifeste et du README reste ; les autres règles Dalamud restent appliquées. 3) EUPL-1.2 : DEC-025 confirmée ; du code sous EUPL-1.2 peut entrer dans Tensho sous GPL v3 par la clause de compatibilité, avec ses avis de droit d'auteur gardés et le crédit (README, page Crédits). 4) Icône actuelle (générée par l'agent, déclarée) gardée.
Reason: Décisions de l'utilisateur (points 1 et 2, et délégation des points 3 et 4) ; texte officiel de l'EUPL lu pour le point 3 ; pour le point 4, rien ne demande de la refaire : elle est déclarée, et la préférence pour une icône faite main venait de la politique IA retirée.
Consequences: Modifie DEC-009 et DEC-010 (commits et releases faits par l'agent, plus par l'application de bureau ni par l'utilisateur), DEC-024 (releases par l'agent ; le premier numéro reste à l'utilisateur, garde `-Major` ; la 1.0.0 marque la fin du plan global sur le dépôt personnalisé, DEC-039), DEC-033 (« release après une session de jeu » devient le point 1 ci-dessus), DEC-039 et DEC-010 (la politique IA sort des règles suivies). PUBLISHING : points 6 et 7 fermés, point 4 aligné sur DEC-047. GAPS : EUPL-1.2 « compatible avec avis » partout. CONTRIBUTING et README disent qui essaie en jeu et quand une release part.
References: DEC-009, DEC-010, DEC-024, DEC-025, DEC-033, DEC-039, DEC-047, scripts/Release.ps1, scripts/Verify.ps1, docs/project/PUBLISHING.md, docs/project/GAPS.md, CONTRIBUTING.md, README.md

DEC-ID: DEC-051
Timestamp: 2026-11-11 Europe/Brussels (0.1082)
Problem: Aucune décision ne notait que les fichiers de travail de l'agent ont quitté le dépôt en 0.802, ni que l'agent fait les commits ; DEC-010 disait encore que l'utilisateur fait les commits (GitHub Desktop) et qu'aucun zip n'est plus fait.
Evidence: Demande de l'utilisateur du 2026-11-07 (CHANGELOG 0.802.0) : retirer du dépôt le contenu fait pour l'agent IA ; les fichiers restent sur le disque de l'auteur. Accord de l'utilisateur du 2026-11-03, élargi le 2026-11-09 (DEC-050) : l'agent gère git (commits et push sur Devloppement à chaque version). Décision de l'utilisateur du 2026-11-11 (audit du 2026-11-09, constat A039) : le noter dans une DEC.
Options: Laisser DEC-010 telle quelle ; noter la séparation et les commits dans une nouvelle DEC qui modifie DEC-010.
Decision: 1) Les fichiers de travail de l'agent (consignes, réglages, règles, état, notes, ses propres scripts) ne sont plus suivis par git depuis 0.802 et sont ignorés par `.gitignore` (bloc « 0.802 ») ; ils vivent seulement sur le disque de l'auteur, avec des sauvegardes locales. Un fichier suivi n'y renvoie jamais ; `scripts/Release.ps1` refuse de les publier. 2) L'agent fait les commits et les push sur la branche de travail à chaque version, et les releases (DEC-050) ; jamais de push forcé, de réécriture d'historique ni de suppression de tag ou de branche. 3) Les sauvegardes zip locales restent hors de git (`checkpoints/` ignoré, DEC-010) : avant une grosse modification, pas à chaque version.
Reason: Le dépôt contient ce que joueurs et contributeurs utilisent ; les notes de l'agent sont les siennes et changent à chaque tour. Le noter dans une DEC lève la contradiction avec DEC-010.
Consequences: Modifie DEC-010 (commits par l'agent, plus par l'utilisateur ; sauvegardes locales avant une grosse modification). La carte des documents (`docs/README.md`) et CONTRIBUTING ne décrivent que des fichiers suivis.
References: DEC-009, DEC-010, DEC-050, .gitignore, scripts/Release.ps1, CHANGELOG.md (0.802.0)
