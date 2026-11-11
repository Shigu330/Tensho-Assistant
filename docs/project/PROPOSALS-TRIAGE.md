# Triage of audit proposals

> *Traduction française plus bas.*

**Frozen snapshot of 2026-11-05, no longer updated.** Its class « After 1.0.0 » is replaced by the [global plan](GLOBAL-PLAN.md),
where nothing comes after 1.0.0, and most of its points are done since; the current state of each point is in the global plan.

Triage of 2026-11-05, at version 0.539.0, of the 95 proposals left open in [MAP-PROPOSALS](MAP-PROPOSALS.md), [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md), [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md) and [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md). The detail of each proposal stays in those documents, under the same number.

**Method and limits.** The status comes from a search in the code and in the interface texts, without reading every function or trying in game: a « not done » may be partly done. Value and size (S, M) are the audits' estimates. The ranking is a recommendation by the agent: nothing is decided until the user has chosen. None of these proposals is required for 1.0.0 ([PUBLISHING](PUBLISHING.md)).

**Review done with the user on 2026-11-05: their decisions are in the [global plan](GLOBAL-PLAN.md), which takes precedence over the recommendations below.** No code until they have re-read that plan.

| Class | Count | Recommendation |
|---|---|---|
| [Already done](#already-done) | 3 | Marked done |
| [Batch 1: high value, small](#batch-1-high-value-small) | 8 | To do first |
| [Batch 2: medium value, small](#batch-2-medium-value-small) | 10 | Next |
| [Batch 3: high value, medium](#batch-3-high-value-medium) | 12 | One by one, as the mood takes |
| [To probe in game first](#to-probe-in-game-first) | 21 | A reading session before coding |
| [After 1.0.0](#after-100) | 33 | Kept as ideas |
| [To discard](#to-discard) | 8 | Do not do, unless advised otherwise |

## Already done

| No. | Proposal | Evidence in the code |
|---|---|---|
| C8 | GP plan of the gathering panel: quantity and effects | Plan per node and effects of the place (0.515 to 0.517, `Gathering/GatherNodeEffects.cs`) |
| C34 | Supply missions of the day | `Items/GcSupplyToday.cs` (0.469) |
| P14 | Measure the display time of the map | « Map » line of the diagnostics (0.489, `MapViewWindow.cs`); any lightening remains to be decided from measurements |

## Batch 1: high value, small

Size S, no probe before coding.

| No. | Proposal | Why first |
|---|---|---|
| P1 | Map zoom at opening, adjustable and kept | Gesture repeated at each opening |
| P2 | Label size separate from icon size | Readability of the map |
| I0 | Count worn pieces in « owned » | Fixes a wrong count; to check in game afterwards |
| C2 | Try your saved sequences on all recipes | Reuses the simulator |
| C3 | Simulate another craft without wearing it | Avoids changing job to simulate |
| C4 | Attribute thresholds of a sequence | Says whether a macro works with your gear |
| T3 | Quests: Level and Zone columns, sort and filter | Data already read |
| T4 | Monsters: flag on the monster's position | Avoids a flag at the centre of the map |

## Batch 2: medium value, small

Size S, no probe.

| No. | Proposal |
|---|---|
| P5 | Adjustable map wheel |
| P20 | « Layers » button: show or hide everything |
| C19 | Where are my materials, per storage |
| C24 | Remaining preparation need in the gathering and fishing helpers |
| C31 | Weather of several zones in one table |
| C32 | Commands « /tensho recolte » and « /tensho recette » |
| I16 | Commands « /tensho objet » and « /tensho prix » |
| T17 | Reminders ticked by themselves when the game says so |
| T19 | Alarms: edit the name and the time, « Remind me again after 5 min » |
| T20 | Alarms: also ring at the exact time |

## Batch 3: high value, medium

| No. | Proposal | Note |
|---|---|---|
| I1 | Flag and erase the reading of a dismissed retainer | Size S to M |
| I3 | NPC vendor prices in the preparation cost | Size S to M; a check in game on a known item |
| C0 | Target quality in the log panel | Partly (0.419) |
| C5 | Follow your sequence during crafting | Information only, without playing any action |
| C7 | Gathering points open now or soon | Partly (0.415) |
| C9 | « Possible now? » in the fishing helper | Partly (0.396) |
| T0 | Real-time alarms: every day, every week, at each reset | Partly: `AlarmRepeat` only has Once and EorzeaDaily |
| T10 | Home: « Today » block | One decision: which lines by default (recommended: all, each can be hidden) |
| T11 | Quests available now, quests of the zone | A column of the Quest sheet to probe |
| T12 | Path to a quest: remaining prerequisites | |
| T14 | Gear: prepare the plan of another job | |
| T15 | Fashion Report: outfit of the week, tried on in one click | On click only |

## To probe in game first

A piece of data is not proven: no code before the reading (project rule, no invented data). To be grouped with [issue #23](https://github.com/Shigu330/Tensho-Assistant/issues/23).

| Domain | No. |
|---|---|
| Map | P6 unit of distances; P7 non-attuned aetherytes; P11 undiscovered parts; P17 leve markers; P19 map tests; P22 quest icon variants |
| Items and market | I4 Universalis requests in batches; I10 retainers' crystals |
| Crafting and gathering | C6 level-adjusted recipes and Cosmic Exploration; C13 HQ chance; C22 workshop projects; C27 class of the point; C28 duration of Fisher's Intuition; C29 spearfishing in the Gathering page; C30 reading of catches; C33 reading session |
| Other tools | T5 reading session; T13 « Unlocked by »; T21 experience of all jobs; T26 last airship voyage; T27 company actions |

## After 1.0.0

Useful without being a priority. « Partly »: a first version exists.

| Domain | No. |
|---|---|
| Map | P3 aetheryte nearest to the flag (partly); P9 FATE; P10 search on the map (partly, Maps page); P12 names next to icons (partly: place names, gathering labels); P15 state of the readings; P16 left click for the flag |
| Items and sorting | I5 data center price; I8 Universalis, Garland, Teamcraft links (partly); I9 copy the name, item link; I11 Bank as a sortable list; I14 undo the last sort; I15 share a sort template; I18 price minus 1 from the Market page (partly); I19 English name; I20 moved cells in the preview; I21 job filter in the rules; I22 computations at every frame |
| Crafting and gathering | C12 initial quality according to HQ ingredients (partly, typed value); C14 complete recipe sheet (partly); C15 more direct simulator; C16 sequence library (partly); C17 export options (partly); C18 production chain; C21 « What can I craft? »; C35 fishing helper outside Draw; C36 an unreadable helper says so |
| Other tools | T16 one line per retainer (partly); T18 Discord tags (partly); T23 vessels per company; T24 one Discord channel per category; T25 named plans per job; T28 recently obtained; T29 visible Mini Cactpot table (partly) |

## To discard

| No. | Proposal | Reason |
|---|---|---|
| P21 | Keyboard shortcuts in the map | Low value, risk of conflict with the game's keys |
| P23 | Close friends on the map | Low value; shows other players, to avoid before the Dalamud review |
| P24 | Teleport command by name | Low value; « Go there » covers the need |
| P25 | Tensho mini-map | Second map to maintain, the game has one |
| P26 | Position highlighted when hovering over the lists | Low value |
| P27 | Adjustable colour per icon | Low value |
| C10 | Bite timer at each cast | Requires a probe, and the existing fishing plugins already do it. 2026-11-09: nothing more after 1.0.0; the probe existed (0.887): done in 0.1011 |
| C23 | Next action on the hotbars | Drawing on the game's interface at every frame, sensitive to updates; C5 gives the information without that risk |

## Full list

Everything left to review for the global plan, all at once. The « Recommendation » column takes up the classes above; nothing is decided.

### Fixes

| Where | What is missing | Issue |
|---|---|---|
| Garden | Unrecognised upkeep and harvest messages, then capture log lines to remove | [#36](https://github.com/Shigu330/Tensho-Assistant/issues/36) |
| Garden | The Bleuets are missing from the seed list | [#36](https://github.com/Shigu330/Tensho-Assistant/issues/36) |
| Squadron | No alarm button for a mission already in progress | [#37](https://github.com/Shigu330/Tensho-Assistant/issues/37) |
| Gathering | ~~GP regeneration during gathering (+1) not measured~~ measured on 2026-11-06, nothing to code | [#37](https://github.com/Shigu330/Tensho-Assistant/issues/37) |
| Items | Item blocks without a price | [#37](https://github.com/Shigu330/Tensho-Assistant/issues/37) |

### Map

Detail: [MAP-PROPOSALS](MAP-PROPOSALS.md).

| No. | Proposal | What it brings | Value | Size | Recommendation |
|---|---|---|---|---|---|
| P1 | Zoom at opening, adjustable and kept between sessions | The map opens at the zoom chosen by the player: the last zoom used (kept after a game restart), a fixed percentage following the character, or the whole map. | high | S | Batch 1 |
| P2 | Label size set apart from that of the icons | Place names and the level labels of the gathering points stay readable at every zoom without overlapping. | high | S | Batch 1 |
| P3 | Aetheryte nearest to the flag or to a point, with its cost | Right click on the map or on the flag: the entry « Teleport to the nearest aetheryte » gives the name and the real cost in gil, then asks for the usual confirmation. | high | S | After 1.0.0 (partly done) |
| P5 | Wheel: zoom from the whole window and not adjustable | The wheel zooms everywhere above the map: on the zoom slider, on the tool column and on the names placed over the map. The wheel step is adjustable and its direction can be inverted. | medium | S | Batch 2 |
| P6 | Character coordinates always visible, and distances | A corner of the map permanently shows the X/Y coordinates of the character, and those of the flag if there is one. | medium | S | Probe |
| P7 | Non-attuned aetherytes flagged and cost on hover | Hovering over an aetheryte immediately gives its teleport cost. A non-attuned aetheryte is greyed out and says so, instead of finding out after the click. | medium | S | Probe |
| P9 | FATE: experience bonus and colour according to the time left | On the map and in the FATE tab of the right click, a FATE with a bonus is marked, and the colour shows those about to end. | medium | S | After 1.0.0 |
| P10 | Search on the map (places, aetherytes, NPCs, quests, resources) | A search field in the map window finds a place name, an aetheryte, a vendor or Triple Triad NPC, a quest giver or a gathering resource, on all maps. | medium | M | After 1.0.0 |
| P11 | Undiscovered parts hidden like on the game map | Like the game map, the Tensho map hides or greys out the zones and regions that the character has not yet discovered. This avoids revealing places. | medium | M | Probe |
| P12 | Names written next to icons, like the game map | The names that the game writes next to some icons (aetherytes, important places) are written on the Tensho map with the same placement, and not only in the tooltip. | medium | M | After 1.0.0 |
| P15 | State of the game readings shown on the map (R-14, map part) | After an FF14 update, the map says precisely what no longer works (« quest markers unavailable after a game update », « teleport unavailable »), instead of an empty map without explanation. | medium | S | After 1.0.0 |
| P16 | Option: left click on the map to place the flag | A short left click on an empty spot of the map places the flag, without going through the right-click menu; a drag still moves the map. | medium | S | After 1.0.0 |
| P17 | Reading of the marker type of the leves and of their zones | The leves form a proper category in the edge markers and in the zone colours, instead of being mixed with the « other events ». | medium | S | Probe |
| P19 | In-game map tests still pending | Before adding functions to the map, we confirm that the latest changes work. Otherwise, an old defect and a new one risk getting mixed up. | medium | S | Probe |
| P20 | « Layers » button to show or hide everything in one click | A button in the tool column opens a small list of layers (quests, FATEs, gathering, vendors, exploration, currents, party, favourites) that you show or hide with a click, without opening the settings. | low | S | Batch 2 |
| P21 | Keyboard shortcuts in the map | When the map has focus: +/- to zoom, arrows to move it, Home for the whole map, a key to follow the character. | low | S | To discard |
| P22 | Names of the quest icon variants (71141 / 71151) | The icon settings display the real meaning of each quest icon variant, instead of « variant n ». | low | S | Probe |
| P23 | Close friends on the map | The friends present around the character, therefore already loaded by the game, are drawn on the Tensho map like the party members. | low | S | To discard |
| P24 | Teleport command by name (/tensho tp) | Typing « /tensho tp Ul'dah » (or an alias) launches the same teleport request as a click on an aetheryte of the map, with its cost. | low | S | To discard |
| P25 | Tensho mini-map | A small compact window that follows the character (aetherytes, party, FATEs, flag), resizable and zoomable, next to the game's mini-map or in its place. | low | L | To discard |
| P26 | Position highlighted when hovering over the right-click lists | When hovering over a line of the right-click tabs (quest, NPC, gathering point), the matching spot blinks on the map below. | low | S | To discard |
| P27 | Adjustable colour per icon | Each icon type can be given a tint, in addition to its size and its priority, to spot at a glance what matters. | low | S | To discard |

### Items, bank, sorting and market

Detail: [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md).

| No. | Proposal | What it brings | Value | Size | Recommendation |
|---|---|---|---|---|---|
| I0 | Count worn pieces in the stocks and in « owned » | Worn gear counts as owned. The Fashion Report of the week no longer says « not owned » for the piece you are wearing. The stock on hover and the item sheet include it, on a « Worn » line. | high | S | Batch 1 |
| I1 | Flag and erase the reading of a dismissed retainer or of an old storage | A dismissed retainer no longer counts forever in your stocks. The Storage page flags the recorded retainers that no longer appear at the summoning bell, and an « Erase this read » button (two clicks) removes them. | high | S-M | Batch 3 |
| I3 | NPC vendor prices in the preparation cost and on the Market page | For each ingredient sold for gil by an NPC vendor, the preparation cost shows the NPC price next to the market minimum and can keep the cheapest. | high | S-M | Batch 3 |
| I4 | Universalis prices in batches: one request for a whole list | « Read the prices » of the preparation cost (and of the lists to come) answers in one request instead of one per ingredient: a few seconds instead of about 100 s for 32 ingredients, without risking the 2-minute stop, with far fewer requests to Universalis. | high | M | Probe |
| I5 | Preparation cost at the data center price (option of A-06) | The preparation cost is computed at the current world or at the cheapest of the data center, with the world where to buy, for whoever changes world to do their shopping. | medium | S | After 1.0.0 |
| I8 | X-07, anticipated part: open the item on Universalis, Garland Tools, Teamcraft or the wiki | Right click on an item, in the Tensho windows and in the game's « Tensho » menu: « Open on… » opens the item's page in the browser. | medium | S | After 1.0.0 (partly done) |
| I9 | Copy the item name and item link in the chat (local) | Right click on an item: « Copy the name », to paste into the game's market search. | medium | S | After 1.0.0 |
| I10 | Retainers' crystals and gil in the retainer's reading | The crystals entrusted to the retainers count in the Crystals tab (one column per retainer), in the « missing » of crafting and on hover. The gil of each retainer is kept, with the date of the reading. | medium | M | Probe (partly done) |
| I11 | Sortable list view of the Bank | In addition to the 5 × 7 grids, a « List » view: one item per line, with NQ, HQ, total and the storages where it is, sortable by name, quantity or category. | medium | S-M | After 1.0.0 |
| I14 | Undo the last sort | After « Apply », an « Undo last sort » button restores the previous order of the bags or of the open storage, as long as nothing has moved since. | medium | S-M | After 1.0.0 |
| I15 | Share a sort template as text (copy / paste) | A storage template is copied as text and pasted on another player's or another PC. | medium | S | After 1.0.0 |
| I16 | Commands « /tensho objet » and « /tensho prix » | « /tensho objet iron ore » opens the item's sheet, or the search if there are several results; « /tensho prix … » opens the Market page. Both also work in a game macro. | medium | S | Batch 2 |
| I18 | I-07, anticipated part: copy « lowest price − 1 » from the Market page | On the Listings tab of Tensho, a button copies the lowest price minus 1 (NQ or HQ). Your own retainers are marked, so as not to undercut yourself. | low | S | After 1.0.0 (partly done) |
| I19 | English name of the item in the sheet and the tooltip | Optionally, the item's English name is displayed under its name, useful with English guides and wikis. | low | S | After 1.0.0 |
| I20 | Sort preview: moved cells flagged | The preview frames the cells that change and says how many items move. « Apply » is greyed out when the sort would change nothing. | low | S | After 1.0.0 |
| I21 | Job and gear set filter in the sort rules | A sort zone can take only the gear of one job, or the pieces of your gear sets. | low | M | After 1.0.0 |
| I22 | Computations done at every frame (postponed to e): preparation cost and market history | Nothing visible: the preparation cost and history pages stay light with long lists. | low | S | After 1.0.0 |

### Crafting and gathering

Detail: [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md).

| No. | Proposal | What it brings | Value | Size | Recommendation |
|---|---|---|---|---|---|
| C0 | A-01, anticipated part: choose the target quality in the log panel | In the panel next to the crafting log, you choose the target: maximum quality (as today), a typed value or a percentage of the maximum. | high | S | Batch 3 (partly done) |
| C2 | Try your saved sequences on all recipes | The log panel also tries your recorded sequences for other recipes (the same macro often serves a whole series of recipes of the same difficulty, whatever the craft) and displays the best one that finishes and reaches the target, with its name and the original recipe. | high | S | Batch 1 |
| C3 | Simulate another craft without wearing it: last attributes read per craft and planned attributes of the Gear page | The game's log lets you browse the recipes of all crafts. The panel then computes with the last attributes read for the recipe's craft (marked « read on … »), instead of saying « Equip the craft ». | high | S | Batch 1 |
| C4 | Attribute thresholds of a sequence: minimum Craftsmanship, Control and CP | Under a finished simulation: « This sequence holds with at least 3,950 Craftsmanship, 3,820 Control and 590 CP (margin: +120, +45, +12) ». | high | S | Batch 1 |
| C5 | Follow your sequence during crafting: next action and gap with the plan | A small panel next to the game's crafting window follows the chosen sequence (that of the log panel or of the simulator): « Step 7/22: Innovation », expected progress and quality against those of the game, and a warning when a Good, Excellent or Poor condition made the craft diverge. | high | M | Batch 3 |
| C6 | Level-adjusted, imposed-quality recipes and Cosmic Exploration | The simulator, the log panel and the live crafting accept the recipes currently refused as « adjusted » or « imposed quality », including those of Cosmic Exploration, the crafters' current content, with their mission thresholds. | high | M-L | Probe |
| C7 | R-13, anticipated part: timed points open now or soon | On the Gathering page, a « Now » tab lists all the ET-timed points (miner, botanist) that are open, then those that open within the real hour, sorted by next window, with countdown, zone, level and alarm button. | high | S-M | Batch 3 (partly done) |
| C9 | R-02, anticipated part: « possible now? » in the fishing helper | Each fish of the fishing helper and of « How to fish it » shows three badges: ET window open, current weather matching, previous weather matching (✓ / ✗ / ?). | high | S | Batch 3 (partly done) |
| C10 | Bite timer restarted by itself at each cast | The fishing helper's timer, compared with the Teamcraft bite times, restarts from zero at each cast, without a click on « Restart ». The green « it may bite » range becomes accurate for each cast. | high | S | To discard |
| C12 | Initial quality computed from the chosen HQ ingredients | In the simulator, you indicate how many HQ ingredients you use; the initial quality is computed instead of being typed. You compare « 3 more HQ » against « one more step » without computing by hand. | medium | S | After 1.0.0 |
| C13 | HQ chance of a final quality | For a recipe where HQ is possible that finishes under the maximum: « Quality 7,200 / 9,000, HQ chance ≈ 55% ». Useful for levelling and for deliveries without mandatory HQ, and to stop a search earlier. | medium | S | Probe |
| C14 | A-12, anticipated part: complete recipe sheet (master recipe book, quick synthesis, HQ, prerequisites, numeric gap) | The recipe (Craft tab of the item sheet and log panel) says: master recipe book required (and whether it is read), quick synthesis possible, HQ possible, specialist required, item or status required, and the numeric gap « Craftsmanship 3,950 / 4,000: −50 ». | medium | S | After 1.0.0 (partly done) |
| C15 | More direct simulator: recipe choice in the page, result recomputed by itself, drag and drop | The simulator has its own recipe search (name, craft, level); the result updates at each action added, moved or removed, without a click on « Simulate »; actions move by drag and drop; two bars show progress and quality. | medium | S | After 1.0.0 |
| C16 | Sequence library: search, rename, duplicate, copy and paste as text (A-08, text part) | The page of recorded sequences can be searched by name, item or craft and sorted; a sequence is renamed, duplicated, copied as macro text and, the other way round, a pasted /ac macro becomes a sequence that can be checked in the simulator. | medium | S-M | After 1.0.0 (partly done) |
| C17 | Macro export options: separate waits (X-12), sound, final echo, icon | You set the wait of the actions and that of the effects separately, choose the end sound (or none), the echo on each page or only on the last one, and the icon of the macros written in the game instead of the imposed « ? ». | medium | S | After 1.0.0 (partly done) |
| C18 | Production chain: buttons per step, identical steps grouped, « done » boxes | Each step of the chain has its « Log » (opens the game's log on the recipe) and « Simulate » buttons; the repeated steps of the same recipe are shown grouped (×N in total); you tick the steps done over the session. | medium | S-M | After 1.0.0 |
| C19 | Where are my materials: detail per storage in the preparation and the chain | On hover of « Read » of an ingredient: « Bags 12 · Retainer Lune 40 (reading of 01/11) · Saddlebag 8 ». You know which retainer to call before crafting, without opening the sheet of each item. | medium | S | Batch 2 |
| C21 | « What can I craft? » with my stocks | A list of the recipes that can be made right now with your bags and the recorded Bank, filtered by craft and level, with the number of possible crafts. Handy to use up materials or level a craft. | medium | M | After 1.0.0 |
| C22 | Free company workshop projects: materials per part and per phase | For a submarine or airship part, or another workshop project, the Free company page lists the materials of each phase against your stocks, with a link to the production chain of each. | medium | M | Probe |
| C23 | Next action highlighted on the hotbars | During a followed craft (K6, then A-04), the advised action blinks on your bars like a game combo action. You craft by hand without taking your eyes off the bars. Nothing is executed. | medium | M | To discard |
| C24 | Remaining preparation need in the gathering and fishing helpers | At the gathering or fishing point, the item useful to your last crafting chain is marked: « 14 more for … ingot ». The number goes down as you gather. | medium | S | Batch 2 |
| C27 | Class of the point (normal, ephemeral, legendary…) and folklore tome required | Each point says its class and, for a legendary point, the folklore tome needed (and whether it is read). You no longer make the trip to a point that you cannot open. | medium | S | Probe |
| C28 | Duration of Fisher's Intuition and countdown | For a fish with predators: « Fisher's Intuition: 350 s after 3 × X ». When Fisher's Intuition is active, the fishing helper displays the time left. Indispensable for big fish. | medium | S | Probe |
| C29 | R-15, anticipated part: spearfishing fish in the Gathering page | Spearfishing fish appear in the search of the Gathering page with their place, like the other resources. The help on the spearfishing window will come with R-15. | medium | S-M | Probe (partly done) |
| C30 | Local reading of your catches and personal bite times | Tensho notes at your end each catch (fish, place, bait, bite time, hookset) and displays your own ranges next to those of Teamcraft, per place. Nothing is sent. | medium | M | Probe |
| C31 | Weather of several zones in one table (favourite zones) | The Weather page shows the current and next weather of your favourite zones, or of all the zones of an expansion, on a single grid. Useful to chain fish and points according to the weather. | medium | S | Batch 2 |
| C32 | Commands « /tensho recolte » and « /tensho recette » (continuation of I16) | « /tensho recolte iron ore » opens the Gathering page on the item, « /tensho recette … » opens its Craft tab; also usable in a game macro, like /gather of GatherBuddy. | medium | S | Batch 2 |
| C33 | A single in-game reading session for crafting and gathering, before coding | A guided session reads at once, in developer mode, the game values on which several proposals and retained gaps depend. The following versions then move forward without waiting for one reading each. | medium | S-M | Probe |
| C35 | Fishing helper: game readings and first tables outside Draw | Nothing visible, except the absence of a hitch at the first opening of the fishing helper or of the gathering panel, and a helper that stays compliant with the project rules after an update. | low | S | After 1.0.0 |
| C36 | R-14, crafting and gathering part: an unreadable helper says so instead of disappearing | After a game update, the log panel, the gathering help or the live crafting display « unavailable after a game update » in their usual place, and Diagnostics says which reading failed. | low | S | After 1.0.0 |

### Alarms, company, quests, gear, mini-games

Detail: [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md).

| No. | Proposal | What it brings | Value | Size | Recommendation |
|---|---|---|---|---|---|
| T0 | Real-time alarms: by hand, every day, every week or at each reset (X-10, anticipated part) | An alarm set only once comes back by itself: « 30 min before the weekly reset », « every day at 21:00 », « end of the supply missions ». | high | S-M | Batch 3 (partly done) |
| T3 | Quests: Level and Zone columns, sort and level filter | The quest list shows the level and the zone of each quest. You sort it by level or by zone and limit it to a range of levels, for example the side quests of your level in a zone. | high | S | Batch 1 |
| T4 | Monsters: flag on the monster's position, not at the centre of the map | On the sheet of a monster that drops the item, « Open the map » places the flag at the spot given by Garland Tools and opens the Tensho map on it, with the mention « community position ». | high | S | Batch 1 |
| T5 | An in-game reading session for these tools, before coding | A single guided session (developer mode) provides all the missing evidence at once. The proposals that depend on it are then coded without back and forth. | high | S | Probe |
| T10 | Home: « Today » block and tiles of your choice | When Tensho opens, you see what matters now: reminders not done before the next reset, vessels back or next return, retainer ventures finished, followed quests, the next three alarms. | high | M | Batch 3 |
| T11 | Quests « available now » and quests of the current zone | A filter shows the quests you can take: not done, prerequisites completed, not excluded by another choice (starting city, Grand Company), level reached. | high | M | Batch 3 |
| T12 | Path to a quest: remaining prerequisites, count and next one to do | On the sheet of a quest (or of a duty, with « Unlocked by »), Tensho lists in order all the quests still to do to reach it, their number, and the first one to take now, with a map button. | high | M | Batch 3 |
| T13 | Duties and quests: « Unlocked by » and « Unlocks » | On the Duty loot page and in the Duty Finder panel, each dungeon, trial or raid says which quest unlocks it and whether you have done it, with a link to the quest and its path. | high | M | Probe |
| T14 | Gear: prepare the plan of a job other than the one worn | A job menu on the Gear page: you prepare or consult the plan of your healer while being a blacksmith. | high | M | Batch 3 |
| T15 | Fashion Report: outfit of the chosen week, tried on in one click, with the place of each piece | You choose one piece per slot among those proposed for the week. | high | M | Batch 3 |
| T16 | Retainer ventures: one line per retainer, with alarm | The « Retainer ventures » line unfolds: for each retainer, its finished venture or its end time, a bell, and an alarm « when all are finished ». | medium | S | After 1.0.0 (partly done) |
| T17 | Reminders ticked by themselves when the game says so | When the game indicates 0 custom deliveries or 0 allied society quests left, the « done » box ticks itself until the reset. The manual tick stays for the rest. | medium | S | Batch 2 |
| T18 | Discord: time read in the time zone of each member, and tags [zone], [objet], [et] | In a shared free company channel, each member reads the alarm time in their own time zone (and « in 5 minutes »). Your messages can also give the zone, the item and the Eorzea time. | medium | S | After 1.0.0 |
| T19 | Alarms: edit the name and the time, and « Remind me again after 5 min » | You rename an alarm or change its ET time or its date without recreating it. An alarm that has just rung can be postponed by a few minutes, from the Alarms page or the tooltip of the server bar. | medium | S | Batch 2 |
| T20 | Alarms: also ring at the exact time | In addition to the reminder N minutes before, the alarm can ring a second time when the window opens: gathering point, fish, vessel return. | medium | S | Batch 2 |
| T21 | Character: levels and experience of all jobs | The levels and experience of all your jobs on the Character page. | medium | S | Probe |
| T23 | Free company: vessels grouped by company, erasable old reading, value of the last loot | With characters in several companies, each workshop has its own frame (company name, reading date). The reading of a company you have left can be erased. | medium | S | After 1.0.0 |
| T24 | Discord: one channel per category (several webhooks) | Vessel returns go to the free company channel, your gathering alarms to your private channel. | medium | M | After 1.0.0 |
| T25 | Gear: several named plans per job | For the same job, you keep « levelling », « crafting » and « best possible », and switch from one to the other; each one is shared by code. | medium | S-M | After 1.0.0 |
| T26 | Airships: route of the last voyage reconstructed | The Route column of the airships displays the destinations instead of « route unknown ». | medium | S | Probe |
| T27 | Active company actions: end time and alarm | The Reminders show the company actions in progress (experience bonus, gathering bonus…) and their end time, with a bell to relaunch one in time. | medium | M | Probe |
| T28 | Collections: « Recently obtained » and sources menu for emotes | A « Recently obtained » line lists, with the date, what you have gained since the previous reading (mounts, minions, orchestrion rolls…). Right-clicking an emote opens its sources, as for mounts. | low | S | After 1.0.0 |
| T29 | Mini Cactpot: payout table proven and visible | Under the advice, the payout of each line sum (6 to 24) is displayed, as on the game's ticket: you see what the advised line aims at. | low | S | After 1.0.0 |

### Big projects and gaps left

Detail: [G-PROPOSALS](G-PROPOSALS.md), « Gaps left ».

| Point | What would be needed first |
|---|---|
| I-15, search in the game's inventory windows | A read-only probe, in game: native pointers at every frame |
| I-05, continuation of the retainers' sales | A try of 0.508; decision: comparison on click (applied) or proposed at opening |
| N-04, housing lottery | A probe: game structure not found |
| N-07, island resource map | Check in game the unit of X, Y and of the radius |
| N-10, island workshop | Re-read the design, then probe; set aside (DEC-033) |
| N-12, spawn conditions of S ranks | Community sources to decide together |
| N-13, visible furniture | Reading of furniture: risks to decide together |
| X-14, housing tools | Display fixes: risks to decide together |
| Beastmaster, continuation and Labyrinth panel | Set aside; contact the Dalamud team before coding the panel (DEC-032) |

### Ideas spotted while coding

Taken from [G-PROPOSALS](G-PROPOSALS.md), not sorted.

| Spotted in | Idea | Why not yet |
|---|---|---|
| 0.344 | Alert on the weekly tomestone cap (in addition to the total cap) | Outside the user's choice for I-02 |
| 0.345 | « Where is this item? » on all characters (tooltip and item sheet), by extending ItemHoldings | Big change of the stocks; to do after in-game validation of I-03 |
| 0.345 | Retainers' gil in the characters view | The game only gives it when the bell is opened; to probe |
| 0.347 | List of all annotated items (Items page) with filter by label | Small; to do with the other item lists (I-12) |
| 0.346 | Undercutting: also display the recent average selling price before pasting (safeguard against too low an offer) | Requires one more Universalis call |
| 0.345 | Only rewrite a character's file if its quantities have changed | Saves disk writes; today one write every 2 minutes per storage read automatically |

Triage of the proposals of the four audits (2026-11-05): [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md).

### Provisional choices

The alternatives to the choices already applied stay as ideas: [G-PROPOSALS](G-PROPOSALS.md), « Provisional choices » (confirmed by [DEC-026](DECISIONS.md)).

---

# Tri des propositions d'audit

**Instantané figé du 2026-11-05, plus mis à jour.** Sa classe « Après la 1.0.0 » est remplacée par le [plan global](GLOBAL-PLAN.md),
où rien ne vient après la 1.0.0, et la plupart de ses points sont faits depuis ; l'état courant de chaque point est dans le plan global.

Tri du 2026-11-05, à la version 0.539.0, des 95 propositions restées ouvertes dans [MAP-PROPOSALS](MAP-PROPOSALS.md), [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md), [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md) et [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md). Le détail de chaque proposition reste dans ces documents, sous le même numéro.

**Méthode et limites.** L'état vient d'une recherche dans le code et les textes de l'interface, sans lire chaque fonction ni essayer en jeu : un « non fait » peut être en partie fait. Valeur et taille (S, M) sont les estimations des audits. Le classement est une recommandation de l'agent : rien n'est décidé tant que l'utilisateur n'a pas choisi. Aucune de ces propositions n'est exigée pour la 1.0.0 ([PUBLISHING](PUBLISHING.md)).

**Revue faite avec l'utilisateur le 2026-11-05 : ses décisions sont dans le [plan global](GLOBAL-PLAN.md), qui prime sur les recommandations ci-dessous.** Aucun code tant qu'il n'a pas relu ce plan.

| Classe | Nombre | Recommandation |
|---|---|---|
| [Déjà faites](#déjà-faites) | 3 | Marquées faites |
| [Lot 1 : valeur haute, petites](#lot-1--valeur-haute-petites) | 8 | À faire en premier |
| [Lot 2 : valeur moyenne, petites](#lot-2--valeur-moyenne-petites) | 10 | Ensuite |
| [Lot 3 : valeur haute, moyennes](#lot-3--valeur-haute-moyennes) | 12 | Une par une, selon l'envie |
| [À sonder en jeu d'abord](#à-sonder-en-jeu-dabord) | 21 | Une séance de relevés avant de coder |
| [Après la 1.0.0](#après-la-100) | 33 | Gardées comme idées |
| [À écarter](#à-écarter) | 8 | Ne pas faire, sauf avis contraire |

## Déjà faites

| N° | Proposition | Preuve dans le code |
|---|---|---|
| C8 | Plan de PR du panneau de récolte : quantité et effets | Plan par nœud et effets du lieu (0.515 à 0.517, `Gathering/GatherNodeEffects.cs`) |
| C34 | Missions d'approvisionnement du jour | `Items/GcSupplyToday.cs` (0.469) |
| P14 | Mesurer le temps d'affichage de la carte | Ligne « Map » des diagnostics (0.489, `MapViewWindow.cs`) ; l'allègement éventuel reste à décider sur mesure |

## Lot 1 : valeur haute, petites

Taille S, aucune sonde avant de coder.

| N° | Proposition | Pourquoi d'abord |
|---|---|---|
| P1 | Zoom de la carte à l'ouverture, réglable et gardé | Geste répété à chaque ouverture |
| P2 | Taille des étiquettes séparée de celle des icônes | Lisibilité de la carte |
| I0 | Compter les pièces portées dans « possédé » | Corrige un compte faux ; à contrôler en jeu après coup |
| C2 | Essayer ses séquences sauvegardées sur toutes les recettes | Réutilise le simulateur |
| C3 | Simuler un autre métier sans le porter | Évite de changer de job pour simuler |
| C4 | Seuils d'attributs d'une séquence | Dit si une macro passe avec son équipement |
| T3 | Quêtes : colonnes Niveau et Zone, tri et filtre | Données déjà lues |
| T4 | Monstres : drapeau sur la position du monstre | Évite un drapeau au centre de la carte |

## Lot 2 : valeur moyenne, petites

Taille S, aucune sonde.

| N° | Proposition |
|---|---|
| P5 | Molette de la carte réglable |
| P20 | Bouton « Couches » : tout afficher ou masquer |
| C19 | Où sont mes matériaux, par rangement |
| C24 | Besoin restant de la préparation dans les aides de récolte et de pêche |
| C31 | Météo de plusieurs zones en un tableau |
| C32 | Commandes « /tensho recolte » et « /tensho recette » |
| I16 | Commandes « /tensho objet » et « /tensho prix » |
| T17 | Rappels cochés d'eux-mêmes quand le jeu le dit |
| T19 | Alarmes : modifier le nom et l'heure, « Me rappeler dans 5 min » |
| T20 | Alarmes : sonner aussi à l'heure exacte |

## Lot 3 : valeur haute, moyennes

| N° | Proposition | Note |
|---|---|---|
| I1 | Signaler et effacer le relevé d'un servant renvoyé | Taille S à M |
| I3 | Prix des marchands PNJ dans le coût de préparation | Taille S à M ; un contrôle en jeu sur un objet connu |
| C0 | Qualité visée dans le panneau du carnet | En partie (0.419) |
| C5 | Suivre sa séquence pendant la synthèse | Information seulement, sans jouer d'action |
| C7 | Points de récolte ouverts maintenant ou bientôt | En partie (0.415) |
| C9 | « Possible maintenant ? » dans l'aide à la pêche | En partie (0.396) |
| T0 | Alarmes en heure réelle : chaque jour, chaque semaine, à chaque remise à zéro | En partie : `AlarmRepeat` n'a que Once et EorzeaDaily |
| T10 | Accueil : bloc « Aujourd'hui » | Une décision : quelles lignes par défaut (recommandé : toutes, chacune masquable) |
| T11 | Quêtes disponibles maintenant, quêtes de la zone | Une colonne de la feuille Quest à sonder |
| T12 | Chemin vers une quête : prérequis restants | |
| T14 | Équipement : préparer le plan d'un autre job | |
| T15 | Revue de mode : tenue de la semaine, essayée en un clic | Sur clic seulement |

## À sonder en jeu d'abord

Une donnée n'est pas prouvée : pas de code avant le relevé (règle du projet, aucune donnée inventée). À regrouper avec [l'issue #23](https://github.com/Shigu330/Tensho-Assistant/issues/23).

| Domaine | N° |
|---|---|
| Carte | P6 unité des distances ; P7 éthérites non harmonisées ; P11 parties non découvertes ; P17 marqueurs des mandats ; P19 tests de la carte ; P22 variantes d'icônes de quête |
| Objets et marché | I4 requêtes Universalis par lots ; I10 cristaux des servants |
| Artisanat et récolte | C6 recettes à niveau ajusté et Exploration cosmique ; C13 chance HQ ; C22 projets de l'atelier ; C27 classe du point ; C28 durée de l'intuition ; C29 harpon dans la page Récolte ; C30 relevé des prises ; C33 séance de relevés |
| Autres outils | T5 séance de relevés ; T13 « Débloqué par » ; T21 expérience de tous les jobs ; T26 dernier voyage des dirigeables ; T27 actions de compagnie |

## Après la 1.0.0

Utiles sans être prioritaires. « En partie » : une première version existe.

| Domaine | N° |
|---|---|
| Carte | P3 éthérite la plus proche du drapeau (en partie) ; P9 ALEA ; P10 recherche sur la carte (en partie, page Cartes) ; P12 noms à côté des icônes (en partie : noms de lieux, étiquettes de récolte) ; P15 état des lectures ; P16 clic gauche pour le drapeau |
| Objets et tri | I5 prix du centre de données ; I8 liens Universalis, Garland, Teamcraft (en partie) ; I9 copier le nom, lien d'objet ; I11 Banque en liste triable ; I14 annuler le dernier tri ; I15 partager un modèle de tri ; I18 prix moins 1 depuis la page Marché (en partie) ; I19 nom anglais ; I20 cases déplacées dans l'aperçu ; I21 filtre de job dans les règles ; I22 calculs à chaque image |
| Artisanat et récolte | C12 qualité initiale selon les ingrédients HQ (en partie, valeur saisie) ; C14 fiche de recette complète (en partie) ; C15 simulateur plus direct ; C16 bibliothèque des séquences (en partie) ; C17 options d'export (en partie) ; C18 chaîne de fabrication ; C21 « Que puis-je fabriquer ? » ; C35 aide à la pêche hors de Draw ; C36 une aide illisible le dit |
| Autres outils | T16 une ligne par servant (en partie) ; T18 balises Discord (en partie) ; T23 vaisseaux par compagnie ; T24 un salon Discord par catégorie ; T25 plans nommés par job ; T28 obtenus récemment ; T29 table du Mini Cactpot visible (en partie) |

## À écarter

| N° | Proposition | Raison |
|---|---|---|
| P21 | Raccourcis clavier dans la carte | Valeur basse, risque de conflit avec les touches du jeu |
| P23 | Amis proches sur la carte | Valeur basse ; montre d'autres joueurs, à éviter avant l'examen Dalamud |
| P24 | Commande de téléportation par nom | Valeur basse ; « Y aller » couvre le besoin |
| P25 | Mini-carte Tensho | Seconde carte à maintenir, le jeu en a une |
| P26 | Position mise en évidence au survol des listes | Valeur basse |
| P27 | Couleur réglable par icône | Valeur basse |
| C10 | Chrono de touche à chaque lancer | Demande une sonde, et les plugins de pêche existants le font déjà. 2026-11-09 : plus rien après la 1.0.0 ; la sonde existait (0.887) : fait en 0.1011 |
| C23 | Action suivante sur les barres de raccourcis | Dessin sur l'interface du jeu à chaque image, sensible aux mises à jour ; C5 donne l'information sans ce risque |

## Liste complète

Tout ce qui reste à revoir pour le plan global, d'un seul coup. La colonne « Recommandation » reprend les classes ci-dessus ; rien n'est décidé.

### Corrections

| Où | Ce qui manque | Issue |
|---|---|---|
| Jardin | Messages d'entretien et de récolte non reconnus, puis lignes de journal de capture à retirer | [#36](https://github.com/Shigu330/Tensho-Assistant/issues/36) |
| Jardin | Les Bleuets manquent dans la liste de graines | [#36](https://github.com/Shigu330/Tensho-Assistant/issues/36) |
| Escouade | Pas de bouton d'alarme pour une mission déjà en cours | [#37](https://github.com/Shigu330/Tensho-Assistant/issues/37) |
| Récolte | ~~Régénération de PR pendant la récolte (+1) non mesurée~~ mesurée le 2026-11-06, rien à coder | [#37](https://github.com/Shigu330/Tensho-Assistant/issues/37) |
| Objets | Blocs d'objets sans prix | [#37](https://github.com/Shigu330/Tensho-Assistant/issues/37) |

### Carte

Détail : [MAP-PROPOSALS](MAP-PROPOSALS.md).

| N° | Proposition | Ce que ça apporte | Valeur | Taille | Recommandation |
|---|---|---|---|---|---|
| P1 | Zoom à l'ouverture, réglable et gardé entre les sessions | La carte s'ouvre au zoom choisi par le joueur : le dernier zoom utilisé (gardé après une relance du jeu), un pourcentage fixe en suivant le personnage, ou la carte entière. | haute | S | Lot 1 |
| P2 | Taille des étiquettes réglée à part de celle des icônes | Les noms de lieux et les étiquettes de niveau des points de récolte restent lisibles à tous les zooms sans se chevaucher. | haute | S | Lot 1 |
| P3 | Éthérite la plus proche du drapeau ou d'un point, avec son coût | Clic droit sur la carte ou sur le drapeau : l'entrée « Se téléporter à l'éthérite la plus proche » donne le nom et le coût réel en gils, puis demande la confirmation habituelle. | haute | S | Après 1.0.0 (en partie fait) |
| P5 | Molette : zoom depuis toute la fenêtre et pas réglable | La molette zoome partout au-dessus de la carte : sur le curseur de zoom, sur la colonne d'outils et sur les noms posés par-dessus la carte. Le pas de la molette est réglable et son sens peut être inversé. | moyenne | S | Lot 2 |
| P6 | Coordonnées du personnage toujours visibles et distances | Un coin de la carte montre en permanence les coordonnées X/Y du personnage, et celles du drapeau s'il y en a un. | moyenne | S | Sonde |
| P7 | Éthérites non harmonisées signalées et coût au survol | Le survol d'une éthérite donne tout de suite son coût de téléportation. Une éthérite non harmonisée est grisée et le dit, au lieu de l'apprendre après le clic. | moyenne | S | Sonde |
| P9 | ALEA : bonus d'expérience et couleur selon le temps restant | Sur la carte et dans l'onglet ALEA du clic droit, une ALEA avec bonus est marquée, et la couleur indique celles qui vont bientôt finir. | moyenne | S | Après 1.0.0 |
| P10 | Recherche sur la carte (lieux, éthérites, PNJ, quêtes, ressources) | Un champ de recherche dans la fenêtre de la carte trouve un nom de lieu, une éthérite, un PNJ vendeur ou de Triple Triad, un donneur de quête ou une ressource de récolte, sur toutes les cartes. | moyenne | M | Après 1.0.0 |
| P11 | Parties non découvertes masquées comme sur la carte du jeu | Comme la carte du jeu, la carte Tensho cache ou grise les zones et les régions que le personnage n'a pas encore découvertes. On évite ainsi de dévoiler des lieux. | moyenne | M | Sonde |
| P12 | Noms écrits à côté des icônes, comme la carte du jeu | Les noms que le jeu écrit à côté de certaines icônes (éthérites, lieux importants) sont écrits sur la carte Tensho avec le même placement, et pas seulement dans l'infobulle. | moyenne | M | Après 1.0.0 |
| P15 | État des lectures du jeu affiché sur la carte (R-14, partie carte) | Après une mise à jour de FF14, la carte indique précisément ce qui ne marche plus (« marqueurs de quête indisponibles après une mise à jour du jeu », « téléportation indisponible »), au lieu d'une carte vide sans explication. | moyenne | S | Après 1.0.0 |
| P16 | Option : clic gauche sur la carte pour poser le drapeau | Un clic gauche bref sur un endroit vide de la carte pose le drapeau, sans passer par le menu du clic droit ; un glisser déplace toujours la carte. | moyenne | S | Après 1.0.0 |
| P17 | Relevé du type de marqueur des mandats et de leurs zones | Les mandats forment une catégorie juste dans les repères au bord et dans les couleurs de zones, au lieu d'être mêlés aux « autres événements ». | moyenne | S | Sonde |
| P19 | Tests en jeu de la carte encore en attente | Avant d'ajouter des fonctions à la carte, on confirme que les derniers changements fonctionnent. Sinon, un défaut ancien et un nouveau risquent de se mélanger. | moyenne | S | Sonde |
| P20 | Bouton « Couches » pour tout afficher ou masquer en un clic | Un bouton de la colonne d'outils ouvre une petite liste de couches (quêtes, ALEA, récolte, vendeurs, exploration, courants, groupe, favoris) qu'on affiche ou masque d'un clic, sans ouvrir les réglages. | basse | S | Lot 2 |
| P21 | Raccourcis clavier dans la carte | Quand la carte a le focus : +/- pour zoomer, flèches pour la déplacer, Début pour la carte entière, une touche pour suivre le personnage. | basse | S | À écarter |
| P22 | Noms des variantes d'icônes de quête (71141 / 71151) | Les réglages d'icônes affichent le vrai sens de chaque variante d'icône de quête, au lieu de « variante n ». | basse | S | Sonde |
| P23 | Amis proches sur la carte | Les amis présents autour du personnage, donc déjà chargés par le jeu, sont dessinés sur la carte Tensho comme les membres du groupe. | basse | S | À écarter |
| P24 | Commande de téléportation par nom (/tensho tp) | Taper « /tensho tp Ul'dah » (ou un alias) lance la même demande de téléportation que le clic sur une éthérite de la carte, avec son coût. | basse | S | À écarter |
| P25 | Mini-carte Tensho | Une petite fenêtre compacte qui suit le personnage (éthérites, groupe, ALEA, drapeau), redimensionnable et zoomable, à côté de la mini-carte du jeu ou à sa place. | basse | L | À écarter |
| P26 | Position mise en évidence au survol des listes du clic droit | En survolant une ligne des onglets du clic droit (quête, PNJ, point de récolte), l'endroit correspondant clignote sur la carte en dessous. | basse | S | À écarter |
| P27 | Couleur réglable par icône | Chaque type d'icône peut recevoir une teinte, en plus de sa taille et de sa priorité, pour repérer d'un coup d'œil ce qui compte. | basse | S | À écarter |

### Objets, banque, tri et marché

Détail : [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md).

| N° | Proposition | Ce que ça apporte | Valeur | Taille | Recommandation |
|---|---|---|---|---|---|
| I0 | Compter les pièces portées dans les stocks et dans « possédé » | L'équipement porté compte comme possédé. La Revue de mode de la semaine ne dit plus « non possédé » pour la pièce que vous portez. Le stock au survol et la fiche objet l'incluent, sur une ligne « Porté ». | haute | S | Lot 1 |
| I1 | Signaler et effacer le relevé d'un servant renvoyé ou d'un ancien rangement | Un servant renvoyé ne compte plus pour toujours dans vos stocks. La page Rangements signale les servants enregistrés qui n'apparaissent plus à la sonnette, et un bouton « Effacer ce relevé » (deux clics) les retire. | haute | S-M | Lot 3 |
| I3 | Prix des marchands PNJ dans le coût de préparation et sur la page Marché | Pour chaque ingrédient vendu en gils par un marchand PNJ, le coût de préparation montre le prix PNJ à côté du minimum du marché et peut retenir le moins cher. | haute | S-M | Lot 3 |
| I4 | Prix Universalis par lots : une requête pour toute une liste | « Lire les prix » du coût de préparation (et des listes à venir) répond en une requête au lieu d'une par ingrédient : quelques secondes au lieu d'environ 100 s pour 32 ingrédients, sans risquer l'arrêt à 2 minutes, avec beaucoup moins de requêtes vers Universalis. | haute | M | Sonde |
| I5 | Coût de préparation au prix du centre de données (option de A-06) | Le coût de préparation se calcule au monde actuel ou au moins cher du centre de données, avec le monde où acheter, pour qui change de monde pour faire ses achats. | moyenne | S | Après 1.0.0 |
| I8 | X-07, partie anticipée : ouvrir l'objet sur Universalis, Garland Tools, Teamcraft ou le wiki | Clic droit sur un objet, dans les fenêtres Tensho et dans le menu « Tensho » du jeu : « Ouvrir sur… » ouvre la page de l'objet dans le navigateur. | moyenne | S | Après 1.0.0 (en partie fait) |
| I9 | Copier le nom de l'objet et lien d'objet dans le chat (local) | Clic droit sur un objet : « Copier le nom », à coller dans la recherche du marché du jeu. | moyenne | S | Après 1.0.0 |
| I10 | Cristaux et gils des servants dans le relevé du servant | Les cristaux confiés aux servants comptent dans l'onglet Cristaux (une colonne par servant), dans le « manquant » de l'artisanat et au survol. Les gils de chaque servant sont gardés, avec la date du relevé. | moyenne | M | Sonde (en partie fait) |
| I11 | Vue en liste triable de la Banque | En plus des grilles 5 × 7, une vue « Liste » : un objet par ligne, avec NQ, HQ, total et les rangements où il se trouve, triable par nom, quantité ou catégorie. | moyenne | S-M | Après 1.0.0 |
| I14 | Annuler le dernier tri | Après « Appliquer », un bouton « Annuler le dernier tri » remet l'ordre précédent des sacs ou du rangement ouvert, tant que rien n'a bougé depuis. | moyenne | S-M | Après 1.0.0 |
| I15 | Partager un modèle de tri en texte (copier / coller) | Un modèle de rangement se copie en texte et se colle chez un autre joueur ou sur un autre PC. | moyenne | S | Après 1.0.0 |
| I16 | Commandes « /tensho objet » et « /tensho prix » | « /tensho objet minerai de fer » ouvre la fiche de l'objet, ou la recherche s'il y a plusieurs résultats ; « /tensho prix … » ouvre la page Marché. Les deux marchent aussi dans une macro du jeu. | moyenne | S | Lot 2 |
| I18 | I-07, partie anticipée : copier « prix le plus bas − 1 » depuis la page Marché | Sur l'onglet Annonces de Tensho, un bouton copie le prix le plus bas moins 1 (NQ ou HQ). Vos propres servants sont marqués, pour ne pas vous sous-coter. | basse | S | Après 1.0.0 (en partie fait) |
| I19 | Nom anglais de l'objet dans la fiche et l'infobulle | En option, le nom anglais de l'objet s'affiche sous son nom, utile avec les guides et les wikis en anglais. | basse | S | Après 1.0.0 |
| I20 | Aperçu du tri : cases déplacées signalées | L'aperçu encadre les cases qui changent et dit combien d'objets bougent. « Appliquer » est grisé quand le tri ne changerait rien. | basse | S | Après 1.0.0 |
| I21 | Filtre de job et de set d'équipement dans les règles de tri | Une zone de tri peut ne prendre que l'équipement d'un job, ou les pièces de vos sets d'équipement. | basse | M | Après 1.0.0 |
| I22 | Calculs faits à chaque image (reportés à e) : coût de préparation et historique du marché | Rien de visible : les pages du coût de préparation et de l'historique restent légères avec de longues listes. | basse | S | Après 1.0.0 |

### Artisanat et récolte

Détail : [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md).

| N° | Proposition | Ce que ça apporte | Valeur | Taille | Recommandation |
|---|---|---|---|---|---|
| C0 | A-01, partie anticipée : choisir la qualité visée dans le panneau du carnet | Dans le panneau à côté du carnet de synthèse, vous choisissez la cible : qualité maximale (comme aujourd'hui), une valeur tapée ou un pourcentage du maximum. | haute | S | Lot 3 (en partie fait) |
| C2 | Essayer ses séquences sauvegardées sur toutes les recettes | Le panneau du carnet essaie aussi vos séquences enregistrées pour d'autres recettes (la même macro sert souvent à toute une série de recettes de même difficulté, quel que soit le métier) et affiche la meilleure qui termine et atteint la cible, avec son nom et la recette d'origine. | haute | S | Lot 1 |
| C3 | Simuler un autre métier sans le porter : derniers attributs relevés par métier et attributs prévus de la page Équipement | Le carnet du jeu laisse parcourir les recettes de tous les métiers. Le panneau calcule alors avec les derniers attributs relevés du métier de la recette (marqués « relevés le … »), au lieu de dire « Équipez le métier ». | haute | S | Lot 1 |
| C4 | Seuils d'attributs d'une séquence : Habileté, Contrôle et PS minimum | Sous une simulation terminée : « Cette séquence tient avec au moins 3 950 Habileté, 3 820 Contrôle et 590 PS (marge : +120, +45, +12) ». | haute | S | Lot 1 |
| C5 | Suivre sa séquence pendant la synthèse : action suivante et écart avec le plan | Un petit panneau à côté de la fenêtre de synthèse du jeu suit la séquence choisie (celle du panneau du carnet ou du simulateur) : « Étape 7/22 : Innovation », progression et qualité attendues contre celles du jeu, et un avertissement quand une condition Bonne, Excellente ou Médiocre a fait diverger la synthèse. | haute | M | Lot 3 |
| C6 | Recettes à niveau ajusté, à qualité imposée et Exploration cosmique | Le simulateur, le panneau du carnet et la synthèse en direct acceptent les recettes aujourd'hui refusées comme « ajustées » ou « à qualité imposée », dont celles de l'Exploration cosmique, contenu actuel des artisans, avec leurs seuils de mission. | haute | M-L | Sonde |
| C7 | R-13, partie anticipée : points à horaire ouverts maintenant ou bientôt | Sur la page Récolte, un onglet « Maintenant » liste tous les points à horaire ET (mineur, botaniste) ouverts, puis ceux qui ouvrent dans l'heure réelle, triés par prochaine fenêtre, avec compte à rebours, zone, niveau et bouton d'alarme. | haute | S-M | Lot 3 (en partie fait) |
| C9 | R-02, partie anticipée : « possible maintenant ? » dans l'aide à la pêche | Chaque poisson de l'aide à la pêche et de « Comment le pêcher » montre trois pastilles : créneau ET ouvert, météo actuelle conforme, météo précédente conforme (✓ / ✗ / ?). | haute | S | Lot 3 (en partie fait) |
| C10 | Chrono de touche relancé tout seul à chaque lancer | Le chrono de l'aide à la pêche, comparé aux temps de touche Teamcraft, repart de zéro à chaque lancer, sans clic sur « Relancer ». La plage verte « ça peut mordre » devient juste pour chaque lancer. | haute | S | À écarter |
| C12 | Qualité initiale calculée à partir des ingrédients HQ choisis | Dans le simulateur, vous indiquez combien d'ingrédients HQ vous mettez ; la qualité initiale est calculée au lieu d'être tapée. Vous comparez « 3 HQ de plus » contre « une étape de plus » sans calcul à la main. | moyenne | S | Après 1.0.0 |
| C13 | Chance HQ d'une qualité finale | Pour une recette HQ possible qui termine sous le maximum : « Qualité 7 200 / 9 000, chance HQ ≈ 55 % ». Utile pour la montée en niveau et les livraisons sans HQ obligatoire, et pour arrêter une recherche plus tôt. | moyenne | S | Sonde |
| C14 | A-12, partie anticipée : fiche de recette complète (livre de maître, synthèse rapide, HQ, prérequis, écart chiffré) | La recette (onglet Craft de la fiche objet et panneau du carnet) dit : livre de recettes de maître requis (et s'il est lu), synthèse rapide possible, HQ possible, spécialiste requis, objet ou statut requis, et l'écart chiffré « Habileté 3 950 / 4 000 : −50 ». | moyenne | S | Après 1.0.0 (en partie fait) |
| C15 | Simulateur plus direct : choix de la recette dans la page, résultat recalculé tout seul, glisser-déposer | Le simulateur a sa propre recherche de recette (nom, métier, niveau) ; le résultat se met à jour à chaque action ajoutée, déplacée ou retirée, sans clic sur « Simuler » ; les actions se déplacent par glisser-déposer ; deux barres montrent progression et qualité. | moyenne | S | Après 1.0.0 |
| C16 | Bibliothèque des séquences : recherche, renommer, dupliquer, copier et coller en texte (A-08, partie texte) | La page des séquences enregistrées se cherche par nom, objet ou métier et se trie ; une séquence se renomme, se duplique, se copie en texte de macro et, dans l'autre sens, une macro /ac collée devient une séquence vérifiable dans le simulateur. | moyenne | S-M | Après 1.0.0 (en partie fait) |
| C17 | Options d'export des macros : attentes séparées (X-12), son, écho final, icône | Vous réglez l'attente des actions et celle des effets séparément, choisissez le son de fin (ou aucun), l'écho sur chaque page ou seulement sur la dernière, et l'icône des macros écrites dans le jeu au lieu du « ? » imposé. | moyenne | S | Après 1.0.0 (en partie fait) |
| C18 | Chaîne de fabrication : boutons par étape, étapes identiques regroupées, cases « fait » | Chaque étape de la chaîne a ses boutons « Carnet » (ouvre le carnet du jeu sur la recette) et « Simuler » ; les étapes répétées d'une même recette sont montrées regroupées (×N au total) ; vous cochez les étapes faites au fil de la session. | moyenne | S-M | Après 1.0.0 |
| C19 | Où sont mes matériaux : détail par rangement dans la préparation et la chaîne | Au survol de « Relevé » d'un ingrédient : « Sacs 12 · Servant Lune 40 (relevé du 01/11) · Sacoche 8 ». Vous savez quel servant appeler avant de fabriquer, sans ouvrir la fiche de chaque objet. | moyenne | S | Lot 2 |
| C21 | « Que puis-je fabriquer ? » avec mes stocks | Une liste des recettes faisables tout de suite avec vos sacs et la Banque enregistrée, filtrée par métier et niveau, avec le nombre de fabrications possibles. Pratique pour écouler des matériaux ou monter un métier. | moyenne | M | Après 1.0.0 |
| C22 | Projets de l'atelier de compagnie libre : matériaux par pièce et par phase | Pour une pièce de sous-marin ou de dirigeable, ou un autre projet d'atelier, la page Compagnie libre liste les matériaux de chaque phase contre vos stocks, avec un lien vers la chaîne de fabrication de chacun. | moyenne | M | Sonde |
| C23 | Action suivante mise en évidence sur les barres de raccourcis | Pendant une synthèse suivie (K6, puis A-04), l'action conseillée clignote sur vos barres comme une action combo du jeu. Vous fabriquez à la main sans quitter les barres des yeux. Rien n'est exécuté. | moyenne | M | À écarter |
| C24 | Besoin restant de la préparation dans les aides de récolte et de pêche | Au point de récolte ou de pêche, l'objet utile à votre dernière chaîne d'artisanat est marqué : « Encore 14 pour Lingot de… ». Le nombre baisse à mesure que vous récoltez. | moyenne | S | Lot 2 |
| C27 | Classe du point (normal, éphémère, légendaire…) et livre de folklore requis | Chaque point dit sa classe et, pour un point légendaire, le livre de folklore nécessaire (et s'il est lu). Vous ne faites plus le trajet vers un point que vous ne pouvez pas ouvrir. | moyenne | S | Sonde |
| C28 | Durée de l'intuition et compte à rebours | Pour un poisson à prédateurs : « Intuition : 350 s après 3 × X ». Quand l'intuition est active, l'aide à la pêche affiche le temps restant. Indispensable pour les gros poissons. | moyenne | S | Sonde |
| C29 | R-15, partie anticipée : poissons au harpon dans la page Récolte | Les poissons au harpon apparaissent dans la recherche de la page Récolte avec leur lieu, comme les autres ressources. L'aide sur la fenêtre du harpon viendra avec R-15. | moyenne | S-M | Sonde (en partie fait) |
| C30 | Relevé local de vos prises et temps de touche personnels | Tensho note chez vous chaque prise (poisson, lieu, appât, temps de touche, ferrage) et affiche vos propres plages à côté de celles de Teamcraft, par lieu. Rien n'est envoyé. | moyenne | M | Sonde |
| C31 | Météo de plusieurs zones en un tableau (zones favorites) | La page Météo montre la météo actuelle et les prochaines de vos zones favorites, ou de toutes les zones d'une extension, sur une seule grille. Utile pour enchaîner poissons et points selon la météo. | moyenne | S | Lot 2 |
| C32 | Commandes « /tensho recolte » et « /tensho recette » (suite de I16) | « /tensho recolte minerai de fer » ouvre la page Récolte sur l'objet, « /tensho recette … » ouvre son onglet Craft ; utilisables aussi dans une macro du jeu, comme /gather de GatherBuddy. | moyenne | S | Lot 2 |
| C33 | Une seule séance de relevés en jeu pour l'artisanat et la récolte, avant de coder | Une séance guidée relève en une fois, en mode développeur, les valeurs du jeu dont dépendent plusieurs propositions et manques retenus. Les versions suivantes avancent ensuite sans attendre un relevé chacune. | moyenne | S-M | Sonde |
| C35 | Aide à la pêche : lectures du jeu et premières tables hors de Draw | Rien de visible, sinon l'absence d'à-coup à la première ouverture de l'aide à la pêche ou du panneau de récolte, et une aide qui reste conforme aux règles du projet après une mise à jour. | basse | S | Après 1.0.0 |
| C36 | R-14, partie artisanat et récolte : une aide illisible le dit au lieu de disparaître | Après une mise à jour du jeu, le panneau du carnet, l'aide à la récolte ou la synthèse en direct affichent « indisponible après une mise à jour du jeu » à leur place habituelle, et Diagnostics dit quelle lecture a échoué. | basse | S | Après 1.0.0 |

### Alarmes, compagnie, quêtes, équipement, mini-jeux

Détail : [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md).

| N° | Proposition | Ce que ça apporte | Valeur | Taille | Recommandation |
|---|---|---|---|---|---|
| T0 | Alarmes en heure réelle : à la main, chaque jour, chaque semaine ou à chaque remise à zéro (X-10, partie anticipée) | Une alarme posée une seule fois revient d'elle-même : « 30 min avant la remise hebdomadaire », « chaque jour à 21:00 », « fin des missions de ravitaillement ». | haute | S-M | Lot 3 (en partie fait) |
| T3 | Quêtes : colonnes Niveau et Zone, tri et filtre de niveau | La liste des quêtes montre le niveau et la zone de chaque quête. Vous la triez par niveau ou par zone et la limitez à une plage de niveaux, par exemple les quêtes annexes de votre niveau dans une zone. | haute | S | Lot 1 |
| T4 | Monstres : drapeau sur la position du monstre, pas au centre de la carte | Sur la fiche d'un monstre qui laisse tomber l'objet, « Ouvrir la carte » pose le drapeau à l'endroit donné par Garland Tools et ouvre la carte Tensho dessus, avec la mention « position communautaire ». | haute | S | Lot 1 |
| T5 | Une séance de relevés en jeu pour ces outils, avant de coder | Une seule séance guidée (mode développeur) fournit d'un coup les preuves qui manquent. Les propositions qui en dépendent se codent ensuite sans aller-retour. | haute | S | Sonde |
| T10 | Accueil : bloc « Aujourd'hui » et tuiles au choix | À l'ouverture de Tensho, vous voyez ce qui compte maintenant : rappels non faits avant la prochaine remise, vaisseaux revenus ou prochain retour, tâches de servants terminées, quêtes suivies, les trois prochaines alarmes. | haute | M | Lot 3 |
| T11 | Quêtes « disponibles maintenant » et quêtes de la zone actuelle | Un filtre montre les quêtes que vous pouvez prendre : non faites, prérequis terminés, pas exclues par un autre choix (cité de départ, Grande Compagnie), niveau atteint. | haute | M | Lot 3 |
| T12 | Chemin vers une quête : prérequis restants, nombre et prochaine à faire | Sur la fiche d'une quête (ou d'un contenu, avec « Débloqué par »), Tensho liste dans l'ordre toutes les quêtes encore à faire pour l'atteindre, leur nombre, et la première à prendre maintenant, avec un bouton carte. | haute | M | Lot 3 |
| T13 | Contenus et quêtes : « Débloqué par » et « Débloque » | Sur la page Butin des contenus et dans le panneau de la Recherche de mission, chaque donjon, défi ou raid dit quelle quête le débloque et si vous l'avez faite, avec un lien vers la quête et son chemin. | haute | M | Sonde |
| T14 | Équipement : préparer le plan d'un autre job que celui porté | Un menu de job sur la page Équipement : vous préparez ou consultez le plan de votre soigneur en étant forgeron. | haute | M | Lot 3 |
| T15 | Revue de mode : tenue de la semaine choisie, essayée en un clic, avec l'endroit de chaque pièce | Vous choisissez une pièce par emplacement parmi celles proposées pour la semaine. | haute | M | Lot 3 |
| T16 | Tâches de servants : une ligne par servant, avec alarme | La ligne « Tâches de servants » se déplie : pour chaque servant, sa tâche finie ou son heure de fin, une cloche, et une alarme « quand toutes sont finies ». | moyenne | S | Après 1.0.0 (en partie fait) |
| T17 | Rappels cochés d'eux-mêmes quand le jeu le dit | Quand le jeu indique 0 commande spéciale ou 0 quête de tribu restante, la case « fait » se coche seule jusqu'à la remise. La coche manuelle reste pour le reste. | moyenne | S | Lot 2 |
| T18 | Discord : heure lue dans le fuseau de chaque membre, et balises [zone], [objet], [et] | Dans un salon partagé de compagnie libre, chaque membre lit l'heure de l'alarme dans son propre fuseau (et « dans 5 minutes »). Vos messages peuvent aussi donner la zone, l'objet et l'heure d'Éorzéa. | moyenne | S | Après 1.0.0 |
| T19 | Alarmes : modifier le nom et l'heure, et « Me rappeler dans 5 min » | Vous renommez une alarme ou changez son heure ET ou sa date sans la recréer. Une alarme qui vient de sonner peut être reportée de quelques minutes, depuis la page Alarmes ou l'infobulle de la barre du serveur. | moyenne | S | Lot 2 |
| T20 | Alarmes : sonner aussi à l'heure exacte | En plus du rappel N minutes avant, l'alarme peut sonner une seconde fois quand la fenêtre s'ouvre : point de récolte, poisson, retour de vaisseau. | moyenne | S | Lot 2 |
| T21 | Personnage : niveaux et expérience de tous les jobs | Les niveaux et l'expérience de tous tes jobs sur la page Personnage. | moyenne | S | Sonde |
| T23 | Compagnie libre : vaisseaux groupés par compagnie, ancien relevé effaçable, valeur du dernier butin | Avec des personnages dans plusieurs compagnies, chaque atelier a son propre cadre (nom de la compagnie, date du relevé). Le relevé d'une compagnie quittée peut être effacé. | moyenne | S | Après 1.0.0 |
| T24 | Discord : un salon par catégorie (plusieurs webhooks) | Les retours de vaisseaux partent dans le salon de la compagnie libre, vos alarmes de récolte dans votre salon privé. | moyenne | M | Après 1.0.0 |
| T25 | Équipement : plusieurs plans nommés par job | Pour un même job, vous gardez « montée de niveau », « artisanat » et « meilleur possible », et passez de l'un à l'autre ; chacun se partage par code. | moyenne | S-M | Après 1.0.0 |
| T26 | Dirigeables : trajet du dernier voyage reconstitué | La colonne Trajet des dirigeables affiche les destinations au lieu de « trajet inconnu ». | moyenne | S | Sonde |
| T27 | Actions de compagnie actives : heure de fin et alarme | Les Rappels affichent les actions de compagnie en cours (bonus d'expérience, de récolte…) et leur heure de fin, avec une cloche pour en relancer une à temps. | moyenne | M | Sonde |
| T28 | Collections : « Obtenus récemment » et menu des sources pour les emotes | Une ligne « Obtenus récemment » liste, avec la date, ce que vous avez gagné depuis le relevé précédent (montures, mascottes, rouleaux…). Le clic droit sur une emote ouvre ses sources, comme pour les montures. | basse | S | Après 1.0.0 |
| T29 | Mini Cactpot : table des gains prouvée et visible | Sous le conseil, le gain de chaque somme de ligne (6 à 24) s'affiche, comme sur le ticket du jeu : vous voyez ce que vise la ligne conseillée. | basse | S | Après 1.0.0 |

### Gros chantiers et écarts laissés

Détail : [G-PROPOSALS](G-PROPOSALS.md), « Écarts laissés ».

| Point | Ce qu'il faudrait d'abord |
|---|---|
| I-15, recherche dans les fenêtres d'inventaire du jeu | Une sonde en lecture seule, en jeu : pointeurs natifs à chaque image |
| I-05, suite des ventes des servants | Un essai de la 0.508 ; décision : comparaison sur clic (appliqué) ou proposée à l'ouverture |
| N-04, loterie immobilière | Une sonde : structure du jeu non trouvée |
| N-07, carte des ressources de l'île | Vérifier en jeu l'unité de X, Y et du rayon |
| N-10, atelier de l'île | Relecture de la conception, puis sonde ; mis de côté (DEC-033) |
| N-12, conditions d'apparition des rangs S | Sources communautaires à décider ensemble |
| N-13, meubles visibles | Lecture des meubles : risques à décider ensemble |
| X-14, outils d'aménagement | Correctifs d'affichage : risques à décider ensemble |
| Dresseur, suite et panneau du Dédale | Mis de côté ; contacter l'équipe Dalamud avant de coder le panneau (DEC-032) |

### Idées repérées en codant

Reprises de [G-PROPOSALS](G-PROPOSALS.md), non triées.

| Repérée en | Idée | Pourquoi pas encore |
|---|---|---|
| 0.344 | Alerte sur le plafond hebdomadaire des mémoquartz (en plus du plafond total) | Hors du choix de l'utilisateur pour I-02 |
| 0.345 | « Où est cet objet ? » sur tous les personnages (infobulle et fiche d'objet), en étendant ItemHoldings | Gros changement des stocks ; à faire après validation en jeu de I-03 |
| 0.345 | Gils des servants dans la vue des personnages | Le jeu ne les donne qu'à l'ouverture de la cloche ; à sonder |
| 0.347 | Liste de tous les objets annotés (page Objets) avec filtre par étiquette | Petit ; à faire avec les autres listes d'objets (I-12) |
| 0.346 | Sous-cotation : afficher aussi le prix moyen de vente récent avant de coller (garde-fou contre une offre trop basse) | Demande un appel Universalis de plus |
| 0.345 | Ne réécrire le fichier d'un personnage que si ses quantités ont changé | Économie d'écritures disque ; aujourd'hui une écriture toutes les 2 minutes par rangement relevé d'office |

Tri des propositions des quatre audits (2026-11-05) : [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md).

### Choix provisoires

Les alternatives aux choix déjà appliqués restent des idées : [G-PROPOSALS](G-PROPOSALS.md), « Choix provisoires » (confirmés par [DEC-026](DECISIONS.md)).
