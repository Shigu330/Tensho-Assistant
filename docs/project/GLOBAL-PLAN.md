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
| Garden | One upkeep and one harvest, to recognise their messages; then remove the capture log lines | Done in 0.1110 |
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
| T26 | Airships: route of the last voyage reconstructed |  | Done in 0.919.0 |
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
| Crescent Isle (Occult Crescent) | In-game readings at the Occult Crescent (developer mode, « Reading fields »), then the MKD sheets | In Tensho: the occult lore in Collections (CollectionKind.OccultLore, MKDLore sheet) and a map label. Record of 11/12 around 2:57 pm (user on the island, developer mode, 4 screenshots: knowledge level 3, 465/2800; Phantom Paladin lv 2, 240/2400; silver 270, gold 12; actions Guard, Pray, Occult Heal, Pledge; traits lv 4 and 5): the line « Reading fields: occult knowledge 465/2800, support job 1 levels 1 2 0 1 0 0 1…, silver 270 gold 12 » matches the screenshots exactly (job 1 = Paladin, level 2); copy in relais/lunule-12-11.log. Left: choose with the user what Tensho shows (see the comparison with other plugins), then code it from the MKD sheets (MKDSupportJob, MKDGrowData, MKDTrait, MKDChain…). Occult Crescent Helper and Phantom Job Switcher: AGPL, read only |
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
| X-01 | Sluggishness: find what blocks a Tensho frame (120 to 230 ms every few minutes, 2.6 s once) and move it out of the drawing | Must before the announcement (sorting of 11/13, accepted by the user). In progress: 0.1103, 0.1104, 0.1111 (map compiled early); relay 34: C1.1 heap and contexts measured (0.1184), C2.1 overlays compiled early (0.1185), C3.1-C3.2 map festivals (0.1186). Relay 35: C1.3 root of the leak of unloaded instances found and fixed (0.1187: static hooks of CompetitiveGate and of the weather emptied at unload; 0.1194: the static field MainWindow.RaphaelSequences held the window and the data cache; « Earlier instance » measure from 0.1187 to 0.1192, evidence docs/evidence/0.1187-unload-roots.md), C3.3 wider MapFirst (0.1188: « canvas vendors » 4 ms on the first map against 32 to 40), steady state measured (0.1190: frame 0.46 ms on average, 95th percentile ≤ 1 ms, worst 3 ms, in town, map open), C2.2 monster sheets read ahead (0.1191; C2.3 not needed, measured: Tracker 0.02 ms a frame). Relay 36: C1.3 confirmed in the game, read in dalamud.log (0.1195: the unloaded instances of 0.1194 have nothing held any more, only shared values, empty arrays and static values; 0.1196: the instance of 0.1195 reads « 0 of 1692 parts alive », the heap no longer grows from one reload to the next); the tools of the search taken out in 0.1196 (emptying of old instances, notes three levels deep), one short line per earlier instance kept. Left: read C2.2 in the open air; C5 (isolated spikes: Storage and Settings pages, region map, Gear page). The load context itself stays alive through Dalamud's settings API (Newtonsoft) and the JSON calls without options: to decide (D-144, settings format). Rule: compare loads of neighbouring rank or after a game restart [detail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-01); relay 40: C5, quest index by id in QuestService off the frame (0.1237); C5 left: Gear page; relay 41: C5 Gear page, the plan's model rebuilt only when an input changes (0.1242, GearModel1242Tests); left: read C2.2 outdoors, D-144 Relay 42 (13/11): C2.2 outdoors still not measurable (character in town, territory 1186, all relay long); to read during the test campaign (test T-0.1191-1 of the route, place « Outdoors »). |
| X-02 | Data kept longer, re-read only if absent; settings that complete « Cache & expiry » | Done in 0.750.0 |
| X-03 | « Open in Tensho » link of an alarm: opens nothing | Done in 0.837.0 |
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
| X-18 | Gil log: icons (gil, categories) and less « Other »: find other categories from the game | Wanted before the announcement, rank 3 (sorting of 11/13). Icons and colours in 0.768; « Other »: read in the log (« Gil other »); 0.775: spending after a loading screen = teleport; « +1,000 » without a window still unknown |
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
| X-43 | Local discovery database: record what is within range (monsters and their locations, loot, learned blue spells, beastmaster captures, gathering points, fishing…) to complete the tools; debug options but active outside debugging; import (merge with ours) and export, to stop depending on community data as one plays | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). Frame accepted on 2026-11-07 (« Oui, ce cadre » ("Yes, this frame")): local file, provenance per entry, no other player, nothing sent, no combat help, cut off in PvP, import … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-43) |
| X-44 | Crafting log: no Tensho right-click menu, neither on a recipe of the list nor on a material | Done in 0.808 |
| X-45 | Map: event vendors are shown although they are not there in game (Limsa, the Aftcastle); to fix just before phase 7 | Done in 0.810 |
| X-46 | Map: the zoom does not always stay the one that was set; to do just before phase 7 | Done in 0.850 |
| X-47 | Quest rewards (quest window): neither a Tensho panel under the item tooltip, nor a Tensho entry; to do just before phase 7 | Done in 0.852 |
| X-48 | Map: right click on a quest target (« Target indicated by the game (log) ») does not offer the nearest aetheryte; to do just before phase 7 | Done in 0.853 |
| X-49 | Map: the quest target does not disappear after the quest is completed (Ishgard, the Foothills, capture of 2026-11-07) | Done in 0.813 |
| X-50 | Map: map-edge icons must pass above everything (hidden under the name banner, Abalathia capture) | Done in 0.814 |
| X-51 | Collectables panel: highlight on the hotbars the skills that the plan asks for, like the gathering help (capture: Œil-de-verre) | Done in 0.815 |
| X-52 | Discovery database: prices seen on the sales board replace Universalis (more reliable), after checking that we have as much information | Done in 0.817 |
| X-53 | Nearby monsters: the woolly yak and the loaghtan (yak milk, loaghtan meat from the tracking list) have no icon on the map nor above them… | Done in 0.828.0 |
| X-54 | When opening a storage other than the inventory (retainer, chocobo saddlebag, Free Company chest, others): highlight the items in it that… | Done in 0.820 |
| X-55 | Crafting and workshop tracking lists: specify when the item is not on oneself (with a retainer, in the saddlebag, the Free Company chest…) | Done in 0.822 |
| X-56 | Crafting and workshop tracking lists: in place of « teleport 219 gil », the Teleport skill icon then the gil icon and the amount, without… | Done in 0.824 |
| X-57 | Map, culinarian quest in Limsa: the target (at the right place) and the quest icon are not at the same place, the displayed quest falls outside the zone, and it does not appear on the Upper Decks map (captures of 2026-11-07, the game map shows the link to the Upper Decks) | Must before the announcement (sorting of 11/13, accepted by the user). To do, after X-56; in progress: the game gives two markers for quest 2046 « Le pari » on map 11 (icons 71141 and 71143, objectives 2046 and 520), its list … [details](archive/GLOBAL-PLAN-X-2026-11-09.md#x-57) |
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
| X-68 | Tensho map: the culinarian quest zone (Mor Dhona) « ne se fait pas zoomer dessus » ("cannot be zoomed on") (capture of 2026-11-08) | Done in 0.846.0 |
| X-69 | Tensho map: « la map ne se rafraîchit pas assez » ("the map does not refresh enough"): the quest moved to « Speak to Raulf », but the «… | Done in 0.847 |
| X-70 | Tensho map: the zone of a quest's new step is missing until the game map has been closed then reopened (Kugane, « L'art du sushi »… | Done in 0.848 |
| X-71 | Tensho map: take over the game map's higher / lower icons (views at Mor Dhona) everywhere, in place of the 0.844 arrow, « on en a besoin… | Done in 0.849 |
| X-72 | Discovery database (parsers): check that it is effective, the user has already played a little (2026-11-08) | Done in 0.856 |
| X-73 | 0.1003 (re-check of 2026-11-09: user's database and community sources): airship loot (new kind AirshipDrop, sector taken from the voyage… | Done in 0.887.0 |
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
| X-85 | User's « to do » document (2026-11-08), Ocean Fishing (priorities given by the user): the Ocean Fishing panel must look like the fishing… | Done in 0.964.0 |
| X-86 | 0.966: « quests not yet available » checkbox (right click and settings). 0.967: height arrow on quest and event zones (discreet chevron… | Done in 0.966 |
| X-87 | 0.968: zone of an accepted leve removed when that leve is in progress (and markers the game does not draw); leve targets with the game's… | Done in 0.968 |
| X-88 | 0.969: 256 categories per zone, adding a whole group of categories (game groups). « To do » document, inventory sorting: more than 64… | Done in 0.969 |
| X-89 | 0.970: quest expansion (game data) in their tooltip and sheet; item expansion only when gathering locations and quests that give it agree… | Done in 0.970 |
| X-90 | 0.996: Deep Dungeon bestiary through the same wiki reading (page of each floor group: enemies and notes; game title without « The » tried)… | Done in 0.996 |
| X-91 | 0.971: parsers rechecked (`docs/evidence/x91-parsers-check.md`): only one planned item was missing (locations of FATEs seen on the S-Rank… | Done in 0.971 |
| X-92 | 0.1001: inventory of the lists frozen in the code: only that of the Labyrinth territories (cut-off) was concerned; its boards are now also… | Done in 0.1001 |
| X-93 | Request of 2026-11-08: « prévisions pour une masse de données conséquente possible » ("provisions for a possibly substantial mass of data")… | Done in 0.1040.0 |
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
| X-108 | Request of 2026-11-09: gardening parser to fill the gaps by discovery, crossbreeding included. Plan: 1) in-game reading of a harvest that yields seeds (chat lines, items); 2) new kinds of discoveries: sprout observed (planting → maturity: real hours per seed), tending observed (interval), harvest (seed planted → items and quantities), crossbreeding (seed obtained ≠ seed planted, with the seeds of neighboring slots noted in the log); 3) community sharing (merge tool updated); 4) the log and the crossbreeding grid completed by these readings, the community source (nick75g) kept as fallback and marked; missing seeds (Cornflowers) from the sheets | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). In-game reading first (reminders table); after X-97 |
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
| X-119 | Follow-up of X-118 (plan 4), steps 1 to 4: 1) application with the game closed, data pages (items, recipes, weather, lexicon, discoveries, collections from the saved files); 2) local link with the plugin (named pipe, current user only, no network port, off by default; state read on the framework thread, copied, sent off the thread, R07) and live pages; 3) actions on a click only, carried out by the plugin like the in-game button (flag, teleport request, alarms, lists); 4) a setting per page: in the game, in the application or both; the map, the panels attached to game windows, the gathering helper, live crafting and the Fashion Report stay in the game. Application distributed separately (GitHub release), never launched nor contained by the plugin; sync (request of 2026-11-10): the application and the plugin carry the same number, are built, tested (every page drawn outside the game in Verify) and published together; each release carries the application, Verify -Release refuses without it; the application checks its compatibility with the installed plugin | Wanted before the announcement, rank 2 (sorting of 11/13). Goal: Tensho on a second screen (accepted on 11/11). Done: step 1 (0.1031), link (0.1081), protocol 2 (0.1114), categories (0.1119), mirror copy per category and stores on it (0.1131, first slice with X-151). Left (passage-25.md): other categories (characters, gil journal, achievements, duties, discoveries), rest of X-151 (host still by reflection), step 3 (actions, two-way channel), step 4 (setting per page), font entry point; the app's stores are read-only in effect until step 3; user's choice of 11/12 for step 3: the app may create or change Tensho's data (alarms, lists, tools, settings) only while the link with the game is active; each action of the app is passed to the plugin, which applies and saves it itself (the app never writes itself); nothing is sent to the game; game closed: read only (X-180) |
| X-120 | Request of 2026-11-10, revised the same day by the user: plan 2 only of the six plans: a test tool for plugin creators, in a separate… | Done in 0.1032 |
| X-121 | Request of 2026-11-10: automatic update of the desktop application from GitHub, fully handled by the agent (no step for the user). At start… | Done in 0.1033 |
| X-122 | The user's decisions of 2026-11-10 (decision session) to code in the plugin: D-102 a « Also send to Discord » box for the alarms written in… | Done in 0.1045 |
| X-123 | The user's decision D-100 (2026-11-10): code of conduct (CODE_OF_CONDUCT.md, both languages), CODEOWNERS (owner review requested on other… | Done in 0.1033 |
| X-124 | The user's approval of 2026-11-10 ("I give you the approval for everything") for the rest of X-120: a separate GitHub repository for… | Done in 0.1035 |
| X-125 | The user's approval of 2026-11-10 ("yes"): PluginUiTest tried on 4 or 5 popular open-source Dalamud plugins (built from their sources… | Done in 0.1036 |
| X-126 | The user's request of 2026-11-11: install the desktop application from Tensho, never mandatory, only from Settings, section "Desktop… | Done in 0.1113 |
| X-127 | Request of 2026-11-11: the wishlist as a tool of its own, with its entry in Tensho's menu (out of the Collections tab); an « added to your… | Done in 0.1106 |
| X-128 | Request of 2026-11-11 (screenshot): Tensho map, the « Quests not available yet » box sits in the right-click menu above the tabs instead of… | Done in 0.1107 |
| X-129 | Request of 2026-11-11 (screenshot, Glowing apple seeds): gathering help, a hidden item revealed after a GP spend has « no plan » (« read… | Done in 0.1108 |
| X-130 | Request of 2026-11-11: garden, crossbreed forecasts from the planted garden (Tensho's journal: seed and place of each bed in the 3 × 3… | Done in 0.1109 |
| X-132 | Request of 2026-11-11: garden alarms (ripe, tending, from the Gardening tool's journal) in an « Alarms » tab of Gardening, their count as a… | Done in 0.1160 |
| X-133 | Request of 2026-11-11: reminder alarms in an « Alarms » tab of the Reminders tool, their count as a number beside « Reminders » in the… | Done in 0.1159 |
| X-134 | Request of 2026-11-11 (grade 3 Thanalan topsoil): gathering help GP plan, items gatherable once per node (one catch counted, the next attempts on the most useful item) and items whose yield cannot be raised (no GP spend proposed for the yield); game data first, else your readings | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). Partly in 0.1167 and 0.1175 (developer-mode log of the Gathering window's numbers, up to 200, never its texts). Left: the user's reading on a point with a « RARE » item (trial of 0.1175), then no GP proposed for its quantity |
| X-135 | Request of 2026-11-11 (Beast labyrinth board 1): read the board result (XBMResult: score, path, targets, bonuses; each beast's experience and rank gains; loot XBMContentsBooty, XBMContentsTreasure) and use the beasts' ranks in the Beastmaster page and the help | Wanted before the announcement, rank 7 (sorting of 11/13). Board result done in 0.1163 (score, each beast's rank and experience, XBMResult window, ranks used by the Beastmaster page); left: the loot (XBMContentsBooty, XBMContentsTreasure, recorded on 11/12 in relais/dedale-12-11-matin.log) |
| X-136 | Request of 2026-11-11: more coherent Beast labyrinth help: team chosen from the enemy's weaknesses and stats, one advice per fight of the board if possible, beasts to level for later boards proposed, beast combos highlighted before the fight, and in combat the Instinct II combo skills highlighted for the summoned beast (option, off by default) | Wanted before the announcement, rank 8 (sorting of 11/13). To do; relay 31: the rest needs data not established (Instinct II combos; meaning of XBMContent columns 3 and 4; « rank 1 to 5 » of board 1 while the beasts there are rank 6): record or source to find first (R06, nothing guessed) |
| X-137 | User's choice of 2026-11-11 (« oui mais en pvp et classement »): Tensho is no longer cut in the Beast labyrinth (removed from… | Done in 0.1115 |
| X-138 | Request of 2026-11-11 (screenshot, Central Shroud): Tensho map, right-click menu, Aetherytes tab: some aetherytes show only the icon, no… | Done in 0.1166 |
| X-140 | Audit A01 and A03 (GPT audit of 11/11, checked in the code by the master, order accepted by the user): the app installer never deletes a… | Done in 0.1117 |
| X-141 | Audit A02 (GPT audit of 11/11, checked in the code by the master, order accepted by the user): the bank deletes on disk every character… | Done in 0.1118 |
| X-142 | Audit A04, A05, A09 (GPT audit of 11/11, checked in the code by the master, order accepted by the user): discoveries and seen prices: « to… | Done in 0.1122 |
| X-143 | Audit A06, A07, A08 (GPT audit of 11/11, checked in the code by the master, order accepted by the user): downloads with a deadline covering… | Done in 0.1125 |
| X-144 | Audit A13 (GPT audit of 11/11, checked in the code by the master, order accepted by the user): a release's « Checked at launch » note is… | Done in 0.1126 |
| X-145 | Audit A11 (GPT audit of 11/11, checked in the code by the master, order accepted by the user): GitHub discoveries exchange: sharing state (gist, issue, hash) tied to the signed-in account, cleared on account change; token deletion failure shown; review the public_repo permission (least privilege) | Replaced by X-175 on 12/11 (GitHub App with least permissions, sharing state tied to the player's account) |
| X-146 | Audit A10 (GPT audit of 11/11, checked in the code by the master, order accepted by the user): discoveries merge tool: bytes, decompression… | Done in 0.1214 |
| X-147 | Audit A14 (GPT audit of 11/11, checked in the code by the master, order accepted by the user): disk cache: measure the cost of Trim… | Done in 0.1217 |
| X-148 | Partly done in 0.1215: workflow actions pinned by hash (A16); A15 done in 0.1120 (X-150). Left, low priority: Dalamud's latest.zip checked by hash (goatcorp publishes no hash, only AssemblyVersion: a hash kept here would break the CI at each Dalamud update, so to decide) and CodeQL on the desktop app (net10.0-windows: a Windows job, to try with the CI watched) | Wanted before the announcement, rank 21 (sorting of 11/13). User's decision of 11/13 around 11:10: Dalamud's latest.zip is not pinned (Dalamud publishes no hash: a kept hash would break the CI at each update). Left: CodeQL of the desktop application (a Windows job), to try while following the CI. Before the announcement, low priority. |
| X-139 | Request of 2026-11-11: PluginUiTest (repository Shigu330/PluginUiTest) shows how windows render: a PNG of each drawn window beside the report, and optionally a preview window on screen; new version of the tool | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do, last priority |
| X-149 | GPT review of 11/11, checked in the code by the master, accepted by the user: the local link (protocol 2) sends the names of changed files… | Done in 0.1119 |
| X-150 | GPT review of 11/11, checked in the code by the master, accepted by the user and audit A15: the app reads the plugin's settings again on… | Done in 0.1120 |
| X-151 | GPT review of 11/11, checked in the code by the master, accepted by the user: typed contract between the desktop app and the pages (today… | Done in 0.1238 |
| X-152 | GPT review of 11/11, checked in the code by the master, accepted by the user: state messages made consistent across pages (« waiting », « source off », « old data », « failed »), in French and English, fallback of partial languages checked | Merged into X-154 on 12/11 (visual plan accepted by the user) |
| X-153 | GPT review of 11/11, checked in the code by the master, accepted by the user: wiring and release of Plugin.cs's services extracted by family (desktop or persistence first), measured by the paths that set up or release the same service and errors at close | Wanted before the announcement, rank 1 (sorting of 11/13). Slice 1 done in 0.1225 (desktop family, Plugin.Desktop.cs, DesktopFamily1225Tests); slice 2 done in 0.1239 (persistence: TenshoData, bank and its bridge, Plugin.Persistence.cs, PersistenceFamily1239Tests); slice 3 done in 0.1240 (server bar, Plugin.ServerBar.cs, ServerBarFamily1240Tests); slice 4 done in 0.1249 (panels, Plugin.Panels.cs, PanelFamily1249Tests); slice 5 done in 0.1250 (discoveries: store, GitHub exchange, loading with the prices seen, Plugin.Discoveries.cs, DiscoveriesFamily1250Tests); left: game readers (about 35, interleaved with their services all through the constructor: to cut by domain, gathering, crafting, company, collections…, one slice per domain, same measure; the discoveries' item watcher with them); low priority (before X-139) |
| X-154 | GPT visual plan of 12/11, checked by the master, place accepted by the user: shared visual base: title, selection, button, state and table… | Done in 0.1255 |
| X-155 | GPT visual plan of 12/11, checked by the master, place accepted by the user: pilot on three screens: Home (next deadline first, one-action… | Done in 0.1261 |
| X-156 | GPT visual plan of 12/11, checked by the master, place accepted by the user: same states on Stocks, Market, Crafting, Gathering, Map and… | Done in 0.1267 |
| X-157 | GPT visual plan of 12/11, checked by the master, place accepted by the user: « Follow Dalamud's style » option (colours and spacing of the… | Done in 0.1272 |
| X-158 | User's request of 12/11 (screenshots, The Viaduct 020, Brewers' Beacon 008, The Invisible City 019): sightseeing log, the tooltip said the… | Done in 0.1128 |
| X-159 | User's request of 12/11 (treasure hunt of 12/11 around 0:35, no portal; reading T0-18 done): the coffer's loot (3 poetics, greaves 3432, 4… | Done in 0.1174 |
| X-160 | User's request of 12/11: Tensho map, FATEs have the favourites' icon: use the game's icon of each FATE (by type); allies and enemies told… | Done in 0.1268 |
| X-161 | User's request of 12/11: Tensho map only: sightseeing icons a little smaller by default, monster icons 50 % smaller; existing setting kept… | Done in 0.1262 |
| X-162 | User's request of 12/11 (screenshot): server info bar, the gil tooltip (and sometimes the alarms') opens above while the others open below… | Done in 0.1129 |
| X-163 | User's request of 12/11: alarms (reminders, gardening and others included) also ring in the desktop app: game running, the plugin signals… | Done in 0.1156 |
| X-164 | User's request of 12/11 (screenshot of the map toolbar): the icons are not centred in their buttons; the zoom percentage can be typed; a «… | Done in 0.1130 |
| X-176 | User's request of 12/11: discoveries merge worker on the user's laptop, a separate project outside the Tensho repository (own folder, e.g. C:\Tensho-travailleur, beside the Discord bot in C:\Serveur discord ff14 without changing it): regular reading of « Discoveries » issues through the GitHub API (outgoing only, no open port), X-168 checks (gist of the issue's author, explicit CC0, bounded sizes, else manual triage), dry-run merge with tools/discoveries-merge.py read from the repository, log file, Discord notice (separate webhook, secret); then, with the user's consent: git and gh installed (winget), GitHub token limited to the repository, pull request opened for the user, automatic start of the bot and the worker (scheduled task); laptop survey of 12/11: Windows 11 Pro, Python 3.12.10, sleep off, git and gh missing, bot started by hand without a log | Wanted before the announcement, rank 22 (sorting of 11/13). Phases 1 and 2 done on 12/11 around 4:20, per the laptop session (checked by the master on GitHub: no travailleur/* branch, no worker pull request, CC0 box ticked on #43): merge tool taken from main, git and gh installed, fine-grained token limited to the repository (expires 10/02/2027), Discord webhook tried, GitHub writing on within its limits (branch travailleur/discoveries-<n>, pull request to Devloppement, bilingual comments, never a push to main or Devloppement, never a merge), bot and worker as scheduled tasks at start (checked after a restart), rotating logs, 35 tests green; #43 « nothing new ». Left: a real write (waits for another player's issue), v3 reader once X-174 is done, GitHub App once X-175 is Phase 3 (11/13 around 11:40, the laptop's report checked by the master on GitHub): worker adapted to main's tool (89a0227), pull request #46 green then merged (9400249) on the user's word, discoveries.json and v3 at 2541 facts, no more push without a change. |
| X-165 | User's request of 12/11: Tensho dashboard. Full in developer mode: memory and collections, frame time per window and page, slow or broken… | Done in 0.1247 |
| X-166 | R01 (GPT audit of 12/11, checked by the master, accepted by the user): an imported discovery that becomes yours is removed from the shared… | Done in 0.1133 |
| X-167 | R03 (GPT audit of 12/11, checked by the master, accepted by the user): sending discoveries to GitHub can credit the player with dates… | Done in 0.1134 |
| X-168 | R04 (GPT audit of 12/11, checked by the master, accepted by the user): GitHub drops the « discoveries » label from an issue created by a… | Done in 0.1146 |
| X-169 | R05 and R06 (GPT audit of 12/11, checked by the master, accepted by the user): zip backups named to the second, the second overwrites the… | Done in 0.1141 |
| X-170 | Additions from the GPT audit of 12/11, checked by the master, accepted by the user, for a single client: installer (clean-up of *.old and… | Done in 0.1145 |
| X-171 | Part 1 done in 0.1216: seen prices (MarketSeen.json) written under a lock and joined with the file. Left, low priority (before X-139): discoveries, one key seen by both clients (SQLite base since 0.1136: distinct keys kept, same key: the last writer wins, rule to set), bank cache between two clients, shared cache purge | Must before the announcement (sorting of 11/13, accepted by the user). Rule decided by the user on 11/13 around 11:10 (« add both »): discoveries with the same key, two clients: each client adds its own observations (increments) to what is saved, nothing lost nor counted twice; behaviour test with two clients. Also left: the bank cache between two clients, the shared cache purge. Before the announcement. |
| X-172 | User's choice of 12/11: SQLite trial against the current storage for the downloaded data cache and discoveries, at 100,000 and 1 million… | Done in 0.1149.0 |
| X-173 | User's choice of 12/11: other data families into SQLite, one at a time with a copy of the old format: bank and inventories, gil journal… | Done in 0.1149 |
| X-174 | User's choice of 12/11 (option 3 kept): v3 discoveries exchange format: JSON lines, compressed, one .jsonl.gz file per kind with a hash… | Done in 0.1137.0 |
| X-175 | User's choice of 12/11 (option B kept): GitHub exchange on the players' side: GitHub App with least permissions (issues of the one… | Done in 0.1161 |
| X-177 | Master's finding of 2026-11-12 (today's dalamud.log): Dalamud hitch warnings (> 50 ms): CompetitiveGate (Add's Guarded callback) 44 times, up to 193 ms, and ReminderDtrEntry.OnUpdate 12 times, up to 82 ms, together at each zone change; measure, move the heavy work out of the frame (lazy read, per-zone cache), behaviour test; medium priority | Must before the announcement (sorting of 11/13, accepted by the user). Step 1 (measure) in 0.1162, step 2 (settings off the thread) in 0.1164. Analysis of 11/12 (docs/ai/lenteurs-12-11.md): CompetitiveGate is only a wrapper (the neighbouring reader is the culprit), UiBuilder 300 to 460 ms comes mostly from reloads (X-01 C1). Relay 35: C6 the « Nearby monsters » line is no longer built out of developer mode (0.1189); C4.4 the time of the game's function EquipGearset is no longer counted as the gear set reader's (0.1193). Relay 36: C4.2 done and measured (0.1198 to 0.1200). The readers' classes are compiled on a thread of their own and each reader starts once its class is compiled (guard delay 20 s); as the runtime compiles one method at a time for the plugin (read in 0.1199: three threads compiling together take as long as one after the other, on 32 processors), one background thread compiles at a time, in the order of the need: the interface's code, the readers' classes, then the rest. Neighbouring loads of the same session (about the 90th reload): « Load created » 2369 ms (0.1197), 5561 (0.1198), 3028 (0.1199), 1889 (0.1200); slow first passes of the first ten seconds 19 and 2141 ms, 3 and 197, 12 and 1073, 4 and 344 (BankInventoryBridge 68, ReminderDtrEntry 107, LocalLinkFeed 74, CraftActionCatalogLoader 95); first frame of the interface 254, 138, 596, 316 ms; in 0.1200 the readers start between 5 and 8.7 s after the load in this session (« Readers compiled on their own thread: 88 classes, 1839 methods in 3626 ms, the last one ready 8740 ms »). Left: C4.1 measure again in a fresh session; first frame of the interface (316 ms: start the warm-up's early part sooner, or the panels before the map); what the four readers still slow call elsewhere; a late slow pass waits behind the warm-up thread (CollectionReader 201 ms « compiling 1 methods »); rest of C6 (repeated lines « Submarine voyage check », « Reading eventMarkers », « Map event marker »; « drop data not loaded » to see with the user) User's decision of 11/13 around 11:10 (C4.2): free panels and bars drawn only once their part of the warm-up is compiled (panels first, the map next, bounded at 2 to 3 s): no more 350 ms freeze at load, panels 1 to 2 s later; doable by Opus (removed from points-fable.md). |
| X-178 | User request of 2026-11-12 (screenshot of Garden › Journal, Shirogane 2-17 garden 1): redo a garden's view as item icons with tooltips, no… | Done in 0.1259 |
| X-179 | User's finding of 2026-11-12 (« manual tending does not update the alarms, I don't think tending is detected »): the log of 11/12 shows… | Done in 0.1165 |
| X-180 | Review N1 (high): night review of 11/12 (relais/revue-nuit-12-11.md, master's folder; 31 findings checked by refutation, no token leak)… | Done in 0.1168 |
| X-181 | Review N2, N7, N8, N29, N30: night review of 11/12 (relais/revue-nuit-12-11.md, master's folder; 31 findings checked by refutation, no… | Done in 0.1171 |
| X-182 | Review N3, N4, N9, N11, N26, N27, N28: night review of 11/12 (relais/revue-nuit-12-11.md, master's folder; 31 findings checked by… | Done in 0.1173 |
| X-183 | Review N5, N12, N13, N14, N16, N17: night review of 11/12 (relais/revue-nuit-12-11.md, master's folder; 31 findings checked by refutation… | Done in 0.1210 |
| X-184 | Review N6, N21, N15, N20, N22: night review of 11/12 (relais/revue-nuit-12-11.md, master's folder; 31 findings checked by refutation, no… | Done in 0.1209 |
| X-185 | Review N10, N18, N19, N23, N24, N25: night review of 11/12 (relais/revue-nuit-12-11.md, master's folder; 31 findings checked by refutation… | Done in 0.1213 |
| X-186 | User's choice of 2026-11-12 (« Yes, ignore them », finding N31 of the night review, relais/revue-nuit-12-11.md): v3 exchange format… | Done in 0.1170 |
| X-187 | User request of 2026-11-12: Tensho's visual sort of the bags (Inventory/InventoryVisualSort.cs, InventoryVisualSortBridge.Dalamud.cs… | Done in 0.1172 |
| X-188 | User request of 2026-11-12 (on the Crescent Isle, « all of them »: the four helpers offered; follows point LUNULE; record… | Done in 0.1178 |
| X-189 | User request of 2026-11-12 (on the Crescent Isle, « all of them »: the four helpers offered; follows point LUNULE; record… | Done in 0.1179 |
| X-190 | User request of 2026-11-12 (on the Crescent Isle, « all of them »: the four helpers offered; follows point LUNULE; record relais/lunule-12-11.log; Occult Crescent Helper and Phantom Job Switcher under AGPL: read only, never copied; R03: show and advise, never play in the player's place): progress of occult knowledge (level, 465/2800 to the next) and of silver and gold pieces (what they can buy, from the island vendor's sheets); progress of the occult lore in Collections (CollectionKind.OccultLore already exists: add the count and what is missing); tests. Priority: with X-188 | Wanted before the announcement, rank 4 (sorting of 11/13). Mostly in 0.1178: one button per island coin → Storage › What to do with it?; « Occult records read: N / M » and « See what is missing »; coins in the island panel. Left: the knowledge level (no field of the game's state: KnowledgeLevelSync is 0 at 465/2800; MKDGrowData does not hold 2800; evidence 0.1178-occult-panel.md): one more in-game reading (level shown by the game against knowledge) will say where to take it from |
| X-191 | User's finding of 2026-11-12 (« Condition 81 % Spiritbond 0 % », on the Crescent Isle); clarified by the user: his gear is at 1 % in game… | Done in 0.1177 |
| X-192 | User request of 2026-11-12 (plugin comparison, K-01, accepted on 11/12): Grand Company expert delivery: on the expert tab, for each item: seals, part of a gearset, goes to the armoire, market value and a "worth more in gil" warning, seals left before the rank cap; a "seals" line in the panel under the tooltip; outside the window, a bag list marked "estimated" until the eligibility rule is recorded; nothing is handed in for the player | Wanted before the announcement, rank 9 (sorting of 11/13). To do: probe of the expert tab (AddonGrandCompanySupplyList), new reader; GCSupplyDutyReward, GrandCompanyRank; eligibility rule declared unknown until recorded |
| X-193 | User request of 2026-11-12 (plugin comparison, K-02, accepted on 11/12): venture planner: for each retainer (job, level, stats when known), the possible targeted ventures, quantity at each tier, market value on click, gain per venture compared with quick exploration (loot seen in Discoveries); the player chooses in the game User's term: « retainer tasks » (French « tâches des servants »). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: RetainerTask, RetainerTaskNormal, RetainerTaskParameter, RetainerTaskRandom sheets; retainer stats read while the retainer is open; in-game record of the tier thresholds |
| X-194 | User choice already made (GAPS M-13), found again on 2026-11-12 by the plugin comparison (K-03): Faux Hollows solver (Idyllshire weekly mini-game): most useful tile highlighted on the 6×6 board, like the Mini Cactpot; weekly reminder in Reminders and the character status | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: AddonWeeklyPuzzle (board), PlayerState (FauxHollowsState, FauxHollowsTimestamp); patterns enumerated by Tensho or a cited source; "unknown" when a pattern is missing; in-game record of the board |
| X-195 | User request of 2026-11-12 (plugin comparison, K-04, accepted on 11/12): fishing: a sound (and an optional line) on a bite, different for !, !! and !!!, a setting per strength; read from memory as in 0.887, no network packets; the player hooks himself | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: FishingHookStrength (292 to 294) already read; alarm sounds; check "!!!" in game (C30 session) |
| X-196 | User request of 2026-11-12 (plugin comparison, K-05, accepted on 11/12): table of the retainers of all characters: job, level, gil, items on sale, end of the sales with a reminder before expiry, current venture, market town; closes reminder #12 | Wanted before the announcement, rank 10 (sorting of 11/13). To do: RetainerManager.Retainer (ClassJob, Level, Gil, MarketItemCount, MarketExpire, VentureId, VentureComplete, Town) read after the list is opened; per-character copy like CharacterStatus; record #12 |
| X-197 | User request of 2026-11-12 (plugin comparison, K-06, accepted on 11/12): panel under the tooltip: a "desynthesis" line (job, item level, your level, above or below) and a "coffer" line (all obtained or n missing), each one can be turned off The user says (11/12): « we already have desynthesis and card packs »: check by Grep what the panel already shows and add only what is missing (compared level, other coffers). | Wanted before the announcement, rank 11 (sorting of 11/13). To do: Item.LevelItem, Item.ClassJobRepair, PlayerState.GetDesynthesisLevel; ItemSupplement (LuminaSupplemental) crossed with the collections; computed once per item |
| X-198 | User request of 2026-11-12 (plugin comparison, K-07, accepted on 11/12): personal tasks with a condition, ticked when Tensho observes it: own N × item (all storages), currency ≥ N, duty completed, mount/outfit/card obtained; "unknown" until the data has been read | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: PersonalTasks + ItemHoldings, Currencies, DutyProgressReader, collection state; FATEs excluded |
| X-199 | User request of 2026-11-12 (plugin comparison, K-08, accepted on 11/12): Plots: lottery phase (entries or results) and its end, read from PaissaDB; an "I entered" button that sets an alarm when the results open; check the coverage of the European worlds (the code says NA only) The user plays on Chaos (Europe): check that PaissaDB covers Chaos and fix « NA servers only » in Community/PaissaDb.cs. | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: show LottoPhase and PhaseUntil (already read); Tensho alarm; check Chaos and Light in PaissaDB; N-04 stays for the in-game reading |
| X-200 | User request of 2026-11-12 (plugin comparison, K-09, accepted on 11/12): submersibles: unlock path to a chosen sector, as for airships | Wanted before the announcement, rank 16 (sorting of 11/13). To do: SubmarineExploration + LuminaSupplemental SubmarineUnlock (GPL-3.0, credited); same model as AirshipSectors.UnlockPath |
| X-202 | User request of 2026-11-12 (plugin comparison, K-11, accepted on 11/12): Cosmic Exploration information page: missions per job and planet (rank, type), bronze/silver/gold thresholds, rewards, next windows of the timed and weather missions, progress read from the game; no red alert promised | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: WKSMission*, WKSCosmoTool* sheets; WKSManager to probe; time and weather conditions to record in game |
| X-203 | User request of 2026-11-12 (plugin comparison, K-12, accepted on 11/12): Ishgard Restoration (items per level, tiers, rewards) and the Diadem (items to gather per level, points, session count) | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: HWDCrafterSupply*, HWDGathererInspection* sheets; no instance weather forecast |
| X-201 | User request of 2026-11-12 (« add an automatic route calculator for airships »): the airship planner (MainWindow.AirshipPlanner.cs) is… | Done in 0.1248 |
| X-204 | User request of 2026-11-12 (plugin comparison, relais/comparaison-plugins-12-11.md; content played by the user, answer of 11/12): K-33… | Done in 0.1235 |
| X-205 | User request of 2026-11-12 (plugin comparison, relais/comparaison-plugins-12-11.md; content played by the user, answer of 11/12): K-35… | Done in 0.1241 |
| X-206 | User request of 2026-11-12 (plugin comparison, K-13, accepted on 11/12): session log: items obtained today or this session with their source (unattributed shown as such), filter, export; duration of each completed duty, shown after the end, history First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: view over ItemGains and GainSources; IDutyState for the duration; TenshoData.db; with X-159; user's choice of 11/12: duration history kept across sessions |
| X-207 | User request of 2026-11-12 (plugin comparison, K-14, accepted on 11/12): estimated value of stocks per storage and per character, a "sleeping and sells" list (sold price × sales per day, NPC price beside), NPC vs market line under the tooltip; advice only, on click First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 12 (sorting of 11/13). To do: bank + batched MarketService + MarketQuote; NpcVendorCatalog.GilPrice; old prices flagged |
| X-208 | User request of 2026-11-12 (plugin comparison, K-16, accepted on 11/12): "My listings": retainer sale lists kept, since when, price, "sold since the last visit" (listing gone + retainer gil rise); Universalis on click only (D-82); optional CSV First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: SellListEntries kept per retainer; cross-check with GilJournal.SplitRetainerRise; after X-171 |
| X-209 | User request of 2026-11-12 (plugin comparison, K-18, accepted on 11/12): import of a Teamcraft list (import link or pasted text) into a Tensho crafting list, complement of the A-09 export; no request First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: base64 decoding of "itemId,recipeId,quantity; …" (MIT format already read); 32 × 64 bounds; message when the format changes |
| X-210 | User request of 2026-11-12 (plugin comparison, K-19, accepted on 11/12): ocean fishing: suggested bait at each stop by state (spectral current, intuition, missions, point fish) with the reason and source (DEC-044), never "guaranteed"; local voyage history First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: IKDSpot, IKDRoute, IKDFishParam, IKDContentBonus, GatherBuddy data; OceanVoyageRecord kept per voyage; C30 session |
| X-211 | User request of 2026-11-12 (plugin comparison, K-20, accepted on 11/12): Moogle Treasure Trove: wanted rewards ticked, irregular tomestones left to earn, already obtained First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 18 (sorting of 11/13). To do: event SpecialShop (festival layers), currency, IUnlockState; record at the next edition |
| X-212 | User request of 2026-11-12 (plugin comparison, K-21, accepted on 11/12): CSV export of the bank (all storages or one character) and copy of an item's seen price history First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 13 (sorting of 11/13). To do: same form as VoyageHistory.Csv; SeenPrices |
| X-213 | User request of 2026-11-12 (plugin comparison, K-23, accepted on 11/12): shared FATEs: rank per zone, progress to the next, rewards left, read once the game window has been opened First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 15 (sorting of 11/13). To do: AgentFateProgress (CurrentRank, MaxRank, FateProgress, NeededFates); copy kept per character |
| X-214 | User request of 2026-11-12 (plugin comparison, K-24, accepted on 11/12): an "Open in the duty finder" button on any mention of a duty, without queuing; Wondrous Tails duties still to do in the duty finder panel; "Go there" only where the game gives a position (D-12) First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: AgentContentsFinder.OpenRegularDuty; WeeklyBingoOrderData; disabled inside a duty |
| X-215 | User request of 2026-11-12 (plugin comparison, K-25, accepted on 11/12): Custom deliveries: items each client asks for this week, bonus, reward at the target tier, link to the recipe or gathering; read at the client window then kept for the week; "unknown" otherwise First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 17 (sorting of 11/13). To do: record of the client window; SatisfactionSupply, SatisfactionNpc; SupplySeed calculation only on decision (X-75.6, DEC-043) |
| X-216 | User request of 2026-11-12 (plugin comparison, K-26, accepted on 11/12): glamour: "same model" on any item sheet (extends SameLook) with ✓/✗ and location; piece-by-piece "Glamour" collection (dresser, armoire, inventories, retainers) First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 14 (sorting of 11/13). To do: Item.ModelMain/ModelSub/EquipSlotCategory, MirageManager; Collections view |
| X-217 | User request of 2026-11-12 (plugin comparison, K-30, accepted on 11/12): optional server bar entries: bags, wear, place and instance, off… | Done in 0.1226 |
| X-218 | User request of 2026-11-12 (plugin comparison, K-32, accepted on 11/12): job levels and gearsets of each character in "Your characters"… | Done in 0.1228 |
| X-219 | User request of 2026-11-12 (plugin comparison, K-34, accepted on 11/12): richer mount sheet  First check by Grep what already exists (user… | Done in 0.1229 |
| X-220 | User request of 2026-11-12 (plugin comparison, K-36, accepted on 11/12): one search in the main window, opens the sheet or page  First… | Done in 0.1230 |
| X-221 | User request of 2026-11-12 (plugin comparison, K-37, accepted on 11/12): Grace remaining XP, "unknown" if the start was not seen  First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 5 (sorting of 11/13). Part 1 done in 0.1231 (probe: cap only in the manuals' description, evidence 0.1231-grace-cap.md; statuses 45 and 46 read in developer mode); left: read the status param in game, then show it |
| X-222 | User request of 2026-11-12 (plugin comparison, K-38, accepted on 11/12): S rank condition counters  First check by Grep what already exists… | Done in 0.1232 |
| X-223 | User request of 2026-11-12 (plugin comparison, K-39, accepted on 11/12): next GATE and draw times, alarm  First check by Grep what already… | Done in 0.1233 |
| X-224 | User request of 2026-11-12 (plugin comparison, K-40, accepted on 11/12): release patch in the item sheet when known  First check by Grep… | Done in 0.1227 |
| X-225 | User request of 2026-11-12 (plugin comparison, K-46, accepted on 11/12): aetherial wheel reminder  First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 6 (sorting of 11/13). Part 1 done in 0.1234 (probe: 20, 45, 70 h per grade, evidence 0.1234-aetherial-wheels.md; « Wheel » window read in developer mode); left: read the stand's window in game, then the reminder |
| X-226 | User request of 2026-11-12 (plugin comparison, K-15, accepted on 11/12; user's R04 agreement on 11/12, pros and cons given): out-of-combat preparation reminder: food over or nearly over, chocobo companion absent or nearly over; chat and sound like wear (D-38), nothing in combat or in a duty; combat food only if the user agrees (R04) First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 19 (sorting of 11/13). To do: status 48 (RemainingTime), Buddy.CompanionInfo.TimeLeft, ICondition; adjustable thresholds |
| X-227 | User request of 2026-11-12 (plugin comparison, K-17, accepted on 11/12; user's R04 agreement on 11/12, pros and cons given; new host api.xivgear.app accepted): import of an xivgear link into the gear planner (pieces and materia), missing pieces and materia framed in the storages (X-54) and flagged under the tooltip; request on click only First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: bounded xivgear client (BoundedHttp), "game data only" switch, credits, SECURITY; Etro second if still served |
| X-228 | User request of 2026-11-12 (plugin comparison, K-22, accepted on 11/12; user's R04 agreement on 11/12, pros and cons given): notifications while the game is in the background (desktop app, Discord or taskbar flash, chosen per event): disconnection, party full, alarm, end of a synthesis, venture or vessel back; private message only as "a message from X" (R04 accepted on 11/12), text on explicit option; nothing on entering combat First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: IClientState, LogMessage (party full) to record, IChatGui TellIncoming, FlashWindowEx; X-163 and DiscordNotifier channels |
| X-229 | User request of 2026-11-12 (plugin comparison, K-31, accepted on 11/12; user's R04 agreement on 11/12, pros and cons given): food in the gear planner, stats at synced level once the formula is proven  First check by Grep what already exists (user request of 11/12). | Wanted before the announcement, rank 20 (sorting of 11/13). To do: ItemFood; formula declared unknown until proven |
| X-230 | User request of 2026-11-12 (plugin comparison, K-42, accepted on 11/12; user's R04 agreement on 11/12, pros and cons given): plates kept in Tensho, applied on click  First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: plate agent |
| X-231 | User request of 2026-11-12 (plugin comparison, K-43, accepted on 11/12; user's R04 agreement on 11/12, pros and cons given): chat keyword alerts  First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: IChatGui, nothing kept |
| X-232 | User request of 2026-11-12 (plugin comparison, K-44, accepted on 11/12; user's R04 agreement on 11/12, pros and cons given): FC chest log summary per item  First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: window probe |
| X-233 | User request of 2026-11-12 (plugin comparison, K-47, accepted on 11/12; user's R04 agreement on 11/12, pros and cons given): optional Discord status  First check by Grep what already exists (user request of 11/12). | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do: Discord app, cleared during the cutoff |
| X-234 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: « fish open now and soon » view, sorted by next window (GatherBuddy); D-54 b reopened: batched computation off the frame | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-235 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: full tree of a craft list (sub-recipes, gathering); D-14 b reopened | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-236 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: duplicates of an item across storages, and where to group them; D-67 b reopened | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-237 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: bonus role alert for duty roulettes, read without network packets; D-59 b reopened | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-238 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: currencies, logs and outfits of every character; D-40 b, D-21 b, D-53 b reopened | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-239 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: hunt mark spawn points on the map; D-10 reopened | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-240 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: coordinates typed in the chat turned into a map link; D-41 reopened | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-241 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: upcoming seasonal event dates, each citing the Lodestone notice, file kept in the repository; C-06 reopened; the user asks to check first whether the events tool already does it | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-242 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: scrip kind of each collectable and cap alert; D-19 b reopened; a probe first | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-243 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: Mini and Jumbo Cactpot tickets, Masked Carnivale targets; D-32 reopened; a probe first | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-244 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: « repair » reminder when a submarine returns; D-43 b reopened; find the data first | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-245 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: estimated Fashion Report score; DEC-012 reopened: only with a real cited source, never invented | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-246 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: highlight in the island sanctuary workshop agenda; C23 and DEC-033 reopened (drawing on the game's interface) | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-247 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: Windows notification « duty found »; D-49 b reopened (with X-163 and K-22) | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-248 | User's choice of 2026-11-12 (plugin comparison, section 5: idea declined before, reopened) First check by Grep what already exists (user request of 11/12).: import on click of third-party site links (e.g. for macros); D-15 b reopened (with K-17) | After the announcement, before 1.0 (sorting of 11/13, accepted by the user). To do |
| X-250 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. BLOCKING… | Done in 0.1180 |
| X-251 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. BLOCKING… | Done in 0.1181 |
| X-252 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. BLOCKING B5… | Done in 0.1182 |
| X-253 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. BLOCKING B6… | Done in 0.1183 |
| X-254 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. G1: README… | Done in 0.1219 |
| X-255 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. G4, D9… | Done in 0.1219.0 |
| X-256 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. G5, G12… | Done in 0.1222 |
| X-257 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. G6, D5, D6… | Done in 0.1222.0 |
| X-258 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. G7… | Done in 0.1220 |
| X-259 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. G8, D4… | Done in 0.1221 |
| X-260 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. G9, G10… | Done in 0.1223 |
| X-261 | First-player review of 11/12 (docs/ai/premier-joueur-12-11.md), before the public announcement, user's choice: all in the plan. G11… | Done in 0.1220.0 |
| X-262 | User's choice of 11/13 around 13:20 (after the Palace of the Dead 1-10): deep dungeons (content type 21) leave DEC-038's cut (CompetitiveZones.Build without DeepDungeonContentType); Tensho's map is cut (the game's map left alone) in an instance with a specific map, found through the sheets rather than a list; PvP, Triple Triad and Mahjong stay cut; DEC-038 and ARCHITECTURE (« Competitive switch-off ») updated, en and fr messages, behaviour test, a trial in the campaign route (enter the Palace of the Dead) | Must before the announcement (sorting of 11/13, accepted by the user). To do, Opus relay |
| X-263 | User request of 11/13 around 12:40 (screenshot « Seen in game: Zelkova log ← venture no. 584 »): show the retainer venture's name rather than its number. DiscoveryVentureName (like DiscoveryDutyName): exploration venture → RetainerTaskRandom name; normal venture → class and level (« level 70 botanist venture »); the number only when the sheet gives nothing; MainWindow.Discoveries.cs (DiscoveryKind.Venture) and DiscoveriesUi.FromVenture in en and fr; test; desktop app (R33) | Must before the announcement (sorting of 11/13, accepted by the user). To do, small point, Opus relay |
| X-264 | User request of 11/13 around 13:35 (reference screenshot of a Grafana-like dashboard, « unrelated to the plugin »): Tensho's dashboard as… | Done in 0.1271 |
| X-265 | User request of 11/13 around 15:05 (« sort allied societies by kind: combat, crafting, gathering »): Allied societies page: sort or group by kind: combat, crafting, gathering (filter or group headers, choice kept). Kind read from the game's sheets (classes of the society's quests, ClassJobCategory) rather than a hand-written list; a mixed society (crafting and gathering) in both or under « mixed »; en and fr; test; desktop app (R33) | Must before the announcement (sorting of 11/13, accepted by the user). To do, small point, Opus relay |
| X-266 | User report of 11/13 around 15:30 (Mor Dhona, screenshot; diagnosis relais/diagnostic-pnj-absents-13-11.md): Tensho's map shows vendors absent in game: a Level position « Always » of an NPC placed only by a quest layer (Cristiana 1016290, layer QST_JobRel101, quest 67748 not taken; 31 cases in the game). Fixes: « QST_ENPC » case-insensitive; a Level position whose layers in the territory are all quest layers → shown only when the linked quest (script starting with the layer's name) is under way or done, hidden otherwise; pure function in NpcShopLinks, tests (FestivalTests), CopyRules raised. The two « ! » seen beside are real quests present in game (confirmed by the user): not a bug | Must before the announcement (sorting of 11/13, accepted by the user). To do, Opus relay |
| X-267 | Tooling 1 (method plan site 3, R36): every test at each version but in parallel (per class, own temp folder per test, exclusion for global… | Done in 0.1275 |
| X-268 | Tooling 1 (R20.3, R19.3, R23, R28.12): a relay's last version made ready to release by the relay (docs.py gen and « suite » commit)… | Done in 0.1276 |
| X-269 | Tooling 1 (R28.10, R28.9, R26): return script without a model (git and CI check of every relay commit, points done or annotated… | Done in 0.1277 |
| X-270 | Tooling 1 (audit of 11/13): accurate start: start message without a stale line (patch reference redone after 7.58, master timestamp kept… | Done in 0.1277 |
| X-271 | Tooling 2 (site 4, R37): reference images of the pages drawn outside the game (themes, sizes, empty, loading, error states, trial garden), compared at each version with tolerance (SSIM); an intended change produces an image to approve; before / after gallery for the user; in-game trials covered by an image removed from his list. Done when: a page changed for no reason fails the publish (tried), the gallery opens, the count of removed trials is given. | Must before the announcement (method plan of 11/13, accepted by the user). To do |
| X-272 | Tooling 2 (R34, R13.2, R02.2, R27.2, R28.16, R28.5, R28.7): trials by batch (q.py lot: a block's trials by place, 15 to 20 min) and a cap of 15 versions never seen in game; idea box with repeats recognised and counted; sorting class in plan.jsonl and announcement criteria checked; relay and reviewer briefs produced with their reminders; guard on the master's writes while a secondary lives. Done when: each command tried, the cap refusing a version that is not a fix (tried). | Must before the announcement (method plan of 11/13, accepted by the user). To do |
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
| X-131 | Dropped on 2026-11-11 at the user's request (« laisse tomber x131 »), after the pros and cons (Dalamud rules, Square Enix terms, item loss) |
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
| Jardin | Un entretien et une récolte, pour reconnaître leurs messages ; puis retirer les lignes de journal de capture | Fait en 0.1110 |
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
| T26 | Dirigeables : trajet du dernier voyage reconstitué |  | Fait en 0.919.0 |
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
| Île de Lunule (Croissant occulte) | Relevés en jeu au Croissant occulte (mode développeur, « Reading fields »), puis les feuilles MKD | Dans Tensho : les archives occultes dans les Collections (CollectionKind.OccultLore, feuille MKDLore) et un libellé pour la carte. Relevé du 12/11 vers 14 h 57 (l'utilisateur sur l'île, mode développeur, 4 captures : niveau de savoir 3, 465/2800 ; Paladin fantôme niv. 2, 240/2400 ; pièces d'argent 270, d'or 12 ; actions Défense, Prière, Magi Soin, Promesse ; traits niv. 4 et 5) : la ligne « Reading fields: occult knowledge 465/2800, support job 1 levels 1 2 0 1 0 0 1…, silver 270 gold 12 » colle exactement aux captures (job 1 = Paladin, niveau 2) ; copie dans relais/lunule-12-11.log. Reste : choisir avec l'utilisateur ce que Tensho montre (voir la comparaison avec les autres plugins), puis le coder d'après les feuilles MKD (MKDSupportJob, MKDGrowData, MKDTrait, MKDChain…). Occult Crescent Helper et Phantom Job Switcher : AGPL, lecture seule |
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
| X-01 | Lourdeur : trouver ce qui bloque une image de Tensho (120 à 230 ms toutes les quelques minutes, 2,6 s une fois) et le sortir du dessin | Indispensable avant l'annonce (tri du 13/11, accepté par l'utilisateur). En cours : 0.1103, 0.1104, 0.1111 (carte compilée tôt) ; relais 34 : C1.1 mesure du tas et des contextes (0.1184), C2.1 overlays compilés tôt (0.1185), C3.1-C3.2 fêtes de la carte (0.1186). Relais 35 : C1.3 racine de la fuite des instances déchargées trouvée et corrigée (0.1187 : accroches statiques de CompetitiveGate et de la météo vidées au déchargement ; 0.1194 : le champ statique MainWindow.RaphaelSequences tenait la fenêtre et le cache de données ; mesure « Earlier instance » de 0.1187 à 0.1192, preuve docs/evidence/0.1187-unload-roots.md), C3.3 MapFirst élargi (0.1188 : « canvas vendors » 4 ms à la première carte contre 32 à 40), régime établi mesuré (0.1190 : image 0,46 ms en moyenne, 95e centile ≤ 1 ms, pire 3 ms, en ville, carte ouverte), C2.2 feuilles des monstres lues à l'avance (0.1191 ; C2.3 inutile, mesuré : Tracker 0,02 ms par image). Relais 36 : C1.3 confirmé en jeu, lu dans dalamud.log (0.1195 : les instances de 0.1194 déchargées n'ont plus rien de retenu, seulement des valeurs partagées, tableaux vides et valeurs statiques ; 0.1196 : l'instance de 0.1195 se lit « 0 of 1692 parts alive », le tas ne grossit plus d'un rechargement à l'autre) ; outils de recherche retirés en 0.1196 (vidage des anciennes instances, note en profondeur), une ligne courte par instance d'avant gardée. Reste : lire C2.2 en plein air ; C5 (pics isolés : pages Rangements et Paramètres, carte de région, page Équipement). Le contexte de chargement lui-même reste vivant par l'API de réglages de Dalamud (Newtonsoft) et les appels JSON sans options : à décider (D-144, format des réglages). Règle : comparer des chargements de rang voisin ou après un redémarrage du jeu [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-01) ; relais 40 : C5, index des quêtes par identifiant dans QuestService hors de l'image (0.1237) ; reste C5 : page Équipement ; relais 41 : C5 page Équipement, modèle du plan reconstruit seulement quand une entrée change (0.1242, GearModel1242Tests) ; reste : lire C2.2 en plein air, D-144 Relais 42 (13/11) : C2.2 en plein air toujours pas mesurable (personnage en ville, territoire 1186, pendant tout le relais) ; à relever pendant la campagne d'essais (essai T-0.1191-1 du parcours, lieu « En plein air »). |
| X-02 | Données gardées plus longtemps, relues seulement si absentes ; réglages qui complètent « Cache & péremption » | Fait en 0.750.0 |
| X-03 | Lien « Ouvrir dans Tensho » d'une alarme : n'ouvre rien | Fait en 0.837.0 |
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
| X-18 | Journal des gils : icônes (gils, catégories) et moins de « Autre » : trouver d'autres catégories d'après le jeu | Souhaitable avant l'annonce, rang 3 (tri du 13/11). Icônes et couleurs en 0.768 ; « Autre » : relevé au journal (« Gil other ») ; 0.775 : dépense après un écran de chargement = téléportation ; « +1 000 » sans fenêtre encore inconnu |
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
| X-43 | Base locale de découvertes : relever ce qui est à portée (monstres et leurs lieux, butin, sorts bleus appris, captures du dresseur, points de récolte, pêche…) pour compléter les outils ; options de débogage mais actif hors débogage ; import (fusion avec la nôtre) et export, pour ne plus dépendre des données communautaires à force de jouer | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). Cadre accepté le 2026-11-07 (« Oui, ce cadre ») : fichier local, provenance par entrée, aucun autre joueur, rien d'envoyé, pas d'aide au combat, coupé en JcJ, import … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-43) |
| X-44 | Carnet d'artisanat : pas de menu Tensho au clic droit, ni sur une recette de la liste ni sur un matériau | Fait en 0.808 |
| X-45 | Carte : des marchands d'événement sont affichés alors qu'ils ne sont pas là en jeu (Limsa, la Dunette) ; à corriger juste avant la phase 7 | Fait en 0.810 |
| X-46 | Carte : le zoom ne reste pas toujours celui qu'on a réglé ; à faire juste avant la phase 7 | Fait en 0.850 |
| X-47 | Récompenses de quête (fenêtre de la quête) : ni panneau Tensho sous l'info-bulle de l'objet, ni entrée Tensho ; à faire juste avant la… | Fait en 0.852 |
| X-48 | Carte : le clic droit sur la cible d'une quête (« Cible indiquée par le jeu (journal) ») n'offre pas l'éthérite la plus proche ; à faire… | Fait en 0.853 |
| X-49 | Carte : la cible de quête ne disparaît pas après validation de la quête (Ishgard, les Contreforts, capture du 2026-11-07) | Fait en 0.813 |
| X-50 | Carte : les icônes de bord de carte doivent passer au-dessus de tout (cachées sous le bandeau des noms, capture d'Abalathia) | Fait en 0.814 |
| X-51 | Panneau des collectionnables : surligner sur les barres de raccourcis les compétences que le plan demande, comme l'aide à la récolte… | Fait en 0.815 |
| X-52 | Base de découvertes : les prix vus au tableau des ventes remplacent Universalis (plus fiables), après avoir vérifié qu'on a autant… | Fait en 0.817 |
| X-53 | Monstres proches : le yak laineux et le loaghtan (lait de yak, viande de loaghtan de la liste de suivi) n'ont pas d'icône sur la carte ni… | Fait en 0.828.0 |
| X-54 | À l'ouverture d'un autre rangement que l'inventaire (servant, sacoche du chocobo, coffre de compagnie, autres) : surligner les objets qui y… | Fait en 0.820 |
| X-55 | Listes de suivi d'artisanat et d'atelier : préciser quand l'objet n'est pas sur soi (chez un servant, dans la sacoche, le coffre de… | Fait en 0.822 |
| X-56 | Listes de suivi d'artisanat et d'atelier : à la place de « téléportation 219 gils », l'icône de la compétence Téléportation puis l'icône… | Fait en 0.824 |
| X-57 | Carte, quête de cuisinier à Limsa : la cible (au bon endroit) et l'icône de quête ne sont pas au même endroit, la quête affichée tombe hors de la zone, et elle n'apparaît pas sur la carte du Tillac (captures du 2026-11-07, la carte du jeu montre le lien vers le Tillac) | Indispensable avant l'annonce (tri du 13/11, accepté par l'utilisateur). À faire, après X-56 ; en cours : le jeu donne deux marqueurs pour la quête 2046 « Le pari » sur la carte 11 (icônes 71141 et 71143, objectifs 2046 et 520), sa liste … [détail](archive/GLOBAL-PLAN-X-2026-11-09.md#x-57) |
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
| X-68 | Carte de Tensho : la zone de la quête de cuisinier (Mor Dhona) « ne se fait pas zoomer dessus » (capture du 2026-11-08) | Fait en 0.846.0 |
| X-69 | Carte de Tensho : « la map ne se rafraîchit pas assez » : la quête est passée à « Parler à Raulf », mais la « Cible indiquée par le jeu… | Fait en 0.847 |
| X-70 | Carte de Tensho : la zone de la nouvelle étape d'une quête manque tant que la carte du jeu n'a pas été fermée puis rouverte (Kugane, «… | Fait en 0.848 |
| X-71 | Carte de Tensho : reprendre les icônes plus haut / plus bas de la carte du jeu (vues à Mor Dhona) partout, à la place de la flèche de la… | Fait en 0.849 |
| X-72 | Base de découvertes (parseurs) : vérifier qu'elle est efficace, l'utilisateur a déjà un peu joué (2026-11-08) | Fait en 0.856 |
| X-73 | 0.1003 (revérification du 2026-11-09 : base de l'utilisateur et sources communautaires) : butin des aéronefs (nouvelle sorte AirshipDrop… | Fait en 0.887.0 |
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
| X-85 | Document « a faire » de l'utilisateur (2026-11-08), pêche en mer (priorités données par l'utilisateur) : le panneau de pêche en mer doit… | Fait en 0.964.0 |
| X-86 | 0.966 : case « quêtes pas encore disponibles » (clic droit et réglages). 0.967 : flèche de hauteur sur les zones de quête et d'événement… | Fait en 0.966 |
| X-87 | 0.968 : zone d'un mandat acceptée retirée quand ce mandat est en cours (et marqueurs que le jeu ne dessine pas) ; cibles du mandat avec… | Fait en 0.968 |
| X-88 | 0.969 : 256 catégories par zone, ajout d'un groupe entier de catégories (groupes du jeu). Document « a faire », tri d'inventaire : plus de… | Fait en 0.969 |
| X-89 | 0.970 : extension des quêtes (donnée du jeu) dans leur info-bulle et leur fiche ; extension des objets seulement quand lieux de récolte et… | Fait en 0.970 |
| X-90 | 0.996 : bestiaire des donjons profonds par la même lecture du wiki (page de chaque groupe d'étages : ennemis et notes ; titre du jeu sans «… | Fait en 0.996 |
| X-91 | 0.971 : parseurs revérifiés (`docs/evidence/x91-parsers-check.md`) : un seul point prévu manquait (lieux des ALÉA vus sur la page des rangs… | Fait en 0.971 |
| X-92 | 0.1001 : inventaire des listes figées dans le code : seule celle des territoires du Dédale (coupure) en relevait ; ses plateaux sont… | Fait en 0.1001 |
| X-93 | Demande du 2026-11-08 : « prévisions pour une masse de données conséquente possible » (découvertes partagées par beaucoup de joueurs… | Fait en 0.1040.0 |
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
| X-108 | Demande du 2026-11-09 : parseur du jardinage pour combler les trous par découverte, croisements inclus. Plan : 1) relevé en jeu d'une récolte qui donne des graines (lignes du chat, objets) ; 2) nouvelles sortes de découvertes : pousse observée (plantation → maturité : heures réelles par graine), entretien observé (intervalle), récolte (graine plantée → objets et quantités), croisement (graine obtenue ≠ graine plantée, avec les graines des emplacements voisins notées au journal) ; 3) partage communautaire (outil de fusion à jour) ; 4) le journal et la grille des croisements complétés par ces relevés, la source communautaire (nick75g) gardée en repli et marquée ; graines manquantes (Bleuets) depuis les feuilles | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). Relevé en jeu d'abord (table des rappels) ; après X-97 |
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
| X-119 | Suite de X-118 (plan 4), étapes 1 à 4 : 1) application jeu fermé, pages de données (objets, recettes, météo, lexique, découvertes, collections depuis les fichiers enregistrés) ; 2) lien local avec le plugin (canal nommé, utilisateur courant seulement, aucun port réseau, désactivé par défaut ; état lu sur le thread du framework, copié, envoyé hors du thread, R07) et pages en direct ; 3) actions sur clic seulement, exécutées par le plugin comme le bouton en jeu (drapeau, demande de téléportation, alarmes, listes) ; 4) réglage par page : dans le jeu, dans l'application ou les deux ; la carte, les panneaux attachés aux fenêtres du jeu, l'aide de récolte, l'artisanat en direct et la Revue de mode restent en jeu. Application distribuée à part (release GitHub), jamais lancée ni contenue par le plugin ; synchro (demande du 2026-11-10, R33) : l'application et le plugin portent le même numéro, se construisent, se testent (toutes les pages dessinées hors du jeu dans Verify) et se publient ensemble ; chaque release porte l'application, Verify -Release refuse sans elle ; l'application vérifie sa compatibilité avec le plugin installé | Souhaitable avant l'annonce, rang 2 (tri du 13/11). Objectif : Tensho sur un 2e écran (accepté le 11/11). Fait : étape 1 (0.1031), lien (0.1081), protocole 2 (0.1114), catégories (0.1119), copie miroir par catégorie et magasins dessus (0.1131, première tranche avec X-151). Reste (passage-25.md) : autres catégories (personnages, journal des gils, succès, missions, découvertes), reste de X-151 (hôte encore par réflexion), étape 3 (actions, canal à deux sens), étape 4 (réglage par page), point d'entrée des polices ; les magasins de l'application sont en lecture seule de fait jusqu'à l'étape 3 ; choix de l'utilisateur du 12/11 pour l'étape 3 : l'application peut créer ou changer les données de Tensho (alarmes, listes, outils, réglages) seulement quand le lien avec le jeu est actif ; chaque action de l'application est transmise au plugin, qui l'applique et l'enregistre lui-même (l'application n'écrit jamais elle-même) ; rien n'est envoyé au jeu ; jeu fermé : lecture seule (X-180) |
| X-120 | Demande du 2026-11-10, revue le même jour par l'utilisateur : plan 2 seulement des six plans (docs de l'agent, proposition) : un outil de… | Fait en 0.1032 |
| X-121 | Demande du 2026-11-10 : mise à jour automatique de l'application de bureau depuis GitHub, gérée entièrement par l'agent (aucun geste de… | Fait en 0.1033 |
| X-122 | Décisions de l'utilisateur du 2026-11-10 (séance des décisions) à coder dans le plugin : D-102 case « Envoyer aussi sur Discord » pour les… | Fait en 0.1045 |
| X-123 | Décision D-100 de l'utilisateur (2026-11-10) : code de conduite (CODE_OF_CONDUCT.md, deux langues), CODEOWNERS (revue du propriétaire… | Fait en 0.1033 |
| X-124 | Accord de l'utilisateur du 2026-11-10 (« je te donne l'accord pour tout ») pour la suite de X-120 : dépôt GitHub séparé pour… | Fait en 0.1035 |
| X-125 | Accord de l'utilisateur du 2026-11-10 (« oui ») : PluginUiTest essayé sur 4 ou 5 plugins Dalamud open source populaires (compilés depuis… | Fait en 0.1036 |
| X-126 | Demande de l'utilisateur du 2026-11-11 : installer l'application de bureau depuis Tensho, jamais obligatoire, seulement depuis Paramètres… | Fait en 0.1113 |
| X-127 | Demande du 2026-11-11 : liste de souhaits en outil à part, avec son entrée dans le menu de Tensho (sortie de l'onglet de Collections)… | Fait en 0.1106 |
| X-128 | Demande du 2026-11-11 (capture) : carte de Tensho, la case « Quêtes pas encore disponibles » est dans le menu du clic droit au-dessus des… | Fait en 0.1107 |
| X-129 | Demande du 2026-11-11 (capture, Graines de pommier luisant) : aide à la récolte, un objet caché révélé après une dépense de PR n'a « aucun… | Fait en 0.1108 |
| X-130 | Demande du 2026-11-11 : jardin, prévisions de croisement à partir du jardin planté (journal de Tensho : graine et place de chaque… | Fait en 0.1109 |
| X-132 | Demande du 2026-11-11 : alarmes du jardin (maturité, entretien, d'après le journal de l'outil Jardinage) dans un onglet « Alarmes » de… | Fait en 0.1160 |
| X-133 | Demande du 2026-11-11 : alarmes de rappel dans un onglet « Alarmes » de l'outil Rappels, leur nombre en chiffre à côté de « Rappels » dans… | Fait en 0.1159 |
| X-134 | Demande du 2026-11-11 (terreau thanalan 3) : plan de PR de l'aide à la récolte, objets récoltables une seule fois par point (une prise comptée, essais suivants sur l'objet le plus utile) et objets dont la quantité ne peut pas être augmentée (aucune dépense de PR proposée pour la quantité) ; d'abord les données du jeu, sinon vos relevés | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). En partie en 0.1167 et 0.1175 (journal en mode développeur des nombres de la fenêtre Gathering, jusqu'à 200, jamais ses textes). Reste : relevé de l'utilisateur sur un point avec un objet « RARE » (essai de 0.1175), puis aucune PR proposée pour sa quantité |
| X-135 | Demande du 2026-11-11 (plateau 1 du Dédale bestial) : lire le résultat du plateau (XBMResult : score, chemin, cibles, bonus ; expérience et gains de rang de chaque bête ; butin XBMContentsBooty, XBMContentsTreasure) et reprendre les rangs des bêtes dans la page Dresseur et l'aide | Souhaitable avant l'annonce, rang 7 (tri du 13/11). Résultat du plateau fait en 0.1163 (score, rang et expérience de chaque bête, fenêtre XBMResult, rangs repris par le Dresseur) ; reste : le butin (XBMContentsBooty, XBMContentsTreasure, relevés le 12/11 dans relais/dedale-12-11-matin.log) |
| X-136 | Demande du 2026-11-11 : aide du Dédale bestial plus cohérente : équipe choisie selon les faiblesses et statistiques de l'ennemi, un conseil par combat du plateau si possible, bêtes à faire monter pour les plateaux suivants proposées, combos des bêtes mis en évidence avant le combat, et en combat surlignage des compétences de combo Instinct II selon la bête invoquée (option, coupée par défaut) | Souhaitable avant l'annonce, rang 8 (tri du 13/11). À faire ; relais 31 : la suite demande des données non établies (combos d'Instinct II ; sens des colonnes 3 et 4 de XBMContent ; « rang 1 à 5 » du 1er plateau alors que les bêtes y sont au rang 6) : relevé ou source à trouver d'abord (R06, rien de deviné) |
| X-137 | Choix de l'utilisateur du 2026-11-11 (« oui mais en pvp et classement ») : Tensho n'est plus coupé dans le Dédale bestial (retiré de la… | Fait en 0.1115 |
| X-138 | Demande du 2026-11-11 (capture, Forêt centrale) : carte de Tensho, menu du clic droit, onglet Éthérites : certaines éthérites n'ont que… | Fait en 0.1166 |
| X-140 | Audit A01 et A03 (audit GPT du 11/11 (C:\Plugins-ff14-audit-2026-11-11-7f5ce3a\AUDIT.md), vérifié dans le code par la maître, ordre accepté… | Fait en 0.1117 |
| X-141 | Audit A02 (audit GPT du 11/11 (C:\Plugins-ff14-audit-2026-11-11-7f5ce3a\AUDIT.md), vérifié dans le code par la maître, ordre accepté par… | Fait en 0.1118 |
| X-142 | Audit A04, A05, A09 (audit GPT du 11/11 (C:\Plugins-ff14-audit-2026-11-11-7f5ce3a\AUDIT.md), vérifié dans le code par la maître, ordre… | Fait en 0.1122 |
| X-143 | Audit A06, A07, A08 (audit GPT du 11/11 (C:\Plugins-ff14-audit-2026-11-11-7f5ce3a\AUDIT.md), vérifié dans le code par la maître, ordre… | Fait en 0.1125 |
| X-144 | Audit A13 (audit GPT du 11/11 (C:\Plugins-ff14-audit-2026-11-11-7f5ce3a\AUDIT.md), vérifié dans le code par la maître, ordre accepté par… | Fait en 0.1126 |
| X-145 | Audit A11 (audit GPT du 11/11 (C:\Plugins-ff14-audit-2026-11-11-7f5ce3a\AUDIT.md), vérifié dans le code par la maître, ordre accepté par l'utilisateur (« oui ca me semble bon »)) : échange des découvertes sur GitHub : état de partage (gist, ticket, empreinte) lié au compte connecté, effacé au changement de compte ; échec d'effacement du jeton visible ; revoir la permission public_repo (moindre privilège) | Remplacé par X-175 le 12/11 (GitHub App aux permissions minimales, état de partage lié au compte du joueur) |
| X-146 | Audit A10 (audit GPT du 11/11 (C:\Plugins-ff14-audit-2026-11-11-7f5ce3a\AUDIT.md), vérifié dans le code par la maître, ordre accepté par… | Fait en 0.1214 |
| X-147 | Audit A14 (audit GPT du 11/11 (C:\Plugins-ff14-audit-2026-11-11-7f5ce3a\AUDIT.md), vérifié dans le code par la maître, ordre accepté par… | Fait en 0.1217 |
| X-148 | Fait en partie en 0.1215 : actions des workflows épinglées par empreinte (A16) ; A15 fait en 0.1120 (X-150). Reste, priorité basse : latest.zip de Dalamud vérifié par empreinte (goatcorp ne publie aucune empreinte, seulement AssemblyVersion : une empreinte gardée ici casserait la CI à chaque mise à jour de Dalamud, donc à décider) et CodeQL sur l'application de bureau (net10.0-windows : un job Windows, à essayer en suivant la CI) | Souhaitable avant l'annonce, rang 21 (tri du 13/11). Décision de l'utilisateur du 13/11 vers 11 h 10 : latest.zip de Dalamud non épinglé (Dalamud ne publie pas d'empreinte : une empreinte gardée casserait la CI à chaque mise à jour). Reste : CodeQL de l'application de bureau (job Windows), à essayer en suivant la CI. Avant l'annonce, priorité basse. |
| X-139 | Demande du 2026-11-11 : PluginUiTest (dépôt Shigu330/PluginUiTest) montre le rendu des fenêtres : image PNG de chaque fenêtre dessinée à côté du rapport, et en option une fenêtre d'aperçu à l'écran ; nouvelle version de l'outil | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire, dernière priorité |
| X-149 | revue GPT du 11/11 (C:\Plugins-ff14-plan-2026-11-11-2dddccb), vérifiée dans le code par la maître, acceptée par l'utilisateur (« Oui… | Fait en 0.1119 |
| X-150 | revue GPT du 11/11 (C:\Plugins-ff14-plan-2026-11-11-2dddccb), vérifiée dans le code par la maître, acceptée par l'utilisateur (« Oui… | Fait en 0.1120 |
| X-151 | revue GPT du 11/11 (C:\Plugins-ff14-plan-2026-11-11-2dddccb), vérifiée dans le code par la maître, acceptée par l'utilisateur (« Oui… | Fait en 0.1238 |
| X-152 | revue GPT du 11/11 (C:\Plugins-ff14-plan-2026-11-11-2dddccb), vérifiée dans le code par la maître, acceptée par l'utilisateur (« Oui, ajoute tout ») : messages d'état harmonisés dans toutes les pages (« en attente », « source désactivée », « donnée ancienne », « échec »), en français et en anglais, repli des langues partielles vérifié | Fondu dans X-154 le 12/11 (plan visuel accepté par l'utilisateur) |
| X-153 | revue GPT du 11/11 (C:\Plugins-ff14-plan-2026-11-11-2dddccb), vérifiée dans le code par la maître, acceptée par l'utilisateur (« Oui, ajoute tout ») : branchement et libération des services de Plugin.cs extraits par famille (bureau ou persistance d'abord), mesuré par les chemins qui installent ou libèrent un même service et les erreurs à la fermeture | Souhaitable avant l'annonce, rang 1 (tri du 13/11). Tranche 1 faite en 0.1225 (famille bureau, Plugin.Desktop.cs, DesktopFamily1225Tests) ; tranche 2 faite en 0.1239 (persistance : TenshoData, banque et son pont, Plugin.Persistence.cs, PersistenceFamily1239Tests) ; tranche 3 faite en 0.1240 (barre serveur, Plugin.ServerBar.cs, ServerBarFamily1240Tests) ; tranche 4 faite en 0.1249 (panneaux, Plugin.Panels.cs, PanelFamily1249Tests) ; tranche 5 faite en 0.1250 (découvertes : magasin, échange GitHub, chargement avec les prix vus, Plugin.Discoveries.cs, DiscoveriesFamily1250Tests) ; reste : lecteurs du jeu (≈ 35, entremêlés avec leurs services dans tout le constructeur : à découper par domaine, récolte, artisanat, compagnie, collections…, une tranche par domaine, même mesure ; le guetteur d'objets des découvertes avec eux) ; priorité basse (avant X-139) |
| X-154 | plan visuel GPT du 12/11 (C:\Plugins-ff14-plan-visuel-2026-11-12-3b1d717), vérifié par la maître, place acceptée par l'utilisateur (« Oui… | Fait en 0.1255 |
| X-155 | plan visuel GPT du 12/11 (C:\Plugins-ff14-plan-visuel-2026-11-12-3b1d717), vérifié par la maître, place acceptée par l'utilisateur (« Oui… | Fait en 0.1261 |
| X-156 | plan visuel GPT du 12/11 (C:\Plugins-ff14-plan-visuel-2026-11-12-3b1d717), vérifié par la maître, place acceptée par l'utilisateur (« Oui… | Fait en 0.1267 |
| X-157 | plan visuel GPT du 12/11 (C:\Plugins-ff14-plan-visuel-2026-11-12-3b1d717), vérifié par la maître, place acceptée par l'utilisateur (« Oui… | Fait en 0.1272 |
| X-158 | Demande de l'utilisateur du 12/11 (captures, Le Viaduc 020, Phare des Brasseurs 008, La Cité invisible 019) : carnet d'exploration… | Fait en 0.1128 |
| X-159 | Demande de l'utilisateur du 12/11 (chasse au trésor du 12/11 vers 0 h 35, sans portail ; relevé T0-18 fait) : le butin du coffre (3… | Fait en 0.1174 |
| X-160 | Demande de l'utilisateur du 12/11 : carte de Tensho, les ALÉA ont l'icône des favoris : reprendre l'icône du jeu de chaque ALÉA (selon son… | Fait en 0.1268 |
| X-161 | Demande de l'utilisateur du 12/11 : carte de Tensho seulement : icônes d'exploration un peu plus petites par défaut, icônes de monstres 50… | Fait en 0.1262 |
| X-162 | Demande de l'utilisateur du 12/11 (capture) : barre d'infos serveur, l'infobulle des gils (et parfois des alarmes) s'ouvre au-dessus alors… | Fait en 0.1129 |
| X-163 | Demande de l'utilisateur du 12/11 : les alarmes (rappels, jardinage et autres comprises) sonnent aussi dans l'application de bureau : jeu… | Fait en 0.1156 |
| X-164 | Demande de l'utilisateur du 12/11 (capture de la barre d'outils de la carte) : les icônes ne sont pas centrées dans leurs boutons ; pouvoir… | Fait en 0.1130 |
| X-176 | Demande de l'utilisateur du 12/11 (« une secondaire de plus, que pour le travailleur pour Tensho », « il doit être séparé du git ») : travailleur de fusion des découvertes sur le portable de l'utilisateur, projet à part, hors du dépôt Tensho (dossier propre, ex. C:\Tensho-travailleur, à côté du bot Discord dans C:\Serveur discord ff14 sans le modifier) : relève régulière des tickets « Discoveries » par l'API GitHub (sorties seulement, aucun port ouvert), vérifications de X-168 (gist de l'auteur du ticket, CC0 explicite, tailles bornées, sinon tri manuel), fusion à blanc par tools/discoveries-merge.py lu depuis le dépôt, journal dans un fichier, notification Discord (webhook à part, secret) ; puis, avec l'accord de l'utilisateur : git et gh installés (winget), jeton GitHub limité au dépôt, demande de fusion (PR) ouverte pour l'utilisateur, démarrage automatique du bot et du travailleur (tâche planifiée) ; état des lieux du portable du 12/11 : Windows 11 Pro, Python 3.12.10, veille désactivée, git et gh absents, bot lancé à la main sans journal | Souhaitable avant l'annonce, rang 22 (tri du 13/11). Phases 1 et 2 faites le 12/11 vers 4 h 20, d'après la session du portable (vérifié par la maître sur GitHub : aucune branche travailleur/*, aucune demande de fusion du travailleur, case CC0 cochée sur #43) : outil de fusion pris sur main, git et gh installés, jeton à grain fin limité au dépôt (expire le 10/02/2027), webhook Discord essayé, écriture GitHub active dans ses limites (branche travailleur/discoveries-<n>, demande de fusion vers Devloppement, commentaires bilingues, jamais de poussée sur main ni Devloppement, jamais de fusion), bot et travailleur en tâches planifiées au démarrage (vérifiées après redémarrage), journaux tournants, 35 tests verts ; #43 « rien de neuf ». Reste : une vraie écriture (attend le ticket d'un autre joueur), lecteur v3 quand X-174 sera fait, GitHub App quand X-175 le sera Phase 3 (13/11 vers 11 h 40, compte rendu du portable vérifié par la maître sur GitHub) : travailleur adapté à l'outil de main (89a0227), demande #46 verte puis fusionnée (9400249) sur un mot de l'utilisateur, discoveries.json et v3 à 2541 faits, plus de repoussée sans changement. |
| X-165 | Demande de l'utilisateur du 12/11 (« ta recommandation semble bien ») : tableau de bord de Tensho. Complet en mode développeur : mémoire et… | Fait en 0.1247 |
| X-166 | R01 (audit GPT du 12/11 (C:\Plugins-ff14-audit-2026-11-12-42a2db0), vérifié par la maître, accepté par l'utilisateur) : une découverte… | Fait en 0.1133 |
| X-167 | R03 (audit GPT du 12/11 (C:\Plugins-ff14-audit-2026-11-12-42a2db0), vérifié par la maître, accepté par l'utilisateur) : l'envoi des… | Fait en 0.1134 |
| X-168 | R04 (audit GPT du 12/11 (C:\Plugins-ff14-audit-2026-11-12-42a2db0), vérifié par la maître, accepté par l'utilisateur) : GitHub retire… | Fait en 0.1146 |
| X-169 | R05 et R06 (audit GPT du 12/11 (C:\Plugins-ff14-audit-2026-11-12-42a2db0), vérifié par la maître, accepté par l'utilisateur) : sauvegardes… | Fait en 0.1141 |
| X-170 | Compléments de l'audit GPT du 12/11 (C:\Plugins-ff14-audit-2026-11-12-42a2db0), vérifié par la maître, accepté par l'utilisateur, pour un… | Fait en 0.1145 |
| X-171 | Partie 1 faite en 0.1216 : prix vus (MarketSeen.json) écrits sous verrou et joints au fichier. Reste, priorité basse (avant X-139) : découvertes, une même clé vue par les deux clients (base SQLite depuis 0.1136 : clés distinctes gardées, même clé : le dernier qui écrit l'emporte, règle à fixer), cache de la banque entre deux clients, purge du cache partagé | Indispensable avant l'annonce (tri du 13/11, accepté par l'utilisateur). Règle décidée par l'utilisateur le 13/11 vers 11 h 10 (« Additionner les deux ») : découvertes à même clé, deux clients : chaque client ajoute ses propres observations (incréments) à ce qui est enregistré, rien perdu ni compté deux fois ; test de comportement à deux clients. Reste aussi : cache de la banque entre deux clients, purge du cache partagé. Avant l'annonce. |
| X-172 | Choix de l'utilisateur du 12/11 (« Oui, retiens tout » ; « je suis le seul a avoir des donnees… si il faut effacer c'est maintenant pas une… | Fait en 0.1149.0 |
| X-173 | Choix de l'utilisateur du 12/11 (« Oui, retiens tout » ; « je suis le seul a avoir des donnees… si il faut effacer c'est maintenant pas une… | Fait en 0.1149 |
| X-174 | Choix de l'utilisateur du 12/11 (« Oui, retiens tout » ; « je suis le seul a avoir des donnees… si il faut effacer c'est maintenant pas une… | Fait en 0.1137.0 |
| X-175 | Choix de l'utilisateur du 12/11 (« Oui, retiens tout » ; « je suis le seul a avoir des donnees… si il faut effacer c'est maintenant pas une… | Fait en 0.1161 |
| X-177 | Constat de la maître du 2026-11-12 (dalamud.log du jour) : saccades signalées par Dalamud (HITCH > 50 ms) : CompetitiveGate (rappel Guarded de Add) 44 fois, jusqu'à 193 ms, et ReminderDtrEntry.OnUpdate 12 fois, jusqu'à 82 ms, ensemble à chaque changement de zone ; mesurer, sortir le travail lourd de l'image (lecture paresseuse, cache par zone), test de comportement ; priorité moyenne | Indispensable avant l'annonce (tri du 13/11, accepté par l'utilisateur). Étape 1 (mesure) en 0.1162, étape 2 (réglages hors du fil) en 0.1164. Analyse du 12/11 (docs/ai/lenteurs-12-11.md) : CompetitiveGate n'est qu'une enveloppe (le lecteur voisin est le coupable), UiBuilder 300 à 460 ms vient surtout des rechargements (X-01 C1). Relais 35 : C6 la ligne « Nearby monsters » n'est plus construite hors du mode développeur (0.1189) ; C4.4 le temps de la fonction du jeu EquipGearset n'est plus compté au lecteur des tenues (0.1193). Relais 36 : C4.2 fait et mesuré (0.1198 à 0.1200). Les classes des lecteurs sont compilées sur un fil à part et chaque lecteur démarre une fois sa classe compilée (délai de garde 20 s) ; le runtime ne compilant qu'une méthode à la fois pour le plugin (lu en 0.1199 : trois fils qui compilent ensemble mettent autant de temps que l'un après l'autre, sur 32 processeurs), un seul fil de fond compile à la fois, dans l'ordre du besoin : code de l'interface, classes des lecteurs, puis le reste. Chargements voisins de la même session (vers le 90e rechargement) : « Load created » 2369 ms (0.1197), 5561 (0.1198), 3028 (0.1199), 1889 (0.1200) ; premiers passages lents des dix premières secondes 19 et 2141 ms, 3 et 197, 12 et 1073, 4 et 344 (BankInventoryBridge 68, ReminderDtrEntry 107, LocalLinkFeed 74, CraftActionCatalogLoader 95) ; première image de l'interface 254, 138, 596, 316 ms ; en 0.1200 les lecteurs démarrent entre 5 et 8,7 s après le chargement dans cette session (« Readers compiled on their own thread: 88 classes, 1839 methods in 3626 ms, the last one ready 8740 ms »). Reste : C4.1 remesurer en session neuve ; première image de l'interface (316 ms : commencer la première partie de l'échauffement plus tôt, ou les panneaux avant la carte) ; ce que les quatre lecteurs encore lents appellent ailleurs ; un passage lent tardif attend derrière le fil d'échauffement (CollectionReader 201 ms « compiling 1 methods ») ; reste de C6 (lignes répétées « Submarine voyage check », « Reading eventMarkers », « Map event marker » ; « drop data not loaded » à voir avec l'utilisateur) Décision de l'utilisateur du 13/11 vers 11 h 10 (C4.2) : panneaux et barres libres dessinés seulement quand leur part de l'échauffement est compilée (panneaux d'abord, carte ensuite, borne 2 à 3 s) : plus de gel de 350 ms au chargement, panneaux 1 à 2 s plus tard ; faisable par Opus (retiré de points-fable.md). |
| X-178 | Demande de l'utilisateur du 2026-11-12 (capture de Jardinage › Journal, Shirogane 2-17 potager 1) : refaire la vue d'un jardin en icônes… | Fait en 0.1259 |
| X-179 | Constat de l'utilisateur du 2026-11-12 (« entretien manuel ne met pas à jour les alarmes, je pense pas que l'entretien soit détecté ») : le… | Fait en 0.1165 |
| X-180 | Revue N1 (haute) : revue de la nuit du 12/11 (relais/revue-nuit-12-11.md, dossier de la maître ; 31 constats vérifiés par réfutation… | Fait en 0.1168 |
| X-181 | Revue N2, N7, N8, N29, N30 : revue de la nuit du 12/11 (relais/revue-nuit-12-11.md, dossier de la maître ; 31 constats vérifiés par… | Fait en 0.1171 |
| X-182 | Revue N3, N4, N9, N11, N26, N27, N28 : revue de la nuit du 12/11 (relais/revue-nuit-12-11.md, dossier de la maître ; 31 constats vérifiés… | Fait en 0.1173 |
| X-183 | Revue N5, N12, N13, N14, N16, N17 : revue de la nuit du 12/11 (relais/revue-nuit-12-11.md, dossier de la maître ; 31 constats vérifiés par… | Fait en 0.1210 |
| X-184 | Revue N6, N21, N15, N20, N22 : revue de la nuit du 12/11 (relais/revue-nuit-12-11.md, dossier de la maître ; 31 constats vérifiés par… | Fait en 0.1209 |
| X-185 | Revue N10, N18, N19, N23, N24, N25 : revue de la nuit du 12/11 (relais/revue-nuit-12-11.md, dossier de la maître ; 31 constats vérifiés par… | Fait en 0.1213 |
| X-186 | Choix de l'utilisateur du 2026-11-12 (« Oui, ignore-les », constat N31 de la revue de la nuit, relais/revue-nuit-12-11.md) : format… | Fait en 0.1170 |
| X-187 | Demande de l'utilisateur du 2026-11-12 : tri visuel des sacs par Tensho (Inventory/InventoryVisualSort.cs… | Fait en 0.1172 |
| X-188 | Demande de l'utilisateur du 2026-11-12 (sur l'île de Lunule, « le tous » : les quatre aides proposées ; suite du point LUNULE ; relevé… | Fait en 0.1178 |
| X-189 | Demande de l'utilisateur du 2026-11-12 (sur l'île de Lunule, « le tous » : les quatre aides proposées ; suite du point LUNULE ; relevé… | Fait en 0.1179 |
| X-190 | Demande de l'utilisateur du 2026-11-12 (sur l'île de Lunule, « le tous » : les quatre aides proposées ; suite du point LUNULE ; relevé relais/lunule-12-11.log ; Occult Crescent Helper et Phantom Job Switcher sous AGPL : lecture seule, jamais copiés ; R03 : montrer et conseiller, jamais jouer à la place du joueur) : progression du savoir occulte (niveau, 465/2800 jusqu'au suivant) et des pièces d'argent et d'or (ce qu'on peut acheter avec, d'après les feuilles du marchand de l'île) ; progression des archives occultes dans les Collections (CollectionKind.OccultLore existe déjà : ajouter le compte et ce qui manque) ; tests. Priorité : avec X-188 | Souhaitable avant l'annonce, rang 4 (tri du 13/11). En grande partie en 0.1178 : bouton par pièce de l'île → Rangements › Que faire avec ? ; « Archives occultes lues : N / M » et « Voir ce qui manque » ; pièces dans le panneau de l'île. Reste : le niveau de savoir (aucun champ de l'état du jeu : KnowledgeLevelSync vaut 0 à 465/2800 ; MKDGrowData ne contient pas 2800 ; preuve 0.1178-occult-panel.md) : un relevé en jeu de plus (niveau affiché par le jeu contre le savoir) dira d'où le tirer |
| X-191 | Constat de l'utilisateur du 2026-11-12 (« État 81 % Symbiose 0 % », sur l'île de Lunule) ; précisé par lui : son équipement est à 1 % dans… | Fait en 0.1177 |
| X-192 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-01, accepté le 12/11) : livraisons expertes de la Grande Compagnie : à l'onglet expert, pour chaque objet : sceaux, pièce d'un set, va à l'armoire, valeur au marché et mise en garde « vaut plus en gils », sceaux restants avant le plafond du grade ; ligne « sceaux » dans le panneau sous l'infobulle ; hors fenêtre, liste des sacs « estimée » tant que la règle d'admission n'est pas relevée ; rien n'est rendu à la place du joueur | Souhaitable avant l'annonce, rang 9 (tri du 13/11). À faire : sonde de l'onglet expert (AddonGrandCompanySupplyList), nouveau lecteur ; GCSupplyDutyReward, GrandCompanyRank ; règle d'admission déclarée inconnue tant que non relevée |
| X-193 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-02, accepté le 12/11) : planificateur de ventures : pour chaque servant (métier, niveau, statistiques si connues), les ventures ciblées possibles, quantité à chaque palier, valeur au marché au clic, gain par venture comparé à l'exploration rapide (butin vu dans les Découvertes) ; le joueur choisit dans le jeu Terme de l'utilisateur : « tâches des servants » (pas « ventures »). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : feuilles RetainerTask, RetainerTaskNormal, RetainerTaskParameter, RetainerTaskRandom ; statistiques du servant lues servant ouvert ; relevé en jeu des seuils de palier |
| X-194 | Choix de l'utilisateur déjà fait (GAPS M-13), retrouvé le 2026-11-12 par la comparaison aux plugins (K-03) : solveur Faux Hollows (mini-jeu hebdomadaire d'Idyllshire) : case la plus utile surlignée sur le plateau 6×6, comme le Mini Cactpot ; rappel hebdomadaire dans Rappels et l'état des personnages | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : AddonWeeklyPuzzle (plateau), PlayerState (FauxHollowsState, FauxHollowsTimestamp) ; motifs énumérés par Tensho ou source citée ; « inconnu » si un motif manque ; relevé en jeu du plateau |
| X-195 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-04, accepté le 12/11) : pêche : son (et ligne facultative) à la touche, différent pour !, !! et !!!, réglage par force ; lu en mémoire comme en 0.887, sans paquet réseau ; le joueur ferre lui-même | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : FishingHookStrength (292 à 294) déjà lu ; sons des alarmes ; vérifier « !!! » en jeu (séance C30) |
| X-196 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-05, accepté le 12/11) : tableau des servants de tous les personnages : métier, niveau, gils, objets en vente, fin des ventes avec rappel avant expiration, venture en cours, ville du marché ; ferme le rappel #12 | Souhaitable avant l'annonce, rang 10 (tri du 13/11). À faire : RetainerManager.Retainer (ClassJob, Level, Gil, MarketItemCount, MarketExpire, VentureId, VentureComplete, Town) lu après ouverture de la liste ; copie par personnage comme CharacterStatus ; relevé #12 |
| X-197 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-06, accepté le 12/11) : panneau sous l'infobulle : ligne « désynthèse » (métier, niveau de l'objet, votre niveau, au-dessus ou en dessous) et ligne « coffret » (tout obtenu ou il manque n), chacune coupable L'utilisateur précise (12/11) : « on a déjà désynthèse et les paquets de cartes » : vérifier par Grep ce que le panneau montre déjà et n'ajouter que ce qui manque (niveau comparé, autres coffrets). | Souhaitable avant l'annonce, rang 11 (tri du 13/11). À faire : Item.LevelItem, Item.ClassJobRepair, PlayerState.GetDesynthesisLevel ; ItemSupplement (LuminaSupplemental) croisé avec les collections ; calcul une fois par objet |
| X-198 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-07, accepté le 12/11) : tâches personnelles avec condition, cochées quand Tensho l'observe : avoir N × objet (tous rangements), monnaie ≥ N, mission terminée, monture/tenue/carte obtenue ; « inconnu » tant que la donnée n'a pas été lue | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : PersonalTasks + ItemHoldings, Currencies, DutyProgressReader, état des collections ; ALÉA exclues |
| X-199 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-08, accepté le 12/11) : Parcelles : phase de la loterie (inscriptions ou résultats) et sa fin, lues dans PaissaDB ; bouton « J'ai participé » qui pose une alarme à l'ouverture des résultats ; vérifier la couverture des mondes européens (le code dit NA seulement) L'utilisateur joue sur Chaos (Europe) : vérifier que PaissaDB couvre Chaos et corriger « serveurs NA seulement » dans Community/PaissaDb.cs. | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : afficher LottoPhase et PhaseUntil (déjà lus) ; alarme Tensho ; vérifier Chaos et Light dans PaissaDB ; N-04 reste pour la lecture dans le jeu |
| X-200 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-09, accepté le 12/11) : sous-marins : chemin de déblocage d'un secteur visé, comme pour les aéronefs | Souhaitable avant l'annonce, rang 16 (tri du 13/11). À faire : SubmarineExploration + LuminaSupplemental SubmarineUnlock (GPL-3.0, crédité) ; même modèle qu'AirshipSectors.UnlockPath |
| X-202 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-11, accepté le 12/11) : Exploration cosmique, page d'information : missions par métier et planète (rang, type), seuils bronze/argent/or, récompenses, prochaines fenêtres des missions horaires et météo, progression lue ; aucune alerte rouge promise | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : feuilles WKSMission*, WKSCosmoTool* ; WKSManager à sonder ; conditions d'heure et de météo à relever en jeu |
| X-203 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-12, accepté le 12/11) : Restauration d'Ishgard (objets par niveau, paliers, récompenses) et Diadème (objets à récolter par niveau, points, compte de la session) | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : feuilles HWDCrafterSupply*, HWDGathererInspection* ; aucune prévision de météo d'instance |
| X-201 | Demande de l'utilisateur du 2026-11-12 (« ajoute un calculateur de trajet auto aux dirigeables ») : le planificateur des aéronefs… | Fait en 0.1248 |
| X-204 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, relais/comparaison-plugins-12-11.md ; contenus joués par l'utilisateur… | Fait en 0.1235 |
| X-205 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, relais/comparaison-plugins-12-11.md ; contenus joués par l'utilisateur… | Fait en 0.1241 |
| X-206 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-13, accepté le 12/11) : journal de session : objets obtenus aujourd'hui ou cette session avec leur source (non attribué dit tel quel), filtre, export ; durée de chaque mission terminée, montrée après la fin, historique Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : vue sur ItemGains et GainSources ; IDutyState pour la durée ; TenshoData.db ; avec X-159 ; choix de l'utilisateur du 12/11 : historique des durées gardé d'une séance à l'autre |
| X-207 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-14, accepté le 12/11) : valeur estimée des stocks par rangement et par personnage, liste « dort et se vend » (prix vendu × ventes par jour, prix PNJ à côté), ligne PNJ face au marché sous l'infobulle ; conseil seulement, au clic Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 12 (tri du 13/11). À faire : banque + MarketService par lots + MarketQuote ; NpcVendorCatalog.GilPrice ; prix anciens signalés |
| X-208 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-16, accepté le 12/11) : « Mes annonces » : listes de vente des servants gardées, depuis quand, prix, « vendu depuis la dernière visite » (annonce disparue + hausse des gils) ; Universalis au clic seulement (D-82) ; CSV facultatif Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : SellListEntries gardées par servant ; croisement avec GilJournal.SplitRetainerRise ; après X-171 |
| X-209 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-18, accepté le 12/11) : import d'une liste Teamcraft (lien d'import ou texte collé) en liste d'artisanat Tensho, complément de l'export A-09 ; aucune requête Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : décodage base64 « itemId,recipeId,quantité ; … » (format MIT déjà lu) ; bornes 32 × 64 ; message si le format change |
| X-210 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-19, accepté le 12/11) : pêche en mer : appât conseillé à l'escale selon l'état (courant, intuition, missions, poissons à points) avec la raison et la source (DEC-044), jamais « garanti » ; historique local des traversées Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : IKDSpot, IKDRoute, IKDFishParam, IKDContentBonus, données GatherBuddy ; OceanVoyageRecord gardé par traversée ; séance C30 |
| X-211 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-20, accepté le 12/11) : Trésor du Mog : récompenses voulues cochées, mémoquartz irréguliers restants à gagner, déjà obtenu Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 18 (tri du 13/11). À faire : SpecialShop de l'événement (couches de fête), monnaie, IUnlockState ; relevé à la prochaine édition |
| X-212 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-21, accepté le 12/11) : export CSV de la banque (tous rangements ou un personnage) et copie de l'historique des prix vus d'un objet Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 13 (tri du 13/11). À faire : même forme que VoyageHistory.Csv ; SeenPrices |
| X-213 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-23, accepté le 12/11) : ALÉA partagés : rang par zone, progression vers le suivant, récompenses restantes, lus quand la fenêtre du jeu a été ouverte Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 15 (tri du 13/11). À faire : AgentFateProgress (CurrentRank, MaxRank, FateProgress, NeededFates) ; copie gardée par personnage |
| X-214 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-24, accepté le 12/11) : bouton « Ouvrir dans l'outil de mission » sur toute mention d'un contenu, sans inscription ; contenus du Livre d'or à faire dans le panneau de l'outil de mission ; « Y aller » seulement où le jeu donne une position (D-12) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : AgentContentsFinder.OpenRegularDuty ; WeeklyBingoOrderData ; désactivé pendant un contenu |
| X-215 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-25, accepté le 12/11) : Livraisons : objets demandés cette semaine par chaque client, bonus, récompense au palier visé, lien recette ou récolte ; lus à la fenêtre du client puis gardés pour la semaine ; « inconnu » sinon Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 17 (tri du 13/11). À faire : relevé de la fenêtre du client ; SatisfactionSupply, SatisfactionNpc ; calcul par SupplySeed seulement sur décision (X-75.6, DEC-043) |
| X-216 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-26, accepté le 12/11) : mirage : « même modèle » sur toute fiche d'objet (étend SameLook) avec ✓/✗ et emplacement ; collection « Mirage » pièce par pièce (coffre, armoire, inventaires, servants) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 14 (tri du 13/11). À faire : Item.ModelMain/ModelSub/EquipSlotCategory, MirageManager ; vue Collections |
| X-217 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-30, accepté le 12/11) : entrées facultatives de la barre du serveur… | Fait en 0.1226 |
| X-218 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-32, accepté le 12/11) : niveaux des métiers et sets de chaque personnage… | Fait en 0.1228 |
| X-219 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-34, accepté le 12/11) : fiche de monture enrichie  Vérifier d'abord par… | Fait en 0.1229 |
| X-220 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-36, accepté le 12/11) : une recherche commune dans la fenêtre… | Fait en 0.1230 |
| X-221 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-37, accepté le 12/11) : expérience restante de la Grâce, « inconnu » si le début n'a pas été vu  Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 5 (tri du 13/11). Partie 1 faite en 0.1231 (sonde : plafond seulement dans la description des manuels, preuve 0.1231-grace-cap.md ; relevé des statuts 45 et 46 en mode développeur) ; reste : lire en jeu le param du statut, puis afficher |
| X-222 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-38, accepté le 12/11) : compteurs des conditions de rang S  Vérifier… | Fait en 0.1232 |
| X-223 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-39, accepté le 12/11) : heures du prochain GATE et du tirage, alarme… | Fait en 0.1233 |
| X-224 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-40, accepté le 12/11) : patch de sortie dans la fiche objet si connu… | Fait en 0.1227 |
| X-225 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-46, accepté le 12/11) : rappel de fin des roues éthérées  Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 6 (tri du 13/11). Partie 1 faite en 0.1234 (sonde : 20, 45, 70 h par grade, preuve 0.1234-aetherial-wheels.md ; fenêtre « Wheel » lue en mode développeur) ; reste : lire en jeu la fenêtre du support, puis le rappel |
| X-226 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-15, accepté le 12/11; accord R04 de l'utilisateur le 12/11, pour et contre donnés) : rappel de préparation hors combat : repas fini ou presque, compagnon chocobo absent ou presque ; chat et son comme l'usure (D-38), rien en combat ni dans une mission ; repas de combat seulement si l'utilisateur l'accepte (R04) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 19 (tri du 13/11). À faire : statut 48 (RemainingTime), Buddy.CompanionInfo.TimeLeft, ICondition ; seuils réglables |
| X-227 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-17, accepté le 12/11; accord R04 de l'utilisateur le 12/11, pour et contre donnés ; nouvel hôte api.xivgear.app accepté) : import d'un lien xivgear dans le planificateur d'équipement (pièces et matérias), pièces et matérias manquantes encadrées dans les rangements (X-54) et signalées sous l'infobulle ; requête sur clic seulement Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : client xivgear borné (BoundedHttp), interrupteur « données du jeu seulement », crédits, SECURITY ; Etro en second si encore servi |
| X-228 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-22, accepté le 12/11; accord R04 de l'utilisateur le 12/11, pour et contre donnés) : notifications quand le jeu est en arrière-plan (application de bureau, Discord ou clignotement de la barre des tâches, au choix par événement) : déconnexion, groupe complet, alarme, fin de synthèse, retour de venture ou de vaisseau ; message privé seulement « un message de X » (R04 accepté le 12/11), texte sur option explicite ; rien sur l'entrée en combat Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : IClientState, LogMessage (groupe complet) à relever, IChatGui TellIncoming, FlashWindowEx ; canaux X-163 et DiscordNotifier |
| X-229 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-31, accepté le 12/11; accord R04 de l'utilisateur le 12/11, pour et contre donnés) : repas dans le planificateur, stats au niveau synchronisé quand la formule est prouvée  Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Souhaitable avant l'annonce, rang 20 (tri du 13/11). À faire : ItemFood ; formule déclarée inconnue tant que non prouvée |
| X-230 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-42, accepté le 12/11; accord R04 de l'utilisateur le 12/11, pour et contre donnés) : plaques gardées dans Tensho, appliquées sur clic  Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : agent des plaques |
| X-231 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-43, accepté le 12/11; accord R04 de l'utilisateur le 12/11, pour et contre donnés) : alertes sur mots-clés du chat  Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : IChatGui, rien gardé |
| X-232 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-44, accepté le 12/11; accord R04 de l'utilisateur le 12/11, pour et contre donnés) : résumé du journal du coffre par objet  Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : sonde de la fenêtre |
| X-233 | Demande de l'utilisateur du 2026-11-12 (comparaison aux plugins, K-47, accepté le 12/11; accord R04 de l'utilisateur le 12/11, pour et contre donnés) : statut Discord facultatif  Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire : application Discord, effacé pendant la coupure |
| X-234 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : vue « poissons ouverts maintenant et bientôt », triée par prochaine fenêtre (GatherBuddy) ; D-54 b rouverte : calcul par lots hors de l'image | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-235 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : arbre complet d'une liste d'artisanat (sous-recettes, récolte) ; D-14 b rouverte | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-236 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : doublons d'un objet entre rangements, et où le regrouper ; D-67 b rouverte | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-237 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : alerte du rôle bonus des missions aléatoires, lue sans paquet réseau ; D-59 b rouverte | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-238 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : monnaies, carnets et tenues de tous les personnages ; D-40 b, D-21 b, D-53 b rouvertes | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-239 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : points d'apparition des marques de chasse sur la carte ; D-10 rouverte | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-240 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : coordonnées écrites en texte dans le chat transformées en lien de carte ; D-41 rouverte | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-241 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : dates des fêtes saisonnières à venir, chaque date citant l'annonce du Lodestone, fichier tenu dans le dépôt ; C-06 rouverte ; l'utilisateur demande de voir d'abord si l'outil des événements ne le fait pas déjà | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-242 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : sorte d'assignat de chaque collectionnable et alerte de plafond ; D-19 b rouverte ; une sonde d'abord | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-243 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : tickets du Mini et du Jumbo Cactpot, cibles du Carnaval masqué ; D-32 rouverte ; une sonde d'abord | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-244 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : rappel « réparer » au retour d'un sous-marin ; D-43 b rouverte ; trouver d'abord la donnée | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-245 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : score estimé de la Revue de mode ; DEC-012 rouverte : seulement avec une source réelle citée, jamais inventé | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-246 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : surlignage dans l'agenda de l'atelier de l'île sanctuaire ; C23 et DEC-033 rouvertes (dessin sur l'interface du jeu) | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-247 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : notification Windows « mission trouvée » ; D-49 b rouverte (avec X-163 et K-22) | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-248 | Choix de l'utilisateur du 2026-11-12 (comparaison aux plugins, section 5 : idée écartée autrefois, rouverte) Vérifier d'abord par Grep ce qui existe déjà (demande de l'utilisateur du 12/11). : import au clic de liens de sites tiers (par exemple pour les macros) ; D-15 b rouverte (avec K-17) | Après l'annonce, avant la 1.0 (tri du 13/11, accepté par l'utilisateur). À faire |
| X-250 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan.… | Fait en 0.1180 |
| X-251 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan.… | Fait en 0.1181 |
| X-252 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan.… | Fait en 0.1182 |
| X-253 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan.… | Fait en 0.1183 |
| X-254 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan. G1… | Fait en 0.1219 |
| X-255 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan. G4… | Fait en 0.1219.0 |
| X-256 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan. G5… | Fait en 0.1222 |
| X-257 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan. G6… | Fait en 0.1222.0 |
| X-258 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan. G7… | Fait en 0.1220 |
| X-259 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan. G8… | Fait en 0.1221 |
| X-260 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan. G9… | Fait en 0.1223 |
| X-261 | Revue « premier joueur » du 12/11 (docs/ai/premier-joueur-12-11.md), avant l'annonce publique, choix de l'utilisateur : tout au plan. G11… | Fait en 0.1220.0 |
| X-262 | Choix de l'utilisateur du 13/11 vers 13 h 20 (après le Palais des morts 1-10, « actif mais si une carte de l'instance specifique il faut coupe la map tant qu'on est dedans ») : les donjons profonds (type de contenu 21) sortent de la coupure de DEC-038 (CompetitiveZones.Build sans DeepDungeonContentType) ; la carte de Tensho est coupée (carte du jeu laissée) dans une instance à carte spécifique, repérée par les feuilles plutôt que par une liste ; JcJ, Triple Triade et Mahjong restent coupés ; DEC-038 et ARCHITECTURE (« Competitive switch-off ») mis à jour, messages en et fr, test de comportement, essai au parcours de la campagne (entrer dans le Palais des morts) | Indispensable avant l'annonce (tri du 13/11, accepté par l'utilisateur). À faire, relais Opus |
| X-263 | Demande de l'utilisateur du 13/11 vers 12 h 40 (capture « Vu en jeu : Rondin de zelkova ← tâche n° 584 ») : voir le nom de la tâche de servant plutôt que son numéro. DiscoveryVentureName (comme DiscoveryDutyName) : tâche d'exploration → nom de RetainerTaskRandom ; tâche normale → classe et niveau (« tâche de bûcheron niv. 70 ») ; numéro seulement si la feuille ne donne rien ; MainWindow.Discoveries.cs (DiscoveryKind.Venture) et DiscoveriesUi.FromVenture en et fr ; test ; application de bureau (R33) | Indispensable avant l'annonce (tri du 13/11, accepté par l'utilisateur). À faire, petit point, relais Opus |
| X-264 | Demande de l'utilisateur du 13/11 vers 13 h 35 (capture de référence d'un tableau de type Grafana, « aucun rapport avec le plugin », gardée… | Fait en 0.1271 |
| X-265 | Demande de l'utilisateur du 13/11 vers 15 h 05 (« prevoir un tri des peuples allies par genre (combat artisanat recolte) ») : page Peuples alliés : tri ou regroupement par genre : combat, artisanat, récolte (filtre ou en-têtes de groupe, choix gardé). Genre tiré des feuilles du jeu (classes des quêtes du peuple, ClassJobCategory) plutôt que d'une liste écrite à la main ; un peuple mixte (artisanat et récolte) dans les deux ou sous « mixte » ; en et fr ; test ; application de bureau (R33) | Indispensable avant l'annonce (tri du 13/11, accepté par l'utilisateur). À faire, petit point, relais Opus |
| X-266 | Signalement de l'utilisateur du 13/11 vers 15 h 30 (Mor Dhona, capture ; diagnostic relais/diagnostic-pnj-absents-13-11.md) : la carte de Tensho montre des marchands absents en jeu : position Level « Always » d'un PNJ placé seulement par une couche de quête (Cristiana 1016290, couche QST_JobRel101, quête 67748 pas prise ; 31 cas dans le jeu). Corrections : « QST_ENPC » sans tenir compte de la casse ; position Level dont toutes les couches du territoire sont des couches de quête → montrée seulement si la quête liée (script commençant par le nom de la couche) est en cours ou faite, cachée sinon ; fonction pure NpcShopLinks, tests (FestivalTests), CopyRules monté. Les deux « ! » vus à côté sont de vraies quêtes présentes en jeu (confirmé par l'utilisateur) : pas un bug | Indispensable avant l'annonce (tri du 13/11, accepté par l'utilisateur). À faire, relais Opus |
| X-267 | Outillage 1 (PLAN-METHODE chantier 3, R36) : tous les tests à chaque version mais en parallèle (par classe, dossier temporaire propre à… | Fait en 0.1275 |
| X-268 | Outillage 1 (R20.3, R19.3, R23, R28.12) : la dernière version d'un relais rendue prête à publier par le relais (docs.py gen et commit «… | Fait en 0.1276 |
| X-269 | Outillage 1 (R28.10, R28.9, R26) : script de retour sans modèle (contrôle git et CI de tous les commits du relais, points validés ou… | Fait en 0.1277 |
| X-270 | Outillage 1 (audit du 13/11) : démarrage juste : message de démarrage sans ligne périmée (référence du patch refaite après 7.58, horodatage… | Fait en 0.1277 |
| X-271 | Outillage 2 (chantier 4, R37) : images de référence des pages dessinées hors du jeu (thèmes, tailles, états vide, chargement, erreur, jardin d'essai), comparées à chaque version avec tolérance (SSIM) ; un changement voulu produit une image à approuver ; galerie avant / après pour l'utilisateur ; essais en jeu couverts par une image retirés de sa liste. Fait quand : une page changée sans raison fait échouer la publication (essai), la galerie s'ouvre, le compte des essais retirés est donné. | Indispensable avant l'annonce (plan de méthode du 13/11, accepté par l'utilisateur). À faire |
| X-272 | Outillage 2 (R34, R13.2, R02.2, R27.2, R28.16, R28.5, R28.7) : essais par lots (q.py lot : essais d'un bloc par lieu, 15 à 20 min) et plafond de 15 versions jamais vues en jeu ; boîte à idées avec redites reconnues et comptées ; classe de tri dans plan.jsonl et critères d'annonce vérifiés ; consignes du relais et du relecteur produites avec leurs rappels ; garde des écritures de la maître pendant qu'une secondaire vit. Fait quand : chaque commande essayée, plafond refusant une version qui n'est pas une correction (essai). | Indispensable avant l'annonce (plan de méthode du 13/11, accepté par l'utilisateur). À faire |
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
| X-131 | Abandonné le 2026-11-11 à la demande de l'utilisateur (« laisse tomber x131 »), après le pour et le contre (règles Dalamud, conditions de Square Enix, perte d'objets) |
<!-- /gen:plan:dropped -->
