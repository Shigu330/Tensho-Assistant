namespace TenshoAssistant.Collections;

public sealed record CollectionGain(CollectionKind Kind,uint Id,string Name,DateTimeOffset At);

/// <summary>0.602 (T28): what a character had at the last reading, and what was gained since Tensho started keeping it.</summary>
public sealed class CollectionMemory
{
    public Dictionary<CollectionKind,List<uint>> Known {get;set;}=[];
    public List<CollectionGain> Recent {get;set;}=[];
}

public static class CollectionRecent
{
    public const int MaximumShown=30;

    /// <summary>
    /// Compares a reading with the last one. The first reading of a kind only records it: what was already owned is not « recent ». Something
    /// no longer read is kept: an absence proves nothing. Achievements are left out: too many, with their own log. True when something changed.
    /// </summary>
    public static bool Update(CollectionMemory memory,IEnumerable<CollectionEntry> entries,DateTimeOffset now)
    {
        var changed=false;
        foreach(var group in entries.Where(entry=>entry.ConfirmedUnlocked && entry.Kind!=CollectionKind.Achievement).GroupBy(entry=>entry.Kind))
        {
            if(!memory.Known.TryGetValue(group.Key,out var known))
            {
                memory.Known[group.Key]=group.Select(entry=>entry.Id).Distinct().ToList();
                changed=true;
                continue;
            }
            var seen=known.ToHashSet();
            foreach(var entry in group.Where(entry=>seen.Add(entry.Id)))
            {
                known.Add(entry.Id);
                memory.Recent.Insert(0,new(entry.Kind,entry.Id,entry.Name,now));
                changed=true;
            }
        }
        if(memory.Recent.Count>MaximumShown)memory.Recent.RemoveRange(MaximumShown,memory.Recent.Count-MaximumShown);
        return changed;
    }
}
