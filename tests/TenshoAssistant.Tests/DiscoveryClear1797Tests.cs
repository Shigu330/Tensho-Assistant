using System.IO.Compression;
using System.Numerics;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1797 (X-784, review of 0.1795, I1): the developer mode's « empty the discoveries base » failed every time since 0.1794: the zip copy taken
// first read Discoveries.db as a file (ZipArchive.CreateEntryFromFile, FileShare.Read) while the plugin keeps it open for writing all session
// (SqliteKeeper), so « nothing was removed ». Behaviour tests on a real base kept open as the plugin does (R28.14): the copy is made, holds
// every fact (the journal's last saves included) and opens as a base; the tokens and the journal files stay out; no snapshot left behind.
internal static class DiscoveryClear1797Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset At = new(2026,11,22,12,0,0,TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-1797-" + Guid.NewGuid().ToString("N"));
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

    // The base as the plugin has it: created, kept open (Plugin.Discoveries), saved twice after (the journal holds the last saves).
    private static (DiscoveryDatabase Database,string Folder,int Count) KeptBase(string root)
    {
        var folder = Path.Combine(root,"Discoveries");
        Directory.CreateDirectory(folder);
        var database = new DiscoveryDatabase(Path.Combine(folder,DiscoveryDatabase.FileName));
        Check(database.KeepOpen(),"kept open, as the plugin does");
        var first = Facts(4,4869);
        var second = Facts(3,4870);
        database.Write(first,[],false);
        database.Write(second,[],false);
        Check(new FileInfo(database.Path + "-wal").Length > 0,"the journal holds the last saves (kept open: not folded in)");
        File.WriteAllText(Path.Combine(folder,"github.token"),"token");
        File.WriteAllText(Path.Combine(folder,"github.refresh"),"refresh");
        File.WriteAllText(Path.Combine(folder,"notes.txt"),"other file");
        return (database,folder,first.Count + second.Count);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery clear 1797: behaviour: the base kept open by the plugin can no longer be read as a file (the review's finding reproduced)",() => InTemp(root =>
        {
            var (database,_,_) = KeptBase(root);
            try
            {
                var refused = false;
                try
                {
                    using var zip = ZipFile.Open(Path.Combine(root,"old.zip"),ZipArchiveMode.Create);
                    zip.CreateEntryFromFile(database.Path,DiscoveryDatabase.FileName);   // the copy before 0.1797
                }
                catch (IOException) { refused = true; }
                Check(refused,"0.1794 to 0.1796: the copy before emptying refused while the base is kept open");
            }
            finally { database.Close(); }
        })),
        ("Discovery clear 1797: behaviour: with the base kept open, the copy is made, holds every fact (the journal's too) and opens as a base; tokens and journal files left out",() => InTemp(root =>
        {
            var (database,folder,count) = KeptBase(root);
            string? copy;
            try { copy = DiscoveryBackup.Create(folder,database,At); }
            finally { database.Close(); }
            Check(copy is not null && File.Exists(copy),"the copy made: " + copy);
            var backups = Path.Combine(root,"Backups");
            Check(Path.GetDirectoryName(copy) == backups && Path.GetFileName(copy!).EndsWith("-" + DiscoveryBackup.Label + ".zip",StringComparison.Ordinal),"in the Backups folder beside Discoveries: " + copy);
            using (var zip = ZipFile.OpenRead(copy!))
            {
                var names = zip.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal).ToArray();
                Check(names.SequenceEqual([DiscoveryDatabase.FileName,"notes.txt"]),"the base and the other file only (no token, no -wal or -shm): " + string.Join(", ",names));
                var restored = Path.Combine(root,"restored");
                Directory.CreateDirectory(restored);
                zip.GetEntry(DiscoveryDatabase.FileName)!.ExtractToFile(Path.Combine(restored,DiscoveryDatabase.FileName));
                Check(new DiscoveryDatabase(Path.Combine(restored,DiscoveryDatabase.FileName)).ReadMine().Facts.Count == count,"every fact in the copy, the journal's last saves included");
                Check(new StreamReader(zip.GetEntry("notes.txt")!.Open()).ReadToEnd() == "other file","the other files copied as they are");
            }
            Check(Directory.GetFiles(backups).Length == 1,"no snapshot left in Backups: " + string.Join(", ",Directory.GetFiles(backups).Select(Path.GetFileName)));
            Check(new DiscoveryDatabase(Path.Combine(folder,DiscoveryDatabase.FileName)).ReadMine().Facts.Count == count,"the live base untouched");
        })),
        ("Discovery clear 1797: behaviour: a file held open for writing by another handle is copied (shared read), its content as it is",() => InTemp(root =>
        {
            var source = Path.Combine(root,"Cache");
            Directory.CreateDirectory(source);
            var held = Path.Combine(source,"prices.db");
            using (var writer = new FileStream(held,FileMode.Create,FileAccess.ReadWrite,FileShare.ReadWrite))
            {
                writer.Write("held open"u8);
                writer.Flush();
                var copy = BackupZip.Create(source,Path.Combine(root,"Backups"),"cache",At);
                using var zip = ZipFile.OpenRead(copy!);
                Check(new StreamReader(zip.GetEntry("prices.db")!.Open()).ReadToEnd() == "held open","copied while held open");
            }
        })),
        ("Discovery clear 1797: behaviour: without a database (it could not be opened) the files are copied as before, the tokens still left out",() => InTemp(root =>
        {
            var folder = Path.Combine(root,"Discoveries");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"github.token"),"token");
            File.WriteAllText(Path.Combine(folder,"Discoveries.json"),"{}");
            var copy = DiscoveryBackup.Create(folder,null,At);
            using var zip = ZipFile.OpenRead(copy!);
            Check(zip.Entries.Select(entry => entry.FullName).SequenceEqual(["Discoveries.json"]),"the file only: " + string.Join(", ",zip.Entries.Select(entry => entry.FullName)));
            Check(DiscoveryBackup.Create(Path.Combine(root,"missing"),null,At) is null,"no folder: no copy");
        })),
        ("Discovery clear 1797: wiring: the button copies through DiscoveryBackup, then empties; a failed copy removes nothing",() =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.Discoveries.cs");
            var copy = window.IndexOf("try{DiscoveryBackup.Create(folder,DiscoveryDatabase,DateTimeOffset.UtcNow);}",StringComparison.Ordinal);
            var refused = window.IndexOf("catch(Exception error){log.Warning(error,\"The discoveries copy before emptying failed; nothing was removed.\");discoveryClearAsked=false;return;}",StringComparison.Ordinal);
            var emptied = window.IndexOf("store.Clear();SaveDiscoveries(false);discoveryClearAsked=false;",StringComparison.Ordinal);
            Check(copy > 0 && refused > copy && emptied > refused,"copy, refusal, then emptying");
            Check(!window.Contains("BackupZip.Create(folder",StringComparison.Ordinal),"no copy of the live file left");
        }),
    ];
}
