# X-93 part 1 — discoveries under load (0.1036)

Measured on 2026-11-10, before any change of the plugin's logic (R07: nothing new in the plugin before the release that follows game patch 7.58).
This page gives the figures of the load test asked for by X-93 part (1), then, for each part (2) to (7) of X-93, what the figures say: needed,
not needed, or the size from which it becomes needed. The earlier, smaller test of 0.938 to 0.943 is in
[0.938-discoveries-load.md](0.938-discoveries-load.md); its index by kind and subject, the shared facts kept apart by kind and compressed,
the kinds read on demand and the 200 000-per-kind bound are in the code measured here.

## How it was measured

- **Machine**: Intel Core i9-13900KF (32 logical CPUs), 31.8 GB of memory, Windows 11 Pro, local SSD; .NET 10.0.14 (Release build, no debugger);
  Python 3.12.10.
- **C# side**: `tools/DiscoveryLoadProbe` links the plugin's own files (`Discoveries/DiscoveriesModel.cs`, `CommunityDiscoveries.cs`,
  `ItemGains.cs`, `AtomicFile.cs`) and calls them as the plugin does: `DiscoveryFiles.Write` / `Read` / `WriteSplit` / `ReadSharedKind`,
  `DiscoveryStore.Load` / `Snapshot` / `Merge` / `Find` / `Summary` / `SeeSource`, `CommunityDiscoveries.Parse` / `Mine` / `Encode`,
  `SeenPlaces.ByMap`. The page and tooltip reads repeat, call for call, the store calls of `MainWindow.Discoveries.cs` (`MainWindow` itself
  cannot be built outside the game); names are a cached lookup, as there.
- **Python side**: `tools/discoveries-load.py` imports `tools/discoveries-merge.py` as it is and runs what `sync` does without the network:
  `read` + `clean` of every share, the player files written, then `rebuild` (writes `discoveries.json`) and `rebuild` again with nothing new.
  Each run in its own process, so that the peak memory is that run's.
- **Data**: invented, shaped like real data. Kind mix: the shared ticket #43 (1 453 facts) and a real local base (1 772 facts) added together
  (mostly kind 1 MonsterPlace, then 3 FateSeen, 23 FishBite, 10 FishCatch, 11 FishCondition, 28 SpearShadow, 21, 4, 12…), plus a small share of
  every other kind so that each code path runs. Subjects bounded as in the game (12 000 monsters, 2 600 FATEs, 1 600 fish…), popular ones
  more often; one to three maps per subject, places around a centre, 50-yalm cells; sightings 64 % once, 18 % twice, 8 % three times, the
  rest up to 40 (the local base's shape); every own fact has its value histogram (DEC-042 version 2). Shared side: 70 % of the facts held by
  one player, 20 % by two or three, 10 % by four to twenty, 100 players. Every fact is distinct (distinct key); at a million facts four small
  kinds run out of distinct keys and are filled by the others (exact counts printed by the tool).
- **Runs**: C#: 5 runs at each size; Python: 3 runs at each size. Figures below: median (minimum–maximum).
- **To run again** (from `C:\Plugins-ff14`, a few minutes each; nothing is written outside a temporary folder):

```powershell
dotnet run -c Release --project tools/DiscoveryLoadProbe -- --sizes 100000,1000000 --runs 5 --json load-cs.json
python -I -X utf8 tools/discoveries-load.py --sizes 100000,1000000 --runs 3 --players 100 --json load-py.json
```

`Verify.ps1` does not run them (minutes, gigabytes of memory); it runs only `LoadTest1036Tests`, which checks that both tools are in place, call
the plugin's and the merge tool's code, and invent facts from the same table of kinds.

## Your own base (Discoveries.json, facts you saw)

| Measure | 100 000 facts | 1 000 000 facts |
|---|---|---|
| `Discoveries.json` size | 29.3 MB (307 bytes a fact) | 293 MB (308 bytes a fact) |
| Over `DiscoveryFiles.MaxBytes` (64 MB)? | no | **yes: `Read` returns nothing** |
| File write, `DiscoveryFiles.Write` (off the frame) | 141 ms (139–478) | 1 546 ms (1 526–1 710) |
| File read, `DiscoveryFiles.Read` (off the frame, at start) | 227 ms (217–304) | refused by the bound; the same read past it: 2 506 ms (2 494–2 817) |
| `DiscoveryStore.Load` (under the store's lock) | 220 ms (215–312) | 2 214 ms (2 103–2 270) |
| Memory held by the store | 48.8 MB (511 bytes a fact) | 459 MB (482 bytes a fact) |
| `Snapshot` (**on the frame**, every minute while the base changed) | **21.2 ms** (20.4–24.8) | **339 ms** (331–346) |
| `WriteSplit` (off the frame) | 167 ms (157–415) | 1 571 ms (1 561–1 699) |
| Share sent to your gist: `Mine` + `Encode` (off the frame) | 585 ms, 5.1 MB of text | 5 916 ms, 43.2 MB of text |
| One more sighting, `SeeSource` (new fact) | < 0.001 ms | < 0.001 ms |

The JSON reader also keeps its large buffers pooled after a read of the whole file (ArrayPool): up to 128 MB after a 29 MB file and 1 GB after the
293 MB one, measured with `GC.GetTotalMemory` (noisy: 0 to 128 MB at 100 000).

## The shared base (common file, kind files)

| Measure | 100 000 facts | 1 000 000 facts |
|---|---|---|
| Common file `discoveries.json` (the plugin's serialization; the merge tool's is smaller, below) | 30.0 MB | 301 MB |
| Over `CommunityDiscoveries.MaxBytes` (32 MB)? | no (94 % of it) | **yes: the exchange fails** |
| `CommunityDiscoveries.Parse` (off the frame) | 157 ms (152–160) | 1 993 ms (1 893–2 117) |
| `Merge` into an empty base (under the lock) | 294 ms (286–355) | 2 241 ms (2 170–2 261) |
| Facts held after it / left out by the 200 000-per-kind bound | 100 000 / 0 | 495 307 / **504 693** |
| `Merge` again, every fact joined (under the lock) | 256 ms (250–276) | 1 543 ms (1 510–1 602) |
| `WriteSplit`, kind files in gzip (off the frame) | 662 ms (658–667) | 2 831 ms (2 817–2 869) |
| `WriteSplit` again, nothing changed | 204 ms (198–232) | 946 ms (914–956) |
| Kind files, all / largest (kind 1) | 4.5 MB / 3.2 MB | 19.2 MB / 8.5 MB |
| Kind 1 file read on demand, `ReadSharedKind` (off the frame) | 139 ms (137–146) | 441 ms (418–442) |
| Kind 1 file loaded (under the lock) | 73.5 ms (72.3–74.3) | 208 ms (202–220) |
| Memory, every kind held | 48.8 MB (512 bytes a fact) | 225 MB (477 bytes a fact, 495 307 held) |

## Reads of the pages and tooltips

Median time of one call; « own » is the base of the first table, « shared » the second one (where the per-kind bound holds 495 307 facts).

| Read (where the plugin makes it) | own 100 000 | own 1 000 000 | shared 100 000 | shared 1 000 000 |
|---|---|---|---|---|
| R1 `Summary()`: Discoveries page, **every frame while it is open** | 4.4 ms | **55.0 ms** | 3.5 ms | **21.3 ms** |
| R2 `SeenFishLine`, its 7 searches (fishing help, a fish, cached until the base changes) | 0.026 ms | 0.051 ms | 0.028 ms | 0.042 ms |
| R3 `DiscoveredSourcesOf`, 10 kinds (item tooltips, an item, cached until the base changes) | 0.064 ms | 0.210 ms | 0.047 ms | 0.176 ms |
| R4 `SeenNpcPlace` (vendor places, an NPC) | < 0.001 ms | 0.001 ms | < 0.001 ms | < 0.001 ms |
| R5 `SeenPlacesOf` (a monster's places: hunt, map, item pages) | 0.127 ms | 0.151 ms | 0.121 ms | 0.124 ms |
| R6 `Seen()` index rebuilt (**on the frame, after each change of the base**; nearby monsters, item menus, duties, beastmaster, sources) | 2.0 ms | **33.6 ms** | 1.7 ms | **34.8 ms** |
| R7 Discoveries page search, `DrawDiscoveryPlaces` (each new text or change) | 3.9 ms | 12.9 ms | 2.4 ms | 9.1 ms |
| R8 one zone through `Find` (reference only: **no page reads by zone today**) | 72 ms | 815 ms | 53 ms | 301 ms |

A frame at 60 images a second lasts 16.7 ms. The base's revision changes at every new fact (and at every `SeeSource`, even a fact seen again), so
R6 can run often while you play with one of its pages or the nearby monsters panel open.

## The merge tool (tools/discoveries-merge.py)

100 players; each fact held by one to twenty of them, so a run reads about 2.4 sightings a fact. The largest share stays far under the tool's
200 000-fact bound per share (2 506 facts at 100 000, 24 267 at a million). No fact refused.

| Measure | 100 000 facts | 1 000 000 facts |
|---|---|---|
| Sightings in the shares | 237 220 | 2 398 881 |
| `read` + `clean` of every share, player files written (`sync` without the network) | 12.2 s (12.2–12.3) | 116 s (116–117) |
| `rebuild`: `discoveries.json` written | 10.2 s (10.2–10.3) | 99.4 s (99.1–99.4) |
| `rebuild` again, nothing new (X-113: not written) | 8.0 s (8.0–8.0) | 84.9 s (84.6–85.3) |
| `discoveries.json` written by the tool | 26.0 MB (273 bytes a fact) | 261 MB (273 bytes a fact) |
| Player files (`community-discoveries/players/`, in git) | 58.0 MB | 587 MB |
| Peak memory of the process | 294 MB | 2 638 MB |

The common file the players download is the one this tool writes: 273 bytes a fact (dates to the day, no `Key`), a little smaller than the
plugin's own serialization above (300 to 315 bytes a fact, used for the C# parse and merge times).

## What the figures say for X-93 parts (2) to (7)

Today's sizes, for scale: your base 1 772 facts (505 KB), the common file 1 453 facts (339 KB). Sizes « from which » below are worked out from the
two measured points (100 000 and 1 000 000), linearly unless said otherwise; they are estimates, not measures.

**(2) Load and save off the game's thread, never the whole file at each sighting — needed, first.**
- `Snapshot` copies the whole base **on the frame** every minute while it changed (own and shared facts held): 21 ms at 100 000 facts, 339 ms at a
  million. It grows a little faster than the base: one frame (16.7 ms) is passed from **50 000 to 80 000 facts held** (worked out from the million and
  from the 100 000 measure), 4 ms (a quarter of a frame) from 12 000 to 20 000. Today, about 1 800 facts: well under a millisecond.
- **Data loss threshold**: `DiscoveryFiles.Read` returns nothing for a `Discoveries.json` over 64 MB, without an error and without setting the file
  aside; the plugin then starts from an empty base and its next save (after the first new sighting) writes over the large file. At 307 bytes a fact
  that is **about 218 000 of your own facts**. Your base held 1 772 facts on 2026-11-09 (X-93 noted 409 KB after about a day of play); how fast a
  base grows over months was not measured, but X-93 plans for months of play. To fix before that size (not in this version, R07): never save over
  a file that could not be read, then read it in parts or raise the bound with (2).
- Loading at start is already off the frame (`Task.Run`), but `Load` (2.2 s at a million) and `Merge` (2.2 s) hold the store's lock: a page or
  tooltip that searches during that time waits on the frame. Worth fixing together with the snapshot: build off the lock, swap in.
- Writing is already off the frame and grouped (once a minute); a full rewrite costs 167 ms at 100 000 and 1.6 s at a million, acceptable off the
  frame; appending (« never the whole file ») is not needed below a million.

**(3) Index by kind, item and zone — by kind and subject: done and enough; by zone: not needed until a page reads by zone; two reads of whole kinds need counters or an index from 150 000 to 300 000 facts.**
- Tooltip reads that go through the 0.938 index stay under 0.25 ms at a million facts (R2 to R5): nothing to do.
- No page reads by zone today; through the present API one zone would cost 0.3 to 0.8 s (R8): a zone index is needed **only when such a page is
  planned**.
- `Summary()` (R1) walks the whole base on every frame of the Discoveries page: 4.4 ms at 100 000, 55 ms at a million; one frame from **300 000
  to 380 000 facts**, 1 ms from about 20 000. Counters kept per kind at each insertion (or the summary worked out once per revision) are needed
  from there.
- `Seen()` (R6) is rebuilt on the frame after every change of the base: 2 ms at 100 000, 34 ms at a million (it sorts whole kinds); one frame
  from about **500 000 facts**, 4 ms from about 150 000 (interpolated between the two measures: the cost grows faster than the base). Needed from there: kept up to date at insertion, or rebuilt off the frame.

**(4) Bound per fact and coarser readings — not needed.**
- One fact costs about 480 to 510 bytes of memory and 307 bytes of file, with its histogram (at most 32 values, DEC-042) included: a fact is
  already bounded; the size grows with the **number** of facts, not with each fact.
- 50-yalm cells: at a million invented facts the MonsterPlace facts are 70 % of the base, as in real data. Coarser cells would cut that count,
  but they would change the key of a fact (format frozen by DEC-042 version 2, part 8): not worth it while the sizes above hold.

**(5) Common file split, downloaded on change, compressed — needed from about 100 000 to 120 000 shared facts.**
- The plugin downloads the common file with a 32 MB bound (`CommunityDiscoveries.MaxBytes`); past it the whole exchange fails (`ExchangeFailed`,
  your own share is not sent either). The file written by the merge tool takes 273 bytes a fact: the bound is reached at **about 123 000 facts**
  in the common file (26 MB at 100 000 facts is already 81 % of it; the plugin's own serialization, 315 bytes a fact, would reach it at about
  106 000). Today: 1 453 facts (339 KB). GitHub's 100 MB limit per file (part 6) comes next, at about 384 000 facts.
- gzip makes the shared facts 7 to 8 times smaller (19 MB of kind files for 495 307 facts held; 4.5 MB for 100 000): compressing the common file
  (or splitting it by kind, as the local copy already is) keeps the download far under the bound up to a million facts.
- The exchange downloads the whole file once a day and only then compares its hash; downloading only on change (an ETag or a small index with each
  part's checksum) saves 30 MB a day per player from 100 000 facts.
- The 200 000-per-kind bound leaves half of a million-fact common file out of memory (504 693 facts, all of kind 1): kind 1 alone reaches the bound
  at about **285 000 facts in the common file**. Shared facts held cost 477 bytes each (225 MB at the bound for the kinds read).

**(6) Merge tool: limits, measured time, batches — limits per player: there; time: fine by hand; memory and git: a bound from about 190 000 facts.**
- Limits already in place hold: a share is cut at 200 000 facts (`clean`) and refused over 64 MB once uncompressed (`from_gist`); the refusal
  over 5 % and the « nothing new » path work at a million facts (no refusal, second rebuild not written).
- Time: a whole `sync` (without the network) and `rebuild` take 22 s at 100 000 facts and 3.6 min at a million; most of `rebuild` is reading
  and checking every player file again (85 s even with nothing new). Batches of players are **not needed** for a tool run by hand; reading
  only the changed players' files would be the first gain if it ever runs often.
- Memory: 294 MB at 100 000, 2.6 GB at a million: fine on this PC (32 GB); nothing to do before several million facts.
- **Git**: `discoveries.json` and the player files are committed. GitHub warns over 50 MB a file and refuses a push with a file over 100 MB
  (GitHub's documented limits, not measured here): at 273 bytes a fact, about **190 000 facts** for the warning and **about 384 000** for the
  refusal. With part (5), the common file should be split (by kind, as the local copy already is) or compressed before then; the player files
  (58 MB at 100 000 facts in all, each player's under 1 MB here) stay under the limit per file.**

**(7) Discoveries page and export: pagination and filters — needed for the page's totals from about 300 000 facts; the list is already bounded.**
- The page lists at most 200 + 100 + 100 + 200 rows (its search), so the rows themselves need no pagination; its search costs 13 ms at a million
  (R7), once per new text.
- Its cost per frame is `Summary()` (R1, part 3 above): from about 300 000 facts the page itself would pass a frame.
- Export: a full export of a million own facts is a 293 MB file (1.5 s off the frame), and your gist share would be 43 MB of text, encoded in
  5.9 s off the frame. GitHub's size limits for a gist were not measured here; the merge tool refuses a share over 64 MB once uncompressed
  (`from_gist`) and keeps at most 200 000 facts of one share (`clean`): a player's share is cut from about **200 000 own facts**.

**(8) Format of DEC-042 version 2 — unchanged by this test.** Every measure above uses the frozen format as it is.
