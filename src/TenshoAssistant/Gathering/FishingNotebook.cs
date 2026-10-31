using System.Collections.Frozen;

namespace TenshoAssistant.Gathering;

public sealed record FishingNotebookEntry(uint FishId,uint ItemId,uint NotebookSpotId,string Description,string Folklore,
    bool InLog,bool Hidden,bool? TimeRestricted,bool? WeatherRestricted,bool NoteLinked);

/// <summary>Static game notebook references, never evidence of the character's catches or unlocks.</summary>
public sealed class FishingNotebookIndex
{
    private readonly FrozenDictionary<uint,IReadOnlyList<FishingNotebookEntry>> byItem;
    public int Count {get;}
    public FishingNotebookIndex(IEnumerable<FishingNotebookEntry> rows,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(rows);cancellation.ThrowIfCancellationRequested();
        var copied=new Dictionary<uint,FishingNotebookEntry>();
        foreach(var row in rows)
        {
            cancellation.ThrowIfCancellationRequested();
            if(row.FishId==0 || row.ItemId==0 || row.Description is null || row.Folklore is null)continue;
            if(!copied.TryAdd(row.FishId,row) && copied[row.FishId]!=row)throw new InvalidOperationException("Conflicting fish notebook identity.");
        }
        Count=copied.Count;
        byItem=copied.Values.GroupBy(row=>row.ItemId).ToFrozenDictionary(group=>group.Key,
            group=>(IReadOnlyList<FishingNotebookEntry>)Array.AsReadOnly(group.OrderBy(row=>row.FishId).ToArray()));
    }
    public IReadOnlyList<FishingNotebookEntry> ForItem(uint itemId)=>byItem.GetValueOrDefault(itemId) ?? [];
    public static bool? Flag(byte value)=>value switch{0=>false,1=>true,_=>null};
}
