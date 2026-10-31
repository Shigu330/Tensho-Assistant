using System.Text.Json;

namespace TenshoAssistant.Market;

public sealed record MarketQualityPrice(decimal? Minimum, decimal? RecentSale, DateTimeOffset? SaleAt);
// Cheapest listing across the data center of the queried world, with the world that holds it.
public sealed record MarketListing(decimal Price, uint WorldId, DateTimeOffset? UploadedAt);
public sealed record MarketQuote(uint WorldId, uint ItemId, DateTimeOffset FetchedAt, DateTimeOffset? UploadedAt,
    MarketQualityPrice Nq, MarketQualityPrice Hq, MarketListing? NqCheapest = null, MarketListing? HqCheapest = null)
{
    public bool HasRecentUpload(DateTimeOffset now) => Recent(UploadedAt, now);
    public static bool Recent(DateTimeOffset? uploaded, DateTimeOffset now) => uploaded is { } date
        && date <= now.AddMinutes(5) && now - date <= TimeSpan.FromHours(24);

    public static MarketQuote Parse(string json, uint worldId, uint itemId, DateTimeOffset fetchedAt)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
        var root = document.RootElement;
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Réponse Universalis invalide.");
        foreach (var row in results.EnumerateArray())
        {
            if (!row.TryGetProperty("itemId", out var id) || id.GetUInt32() != itemId) continue;
            var uploads = new Dictionary<uint, DateTimeOffset?>();
            if (row.TryGetProperty("worldUploadTimes", out var times) && times.ValueKind == JsonValueKind.Array)
                foreach (var time in times.EnumerateArray())
                    if (time.TryGetProperty("worldId", out var world) && world.TryGetUInt32(out var key)) uploads[key] = Timestamp(time, "timestamp");
            return new(worldId, itemId, fetchedAt.ToUniversalTime(), uploads.GetValueOrDefault(worldId), ReadQuality(row, "nq"), ReadQuality(row, "hq"),
                Cheapest(row, "nq", uploads), Cheapest(row, "hq", uploads));
        }
        throw new InvalidDataException("Objet non résolu par Universalis pour ce monde.");
    }
    private static MarketQualityPrice ReadQuality(JsonElement row, string quality)
    {
        if (!row.TryGetProperty(quality, out var value)) return new(null, null, null);
        var min = Scope(value, "minListing", "world");
        var sale = Scope(value, "recentPurchase", "world");
        return new(Price(min), Price(sale), sale is { } entry ? Timestamp(entry, "timestamp") : null);
    }
    // A data-center minimum without an identified world is dropped rather than attributed to the current world.
    private static MarketListing? Cheapest(JsonElement row, string quality, IReadOnlyDictionary<uint, DateTimeOffset?> uploads)
        => row.TryGetProperty(quality, out var value) && Scope(value, "minListing", "dc") is { } dc && Price(dc) is { } price
            && dc.TryGetProperty("worldId", out var world) && world.TryGetUInt32(out var worldId) && worldId != 0
            ? new(price, worldId, uploads.GetValueOrDefault(worldId)) : null;
    private static JsonElement? Scope(JsonElement value, string field, string scope) =>
        value.TryGetProperty(field, out var group) && group.ValueKind == JsonValueKind.Object
        && group.TryGetProperty(scope, out var entry) && entry.ValueKind == JsonValueKind.Object ? entry : null;
    private static decimal? Price(JsonElement? value) => value is { } row && row.TryGetProperty("price", out var price)
        && price.TryGetDecimal(out var amount) && amount > 0 ? amount : null;
    private static DateTimeOffset? Timestamp(JsonElement value, string field) =>
        value.TryGetProperty(field, out var timestamp) && timestamp.TryGetInt64(out var milliseconds) && milliseconds > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds) : null;
}
