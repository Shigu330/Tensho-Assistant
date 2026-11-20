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
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=false};
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

    /// <summary>
    /// X-182 (0.1173, night review N3): facts of a Discoveries.json written again after the conversion, merged in one transaction: a cell the file
    /// alone knows is added; a cell in both keeps the fact seen more often (the latest last sighting when even), never the two added up: the older
    /// version may have saved its whole base read before the conversion, which the database already holds (counts never doubled; a few sightings
    /// of a cell seen by both clients may be lost, never a cell). Facts that fail the checks are left out. Returns how many rows were written.
    /// </summary>
    public int MergeFound(IEnumerable<DiscoveryFact> found)
    {
        using var open=Open();
        using var transaction=open.BeginTransaction();
        using var select=open.CreateCommand();select.Transaction=transaction;
        select.CommandText="SELECT fact FROM facts WHERE key=$key;";
        var key=select.Parameters.Add("$key",SqliteType.Text);
        var write=new List<DiscoveryFact>();
        foreach(var fact in found)
        {
            if(!DiscoveryStore.Valid(fact))continue;
            key.Value=fact.Key;
            DiscoveryFact? known=null;
            if(select.ExecuteScalar() is string text)try{known=JsonSerializer.Deserialize<DiscoveryFact>(text,Options);}catch(JsonException){}
            if(known is null || fact.Seen>known.Seen || fact.Seen==known.Seen && fact.Last>known.Last)write.Add(fact);
        }
        Upsert(open,transaction,write);
        transaction.Commit();
        return write.Count;
    }
    private static DiscoveryFact WithoutOwn(DiscoveryFact fact){var copy=fact.Copy();copy.Own=null;return copy;}

    /// <summary>The journal folded into the file (at unload), so the folder holds one file when the game is closed.</summary>
    public void Checkpoint(){using var open=Open();Run(open,"PRAGMA wal_checkpoint(TRUNCATE);");}

    /// <summary>
    /// X-172: the base of before 0.1136 (Discoveries.json and Shared/kind-NN.json.gz) turned into <see cref="FileName"/>, once: read whole (a fact in
    /// both files joined as the store does), written to a temporary database put in place only once complete, then the old files moved, never
    /// removed, into « before-sqlite-yyyyMMdd-HHmmss » beside it. A stop at any step leaves either the old files or the database whole: the next
    /// start goes on from there. Returns the folder of the old files when it moved some.
    /// </summary>
    /// <param name="merged">X-182 (0.1173, N3): told how many facts of a Discoveries.json written again after the conversion were merged.</param>
    /// <param name="stayed">X-182 (0.1197): told when a file of before could not be moved aside for now (held by another program): it stays, the next start moves it.</param>
    /// <param name="between">The test's stop between the temporary database and its move into place (given the temporary path).</param>
    public static string? ConvertFiles(string folder,DateTimeOffset now,Action<string,Exception> broken,Action<int>? merged=null,Action<string,Exception>? stayed=null,Action<string>? between=null)
    {
        var target=System.IO.Path.Combine(folder,FileName);
        var main=System.IO.Path.Combine(folder,"Discoveries.json");var shared=DiscoveryFiles.SharedFolder(main);
        // X-182 (0.1169, night review N11): leftovers of a stopped conversion removed once they are old. 0.1197: at every start (a lost race
        // whose temporary file could not be removed at once had no later conversion to remove it), and never an error.
        if(Directory.Exists(folder))
            foreach(var leftover in Directory.EnumerateFiles(folder,FileName+".converting*"))
                try{if(DateTime.UtcNow-File.GetLastWriteTimeUtc(leftover)>TimeSpan.FromMinutes(30))File.Delete(leftover);}
                catch(IOException){}catch(UnauthorizedAccessException){}
        var old=File.Exists(main) || HasKinds(shared);
        if(!File.Exists(target) && old)
        {
            // X-182 (0.1169, N11): a temporary name of this conversion only (a second client converting at the same time never removes it);
            // the result moved without overwriting: the first one in stays.
            var temporary=target+".converting-"+Guid.NewGuid().ToString("N");
            var store=new DiscoveryStore();
            Exception? busy=null;
            var read=DiscoveryFiles.ReadBase(main,now,broken);
            if(read.File is { } file)store.Load(file);
            foreach(var kind in DiscoveryFiles.ReadShared(main,broken,(_,error)=>busy??=error))store.LoadShared(kind);
            // X-182 (0.1197): the files of before are moved aside only once a database is in place, so a file that went away while this
            // client read it means the other client's database is in: this one's, read from what was left, is never put in place (the move
            // below does not overwrite). A file held for now: nothing is converted from a part of the base; the next start tries again.
            if((read.Unsafe || busy is not null) && !File.Exists(target))
                throw new IOException("A discoveries file of before could not be read nor set aside for now: nothing converted, the next start tries again.",busy);
            if(!File.Exists(target))
            {
                try
                {
                    var database=new DiscoveryDatabase(temporary);
                    database.Write(store.Snapshot(now).Facts,[],false);
                    database.Checkpoint();
                    SqliteConnection.ClearAllPools();
                    between?.Invoke(temporary);
                    AtomicFile.MoveWhenFree(temporary,target);
                }
                catch(IOException) when(File.Exists(target)){}
                // 0.1197: this conversion's own temporary files never stay in the way nor fail the conversion (what cannot be removed now is
                // a leftover, removed at a later start).
                finally{foreach(var mine in new[]{temporary,temporary+"-wal",temporary+"-shm"})AtomicFile.DeleteWhenFree(mine);}
            }
        }
        // X-182 (0.1173, night review N3): the database is there and Discoveries.json came back (an older version still running, or a build of an
        // older commit, found no file and saved again): written after the database (a minute of margin; merging is never additive) and not a copy
        // already set aside, its facts are merged before it is set aside. Read while another client moves it: nothing merged, nothing lost.
        else if(File.Exists(main) && TenshoDataFile.WrittenAgain(main,new DateTimeOffset(File.GetCreationTimeUtc(target),TimeSpan.Zero).AddMinutes(-1)))
        {
            var read=DiscoveryFiles.ReadBase(main,now,(path,error)=>{if(File.Exists(main))broken(path,error);});
            if(read.File is { } again)merged?.Invoke(new DiscoveryDatabase(target).MergeFound(again.Facts));
            // 0.1197: held for now: nothing merged, and the file is not set aside before it could be read: the next start merges it.
            else if(read.Unsafe && read.SetAside is null)return null;
        }
        if(!old)return null;
        var aside=System.IO.Path.Combine(folder,$"before-sqlite-{now.ToUniversalTime():yyyyMMdd-HHmmss}");
        var moved=false;
        // X-182 (0.1173): a name already taken there (two moves in one second) gets a suffix; the move is never refused for it.
        // 0.1197: a file of before that cannot be moved for now (the other client is still reading it, a scanner holds it) stays where it is:
        // the database is whole and the next start sets the file aside; the conversion never fails for it (a failure here, the database
        // in place, read as « the database could not be opened » and nothing was saved that session). The folder is made only for a move.
        try{if(File.Exists(main)){Directory.CreateDirectory(aside);File.Move(main,Free(System.IO.Path.Combine(aside,"Discoveries.json")));moved=true;}}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){if(File.Exists(main))stayed?.Invoke(main,error);}
        try{if(Directory.Exists(shared)){Directory.CreateDirectory(aside);Directory.Move(shared,Free(System.IO.Path.Combine(aside,"Shared")));moved=true;}}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){if(Directory.Exists(shared))stayed?.Invoke(shared,error);}
        return moved ? aside:null;
    }
    // 0.1197: the folder can be moved aside by the other client between the two looks.
    private static bool HasKinds(string shared)
    {
        try{return Directory.Exists(shared) && Directory.GetFiles(shared,"kind-*.json.gz").Length>0;}
        catch(DirectoryNotFoundException){return false;}
    }
    private static string Free(string path)
    {
        if(!File.Exists(path) && !Directory.Exists(path))return path;
        return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!,System.IO.Path.GetFileNameWithoutExtension(path)+"-"+Guid.NewGuid().ToString("N")[..8]+System.IO.Path.GetExtension(path));
    }
}
