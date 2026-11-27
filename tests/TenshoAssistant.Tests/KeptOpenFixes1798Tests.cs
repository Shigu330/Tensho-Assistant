using System.Numerics;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Inventory;

// 0.1798 (X-785, review of 0.1795, minor findings M2 and M3 of the kept connections of X-750). M3: the data file's save was timed at each load,
// writing a row and forcing a full checkpoint; with two clients on one profile (X-141) that checkpoint waits for the other's readers and holds
// its saves: now timed once per file. M2: the discoveries database kept open by the load before it was given to the window, so an unload in
// between left the connection open; and a failure to keep it open counted as a base that cannot be opened. Behaviour tests on real files
// (R28.14: two clients, an unload during the load), and the load's wiring.
internal static class KeptOpenFixes1798Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset At = new(2026,11,22,13,0,0,TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-1798-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static List<GilEntry> Gil(int count,long start)
        => Enumerable.Range(0,count).Select(index => new GilEntry(At.AddMinutes(index),100 + index,start + index,GilSource.Character,(GilCategory)(index % 9))).ToList();

    private static long Journal(string database) => File.Exists(database + "-wal") ? new FileInfo(database + "-wal").Length : 0;

    // Whether no handle is left on the file: a SQLite handle on Windows does not share deletion, so the move fails while one is open.
    private static bool Released(string file)
    {
        var moved = file + ".moved";
        try { File.Move(file,moved); File.Move(moved,file); return true; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Kept open fixes 1798: behaviour (M3): the save timed at the file's first load only; a second client on the same profile no longer forces a checkpoint at its load",() => InTemp(root =>
        {
            var file = Path.Combine(root,TenshoDataDatabase.FileName);
            var lines = new List<string>();
            var first = new TenshoDataFile(file,(error,message) => throw new Exception(message,error),line => { lock (lines) lines.Add(line); });
            first.KeepOpen();
            Check(lines.Count(line => line.StartsWith("Tensho data: a save took ",StringComparison.Ordinal)) == 1,"timed at the file's first load: " + string.Join(" | ",lines));
            var saved = Gil(30,5_000);
            new GilJournalStore(Path.Combine(root,"GilJournal"),first).Save(4,saved);
            Check(Journal(file) > 0,"the first client's save left in the journal (kept open)");
            lines.Clear();
            var second = new TenshoDataFile(file,(error,message) => throw new Exception(message,error),line => { lock (lines) lines.Add(line); });
            second.KeepOpen();
            Check(second.Database()!.Kept,"the second client keeps the file open too");
            Check(!lines.Any(line => line.StartsWith("Tensho data: a save took ",StringComparison.Ordinal)),"not timed again: " + string.Join(" | ",lines));
            Check(Journal(file) > 0,"no checkpoint forced by the second client's load (it emptied the journal before)");
            second.Close();
            first.Close();
            var reloaded = new GilJournalStore(Path.Combine(root,"GilJournal"),new TenshoDataFile(file,(error,message) => throw new Exception(message,error)));
            Check(reloaded.Load(4).Count == saved.Count,"the save whole after both unloads");
            lines.Clear();
            var third = new TenshoDataFile(file,(error,message) => throw new Exception(message,error),line => { lock (lines) lines.Add(line); });
            third.KeepOpen();
            Check(!lines.Any(line => line.StartsWith("Tensho data: a save took ",StringComparison.Ordinal)) && third.Database()!.Kept,"a later load: kept open, not timed: " + string.Join(" | ",lines));
            third.Close();
        })),
        ("Kept open fixes 1798: behaviour (M2): an unload during the discoveries' load, before or after the base is given to the window, leaves no connection open",() => InTemp(root =>
        {
            // The load's steps (Plugin.Discoveries) and the unload's (the token cancelled, then MainWindow.FlushDiscoveries closing the window's base).
            DiscoveryDatabase? window = null;
            var unloading = new CancellationTokenSource();   // a new one per scenario (a cancelled source cannot be reset)
            void Load(DiscoveryDatabase database)
            {
                window = database;
                database.KeepOpen();
                if (unloading.IsCancellationRequested) database.Close();
            }
            void Unload() { unloading.Cancel(); window?.Checkpoint(); window?.Close(); }

            var before = Path.Combine(root,"before",DiscoveryDatabase.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(before)!);
            var early = new DiscoveryDatabase(before);
            Unload();   // the unload's flush finds no base yet
            Load(early);
            Check(!early.KeepOpen(),"closed by the load itself: never kept again");
            Check(Released(before),"no handle left on the file after an unload that came before the base was given to the window");

            var after = Path.Combine(root,"after",DiscoveryDatabase.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(after)!);
            unloading = new CancellationTokenSource();
            window = null;
            var late = new DiscoveryDatabase(after);
            window = late;   // given to the window
            Unload();        // the flush closes it before the load keeps it
            late.KeepOpen();
            Check(Released(after),"no handle left after an unload that came between giving the base and keeping it");

            // The ordinary load: kept, and the handle really held until the unload.
            var usual = Path.Combine(root,"usual",DiscoveryDatabase.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(usual)!);
            unloading = new CancellationTokenSource();
            window = null;
            var normal = new DiscoveryDatabase(usual);
            Load(normal);
            Check(!Released(usual),"kept open while the plugin runs (the check above can see a handle)");
            Unload();
            Check(Released(usual),"released at unload");
        })),
        ("Kept open fixes 1798: wiring (M2): the base given to the window first, kept open in its own try, closed again when the unload came meanwhile",() =>
        {
            var load = Read("src/TenshoAssistant/Plugin.Discoveries.cs");
            var created = load.IndexOf("log.Error(error,\"The discoveries database could not be created: the JSON files stay in use this session.\");",StringComparison.Ordinal);
            var given = load.IndexOf("mainWindow.DiscoveryDatabase=database;",StringComparison.Ordinal);
            var kept = load.IndexOf("try{database.KeepOpen();}\n                catch(Exception error){log.Warning(error,\"The discoveries database could not be kept open; each save opens it again (X-750).\");}",StringComparison.Ordinal);
            var closed = load.IndexOf("if(gameDataCancel.IsCancellationRequested)database.Close();",StringComparison.Ordinal);
            Check(created > 0 && given > created && kept > given && closed > kept,"open, give to the window, keep in its own try, close if unloading");
            Check(load.Split("database.KeepOpen();").Length == 2,"kept open in one place only");
            var file = Read("src/TenshoAssistant/TenshoDataDatabase.cs");
            Check(file.Contains("if(opened.SavesTimed())return;\n            var (folded,kept)=opened.SaveTimes();",StringComparison.Ordinal),"(M3) the save timed only when not timed yet");
        }),
    ];
}
