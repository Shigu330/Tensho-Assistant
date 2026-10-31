using System.Globalization;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Community;

namespace TenshoAssistant.Monsters;

public sealed record MonsterReference(ulong Id, string Name, string Level, uint PlaceNameId);
// Shared item-source projection; the historical type name predates the fishing consumer.
public sealed record MonsterItemDocument(uint ItemId, DateTimeOffset FetchedAt, bool HasDrops, IReadOnlyList<MonsterReference> Monsters,
    FishingSources? Fishing=null,string? FishingError=null);
public sealed record MonsterDocument(ulong Id, string Name, string Level, uint PlaceNameId, uint InstanceId,
    float? X, float? Y, DateTimeOffset FetchedAt, bool HasDrops, IReadOnlyList<uint> DropItemIds);

public static class MonsterDocuments
{
    private const int EntryLimit = 4096;
    private static JsonDocument Parse(string json, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(json); cancellation.ThrowIfCancellationRequested();
        if (json.Length > GarlandClient.MaximumBytes || Encoding.UTF8.GetByteCount(json) > GarlandClient.MaximumBytes)
            throw new InvalidDataException("Monster document too large.");
        return JsonDocument.Parse(json,new JsonDocumentOptions { MaxDepth=32 });
    }
    private static ulong Id(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out var number) && number > 0 && number <= long.MaxValue) return number;
        if (value.ValueKind == JsonValueKind.String && ulong.TryParse(value.GetString(),NumberStyles.None,CultureInfo.InvariantCulture,out number) && number > 0 && number <= long.MaxValue) return number;
        throw new InvalidDataException("Invalid community entity identity.");
    }
    private static string Text(JsonElement value,string property) => value.TryGetProperty(property,out var text) && text.ValueKind == JsonValueKind.String ? text.GetString()! : "";
    private static uint OptionalId(JsonElement value,string property)
    {
        if (!value.TryGetProperty(property,out var field) || field.ValueKind == JsonValueKind.Null) return 0;
        if (field.ValueKind == JsonValueKind.Number && field.TryGetUInt32(out var number)) return number;
        throw new InvalidDataException("Invalid location identity.");
    }
    private static JsonElement Entity(JsonElement root,string property,ulong expected)
    {
        if (expected == 0 || !root.TryGetProperty(property,out var entity) || entity.ValueKind != JsonValueKind.Object
            || !entity.TryGetProperty("id",out var id) || Id(id) != expected)
            throw new InvalidDataException("Community document identity mismatch.");
        return entity;
    }
    public static MonsterItemDocument ParseItem(string json,uint expected,DateTimeOffset fetchedAt,CancellationToken cancellation = default)
    {
        using var document = Parse(json,cancellation);
        var item = Entity(document.RootElement,"item",expected);
        var dropIds = new HashSet<ulong>(); var ordered = new List<ulong>();
        var hasDrops = item.TryGetProperty("drops",out var drops);
        if (hasDrops)
        {
            if (drops.ValueKind != JsonValueKind.Array || drops.GetArrayLength() > EntryLimit) throw new InvalidDataException("Invalid monster list.");
            foreach (var drop in drops.EnumerateArray())
            { cancellation.ThrowIfCancellationRequested(); var id = Id(drop); if (dropIds.Add(id)) ordered.Add(id); }
        }
        var partials = new Dictionary<ulong,MonsterReference>();
        if (document.RootElement.TryGetProperty("partials",out var list))
        {
            if (list.ValueKind != JsonValueKind.Array || list.GetArrayLength() > EntryLimit) throw new InvalidDataException("Invalid partial list.");
            foreach (var partial in list.EnumerateArray())
            {
                cancellation.ThrowIfCancellationRequested();
                if (partial.ValueKind != JsonValueKind.Object || Text(partial,"type") != "mob") continue;
                if (!partial.TryGetProperty("id",out var identity)) throw new InvalidDataException("Missing mob identity.");
                var id = Id(identity);
                // Partials can contain unrelated reference entities; only item.drops establishes a link.
                if (!dropIds.Contains(id)) continue;
                if (!partial.TryGetProperty("obj",out var obj) || obj.ValueKind != JsonValueKind.Object
                    || !obj.TryGetProperty("i",out var objectId) || Id(objectId) != id)
                    throw new InvalidDataException("Mob partial identity mismatch.");
                var parsed = new MonsterReference(id,Text(obj,"n"),Text(obj,"l"),OptionalId(obj,"z"));
                if (partials.TryGetValue(id,out var prior) && prior != parsed) throw new InvalidDataException("Conflicting mob partials.");
                partials[id] = parsed;
            }
        }
        FishingSources? fishing=null;string? fishingError=null;
        try{fishing=FishingSourceParser.Parse(item,cancellation);}
        catch(InvalidDataException error){fishingError=error.Message;}
        return new(expected,fetchedAt.ToUniversalTime(),hasDrops,Array.AsReadOnly(ordered
            .Select(id => partials.GetValueOrDefault(id) ?? new MonsterReference(id,"","",0)).ToArray()),fishing,fishingError);
    }
    public static MonsterDocument ParseMonster(string json,ulong expected,DateTimeOffset fetchedAt,CancellationToken cancellation = default)
    {
        using var document = Parse(json,cancellation);
        var mob = Entity(document.RootElement,"mob",expected);
        var items = new List<uint>(); var unique = new HashSet<uint>();
        var hasDrops = mob.TryGetProperty("drops",out var drops);
        if (hasDrops)
        {
            if (drops.ValueKind != JsonValueKind.Array || drops.GetArrayLength() > EntryLimit) throw new InvalidDataException("Invalid mob item list.");
            foreach (var drop in drops.EnumerateArray())
            {
                cancellation.ThrowIfCancellationRequested(); var id = Id(drop);
                if (id > uint.MaxValue) throw new InvalidDataException("Invalid Item identity.");
                if (unique.Add((uint)id)) items.Add((uint)id);
            }
        }
        float? x = null, y = null;
        if (mob.TryGetProperty("coords",out var coords) && coords.ValueKind != JsonValueKind.Null)
        {
            if (coords.ValueKind != JsonValueKind.Array || coords.GetArrayLength() < 2
                || !coords[0].TryGetSingle(out var first) || !coords[1].TryGetSingle(out var second)
                || !float.IsFinite(first) || !float.IsFinite(second) || first is < -10000 or > 10000 || second is < -10000 or > 10000)
                throw new InvalidDataException("Invalid monster map coordinates.");
            x = first; y = second;
        }
        return new(expected,Text(mob,"name"),Text(mob,"lvl"),OptionalId(mob,"zoneid"),OptionalId(mob,"instance"),
            x,y,fetchedAt.ToUniversalTime(),hasDrops,Array.AsReadOnly(items.ToArray()));
    }
}
