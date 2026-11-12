# X-172 part 1 — SQLite trial for the local storage

Measured on 2026-11-12; the trial itself changes nothing in the plugin. The probe was built from the working copy of
`DiscoveriesModel.cs`, which already held uncommitted edits of X-172 part 2 (keys of changed facts tracked in `DiscoveryStore`): `WriteSplit`,
`ReadBase` and `ReadShared` were as committed; `DiscoveryStore.Load` adds two lists that stay empty for these facts (none refused or moved to
another cell). The question: is it worth moving the plugin's local storage (the discoveries base,
then the download cache) from JSON files to SQLite before the public announcement? This page gives the figures; it does not decide.

## How it was measured

- **Machine**: Intel Core i9-13900KF (32 logical CPUs), 31.8 GB of memory, Windows 11 Pro (10.0.26300), Samsung SSD 980 PRO (temporary
  folder on it); .NET 10.0.14, Release build, no debugger.
- **Packages** (NuGet, both published over two weeks before the trial): `Microsoft.Data.Sqlite` **10.0.14** (2026-10-25) and
  `SQLitePCLRaw.bundle_e_sqlite3` **2.1.14** (2026-10-13), which brings SQLite **3.54.0**. The bundle is set above the 2.1.13 that
  Microsoft.Data.Sqlite 10.0.14 asks for: NuGet flags `SQLitePCLRaw.lib.e_sqlite3` up to 2.1.11 as vulnerable (GHSA-2m69-gcr7-jv3q); the first
  build, with Microsoft.Data.Sqlite 9.0.0 and 2.1.10, warned NU1903 for that reason.
- **Tool**: `tools/SqliteTrialProbe`. It links the plugin's own `Discoveries/DiscoveriesModel.cs`, `Discoveries/ItemGains.cs` and `AtomicFile.cs`
  and calls them as the plugin does. Each write and each read of a storage runs in a **process of its own**, so that its memory is its own.
  Offline; it writes only to a temporary folder, removed at the end. `Verify.ps1` does not run it.
- **Data**: invented, with the table of kinds and the drawing of `tools/DiscoveryLoadProbe` (copied): mostly MonsterPlace, then FateSeen, fishing
  kinds, desynthesis…, every kind present; every second fact yours (`Seen` > 0, value histograms, one in twenty also imported with its `Own`
  part), the others shared (`Seen` 0, imported by 1 to 20 players, imported histograms).
- **Present storage**: `DiscoveryFiles.WriteSplit` (yours in `Discoveries.json`, the shared facts in `Shared/kind-NN.json.gz`); read back with
  `DiscoveryFiles.ReadBase` + `ReadShared` and `DiscoveryStore.Load`. The read is done past the 64 MB bound of `ReadBase` (`max` raised) so that
  its time can be measured: the plugin itself would set a larger file aside and start empty.
- **SQLite**: one file, `journal_mode=WAL`, `synchronous=NORMAL`, `busy_timeout=2000`, pooling off. One table `facts`, one row per fact, primary
  key = the fact's key columns `(kind, subject, detail, territory, map, cellx, cellz)`, `WITHOUT ROWID` (the table is stored in key order: kind
  first, so the key is the index on kind and the rows of one kind are one range). Other columns: `x`, `z` (REAL), `levelmin`, `levelmax`,
  `seen`, `imported`, `first`, `last` (UTC ticks), `hq`, `importedhq` (INTEGER), `vals`, `importedvals`, `own` (small JSON texts, NULL when empty).
  Full first write in one transaction; save = upsert (`INSERT … ON CONFLICT DO UPDATE`) of the changed facts only. Full read = every row into
  `DiscoveryFact`s, then the same `DiscoveryStore.Load`.
- **Memory**: « added, peak » = highest value sampled every 2 ms during the step minus the value just before it (`GC.GetTotalMemory(false)` for
  managed memory, `Environment.WorkingSet` for the process); « held after » = `GC.GetTotalMemory(true)` after a full collection, store loaded,
  minus the value before; « peak working set » = `PeakWorkingSet64` of that process (the writing process holds the generated facts too).
- **Runs**: 3 runs at each size; figures below: median (minimum–maximum when they differ much). Tests 3, 4 and 5 once.
- **To run again** (from `C:\Plugins-ff14`, about 10 minutes; peak about 3 GB of memory):

```powershell
dotnet run -c Release --project tools/SqliteTrialProbe -- --sizes 100000,1000000 --runs 3 --kills 20 --seconds 10 --json sqlite-trial.json
```

## 1 and 2. Write, save, read, memory, size

| Measure | JSON, 100 000 | SQLite, 100 000 | JSON, 1 000 000 | SQLite, 1 000 000 |
|---|---|---|---|---|
| Full write of the whole base | 789 ms | 1 798 ms (one transaction) | 4 815 ms | 26 111 ms (25 657–43 364) |
| Save after 1 000 changed facts | 734 ms (whole files again, 16.8 MB written) | 67 ms (upsert of 1 000 rows, one transaction) | 4 055 ms (163 MB written) | 95 ms |
| The same 1 000 changes, one transaction each | — | 109 ms in all | — | 168 ms in all |
| WAL after the full write / after the 1 000-change save | — | 9.5 MB / 4.1 MB | — | 95.1 MB / 5.3 MB |
| Checkpoint of the WAL after the full write | — | 2.9 ms | — | 11.6 ms |
| Managed memory added during the save, peak | 164 MB | 18 MB | 1 521 MB | 18 MB |
| Working set added during the save, peak | 177 MB | 34 MB | 1 604 MB | 13 MB |
| Full read into a `DiscoveryStore` | 1 376 ms | 1 193 ms | 6 657 ms | 6 913 ms |
| … of which reading the files / the rows | 505 ms (`ReadBase`) + shared files read with their load (649 ms) | 759 ms | 2 200 ms + 3 019 ms with load | 4 789 ms |
| … of which `DiscoveryStore.Load` | 221 ms (yours) | 399 ms | 1 392 ms (yours) | 2 081 ms |
| Managed memory added during the read, peak | 185 MB | 97 MB | 1 506 MB | 860 MB |
| Managed memory held after the read, store loaded | 115 MB | 51 MB | 924 MB | 412 MB |
| Working set after the read | 155 MB | 141 MB | 1 150 MB | 868 MB |
| Peak working set, reading process | 193 MB | 150 MB | 1 498 MB | 1 002 MB |
| Peak working set, writing process | 379 MB | 111 MB | 2 935 MB | 539 MB |
| Facts held by the store after the read | 100 000 | 100 000 | 849 654 | 849 998 |
| Shared facts left out by the 200 000-per-kind bound | 0 | 0 | 150 346 | 150 002 |
| Size on disk | 16.7 MB (14.3 MB `Discoveries.json` + 2.4 MB gzip kind files) | 9.4 MB (9.6 MB after the changes) | 163 MB (143 MB + 20.2 MB) | 94.6 MB (94.8 MB after the changes) |
| Your file over `DiscoveryFiles.MaxBytes` (64 MB)? | no | — | **yes** (the plugin would set it aside) | — |
| One kind: MonsterPlace (kind 1) | 91 ms, shared facts only (33 975), from its kind file | 365 ms, yours and shared (68 583 rows) | 835 ms, shared only (350 346) | 2 885 ms (701 438 rows) |
| One kind: FishCatch (kind 10) | 4.2 ms, shared only (1 563) | 42 ms (3 137 rows) | 29 ms, shared only (15 720) | 115 ms (31 615 rows) |
| Counts by kind (`SELECT kind, count(*) … GROUP BY kind`), nothing loaded | — | 21 ms | — | 216 ms |

Notes on the table:

- JSON « one kind » reads only the shared facts of that kind (they have a file per kind); your facts of a kind exist only inside
  `Discoveries.json`, read whole. SQLite « one kind » reads every row of the kind, yours and shared, and builds the `DiscoveryFact`s (the JSON
  texts of the histograms are parsed per row, which is most of its time).
- The « held after » gap at equal store content (115 against 51 MB, 924 against 412 MB) is the JSON reader's pooled buffers, which stay in the
  process after reading large files; the same effect is described in [x93-load-test.md](x93-load-test.md). The store alone matches the SQLite
  figure (about 500 bytes a fact, as in x93).
- At a million facts, the shared side holds about 500 000 facts of which about 350 000 MonsterPlace: the 200 000-per-kind bound of the store
  leaves about 150 000 out in both cases (same store code).
- The SQLite full write at a million facts inserts rows in random key order into a key-ordered table: one of the three runs took 43 s.

## 3. Writer killed in the middle of a save

A child process saves in a loop; the parent kills it (`Process.Kill`) after a random 50 to 500 ms, 20 times for each storage, then reads what a
restart would read. 100 000 facts. JSON: each save is a full `WriteSplit` with every kind file rewritten (shared facts may become yours, one way,
as in play); the child writes once completely before the parent starts its clock. SQLite: each save upserts all 100 000 rows in one transaction
(so that a transaction lasts long enough to be cut). `Process.Kill` is a process stop (crash, game closed from the task manager), not a power
cut: the system's file cache is still written to disk.

| | JSON | SQLite |
|---|---|---|
| Kills | 20 | 20 |
| Kills that landed inside a save (as the parent saw it) | 20 | 20 |
| Saves finished before the kills | 0 | 0 |
| Failures (JSON: `Discoveries.json` not readable by `ReadBase`, a kind file unreadable, or a fact missing; SQLite: `PRAGMA integrity_check` not `ok`, or row count / rows read ≠ 100 000) | **0** | **0** |
| Other | 0 temporary files left; 7 664 facts found in both files over the 20 checks (the known case of the X-166 save order, left out when read) | WAL removed when the check closed the database |

## 4. A reader in another process while the game saves

A child process saves in a loop for 10 s (100 000 facts; 1 000 changed facts of yours each time). The parent reads every 50 ms: SQLite: `count(*)`
and every row of FishBite (kind 23), on one connection kept open; JSON: `Discoveries.json` read and parsed whole (`File.ReadAllText`, then the
deserializer), while the child replaces it (`AtomicFile`: temporary file, then `File.Move` over it).

| | SQLite, `Mode=ReadOnly` | SQLite, read-write connection, `busy_timeout` 2000 | JSON file |
|---|---|---|---|
| Reads done in 10 s | 85 | 88 | 30 |
| Read errors | 0 | 0 | 0 |
| Longest read | 110 ms | 92 ms | 322 ms |
| Saves done by the writer | 140 | 138 | 28 |
| **Writer errors** | 0 | 0 | **5** (`UnauthorizedAccessException`: the move over the file is refused while the reader has it open) |
| Longest save | 170 ms | 175 ms | 487 ms |

## 5. Download cache

200 entries of 50 KB to 2 MB of JSON (191 MB in all), shaped like `DataCache`'s envelope (schema, family, scope, version, date, value). Files: one
file each, SHA-256 name, written with `AtomicFile.Write` (temporary file, then move), read with `File.ReadAllBytes` and the envelope parsed.
SQLite: a second database file, one row per entry, the envelope as a BLOB; one transaction per entry (the cache writes entries one at a time).

| Measure | Files | SQLite BLOB rows |
|---|---|---|
| Write all 200 | 521 ms | 2 042 ms (+ 11 ms checkpoint) |
| Read all 200, envelope parsed | 2 492 ms | 2 942 ms |
| Size on disk | 192 MB | 192 MB |
| Replace all 200 (new sizes, 202 MB) | 595 ms | 2 652 ms |
| Size after the replacement | 202 MB | 204 MB |
| Remove half | 66 ms | 191 ms (one transaction) |
| Size after removing half | 101 MB | 204 MB without `VACUUM`; 101 MB after `VACUUM` (1 832 ms) |

## Reading

For the discoveries base, the clearest difference is the regular save: the present code writes every file again, 0.7 s and 17 MB at
100 000 facts, 4 s and 163 MB at a million, with a peak of 1.5 GB of extra memory at that size, while SQLite saves 1 000 changed facts in about
0.1 s with 18 MB of managed memory, and its file is a little over half the size. The first full write is the other way round (1.8 s against
0.8 s at 100 000, 26 s against 4.8 s at a million), and reading the whole base into the present `DiscoveryStore` costs about the same both ways
(1.2–1.4 s at 100 000, 6.7–6.9 s at a million): with the store kept in memory as it is, SQLite does not make the start faster; it lowers memory
after the read (the JSON reader's pooled buffers) and lifts the 64 MB bound of `Discoveries.json`, which a million facts pass. Reading one kind
is not faster in SQLite than the per-kind gzip files for the shared facts, but it covers yours too and needs no whole-file read. Neither storage
lost data when the writer was killed 20 times in the middle of a save. With a second process reading, SQLite in WAL mode gave no error on either
side; the JSON file gave no read error but refused 5 of the game's saves while it was being read, which matters for the desktop application.
For the download cache, files are faster to write (about 4 times) and slightly faster to read than BLOB rows, at the same size, and a database
keeps its size after removals until a `VACUUM`. These are figures on one fast machine with invented data, a trial and not a change of the plugin.
