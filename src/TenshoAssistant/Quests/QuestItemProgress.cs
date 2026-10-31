namespace TenshoAssistant.Quests;

public enum QuestSourceProgressFilter { All, Active, Complete, Unconfirmed }

/// <summary>A static reward candidate joined to the progression of the same quest snapshot.</summary>
public sealed record QuestItemSourceProgress(QuestItemRewardSource Source,QuestProgress Progress,byte? Sequence,bool IsRepeatable);

/// <summary>
/// Immutable projection of quest reward candidates for one Item, joined by full quest ID to the
/// QuestState that produced the index. Counts are computed before the progression filter.
/// No availability, reset or claimable reward is inferred.
/// </summary>
public sealed record QuestItemProgressProjection(ulong CharacterId,DateTimeOffset? ObservedAt,bool FromCache,bool ProgressConfirmed,
    IReadOnlyList<QuestItemSourceProgress> Rows,int Matches,int Active,int Complete,int Unconfirmed);

public static class QuestItemProgress
{
    /// <summary>Returns null when the state carries no reward index (loading, error or invalidated).</summary>
    public static QuestItemProgressProjection? Project(QuestState state,uint item,string? query,QuestSourceProgressFilter filter)
    {
        ArgumentNullException.ThrowIfNull(state);
        if(state.RewardIndex is not { } index)return null;
        var sources=index.Search(item,query);
        // Progress is only meaningful for an owned, dated, settled and error-free observation.
        var confirmed=state.CharacterId!=0 && state.ObservedAt is not null && !state.Loading && state.Error is null;
        Dictionary<uint,QuestEntry>? progress=null;
        if(confirmed && sources.Count>0)
        {
            var wanted=new HashSet<uint>(sources.Select(source=>source.QuestId));
            progress=new Dictionary<uint,QuestEntry>(wanted.Count);
            foreach(var entry in state.Entries)
                if(wanted.Contains(entry.Id))progress.TryAdd(entry.Id,entry);
        }
        var all=new QuestItemSourceProgress[sources.Count];
        int active=0,complete=0,unconfirmed=0;
        for(var position=0;position<sources.Count;position++)
        {
            var source=sources[position];
            var entry=progress?.GetValueOrDefault(source.QuestId);
            var observed=entry?.Progress ?? QuestProgress.Unconfirmed;
            var sequence=observed==QuestProgress.Active ? entry?.Sequence:null;
            all[position]=new(source,observed,sequence,entry?.IsRepeatable ?? false);
            switch(observed)
            {
                case QuestProgress.Active:active++;break;
                case QuestProgress.Complete:complete++;break;
                default:unconfirmed++;break;
            }
        }
        var rows=filter==QuestSourceProgressFilter.All ? all:all.Where(row=>Matches(row.Progress,filter)).ToArray();
        return new(confirmed ? state.CharacterId:0,confirmed ? state.ObservedAt:null,confirmed && state.FromCache,confirmed,
            Array.AsReadOnly(rows),sources.Count,active,complete,unconfirmed);
    }
    private static bool Matches(QuestProgress progress,QuestSourceProgressFilter filter)=>filter switch
    {
        QuestSourceProgressFilter.Active=>progress==QuestProgress.Active,
        QuestSourceProgressFilter.Complete=>progress==QuestProgress.Complete,
        QuestSourceProgressFilter.Unconfirmed=>progress==QuestProgress.Unconfirmed,
        _=>true
    };
}
