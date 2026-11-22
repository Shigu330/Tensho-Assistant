using Microsoft.Data.Sqlite;

namespace TenshoAssistant.Inventory;

/// <summary>
/// g, I-03 (0.345, user's choice: one file per character): the bank kept as one file per character, InventoryBank/&lt;id&gt;.json,
/// each read and written by its own <see cref="FileInventoryBankStore"/> (same bounds and checks, .bak copy, a corrupt file fails
/// closed). The id is the local character id the game gives the logged-in character (ContentId), never an Account ID.
/// X-141 (0.1118, audit A02): two game clients on the same profile share this folder. Every change is targeted (Apply): under a lock file
/// of the profile (InventoryBank.lock, beside the folder, held by one process at a time), only the character concerned is read again from
/// its file, changed (one storage written or forgotten) and written back, so what the other client saved is kept; a character's files
/// are deleted only on the player's explicit « Erase this character ». A whole-list Save never deletes anything. Load runs under the same lock.
/// X-173 (0.1149, user's choice of 12/11): with the player's data file (<see cref="TenshoDataFile"/>, TenshoData.db), the bank and inventories are
/// kept there instead: one row per stored place, a character's rows replaced whole in one transaction (a stop in the middle leaves the last save
/// whole), still under the profile's lock and still one character at a time. A data file that is there but unusable is never replaced (the bank
/// is unavailable, nothing written); when it cannot be created, or none is given, the files stay in use as before.
/// X-574 (0.1793, the user's choice of 17/11): the files of before are no longer converted: the former shared file (InventoryBank.json, split
/// into the folder until then) is never read, and with the data file the folder is not either; each is left where it is, never moved nor
/// removed, and said once (<see cref="TenshoDataFile.LeftAside"/>). The copies set aside by the conversions of before (before-sqlite-*,
/// « .migrated ») are not looked at, except by « Erase this character » (<see cref="TenshoDataDatabase.PurgeSetAside"/>).
/// </summary>
public sealed class PerCharacterBankStore : IInventoryBankStore
{
    private readonly string folder;
    private readonly string legacy;
    private readonly int lockWaitMs;
    private readonly TenshoDataFile? data;
    private readonly object gate=new();
    private readonly Dictionary<ulong,FileInventoryBankStore> stores=[];
    private bool loadFailed;

    /// <param name="legacyFile">The former shared file (InventoryBank.json): X-574 (0.1793), never read; only said when it is still there.</param>
    /// <param name="lockWaitMs">How long a change waits for the other client's lock before it fails (nothing written).</param>
    /// <param name="data">X-173 (0.1149): the player's data file; null keeps the files.</param>
    public PerCharacterBankStore(string folder,string legacyFile,int lockWaitMs=10_000,TenshoDataFile? data=null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);ArgumentException.ThrowIfNullOrWhiteSpace(legacyFile);
        this.folder=Path.GetFullPath(folder);legacy=Path.GetFullPath(legacyFile);this.lockWaitMs=lockWaitMs;this.data=data;
    }

    public static string FileName(ulong character)=>character.ToString(System.Globalization.CultureInfo.InvariantCulture)+".json";

    /// <summary>X-141: the profile's lock file (beside the folder).</summary>
    public string LockPath=>Path.TrimEndingDirectorySeparator(folder)+".lock";

    /// <summary>X-173: « sqlite », « json » or « blocked » (see <see cref="TenshoDataFile.Mode"/>); « json » without a data file.</summary>
    public string Mode=>data?.Mode ?? "json";

    public BankEntry[] Load()
    {
        lock(gate)
        {
            try
            {
                return Locked(()=>
                {
                    BankEntry[] result;
                    if(data?.Database() is { } opened)
                    {
                        using var open=opened.Open();
                        data.LeftAside("Bank",OldFiles(withFolder:true));
                        result=[.. Rows(open,null)];
                    }
                    else
                    {
                        // X-574 (0.1793): the files of the folder are this mode's own; only the former shared file is left aside.
                        data?.LeftAside("Bank",OldFiles(withFolder:false));
                        var all=new List<BankEntry>();
                        foreach(var id in Characters())all.AddRange(Read(id));
                        result=all.ToArray();
                    }
                    InventoryBankService.ValidateEntries(result);
                    return result;
                });
            }
            // X-170 (0.1142): a lock held elsewhere read nothing: not a failed read (the service tries again); anything else protects the files.
            catch(BankLockBusyException){throw;}
            catch{loadFailed=true;throw;}
        }
    }

    /// <summary>X-141: the characters present in the list are written when their file differs; nothing is ever deleted here (erasing is Apply with Erase).</summary>
    public void Save(IReadOnlyList<BankEntry> entries)
    {
        lock(gate)
        {
            if(loadFailed)throw new InvalidOperationException("A failed bank load prevents overwriting saved data.");
            InventoryBankService.ValidateEntries(entries);
            Locked(()=>
            {
                if(data?.Database() is { } opened)
                {
                    using var open=opened.Open();
                    using var transaction=open.BeginTransaction();
                    foreach(var group in entries.GroupBy(entry=>entry.Key.CharacterId))
                    {
                        var list=group.ToArray();
                        if(!Rows(open,transaction,group.Key).SequenceEqual(list))WriteRows(open,transaction,group.Key,list);
                    }
                    transaction.Commit();
                    return 0;
                }
                foreach(var group in entries.GroupBy(entry=>entry.Key.CharacterId))
                {
                    var list=group.ToArray();
                    if(!Read(group.Key).SequenceEqual(list))Store(group.Key).Save(list);
                }
                return 0;
            });
        }
    }

    /// <summary>
    /// X-141: one change of one character under the profile's lock: its file read again (what the other client wrote stays), the storage
    /// written or forgotten, the file written only when it differs; Erase (the player's command) deletes its file and its copy. The entries
    /// to show: the other characters as this client has them, this one as now saved. X-173 (0.1149): the same with the character's rows of the
    /// data file, read again and written in one transaction.
    /// </summary>
    public BankEntry[] Apply(BankChange change,IReadOnlyList<BankEntry> memory)
    {
        var id=change.Character;
        if(id==0)throw new ArgumentException("Unknown character.");
        lock(gate)
        {
            if(loadFailed)throw new InvalidOperationException("A failed bank load prevents overwriting saved data.");
            // X-171 (0.1314): with the data file, every character as saved now is read back under the same lock: the bank shown by this client
            // holds what the other client saved since (before, the other characters stayed as this client had loaded them until a reload).
            BankEntry[]? everyone=null;
            var saved=Locked(()=>
            {
                if(data?.Database() is { } opened)
                {
                    using var open=opened.Open();
                    if(change.Erase!=0)
                    {
                        // X-414 (0.1386): the erase is the player's command and only removes rows: done first, whatever the other characters
                        // hold. X-418 (0.1391): the bank read back is shown only when it passes the checks (unchecked in 0.1380); otherwise the
                        // other characters as this client holds them (checked when they were shown, without the erased one), never a failure:
                        // the erase is done, and saying it failed kept the erased character on screen.
                        // X-420 (0.1398): the read back as a whole: a row of another character that cannot be read (written by another client
                        // or version: bad JSON, another schema, too big, key disagreeing with its entry) throws from Rows, not from the checks.
                        EraseEverywhere(open,id);
                        try{var readBack=Rows(open,null);InventoryBankService.ValidateEntries(readBack);everyone=readBack;}
                        catch(Exception error) when(error is ArgumentException or InvalidDataException or SqliteException){everyone=null;}
                        return Array.Empty<BankEntry>();
                    }
                    using var transaction=open.BeginTransaction();
                    var rowsBefore=change.Erase!=0 ? []:Rows(open,transaction,id);
                    var rowsAfter=change.Erase!=0 ? []:change.On(rowsBefore);
                    InventoryBankService.ValidateEntries(rowsAfter);
                    if(change.Erase!=0 || !StillSaved(change,rowsBefore) && !rowsBefore.SequenceEqual(rowsAfter))WriteRows(open,transaction,id,rowsAfter);
                    everyone=Rows(open,transaction);
                    // X-336 M12 (0.1380): checked before the commit: an invalid row makes the save fail as a whole, never after it was written.
                    // X-414 (0.1386): a row of another character that fails the checks (written by another client or version) fails this
                    // character's save too, nothing written: the same rows make Load fail closed (the bank unavailable, the files protected).
                    InventoryBankService.ValidateEntries(everyone);
                    transaction.Commit();
                    return rowsAfter;
                }
                if(change.Erase!=0)
                {
                    var path=Path.Combine(folder,FileName(id));
                    File.Delete(path);File.Delete(path+".bak");
                    stores.Remove(id);
                    TenshoDataDatabase.PurgeSetAside(Path.GetDirectoryName(folder)!,id);
                    return Array.Empty<BankEntry>();
                }
                var before=Read(id);
                var after=change.On(before);
                InventoryBankService.ValidateEntries(after);
                if(!StillSaved(change,before) && !before.SequenceEqual(after))Store(id).Save(after);
                return after;
            });
            if(everyone is not null)return everyone;
            var result=memory.Where(entry=>entry.Key.CharacterId!=id).Concat(saved).ToArray();
            InventoryBankService.ValidateEntries(result);
            return result;
        }
    }

    /// <summary>
    /// X-185 (0.1211, night review N10): the player's « Erase this character »: its rows of every family kept per character (storages, gil journal,
    /// achievement progress) deleted in one transaction with secure_delete on (the freed pages wiped), the journal folded in, then the copies of its
    /// files of before removed from every before-sqlite-* folder. The former files shared by every character are kept (the tooltip says so).
    /// </summary>
    private void EraseEverywhere(SqliteConnection open,ulong id)
    {
        var secure=System.Convert.ToInt64(TenshoDataDatabase.Scalar(open,"PRAGMA secure_delete;"),System.Globalization.CultureInfo.InvariantCulture);
        TenshoDataDatabase.Run(open,"PRAGMA secure_delete=ON;");
        try
        {
            using(var transaction=open.BeginTransaction())
            {
                TenshoDataDatabase.EraseCharacter(open,transaction,id);
                transaction.Commit();
            }
            TenshoDataDatabase.Run(open,"PRAGMA wal_checkpoint(TRUNCATE);");
        }
        finally{TenshoDataDatabase.Run(open,$"PRAGMA secure_delete={(secure!=0 ? "ON":"OFF")};");}
        stores.Remove(id);
        TenshoDataDatabase.PurgeSetAside(Path.GetDirectoryName(folder)!,id);
    }

    /// <summary>One character's entries as on disk (a file holding another character's entries fails closed).</summary>
    private BankEntry[] Read(ulong id)
    {
        var entries=Store(id).Load();
        if(entries.Any(entry=>entry.Key.CharacterId!=id))throw new InvalidDataException("A character's bank file holds another character's entries.");
        return entries;
    }

    /// <summary>
    /// X-182 (0.1173, night review N26): « unchanged, not written again » decided here, against what is saved (under the lock), never against the
    /// client's memory: the same storage stored with the same contents and a date under an hour older. Another client that forgot or changed it
    /// meanwhile makes the reading written.
    /// </summary>
    private static bool StillSaved(BankChange change,IReadOnlyList<BankEntry> stored)
        => change.SkipWhenSame && change.Put is { } put && stored.FirstOrDefault(entry=>entry.Key==put.Key) is { } kept && BankEntryCompare.StillSaved(kept,put);

    // A character's rows replaced whole (none: the character erased), in the caller's transaction.
    private static void WriteRows(SqliteConnection open,SqliteTransaction transaction,ulong id,IReadOnlyList<BankEntry> entries)
    {
        if(entries.Any(entry=>entry.Key.CharacterId!=id))throw new ArgumentException("Entries of another character.");
        var rows=FileInventoryBankStore.EntryRows(entries);
        using(var delete=open.CreateCommand())
        {
            delete.Transaction=transaction;delete.CommandText="DELETE FROM bank_entries WHERE character=$character;";
            delete.Parameters.AddWithValue("$character",unchecked((long)id));
            delete.ExecuteNonQuery();
        }
        using var insert=open.CreateCommand();
        insert.Transaction=transaction;
        insert.CommandText="INSERT INTO bank_entries(character,kind,storage,seq,entry) VALUES($character,$kind,$storage,$seq,$entry);";
        var character=insert.Parameters.Add("$character",SqliteType.Integer);var kind=insert.Parameters.Add("$kind",SqliteType.Integer);
        var storage=insert.Parameters.Add("$storage",SqliteType.Integer);var seq=insert.Parameters.Add("$seq",SqliteType.Integer);
        var entry=insert.Parameters.Add("$entry",SqliteType.Text);
        for(var index=0;index<entries.Count;index++)
        {
            var key=entries[index].Key;
            character.Value=unchecked((long)id);kind.Value=(long)key.Kind;storage.Value=unchecked((long)key.StorageId);seq.Value=(long)index;entry.Value=rows[index];
            insert.ExecuteNonQuery();
        }
    }

    // The rows of one character (or of all, in character order), each checked as a bank file is; a row whose key columns disagree with its entry fails closed.
    private static BankEntry[] Rows(SqliteConnection open,SqliteTransaction? transaction,ulong? id=null)
    {
        using var command=open.CreateCommand();
        command.Transaction=transaction;
        command.CommandText="SELECT character,kind,storage,entry FROM bank_entries"+(id is null ? "":" WHERE character=$character")+" ORDER BY character,seq;";
        if(id is { } one)command.Parameters.AddWithValue("$character",unchecked((long)one));
        var read=new List<(ulong Character,long Kind,ulong Storage,string Entry)>();
        using(var reader=command.ExecuteReader())
            while(reader.Read())read.Add((unchecked((ulong)reader.GetInt64(0)),reader.GetInt64(1),unchecked((ulong)reader.GetInt64(2)),reader.GetString(3)));
        var result=new List<BankEntry>();
        // Characters in the files' order (unsigned id; SQLite sorts the stored signed number).
        foreach(var group in read.GroupBy(row=>row.Character).OrderBy(group=>group.Key))
        {
            var entries=FileInventoryBankStore.FromRows([.. group.Select(row=>row.Entry)]);
            if(entries.Zip(group).Any(pair=>pair.First.Key!=new BankKey(pair.Second.Character,(BankStorageKind)pair.Second.Kind,pair.Second.Storage)))
                throw new InvalidDataException("A bank row's key disagrees with its entry.");
            result.AddRange(entries);
        }
        return [.. result];
    }

    /// <summary>
    /// X-141: work done while holding the profile's lock file (opened without sharing: the other client, or another store here, waits);
    /// tried again every 50 ms up to lockWaitMs (fair since X-310: <see cref="FairFileLock"/>), then <see cref="BankLockBusyException"/> (nothing changed; X-170, 0.1142: its own type, so the
    /// service can tell « the other client holds the lock » from a file that cannot be read). X-173 (0.1149): the data file kept busy by another
    /// writer past SQLite's wait is the same case (nothing changed, tried again).
    /// </summary>
    private T Locked<T>(Func<T> work)
    {
        // X-310 (0.1309): the fair lock: a client saving in a row lets the waiting client go first (it could be refused past its wait before).
        using var held=FairFileLock.Hold(LockPath,lockWaitMs,error=>new BankLockBusyException(LockPath,error));
        try{return work();}
        catch(SqliteException error) when(error.SqliteErrorCode is 5 or 6){throw new BankLockBusyException(LockPath,error);}
    }

    private FileInventoryBankStore Store(ulong id)
    {
        if(!stores.TryGetValue(id,out var store))stores[id]=store=new FileInventoryBankStore(Path.Combine(folder,FileName(id)));
        return store;
    }

    // Characters with a file (or only its .bak) in the folder; other file names are ignored.
    private IEnumerable<ulong> Characters()
    {
        if(!Directory.Exists(folder))return [];
        return Directory.EnumerateFiles(folder,"*.json*")
            .Select(path=>Path.GetFileName(path))
            .Select(name=>name.EndsWith(".json.bak",StringComparison.Ordinal) ? name[..^4]:name)
            .Where(name=>name.EndsWith(".json",StringComparison.Ordinal))
            .Select(name=>ulong.TryParse(name[..^5],System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var id) ? id:0)
            .Where(id=>id!=0).Distinct().OrderBy(id=>id).ToArray();
    }

    /// <summary>
    /// X-574 (0.1793): the files of an old format still there, by name: the former shared file (and its copy « .bak »), and with the data file
    /// (<paramref name="withFolder"/>) the folder of one file per character when it holds anything. Looked at only, never opened; a folder that
    /// cannot be listed is left out (only a log line depends on it, never the load).
    /// </summary>
    private string[] OldFiles(bool withFolder)
    {
        var found=new List<string>();
        foreach(var path in new[]{legacy,legacy+".bak"})if(File.Exists(path))found.Add(Path.GetFileName(path));
        try{if(withFolder && Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())found.Add(Path.GetFileName(folder)+"/");}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){}
        return [.. found];
    }
}

/// <summary>X-170 (0.1142): the profile's bank lock stayed held by another process (the other game client, the desktop application) for the whole wait.</summary>
public sealed class BankLockBusyException(string path,Exception inner) : IOException($"The bank lock ({Path.GetFileName(path)}) is held by another program.",inner);
