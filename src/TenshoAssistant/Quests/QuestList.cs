namespace TenshoAssistant.Quests;

/// <summary>0.550 (T3): the quest list's level range and sort. A limit of 0 means none; with a limit set, a quest whose level is unknown is left out, not taken for level 0.</summary>
public static class QuestList
{
    public const int ColumnQuest=0,ColumnLevel=1,ColumnZone=2,ColumnCategory=3,ColumnProgress=4;

    public static bool InLevels(int level,int from,int to)
    {
        if(from<=0 && to<=0)return true;
        if(level<=0)return false;
        return (from<=0 || level>=from) && (to<=0 || level<=to);
    }

    /// <summary>0.582 (T11): not started, and nothing known in its way: no prerequisite, or all it asks is done. What Tensho cannot read (level of the right job, a required step) stays out of this answer.</summary>
    public static bool Available(QuestLinks links,QuestEntry entry)=>entry.Progress==QuestProgress.Unconfirmed && (entry.PreviousQuestIds.Count==0 || links.CompletionEvidence(entry));

    /// <summary>0.583 (T12): the quests still to do before this one, first to take first. Unknown: a condition could not be read, the list stops there.</summary>
    public sealed record QuestPath(IReadOnlyList<QuestEntry> Todo,bool Unknown,bool Cut);
    /// <summary>A walk with its own stack, not a recursion: a long story line must not exhaust the game's stack. Bounded, and a cycle is only visited once.</summary>
    public static QuestPath PathTo(QuestLinks links,uint id,int limit=2000)
    {
        var todo=new List<QuestEntry>();var seen=new HashSet<uint>{id};var stack=new Stack<(QuestEntry Entry,bool Expanded)>();
        var unknown=false;var cut=false;
        if(links.Find(id) is not { } start)return new([],true,false);
        stack.Push((start,false));
        while(stack.Count>0)
        {
            var (entry,expanded)=stack.Pop();
            if(expanded){if(entry.Id!=id)todo.Add(entry);continue;}
            if(seen.Count>limit){cut=true;break;}
            stack.Push((entry,true));
            if(entry.PreviousQuestIds.Count==0)continue;
            var previous=entry.PreviousQuestIds.Select(links.Find).ToArray();
            if(previous.Any(found=>found is null) || entry.PreviousQuestJoin is not (1 or 2)){unknown=true;continue;}
            // Any of them is enough: nothing if one is done, else the one of the lowest level.
            if(entry.PreviousQuestJoin==2)
            {
                if(previous.Any(found=>found!.Progress==QuestProgress.Complete))continue;
                previous=[previous.OrderBy(found=>found!.Level).First()];
            }
            foreach(var found in previous.Reverse())
                if(found!.Progress!=QuestProgress.Complete && seen.Add(found.Id))stack.Push((found,false));
        }
        return new(todo,unknown,cut);
    }

    public static QuestEntry[] Sorted(IEnumerable<QuestEntry> entries,int column,bool descending)
    {
        Func<QuestEntry,IComparable> key=column switch
        {
            ColumnLevel=>entry=>entry.Level,
            ColumnZone=>entry=>entry.Zone ?? "",
            ColumnCategory=>entry=>entry.Category,
            ColumnProgress=>entry=>entry.Progress,
            _=>entry=>entry.Name,
        };
        var ordered=descending ? entries.OrderByDescending(key):entries.OrderBy(key);
        return ordered.ThenBy(entry=>entry.Name,StringComparer.CurrentCultureIgnoreCase).ThenBy(entry=>entry.Id).ToArray();
    }
}
