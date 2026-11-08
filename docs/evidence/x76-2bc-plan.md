# X-76 steps 2b and 2c — harmonisation and parser coverage plan (2026-11-08)

Produced by a read-only workflow (8 agents: 4 harmonisation families, 4 parser scopes, then a synthesis agent that spot-checked the five main claims in the code). The state of each item is kept in docs/project/GLOBAL-PLAN.md (X-76). Raw findings: 48 harmonisation findings, 59 parser items (kept locally in the session).

Ranked X-76 plan: 10 harmonisation versions (2b), 10 parser items (2c) and 4 extra steps. Read only: nothing was edited, built or committed. Comments and crystal/storage code were left out because another agent is editing them.

## Spot-checks (code read myself)

1. **Tooltip helper and the bug on greyed controls: confirmed.**
   - `Tip()` is at `MainWindow.Advanced.cs:34-37` and uses `AllowWhenDisabled`.
   - Counted by regex: 110 sites write `IsItemHovered())WrappedTooltip(localization.Get("const"))` by hand without the flag. 21 more sites copy Tip's body by hand, with the flag.
   - Three hand-written tips vanish on a greyed control: `MainWindow.Gathering.cs:127` (after `EndDisabled` at 126), `MainWindow.MyTools.cs:145` (disabled at 142) and `MainWindow.CraftSimulation.cs:295`. Line 297 next to it already uses the flag.
2. **Alarm options cannot be rolled back after a failed save: confirmed.**
   - `MainWindow.Alarms.cs:474`, `:491` and `:495-497` change `options.LeadMinutes`, `SnoozeMinutes` and `RingOnTime` directly on the saved object.
   - `previous=configuration.AlarmOptions` at `:532` is that same object, so the restore at `:537` brings nothing back. A failure is only logged.
   - `IntSetting` (`UI/Components/SettingRow.cs:43-46`) also writes before saving and never restores the old value.
3. **Gear page text is almost invisible on light themes: confirmed.**
   - `MainWindow.Gear.cs:251` uses a fixed white `0.93,0.93,0.93`, `:323` a fixed red and `:620` a fixed orange `MeldWarning`.
   - The Light panel is `0.98,0.97,0.94` and the ClearWhite panel `0.93,0.94,0.96` (`UI/ThemePalettes.cs:15,21`).
   - The theme already has the right colours: `FfxivTheme.Text`, `.Error` and `.Warning` at `UI/FfxivTheme.cs:17,22,23`.
4. **Quest hand-ins lose the HQ demand: confirmed.**
   - `ItemDiscoveryWatcher.Dalamud.cs:321` reads `WantHQ`.
   - At `:335` the `hq` value is never used.
   - This contradicts `DiscoveriesModel.cs:37-38` ("HQ demand is kept as LevelMax = 1").
5. **Discovery labels and gain attribution: confirmed.**
   - `MainWindow.Discoveries.cs:254-279` has no branch for FishLure, FishTransition or FishBite. They fall to `_=>DiscoveryMonsterName(fact.Subject)`, so a fish id is printed as a monster name, and these kinds are shown in the item lines at `:110-111`.
   - `BusyWindows` (`ItemDiscoveryWatcher.Dalamud.cs:27-30`) is missing ShopExchangeCoin, FreeShop, CollectablesShop and SkyIslandExchange, which are in `ShopWindows` (`:36-37`).
   - In deep dungeons, `:242` records a chest item without the territory while `:446` records it with the territory. The key includes the territory (`DiscoveriesModel.cs:69`), so the same item is counted in two facts.
   - Also true: `UndercutWatcher.OnOfferings` (`Market/UndercutWatcher.Dalamud.cs:53`) has no `CompetitiveGate.Cut` check, while `SeenPriceReader.Dalamud.cs:23` has one.
   - The code has no calls to any of these: `IsItemUnlocked`, `RetainerTaskNormal`, `AdventureExPhase`, `NotoriousMonsterTerritory`, `SurveyLine`, `PointRating`, `DailyQuests`, `UnacceptedQuestMarkers`, `CurrentSupplySubRowIds`.

## Harmonisation versions (ranked, one family per version)

**H1. One tooltip helper everywhere.**
- **Change:**
  - Move `WrappedTooltip` and `Tip` out of `MainWindow.Advanced.cs:21/34` into a new `UI/Components/Tooltip.cs`.
  - Add `TipText(string)`, `TooltipScope(widthEm)` and `HelpMarker(key)`.
  - Convert the 110 hand-written sites, the 21 copies of Tip's body and the 5 sites with a conditional key. Keep the composite text at `CollectablePanel.cs:90` and `MarketBoard.cs:109`.
  - Fix the 3 tips lost on greyed controls, and `AreaSetting` at `MapView.cs:970`.
  - Give the 12 hand-written `BeginTooltip` blocks one wrap width.
  - One shared server-bar tooltip for `AlarmDtrEntry.Dalamud.cs:96-113` and `ReminderDtrEntry.Dalamud.cs:94-107`.
  - Turn `CraftLog.cs:231` "(?)" and `DutyFinder.cs:80` InfoCircle into `HelpMarker`.
- **Files:** about 50 `MainWindow.*.cs`, `Alarms/*Dtr*.cs`, the new `UI/Components/Tooltip.cs`.
- **Visible:** yes. Tips now show on greyed controls; wrap widths become uniform.
- **Risk:** low. **In-game capture:** no.
- **Tests:**
  - `TooltipRatchet325Tests.cs:19` stays green as long as the new helper names contain "Tip" or "WrappedTooltip".
  - Extend `Program.cs:918-930` so it also collects the `IntSetting` keys and the converted keys. Any key missing in fr will then show up.
  - Keep `ReminderTabs687Tests.cs:29`, `Interfaces535Tests.cs:332`, `Gathering478Tests.cs:358` and `Campaign455Tests.cs:29` (the `DrawTooltip()` names stay).

**H2. Theme colours and glyphs.**
- **Change:**
  - Add `Text`, `Error` and `Warning` aliases next to `MainWindow.cs:32-34`.
  - Replace the fixed colours at `Gear.cs:251,413,427,612,323,415,464,574`, remove `Gear.cs:620`, and fix `:445-446`, `VesselBuilds.cs:141-257`, `SpearfishingPanel.cs:71`, `FashionWeek.cs:254` and `LootFilters.cs:58-59`.
  - Add one `DrawStateMark(bool?)`. `Beastmaster.cs:108` uses "✗", which is missing from the font (see the comment at `ChocoboBarding.cs:81`). Also convert `Guide.cs:79`, `TriadNpcs.cs:34,171` and `FashionWeek.cs:136`.
  - One colour for selected toggle buttons, `FfxivTheme.Selected`: `CollectablePanel.cs:84` and `Items.cs:454`.
- **Visible:** yes. This fixes the Light and ClearWhite themes.
- **Risk:** low. **In-game capture:** no.
- **Tests:**
  - `Chocobo341Tests.cs:19-20` must stay green.
  - Add a ratchet test that forbids `new Vector4(0.93f` and `0.9f,0.45f` in `MainWindow*.cs`.
  - The ✓/✗ text in en.json (lines 482 and 1199) needs the same review in all 4 language files.

**H3. Setting save errors and rollback.**
- **Change:**
  - Add a `TrySave(what, rollback)` next to `SaveSetting` (`SettingRow.cs:80-85`).
  - Fix the alarm options rollback: build a copy before editing.
  - Make `IntSetting` restore the old value when the save fails.
  - Convert the 14 silent sites that do not restore the old value (for example `PreparationCost.cs:42,45`, `Plots.cs:47`, `HuntTrain.cs:84`, `Weather.cs:35`, `CraftFromStock.cs:118`, `CurrencyValue.cs:106`) and the inline copies (`Advanced.cs:82,88`, `Caching.cs:31,43`, `Theme.cs:42`, `MapView.cs:49,572,787`).
  - Draw `saveError` once in the main window frame in `FfxivTheme.Error`, instead of `Settings.cs:38` and `MapView.cs:575`.
- **Visible:** yes. A failed save is now shown on every page.
- **Risk:** medium. **In-game capture:** no.
- **Tests:**
  - `AlarmChatInDutiesTests.cs:51` and `SourceSwitch324Tests.cs:72-84` must stay green (keep the `DefaultButton` ids).
  - `CurrencySales794Tests.cs:27`.
  - Add a unit test for the alarm rollback.

**H4. Number settings save on release.**
- **Change:**
  - Add a `SliderSetting` helper (int and float) built on `IntSetting`'s release logic, replacing the hand-written copies at `Advanced.cs:56-66` and `:121-135`.
  - Today a save is written to disk on every drag frame at `Gear.cs:206`, `GearWear.cs:21,25` and `MapView.cs:594-761` (saved at `:784-788`), and on every key press at `Gear.cs:212`. This goes against "nothing heavy in Draw".
  - Merge the two `CraftStockSaleDays` editors (`CraftFromStock.cs:114-119` and `CurrencyValue.cs:103-107`).
- **Visible:** no.
- **Risk:** medium. **In-game capture:** no.
- **Tests:** `CurrencySales794Tests.cs:27` pins `configuration.CraftStockSaleDays=Math.Clamp(saleDays,1,30);`.

**H5. Setting row components.**
- **Change:**
  - Raw checkboxes become `BoolSetting`: `PreparationCost.cs:41-46`, `GatheringPlanSettings.cs:92-98`, `Reminders.cs:158-160`, `Advanced.cs:77-89`.
  - Add `ComboSetting<T>` for `Plots.cs:44-48` and `HuntTrain.cs:80-87`.
  - Add the missing `defaultOf` at `Guide.cs:173-178` and `Settings.cs:61`.
  - Make the bool rows use `BoolDefaultButton` (or merge it into `DefaultButton(id, differs, defaultName?)`).
  - Fix the one setting with two tooltips: `Guide.cs:173` against `MapView.cs:776`.
  - Move the FashionWeek and MacroCopy batches (`FashionWeek.cs:90-110`, `Settings.cs:196-233`) to one row per option.
- **Visible:** yes. Reset buttons appear.
- **Risk:** low to medium. **In-game capture:** no.
- **Tests:** `SourceSwitch324Tests.cs:83,87-89`, `Hover327Tests.cs:32`, `Settings327Tests.cs:69`, `AlarmChatInDutiesTests.cs:51`.

**H6. Setting tooltip texts (text only).**
- **Change:**
  - Add the "\nDefault: …" line to the setting tips that lack it (65 BoolSetting, 5 IntSetting, 3 DiscordText, 4 SettingBox, 5 AreaSetting).
  - Move the 18 inline "Default:" texts onto their own line, and rewrite the 3 "by default" sentences.
  - Help text shown twice: `Settings.cs:54-58,290-291` and `GatheringGp.cs:105-106`.
  - The ShowKeys toggle appears twice: `Settings.cs:309` and `Advanced.cs:258`.
  - Add one ratchet test over the tip keys of every setting helper.
- **Files:** en, fr, de and ja files, plus a few call sites.
- **Visible:** yes. **Risk:** low. **In-game capture:** no.
- **Tests:** the new ratchet; `Hover327Tests.cs:33,44,73` and `Settings327Tests.cs:51,71` already require the line.

**H7. Page status lines.**
- **Change:**
  - Route the 8 inline error sites through `DrawDataError` (`ErrorPresentation.cs:16-23`): `BlueSpellSources.cs:120`, `Collections.cs:59`, `FishingSources.cs:27`, `Market.cs:44`, `Monsters.cs:62,99`, `DutyFinder.cs:69`, `MarketBoard.cs:136-140`.
  - Add one `EmptyState(key)` for the 18 white `TextWrapped` empty or loading lines, `Items.cs:785` and `Home.cs:44` (drawn gold).
  - Make Copy buttons small, and add one `CopyButton` with "copied" feedback (6 separate forms today).
- **Visible:** yes. **Risk:** low. **In-game capture:** no.
- **Tests:** keep `ReviewFixes326Tests.cs:58,63`. `BeastNearby804Tests.cs:26` must stay green.

**H8. Item display.**
- **Change:**
  - Add `DrawGameIcon(iconId,size)`, taken out of `ItemCell.cs:57-64`. It replaces the 41 workarounds of the form `DrawItemIcon(new ItemDefinition(0,…))` and the raw icon sites (`ChocoboBarding.cs:70`, `ItemFilters.cs:93`, `MapLocalLists.cs:353-358`, `MapView.cs:899,1722,1958`).
  - Use `ItemCell` for the plain name rows: `ChocoboBarding.cs:89`, `Events.cs:104`, `CurrencyValue.cs:187`, `CraftLive.cs:36`.
  - Add one `ItemNameOf(id)` and remove the duplicate keys `MonsterUi.Item`, `HoverUi.Item`, `CraftPresetMessages.Item` and `CraftLogUi.Item` from all 4 languages at once.
  - Move `ToolButton` into `UI/Components` with a size scaled to the frame height.
- **Visible:** yes, small changes. **Risk:** low. **In-game capture:** no.
- **Tests:**
  - `Chocobo341Tests.cs:20` (`DrawOwnedMark` stays right after the name).
  - `MapZoom542Tests.cs:43` (keep the `ToolButton` name and its argument order).
  - `CollectionItems387Tests.cs:18-23` and `Craft464Tests.cs:34`.

**H9. Logs.**
- **Change:**
  - Warn once, then re-arm after a success, at `DutyFinderReader.Dalamud.cs:61`, `ItemHoverReader.Dalamud.cs:52` and `MapLiveReader.Dalamud.cs:231`. Today each logs every 5 s.
  - Show the developer-only logs in developer mode only: `GardenChatWatcher.Dalamud.cs:109` (every chat line in housing), `RetainerSalesReader.Dalamud.cs:77`, `GcSupplyReader.Dalamud.cs:64`, `TrackerWindow.cs:75,92`.
  - One log level for "catalog unavailable" (5 sites use Error, the rest Warning).
  - Templates instead of `$""` text: `Squadron.cs:97-107`, `Plugin.cs:973,1458,1473`, `TitleChanger.Dalamud.cs:64`.
- **Visible:** no. **Risk:** low. **In-game capture:** no.
- **Tests:** keep the texts pinned by `GardenCare616Tests.cs:37`, `Resilience453Tests.cs:24`, `Resilience507Tests.cs:12-17` and `Resilience452Tests.cs:16`.

**H10. Files and subscriptions.**
- **Change:**
  - Add `AtomicFile.Write` (temporary file with a GUID name, deleted in `finally`). Today 5 writers share the same 3 lines and 3 use a fixed `.tmp` name.
  - Use `RegistrationScope` instead of hand-written nested try/catch in 9 readers and 4 watchers that never undo a subscription.
  - Add the `CompetitiveGate.Cut` check to `UndercutWatcher.OnOfferings`. This touches DEC-038, so give the user a short summary and get his yes first.
  - Move the writes made on the framework thread to a queue: `GilWatcher.Dalamud.cs:149`, `CharacterStatusWatcher.Dalamud.cs:133`, `VoyageHistory.cs:119`.
  - `GitHubExchange.cs:55` resets an unreadable file and later writes over it.
- **Visible:** no. **Risk:** medium. **In-game capture:** no.
- **Tests:** `GitHubExchange883Tests.cs:26`, `DiscoveriesTests.cs:60`, `Market346Tests.cs:18`, `Resilience453Tests.cs:18`, `CompetitiveCut610Tests.cs:42,50-52`.
- **Note:** check `GilWatcher` with the agent editing the storage code first.

**Dropped or deferred harmonisation items:**
- **Renaming tooltip keys** (`*Help` to `*Tip`, `SettingTips.*`): invisible, touches all 4 languages and 5 tests.
- **One shape for catalogue loaders:** high risk, many pinned tests.
- **`ImRaii.Disabled`:** purely cosmetic.
- **Raw section headers in the list panels** (`ListPanels.cs:217,288`): the smaller font there may be intended. Needs the user's decision, and `CraftSteps789Tests.cs:35` pins it.

## Parser items (2c, ranked)

**P1. Keep the HQ demand of quest hand-ins.**
- **Change:** keep `WantHQ` (`ItemDiscoveryWatcher.Dalamud.cs:335`). Pick one encoding and make the model comment (`DiscoveriesModel.cs:37-38`) and the reader (`MainWindow.ItemUses.cs:149-152`, which shows `LevelMax` as the quantity) agree.
- **Visible:** yes. **Risk:** low. **In-game capture:** no.
- **Tests:** a new unit test on the encoding.

**P2. Correct gain attribution.**
- **Change:**
  - Use one shared window list for busy and shop windows, so purchases are no longer recorded as FATE rewards.
  - Add the territory on the deep-dungeon chest path (`:242`) so its key matches `:446`.
  - The claim that the FATE check runs before the monster-drop check needs one read of `ItemGains.cs:59-69` before changing it.
- **Visible:** yes (cleaner facts). **Risk:** low. **In-game capture:** no.
- **Tests:** unit tests on `ItemGains.Attribute`.

**P3. A label for every discovery kind, and a test that enforces it.**
- **Change:**
  - Add branches for FishLure, FishTransition and FishBite in `DiscoveryFactName`.
  - List NpcPlace, BlueSpellCaster, QuestItemAsked and the other unlisted kinds on the Discoveries page.
  - Add a test over `Enum.GetValues<DiscoveryKind>()` so every new kind gets a label.
- **Visible:** yes. **Risk:** low. **In-game capture:** no.

**P4. Item unlock state from the game.**
- **Change:** call `IUnlockState.IsItemUnlockable` and `IsItemUnlocked` (never called today). They replace the table that knows only 2 item actions (`ItemUnlockCopy.Lumina.cs:9-12`) in item hover, currency value (`CurrencyValue.cs:93-96`) and loot filters.
- **Visible:** yes. **Risk:** low. **In-game capture:** no, but the result should be checked in game.

**P5. Game sheets instead of community data.**
- **Change:** offline sheet probes first, then:
  - `RetainerTask` and `RetainerTaskNormal` for the venture line.
  - `AdventureExPhase` for vista numbers.
  - `NotoriousMonsterTerritory` to check `SRankConditions` by a test.
  - The duty level of blue-mage spells from `AozActionTransient` (CFC branch, `CollectionReader.Dalamud.cs:267`).
  - `MJIAnimals` Size and Reward.
- This serves step 3 of X-76 (the single "game and observed data only" setting).
- **Visible:** yes. **Risk:** low. **In-game capture:** no.

**P6. Fishing facts.**
- **Change:**
  - Note Fisher's Intuition (status 568) at the cast, its length when it starts, and the fish caught just before it (predator candidates).
  - Use the swimbait item when cast flag 32 is set.
  - Add the bait to the bite-time key.
- This finishes X-73.18 ("reste Intuition et prédateurs").
- **Visible:** yes. **Risk:** medium (key change).
- **In-game capture:** Intuition no (`docs/evidence/0.887-fishing-bite.md`); swimbait yes.

**P7. Submarine sector facts.**
- **Change:** attach the vessel's surveillance band to each `VoyageDrop`, and keep the HQ part, DoubleDip, ExpGained and FirstExploration, plus one visit record per sector.
- The meaning of the Survey, Yield and Discovered lines is not proven.
- **Visible:** later (planner). **Risk:** medium. **In-game capture:** yes.

**P8. Counts for drop rates.**
- **Change:** count kills per monster, chests per duty and visits per sector.
- **Depends on:** the discovery file schema (extra step E1).
- **Risk:** medium. **In-game capture:** no.

**P9. Data the game already gives but Tensho drops.**
- **Change:**
  - Save in `CharacterStatus` the fields `GameTimers.Read` already returns: leves, deliveries, allied quests, squadron.
  - Read from the retainer struct: ClassJob, Level, MarketExpire.
- **Visible:** yes. **Risk:** low.
- **In-game capture:** the timer fields no; the retainer fields yes.

**P10. Duty rewards and chests.**
- **Change:** record end-of-duty rewards (`IDutyState.DutyCompleted` plus the bag gains), and resolve `ChestObjectId` to the chest's object.
- Whether the chest id is stable is not proven.
- **Visible:** yes. **Risk:** medium. **In-game capture:** yes.

**Deferred parser items:**
- This week's custom-delivery request (which client field means what is not proven).
- Island stocks, granary, farm and favours (N-07/N-10).
- Fashion Report preview, best score and tries left.
- Map markers for quests that can be accepted now.
- `NormalQuests` and `DailyQuests`.
- Structured Grand Company supply data.
- Airship entries.
- Bountiful and starting-quality checks.
- The open shop's id (not probed yet).
- Field content (CE, Occult Crescent): the user asked not to start Occult Crescent without his request.
- Storage and armoury items: left to the agent editing the storage code.

## Extra X-76 steps worth adding

- **E1. Decide the discovery file schema before 1.0.** Add optional fields (HQ flag, counts per value, counts per visit) and raise `CurrentVersion` (`DiscoveriesModel.cs:76-83`), updating `tools/discoveries-merge.py`. This is cheaper now, while there is one shared player file, than migrating every player's file after 1.0. P6, P7 and P8 depend on it.
- **E2. One "UI rules" test that cannot go backwards.** It forbids hand-written key tooltips, fixed text colours in `MainWindow*.cs`, and `try{save();` outside `SettingRow.cs`. Without it, H1 to H5 drift back with every new page.
- **E3. One test for every docs/evidence path cited by the code.** 159 paths are cited, and only 3 tests check theirs today. It keeps CLAUDE.md rule 7 enforced through the renames of step 7.
- **E4. One "once per event" helper for watchers.** `lootSeen` stops silently at 256 entries (`ItemDiscoveryWatcher.Dalamud.cs:445`, confirmed). The 4096 cap on `voyagesNoted` was not checked.
## Done

- 0.911: P1, P2, P3 and the three defects found by the check of the recorded data (docs/evidence/0.911-discoveries-check.md).
- 0.912: H2. Theme aliases Text, Error, Warning, Selected; fixed text colours replaced on the gear, vessel, map timed points, fishing bites, gathering
  panel and spearfishing pages; one state mark (DrawStateMark: check, cross, question in the icon font) for beastmaster, guide, Fashion Report, chocobo
  barding, gear and recipes; « ✓ »/« ✗ » removed from texts (the game font lacks « ✗ »); selected toggle buttons use the theme's Selected colour.
  Kept fixed: colours drawn on the map image or its own dark badges, icon tints, dye and Triple Triad side colours (data), self-contrasted badges.
- 0.913: H1. Tooltip helpers in UI/Components/Tooltip.cs (WrappedTooltip, Tip, TipText, HelpMarker, TooltipScope with one wrap width); 110
  hand-written key tooltips, 21 copies of Tip's body and 10 conditional ones go through Tip (shown on greyed controls too: three tooltips that vanished
  on a greyed control are fixed, and a few more by the same change); the 12 BeginTooltip blocks share one wrap width; the two server-bar tooltips share
  their placement; one help marker (icon). Kept: composite tooltips and about 95 computed-text tooltips (their text is built only on hover). The test of
  setting tooltips now checks every Tip/HelpMarker key exists in English and French (667 keys, none missing).
- 0.914: H3 + H4. One save path for settings (TrySave in SettingRow.cs, UI/SettingSave.cs testable): a failed save restores the previous value, logs
  once and shows one error line in the main window's footer (with a hide button; the map layers popup, a separate window, keeps its own). Alarm options
  are now edited on a copy, so a failed save really restores them (the restore used the same object before). IntSetting restores too. Sliders and number
  fields (gear, wear, alarm bar, command delay, map settings, macro copy, sale days) change live but write the file only when the edit ends or when no
  control is held (map settings: colour pickers make « edit ended » unreliable), no more disk write per frame or per key. One sale-days editor.
- 0.915: H5 + H6. Every on/off setting row has its reset button (BoolSetting's default is required; one DefaultButton); new ComboSetting for plot
  size, hunt call channel, market scope and macro book; raw checkboxes turned into BoolSetting (reminders, Fashion Report options one per row, macro copy
  switches, cost options, guide rows); the guide's map replacement row uses the same tooltip as the settings; help texts shown twice now shown once (as the
  tooltip); « Show translation keys » kept in Advanced › Localization only. Every setting tooltip ends with « Default: … » / « Par défaut : … » (99 tips
  added, 239 in all), checked against a fresh Configuration by a test. Raw checkboxes kept on purpose are listed in the test with their reason.
- 0.920: H7. One look for page status lines: data errors through DrawDataError (red, raw text in developer mode only), empty and loading lines through EmptyState (muted, wrapped), copy buttons through CopyButton (small, « Copied » for two seconds). Counts: {'error': 9, 'empty': 22, 'copy': 24}. A ratchet test (StatusLines920Tests) forbids the hand-written forms in MainWindow*.cs. Kept: the Home alerts (gold « in progress » lines, all alike).
- 0.921: H8. DrawGameIcon(iconId,size,keepSpace) in UI/Components/ItemCell.cs: 41 DrawItemIcon(new ItemDefinition(0,…)) workarounds and 6 raw icon widgets (chocobo barding, key items, map lists, map tool bar, map hover, vistas) go through it; one fallback key for an unnamed item (BankUi.Item; MonsterUi/HoverUi/CraftPresetMessages/CraftLogUi.Item removed in en and fr, de and ja had none); ToolButton moved to UI/Components, square on the frame height. Kept: icons drawn on the map image (draw list), the ItemCell rows of the plan (ChocoboBarding, Events, CurrencyValue, CraftLive) left for a page-by-page look, since their names are not plain items there.
- 0.922: H9. Duty finder, item hover and map position failures logged once until a good read (every 5 s before); garden chat lines, retainer sale rows, Grand Company supply window and the panel/map « steps aside » lines logged in developer mode only; the five « catalog could not be built » at Warning (the plugin keeps running); every interpolated log message turned into a template (Squadron probe, title changer, overlays, sheets, garden crossbreeds). Test Logs922Tests keeps it so.
- 0.923: H10, part 1. AtomicFile.Write (unique temporary name, deleted in finally) used by the eight saved-file writers (craft lists, voyage history, gil journal, my tools, character status, discoveries, market prices seen, GitHub exchange); three of them used a fixed « .tmp » name. An unreadable github.json is kept as .broken. Kept as they are: DataCache (own temporary handling, tested) and BankFileStore (File.Replace with a .bak copy).
- 0.924: H10, part 2. The five watchers that make several subscriptions (collections, chat map links, item context menu, item discoveries, developer reading capture) own them through RegistrationScope: a failure part-way undoes the ones already made, Dispose releases them in reverse order. A recount found no watcher that never undoes a subscription (the plan's « 9 readers and 4 watchers » counted single-subscription readers, already paired in their Dispose).
- 0.925: H10, part 3. BackgroundWriter (one ordered queue per store, failures reported): the gil journal (once a minute), the character status (every 10 min and each « done » box ticked from the window) and the voyage history (each workshop read) are written off the framework thread from a copy; the one-time migration of the shared « done » boxes stays synchronous (it needs the result); shutdown waits up to 3 s. Left for the user: CompetitiveGate.Cut in UndercutWatcher.OnOfferings (DEC-038).
- 0.927: P4. IUnlockState.IsItemUnlockable / IsItemUnlocked (members checked with tools/ApiProbe: both take a Lumina Item row) asked for any item, cached 5 s, null when not logged in or in PvP; the hover shows « Teaches: already learned / not learned yet » for unlocking items outside the four tracked collections, and fills an unknown collection state; the loot « not obtained only » filter and the currency value use it. The ItemUnlockCopy table (mount and minion ids) stays: it gives the collection row to open, which the game's yes/no does not.
- 0.928: P5 (ventures). RetainerTask rows that are not random, through Task to RetainerTaskNormal.Item (members checked with tools/ApiProbe), give the targeted ventures of an item with the retainer level and job category; shown on the item hover; the LuminaSupplemental « venture » line stays only for items the sheets do not list (exploration ventures, whose items are not in the sheets).
- 0.929: P5 (vistas). Sightseeing log numbers from AdventureExPhase (raw rows read with tools/XbmProbe: 2162830-2162891 ExVersion 2, 2162892-2162936 3, 2162937-2162982 4, 2162983-2163010 and 2163011-2163027 5), A Realm Reborn the first 80 Adventure rows; the wiki's number stays only as a fallback. Before, vistas after A Realm Reborn had a number only when the wiki row matched.
- 0.930: P5 (blue magic). When AozActionTransient.Location points to a ContentFinderCondition row, its ClassJobLevelRequired is kept on the collection entry (DutyLevel) and shown after the place.
- 0.931: P5 (S ranks). New probe mode PROBE_SRANK (tools/ShopCurrencyProbe): the 50 marks of SRankConditions all match a rank 3 NotoriousMonster of their territory, and no S rank of the game is missing (docs/evidence/0.931-srank-check.md); a test keeps the table and the probe output in step.
