using System.Net.Http;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Community;

namespace TenshoAssistant.Market;

/// <summary>A retainer's listing as the game's sale list shows it. UnitPrice is null when its text could not be read.</summary>
public sealed record RetainerListing(int Row, uint ItemId, bool Hq, int Quantity, long? UnitPrice);
public enum RetainerSalesStatus { Closed, Ready, Unavailable }
public sealed record RetainerSalesState(RetainerSalesStatus Status, IReadOnlyList<RetainerListing> Listings, IReadOnlySet<string> OwnNames, string HomeWorld, bool AtHome);
public enum RetainerVerdictKind { Unknown, PriceUnknown, Alone, Lowest, Tied, Undercut }
/// <summary>Paste: the price to copy, only when another seller is at or under yours. Null otherwise.</summary>
public sealed record RetainerVerdict(RetainerVerdictKind Kind, long? Lowest, long? Gap, string? Competitor, long? Paste);
/// <summary>What Universalis holds on an item. DataAt null: nobody has uploaded it, which says nothing about its sellers.</summary>
public sealed record RetainerItemOffers(IReadOnlyList<MarketOffer> Offers, DateTimeOffset? DataAt);
public sealed record RetainerCompareState(bool Loading, IReadOnlyDictionary<uint, RetainerItemOffers>? Items, string? Error, DateTimeOffset? FetchedAt, string World);

/// <summary>
/// I-05 (issue #26, design checkpoints/features-design-2026-11-03.json with its review): a retainer's sale list against the other sellers. Read-only: nothing is
/// listed, repriced or moved. The prices are those of the list the game itself shows (AgentRetainer.SellListEntries), not the shared price array, which can hold
/// another retainer's prices just after a switch. Universalis is asked on the player's click only.
/// </summary>
public static class RetainerSales
{
    public const int MaximumListings = 20;
    private const long MaximumPrice = 999_999_999;

    /// <summary>The digits of a price as the game writes it (separators and the gil sign left out); null if it is not a plausible price.</summary>
    public static long? ParsePrice(string? text)
    {
        if (text is null) return null;
        long price = 0;
        var digits = 0;
        foreach (var letter in text)
        {
            if (letter is < '0' or > '9') continue;
            price = price * 10 + (letter - '0');
            if (++digits > 10) return null;
        }
        return digits == 0 || price is <= 0 or > MaximumPrice ? null : price;
    }

    /// <summary>The item and quality from the game's raw number, as elsewhere in the plugin: 1 000 000 and above is high quality.</summary>
    public static (uint Item, bool Hq) SplitItem(uint raw) => (raw % 500_000, raw >= 1_000_000);

    /// <summary>
    /// Only a seller of the same quality who is not one of the player's retainers counts. No data is Unknown, never « you are the lowest »: an item nobody uploaded,
    /// or a list cut at 100 rows of which none is comparable (Universalis mixes qualities before cutting).
    /// </summary>
    public static RetainerVerdict Compare(RetainerListing listing, RetainerItemOffers? offers, IReadOnlySet<string> ownNames)
    {
        if (listing.UnitPrice is not { } yours) return new(RetainerVerdictKind.PriceUnknown, null, null, null, null);
        if (offers is not { DataAt: not null }) return new(RetainerVerdictKind.Unknown, null, null, null, null);
        var lowest = offers.Offers.Where(offer => offer.Hq == listing.Hq && !ownNames.Contains(offer.Retainer)).OrderBy(offer => offer.UnitPrice).FirstOrDefault();
        if (lowest is null) return new(offers.Offers.Count >= MarketBoard.MaximumOffers ? RetainerVerdictKind.Unknown : RetainerVerdictKind.Alone, null, null, null, null);
        var gap = yours - lowest.UnitPrice;
        var paste = Math.Max(1, lowest.UnitPrice - 1);
        return gap > 0 ? new(RetainerVerdictKind.Undercut, lowest.UnitPrice, gap, lowest.Retainer, paste)
            : gap == 0 ? new(RetainerVerdictKind.Tied, lowest.UnitPrice, 0, lowest.Retainer, paste)
            : new(RetainerVerdictKind.Lowest, lowest.UnitPrice, gap, lowest.Retainer, null);
    }

    /// <summary>Universalis' two answers: several items under « items », or one item at the root.</summary>
    public static IReadOnlyDictionary<uint, RetainerItemOffers> ParseMany(string json, string world, IReadOnlyList<uint> asked, DateTimeOffset now)
    {
        var result = new Dictionary<uint, RetainerItemOffers>();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        void Add(uint item, JsonElement view)
        {
            var uploaded = view.TryGetProperty("lastUploadTime", out var time) && time.TryGetInt64(out var at) && at > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(at) : (DateTimeOffset?)null;
            result[item] = new(MarketBoard.Parse(view.GetRawText(), world, item, now).Offers, uploaded);
        }
        if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in items.EnumerateObject())
                if (uint.TryParse(property.Name, out var id) && asked.Contains(id)) Add(id, property.Value);
        }
        else if (asked.Count == 1) Add(asked[0], root);
        return result;
    }
}

/// <summary>One request for a retainer's items, on the player's click, at least 3 s apart, bounded in size. Its own short path: it shares no state with the market page.</summary>
public sealed class RetainerCompareService : IDisposable
{
    private const int MaximumBytes = 4 * 1024 * 1024;
    private static readonly TimeSpan Pause = TimeSpan.FromSeconds(3);
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly object gate = new();
    private readonly CancellationTokenSource cancellation = new();
    private RetainerCompareState state = new(false, null, null, null, "");
    private DateTimeOffset nextAllowed;
    private bool disposed;
    public Func<bool>? Enabled { get; set; }
    public RetainerCompareState State { get { lock (gate) return state; } }

    public RetainerCompareService(HttpClient? client = null)
    {
        ownsClient = client is null;
        this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    public static string Address(string world, IEnumerable<uint> items)
        => $"https://universalis.app/api/v2/{Uri.EscapeDataString(world)}/{string.Join(',', items)}?listings={MarketBoard.MaximumOffers}&entries=0";

    /// <summary>False when it was not sent: already running, too soon, source off, or nothing to ask.</summary>
    public bool Request(string world, IEnumerable<uint> items, DateTimeOffset now)
    {
        var asked = items.Distinct().Take(RetainerSales.MaximumListings).ToArray();
        lock (gate)
        {
            if (disposed || state.Loading || now < nextAllowed || asked.Length == 0 || world.Length == 0 || Enabled?.Invoke() == false) return false;
            nextAllowed = now + Pause;
            state = state with { Loading = true, Error = null, World = world };
        }
        _ = Task.Run(() => Fetch(world, asked));
        return true;
    }

    private async Task Fetch(string world, IReadOnlyList<uint> asked)
    {
        RetainerCompareState result;
        try
        {
            // X-76 (step 2): the shared bounded reader (size limit, Tensho's User-Agent, strict UTF-8).
            var json = await BoundedHttp.ReadAsync(client, new Uri(Address(world, asked)), MaximumBytes, cancellation.Token).ConfigureAwait(false);
            var now = DateTimeOffset.UtcNow;
            result = new(false, RetainerSales.ParseMany(json, world, asked, now), null, now, world);
        }
        catch (OperationCanceledException) { result = new(false, null, "Network", null, world); }
        catch (HttpRequestException) { result = new(false, null, "Network", null, world); }
        catch (Exception error) when (error is JsonException or InvalidDataException or KeyNotFoundException or FormatException or DecoderFallbackException) { result = new(false, null, "Data", null, world); }
        lock (gate) if (!disposed) state = result;
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
        }
        cancellation.Cancel();
        cancellation.Dispose();
        if (ownsClient) client.Dispose();
    }
}
