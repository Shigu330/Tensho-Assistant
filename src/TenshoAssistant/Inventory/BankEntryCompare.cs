namespace TenshoAssistant.Inventory;

/// <summary>
/// 0.605: an automatic reading of an unchanged storage does not need the disk: every stack the same, same coverage, and the file's date under an hour old.
/// </summary>
public static class BankEntryCompare
{
    public static readonly TimeSpan LongestWithoutSave = TimeSpan.FromHours(1);

    public static bool SameContents(BankEntry a, BankEntry b)
    {
        if (a.Key != b.Key || a.Name != b.Name || a.Snapshot.Containers.Count != b.Snapshot.Containers.Count) return false;
        foreach (var (location, container) in a.Snapshot.Containers)
            if (!b.Snapshot.Containers.TryGetValue(location, out var other) || container.Coverage != other.Coverage || !container.Stacks.SequenceEqual(other.Stacks)) return false;
        return true;
    }

    /// <summary>Only for an entry written in this session: after a restart the first reading is always written.</summary>
    public static bool CanSkipSave(BankEntry? current, BankEntry incoming, IReadOnlyDictionary<BankKey, DateTimeOffset> savedAt)
        => current is not null && savedAt.TryGetValue(incoming.Key, out var saved) && incoming.Snapshot.ObservedAt - saved < LongestWithoutSave && incoming.Snapshot.ObservedAt >= saved && SameContents(current, incoming);

    /// <summary>X-182 (0.1173, night review N26): the reading matches what is stored (same contents, the stored date under an hour older).</summary>
    public static bool StillSaved(BankEntry stored, BankEntry incoming)
        => incoming.Snapshot.ObservedAt >= stored.Snapshot.ObservedAt && incoming.Snapshot.ObservedAt - stored.Snapshot.ObservedAt < LongestWithoutSave && SameContents(stored, incoming);
}

