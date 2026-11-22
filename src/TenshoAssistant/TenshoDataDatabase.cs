using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TenshoAssistant;

/// <summary>
/// X-173 (0.1139, user's choice of 12/11: « two SQLite files, an erasable cache and the data to protect »): the player's data file,
/// <c>TenshoData.db</c>, at the root of the plugin's configuration folder. The families of X-173 moved into it one at a time, each with its own
/// tables: the gil journal (0.1139, <see cref="Inventory.GilJournalStore"/>), the achievement progress (0.1140, <see cref="Collections.AchievementProgressData"/>),
/// the voyage history (0.1148, <see cref="FreeCompany.VoyageHistoryData"/>), the bank and inventories (0.1149, <see cref="Inventory.PerCharacterBankStore"/>).
/// X-574 (0.1793, the user's choice of 17/11): their files of before are no longer converted; one still there is left aside, never read
/// (<see cref="TenshoDataFile.LeftAside"/>). The discoveries keep their own file for now (Discoveries/Discoveries.db, X-172; choice noted as
/// D-141). WAL journal: a stop in the middle of a save leaves the last one whole, another process may read while the game writes. Every call
/// opens its own connection: safe from any thread.
/// </summary>
public sealed class TenshoDataDatabase
{
    public const string FileName="TenshoData.db";
    /// <summary>
    /// The layout this version writes (PRAGMA user_version). It changes only for a change an older version could not read safely; a table added
    /// for a new family keeps it (an older version leaves the table alone). A newer one is refused, never written over.
    /// </summary>
    public const int Schema=1;
    private readonly string connection;
    private readonly SqliteKeeper keeper;
    public string Path { get; }

    /// <summary>Opens (or creates) the file; throws when SQLite cannot be loaded or the file is of a newer layout or damaged.</summary>
    public TenshoDataDatabase(string path)
    {
        Path=System.IO.Path.GetFullPath(path);
        connection=new SqliteConnectionStringBuilder{DataSource=Path,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false}.ToString();
        keeper=new(Open);
        using var open=Open();
        Run(open,"PRAGMA journal_mode=WAL;");
        var version=System.Convert.ToInt32(Scalar(open,"PRAGMA user_version;"),CultureInfo.InvariantCulture);
        if(version>Schema)throw new InvalidDataException($"Tensho data file of a newer layout ({version}).");
        using(var transaction=open.BeginTransaction())
        {
            // 0.1139: the gil journal: one row per entry, numbered per character in the journal's order (oldest first); a character's id is stored
            // as the signed 64-bit number of the same bits (SQLite integers are signed).
            Run(open,"CREATE TABLE IF NOT EXISTS gil_entries(character INTEGER NOT NULL,seq INTEGER NOT NULL,at INTEGER NOT NULL,delta INTEGER NOT NULL,"
                +"balance INTEGER NOT NULL,source INTEGER NOT NULL,category INTEGER NOT NULL,PRIMARY KEY(character,seq)) WITHOUT ROWID;",transaction);
            // A character whose journal file of before is in the table (written in the same transaction as its rows): never converted twice.
            // X-574 (0.1793): no longer read nor written (nothing is converted); kept, with its rows, so the layout stays the same.
            Run(open,"CREATE TABLE IF NOT EXISTS gil_converted(character INTEGER PRIMARY KEY NOT NULL,at INTEGER NOT NULL);",transaction);
            // 0.1140: a family kept in one file before (« achievements »…), converted: never converted twice. X-574 (0.1793): as gil_converted.
            Run(open,"CREATE TABLE IF NOT EXISTS converted(family TEXT PRIMARY KEY NOT NULL,at INTEGER NOT NULL);",transaction);
            // 0.1140: the achievement progress seen, one row per character and achievement.
            Run(open,"CREATE TABLE IF NOT EXISTS achievement_progress(character INTEGER NOT NULL,achievement INTEGER NOT NULL,current INTEGER NOT NULL,"
                +"max INTEGER NOT NULL,seen_at INTEGER NOT NULL,PRIMARY KEY(character,achievement)) WITHOUT ROWID;",transaction);
            // 0.1148: the voyage history, one row per voyage (house, kind, slot, registration); its lists (sectors, route, loot) as JSON text.
            Run(open,"CREATE TABLE IF NOT EXISTS voyage_records(house INTEGER NOT NULL,kind INTEGER NOT NULL,slot INTEGER NOT NULL,registered INTEGER NOT NULL,"
                +"vessel TEXT NOT NULL,rank INTEGER NOT NULL,return_at INTEGER,points TEXT NOT NULL,route TEXT NOT NULL,loot TEXT NOT NULL,"
                +"PRIMARY KEY(house,kind,slot,registered)) WITHOUT ROWID;",transaction);
            // 0.1149: the bank and inventories, one row per stored place (character, kind, retainer or 0) in the character's order (seq); each
            // row holds the entry as the bank file wrote it (JSON, same names and bounds), checked again when read.
            Run(open,"CREATE TABLE IF NOT EXISTS bank_entries(character INTEGER NOT NULL,kind INTEGER NOT NULL,storage INTEGER NOT NULL,seq INTEGER NOT NULL,"
                +"entry TEXT NOT NULL,PRIMARY KEY(character,kind,storage)) WITHOUT ROWID;",transaction);
            // 0.1228 (X-218): each character's job levels and gear sets as last read (Storage › Characters), replaced whole by a newer reading.
            Run(open,"CREATE TABLE IF NOT EXISTS character_jobs(character INTEGER NOT NULL,job INTEGER NOT NULL,level INTEGER NOT NULL,seen_at INTEGER NOT NULL,"
                +"PRIMARY KEY(character,job)) WITHOUT ROWID;",transaction);
            Run(open,"CREATE TABLE IF NOT EXISTS character_gearsets(character INTEGER NOT NULL,number INTEGER NOT NULL,job INTEGER NOT NULL,item_level INTEGER NOT NULL,"
                +"name TEXT NOT NULL,seen_at INTEGER NOT NULL,PRIMARY KEY(character,number)) WITHOUT ROWID;",transaction);
            // 0.1387 (X-274): each character's titles and achievements as the game last sent them completely (ids obtained, comma-separated).
            Run(open,"CREATE TABLE IF NOT EXISTS completion_lists(character INTEGER NOT NULL,kind INTEGER NOT NULL,read_at INTEGER NOT NULL,ids TEXT NOT NULL,"
                +"PRIMARY KEY(character,kind)) WITHOUT ROWID;",transaction);
            // X-750 (0.1794): one row written again at each plugin load to time a save (SaveTimes); nothing else reads it.
            Run(open,"CREATE TABLE IF NOT EXISTS save_probe(id INTEGER PRIMARY KEY NOT NULL,at INTEGER NOT NULL);",transaction);
            if(version<Schema)Run(open,$"PRAGMA user_version={Schema};",transaction);
            transaction.Commit();
        }
        if(Scalar(open,"PRAGMA quick_check;") is not "ok")throw new InvalidDataException("Tensho data file damaged (quick_check).");
    }

    /// <summary>A new connection to the file (busy wait of 5 s when another process writes).</summary>
    public SqliteConnection Open()
    {
        var open=new SqliteConnection(connection);
        open.Open();
        Run(open,"PRAGMA busy_timeout=5000;PRAGMA synchronous=NORMAL;");
        return open;
    }
    public static void Run(SqliteConnection open,string sql,SqliteTransaction? transaction=null)
    {
        using var command=open.CreateCommand();command.CommandText=sql;command.Transaction=transaction;command.ExecuteNonQuery();
    }
    public static object? Scalar(SqliteConnection open,string sql)
    {
        using var command=open.CreateCommand();command.CommandText=sql;return command.ExecuteScalar();
    }

    /// <summary>The SQLite library's version (for the log).</summary>
    /// <summary>X-185 (0.1211, N10): the tables that keep rows per character (« Erase this character » empties them all for one).</summary>
    public static readonly string[] CharacterTables=["bank_entries","gil_entries","achievement_progress","character_jobs","character_gearsets","completion_lists"];

    /// <summary>X-185 (0.1211, N10): one character's rows of every per-character table, in the caller's transaction.</summary>
    public static void EraseCharacter(SqliteConnection open,SqliteTransaction transaction,ulong character)
    {
        foreach(var table in CharacterTables)
        {
            using var delete=open.CreateCommand();
            delete.Transaction=transaction;delete.CommandText=$"DELETE FROM {table} WHERE character=$character;";
            delete.Parameters.AddWithValue("$character",unchecked((long)character));
            delete.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// X-185 (0.1211, N10): the copies of one character's files of before (« id.json », « id.json.bak »…) removed from every before-sqlite-* folder
    /// in the profile folder or one level below it (the bank's and the gil journal's); a file that cannot be removed is left (never an error).
    /// X-574 (0.1793): and from the folders the files of before were left in, no longer converted (<see cref="LeftInPlace"/>: InventoryBank/,
    /// GilJournal/): « Erase this character » removes them too, as the conversion of before would have put them in before-sqlite-*.
    /// Returns how many were removed.
    /// </summary>
    public static int PurgeSetAside(string root,ulong character)
    {
        var name=character.ToString(CultureInfo.InvariantCulture)+".json";var removed=0;
        if(string.IsNullOrEmpty(root) || !Directory.Exists(root))return 0;
        try
        {
            var kept=Directory.EnumerateDirectories(root,"before-sqlite-*").Concat(Directory.EnumerateDirectories(root).SelectMany(folder=>Directory.EnumerateDirectories(folder,"before-sqlite-*"))).ToArray();
            var inPlace=LeftInPlace.Select(folder=>System.IO.Path.Combine(root,folder)).Where(Directory.Exists).SelectMany(folder=>Directory.EnumerateFiles(folder,"*",SearchOption.TopDirectoryOnly));
            foreach(var file in kept.SelectMany(folder=>Directory.EnumerateFiles(folder,"*",SearchOption.AllDirectories)).Concat(inPlace).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
            {
                var own=System.IO.Path.GetFileName(file);
                if(own!=name && !own.StartsWith(name+".",StringComparison.Ordinal))continue;
                try{File.Delete(file);removed++;}
                catch(Exception error) when(error is IOException or UnauthorizedAccessException){}
            }
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){}
        return removed;
    }

    /// <summary>X-574 (0.1793): the folders of one file per character of before (the bank's, the gil journal's), under the profile folder.</summary>
    public static readonly string[] LeftInPlace=["InventoryBank","GilJournal"];

    public string LibraryVersion(){using var open=Open();return Scalar(open,"SELECT sqlite_version();") as string ?? "";}

    /// <summary>
    /// X-182 (0.1169, night review N27): the journal written into the file and synced (a full checkpoint), so a conversion's commit survives a
    /// power cut before its file of before is moved; false when another connection kept it from finishing (the move then waits for the next start).
    /// X-574 (0.1793): no conversion calls it any more; kept as the data file's « written and synced » check.
    /// </summary>
    public bool Durable()
    {
        using var open=Open();using var command=open.CreateCommand();
        command.CommandText="PRAGMA wal_checkpoint(FULL);";
        using var reader=command.ExecuteReader();
        return reader.Read() && reader.GetInt64(0)==0;
    }

    /// <summary>The journal folded into the file (at unload), so the folder holds one file when the game is closed.</summary>
    public void Checkpoint(){using var open=Open();Run(open,"PRAGMA wal_checkpoint(TRUNCATE);");}

    /// <summary>
    /// X-750 (0.1794, idea I-024 chosen by the user on 21/11): the file kept open while the plugin runs (<see cref="SqliteKeeper"/>): a save's
    /// connection is then never the last one, and its close no longer folds the journal into the file (Pooling=false: before, every close of
    /// the last connection did, up to 1.7 s measured under load). The journal is folded in by SQLite's own checkpoints, on the saving thread
    /// (never the game's: every save of this file runs off the frame), and at <see cref="Close"/>. Called by the plugin off the frame, once;
    /// false when already kept or closed. The tests and the desktop application's copies never keep it.
    /// </summary>
    public bool KeepOpen()=>keeper.Keep();

    /// <summary>X-750: the kept connection closed, the journal folded in first (at unload); a later save opens its own connection as before.</summary>
    public void Close()=>keeper.Close();

    /// <summary>X-750: whether the file is kept open now.</summary>
    public bool Kept=>keeper.Held;

    /// <summary>
    /// X-750: one save timed both ways, for the log: Folded, the save then the journal folded in (what each close of the last connection did
    /// before X-750); Kept, the save alone (what a close does while the file is kept open). One row of save_probe written each time.
    /// </summary>
    public (double Folded,double Kept) SaveTimes()
    {
        var started=System.Diagnostics.Stopwatch.GetTimestamp();
        ProbeSave();
        Checkpoint();
        var folded=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        started=System.Diagnostics.Stopwatch.GetTimestamp();
        ProbeSave();
        return (folded,System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    /// <summary>X-785 M3 (0.1798): whether a save was already timed in this file (its row of save_probe).</summary>
    public bool SavesTimed(){using var open=Open();return Convert.ToInt64(Scalar(open,"SELECT COUNT(*) FROM save_probe;"))>0;}

    private void ProbeSave()
    {
        using var open=Open();using var transaction=open.BeginTransaction();using var command=open.CreateCommand();
        command.Transaction=transaction;
        command.CommandText="INSERT OR REPLACE INTO save_probe(id,at) VALUES(1,$at);";
        command.Parameters.AddWithValue("$at",DateTimeOffset.UtcNow.UtcTicks);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    /// <summary>The largest player's data file the desktop application copies.</summary>
    public const long MaxDesktopCopy=256L<<20;

    /// <summary>
    /// X-119 with X-173 (0.1147): the desktop application's own copy of the player's data file (its mirror folder, never the plugin's), made by
    /// SQLite's online backup: the plugin's file is opened read-only and copied whole as of one moment, even while the game writes it (a plain
    /// file copy of a WAL database could take half a save); the copy is written in one step under SQLite's lock, so a reader of the copy sees
    /// the old copy or the new one. No file in the plugin's folder: the copy goes too. Called by the application by its name (Host.CopyData,
    /// DesktopData1147Tests); the number of files copied (0 or 1) and the problems met (English, for the log).
    /// </summary>
    public static (int Copied,IReadOnlyList<string> Problems) CopyForDesktop(string source,string target)=>CopyDatabase(source,target,FileName,MaxDesktopCopy);

    /// <summary>X-119 (0.1152): the largest discoveries database the desktop application copies.</summary>
    public const long MaxDiscoveriesCopy=512L<<20;

    /// <summary>
    /// X-119 (0.1152): the discoveries database (Discoveries/Discoveries.db) copied for the desktop application the same way: that one file only,
    /// never the folder's other files (the GitHub exchange's sign-in, imports, exports). Called by the application by its name (Host.CopyDiscoveries).
    /// </summary>
    public static (int Copied,IReadOnlyList<string> Problems) CopyDiscoveriesForDesktop(string source,string target)
        =>CopyDatabase(source,target,Discoveries.DiscoveryDatabase.FileName,MaxDiscoveriesCopy);

    private static (int Copied,IReadOnlyList<string> Problems) CopyDatabase(string source,string target,string name,long max)
    {
        var problems=new List<string>();
        var from=System.IO.Path.GetFullPath(source);var to=System.IO.Path.GetFullPath(target);
        if(string.Equals(from,to,StringComparison.OrdinalIgnoreCase))return (0,[$"{name}: the copy and the plugin's file are the same, nothing copied"]);
        try
        {
            if(!File.Exists(from))
            {
                foreach(var path in new[]{to,to+"-wal",to+"-shm"})if(File.Exists(path))File.Delete(path);
                return (0,problems);
            }
            if(new FileInfo(from).Length>max)return (0,[$"{name}: larger than {max>>20} MB, not copied"]);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(to)!);
            try{Backup(from,to);}
            // The application's own copy damaged (not a database, corrupt): it is only a copy, made again from scratch once. X-587 (external
            // audit A20 of 18/11): unless the plugin's own file is the damaged one: then the copy may be the last readable one and is kept as it
            // is; the new copy is made beside it and takes its place only once whole.
            catch(SqliteException error) when(error.SqliteErrorCode is 11 or 26)
            {
                if(!Sound(from))return (0,[$"{name}: the plugin's file is damaged (SQLite error {error.SqliteErrorCode}); the last copy is kept"]);
                var fresh=to+".new";
                foreach(var path in new[]{fresh,fresh+"-wal",fresh+"-shm"})if(File.Exists(path))File.Delete(path);
                Backup(from,fresh);
                foreach(var path in new[]{to+"-wal",to+"-shm"})if(File.Exists(path))File.Delete(path);
                File.Move(fresh,to,true);
            }
            return (1,problems);
        }
        catch(SqliteException error){problems.Add($"{name}: not copied (SQLite error {error.SqliteErrorCode})");}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){problems.Add($"{name}: not copied ({error.GetType().Name})");}
        return (0,problems);
    }

    /// <summary>0.1202: how long a copy waits for the other side (a save of the plugin, a read of the application's copy) before it gives
    /// up and keeps the last whole copy: 5 s. DesktopData1147Tests shortens it to see the same refusal in a fraction of the time.</summary>
    internal static int CopyBusyWait=5000;

    /// <summary>X-587 (A20): the plugin's file passes SQLite's quick check, read-only (false: damaged, « corrupt » or « not a database »).</summary>
    private static bool Sound(string from)
    {
        try
        {
            using var reader=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=from,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());
            reader.Open();
            Run(reader,$"PRAGMA busy_timeout={CopyBusyWait};");
            using var command=reader.CreateCommand();command.CommandText="PRAGMA quick_check;";
            return command.ExecuteScalar() as string=="ok";
        }
        catch(SqliteException error) when(error.SqliteErrorCode is 11 or 26){return false;}
    }

    private static void Backup(string from,string to)
    {
        using var reader=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=from,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());
        reader.Open();
        Run(reader,$"PRAGMA busy_timeout={CopyBusyWait};");
        using var writer=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=to,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false}.ToString());
        writer.Open();
        Run(writer,$"PRAGMA busy_timeout={CopyBusyWait};");
        reader.BackupDatabase(writer);
    }
}

/// <summary>
/// X-173 (0.1140): the player's data file as the plugin uses it, one for every family: opened on first use (off the frame), once. A file that
/// exists but cannot be opened (SQLite not loaded, damaged, newer layout) is never replaced: every family is then unavailable and writes nothing
/// this session (<see cref="Database"/> throws); when the file cannot be created at all, <see cref="Database"/> gives null and each family keeps
/// its JSON files as before (a mode of its own: those files are then read and written, X-574 changed nothing there).
/// </summary>
public sealed class TenshoDataFile
{
    private readonly string path;
    private readonly Action<Exception,string> warn;
    private readonly Action<string>? info;
    private readonly object gate=new();
    private bool tried;
    private TenshoDataDatabase? database;
    private Exception? blocked;

    public TenshoDataFile(string path,Action<Exception,string> warn,Action<string>? info=null){this.path=path;this.warn=warn;this.info=info;}

    /// <summary>The database, or null when the JSON files stay in use; throws <see cref="IOException"/> when the file is there but unusable.</summary>
    public TenshoDataDatabase? Database()
    {
        lock(gate)
        {
            if(!tried)
            {
                tried=true;
                try
                {
                    database=new TenshoDataDatabase(path);
                    info?.Invoke($"Tensho data: SQLite {database.LibraryVersion()} data file {TenshoDataDatabase.FileName}.");
                }
                catch(Exception error)
                {
                    database=null;
                    if(File.Exists(path)){blocked=error;warn(error,"The Tensho data file could not be opened: the gil journal, the achievement progress, the voyage history and the bank are not written this session.");}
                    else warn(error,"The Tensho data file could not be created: the JSON files stay in use this session.");
                }
            }
            if(blocked is not null)throw new IOException("The Tensho data file could not be opened.",blocked);
            return database;
        }
    }

    /// <summary>X-789 (0.1802, idea I-058): the data file could not be created and the JSON files of before are written instead (« json »
    /// mode), known once a family opened it; never opens the file (read in Draw by the main window's banner). What is written meanwhile is
    /// not taken into the data file once it can be created (X-574: the files of before are no longer converted).</summary>
    public bool JsonFallback{get{lock(gate)return tried && database is null && blocked is null;}}

    /// <summary>« sqlite », « json » (file could not be created) or « blocked » (file there but unusable); opens it on first use.</summary>
    public string Mode
    {
        get{try{return Database() is null ? "json":"sqlite";}catch(IOException){return "blocked";}}
    }

    /// <summary>The journal folded in (at unload); nothing when the file was never opened or is unusable.</summary>
    public void Checkpoint()
    {
        TenshoDataDatabase? opened;
        lock(gate)opened=blocked is null ? database:null;
        opened?.Checkpoint();
    }

    private bool closing;

    /// <summary>
    /// X-750 (0.1794): the plugin's data file opened and kept open (<see cref="TenshoDataDatabase.KeepOpen"/>), off the frame, at the plugin's
    /// start; the first time for this file (X-785 M3, 0.1798), one save timed both ways said in the log. Nothing when the JSON files are in
    /// use, the file is unusable, or after <see cref="Close"/>.
    /// </summary>
    public void KeepOpen()
    {
        TenshoDataDatabase? opened;
        try{opened=Database();}catch(IOException){return;}
        lock(gate)if(closing)return;
        if(opened is null || !opened.KeepOpen())return;
        try
        {
            // X-785 M3 (0.1798, review of 0.1795): timed once per data file (its first load since X-750), no longer at each load: each one wrote
            // a row and forced a full checkpoint, which with two clients on one profile (X-141) waits for the other's readers and holds its
            // saves meanwhile (up to busy_timeout, 5 s). The measure served X-750 (86 ms → 5 ms in game, 22/11).
            if(opened.SavesTimed())return;
            var (folded,kept)=opened.SaveTimes();
            info?.Invoke($"Tensho data: a save took {folded:0} ms with the journal folded in at its close (each save before X-750), {kept:0} ms with the file kept open (X-750).");
        }
        catch(Exception error) when(error is SqliteException or IOException or InvalidOperationException){warn(error,"Tensho data: a save could not be timed (X-750); the file stays kept open.");}
    }

    /// <summary>X-750: at unload, the kept connection closed and the journal folded in; a save after that opens its own connection.</summary>
    public void Close()
    {
        TenshoDataDatabase? opened;
        lock(gate){closing=true;opened=blocked is null ? database:null;}
        opened?.Close();
    }

    /// <summary>
    /// X-574 (0.1793, the user's choice of 17/11: « remove it, I am the only one using the plugin »): the files of before of a family (the bank's
    /// InventoryBank.json and InventoryBank/, the gil journal's, the voyage history's, the achievement progress's), found while the data file
    /// is in use, are no longer converted: each is left where it is, never read, moved nor removed. Said once per family for this data file
    /// (one per plugin load), in English through the log; nothing when <paramref name="names"/> is empty. Returns true when it was said now.
    /// </summary>
    public bool LeftAside(string family,params string[] names)
    {
        if(names.Length==0)return false;
        lock(gate)if(!saidAside.Add(family))return false;
        info?.Invoke(names.Length==1
            ? $"{family}: a file of an old format ({names[0]}) was left aside, not read (X-574)."
            : $"{family}: files of an old format ({string.Join(", ",names)}) were left aside, not read (X-574).");
        return true;
    }
    private readonly HashSet<string> saidAside=new(StringComparer.Ordinal);
}

/// <summary>
/// X-750 (0.1794): one idle connection kept open on a SQLite file in WAL mode while the plugin runs, so that the connections of the saves
/// (Pooling=false, one per call) are never the last one and their close does not fold the journal into the file. <see cref="Close"/> folds it
/// in (TRUNCATE) and closes the kept connection (at unload: the load context then holds no handle on the file). Thread-safe; a keeper closed
/// is never kept again. A file another process uses at the same time is not affected (WAL: readers and one writer at a time, as before).
/// </summary>
public sealed class SqliteKeeper(Func<SqliteConnection> open)
{
    private readonly object gate=new();
    private SqliteConnection? held;
    private bool closed;

    public bool Held{get{lock(gate)return held is not null;}}

    /// <summary>The connection opened and kept; false when it is already kept or the keeper was closed.</summary>
    public bool Keep()
    {
        lock(gate)
        {
            if(closed || held is not null)return false;
            held=open();
            return true;
        }
    }

    /// <summary>The journal folded in, then the kept connection closed; nothing when none is kept. A checkpoint that fails (busy) is left
    /// to SQLite (the next start folds it in); the connection is closed whatever happens.</summary>
    public void Close()
    {
        SqliteConnection? kept;
        lock(gate){closed=true;kept=held;held=null;}
        if(kept is null)return;
        try{TenshoDataDatabase.Run(kept,"PRAGMA wal_checkpoint(TRUNCATE);");}
        catch(SqliteException){}
        finally{kept.Dispose();}
    }
}
