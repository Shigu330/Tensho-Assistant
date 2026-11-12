using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Desktop;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Link;

// 0.1152 (X-119): the discoveries in the desktop application, read from its own copy of Discoveries.db. Behaviour tests on real files: the database
// copied by SQLite's backup, that file only (never the folder's GitHub sign-in or imports); copies taken while the plugin writes are each whole and
// readable; the plugin's facts never changed by the copy or by the application's reading; the copy waits its turn (at most every 5 minutes), a
// notice in between is kept, never lost; the application never writes its copy.
internal static class DesktopDiscoveries1152Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026,11,11,20,0,0,TimeSpan.Zero);

    private static void InTemp(Action<string,string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-dis-1152-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(Path.Combine(root,"plugin"),Path.Combine(root,"mirror")); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static void Save(DiscoveryStore store,DiscoveryDatabase database)
    {
        var changes = store.ChangesToSave(out var stamp);
        database.Write(changes.Write,changes.Remove,changes.All);
        store.MarkSaved(stamp);
    }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop discoveries 1152: the database alone copied, read back whole; copies taken while the plugin writes are each whole; the plugin's facts unchanged",() => InTemp((plugin,mirror) =>
        {
            var folder = Path.Combine(plugin,"Discoveries");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"github.json"),"{\"login\":\"someone\"}");
            Directory.CreateDirectory(Path.Combine(folder,"Import"));File.WriteAllText(Path.Combine(folder,"Import","x.json"),"{}");
            var database = new DiscoveryDatabase(Path.Combine(folder,DiscoveryDatabase.FileName));
            var store = new DiscoveryStore();
            for (uint subject = 1; subject <= 200; subject++) store.SeeSource(DiscoveryKind.Desynthesis,subject,1,1,At);
            Save(store,database);
            var source = Path.Combine(folder,DiscoveryDatabase.FileName);var target = Path.Combine(mirror,"Discoveries",DiscoveryDatabase.FileName);
            var copy = TenshoDataDatabase.CopyDiscoveriesForDesktop(source,target);
            Check(copy.Copied == 1 && copy.Problems.Count == 0,"copied: " + string.Join("; ",copy.Problems));
            Check(Directory.GetFiles(Path.Combine(mirror,"Discoveries")).All(path => Path.GetFileName(path).StartsWith(DiscoveryDatabase.FileName,StringComparison.Ordinal))
                && !Directory.Exists(Path.Combine(mirror,"Discoveries","Import")),"only the database (never github.json nor the imports)");
            Check(new DiscoveryDatabase(target).Count() == (200,0),"read back whole");
            // The plugin keeps writing while the application copies: every copy is a whole save (a multiple of 50 facts here), never half of one.
            var writing = Task.Run(() =>
            {
                for (var round = 0; round < 8; round++)
                {
                    for (uint subject = 0; subject < 50; subject++) store.SeeSource(DiscoveryKind.Desynthesis,1000 + (uint)round * 50 + subject,1,1,At);
                    Save(store,database);
                }
            });
            var counts = new List<long>();
            while (!writing.IsCompleted)
            {
                var again = TenshoDataDatabase.CopyDiscoveriesForDesktop(source,target);
                if (again.Copied == 1) counts.Add(new DiscoveryDatabase(target).Count().Mine);
            }
            writing.GetAwaiter().GetResult();
            Check(TenshoDataDatabase.CopyDiscoveriesForDesktop(source,target).Copied == 1 && new DiscoveryDatabase(target).Count() == (600,0),"the last copy holds every save");
            Check(counts.All(count => count >= 200 && count <= 600 && count % 50 == 0),"each copy a whole save: " + string.Join(",",counts));
            Check(database.Count() == (600,0),"the plugin's facts unchanged by the copies and the readings");
            Check(TenshoDataDatabase.CopyDiscoveriesForDesktop(source,source).Copied == 0,"never over the plugin's own file");
            File.Delete(source);
            foreach (var extra in new[] { source + "-wal",source + "-shm" }) if (File.Exists(extra)) File.Delete(extra);
            Check(TenshoDataDatabase.CopyDiscoveriesForDesktop(source,target).Copied == 0 && !File.Exists(target),"no database in the plugin's folder: the copy goes too");
        })),
        ("Desktop discoveries 1152: the copy waits its turn (5 minutes), a notice in between is kept and copied when the turn comes; other categories at once",() =>
        {
            var now = new DateTime(2026,11,12,8,0,0,DateTimeKind.Utc);var next = now.AddMinutes(5);var waiting = false;
            var first = DesktopMirror.Due(["discoveries","bank","settings","bank"],now,ref next,ref waiting);
            Check(first.SequenceEqual(["bank"]) && waiting,"the bank at once (once), the settings not a copied category, the discoveries wait: " + string.Join(",",first));
            Check(DesktopMirror.Due([],now.AddMinutes(4),ref next,ref waiting).Count == 0 && waiting,"not yet");
            Check(DesktopMirror.Due([],now.AddMinutes(5),ref next,ref waiting).SequenceEqual([LocalLinkProtocol.DiscoveriesCategory]) && !waiting && next == now.AddMinutes(10),"copied when the turn comes, even with no new notice");
            Check(DesktopMirror.Due(["discoveries"],now.AddMinutes(6),ref next,ref waiting).Count == 0 && waiting,"a new notice waits for the next turn");
            Check(DesktopMirror.Due(["voyages"],now.AddMinutes(11),ref next,ref waiting).SequenceEqual(["voyages",LocalLinkProtocol.DiscoveriesCategory]),"then copied with the others");
            Check(DesktopMirror.DiscoveriesEvery == TimeSpan.FromMinutes(5) && LocalLinkProtocol.DesktopCategories.Contains(LocalLinkProtocol.DiscoveriesCategory),"5 minutes; a category the application reads");
        }),
        ("Desktop discoveries 1152: the application reads its copy off the frame, never writes it, and copies the database alone",() =>
        {
            var data = Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            var use = data.IndexOf("private void UseDesktopDiscoveries(",StringComparison.Ordinal);
            Check(use > 0 && data.IndexOf("DiscoveriesWriteBlocked=true;",use,StringComparison.Ordinal) < data.IndexOf("_=Task.Run(()=>",use,StringComparison.Ordinal)
                && data.Contains("if(Volatile.Read(ref desktopDiscoveriesGeneration)!=generation)return;\n            DiscoveryFolder=discoveries;DiscoveryStore=store;DiscoveriesLoaded=true;",StringComparison.Ordinal),
                "writing blocked before anything is shown; the newest reading kept");
            Check(Read("src/TenshoAssistant/MainWindow.Discoveries.cs").Contains("|| DiscoveriesWriteBlocked ||",StringComparison.Ordinal),"a blocked base is never saved");
            var bar = Read("src/TenshoAssistant.Desktop/StatusBar.cs");
            Check(bar.Contains("category==TenshoAssistant.Link.LocalLinkProtocol.DiscoveriesCategory ? host.CopyDiscoveries(data.Source,data.Mirror)",StringComparison.Ordinal)
                && bar.Contains("foreach(var category in DesktopMirror.Due(categories,DateTime.UtcNow,ref discoveriesNext,ref discoveriesWaiting))",StringComparison.Ordinal),"status bar");
            Check(Read("src/TenshoAssistant.Desktop/Host.cs").Contains("GetMethod(\"CopyDiscoveriesForDesktop\",BindingFlags.Static|BindingFlags.Public,[typeof(string),typeof(string)])",StringComparison.Ordinal),"the application calls the plugin's copy by its name");
        }),
    ];
}
