using System.Collections.Frozen;

namespace TenshoAssistant.Duties;

public sealed record DutyDefinition(uint DutyId, uint InstanceId, uint TerritoryId, string Name, string Category, uint Level, uint Icon = 0, uint UnlockQuest = 0);

/// <summary>T13 (0.648): the quest that unlocks a duty, where the game's sheets name one (docs/evidence/0.648-duty-unlocks.md). 0: not known, which does not mean none.</summary>
public static class DutyUnlocks
{
    /// <summary>ContentFinderCondition: UnlockType 1 means UnlockCriteria is a Quest row; same for the second pair.</summary>
    public static uint FromFinder(byte type,uint criteria,byte type2,uint criteria2)=>type==1 ? criteria:type2==1 ? criteria2:0;
    /// <summary>Quest.InstanceContentUnlock, the other way round, for a duty the finder row says nothing about.</summary>
    public static DutyDefinition[] Apply(IEnumerable<DutyDefinition> duties,IReadOnlyDictionary<uint,uint> questByInstance)
        =>duties.Select(duty=>duty.UnlockQuest==0 && questByInstance.TryGetValue(duty.InstanceId,out var quest) ? duty with {UnlockQuest=quest}:duty).ToArray();
}

public sealed class DutyCatalog
{
    private ILookup<uint,DutyDefinition>? unlocks;
    /// <summary>T13 (0.648): the duties this quest unlocks.</summary>
    public IEnumerable<DutyDefinition> UnlockedBy(uint quest)=>quest==0 ? []:(unlocks ??= Entries.ToLookup(duty=>duty.UnlockQuest))[quest];
    private readonly FrozenDictionary<uint, DutyDefinition> byDuty;
    public IReadOnlyList<DutyDefinition> Entries { get; }
    public DutyCatalog(IEnumerable<DutyDefinition> source, CancellationToken cancellation = default)
    {
        var copied = new Dictionary<uint, DutyDefinition>();
        foreach (var duty in source)
        {
            cancellation.ThrowIfCancellationRequested();
            if (duty.DutyId == 0 || duty.InstanceId == 0 || duty.TerritoryId == 0 || string.IsNullOrWhiteSpace(duty.Name)) continue;
            if (!copied.TryAdd(duty.DutyId, duty)) throw new ArgumentException("Duplicate duty finder identity.", nameof(source));
        }
        cancellation.ThrowIfCancellationRequested();
        byDuty = copied.ToFrozenDictionary();
        Entries = Array.AsReadOnly(copied.Values.OrderBy(duty => duty.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(duty => duty.DutyId).ToArray());
    }
    public DutyDefinition? Find(uint dutyId) => byDuty.GetValueOrDefault(dutyId);
}

public sealed record DutyCatalogState(DutyCatalog? Catalog, int Copied, int Total, string? Error);
public sealed class DutyCatalogService
{
    private DutyCatalogState state = new(null, 0, 0, null);
    public DutyCatalogState State => Volatile.Read(ref state);
    internal void Progress(int copied, int total) => Interlocked.Exchange(ref state, new(null,copied,total,null));
    internal void Publish(DutyCatalog catalog) => Interlocked.Exchange(ref state, new(catalog,0,0,null));
    internal void Fail(Exception error) => Interlocked.Exchange(ref state, new(null,0,0,error.Message));
    internal void Clear() => Interlocked.Exchange(ref state, new(null,0,0,null));
}
