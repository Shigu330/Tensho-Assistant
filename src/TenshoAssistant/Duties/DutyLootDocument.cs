using System.Text;
using System.Text.Json;

namespace TenshoAssistant.Duties;

public enum DutyLootSourceKind { Reward, FightCoffer, ZoneCoffer }
public sealed record DutyLootEntry(uint ItemId, DutyLootSourceKind Kind, int SourceIndex, string SourceName);
public sealed record DutyLootDocument(uint InstanceId, DateTimeOffset FetchedAt, bool HasRewards,
    bool HasFights, bool HasCoffers, IReadOnlyList<DutyLootEntry> Entries)
{
    public string Provider => "Garland Tools";
    public bool HasUnparsedFightDrops { get; init; }
    // Presence of all categories still does not establish an exhaustive drop table.
    public bool IsExhaustive => false;
}

public static class DutyLootParser
{
    public const int MaximumBytes = 1024 * 1024;
    public const int MaximumEntries = 16384;

    public static DutyLootDocument Parse(string json, uint expectedInstance, DateTimeOffset fetchedAt,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        cancellation.ThrowIfCancellationRequested();
        if (expectedInstance == 0) throw new ArgumentOutOfRangeException(nameof(expectedInstance));
        if (json.Length > MaximumBytes || Encoding.UTF8.GetByteCount(json) > MaximumBytes)
            throw new InvalidDataException("Duty loot document exceeds the size limit.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
        if (!document.RootElement.TryGetProperty("instance", out var instance) || instance.ValueKind != JsonValueKind.Object
            || !instance.TryGetProperty("id", out var id) || !id.TryGetUInt32(out var actual) || actual != expectedInstance)
            throw new InvalidDataException("Duty loot instance identity mismatch.");
        var entries = new List<DutyLootEntry>();
        var unique = new HashSet<(uint, DutyLootSourceKind, int)>();
        var unparsedFightDrops = false;
        void Items(JsonElement list, DutyLootSourceKind kind, int index, string name)
        {
            if (list.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Expected an item array.");
            if (list.GetArrayLength() > MaximumEntries) throw new InvalidDataException("Too many loot items.");
            foreach (var item in list.EnumerateArray())
            {
                cancellation.ThrowIfCancellationRequested();
                if (item.ValueKind != JsonValueKind.Number || !item.TryGetUInt32(out var itemId) || itemId == 0)
                    throw new InvalidDataException("Invalid loot item identity.");
                if (!unique.Add((itemId, kind, index))) continue;
                if (entries.Count >= MaximumEntries) throw new InvalidDataException("Too many loot entries.");
                entries.Add(new(itemId, kind, index, name));
            }
        }
        bool Group(string property, DutyLootSourceKind kind)
        {
            if (!instance.TryGetProperty(property, out var list)) return false;
            if (list.ValueKind != JsonValueKind.Array || list.GetArrayLength() > 4096)
                throw new InvalidDataException("Invalid loot source group.");
            var index = 0;
            foreach (var source in list.EnumerateArray())
            {
                cancellation.ThrowIfCancellationRequested();
                if (source.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Invalid loot source.");
                var name = source.TryGetProperty("name", out var text) && text.ValueKind == JsonValueKind.String ? text.GetString()! : "";
                var container = source;
                if (kind == DutyLootSourceKind.FightCoffer)
                {
                    if (source.TryGetProperty("drops",out var drops) && drops.ValueKind != JsonValueKind.Null
                        && (drops.ValueKind != JsonValueKind.Array || drops.GetArrayLength() > 0))
                        unparsedFightDrops = true;
                    if (!source.TryGetProperty("coffer", out container) || container.ValueKind == JsonValueKind.Null) { index++; continue; }
                    if (container.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Invalid fight coffer.");
                }
                if (!container.TryGetProperty("items", out var items)) throw new InvalidDataException("Missing coffer items.");
                Items(items, kind, index++, name);
            }
            return true;
        }
        var rewards = instance.TryGetProperty("rewards", out var rewardItems);
        if (rewards) Items(rewardItems, DutyLootSourceKind.Reward, 0, "");
        var fights = Group("fights", DutyLootSourceKind.FightCoffer);
        var coffers = Group("coffers", DutyLootSourceKind.ZoneCoffer);
        return new(expectedInstance, fetchedAt.ToUniversalTime(), rewards, fights, coffers, Array.AsReadOnly(entries.ToArray()))
        { HasUnparsedFightDrops = unparsedFightDrops };
    }
}
