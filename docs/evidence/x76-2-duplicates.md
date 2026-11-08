# X-76 step 2 — duplicated logic and incoherences found (2026-11-08)

Found by four read-only agents (one theme each: places and times, Draw caches, game windows and HTTP, words and comments), each finding read on site.
« Change » = the copies differ so merging changes what a user sees. One family per version; the state column is kept up to date.

| # | Finding | Sites | Risk | Change | State |
|---|---|---|---|---|---|
| 1 | Same AtkValue-to-int converter written twice, and the 'is this value text' check written 7 times | 9 | low | no | 0.907 |
| 2 | ReadingCapture dumps window numbers with the same loop 3 times in one file | 3 | low | no | 0.907 |
| 3 | 'Is the window still the same and shown' check after copying, written 4 times (plus the same opening check) | 4 | low | no | 0.907 |
| 4 | Incoherence: native node-tree walks do not use the same safety guard (R-14 NodeListSize/LoadedState only in some) | 6 | medium | no | 0.907 |
| 5 | BoundedHttp re-implemented by hand in RetainerCompareService and CommunityHelpIndex | 3 | low | no | 0.907 |
| 6 | Tensho User-Agent built in 5 places with different values; GatherBuddyFish sends it twice; Market services set it on injected clients | 5 | low | no | 0.907 |
| 7 | Incoherent HttpClient ownership: MarketBoardService disposes a client it may not own; 14 copies of 'client ?? new HttpClient{Timeout}' + owns flag | 14 | low | no | 0.907 |
| 8 | Incoherence: AgentMap.Instance() dereferenced without the null check used elsewhere in the same file | 5 | low | no | 0.907 |
| 9 | Developer-log window-value dumps written 3 times with the same switch but different output formats | 3 | medium | yes | to do |
| 10 | Quest / achievement / triad-card lookups rebuilt per page instead of using the shared DataIndexes | 9 | low | no | to do |
| 11 | Per-key memo dictionary reset when a generation changes (Revision, second, catalogue reference): 7 copies with different size policies | 7 | low | no | to do |
| 12 | Two-click 'armed' delete button with the 'dropped if not drawn last frame' guard, written three times | 8 | low | no | to do |
| 13 | Inconsistency: four other two-click delete buttons stay armed after the page is left | 8 | low | yes | to do |
| 14 | Timed gathering point 'open now and time left' worked out in four places, with the countdown text twice | 6 | low | no | 0.906 |
| 15 | 'Ask for the quest journal at most once a minute while it is empty' written three times with two timestamps | 5 | low | no | to do |
| 16 | EquivalentMaps and SameLandMaps: the same single-slot cache of (map, catalogue) to a HashSet, with different guards | 4 | low | no | to do |
| 17 | World position to map coordinate conversion inlined 8 times although a helper exists | 9 | low | no | 0.905 |
| 18 | The "X 12.3 Y 4.5" coordinate text is hand-written with 5 spacings and 2 cultures | 9 | low | yes | 0.905 |
| 19 | Coordinate numbers passed to localized texts: current culture at some sites, invariant at others | 12 | low | yes | 0.905 |
| 20 | Default bookmark name built twice the same way, differently from the import, and saved | 3 | low | no | 0.905 |
| 21 | Eorzea minute of the day computed in 4 places | 4 | low | no | 0.906 |
| 22 | Eorzea "HH:mm" from a minute of the day written by hand about 8 times | 8 | low | no | 0.906 |
| 23 | Weather ET 8-hour window text written 3 times on the Weather page | 3 | low | no | 0.906 |
| 24 | Timed gathering countdown (open or closed, time left, h:mm:ss) repeated on 3 pages | 3 | low | no | 0.906 |
| 25 | "H h mm" duration hard-coded 4 times on the airship and submarine pages | 4 | low | no | 0.906 |
| 26 | Reminders.In "in H h mm (date)" line duplicated between Free Company and Reminders | 2 | low | no | 0.906 |
| 27 | Unused duration formatter CacheAge.Label with a hard-coded French "j" | 1 | low | no | 0.906 |
| 28 | "Seen by you" labels are shown for imported and community facts (10 keys, one rule missing) | 18 | low | yes | 0.908 |
| 29 | Most-seen spearfishing shadow of a fish is computed twice | 2 | low | no | 0.908 |
| 30 | Discovery fact formatting repeated: Seen+Imported sum (10x), level range (2x), Eorzea-hour range (3x) | 13 | low | no | 0.908 |
| 31 | Tool directory: the same reference strings in four switches, and a French reference « Banque d'objets » | 8 | low | yes | to do |
| 32 | Crystal classifier keeps French canonical values, mapped back to English keys by two parallel arrays | 7 | low | no | to do |
| 33 | French storage names still built in BankInventoryBridge while their siblings are English | 3 | low | no | to do |
| 34 | Doc comments detached from their member (stacked <summary> blocks) and one stale cell size | 19 | low | no | to do |
| 35 | French text in English comments: Tensho's own UI labels and user quotes | 11 | low | no | to do |
| 36 | Chat command words: matched in two places by hand, accent variants inconsistent, and a doc comment on the wrong method | 5 | low | no | to do |
| 37 | « No character » state worded three ways on four pages | 8 | low | yes | to do |

## 0.905 — map coordinates (findings 17 to 20)

- One conversion world → map coordinate (`MapProjection.CoordinateFromWorld`), used at the seven sites that wrote it inline.
- One coordinate text (`MapProjection.CoordinateText`, « X 12.3 Y 4.5 ») and one coordinate number (`CoordinateNumber`), always with a dot as the game
  writes them: a Windows set to French showed « 12,3 » on some pages and « 12.3 » on others. Also the monster position from Garland Tools (2 decimals
  before, 1 now, like the game).
- One default bookmark name (`MapBookmarks.DefaultName`), used by the two places that built it.

## 0.906 — Eorzea time and durations (findings 14, 21 to 27)

- One Eorzea minute of the day (`GatheringTime.MinuteOfDay`, the exact formula) for the map, the discoveries and the Discord alarms.
- One « HH:mm » (`GatheringTime.Clock`) for schedules, alarms, discoveries, the fishing panel and the map.
- One weather window text (`WeatherPeriod.EtHours`), one opening state (`GatheringOpening.At(now)`: open, time left) and one countdown text
  (`WindowCountdown`) for the gathering clock, the map tooltip, the lists and the duty finder.
- One « H h mm » for the airship and submarine pages, one « in H h mm (date) » for reminders and the company page.
- `CacheAge.Label` removed (no caller, a French « j » for days in code).
- Texts unchanged.

## 0.907 — game windows and web requests (findings 1 to 8)

- `UI/AtkValueRead`: one number reader and one « is text » check for the game windows' values (was 2 + 7 copies).
- The developer-mode capture dumps a window's values through one helper (3 loops before); output identical.
- `UI/GameWindowCheck.StillShown`: the « same window, still shown after copying » check of four readers (craft, gathering, mini cactpot, triad).
- `UI/UiNodes.Walkable`: one node-tree guard (R-14: node count within the list size, component loaded) for the six walkers; four of them had only
  part of it (gathering node, collectable and storage highlights, beastmaster probe). **To watch in game**: these highlights and the gathering read.
- Two hand-written bounded downloads (retainer prices, community help) now go through `BoundedHttp` (same limits, counted by the self-check, strict
  UTF-8 handled); one Tensho User-Agent; the market board service no longer disposes a client it was given; the map no longer reads AgentMap
  without checking it (two places).

## 0.908 — discoveries (findings 28 to 30)

- The tools read the whole discoveries base, own and imported facts alike (the shared GitHub file), but sixteen labels said « seen by you »: they
  now say « seen in game » (every fact is an observation in game, the player's or another player's). The Discoveries page keeps its « seen by you » /
  « imported only » counts, and « nothing seen by you to share » stays (only your own facts are shared).
- One most-seen spearfishing shadow (`DiscoveryStore.MostSeenSpearShadow`), one « times seen » (`DiscoveryFact.TimesOf`), one level range text.
