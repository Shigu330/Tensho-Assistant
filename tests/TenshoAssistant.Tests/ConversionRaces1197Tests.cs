using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1197 (X-182; the test « Concurrent data 1169: two conversions of the discoveries at once » failed once on a file « in use »). The test
// was right: two clients converting at once could fail one another. One read Discoveries.json while the other moved it aside: the move
// failed, the database already in place, and the plugin took that for a database it could not open (nothing saved that session). A file
// held for an instant was read as damaged, and renamed « .broken » the moment it was let go: an empty base converted in its place.
// Behaviour tests (R28: data loss, concurrency), each race replayed by hand, without a clock: a handle held as the other program holds
// it, and the stop between the temporary database and its move into place.
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

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Conversion races 1197: behaviour: the other client still reads Discoveries.json when this one sets it aside: the conversion succeeds, the file stays, the next start sets it aside", () => InTemp(root =>
        {
            var main = Before(root);
            string? stayed = null;
            // The other client's read: opened as File.ReadAllText opens it (others may read, nobody may move it).
            using (var reading = new FileStream(main, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var aside = DiscoveryDatabase.ConvertFiles(root, At, (_, error) => throw error, null, (path, _) => stayed = path);
                Check(Count(root) == (1, 2), "the database is whole: " + Count(root));
                Check(File.Exists(main) && stayed == main, "the file held stays where it is, and it is said: " + stayed);
                Check(aside is not null && Directory.Exists(Path.Combine(aside, "Shared")) && !File.Exists(Path.Combine(aside, "Discoveries.json")), "the shared files, which nobody held, are aside");
            }
            var written = 0;
            var next = DiscoveryDatabase.ConvertFiles(root, At.AddMinutes(5), (_, error) => throw error, rows => written += rows);
            Check(next is not null && !File.Exists(main) && Aside(root).Count(path => Path.GetFileName(path) == "Discoveries.json") == 1, "set aside by the next start, once");
            Check(written == 0 && Count(root) == (1, 2), "nothing counted twice: " + Count(root));
            Check(Temporaries(root).Length == 0, "no temporary file left");
        })),
        ("Conversion races 1197: behaviour: the other client converts between this one's reading and its move into place: the first base in stays, nothing fails, nothing is left behind", () => InTemp(root =>
        {
            var main = Before(root);
            string? other = null;
            // The lost race, replayed whole at the stop: the other client reads a base one fact richer, puts its database in place and moves the
            // old files aside; this client then comes with what it had read before.
            var aside = DiscoveryDatabase.ConvertFiles(root, At, (_, error) => throw error, null, null, _ =>
            {
                Before(root, 1);
                other = DiscoveryDatabase.ConvertFiles(root, At, (_, error) => throw error);
            });
            Check(other is not null && aside is null, "the other client set the files of before aside; this one had nothing left to move");
            Check(Count(root) == (2, 2), "the first base in stays, never replaced by the later one: " + Count(root));
            Check(Temporaries(root).Length == 0, "this client's temporary database is removed");
            Check(!File.Exists(main) && Aside(root).Count(path => Path.GetFileName(path) == "Discoveries.json") == 1 && Directory.GetDirectories(root, "before-sqlite-*").Length == 1, "the files of before aside once, no empty folder beside");
        })),
        ("Conversion races 1197: behaviour: Discoveries.json held by another program for an instant is not a damaged file: not reported, not renamed, nothing converted; whole at the next start", () => InTemp(root =>
        {
            var main = Before(root);
            var reported = 0;
            FileStream? holder = new FileStream(main, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            try
            {
                var failed = false;
                // The report of a damaged file is where the holder lets go. Before 0.1197 the file was reported, then renamed « .broken » the
                // instant after, and the conversion went on from an empty base: the player's own discoveries out of the database.
                try { DiscoveryDatabase.ConvertFiles(root, At, (_, _) => { reported++; holder?.Dispose(); holder = null; }); }
                catch (IOException) { failed = true; }
                Check(failed && reported == 0, "held is not damaged: nothing reported, the conversion waits for the next start (reported " + reported + ")");
                Check(File.Exists(main) && Directory.GetFiles(root, "Discoveries.json.broken*").Length == 0, "the file as it was, under its name");
                Check(!File.Exists(Path.Combine(root, DiscoveryDatabase.FileName)) && Temporaries(root).Length == 0 && Directory.GetDirectories(root, "before-sqlite-*").Length == 0, "no database from a part of the base, nothing moved");
            }
            finally { holder?.Dispose(); }
            Check(DiscoveryDatabase.ConvertFiles(root, At.AddMinutes(1), (_, error) => throw error) is not null && Count(root) == (1, 2), "converted whole once the file is free: " + Count(root));
        })),
        ("Conversion races 1197: behaviour: a shared kind file held for an instant: nothing converted from a part of the base, the file neither renamed nor lost", () => InTemp(root =>
        {
            var main = Before(root);
            var kind = Kind(main);
            var reported = 0;
            using (var holder = new FileStream(kind, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var failed = false;
                try { DiscoveryDatabase.ConvertFiles(root, At, (_, _) => reported++); }
                catch (IOException) { failed = true; }
                Check(failed && reported == 0, "nothing converted, nothing reported as damaged");
                Check(File.Exists(kind) && !File.Exists(kind + ".broken") && File.Exists(main) && !File.Exists(Path.Combine(root, DiscoveryDatabase.FileName)), "the base of before as it was, no database without its shared facts");
            }
            Check(DiscoveryDatabase.ConvertFiles(root, At.AddMinutes(1), (_, error) => throw error) is not null && Count(root) == (1, 2), "converted whole once the file is free: " + Count(root));
        })),
        ("Conversion races 1197: behaviour: the temporary database held when it is put in place: no database, the files of before untouched; the next start converts and removes the leftover once old", () => InTemp(root =>
        {
            var main = Before(root);
            FileStream? holder = null;
            var failed = false;
            try { DiscoveryDatabase.ConvertFiles(root, At, (_, error) => throw error, null, null, temporary => holder = new FileStream(temporary, FileMode.Open, FileAccess.Read, FileShare.Read)); }
            catch (IOException) { failed = true; }
            finally { holder?.Dispose(); }
            Check(failed && !File.Exists(Path.Combine(root, DiscoveryDatabase.FileName)), "given up after its attempts: no database");
            Check(File.Exists(main) && File.Exists(Kind(main)) && Directory.GetDirectories(root, "before-sqlite-*").Length == 0, "the files of before whole and in place");
            var leftover = Temporaries(root);
            Check(leftover.Length == 1, "the temporary database, held, could not be removed: " + leftover.Length);
            // Half an hour later, at a later start (a younger leftover may be another client's conversion under way: left alone).
            Check(DiscoveryDatabase.ConvertFiles(root, At.AddMinutes(1), (_, error) => throw error) is not null && Count(root) == (1, 2) && Temporaries(root).Length == 1, "converted; a young leftover is left alone");
            File.SetLastWriteTimeUtc(leftover[0], DateTime.UtcNow.AddMinutes(-31));
            Check(DiscoveryDatabase.ConvertFiles(root, At.AddMinutes(40), (_, error) => throw error) is null && Temporaries(root).Length == 0, "removed once old, even with nothing left to convert");
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
        ("Conversion races 1197: wiring: the plugin says a file that stays, and a failure once the database is in place can no longer come from the files of before", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs")); // X-153 (0.1250): the discoveries family
            Check(plugin.Contains("is held by another program for now and stays where it is; the next start sets it aside", StringComparison.Ordinal), "the file that stays is said in the log");
            var database = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/DiscoveryDatabase.cs")).ReplaceLineEndings("\n");
            var convert = database[database.IndexOf("public static string? ConvertFiles(", StringComparison.Ordinal)..database.IndexOf("private static string Free(", StringComparison.Ordinal)];
            Check(convert.Contains("AtomicFile.MoveWhenFree(temporary,target);", StringComparison.Ordinal) && convert.Contains("AtomicFile.DeleteWhenFree(mine);", StringComparison.Ordinal), "the temporary database: moved when free, removed without an error");
            Check(!convert.Contains("File.Delete(mine)", StringComparison.Ordinal) && convert.Split("catch(Exception error) when(error is IOException or UnauthorizedAccessException)").Length == 3, "both moves aside are tolerant");
            Check(convert.IndexOf("Directory.EnumerateFiles(folder,FileName+\".converting*\")", StringComparison.Ordinal) < convert.IndexOf("if(!File.Exists(target) && old)", StringComparison.Ordinal), "leftovers looked for at every start");
        }),
    ];
}
