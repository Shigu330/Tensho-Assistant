namespace TenshoAssistant.Inventory;

/// <summary>
/// g, I-03 (0.345, user's choice: one file per character): the bank kept as one file per character, InventoryBank/&lt;id&gt;.json,
/// each read and written by its own <see cref="FileInventoryBankStore"/> (same bounds and checks, .bak copy, a corrupt file fails
/// closed). The id is the local character id the game gives the logged-in character (ContentId), never an Account ID.
/// The former shared file (InventoryBank.json) is split once: written into a temporary folder, moved into place only when complete,
/// then kept beside as « .migrated »; a failed or interrupted split leaves the former file untouched and is redone at the next load.
/// X-141 (0.1118, audit A02): two game clients on the same profile share this folder. Every change is targeted (Apply): under a lock file
/// of the profile (InventoryBank.lock, beside the folder, held by one process at a time), only the character concerned is read again from
/// its file, changed (one storage written or forgotten) and written back, so what the other client saved is kept; a character's files
/// are deleted only on the player's explicit « Erase this character ». A whole-list Save never deletes anything. Load and the split of
/// the former file run under the same lock.
/// </summary>
public sealed class PerCharacterBankStore : IInventoryBankStore
{
    private readonly string folder;
    private readonly string legacy;
    private readonly int lockWaitMs;
    private readonly object gate=new();
    private readonly Dictionary<ulong,FileInventoryBankStore> stores=[];
    private bool loadFailed;

    /// <param name="lockWaitMs">How long a change waits for the other client's lock before it fails (nothing written).</param>
    public PerCharacterBankStore(string folder,string legacyFile,int lockWaitMs=10_000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);ArgumentException.ThrowIfNullOrWhiteSpace(legacyFile);
        this.folder=Path.GetFullPath(folder);legacy=Path.GetFullPath(legacyFile);this.lockWaitMs=lockWaitMs;
    }

    public static string FileName(ulong character)=>character.ToString(System.Globalization.CultureInfo.InvariantCulture)+".json";

    /// <summary>X-141: the profile's lock file (beside the folder: the folder's absence still means « not split yet »).</summary>
    public string LockPath=>Path.TrimEndingDirectorySeparator(folder)+".lock";

    public BankEntry[] Load()
    {
        lock(gate)
        {
            try
            {
                return Locked(()=>
                {
                    Migrate();
                    var all=new List<BankEntry>();
                    foreach(var id in Characters())all.AddRange(Read(id));
                    var result=all.ToArray();
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
    /// to show: the other characters as this client has them, this one as now saved.
    /// </summary>
    public BankEntry[] Apply(BankChange change,IReadOnlyList<BankEntry> memory)
    {
        var id=change.Character;
        if(id==0)throw new ArgumentException("Unknown character.");
        lock(gate)
        {
            if(loadFailed)throw new InvalidOperationException("A failed bank load prevents overwriting saved data.");
            var saved=Locked(()=>
            {
                if(change.Erase!=0)
                {
                    var path=Path.Combine(folder,FileName(id));
                    File.Delete(path);File.Delete(path+".bak");
                    stores.Remove(id);
                    return Array.Empty<BankEntry>();
                }
                var before=Read(id);
                var after=change.On(before);
                InventoryBankService.ValidateEntries(after);
                if(!before.SequenceEqual(after))Store(id).Save(after);
                return after;
            });
            var result=memory.Where(entry=>entry.Key.CharacterId!=id).Concat(saved).ToArray();
            InventoryBankService.ValidateEntries(result);
            return result;
        }
    }

    /// <summary>One character's entries as on disk (a file holding another character's entries fails closed).</summary>
    private BankEntry[] Read(ulong id)
    {
        var entries=Store(id).Load();
        if(entries.Any(entry=>entry.Key.CharacterId!=id))throw new InvalidDataException("A character's bank file holds another character's entries.");
        return entries;
    }

    /// <summary>
    /// X-141: work done while holding the profile's lock file (opened without sharing: the other client, or another store here, waits);
    /// tried again every 50 ms up to lockWaitMs, then <see cref="BankLockBusyException"/> (nothing changed; X-170, 0.1142: its own type, so the
    /// service can tell « the other client holds the lock » from a file that cannot be read).
    /// </summary>
    private T Locked<T>(Func<T> work)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LockPath)!);
        var until=Environment.TickCount64+lockWaitMs;
        while(true)
        {
            FileStream held;
            try{held=new FileStream(LockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
            catch(IOException) when(Environment.TickCount64<until){Thread.Sleep(50);continue;}
            catch(IOException error){throw new BankLockBusyException(LockPath,error);}
            using(held)return work();
        }
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

    private void Migrate()
    {
        if(Directory.Exists(folder) || !File.Exists(legacy) && !File.Exists(legacy+".bak"))return;
        var entries=new FileInventoryBankStore(legacy).Load();
        var temporary=folder+".migrating";
        if(Directory.Exists(temporary))Directory.Delete(temporary,true);
        Directory.CreateDirectory(temporary);
        foreach(var group in entries.GroupBy(entry=>entry.Key.CharacterId))
            new FileInventoryBankStore(Path.Combine(temporary,FileName(group.Key))).Save(group.ToArray());
        Directory.Move(temporary,folder);
        foreach(var path in new[]{legacy,legacy+".bak"})
            if(File.Exists(path))File.Move(path,FreeName(path+".migrated"));
    }

    private static string FreeName(string path)
    {
        var candidate=path;
        for(var index=1;File.Exists(candidate);index++)candidate=$"{path}.{index}";
        return candidate;
    }
}

/// <summary>X-170 (0.1142): the profile's bank lock stayed held by another process (the other game client, the desktop application) for the whole wait.</summary>
public sealed class BankLockBusyException(string path,Exception inner) : IOException($"The bank lock ({Path.GetFileName(path)}) is held by another program.",inner);
