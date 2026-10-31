using System.Collections.Frozen;

namespace TenshoAssistant.Duties;

public sealed record DutyDefinition(uint DutyId, uint InstanceId, uint TerritoryId, string Name, string Category, uint Level, uint Icon = 0);

public sealed class DutyCatalog
{
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
