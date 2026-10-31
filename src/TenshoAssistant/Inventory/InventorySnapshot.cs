using System.Collections.Frozen;

namespace TenshoAssistant.Inventory;

public readonly record struct InventoryOwnerId(ulong Value);
public readonly record struct InventoryContainerId(int Value);
public readonly record struct InventoryItemId(uint Value);
public readonly record struct InventoryLocation(InventoryOwnerId Owner, InventoryContainerId Container);
public readonly record struct InventoryItem(InventoryItemId Id, bool IsHighQuality);
public readonly record struct InventoryStack(InventoryItem Item, long Quantity, int Slot = -1, int DisplayIndex = -1);

/// <summary>Completeness within the explicitly requested scope, never all character storage.</summary>
public enum InventoryCoverage { Unknown, Partial, Complete }

/// <summary>ObservedQuantity is a lower bound unless coverage is Complete.</summary>
public readonly record struct InventoryQuantity(long ObservedQuantity, InventoryCoverage Coverage)
{
    public long? ExactQuantity => Coverage == InventoryCoverage.Complete ? ObservedQuantity : null;
}

/// <summary>A copied, immutable observation of one owner's container.</summary>
public sealed class InventoryContainerSnapshot
{
    private readonly FrozenDictionary<InventoryItem, long> quantities;
    private readonly IReadOnlyList<InventoryStack> stacks;

    public InventoryLocation Location { get; }
    public InventoryCoverage Coverage { get; }
    public IReadOnlyDictionary<InventoryItem, long> Quantities => quantities;
    public IReadOnlyList<InventoryStack> Stacks => stacks;

    public InventoryContainerSnapshot(InventoryLocation location, InventoryCoverage coverage,
        IEnumerable<InventoryStack> stacks)
    {
        ArgumentNullException.ThrowIfNull(stacks);
        if (location.Owner.Value == 0 || location.Container.Value < 0)
            throw new ArgumentException("An owner and a nonnegative container identifier are required.", nameof(location));
        if (!Enum.IsDefined(coverage)) throw new ArgumentOutOfRangeException(nameof(coverage));

        var copiedStacks = stacks.ToArray();
        var copied = new Dictionary<InventoryItem, long>();
        var seenSlots = new HashSet<int>();
        foreach (var stack in copiedStacks)
        {
            if (stack.Item.Id.Value == 0 || stack.Quantity <= 0 || stack.Slot < -1 || stack.DisplayIndex < -1)
                throw new ArgumentException("Stacks require a base item identifier and positive quantity.", nameof(stacks));
            if (coverage == InventoryCoverage.Unknown)
                throw new ArgumentException("Unknown containers cannot contain observed stacks.", nameof(stacks));
            if (stack.Slot >= 0 && !seenSlots.Add(stack.Slot))
                throw new ArgumentException("Inventory slots must be unique within a container.", nameof(stacks));
            copied[stack.Item] = checked(copied.GetValueOrDefault(stack.Item) + stack.Quantity);
        }

        Location = location;
        Coverage = coverage;
        this.stacks = Array.AsReadOnly(copiedStacks);
        quantities = copied.ToFrozenDictionary();
    }

    internal long GetObservedQuantity(InventoryItem item) => quantities.GetValueOrDefault(item);
}

/// <summary>One acquisition batch. Its timestamp applies to all contained observations.</summary>
public sealed class InventorySnapshot
{
    private readonly FrozenDictionary<InventoryLocation, InventoryContainerSnapshot> containers;

    public DateTimeOffset ObservedAt { get; }
    public string Source { get; }
    public IReadOnlyDictionary<InventoryLocation, InventoryContainerSnapshot> Containers => containers;

    public InventorySnapshot(DateTimeOffset observedAt, string source,
        IEnumerable<InventoryContainerSnapshot> containers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(containers);
        if (observedAt == default) throw new ArgumentException("Observation time is required.", nameof(observedAt));
        ObservedAt = observedAt.ToUniversalTime();
        Source = source;
        var copied = new Dictionary<InventoryLocation, InventoryContainerSnapshot>();
        foreach (var container in containers)
        {
            ArgumentNullException.ThrowIfNull(container);
            if (!copied.TryAdd(container.Location, container))
                throw new ArgumentException("Duplicate owner/container observation.", nameof(containers));
        }
        this.containers = copied.ToFrozenDictionary();
    }

    public InventoryQuantity GetQuantity(InventoryItem item, IEnumerable<InventoryLocation> scope)
    {
        if (item.Id.Value == 0) throw new ArgumentException("A base item identifier is required.", nameof(item));
        ArgumentNullException.ThrowIfNull(scope);
        var requested = scope.Distinct().ToArray();
        if (requested.Length == 0) throw new ArgumentException("Query scope cannot be empty.", nameof(scope));

        long total = 0;
        var allComplete = true;
        var anyObserved = false;
        foreach (var location in requested)
        {
            if (location.Owner.Value == 0 || location.Container.Value < 0)
                throw new ArgumentException("Invalid owner/container query scope.", nameof(scope));
            if (!containers.TryGetValue(location, out var container))
            {
                allComplete = false;
                continue;
            }

            allComplete &= container.Coverage == InventoryCoverage.Complete;
            anyObserved |= container.Coverage != InventoryCoverage.Unknown;
            total = checked(total + container.GetObservedQuantity(item));
        }

        return new InventoryQuantity(total, allComplete ? InventoryCoverage.Complete
            : anyObserved ? InventoryCoverage.Partial : InventoryCoverage.Unknown);
    }
}
