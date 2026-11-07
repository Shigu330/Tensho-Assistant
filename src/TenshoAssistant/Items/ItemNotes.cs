using TenshoAssistant.Inventory;

namespace TenshoAssistant.Items;

/// <summary>g, I-08 (0.347): the player's note and labels on one item (local only, saved in the configuration).</summary>
public sealed record ItemNote(string Text,bool Important,bool ToSell)
{
    public bool Empty=>Text.Trim().Length==0 && !Important && !ToSell;
}

/// <summary>
/// g, I-08 (0.347): notes and labels « important » and « to sell to an NPC » on items, original code (ideas from TooltipNotes, Very
/// Important Item and Sundried, read only). The dictionary is replaced whole on every change (copy on write): the framework thread
/// reads it for the shop reminder without a lock. Notes are bounded (500 characters, 2 000 items).
/// </summary>
public enum AnnotatedFilter {All,Important,ToSell}
public sealed record AnnotatedItem(uint ItemId,string Name,ItemNote Note);

public static class ItemNotes
{
    /// <summary>0.607: the items with a real note or label, sorted by name; every word of the search in the name or the note.</summary>
    public static AnnotatedItem[] Annotated(IReadOnlyDictionary<uint,ItemNote> notes,AnnotatedFilter filter,string? query,Func<uint,string> name)
    {
        var words=(query ?? "").Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        return notes.Where(pair=>pair.Value is not null && !pair.Value.Empty && (filter switch{AnnotatedFilter.Important=>pair.Value.Important,AnnotatedFilter.ToSell=>pair.Value.ToSell,_=>true}))
            .Select(pair=>new AnnotatedItem(pair.Key,name(pair.Key),pair.Value))
            .Where(row=>words.All(word=>row.Name.Contains(word,StringComparison.CurrentCultureIgnoreCase) || row.Note.Text.Contains(word,StringComparison.CurrentCultureIgnoreCase)))
            .OrderBy(row=>row.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
    public const int MaximumText=500,MaximumItems=2000;

    /// <summary>A copy of the notes with this item's note set (an empty note removes it); null when the limit would be passed.</summary>
    public static Dictionary<uint,ItemNote>? With(IReadOnlyDictionary<uint,ItemNote>? notes,uint item,ItemNote note)
    {
        var copy=notes is null ? new Dictionary<uint,ItemNote>():new Dictionary<uint,ItemNote>(notes);
        var bounded=note with { Text=note.Text.Length>MaximumText ? note.Text[..MaximumText]:note.Text };
        if(bounded.Empty)copy.Remove(item);
        else if(!copy.ContainsKey(item) && copy.Count>=MaximumItems)return null;
        else copy[item]=bounded;
        return copy;
    }

    /// <summary>
    /// Items of the player's four bags carrying a label, with the quantity held: what to sell, and what is important (the shop reminder).
    /// Unknown bags are left out (never counted as empty).
    /// </summary>
    public static (IReadOnlyList<(uint Item,long Quantity)> ToSell,IReadOnlyList<(uint Item,long Quantity)> Important) InBags(
        InventorySnapshot? live,ulong character,IReadOnlyDictionary<uint,ItemNote>? notes)
    {
        if(live is null || character==0 || notes is null || notes.Count==0)return ([],[]);
        var held=new Dictionary<uint,long>();
        foreach(var container in live.Containers.Values)
        {
            if(container.Location.Owner.Value!=character || container.Location.Container.Value is < 0 or > 3 || container.Coverage==InventoryCoverage.Unknown)continue;
            foreach(var (key,quantity) in container.Quantities)
                if(notes.ContainsKey(key.Id.Value))held[key.Id.Value]=held.GetValueOrDefault(key.Id.Value)+quantity;
        }
        var toSell=held.Where(pair=>notes[pair.Key].ToSell).Select(pair=>(pair.Key,pair.Value)).OrderBy(pair=>pair.Key).ToArray();
        var important=held.Where(pair=>notes[pair.Key].Important).Select(pair=>(pair.Key,pair.Value)).OrderBy(pair=>pair.Key).ToArray();
        return (toSell,important);
    }
}
