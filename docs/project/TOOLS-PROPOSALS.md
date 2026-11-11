# Alarms, free company, quests, gear, mini-games: improvement proposals (audit of 2026-11-02, step e)

> *Traduction française plus bas.*

List drawn up by a read-only multi-agent audit (0.330). **Nothing was implemented** at the time of the audit (re-read on 2026-11-03: the proposals made since then, at step g, carry a « Status » line): you choose what we add
(rule of 2026-11-02, [ROADMAP](ROADMAP.md) « Decisions postponed to the end »). The defects found by the same audit are fixed in
0.331 ([CHANGELOG](../../CHANGELOG.md)). Several proposals overlap with gaps already kept for step g)
([GAPS](GAPS.md), I-*): they say so.

Value and size are the audit's estimates. « Probe »: a read of the game or an in-game reading is needed before coding.

## T0 — Real-time alarms: by hand, every day, every week or at every reset (X-10, anticipated part)

- **Status (re-read on 2026-11-05)**: DONE 0.577.
- **Status (re-read on 2026-11-03)**: partly 0.386 (X-10: single alarm at a local date); every day, every week or at every reset not done.
- **Value**: high; main file: `src/TenshoAssistant/Alarms/TenshoAlarm.cs`.
- **What the player gets**: An alarm set only once comes back by itself: « 30 min before the weekly reset », « every day at 21:00 », « end of the supply missions ». The Reminders bell offers « At each daily reset » instead of a single alarm that disappears after ringing. You can also set by hand an alarm at a local time, or a timer (« in 45 min »).
- **How, size, dependencies**: Add AlarmRepeat.RealDaily (local time), RealWeekly (day + time) and Reset (ResetSchedule key: daily 15:00 UTC, supply 20:00 UTC, weekly Tuesday 08:00 UTC, leves 00:00/12:00 UTC). AlarmSchedule.Next computes them (pure function, with tests). « By hand » form: ET time, local time or timer. Copy into the game: ET only, as today (with « lt », the meaning of « rp » is not proven: docs/evidence/0.205-alarm-command.md). Size S-M. Dependencies: none; Discord unchanged (Reminders category). Probe: none. Decision: does the Reminders bell create a recurring alarm by default (recommended: yes).
- **Source**: Alarms/TenshoAlarm.cs:3: AlarmRepeat only knows Once and EorzeaDaily. MainWindow.Reminders.cs:108 and MainWindow.FreeCompany.cs:111 create Once alarms, removed 10 min after their time (AlarmSchedule.RemoveExpired). MainWindow.Alarms.cs:349-362: adding by hand only offers a daily ET time. Reset times: docs/evidence/0.300-calendar-resets.md (Reminders/ResetSchedule.cs). GAPS.md X-10 (« alarms by hand », small, Choice « Add »).

## T1 — Per-character reminders: « done » boxes and treasure map of each character (M-04, anticipated part)

- **Status (re-read on 2026-11-03)**: DONE 0.374 (M-04: « done » boxes per character).
- **Value**: high; main file: `src/TenshoAssistant/Configuration.cs`.
- **What the player gets**: With several characters, ticking « Fashion Report done » or « treasure map opened » on one no longer ticks the others. Each character keeps its 18 h wait and its boxes until the reset.
- **How, size, dependencies**: Key by the ContentId of the logged-in character: Dictionary<ulong,…> for ReminderDone and TreasureMapAt. At the first login, the old values go to the logged-in character (tested migration). When logged out, the page says « Unknown character » and nothing can be ticked. Size S. Dependencies: none; prepares M-04 (view of several characters). Probe: none. Decision: none (already taken for M-04).
- **Source**: Configuration.cs:208-209: ReminderDone and TreasureMapAt exist as a single copy for the whole configuration, with no character identifier. MainWindow.Reminders.cs:51-58 and 94-101 read and write them as they are. docs/evidence/0.300-calendar-resets.md: treasure maps are « specific to the character (18 h after the last map) ». GAPS.md M-04 (Choice: one file per character, like I-03, never an Account ID).

## T2 — Vessel return alarm created automatically at each reading (M-01, voyages part)

- **Status (re-read on 2026-11-03)**: DONE 0.371 (M-01).
- **Value**: high; main file: `src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs`.
- **What the player gets**: As soon as you visit the workshop, each submarine and each airship under way has its return alarm, and its Discord message if you set it, without clicking the bell of each line. A restarted voyage replaces the old alarm.
- **How, size, dependencies**: After Voyages.Merge (VoyageWatcher.Dalamud.cs:122, framework thread), for each vessel under way: a Once alarm with Source=Voyage and a stable key (house + type + slot). It is replaced if the time changes and removed if the vessel is back. Respect AlarmSchedule.Maximum (60) and say so. Setting « Automatic return alarms » (on, tooltip, ↺). Size S. Dependencies: none; the retainers part of M-01 follows « Retainer ventures: one line per retainer ». Probe: none. Decision: already taken (DEC-025).
- **Source**: MainWindow.FreeCompany.cs:109-112: the alarm only exists on a bell click, line by line. GAPS.md M-01 (small, value high, Choice: « Tensho alarm created automatically at each reading, setting on by default; never a game alarm through /alarm automatically »). Return time read from the game: docs/evidence/0.307-fc-voyages.md (ReturnTime).

## T3 — Quests: Level and Zone columns, sort and level filter

- **Status (re-read on 2026-11-05)**: DONE 0.550.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.Quests.cs`.
- **What the player gets**: The quest list shows the level and the zone of each quest. You sort it by level or by zone and limit it to a range of levels, for example the side quests of your level in a zone.
- **How, size, dependencies**: Two more columns, with ImGuiTableFlags.Sortable. The sort is redone only when the input changes, like questMatches (nothing heavy in Draw). « Level from … to … » fields with tooltip and ↺, remembered. Size S. Dependencies: none. Probe: none. Decision: none.
- **Source**: MainWindow.Quests.cs:143-150: table of 3 columns (quest, category, progress), without sort. QuestEntry already carries Level, AdditionalLevel and Zone (Quests/QuestService.cs:20-22), which only the detail sheet shows (MainWindow.Quests.cs:209-214).

## T4 — Monsters: flag on the monster's position, not at the centre of the map

- **Status (re-read on 2026-11-05)**: DONE 0.551.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.Monsters.cs`.
- **What the player gets**: On the sheet of a monster that drops the item, « Open the map » places the flag at the spot given by Garland Tools and opens the Tensho map on it, with the mention « community position ». Today the map opens at the centre.
- **How, size, dependencies**: If X and Y are known: convert the map coordinates into a world position (Maps/MapProjection) then call ShowOnTenshoMap(map, position). Otherwise, keep the current behaviour. The tooltip says the source is Garland and that the spawn is not proven. Size S. Dependencies: Tensho map. Probe: none. Decision: none.
- **Source**: MainWindow.Monsters.cs:183: the button sets mapX=mapY=20.5 and opens the Maps page, even when the sheet shows the position (MainWindow.Monsters.cs:150-151; Garland « coords » coordinates read in Monsters/MonsterDocuments.cs:100). ShowOnTenshoMap and FlagAt already exist (MainWindow.MapView.cs:815, MainWindow.MapLocalLists.cs:57).

## T5 — An in-game reading session for these tools, before coding

- **Value**: high; main file: `docs/project/TESTING.md`.
- **What the player gets**: A single guided session (developer mode) provides all the missing evidence at once. The proposals that depend on it are then coded without back and forth.
- **How, size, dependencies**: First tools/ApiProbe outside the game (owners, types, units), then a list of in-game readings in TESTING.md; the logs go into docs/evidence/. Size S. Dependencies: none. Decision: timing of the session (recommended: before the treasure map, the roulettes and the completed duties).
- **Source**: Probes requested by the proposals of this batch (names found on 2026-11-02 in FFXIVClientStructs.dll and Lumina.Excel.dll, owners and types not proven): GetNextMapAllowanceTimestamp (treasure map), IsRouletteComplete (roulettes), IsInstanceContentCompleted (completed duties), ContentFinderCondition.UnlockQuest and Quest.InstanceContentUnlock / ClassJobUnlock (unlocks), ClassJobLevels / ClassJobExperience (job levels), Condition / SpiritbondOrCollectability (condition and spiritbond), HousingWorkshopAirshipGathered (airship route), FreeCompanyActionTimer (company actions), name of the company at the workshop, roulette reset time, Mini Cactpot payout table. Same method as CRAFT-GATHER-PROPOSALS.md C33.

## T6 — Treasure map: next allowance read from the game (M-02, anticipated part)

- **Status (re-read on 2026-11-03)**: DONE 0.372 (M-02).
- **Value**: high; main file: `src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs`.
- **What the player gets**: The « Treasure maps » line of the Reminders gives the real time of the next map, like the game's Calendar, without a click on « opened ». The manual button stays as a fallback.
- **How, size, dependencies**: tools/ApiProbe probe (owner, type, unit), evidence in docs/evidence/. Reading in GameTimers.Read, on the framework thread and guarded R-14: value absent or out of bounds = « unavailable after a game update », with fallback to the button. Size S. Dependencies: « Per-character reminders », reading session. Probe: yes, plus a reading compared with the Calendar. Decision: none.
- **Source**: MainWindow.Reminders.cs:51-58: the 18 h wait is started by hand (comment: « the game gives no time that Tensho can read »). The game's Calendar does show it (docs/evidence/0.300-calendar-resets.md). The names GetNextMapAllowanceTimestamp and NextMapAllowanceDateTime are present in FFXIVClientStructs.dll (string found on 2026-11-02; owner and signature to confirm). GAPS.md M-02 (« treasure map allowance », Choice « Add »).

## T7 — Daily roulettes done or not, in the Reminders (C-02, anticipated part)

- **Status (re-read on 2026-11-03)**: DONE 0.356 (C-02).
- **Value**: high; main file: `src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs`.
- **What the player gets**: The Reminders list your roulettes (expert, levelling, alliance…) with a tick read from the game. You see at a glance which ones remain before the daily reset, with an alarm.
- **How, size, dependencies**: Probe of the owner and of the expected index, evidence in docs/evidence/. Read once per second with the other counters, guarded R-14; a collapsible line « Roulettes (n left) ». Read in game the reset time (Duty Finder window) before displaying it. Size S. Dependencies: reading session; the role bonus alert stays with C-02 in g). Decision: none (choice made).
- **Source**: GAPS.md C-02 (small, value high, Choice « Add »; depends on GameTimers and ResetSchedule). The name IsRouletteComplete is present in FFXIVClientStructs.dll (found on 2026-11-02, owner to confirm); it is used nowhere in src/. Roulette names: ContentRoulette sheet (to probe). The roulette reset time is not in docs/evidence/0.300-calendar-resets.md.

## T8 — Completed duties or not, with a « not completed » filter (C-01, anticipated part)

- **Status (re-read on 2026-11-03)**: DONE 0.355 (C-01).
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.Duties.cs`.
- **What the player gets**: The duty list marks ✓ those you have already completed and can show only the others. The Duty Finder panel says it too.
- **How, size, dependencies**: In-game probe on a done duty and a not-done one (evidence in docs/evidence/). Reading on the framework thread when the page opens, then at each zone change; copy into a set of identifiers, guarded R-14 (« unavailable after a game update »). Size S. Dependencies: reading session. Decision: none (choice made).
- **Source**: GAPS.md C-01 (small, value high, Choice « Add »; probe of the identifier expected by IsInstanceContentCompleted). The name IsInstanceContentCompleted is present in FFXIVClientStructs.dll but is not used in src/. DutyDefinition already keeps InstanceId (Duties/DutyCatalog.cs:5).

## T9 — Gear: upgrades you already own (M-12, anticipated part)

- **Status (re-read on 2026-11-03)**: DONE 0.383 (M-12).
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.Gear.cs`.
- **What the player gets**: For the worn job, Tensho flags per slot any piece with a higher item level than the one worn, usable at your level, already in your bags, the armoury chest, a saddlebag or with a retainer. It says where to take it from.
- **How, size, dependencies**: Computation outside Draw, redone when the worn gear, the stocks or the job change: GearRules.Fits, level ≤ job level, item level > worn piece. « Owned upgrades » list with the storage and a « Try on » button. Size S-M. Dependencies: storages read by the Bank. Probe: none. Decision: count the glamour dresser and the armoire (recommended: no, these pieces cannot be worn directly).
- **Source**: GAPS.md M-12 (« possible upgrades for all jobs », Choice « Add »). The page already shows possession (DrawOwnedMark, MainWindow.Gear.cs:515) and the pieces to obtain (MainWindow.Gear.cs:535), but not the owned pieces better than those worn. ItemHoldings gives the detail per storage (Inventory/ItemHoldings.cs:4-14).

## T10 — Home: « Today » block and tiles of your choice

- **Status (re-read on 2026-11-05)**: DONE 0.599.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.Home.cs`.
- **What the player gets**: When Tensho opens, you see what matters now: reminders not done before the next reset, vessels back or next return, retainer ventures finished, followed quests, the next three alarms. Each line opens its page. You also choose the tiles of the home page (Reminders, Free company, Gear…).
- **How, size, dependencies**: Move the reading of GameTimers into a small service on Framework.Update (once per second, guarded R-14, immutable copy), read by the Reminders and the Home. Build the « Today » block once per second (nothing heavy in Draw). List of tiles in the configuration: order and box per tile, with tooltip and ↺. Size M. Dependencies: « Per-character reminders », return alarms, « Retainer ventures ». Probe: none. Decision: lines shown by default (recommended: all, each can be hidden).
- **Source**: MainWindow.Home.cs:39-56: only the calculations in progress, the next alarm and the errors. MainWindow.Home.cs:70-82: ten fixed tiles, without Reminders, Free company, Gear or Fashion Report. The game counters are only read while the Reminders page is drawn (MainWindow.Reminders.cs:26-36).

## T11 — Quests « available now » and quests of the current zone

- **Status (re-read on 2026-11-05)**: DONE 0.582.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.Quests.cs`.
- **What the player gets**: A filter shows the quests you can take: not done, prerequisites completed, not excluded by another choice (starting city, Grand Company), level reached. A second filter limits to the zone where you are, with the quest giver on the map. The conditions that Tensho cannot read are flagged (« condition not interpreted ») and never count as met.
- **How, size, dependencies**: Computation on the published list, not in Draw: unconfirmed + CompletionEvidence + absent from questOtherCity. Level compared with that of the required job if « Levels and experience of all jobs » is done, otherwise only displayed. Unknown join or required step: « Verify ». Zone: current territory, already read by the map. Size M. Dependencies: job levels for the level filter; ClassJobCategory column of Quest to probe. Decision: include repeatable quests (recommended: not by default).
- **Source**: MainWindow.Quests.cs:112-118: only the filters All, In progress, Completed and Unconfirmed. QuestLinks.CompletionEvidence (Quests/QuestService.cs:53-58) already knows whether the prerequisites are done; QuestAlternatives.NotPlayable (0.286) rules out the options not taken; PreviousQuestStep stays raw (QuestService.cs:17). Giver and map: MainWindow.Quests.cs:215-227.

## T12 — Path to a quest: remaining prerequisites, count and next one to do

- **Status (re-read on 2026-11-05)**: DONE 0.583.
- **Value**: high; main file: `src/TenshoAssistant/Quests/QuestService.cs`.
- **What the player gets**: On the sheet of a quest (or of a duty, with « Unlocked by »), Tensho lists in order all the quests still to do to reach it, their number, and the first one to take now, with a map button. Useful for a main scenario, a job or a dungeon to unlock.
- **How, size, dependencies**: Bounded traversal on a cancellable worker: cycle guard, node ceiling, result published and cached per quest and per observation. Join 1 = all branches; join 2 = the branch already done, otherwise the shortest; unknown mode = stop with « condition not interpreted ». Collapsible list in the sheet, a click opens the quest. Size M. Dependencies: none; serves « Unlocked by ». Probe: none. Decision: none.
- **Source**: Quests/QuestService.cs:32: « No recursive traversal ». The sheet only shows the direct prerequisites (MainWindow.Quests.cs:230-245) and the direct following quests. The modes of PreviousQuestJoin 1 (all) and 2 (at least one) are already interpreted (QuestService.cs:53-58).

## T13 — Duties and quests: « Unlocked by » and « Unlocks »

- **Value**: high; main file: `src/TenshoAssistant/Duties/DutyCatalogLoader.Dalamud.cs`.
- **What the player gets**: On the Duty loot page and in the Duty Finder panel, each dungeon, trial or raid says which quest unlocks it and whether you have done it, with a link to the quest and its path. The sheet of a quest says what it unlocks (duty, job).
- **How, size, dependencies**: tools/ApiProbe probe of the columns (type, null values), evidence in docs/evidence/. Add UnlockQuestId to DutyDefinition, copied on the framework like the rest; reverse index quest → duties built with the quest list. « Unlocked by … (done / not done / unknown) » line in MainWindow.Duties.cs and MainWindow.DutyFinder.cs. Size M. Dependencies: « Path to a quest ». Probe: yes. Decision: none.
- **Source**: Duties/DutyCatalogLoader.Dalamud.cs:47-52 only copies the name, the type, the level and the icon. The columns UnlockQuest (ContentFinderCondition), InstanceContentUnlock and ClassJobUnlock (Quest) have their name in Lumina.Excel.dll (found on 2026-11-02) but are read nowhere in src/. No evidence in docs/evidence.

## T14 — Gear: prepare the plan of a job other than the one worn

- **Status (re-read on 2026-11-05)**: DONE 0.580.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.Gear.cs`.
- **What the player gets**: A job menu on the Gear page: you prepare or consult the plan of your healer while being a blacksmith. Pieces, materia, pieces to obtain and try-on work; the real attributes are marked « read on the worn job only ».
- **How, size, dependencies**: Displayed job = chosen job (by default, the worn job); catalogue filtered for that job. If that job is not worn, the attributes column only shows the sum of the pieces and the materia, with no invented real value. Size M. Dependencies: job levels for the level of that job (otherwise a level field). Probe: none. Decision: none.
- **Source**: MainWindow.Gear.cs:39 and 45: the plan and the materia are taken for state.JobId, the worn job. A code imported for another job is saved but stays invisible as long as that job is not worn (MainWindow.Gear.cs:147, « ImportedOtherJob »).

## T15 — Fashion Report: outfit of the chosen week, tried on in one click, with the place of each piece

- **Status (re-read on 2026-11-05)**: DONE 0.581.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.FashionWeek.cs`.
- **What the player gets**: You choose one piece per slot among those proposed for the week. « Try on the outfit » puts them all in the fitting room, and a list says where each piece is (bags, armoury chest, glamour dresser, armoire, retainer), which ones are missing and their sources.
- **How, size, dependencies**: Choice per slot kept until the weekly reset (Tuesday 08:00 UTC, docs/evidence/0.300-calendar-resets.md). « Try on the outfit » button, on click only. Table slot → piece → storage, recomputed when the choice or the stocks change. Size M. Dependencies: none. Probe: none. Decision: none.
- **Source**: MainWindow.FashionWeek.cs:93 and 129-132: the pieces are sorted « owned first », without choice or grouped try-on. Try-on already exists (Plugin.cs:266, TryOnItem; « Try on the plan » on the Gear page, MainWindow.Gear.cs:97-100). ItemHoldings gives the storage (Inventory/ItemHoldings.cs:4).

## T16 — Retainer ventures: one line per retainer, with alarm

- **Status (re-read on 2026-11-05)**: DONE 0.570.
- **Status (re-read on 2026-11-03)**: partly 0.371 (automatic alarm at the end of each retainer venture) and 0.374 (view of your characters).
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.Reminders.cs`.
- **What the player gets**: The « Retainer ventures » line unfolds: for each retainer, its finished venture or its end time, a bell, and an alarm « when all are finished ».
- **How, size, dependencies**: Collapsible line (TreeNode) in the same table; Once alarms with Source=Reminder (Discord category « Reminders »); copy of the end times kept per character for the Home. Size S. Dependencies: « Per-character reminders ». The automatic alarms of M-01 for retainers stay in g), or follow the same option as the vessels. Probe: none. Decision: none.
- **Source**: Reminders/GameTimers.Dalamud.cs:30-40 already reads the name and the end time of each retainer, but MainWindow.Reminders.cs:67-71 only shows two numbers and the next end. This data is only known after the retainer list has been opened (RetainerManager.IsReady).

## T17 — Reminders ticked by themselves when the game says so

- **Status (re-read on 2026-11-05)**: DONE 0.558.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.Reminders.cs`.
- **What the player gets**: When the game indicates 0 custom deliveries or 0 allied society quests left, the « done » box ticks itself until the reset. The manual tick stays for the rest.
- **How, size, dependencies**: Done = manual tick, or counter read at 0 since the last reset (marked « from the game »); an unknown counter allows no deduction. Setting « Tick from the game » (on, tooltip, ↺). Size S. Dependencies: none. Probe: none. Decision: on by default (recommended: yes).
- **Source**: MainWindow.Reminders.cs:46 and 49: DeliveriesLeft and AlliedQuestsLeft are read and displayed, but the « done » box (MainWindow.Reminders.cs:94-101) is never ticked from them.

## T18 — Discord: time read in the time zone of each member, and tags [zone], [objet], [et]

- **Status (re-read on 2026-11-05)**: DONE 0.564.
- **Value**: medium; main file: `src/TenshoAssistant/Alarms/DiscordWebhook.cs`.
- **What the player gets**: In a shared free company channel, each member reads the alarm time in their own time zone (and « in 5 minutes »). Your messages can also give the zone, the item and the Eorzea time.
- **How, size, dependencies**: New tags [discordtime] (<t:unix:t>), [relative] (<t:unix:R>), [zone], [objet] and [et]. The names are resolved on the framework thread when the alarm rings, then passed to the notifier. [time] does not change. Format tests. Size S. Dependencies: none. Probe: none (Discord syntax). Decision: change the default template (recommended: no, keep the current one).
- **Source**: Alarms/DiscordWebhook.cs:116: [time] is your local time in HH:mm format. Only tags: [label], [minutes], [time], [character]. TenshoAlarm already keeps TerritoryId, ItemId and EtMinute (Alarms/TenshoAlarm.cs:23-28). USER-REQUESTS R-12 (removed in 0.1088, git history): « see more if possible or interesting ».

## T19 — Alarms: edit the name and the time, and « Remind me again after 5 min »

- **Status (re-read on 2026-11-05)**: DONE 0.557.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.Alarms.cs`.
- **What the player gets**: You rename an alarm or change its ET time or its date without recreating it. An alarm that has just rung can be postponed by a few minutes, from the Alarms page or the tooltip of the server bar.
- **How, size, dependencies**: Name and time fields, saved when leaving the field, as for Discord. For an alarm copied into the game, a message says to correct the game's one by hand (Alarms.DeleteInGame). « Snooze » creates a Once alarm with the same name; adjustable duration (5 min, tooltip, ↺). Size S. Dependencies: none. Probe: none. Decision: default snooze duration (recommended: 5 min).
- **Source**: MainWindow.Alarms.cs:307-331: each alarm only has its box, its reminder and « Delete ». No snooze possible: AlarmService.Ring marks the occurrence as handled (Alarms/AlarmService.Dalamud.cs:158).

## T20 — Alarms: also ring at the exact time

- **Status (re-read on 2026-11-05)**: DONE 0.556.
- **Value**: medium; main file: `src/TenshoAssistant/Alarms/AlarmService.Dalamud.cs`.
- **What the player gets**: In addition to the reminder N minutes before, the alarm can ring a second time when the window opens: gathering point, fish, vessel return.
- **How, size, dependencies**: Per-alarm option, plus a default setting « Ring again at the exact time » (off, tooltip, ↺). A second « on time handled » state avoids ringing twice; Discord only at the first ring (rate limit). Check tests. Size S. Dependencies: none. Probe: none. Decision: also send the second ring on Discord (recommended: no).
- **Source**: Alarms/AlarmService.Dalamud.cs:147-176 and Alarms/TenshoAlarm.cs:93-101: an occurrence only rings once, at the start of the reminder (LastOccurrence).

## T21 — Character: levels and experience of all jobs

- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.Character.cs`.
- **What the player gets** (Character page removed in 0.341: to be placed in the Gear page): The page lists your jobs with their level, an experience bar, and those not yet unlocked. Other tools use it: available quests, plan of another job, crafting simulation.
- **How, size, dependencies**: Probe (fields, index, value of a non-unlocked job), evidence in docs/evidence/. Copy once per second on the framework thread in CharacterStatsReader, guarded R-14; table per role. Size S. Dependencies: reading session. Decision: none.
- **Source**: MainWindow.Character.cs:16-40: only the worn job is displayed. ClassJobLevels and ClassJobExperience have their name in FFXIVClientStructs.dll (found on 2026-11-02) but are not used in src/. The job → index mapping is to be proven (probably the ClassJob.ExpArrayIndex column).

## T22 — Gear: condition and spiritbond of worn pieces (M-06, display part)

- **Status (re-read on 2026-11-03)**: DONE 0.376 (M-06).
- **Value**: medium; main file: `src/TenshoAssistant/Gear/GearLoader.Dalamud.cs`.
- **What the player gets**: On the Gear page, each worn piece shows its condition (%) and its spiritbond. A piece to repair, or ready for materia extraction, stands out.
- **How, size, dependencies**: Probe the scales (piece at 100% then worn, spiritbond at 100%), evidence in docs/evidence/. Two more fields in EquippedPiece, copied at the same reading and guarded R-14. Display only: the alert and the bar stay in M-06. Size S. Dependencies: reading session. Decision: none.
- **Source**: GAPS.md M-06 (Choice: Tensho alert and on-screen bar, adjustable thresholds). Gear/GearLoader.Dalamud.cs:127 already reads the EquippedItems container, but EquippedPiece (Gear/GearCatalog.cs:34) keeps neither the condition nor the spiritbond. The names Condition and SpiritbondOrCollectability are present in FFXIVClientStructs.dll; their scale remains to be proven.

## T23 — Free company: vessels grouped by company, erasable old reading, value of the last loot

- **Status (re-read on 2026-11-05)**: DONE 0.603.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.FreeCompany.cs`.
- **What the player gets**: With characters in several companies, each workshop has its own frame (company name, reading date). The reading of a company you have left can be erased. Each returned vessel shows the value of its loot at the NPC price.
- **How, size, dependencies**: Read the company name at the reading (source to probe; failing that, « Workshop n »). One frame (Header) per house; « Erase this read » button with two clicks. Value = Σ quantity × NPC price (the market price will come with f) 2). Size S. Dependencies: none. Probe: company name. Decision: none.
- **Source**: MainWindow.FreeCompany.cs:39-50: grouping by vessel type only. FreeCompany/VoyageWatcher.Dalamud.cs:122 merges the readings by HouseId, so several workshops get mixed, with the same slot numbers. The NPC prices are already in the voyage catalogue (VoyageWatcher.Dalamud.cs:17-20).

## T24 — Discord: one channel per category (several webhooks)

- **Status (re-read on 2026-11-05)**: DONE 0.598.
- **Value**: medium; main file: `src/TenshoAssistant/Alarms/DiscordWebhook.cs`.
- **What the player gets**: Vessel returns go to the free company channel, your gathering alarms to your private channel.
- **How, size, dependencies**: Optional address per category (empty = main address). Each one is masked, checked by IsValidUrl, never written in the logs nor in the Diagnostics report. Queue and rate pause specific to each address; « Send a test » per address. Size M. Dependencies: none. Probe: none. Decision: number of addresses (recommended: one per existing category).
- **Source**: Alarms/DiscordWebhook.cs:14-30: a single address for all categories. R-12 (ROADMAP; its wording was in USER-REQUESTS, removed in 0.1088) puts « several webhooks » to study.

## T25 — Gear: several named plans per job

- **Status (re-read on 2026-11-05)**: DONE 0.593.
- **Value**: medium; main file: `src/TenshoAssistant/Configuration.cs`.
- **What the player gets**: For the same job, you keep « levelling », « crafting » and « best possible », and switch from one to the other; each one is shared by code.
- **How, size, dependencies**: Named list per job (name, pieces, materia) and choice of the active plan. Migration of the old plan into « Plan 1 », with a test. Duplicate, rename, delete; the tooltip says that deletion is immediate (rule 0.325). Size S-M. Dependencies: none. Probe: none. Decision: maximum number of plans (recommended: 10 per job).
- **Source**: Configuration.cs:218 and 228: GearPlans and GearMateria only keep one plan per job, and « Clear the plan » erases everything (MainWindow.Gear.cs:107).

## T26 — Airships: route of the last voyage reconstructed

- **Value**: medium; main file: `src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs`.
- **What the player gets**: The Route column of the airships shows the destinations instead of « route unknown ».
- **How, size, dependencies**: Probe HousingWorkshopAirshipGathered for a point field (like Point on the submarine side). If it exists: route = distinct points of the finds, names taken from AirshipExplorationPoint, with the mention « from the loot ». Otherwise, stay « unknown ». Size S after the probe. Dependencies: reading session; useful for the airship planner of f) 3. Decision: none.
- **Source**: docs/evidence/0.307-fc-voyages.md: « No list of route points for the airships »; MainWindow.FreeCompany.cs:74 displays RouteUnknown. The airship finds are already read (HousingWorkshopAirshipGathered, VoyageWatcher.Dalamud.cs:94-103), but nothing proves that they carry a route point.

## T27 — Active company actions: end time and alarm

- **Value**: medium; main file: `src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs`.
- **What the player gets**: The Reminders show the company actions in progress (experience bonus, gathering bonus…) and their end time, with a bell to relaunch one in time.
- **How, size, dependencies**: Probe the owner and the moment when the game fills the data (perhaps only when the company window is opened). Reminders line guarded R-14, with the date of the reading. Size M. Dependencies: reading session. Decision: keep the proposal if the data only exists when opened (recommended: yes, with the date of the reading).
- **Source**: Nothing reads them in src/ (search done). The names FreeCompanyActionTimer and CompanyActionTimer are present in FFXIVClientStructs.dll (found on 2026-11-02, owner unknown). Action names: CompanyAction sheet (to probe). Rule of GAPS.md: data that the game only sends when a window is opened is only read after an opening made by the player.

## T28 — Collections: « Recently obtained » and sources menu for emotes

- **Status (re-read on 2026-11-05)**: DONE 0.602.
- **Value**: low; main file: `src/TenshoAssistant/MainWindow.Collections.cs`.
- **What the player gets**: A « Recently obtained » line lists, with the date, what you have gained since the previous reading (mounts, minions, orchestrion rolls…). Right-clicking an emote opens its sources, as for mounts.
- **How, size, dependencies**: At each published observation, compare with the already recorded obtained identifiers (per character, 200 entries at most, kept 90 days); list at the top of the page. Add Emote to the sources menu. Size S. Dependencies: « Per-character reminders » (key per character). Probe: none. Decision: retention period (recommended: 90 days).
- **Source**: MainWindow.Collections.cs:92: totals only, no history. MainWindow.Collections.cs:153-154: CollectionSourcesMenu serves mounts, minions, cards and orchestrion rolls, not emotes, whose tooltip does have the sources (line 149).

## T29 — Mini Cactpot: payout table proven and visible

- **Status (re-read on 2026-11-05)**: DONE 0.571.
- **Value**: low; main file: `src/TenshoAssistant/MiniGames/MiniCactpotSolver.cs`.
- **What the player gets**: Under the advice, the payout of each line sum (6 to 24) is displayed, as on the game's ticket: you see what the advised line aims at.
- **How, size, dependencies**: Look for the table in the game data (probe of the Gold Saucer sheets) or read it on the ticket in game; evidence in docs/evidence/ and a test that compares. Collapsible table under the advice. Size S. Dependencies: reading session. Decision: none.
- **Source**: MiniGames/MiniCactpotSolver.cs:9: table of 25 payouts written in the code, credited to the game (Credits/CreditCatalog.cs:38-39) but with no evidence in docs/evidence. Rule: no game data without a cited source.

---

# Alarmes, compagnie libre, quêtes, équipement, mini-jeux : propositions d'amélioration (audit du 2026-11-02, étape e)

Liste établie par un audit multi-agents en lecture seule (0.330). **Rien n'était implémenté** à l'audit (relu le 2026-11-03 : les propositions faites depuis, à l'étape g, portent une ligne « État ») : vous choisissez ce qu'on ajoute
(règle du 2026-11-02, [ROADMAP](ROADMAP.md) « Décisions reportées à la fin »). Les défauts trouvés par le même audit sont corrigés en
0.331 ([CHANGELOG](../../CHANGELOG.md)). Plusieurs propositions recoupent des manques déjà retenus pour l'étape g)
([GAPS](GAPS.md), I-*) : elles le disent.

Valeur et taille sont les estimations de l'audit. « Sonde » : une lecture du jeu ou un relevé en jeu est nécessaire avant de coder.

## T0 — Alarmes en heure réelle : à la main, chaque jour, chaque semaine ou à chaque remise à zéro (X-10, partie anticipée)

- **État (relu le 2026-11-05)** : FAIT 0.577.
- **État (relu le 2026-11-03)** : en partie 0.386 (X-10 : alarme unique à une date locale) ; chaque jour, chaque semaine ou à chaque remise non fait.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Alarms/TenshoAlarm.cs`.
- **Ce que le joueur obtient** : Une alarme posée une seule fois revient d'elle-même : « 30 min avant la remise hebdomadaire », « chaque jour à 21:00 », « fin des missions de ravitaillement ». La cloche des Rappels propose « à chaque remise à zéro » au lieu d'une alarme unique qui disparaît après avoir sonné. Vous pouvez aussi poser à la main une alarme à une heure locale, ou un minuteur (« dans 45 min »).
- **Comment, taille, dépendances** : Ajouter AlarmRepeat.RealDaily (heure locale), RealWeekly (jour + heure) et Reset (clé de ResetSchedule : quotidien 15:00 UTC, ravitaillement 20:00 UTC, hebdomadaire mardi 08:00 UTC, mandats 00:00/12:00 UTC). AlarmSchedule.Next les calcule (fonction pure, avec tests). Formulaire « à la main » : heure ET, heure locale ou minuteur. Copie dans le jeu : ET seulement, comme aujourd'hui (avec « lt », le sens de « rp » n'est pas prouvé : docs/evidence/0.205-alarm-command.md). Taille S-M. Dépendances : aucune ; Discord inchangé (catégorie Rappels). Sonde : aucune. Décision : la cloche des Rappels crée-t-elle une alarme récurrente par défaut (recommandé : oui).
- **Source** : Alarms/TenshoAlarm.cs:3 : AlarmRepeat ne connaît que Once et EorzeaDaily. MainWindow.Reminders.cs:108 et MainWindow.FreeCompany.cs:111 créent des alarmes Once, retirées 10 min après leur heure (AlarmSchedule.RemoveExpired). MainWindow.Alarms.cs:349-362 : l'ajout à la main ne propose qu'une heure ET quotidienne. Heures de remise : docs/evidence/0.300-calendar-resets.md (Reminders/ResetSchedule.cs). GAPS.md X-10 (« alarmes à la main », petit, Choix « Ajouter »).

## T1 — Rappels par personnage : cases « fait » et carte au trésor de chaque personnage (M-04, partie anticipée)

- **État (relu le 2026-11-03)** : FAIT 0.374 (M-04 : cases « fait » par personnage).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Configuration.cs`.
- **Ce que le joueur obtient** : Avec plusieurs personnages, cocher « Revue de mode faite » ou « carte au trésor ouverte » sur l'un ne coche plus les autres. Chaque personnage garde son attente de 18 h et ses cases jusqu'à la remise à zéro.
- **Comment, taille, dépendances** : Clé par ContentId du personnage connecté : Dictionary<ulong,…> pour ReminderDone et TreasureMapAt. À la première connexion, les anciennes valeurs passent au personnage connecté (migration testée). Hors connexion, la page dit « personnage inconnu » et rien n'est cochable. Taille S. Dépendances : aucune ; prépare M-04 (vue de plusieurs personnages). Sonde : aucune. Décision : aucune (déjà prise pour M-04).
- **Source** : Configuration.cs:208-209 : ReminderDone et TreasureMapAt existent en un seul exemplaire pour toute la configuration, sans identifiant de personnage. MainWindow.Reminders.cs:51-58 et 94-101 les lisent et les écrivent tels quels. docs/evidence/0.300-calendar-resets.md : les cartes au trésor sont « propre[s] au personnage (18 h après la dernière carte) ». GAPS.md M-04 (Choix : un fichier par personnage, comme I-03, jamais d'Account ID).

## T2 — Alarme de retour des vaisseaux créée d'office à chaque relevé (M-01, partie expéditions)

- **État (relu le 2026-11-03)** : FAIT 0.371 (M-01).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs`.
- **Ce que le joueur obtient** : Dès que vous passez à l'atelier, chaque sous-marin et chaque dirigeable en route a son alarme de retour, et son message Discord si vous l'avez réglé, sans cliquer sur la cloche de chaque ligne. Un trajet relancé remplace l'ancienne alarme.
- **Comment, taille, dépendances** : Après Voyages.Merge (VoyageWatcher.Dalamud.cs:122, thread du framework), pour chaque vaisseau en route : une alarme Once avec Source=Voyage et une clé stable (maison + type + emplacement). Elle est remplacée si l'heure change et retirée si le vaisseau est revenu. Respecter AlarmSchedule.Maximum (60) et le dire. Réglage « Alarmes de retour automatiques » (activé, infobulle, ↺). Taille S. Dépendances : aucune ; la partie servants de M-01 suit « Tâches de servants : une ligne par servant ». Sonde : aucune. Décision : déjà prise (DEC-025).
- **Source** : MainWindow.FreeCompany.cs:109-112 : l'alarme n'existe que sur clic de la cloche, ligne par ligne. GAPS.md M-01 (petit, valeur haute, Choix : « alarme Tensho créée d'office à chaque relevé, réglage activé par défaut ; jamais d'alarme du jeu par /alarm d'office »). Heure de retour lue dans le jeu : docs/evidence/0.307-fc-voyages.md (ReturnTime).

## T3 — Quêtes : colonnes Niveau et Zone, tri et filtre de niveau

- **État (relu le 2026-11-05)** : FAIT 0.550.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.Quests.cs`.
- **Ce que le joueur obtient** : La liste des quêtes montre le niveau et la zone de chaque quête. Vous la triez par niveau ou par zone et la limitez à une plage de niveaux, par exemple les quêtes annexes de votre niveau dans une zone.
- **Comment, taille, dépendances** : Deux colonnes de plus, avec ImGuiTableFlags.Sortable. Le tri est refait seulement quand l'entrée change, comme questMatches (rien de lourd dans Draw). Champs « niveau de … à … » avec infobulle et ↺, mémorisés. Taille S. Dépendances : aucune. Sonde : aucune. Décision : aucune.
- **Source** : MainWindow.Quests.cs:143-150 : tableau à 3 colonnes (quête, catégorie, progression), sans tri. QuestEntry porte déjà Level, AdditionalLevel et Zone (Quests/QuestService.cs:20-22), que seule la fiche affiche (MainWindow.Quests.cs:209-214).

## T4 — Monstres : drapeau sur la position du monstre, pas au centre de la carte

- **État (relu le 2026-11-05)** : FAIT 0.551.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.Monsters.cs`.
- **Ce que le joueur obtient** : Sur la fiche d'un monstre qui laisse tomber l'objet, « Ouvrir la carte » pose le drapeau à l'endroit donné par Garland Tools et ouvre la carte Tensho dessus, avec la mention « position communautaire ». Aujourd'hui la carte s'ouvre au centre.
- **Comment, taille, dépendances** : Si X et Y sont connus : convertir les coordonnées de carte en position monde (Maps/MapProjection) puis appeler ShowOnTenshoMap(map, position). Sinon, garder le comportement actuel. L'infobulle dit que la source est Garland et que l'apparition n'est pas prouvée. Taille S. Dépendances : carte Tensho. Sonde : aucune. Décision : aucune.
- **Source** : MainWindow.Monsters.cs:183 : le bouton met mapX=mapY=20.5 et ouvre la page Cartes, même quand la fiche affiche la position (MainWindow.Monsters.cs:150-151 ; coordonnées « coords » de Garland lues dans Monsters/MonsterDocuments.cs:100). ShowOnTenshoMap et FlagAt existent déjà (MainWindow.MapView.cs:815, MainWindow.MapLocalLists.cs:57).

## T5 — Une séance de relevés en jeu pour ces outils, avant de coder

- **Valeur** : haute ; fichier principal : `docs/project/TESTING.md`.
- **Ce que le joueur obtient** : Une seule séance guidée (mode développeur) fournit d'un coup les preuves qui manquent. Les propositions qui en dépendent se codent ensuite sans aller-retour.
- **Comment, taille, dépendances** : D'abord tools/ApiProbe hors jeu (propriétaires, types, unités), puis une liste de relevés en jeu dans TESTING.md ; les journaux vont dans docs/evidence/. Taille S. Dépendances : aucune. Décision : moment de la séance (recommandé : avant la carte au trésor, les missions aléatoires et les contenus terminés).
- **Source** : Sondes demandées par les propositions de ce lot (noms relevés le 2026-11-02 dans FFXIVClientStructs.dll et Lumina.Excel.dll, propriétaires et types non prouvés) : GetNextMapAllowanceTimestamp (carte au trésor), IsRouletteComplete (missions aléatoires), IsInstanceContentCompleted (contenus terminés), ContentFinderCondition.UnlockQuest et Quest.InstanceContentUnlock / ClassJobUnlock (déblocages), ClassJobLevels / ClassJobExperience (niveaux des jobs), Condition / SpiritbondOrCollectability (état et symbiose), HousingWorkshopAirshipGathered (trajet des dirigeables), FreeCompanyActionTimer (actions de compagnie), nom de la compagnie à l'atelier, heure de remise des roulettes, table des gains du Mini Cactpot. Même méthode que CRAFT-GATHER-PROPOSALS.md C33.

## T6 — Carte au trésor : prochaine allocation lue dans le jeu (M-02, partie anticipée)

- **État (relu le 2026-11-03)** : FAIT 0.372 (M-02).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs`.
- **Ce que le joueur obtient** : La ligne « Cartes au trésor » des Rappels donne l'heure réelle de la prochaine carte, comme le Calendrier du jeu, sans clic sur « ouverte ». Le bouton manuel reste en secours.
- **Comment, taille, dépendances** : Sonde tools/ApiProbe (propriétaire, type, unité), preuve dans docs/evidence/. Lecture dans GameTimers.Read, sur le thread du framework et gardée R-14 : valeur absente ou hors bornes = « indisponible après une mise à jour du jeu », avec repli sur le bouton. Taille S. Dépendances : « Rappels par personnage », séance de relevés. Sonde : oui, plus un relevé comparé au Calendrier. Décision : aucune.
- **Source** : MainWindow.Reminders.cs:51-58 : l'attente de 18 h est démarrée à la main (commentaire : « le jeu ne donne aucune heure que Tensho peut lire »). Le Calendrier du jeu l'affiche pourtant (docs/evidence/0.300-calendar-resets.md). Les noms GetNextMapAllowanceTimestamp et NextMapAllowanceDateTime sont présents dans FFXIVClientStructs.dll (chaîne relevée le 2026-11-02 ; propriétaire et signature à confirmer). GAPS.md M-02 (« allocation de cartes au trésor », Choix « Ajouter »).

## T7 — Missions aléatoires du jour faites ou non, dans les Rappels (C-02, partie anticipée)

- **État (relu le 2026-11-03)** : FAIT 0.356 (C-02).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs`.
- **Ce que le joueur obtient** : Les Rappels listent vos missions aléatoires (expert, niveaux, alliance…) avec une coche lue dans le jeu. Vous voyez d'un coup d'œil celles qui restent avant la remise quotidienne, avec une alarme.
- **Comment, taille, dépendances** : Sonde du propriétaire et de l'index attendu, preuve dans docs/evidence/. Lecture une fois par seconde avec les autres compteurs, gardée R-14 ; une ligne repliable « Missions aléatoires (n restantes) ». Relever en jeu l'heure de remise (fenêtre de la Recherche de mission) avant de l'afficher. Taille S. Dépendances : séance de relevés ; l'alerte de bonus de rôle reste à C-02 en g). Décision : aucune (choix fait).
- **Source** : GAPS.md C-02 (petit, valeur haute, Choix « Ajouter » ; dépend de GameTimers et ResetSchedule). Le nom IsRouletteComplete est présent dans FFXIVClientStructs.dll (relevé le 2026-11-02, propriétaire à confirmer) ; il n'est utilisé nulle part dans src/. Noms des missions : feuille ContentRoulette (à sonder). L'heure de remise des roulettes n'est pas dans docs/evidence/0.300-calendar-resets.md.

## T8 — Contenus terminés ou non, avec un filtre « non terminés » (C-01, partie anticipée)

- **État (relu le 2026-11-03)** : FAIT 0.355 (C-01).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.Duties.cs`.
- **Ce que le joueur obtient** : La liste des contenus marque ✓ ceux que vous avez déjà terminés et peut n'afficher que les autres. Le panneau de la Recherche de mission le dit aussi.
- **Comment, taille, dépendances** : Sonde en jeu sur un contenu fait et un non fait (preuve dans docs/evidence/). Lecture sur le thread du framework à l'ouverture de la page, puis à chaque changement de zone ; copie en ensemble d'identifiants, gardée R-14 (« indisponible après une mise à jour du jeu »). Taille S. Dépendances : séance de relevés. Décision : aucune (choix fait).
- **Source** : GAPS.md C-01 (petit, valeur haute, Choix « Ajouter » ; sonde de l'identifiant attendu par IsInstanceContentCompleted). Le nom IsInstanceContentCompleted est présent dans FFXIVClientStructs.dll mais n'est pas utilisé dans src/. DutyDefinition garde déjà InstanceId (Duties/DutyCatalog.cs:5).

## T9 — Équipement : améliorations que vous possédez déjà (M-12, partie anticipée)

- **État (relu le 2026-11-03)** : FAIT 0.383 (M-12).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.Gear.cs`.
- **Ce que le joueur obtient** : Pour le job porté, Tensho signale par emplacement toute pièce de meilleur niveau d'objet que celle portée, utilisable à votre niveau, déjà présente dans vos sacs, l'arsenal, une sacoche ou chez un servant. Il dit où la prendre.
- **Comment, taille, dépendances** : Calcul hors Draw, refait quand l'équipement porté, les stocks ou le job changent : GearRules.Fits, niveau ≤ niveau du job, niveau d'objet > pièce portée. Liste « Améliorations possédées » avec le rangement et un bouton « Essayer ». Taille S-M. Dépendances : rangements relevés par la Banque. Sonde : aucune. Décision : compter le coffre à apparence et l'armoire (recommandé : non, ces pièces ne se portent pas directement).
- **Source** : GAPS.md M-12 (« améliorations possibles pour tous les jobs », Choix « Ajouter »). La page montre déjà la possession (DrawOwnedMark, MainWindow.Gear.cs:515) et les pièces à obtenir (MainWindow.Gear.cs:535), mais pas les pièces possédées meilleures que celles portées. ItemHoldings donne le détail par rangement (Inventory/ItemHoldings.cs:4-14).

## T10 — Accueil : bloc « Aujourd'hui » et tuiles au choix

- **État (relu le 2026-11-05)** : FAIT 0.599.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.Home.cs`.
- **Ce que le joueur obtient** : À l'ouverture de Tensho, vous voyez ce qui compte maintenant : rappels non faits avant la prochaine remise, vaisseaux revenus ou prochain retour, tâches de servants terminées, quêtes suivies, les trois prochaines alarmes. Chaque ligne ouvre sa page. Vous choisissez aussi les tuiles de l'accueil (Rappels, Compagnie libre, Équipement…).
- **Comment, taille, dépendances** : Déplacer la lecture de GameTimers dans un petit service sur Framework.Update (une fois par seconde, gardé R-14, copie immuable), lu par les Rappels et l'Accueil. Construire le bloc « Aujourd'hui » une fois par seconde (rien de lourd dans Draw). Liste des tuiles dans la configuration : ordre et case par tuile, avec infobulle et ↺. Taille M. Dépendances : « Rappels par personnage », alarmes de retour, « Tâches de servants ». Sonde : aucune. Décision : lignes affichées par défaut (recommandé : toutes, chacune masquable).
- **Source** : MainWindow.Home.cs:39-56 : seulement les calculs en cours, la prochaine alarme et les erreurs. MainWindow.Home.cs:70-82 : dix tuiles fixes, sans Rappels, Compagnie libre, Équipement ni Revue de mode. Les compteurs du jeu ne sont lus que pendant le dessin de la page Rappels (MainWindow.Reminders.cs:26-36).

## T11 — Quêtes « disponibles maintenant » et quêtes de la zone actuelle

- **État (relu le 2026-11-05)** : FAIT 0.582.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.Quests.cs`.
- **Ce que le joueur obtient** : Un filtre montre les quêtes que vous pouvez prendre : non faites, prérequis terminés, pas exclues par un autre choix (cité de départ, Grande Compagnie), niveau atteint. Un second filtre se limite à la zone où vous êtes, avec le donneur sur la carte. Les conditions que Tensho ne sait pas lire sont signalées (« condition non interprétée ») et ne comptent jamais comme remplies.
- **Comment, taille, dépendances** : Calcul sur la liste publiée, pas dans Draw : non confirmée + CompletionEvidence + absente de questOtherCity. Niveau comparé à celui du job requis si « Niveaux et expérience de tous les jobs » est fait, sinon seulement affiché. Jointure inconnue ou étape requise : « à vérifier ». Zone : territoire courant, déjà lu par la carte. Taille M. Dépendances : niveaux des jobs pour le filtre de niveau ; colonne ClassJobCategory de Quest à sonder. Décision : inclure les quêtes répétables (recommandé : non par défaut).
- **Source** : MainWindow.Quests.cs:112-118 : seulement les filtres Toutes, Actives, Terminées et Non confirmées. QuestLinks.CompletionEvidence (Quests/QuestService.cs:53-58) sait déjà si les prérequis sont faits ; QuestAlternatives.NotPlayable (0.286) écarte les options non prises ; PreviousQuestStep reste brut (QuestService.cs:17). Donneur et carte : MainWindow.Quests.cs:215-227.

## T12 — Chemin vers une quête : prérequis restants, nombre et prochaine à faire

- **État (relu le 2026-11-05)** : FAIT 0.583.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Quests/QuestService.cs`.
- **Ce que le joueur obtient** : Sur la fiche d'une quête (ou d'un contenu, avec « Débloqué par »), Tensho liste dans l'ordre toutes les quêtes encore à faire pour l'atteindre, leur nombre, et la première à prendre maintenant, avec un bouton carte. Utile pour une épopée, un job ou un donjon à débloquer.
- **Comment, taille, dépendances** : Parcours borné sur un worker annulable : garde de cycle, plafond de nœuds, résultat publié et mis en cache par quête et par observation. Jointure 1 = toutes les branches ; jointure 2 = la branche déjà faite, sinon la plus courte ; mode inconnu = arrêt avec « condition non interprétée ». Liste repliable dans la fiche, un clic ouvre la quête. Taille M. Dépendances : aucune ; sert à « Débloqué par ». Sonde : aucune. Décision : aucune.
- **Source** : Quests/QuestService.cs:32 : « No recursive traversal ». La fiche ne montre que les prérequis directs (MainWindow.Quests.cs:230-245) et les quêtes suivantes directes. Les modes de PreviousQuestJoin 1 (toutes) et 2 (au moins une) sont déjà interprétés (QuestService.cs:53-58).

## T13 — Contenus et quêtes : « Débloqué par » et « Débloque »

- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Duties/DutyCatalogLoader.Dalamud.cs`.
- **Ce que le joueur obtient** : Sur la page Butin des contenus et dans le panneau de la Recherche de mission, chaque donjon, défi ou raid dit quelle quête le débloque et si vous l'avez faite, avec un lien vers la quête et son chemin. La fiche d'une quête dit ce qu'elle débloque (contenu, job).
- **Comment, taille, dépendances** : Sonde tools/ApiProbe des colonnes (type, valeurs nulles), preuve dans docs/evidence/. Ajouter UnlockQuestId à DutyDefinition, copié sur le framework comme le reste ; index inverse quête → contenus construit avec la liste des quêtes. Ligne « Débloqué par … (faite / non faite / inconnu) » dans MainWindow.Duties.cs et MainWindow.DutyFinder.cs. Taille M. Dépendances : « Chemin vers une quête ». Sonde : oui. Décision : aucune.
- **Source** : Duties/DutyCatalogLoader.Dalamud.cs:47-52 ne copie que le nom, le type, le niveau et l'icône. Les colonnes UnlockQuest (ContentFinderCondition), InstanceContentUnlock et ClassJobUnlock (Quest) ont leur nom dans Lumina.Excel.dll (relevé le 2026-11-02) mais ne sont lues nulle part dans src/. Aucune preuve dans docs/evidence.

## T14 — Équipement : préparer le plan d'un autre job que celui porté

- **État (relu le 2026-11-05)** : FAIT 0.580.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.Gear.cs`.
- **Ce que le joueur obtient** : Un menu de job sur la page Équipement : vous préparez ou consultez le plan de votre soigneur en étant forgeron. Pièces, matérias, pièces à obtenir et essayage fonctionnent ; les attributs réels sont marqués « relevés sur le job porté seulement ».
- **Comment, taille, dépendances** : Job affiché = job choisi (par défaut, le job porté) ; catalogue filtré pour ce job. Si ce job n'est pas porté, la colonne des attributs ne montre que la somme des pièces et des matérias, sans valeur réelle inventée. Taille M. Dépendances : niveaux des jobs pour le niveau de ce job (sinon un champ niveau). Sonde : aucune. Décision : aucune.
- **Source** : MainWindow.Gear.cs:39 et 45 : le plan et les matérias sont pris pour state.JobId, le job porté. Un code importé pour un autre job est enregistré mais reste invisible tant que ce job n'est pas porté (MainWindow.Gear.cs:147, « ImportedOtherJob »).

## T15 — Revue de mode : tenue de la semaine choisie, essayée en un clic, avec l'endroit de chaque pièce

- **État (relu le 2026-11-05)** : FAIT 0.581.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.FashionWeek.cs`.
- **Ce que le joueur obtient** : Vous choisissez une pièce par emplacement parmi celles proposées pour la semaine. « Essayer la tenue » les met toutes dans la cabine d'essayage, et une liste dit où se trouve chaque pièce (sacs, arsenal, coffre à apparence, armoire, servant), lesquelles manquent et leurs sources.
- **Comment, taille, dépendances** : Choix par emplacement gardé jusqu'à la remise hebdomadaire (mardi 08:00 UTC, docs/evidence/0.300-calendar-resets.md). Bouton « Essayer la tenue », sur clic seulement. Tableau emplacement → pièce → rangement, recalculé quand le choix ou les stocks changent. Taille M. Dépendances : aucune. Sonde : aucune. Décision : aucune.
- **Source** : MainWindow.FashionWeek.cs:93 et 129-132 : les pièces sont classées « possédées d'abord », sans choix ni essayage groupé. L'essayage existe déjà (Plugin.cs:266, TryOnItem ; « Essayer le plan » sur la page Équipement, MainWindow.Gear.cs:97-100). ItemHoldings donne le rangement (Inventory/ItemHoldings.cs:4).

## T16 — Tâches de servants : une ligne par servant, avec alarme

- **État (relu le 2026-11-05)** : FAIT 0.570.
- **État (relu le 2026-11-03)** : en partie 0.371 (alarme automatique à la fin de chaque tâche de servant) et 0.374 (vue de vos personnages).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Reminders.cs`.
- **Ce que le joueur obtient** : La ligne « Tâches de servants » se déplie : pour chaque servant, sa tâche finie ou son heure de fin, une cloche, et une alarme « quand toutes sont finies ».
- **Comment, taille, dépendances** : Ligne repliable (TreeNode) dans la même table ; alarmes Once avec Source=Reminder (catégorie Discord « Rappels ») ; copie des heures de fin gardée par personnage pour l'Accueil. Taille S. Dépendances : « Rappels par personnage ». Les alarmes d'office de M-01 pour les servants restent en g), ou suivent la même option que les vaisseaux. Sonde : aucune. Décision : aucune.
- **Source** : Reminders/GameTimers.Dalamud.cs:30-40 relève déjà le nom et l'heure de fin de chaque servant, mais MainWindow.Reminders.cs:67-71 n'affiche que deux nombres et la prochaine fin. Ces données ne sont connues qu'après l'ouverture de la liste des servants (RetainerManager.IsReady).

## T17 — Rappels cochés d'eux-mêmes quand le jeu le dit

- **État (relu le 2026-11-05)** : FAIT 0.558.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Reminders.cs`.
- **Ce que le joueur obtient** : Quand le jeu indique 0 commande spéciale ou 0 quête de tribu restante, la case « fait » se coche seule jusqu'à la remise. La coche manuelle reste pour le reste.
- **Comment, taille, dépendances** : Fait = coche manuelle, ou compteur lu à 0 depuis la dernière remise (marqué « d'après le jeu ») ; un compteur inconnu ne permet aucune déduction. Réglage « Cocher d'après le jeu » (activé, infobulle, ↺). Taille S. Dépendances : aucune. Sonde : aucune. Décision : activé par défaut (recommandé : oui).
- **Source** : MainWindow.Reminders.cs:46 et 49 : DeliveriesLeft et AlliedQuestsLeft sont lus et affichés, mais la case « fait » (MainWindow.Reminders.cs:94-101) n'est jamais cochée d'après eux.

## T18 — Discord : heure lue dans le fuseau de chaque membre, et balises [zone], [objet], [et]

- **État (relu le 2026-11-05)** : FAIT 0.564.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Alarms/DiscordWebhook.cs`.
- **Ce que le joueur obtient** : Dans un salon partagé de compagnie libre, chaque membre lit l'heure de l'alarme dans son propre fuseau (et « dans 5 minutes »). Vos messages peuvent aussi donner la zone, l'objet et l'heure d'Éorzéa.
- **Comment, taille, dépendances** : Nouvelles balises [discordtime] (<t:unix:t>), [relative] (<t:unix:R>), [zone], [objet] et [et]. Les noms sont résolus sur le thread du framework quand l'alarme sonne, puis passés au notificateur. [time] ne change pas. Tests de Format. Taille S. Dépendances : aucune. Sonde : aucune (syntaxe de Discord). Décision : changer le modèle par défaut (recommandé : non, garder l'actuel).
- **Source** : Alarms/DiscordWebhook.cs:116 : [time] est votre heure locale au format HH:mm. Seules balises : [label], [minutes], [time], [character]. TenshoAlarm garde déjà TerritoryId, ItemId et EtMinute (Alarms/TenshoAlarm.cs:23-28). USER-REQUESTS R-12 (retiré en 0.1088, historique git) : « voir plus si possible ou intéressant ».

## T19 — Alarmes : modifier le nom et l'heure, et « Me rappeler dans 5 min »

- **État (relu le 2026-11-05)** : FAIT 0.557.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Alarms.cs`.
- **Ce que le joueur obtient** : Vous renommez une alarme ou changez son heure ET ou sa date sans la recréer. Une alarme qui vient de sonner peut être reportée de quelques minutes, depuis la page Alarmes ou l'infobulle de la barre du serveur.
- **Comment, taille, dépendances** : Champs nom et heure, enregistrés en quittant le champ, comme pour Discord. Pour une alarme copiée dans le jeu, un message dit de corriger celle du jeu à la main (Alarms.DeleteInGame). « Reporter » crée une alarme Once au même nom ; durée réglable (5 min, infobulle, ↺). Taille S. Dépendances : aucune. Sonde : aucune. Décision : durée par défaut du report (recommandé : 5 min).
- **Source** : MainWindow.Alarms.cs:307-331 : chaque alarme n'a que sa case, son rappel et « Supprimer ». Aucun report possible : AlarmService.Ring marque l'occurrence comme traitée (Alarms/AlarmService.Dalamud.cs:158).

## T20 — Alarmes : sonner aussi à l'heure exacte

- **État (relu le 2026-11-05)** : FAIT 0.556.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Alarms/AlarmService.Dalamud.cs`.
- **Ce que le joueur obtient** : En plus du rappel N minutes avant, l'alarme peut sonner une seconde fois quand la fenêtre s'ouvre : point de récolte, poisson, retour de vaisseau.
- **Comment, taille, dépendances** : Option par alarme, plus un réglage par défaut « Sonner aussi à l'heure » (désactivé, infobulle, ↺). Un second état « à l'heure traité » évite de sonner deux fois ; Discord seulement au premier son (débit). Tests de Check. Taille S. Dépendances : aucune. Sonde : aucune. Décision : envoyer aussi le second son sur Discord (recommandé : non).
- **Source** : Alarms/AlarmService.Dalamud.cs:147-176 et Alarms/TenshoAlarm.cs:93-101 : une occurrence ne sonne qu'une fois, au début du rappel (LastOccurrence).

## T21 — Personnage : niveaux et expérience de tous les jobs

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Character.cs`.
- **Ce que le joueur obtient** (page Personnage retirée en 0.341 : à placer dans la page Équipement) : La page liste vos jobs avec leur niveau, une barre d'expérience, et ceux pas encore débloqués. D'autres outils s'en servent : quêtes disponibles, plan d'un autre job, simulation d'artisanat.
- **Comment, taille, dépendances** : Sonde (champs, index, valeur d'un job non débloqué), preuve dans docs/evidence/. Copie une fois par seconde sur le thread du framework dans CharacterStatsReader, gardée R-14 ; tableau par rôle. Taille S. Dépendances : séance de relevés. Décision : aucune.
- **Source** : MainWindow.Character.cs:16-40 : seul le job porté est affiché. ClassJobLevels et ClassJobExperience ont leur nom dans FFXIVClientStructs.dll (relevé le 2026-11-02) mais ne sont pas utilisés dans src/. La correspondance job → index est à prouver (probablement la colonne ClassJob.ExpArrayIndex).

## T22 — Équipement : état et symbiose des pièces portées (M-06, partie affichage)

- **État (relu le 2026-11-03)** : FAIT 0.376 (M-06).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Gear/GearLoader.Dalamud.cs`.
- **Ce que le joueur obtient** : Sur la page Équipement, chaque pièce portée affiche son état (%) et sa symbiose. Une pièce à réparer, ou prête pour l'extraction de matéria, ressort.
- **Comment, taille, dépendances** : Sonder les échelles (pièce à 100 % puis usée, symbiose à 100 %), preuve dans docs/evidence/. Deux champs de plus dans EquippedPiece, copiés au même relevé et gardés R-14. Affichage seulement : l'alerte et la barre restent dans M-06. Taille S. Dépendances : séance de relevés. Décision : aucune.
- **Source** : GAPS.md M-06 (Choix : alerte Tensho et barre à l'écran, seuils réglables). Gear/GearLoader.Dalamud.cs:127 lit déjà le conteneur EquippedItems, mais EquippedPiece (Gear/GearCatalog.cs:34) ne garde ni l'état ni la symbiose. Les noms Condition et SpiritbondOrCollectability sont présents dans FFXIVClientStructs.dll ; leur échelle reste à prouver.

## T23 — Compagnie libre : vaisseaux groupés par compagnie, ancien relevé effaçable, valeur du dernier butin

- **État (relu le 2026-11-05)** : FAIT 0.603.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.FreeCompany.cs`.
- **Ce que le joueur obtient** : Avec des personnages dans plusieurs compagnies, chaque atelier a son propre cadre (nom de la compagnie, date du relevé). Le relevé d'une compagnie quittée peut être effacé. Chaque vaisseau revenu affiche la valeur de son butin au prix PNJ.
- **Comment, taille, dépendances** : Lire le nom de la compagnie au relevé (source à sonder ; à défaut, « Atelier n »). Un cadre (Header) par maison ; bouton « Effacer ce relevé » à deux clics. Valeur = Σ quantité × prix PNJ (le prix du marché viendra avec f) 2). Taille S. Dépendances : aucune. Sonde : nom de la compagnie. Décision : aucune.
- **Source** : MainWindow.FreeCompany.cs:39-50 : regroupement par type de vaisseau seulement. FreeCompany/VoyageWatcher.Dalamud.cs:122 fusionne les relevés par HouseId, donc plusieurs ateliers se mélangent, avec les mêmes numéros d'emplacement. Les prix PNJ sont déjà dans le catalogue des trajets (VoyageWatcher.Dalamud.cs:17-20).

## T24 — Discord : un salon par catégorie (plusieurs webhooks)

- **État (relu le 2026-11-05)** : FAIT 0.598.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Alarms/DiscordWebhook.cs`.
- **Ce que le joueur obtient** : Les retours de vaisseaux partent dans le salon de la compagnie libre, vos alarmes de récolte dans votre salon privé.
- **Comment, taille, dépendances** : Adresse facultative par catégorie (vide = adresse principale). Chacune est masquée, vérifiée par IsValidUrl, jamais écrite dans les journaux ni dans le rapport de Diagnostics. File et pause de débit propres à chaque adresse ; « Envoyer un test » par adresse. Taille M. Dépendances : aucune. Sonde : aucune. Décision : nombre d'adresses (recommandé : une par catégorie existante).
- **Source** : Alarms/DiscordWebhook.cs:14-30 : une seule adresse pour toutes les catégories. R-12 (ROADMAP ; son libellé était dans USER-REQUESTS, retiré en 0.1088) met « plusieurs webhooks » à étudier.

## T25 — Équipement : plusieurs plans nommés par job

- **État (relu le 2026-11-05)** : FAIT 0.593.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Configuration.cs`.
- **Ce que le joueur obtient** : Pour un même job, vous gardez « montée de niveau », « artisanat » et « meilleur possible », et passez de l'un à l'autre ; chacun se partage par code.
- **Comment, taille, dépendances** : Liste nommée par job (nom, pièces, matérias) et choix du plan actif. Migration de l'ancien plan en « Plan 1 », avec test. Dupliquer, renommer, supprimer ; l'infobulle dit que la suppression est immédiate (règle 0.325). Taille S-M. Dépendances : aucune. Sonde : aucune. Décision : nombre maximum de plans (recommandé : 10 par job).
- **Source** : Configuration.cs:218 et 228 : GearPlans et GearMateria ne gardent qu'un plan par job, et « Effacer le plan » efface tout (MainWindow.Gear.cs:107).

## T26 — Dirigeables : trajet du dernier voyage reconstitué

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs`.
- **Ce que le joueur obtient** : La colonne Trajet des dirigeables affiche les destinations au lieu de « trajet inconnu ».
- **Comment, taille, dépendances** : Sonder HousingWorkshopAirshipGathered pour un champ de point (comme Point côté sous-marins). S'il existe : trajet = points distincts des trouvailles, noms tirés d'AirshipExplorationPoint, avec la mention « d'après le butin ». Sinon, rester « inconnu ». Taille S après la sonde. Dépendances : séance de relevés ; utile au planificateur de dirigeables de f) 3. Décision : aucune.
- **Source** : docs/evidence/0.307-fc-voyages.md : « Pas de liste de points de trajet pour les dirigeables » ; MainWindow.FreeCompany.cs:74 affiche RouteUnknown. Les trouvailles des dirigeables sont déjà lues (HousingWorkshopAirshipGathered, VoyageWatcher.Dalamud.cs:94-103), mais rien ne prouve qu'elles portent un point de trajet.

## T27 — Actions de compagnie actives : heure de fin et alarme

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs`.
- **Ce que le joueur obtient** : Les Rappels affichent les actions de compagnie en cours (bonus d'expérience, de récolte…) et leur heure de fin, avec une cloche pour en relancer une à temps.
- **Comment, taille, dépendances** : Sonder le propriétaire et le moment où le jeu remplit la donnée (peut-être seulement à l'ouverture de la fenêtre de compagnie). Ligne de Rappels gardée R-14, avec la date du relevé. Taille M. Dépendances : séance de relevés. Décision : garder la proposition si la donnée n'existe qu'à l'ouverture (recommandé : oui, avec la date du relevé).
- **Source** : Rien ne les lit dans src/ (recherche faite). Les noms FreeCompanyActionTimer et CompanyActionTimer sont présents dans FFXIVClientStructs.dll (relevé le 2026-11-02, propriétaire inconnu). Noms des actions : feuille CompanyAction (à sonder). Règle de GAPS.md : une donnée que le jeu n'envoie qu'à l'ouverture d'une fenêtre se lit seulement après une ouverture faite par le joueur.

## T28 — Collections : « Obtenus récemment » et menu des sources pour les emotes

- **État (relu le 2026-11-05)** : FAIT 0.602.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.Collections.cs`.
- **Ce que le joueur obtient** : Une ligne « Obtenus récemment » liste, avec la date, ce que vous avez gagné depuis le relevé précédent (montures, mascottes, rouleaux…). Le clic droit sur une emote ouvre ses sources, comme pour les montures.
- **Comment, taille, dépendances** : À chaque observation publiée, comparer avec les identifiants obtenus déjà enregistrés (par personnage, 200 entrées au plus, gardées 90 jours) ; liste en tête de la page. Ajouter Emote au menu des sources. Taille S. Dépendances : « Rappels par personnage » (clé par personnage). Sonde : aucune. Décision : durée de conservation (recommandé : 90 jours).
- **Source** : MainWindow.Collections.cs:92 : des totaux seulement, aucun historique. MainWindow.Collections.cs:153-154 : CollectionSourcesMenu sert aux montures, mascottes, cartes et rouleaux, pas aux emotes, dont l'infobulle a pourtant les sources (ligne 149).

## T29 — Mini Cactpot : table des gains prouvée et visible

- **État (relu le 2026-11-05)** : FAIT 0.571.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MiniGames/MiniCactpotSolver.cs`.
- **Ce que le joueur obtient** : Sous le conseil, le gain de chaque somme de ligne (6 à 24) s'affiche, comme sur le ticket du jeu : vous voyez ce que vise la ligne conseillée.
- **Comment, taille, dépendances** : Chercher la table dans les données du jeu (sonde des feuilles du Gold Saucer) ou la relever sur le ticket en jeu ; preuve dans docs/evidence/ et test qui compare. Tableau repliable sous le conseil. Taille S. Dépendances : séance de relevés. Décision : aucune.
- **Source** : MiniGames/MiniCactpotSolver.cs:9 : table de 25 gains écrite dans le code, créditée au jeu (Credits/CreditCatalog.cs:38-39) mais sans preuve dans docs/evidence. Règle : aucune donnée du jeu sans source citée.
