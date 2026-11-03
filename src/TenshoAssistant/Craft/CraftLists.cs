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
    public const long MaxQuantity=999999;
    private static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
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

    public static IReadOnlyList<CraftList> Read(string path)
    {
        if(!File.Exists(path))return [];
        if(new FileInfo(path).Length>2*1024*1024)throw new InvalidDataException("Crafting lists file too large.");
        var dto=JsonSerializer.Deserialize<FileDto>(File.ReadAllBytes(path),Json) ?? throw new InvalidDataException("Empty crafting lists file.");
        if(dto.Schema!=1 || dto.Lists is null)throw new InvalidDataException("Unknown crafting lists schema.");
        return dto.Lists.Where(list=>CleanName(list.Name) is not null && list.Entries is not null).Take(MaxLists)
            .Select(list=>list with {Entries=list.Entries.Where(entry=>entry.ItemId!=0 && entry.Quantity is >0 and <=MaxQuantity).Take(MaxEntries).ToArray()}).ToArray();
    }

    public static void Write(string path,IReadOnlyList<CraftList> lists)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        File.WriteAllBytes(temporary,JsonSerializer.SerializeToUtf8Bytes(new FileDto(1,lists.Take(MaxLists).ToArray()),Json));
        File.Move(temporary,path,true);
    }
}

/// <summary>Owner of the lists file (window thread). A file that cannot be read is never written over.</summary>
public sealed class CraftListStore(string path,Action<Exception,string> warn)
{
    private List<CraftList>? lists;
    private bool failed;
    public bool Unavailable{get{Load();return failed;}}
    public IReadOnlyList<CraftList> Lists{get{Load();return lists!.ToArray();}}

    /// <summary>Replaces the list at the index (null removes it; index = count adds one); false when nothing could be saved.</summary>
    public bool Set(int index,CraftList? list)
    {
        Load();
        if(failed || index<0 || index>lists!.Count)return false;
        var next=lists.ToList();
        if(list is null){if(index==next.Count)return false;next.RemoveAt(index);}
        else if(index==next.Count){if(next.Count>=CraftLists.MaxLists)return false;next.Add(list);}
        else next[index]=list;
        try{CraftLists.Write(path,next);lists=next;return true;}
        catch(Exception error){warn(error,"Crafting lists could not be saved.");return false;}
    }

    private void Load()
    {
        if(lists is not null)return;
        try{lists=CraftLists.Read(path).ToList();}
        catch(Exception error){lists=[];failed=true;warn(error,"Crafting lists could not be read; the file is not written over.");}
    }
}
