using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TenshoAssistant;

/// <summary>
/// X-173 (0.1139, user's choice of 12/11: « two SQLite files, an erasable cache and the data to protect »): the player's data file,
/// <c>TenshoData.db</c>, at the root of the plugin's configuration folder. The families of X-173 move into it one at a time, each with its own
/// tables, its files of before converted once and kept aside: the gil journal (0.1139, <see cref="Inventory.GilJournalStore"/>), the achievement
/// progress (0.1140, <see cref="Collections.AchievementProgressData"/>), the voyage history (0.1148, <see cref="FreeCompany.VoyageHistoryData"/>), the bank and
/// inventories (0.1149, <see cref="Inventory.PerCharacterBankStore"/>). The discoveries keep their own file for now (Discoveries/Discoveries.db,
/// X-172; choice noted as D-141). WAL journal: a stop in the middle of a save leaves the last one whole, another process may read while the game
/// writes. Every call opens its own connection: safe from any thread.
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
    public string Path { get; }

    /// <summary>Opens (or creates) the file; throws when SQLite cannot be loaded or the file is of a newer layout or damaged.</summary>
    public TenshoDataDatabase(string path)
    {
        Path=System.IO.Path.GetFullPath(path);
        connection=new SqliteConnectionStringBuilder{DataSource=Path,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false}.ToString();
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
            Run(open,"CREATE TABLE IF NOT EXISTS gil_converted(character INTEGER PRIMARY KEY NOT NULL,at INTEGER NOT NULL);",transaction);
            // 0.1140: a family kept in one file before (« achievements »…), converted: never converted twice.
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

    /// <summary>Whether a family kept in one file before is already in the database.</summary>
    public static bool Converted(SqliteConnection open,string family,SqliteTransaction? transaction=null)
    {
        using var command=open.CreateCommand();command.Transaction=transaction;
        command.CommandText="SELECT COUNT(*) FROM converted WHERE family=$family;";
        command.Parameters.AddWithValue("$family",family);
        return System.Convert.ToInt64(command.ExecuteScalar(),CultureInfo.InvariantCulture)>0;
    }
    /// <summary>X-182 (0.1169): when a family was converted (its « converted » mark), or null when it is not.</summary>
    public static DateTimeOffset? ConvertedAt(SqliteConnection open,string family,SqliteTransaction? transaction=null)
    {
        using var command=open.CreateCommand();command.Transaction=transaction;
        command.CommandText="SELECT at FROM converted WHERE family=$family;";
        command.Parameters.AddWithValue("$family",family);
        return command.ExecuteScalar() is long at ? new DateTimeOffset(at,TimeSpan.Zero):null;
    }
    /// <summary>Marks a family converted, inside the transaction that wrote its rows.</summary>
    public static void MarkConverted(SqliteConnection open,SqliteTransaction transaction,string family,DateTimeOffset at)
    {
        using var command=open.CreateCommand();command.Transaction=transaction;
        command.CommandText="INSERT OR REPLACE INTO converted(family,at) VALUES($family,$at);";
        command.Parameters.AddWithValue("$family",family);command.Parameters.AddWithValue("$at",at.UtcTicks);
        command.ExecuteNonQuery();
    }

    /// <summary>The SQLite library's version (for the log).</summary>
    public string LibraryVersion(){using var open=Open();return Scalar(open,"SELECT sqlite_version();") as string ?? "";}

    /// <summary>
    /// X-182 (0.1169, night review N27): the journal written into the file and synced (a full checkpoint), so a conversion's commit survives a
    /// power cut before its file of before is moved; false when another connection kept it from finishing (the move then waits for the next start).
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
            // The application's own copy damaged (not a database, corrupt): it is only a copy, made again from scratch once.
            catch(SqliteException error) when(error.SqliteErrorCode is 11 or 26)
            {
                foreach(var path in new[]{to,to+"-wal",to+"-shm"})if(File.Exists(path))File.Delete(path);
                Backup(from,to);
            }
            return (1,problems);
        }
        catch(SqliteException error){problems.Add($"{name}: not copied (SQLite error {error.SqliteErrorCode})");}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){problems.Add($"{name}: not copied ({error.GetType().Name})");}
        return (0,problems);
    }

    private static void Backup(string from,string to)
    {
        using var reader=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=from,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());
        reader.Open();
        Run(reader,"PRAGMA busy_timeout=5000;");
        using var writer=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=to,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false}.ToString());
        writer.Open();
        Run(writer,"PRAGMA busy_timeout=5000;");
        reader.BackupDatabase(writer);
    }
}

/// <summary>
/// X-173 (0.1140): the player's data file as the plugin uses it, one for every family: opened on first use (off the frame), once. A file that
/// exists but cannot be opened (SQLite not loaded, damaged, newer layout) is never replaced: every family is then unavailable and writes nothing
/// this session (<see cref="Database"/> throws); when the file cannot be created at all, <see cref="Database"/> gives null and each family keeps
/// its JSON files as before.
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

    /// <summary>
    /// X-182 (0.1169, night review N3): a file of before found again after its family was converted, written after the conversion (by an older
    /// version of the plugin, or another client still running one) and not a copy already set aside: its entries are merged, never dropped.
    /// </summary>
    public static bool WrittenAgain(string file,DateTimeOffset converted)
    {
        if(!File.Exists(file) || File.GetLastWriteTimeUtc(file)<=converted.UtcDateTime)return false;
        var length=new FileInfo(file).Length;
        var name=System.IO.Path.GetFileNameWithoutExtension(file);var extension=System.IO.Path.GetExtension(file);
        foreach(var aside in Directory.EnumerateDirectories(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(file))!,"before-sqlite-*"))
            foreach(var kept in Directory.EnumerateFiles(aside,name+"*"+extension))
                if(new FileInfo(kept).Length==length && File.ReadAllBytes(kept).AsSpan().SequenceEqual(File.ReadAllBytes(file)))return false;
        return true;
    }

    /// <summary>
    /// A file of before moved beside itself into before-sqlite-yyyyMMdd-HHmmss/, never removed (a name already taken there gets a suffix). A move
    /// that fails is said and retried at the next start (the « converted » mark keeps the file from being read again). 0.1149: a folder of before
    /// (the bank's InventoryBank/) is moved the same way, whole.
    /// </summary>
    public void SetAside(string file,DateTimeOffset now,string what)
    {
        try
        {
            file=System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(file));
            var folder=Directory.Exists(file);
            // X-182 (0.1169): already moved by another client; N27: never moved before the rows that replace it are synced to the disk.
            if(!folder && !File.Exists(file))return;
            TenshoDataDatabase? opened;
            lock(gate)opened=blocked is null ? database:null;
            if(opened is not null && !opened.Durable()){info?.Invoke($"Tensho data: the {what} file of before stays until the next start (the data file was busy).");return;}
            var aside=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file)!,$"before-sqlite-{now.ToUniversalTime():yyyyMMdd-HHmmss}");
            Directory.CreateDirectory(aside);
            var target=System.IO.Path.Combine(aside,System.IO.Path.GetFileName(file));
            if(File.Exists(target) || Directory.Exists(target))
                target=System.IO.Path.Combine(aside,System.IO.Path.GetFileNameWithoutExtension(file)+"-"+Guid.NewGuid().ToString("N")[..8]+System.IO.Path.GetExtension(file));
            if(folder)Directory.Move(file,target);else File.Move(file,target);
            info?.Invoke($"Tensho data: the {what} file of before moved to {TenshoDataDatabase.FileName}, kept in {System.IO.Path.GetFileName(aside)}.");
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException)
        {
            warn(error,$"The {what} file of before could not be set aside; it is not read again.");
        }
    }
}
