# X-119 — the desktop application, step 2, part 1: the local link with the plugin (2026-11-11, 0.1081)

Plan 4 of the desktop application (decision D-134), step 2: « local link with the plugin (named pipe, current user only, no network port,
off by default; state read on the framework thread, copied, sent off the thread, R07) and live pages ». This part builds the link itself
and shows it in the application's status bar; the live pages (the 17 NeedsLink pages of `DesktopPages.cs`) and the actions (step 3) are
not done. Not validated in game: the in-game checks are in the final campaign (R34).

## What was built

| Part | Where | What it does |
|---|---|---|
| Protocol | `src/TenshoAssistant/Link/LocalLink.cs` (`LocalLinkProtocol`, `LinkSnapshot`) | One JSON line per snapshot: protocol number (1), plugin version, logged in, paused (competitive cut), territory id and its name. A line over 4,096 bytes, malformed, of another protocol, or with a field over its bound (version 32, zone 128 characters) is refused. The same file is compiled into the plugin, the application (linked by its csproj) and the tests: one source for both ends. |
| Pipe name | `LocalLinkProtocol.PipeName()` | `TenshoAssistant.Link.1.` + the first 16 hex digits of SHA-256 of `DOMAIN\user` (upper case): the same in the game and in the application, apart between Windows users of one PC. |
| Plugin's end | `LocalLinkServer` | One pipe instance (`FirstPipeInstance`, at most 1), **out only** (`PipeDirection.Out`: nothing the application sends is ever read). Access list: the current Windows user allowed, the `NETWORK` well-known SID denied (no logon from another machine). Written on a worker task: the latest snapshot when it changes and again every 5 s, so a closed application is seen (write error) and a new instance offered. A second game client of the same user finds the pipe taken: « Busy », tried again every 3 s. `Dispose` cancels and waits 1 s at most. |
| Feed | `src/TenshoAssistant/Link/LocalLinkFeed.Dalamud.cs` | Once a second on the framework thread, through `CompetitiveGate.Add` (DEC-038: nothing read during a cut): the setting is read, the pipe opened or closed to match, then `IsLoggedIn`, `IsLoaded`, `TerritoryType` and the territory's place name (from the sheets) are copied into a snapshot handed to the worker (R07). When a cut starts, a « paused » snapshot without game data replaces the last one. Owned by `Plugin.cs` through its registrations (removed and closed on unload). |
| Setting | Settings › Advanced › « Link with the desktop app » (`Configuration.DesktopLink`) | Off by default, reset by « restore every setting », its state under it (off, waiting, linked, busy, unavailable and why). |
| Application's end | `LocalLinkClient`, `src/TenshoAssistant.Desktop/Program.cs`, `StatusBar.cs` | Connects with `PipeOptions.CurrentUserOnly` (the runtime refuses a pipe whose owner is another user), **in only**, reads bounded lines, keeps the last snapshot; one older than 15 s (three heartbeats) means not linked. Tries again every 2 s. The status bar says « Linked to the game: plugin x, in <zone> » (or no character, or paused), and on a NeedsLink page that its live parts do not come from the link yet. `--no-link` leaves it closed. |

No network port is opened: a named pipe is local to the PC, and network logons are denied by the access list. The link carries no
other player's data, no chat, no position: who (logged in or not) and where (the zone).

## Protocol 2 (0.1114, X-119 step 2 suite)

The user's default of 2026-11-11 (« the plugin sends a notice "this file changed" and the application reads the plugin's saved files again;
only the small live states pass whole »):

| Part | What | Bounds |
|---|---|---|
| Small live states (`LinkLive`) | Job abbreviation and level, GP and maximum GP (gatherers), free slots of the four bags and their size; read with the snapshot on the framework thread, copied; a reading that fails once is left out for the session (R08). | Job ≤ 16 characters, level ≤ 1000, GP ≤ 100 000, bag size ≤ 1000, free ≤ size |
| Change notices (`LinkChange`) | Two file watchers while the link is on: the settings file (`pluginConfigs\TenshoAssistant.json`) and the plugin's folder (`pluginConfigs\TenshoAssistant`); their events only add a name and a generation to a log. Temporary files, the data cache and `PlayState.json` are left out. Never a file's content. | 16 names per snapshot (newest generation of each), 160 characters each, relative, « / » only, no « . » or « .. » part |
| Application | `LinkChangeReader` takes each new name once (a new plugin start counts again); for the settings file, `Host.ReloadSettings` reads it again into the window's own settings (Newtonsoft populate, lists replaced), between frames, read-only; the status bar says when. The other saved files are not read by the app yet. | Line ≤ 8 192 bytes; another protocol number is refused (the pipe's name carries it) |

## Protocol 3 (0.1119, X-149)

GPT's review of 2026-11-11 (accepted by the user): protocol 2 sent the names of the saved files that changed, and the bank's and the
characters' files are named by the character's id, while the application only reads the settings again. Protocol 3 sends categories:

| Part | What | Bounds |
|---|---|---|
| Change notices (`LinkChange(Category, Generation)`) | `LocalLinkProtocol.ChangeCategory`: the settings file gives `settings`; a file or sub-folder of the plugin's folder gives the category of its name up to the first dot (`InventoryBank` → `bank`, `Characters` → `characters`, `GilJournal` → `gil-journal`, `CraftLists` → `craft-lists`, `MyTools` → `my-tools`, `VoyageHistory` → `voyages`, `AchievementProgress` → `achievements`, `BeastTeams` → `beast-teams`, `DutyProgress` → `duties`, `Discoveries` → `discoveries`); anything else (temporary and lock files, data cache, play state, unknown files) is not sent. | A category outside the closed list is refused by the application (« change list refused ») |
| Compatibility | The pipe's name carries the protocol (`TenshoAssistant.Link.3.…`): a plugin and an app of different protocols never exchange a line. The app, when no pipe of its protocol answers, lists the local pipes and, if one of another number exists for the same user (`LocalLinkProtocol.OtherProtocol`), its status bar says so and asks to install the app of the plugin's version. | Only the names of local pipes are read |

Behaviour test: `LinkCategories1119Tests` (real files of a plugin folder named by a character id, their notices through a real pipe, the bytes
received searched for any file name or id).

## Alerts of the game (0.1155, X-163)

The user asked (12/11) that the reminders ringing in the game also ring in the desktop application. The Tensho alarms and the garden are
computed by the application itself (AlarmBell 0.1132, GardenBell 0.1153); the other alerts need the game, so the plugin sends them:

| Part | What | Bounds |
|---|---|---|
| Alerts (`LinkNotice(Id, At, Text)`, field `Notices` of the snapshot) | Filled by `Plugin.cs` where an alert rings in the game (chat line or sound allowed): currency thresholds, duty ready, GP threshold, gear to repair or materia to extract, important items at a shop (when their sound is on), A and S hunt marks; never a Tensho alarm or the garden (rung by the application's own rule). Text in the plugin's language, as the chat line said it; no character name. | `LinkNoticeLog`: the newest 4, 200 characters each, kept 2 minutes; the application refuses more (« notice list refused »); the worst line stays under 8 192 bytes (test) |
| Application | `LinkNoticeReader`: each alert once by its number; at the first snapshot and after a plugin start (sequence counting again), only the alerts of the last 30 seconds; a lost link keeps the memory. `MainWindow.DesktopLinkNotices` rings them with the alarms' options (only when the game is not in front, Windows sound, Windows notification). | Read once a second with the bell |
| Compatibility | Optional field of protocol 3: an application of before 0.1155 ignores it, a plugin of before sends none. | — |

Behaviour test: `LinkNotices1155Tests` (alerts through a real pipe, read by the application's reader: once each, history not rung, a plugin
started again rings its new ones; worst line measured).

## Settings read by the application (0.1120, X-150)

On a `settings` notice the application reads the plugin's settings off the frame (`Grouped`: a burst of notices is one reading), into a
new object: the file read whole and bounded to 8 MB while the game may write it (`SettingsGuard.ReadBounded`), the Discord addresses
(`WebhookUrl`, `CategoryUrls`) taken out at any depth (`SettingsGuard.Project`), then Newtonsoft with type names whose binder accepts
only the plugin's types, primitives and collections of those (`SettingsGuard.AllowedType`, audit A15). A valid reading is applied at once
between two frames (`Host.ApplySettings`); a failed one is said and the last valid settings stay. Behaviour test: `SettingsRead1120Tests`.

## How it is checked without the game

- `tests/TenshoAssistant.Tests/LocalLink1081Tests.cs`: the wire format both ways and its refusals; a real pipe on Windows (a unique test
  name): two snapshots go from `LocalLinkServer` to `LocalLinkClient`, a second server of the same name is Busy, `Dispose` turns it off;
  the wiring (off by default and reset, out only, network denied, current user only on the client, competitive gate, registrations, the
  application compiling the same file, FAQ entry).
- `Verify.ps1`'s headless run of the application: the status bar's link texts (linked in a zone, no character, paused) on a page that
  needs the link; a missing text fails the run.

## Limits, to see in game (final campaign)

- A game started as administrator and an application started as a normal user: the pipe may be refused by Windows' integrity levels
  (not measured); the application then simply stays « not linked ».
- Wine or Proton (Linux, Steam Deck): the access list may not be supported; the setting then says « unavailable » and nothing else changes.
- Two game clients of one Windows user: the first holds the link, the second says « busy ».
