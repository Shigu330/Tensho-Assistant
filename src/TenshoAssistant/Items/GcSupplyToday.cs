namespace TenshoAssistant.Items;

public enum GcSupplyStatus { NotObserved, Requested, NotListed }

/// <summary>The Grand Company items seen in the game's supply missions window, for one character and one daily reset.</summary>
public sealed record GcSupplySeen(ulong Owner,DateTimeOffset Reset,List<uint> Items);

/// <summary>
/// 0.469 (user request, in-game campaign): what the game's supply missions window showed today. Nothing seen means « not observed », never « not requested »; an item missing from what was
/// seen is only « not listed », because the player may not have opened every tab. It starts empty at each daily reset.
/// </summary>
public sealed class GcSupplyToday
{
    private readonly object gate=new();
    private GcSupplySeen? seen;
    public GcSupplySeen? Seen{get{lock(gate)return seen is null ? null:seen with {Items=[..seen.Items]};}}
    public void Restore(GcSupplySeen? saved){lock(gate)seen=saved is {Owner:not 0,Items:not null} ? saved with {Items=[..saved.Items]}:null;}
    /// <summary>Adds what the window shows; true when something new was learned.</summary>
    public bool Observe(ulong owner,DateTimeOffset reset,IEnumerable<uint> items)
    {
        var found=items.Where(item=>item!=0).Distinct().ToArray();
        if(owner==0 || found.Length==0)return false;
        lock(gate)
        {
            if(seen is null || seen.Owner!=owner || seen.Reset!=reset){seen=new(owner,reset,[..found]);return true;}
            var added=found.Except(seen.Items).ToArray();
            if(added.Length==0)return false;
            seen.Items.AddRange(added);return true;
        }
    }
    public GcSupplyStatus Status(ulong owner,DateTimeOffset reset,uint item)
    {
        lock(gate)
        {
            if(seen is null || seen.Owner!=owner || seen.Reset!=reset || seen.Items.Count==0)return GcSupplyStatus.NotObserved;
            return seen.Items.Contains(item) ? GcSupplyStatus.Requested:GcSupplyStatus.NotListed;
        }
    }
    /// <summary>A window's text without the game's own glyphs (private use area, such as the high quality mark) and outer spaces.</summary>
    public static string Normalize(string? text)
        =>string.IsNullOrEmpty(text) ? "":new string(text.Where(letter=>!char.IsControl(letter) && letter is not (>='' and <='')).ToArray()).Trim();
    /// <summary>The candidate items whose exact name is one of the window's texts: a text that is not exactly an item's name proves nothing.</summary>
    public static IReadOnlyList<uint> Match(IEnumerable<string?> texts,IReadOnlyDictionary<string,uint> names)
    {
        var found=new List<uint>();
        foreach(var text in texts)if(names.TryGetValue(Normalize(text),out var item) && !found.Contains(item))found.Add(item);
        return found;
    }
    public static IReadOnlyDictionary<string,uint> NameIndex(IEnumerable<(uint Item,string? Name)> candidates)
    {
        var index=new Dictionary<string,uint>(StringComparer.OrdinalIgnoreCase);
        foreach(var (item,name) in candidates)if(item!=0 && Normalize(name) is {Length:>0} clean)index.TryAdd(clean,item);
        return index;
    }
}
