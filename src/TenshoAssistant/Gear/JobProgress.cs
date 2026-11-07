namespace TenshoAssistant.Gear;

/// <summary>T21 (0.646): a job's level and experience. Level 0: not unlocked. Needed 0: nothing more to earn at this level (ParamGrow.ExpToNext).</summary>
public sealed record JobProgressRow(uint Job,int Level,int Experience,int Needed)
{
    public bool Unlocked=>Level>0;
    public float Fraction=>Needed<=0 ? 1:Math.Clamp((float)Experience/Needed,0,1);
}

public static class JobProgressList
{
    /// <summary>A class and its job share one experience slot (ClassJob.ExpArrayIndex): the job, the later row, stands for both. In the game's order (ClassJob.UIPriority).</summary>
    public static IReadOnlyList<(uint Job,int Index)> Jobs(IReadOnlyDictionary<uint,int> expIndex,IReadOnlyDictionary<uint,int> order)
        =>expIndex.GroupBy(entry=>entry.Value).Select(group=>(Job:group.Max(entry=>entry.Key),Index:group.Key))
            .OrderBy(row=>order.GetValueOrDefault(row.Job)).ThenBy(row=>row.Job).ToArray();
}
