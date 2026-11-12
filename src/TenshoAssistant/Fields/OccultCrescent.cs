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
    uint NeededJobExperience,int Silver,int Gold)
{
    /// <summary>X-189 (0.1179): the island's critical engagements that are not inactive (DynamicEventContainer of the island's director).</summary>
    public IReadOnlyList<OccultEvent> Events { get; init; }=[];
}

/// <summary>
/// X-189 (0.1179): a critical engagement as the game holds it (ClientStructs DynamicEvent: DynamicEventId, State 1 Register, 2 Warmup, 3 Battle,
/// SecondsLeft, StartTimestamp, map marker's map and position), copied on the framework thread.
/// </summary>
public sealed record OccultEvent(int Id,string Name,int State,uint SecondsLeft,int Start,uint MapId,float X,float Z);

/// <summary>
/// X-189 (0.1179, user request of 12/11): when to say that a critical engagement opened its registration, or that a FATE started, on the island.
/// Each one is said once while it lasts (the set keeps only those still there, so the next one with the same id is said again).
/// </summary>
public static class OccultAlerts
{
    /// <summary>DynamicEventState.Register: the engagement takes players (« Starts in 1:07 » in the game).</summary>
    public const int Register=1;

    /// <summary>The engagements open for registration not said yet; the set forgets those no longer open, then keeps the new ones.</summary>
    public static IReadOnlyList<OccultEvent> Engagements(IEnumerable<OccultEvent> events,HashSet<int> said)
    {
        var open=events.Where(item=>item.State==Register && item.Id!=0).ToArray();
        said.IntersectWith(open.Select(item=>item.Id));
        return open.Where(item=>said.Add(item.Id)).ToArray();
    }

    /// <summary>The FATEs not said yet, by id; the set forgets those that ended.</summary>
    public static IReadOnlyList<T> Fates<T>(IEnumerable<T> fates,Func<T,uint> id,HashSet<uint> said)
    {
        var running=fates.ToArray();
        said.IntersectWith(running.Select(id));
        return running.Where(fate=>said.Add(id(fate))).ToArray();
    }

    /// <summary>Minutes and seconds, as the game writes a countdown (1:07).</summary>
    public static string Clock(long seconds)=>seconds<=0 ? "0:00":$"{seconds/60}:{seconds%60:00}";
}

/// <summary>X-190 (0.1178): a coin of the island (MKDData.CurrencyItem), what « what it buys » opens.</summary>
public sealed record OccultCoin(uint ItemId,string Name);

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

    /// <summary>X-188 (0.1178): the job worn, for the panel on the island (null: none worn, or not in the sheets).</summary>
    public static OccultJob? Worn(IEnumerable<OccultJob> jobs,OccultState state)=>state.CurrentJob<0 ? null:jobs.FirstOrDefault(job=>job.Id==(uint)state.CurrentJob);

    /// <summary>X-188 (0.1178): the worn job's experience and what it needs (null: mastered, or not read).</summary>
    public static (uint Have,uint Need)? WornExperience(OccultJob job,OccultState state)
    {
        var level=LevelOf(state,job.Id);
        if(level>=job.MaxLevel || state.NeededJobExperience==0 || job.Id>=state.Experience.Count)return null;
        return (state.Experience[(int)job.Id],state.NeededJobExperience);
    }

    /// <summary>X-190 (0.1178): each coin once (the northern part's rows repeat theirs), in the sheet's order, no empty row.</summary>
    public static IReadOnlyList<OccultCoin> Coins(IEnumerable<OccultCoin> coins)=>coins.Where(coin=>coin.ItemId!=0 && coin.Name.Length>0).DistinctBy(coin=>coin.ItemId).ToArray();

    /// <summary>X-190 (0.1178): the occult records the game says are read, out of all of them (MKDLore rows with a name).</summary>
    public static (int Read,int Total) Lore(IEnumerable<TenshoAssistant.Collections.CollectionEntry> entries)
    {
        var read=0;var total=0;
        foreach(var entry in entries)
        {
            if(entry.Kind!=TenshoAssistant.Collections.CollectionKind.OccultLore)continue;
            total++;
            if(entry.ConfirmedUnlocked)read++;
        }
        return (read,total);
    }

    /// <summary>The first reading after entering the island holds only zeros (dalamud.log of 12/11 14:57:32): not a state.</summary>
    public static bool Empty(OccultState state)=>state.Knowledge==0 && state.KnowledgeNeeded==0 && state.Levels.All(level=>level==0);
}
