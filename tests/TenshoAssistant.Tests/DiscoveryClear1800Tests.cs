using System.IO.Compression;
using System.Numerics;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1800 (X-793 M2 and X-794 M1, review of 0.1799). M2: a game killed or crashed during the copy taken before « empty the base » left its
// passing files in the Backups folder for good (SQLite's snapshot of the discoveries, a whole copy of the base, and a zip half written:
// DeleteOnClose and the finally never run when the process dies; the cleaning took only finished copies). Behaviour tests on the files as a
// killed copy leaves them (R28.14): removed at the next copy once an hour old, never while held open, nothing else touched. M1: the copy was
// made in Draw, on the game's thread (VACUUM INTO of the whole base, then the zip: seconds with a big base); it runs on a worker now and the
// base is emptied only once it ended well.
internal static class DiscoveryClear1800Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset At = new(2026,11,22,12,0,0,TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-1800-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static IReadOnlyCollection<DiscoveryFact> Facts(int sightings,uint item)
    {
        var store = new DiscoveryStore();
        for (var sighting = 0; sighting < sightings; sighting++)
            store.SeeSource(DiscoveryKind.FishCatch,item,1,3 + sighting,At.AddMinutes(sighting),135,16,new Vector3(10,0,20));
        return store.Snapshot(At).Facts;
    }

    private static string Made(string folder,string name,DateTime written,string content = "x")
    {
        var path = Path.Combine(folder,name);
        File.WriteAllText(path,content);
        File.SetLastWriteTimeUtc(path,written);
        return path;
    }

    private static string Guid32 => Guid.NewGuid().ToString("N");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery clear 1800: behaviour: the passing files of a copy cut short (game killed) are removed by the next copy once an hour old; younger ones, other files and finished copies kept",() => InTemp(root =>
        {
            var folder = Path.Combine(root,"Discoveries");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"Discoveries.json"),"{}");
            var backups = DiscoveryBackup.Folder(folder);
            Check(backups == Path.Combine(root,"Backups"),"the Backups folder beside Discoveries: " + backups);
            Directory.CreateDirectory(backups);
            var old = DateTime.UtcNow.AddHours(-2);
            var snapshot = Made(backups,"snapshot-" + Guid32 + ".db.tmp",old,"a whole copy of the base");
            var halfZip = Made(backups,"backup-20261122-110000-discoveries.zip." + Guid32 + ".tmp",old);
            var young = Made(backups,"snapshot-" + Guid32 + ".db.tmp",DateTime.UtcNow.AddMinutes(-5));
            var others = new[] { Made(backups,"notes.tmp",old),Made(backups,"snapshot-mine.db.tmp",old),Made(backups,"backup-20261122-110000-cache.zip",old) };
            var copy = DiscoveryBackup.Create(folder,null,At);
            Check(copy is not null && File.Exists(copy),"the copy made: " + copy);
            Check(!File.Exists(snapshot) && !File.Exists(halfZip),"the snapshot and the half-written zip of the copy cut short removed (before 0.1800: kept for good)");
            Check(File.Exists(young),"a younger one kept (a copy maybe still running)");
            Check(others.All(File.Exists),"other files and the finished copies kept");
            Check(BackupZip.Sweep(backups,DateTime.UtcNow.AddHours(2)) == 1 && !File.Exists(young),"the younger one too, an hour later");
            Check(Directory.GetFiles(backups).Length == 4,"the copy and the three others left: " + string.Join(", ",Directory.GetFiles(backups).Select(Path.GetFileName)));
        })),
        ("Discovery clear 1800: behaviour: a passing file held open by a copy still running (or read-only) is never removed, whatever its age; removed once let go",() => InTemp(root =>
        {
            var backups = Path.Combine(root,"Backups");
            Directory.CreateDirectory(backups);
            var old = DateTime.UtcNow.AddHours(-3);
            var held = Made(backups,"snapshot-" + Guid32 + ".db.tmp",old);
            using (new FileStream(held,FileMode.Open,FileAccess.ReadWrite,FileShare.Read | FileShare.Write))   // as SQLite holds the file it writes
                Check(BackupZip.Sweep(backups,DateTime.UtcNow) == 0 && File.Exists(held),"held open: left, no exception");
            var locked = Made(backups,"backup-20261122-090000-cache.zip." + Guid32 + ".tmp",old);
            File.SetAttributes(locked,FileAttributes.ReadOnly);
            try
            {
                Check(BackupZip.Sweep(backups,DateTime.UtcNow) == 1 && !File.Exists(held) && File.Exists(locked),"let go: removed; read-only: left, no exception");
            }
            finally { File.SetAttributes(locked,FileAttributes.Normal); }
            Check(BackupZip.Sweep(Path.Combine(root,"missing"),DateTime.UtcNow) == 0,"no Backups folder: nothing");
        })),
        ("Discovery clear 1800: the names: SQLite's snapshot and a zip being written, as the copies make them, and nothing else",() => InTemp(root =>
        {
            var guid = Guid32;
            Check(BackupZip.Leftover("snapshot-" + guid + ".db.tmp") && BackupZip.Leftover("backup-20261122-120000-discoveries.zip." + guid + ".tmp")
                && BackupZip.Leftover("backup-20261122-120000-2-cache.zip." + guid + ".tmp"),"the passing files");
            Check(!BackupZip.Leftover("backup-20261122-120000-discoveries.zip") && !BackupZip.Leftover("notes.tmp") && !BackupZip.Leftover("snapshot-mine.db.tmp")
                && !BackupZip.Leftover("snapshot-" + guid + ".db") && !BackupZip.Leftover(DiscoveryDatabase.FileName) && !BackupZip.Leftover("backup-x.zip." + guid + ".tmp"),
                "nothing else");
            var folder = Path.Combine(root,"Discoveries");
            Directory.CreateDirectory(folder);
            var backups = DiscoveryBackup.Folder(folder);
            var database = new DiscoveryDatabase(Path.Combine(folder,DiscoveryDatabase.FileName));
            try
            {
                database.Write(Facts(2,4869),[],false);
                using (database.Snapshot(backups))
                {
                    var names = Directory.GetFiles(backups).Select(Path.GetFileName).ToArray();
                    Check(names.Length == 1 && BackupZip.Leftover(names[0]!),"the snapshot SQLite writes: " + string.Join(", ",names));
                }
            }
            finally { database.Close(); }
            string[] passing = [];
            BackupZip.Create(folder,backups,"names",At,null,file => { passing = Directory.GetFiles(backups).Select(name => Path.GetFileName(name)!).ToArray(); return null; });
            Check(passing.Length == 1 && BackupZip.Leftover(passing[0]),"the zip being written: " + string.Join(", ",passing));
        })),
        ("Discovery clear 1800: behaviour: the copy runs on a worker; ended well (every fact) the base is emptied; failed, nothing is removed; still running, nothing yet",() => InTemp(root =>
        {
            var folder = Path.Combine(root,"Discoveries");
            Directory.CreateDirectory(folder);
            var database = new DiscoveryDatabase(Path.Combine(folder,DiscoveryDatabase.FileName));
            Task<string?> copy;
            try
            {
                Check(database.KeepOpen(),"kept open, as the plugin does");
                var facts = Facts(4,4869);
                database.Write(facts,[],false);
                copy = DiscoveryBackup.Start(folder,database,At);
                Check(SpinWait.SpinUntil(() => copy.IsCompleted,60_000),"the copy ended");
                Check(DiscoveryBackup.Outcome(copy) == (true,true,null),"ended well: the base is emptied");
                using var zip = ZipFile.OpenRead(copy.Result!);
                var restored = Path.Combine(root,"restored.db");
                zip.GetEntry(DiscoveryDatabase.FileName)!.ExtractToFile(restored);
                Check(new DiscoveryDatabase(restored).ReadMine().Facts.Count == facts.Count,"every fact in the copy");
            }
            finally { database.Close(); }
            var other = Path.Combine(root,"Other");
            Directory.CreateDirectory(other);
            var heldPath = Path.Combine(other,"held.json");
            using (var held = new FileStream(heldPath,FileMode.Create,FileAccess.ReadWrite,FileShare.None))
            {
                held.Write("{}"u8);
                var failed = DiscoveryBackup.Start(other,null,At);
                Check(SpinWait.SpinUntil(() => failed.IsCompleted,60_000),"the failed copy ended");
                var (ended,empty,error) = DiscoveryBackup.Outcome(failed);
                Check(ended && !empty && error is IOException,"failed: nothing removed, the error for the log: " + error);
            }
            Check(File.Exists(heldPath),"the files left as they were");
            Check(!Directory.GetFiles(Path.Combine(root,"Backups")).Any(path => path.EndsWith(".tmp",StringComparison.Ordinal)),"no passing file left by the failed copy");
            var pending = new TaskCompletionSource<string?>();
            Check(DiscoveryBackup.Outcome(pending.Task) == (false,false,null),"still running: nothing yet");
            pending.SetCanceled();
            var cancelled = DiscoveryBackup.Outcome(pending.Task);
            Check(cancelled.Ended && !cancelled.Empty && cancelled.Error is OperationCanceledException,"cancelled: nothing removed");
        })),
        ("Discovery clear 1800: wiring: the button starts the copy on a worker and empties nothing; the base emptied once a second on the game's thread; Backups swept at load and before each copy",() =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.Discoveries.cs");
            var draw = window[window.IndexOf("private void DrawDiscoveryDebug(",StringComparison.Ordinal)..window.IndexOf("private void EndDiscoveryClear(",StringComparison.Ordinal)];
            Check(draw.Contains("discoveryClearCopy=DiscoveryBackup.Start(folder,DiscoveryDatabase,DateTimeOffset.UtcNow);",StringComparison.Ordinal)
                && !draw.Contains("store.Clear()",StringComparison.Ordinal) && !draw.Contains("DiscoveryBackup.Create(",StringComparison.Ordinal),"the button starts the copy, empties nothing");
            Check(draw.Contains("if(discoveryClearCopy is not null){ImGui.TextDisabled(localization.Get(\"DiscoveriesUi.ClearCopying\"));return;}",StringComparison.Ordinal),"while it runs, only that is said");
            Check(window.Contains("discoveriesAt=now;\n        EndDiscoveryClear(store);",StringComparison.Ordinal),"ended once a second, with the page closed too (CollectDiscoveries)");
            Check(Read("src/TenshoAssistant/Discoveries/DiscoveryBackup.cs").Contains("=>Task.Run(()=>Create(folder,database,at));",StringComparison.Ordinal),"the copy on a worker");
            Check(Read("src/TenshoAssistant/Plugin.Discoveries.cs").Contains("var removed=BackupZip.Sweep(Discoveries.DiscoveryBackup.Folder(discoveryFolder),DateTime.UtcNow);",StringComparison.Ordinal),"swept at load");
            var zip = Read("src/TenshoAssistant/BackupZip.cs");
            var sweep = zip.IndexOf("Sweep(backups,DateTime.UtcNow);",StringComparison.Ordinal);
            Check(sweep > 0 && sweep < zip.IndexOf("var temporary=",StringComparison.Ordinal),"swept before each copy");
            foreach (var language in new[] { "en","fr" })
            {
                var text = Read("src/TenshoAssistant/Localization/Languages/" + language + ".json");
                Check(text.Contains("\"ClearCopying\":",StringComparison.Ordinal) && text.Contains("\"ClearFailed\":",StringComparison.Ordinal),"the page's two lines in " + language);
            }
        }),
    ];
}
