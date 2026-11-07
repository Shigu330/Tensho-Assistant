using System.Text.Json;

namespace TenshoAssistant.Market;

// 0.343 (I-01): Average = average sale price, Velocity = sales per day, both on the world (Universalis averageSalePrice / dailySaleVelocity).
public sealed record MarketQualityPrice(decimal? Minimum, decimal? RecentSale, DateTimeOffset? SaleAt, decimal? Average = null, decimal? Velocity = null);
// Cheapest listing across the data center of the queried world, with the world that holds it.
public sealed record MarketListing(decimal Price, uint WorldId, DateTimeOffset? UploadedAt);
// 0.343 (I-01): NqRegion / HqRegion = cheapest listing of the whole region (Universalis minListing.region), with its world.
public sealed record MarketQuote(uint WorldId, uint ItemId, DateTimeOffset FetchedAt, DateTimeOffset? UploadedAt,
    MarketQualityPrice Nq, MarketQualityPrice Hq, MarketListing? NqCheapest = null, MarketListing? HqCheapest = null,
    MarketListing? NqRegion = null, MarketListing? HqRegion = null)
{
    /// <summary>
    /// 0.711 (user report): what an item really sold for on this world, not what is asked for it: the lower of the last sale and the average sale, so that one odd sale does not lift it.
    /// HQ when asked and sold as such, else NQ. Null: no sale known.
    /// </summary>
    public (decimal Price,decimal? PerDay)? Sold(bool hq)
    {
        static decimal? Value(MarketQualityPrice price)=>price.Average is { } average && price.RecentSale is { } last ? Math.Min(average,last):price.Average ?? price.RecentSale;
        // 0.725 (user report): one old HQ sale says nothing when that quality no longer sells and the other one does.
        var hqStopped=Hq.Velocity==0 && Nq.Velocity>0 && Value(Nq) is not null;
        if(hq && !hqStopped && Value(Hq) is { } best)return (best,Hq.Velocity);
        return Value(Nq) is { } plain ? (plain,Nq.Velocity):null;
    }
    /// <summary>
    /// 0.725 (user report): the aggregated answer leaves out the speed of an item with no recent sale, and its window is short. The item's own answer gives the units sold a day over seven
    /// days, zero included: docs/evidence/0.725-sales-speed.md. Without this answer the speed stays as it was: unknown is not zero.
    /// </summary>
    public const int SpeedEntries=200;
    public static string SpeedUrl(uint world,IReadOnlyCollection<uint> ids)=>
        $"https://universalis.app/api/v2/{world}/{string.Join(',',ids)}?listings=0&entries={SpeedEntries}&fields={(ids.Count==1 ? "nqSaleVelocity,hqSaleVelocity":"items.nqSaleVelocity,items.hqSaleVelocity")}";
    public MarketQuote WithSpeed(string? json)
    {
        if(string.IsNullOrEmpty(json))return this;
        try
        {
            using var document=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});
            var row=document.RootElement;
            if(row.ValueKind!=JsonValueKind.Object)return this;
            if(row.TryGetProperty("items",out var items))
            {
                if(items.ValueKind!=JsonValueKind.Object || !items.TryGetProperty(ItemId.ToString(System.Globalization.CultureInfo.InvariantCulture),out row) || row.ValueKind!=JsonValueKind.Object)return this;
            }
            static decimal? Speed(JsonElement value,string field)=>value.TryGetProperty(field,out var speed) && speed.TryGetDecimal(out var perDay) && perDay>=0 ? perDay:null;
            return this with {Nq=Nq with {Velocity=Speed(row,"nqSaleVelocity") ?? Nq.Velocity},Hq=Hq with {Velocity=Speed(row,"hqSaleVelocity") ?? Hq.Velocity}};
        }
        catch(JsonException){return this;}
    }
    public bool HasRecentUpload(DateTimeOffset now) => Recent(UploadedAt, now);
    public static bool Recent(DateTimeOffset? uploaded, DateTimeOffset now) => uploaded is { } date
        && date <= now.AddMinutes(5) && now - date <= TimeSpan.FromHours(24);

    public static MarketQuote Parse(string json, uint worldId, uint itemId, DateTimeOffset fetchedAt)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
        var root = document.RootElement;
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Invalid Universalis response.");
        foreach (var row in results.EnumerateArray())
        {
            if (!row.TryGetProperty("itemId", out var id) || id.GetUInt32() != itemId) continue;
            var uploads = new Dictionary<uint, DateTimeOffset?>();
            if (row.TryGetProperty("worldUploadTimes", out var times) && times.ValueKind == JsonValueKind.Array)
                foreach (var time in times.EnumerateArray())
                    if (time.TryGetProperty("worldId", out var world) && world.TryGetUInt32(out var key)) uploads[key] = Timestamp(time, "timestamp");
            return new(worldId, itemId, fetchedAt.ToUniversalTime(), uploads.GetValueOrDefault(worldId), ReadQuality(row, "nq"), ReadQuality(row, "hq"),
                Cheapest(row, "nq", uploads), Cheapest(row, "hq", uploads), Cheapest(row, "nq", uploads, "region"), Cheapest(row, "hq", uploads, "region"));
        }
        throw new InvalidDataException("Item not resolved by Universalis for this world.");
    }
    private static MarketQualityPrice ReadQuality(JsonElement row, string quality)
    {
        if (!row.TryGetProperty(quality, out var value)) return new(null, null, null);
        var min = Scope(value, "minListing", "world");
        var sale = Scope(value, "recentPurchase", "world");
        var velocity = Scope(value, "dailySaleVelocity", "world") is { } speed && speed.TryGetProperty("quantity", out var quantity)
            && quantity.TryGetDecimal(out var perDay) && perDay >= 0 ? perDay : (decimal?)null;
        return new(Price(min), Price(sale), sale is { } entry ? Timestamp(entry, "timestamp") : null, Price(Scope(value, "averageSalePrice", "world")), velocity);
    }
    // A data-center minimum without an identified world is dropped rather than attributed to the current world.
    private static MarketListing? Cheapest(JsonElement row, string quality, IReadOnlyDictionary<uint, DateTimeOffset?> uploads, string scope = "dc")
        => row.TryGetProperty(quality, out var value) && Scope(value, "minListing", scope) is { } dc && Price(dc) is { } price
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
