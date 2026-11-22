using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1197 (X-182; the test « Concurrent data 1169: two conversions of the discoveries at once » failed once on a file « in use »). The test
// was right: two clients converting at once could fail one another. One read Discoveries.json while the other moved it aside: the move
// failed, the database already in place, and the plugin took that for a database it could not open (nothing saved that session). A file
// held for an instant was read as damaged, and renamed « .broken » the moment it was let go: an empty base converted in its place.
// Behaviour tests (R28: data loss, concurrency), each race replayed by hand, without a clock: a handle held as the other program holds
// it, and the stop between the temporary database and its move into place. X-574 (0.1793, the user's choice of 17/11): the conversion is gone;
// the races of the discoveries' start are now those of a database opened beside files of before that are never read, moved nor removed, even
// held by another program; the races of AtomicFile and of the JSON mode's base stay as they were.
internal static class ConversionRaces1197Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1197-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static DiscoveryFact Fact(uint subject) => new() { Kind = DiscoveryKind.FishCatch, Subject = subject, Detail = 1, Territory = 135, Map = 16,
        CellX = DiscoveryStore.Cell(120, -40).X, CellZ = DiscoveryStore.Cell(120, -40).Z, X = 120, Z = -40, LevelMin = 1, LevelMax = 1, Seen = 1, First = At.AddDays(-3), Last = At.AddDays(-2) };

    // The base of before 0.1136: one fact of the player's own in Discoveries.json (« extra » more when asked), two shared ones in Shared/.
    private static string Before(string root, uint extra = 0)
    {
        var main = Path.Combine(root, "Discoveries.json");
        var store = new DiscoveryStore();
        store.Merge([Fact(4869), Fact(4870)]);
        for (uint subject = 7; subject <= 7 + extra; subject++) store.SeeSource(DiscoveryKind.Desynthesis, subject, 1, 1, At);
        DiscoveryFiles.WriteSplit(main, store.Snapshot(At), []);
        return main;
    }
    private static (long Mine, long Shared) Count(string root) => new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName)).Count();
    private static string[] Temporaries(string root) => Directory.GetFiles(root, DiscoveryDatabase.FileName + ".converting*");
    private static string[] Aside(string root) => Directory.GetDirectories(root, "before-sqlite-*").SelectMany(Directory.GetFiles).ToArray();
    private static string Kind(string main) => DiscoveryFiles.SharedPath(DiscoveryFiles.SharedFolder(main), DiscoveryKind.FishCatch);
    // The start of the discoveries as the plugin does it (Plugin.Discoveries.cs): the database opened (made when missing), the files of before named.
    private static (DiscoveryDatabase Database, string[] Old) Start(string root)
        => (new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName)), DiscoveryFiles.OldFiles(Path.Combine(root, "Discoveries.json")));
    private static (string Name, byte[] Bytes)[] Files(string root) => Directory.GetFiles(root, "*", SearchOption.AllDirectories)
        .Where(path => !Path.GetFileName(path).StartsWith(DiscoveryDatabase.FileName, StringComparison.Ordinal)).Order(StringComparer.Ordinal).Select(path => (path, File.ReadAllBytes(path))).ToArray();
    private static bool Same((string Name, byte[] Bytes)[] left, (string Name, byte[] Bytes)[] right)
        => left.Length == right.Length && left.Zip(right).All(pair => pair.First.Name == pair.Second.Name && pair.First.Bytes.SequenceEqual(pair.Second.Bytes));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Conversion races 1197: behaviour: the other client still reads Discoveries.json while this one starts: the database opens, the file stays as it is (X-574)", () => InTemp(root =>
        {
            var main = Before(root);
            var before = Files(root);
            using (var reading = new FileStream(main, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var (database, old) = Start(root);
                Check(database.Count() == (0, 0), "the database opens, nothing read from the files of before: " + database.Count());
                Check(old.SequenceEqual(["Discoveries.json", "Shared/"]), "both named as left aside: " + string.Join(", ", old));
            }
            Check(Same(Files(root), before) && Directory.GetDirectories(root, "before-sqlite-*").Length == 0, "every file of before in place, unchanged, nothing set aside");
            Check(Temporaries(root).Length == 0, "no temporary file");
        })),
        ("Conversion races 1197: behaviour: two clients starting together beside the files of before: one database, both clients' facts, nothing moved (X-574)", () => InTemp(root =>
        {
            var main = Before(root);
            var before = Files(root);
            var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            Parallel.For(0, 2, side => { try { Start(root).Database.Write([Fact(9000 + (uint)side)], [], false); } catch (Exception error) { errors.Add(error); } });
            Check(errors.IsEmpty, "no start failed: " + string.Join("; ", errors.Select(error => error.Message)));
            Check(Count(root) == (2, 0), "both clients' facts, none of the files of before: " + Count(root));
            Check(Same(Files(root), before) && File.Exists(main) && Directory.GetDirectories(root, "before-sqlite-*").Length == 0, "the files of before as they were");
        })),
        ("Conversion races 1197: behaviour: Discoveries.json held by another program (no sharing) is neither reported, renamed nor in the way: the database opens (X-574)", () => InTemp(root =>
        {
            var main = Before(root);
            using (var holder = new FileStream(main, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var (database, old) = Start(root);
                Check(database.Count() == (0, 0) && old.Contains("Discoveries.json"), "the database opens; the held file is still named");
                database.Write([Fact(9100)], [], false);
            }
            Check(File.Exists(main) && Directory.GetFiles(root, "Discoveries.json.broken*").Length == 0 && Count(root) == (1, 0), "the file under its name, the fact saved");
        })),
        ("Conversion races 1197: behaviour: a shared kind file held for an instant: the database opens, the file neither renamed nor lost (X-574)", () => InTemp(root =>
        {
            var main = Before(root);
            var kind = Kind(main);
            using (var holder = new FileStream(kind, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var (database, old) = Start(root);
                Check(database.Count() == (0, 0) && old.Contains("Shared/"), "the database opens; the folder of the shared files named");
            }
            Check(File.Exists(kind) && !File.Exists(kind + ".broken") && File.Exists(main), "the files of before as they were");
        })),
        ("Conversion races 1197: behaviour: a temporary database left by a conversion of before is never read nor removed (X-574)", () => InTemp(root =>
        {
            Before(root);
            var leftover = Path.Combine(root, DiscoveryDatabase.FileName + ".converting-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(leftover, "left by a stopped conversion");
            File.SetLastWriteTimeUtc(leftover, DateTime.UtcNow.AddHours(-2));
            var (database, _) = Start(root);
            Check(database.Count() == (0, 0), "nothing read from it");
            Check(Temporaries(root).SequenceEqual([leftover]) && File.ReadAllText(leftover) == "left by a stopped conversion", "left where it is, even old");
        })),
        ("Conversion races 1197: behaviour: a move tried again while the file is held, done once it is let go; a destination that exists is neither waited for nor replaced; a removal never fails", () => InTemp(root =>
        {
            var from = Path.Combine(root, "a.tmp"); var to = Path.Combine(root, "a.db");
            File.WriteAllText(from, "x");
            FileStream? holder = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.Read);
            var waits = new List<int>();
            try { AtomicFile.MoveWhenFree(from, to, 6, attempt => { waits.Add(attempt); if (attempt == 2) { holder?.Dispose(); holder = null; } }); }
            finally { holder?.Dispose(); }
            Check(File.Exists(to) && !File.Exists(from) && waits.SequenceEqual([1, 2]), "moved at the third attempt, the file let go after the second: " + string.Join(",", waits));
            File.WriteAllText(from, "y");
            using (var held = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var tried = 0; var failed = false;
                try { AtomicFile.MoveWhenFree(from, Path.Combine(root, "b.db"), 4, _ => tried++); } catch (IOException) { failed = true; }
                Check(failed && tried == 3 && File.Exists(from), "never let go: given up after four attempts, with the error: " + tried);
                Check(!AtomicFile.DeleteWhenFree(from, 3, _ => tried++) && tried == 5 && File.Exists(from), "a removal that cannot be done says so and does not fail");
            }
            var waited = 0; var refused = false;
            try { AtomicFile.MoveWhenFree(from, to, 6, _ => waited++); } catch (IOException) { refused = true; }
            Check(refused && waited == 0 && File.ReadAllText(to) == "x" && File.Exists(from), "a destination that exists: refused at once, never replaced");
            Check(AtomicFile.DeleteWhenFree(from) && !File.Exists(from) && AtomicFile.DeleteWhenFree(from), "removed; a file that is not there is fine");
        })),
        ("Conversion races 1197: behaviour: a base gone while it is read is not there, a held one is left alone and blocks the session's saves, a damaged one alone is set aside", () => InTemp(root =>
        {
            var main = Before(root);
            var text = File.ReadAllText(main);
            var reported = 0;
            using (var held = new FileStream(main, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var read = DiscoveryFiles.ReadBase(main, At, (_, _) => reported++);
                Check(read.File is null && read.SetAside is null && read.Unsafe && reported == 0, "held: nothing read, nothing set aside, nothing written this session");
            }
            Check(File.ReadAllText(main) == text && DiscoveryFiles.ReadBase(main, At, (_, _) => reported++).File is not null && reported == 0, "let go: read as it was");
            Check(DiscoveryFiles.Away(new FileNotFoundException()) && DiscoveryFiles.Away(new DirectoryNotFoundException()) && !DiscoveryFiles.Away(new IOException()), "gone");
            Check(DiscoveryFiles.Busy(new UnauthorizedAccessException()) && !DiscoveryFiles.Busy(new InvalidDataException()) && !DiscoveryFiles.Busy(new EndOfStreamException()) && !DiscoveryFiles.Busy(new System.Text.Json.JsonException()), "held is about access; a cut or damaged file is not « held »");
            File.WriteAllText(main, "{\"Version\":2,\"Facts\":[");
            var damaged = DiscoveryFiles.ReadBase(main, At, (_, _) => reported++);
            Check(damaged.File is null && !damaged.Unsafe && damaged.SetAside is not null && reported == 1 && !File.Exists(main), "damaged: reported and set aside, as before");
            // The shared kinds: one held is told and left out, nothing renamed; a folder gone is an empty list.
            var kind = Kind(main); var busy = 0;
            using (var held = new FileStream(kind, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Check(!DiscoveryFiles.ReadShared(main, (_, _) => reported++, (_, _) => busy++).Any() && busy == 1 && reported == 1 && !File.Exists(kind + ".broken"), "a held kind: told, left as it is");
            Check(DiscoveryFiles.ReadShared(main, (_, _) => reported++).Single().Facts.Count == 2 && reported == 1, "let go: read");
            Directory.Delete(DiscoveryFiles.SharedFolder(main), true);
            Check(!DiscoveryFiles.ReadShared(main, (_, _) => reported++).Any() && reported == 1, "no folder: nothing, no report");
        })),
        ("Conversion races 1197: wiring: the plugin opens the database without converting and says the files of before once; the JSON base read only without a database (X-574)", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs")).ReplaceLineEndings("\n"); // X-153 (0.1250): the discoveries family
            Check(!plugin.Contains("ConvertFiles(", StringComparison.Ordinal) && plugin.Contains("database=new Discoveries.DiscoveryDatabase(databasePath){Log=message=>log.Warning(message)};", StringComparison.Ordinal), "the database opened, nothing converted");
            Check(plugin.Contains("if(Discoveries.DiscoveryFiles.OldFiles(saved) is { Length:>0 } old)", StringComparison.Ordinal)
                && plugin.Contains("\"Discoveries: a file of an old format ({0}) was left aside, not read (X-574).\"", StringComparison.Ordinal), "the files of before said in the log");
            Check(plugin.IndexOf("mainWindow.DiscoveryDatabase=database;", StringComparison.Ordinal) < plugin.IndexOf("var read=Discoveries.DiscoveryFiles.ReadBase(saved,", StringComparison.Ordinal)
                && plugin.Contains("if(database is { } opened)", StringComparison.Ordinal), "the JSON base read only when no database could be made (its own mode)");
            var database = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/DiscoveryDatabase.cs"));
            Check(!database.Contains("MergeFound", StringComparison.Ordinal) && !database.Contains("before-sqlite", StringComparison.Ordinal), "no merge, nothing set aside");
        }),
    ];
}
