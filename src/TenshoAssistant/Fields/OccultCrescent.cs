namespace TenshoAssistant.Fields;

/// <summary>X-188 (0.1176): what a phantom job learns at a level: an action (MKDSupportJob.Actions) or a trait (MKDTrait), with its description.</summary>
public sealed record OccultUnlock(int Level,string Name,string Description,uint Icon,bool Trait);
/// <summary>X-188 (0.1176): a phantom job of the Occult Crescent (MKDSupportJob row: the row is the job's index in the game's state), its last level.</summary>
public sealed record OccultJob(uint Id,string Name,int MaxLevel,IReadOnlyList<OccultUnlock> Unlocks);
/// <summary>X-188 (0.1176): the game's own words for the island (Addon rows 16607 Knowledge, 16611 Phantom Job, 16608 MASTERED, 16618 Phantom EXP), client language.</summary>
public sealed record OccultLabels(string Knowledge,string PhantomJob,string Mastered,string Experience);
/// <summary>
/// X-188 (0.1176): what the game holds on the island (OccultCrescentState), copied on the framework thread: knowledge and what the next level needs,
/// the phantom job worn, each job's level and experience (by job index), the experience the worn job needs, the silver and gold coins.
/// </summary>
public sealed record OccultState(DateTimeOffset At,uint Knowledge,uint KnowledgeNeeded,int CurrentJob,IReadOnlyList<int> Levels,IReadOnlyList<uint> Experience,
    uint NeededJobExperience,int Silver,int Gold);

/// <summary>X-188 (0.1176): the phantom jobs as the page shows them (docs/evidence/0.1176-occult-jobs.md).</summary>
public static class OccultCrescent
{
    /// <summary>A job's level in the state (0: never worn, or not read).</summary>
    public static int LevelOf(OccultState state,uint job)=>job<state.Levels.Count ? state.Levels[(int)job]:0;

    /// <summary>What the job learns next: the first action or trait above the level (an action before a trait of the same level); null at its last level.</summary>
    public static OccultUnlock? Next(OccultJob job,int level)=>level>=job.MaxLevel ? null
        :job.Unlocks.Where(unlock=>unlock.Level>level).OrderBy(unlock=>unlock.Level).ThenBy(unlock=>unlock.Trait).FirstOrDefault();

    /// <summary>The job worn first, then the jobs with levels (highest first), then the others in the game's order.</summary>
    public static IReadOnlyList<OccultJob> Ordered(IEnumerable<OccultJob> jobs,OccultState state)=>
        jobs.OrderBy(job=>job.Id==(uint)state.CurrentJob ? 0:LevelOf(state,job.Id)>0 ? 1:2).ThenByDescending(job=>LevelOf(state,job.Id)).ThenBy(job=>job.Id).ToArray();

    /// <summary>A share between 0 and 1 (nothing needed: 0).</summary>
    public static float Share(uint have,uint need)=>need==0 ? 0:Math.Clamp((float)have/need,0,1);

    /// <summary>The first reading after entering the island holds only zeros (dalamud.log of 12/11 14:57:32): not a state.</summary>
    public static bool Empty(OccultState state)=>state.Knowledge==0 && state.KnowledgeNeeded==0 && state.Levels.All(level=>level==0);
}
