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
