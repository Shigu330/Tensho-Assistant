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
    /// <paramref name="all"/> (a base cleared on purpose), in one transaction: either the whole save is in the file or none of it.
    /// </summary>
    public void Write(IReadOnlyCollection<DiscoveryFact> write,IReadOnlyCollection<string> remove,bool all)
    {
        using var open=Open();
        using var transaction=open.BeginTransaction();
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
