# X-76 step 2 — duplicated logic and incoherences found (2026-11-08)

Found by four read-only agents (one theme each: places and times, Draw caches, game windows and HTTP, words and comments), each finding read on site.
« Change » = the copies differ so merging changes what a user sees. One family per version; the state column is kept up to date.

| # | Finding | Sites | Risk | Change | State |
|---|---|---|---|---|---|
| 1 | Same AtkValue-to-int converter written twice, and the 'is this value text' check written 7 times | 9 | low | no | to do |
| 2 | ReadingCapture dumps window numbers with the same loop 3 times in one file | 3 | low | no | to do |
| 3 | 'Is the window still the same and shown' check after copying, written 4 times (plus the same opening check) | 4 | low | no | to do |
| 4 | Incoherence: native node-tree walks do not use the same safety guard (R-14 NodeListSize/LoadedState only in some) | 6 | medium | no | to do |
| 5 | BoundedHttp re-implemented by hand in RetainerCompareService and CommunityHelpIndex | 3 | low | no | to do |
| 6 | Tensho User-Agent built in 5 places with different values; GatherBuddyFish sends it twice; Market services set it on injected clients | 5 | low | no | to do |
| 7 | Incoherent HttpClient ownership: MarketBoardService disposes a client it may not own; 14 copies of 'client ?? new HttpClient{Timeout}' + owns flag | 14 | low | no | to do |
| 8 | Incoherence: AgentMap.Instance() dereferenced without the null check used elsewhere in the same file | 5 | low | no | to do |
| 9 | Developer-log window-value dumps written 3 times with the same switch but different output formats | 3 | medium | yes | to do |
| 10 | Quest / achievement / triad-card lookups rebuilt per page instead of using the shared DataIndexes | 9 | low | no | to do |
| 11 | Per-key memo dictionary reset when a generation changes (Revision, second, catalogue reference): 7 copies with different size policies | 7 | low | no | to do |
| 12 | Two-click 'armed' delete button with the 'dropped if not drawn last frame' guard, written three times | 8 | low | no | to do |
| 13 | Inconsistency: four other two-click delete buttons stay armed after the page is left | 8 | low | yes | to do |
| 14 | Timed gathering point 'open now and time left' worked out in four places, with the countdown text twice | 6 | low | no | to do |
| 15 | 'Ask for the quest journal at most once a minute while it is empty' written three times with two timestamps | 5 | low | no | to do |
| 16 | EquivalentMaps and SameLandMaps: the same single-slot cache of (map, catalogue) to a HashSet, with different guards | 4 | low | no | to do |
| 17 | World position to map coordinate conversion inlined 8 times although a helper exists | 9 | low | no | 0.905 |
| 18 | The "X 12.3 Y 4.5" coordinate text is hand-written with 5 spacings and 2 cultures | 9 | low | yes | 0.905 |
| 19 | Coordinate numbers passed to localized texts: current culture at some sites, invariant at others | 12 | low | yes | 0.905 |
| 20 | Default bookmark name built twice the same way, differently from the import, and saved | 3 | low | no | 0.905 |
| 21 | Eorzea minute of the day computed in 4 places | 4 | low | no | to do |
| 22 | Eorzea "HH:mm" from a minute of the day written by hand about 8 times | 8 | low | no | to do |
| 23 | Weather ET 8-hour window text written 3 times on the Weather page | 3 | low | no | to do |
| 24 | Timed gathering countdown (open or closed, time left, h:mm:ss) repeated on 3 pages | 3 | low | no | to do |
| 25 | "H h mm" duration hard-coded 4 times on the airship and submarine pages | 4 | low | no | to do |
| 26 | Reminders.In "in H h mm (date)" line duplicated between Free Company and Reminders | 2 | low | no | to do |
| 27 | Unused duration formatter CacheAge.Label with a hard-coded French "j" | 1 | low | no | to do |
| 28 | "Seen by you" labels are shown for imported and community facts (10 keys, one rule missing) | 18 | low | yes | to do |
| 29 | Most-seen spearfishing shadow of a fish is computed twice | 2 | low | no | to do |
| 30 | Discovery fact formatting repeated: Seen+Imported sum (10x), level range (2x), Eorzea-hour range (3x) | 13 | low | no | to do |
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
