using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-172 (0.1136, user's choice of 12/11, trial docs/evidence/x172-sqlite-trial.md): the discoveries in one SQLite file, <c>Discoveries.db</c> (data to
/// protect, beside the plugin's settings): one row per fact, keyed by its <see cref="DiscoveryFact.Key"/>, the fact itself as JSON (the same shape as the
/// exchange files), its kind and whether it is shared (imported, not seen by you) as columns, so a kind is read alone. A save writes only the rows
/// changed since the last one (<see cref="DiscoveryStore.ChangesToSave"/>), in one transaction: a stop in the middle leaves the last save whole (WAL
/// journal). Another process (the desktop application) may read it while the game writes. Every method opens its own connection: safe from any thread.
/// </summary>
public sealed class DiscoveryDatabase
{
    public const string FileName="Discoveries.db";
    /// <summary>The layout this version writes (PRAGMA user_version); a newer one is refused, never written over.</summary>
    public const int Schema=1;
    private static readonly JsonSerializerOptions Options=TenshoJson.Own(new(){WriteIndented=false});
    private readonly string connection;
    public string Path { get; }

    /// <summary>Opens (or creates) the database; throws when SQLite cannot be loaded or the file is newer or damaged.</summary>
    public DiscoveryDatabase(string path)
    {
        Path=System.IO.Path.GetFullPath(path);
        connection=new SqliteConnectionStringBuilder{DataSource=Path,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false}.ToString();
        using var open=Open();
        Run(open,"PRAGMA journal_mode=WAL;");
        var version=Convert.ToInt32(Scalar(open,"PRAGMA user_version;"));
        if(version>Schema)throw new InvalidDataException($"Discoveries database of a newer layout ({version}).");
        using var transaction=open.BeginTransaction();
        Run(open,"CREATE TABLE IF NOT EXISTS facts(key TEXT PRIMARY KEY NOT NULL,kind INTEGER NOT NULL,shared INTEGER NOT NULL,fact TEXT NOT NULL) WITHOUT ROWID;",transaction);
        Run(open,"CREATE INDEX IF NOT EXISTS facts_kind ON facts(kind,shared);",transaction);
        Run(open,$"PRAGMA user_version={Schema};",transaction);
        transaction.Commit();
        if(Scalar(open,"PRAGMA quick_check;") is not "ok")throw new InvalidDataException("Discoveries database damaged (quick_check).");
    }

    private SqliteConnection Open()
    {
        var open=new SqliteConnection(connection);
        open.Open();
        Run(open,"PRAGMA busy_timeout=5000;PRAGMA synchronous=NORMAL;");
        return open;
    }
    private static void Run(SqliteConnection open,string sql,SqliteTransaction? transaction=null)
    {
        using var command=open.CreateCommand();command.CommandText=sql;command.Transaction=transaction;command.ExecuteNonQuery();
    }
    private static object? Scalar(SqliteConnection open,string sql)
    {
        using var command=open.CreateCommand();command.CommandText=sql;return command.ExecuteScalar();
    }

    /// <summary>The SQLite library's version (for the log).</summary>
    public string LibraryVersion(){using var open=Open();return Scalar(open,"SELECT sqlite_version();") as string ?? "";}

    /// <summary>Your facts (seen by you), read whole at start; a row that cannot be read is reported and left in the file.</summary>
    public DiscoveryFile ReadMine(Action<string,Exception>? broken=null)=>Read("SELECT key,fact FROM facts WHERE shared=0;",null,broken);
    /// <summary>The shared facts of one kind (read when a page asks for that kind, X-93).</summary>
    public DiscoveryFile ReadShared(DiscoveryKind kind,Action<string,Exception>? broken=null)=>Read("SELECT key,fact FROM facts WHERE kind=$kind AND shared=1;",(int)kind,broken);
    /// <summary>
    /// X-619 M1 (review of 0.1557): rows that could not be read, moved into the table <c>facts_aside</c> (key, kind, shared, the text as it
    /// was, the time), in one transaction. A row read again meanwhile and readable now (another client wrote it) stays. Before, a damaged row
    /// stayed in <c>facts</c> and marked your facts incomplete at every start: nothing was ever sent to your gist again. The text is kept,
    /// never deleted. Returns how many rows were moved.
    /// </summary>
    public int SetAside(IReadOnlyCollection<string> keys,DateTimeOffset now)
    {
        if(keys.Count==0)return 0;
        using var open=Open();
        using var transaction=open.BeginTransaction(deferred:false);
        Run(open,"CREATE TABLE IF NOT EXISTS facts_aside(key TEXT NOT NULL,kind INTEGER NOT NULL,shared INTEGER NOT NULL,fact TEXT NOT NULL,at TEXT NOT NULL);",transaction);
        using var read=open.CreateCommand();read.Transaction=transaction;
        read.CommandText="SELECT fact FROM facts WHERE key=$key;";
        var readKey=read.Parameters.Add("$key",SqliteType.Text);
        using var copy=open.CreateCommand();copy.Transaction=transaction;
        copy.CommandText="INSERT INTO facts_aside(key,kind,shared,fact,at) SELECT key,kind,shared,fact,$at FROM facts WHERE key=$key;";
        var copyKey=copy.Parameters.Add("$key",SqliteType.Text);copy.Parameters.AddWithValue("$at",now.ToString("O"));
        using var delete=open.CreateCommand();delete.Transaction=transaction;
        delete.CommandText="DELETE FROM facts WHERE key=$key;";
        var deleteKey=delete.Parameters.Add("$key",SqliteType.Text);
        var moved=0;
        foreach(var key in keys)
        {
            readKey.Value=key;
            if(read.ExecuteScalar() is not string text || Readable(text))continue;
            copyKey.Value=key;copy.ExecuteNonQuery();
            deleteKey.Value=key;delete.ExecuteNonQuery();
            moved++;
        }
        transaction.Commit();
        return moved;
    }
    private static bool Readable(string text)
    {
        try{JsonSerializer.Deserialize<DiscoveryFact>(text,Options);return true;}
        catch(JsonException){return false;}
    }
    /// <summary>X-619 M1: how many rows were set aside (0 when the table does not exist).</summary>
    public long CountAside()
    {
        using var open=Open();
        return Scalar(open,"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='facts_aside';") is long and > 0 ? Convert.ToInt64(Scalar(open,"SELECT COUNT(*) FROM facts_aside;")):0;
    }

    // X-621 (review of 0.1560, I1): a row set aside is settled once its copy on your gist, if any, is back in facts (restored=1) or there is
    // none to keep (restored=0); the text set aside stays in facts_aside either way.
    private const string AsideDoneTable="CREATE TABLE IF NOT EXISTS facts_aside_done(key TEXT PRIMARY KEY NOT NULL,at TEXT NOT NULL,restored INTEGER NOT NULL) WITHOUT ROWID;";
    private static bool HasTable(SqliteConnection open,string name,SqliteTransaction? transaction=null)
    {
        using var command=open.CreateCommand();command.Transaction=transaction;
        command.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;";command.Parameters.AddWithValue("$name",name);
        return command.ExecuteScalar() is long and > 0;
    }
    /// <summary>
    /// X-621 (review of 0.1560, I1): the keys of your rows set aside (facts_aside, not shared) and not settled yet: your gist may still hold a
    /// valid copy of them, which a send would otherwise drop (the gist is replaced by your facts in memory). Read at start; the next send reads
    /// those copies back first (GitHubExchange, DiscoveryStore.Aside).
    /// </summary>
    public IReadOnlyList<string> AsidePending()
    {
        using var open=Open();
        if(!HasTable(open,"facts_aside"))return [];
        using var select=open.CreateCommand();
        select.CommandText=HasTable(open,"facts_aside_done")
            ? "SELECT DISTINCT key FROM facts_aside WHERE shared=0 AND key NOT IN (SELECT key FROM facts_aside_done) ORDER BY key;"
            : "SELECT DISTINCT key FROM facts_aside WHERE shared=0 ORDER BY key;";
        var keys=new List<string>();
        using var reader=select.ExecuteReader();
        while(reader.Read())keys.Add(reader.GetString(0));
        return keys;
    }
    /// <summary>
    /// X-621 (review of 0.1560, I1): the rows of <paramref name="keys"/> settled, in one transaction that takes the write lock at once (a second
    /// client finds them settled and joins nothing twice). A key's copy read back from your gist (<paramref name="kept"/>) is joined to its row as
    /// it is now (the sightings made since the set-aside added to those of before, DiscoveryStore.Join), or put in when there is none (or it cannot
    /// be read). Returns the copies joined, for the memory to join them the same way (DiscoveryStore.JoinSaved); a key already settled is left out.
    /// </summary>
    /// <remarks>X-624 M2 (review of 0.1562): a key another client of this profile settled with its copy (restored=1) is not written again, but
    /// its copy read here is returned too: this client's memory lacked it (its row was set aside at its start) and its next send replaced the gist
    /// without it. A key settled with nothing kept (restored=0) is still left out: the row on disk has no copy either.</remarks>
    public IReadOnlyList<DiscoveryFact> RestoreAside(IReadOnlyCollection<string> keys,IReadOnlyCollection<DiscoveryFact> kept,DateTimeOffset now)
    {
        var joined=new List<DiscoveryFact>();
        if(keys.Count==0)return joined;
        var byKey=new Dictionary<string,DiscoveryFact>(StringComparer.Ordinal);
        foreach(var fact in kept)if(DiscoveryStore.Valid(fact) && fact.Seen>0)byKey.TryAdd(fact.Key,fact);
        using var open=Open();
        using var transaction=open.BeginTransaction(deferred:false);
        Run(open,AsideDoneTable,transaction);
        using var settled=open.CreateCommand();settled.Transaction=transaction;
        settled.CommandText="SELECT MAX(restored) FROM facts_aside_done WHERE key=$key;";
        var settledKey=settled.Parameters.Add("$key",SqliteType.Text);
        using var select=open.CreateCommand();select.Transaction=transaction;
        select.CommandText="SELECT fact FROM facts WHERE key=$key;";
        var selectKey=select.Parameters.Add("$key",SqliteType.Text);
        using var done=open.CreateCommand();done.Transaction=transaction;
        done.CommandText="INSERT INTO facts_aside_done(key,at,restored) VALUES($key,$at,$restored);";
        var doneKey=done.Parameters.Add("$key",SqliteType.Text);var restored=done.Parameters.Add("$restored",SqliteType.Integer);
        done.Parameters.AddWithValue("$at",now.ToString("O"));
        var write=new List<DiscoveryFact>();
        foreach(var key in keys.Distinct(StringComparer.Ordinal))
        {
            settledKey.Value=key;
            var copy=byKey.GetValueOrDefault(key);
            if(settled.ExecuteScalar() is long elsewhere){if(elsewhere>0 && copy is not null)joined.Add(copy.Copy());continue;}
            if(copy is not null)
            {
                selectKey.Value=key;
                DiscoveryFact? row=null;
                if(select.ExecuteScalar() is string text)try{row=JsonSerializer.Deserialize<DiscoveryFact>(text,Options);}catch(JsonException){}
                if(row is null)write.Add(copy.Copy());
                else{DiscoveryStore.Join(row,copy);write.Add(row);}
                joined.Add(copy.Copy());
            }
            doneKey.Value=key;restored.Value=copy is null ? 0:1;done.ExecuteNonQuery();
        }
        if(write.Count>0)Upsert(open,transaction,write);
        transaction.Commit();
        return joined;
    }

    /// <summary>The kinds that have shared facts in the file.</summary>
    public IReadOnlyList<DiscoveryKind> SharedKinds()
    {
        using var open=Open();using var command=open.CreateCommand();
        command.CommandText="SELECT DISTINCT kind FROM facts WHERE shared=1;";
        var kinds=new List<DiscoveryKind>();
        using var reader=command.ExecuteReader();
        while(reader.Read())if(Enum.IsDefined((DiscoveryKind)reader.GetInt32(0)))kinds.Add((DiscoveryKind)reader.GetInt32(0));
        return kinds;
    }
    /// <summary>How many rows (yours, shared).</summary>
    public (long Mine,long Shared) Count()
    {
        using var open=Open();
        return (Convert.ToInt64(Scalar(open,"SELECT COUNT(*) FROM facts WHERE shared=0;")),Convert.ToInt64(Scalar(open,"SELECT COUNT(*) FROM facts WHERE shared=1;")));
    }

    private DiscoveryFile Read(string sql,int? kind,Action<string,Exception>? broken)
    {
        using var open=Open();using var command=open.CreateCommand();
        command.CommandText=sql;
        if(kind is { } value)command.Parameters.AddWithValue("$kind",value);
        var facts=new List<DiscoveryFact>();
        using var reader=command.ExecuteReader();
        while(reader.Read())
        {
            var key=reader.GetString(0);
            try{if(JsonSerializer.Deserialize<DiscoveryFact>(reader.GetString(1),Options) is { } fact)facts.Add(fact);}
            catch(JsonException error){broken?.Invoke(key,error);}
        }
        return new DiscoveryFile{Written=DateTimeOffset.UtcNow,Facts=facts};
    }

    /// <summary>
    /// One save: the rows of <paramref name="write"/> put in (or replaced), those of <paramref name="remove"/> taken out, everything else first when
    /// <paramref name="all"/> (a base cleared on purpose), in one transaction: either the whole save is in the file or none of it. X-171 (0.1313,
    /// « add both »): a fact given in <paramref name="before"/> (seen by this client since its last save) is added to the row as it is now, read in
    /// the same transaction (the other client may have written it since): DiscoveryStore.AddMine. The transaction takes the write lock at once
    /// (BEGIN IMMEDIATE), so two clients never read the same row and both write it.
    /// </summary>
    public void Write(IReadOnlyCollection<DiscoveryFact> write,IReadOnlyCollection<string> remove,bool all,IReadOnlyDictionary<string,DiscoveryFact?>? before=null)
    {
        using var open=Open();
        using var transaction=open.BeginTransaction(deferred:false);
        if(before is { Count:>0 } && !all)write=AddedToSaved(open,transaction,write,before);
        if(all)Run(open,"DELETE FROM facts;",transaction);
        if(remove.Count>0)
        {
            using var delete=open.CreateCommand();delete.Transaction=transaction;
            delete.CommandText="DELETE FROM facts WHERE key=$key;";
            var key=delete.Parameters.Add("$key",SqliteType.Text);
            foreach(var gone in remove){key.Value=gone;delete.ExecuteNonQuery();}
        }
        if(write.Count>0)Upsert(open,transaction,write);
        transaction.Commit();
    }
    // X-171 (0.1313): each fact seen by this client since its last save, added to its row as saved now (a row that cannot be read is replaced as before).
    private static List<DiscoveryFact> AddedToSaved(SqliteConnection open,SqliteTransaction transaction,IReadOnlyCollection<DiscoveryFact> write,IReadOnlyDictionary<string,DiscoveryFact?> before)
    {
        using var select=open.CreateCommand();select.Transaction=transaction;
        select.CommandText="SELECT fact FROM facts WHERE key=$key;";
        var key=select.Parameters.Add("$key",SqliteType.Text);
        var result=new List<DiscoveryFact>(write.Count);
        foreach(var fact in write)
        {
            if(!before.TryGetValue(fact.Key,out var was)){result.Add(fact);continue;}
            key.Value=fact.Key;
            DiscoveryFact? stored=null;
            if(select.ExecuteScalar() is string text)try{stored=JsonSerializer.Deserialize<DiscoveryFact>(text,Options);}catch(JsonException){}
            result.Add(stored is null ? fact:DiscoveryStore.AddMine(stored,fact,was));
        }
        return result;
    }
    private static void Upsert(SqliteConnection open,SqliteTransaction transaction,IEnumerable<DiscoveryFact> write)
    {
        using var upsert=open.CreateCommand();upsert.Transaction=transaction;
        upsert.CommandText="INSERT INTO facts(key,kind,shared,fact) VALUES($key,$kind,$shared,$fact) ON CONFLICT(key) DO UPDATE SET kind=excluded.kind,shared=excluded.shared,fact=excluded.fact;";
        var key=upsert.Parameters.Add("$key",SqliteType.Text);var kind=upsert.Parameters.Add("$kind",SqliteType.Integer);
        var shared=upsert.Parameters.Add("$shared",SqliteType.Integer);var json=upsert.Parameters.Add("$fact",SqliteType.Text);
        foreach(var fact in write)
        {
            key.Value=fact.Key;kind.Value=(int)fact.Kind;shared.Value=fact.Seen==0 ? 1:0;
            json.Value=JsonSerializer.Serialize(fact.Seen==0 && fact.Own is not null ? WithoutOwn(fact):fact,Options);
            upsert.ExecuteNonQuery();
        }
    }

    private static DiscoveryFact WithoutOwn(DiscoveryFact fact){var copy=fact.Copy();copy.Own=null;return copy;}

    /// <summary>The journal folded into the file (at unload), so the folder holds one file when the game is closed.</summary>
    public void Checkpoint(){using var open=Open();Run(open,"PRAGMA wal_checkpoint(TRUNCATE);");}
}
