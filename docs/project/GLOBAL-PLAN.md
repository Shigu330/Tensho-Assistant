# Global plan

> *Traduction française plus bas.*

Plan dated 2026-11-05, at version 0.539.0, drawn from the review done with the user and reorganised at their request; origin of each point: [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md) (frozen snapshot of that day). **In progress since 2026-11-05 (green light from the user).** The order of the phases and points is a proposal from the agent.

**Since [DEC-039](DECISIONS.md): no submission to the official repository, custom repository only, Dalamud rules kept.** **1.0.0 comes after the whole plan** (user's decision, [DEC-037](DECISIONS.md)): they want a complete plugin, and accept that a submission judged too big may be refused ([PUBLISHING](PUBLISHING.md), point 6).

**Everything that requires playing comes last, one point at a time** (user's request, 2026-11-05). Phases 1 to 4 are done without them: `Verify.ps1` green, commit and push, no in-game test. A full build reloads the DLL if the game is running: they have nothing to do for that.

<!-- gen:plan:phases-en -->
| Phase | Content | Points | Need to play? | State |
|---|---|---:|---|---|
| 1 | Fixes without the game | 3 | No | Done |
| 2 | Small proposals | 43 | No | Done |
| 3 | Medium proposals | 19 | No | Done |
| 4 | Ideas, web probe and sources without the game | 6 | No | Done |
| 5 | In-game readings, then the code that depends on them | 29 | Yes, one after the other | In progress: the in-game readings are left |
| 6 | In-game tests of everything above | 1 | Yes, one after the other | To do: every test in one campaign, as late as possible |
| 7 | Just before 1.0.0: tool lists, documents, help |  | No | Mostly done before phase 6, at the user's request; last review just before 1.0.0 |
| Then | Version 1.0.0, on the custom repository (no submission, DEC-039) |  |  | When the user decides |

Total before 1.0.0 (phases 1 to 6): 101 points, a count produced by a script from the plan's source.
<!-- /gen:plan:phases-en -->

**Nothing is planned after 1.0.0 (user's request, 2026-11-09)**: C10, the only point that was there, moved to phase 5. Porting to Dalamud API 16 is not a point of the plan: it is done as soon as Dalamud publishes it, whatever the Tensho version (docs/evidence/0.1009-dalamud-api16-watch.md). The button to show or hide icons on the map already exists (user's report): the request is withdrawn.

## In-game run (phases 5 and 6)

1. The agent gives **a single point**: what to do in the game, in a few lines.
2. The user does it when they want, then replies; the agent reads the log.
3. If there is code to write or an error, the agent fixes it, then the same point is done again.
4. Only then, the next point. A session may stop between two points.

**Order (user's request, 2026-11-06):** 1. what can be done freely; 2. ranked content, what depends on a timer, and particular instances: Triple Triad tournaments, Lord of Verminion, Doman Mahjong, the beastmaster labyrinth ("Dédale"), Deep Dungeon, garden harvest; 3. **PvP at the very end**: arena match against a player, Crystalline Conflict, Frontline.

## Phase 1: fixes without the game

<!-- gen:plan:phase-1-en -->
| Where | What is missing | Issue |
|---|---|---|
| Garden | Add Cornflowers to the seed list | Done in 0.540 |
| Items | Item blocks without a price | Done in 0.541 |
| Squadron | Alarm button for a mission in progress already present: bell on the row in Reminders, and automatic alarm since 0.455… | Done |
<!-- /gen:plan:phase-1-en -->

If one of them finally needs an in-game survey, it moves to phase 5.

## Phase 2: small proposals

<!-- gen:plan:phase-2-en -->
| No. | Proposal | Size | Detail |
|---|---|---|---|
| P1 | Zoom at opening, adjustable and kept between sessions |  | Done in 0.542 |
| P2 | Label size set separately from icon size |  | Done in 0.543 |
| P3 | Nearest aetheryte to the flag or to a point, with its cost |  | Done in 0.544 |
| P5 | Mouse wheel: zoom from the whole window and not adjustable |  | Done in 0.545 |
| P9 | FATE: experience bonus and colour by remaining time |  | Done in 0.548 |
| P15 | State of the game readings shown on the map (R-14, map part) |  | Done in 0.569 |
| P16 | Option: left click on the map to place the flag |  | Done in 0.546 |
| P20 | « Layers » button to show or hide everything in one click |  | Done in 0.547 |
| I0 | Count worn pieces in the stocks and in « owned » |  | Done in 0.549 |
| I5 | Preparation cost at the data centre price (option of A-06) |  | Done in 0.574 |
| I8 | X-07, early part: open the item on Universalis, Garland Tools, Teamcraft or the wiki |  | Done in 0.561 |
| I9 | Copy the item name and item link into chat (local) |  | Done in 0.561 |
| I15 | Share a sorting template as text (copy / paste) |  | Done in 0.566 |
| I16 | Commands « /tensho objet » and « /tensho prix » |  | Done in 0.555 |
| I18 | I-07, early part: copy « lowest price − 1 » from the Market page |  | Done in 0.567 |
| I19 | English item name in the item sheet and tooltip |  | Done in 0.562 |
| I20 | Sorting preview: moved slots flagged |  | Done in 0.565 |
| ~~I22~~ done in 0.669 without an in-game measurement | Calculations done every frame (deferred to e): preparation cost and market history |  | Done in 0.669 |
| C0 | A-01, early part: choose the target quality in the log panel already present |  | Done |
| C2 | Try one's saved sequences on all recipes |  | Done in 0.552 |
| C3 | Simulate another job without wearing it: last attributes read per job and planned attributes from the Gear page |  | Done in 0.554 |
| C4 | Attribute thresholds of a sequence: minimum Craftsmanship, Control and CP |  | Done in 0.553 |
| C9 | R-02, early part: « possible now? » in the fishing help |  | Done in 0.568 |
| ~~C12~~ done in 0.669 | Initial quality computed from the chosen HQ ingredients |  | Done in 0.669 |
| C14 | A-12, early part: full recipe sheet (master book, quick synthesis, HQ, prerequisites, figured gap) |  | Done in 0.600 |
| C15 | More direct simulator: recipe choice in the page, result recomputed on its own, drag and drop |  | Done in 0.572 |
| C17 | Macro export options: separate waits (X-12), sound, final echo, icon |  | Done in 0.573 |
| C19 | Where are my materials: detail per storage in the preparation and the chain |  | Done in 0.559 |
| C24 | Remaining preparation need in the gathering and fishing helps |  | Done in 0.560 |
| C31 | Weather of several zones in one table (favourite zones) |  | Done in 0.563 |
| C32 | Commands « /tensho recolte » and « /tensho recette » (follow-up of I16) |  | Done in 0.555 |
| C35 | Fishing help: game readings and first tables out of Draw |  | Done in 0.604 |
| C36 | R-14, crafting and gathering part: an unreadable help says so instead of disappearing |  | Done in 0.601 |
| T3 | Quests: Level and Zone columns, level sort and filter |  | Done in 0.550 |
| T4 | Monsters: flag on the monster's position, not at the map centre |  | Done in 0.551 |
| T16 | Retainer tasks: one row per retainer, with alarm |  | Done in 0.570 |
| T17 | Reminders ticked by themselves when the game says so |  | Done in 0.558 |
| T18 | Discord: time read in each member's time zone, and [zone], [item], [and] tags |  | Done in 0.564 |
| T19 | Alarms: edit the name and time, and « Remind me in 5 min » |  | Done in 0.557 |
| T20 | Alarms: also ring at the exact time |  | Done in 0.556 |
| T23 | Free Company: ships grouped by company, old reading erasable, value of the last loot |  | Done in 0.603 |
| T28 | Collections: « Recently obtained » and source menu for emotes |  | Done in 0.602 |
| T29 | Mini Cactpot: payout table proven and visible |  | Done in 0.571 |
<!-- /gen:plan:phase-2-en -->

C12 moves to phase 5: its calculation rule must be compared with the initial quality read in the game.

## Phase 3: medium proposals

<!-- gen:plan:phase-3-en -->
| No. | Proposal | Size | Detail |
|---|---|---|---|
| P10 | Search on the map (places, aetherytes, NPCs, quests, resources) |  | Done in 0.597 |
| P12 | Names written next to the icons, like the game map |  | Done in 0.596 |
| I1 | Flag and erase the reading of a dismissed retainer or an old storage |  | Done in 0.576 |
| I3 | NPC vendor prices in the preparation cost and on the Market page |  | Done in 0.575 |
| I11 | Sortable list view of the Bank |  | Done in 0.594 |
| I21 | Job and gear set filter in the sorting rules |  | Done in 0.595 |
| C5 | Follow one's sequence during the synthesis: next action and gap with the plan |  | Done in 0.589 |
| C7 | R-13, early part: timed points open now or soon |  | Done in 0.588 |
| C16 | Sequence library: search, rename, duplicate, copy and paste as text (A-08, text part) |  | Done in 0.590 |
| C18 | Crafting chain: buttons per step, identical steps grouped, « done » boxes |  | Done in 0.591 |
| C21 | « What can I craft? » with my stocks |  | Done in 0.592 |
| T0 | Real-time alarms: by hand, every day, every week or at every reset (X-10, early part) |  | Done in 0.577 |
| T10 | Home: « Today » block and tiles of one's choice |  | Done in 0.599 |
| T11 | « Available now » quests and quests of the current zone |  | Done in 0.582 |
| T12 | Path to a quest: remaining prerequisites, count and next one to do |  | Done in 0.583 |
| T14 | Gear: prepare the plan of a job other than the one worn |  | Done in 0.580 |
| T15 | Fashion Report: outfit of the chosen week, tried on in one click, with where to find each piece |  | Done in 0.581 |
| T24 | Discord: one channel per category (several webhooks) |  | Done in 0.598 |
| T25 | Gear: several named plans per job |  | Done in 0.593 |
<!-- /gen:plan:phase-3-en -->

## Phase 4: ideas, web probe and sources without the game

<!-- gen:plan:phase-4-en -->
| Point | Note |
|---|---|
| I4, Universalis prices in batches | Done in 0.608 |
| N-12, S Rank conditions (done in 0.670) |
| Weekly tomestone cap alert | Done in 0.606 |
| « Where is this item? » across all characters | Done in 0.609 |
| List of annotated items, filter by label | Done in 0.607 |
| Write a character's file only if it changes | Done in 0.605 |
<!-- /gen:plan:phase-4-en -->

## Before phase 5: switch-off in PvP and in ranked content

Tensho switches itself off and back on by itself, with no setting (DEC-038). ~~To code~~ done in 0.610: [how it works and known gaps](ARCHITECTURE.md), D1 to D5 in DEC-038. In-game test: first point of phase 5.

## Phase 5: in-game surveys, then code

A piece of data is not proven: the survey comes before the code. Order: by activity, to avoid constantly changing place in the game.

### Fixes

<!-- gen:plan:phase-5-corrections-en -->
| Where | What to survey | Issue |
|---|---|---|
| Garden | One upkeep and one harvest, to recognise their messages; then remove the capture log lines | #36 |
| Gathering | GP regeneration during a gathering read on 2026-11-06: no increase within the window, +8 every 3 s outside, nothing to code (evidence) | Done |
<!-- /gen:plan:phase-5-corrections-en -->

### Gathering and fishing

<!-- gen:plan:phase-5-recolte-et-peche-en -->
| No. | Proposal | Size | Detail |
|---|---|---|---|
| C33 | A single in-game reading session for crafting and gathering, before coding | S-M |  |
| ~~C27~~ done in 0.647 | Node class (normal, ephemeral, legendary…) and required folklore tome |  | Done in 0.647 |
| ~~C28~~ done in 0.663 | Intuition duration and countdown |  | Done in 0.663 |
| ~~C29~~ done in 0.649 | R-15, early part: spearfishing fish in the Gathering page |  | Done in 0.649 |
| C30 | Local record of your catches and personal bite times | M |  |
| ~~C10~~ done in 0.1011 | Bite timer restarted on its own at every cast (fishing help and Ocean Fishing); the reading is that of 0.887 (« cast » state of the line) |  | Done in 0.1011 |
<!-- /gen:plan:phase-5-recolte-et-peche-en -->

### Crafting

<!-- gen:plan:phase-5-artisanat-en -->
| No. | Proposal | Size | Detail |
|---|---|---|---|
| ~~C6~~ done in 0.666 for the simulation | Level-adjusted recipes, forced-quality recipes and Cosmic Exploration |  | Done in 0.666 |
| ~~C13~~ done in 0.668 | HQ chance of a final quality |  | Done in 0.668 |
| ~~C22~~ done in 0.664 | Free Company workshop projects: materials per part and per phase |  | Done in 0.664 |
<!-- /gen:plan:phase-5-artisanat-en -->

### Map

<!-- gen:plan:phase-5-carte-en -->
| No. | Proposal | Size | Detail |
|---|---|---|---|
| ~~P6~~ done in 0.644 | Character coordinates always visible and distances |  | Done in 0.644 |
| ~~P7~~ done in 0.645 | Unattuned aetherytes flagged and cost on hover |  | Done in 0.645 |
| P11 | Undiscovered parts hidden like on the game map |  | Done |
| ~~P17~~ done in 0.652 | Reading of the marker type of levequests and their zones |  | Done in 0.652 |
| ~~P22~~ done in 0.665 for variants 1 and 5 | Names of quest icon variants (71141 / 71151) |  | Done in 0.665 |
<!-- /gen:plan:phase-5-carte-en -->

### Items

<!-- gen:plan:phase-5-objets-en -->
| No. | Proposal | Size | Detail |
|---|---|---|---|
| I10 | Retainer crystals and gil in the retainer reading |  | Done |
<!-- /gen:plan:phase-5-objets-en -->

### Tools

<!-- gen:plan:phase-5-outils-en -->
| No. | Proposal | Size | Detail |
|---|---|---|---|
| T5 | An in-game reading session for these tools, before coding | S |  |
| ~~T13~~ done in 0.648 for the content the data names | Content and quests: « Unlocked by » and « Unlocks » |  | Done in 0.648 |
| ~~T21~~ done in 0.646 | Character: levels and experience of all jobs |  | Done in 0.646 |
| T26 | Airships: route of the last voyage reconstructed | S | 2026-11-07: the user's airship is under construction, to be read at its first return. 2026-11-08 18:50: first return read (« Reading airship »: loot per stop, 23 log lines with sector names, return time; `docs/evidence/0.919-airship-reading-2026-11-08.log`): route reader done in 0.919 (« Last voyage » on the ships tab, single-airship company); to check in game at the next return |
| ~~T27~~ done in 0.651 | Active Free Company actions: end time and alarm |  | Done in 0.651 |
<!-- /gen:plan:phase-5-outils-en -->

### Big projects and idea

<!-- gen:plan:phase-5-gros-chantiers-et-idee-en -->
| Point | First | Detail |
|---|---|---|
| Retainer gil in the characters view |  | Done |
| I-05, follow-up of retainer sales |  | Done |
| N-04, housing lottery | Probe: game structure to find |  |
| N-07, island resources | Check the unit of positions | Map done in 0.667, materials list in 0.673; your island stocks are not read |
| N-10, island workshop |  | Done |
| ~~Labyrinth panel~~ dropped (DEC-038), taken up again as a limited exception by DEC-047 (X-109); follow-up of the beastmaster | The page stays a list of information; the team panel in front of the Labyrinth board is X-109 (DEC-047) |  |
| Crescent Isle (Occult Crescent) | In-game readings at the Occult Crescent (developer mode, « Reading fields »), then the MKD sheets | In Tensho: the occult lore in Collections (`CollectionKind.OccultLore`, sheet `MKDLore`) and a label for the map, nothing else. Game data to study: sheets `MKDSupportJob`, `MKDGrowData`, `MKDGrowDataSJob`, `MKDBNpcData`, `MKDChain`, `MKDData`, `MKDTrait`, structures `OccultCrescentMKDData`, `AgentMKDInfo`, `AgentMKDSupportJob`. Occult Crescent Helper and Phantom Job Switcher are under AGPL: read only, never copied. Moved to phase 5 on 2026-11-09 (user request: before 1.0.0). |
<!-- /gen:plan:phase-5-gros-chantiers-et-idee-en -->

## Phase 6: in-game tests

<!-- gen:plan:phase-6-en -->
| No. | Proposal | Size | Detail |
|---|---|---|---|
| P19 | Map in-game tests still pending | S | 0.1010: campaign route, the 782 open trials sorted by activity, post-patch step 0 first: [TESTING-ROUTE](TESTING-ROUTE.md) |
<!-- /gen:plan:phase-6-en -->

P19 widens into a guided final campaign: everything that was coded, with the open boxes of [TESTING](TESTING.md), following the run described above.

## Phase 7: just before 1.0.0

User's request (2026-11-05): bring everything up to date once the plan is coded, in a logical order, so as not to redo everything. Each step builds on the previous one:

1. Review all the credits: they depend on what was added to the code.
2. The list of all the tools, on the home page and everywhere it appears, in English and in French.
3. The plugin's help and getting-started guide: they describe the tools.
4. All the `.md` files of the repository, brought up to date then in French and in English: English always first, with a note in French saying that the translation is at the bottom.
5. On Git and GitHub, two languages, English first then French (user's decision of 2026-11-09, which replaces "everything in English if possible"): commit bodies, issue comments, release notes, announcements; short titles, labels and milestones in English only. A single format, `tools/bilingual.py`, followed by `scripts/Release.ps1` (commit bodies) and `tools/discoveries-merge.py notice`; the release notes follow with the release chain. Open issues get the French when they are touched.

**Plan review (2026-11-08, night instruction X-74), to match the code of 0.865:**
- Phase 7 comes before phase 6 (in-game tests), at the user's request; what changes at the tests will only touch the texts concerned.
- 1. Credits: `Credits/CreditCatalog.cs` against the sources actually called (folders `Community/`, `Market/`, `Monsters/`, `Fashion/`, `Maps/`, embedded files); the discoveries base (X-43, X-72 to X-74) is not an outside source, it is described in the help, not in the credits. **0.866: every host called is credited; 14 references read for ideas (cited in the code) were missing: added, checked at the source (docs/evidence/0.866-credit-references.md).**
- 2. List of tools: the home page (`MainWindow.Pages.cs`), the README and everywhere a list appears, with the tools added since 0.539 (Discoveries, list panels, list alarms, etc.), in English and in French (language files).
- 3. Help and getting started: `Guide/GuideFlow.cs` and `MainWindow.Guide.cs`, to be reread against the list of step 2. **0.866: the help had one entry per page; outdated texts corrected (Discoveries, storage, crafting, company, map, island, deliveries, reminders), one question "What does Tensho keep on my PC?", two more stops in the guide (Discoveries, ocean fishing).**
- 4. `.md` files tracked by Git (30 apart from the evidence): English first, then the note in French and the translation at the bottom. Exceptions kept: `CHANGELOG.md` (each entry already has its English part), `docs/evidence/` (technical evidence, in English, cited by the code), the maintainer's notes (not in the repository). **0.866: README (all the pages, both halves) and the 14 public files done; ARCHITECTURE and ROADMAP lightened of their archive notes (they remain in Git); the living documents wait for the decision below.**
- 5. Git: commit messages already in English ("English" part of the CHANGELOG); issues, milestones, labels and the repository description remain to be checked. **2026-11-08: tickets already in English; #31 (submission to the official repository) closed "not planned" (DEC-039); milestone renamed "1.0.0 — Custom repository release"; #42 opened for the discoveries (X-73, X-74). The repository description ("TenshoAssistant") is a GitHub setting: to be changed only with the user's agreement.** **2026-11-08 (user's agreement): repository description written in English, Dalamud repository address as home page, topics added, push secret protection enabled, merged branches deleted; #39 and #40 moved to milestone 2 with "in-game-test".**
- **User's clarification (2026-11-08):** the `.md` documents are not only translated: each one is rechecked against the current code, right after the X-76 cleanup.
- **User's decision (2026-11-08, « Oui, à la fin » ("Yes, at the end")):** the living working documents of `docs/project` (GLOBAL-PLAN, TESTING, BACKLOG, USER-REQUESTS, DECISIONS, GAPS, REFERENCES, the *-PROPOSALS, PROPOSALS-TRIAGE, COMPETITIVE-OFF-PLAN) change with every version; translating them now would mean keeping two copies. Recommended default: translate them last, just before 1.0.0, once phase 6 is finished; the public files are done now. **Chosen: translated just before 1.0.0, once only.**
- **User's decision (2026-11-10 around 0:10, « Les 17 » ("The 17")):** it replaces the one of 2026-11-08 (R01): the 17 working documents of `docs/project` still in French only are translated now, before the in-game campaign, in several batches from the smallest to the biggest; the parts produced by a tool are translated in the tool. A translated document is then changed in both its halves (agreement check in check-docs). **0.1023: batch 1, 9 documents (CONTENT-TOOLS, USER-REQUESTS, G-PROPOSALS, COMPETITIVE-OFF-PLAN, BACKLOG, ITEMS-PROPOSALS, MAP-PROPOSALS, TOOLS-PROPOSALS, PROPOSALS-TRIAGE).**
- **User's decision (2026-11-10 around 1:20, « Les laisser en français » ("Leave them in French")):** `TESTING.md` and `TESTING-ROUTE.md` stay in French only. Both are written and read by the test tool (one checkbox per test, line numbers cited by the in-game test campaign); a second half would have to follow every test. They are the two exceptions of the bilingual check, each with its reason. **0.1024 to 0.1026: batches 2 to 4 (SETTINGS, CRAFT-GATHER-PROPOSALS, GAPS, DECISIONS; GLOBAL-PLAN; REFERENCES). 0.1027: point 4 done for what is translated, 33 bilingual documents of 35, the 2 others kept in French by this decision.**

## Full cleanup before 1.0.0 (X-76, detailed plan of 2026-11-08) — approved by the user (« Oui, ce plan » ("Yes, this plan"))

User's request: « un nettoyage qui te semblerait plus complet et cohérent avant une version 1.0.0 » ("a cleanup that seems more complete and consistent to you before a version 1.0.0"). Order first planned: after the necessary in-game tests,
before the `.md` documents (rechecked against the code then translated); done in fact before phase 6, like phase 7. Ultracode granted. Rule for the whole step: **nothing is removed without proof that it is useless**,
one family of changes per version, `Verify` green each time, zip backup in `checkpoints/` before starting.

**Progress:** step 0 done (0.904: `docs/project/audits/x76-0-baseline.md`, backup `checkpoints/backup-20261108-1450-x76-start.zip`); step 1, first pass done (0.904: `docs/evidence/0.904-dead-code.md`, 91 items removed); what remains in step 1 is the parts of the capture already proven (after the travel and extraction surveys). Step 2: 37 findings (`docs/project/audits/x76-2-duplicates.md`), handled by families; 0.905: map coordinates; 0.906: Eorzea times and durations; 0.907: game windows and web requests; 0.908: discoveries ("seen in game"); 0.909: French removed from the code, comments put back in place. French title "Tensho Assistant · Monnaies" of the server info bar: renamed "Tensho Assistant (Currencies)" in 0.910 with the user's agreement (« effacer les réglages n'est pas grave, il n'y a que moi qui utilise le plugin » ("erasing the settings does not matter, I am the only one using the plugin")).

**0. Measured starting point.** Figures recorded and kept in `docs/evidence/` to compare at the end: files and lines per folder, tests, language keys
(en, fr, de, ja), settings, network hosts called, parts of the capture, native types read, disabled warnings.

**1. Dead code and leftovers.**
- Trial build with the "member never used" analysers enabled (without keeping them): types, methods, fields, properties never read.
- Language keys never requested by the code (a script compares the language files with the code; keys built by name are listed separately).
- Embedded resources, data files, tools and scripts that nothing uses any more.
- Parts of the developer-mode capture already proven (leves, retainer, synthesis…) and debug logs of closed investigations.
- Settings always true or never read; old configuration migration branches that are too old (reading the old values stays).
- Cross-check by a second agent who tries to prove that each item is still used (reflection, built name, test, backup, command).

**2. Duplicates and code consistency.**
- Logic written several times (formatting of places and times, "per revision" caches, reads of game windows, HTTP clients, place menus): a single shared version when the risk is low.
- Names and terms aligned ("discovery", "seen by you", "imported", "community source") in the code and the texts.
- Outdated comments corrected; no French in the code (language rule).
- Files that are too long (Plugin.cs, large MainWindow files): split only where the split is mechanical and covered by tests.

**2b. Code harmonisation** (user's addition, 2026-11-08: « que tout soit pareil où ça a besoin de l'être, par exemple info-bulles ou autre » ("that everything be the same where it needs to be, for example tooltips or other")).
- A single way of doing each interface gesture: tooltips ("?" help, hover, settings), checkboxes and settings with a default, headers and
  sections, tables, buttons, theme colours and icons, error and status messages, logs (levels, English, message form).
- Spotted by agents (ultracode), then hand-written code replaced by the shared components, family by family, texts unchanged.

**2c. Parser coverage** (user's addition, 2026-11-08: « vérifier les parseurs, si on n'oublie pas de données, qu'on couvre tous nos besoins
et plus, en prévision de futurs outils » ("check the parsers, that we do not forget any data, that we cover all our needs and more, in anticipation of future tools")).
- For each parser and game reader: what it reads, what more it could read at the same moment (ignored fields), what is missing to replace a
  community source still read, and what the planned tools of the plan will need to keep from now on.
- Safe additions are coded; those that need an in-game survey go to the reminders table.

**Order set by the user (2026-11-08, night):** « fais X-93 en premier et le nettoyage après, on reprendra le reste après » ("do X-93 first and the cleanup after, we will take up the rest after"): X-93 in full (no discovery forgotten, discoveries shared on request), then X-76 steps 3 to 9, then the rest (X-85 V2…, X-86 to X-97).

**Order recalled by the user (2026-11-08, evening):** « uniformisation en premier, comme ça on l'applique à tout nouveau parseur » ("harmonisation first, so we apply it to any new parser"): 2b H7–H10 then 2c P4–P10 and E2–E5 come before the rest of X-85 (V2 points to V6), X-86 to X-89 and any new parser.

**Progress of 2b and 2c:** plan produced by a workflow (`docs/project/audits/x76-2bc-plan.md`): harmonisation H1 tooltips (0.913), H2 theme colours (light themes, 0.912),
H3 saving of settings and rollback, H4 sliders saved on release (0.914), H5 setting components, H6 "Default" line (0.915), H7 page states (0.920),
H8 item display (0.921), H9 logs (0.922), H10 files and subscriptions (0.923–0.925; DEC-038: reading of market offers switched off in PvP in 0.926, user's agreement); parsers P1 HQ of discounts, P2 attribution of gains, P3 label of each kind (0.911),
P4 "unlocked" state of items (0.927), P5 game sheets instead of community data (0.928–0.932), P6 Intuition and predators (0.933, except the swimmer lure: in-game survey), P7 submarine sectors (0.934: HQ, visits; meaning of the Survey/Yield/Discovered lines to be surveyed in game),
P8 counters for rates (0.935), P9 data read but thrown away, P10 end-of-mission rewards; steps added E1 format of the discoveries file frozen (0.916, DEC-042)
before 1.0.0, E2 test of the interface rules (covered by tests 912 to 922), E3 test of the cited evidence (0.937), E4 help "once per event" (0.937), E5 resilience against a large mass of data (X-93 done, 0.938–0.942: load test, indexed searches, your discoveries never refused, shared separately per kind and compressed, read on demand; docs/evidence/0.938-discoveries-load.md; limit: 200,000 shared per kind, user's choice, 0.943). Check of the data already surveyed
(user's request): probe `tools/DiscoveryCheckProbe`, 1281 facts, 11 wrong ones coming from 3 defects fixed in 0.911
(`docs/evidence/0.911-discoveries-check.md`). Decision for the final batch: reading market offers during a listing
(UndercutWatcher) does not stop in PvP (DEC-038), summary to give to the user before touching it.

**3. Settings: full inventory and putting in order.** — 0.944: inventory written (`docs/project/SETTINGS.md`, 301 lines, workflow of 4 agents), 9 confirmed anomalies fixed; 0.945: single setting "game and observed data only", clear the cache (new) and the discoveries with a zip copy beforehand (Backups folder, 5 kept); 0.948 (user's report, « 3 fois le même réglage » ("the same setting 3 times")): crafting comparison window and its two parts grouped (parts indented and greyed out when the window is off); the inventory had looked at each setting alone, not their links: groups pass done (agent, 26 groups, checkpoints/x76-settings-groups.json): batch A in 0.952 (settings pages), batch B in 0.953 (map), overlaps 13, 24, 25 in 0.954; what remains is the possible reorganisation of the pages (G-PROPOSALS, to be reviewed); what remains is the possible reorganisation of the pages (grouped decision).
- Table of all the settings (`docs/project/SETTINGS.md`): name, default value, where it is read, page, tooltip, real effect, what happens if it is off.
- Fixes: setting with no effect, effect different from its name, inconsistent default, duplicate between pages.
- Reorganisation of the settings pages by theme if the inventory justifies it (grouped decision submitted to the user, old value still read).
- Missing settings where a behaviour is imposed with no choice.
- Requests already noted: a single setting "game and observed data only" (switches off all community sources at once); clear the cache and the discoveries with a zip backup just before.

**4. Tooltips and texts.** — 0.947: the "Default" line of the 137 yes/no settings checked against the real value, in English and in French (all correct, test DefaultLines947Tests); settings tooltips reread by the inventory of step 3 (4 corrected in 0.944); English and French: 6,900 keys each, none missing; German and Japanese: 35 keys, the rest in English (reported, not translated); remaining: the help and the guide reread against the final pages, with the documents (X-91).
- Each tooltip reread against the code, those of the settings first, with a correct "Default: …" line.
- English and French parity; German and Japanese: only report what is missing.
- Help and guide texts aligned with the final state of the pages.

**5. Network, privacy and Dalamud rules.** — 0.949: audit (`docs/project/audits/0.949-network-rules.md`); user's choices applied (PaissaDB switch covered by "game data only", GitHub exchange credited, shared dates rounded to the day, warning before connecting, manifest); not retained for now: stopping already started jobs in PvP and the Discord alert "mission found", exact position shared.
- List of each host called: when (click, automatic chosen by the user), size limit, timeout, header, switch-off in PvP, setting that turns it off.
- Check of the list of Dalamud rules (`docs/project/PUBLISHING.md`) point by point; any gap summarised to the user before fixing.
- Data kept on disk: list, maximum size, purge, what "clear" erases.

**6. Robustness and performance.** — 0.950–0.951: map vendors, fine measurements; audit of per-frame calculations (agent, checkpoints/x76-draw-cost.json, 9 findings): batch 1 in 0.955 (vendor card, equipment sets, Bank, achievements by name); batch 2 in 0.956 (nearby monsters, map menus, guide, collections); to follow: reread the finely measured "Slow frame" entries (home page, hover);
- Each native read: framework thread, checked pointer, copy, switch-off in PvP, resilience to game updates (R-14).
- Each subscription, timer and task: owner and cleanup (existing test extended if needed).
- Draw: loops and allocations per frame, caches recomputed too often; the "Slow frame" report is the judge.

**7. Tests.**
- Tests that freeze an old exact text instead of a behaviour: rewritten on the behaviour.
- Missing tests for what the cleanup touches (settings, migrations, new global setting).
- Dead or duplicate tests removed.

**8. Repository.**
- `.gitignore`, scripts, `tools/README.md`, CodeQL and build workflows, open alerts, labels, milestones and tickets closed on proof.
- `community-discoveries` and `community-help` checked (discoveries validation rule).

**9. End of the step.** — 0.957: written comparison (`docs/project/audits/x76-9-comparison.md`); steps 7 and 8 done (state of the tests, clean repository); what remains is the guided in-game pass with the user and the proposals to review.
- Comparison with the starting point (evidence in `docs/evidence/`).
- A quick in-game pass over each page (guided list, one point at a time), before the documents then 1.0.0.

## Topics added on 2026-11-06

Chosen by the user from the list of sections of a French website. No data from that site: see [REFERENCES](REFERENCES.md). Everything comes from the game's sheets and structures.

<!-- gen:plan:w-en -->
| Point | Topic | State |
|---|---|---|
| W-01 | Custom Deliveries page: clients, rank, points, week | Done in 0.675 |
| W-02 | Masked Carnivale: stages done and remaining, no combat advice | Done in 0.683 |
| W-03 | Relic weapons: progress per expansion | Done |
| W-04 | Island Sanctuary animals | Done in 0.685 |
| W-05 | On-screen panels: workshop project materials and preparation list, adjustable | Done in 0.691 |
| W-06 | « What can I craft? »: sort by most profitable in gil, scrips or other | Done in 0.707 |
| W-07 | « What can I craft? »: count sub-recipes | Done in 0.696 |
| W-08 | Levequests: right click to the nearest aetheryte | Done in 0.698 |
| W-09 | Tracking list by origin (gathering, NPC, monster, other), workshop and crafting | Done in 0.699 |
| W-10 | Allied Societies: rank and quests of each society | Done in 0.713 |
| W-11 | Grand Company: rank, seals and missions on one page | Done in 0.714 |
| W-12 | Special deliveries: Crystarium, Sharlayan Academy, « Bazar aux Mille Perles » | Done in 0.715 |
| W-13 | Next relics: Anima, Eureka, Resistance, Manderville, phantoms, and their armour | Done |
| W-14 | Eureka and Bozja: FATEs, skirmishes, Critical Engagements, front reports, Lost Actions, no combat advice | Done in 0.717 |
| W-15 | Deep Dungeon: Palace of the Dead, Heaven-on-High, Eureka Orthos, Pilgrim's Traverse, floors and loot, no combat advice | Done in 0.718 |
| W-16 | Lexicon of game words, French and English, from its sheets | Done in 0.719 |
| W-14b | FATEs of all open zones, location and level, from the zone files | Done in 0.721 |
<!-- /gen:plan:w-en -->

## Requests of 2026-11-07, while playing

Noted in the order the user made them; to be done after the current task, one version at a time, without forgetting any. A line changes to "done" with its version.

<!-- gen:plan:x-en -->
| Point | Request | State |
|---|---|---|
| X-01 | Sluggishness: find what blocks a Tensho frame (120 to 230 ms every few minutes, 2.6 s once) and move it out of the drawing | In progress: « Slow frame » reading in 0.745; gathering help (97 ms) computed outside the frame in 0.747, with the measurement of the main window and the other windows … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-01) |
| X-02 | Data kept longer, re-read only if absent; settings that complete « Cache & expiry » | Longer default durations and « Never » renamed « Only if absent, or on Refresh » in 0.750. Agreement given … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-02) |
| X-03 | « Open in Tensho » link of an alarm: opens nothing | No click arrived (0.743 log). Dalamud no longer finds the link after a plugin reload: registered again at load in 0.759 … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-03) |
| X-04 | At a gathering point, highlight the items that the tracking lists (crafting and Free Company workshop) need | Done in 0.751 |
| X-05 | Crafting and workshop tracking lists gathered by gathering zone | Done in 0.752 |
| X-06 | « Gather automatically » ticked in the game window: the plan keeps only the actions done before the first gathering, then nothing, and… | Done in 0.753 |
| X-07 | « What can I craft? »: an item craftable by two jobs counted once | Done in 0.739 |
| X-08 | Tooltip of a timed point (map): « closed now » without saying when it opens; add the countdown | Done in 0.754 |
| X-09 | Crafting and workshop tracking lists: a button that adds the alarms of all their timed points, without duplicating an existing alarm | Done in 0.755 |
| X-10 | Gathering help: first select an item that the tracking lists need | Done in 0.756 |
| X-11 | Tracking lists: be able to collapse each gathering zone | Done in 0.757 |
| X-12 | Tracking lists by zone: the zone where one is first, then the others sorted taking into account the teleport cost to each | Done in 0.757 |
| X-13 | Tracking lists: items of timed points (alarms) in their own collapsible group, with a suitable name, instead of the zone name | Done in 0.758 |
| X-14 | « Timed points » group of the tracking lists: say for each item whether its point is open or closed | Done in 0.761 |
| X-15 | Right-click menu of an item: « Go there » directly (gathering point, nearest aetheryte) | Done in 0.762 |
| X-16 | GP plan: at equal yield, prefer actions that last the whole gathering, for fewer clicks | Done in 0.763 |
| X-17 | Reduced tracking panel: the help loses « Your lists still need » and the frame; frames go above the other windows | Done in 0.764 |
| X-18 | Gil log: icons (gil, categories) and less « Other »: find other categories from the game | Icons and colours in 0.768; « Other »: read in the log (« Gil other »); 0.775: spending after a loading screen = teleport; « +1,000 » without a window still unknown |
| X-19 | Gil log: a deposit with a retainer counted as spending; the retainers' total only follows on reopening | Done in 0.769 |
| X-20 | Items deposited with a retainer, in the saddlebags or in the Free Company chest: the copy must follow while the window is open, without… | Done in 0.770 |
| X-21 | « Add the alarms » offered 19 alarms, including items that looked already complete and crystals that can also be found without a schedule | Done in 0.772 |
| X-22 | Monster loot, wiki lines « Monster · level · Zone (x,y) »: no right click to the nearest aetheryte | Done in 0.777 |
| X-23 | Alarms: sending to Discord disabled by default | Done in 0.778 |
| X-24 | After a plugin load, monster items spend a while under « Other »: the monster source is only re-read from disk later | Done in 0.779 |
| X-25 | **Game freeze, 15:18:45**: the Raphael solver (native) panics on a division by zero (quality_upper_bound_solver/state.rs:59) when opening… | Done in 0.782 |
| X-26 | « Compare next moves » on an impossible high-quality recipe: « mismatched values », no projected move (the game shows a quality out of… | Done in 0.784 |
| X-27 | Tracking lists must be able to show only finished items (the items to craft, without their materials); « oublie pas atelier et artisanat »… | Done in 0.785 |
| X-28 | Free Company workshop: take into account what is already delivered to the project; « oublie pas de verifie si on a valide des etapes aussi… | Done in 0.787 |
| X-29 | Crafting log: as Carpenter on a Blacksmith recipe (Mythril Ingot), Tensho does not flag that it is not the right job and offers its macro | Done in 0.788 |
| X-30 | See the remaining crafting steps (intermediate items to craft) for the workshop and crafting lists | Done in 0.789 |
| X-31 | « Remaining crafts »: each step must be expandable down to the final step; a single number format, like the other rows (« owned / required… | Done in 0.790 |
| X-32 | Map: find a solution to the blockage worked around by opening then closing the game map (levequest and quest zones absent until the game… | Done in 0.793 |
| X-33 | Storage, « What to do with it? »: set prices like « What can I craft » and take sales per day into account (Grade 1 Dark Matter at 350,000… | Done in 0.794 |
| X-34 | Nearby monsters (game object table), information only: Dalamud rule to summarise and user's agreement before any code | Done in 0.795 |
| X-35 | Nearby monsters: those that drop an item requested by the tracking lists (« Monster » category), on the map | Done in 0.795 |
| X-36 | Nearby monsters: those missing from the hunting log | Done in 0.796 |
| X-37 | Nearby monsters: targets of accepted levequests and current FATEs | Done in 0.797 |
| X-38 | Blue Mage: nearby monsters that teach a spell not yet learned, Blue Mage icon above their head; setting to have it all the time, even… | Done in 0.798 |
| X-39 | Beastmaster: nearby monsters from the beastmaster's bestiary, same display and same settings | Done in 0.804 |
| X-40 | Nearby monsters: other uses to propose, and consistent settings in the right place (map, relevant page) for each use | Done in 0.1002 |
| X-41 | Nearby monsters: those that drop a missing collection item (mount, minion, orchestrion roll), if the loot data says so | Done in 0.800 |
| X-42 | Before phase 7: take the user's settings and make them the default values | Dropped on 2026-11-07 at the user's request (« garde valeur par défaut cohérente » ("keep consistent default values")): the default values stay those of the code, nothing was published |
| X-43 | Local discovery database: record what is within range (monsters and their locations, loot, learned blue spells, beastmaster captures, gathering points, fishing…) to complete the tools; debug options but active outside debugging; import (merge with ours) and export, to stop depending on community data as one plays | Frame accepted on 2026-11-07 (« Oui, ce cadre » ("Yes, this frame")): local file, provenance per entry, no other player, nothing sent, no combat help, cut off in PvP, import … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-43) |
| X-44 | Crafting log: no Tensho right-click menu, neither on a recipe of the list nor on a material | Done in 0.808 |
| X-45 | Map: event vendors are shown although they are not there in game (Limsa, the Aftcastle); to fix just before phase 7 | Done in 0.810 |
| X-46 | Map: the zoom does not always stay the one that was set; to do just before phase 7 | Done in 0.850 |
| X-47 | Quest rewards (quest window): neither a Tensho panel under the item tooltip, nor a Tensho entry; to do just before phase 7 | Done in 0.852 |
| X-48 | Map: right click on a quest target (« Target indicated by the game (log) ») does not offer the nearest aetheryte; to do just before phase 7 | Done in 0.853 |
| X-49 | Map: the quest target does not disappear after the quest is completed (Ishgard, the Foothills, capture of 2026-11-07) | Done in 0.813 |
| X-50 | Map: map-edge icons must pass above everything (hidden under the name banner, Abalathia capture) | Done in 0.814 |
| X-51 | Collectables panel: highlight on the hotbars the skills that the plan asks for, like the gathering help (capture: Œil-de-verre) | Done in 0.815 |
| X-52 | Discovery database: prices seen on the sales board replace Universalis (more reliable), after checking that we have as much information | Done in 0.817 |
| X-53 | Nearby monsters: the woolly yak and the loaghtan (yak milk, loaghtan meat from the tracking list) have no icon on the map nor above them (captures, Coerthas Western Highlands, Dravanian Forelands) | In progress: the MobDrop data do link 12888 to woolly yaks 3479 and 11214; 0.818 notes the chain in the log in developer mode … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-53) |
| X-54 | When opening a storage other than the inventory (retainer, chocobo saddlebag, Free Company chest, others): highlight the items in it that… | Done in 0.820 |
| X-55 | Crafting and workshop tracking lists: specify when the item is not on oneself (with a retainer, in the saddlebag, the Free Company chest…) | Done in 0.822 |
| X-56 | Crafting and workshop tracking lists: in place of « teleport 219 gil », the Teleport skill icon then the gil icon and the amount, without… | Done in 0.824 |
| X-57 | Map, culinarian quest in Limsa: the target (at the right place) and the quest icon are not at the same place, the displayed quest falls outside the zone, and it does not appear on the Upper Decks map (captures of 2026-11-07, the game map shows the link to the Upper Decks) | To do, after X-56; in progress: the game gives two markers for quest 2046 « Le pari » on map 11 (icons 71141 and 71143, objectives 2046 and 520), its list … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-57) |
| X-58 | Tensho right-click menu on an item: add its recipe to an already created crafting list | Done in 0.826 |
| X-59 | Tracking lists, timed points: remove « open » / « closed », keep the timer alone, green when the point is open, red when it is closed | Done in 0.827 |
| X-60 | Tracking lists, right click on an item: « Place the flag » absent when on the place's map, and « Go there » absent for some (Cyclops Onion… | Done in 0.830 |
| X-61 | Gathering help: no plan offered, « item read after a GP spend on this point » although the trials are at 4/4 (GP 291/823, Cyclops Onion… | Done in 0.831 |
| X-62 | Alarms added from a tracking list: they still ring when the resource is already gathered (Abalathia's Spine Rock Salt 24/2, capture of… | Done in 0.832 |
| X-63 | « Go there » to a zone without an aetheryte (Dravanian Hinterlands): teleport to an aetheryte of a neighbouring zone that opens onto the… | Done in 0.833 |
| X-64 | Tracking lists, right click: neither « Go there » nor « Place the flag » for Emerald Beans (Fauche, the Churning Mists of Dravania, capture… | Done in 0.834 |
| X-65 | Crafting tracking list (Candied Chestnuts): birch syrup, a held intermediate ingredient (157/1), does not appear in the panel; and the… | Done in 0.838 |
| X-66 | Storage frame (X-54): at the retainer, it frames the cooking wine instead of the quality cooking wine (same icon), and the frame stays when… | Done in 0.839 |
| X-67 | Tensho map: flag that a target is higher or lower, like the game map (up or down arrow on the icon) (Mor Dhona capture, 2026-11-07) | Done in 0.842 |
| X-68 | Tensho map: the culinarian quest zone (Mor Dhona) « ne se fait pas zoomer dessus » ("cannot be zoomed on") (capture of 2026-11-08) | Clarified: « from the log ». 0.845: opened from the log, a quest that has a zone is zoomed to show it entirely (60% of the view) … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-68) |
| X-69 | Tensho map: « la map ne se rafraîchit pas assez » ("the map does not refresh enough"): the quest moved to « Speak to Raulf », but the «… | Done in 0.847 |
| X-70 | Tensho map: the zone of a quest's new step is missing until the game map has been closed then reopened (Kugane, « L'art du sushi »… | Done in 0.848 |
| X-71 | Tensho map: take over the game map's higher / lower icons (views at Mor Dhona) everywhere, in place of the 0.844 arrow, « on en a besoin… | Done in 0.849 |
| X-72 | Discovery database (parsers): check that it is effective, the user has already played a little (2026-11-08) | Done in 0.856 |
| X-73 | 0.1003 (re-check of 2026-11-09: user's database and community sources): airship loot (new kind AirshipDrop, sector taken from the voyage log, guard: a single airship and as many steps as sectors); development log of unattributed gains (no monster loot noted: to explain at the next fight). Still without a parser: treasure map loot (in-game reading first), garden (X-108), PaissaDB (hook, Dalamud rule). Parsers: discover in game everything Tensho takes from community sources (LuminaSupplemental, Garland Tools, wiki, GatherBuddy fishing, Teamcraft bite times, Fashion Report) and complete Universalis (prices of the other worlds of the data centre when going there); eventually depend only on Universalis, if possible; ultracode authorised if needed (2026-11-08) | Mapping done (docs/project/audits/0.857-community-map.md); steps X-73.1 to X-73.12 below, in this order |
| X-73.1 | Market: prices seen on each visited world of the data centre, preferred to Universalis when more recent; age shown; purge after 90 days | Done in 0.857 |
| X-73.2 | Market: sales histories merged from one opening to the next (sale speed computed locally) | Done in 0.858 |
| X-73.3 | Fishing: flags from the game's log (« no time or weather condition ») instead of community data | Done in 0.859 |
| X-73.4 | Fashion Report: theme and categories read from the game (FashionCheckManager) instead of guesses | Done in 0.861 |
| X-73.5 | Desynthesis and reduction: items obtained from which item (AgentSalvage, AgentPurify) | Done in 0.860 |
| X-73.6 | Retainer ventures: rewards seen (AgentRetainerTask) | Done in 0.860 |
| X-73.7 | Duty chests: items seen per duty (Loot.Items) | Done in 0.860 |
| X-73.8 | Voyages: loot per sector (already read in the workshop) | Done in 0.862 |
| X-73.9 | Blue Mage and Beastmaster: spell learned and beast tamed, with the monster | Done in 0.863 |
| X-73.10 | Fishing: catches (bait, mooch, place), bite time, conditions seen; in-game probes first | Done in 0.971 |
| X-73.11 | Monster loot: items received after the death of one's target (the riskiest: attribution) | Done in 0.860 |
| X-73.12 | The rest according to the map: item chests, FATE rewards, vistas, island animals, festivals seen | Done in 0.864 |
| X-73.13 | Request of 2026-11-08 (« oui tous en faisant tests en jeu nécessaires » ("yes all, doing the necessary in-game tests")): developer-mode… | Done in 0.869 |
| X-73.14 | Offers seen marked « mine / not mine » (retainer number compared to yours, no name kept): retainer comparison and « copy the price » with… | Done in 0.870 |
| X-73.15 | Data center → region table from the game's sheets (WorldDCGroupType) instead of Universalis | Done in 0.871 |
| X-73.16 | Location of vendors missing from the sheets: noted when their shop opens (target, zone, position) | Done in 0.872 |
| X-73.17 | Loot from the chests of Deep Dungeons, Eureka and Bozja | Done in 0.873 |
| X-73.18 | After the X-73.13 readings: bite time and type, mooch, Intuition and predators; aetherial reduction; quest items | Done in 0.887 |
| X-74 | Night instruction (2026-11-08, the user is asleep): add as many coherent parsers as possible (« mieux vaut prendre plus et retirer ensuite… | Done |
| X-75 | Request of 2026-11-08: a test for each probe; recheck that no probe is missing for discovering in game the data taken from somewhere other… | Done |
| X-75.1 | A test for each probe: list of the parts of the capture frozen by a test, text test for each probe and parser that has none; in-game… | Done in 0.874 |
| X-75.2 | « Seen in game » first everywhere the learned data already exists: Monsters page, nearby monsters (loot, beasts), right-click menu… | Done in 0.875 |
| X-75.3 | Fishing: mooch catch noted with the mooched fish (not the bait), lure, previous weather, « seen in … » in the fishing panel | Done in 0.877 |
| X-75.4 | Blue Mage: monsters seen casting a spell (cast bar), spell levels drawn from what has been seen | Done in 0.878 |
| X-75.5 | Other pages: festivals « seen from … to … », locations of FATEs seen on the S-Rank page, weather of vistas seen, region's best price… | Done in 0.879 |
| X-75.6 | Game calculations instead of community formulas: submersible voyage durations (game functions), check of the Ocean Fishing schedule and of… | Done in 0.880 |
| X-75.7 | New developer-mode probes, then parsers after the readings: island animals, spearfishing, airship sectors and destinations, submersible… | Done in 0.881 |
| X-75.8 | Outside the parser: PaissaDB (reading the sign requires a hook: Dalamud rule, user's agreement first), hookset used (hook), Mog Station shop, future dates, worlds never visited: these stay with community sources | Noted |
| X-77 | Request of 2026-11-08: if possible, a system that goes through Git to send the data discovered by the parsers and fetch other people's that… | Done in 0.882 |
| X-77.1 | User feedback (2026-11-08): « le système actuel pour import export avec git est pas très pratique » ("the current import/export system with… | Done in 0.883 |
| X-78 | Request of 2026-11-08 (capture of the Tensho map, The Ruby Sea): keep the left-hand icons (spearfishing node fish, with its zone) and also… | Done in 0.890 |
| X-79 | Request of 2026-11-08 (spearfishing trial): the spearfishing panel's list must stay all the time and highlight the fish of the right size… | Done in 0.891 |
| X-80 | Request of 2026-11-08 (capture of the map filter): « Gathering points » split into miner, botanist and fisher | Done in 0.895 |
| X-81 | In-game trial of forced melding (X-75.7) | Done in 0.899 |
| X-82 | In-game trial of the Fashion Report (X-75.7) and user feedback: right-click menu greyed out on a list piece's icon | Done in 0.900 |
| X-83 | User feedback (2026-11-08, on the island next to a monster: no icon on the map) and reading of island animals (X-75.7) | Done in 0.901 |
| X-84 | In-game trial of mail (X-75.7): a letter with an item and 1 gil | Done in 0.903 |
| X-85 | User's « to do » document (2026-11-08), Ocean Fishing (priorities given by the user): the Ocean Fishing panel must look like the fishing aid (fish icons, bite times); the ordinary fishing aid switched off during Ocean Fishing; spectral current fish and fish that yield orange scrips highlighted; **prio 1**: best bait to trigger the spectral current; **prio 2**: mooching and current objectives; defect seen in the log: the computed schedule gives route 12 or 15 when the game says 14 | V1 done (0.917): schedule checked per port by your voyages, ordinary fishing aid switched off at sea … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-85) |
| X-86 | 0.966: « quests not yet available » checkbox (right click and settings). 0.967: height arrow on quest and event zones (discreet chevron… | Done in 0.966 |
| X-87 | 0.968: zone of an accepted leve removed when that leve is in progress (and markers the game does not draw); leve targets with the game's… | Done in 0.968 |
| X-88 | 0.969: 256 categories per zone, adding a whole group of categories (game groups). « To do » document, inventory sorting: more than 64… | Done in 0.969 |
| X-89 | 0.970: quest expansion (game data) in their tooltip and sheet; item expansion only when gathering locations and quests that give it agree… | Done in 0.970 |
| X-90 | 0.996: Deep Dungeon bestiary through the same wiki reading (page of each floor group: enemies and notes; game title without « The » tried)… | Done in 0.996 |
| X-91 | 0.971: parsers rechecked (`docs/evidence/x91-parsers-check.md`): only one planned item was missing (locations of FATEs seen on the S-Rank… | Done in 0.971 |
| X-92 | 0.1001: inventory of the lists frozen in the code: only that of the Labyrinth territories (cut-off) was concerned; its boards are now also… | Done in 0.1001 |
| X-93 | Request of 2026-11-08: « prévisions pour une masse de données conséquente possible » ("provisions for a possibly substantial mass of data") (discoveries shared by many players, long play times). Starting measurement: your Discoveries.json file is 409 KB after about a day of play, MarketSeen.json 60 KB, the shared file 166 KB for 664 facts (2026-11-08); at 100 players over a few months, the shared file may exceed tens of MB. To plan: (1) load test with invented data (100,000 then 1,000,000 facts): load time, memory, save time, merge time, per-frame cost of the pages that read them; (2) loading and saving off the game thread, never the whole file at each reading (batched writing, temporary file then replacement); (3) in-memory index by kind, item and zone, so that pages and tooltips never scan everything; (4) cap per fact (bounded counters, distinct values already limited to 32 by DEC-042) and merging of readings that are too fine (map cells); (5) shared file split if needed (by kind or by expansion) and downloaded only on change (checksum), compressed; (6) merge tool: limits per player and per issue (already: refusal ≤ 5%, plausible volumes), measured merge time, players merged in batches; (7) Discoveries page and export: pagination and filters instead of showing everything; (8) the format stays the one frozen by DEC-042 (version 2): splitting and compression are done around it, without changing the content of a fact. | With 2c of the X-76 cleanup (P8 counters, P9 discarded data), before 1.0.0. Load test (1) done in 0.1036 (docs/evidence/x93-load-test.md); according to its figures, after the release that follows patch 7.58: first (2) (the base copied on the frame: a frame lost from 50,000 to 80,000 facts; a Discoveries.json over 64 MB, about 218,000 of your own facts, is not read and then overwritten: to prevent before then), then (5) before about 120,000 facts in the common file (the plugin's 32 MB bound; GitHub's 100 MB per file at about 384,000), (3) and (7): the page's totals and the Seen() index off the frame from 150,000 to 300,000 facts; (4) not needed; (6) limits in place, batches not needed (3.6 min at a million) |
| X-94 | 0.960: cause found and fixed (a fishing spot's number was looked up in the gathering points table and landed on a miner's legendary point).… | Done in 0.960 |
| X-95 | 0.950: first cause found and fixed (map vendor list rebuilt twice at each zone change, festivals read empty for a moment… | Done in 0.950 |
| X-96 | 0.959: cause found and fixed (zones with individual weather — story not completed — excluded from any forecast… | Done in 0.959 |
| X-97 | 0.982: user's answer (DEC-045): seen in the fishing aid; under each collectable fish, the collectability tiers and the scrips of each (game… | Done in 0.982 |
| X-98 | V2 points and V6 scrip reading (0.964): points of each catch read from the stop's data and noted, displayed per fish; scrips written to the… | Done |
| X-99 | Review of 2026-11-09 (DEC-045), report: after a teleport, markers of quests not yet accepted were missing from the Tensho map | Done in 0.977 |
| X-100 | 0.999 (DEC-048): Settings in seven tabs (General, Panels beside the game, Map, Tracking and notifications, Data and sources, Advanced… | Done in 0.999 |
| X-101 | Idea kept (DEC-045, from C-09): alert when an item on the wish list is for sale under a chosen price (Universalis) | Done in 0.1000 |
| X-102 | Idea kept (DEC-045, from C-15): progress of tracked achievements kept on disk, alert when one is completed | Done in 0.987 |
| X-103 | Idea kept (DEC-045, from C-14): « Prepare » button that sends the items of accepted crafting leves to the preparation list | Done in 0.988 |
| X-104 | Idea kept (DEC-045, from C-05): missing pieces of an outfit added to a list in one click | Done in 0.989 |
| X-105 | Idea kept (DEC-045, from I-12): filter by job in the item list (item cache format to change) | Done in 0.991 |
| X-106 | Idea kept (DEC-045, from C-01): « done » state of duties kept per character, visible offline | Done in 0.990 |
| X-107 | Idea kept (DEC-045, from I-04): gil earned or spent today in the server bar | Done in 0.986 |
| X-108 | Request of 2026-11-09: gardening parser to fill the gaps by discovery, crossbreeding included. Plan: 1) in-game reading of a harvest that yields seeds (chat lines, items); 2) new kinds of discoveries: sprout observed (planting → maturity: real hours per seed), tending observed (interval), harvest (seed planted → items and quantities), crossbreeding (seed obtained ≠ seed planted, with the seeds of neighboring slots noted in the log); 3) community sharing (merge tool updated); 4) the log and the crossbreeding grid completed by these readings, the community source (nick75g) kept as fallback and marked; missing seeds (Cornflowers) from the sheets | In-game reading first (reminders table); after X-97 |
| X-109 | 0.981: panel beside the game's board (before entering, and in the Labyrinth only with the board open and out of combat: Tensho's only… | Done in 0.981 |
| X-110 | 0.984: Beastmaster page, location column: « (n places seen) » with the positions menu (flag, nearest aetheryte): captures with a location… | Done in 0.984 |
| X-111 | 0.983: the shared component has existed since 0.704 (PlaceMenu: right click, flag, nearest aetheryte, map, weather; 21 places); inventory… | Done in 0.983 |
| X-112 | Finding from the test audit of 2026-11-09 (no. 0), noted in 0.1019: the map-edge arrows for timed gathering points (MainWindow.MapView.cs… | Done in 0.1041 |
| X-76 | Request of 2026-11-08: full code cleanup (« ne plus avoir de chose qui serait inutile maintenant » ("no longer have anything that would be… | Done |
| X-113 | Shared discoveries: when a ticket brings no new fact (gist unchanged), discoveries-merge.py must neither rewrite… | Done in 0.1029 |
| X-114 | Request of 2026-11-10: Tensho's map only (not the game's map: its compass already shows the weather): an icon of the current weather of… | Done in 0.1049 |
| X-115 | Request of 2026-11-10: client map database tool, without Dalamud (Lumina alone, reading the sqpack), step A: snapshot of the installed… | Done in 0.1028 |
| X-116 | Request of 2026-11-10, follow-up of X-115 (B and C): full database per version (every placement of the .lgb layers: NPCs, objects, FATE… | Done in 0.1029 |
| X-117 | Request of 2026-11-10, follow-up of X-115 (D): in the plugin, at the first load after a patch, compare the old cache with the new client… | Done in 0.1048 |
| X-118 | Request of 2026-11-10, plan 4 chosen by the user (desktop application linked to the plugin, the same pages outside the game), step 0: probe… | Done in 0.1030 |
| X-119 | Follow-up of X-118 (plan 4), steps 1 to 4: 1) application with the game closed, data pages (items, recipes, weather, lexicon, discoveries, collections from the saved files); 2) local link with the plugin (named pipe, current user only, no network port, off by default; state read on the framework thread, copied, sent off the thread, R07) and live pages; 3) actions on a click only, carried out by the plugin like the in-game button (flag, teleport request, alarms, lists); 4) a setting per page: in the game, in the application or both; the map, the panels attached to game windows, the gathering helper, live crafting and the Fashion Report stay in the game. Application distributed separately (GitHub release), never launched nor contained by the plugin; sync (request of 2026-11-10): the application and the plugin carry the same number, are built, tested (every page drawn outside the game in Verify) and published together; each release carries the application, Verify -Release refuses without it; the application checks its compatibility with the installed plugin | Step 1 and sync done in 0.1031 (application src/TenshoAssistant.Desktop with the game closed, self-contained, released with the plugin; Verify draws it without a window). Left after the post-patch 7.58 release: steps 2 to 4, and the font seam in the plugin replacing the Mono.Cecil rewrite of a copy (39 UiBuilder.IconFont calls; font read from UiComponents, set by Plugin.cs from UiBuilder and by the application from its atlas, ratchet test) |
| X-120 | Request of 2026-11-10, revised the same day by the user: plan 2 only of the six plans: a test tool for plugin creators, in a separate… | Done in 0.1032 |
| X-121 | Request of 2026-11-10: automatic update of the desktop application from GitHub, fully handled by the agent (no step for the user). At start… | Done in 0.1033 |
| X-122 | The user's decisions of 2026-11-10 (decision session) to code in the plugin: D-102 a « Also send to Discord » box for the alarms written in… | Done in 0.1045 |
| X-123 | The user's decision D-100 (2026-11-10): code of conduct (CODE_OF_CONDUCT.md, both languages), CODEOWNERS (owner review requested on other… | Done in 0.1033 |
| X-124 | The user's approval of 2026-11-10 ("I give you the approval for everything") for the rest of X-120: a separate GitHub repository for… | Done in 0.1035 |
| X-125 | The user's approval of 2026-11-10 ("yes"): PluginUiTest tried on 4 or 5 popular open-source Dalamud plugins (built from their sources… | Done in 0.1036 |
| X-126 | The user's request of 2026-11-11: install the desktop application from Tensho, never mandatory, only from Settings, section "Desktop application" (made precise on 2026-11-11), with the interface settings that go with it ("installed / version / up to date" state, Install or Update button, install folder, X-121's automatic update, link to the release; en/fr texts, FAQ). Download from the repository's GitHub release, SHA-256 checked (X-121), installed beside, never started by the plugin without a click of the user. Decision D-137 (2026-11-11): an accepted exception to Dalamud's plugin rules for our application, Tensho being on a custom repository; should Tensho ever aim at the official repository, this install can be turned off in the submitted build. Changes X-119's rule "never started nor contained by the plugin". | After X-119 is finished (steps 2 to 4: local link, actions, per-page setting); not before |
<!-- /gen:plan:x-en -->

## In-game survey reminders, by moment

Surveys from the "to do" document (2026-11-08): combat mandate OK (X-37); housing lottery OK (N-04); submarine done (result windows captured, parser P7 to code);
ocean fishing done (27 catches noted, "!!!" seen); not yet: "defeat X" quest, garden harvest (timer), first quest item hand-in.


User's instruction (2026-11-07): the surveys are done while playing; when the time comes, they ask "remind me" and the agent gives the steps for that moment. Developer mode enabled for everyone (the log carries the "Reading …" lines).

This table is the one list of the pending in-game readings: the test route (step 0) and the reminders point to it.

<!-- gen:plan:reminders-en -->
| When | What to do | Line read in the log | Point |
|---|---|---|---|
| Tensho map open, while playing | Nothing more, let it run a few minutes | « Slow frame » (10 chunks) | X-01 |
| An alarm rings in the chat | Right click on the item's line | « Context menu opened » | X-03 |
| Battle leve | Accept it, start it, head toward its targets | « Reading monsterEvents » | X-37 |
| « Defeat X » quest | Go toward the quest's monsters | « Reading monsterEvents » | X-40 |
| Beastmaster | Beastmaster page: « Read the wiki bestiary », then as a Beastmaster near a monster of a missing beast: Beastmaster icon above it | nothing, by eye (capture if wrong) | X-39 |
| A gil gain without a window (« +1,000 ») | Note the time | « Gil other » | X-18 |
| Garden, timer finished | Harvest (0.978: harvest recognized on a slot); a flower pot too | garden messages, pot target | #36 |
| Garden, harvest with seeds (crossbreeding) | Harvest a slot whose neighbors have other seeds (developer mode) | « Garden chat capture »: lines of the items and seeds obtained | X-108 |
| The airship returns | Open the voyage panel | voyage reading | T26 |
| Free Company workshop (developer mode) | Enter the workshop, open the submersibles window | « Submarine voyage check »: game functions against the formula (DEC-043) | X-75.6 |
| Eureka Anemos map (X-86) | Open the Tensho map with « hide undiscovered », take a capture next to the game's map | offset of the undiscovered parts (mask size and position) | X-86 |
| Retainer list open (developer mode) | Open the retainer list | job, level and end of sales of each retainer (fields ClassJob, Level, MarketExpire) | X-76 P9 |
| End of a duty (dungeon, trial) | Finish the duty, open the chests | end-of-duty rewards and chest identifier (P10: DutyCompleted, ChestObjectId) | X-76 P10 |
| Eureka, Bozja or Occult Crescent (developer mode) | Enter, gain a little experience, change the magia (Eureka) or the holster (Bozja) | « Reading fields »: levels, experience, magia | X-90 |
| Fishing with a surface lure | Cast with a surface lure, catch a fish | cast indicator 32 and the lure item | X-76 P6 |
| Housing lottery | Open the lottery window | lottery reading | N-04 |
| Island Sanctuary | Island resources and workshop | « Reading island », « Reading islandAgenda » | N-07, N-10 |
| Fishing | A few catches | fishing reading | C30 |
| S-Rank | Be in its zone | S-Rank conditions | N-12 |
| PvP or ranked content, at the very end | Enter: Tensho must switch itself off | competitive cut-off | #39 |
| Nearby monsters (0.795–0.800) | Near monsters from your lists, from the bestiary, from a FATE; as a Blue Mage after « Read the missing spells » | map and icons | X-35 to X-41 |
| Zone not fully discovered (a recent zone, or another character) | Open the game's map and the Tensho map side by side | the same blank parts; otherwise a capture of both | P11 |
| Fighting monsters (developer mode) | Defeat a few monsters, pick up their loot; as a Blue Mage learn a spell; as a Beastmaster capture a beast | « Reading messages » | X-43 |
| Cook quest « Le pari » (developer mode) | Open the Tensho map on the Tillac then on the Entrepont | « Reading eventMarkers » | X-57 |
| An item linked in the chat (loot, a player's message) | Right click on its name: is there a Tensho entry? | « Context menu opened » | X-03 |
| Materia extraction window (gear at 100% spiritbond) | Open it | the log, read by the agent | T0-15 |
| Treasure map | Open the chest | the chest's loot | T0-18 |
| After a fight (developer mode) | Nothing | « Item gain not attributed » when a loot is not matched | T0-21 |
<!-- /gen:plan:reminders-en -->

## Kept for later

Nothing for now. The "Île de Lunule", kept here on 2026-11-06, moved to phase 5 on 2026-11-09: nothing is planned after 1.0.0 (user's request).

<!-- gen:plan:later-en -->
<!-- /gen:plan:later-en -->

## Dropped at review

Do not redo without a new request:

<!-- gen:plan:dropped-en -->
| Point | Reason given |
|---|---|
| P21 shortcuts, P23 friends, P24 teleport by name, P25 minimap | Refused by the user |
| P26 blinking, P27 icon tint | Not kept after explanation |
| C23 hotbars | Refused |
| I14 undo a sort | « Aucun intérêt » ("No interest") |
| I-15 search in the inventory windows | « Pas l'intérêt » ("Not interesting") |
| N-13 visible furniture, X-14 layout | Not kept after explanation |
| Recent average price before pasting | Not ticked, therefore rejected |
<!-- /gen:plan:dropped-en -->

---

# Plan global

Plan du 2026-11-05, à la version 0.539.0, issu de la revue faite avec l'utilisateur et réorganisé à sa demande ; origine de chaque point : [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md) (instantané figé de ce jour). **En cours depuis le 2026-11-05 (feu vert de l'utilisateur).** L'ordre des phases et des points est une proposition de l'agent.

**Depuis [DEC-039](DECISIONS.md) : pas de soumission au dépôt officiel, dépôt personnalisé seulement, règles Dalamud gardées.** **La 1.0.0 vient après tout le plan** (décision de l'utilisateur, [DEC-037](DECISIONS.md)) : il veut un plugin complet, et accepte qu'une soumission jugée trop grosse puisse être refusée ([PUBLISHING](PUBLISHING.md), point 6).

**Tout ce qui demande de jouer vient en dernier, un point à la fois** (demande de l'utilisateur, 2026-11-05). Les phases 1 à 4 se font sans lui : `Verify.ps1` vert, commit et poussée, sans essai en jeu. Une compilation complète recharge la DLL si le jeu tourne : il n'a rien à faire pour autant.

<!-- gen:plan:phases -->
| Phase | Contenu | Points | Il faut jouer ? | État |
|---|---|---:|---|---|
| 1 | Corrections sans le jeu | 3 | Non | Fait |
| 2 | Petites propositions | 43 | Non | Fait |
| 3 | Propositions moyennes | 19 | Non | Fait |
| 4 | Idées, sonde web et sources sans le jeu | 6 | Non | Fait |
| 5 | Relevés en jeu, puis le code qui en dépend | 29 | Oui, l'un après l'autre | En cours : il reste les relevés en jeu |
| 6 | Tests en jeu de tout ce qui précède | 1 | Oui, l'un après l'autre | À faire : tous les essais en une seule campagne, le plus tard possible |
| 7 | Juste avant la 1.0.0 : listes d'outils, documents, aide |  | Non | Faite en grande partie avant la phase 6, à la demande de l'utilisateur ; dernière relecture juste avant la 1.0.0 |
| Ensuite | Version 1.0.0, sur le dépôt personnalisé (pas de soumission, DEC-039) |  |  | Quand l'utilisateur le décide |

Total avant la 1.0.0 (phases 1 à 6) : 101 points, compte produit par script depuis la source du plan.
<!-- /gen:plan:phases -->

**Rien n'est prévu après la 1.0.0 (demande de l'utilisateur, 2026-11-09)** : C10, seul point qui y était, est passé en phase 5. Le portage à l'API 16 de Dalamud n'est pas un point du plan : il se fait dès que Dalamud la publie, quelle que soit la version de Tensho (docs/evidence/0.1009-dalamud-api16-watch.md). Le bouton pour afficher ou cacher des icônes sur la carte existe déjà (rapport de l'utilisateur) : la demande est retirée.

## Déroulement en jeu (phases 5 et 6)

1. L'agent donne **un seul point** : quoi faire dans le jeu, en quelques lignes.
2. L'utilisateur le fait quand il veut, puis répond ; l'agent lit le journal.
3. S'il y a du code à écrire ou une erreur, l'agent corrige, puis on refait ce même point.
4. Seulement alors, le point suivant. Une session peut s'arrêter entre deux points.

**Ordre (demande de l'utilisateur, 2026-11-06) :** 1. ce qui se fait librement ; 2. le contenu classé, ce qui dépend d'un timer et les instances particulières : tournois de Triple Triade, Lord of Verminion, Mahjong, Dédale, donjon profond, récolte du jardin ; 3. **le JcJ tout à la fin** : match de l'arène contre un joueur, Crystalline Conflict, Frontline.

## Phase 1 : corrections sans le jeu

<!-- gen:plan:phase-1 -->
| Où | Ce qui manque | Issue |
|---|---|---|
| Jardin | Ajouter les Bleuets à la liste de graines | Fait en 0.540 |
| Objets | Blocs d'objets sans prix | Fait en 0.541 |
| Escouade | Bouton d'alarme pour une mission en cours déjà présent : cloche de la ligne dans Rappels, et alarme automatique depuis 0.455… | Fait |
<!-- /gen:plan:phase-1 -->

Si l'une d'elles demande finalement un relevé en jeu, elle passe en phase 5.

## Phase 2 : petites propositions

<!-- gen:plan:phase-2 -->
| N° | Proposition | Taille | Précision |
|---|---|---|---|
| P1 | Zoom à l'ouverture, réglable et gardé entre les sessions |  | Fait en 0.542 |
| P2 | Taille des étiquettes réglée à part de celle des icônes |  | Fait en 0.543 |
| P3 | Éthérite la plus proche du drapeau ou d'un point, avec son coût |  | Fait en 0.544 |
| P5 | Molette : zoom depuis toute la fenêtre et pas réglable |  | Fait en 0.545 |
| P9 | ALEA : bonus d'expérience et couleur selon le temps restant |  | Fait en 0.548 |
| P15 | État des lectures du jeu affiché sur la carte (R-14, partie carte) |  | Fait en 0.569 |
| P16 | Option : clic gauche sur la carte pour poser le drapeau |  | Fait en 0.546 |
| P20 | Bouton « Couches » pour tout afficher ou masquer en un clic |  | Fait en 0.547 |
| I0 | Compter les pièces portées dans les stocks et dans « possédé » |  | Fait en 0.549 |
| I5 | Coût de préparation au prix du centre de données (option de A-06) |  | Fait en 0.574 |
| I8 | X-07, partie anticipée : ouvrir l'objet sur Universalis, Garland Tools, Teamcraft ou le wiki |  | Fait en 0.561 |
| I9 | Copier le nom de l'objet et lien d'objet dans le chat (local) |  | Fait en 0.561 |
| I15 | Partager un modèle de tri en texte (copier / coller) |  | Fait en 0.566 |
| I16 | Commandes « /tensho objet » et « /tensho prix » |  | Fait en 0.555 |
| I18 | I-07, partie anticipée : copier « prix le plus bas − 1 » depuis la page Marché |  | Fait en 0.567 |
| I19 | Nom anglais de l'objet dans la fiche et l'infobulle |  | Fait en 0.562 |
| I20 | Aperçu du tri : cases déplacées signalées |  | Fait en 0.565 |
| ~~I22~~ fait en 0.669 sans mesure en jeu | Calculs faits à chaque image (reportés à e) : coût de préparation et historique du marché |  | Fait en 0.669 |
| C0 | A-01, partie anticipée : choisir la qualité visée dans le panneau du carnet déjà présent |  | Fait |
| C2 | Essayer ses séquences sauvegardées sur toutes les recettes |  | Fait en 0.552 |
| C3 | Simuler un autre métier sans le porter : derniers attributs relevés par métier et attributs prévus de la page Équipement |  | Fait en 0.554 |
| C4 | Seuils d'attributs d'une séquence : Habileté, Contrôle et PS minimum |  | Fait en 0.553 |
| C9 | R-02, partie anticipée : « possible maintenant ? » dans l'aide à la pêche |  | Fait en 0.568 |
| ~~C12~~ fait en 0.669 | Qualité initiale calculée à partir des ingrédients HQ choisis |  | Fait en 0.669 |
| C14 | A-12, partie anticipée : fiche de recette complète (livre de maître, synthèse rapide, HQ, prérequis, écart chiffré) |  | Fait en 0.600 |
| C15 | Simulateur plus direct : choix de la recette dans la page, résultat recalculé tout seul, glisser-déposer |  | Fait en 0.572 |
| C17 | Options d'export des macros : attentes séparées (X-12), son, écho final, icône |  | Fait en 0.573 |
| C19 | Où sont mes matériaux : détail par rangement dans la préparation et la chaîne |  | Fait en 0.559 |
| C24 | Besoin restant de la préparation dans les aides de récolte et de pêche |  | Fait en 0.560 |
| C31 | Météo de plusieurs zones en un tableau (zones favorites) |  | Fait en 0.563 |
| C32 | Commandes « /tensho recolte » et « /tensho recette » (suite de I16) |  | Fait en 0.555 |
| C35 | Aide à la pêche : lectures du jeu et premières tables hors de Draw |  | Fait en 0.604 |
| C36 | R-14, partie artisanat et récolte : une aide illisible le dit au lieu de disparaître |  | Fait en 0.601 |
| T3 | Quêtes : colonnes Niveau et Zone, tri et filtre de niveau |  | Fait en 0.550 |
| T4 | Monstres : drapeau sur la position du monstre, pas au centre de la carte |  | Fait en 0.551 |
| T16 | Tâches de servants : une ligne par servant, avec alarme |  | Fait en 0.570 |
| T17 | Rappels cochés d'eux-mêmes quand le jeu le dit |  | Fait en 0.558 |
| T18 | Discord : heure lue dans le fuseau de chaque membre, et balises [zone], [objet], [et] |  | Fait en 0.564 |
| T19 | Alarmes : modifier le nom et l'heure, et « Me rappeler dans 5 min » |  | Fait en 0.557 |
| T20 | Alarmes : sonner aussi à l'heure exacte |  | Fait en 0.556 |
| T23 | Compagnie libre : vaisseaux groupés par compagnie, ancien relevé effaçable, valeur du dernier butin |  | Fait en 0.603 |
| T28 | Collections : « Obtenus récemment » et menu des sources pour les emotes |  | Fait en 0.602 |
| T29 | Mini Cactpot : table des gains prouvée et visible |  | Fait en 0.571 |
<!-- /gen:plan:phase-2 -->

C12 passe en phase 5 : sa règle de calcul doit être comparée à la qualité initiale lue en jeu.

## Phase 3 : propositions moyennes

<!-- gen:plan:phase-3 -->
| N° | Proposition | Taille | Précision |
|---|---|---|---|
| P10 | Recherche sur la carte (lieux, éthérites, PNJ, quêtes, ressources) |  | Fait en 0.597 |
| P12 | Noms écrits à côté des icônes, comme la carte du jeu |  | Fait en 0.596 |
| I1 | Signaler et effacer le relevé d'un servant renvoyé ou d'un ancien rangement |  | Fait en 0.576 |
| I3 | Prix des marchands PNJ dans le coût de préparation et sur la page Marché |  | Fait en 0.575 |
| I11 | Vue en liste triable de la Banque |  | Fait en 0.594 |
| I21 | Filtre de job et de set d'équipement dans les règles de tri |  | Fait en 0.595 |
| C5 | Suivre sa séquence pendant la synthèse : action suivante et écart avec le plan |  | Fait en 0.589 |
| C7 | R-13, partie anticipée : points à horaire ouverts maintenant ou bientôt |  | Fait en 0.588 |
| C16 | Bibliothèque des séquences : recherche, renommer, dupliquer, copier et coller en texte (A-08, partie texte) |  | Fait en 0.590 |
| C18 | Chaîne de fabrication : boutons par étape, étapes identiques regroupées, cases « fait » |  | Fait en 0.591 |
| C21 | « Que puis-je fabriquer ? » avec mes stocks |  | Fait en 0.592 |
| T0 | Alarmes en heure réelle : à la main, chaque jour, chaque semaine ou à chaque remise à zéro (X-10, partie anticipée) |  | Fait en 0.577 |
| T10 | Accueil : bloc « Aujourd'hui » et tuiles au choix |  | Fait en 0.599 |
| T11 | Quêtes « disponibles maintenant » et quêtes de la zone actuelle |  | Fait en 0.582 |
| T12 | Chemin vers une quête : prérequis restants, nombre et prochaine à faire |  | Fait en 0.583 |
| T14 | Équipement : préparer le plan d'un autre job que celui porté |  | Fait en 0.580 |
| T15 | Revue de mode : tenue de la semaine choisie, essayée en un clic, avec l'endroit de chaque pièce |  | Fait en 0.581 |
| T24 | Discord : un salon par catégorie (plusieurs webhooks) |  | Fait en 0.598 |
| T25 | Équipement : plusieurs plans nommés par job |  | Fait en 0.593 |
<!-- /gen:plan:phase-3 -->

## Phase 4 : idées, sonde web et sources sans le jeu

<!-- gen:plan:phase-4 -->
| Point | Note |
|---|---|
| I4, prix Universalis par lots | Fait en 0.608 |
| N-12, conditions des rangs S (fait en 0.670) |
| Alerte du plafond hebdomadaire des mémoquartz | Fait en 0.606 |
| « Où est cet objet ? » sur tous les personnages | Fait en 0.609 |
| Liste des objets annotés, filtre par étiquette | Fait en 0.607 |
| Écrire le fichier d'un personnage seulement s'il change | Fait en 0.605 |
<!-- /gen:plan:phase-4 -->

## Avant la phase 5 : coupure en JcJ et en contenu classé

Tensho se coupe tout seul et repart tout seul, sans réglage (DEC-038). ~~À coder~~ fait en 0.610 : [fonctionnement et écarts connus](ARCHITECTURE.md), D1 à D5 dans DEC-038. Essai en jeu : premier point de la phase 5.

## Phase 5 : relevés en jeu, puis code

Une donnée n'est pas prouvée : le relevé précède le code. Ordre : par activité, pour ne pas changer sans cesse d'endroit dans le jeu.

### Corrections

<!-- gen:plan:phase-5-corrections -->
| Où | Ce qu'il faut relever | Issue |
|---|---|---|
| Jardin | Un entretien et une récolte, pour reconnaître leurs messages ; puis retirer les lignes de journal de capture | #36 |
| Récolte | La régénération de PR pendant une récolte relevée le 2026-11-06 : aucune hausse dans la fenêtre, +8 toutes les 3 s dehors, rien à coder… | Fait |
<!-- /gen:plan:phase-5-corrections -->

### Récolte et pêche

<!-- gen:plan:phase-5-recolte-et-peche -->
| N° | Proposition | Taille | Précision |
|---|---|---|---|
| C33 | Une seule séance de relevés en jeu pour l'artisanat et la récolte, avant de coder | S-M |  |
| ~~C27~~ fait en 0.647 | Classe du point (normal, éphémère, légendaire…) et livre de folklore requis |  | Fait en 0.647 |
| ~~C28~~ fait en 0.663 | Durée de l'intuition et compte à rebours |  | Fait en 0.663 |
| ~~C29~~ fait en 0.649 | R-15, partie anticipée : poissons au harpon dans la page Récolte |  | Fait en 0.649 |
| C30 | Relevé local de vos prises et temps de touche personnels | M |  |
| ~~C10~~ fait en 0.1011 | Chrono de touche relancé tout seul à chaque lancer (aide à la pêche et pêche en mer) ; le relevé est celui de la 0.887 (état « lancer » de… |  | Fait en 0.1011 |
<!-- /gen:plan:phase-5-recolte-et-peche -->

### Artisanat

<!-- gen:plan:phase-5-artisanat -->
| N° | Proposition | Taille | Précision |
|---|---|---|---|
| ~~C6~~ fait en 0.666 pour la simulation | Recettes à niveau ajusté, à qualité imposée et Exploration cosmique |  | Fait en 0.666 |
| ~~C13~~ fait en 0.668 | Chance HQ d'une qualité finale |  | Fait en 0.668 |
| ~~C22~~ fait en 0.664 | Projets de l'atelier de compagnie libre : matériaux par pièce et par phase |  | Fait en 0.664 |
<!-- /gen:plan:phase-5-artisanat -->

### Carte

<!-- gen:plan:phase-5-carte -->
| N° | Proposition | Taille | Précision |
|---|---|---|---|
| ~~P6~~ fait en 0.644 | Coordonnées du personnage toujours visibles et distances |  | Fait en 0.644 |
| ~~P7~~ fait en 0.645 | Éthérites non harmonisées signalées et coût au survol |  | Fait en 0.645 |
| P11 | Parties non découvertes masquées comme sur la carte du jeu |  | Fait |
| ~~P17~~ fait en 0.652 | Relevé du type de marqueur des mandats et de leurs zones |  | Fait en 0.652 |
| ~~P22~~ fait en 0.665 pour les variantes 1 et 5 | Noms des variantes d'icônes de quête (71141 / 71151) |  | Fait en 0.665 |
<!-- /gen:plan:phase-5-carte -->

### Objets

<!-- gen:plan:phase-5-objets -->
| N° | Proposition | Taille | Précision |
|---|---|---|---|
| I10 | Cristaux et gils des servants dans le relevé du servant |  | Fait |
<!-- /gen:plan:phase-5-objets -->

### Outils

<!-- gen:plan:phase-5-outils -->
| N° | Proposition | Taille | Précision |
|---|---|---|---|
| T5 | Une séance de relevés en jeu pour ces outils, avant de coder | S |  |
| ~~T13~~ fait en 0.648 pour les contenus que les données nomment | Contenus et quêtes : « Débloqué par » et « Débloque » |  | Fait en 0.648 |
| ~~T21~~ fait en 0.646 | Personnage : niveaux et expérience de tous les jobs |  | Fait en 0.646 |
| T26 | Dirigeables : trajet du dernier voyage reconstitué | S | 2026-11-07 : l'aéronef de l'utilisateur est en construction, à relever à son premier retour. 2026-11-08 18:50 : premier retour relevé (« Reading airship » : butin par escale, 23 lignes du journal de bord avec les noms des secteurs, heure de retour ; `docs/evidence/0.919-airship-reading-2026-11-08.log`) : lecteur du trajet fait en 0.919 (« Dernier voyage » sur l'onglet des navires, compagnie à un seul aéronef) ; à vérifier en jeu au prochain retour |
| ~~T27~~ fait en 0.651 | Actions de compagnie actives : heure de fin et alarme |  | Fait en 0.651 |
<!-- /gen:plan:phase-5-outils -->

### Gros chantiers et idée

<!-- gen:plan:phase-5-gros-chantiers-et-idee -->
| Point | D'abord | Précision |
|---|---|---|
| Gils des servants dans la vue des personnages |  | Fait |
| I-05, suite des ventes des servants |  | Fait |
| N-04, loterie immobilière | Sonde : structure du jeu à trouver |  |
| N-07, ressources de l'île | Vérifier l'unité des positions | Carte faite en 0.667, liste des matériaux en 0.673 ; vos stocks de l'île ne sont pas lus |
| N-10, atelier de l'île |  | Fait |
| ~~Panneau du Dédale~~ abandonné (DEC-038), repris en exception limitée par DEC-047 (X-109) ; suite du dresseur | La page reste une liste d'informations ; le panneau d'équipe devant le plateau du Dédale est X-109 (DEC-047) |  |
| Île de Lunule (Croissant occulte) | Relevés en jeu au Croissant occulte (mode développeur, « Reading fields »), puis les feuilles MKD | Dans Tensho : les archives occultes dans les Collections (`CollectionKind.OccultLore`, feuille `MKDLore`) et un libellé pour la carte, rien d'autre. Données du jeu à étudier : feuilles `MKDSupportJob`, `MKDGrowData`, `MKDGrowDataSJob`, `MKDBNpcData`, `MKDChain`, `MKDData`, `MKDTrait`, structures `OccultCrescentMKDData`, `AgentMKDInfo`, `AgentMKDSupportJob`. Occult Crescent Helper et Phantom Job Switcher sont sous AGPL : lecture seule, jamais copiés. Passée en phase 5 le 2026-11-09 (demande de l'utilisateur : avant la 1.0.0). |
<!-- /gen:plan:phase-5-gros-chantiers-et-idee -->

## Phase 6 : tests en jeu

<!-- gen:plan:phase-6 -->
| N° | Proposition | Taille | Précision |
|---|---|---|---|
| P19 | Tests en jeu de la carte encore en attente | S | 0.1010 : parcours de la campagne, les 782 essais ouverts rangés par activité, étape 0 d'après-patch en tête : [TESTING-ROUTE](TESTING-ROUTE.md) |
<!-- /gen:plan:phase-6 -->

P19 s'élargit en campagne guidée de fin : tout ce qui a été codé, avec les cases ouvertes de [TESTING](TESTING.md), selon le déroulement ci-dessus.

## Phase 7 : juste avant la 1.0.0

Demande de l'utilisateur (2026-11-05) : tout remettre à jour une fois le plan codé, dans un ordre logique, pour ne pas tout refaire. Chaque étape s'appuie sur la précédente :

1. Revoir tous les crédits : ils dépendent de ce qui a été ajouté au code.
2. La liste de tous les outils, sur l'accueil et partout où elle apparaît, en anglais et en français.
3. L'aide et la prise en main du plugin : elles décrivent les outils.
4. Tous les fichiers `.md` du dépôt, à jour puis en français et en anglais : l'anglais toujours en premier, avec une mention en français disant que la traduction est en bas.
5. Sur Git et GitHub, deux langues, l'anglais d'abord puis le français (décision de l'utilisateur du 2026-11-09, qui remplace « tout en anglais si possible ») : corps des commits, commentaires des issues, notes de release, annonces ; titres courts, étiquettes et jalons en anglais seulement. Un seul format, `tools/bilingual.py`, que suivent `scripts/Release.ps1` (corps des commits) et `tools/discoveries-merge.py notice` ; les notes de release suivent avec la chaîne de release. Les issues ouvertes reçoivent le français quand elles sont touchées.

**Revue du plan (2026-11-08, consigne de nuit X-74), pour coller au code de la 0.865 :**
- La phase 7 passe avant la phase 6 (essais en jeu), à la demande de l'utilisateur ; ce qui changera aux essais retouchera seulement les textes concernés.
- 1. Crédits : `Credits/CreditCatalog.cs` contre les sources réellement appelées (dossiers `Community/`, `Market/`, `Monsters/`, `Fashion/`, `Maps/`, fichiers embarqués) ; la base de découvertes (X-43, X-72 à X-74) n'est pas une source extérieure, elle se décrit dans l'aide, pas dans les crédits. **0.866 : chaque hôte appelé est crédité ; 14 références lues pour des idées (citées dans le code) manquaient : ajoutées, vérifiées à la source (docs/evidence/0.866-credit-references.md).**
- 2. Liste des outils : l'accueil (`MainWindow.Pages.cs`), le README et partout où une liste apparaît, avec les outils ajoutés depuis la 0.539 (Découvertes, panneaux des listes, alarmes des listes, etc.), en anglais et en français (fichiers de langue).
- 3. Aide et prise en main : `Guide/GuideFlow.cs` et `MainWindow.Guide.cs`, à relire contre la liste de l'étape 2. **0.866 : l'aide avait une entrée par page ; textes dépassés corrigés (Découvertes, rangements, artisanat, compagnie, carte, île, livraisons, rappels), une question « Que garde Tensho sur mon PC ? », deux arrêts de plus au guide (Découvertes, pêche océanique).**
- 4. Fichiers `.md` suivis par Git (30 hors preuves) : anglais d'abord, puis la mention en français et la traduction en bas. Exceptions gardées : `CHANGELOG.md` (chaque entrée a déjà sa partie anglaise), `docs/evidence/` (preuves techniques, en anglais, citées par le code), les fichiers de l'agent hors Git (`docs/ai/`, `docs/goals/`, `CLAUDE.md`). **0.866 : README (toutes les pages, les deux moitiés) et les 14 fichiers publics faits ; ARCHITECTURE et ROADMAP allégés de leurs notes d'archive (restent dans Git) ; les documents vivants attendent la décision ci-dessous.**
- 5. Git : messages de commit déjà en anglais (partie « English » du CHANGELOG) ; reste à vérifier issues, jalons, étiquettes et description du dépôt. **2026-11-08 : tickets déjà en anglais ; #31 (soumission au dépôt officiel) fermé « non prévu » (DEC-039) ; jalon renommé « 1.0.0 — Custom repository release » ; #42 ouvert pour les découvertes (X-73, X-74). La description du dépôt (« TenshoAssistant ») est un réglage GitHub : à changer seulement avec l'accord de l'utilisateur.** **2026-11-08 (accord de l'utilisateur) : description du dépôt rédigée en anglais, adresse du dépôt Dalamud en page d'accueil, sujets ajoutés, protection des secrets à l'envoi activée, branches fusionnées supprimées ; #39 et #40 rangés au jalon 2 avec « in-game-test ».**
- **Précision de l'utilisateur (2026-11-08) :** les documents `.md` ne sont pas seulement traduits : chacun est revérifié contre le code actuel, juste après le nettoyage X-76.
- **Décision de l'utilisateur (2026-11-08, « Oui, à la fin ») :** les documents de travail vivants de `docs/project` (GLOBAL-PLAN, TESTING, BACKLOG, USER-REQUESTS, DECISIONS, GAPS, REFERENCES, les *-PROPOSALS, PROPOSALS-TRIAGE, COMPETITIVE-OFF-PLAN) changent à chaque version ; les traduire maintenant obligerait à tenir deux copies. Défaut recommandé : les traduire en dernier, juste avant la 1.0.0, une fois la phase 6 finie ; les fichiers publics sont faits maintenant. **Choisi : traduits juste avant la 1.0.0, une seule fois.**
- **Décision de l'utilisateur (2026-11-10 vers 0 h 10, « Les 17 ») :** elle remplace celle du 2026-11-08 (R01) : les 17 documents de travail de `docs/project` encore en français seul se traduisent maintenant, avant la campagne en jeu, en plusieurs lots du plus petit au plus gros ; les parties produites par un outil se traduisent dans l'outil. Un document traduit se change ensuite dans ses deux moitiés (contrôle d'accord dans check-docs). **0.1023 : lot 1, 9 documents (CONTENT-TOOLS, USER-REQUESTS, G-PROPOSALS, COMPETITIVE-OFF-PLAN, BACKLOG, ITEMS-PROPOSALS, MAP-PROPOSALS, TOOLS-PROPOSALS, PROPOSALS-TRIAGE).**
- **Décision de l'utilisateur (2026-11-10 vers 1 h 20, « Les laisser en français ») :** `TESTING.md` et `TESTING-ROUTE.md` restent en français seul. Tous deux sont écrits et lus par l'outil des essais (une case par essai, numéros de ligne cités par la campagne en jeu) ; une seconde moitié devrait suivre chaque essai. Ce sont les deux exceptions du contrôle bilingue, chacune avec sa raison. **0.1024 à 0.1026 : lots 2 à 4 (SETTINGS, CRAFT-GATHER-PROPOSALS, GAPS, DECISIONS ; GLOBAL-PLAN ; REFERENCES). 0.1027 : point 4 fini pour ce qui se traduit, 33 documents bilingues sur 35, les 2 autres gardés en français par cette décision.**

## Nettoyage complet avant la 1.0.0 (X-76, plan détaillé du 2026-11-08) — approuvé par l'utilisateur (« Oui, ce plan »)

Demande de l'utilisateur : « un nettoyage qui te semblerait plus complet et cohérent avant une version 1.0.0 ». Ordre prévu d'abord : après les essais en jeu nécessaires,
avant les documents `.md` (revérifiés contre le code puis traduits) ; fait en réalité avant la phase 6, comme la phase 7. Ultracode accordé. Règle de toute l'étape : **rien n'est retiré sans preuve qu'il est inutile**,
une famille de changements par version, `Verify` vert à chaque fois, sauvegarde zip dans `checkpoints/` avant de commencer.

**Avancement :** étape 0 faite (0.904 : `docs/project/audits/x76-0-baseline.md`, sauvegarde `checkpoints/backup-20261108-1450-x76-start.zip`) ; étape 1, première passe faite (0.904 : `docs/evidence/0.904-dead-code.md`, 91 éléments retirés) ; restent dans l'étape 1 les parties de la capture déjà prouvées (après les relevés de voyage et d'extraction). Étape 2 : 37 constats (`docs/project/audits/x76-2-duplicates.md`), traités par familles ; 0.905 : coordonnées de carte ; 0.906 : heures d'Eorzéa et durées ; 0.907 : fenêtres du jeu et requêtes web ; 0.908 : découvertes (« vu en jeu ») ; 0.909 : français retiré du code, commentaires remis en place. Titre français « Tensho Assistant · Monnaies » de la barre d'infos du serveur : renommé « Tensho Assistant (Currencies) » en 0.910 avec l'accord de l'utilisateur (« effacer les réglages n'est pas grave, il n'y a que moi qui utilise le plugin »).

**0. Point de départ mesuré.** Chiffres relevés et gardés dans `docs/evidence/` pour comparer à la fin : fichiers et lignes par dossier, tests, clés de langue
(en, fr, de, ja), réglages, hôtes réseau appelés, parties de la capture, types natifs lus, avertissements désactivés.

**1. Code mort et restes.**
- Compilation d'essai avec les analyseurs « membre jamais utilisé » activés (sans les garder) : types, méthodes, champs, propriétés jamais lus.
- Clés de langue jamais demandées par le code (un script compare les fichiers de langue au code ; les clés construites par nom sont listées à part).
- Ressources embarquées, fichiers de données, outils et scripts que plus rien n'utilise.
- Parties de la capture du mode développeur déjà prouvées (leves, retainer, synthesis…) et journaux de débogage d'enquêtes closes.
- Réglages toujours vrais ou jamais lus ; anciennes branches de migration de configuration trop vieilles (la lecture des anciennes valeurs reste).
- Contre-vérification par un second agent qui cherche à prouver que chaque élément sert encore (réflexion, nom construit, test, sauvegarde, commande).

**2. Doublons et cohérence du code.**
- Logiques écrites plusieurs fois (formatage des lieux et heures, caches « par révision », lectures de fenêtres du jeu, clients HTTP, menus de lieu) : une seule version partagée quand le risque est faible.
- Noms et termes alignés (« découverte », « vu par vous », « importé », « source communautaire ») dans le code et les textes.
- Commentaires dépassés corrigés ; aucun français dans le code (règle des langues).
- Fichiers trop longs (Plugin.cs, gros fichiers MainWindow) : découpés seulement là où le découpage est mécanique et couvert par les tests.

**2b. Harmonisation du code** (ajout de l'utilisateur, 2026-11-08 : « que tout soit pareil où ça a besoin de l'être, par exemple info-bulles ou autre »).
- Une seule façon de faire chaque geste d'interface : info-bulles (aide « ? », survol, réglages), cases à cocher et réglages avec défaut, en-têtes et
  sections, tableaux, boutons, couleurs et icônes du thème, messages d'erreur et d'état, journaux (niveaux, anglais, forme des messages).
- Repérage par agents (ultracode), puis remplacement des écritures à la main par les composants communs, famille par famille, textes inchangés.

**2c. Couverture des parseurs** (ajout de l'utilisateur, 2026-11-08 : « vérifier les parseurs, si on n'oublie pas de données, qu'on couvre tous nos besoins
et plus, en prévision de futurs outils »).
- Pour chaque parseur et lecteur du jeu : ce qu'il lit, ce qu'il pourrait lire de plus au même moment (champs ignorés), ce qui manque pour remplacer une
  source communautaire encore lue, et ce que les outils prévus du plan auront besoin de garder dès maintenant.
- Les ajouts sûrs sont codés ; ceux qui demandent un relevé en jeu vont dans la table des rappels.

**Ordre fixé par l'utilisateur (2026-11-08, nuit) :** « fais X-93 en premier et le nettoyage après, on reprendra le reste après » : X-93 en entier (aucune découverte oubliée, découvertes partagées à la demande), puis X-76 étapes 3 à 9, puis le reste (X-85 V2…, X-86 à X-97).

**Ordre rappelé par l'utilisateur (2026-11-08, soir) :** « uniformisation en premier, comme ça on l'applique à tout nouveau parseur » : 2b H7–H10 puis 2c P4–P10 et E2–E5 passent avant la suite de X-85 (V2 points à V6), X-86 à X-89 et tout nouveau parseur.

**Avancement 2b et 2c :** plan rendu par un workflow (`docs/project/audits/x76-2bc-plan.md`) : harmonisation H1 info-bulles (0.913), H2 couleurs du thème (thèmes clairs, 0.912),
H3 enregistrement des réglages et retour arrière, H4 curseurs enregistrés au relâchement (0.914), H5 composants de réglage, H6 ligne « Par défaut » (0.915), H7 états des pages (0.920),
H8 affichage des objets (0.921), H9 journaux (0.922), H10 fichiers et abonnements (0.923–0.925 ; DEC-038 : lecture des offres du marché coupée en JcJ en 0.926, accord de l'utilisateur) ; parseurs P1 HQ des remises, P2 attribution des gains, P3 libellé de chaque sorte (0.911),
P4 état « débloqué » des objets (0.927), P5 feuilles du jeu à la place de données communautaires (0.928–0.932), P6 Intuition et prédateurs (0.933, sauf le leurre nageur : relevé en jeu), P7 secteurs des sous-marins (0.934 : HQ, visites ; sens des lignes Survey/Yield/Discovered à relever en jeu),
P8 compteurs pour des taux (0.935), P9 données lues mais jetées, P10 récompenses de fin de mission ; étapes ajoutées E1 format du fichier des découvertes figé (0.916, DEC-042)
avant la 1.0.0, E2 test des règles d'interface (couvert par les tests 912 à 922), E3 test des preuves citées (0.937), E4 aide « une fois par événement » (0.937), E5 tenue face à une grosse masse de données (X-93 fait, 0.938–0.942 : essai de charge, recherches indexées, vos découvertes jamais refusées, partagées à part par sorte et compressées, lues à la demande ; docs/evidence/0.938-discoveries-load.md ; limite : 200 000 partagées par sorte, choix de l'utilisateur, 0.943). Vérification des données déjà relevées
(demande de l'utilisateur) : sonde `tools/DiscoveryCheckProbe`, 1281 faits, 11 faux venant de 3 défauts corrigés en 0.911
(`docs/evidence/0.911-discoveries-check.md`). Décision pour le lot de fin : la lecture des offres du marché pendant une mise en vente
(UndercutWatcher) ne s'arrête pas en JcJ (DEC-038), résumé à faire à l'utilisateur avant d'y toucher.

**3. Réglages : inventaire complet et remise en ordre.** — 0.944 : inventaire écrit (`docs/project/SETTINGS.md`, 301 lignes, workflow de 4 agents), 9 anomalies confirmées corrigées ; 0.945 : réglage unique « données du jeu et observées seulement », vider le cache (nouveau) et les découvertes avec une copie zip avant (dossier Backups, 5 gardées) ; 0.948 (signalement de l'utilisateur, « 3 fois le même réglage ») : fenêtre de comparaison de l'artisanat et ses deux parties regroupées (parties en retrait et grisées quand la fenêtre est coupée) ; l'inventaire avait regardé chaque réglage seul, pas leurs liens : passe des groupes faite (agent, 26 groupes, checkpoints/x76-settings-groups.json) : lot A en 0.952 (pages de réglages), lot B en 0.953 (carte), recoupements 13, 24, 25 en 0.954 ; reste la réorganisation éventuelle des pages (G-PROPOSALS, à revoir) ; reste la réorganisation éventuelle des pages (décision groupée).
- Tableau de tous les réglages (`docs/project/SETTINGS.md`) : nom, valeur par défaut, où il est lu, page, info-bulle, effet réel, ce qui se passe s'il est coupé.
- Corrections : réglage sans effet, effet différent de son nom, défaut incohérent, doublon entre pages.
- Réorganisation des pages de réglages par thème si l'inventaire le justifie (décision groupée soumise à l'utilisateur, ancienne valeur toujours lue).
- Réglages manquants là où un comportement est imposé sans choix.
- Demandes déjà notées : un réglage unique « données du jeu et observées seulement » (coupe d'un coup toutes les sources communautaires) ; vider le cache et les découvertes avec une sauvegarde zip juste avant.

**4. Info-bulles et textes.** — 0.947 : la ligne « Par défaut » des 137 réglages oui/non vérifiée contre la vraie valeur, en anglais et en français (toutes justes, test DefaultLines947Tests) ; info-bulles des réglages relues par l'inventaire de l'étape 3 (4 corrigées en 0.944) ; anglais et français : 6 900 clés chacun, aucune manquante ; allemand et japonais : 35 clés, le reste en anglais (signalé, pas traduit) ; reste : l'aide et le guide relus contre les pages finales, avec les documents (X-91).
- Chaque info-bulle relue contre le code, celles des réglages d'abord, avec une ligne « Par défaut : … » juste.
- Parité anglais et français ; allemand et japonais : signaler seulement ce qui manque.
- Textes de l'aide et du guide alignés sur l'état final des pages.

**5. Réseau, vie privée et règles Dalamud.** — 0.949 : audit (`docs/project/audits/0.949-network-rules.md`) ; choix de l'utilisateur appliqués (interrupteur PaissaDB couvert par « données du jeu seulement », échange GitHub crédité, dates partagées arrondies au jour, avertissement avant connexion, manifeste) ; non retenus pour l'instant : arrêt en JcJ des travaux déjà lancés et alerte Discord « mission trouvée », position exacte partagée.
- Liste de chaque hôte appelé : quand (clic, automatique choisi par l'utilisateur), limite de taille, délai, en-tête, coupure en JcJ, réglage qui le coupe.
- Contrôle de la liste des règles Dalamud (`docs/project/PUBLISHING.md`) point par point ; tout écart résumé à l'utilisateur avant de corriger.
- Données gardées sur le disque : liste, taille maximale, purge, ce que « vider » efface.

**6. Robustesse et performance.** — 0.950–0.951 : vendeurs de la carte, mesures fines ; audit des calculs par image (agent, checkpoints/x76-draw-cost.json, 9 constats) : lot 1 en 0.955 (fiche vendeur, ensembles d'équipement, Banque, hauts faits par nom) ; lot 2 en 0.956 (monstres proches, menus de la carte, guide, collections) ; à suivre : relire les « Slow frame » mesurés finement (accueil, survol) ;
- Chaque lecture native : thread du framework, pointeur vérifié, copie, coupure en JcJ, résistance aux mises à jour du jeu (R-14).
- Chaque abonnement, minuterie et tâche : propriétaire et nettoyage (test existant étendu si besoin).
- Draw : boucles et allocations par image, caches recalculés trop souvent ; le relevé « Slow frame » sert de juge.

**7. Tests.**
- Tests qui figent un ancien texte exact au lieu d'un comportement : réécrits sur le comportement.
- Tests manquants pour ce que le nettoyage touche (réglages, migrations, nouveau réglage global).
- Tests morts ou en double retirés.

**8. Dépôt.**
- `.gitignore`, scripts, `tools/README.md`, flux CodeQL et compilation, alertes ouvertes, étiquettes, jalons et tickets fermés sur preuve.
- `community-discoveries` et `community-help` contrôlés (règle de validation des découvertes).

**9. Fin de l'étape.** — 0.957 : comparaison écrite (`docs/project/audits/x76-9-comparison.md`) ; étapes 7 et 8 faites (état des tests, dépôt propre) ; reste le passage en jeu guidé avec l'utilisateur et les propositions à revoir.
- Comparaison avec le point de départ (preuve dans `docs/evidence/`).
- Un passage en jeu rapide de chaque page (liste guidée, un point à la fois), avant les documents puis la 1.0.0.

## Sujets ajoutés le 2026-11-06

Choisis par l'utilisateur à partir de la liste de rubriques d'un site français. Aucune donnée de ce site : voir [REFERENCES](REFERENCES.md). Tout vient des feuilles et des structures du jeu.

<!-- gen:plan:w -->
| Point | Sujet | État |
|---|---|---|
| W-01 | Page Commandes spéciales : clients, rang, points, semaine | Fait en 0.675 |
| W-02 | Grande Mascarade : épreuves faites et restantes, sans conseil de combat | Fait en 0.683 |
| W-03 | Armes reliques : progression par extension | Fait |
| W-04 | Animaux de la Félicité insulaire | Fait en 0.685 |
| W-05 | Panneaux à l'écran : matériaux du projet d'atelier et liste de préparation, réglables | Fait en 0.691 |
| W-06 | « Que puis-je fabriquer ? » : trier par le plus rentable en gils, assignats ou autre | Fait en 0.707 |
| W-07 | « Que puis-je fabriquer ? » : compter les sous-recettes | Fait en 0.696 |
| W-08 | Mandats : clic droit vers l'éthérite la plus proche | Fait en 0.698 |
| W-09 | Liste de suivi par provenance (récolte, PNJ, monstre, autre), atelier et artisanat | Fait en 0.699 |
| W-10 | Peuples alliés : rang et quêtes de chaque peuple | Fait en 0.713 |
| W-11 | Grande compagnie : rang, sceaux et missions sur une page | Fait en 0.714 |
| W-12 | Livraisons spéciales : Cristarium, Académie de Sharlayan, Bazar aux Mille Perles | Fait en 0.715 |
| W-13 | Reliques suivantes : Anima, Eurêka, Résistance, Manderville, fantômes, et leurs armures | Fait |
| W-14 | Eureka et Bozja : ALEA, escarmouches, affrontements cruciaux, rapports du front, actions oubliées, sans conseil de combat | Fait en 0.717 |
| W-15 | Donjons profonds : Palais des morts, Pilier des cieux, Eureka Orthos, Sanctuaire des pèlerins, étages et butin, sans conseil de combat | Fait en 0.718 |
| W-16 | Lexique des mots du jeu, français et anglais, à partir de ses feuilles | Fait en 0.719 |
| W-14b | ALEA de toutes les zones ouvertes, lieu et niveau, d'après les fichiers de zone | Fait en 0.721 |
<!-- /gen:plan:w -->

## Demandes du 2026-11-07, en jouant

Notées dans l'ordre où l'utilisateur les a faites ; à faire après la tâche en cours, une version à la fois, sans en oublier. Une ligne passe à « fait » avec sa version.

<!-- gen:plan:x -->
| Point | Demande | État |
|---|---|---|
| X-01 | Lourdeur : trouver ce qui bloque une image de Tensho (120 à 230 ms toutes les quelques minutes, 2,6 s une fois) et le sortir du dessin | En cours : relevé « Slow frame » en 0.745 ; aide à la récolte (97 ms) calculée hors de l'image en 0.747, avec la mesure de la fenêtre principale et des autres fenêtres … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-01) |
| X-02 | Données gardées plus longtemps, relues seulement si absentes ; réglages qui complètent « Cache & péremption » | Durées par défaut plus longues et « Jamais » renommé « Seulement si absent, ou sur Actualiser » en 0.750. Accord donné … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-02) |
| X-03 | Lien « Ouvrir dans Tensho » d'une alarme : n'ouvre rien | Aucun clic n'arrivait (journal de 0.743). Dalamud ne trouve plus le lien après un rechargement du plugin : réenregistré au chargement en 0.759 … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-03) |
| X-04 | Au point de récolte, mettre en évidence les objets dont les listes de suivi (artisanat et atelier de compagnie) ont besoin | Fait en 0.751 |
| X-05 | Listes de suivi d'artisanat et d'atelier rassemblées par zone de récolte | Fait en 0.752 |
| X-06 | « Récolter automatiquement » coché dans la fenêtre du jeu : le plan ne garde que les actions faites avant la première récolte, puis plus… | Fait en 0.753 |
| X-07 | « Que puis-je fabriquer ? » : un objet faisable par deux métiers compté une fois | Fait en 0.739 |
| X-08 | Infobulle d'un point à horaire (carte) : « fermé maintenant » sans dire dans combien de temps il ouvre ; ajouter le compte à rebours | Fait en 0.754 |
| X-09 | Listes de suivi d'artisanat et d'atelier : un bouton qui ajoute les alarmes de tous leurs points à horaire, sans doubler une alarme qui… | Fait en 0.755 |
| X-10 | Aide à la récolte : se placer d'abord sur un objet dont les listes de suivi ont besoin | Fait en 0.756 |
| X-11 | Listes de suivi : pouvoir replier chaque zone de récolte | Fait en 0.757 |
| X-12 | Listes de suivi par zone : la zone où l'on se trouve en premier, puis les autres triées en tenant compte du coût de téléportation vers… | Fait en 0.757 |
| X-13 | Listes de suivi : les objets des points à horaire (alarmes) dans leur propre groupe repliable, avec un nom approprié, au lieu du nom de zone | Fait en 0.758 |
| X-14 | Groupe « Points à horaire » des listes de suivi : dire pour chaque objet si son point est ouvert ou fermé | Fait en 0.761 |
| X-15 | Menu du clic droit d'un objet : « Y aller » directement (point de récolte, éthérite la plus proche) | Fait en 0.762 |
| X-16 | Plan de PR : à rendement égal, préférer les actions qui durent toute la récolte, pour moins de clics | Fait en 0.763 |
| X-17 | Panneau de suivi réduit : l'aide perd « Vos listes en demandent encore » et le cadre ; les cadres passent au-dessus des autres fenêtres | Fait en 0.764 |
| X-18 | Journal des gils : icônes (gils, catégories) et moins de « Autre » : trouver d'autres catégories d'après le jeu | Icônes et couleurs en 0.768 ; « Autre » : relevé au journal (« Gil other ») ; 0.775 : dépense après un écran de chargement = téléportation ; « +1 000 » sans fenêtre encore inconnu |
| X-19 | Journal des gils : un dépôt chez un servant compté comme dépense ; le total des servants ne suit qu'en rouvrant | Fait en 0.769 |
| X-20 | Objets déposés chez un servant, dans les sacoches ou au coffre de compagnie : la copie doit suivre pendant que la fenêtre est ouverte, sans… | Fait en 0.770 |
| X-21 | « Ajouter les alarmes » proposait 19 alarmes, dont des objets déjà complets en apparence et des cristaux qu'on trouve aussi sans horaire | Fait en 0.772 |
| X-22 | Butin des monstres, lignes du wiki « Monstre · niveau · Zone (x,y) » : pas de clic droit vers l'éthérite la plus proche | Fait en 0.777 |
| X-23 | Alarmes : l'envoi sur Discord désactivé par défaut | Fait en 0.778 |
| X-24 | Après un chargement du plugin, les objets de monstres passent un moment en « Autres » : la source des monstres n'est relue du disque que… | Fait en 0.779 |
| X-25 | **Gel du jeu, 15:18:45** : le solveur Raphael (natif) panique sur une division par zéro (quality_upper_bound_solver/state.rs:59) en ouvrant… | Fait en 0.782 |
| X-26 | « Comparer les prochains coups » sur une recette en haute qualité impossible : « valeurs non concordantes », aucun coup projeté (le jeu… | Fait en 0.784 |
| X-27 | Les listes de suivi doivent pouvoir n'afficher que les objets finis (les objets à fabriquer, sans leurs matériaux) ; « oublie pas atelier… | Fait en 0.785 |
| X-28 | Atelier de compagnie : tenir compte de ce qui est déjà livré au projet ; « oublie pas de verifie si on a valide des etapes aussi » | Fait en 0.787 |
| X-29 | Carnet de synthèse : en Menuisier sur une recette de Métallurgie (Lingot de mithril), Tensho ne signale pas qu'on n'est pas le bon métier… | Fait en 0.788 |
| X-30 | Voir les étapes de fabrication qui restent (objets intermédiaires à fabriquer) pour les listes d'atelier et d'artisanat | Fait en 0.789 |
| X-31 | « Fabrications restantes » : chaque étape doit pouvoir se déplier jusqu'à l'étape finale ; un seul format de chiffres, comme les autres… | Fait en 0.790 |
| X-32 | Carte : trouver une solution au blocage contourné en ouvrant puis fermant la carte du jeu (zones de mandats et de quêtes absentes tant que… | Fait en 0.793 |
| X-33 | Rangements, « Que faire avec ? » : régler les prix comme « Que puis-je fabriquer » et tenir compte des ventes par jour (Matière sombre… | Fait en 0.794 |
| X-34 | Monstres proches (table des objets du jeu), information seulement : règle Dalamud à résumer et accord de l'utilisateur avant tout code | Fait en 0.795 |
| X-35 | Monstres proches : ceux qui lâchent un objet demandé par les listes de suivi (catégorie « Monstre »), sur la carte | Fait en 0.795 |
| X-36 | Monstres proches : ceux qui manquent au carnet de chasse | Fait en 0.796 |
| X-37 | Monstres proches : cibles des mandats acceptés et des ALÉA en cours | Fait en 0.797 |
| X-38 | Mage bleu : monstres proches qui apprennent une compétence pas encore apprise, icône mage bleu au-dessus de leur tête ; réglage pour… | Fait en 0.798 |
| X-39 | Dresseur : monstres proches du bestiaire du dresseur, même affichage et mêmes réglages | Fait en 0.804 |
| X-40 | Monstres proches : autres usages à proposer, et des réglages cohérents à l'endroit approprié (carte, page concernée) pour chaque usage | Fait en 0.1002 |
| X-41 | Monstres proches : ceux qui lâchent un objet de collection qui manque (monture, mascotte, orchestrion), si les données de butin le disent | Fait en 0.800 |
| X-42 | Avant la phase 7 : prendre les réglages de l'utilisateur et en faire les valeurs par défaut | Abandonné le 2026-11-07 à la demande de l'utilisateur (« garde valeur par défaut cohérente ») : les valeurs par défaut restent celles du code, rien n'a été publié |
| X-43 | Base locale de découvertes : relever ce qui est à portée (monstres et leurs lieux, butin, sorts bleus appris, captures du dresseur, points de récolte, pêche…) pour compléter les outils ; options de débogage mais actif hors débogage ; import (fusion avec la nôtre) et export, pour ne plus dépendre des données communautaires à force de jouer | Cadre accepté le 2026-11-07 (« Oui, ce cadre ») : fichier local, provenance par entrée, aucun autre joueur, rien d'envoyé, pas d'aide au combat, coupé en JcJ, import … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-43) |
| X-44 | Carnet d'artisanat : pas de menu Tensho au clic droit, ni sur une recette de la liste ni sur un matériau | Fait en 0.808 |
| X-45 | Carte : des marchands d'événement sont affichés alors qu'ils ne sont pas là en jeu (Limsa, la Dunette) ; à corriger juste avant la phase 7 | Fait en 0.810 |
| X-46 | Carte : le zoom ne reste pas toujours celui qu'on a réglé ; à faire juste avant la phase 7 | Fait en 0.850 |
| X-47 | Récompenses de quête (fenêtre de la quête) : ni panneau Tensho sous l'info-bulle de l'objet, ni entrée Tensho ; à faire juste avant la… | Fait en 0.852 |
| X-48 | Carte : le clic droit sur la cible d'une quête (« Cible indiquée par le jeu (journal) ») n'offre pas l'éthérite la plus proche ; à faire… | Fait en 0.853 |
| X-49 | Carte : la cible de quête ne disparaît pas après validation de la quête (Ishgard, les Contreforts, capture du 2026-11-07) | Fait en 0.813 |
| X-50 | Carte : les icônes de bord de carte doivent passer au-dessus de tout (cachées sous le bandeau des noms, capture d'Abalathia) | Fait en 0.814 |
| X-51 | Panneau des collectionnables : surligner sur les barres de raccourcis les compétences que le plan demande, comme l'aide à la récolte… | Fait en 0.815 |
| X-52 | Base de découvertes : les prix vus au tableau des ventes remplacent Universalis (plus fiables), après avoir vérifié qu'on a autant… | Fait en 0.817 |
| X-53 | Monstres proches : le yak laineux et le loaghtan (lait de yak, viande de loaghtan de la liste de suivi) n'ont pas d'icône sur la carte ni au-dessus d'eux (captures, Coerthas occidental, Avant-pays dravanien) | En cours : les données MobDrop relient bien 12888 aux yaks laineux 3479 et 11214 ; 0.818 note la chaîne au journal en mode développeur … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-53) |
| X-54 | À l'ouverture d'un autre rangement que l'inventaire (servant, sacoche du chocobo, coffre de compagnie, autres) : surligner les objets qui y… | Fait en 0.820 |
| X-55 | Listes de suivi d'artisanat et d'atelier : préciser quand l'objet n'est pas sur soi (chez un servant, dans la sacoche, le coffre de… | Fait en 0.822 |
| X-56 | Listes de suivi d'artisanat et d'atelier : à la place de « téléportation 219 gils », l'icône de la compétence Téléportation puis l'icône… | Fait en 0.824 |
| X-57 | Carte, quête de cuisinier à Limsa : la cible (au bon endroit) et l'icône de quête ne sont pas au même endroit, la quête affichée tombe hors de la zone, et elle n'apparaît pas sur la carte du Tillac (captures du 2026-11-07, la carte du jeu montre le lien vers le Tillac) | À faire, après X-56 ; en cours : le jeu donne deux marqueurs pour la quête 2046 « Le pari » sur la carte 11 (icônes 71141 et 71143, objectifs 2046 et 520), sa liste … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-57) |
| X-58 | Menu Tensho du clic droit sur un objet : ajouter sa recette à une liste d'artisanat déjà créée | Fait en 0.826 |
| X-59 | Listes de suivi, points à horaire : retirer « ouvert » / « fermé », garder le minuteur seul, en vert quand le point est ouvert, en rouge… | Fait en 0.827 |
| X-60 | Listes de suivi, clic droit sur un objet : « Placer le drapeau » absent quand on est sur la carte du lieu, et « Y aller » absent pour… | Fait en 0.830 |
| X-61 | Aide à la récolte : pas de plan proposé, « objet lu après une dépense de PR sur ce point » alors que les essais sont à 4/4 (PR 291/823… | Fait en 0.831 |
| X-62 | Alarmes ajoutées depuis une liste de suivi : elles sonnent encore quand la ressource est déjà réunie (Sel gemme d'Abalathia 24/2, capture… | Fait en 0.832 |
| X-63 | « Y aller » vers une zone sans éthérite (Arrière-pays dravanien) : téléporter à une éthérite d'une zone voisine qui donne sur la zone… | Fait en 0.833 |
| X-64 | Listes de suivi, clic droit : ni « Y aller » ni « Placer le drapeau » pour Haricots émeraude (Fauche, L'Écume des cieux de Dravania… | Fait en 0.834 |
| X-65 | Liste de suivi d'artisanat (Marrons glacés) : le sirop de bouleau, ingrédient intermédiaire détenu (157/1), n'apparaît pas dans le panneau… | Fait en 0.838 |
| X-66 | Encadré des rangements (X-54) : chez le servant, il encadre le vin de cuisine au lieu du vin de cuisine de qualité (même icône), et… | Fait en 0.839 |
| X-67 | Carte de Tensho : signaler qu'une cible est plus haut ou plus bas, comme la carte du jeu (flèche haut ou bas sur l'icône) (capture de Mor… | Fait en 0.842 |
| X-68 | Carte de Tensho : la zone de la quête de cuisinier (Mor Dhona) « ne se fait pas zoomer dessus » (capture du 2026-11-08) | Précisé : « depuis le journal ». 0.845 : ouverte depuis le journal, une quête qui a une zone est zoomée pour la montrer entière (60 % de la vue) … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-68) |
| X-69 | Carte de Tensho : « la map ne se rafraîchit pas assez » : la quête est passée à « Parler à Raulf », mais la « Cible indiquée par le jeu… | Fait en 0.847 |
| X-70 | Carte de Tensho : la zone de la nouvelle étape d'une quête manque tant que la carte du jeu n'a pas été fermée puis rouverte (Kugane, «… | Fait en 0.848 |
| X-71 | Carte de Tensho : reprendre les icônes plus haut / plus bas de la carte du jeu (vues à Mor Dhona) partout, à la place de la flèche de la… | Fait en 0.849 |
| X-72 | Base de découvertes (parseurs) : vérifier qu'elle est efficace, l'utilisateur a déjà un peu joué (2026-11-08) | Fait en 0.856 |
| X-73 | 0.1003 (revérification du 2026-11-09 : base de l'utilisateur et sources communautaires) : butin des aéronefs (nouvelle sorte AirshipDrop, secteur tiré du journal du voyage, garde : un seul aéronef et autant d'étapes que de secteurs) ; journal de développement des gains non attribués (aucun butin de monstre noté : à expliquer au prochain combat). Restent sans parseur : butin des cartes au trésor (relevé en jeu d'abord), jardin (X-108), PaissaDB (crochet, règle Dalamud). Parseurs : découvrir en jeu tout ce que Tensho prend aux sources communautaires (LuminaSupplemental, Garland Tools, wiki, GatherBuddy pêche, Teamcraft temps de touche, revue de mode) et compléter Universalis (prix des autres mondes du centre de données quand on s'y rend) ; à terme ne plus dépendre que d'Universalis, si c'est possible ; ultracode autorisé si besoin (2026-11-08) | Cartographie faite (docs/project/audits/0.857-community-map.md) ; étapes X-73.1 à X-73.12 ci-dessous, dans cet ordre |
| X-73.1 | Marché : prix vus sur chaque monde visité du centre de données, préférés à Universalis quand plus récents ; âge montré ; purge après 90… | Fait en 0.857 |
| X-73.2 | Marché : historiques de ventes fusionnés d'une ouverture à l'autre (vitesse de vente calculée sur place) | Fait en 0.858 |
| X-73.3 | Pêche : drapeaux du carnet du jeu (« sans condition d'heure ni de météo ») à la place de la donnée communautaire | Fait en 0.859 |
| X-73.4 | Revue de mode : thème et catégories lus dans le jeu (FashionCheckManager) au lieu des suppositions | Fait en 0.861 |
| X-73.5 | Désynthèse et réduction : objets obtenus de quel objet (AgentSalvage, AgentPurify) | Fait en 0.860 |
| X-73.6 | Tâches des servants : récompenses vues (AgentRetainerTask) | Fait en 0.860 |
| X-73.7 | Coffres des missions : objets vus par mission (Loot.Items) | Fait en 0.860 |
| X-73.8 | Voyages : butin par secteur (déjà lu dans l'atelier) | Fait en 0.862 |
| X-73.9 | Mage bleu et dresseur : sort appris et bête apprivoisée, avec le monstre | Fait en 0.863 |
| X-73.10 | Pêche : prises (appât, mooch, lieu), temps de touche, conditions vues ; sondes en jeu d'abord | Fait en 0.971 |
| X-73.11 | Butin des monstres : objets reçus après la mort de sa cible (le plus risqué : attribution) | Fait en 0.860 |
| X-73.12 | Le reste selon la carte : coffres d'objets, ALÉA récompenses, panoramas, animaux de l'île, fêtes vues | Fait en 0.864 |
| X-73.13 | Demande du 2026-11-08 (« oui tous en faisant tests en jeu nécessaires ») : sondes en mode développeur pour la pêche (lancer, touche, type… | Fait en 0.869 |
| X-73.14 | Offres vues marquées « à moi / pas à moi » (numéro de servant comparé aux vôtres, aucun nom gardé) : comparatif des servants et « copier le… | Fait en 0.870 |
| X-73.15 | Table centre de données → région depuis les feuilles du jeu (WorldDCGroupType) au lieu d'Universalis | Fait en 0.871 |
| X-73.16 | Lieu des marchands absents des feuilles : noté à l'ouverture de leur boutique (cible, zone, position) | Fait en 0.872 |
| X-73.17 | Butin des coffres des donjons sans fond, d'Eurêka et de Bozja | Fait en 0.873 |
| X-73.18 | Après les relevés de X-73.13 : temps et type de touche, mooch, Intuition et prédateurs ; réduction éthérée ; objets de quête | Fait en 0.887 |
| X-74 | Consigne de nuit (2026-11-08, l'utilisateur dort) : ajouter le plus de parseurs cohérents possible (« mieux vaut prendre plus et retirer… | Fait |
| X-75 | Demande du 2026-11-08 : un test pour chaque sonde ; revérifier qu'aucune sonde ne manque pour découvrir en jeu les données prises ailleurs… | Fait |
| X-75.1 | Un test pour chaque sonde : liste des parties de la capture figée par un test, test de texte pour chaque sonde et parseur qui n'en a pas… | Fait en 0.874 |
| X-75.2 | « Vu en jeu » d'abord partout où la donnée apprise existe déjà : page Monstres, monstres proches (butin, bêtes), menu clic droit… | Fait en 0.875 |
| X-75.3 | Pêche : prise au mooch notée avec le poisson moochés (et non l'appât), leurre, météo précédente, « vu en … » dans le panneau de pêche | Fait en 0.877 |
| X-75.4 | Mage bleu : monstres vus lançant un sort (barre d'incantation), niveaux des sorts tirés de ce qui a été vu | Fait en 0.878 |
| X-75.5 | Autres pages : fêtes « vues du … au … », lieux des ALÉA vus sur la page des rangs S, météos des panoramas vues, meilleur prix de la région… | Fait en 0.879 |
| X-75.6 | Calculs du jeu à la place des formules communautaires : durées des voyages de sous-marins (fonctions du jeu), contrôle de l'horaire de la… | Fait en 0.880 |
| X-75.7 | Nouvelles sondes en mode développeur, puis parseurs après les relevés : animaux de l'île, pêche au harpon, secteurs et destinations des… | Fait en 0.881 |
| X-75.8 | Hors parseur : PaissaDB (lire la pancarte demande un crochet : règle Dalamud, accord de l'utilisateur d'abord), ferrage utilisé (crochet), boutique Mog Station, dates futures, mondes jamais visités : restent aux sources communautaires | Noté |
| X-77 | Demande du 2026-11-08 : si possible, un système qui passe par Git pour envoyer les données découvertes par les parseurs et récupérer celles… | Fait en 0.882 |
| X-77.1 | Retour de l'utilisateur (2026-11-08) : « le système actuel pour import export avec git est pas très pratique » ; choix de l'utilisateur… | Fait en 0.883 |
| X-78 | Demande du 2026-11-08 (capture de la carte de Tensho, La Mer de Rubis) : garder les icônes de gauche (poisson du point de harpon, avec sa… | Fait en 0.890 |
| X-79 | Demande du 2026-11-08 (essai du harpon) : la liste du panneau de harpon doit rester tout le temps et mettre en évidence les poissons de la… | Fait en 0.891 |
| X-80 | Demande du 2026-11-08 (capture du filtre de la carte) : « Points de récolte » séparé en mineur, botaniste et pêcheur | Fait en 0.895 |
| X-81 | Essai en jeu du sertissage forcé (X-75.7) | Fait en 0.899 |
| X-82 | Essai en jeu de la revue de mode (X-75.7) et retour de l'utilisateur : menu du clic droit grisé sur l'icône d'une pièce de la liste | Fait en 0.900 |
| X-83 | Retour de l'utilisateur (2026-11-08, sur l'île à côté d'un monstre : aucune icône sur la carte) et relevé des animaux de l'île (X-75.7) | Fait en 0.901 |
| X-84 | Essai en jeu du courrier (X-75.7) : une lettre avec un objet et 1 gil | Fait en 0.903 |
| X-85 | Document « a faire » de l'utilisateur (2026-11-08), pêche en mer (priorités données par l'utilisateur) : le panneau de pêche en mer doit ressembler à l'aide à la pêche (icônes des poissons, temps de touche) ; l'aide à la pêche ordinaire coupée pendant la pêche en mer ; poissons du courant spectral et poissons qui rapportent des assignats orange mis en évidence ; **prio 1** : meilleur appât pour déclencher le courant spectral ; **prio 2** : pêche au vif et objectifs en cours ; défaut vu au journal : l'horaire calculé donne la route 12 ou 15 quand le jeu dit 14 | V1 faite (0.917) : horaire vérifié par port par vos voyages, aide à la pêche ordinaire coupée en mer … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-85) |
| X-86 | 0.966 : case « quêtes pas encore disponibles » (clic droit et réglages). 0.967 : flèche de hauteur sur les zones de quête et d'événement… | Fait en 0.966 |
| X-87 | 0.968 : zone d'un mandat acceptée retirée quand ce mandat est en cours (et marqueurs que le jeu ne dessine pas) ; cibles du mandat avec… | Fait en 0.968 |
| X-88 | 0.969 : 256 catégories par zone, ajout d'un groupe entier de catégories (groupes du jeu). Document « a faire », tri d'inventaire : plus de… | Fait en 0.969 |
| X-89 | 0.970 : extension des quêtes (donnée du jeu) dans leur info-bulle et leur fiche ; extension des objets seulement quand lieux de récolte et… | Fait en 0.970 |
| X-90 | 0.996 : bestiaire des donjons profonds par la même lecture du wiki (page de chaque groupe d'étages : ennemis et notes ; titre du jeu sans «… | Fait en 0.996 |
| X-91 | 0.971 : parseurs revérifiés (`docs/evidence/x91-parsers-check.md`) : un seul point prévu manquait (lieux des ALÉA vus sur la page des rangs… | Fait en 0.971 |
| X-92 | 0.1001 : inventaire des listes figées dans le code : seule celle des territoires du Dédale (coupure) en relevait ; ses plateaux sont… | Fait en 0.1001 |
| X-93 | Demande du 2026-11-08 : « prévisions pour une masse de données conséquente possible » (découvertes partagées par beaucoup de joueurs, longues durées de jeu). Mesure de départ : votre fichier Discoveries.json fait 409 Ko après environ une journée de jeu, MarketSeen.json 60 Ko, le fichier commun 166 Ko pour 664 faits (2026-11-08) ; à 100 joueurs sur quelques mois, le fichier commun peut dépasser des dizaines de Mo. À prévoir : (1) essai de charge avec des données inventées (100 000 puis 1 000 000 de faits) : temps de chargement, mémoire, temps d'enregistrement, temps de la fusion, coût par image des pages qui les lisent ; (2) chargement et enregistrement hors du fil du jeu, jamais tout le fichier à chaque relevé (écriture groupée, fichier temporaire puis remplacement) ; (3) index en mémoire par sorte, objet et zone, pour que les pages et info-bulles ne parcourent jamais tout ; (4) plafond par fait (compteurs bornés, valeurs distinctes déjà limitées à 32 par DEC-042) et fusion des relevés trop fins (cases de carte) ; (5) fichier commun découpé si besoin (par sorte ou par extension) et téléchargé seulement en cas de changement (somme de contrôle), compressé ; (6) outil de fusion : limites par joueur et par ticket (déjà : refus ≤ 5 %, volumes plausibles), temps de fusion mesuré, joueurs fusionnés par lots ; (7) page Découvertes et export : pagination et filtres au lieu de tout afficher ; (8) le format reste celui figé par DEC-042 (version 2) : le découpage et la compression se font autour, sans changer le contenu d'un fait. | Avec 2c du nettoyage X-76 (P8 compteurs, P9 données jetées), avant la 1.0.0. Essai de charge (1) fait en 0.1036 (docs/evidence/x93-load-test.md) ; selon ses chiffres, après la release d'après patch 7.58 : d'abord (2) (copie de la base sur l'image : une image perdue dès 50 000 à 80 000 faits ; un Discoveries.json de plus de 64 Mo, environ 218 000 faits à vous, n'est pas lu puis est écrasé : à empêcher avant), puis (5) avant environ 120 000 faits dans le fichier commun (borne de 32 Mo du plugin ; 100 Mo par fichier sur GitHub vers 384 000), (3) et (7) : totaux de la page et index Seen() hors de l'image à partir de 150 000 à 300 000 faits ; (4) pas nécessaire ; (6) limites en place, lots inutiles (3,6 min à un million) |
| X-94 | 0.960 : cause trouvée et corrigée (le numéro d'un lieu de pêche était cherché dans la table des points de récolte et tombait sur un point… | Fait en 0.960 |
| X-95 | 0.950 : première cause trouvée et corrigée (liste des vendeurs de la carte reconstruite deux fois à chaque changement de zone, fêtes lues… | Fait en 0.950 |
| X-96 | 0.959 : cause trouvée et corrigée (zones à météo individuelle — histoire non faite — exclues de toute prévision… | Fait en 0.959 |
| X-97 | 0.982 : réponse de l'utilisateur (DEC-045) : vu dans l'aide à la pêche ; sous chaque poisson collectionnable, les paliers de collectabilité… | Fait en 0.982 |
| X-98 | V2 points et V6 relevé des assignats (0.964) : points de chaque prise lus dans les données de l'escale et notés, affichés par poisson… | Fait |
| X-99 | Revue du 2026-11-09 (DEC-045), signalement : après une téléportation, les marqueurs de quêtes pas encore acceptées manquaient sur la carte… | Fait en 0.977 |
| X-100 | 0.999 (DEC-048) : Paramètres en sept onglets (Général, Panneaux à côté du jeu, Carte, Suivi et notifications, Données et sources, Avancé… | Fait en 0.999 |
| X-101 | Idée retenue (DEC-045, de C-09) : alerte quand un objet de la liste de souhaits est en vente sous un prix choisi (Universalis) | Fait en 0.1000 |
| X-102 | Idée retenue (DEC-045, de C-15) : progression des succès suivis gardée sur disque, alerte quand l'un est terminé | Fait en 0.987 |
| X-103 | Idée retenue (DEC-045, de C-14) : bouton « Préparer » qui envoie les objets des mandats d'artisanat acceptés à la liste de préparation | Fait en 0.988 |
| X-104 | Idée retenue (DEC-045, de C-05) : pièces manquantes d'une tenue ajoutées d'un clic à une liste | Fait en 0.989 |
| X-105 | Idée retenue (DEC-045, de I-12) : filtre par job dans la liste des objets (format du cache des objets à changer) | Fait en 0.991 |
| X-106 | Idée retenue (DEC-045, de C-01) : état « fait » des contenus gardé par personnage, visible hors connexion | Fait en 0.990 |
| X-107 | Idée retenue (DEC-045, de I-04) : gils gagnés ou dépensés aujourd'hui dans la barre du serveur | Fait en 0.986 |
| X-108 | Demande du 2026-11-09 : parseur du jardinage pour combler les trous par découverte, croisements inclus. Plan : 1) relevé en jeu d'une récolte qui donne des graines (lignes du chat, objets) ; 2) nouvelles sortes de découvertes : pousse observée (plantation → maturité : heures réelles par graine), entretien observé (intervalle), récolte (graine plantée → objets et quantités), croisement (graine obtenue ≠ graine plantée, avec les graines des emplacements voisins notées au journal) ; 3) partage communautaire (outil de fusion à jour) ; 4) le journal et la grille des croisements complétés par ces relevés, la source communautaire (nick75g) gardée en repli et marquée ; graines manquantes (Bleuets) depuis les feuilles | Relevé en jeu d'abord (table des rappels) ; après X-97 |
| X-109 | 0.981 : panneau à côté du plateau du jeu (avant l'entrée, et dans le Dédale seulement plateau ouvert et hors combat : seule fenêtre de… | Fait en 0.981 |
| X-110 | 0.984 : page Dresseur, colonne du lieu : « (n lieux vus) » avec le menu des positions (drapeau, éthérite la plus proche) : captures avec un… | Fait en 0.984 |
| X-111 | 0.983 : le composant commun existe depuis la 0.704 (PlaceMenu : clic droit, drapeau, éthérite la plus proche, carte, météo ; 21 endroits)… | Fait en 0.983 |
| X-112 | Constat de l'audit des tests du 2026-11-09 (n° 0), noté en 0.1019 : les flèches du bord de la carte pour les points de récolte à heure… | Fait en 0.1041 |
| X-76 | Demande du 2026-11-08 : nettoyage complet du code (« ne plus avoir de chose qui serait inutile maintenant », sans rien casser) : code mort… | Fait |
| X-113 | Découvertes partagées : quand un ticket n'apporte aucun fait nouveau (gist inchangé), discoveries-merge.py ne doit ni réécrire… | Fait en 0.1029 |
| X-114 | Demande du 2026-11-10 : carte de Tensho seulement (pas la carte du jeu : sa boussole montre déjà la météo) : une icône de la météo actuelle… | Fait en 0.1049 |
| X-115 | Demande du 2026-11-10 : outil de base cartographique du client, sans Dalamud (Lumina seul, lecture du sqpack), étape A : instantané de la… | Fait en 0.1028 |
| X-116 | Demande du 2026-11-10, suite de X-115 (B et C) : base complète par version (tous les placements des calques .lgb : PNJ, objets, zones des… | Fait en 0.1029 |
| X-117 | Demande du 2026-11-10, suite de X-115 (D) : dans le plugin, au premier chargement après un patch, comparer l'ancien cache au nouveau… | Fait en 0.1048 |
| X-118 | Demande du 2026-11-10, plan 4 choisi par l'utilisateur (application de bureau reliée au plugin, mêmes pages hors du jeu), étape 0 : sonde… | Fait en 0.1030 |
| X-119 | Suite de X-118 (plan 4), étapes 1 à 4 : 1) application jeu fermé, pages de données (objets, recettes, météo, lexique, découvertes, collections depuis les fichiers enregistrés) ; 2) lien local avec le plugin (canal nommé, utilisateur courant seulement, aucun port réseau, désactivé par défaut ; état lu sur le thread du framework, copié, envoyé hors du thread, R07) et pages en direct ; 3) actions sur clic seulement, exécutées par le plugin comme le bouton en jeu (drapeau, demande de téléportation, alarmes, listes) ; 4) réglage par page : dans le jeu, dans l'application ou les deux ; la carte, les panneaux attachés aux fenêtres du jeu, l'aide de récolte, l'artisanat en direct et la Revue de mode restent en jeu. Application distribuée à part (release GitHub), jamais lancée ni contenue par le plugin ; synchro (demande du 2026-11-10, R33) : l'application et le plugin portent le même numéro, se construisent, se testent (toutes les pages dessinées hors du jeu dans Verify) et se publient ensemble ; chaque release porte l'application, Verify -Release refuse sans elle ; l'application vérifie sa compatibilité avec le plugin installé | Étape 1 et synchro faites en 0.1031 (application src/TenshoAssistant.Desktop jeu fermé, autonome, publiée avec le plugin ; Verify la dessine sans fenêtre). Restent après la release d'après patch 7.58 : étapes 2 à 4, et le point d'entrée des polices dans le plugin à la place de la réécriture Mono.Cecil d'une copie (39 appels UiBuilder.IconFont ; police lue dans UiComponents, posée par Plugin.cs depuis UiBuilder et par l'application depuis son atlas, test à cliquet) |
| X-120 | Demande du 2026-11-10, revue le même jour par l'utilisateur : plan 2 seulement des six plans (docs de l'agent, proposition) : un outil de… | Fait en 0.1032 |
| X-121 | Demande du 2026-11-10 : mise à jour automatique de l'application de bureau depuis GitHub, gérée entièrement par l'agent (aucun geste de… | Fait en 0.1033 |
| X-122 | Décisions de l'utilisateur du 2026-11-10 (séance des décisions) à coder dans le plugin : D-102 case « Envoyer aussi sur Discord » pour les… | Fait en 0.1045 |
| X-123 | Décision D-100 de l'utilisateur (2026-11-10) : code de conduite (CODE_OF_CONDUCT.md, deux langues), CODEOWNERS (revue du propriétaire… | Fait en 0.1033 |
| X-124 | Accord de l'utilisateur du 2026-11-10 (« je te donne l'accord pour tout ») pour la suite de X-120 : dépôt GitHub séparé pour… | Fait en 0.1035 |
| X-125 | Accord de l'utilisateur du 2026-11-10 (« oui ») : PluginUiTest essayé sur 4 ou 5 plugins Dalamud open source populaires (compilés depuis… | Fait en 0.1036 |
| X-126 | Demande de l'utilisateur du 2026-11-11 : installer l'application de bureau depuis Tensho, jamais obligatoire, seulement depuis Paramètres, section « Application de bureau » (précision du 2026-11-11), avec les réglages d'interface qui vont avec (état « installée / version / à jour », bouton Installer ou Mettre à jour, dossier d'installation, mise à jour automatique de X-121, lien vers la release ; textes en/fr, FAQ). Téléchargement depuis la release GitHub du dépôt, empreinte SHA-256 vérifiée (X-121), installé à côté, jamais lancé par le plugin sans un clic de l'utilisateur. Décision D-137 (2026-11-11) : exception assumée aux règles des plugins Dalamud pour notre application, Tensho étant sur un dépôt personnalisé ; si Tensho vise un jour le dépôt officiel, pouvoir couper cette installation dans la version soumise. Change la règle de X-119 « jamais lancée ni contenue par le plugin ». | Après X-119 terminé (étapes 2 à 4 : lien local, actions, réglage par page) ; pas avant |
<!-- /gen:plan:x -->

## Rappels de relevés en jeu, par moment

Relevés du document « a faire » (2026-11-08) : mandat de combat OK (X-37) ; loterie immobilière OK (N-04) ; sous-marin fait (fenêtres de résultat capturées, parseur P7 à coder) ;
pêche en mer faite (27 prises notées, « !!! » vu) ; pas encore : quête « vaincre X », récolte du jardin (timer), première remise d'objet de quête.


Consigne de l'utilisateur (2026-11-07) : les relevés se font en jouant ; au moment venu, il demande « rappelle-moi » et l'agent donne les étapes de ce moment-là. Mode développeur activé pour tous (le journal porte les lignes « Reading … »).

Ce tableau est la seule liste des relevés en jeu en attente : le parcours des essais (étape 0) et les rappels y renvoient.

<!-- gen:plan:reminders -->
| Quand | Quoi faire | Ligne lue au journal | Point |
|---|---|---|---|
| Carte Tensho ouverte, en jouant | Rien de plus, laisser tourner quelques minutes | « Slow frame » (10 morceaux) | X-01 |
| Une alarme sonne dans le chat | Clic droit sur la ligne de l'objet | « Context menu opened » | X-03 |
| Mandat de combat | L'accepter, le commencer, aller vers ses cibles | « Reading monsterEvents » | X-37 |
| Quête « vaincre X » | Aller vers les monstres de la quête | « Reading monsterEvents » | X-40 |
| Dresseur | Page Dresseur : « Lire le bestiaire du wiki », puis en dresseur près d'un monstre d'une bête manquante : icône du dresseur au-dessus de lui | rien, à l'œil (capture si faux) | X-39 |
| Un gain de gils sans fenêtre (« +1 000 ») | Noter l'heure | « Gil other » | X-18 |
| Jardin, timer fini | Récolter (0.978 : récolte reconnue sur un emplacement) ; un pot de fleurs aussi | messages du jardin, cible du pot | #36 |
| Jardin, récolte avec des graines (croisement) | Récolter un emplacement dont les voisins ont d'autres graines (mode développeur) | « Garden chat capture » : lignes des objets et des graines obtenus | X-108 |
| L'aéronef revient | Ouvrir le panneau de voyage | relevé du voyage | T26 |
| Atelier de compagnie (mode développeur) | Entrer dans l'atelier, ouvrir la fenêtre des sous-marins | « Submarine voyage check » : fonctions du jeu contre la formule (DEC-043) | X-75.6 |
| Carte d'Eurêka Anemos (X-86) | Ouvrir la carte Tensho avec « masquer le non découvert », faire une capture à côté de la carte du jeu | décalage des parties non découvertes (taille et position du masque) | X-86 |
| Liste des servants ouverte (mode développeur) | Ouvrir la liste des servants | métier, niveau et fin des ventes de chaque servant (champs ClassJob, Level, MarketExpire) | X-76 P9 |
| Fin d'une mission (donjon, défi) | Finir la mission, ouvrir les coffres | récompenses de fin de mission et identifiant des coffres (P10 : DutyCompleted, ChestObjectId) | X-76 P10 |
| Eurêka, Bozja ou Croissant occulte (mode développeur) | Entrer, gagner un peu d'expérience, changer la magia (Eurêka) ou le holster (Bozja) | « Reading fields » : niveaux, expérience, magia | X-90 |
| Pêche avec un leurre nageur | Lancer avec un leurre nageur, pêcher un poisson | indicateur 32 du lancer et objet du leurre | X-76 P6 |
| Loterie immobilière | Ouvrir la fenêtre de la loterie | relevé de la loterie | N-04 |
| Île sanctuaire | Ressources et atelier de l'île | « Reading island », « Reading islandAgenda » | N-07, N-10 |
| Pêche | Quelques prises | relevé de pêche | C30 |
| Rang S | Être dans sa zone | conditions des rangs S | N-12 |
| JcJ ou contenu classé, en tout dernier | Entrer : Tensho doit se couper | coupure compétitive | #39 |
| Monstres proches (0.795–0.800) | Près de monstres de vos listes, du bestiaire, d'un ALÉA ; en mage bleu après « Lire les sorts manquants » | carte et icônes | X-35 à X-41 |
| Zone pas entièrement découverte (une zone récente, ou un autre personnage) | Ouvrir la carte du jeu et la carte Tensho côte à côte | les mêmes parties vierges ; sinon une capture des deux | P11 |
| Combat contre des monstres (mode développeur) | Vaincre quelques monstres, ramasser leur butin ; en mage bleu apprendre un sort ; en dresseur capturer une bête | « Reading messages » | X-43 |
| Quête de cuisinier « Le pari » (mode développeur) | Ouvrir la carte Tensho sur le Tillac puis sur l'Entrepont | « Reading eventMarkers » | X-57 |
| Un objet lié dans le chat (butin, message d'un joueur) | Clic droit sur son nom : y a-t-il une entrée Tensho ? | « Context menu opened » | X-03 |
| Fenêtre d'extraction de matéria (équipement à 100 % de symbiose) | L'ouvrir | le journal, relu par l'agent | T0-15 |
| Carte au trésor | Ouvrir le coffre | butin du coffre | T0-18 |
| Après un combat (mode développeur) | Rien | « Item gain not attributed » si un butin n'est pas rattaché | T0-21 |
<!-- /gen:plan:reminders -->

## Retenu pour plus tard

Rien pour l'instant. L'Île de Lunule, gardée ici le 2026-11-06, est passée en phase 5 le 2026-11-09 : rien n'est prévu après la 1.0.0 (demande de l'utilisateur).

<!-- gen:plan:later -->
<!-- /gen:plan:later -->

## Écartés à la revue

Ne pas refaire sans nouvelle demande :

<!-- gen:plan:dropped -->
| Point | Raison donnée |
|---|---|
| P21 raccourcis, P23 amis, P24 téléportation par nom, P25 mini-carte | Refusés par l'utilisateur |
| P26 clignotement, P27 teinte d'icône | Non retenus après explication |
| C23 barres de raccourcis | Refusé |
| I14 annuler un tri | « Aucun intérêt » |
| I-15 recherche dans les fenêtres d'inventaire | « Pas l'intérêt » |
| N-13 meubles visibles, X-14 aménagement | Non retenus après explication |
| Prix moyen récent avant de coller | Non coché, donc rejeté |
<!-- /gen:plan:dropped -->
