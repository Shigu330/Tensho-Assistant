namespace TenshoAssistant.Quests;

public enum QuestProgress { Unconfirmed, Active, Complete }
public sealed record QuestEntry(uint Id, string Name, string Category, uint Icon, QuestProgress Progress, byte? Sequence)
{
    public IReadOnlyList<uint> PreviousQuestIds { get; init; } = Array.Empty<uint>();
    public byte PreviousQuestJoin { get; init; }
    // Unknown7 in the installed schema: retained raw; no step-satisfaction inference.
    public byte PreviousQuestStep { get; init; }
    public bool IsRepeatable { get; init; }
    public ushort Level { get; init; }
    public ushort AdditionalLevel { get; init; }
    public string Zone { get; init; } = "";
    public uint IssuerId { get; init; }
    public uint IssuerMapId { get; init; }
    public uint IssuerTerritoryId { get; init; }
    public QuestRewardSnapshot? Rewards {get;init;}
    public string? RewardsError {get;init;}
}
public sealed record QuestState(ulong CharacterId, DateTimeOffset? ObservedAt, IReadOnlyList<QuestEntry> Entries, bool Loading, string? Error,
    QuestRewardIndex? RewardIndex=null,bool RewardIndexLoading=false,string? RewardIndexError=null,bool FromCache=false);

/// <summary>One immutable, direct-link index per published observation. No recursive traversal.</summary>
public sealed class QuestLinks
{
    private readonly IReadOnlyDictionary<uint, QuestEntry> entries;
    private readonly IReadOnlyDictionary<uint, IReadOnlyList<QuestEntry>> next;
    public QuestLinks(IEnumerable<QuestEntry> source)
    {
        var copied = source.Select(entry => entry with
        {
            PreviousQuestIds = Array.AsReadOnly(entry.PreviousQuestIds.Where(id => id != 0).Distinct().ToArray())
        }).ToArray();
        entries = copied.ToDictionary(entry => entry.Id);
        next = copied.SelectMany(entry => entry.PreviousQuestIds.Select(id => (Id: id, Entry: entry)))
            .GroupBy(link => link.Id).ToDictionary(group => group.Key,
                group => (IReadOnlyList<QuestEntry>)Array.AsReadOnly(group.Select(link => link.Entry)
                    .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.Id).ToArray()));
    }
    public QuestEntry? Find(uint id) => entries.GetValueOrDefault(id);
    public IReadOnlyList<QuestEntry> Following(uint id) => next.GetValueOrDefault(id) ?? Array.Empty<QuestEntry>();
    public static string JoinLabel(byte mode) => mode switch
    { 1 => "Toutes les quêtes liées", 2 => "Au moins une des quêtes liées", _ => $"Condition non interprétée (mode {mode})" };
    public bool CompletionEvidence(QuestEntry entry) => entry.PreviousQuestIds.Count > 0 && entry.PreviousQuestJoin switch
    {
        1 => entry.PreviousQuestIds.All(id => Find(id)?.Progress == QuestProgress.Complete),
        2 => entry.PreviousQuestIds.Any(id => Find(id)?.Progress == QuestProgress.Complete),
        _ => false
    };
}

public sealed class QuestService : IDisposable
{
    private readonly object gate=new();
    private readonly Func<IReadOnlyList<QuestEntry>,CancellationToken,Task<QuestRewardIndex>> buildIndex;
    private QuestState state = new(0, null, Array.Empty<QuestEntry>(), true, null);
    private CancellationTokenSource? indexCancellation;
    private long generation;
    private bool disposed;
    private Task indexWork=Task.CompletedTask;
    private int refresh;
    public QuestService():this((entries,token)=>Task.Run(()=>QuestRewardIndex.Build(entries,token),token)){}
    internal QuestService(Func<IReadOnlyList<QuestEntry>,CancellationToken,Task<QuestRewardIndex>> builder){buildIndex=builder;}
    public QuestState State => Volatile.Read(ref state);
    internal Task IndexWork {get{lock(gate)return indexWork;}}
    public void RequestRefresh(){if(!Volatile.Read(ref disposed))Interlocked.Exchange(ref refresh,1);}
    internal bool ConsumeRefresh()=>!Volatile.Read(ref disposed) && Interlocked.Exchange(ref refresh,0)==1;
    internal void Publish(ulong character,IEnumerable<QuestEntry> entries,DateTimeOffset? observedAt=null,bool fromCache=false)
    {
        var copied=Array.AsReadOnly(entries.Select(entry=>entry with
                {
                    Progress = character == 0 ? QuestProgress.Unconfirmed : entry.Progress,
                    Sequence = character == 0 ? null : entry.Sequence,
                    PreviousQuestIds = Array.AsReadOnly(entry.PreviousQuestIds.Where(id => id != 0).Distinct().ToArray())
                })
                .OrderBy(entry=>entry.Name,StringComparer.OrdinalIgnoreCase).ThenBy(entry=>entry.Id).ToArray());
        lock(gate)
        {
            if(disposed)return;
            CancelIndex();var currentGeneration=++generation;var token=new CancellationTokenSource();indexCancellation=token;
            var expected=new QuestState(character,character==0 ? null:observedAt ?? DateTimeOffset.UtcNow,copied,false,null,RewardIndexLoading:true,FromCache:fromCache);
            Interlocked.Exchange(ref state,expected);
            indexWork=BuildIndex(expected,currentGeneration,token);
        }
    }
    private async Task BuildIndex(QuestState expected,long version,CancellationTokenSource cancellation)
    {
        try
        {
            var result=await buildIndex(expected.Entries,cancellation.Token).ConfigureAwait(false);
            if(result is null)throw new InvalidDataException("No quest reward index produced.");
            lock(gate)if(!disposed && version==generation && !cancellation.IsCancellationRequested && ReferenceEquals(state,expected))
                Interlocked.Exchange(ref state,expected with {RewardIndex=result,RewardIndexLoading=false});
        }
        catch(OperationCanceledException) when(cancellation.IsCancellationRequested){}
        catch(Exception error)
        {
            lock(gate)if(!disposed && version==generation && ReferenceEquals(state,expected))
                Interlocked.Exchange(ref state,expected with {RewardIndexLoading=false,RewardIndexError=error.Message});
        }
        finally
        {
            lock(gate){if(ReferenceEquals(indexCancellation,cancellation))indexCancellation=null;cancellation.Dispose();}
        }
    }
    private void CancelIndex(){var pending=indexCancellation;indexCancellation=null;pending?.Cancel();}
    internal void Invalidate()
    {
        lock(gate)
        {
            if(disposed)return;CancelIndex();generation++;indexWork=Task.CompletedTask;
            Interlocked.Exchange(ref state,new(0,null,Array.AsReadOnly(State.Entries.Select(entry=>entry with {Progress=QuestProgress.Unconfirmed,Sequence=null}).ToArray()),true,null));
        }
    }
    internal void Fail(Exception error)
    {
        lock(gate){if(disposed)return;Invalidate();Interlocked.Exchange(ref state,State with {Loading=false,Error=error.Message});}
    }
    internal void Clear()
    {
        lock(gate){CancelIndex();generation++;indexWork=Task.CompletedTask;Interlocked.Exchange(ref refresh,0);Interlocked.Exchange(ref state,new(0,null,Array.Empty<QuestEntry>(),false,null));}
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;Clear();}}
}
