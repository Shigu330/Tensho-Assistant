using Microsoft.Data.Sqlite;

namespace TenshoAssistant;

/// <summary>
/// X-173 (0.1139, user's choice of 12/11: « two SQLite files, an erasable cache and the data to protect »): the player's data file,
/// <c>TenshoData.db</c>, at the root of the plugin's configuration folder. The families of X-173 move into it one at a time, each with its own
/// tables, its files of before converted once and kept aside; this version holds the gil journal (<see cref="Inventory.GilJournalStore"/>). The
/// discoveries keep their own file for now (Discoveries/Discoveries.db, X-172; choice noted as D-141). WAL journal: a stop in the middle of a
/// save leaves the last one whole, another process may read while the game writes. Every call opens its own connection: safe from any thread.
/// </summary>
public sealed class TenshoDataDatabase
{
    public const string FileName="TenshoData.db";
    /// <summary>The layout this version writes (PRAGMA user_version): 1 = gil journal. A newer one is refused, never written over.</summary>
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
        var version=System.Convert.ToInt32(Scalar(open,"PRAGMA user_version;"),System.Globalization.CultureInfo.InvariantCulture);
        if(version>Schema)throw new InvalidDataException($"Tensho data file of a newer layout ({version}).");
        using(var transaction=open.BeginTransaction())
        {
            // The gil journal: one row per entry, numbered per character in the journal's order (oldest first); a character's id is stored as the
            // signed 64-bit number of the same bits (SQLite integers are signed).
            Run(open,"CREATE TABLE IF NOT EXISTS gil_entries(character INTEGER NOT NULL,seq INTEGER NOT NULL,at INTEGER NOT NULL,delta INTEGER NOT NULL,"
                +"balance INTEGER NOT NULL,source INTEGER NOT NULL,category INTEGER NOT NULL,PRIMARY KEY(character,seq)) WITHOUT ROWID;",transaction);
            // A character whose journal file of before is in the table (written in the same transaction as its rows): never converted twice.
            Run(open,"CREATE TABLE IF NOT EXISTS gil_converted(character INTEGER PRIMARY KEY NOT NULL,at INTEGER NOT NULL);",transaction);
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
    public string LibraryVersion(){using var open=Open();return Scalar(open,"SELECT sqlite_version();") as string ?? "";}

    /// <summary>The journal folded into the file (at unload), so the folder holds one file when the game is closed.</summary>
    public void Checkpoint(){using var open=Open();Run(open,"PRAGMA wal_checkpoint(TRUNCATE);");}
}
