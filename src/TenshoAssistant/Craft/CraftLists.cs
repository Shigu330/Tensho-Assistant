using System.Text.Json;

namespace TenshoAssistant.Craft;

/// <summary>A-05 (0.433): one crafting list — a name and the items to make with their quantities.</summary>
public sealed record CraftListEntry(uint ItemId,long Quantity);
public sealed record CraftList(string Name,IReadOnlyList<CraftListEntry> Entries);

/// <summary>
/// A-05 (0.433, user's choice in GAPS: a file of its own, like the Bank): crafting lists of several items, kept in « CraftLists.json »
/// in the plugin's folder (written through a temporary file; a file that cannot be read is never written over). Bounded: 32 lists of
/// 64 items, quantities 1 to 999 999, names of 1 to 40 characters.
/// </summary>
public static class CraftLists
{
    public const int MaxLists=32,MaxEntries=64,MaxName=40;
    /// <summary>X-842 M2 (0.1843, review of 0.1838): the most lists the file holds when two game clients each added theirs (each client still adds
    /// only up to <see cref="MaxLists"/>); a merge past it is not saved and said, never cut in silence.</summary>
    public const int MaxListsMerged=2*MaxLists;
    public const long MaxQuantity=999999;
    private static readonly JsonSerializerOptions Json=TenshoJson.Own(new(){WriteIndented=true});
    private sealed record FileDto(int Schema,CraftList[] Lists);

    public static string? CleanName(string? name)
    {
        var text=(name ?? "").Trim();
        return text.Length is >= 1 and <= MaxName && !text.Any(char.IsControl) ? text:null;
    }

    /// <summary>The list with the item added (quantities added when it is already there), or null when a bound is reached.</summary>
    public static CraftList? Add(CraftList list,uint itemId,long quantity)
    {
        if(itemId==0 || quantity<=0)return null;
        var entries=list.Entries.ToList();
        var index=entries.FindIndex(entry=>entry.ItemId==itemId);
        if(index>=0){entries[index]=entries[index] with {Quantity=Math.Min(MaxQuantity,entries[index].Quantity+quantity)};}
        else{if(entries.Count>=MaxEntries)return null;entries.Add(new(itemId,Math.Min(MaxQuantity,quantity)));}
        return list with {Entries=entries.ToArray()};
    }

    public static CraftList SetQuantity(CraftList list,uint itemId,long quantity)
        =>list with {Entries=list.Entries.Select(entry=>entry.ItemId==itemId ? entry with {Quantity=Math.Clamp(quantity,1,MaxQuantity)}:entry).ToArray()};

    public static CraftList Remove(CraftList list,uint itemId)=>list with {Entries=list.Entries.Where(entry=>entry.ItemId!=itemId).ToArray()};

    /// <summary>Total of each direct ingredient over the items' plans (item → required), in first-seen order.</summary>
    public static IReadOnlyList<(uint ItemId,long Required)> Ingredients(IEnumerable<CraftPlan> plans)
    {
        var totals=new Dictionary<uint,long>();var order=new List<uint>();
        foreach(var material in plans.SelectMany(plan=>plan.Materials))
        {
            if(!totals.ContainsKey(material.ItemId)){totals[material.ItemId]=0;order.Add(material.ItemId);}
            totals[material.ItemId]=checked(totals[material.ItemId]+material.Required);
        }
        return order.Select(id=>(id,totals[id])).ToArray();
    }

    /// <summary>
    /// 0.478 (user request): all the raw materials of a list, recipes followed to the end, before any stock: a material shared by two items is counted for both, and the caller takes the stock off once.
    /// Intermediate items already made are therefore not taken off. An item without a chosen recipe is itself what is needed.
    /// </summary>
    public static IReadOnlyDictionary<uint,long> RawNeeds(CraftList list,IReadOnlyDictionary<uint,RecipeDefinition> recipes,RecipeCatalog catalog,CancellationToken cancellation=default)
    {
        var needs=new Dictionary<uint,long>();
        void Add(uint item,long amount)=>needs[item]=checked(needs.GetValueOrDefault(item)+amount);
        var nothing=new TenshoAssistant.Inventory.ItemHoldingsSummary([],0,0,false,false,false,false);
        foreach(var entry in list.Entries)
        {
            cancellation.ThrowIfCancellationRequested();
            if(!recipes.TryGetValue(entry.ItemId,out var recipe)){Add(entry.ItemId,entry.Quantity);continue;}
            foreach(var material in CraftChain.Build(recipe,entry.Quantity,catalog,_=>nothing,cancellation).Materials)
                if(material.Reason==CraftChainReason.RawMaterial)Add(material.ItemId,material.Amount);
        }
        return needs;
    }

    /// <summary>X-833: the file, or its previous version (« .bak ») when it is missing or does not read (AtomicFile.ReadKept).</summary>
    public static IReadOnlyList<CraftList> Read(string path,Action<string,Exception>? recovered=null)=>AtomicFile.ReadKept(path,ReadFile,recovered);

    private static IReadOnlyList<CraftList> ReadFile(string path)
    {
        if(!File.Exists(path))return [];
        if(new FileInfo(path).Length>2*1024*1024)throw new InvalidDataException("Crafting lists file too large.");
        var dto=JsonSerializer.Deserialize<FileDto>(File.ReadAllBytes(path),Json) ?? throw new InvalidDataException("Empty crafting lists file.");
        if(dto.Schema!=1 || dto.Lists is null)throw new InvalidDataException("Unknown crafting lists schema.");
        return dto.Lists.Where(list=>CleanName(list.Name) is not null && list.Entries is not null).Take(MaxListsMerged)
            .Select(list=>list with {Entries=list.Entries.Where(entry=>entry.ItemId!=0 && entry.Quantity is >0 and <=MaxQuantity).Take(MaxEntries).ToArray()}).ToArray();
    }

    /// <summary>
    /// X-832 (security review I4): two game clients on one profile each kept the lists read at their start and wrote their whole copy back: a
    /// list added in one was erased by any change in the other. Three-way, by name (a name met twice: by its rank among that name): a list this
    /// client did not change is the file's (or stays removed when the file removed it); one it added or changed is its own; one it removed stays
    /// removed; one only the file has, never seen here, is kept. Mine's order first, then the file's other lists.
    /// </summary>
    public static IReadOnlyList<CraftList> Merge(IReadOnlyList<CraftList>? before,IReadOnlyList<CraftList> mine,IReadOnlyList<CraftList> theirs)
    {
        var old=Keyed(before ?? []);var file=Keyed(theirs);var own=Keyed(mine);
        var result=new List<CraftList>();
        foreach(var (key,list) in own)
        {
            var had=old.TryGetValue(key,out var was);
            var there=file.TryGetValue(key,out var other);
            if(had && Same(was!,list)){if(there)result.Add(other!);continue;}
            result.Add(list);
        }
        foreach(var (key,list) in file)
            if(!own.ContainsKey(key) && !old.ContainsKey(key))result.Add(list);
        return result;
    }

    private static List<KeyValuePair<string,CraftList>> KeyedList(IReadOnlyList<CraftList> lists)
    {
        var seen=new Dictionary<string,int>(StringComparer.Ordinal);
        return lists.Select(list=>{var rank=seen[list.Name]=seen.GetValueOrDefault(list.Name)+1;return KeyValuePair.Create(rank+":"+list.Name,list);}).ToList();
    }
    private static OrderedKeys Keyed(IReadOnlyList<CraftList> lists)=>new(KeyedList(lists));
    private sealed class OrderedKeys(List<KeyValuePair<string,CraftList>> pairs):IEnumerable<KeyValuePair<string,CraftList>>
    {
        private readonly Dictionary<string,CraftList> byKey=pairs.ToDictionary(pair=>pair.Key,pair=>pair.Value,StringComparer.Ordinal);
        public bool TryGetValue(string key,out CraftList? list)=>byKey.TryGetValue(key,out list);
        public bool ContainsKey(string key)=>byKey.ContainsKey(key);
        public IEnumerator<KeyValuePair<string,CraftList>> GetEnumerator()=>pairs.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private static bool Same(CraftList a,CraftList b)=>a.Name==b.Name && a.Entries.SequenceEqual(b.Entries);

    public static void Write(string path,IReadOnlyList<CraftList> lists)
    {
        AtomicFile.Write(path,JsonSerializer.SerializeToUtf8Bytes(new FileDto(1,lists.Take(MaxListsMerged).ToArray()),Json),keep:true);
    }
}

/// <summary>
/// Owner of the lists file (window thread). A file that cannot be read is never written over. X-832: each save holds the profile's lock
/// (« CraftLists.json.lock », FairFileLock, shared by the game clients of the profile), reads the file again and merges it with this client's
/// change (CraftLists.Merge); the lists in memory become the merged ones (another client's lists shown from then on).
/// </summary>
public sealed class CraftListStore(string path,Action<Exception,string> warn,bool readOnly=false)
{
    /// <summary>X-832: the wait for another game client's save (a save takes milliseconds); past it, this save is refused and said.</summary>
    public const int LockWaitMs=2000;
    /// <summary>X-842 M4 (0.1843, review of 0.1838): Set runs on the window's thread (drawing): it waits a tenth of a second at most (the other
    /// client's save takes milliseconds), as the settings do (SettingsFile.GameThreadLockWaitMs); past it the change is refused and said.</summary>
    public const int DrawLockWaitMs=100;
    private List<CraftList>? lists;
    private IReadOnlyList<CraftList>? before;
    private bool failed;
    /// <summary>X-180 (0.1168): read-only (the desktop application's copy, overwritten from the game): Set changes and writes nothing.</summary>
    public bool ReadOnly=>readOnly;
    public bool Unavailable{get{Load();return failed;}}
    public IReadOnlyList<CraftList> Lists{get{Load();return lists!.ToArray();}}

    /// <summary>Replaces the list at the index (null removes it; index = count adds one); false when nothing could be saved.</summary>
    public bool Set(int index,CraftList? list)
    {
        Load();
        if(failed || readOnly || index<0 || index>lists!.Count)return false;
        var next=lists.ToList();
        if(list is null){if(index==next.Count)return false;next.RemoveAt(index);}
        else if(index==next.Count){if(next.Count>=CraftLists.MaxLists)return false;next.Add(list);}
        else next[index]=list;
        try
        {
            using(FairFileLock.Hold(path+".lock",DrawLockWaitMs,error=>new IOException("The crafting lists' lock is held by another game client.",error)))
            {
                var onDisk=CraftLists.Read(path,Recovered);
                var merged=CraftLists.Merge(before,next,onDisk).ToList();
                if(merged.Count>CraftLists.MaxListsMerged)
                    throw new InvalidDataException($"With the other game client's lists there would be {merged.Count} lists, more than {CraftLists.MaxListsMerged}: nothing was saved; remove a list first.");
                CraftLists.Write(path,merged);
                lists=merged;before=merged;
            }
            return true;
        }
        catch(Exception error){warn(error,"Crafting lists could not be saved.");return false;}
    }

    private void Load()
    {
        if(lists is not null)return;
        try{lists=CraftLists.Read(path,Recovered).ToList();before=lists.ToArray();}
        catch(Exception error){lists=[];failed=true;warn(error,"Crafting lists could not be read; the file is not written over.");}
    }

    private void Recovered(string aside,Exception error)=>warn(error,$"Crafting lists did not read; their previous version (.bak) was read, the damaged file kept as {Path.GetFileName(aside)}.");
}
