# Crafting and gathering: improvement proposals (audit of 2026-11-02, step e)

> *Traduction française plus bas.*

List drawn up by a read-only multi-agent audit (0.329). **Nothing was implemented** at the time of the audit (reread on 2026-11-03: the proposals made since then, at step g, carry a "State" line): you choose what gets added
(rule of 2026-11-02, [ROADMAP](ROADMAP.md) "Decisions postponed to the end"). The defects found by the same audit are fixed in
0.330 ([CHANGELOG](../../CHANGELOG.md)). Several proposals overlap with gaps already kept for step g)
([GAPS](GAPS.md), I-*): they say so.

Value and size are the audit's estimates. "Probe": a read of the game or an in-game survey is needed before coding.

## C0 — K1 — A-01, part brought forward: choose the target quality in the crafting log panel

- **State (reread on 2026-11-05)**: already present: `DrawCraftLogTarget` and `CraftLogTargets`, a target per recipe in the crafting log panel, found during the code review.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.CraftLog.cs`.
- **What the player gets**: In the panel beside the crafting log, you choose the target: maximum quality (as today), a typed value or a percentage of the maximum. For an item handed in as NQ, a collectable with a low tier or a level-up, the proposed macro becomes shorter and costs less CP. The exact collectable tiers will come later with A-01, after the probe.
- **How, size, dependencies**: "Crafting log target" setting (maximum / value / %), with tooltip and ↺; it goes into CraftLogInput.Options, so into the cache key (CraftLogSession.CacheKey). Show "target: N / max" above the macros. Size S. Dependencies: none; the collectable tiers (CollectablesShopRefine…) stay for A-01. Probe: none for this part. Decision: default value (recommended: maximum, as today).
- **Source**: GAPS.md:72 (A-01, small, high value, Choice "Add"): the solver already accepts a typed target, only the crafting log assistant imposes maximum quality. Code: MainWindow.CraftLog.cs:55 passes recipe.Simulation!.Quality as the target; CraftSolver.cs:3 (CraftSolveOptions.TargetQuality) and MainWindow.CraftSolver.cs:124-125 (typed target in the simulator) already exist.

## C1 — K2 — X-11, brought forward: Manipulation in the proposed macro when learned

- **State (reread on 2026-11-03)**: DONE 0.419 (X-11).
- **Value**: high; main file: `src/TenshoAssistant/Craft/CraftLogObservation.cs`.
- **What the player gets**: From level 65, the macro proposed by the crafting log panel can use Manipulation, like those a crafter really uses: more durability, often more quality or fewer steps.
- **How, size, dependencies**: Step 1 (S, no probe): "Manipulation learned" checkbox per character, off by default, with tooltip and ↺, passed to CraftLogContext.Resolve. Step 2 (S, after probe): read the real unlock on the framework thread (Action.UnlockLink and the FFXIVClientStructs unlock state, to be probed), guarded by R-14, the checkbox remaining as a fallback. Size S. Dependencies: none. Decision: step 1 only or both (recommended: both).
- **Source**: GAPS.md:203 (X-11, small, high value): the proposed macro never uses Manipulation. Code: CraftLogObservation.cs:35 creates CraftStats with ManipulationAllowed=false; CraftSolver.cs:28 then removes Manipulation from the actions; the simulator already has a checkbox (MainWindow.CraftSimulation.cs:131).

## C2 — K3 — Try your saved sequences on all recipes

- **State (reread on 2026-11-05)**: DONE 0.552.
- **Value**: high; main file: `src/TenshoAssistant/Craft/CraftLogSession.cs`.
- **What the player gets**: The crafting log panel also tries your recorded sequences for other recipes (the same macro often serves a whole series of recipes of the same difficulty, whatever the job) and shows the best one that finishes and reaches the target, with its name and the recipe it was recorded for. A library of a few generic macros is then enough.
- **How, size, dependencies**: In CraftLogSession.Build (already on a cancellable worker), simulate all valid sequences, keep the best ones by CraftSolver.BetterCompletion, label "Sequence X (recorded for Y)". Setting "Try sequences from other recipes" (on, tooltip, ↺). The same in the simulator ("Try my sequences"). Size S. Probe: none. Decision: on by default (recommended: yes).
- **Source**: CraftLogSession.cs:105-106: a sequence is tried only if RecipeId, ResultItemId and CraftTypeId are those of the displayed recipe. Actions are stored by type (CraftActionKind) and exist for all 8 jobs (CraftActionCatalog.cs:198, 26 × 8 rows). At most 100 sequences (CraftPresets.cs:133). docs/evidence/0.318-plugin-gaps.json (Craftimizer): macro library = partial.

## C3 — K4 — Simulate another job without wearing it: last attributes surveyed per job and planned attributes of the Gear page

- **State (reread on 2026-11-05)**: DONE 0.554.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.CraftSimulation.cs`.
- **What the player gets**: The game's crafting log lets you browse the recipes of all jobs. The panel then calculates with the last surveyed attributes of the recipe's job (marked "surveyed on …"), instead of saying "Equip the job". The simulator offers the same values, and a "Simulate with these attributes" button on the Gear page sends the planned Craftsmanship, Control and CP (chosen pieces and materia) to the simulator.
- **How, size, dependencies**: Keep, per character and per job, the last survey (Craftsmanship, Control, CP, level, date) in the configuration (user data, like the sequences). Resolution: worn job first, otherwise the marked survey, recalculated as soon as the job is worn. Button on the Gear page when the displayed job is a crafter. Size S. Probe: none. Decision: use the surveys in the crafting log panel (recommended: yes, always marked, any food included in the survey).
- **Source**: CraftLogObservation.cs:26 and 29: calculation refused if the worn job is not the recipe's. MainWindow.CraftSimulation.cs:110-121: "Copy" requires the worn job. MainWindow.Gear.cs:286-288 already calculates the planned values (GearRules.Planned), Craftsmanship, Control and CP included for a crafter (Gear/GearCatalog.cs:148-149).

## C4 — K5 — Attribute thresholds of a sequence: minimum Craftsmanship, Control and CP

- **State (reread on 2026-11-05)**: DONE 0.553.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.CraftSimulation.cs`.
- **What the player gets**: Under a finished simulation: "This sequence holds with at least 3,950 Craftsmanship, 3,820 Control and 590 CP (margin: +120, +45, +12)". You know at once whether a cheaper meal, materia or piece is enough, or what is missing to reach the target.
- **How, size, dependencies**: Pure function CraftStatThresholds: bisection on Craftsmanship (the sequence finishes), on Control (quality ≥ target), CP = sum of the costs; bounds from the recipe (RequiredCraftsmanship, RequiredControl). Calculated on a task cancelled when the result changes, never in Draw. Size S. Dependencies: CraftSimulation.Run. Probe: none (no new data). Decision: none.
- **Source**: No such function in Craft/ (CraftSimulation, CraftSolver, CraftConditionEnvelope). The simulator gives only the final state (MainWindow.CraftSimulation.cs:180-188). ROADMAP.md:209 (CRAFT-001 PARTIAL). Progress depends only on Craftsmanship and quality only on Control in the model (CraftSimulation.cs:51-54), which allows a bisection search with the same engine.

## C5 — K6 — Follow your sequence during the synthesis: next action and gap from the plan

- **State (reread on 2026-11-05)**: DONE 0.589.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.CraftLive.cs`.
- **What the player gets**: A small panel beside the game's synthesis window follows the chosen sequence (the one from the crafting log panel or the simulator): "Step 7/22: Innovation", expected progress and quality against the game's, and a warning when a Good, Excellent or Poor condition has made the synthesis diverge. Useful for anyone crafting by hand without a macro. Display only, nothing is executed.
- **How, size, dependencies**: Reuse CraftLiveReader (opt-in observation already guarded): compare native.Step and the resources with the matching step of the simulation, show the next action and the gap. Panel attached to the Synthesis window like the crafting log one, setting with tooltip and ↺. Size M. Dependencies: "Craft" observation; A-04 will later replace the next action with a new calculation. Probe: none; in-game survey afterwards. Decision: which sequence to follow (recommended: the crafting log panel's for this recipe, otherwise the simulator's).
- **Source**: MainWindow.CraftLive.cs:8-11: the live synthesis is a page of the main window, with no panel near the game window; CraftLiveProjection.cs (summary l.15) projects only one move. docs/evidence/0.318-plugin-gaps.json: Craft Remaining (remaining progress and quality in the synthesis window) = partial; Craftimizer (next moves during the synthesis) = partial. GAPS.md:75 (A-04) remains the solver version.

## C6 — K7 — Level-adjusted recipes, required-quality recipes and Cosmic Exploration

- **Value**: high; main file: `src/TenshoAssistant/Craft/CraftSimulation.cs`.
- **What the player gets**: The simulator, the crafting log panel and the live synthesis accept the recipes now refused as "adjusted" or "required quality", including those of Cosmic Exploration, the crafters' current content, with their mission thresholds.
- **How, size, dependencies**: Probe first (tools/ApiProbe, then an in-game survey in docs/evidence): effect of MaxAdjustableJobLevel on level and attributes, meaning of RequiredQuality (failure or NQ item), WKS* sheets (recipes, thresholds). Then: accept these recipes in Unsupported with the proven rules, thresholds as targets. Size M-L. Dependencies: K1 (target), A-07 if a recipe is also expert. Decision: scope (recommended: adjusted and required-quality recipes first, Cosmic Exploration afterwards).
- **Source**: CraftSimulation.cs:32-33 refuses LevelScaled (RecipeSimulationCopy.Lumina.cs:15: MaxAdjustableJobLevel) and RequiredQuality>0, in addition to expert and specialist recipes. docs/evidence/0.318-plugin-gaps.json (Craftimizer): "Level-adjusted recipes and Cosmic Exploration (WKS notebook, synced level, mission thresholds)" = missing, no read of WKSRecipeNotebook. Absent from the A-* of GAPS.md (A-07 covers only expert recipes). ROADMAP.md:209: special recipes to complete.

## C7 — G1 — R-13, part brought forward: timed nodes open now or soon

- **State (reread on 2026-11-05)**: DONE 0.588.
- **State (reread on 2026-11-03)**: partly 0.415 (R-13: alarm item tracking window); "open now or soon" list not done.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.GatheringBrowser.cs`.
- **What the player gets**: On the Gathering page, a "Now" tab lists all timed nodes (Miner, Botanist) open in ET, then those opening within the real hour, sorted by next window, with countdown, zone, level and alarm button. Filters: job, level, hidden items. You know where to go without searching item by item.
- **How, size, dependencies**: Index of Limited timed sources, next window calculated once a minute in ET (cache, never every frame), existing ItemCell and alarm (DrawGatheringAlarmButton). Fish excluded until R-02 (weather) is done. Size S-M. Dependencies: none; R-13 will reuse the list for its pinned window. Probe: none. Decision: "soon" horizon (recommended: one real hour, setting with tooltip and ↺).
- **Source**: GAPS.md:104 (R-13, pinned window of tracked items, medium). MainWindow.GatheringBrowser.cs:65-70: the list filters only by name, zone and type, with no schedule or level. The schedules are already copied (GatheringSchedule, GatheringTime.Next). docs/evidence/0.318-plugin-gaps.json (GatherBuddy): pinned window sorted by next window = partial.

## C8 — G2 — GP plan of the gathering panel: observed quantity and effects already active

- **State (reread on 2026-11-05)**: DONE 0.515–0.517 (plan per node and place effects; triage of 2026-11-05, [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md)).
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.GatheringPanel.cs`.
- **What the player gets**: The GP plan of the in-game panel takes into account what is already bought on the node (Bountiful Yield, "Don de la terre", "Bonne augure"…) and the displayed quantity: it no longer proposes an action already active and no longer counts its effect twice. The displayed expected gain becomes reliable.
- **How, size, dependencies**: Probe first: ids of the statuses applied by each action (Lumina Status, with tools/GatheringActionsProbe), then a read of the player's statuses in GatheringObservationReader (framework, guarded by R-14). Fill GpActiveEffects and the base yield (displayed quantity minus the active effect); "active effects unknown" if the read fails. Size S-M. Dependencies: R-09 (Solid Reason, Eureka) will complete the model. Decision: none.
- **Source**: MainWindow.GatheringPanel.cs:80: GpOptimizer.Solve with a base yield of 1 and no active effect (GpActiveEffects.None), whereas the displayed rates include the bought effects (GpOptimizer.cs:43, comment). The displayed quantity is read (GatheringObservation.cs:12, DisplayedQuantity) but only shown in the observation table (MainWindow.GatheringObservation.cs:55). ROADMAP.md:214 (GP-001: native decoder to complete).

## C9 — G3 — R-02, part brought forward: "possible now?" in the fishing help

- **State (reread on 2026-11-05)**: DONE 0.568.
- **State (reread on 2026-11-03)**: partly 0.396 (R-02: next windows on the fish sheet); not in the fishing help.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.FishingPanel.cs`.
- **What the player gets**: Each fish in the fishing help and in "How to catch it" shows three badges: ET window open, current weather matching, previous weather matching (✓ / ✗ / ?). You see at once which fish are possible here, now, without cross-checking yourself with the Weather page.
- **How, size, dependencies**: Current and previous weather of the territory calculated once per 1,400 s period (cache, like MainWindow.Weather.cs), compared with the GatherBuddy ids; window by GatheringTime. Size S. Dependencies: none; the list of the next real windows stays for R-02. Probe: none. Decision: none.
- **Source**: GAPS.md:93 (R-02, small, high value): the sheet shows the ET range and the weathers separately. Code: MainWindow.FishingPanel.cs (FishRow) and MainWindow.GatheringFish.cs:57-58 display the weathers as text, with no comparison; WeatherCatalog.Forecast exists (Gathering/WeatherForecast.cs).

## C10 — G4 — Bite timer restarted on its own at each cast

- **State (2026-11-09)**: DONE 0.1011, before 1.0.0 (nothing more is planned afterwards). Cast signal: the line's CastingOut state, proven in game in 0.887 (docs/evidence/0.887-fishing-bite.md), read every frame in `Gathering/FishingBaitReader.Dalamud.cs`; the timers of the fishing help and of ocean fishing restart from that moment; the manual button stays.

- **Value**: high; main file: `src/TenshoAssistant/MainWindow.FishingPanel.cs`.
- **What the player gets**: The fishing help timer, compared with the Teamcraft bite times, restarts from zero at each cast, without clicking "Restart". The green "it may bite" range becomes right for each cast.
- **How, size, dependencies**: Probe first: a reliable cast signal (condition flags during the cast and the wait, or the FFXIVClientStructs fishing manager state), surveyed in game in docs/evidence. Then: copy on the framework thread, guarded by R-14; the manual button stays. Size S after the probe. Dependencies: G15 (reads outside Draw). Decision: none.
- **Source**: MainWindow.FishingPanel.cs:47: the timer starts only on the switch to the "fishing" state; l.55: restart by hand. Plugin.cs:199 reads only ConditionFlag.Fishing. TESTING.md:117 (0.232, to validate).

## C11 — K8 — A-13, part brought forward: simulate specialist recipes without the specialist actions

- **State (reread on 2026-11-03)**: DONE 0.434-0.435 (A-13: "Specialist" setting, specialist recipes accepted).
- **Value**: medium; main file: `src/TenshoAssistant/Craft/CraftSimulation.cs`.
- **What the player gets**: Recipes reserved for specialists (non-expert) are no longer refused: the simulator, the search and the crafting log panel calculate them with the ordinary actions, with the note "specialist actions not modelled". The macro stays valid, just less optimised.
- **How, size, dependencies**: Accept Specialist when Expert, LevelScaled and RequiredQuality are false; warning text translated in the three views. Size S. Dependencies: A-13 will add the actions afterwards. Probe: none; one specialist synthesis compared in game. Decision: none.
- **Source**: CraftSimulation.cs:32 refuses Specialist just like Expert. GAPS.md:84 (A-13): "Specialist recipes are refused"; the specialist actions only add choices. RecipeSimulationCopy.Lumina.cs:15 copies IsSpecializationRequired with no other parameter of its own.

## C12 — K9 — Starting quality calculated from the chosen HQ ingredients

- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.CraftSimulation.cs`.
- **What the player gets**: In the simulator, you state how many HQ ingredients you use; the starting quality is calculated instead of typed. You compare "3 more HQ" against "one more step" without calculating by hand.
- **How, size, dependencies**: Source to cite first: calculation rule (Craftimizer, MIT) surveyed in docs/evidence and checked in game against the starting quality read by the crafting log panel (comparison shown in developer mode). Then one row per ingredient that can be HQ (0 to quantity), cap MaxStartingQuality. Size S. Dependencies: none. Decision: none.
- **Source**: MainWindow.CraftSimulation.cs:130: starting quality typed, bounded by MaxStartingQuality. docs/evidence/0.318-plugin-gaps.json (Craftimizer): "Starting quality calculated from the number of HQ ingredients chosen" = partial. The crafting log panel already reads the real value from the game (CraftLogObservation.StartingQuality), which allows the check.

## C13 — K10 — HQ chance of a final quality

- **Value**: medium; main file: `src/TenshoAssistant/Craft/CraftSimulation.cs`.
- **What the player gets**: For a recipe that can be HQ and finishes under the maximum: "Quality 7,200 / 9,000, HQ chance ≈ 55%". Useful for levelling and for hand-ins without mandatory HQ, and to stop a search earlier.
- **How, size, dependencies**: Source first: survey the HQ chance shown by the synthesis window (AtkValue index to probe) at several quality percentages, and compare it with the Craftimizer table (MIT); record in docs/evidence. Then: pure function quality % → HQ %, shown in the simulator and the crafting log panel. Size S after the source. Dependencies: none. Decision: accept the Craftimizer table checked by surveys (recommended), or only the surveys.
- **Source**: No trace of an HQ chance in the code (search HqChance / HQ % empty). CraftLiveReader.Dalamud.cs:30-31 already reads AtkValues of the synthesis window, which shows the game's HQ chance. Rule "never invent" (the maintainer's notes (not in the repository), Method).

## C14 — K11 — A-12, part brought forward: complete recipe sheet (master recipe book, Quick Synthesis, HQ, prerequisites, figures for the gap)

- **State (reread on 2026-11-05)**: DONE 0.600.
- **State (reread on 2026-11-03)**: partly 0.427 (A-12: level, Craftsmanship, Control against the character, "Equip"); master recipe book, Quick Synthesis and HQ not done.
- **Value**: medium; main file: `src/TenshoAssistant/Craft/RecipeCatalogLoader.Dalamud.cs`.
- **What the player gets**: The recipe (Craft tab of the item sheet and crafting log panel) says: master recipe book required (and whether it is read), Quick Synthesis possible, HQ possible, specialist required, item or status required, and the figured gap "Craftsmanship 3,950 / 4,000: −50". You know why a recipe cannot be made.
- **How, size, dependencies**: Copy these Recipe columns into RecipeDefinition (catalog cache version to raise). Show in DrawCraftBook and the panel. "Read" state of the book after a probe (FFXIVClientStructs client state, to be probed), otherwise "not checked". Size S. Dependencies: the whole-set change on click stays in A-12 (step g). Decision: none.
- **Source**: GAPS.md:83 (A-12, small): CraftLogContext reports only the wrong job and the minimum. Code: CraftLogObservation.cs:33-34 (message with no gap); RecipeCatalogLoader.Dalamud.cs:131 copies neither SecretRecipeBook, nor CanQuickSynth, nor ItemRequired, nor StatusRequired (empty search in the code). The master recipe book is not named anywhere in GAPS (only as a collection type, C-04).

## C15 — K12 — More direct simulator: recipe choice in the page, result recalculated on its own, drag and drop

- **State (reread on 2026-11-05)**: DONE 0.572.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.CraftSimulation.cs`.
- **What the player gets**: The simulator has its own recipe search (name, job, level); the result updates with each action added, moved or removed, without clicking "Simulate"; actions move by drag and drop; two bars show progress and quality.
- **How, size, dependencies**: Search reusing the item catalog and RecipeCatalog.ForResult (index calculated once). Simulation restarted once per change (key of the sequence and attributes), at most 100 actions, never every frame. ImGui drag and drop. Setting "Simulate automatically" (on, tooltip, ↺). Size S. Probe: none. Decision: automatic recalculation by default (recommended: yes).
- **Source**: MainWindow.CraftSimulation.cs:97: without a recipe, the page says to go through the item sheet; l.169-171: simulation on click only; l.205-206: moving by ↑/↓ buttons. docs/evidence/0.318-plugin-gaps.json (Craftimizer): macro editor = partial.

## C16 — K13 — Sequence library: search, rename, duplicate, copy and paste as text (A-08, text part)

- **State (reread on 2026-11-05)**: DONE 0.590.
- **State (reread on 2026-11-03)**: partly 0.425 (A-08: import of a pasted macro); search, rename, duplicate not done.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.CraftPresets.cs`.
- **What the player gets**: The page of saved sequences can be searched by name, item or job and sorted; a sequence can be renamed, duplicated, copied as macro text and, the other way round, a pasted /ac macro becomes a sequence that can be checked in the simulator.
- **How, size, dependencies**: Filters and sort cached; pure Rename and Duplicate in CraftPresets (same Commit); copy by CraftMacroExporter; parsing of the /ac lines against the catalog's action names in the client language, unknown lines reported. Size S-M. Dependencies: the online links of A-08 stay for step g. Probe: none. Decision: none new.
- **Source**: MainWindow.CraftPresets.cs:40-63: raw list (Load, Delete), with no search or renaming; CraftPresets.cs:137-155: Create, Add, Remove only. GAPS.md:79 (A-08, Choice: pasted text and Teamcraft / Craftingway links). docs/evidence/0.318-plugin-gaps.json (Craftimizer): library (rename, copy, save as) = partial.

## C17 — K14 — Macro export options: separate waits (X-12), sound, final echo, icon

- **State (reread on 2026-11-05)**: DONE 0.573.
- **State (reread on 2026-11-03)**: partly 0.419 (X-12: separate wait for buffs); sound, final echo, icon not done.
- **Value**: medium; main file: `src/TenshoAssistant/Craft/CraftMacroExport.cs`.
- **What the player gets**: You set the wait of the actions and that of the effects separately, choose the end sound (or none), the echo on every page or only on the last one, and the icon of the macros written in the game instead of the imposed "?".
- **How, size, dependencies**: Fields in CraftSearchSettings (configuration), each with tooltip and ↺, same bounds as the current wait; they go into the crafting log cache key. Icon: list of the game's macro icons (sheet to probe) or that of the crafted item if valid. Size S. Probe: macro icon sheet. Decision: default values (recommended: unchanged).
- **Source**: GAPS.md:204 (X-12): fixed waits of 2 s / 3 s and a single extra (CraftMacroExport.cs:13-17, 34). CraftMacroExport.cs:42: echo with fixed <se.1> on every page. Macros/MacroWriter.Dalamud.cs:16: icon 66001 imposed. docs/evidence/0.318-plugin-gaps.json (Craftimizer, Macro Mate): export options and icon choice = partial / missing.

## C18 — K15 — Crafting chain: buttons per step, identical steps grouped, "done" checkboxes

- **State (reread on 2026-11-05)**: DONE 0.591.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.CraftChain.cs`.
- **What the player gets**: Each step of the chain has its "Log" buttons (opens the game's crafting log on the recipe) and "Simulate"; repeated steps of the same recipe are shown grouped (×N in total); you tick the steps done during the session.
- **How, size, dependencies**: Grouping at display only (sum by RecipeId, order of first appearance), the calculation does not change. Buttons reusing OpenCraftLog (on click). Checkboxes kept for the session (or in the configuration, to be chosen). Size S-M. Dependencies: A-05 (multi-item lists) will take over the view. Probe: none. Decision: checkboxes kept between sessions or not (recommended: session only).
- **Source**: MainWindow.CraftChain.cs:53-62: a step = icon and text, with no action. Craft/CraftChain.cs:93: "Do not merge repeated steps" (order kept for the dependencies). OpenCraftLog and OpenCraftSimulation already exist (MainWindow.CraftLog.cs:26, MainWindow.CraftSimulation.cs:74).

## C19 — K16 — Where are my materials: detail per storage in the preparation and the chain

- **State (reread on 2026-11-05)**: DONE 0.559.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.CraftPlan.cs`.
- **What the player gets**: On hovering "Surveyed" of an ingredient: "Bags 12 · Retainer Lune 40 (surveyed 01/11) · Saddlebag 8". You know which retainer to call before crafting, without opening the sheet of each item.
- **How, size, dependencies**: Common tooltip (same rendering as the Stocks tab of the sheet) on the cells of the preparation and of the chain's materials, from the Rows already calculated; nothing recalculated every frame. Size S. Probe: none. Decision: none.
- **Source**: MainWindow.CraftPlan.cs:61: only the total is shown; the chain shows only what is missing (MainWindow.CraftChain.cs:66-86). ItemHoldingsSummary already keeps the detail (Inventory/ItemHoldings.cs:11-17, StorageHolding: category, name, coverage).

## C20 — K17 — A-09, part brought forward: open the preparation or the chain in Teamcraft

- **State (reread on 2026-11-03)**: DONE 0.423 (A-09: Teamcraft link from the preparation).
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.CraftChain.cs`.
- **What the player gets**: A "Teamcraft" button copies (or opens on click) the import link of the prepared item and its quantity; the list appears in Teamcraft with its sub-crafts. Tensho itself sends nothing.
- **How, size, dependencies**: Pure link-building function (tested), button in DrawCraftPlan and DrawCraftChain; the browser is opened on click only. Size S. Dependencies: A-05 will extend this to several items. Probe: one link tried by hand. Decision: copy or open (recommended: both buttons).
- **Source**: GAPS.md:80 (A-09, small, Choice "Add"): public format https://ffxivteamcraft.com/import/ + base64 of "itemId,recipeId,quantity" separated by ";", with no network request. No trace in the code (search for ffxivteamcraft.com/import empty according to docs/evidence/0.318-plugin-gaps.json).

## C21 — K18 — "What can I craft?" with my stocks

- **State (reread on 2026-11-05)**: DONE 0.592.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.Items.cs`.
- **What the player gets**: A list of the recipes that can be made right now with your bags and the saved Bank, filtered by job and level, with the number of possible crafts. Handy for using up materials or levelling a job.
- **How, size, dependencies**: Calculation on a cancellable worker on click (all recipes, direct ingredients), result kept as long as the inventory and the Bank do not change; lower bound (≥) when a stock is unknown, as today. Size M. Dependencies: none. Probe: none. Decision: direct ingredients only or sub-crafts included (recommended: direct first).
- **Source**: MainWindow.Items.cs:637 (GetCraftableCount) serves only the displayed recipe (l.389). docs/evidence/0.318-plugin-gaps.json: WachuMakeyMaking? (recipes that can be made with one's resources) = partial; LazyCrafter (catalog of recipes that can be made) = partial.

## C22 — K19 — Free company workshop projects: materials per part and per phase

- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.FreeCompany.cs`.
- **What the player gets**: For a submarine or airship part, or another workshop project, the Free Company page lists the materials of each phase against your stocks, with a link to the crafting chain of each.
- **How, size, dependencies**: Probe first: columns of CompanyCraftSequence, CompanyCraftPart, CompanyCraftProcess, CompanyCraftSupplyItem (tools/ApiProbe), recorded in docs/evidence. Then copy on the framework, "Workshop" tab of the Free Company page, stocks by ItemHoldings, links to the chain. Size M. Dependencies: R-07. Decision: location (recommended: tab of the Free Company page).
- **Source**: docs/evidence/0.318-plugin-gaps.json (Artisan): "Free company workshop projects: materials per project, part and phase" = missing, no trace of CompanyCraft*; absent from the A-* of GAPS.md. MainWindow.VesselBuilds.cs covers only the parts' statistics (R-07).

## C23 — K20 — Next action highlighted on the hotbars

- **State (reread on 2026-11-05)**: DISCARDED at the review with the user ([global plan](GLOBAL-PLAN.md)).
- **Value**: medium; main file: `src/TenshoAssistant/Craft/CraftLiveReader.Dalamud.cs`.
- **What the player gets**: During a followed synthesis (K6, then A-04), the advised action flashes on your bars like a combo action of the game. You craft by hand without taking your eyes off the bars. Nothing is executed.
- **How, size, dependencies**: New read/return-only hook (ActionManager.IsActionHighlighted), created only when the setting is on (off by default, tooltip, ↺), guarded by R-14 (cleanly disabled if the signature changes), released on Dispose. Size M. Dependencies: K6 or A-04 for the advised action. Probe: signature in FFXIVClientStructs. Decision: accept a new hook (recommended: yes, off by default).
- **Source**: docs/evidence/0.318-plugin-gaps.json (Craftimizer): highlight of the suggested action on the bars (IsActionHighlighted hook) = missing. GAPS.md:75 (A-04, display only). Craftimizer, which does it, is in D17 (GAPS.md:74, note).

## C24 — G8 — Remaining need of the preparation in the gathering and fishing helps

- **State (reread on 2026-11-05)**: DONE 0.560.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.GatheringPanel.cs`.
- **What the player gets**: At the gathering or fishing spot, the item useful for your last crafting chain is marked: "14 more for Ingot of…". The number goes down as you gather.
- **How, size, dependencies**: Remaining = shortfall at the calculation − (current stock − stock at the calculation), bounded at 0; read from the chain session state, with no recalculation. Same note in the fishing help. Size S. Dependencies: none; A-05 (lists) will extend it. Probe: none. Decision: none.
- **Source**: MainWindow.GatheringPanel.cs:44-60: an item's row shows success, boon, Bountiful and stock, with no link to the preparation. The chain keeps the shortfalls and the stocks of the moment of the calculation (Craft/CraftChain.cs:9-14, Materials and Observations).

## C25 — G9 — R-14 (gathering), part brought forward: source type of the chain's raw materials

- **State (reread on 2026-11-03)**: DONE 0.416 (R-14: raw materials of the chain as a gathering list by zone, those without a location apart).
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.CraftChain.cs`.
- **What the player gets**: Each missing raw material carries its sources as icons (Miner, Botanist, fishing with a schedule or not, NPC vendor, monster, market) and a grouping by zone at the top. You see at a glance what is gathered, bought or hunted.
- **How, size, dependencies**: Labels calculated once per chain result (cache by result identity), grouping by zone of the gathering sources; detailed schedules and the route stay for R-14. Size S. Dependencies: I3 (NPC prices) for the price next to the vendor. Probe: none. Decision: none.
- **Source**: GAPS.md:105 (R-14: gathering list by zone from a chain, medium). MainWindow.CraftChain.cs:79-81: a "Places" button opens the materials one by one. The data exist: GatheringCatalog.ForItem, NPC vendor catalog, LuminaSupplemental.

## C26 — G5 — R-04, part brought forward: bait stock in the fishing help

- **State (reread on 2026-11-03)**: DONE 0.398 (R-04: bait in hand and its stock).
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.FishingPanel.cs`.
- **What the player gets**: Under each bait of the route: "×37", in red at zero. You no longer go fishing a fish without its bait. Detection of the bait worn will come with R-04.
- **How, size, dependencies**: Same GetRecipeHoldings as for the fish (existing cache), in the fishing help and in "How to catch it". Size S. Dependencies: R-04 for the bait worn (probe PlayerState.FishingBait). Probe: none for this part. Decision: none.
- **Source**: GAPS.md:95 (R-04, small, high value: bait in hand and "how many are left"). MainWindow.FishingPanel.cs:85: the bait tooltip gives the name and the right click, with no stock; the fish already have their stock (l.76-77, GetRecipeHoldings).

## C27 — G6 — Node class (normal, ephemeral, legendary…) and folklore tome required

- **Value**: medium; main file: `src/TenshoAssistant/Gathering/GatheringCatalogLoader.Dalamud.cs`.
- **What the player gets**: Each node states its class and, for a legendary node, the folklore tome needed (and whether it is read). You no longer make the trip to a node you cannot open.
- **How, size, dependencies**: Probe first: values of GatheringPoint.Type and the link GatheringPoint.GatheringSubCategory → FolkloreBook (tools/GatheringProbe), "tome read" state (client state to probe). Copy into GatheringSource (cache version to raise), show in the sheet and the list. Size S. Dependencies: none. Decision: none.
- **Source**: ROADMAP.md:212 (GATHER-001: "Intuition/prerequisites to complete"). The folklore tome is read only for fish (Gathering/FishingNotebookCopy.Lumina.cs:11); GatheringCatalogLoader.Dalamud.cs:221 handles type 8 apart without naming it; GatheringSource has no node class (Gathering/GatheringCatalog.cs:5-7).

## C28 — G7 — Intuition duration and countdown

- **Value**: medium; main file: `src/TenshoAssistant/Community/GatherBuddyFish.cs`.
- **What the player gets**: For a fish with predators: "Intuition: 350 s after 3 × X". When Intuition is active, the fishing help shows the time remaining. Essential for big fish.
- **How, size, dependencies**: First check in the GatherBuddy source (Apache-2.0) that this first number is the duration in seconds, and record it in docs/evidence; then read it. Countdown: Intuition status to probe (Lumina Status), read on the framework. GatherBuddy cache version to raise. Size S. Dependencies: none. Decision: none.
- **Source**: Community/GatherBuddyFish.cs:76-78: only the (fish, number) pairs of the predators are kept, the first number of the call is ignored. docs/evidence/0.318-plugin-gaps.json (GatherBuddy): "Intuition duration (predators)" = partial. ROADMAP.md:212: "Intuition to complete".

## C29 — G10 — R-15, part brought forward: spearfishing fish in the Gathering page

- **State (reread on 2026-11-03)**: partly: spearfishing log on the Gathering page (0.402) and spearfishing help beside the game window (0.417).
- **Value**: medium; main file: `src/TenshoAssistant/Gathering/GatheringCatalogLoader.Dalamud.cs`.
- **What the player gets**: Spearfishing fish appear in the search of the Gathering page with their location, like the other resources. The help on the spearfishing window will come with R-15.
- **How, size, dependencies**: Probe first (tools/GatheringProbe): number of SpearfishingItem references, link with SpearfishingNotebook and the locations, recorded in docs/evidence. Then "spearfishing" source rows (Fisher job, AlarmSchedule.JobOf already ready). Size S-M. Dependencies: none. Decision: none.
- **Source**: GAPS.md:106 (R-15). GatheringCatalogLoader.Dalamud.cs:179 ignores any node reference that is not a GatheringItem; no read of SpearfishingItem in the code (empty search). docs/evidence/0.203-gathering-jobs.md: types 4-5 (spearfishing) exist with placeholder names.

## C30 — G11 — Local record of your catches and personal bite times

- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.FishingPanel.cs`.
- **What the player gets**: Tensho notes at your end each catch (fish, place, bait, bite time, hookset) and shows your own ranges beside Teamcraft's, per place. Nothing is sent.
- **How, size, dependencies**: Probe first: source of the catch event (game message in the chat, or a read of the fishing state), recorded in docs/evidence. Separate file like the Bank, writing outside Draw, bounded. Size M. Dependencies: G4 (cast detected). Decision: source of the event (recommended: game message, no hook) and retention period.
- **Source**: docs/evidence/0.318-plugin-gaps.json (GatherBuddy): "Local records of catches (time, bait, size, hookset, chum), statistics per place" = missing; absent from the R-* of GAPS.md. The timer already exists (MainWindow.FishingPanel.cs:47-56).

## C31 — G12 — Weather of several zones in one table (favourite zones)

- **State (reread on 2026-11-05)**: DONE 0.563.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.Weather.cs`.
- **What the player gets**: The Weather page shows the current and next weather of your favourite zones, or of all the zones of an expansion, on a single grid. Useful for chaining fish and nodes according to the weather.
- **How, size, dependencies**: List of favourite zones in the configuration (user data), grid recalculated once per 1,400 s period; click on a zone = current view. Size S. Dependencies: none. Probe: none. Decision: number of periods (recommended: 6).
- **Source**: MainWindow.Weather.cs:15-35: a single zone at a time. docs/evidence/0.318-plugin-gaps.json: GatherBuddy (forecasts of all zones in a table) = partial; Kangas Tweaks (60 periods, favourite zones) = partial.

## C32 — G13 — Commands "/tensho recolte" and "/tensho recette" (follow-up of I16)

- **State (reread on 2026-11-05)**: DONE 0.555, with I16.
- **Value**: medium; main file: `src/TenshoAssistant/Plugin.cs`.
- **What the player gets**: "/tensho recolte minerai de fer" opens the Gathering page on the item, "/tensho recette …" opens its Craft tab; also usable in a game macro, like GatherBuddy's /gather.
- **How, size, dependencies**: Same argument parsing as I16, OpenGathering and the Craft tab; French and English words; command help translated. Size S. Dependencies: I16. Probe: none. Decision: keywords (recommended: recolte/gather and recette/recipe).
- **Source**: Plugin.cs:376-380: only "peche" is recognised. ITEMS-PROPOSALS.md:122-127 (I16: "/tensho objet" and "/tensho prix"). docs/evidence/0.318-plugin-gaps.json (GatherBuddy): search by name with /gather = partial.

## C33 — G14 — A single in-game survey session for crafting and gathering, before coding

- **Value**: medium; main file: `docs/project/TESTING.md`.
- **What the player gets**: A guided session surveys at once, in developer mode, the game values on which several proposals and retained gaps depend. The following versions then move forward without waiting for one survey each.
- **How, size, dependencies**: Survey screen in developer mode, read-only: copy on the framework thread, guarded by R-14, "Copy" button; results recorded in docs/evidence. The in-game validations stay for step h (user's rule). Size S-M. Dependencies: none. Decision: time of the session (recommended: just before step g, Gathering).
- **Source**: Surveys requested: K2 (Manipulation unlock), K7 (adjusted level, WKS), K10 (HQ chance shown), K11 (master recipe book read), G2 (statuses of GP actions), G4 (cast), G6 (node type, folklore), G7 (Intuition), G10 (spearfishing); GAPS.md:72 (A-01, CollectablesShopRefine), GAPS.md:95-96 (R-04 PlayerState.FishingBait, R-05 IsFishCaught). ROADMAP.md:85: estimated Bountiful bonus to compare in game.

## C34 — K21 — Today's Grand Company supply and provisioning missions

- **State (reread on 2026-11-05)**: DONE 0.469 (`Items/GcSupplyToday.cs`; triage of 2026-11-05, [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md)).
- **Value**: low; main file: `src/TenshoAssistant/MainWindow.Reminders.cs`.
- **What the player gets**: The list of items requested today by your Grand Company (crafters and gatherers), with your stocks and a link to the recipe or the gathering. Handy for levelling.
- **How, size, dependencies**: Probe first: where the client keeps the day's list after the supply window or the timers is opened (FFXIVClientStructs, to probe), no request triggered by Tensho. Then copy on the framework, guarded by R-14, shown on the Reminders page. Size M. Dependencies: none. Decision: location (recommended: Reminders page).
- **Source**: No trace in the code or in GAPS.md (search for "approvisionnement" empty; C-14 covers only guildleves, GAPS.md:145). List drawn from the server: readable only after the player opens the window (GAPS.md:45-47).

## C35 — G15 — Fishing help: game reads and first tables outside Draw

- **State (reread on 2026-11-05)**: DONE 0.604.
- **Value**: low; main file: `src/TenshoAssistant/Plugin.cs`.
- **What the player gets**: Nothing visible, except no stutter at the first opening of the fishing help or the gathering panel, and a help that stays in line with the project's rules after an update.
- **How, size, dependencies**: Copy job, territory, position and fishing state on IFramework.Update (like the other readers); build the two tables with the gathering catalog (worker, cache), then read without an Excel sheet. Size S. Probe: none. Decision: none.
- **Source**: ARCHITECTURE.md:110-116 (known gaps, undecided): fishing help (player, territory, condition at every frame; FishingSpot table built at the first call), Bountiful II base built at the first call. Plugin.cs:195-218 and 222-232. The maintainer's notes (not in the repository), line 9 (nothing massive in Draw).

## C36 — G16 — R-14, crafting and gathering part: an unreadable help says so instead of disappearing

- **State (reread on 2026-11-05)**: DONE 0.601.
- **Value**: low; main file: `src/TenshoAssistant/Craft/CraftLogReader.Dalamud.cs`.
- **What the player gets**: After a game update, the crafting log panel, the gathering help or the live synthesis show "unavailable after a game update" in their usual place, and Diagnostics says which read failed. Today, the panel disappears without a word.
- **How, size, dependencies**: Status per reader (log, synthesis, gathering, fishing) published with the state, short translated message at the panel's location, line in Diagnostics. Size S. Dependencies: full R-14 pass at step g2. Probe: none. Decision: none.
- **Source**: USER-REQUESTS.md:32 (R-14) and ROADMAP.md:65 (g2). CraftLogReader.Dalamud.cs:83 clears the observation: the panel is no longer shown, the error appears only in the preparation (MainWindow.CraftPlan.cs:188). MainWindow.GatheringPanel.cs:21-22 hides itself on an observation in error. Same approach as MAP-PROPOSALS.md:115 (P15, map part).

---

# Artisanat et récolte : propositions d'amélioration (audit du 2026-11-02, étape e)

Liste établie par un audit multi-agents en lecture seule (0.329). **Rien n'était implémenté** à l'audit (relu le 2026-11-03 : les propositions faites depuis, à l'étape g, portent une ligne « État ») : vous choisissez ce qu'on ajoute
(règle du 2026-11-02, [ROADMAP](ROADMAP.md) « Décisions reportées à la fin »). Les défauts trouvés par le même audit sont corrigés en
0.330 ([CHANGELOG](../../CHANGELOG.md)). Plusieurs propositions recoupent des manques déjà retenus pour l'étape g)
([GAPS](GAPS.md), I-*) : elles le disent.

Valeur et taille sont les estimations de l'audit. « Sonde » : une lecture du jeu ou un relevé en jeu est nécessaire avant de coder.

## C0 — K1 — A-01, partie anticipée : choisir la qualité visée dans le panneau du carnet

- **État (relu le 2026-11-05)** : déjà présent : `DrawCraftLogTarget` et `CraftLogTargets`, cible par recette dans le panneau du carnet, repérés à la revue du code.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.CraftLog.cs`.
- **Ce que le joueur obtient** : Dans le panneau à côté du carnet de synthèse, vous choisissez la cible : qualité maximale (comme aujourd'hui), une valeur tapée ou un pourcentage du maximum. Pour un objet livré en NQ, un collectionnable à palier bas ou une montée de niveau, la macro proposée devient plus courte et coûte moins de PS. Les paliers exacts des collectionnables viendront ensuite avec A-01, après la sonde.
- **Comment, taille, dépendances** : Réglage « Cible du carnet » (maximum / valeur / %), avec infobulle et ↺ ; il entre dans CraftLogInput.Options, donc dans la clé du cache (CraftLogSession.CacheKey). Afficher « cible : N / max » au-dessus des macros. Taille S. Dépendances : aucune ; les paliers des collectionnables (CollectablesShopRefine…) restent pour A-01. Sonde : aucune pour cette partie. Décision : valeur par défaut (recommandé : maximum, comme aujourd'hui).
- **Source** : GAPS.md:72 (A-01, petit, valeur haute, Choix « Ajouter ») : le solveur accepte déjà une cible tapée, seul l'assistant du carnet impose la qualité maximale. Code : MainWindow.CraftLog.cs:55 passe recipe.Simulation!.Quality comme cible ; CraftSolver.cs:3 (CraftSolveOptions.TargetQuality) et MainWindow.CraftSolver.cs:124-125 (cible tapée dans le simulateur) existent déjà.

## C1 — K2 — X-11, anticipé : Manipulation dans la macro proposée quand elle est apprise

- **État (relu le 2026-11-03)** : FAIT 0.419 (X-11).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Craft/CraftLogObservation.cs`.
- **Ce que le joueur obtient** : À partir du niveau 65, la macro proposée par le panneau du carnet peut utiliser Manipulation, comme celles qu'un artisan utilise vraiment : plus de solidité, souvent plus de qualité ou moins d'étapes.
- **Comment, taille, dépendances** : Étape 1 (S, sans sonde) : case « Manipulation apprise » par personnage, désactivée par défaut, avec infobulle et ↺, passée à CraftLogContext.Resolve. Étape 2 (S, après sonde) : lecture du déblocage réel sur le thread du framework (Action.UnlockLink et état de déblocage FFXIVClientStructs, à sonder), gardée R-14, la case restant en secours. Taille S. Dépendances : aucune. Décision : étape 1 seule ou les deux (recommandé : les deux).
- **Source** : GAPS.md:203 (X-11, petit, valeur haute) : la macro proposée n'utilise jamais Manipulation. Code : CraftLogObservation.cs:35 crée CraftStats avec ManipulationAllowed=false ; CraftSolver.cs:28 retire alors Manipulation des actions ; le simulateur a déjà une case (MainWindow.CraftSimulation.cs:131).

## C2 — K3 — Essayer ses séquences sauvegardées sur toutes les recettes

- **État (relu le 2026-11-05)** : FAIT 0.552.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Craft/CraftLogSession.cs`.
- **Ce que le joueur obtient** : Le panneau du carnet essaie aussi vos séquences enregistrées pour d'autres recettes (la même macro sert souvent à toute une série de recettes de même difficulté, quel que soit le métier) et affiche la meilleure qui termine et atteint la cible, avec son nom et la recette d'origine. Une bibliothèque de quelques macros génériques suffit alors.
- **Comment, taille, dépendances** : Dans CraftLogSession.Build (déjà sur un worker annulable), simuler toutes les séquences valides, garder les meilleures par CraftSolver.BetterCompletion, étiqueter « Séquence X (enregistrée pour Y) ». Réglage « Essayer les séquences des autres recettes » (activé, infobulle, ↺). Même chose dans le simulateur (« Essayer mes séquences »). Taille S. Sonde : aucune. Décision : activé par défaut (recommandé : oui).
- **Source** : CraftLogSession.cs:105-106 : une séquence n'est essayée que si RecipeId, ResultItemId et CraftTypeId sont ceux de la recette affichée. Les actions sont stockées par type (CraftActionKind) et existent pour les 8 métiers (CraftActionCatalog.cs:198, 26 × 8 lignes). 100 séquences au plus (CraftPresets.cs:133). docs/evidence/0.318-plugin-gaps.json (Craftimizer) : bibliothèque de macros = partial.

## C3 — K4 — Simuler un autre métier sans le porter : derniers attributs relevés par métier et attributs prévus de la page Équipement

- **État (relu le 2026-11-05)** : FAIT 0.554.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.CraftSimulation.cs`.
- **Ce que le joueur obtient** : Le carnet du jeu laisse parcourir les recettes de tous les métiers. Le panneau calcule alors avec les derniers attributs relevés du métier de la recette (marqués « relevés le … »), au lieu de dire « Équipez le métier ». Le simulateur propose les mêmes valeurs, et un bouton « Simuler avec ces attributs » sur la page Équipement envoie l'Habileté, le Contrôle et les PS prévus (pièces et matérias choisies) au simulateur.
- **Comment, taille, dépendances** : Garder par personnage et par métier le dernier relevé (Habileté, Contrôle, PS, niveau, date) dans la configuration (données de l'utilisateur, comme les séquences). Résolution : métier porté d'abord, sinon relevé marqué, recalcul dès que le métier est porté. Bouton sur la page Équipement quand le métier affiché est un artisan. Taille S. Sonde : aucune. Décision : utiliser les relevés dans le panneau du carnet (recommandé : oui, toujours marqué, nourriture éventuelle comprise dans le relevé).
- **Source** : CraftLogObservation.cs:26 et 29 : calcul refusé si le métier porté n'est pas celui de la recette. MainWindow.CraftSimulation.cs:110-121 : « Copier » exige le métier porté. MainWindow.Gear.cs:286-288 calcule déjà les valeurs prévues (GearRules.Planned), Habileté, Contrôle et PS compris pour un artisan (Gear/GearCatalog.cs:148-149).

## C4 — K5 — Seuils d'attributs d'une séquence : Habileté, Contrôle et PS minimum

- **État (relu le 2026-11-05)** : FAIT 0.553.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.CraftSimulation.cs`.
- **Ce que le joueur obtient** : Sous une simulation terminée : « Cette séquence tient avec au moins 3 950 Habileté, 3 820 Contrôle et 590 PS (marge : +120, +45, +12) ». Vous savez tout de suite si un plat, une matéria ou une pièce moins chère suffit, ou ce qui manque pour atteindre la cible.
- **Comment, taille, dépendances** : Fonction pure CraftStatThresholds : dichotomie sur l'Habileté (la séquence termine), sur le Contrôle (qualité ≥ cible), PS = somme des coûts ; bornes de la recette (RequiredCraftsmanship, RequiredControl). Calcul sur une tâche annulable au changement de résultat, jamais dans Draw. Taille S. Dépendances : CraftSimulation.Run. Sonde : aucune (aucune donnée nouvelle). Décision : aucune.
- **Source** : Aucune fonction de ce genre dans Craft/ (CraftSimulation, CraftSolver, CraftConditionEnvelope). Le simulateur ne donne que l'état final (MainWindow.CraftSimulation.cs:180-188). ROADMAP.md:209 (CRAFT-001 PARTIAL). La progression ne dépend que de l'Habileté et la qualité que du Contrôle dans le modèle (CraftSimulation.cs:51-54), ce qui permet une recherche par dichotomie avec le même moteur.

## C5 — K6 — Suivre sa séquence pendant la synthèse : action suivante et écart avec le plan

- **État (relu le 2026-11-05)** : FAIT 0.589.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.CraftLive.cs`.
- **Ce que le joueur obtient** : Un petit panneau à côté de la fenêtre de synthèse du jeu suit la séquence choisie (celle du panneau du carnet ou du simulateur) : « Étape 7/22 : Innovation », progression et qualité attendues contre celles du jeu, et un avertissement quand une condition Bonne, Excellente ou Médiocre a fait diverger la synthèse. Utile pour qui fabrique à la main sans macro. Affichage seulement, rien n'est exécuté.
- **Comment, taille, dépendances** : Réutiliser CraftLiveReader (observation opt-in déjà gardée) : comparer native.Step et les ressources à l'étape correspondante de la simulation, afficher l'action suivante et l'écart. Panneau accroché à la fenêtre Synthesis comme celui du carnet, réglage avec infobulle et ↺. Taille M. Dépendances : observation « Craft » ; A-04 remplacera plus tard l'action suivante par un nouveau calcul. Sonde : aucune ; relevé en jeu ensuite. Décision : quelle séquence suivre (recommandé : celle du panneau du carnet pour cette recette, sinon celle du simulateur).
- **Source** : MainWindow.CraftLive.cs:8-11 : la synthèse en direct est une page de la fenêtre principale, sans panneau près de la fenêtre du jeu ; CraftLiveProjection.cs (résumé l.15) ne projette qu'un coup. docs/evidence/0.318-plugin-gaps.json : Craft Remaining (progression et qualité restantes dans la fenêtre de synthèse) = partial ; Craftimizer (prochains coups pendant la synthèse) = partial. GAPS.md:75 (A-04) reste la version par solveur.

## C6 — K7 — Recettes à niveau ajusté, à qualité imposée et Exploration cosmique

- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Craft/CraftSimulation.cs`.
- **Ce que le joueur obtient** : Le simulateur, le panneau du carnet et la synthèse en direct acceptent les recettes aujourd'hui refusées comme « ajustées » ou « à qualité imposée », dont celles de l'Exploration cosmique, contenu actuel des artisans, avec leurs seuils de mission.
- **Comment, taille, dépendances** : Sonde d'abord (tools/ApiProbe, puis relevé en jeu dans docs/evidence) : effet de MaxAdjustableJobLevel sur le niveau et les attributs, sens de RequiredQuality (échec ou objet NQ), feuilles WKS* (recettes, seuils). Ensuite : accepter ces recettes dans Unsupported avec les règles prouvées, seuils comme cibles. Taille M-L. Dépendances : K1 (cible), A-07 si une recette est aussi experte. Décision : périmètre (recommandé : recettes ajustées et à qualité imposée d'abord, Exploration cosmique ensuite).
- **Source** : CraftSimulation.cs:32-33 refuse LevelScaled (RecipeSimulationCopy.Lumina.cs:15 : MaxAdjustableJobLevel) et RequiredQuality>0, en plus des recettes expertes et spécialisées. docs/evidence/0.318-plugin-gaps.json (Craftimizer) : « Recettes à niveau ajusté et Exploration cosmique (carnet WKS, niveau synchronisé, seuils de mission) » = missing, aucune lecture de WKSRecipeNotebook. Absent des A-* de GAPS.md (A-07 ne couvre que les expertes). ROADMAP.md:209 : recettes spéciales à compléter.

## C7 — G1 — R-13, partie anticipée : points à horaire ouverts maintenant ou bientôt

- **État (relu le 2026-11-05)** : FAIT 0.588.
- **État (relu le 2026-11-03)** : en partie 0.415 (R-13 : fenêtre de suivi des objets des alarmes) ; liste « ouverts maintenant ou bientôt » non faite.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.GatheringBrowser.cs`.
- **Ce que le joueur obtient** : Sur la page Récolte, un onglet « Maintenant » liste tous les points à horaire ET (mineur, botaniste) ouverts, puis ceux qui ouvrent dans l'heure réelle, triés par prochaine fenêtre, avec compte à rebours, zone, niveau et bouton d'alarme. Filtres : métier, niveau, objets cachés. Vous savez où aller sans chercher objet par objet.
- **Comment, taille, dépendances** : Index des sources à horaire Limited, prochaine fenêtre calculée une fois par minute ET (cache, jamais à chaque image), ItemCell et alarme existants (DrawGatheringAlarmButton). Poissons exclus tant que R-02 (météo) n'est pas fait. Taille S-M. Dépendances : aucune ; R-13 reprendra la liste pour sa fenêtre épinglée. Sonde : aucune. Décision : horizon « bientôt » (recommandé : une heure réelle, réglage avec infobulle et ↺).
- **Source** : GAPS.md:104 (R-13, fenêtre épinglée des objets suivis, moyen). MainWindow.GatheringBrowser.cs:65-70 : la liste ne filtre que par nom, zone et type, sans horaire ni niveau. Les horaires sont déjà copiés (GatheringSchedule, GatheringTime.Next). docs/evidence/0.318-plugin-gaps.json (GatherBuddy) : fenêtre épinglée triée par prochaine fenêtre = partial.

## C8 — G2 — Plan PA du panneau de récolte : quantité observée et effets déjà actifs

- **État (relu le 2026-11-05)** : FAIT 0.515–0.517 (plan par nœud et effets du lieu ; tri du 2026-11-05, [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md)).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.GatheringPanel.cs`.
- **Ce que le joueur obtient** : Le plan PA du panneau en jeu tient compte de ce qui est déjà acheté sur le point (Rendement accru, Don de la terre, Bonne augure…) et de la quantité affichée : il ne propose plus une action déjà active et ne compte plus son effet deux fois. Le gain attendu affiché devient fiable.
- **Comment, taille, dépendances** : Sonde d'abord : identifiants des statuts posés par chaque action (Lumina Status, avec tools/GatheringActionsProbe), puis lecture des statuts du joueur dans GatheringObservationReader (framework, gardée R-14). Remplir GpActiveEffects et le rendement de base (quantité affichée moins l'effet actif) ; « effets actifs inconnus » si la lecture échoue. Taille S-M. Dépendances : R-09 (Raisonnement solide, Eurêka) complétera le modèle. Décision : aucune.
- **Source** : MainWindow.GatheringPanel.cs:80 : GpOptimizer.Solve avec un rendement de base de 1 et aucun effet actif (GpActiveEffects.None), alors que les taux affichés incluent les effets achetés (GpOptimizer.cs:43, commentaire). La quantité affichée est lue (GatheringObservation.cs:12, DisplayedQuantity) mais seulement montrée dans le tableau d'observation (MainWindow.GatheringObservation.cs:55). ROADMAP.md:214 (GP-001 : décodeur natif à compléter).

## C9 — G3 — R-02, partie anticipée : « possible maintenant ? » dans l'aide à la pêche

- **État (relu le 2026-11-05)** : FAIT 0.568.
- **État (relu le 2026-11-03)** : en partie 0.396 (R-02 : prochaines fenêtres sur la fiche du poisson) ; pas dans l’aide à la pêche.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.FishingPanel.cs`.
- **Ce que le joueur obtient** : Chaque poisson de l'aide à la pêche et de « Comment le pêcher » montre trois pastilles : créneau ET ouvert, météo actuelle conforme, météo précédente conforme (✓ / ✗ / ?). Vous voyez tout de suite quels poissons sont possibles là, maintenant, sans croiser vous-même avec la page Météo.
- **Comment, taille, dépendances** : Météo actuelle et précédente du territoire calculées une fois par période de 1 400 s (cache, comme MainWindow.Weather.cs), comparées aux ids GatherBuddy ; créneau par GatheringTime. Taille S. Dépendances : aucune ; la liste des prochaines fenêtres réelles reste pour R-02. Sonde : aucune. Décision : aucune.
- **Source** : GAPS.md:93 (R-02, petit, valeur haute) : la fiche montre la plage ET et les météos séparément. Code : MainWindow.FishingPanel.cs (FishRow) et MainWindow.GatheringFish.cs:57-58 affichent les météos en texte, sans comparaison ; WeatherCatalog.Forecast existe (Gathering/WeatherForecast.cs).

## C10 — G4 — Chrono de touche relancé tout seul à chaque lancer

- **État (2026-11-09)** : FAIT 0.1011, avant la 1.0.0 (plus rien n'est prévu après). Signal du lancer : état CastingOut de la ligne, prouvé en jeu en 0.887 (docs/evidence/0.887-fishing-bite.md), lu à chaque image dans `Gathering/FishingBaitReader.Dalamud.cs` ; les chronos de l'aide à la pêche et de la pêche en mer repartent de ce moment ; le bouton manuel reste.

- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.FishingPanel.cs`.
- **Ce que le joueur obtient** : Le chrono de l'aide à la pêche, comparé aux temps de touche Teamcraft, repart de zéro à chaque lancer, sans clic sur « Relancer ». La plage verte « ça peut mordre » devient juste pour chaque lancer.
- **Comment, taille, dépendances** : Sonde d'abord : un signal fiable du lancer (drapeaux de condition pendant le lancer et l'attente, ou état du gestionnaire de pêche FFXIVClientStructs), relevé en jeu dans docs/evidence. Ensuite : copie sur le thread du framework, gardée R-14 ; le bouton manuel reste. Taille S après la sonde. Dépendances : G15 (lectures hors Draw). Décision : aucune.
- **Source** : MainWindow.FishingPanel.cs:47 : le chrono ne démarre qu'au passage en état « pêche » ; l.55 : relance à la main. Plugin.cs:199 ne lit que ConditionFlag.Fishing. TESTING.md:117 (0.232, à valider).

## C11 — K8 — A-13, partie anticipée : simuler les recettes de spécialiste sans les actions de spécialiste

- **État (relu le 2026-11-03)** : FAIT 0.434-0.435 (A-13 : réglage « Spécialiste », recettes de spécialiste acceptées).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Craft/CraftSimulation.cs`.
- **Ce que le joueur obtient** : Les recettes réservées aux spécialistes (non expertes) ne sont plus refusées : simulateur, recherche et panneau du carnet les calculent avec les actions ordinaires, avec la mention « actions de spécialiste non modélisées ». La macro reste valable, juste moins optimisée.
- **Comment, taille, dépendances** : Accepter Specialist quand Expert, LevelScaled et RequiredQuality sont faux ; texte d'avertissement traduit dans les trois vues. Taille S. Dépendances : A-13 ajoutera ensuite les actions. Sonde : aucune ; une synthèse de spécialiste comparée en jeu. Décision : aucune.
- **Source** : CraftSimulation.cs:32 refuse Specialist au même titre que Expert. GAPS.md:84 (A-13) : « Les recettes spécialisées sont refusées » ; les actions de spécialiste ne font qu'ajouter des choix. RecipeSimulationCopy.Lumina.cs:15 copie IsSpecializationRequired sans autre paramètre propre.

## C12 — K9 — Qualité initiale calculée à partir des ingrédients HQ choisis

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.CraftSimulation.cs`.
- **Ce que le joueur obtient** : Dans le simulateur, vous indiquez combien d'ingrédients HQ vous mettez ; la qualité initiale est calculée au lieu d'être tapée. Vous comparez « 3 HQ de plus » contre « une étape de plus » sans calcul à la main.
- **Comment, taille, dépendances** : Source à citer d'abord : règle de calcul (Craftimizer, MIT) relevée dans docs/evidence et vérifiée en jeu contre la qualité initiale lue par le panneau du carnet (comparaison affichée en mode développeur). Puis une ligne par ingrédient pouvant être HQ (0 à quantité), plafond MaxStartingQuality. Taille S. Dépendances : aucune. Décision : aucune.
- **Source** : MainWindow.CraftSimulation.cs:130 : qualité initiale tapée, bornée par MaxStartingQuality. docs/evidence/0.318-plugin-gaps.json (Craftimizer) : « Qualité initiale calculée selon le nombre d'ingrédients HQ choisis » = partial. Le panneau du carnet lit déjà la vraie valeur du jeu (CraftLogObservation.StartingQuality), ce qui permet la vérification.

## C13 — K10 — Chance HQ d'une qualité finale

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Craft/CraftSimulation.cs`.
- **Ce que le joueur obtient** : Pour une recette HQ possible qui termine sous le maximum : « Qualité 7 200 / 9 000, chance HQ ≈ 55 % ». Utile pour la montée en niveau et les livraisons sans HQ obligatoire, et pour arrêter une recherche plus tôt.
- **Comment, taille, dépendances** : Source d'abord : relever la chance HQ affichée par la fenêtre de synthèse (index d'AtkValue à sonder) à plusieurs pourcentages de qualité, et la comparer à la table de Craftimizer (MIT) ; consigner dans docs/evidence. Ensuite : fonction pure qualité % → HQ %, affichée dans le simulateur et le panneau du carnet. Taille S après la source. Dépendances : aucune. Décision : accepter la table de Craftimizer vérifiée par relevés (recommandé), ou seulement les relevés.
- **Source** : Aucune trace de chance HQ dans le code (recherche HqChance / HQ % vide). CraftLiveReader.Dalamud.cs:30-31 lit déjà des AtkValues de la fenêtre de synthèse, qui affiche la chance HQ du jeu. Règle « ne jamais inventer » (CLAUDE.md, Méthode).

## C14 — K11 — A-12, partie anticipée : fiche de recette complète (livre de maître, synthèse rapide, HQ, prérequis, écart chiffré)

- **État (relu le 2026-11-05)** : FAIT 0.600.
- **État (relu le 2026-11-03)** : en partie 0.427 (A-12 : niveau, Habileté, Contrôle face au personnage, « Équiper ») ; livre de maître, synthèse rapide et HQ non faits.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Craft/RecipeCatalogLoader.Dalamud.cs`.
- **Ce que le joueur obtient** : La recette (onglet Craft de la fiche objet et panneau du carnet) dit : livre de recettes de maître requis (et s'il est lu), synthèse rapide possible, HQ possible, spécialiste requis, objet ou statut requis, et l'écart chiffré « Habileté 3 950 / 4 000 : −50 ». Vous savez pourquoi une recette n'est pas faisable.
- **Comment, taille, dépendances** : Copier ces colonnes de Recipe dans RecipeDefinition (version de cache du catalogue à monter). Afficher dans DrawCraftBook et le panneau. État « lu » du livre après sonde (état client FFXIVClientStructs, à sonder), sinon « non vérifié ». Taille S. Dépendances : le changement d'ensemble sur clic reste dans A-12 (étape g). Décision : aucune.
- **Source** : GAPS.md:83 (A-12, petit) : CraftLogContext ne signale que le mauvais métier et le minimum. Code : CraftLogObservation.cs:33-34 (message sans écart) ; RecipeCatalogLoader.Dalamud.cs:131 ne copie ni SecretRecipeBook, ni CanQuickSynth, ni ItemRequired, ni StatusRequired (recherche vide dans le code). Le livre de maître n'est cité nulle part dans GAPS (seulement comme type de collection, C-04).

## C15 — K12 — Simulateur plus direct : choix de la recette dans la page, résultat recalculé tout seul, glisser-déposer

- **État (relu le 2026-11-05)** : FAIT 0.572.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.CraftSimulation.cs`.
- **Ce que le joueur obtient** : Le simulateur a sa propre recherche de recette (nom, métier, niveau) ; le résultat se met à jour à chaque action ajoutée, déplacée ou retirée, sans clic sur « Simuler » ; les actions se déplacent par glisser-déposer ; deux barres montrent progression et qualité.
- **Comment, taille, dépendances** : Recherche réutilisant le catalogue d'objets et RecipeCatalog.ForResult (index calculé une fois). Simulation relancée une fois par changement (clé de la séquence et des attributs), au plus 100 actions, jamais à chaque image. Glisser-déposer ImGui. Réglage « Simuler automatiquement » (activé, infobulle, ↺). Taille S. Sonde : aucune. Décision : recalcul automatique par défaut (recommandé : oui).
- **Source** : MainWindow.CraftSimulation.cs:97 : sans recette, la page dit de passer par la fiche objet ; l.169-171 : simulation au clic seulement ; l.205-206 : déplacement par boutons ↑/↓. docs/evidence/0.318-plugin-gaps.json (Craftimizer) : éditeur de macros = partial.

## C16 — K13 — Bibliothèque des séquences : recherche, renommer, dupliquer, copier et coller en texte (A-08, partie texte)

- **État (relu le 2026-11-05)** : FAIT 0.590.
- **État (relu le 2026-11-03)** : en partie 0.425 (A-08 : import d’une macro collée) ; recherche, renommer, dupliquer non faits.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.CraftPresets.cs`.
- **Ce que le joueur obtient** : La page des séquences enregistrées se cherche par nom, objet ou métier et se trie ; une séquence se renomme, se duplique, se copie en texte de macro et, dans l'autre sens, une macro /ac collée devient une séquence vérifiable dans le simulateur.
- **Comment, taille, dépendances** : Filtres et tri mis en cache ; Rename et Duplicate purs dans CraftPresets (même Commit) ; copie par CraftMacroExporter ; analyse des lignes /ac contre les noms d'actions du catalogue dans la langue du client, lignes inconnues signalées. Taille S-M. Dépendances : les liens en ligne de A-08 restent pour l'étape g. Sonde : aucune. Décision : aucune nouvelle.
- **Source** : MainWindow.CraftPresets.cs:40-63 : liste brute (Charger, Supprimer), sans recherche ni renommage ; CraftPresets.cs:137-155 : Create, Add, Remove seulement. GAPS.md:79 (A-08, Choix : texte collé et liens Teamcraft / Craftingway). docs/evidence/0.318-plugin-gaps.json (Craftimizer) : bibliothèque (renommer, copier, enregistrer sous) = partial.

## C17 — K14 — Options d'export des macros : attentes séparées (X-12), son, écho final, icône

- **État (relu le 2026-11-05)** : FAIT 0.573.
- **État (relu le 2026-11-03)** : en partie 0.419 (X-12 : attente à part pour les buffs) ; son, écho final, icône non faits.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Craft/CraftMacroExport.cs`.
- **Ce que le joueur obtient** : Vous réglez l'attente des actions et celle des effets séparément, choisissez le son de fin (ou aucun), l'écho sur chaque page ou seulement sur la dernière, et l'icône des macros écrites dans le jeu au lieu du « ? » imposé.
- **Comment, taille, dépendances** : Champs dans CraftSearchSettings (configuration), chacun avec infobulle et ↺, mêmes bornes que l'attente actuelle ; ils entrent dans la clé du cache du carnet. Icône : liste des icônes de macro du jeu (feuille à sonder) ou celle de l'objet fabriqué si valide. Taille S. Sonde : feuille des icônes de macro. Décision : valeurs par défaut (recommandé : inchangées).
- **Source** : GAPS.md:204 (X-12) : attentes fixes 2 s / 3 s et un seul supplément (CraftMacroExport.cs:13-17, 34). CraftMacroExport.cs:42 : écho avec <se.1> fixe sur chaque page. Macros/MacroWriter.Dalamud.cs:16 : icône 66001 imposée. docs/evidence/0.318-plugin-gaps.json (Craftimizer, Macro Mate) : options d'export et choix de l'icône = partial / missing.

## C18 — K15 — Chaîne de fabrication : boutons par étape, étapes identiques regroupées, cases « fait »

- **État (relu le 2026-11-05)** : FAIT 0.591.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.CraftChain.cs`.
- **Ce que le joueur obtient** : Chaque étape de la chaîne a ses boutons « Carnet » (ouvre le carnet du jeu sur la recette) et « Simuler » ; les étapes répétées d'une même recette sont montrées regroupées (×N au total) ; vous cochez les étapes faites au fil de la session.
- **Comment, taille, dépendances** : Regroupement à l'affichage seulement (somme par RecipeId, ordre de la première apparition), le calcul ne change pas. Boutons réutilisant OpenCraftLog (sur clic). Cases gardées pour la session (ou dans la configuration, à choisir). Taille S-M. Dépendances : A-05 (listes multi-objets) reprendra la vue. Sonde : aucune. Décision : cases gardées entre les sessions ou non (recommandé : session seulement).
- **Source** : MainWindow.CraftChain.cs:53-62 : une étape = icône et texte, sans action. Craft/CraftChain.cs:93 : « Do not merge repeated steps » (ordre gardé pour les dépendances). OpenCraftLog et OpenCraftSimulation existent déjà (MainWindow.CraftLog.cs:26, MainWindow.CraftSimulation.cs:74).

## C19 — K16 — Où sont mes matériaux : détail par rangement dans la préparation et la chaîne

- **État (relu le 2026-11-05)** : FAIT 0.559.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.CraftPlan.cs`.
- **Ce que le joueur obtient** : Au survol de « Relevé » d'un ingrédient : « Sacs 12 · Servant Lune 40 (relevé du 01/11) · Sacoche 8 ». Vous savez quel servant appeler avant de fabriquer, sans ouvrir la fiche de chaque objet.
- **Comment, taille, dépendances** : Infobulle commune (même rendu que l'onglet Stocks de la fiche) sur les cellules de la préparation et des matériaux de la chaîne, à partir des Rows déjà calculées ; rien de recalculé à chaque image. Taille S. Sonde : aucune. Décision : aucune.
- **Source** : MainWindow.CraftPlan.cs:61 : seul le total est affiché ; la chaîne ne montre que le manquant (MainWindow.CraftChain.cs:66-86). ItemHoldingsSummary garde déjà le détail (Inventory/ItemHoldings.cs:11-17, StorageHolding : catégorie, nom, couverture).

## C20 — K17 — A-09, partie anticipée : ouvrir la préparation ou la chaîne dans Teamcraft

- **État (relu le 2026-11-03)** : FAIT 0.423 (A-09 : lien Teamcraft depuis la préparation).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.CraftChain.cs`.
- **Ce que le joueur obtient** : Un bouton « Teamcraft » copie (ou ouvre sur clic) le lien d'import de l'objet préparé et de sa quantité ; la liste apparaît dans Teamcraft avec ses sous-crafts. Tensho n'envoie rien lui-même.
- **Comment, taille, dépendances** : Fonction pure de construction du lien (testée), bouton dans DrawCraftPlan et DrawCraftChain ; ouverture du navigateur seulement sur clic. Taille S. Dépendances : A-05 étendra à plusieurs objets. Sonde : un lien essayé à la main. Décision : copier ou ouvrir (recommandé : les deux boutons).
- **Source** : GAPS.md:80 (A-09, petit, Choix « Ajouter ») : format public https://ffxivteamcraft.com/import/ + base64 de « itemId,recipeId,quantité » séparés par « ; », sans requête réseau. Aucune trace dans le code (recherche ffxivteamcraft.com/import vide d'après docs/evidence/0.318-plugin-gaps.json).

## C21 — K18 — « Que puis-je fabriquer ? » avec mes stocks

- **État (relu le 2026-11-05)** : FAIT 0.592.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Items.cs`.
- **Ce que le joueur obtient** : Une liste des recettes faisables tout de suite avec vos sacs et la Banque enregistrée, filtrée par métier et niveau, avec le nombre de fabrications possibles. Pratique pour écouler des matériaux ou monter un métier.
- **Comment, taille, dépendances** : Calcul sur un worker annulable au clic (toutes les recettes, ingrédients directs), résultat gardé tant que l'inventaire et la Banque ne changent pas ; borne basse (≥) quand un stock est inconnu, comme aujourd'hui. Taille M. Dépendances : aucune. Sonde : aucune. Décision : ingrédients directs seulement ou sous-crafts compris (recommandé : directs d'abord).
- **Source** : MainWindow.Items.cs:637 (GetCraftableCount) ne sert qu'à la recette affichée (l.389). docs/evidence/0.318-plugin-gaps.json : WachuMakeyMaking? (recettes réalisables avec ses ressources) = partial ; LazyCrafter (catalogue des recettes faisables) = partial.

## C22 — K19 — Projets de l'atelier de compagnie libre : matériaux par pièce et par phase

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.FreeCompany.cs`.
- **Ce que le joueur obtient** : Pour une pièce de sous-marin ou de dirigeable, ou un autre projet d'atelier, la page Compagnie libre liste les matériaux de chaque phase contre vos stocks, avec un lien vers la chaîne de fabrication de chacun.
- **Comment, taille, dépendances** : Sonde d'abord : colonnes de CompanyCraftSequence, CompanyCraftPart, CompanyCraftProcess, CompanyCraftSupplyItem (tools/ApiProbe), consignées dans docs/evidence. Puis copie sur le framework, onglet « Atelier » de la page Compagnie libre, stocks par ItemHoldings, liens vers la chaîne. Taille M. Dépendances : R-07. Décision : emplacement (recommandé : onglet de la page Compagnie libre).
- **Source** : docs/evidence/0.318-plugin-gaps.json (Artisan) : « Projets d'atelier de compagnie libre : matières par projet, partie et phase » = missing, aucune trace de CompanyCraft* ; absent des A-* de GAPS.md. MainWindow.VesselBuilds.cs ne couvre que les statistiques des pièces (R-07).

## C23 — K20 — Action suivante mise en évidence sur les barres de raccourcis

- **État (relu le 2026-11-05)** : ÉCARTÉE à la revue avec l'utilisateur ([plan global](GLOBAL-PLAN.md)).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Craft/CraftLiveReader.Dalamud.cs`.
- **Ce que le joueur obtient** : Pendant une synthèse suivie (K6, puis A-04), l'action conseillée clignote sur vos barres comme une action combo du jeu. Vous fabriquez à la main sans quitter les barres des yeux. Rien n'est exécuté.
- **Comment, taille, dépendances** : Nouveau hook en lecture/retour seul (ActionManager.IsActionHighlighted), créé seulement quand le réglage est activé (désactivé par défaut, infobulle, ↺), gardé R-14 (désactivé proprement si la signature change), libéré au Dispose. Taille M. Dépendances : K6 ou A-04 pour l'action conseillée. Sonde : signature dans FFXIVClientStructs. Décision : accepter un nouveau hook (recommandé : oui, désactivé par défaut).
- **Source** : docs/evidence/0.318-plugin-gaps.json (Craftimizer) : surbrillance de l'action suggérée sur les barres (hook IsActionHighlighted) = missing. GAPS.md:75 (A-04, affichage seulement). Craftimizer, qui le fait, est dans D17 (GAPS.md:74, note).

## C24 — G8 — Besoin restant de la préparation dans les aides de récolte et de pêche

- **État (relu le 2026-11-05)** : FAIT 0.560.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.GatheringPanel.cs`.
- **Ce que le joueur obtient** : Au point de récolte ou de pêche, l'objet utile à votre dernière chaîne d'artisanat est marqué : « Encore 14 pour Lingot de… ». Le nombre baisse à mesure que vous récoltez.
- **Comment, taille, dépendances** : Restant = manque au calcul − (stock actuel − stock au calcul), borné à 0 ; lu depuis l'état de la session de chaîne, sans recalcul. Même mention dans l'aide à la pêche. Taille S. Dépendances : aucune ; A-05 (listes) l'étendra. Sonde : aucune. Décision : aucune.
- **Source** : MainWindow.GatheringPanel.cs:44-60 : la ligne d'un objet montre réussite, supplément, Abondance et stock, sans lien avec la préparation. La chaîne garde les manques et les stocks du moment du calcul (Craft/CraftChain.cs:9-14, Materials et Observations).

## C25 — G9 — R-14 (récolte), partie anticipée : type de source des matières brutes de la chaîne

- **État (relu le 2026-11-03)** : FAIT 0.416 (R-14 : matières brutes de la chaîne en liste de récolte par zone, celles sans lieu à part).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.CraftChain.cs`.
- **Ce que le joueur obtient** : Chaque matière brute manquante porte ses sources en icônes (mineur, botaniste, pêche avec horaire ou non, marchand PNJ, monstre, marché) et un regroupement par zone en tête. Vous voyez d'un coup ce qui se récolte, s'achète ou se chasse.
- **Comment, taille, dépendances** : Étiquettes calculées une fois par résultat de chaîne (cache par identité du résultat), regroupement par zone des sources de récolte ; les horaires détaillés et l'itinéraire restent pour R-14. Taille S. Dépendances : I3 (prix PNJ) pour le prix en face du marchand. Sonde : aucune. Décision : aucune.
- **Source** : GAPS.md:105 (R-14 : liste de récolte par zone depuis une chaîne, moyen). MainWindow.CraftChain.cs:79-81 : un bouton « Lieux » ouvre les matériaux un par un. Les données existent : GatheringCatalog.ForItem, catalogue des marchands PNJ, LuminaSupplemental.

## C26 — G5 — R-04, partie anticipée : stock des appâts dans l'aide à la pêche

- **État (relu le 2026-11-03)** : FAIT 0.398 (R-04 : appât en main et son stock).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.FishingPanel.cs`.
- **Ce que le joueur obtient** : Sous chaque appât de la route : « ×37 », en rouge à zéro. Vous ne partez plus pêcher un poisson sans son appât. La détection de l'appât porté viendra avec R-04.
- **Comment, taille, dépendances** : Même GetRecipeHoldings que pour les poissons (cache existant), sur l'aide à la pêche et « Comment le pêcher ». Taille S. Dépendances : R-04 pour l'appât porté (sonde PlayerState.FishingBait). Sonde : aucune pour cette partie. Décision : aucune.
- **Source** : GAPS.md:95 (R-04, petit, valeur haute : appât en main et « combien il en reste »). MainWindow.FishingPanel.cs:85 : l'infobulle de l'appât donne le nom et le clic droit, sans stock ; les poissons ont déjà leur stock (l.76-77, GetRecipeHoldings).

## C27 — G6 — Classe du point (normal, éphémère, légendaire…) et livre de folklore requis

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Gathering/GatheringCatalogLoader.Dalamud.cs`.
- **Ce que le joueur obtient** : Chaque point dit sa classe et, pour un point légendaire, le livre de folklore nécessaire (et s'il est lu). Vous ne faites plus le trajet vers un point que vous ne pouvez pas ouvrir.
- **Comment, taille, dépendances** : Sonde d'abord : valeurs de GatheringPoint.Type et lien GatheringPoint.GatheringSubCategory → FolkloreBook (tools/GatheringProbe), état « livre lu » (état client à sonder). Copier dans GatheringSource (version du cache à monter), afficher dans la fiche et la liste. Taille S. Dépendances : aucune. Décision : aucune.
- **Source** : ROADMAP.md:212 (GATHER-001 : « intuition/prérequis à compléter »). Le livre de folklore n'est lu que pour les poissons (Gathering/FishingNotebookCopy.Lumina.cs:11) ; GatheringCatalogLoader.Dalamud.cs:221 traite le type 8 à part sans le nommer ; GatheringSource n'a pas de classe de point (Gathering/GatheringCatalog.cs:5-7).

## C28 — G7 — Durée de l'intuition et compte à rebours

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Community/GatherBuddyFish.cs`.
- **Ce que le joueur obtient** : Pour un poisson à prédateurs : « Intuition : 350 s après 3 × X ». Quand l'intuition est active, l'aide à la pêche affiche le temps restant. Indispensable pour les gros poissons.
- **Comment, taille, dépendances** : Vérifier d'abord dans la source GatherBuddy (Apache-2.0) que ce premier nombre est la durée en secondes, et le consigner dans docs/evidence ; le lire alors. Compte à rebours : statut de l'intuition à sonder (Lumina Status), lu sur le framework. Version du cache GatherBuddy à monter. Taille S. Dépendances : aucune. Décision : aucune.
- **Source** : Community/GatherBuddyFish.cs:76-78 : seuls les couples (poisson, nombre) des prédateurs sont gardés, le premier nombre de l'appel est ignoré. docs/evidence/0.318-plugin-gaps.json (GatherBuddy) : « Durée de l'intuition (prédateurs) » = partial. ROADMAP.md:212 : « intuition à compléter ».

## C29 — G10 — R-15, partie anticipée : poissons au harpon dans la page Récolte

- **État (relu le 2026-11-03)** : en partie : carnet du harpon sur la page Récolte (0.402) et aide au harpon à côté de la fenêtre du jeu (0.417).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Gathering/GatheringCatalogLoader.Dalamud.cs`.
- **Ce que le joueur obtient** : Les poissons au harpon apparaissent dans la recherche de la page Récolte avec leur lieu, comme les autres ressources. L'aide sur la fenêtre du harpon viendra avec R-15.
- **Comment, taille, dépendances** : Sonde d'abord (tools/GatheringProbe) : nombre de références SpearfishingItem, lien avec SpearfishingNotebook et les lieux, consigné dans docs/evidence. Puis lignes de source « harpon » (métier pêcheur, AlarmSchedule.JobOf déjà prêt). Taille S-M. Dépendances : aucune. Décision : aucune.
- **Source** : GAPS.md:106 (R-15). GatheringCatalogLoader.Dalamud.cs:179 ignore toute référence de point qui n'est pas un GatheringItem ; aucune lecture de SpearfishingItem dans le code (recherche vide). docs/evidence/0.203-gathering-jobs.md : les types 4-5 (harpon) existent avec des noms de remplacement.

## C30 — G11 — Relevé local de vos prises et temps de touche personnels

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.FishingPanel.cs`.
- **Ce que le joueur obtient** : Tensho note chez vous chaque prise (poisson, lieu, appât, temps de touche, ferrage) et affiche vos propres plages à côté de celles de Teamcraft, par lieu. Rien n'est envoyé.
- **Comment, taille, dépendances** : Sonde d'abord : source de l'événement de prise (message du jeu dans le chat, ou lecture de l'état de pêche), consignée dans docs/evidence. Fichier à part comme la Banque, écriture hors Draw, borné. Taille M. Dépendances : G4 (lancer détecté). Décision : source de l'événement (recommandé : message du jeu, sans hook) et durée de conservation.
- **Source** : docs/evidence/0.318-plugin-gaps.json (GatherBuddy) : « Relevés locaux des prises (temps, appât, taille, ferrage, chum), statistiques par lieu » = missing ; absent des R-* de GAPS.md. Le chrono existe déjà (MainWindow.FishingPanel.cs:47-56).

## C31 — G12 — Météo de plusieurs zones en un tableau (zones favorites)

- **État (relu le 2026-11-05)** : FAIT 0.563.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Weather.cs`.
- **Ce que le joueur obtient** : La page Météo montre la météo actuelle et les prochaines de vos zones favorites, ou de toutes les zones d'une extension, sur une seule grille. Utile pour enchaîner poissons et points selon la météo.
- **Comment, taille, dépendances** : Liste de zones favorites dans la configuration (données de l'utilisateur), grille recalculée une fois par période de 1 400 s ; clic sur une zone = vue actuelle. Taille S. Dépendances : aucune. Sonde : aucune. Décision : nombre de périodes (recommandé : 6).
- **Source** : MainWindow.Weather.cs:15-35 : une seule zone à la fois. docs/evidence/0.318-plugin-gaps.json : GatherBuddy (prévisions de toutes les zones dans un tableau) = partial ; Kangas Tweaks (60 périodes, zones favorites) = partial.

## C32 — G13 — Commandes « /tensho recolte » et « /tensho recette » (suite de I16)

- **État (relu le 2026-11-05)** : FAIT 0.555, avec I16.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Plugin.cs`.
- **Ce que le joueur obtient** : « /tensho recolte minerai de fer » ouvre la page Récolte sur l'objet, « /tensho recette … » ouvre son onglet Craft ; utilisables aussi dans une macro du jeu, comme /gather de GatherBuddy.
- **Comment, taille, dépendances** : Même analyse d'arguments que I16, OpenGathering et l'onglet Craft ; mots français et anglais ; aide de la commande traduite. Taille S. Dépendances : I16. Sonde : aucune. Décision : mots-clés (recommandé : recolte/gather et recette/recipe).
- **Source** : Plugin.cs:376-380 : seul « peche » est reconnu. ITEMS-PROPOSALS.md:122-127 (I16 : « /tensho objet » et « /tensho prix »). docs/evidence/0.318-plugin-gaps.json (GatherBuddy) : recherche par nom avec /gather = partial.

## C33 — G14 — Une seule séance de relevés en jeu pour l'artisanat et la récolte, avant de coder

- **Valeur** : moyenne ; fichier principal : `docs/project/TESTING.md`.
- **Ce que le joueur obtient** : Une séance guidée relève en une fois, en mode développeur, les valeurs du jeu dont dépendent plusieurs propositions et manques retenus. Les versions suivantes avancent ensuite sans attendre un relevé chacune.
- **Comment, taille, dépendances** : Écran de relevés en mode développeur, en lecture seule : copie sur le thread du framework, gardée R-14, bouton « Copier » ; résultats consignés dans docs/evidence. Les validations en jeu restent pour l'étape h (règle de l'utilisateur). Taille S-M. Dépendances : aucune. Décision : moment de la séance (recommandé : juste avant l'étape g, Récolte).
- **Source** : Relevés demandés : K2 (déblocage de Manipulation), K7 (niveau ajusté, WKS), K10 (chance HQ affichée), K11 (livre de maître lu), G2 (statuts des actions PA), G4 (lancer), G6 (type de point, folklore), G7 (intuition), G10 (harpon) ; GAPS.md:72 (A-01, CollectablesShopRefine), GAPS.md:95-96 (R-04 PlayerState.FishingBait, R-05 IsFishCaught). ROADMAP.md:85 : bonus d'Abondance estimé à comparer en jeu.

## C34 — K21 — Missions d'approvisionnement de la Grande Compagnie du jour

- **État (relu le 2026-11-05)** : FAIT 0.469 (`Items/GcSupplyToday.cs` ; tri du 2026-11-05, [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md)).
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.Reminders.cs`.
- **Ce que le joueur obtient** : La liste des objets demandés aujourd'hui par votre Grande Compagnie (artisans et récolteurs), avec vos stocks et un lien vers la recette ou la récolte. Pratique pour la montée en niveau.
- **Comment, taille, dépendances** : Sonde d'abord : où le client garde la liste du jour après ouverture de la fenêtre d'approvisionnement ou des minuteurs (FFXIVClientStructs, à sonder), aucune requête déclenchée par Tensho. Puis copie sur le framework, gardée R-14, affichée sur la page Rappels. Taille M. Dépendances : aucune. Décision : emplacement (recommandé : page Rappels).
- **Source** : Aucune trace dans le code ni dans GAPS.md (recherche « approvisionnement » vide ; C-14 ne couvre que les mandats, GAPS.md:145). Liste tirée du serveur : lisible seulement après ouverture de la fenêtre par le joueur (GAPS.md:45-47).

## C35 — G15 — Aide à la pêche : lectures du jeu et premières tables hors de Draw

- **État (relu le 2026-11-05)** : FAIT 0.604.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/Plugin.cs`.
- **Ce que le joueur obtient** : Rien de visible, sinon l'absence d'à-coup à la première ouverture de l'aide à la pêche ou du panneau de récolte, et une aide qui reste conforme aux règles du projet après une mise à jour.
- **Comment, taille, dépendances** : Copier job, territoire, position et état de pêche sur IFramework.Update (comme les autres lecteurs) ; construire les deux tables avec le catalogue de récolte (worker, cache), lues ensuite sans feuille Excel. Taille S. Sonde : aucune. Décision : aucune.
- **Source** : ARCHITECTURE.md:110-116 (écarts connus, non tranchés) : aide à la pêche (joueur, territoire, condition à chaque image ; table FishingSpot construite au premier appel), base d'Abondance II construite au premier appel. Plugin.cs:195-218 et 222-232. docs/ai/RULES.md:9 (rien de massif dans Draw).

## C36 — G16 — R-14, partie artisanat et récolte : une aide illisible le dit au lieu de disparaître

- **État (relu le 2026-11-05)** : FAIT 0.601.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/Craft/CraftLogReader.Dalamud.cs`.
- **Ce que le joueur obtient** : Après une mise à jour du jeu, le panneau du carnet, l'aide à la récolte ou la synthèse en direct affichent « indisponible après une mise à jour du jeu » à leur place habituelle, et Diagnostics dit quelle lecture a échoué. Aujourd'hui, le panneau disparaît sans rien dire.
- **Comment, taille, dépendances** : Statut par lecteur (carnet, synthèse, récolte, pêche) publié avec l'état, court message traduit à l'emplacement du panneau, ligne dans Diagnostics. Taille S. Dépendances : passage complet R-14 à l'étape g2. Sonde : aucune. Décision : aucune.
- **Source** : USER-REQUESTS.md:32 (R-14) et ROADMAP.md:65 (g2). CraftLogReader.Dalamud.cs:83 efface l'observation : le panneau ne s'affiche plus, l'erreur n'apparaît que dans la préparation (MainWindow.CraftPlan.cs:188). MainWindow.GatheringPanel.cs:21-22 se masque sur une observation en erreur. Même démarche que MAP-PROPOSALS.md:115 (P15, partie carte).
