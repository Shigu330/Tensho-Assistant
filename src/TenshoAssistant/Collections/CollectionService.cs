using TenshoAssistant.MiniGames;

namespace TenshoAssistant.Collections;

public enum CollectionKind { Mount, Companion, Emote, BlueMagic, TripleTriad, Orchestrion, Title, Achievement }
public enum CollectionAcquisition { Unknown, Acquired, NotAcquired }
public sealed record TitleMetadata(string Masculine,string Feminine,bool IsPrefix,int Order);
public sealed record AchievementMetadata(uint CategoryId,string CategoryName,int? CategoryOrder,uint KindId,string KindName,int? KindOrder,
    int Order,int Points,uint RewardItemId,uint RewardTitleId,uint HideConditionId);
public sealed record OrchestrionMetadata(uint CategoryId,string CategoryName,int? Order,int? CategoryOrder);
public sealed record CollectionCategoryIssue(CollectionKind Kind,string Reason);
public sealed record CollectionEntry(CollectionKind Kind, uint Id, string Name, uint IconId, bool ConfirmedUnlocked,
    int Number = 0, int Rank = 0, string Description = "", string Location = "", TriadCardDefinition? Card = null,OrchestrionMetadata? Music=null,
    TitleMetadata? Title=null,AchievementMetadata? Achievement=null,bool NegativeConfirmed=false)
{
    public CollectionAcquisition Acquisition=>ConfirmedUnlocked ? CollectionAcquisition.Acquired:NegativeConfirmed ? CollectionAcquisition.NotAcquired:CollectionAcquisition.Unknown;
}
public static class CollectionAcquisitionEvidence
{
    public static CollectionEntry Observe(CollectionEntry entry,bool positive,bool loadedBefore,bool loadedAfter)=>entry with
        {ConfirmedUnlocked=positive,NegativeConfirmed=!positive && loadedBefore && loadedAfter};
    public static CollectionEntry RequireStableCoverage(CollectionEntry entry,bool stable)=>stable ? entry:entry with {NegativeConfirmed=false};
}
public sealed record CollectionState(ulong CharacterId, DateTimeOffset? ObservedAt,
    IReadOnlyList<CollectionEntry> Entries, bool Loading, string? Error,IReadOnlyList<CollectionCategoryIssue>? CategoryIssues=null,bool FromCache=false);

public sealed class CollectionService
{
    private CollectionState state = new(0, null, Array.Empty<CollectionEntry>(), true, null);
    private int refresh;
    public CollectionState State => Volatile.Read(ref state);
    public void RequestRefresh() => Interlocked.Exchange(ref refresh, 1);
    internal bool ConsumeRefresh() => Interlocked.Exchange(ref refresh, 0) == 1;
    internal void Publish(ulong characterId, IEnumerable<CollectionEntry> entries,IEnumerable<CollectionCategoryIssue>? issues=null,DateTimeOffset? observedAt=null,bool fromCache=false)
    {
        var copied = entries.Select(entry => characterId == 0 ? entry with { ConfirmedUnlocked = false,NegativeConfirmed=false } : entry)
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.Id).ToArray();
        if(copied.Any(entry=>entry.ConfirmedUnlocked && entry.NegativeConfirmed))throw new ArgumentException("Contradictory collection acquisition evidence.",nameof(entries));
        var copiedIssues=Array.AsReadOnly((issues ?? []).ToArray());
        Interlocked.Exchange(ref state, new(characterId, characterId == 0 ? null : observedAt ?? DateTimeOffset.UtcNow,
            Array.AsReadOnly(copied), false, null,copiedIssues,fromCache));
    }
    internal void Invalidate()
    {
        var previous = State;
        Interlocked.Exchange(ref state, new(0, null,
            Array.AsReadOnly(previous.Entries.Select(entry => entry with { ConfirmedUnlocked = false,NegativeConfirmed=false }).ToArray()), true, null));
    }
    internal void Fail(Exception error)
    {
        Invalidate();
        Interlocked.Exchange(ref state, State with { Loading = false, Error = error.Message });
    }
    internal void Clear() => Interlocked.Exchange(ref state, new(0, null, Array.Empty<CollectionEntry>(), false, null));
}
