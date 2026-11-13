using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;
using TenshoAssistant.Link;

// 0.1147 (X-119 with X-173): the desktop application reads the player's data file (TenshoData.db) from its own copy, made by SQLite's backup.
// Behaviour tests (R28: concurrency, data loss): a real data file written by the plugin's own store, copied while a save is half done (open
// write transaction) and while the application reads its copy (refused whole, then copied); the plugin's file unchanged by the copy; the copy follows the plugin's file
// (newer saves, file gone); a damaged copy is made again; the gil journal shown is the newest character's, with its last balances.
internal static class DesktopData1147Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly Action<Exception,string> Warn=(error,message)=>throw new Exception(message,error);
    private static string Hash(string path)=>File.Exists(path) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "-";
    private static GilEntry Entry(DateTimeOffset at,long delta,long balance,GilSource source=GilSource.Character)=>new(at,delta,balance,source,GilCategory.Other);

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Desktop data 1147: the data file copied whole by SQLite's backup, mid-save and mid-read, the plugin's file only read",() =>
        {
            var root=Path.Combine(Path.GetTempPath(),"tensho-1147-"+Guid.NewGuid().ToString("N"));
            var plugin=Path.Combine(root,"plugin");var mirror=Path.Combine(root,"mirror");
            Directory.CreateDirectory(plugin);
            var source=Path.Combine(plugin,TenshoDataDatabase.FileName);var target=Path.Combine(mirror,TenshoDataDatabase.FileName);
            try
            {
                var now=DateTimeOffset.UtcNow;
                var store=new GilJournalStore(Path.Combine(plugin,"GilJournal"),new TenshoDataFile(source,Warn));
                store.Save(1,[Entry(now.AddHours(-3),100,1100),Entry(now.AddHours(-2),-50,1050)]);
                store.Save(2,[Entry(now.AddMinutes(-30),10,510),Entry(now.AddMinutes(-20),5_000,90_000,GilSource.Retainers)]);
                // The plugin's file and its journal as they are, compared after the copy (the reader writes no byte of them).
                var before=(Hash(source),Hash(source+"-wal"));
                var copy=TenshoDataDatabase.CopyForDesktop(source,target);
                Check(copy.Copied==1 && copy.Problems.Count==0,"copied: "+string.Join("; ",copy.Problems));
                // The data unchanged; a read-only reader may leave an empty journal (-wal) and its index (-shm) beside, as any SQLite reader of a WAL file.
                var wal=source+"-wal";
                Check(Hash(source)==before.Item1 && (Hash(wal)==before.Item2 || new FileInfo(wal).Length==0),"the plugin's file unchanged by the copy, its journal unchanged or empty: "+before+" -> "+(Hash(source),Hash(wal)));
                var app=new GilJournalStore(Path.Combine(mirror,"GilJournal"),new TenshoDataFile(target,Warn));
                Check(app.Latest()==2UL,"the newest character is the one shown");
                Check(app.Load(1,now).Count==2 && app.Load(2,now).Count==2,"every row copied");
                var (gil,retainers,company)=GilJournal.LastBalances(app.Load(2,now));
                Check(gil==510 && retainers==90_000 && company is null,"last balances per source, none guessed for the company chest");

                // A save half done in the plugin (its transaction open): the copy holds the last whole save, never half of the next one.
                using(var writing=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=source,Pooling=false}.ToString()))
                {
                    writing.Open();
                    using var transaction=writing.BeginTransaction();
                    using(var command=writing.CreateCommand()){command.Transaction=transaction;command.CommandText="DELETE FROM gil_entries WHERE character=1;";command.ExecuteNonQuery();}
                    copy=TenshoDataDatabase.CopyForDesktop(source,target);
                    Check(copy.Copied==1,"copied while the game writes: "+string.Join("; ",copy.Problems));
                    Check(app.Load(1,now).Count==2,"the half-done save is not in the copy");
                    transaction.Rollback();
                }
                // A newer save, copied while the application reads its copy (an open read on the copy).
                store.Save(3,[Entry(now.AddMinutes(-1),1,7)]);
                using(var reading=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=target,Pooling=false}.ToString()))
                {
                    reading.Open();
                    using var transaction=reading.BeginTransaction();
                    using(var command=reading.CreateCommand()){command.Transaction=transaction;command.CommandText="SELECT COUNT(*) FROM gil_entries;";command.ExecuteScalar();}
                    // 0.1202: the same wait and refusal in 0.3 s instead of the plugin's 5 s (the test took 5.8 s).
                    var wait=TenshoDataDatabase.CopyBusyWait;TenshoDataDatabase.CopyBusyWait=300;
                    try{copy=TenshoDataDatabase.CopyForDesktop(source,target);}
                    finally{TenshoDataDatabase.CopyBusyWait=wait;}
                    transaction.Commit();
                }
                // A read of the copy under way: the backup waits, then gives up (SQLite busy) and the copy stays the last whole one; the next notice copies.
                Check(copy.Copied==1 || copy.Problems.Count==1 && copy.Problems[0].Contains("SQLite error 5",StringComparison.Ordinal) && app.Latest()==2UL && app.Load(1,now).Count==2,"while the app reads: copied, or refused with the copy whole: "+string.Join("; ",copy.Problems));
                copy=TenshoDataDatabase.CopyForDesktop(source,target);
                Check(copy.Copied==1,"copied once the read is over: "+string.Join("; ",copy.Problems));
                Check(app.Latest()==3UL && app.Load(3,now).Count==1,"the copy follows the plugin's file");

                // The game closed: the plugin's journal folded in, no connection left: still copied, read-only.
                new TenshoDataFile(source,Warn).Checkpoint();
                copy=TenshoDataDatabase.CopyForDesktop(source,target);
                Check(copy.Copied==1,"copied with the game closed: "+string.Join("; ",copy.Problems));

                // The application's own copy damaged: made again from scratch.
                foreach(var path in new[]{target+"-wal",target+"-shm"})if(File.Exists(path))File.Delete(path);
                File.WriteAllText(target,"this is not a database, this is not a database, this is not a database, this is not a database");
                copy=TenshoDataDatabase.CopyForDesktop(source,target);
                Check(copy.Copied==1 && new GilJournalStore(Path.Combine(mirror,"GilJournal"),new TenshoDataFile(target,Warn)).Latest()==3UL,"a damaged copy is made again: "+string.Join("; ",copy.Problems));

                // Same file: refused. The plugin's file gone: the copy goes too.
                copy=TenshoDataDatabase.CopyForDesktop(source,source);
                Check(copy.Copied==0 && copy.Problems.Count==1 && File.Exists(source),"never onto the plugin's own file");
                SqliteConnection.ClearAllPools();
                foreach(var path in new[]{source,source+"-wal",source+"-shm"})if(File.Exists(path))File.Delete(path);
                copy=TenshoDataDatabase.CopyForDesktop(source,target);
                Check(copy.Copied==0 && copy.Problems.Count==0 && !File.Exists(target),"no data file: no copy");
            }
            finally{SqliteConnection.ClearAllPools();Directory.Delete(root,true);}
        }),
        ("Desktop data 1147: empty journal, the link category and its wiring",() =>
        {
            var root=Path.Combine(Path.GetTempPath(),"tensho-1147-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var empty=new GilJournalStore(Path.Combine(root,"GilJournal"),new TenshoDataFile(Path.Combine(root,TenshoDataDatabase.FileName),Warn));
                Check(empty.Latest() is null,"no entry: no character shown");
                Check(GilJournal.LastBalances([])==(null,null,null),"no balance guessed");
            }
            finally{SqliteConnection.ClearAllPools();Directory.Delete(root,true);}
            var folder=Path.Combine(Path.GetTempPath(),"tensho-1147-link");var settings=Path.Combine(Path.GetTempPath(),"TenshoAssistant.json");
            Check(LocalLinkProtocol.ChangeCategory(settings,folder,Path.Combine(folder,"TenshoData.db"))=="data"
                && LocalLinkProtocol.ChangeCategory(settings,folder,Path.Combine(folder,"TenshoData.db-wal"))=="data","the data file and its journal: « data »");
            Check(LocalLinkProtocol.ChangeCategory(settings,folder,Path.Combine(folder,"TenshoData.db-shm")) is null
                && LocalLinkProtocol.ChangeCategory(settings,folder,Path.Combine(folder,"Discoveries","Discoveries.db-shm")) is null,"a reader's shared-memory index is no change (the app's copy would wake itself)");
            Check(LocalLinkProtocol.DesktopCategories.Contains(LocalLinkProtocol.DataCategory) && LocalLinkProtocol.ValidCategory("data"),"a category of the closed list, read by the app");
            var bar=Read("src/TenshoAssistant.Desktop/StatusBar.cs");
            Check(bar.Contains("category==TenshoAssistant.Link.LocalLinkProtocol.DataCategory ? host.CopyData(data.Source,data.Mirror)") && bar.Contains(": DesktopMirror.Copy(data.Source,data.Mirror,category);")
                && bar.Contains("Apply(category,Copy(data,category));") && bar.Contains("new(()=>Copy(data,category))"),"the app copies « data » by the backup, at start and on notice");
            var host=Read("src/TenshoAssistant.Desktop/Host.cs");
            Check(host.Contains("GetMethod(\"CopyForDesktop\",BindingFlags.Static|BindingFlags.Public,[typeof(string),typeof(string)])"),"called by its name and signature");
            var window=Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(window.Contains("case \"data\":") && window.Contains("if(Volatile.Read(ref desktopGilGeneration)==generation)Volatile.Write(ref desktopGil,state);")
                && window.Contains("_=Task.Run(()=>"),"read off the frame, an older reading never replaces a newer one");
            var security=Read("SECURITY.md");
            Check(security.Contains("`discoveries`, `data`), never a file name") && security.Contains("`discoveries`, `data`), jamais un nom de fichier"),"SECURITY lists it, both halves");
        }),
    ];
}
