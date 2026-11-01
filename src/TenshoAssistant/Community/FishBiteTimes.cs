using System.Net.Http;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Community;

/// <summary>Bite time of a fish at a spot (0.231): usual range in seconds, most frequent second and number of reports.</summary>
public sealed record FishBiteTime(uint FishId, int Minimum, int Maximum, int Usual, int Reports);

public static class FishBiteTimeSummary
{
    /// <summary>
    /// Range from Teamcraft's floored bite times (second, reports): seconds with at least 3 reports and 2 % of the fish's reports, so that a
    /// few wrong records do not stretch the range; the usual second is the most reported one.
    /// </summary>
    public static IReadOnlyDictionary<uint, FishBiteTime> Summarize(IEnumerable<(uint Fish, int Seconds, int Reports)> rows)
    {
        var result = new Dictionary<uint, FishBiteTime>();
        foreach (var fish in rows.Where(row => row.Seconds > 0 && row.Reports > 0).GroupBy(row => row.Fish))
        {
            var total = fish.Sum(row => row.Reports);
            var kept = fish.Where(row => row.Reports >= Math.Max(3, total * 0.02)).ToArray();
            if (kept.Length == 0) continue;
            var usual = kept.OrderByDescending(row => row.Reports).ThenBy(row => row.Seconds).First().Seconds;
            result[fish.Key] = new(fish.Key, kept.Min(row => row.Seconds), kept.Max(row => row.Seconds), usual, total);
        }
        return result;
    }
}

public sealed record FishBiteSpotState(IReadOnlyDictionary<uint, FishBiteTime>? Times, bool Loading, string? Error);
public sealed record FishBiteCache(List<int[]> Rows);

/// <summary>
/// Bite times from Teamcraft's public reports (gubal.ffxivteamcraft.com GraphQL, view bite_time_per_fish_per_spot, the query Teamcraft's
/// own client uses; Teamcraft is MIT): one request per fishing spot, one at a time, cached in « teamcraft-bites » for the sources lifetime.
/// </summary>
public sealed class FishBiteTimeService : IDisposable
{
    private const string Family = "teamcraft-bites", Version = "v1";
    private static readonly Uri Endpoint = new("https://gubal.ffxivteamcraft.com/graphql");
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly CancellationTokenSource cancellation = new();
    private readonly Func<TimeSpan> lifetime;
    private readonly DataCache? cache;
    private readonly object gate = new();
    private readonly Dictionary<uint, FishBiteSpotState> states = [];
    private readonly Queue<uint> pending = new();
    private Task? worker;
    public Func<bool>? Enabled { get; set; }

    public FishBiteTimeService(Func<TimeSpan> lifetime, DataCache? cache) { this.lifetime = lifetime; this.cache = cache; }

    /// <summary>Bite times of every fish of the spot; the first call queues the request.</summary>
    public FishBiteSpotState Get(uint spotId)
    {
        if (spotId == 0 || Enabled?.Invoke() == false) return new(null, false, null);
        lock (gate)
        {
            if (states.TryGetValue(spotId, out var known)) return known;
            var loading = new FishBiteSpotState(null, true, null); states[spotId] = loading;
            pending.Enqueue(spotId);
            if (worker is null || worker.IsCompleted) worker = Task.Run(DrainAsync);
            return loading;
        }
    }

    private async Task DrainAsync()
    {
        while (true)
        {
            uint spot;
            lock (gate) { if (pending.Count == 0 || cancellation.IsCancellationRequested) return; spot = pending.Dequeue(); }
            FishBiteSpotState result;
            try
            {
                var token = cancellation.Token;
                var cached = cache is null ? null : await cache.ReadAsync<FishBiteCache>(Family, spot.ToString(), Version, token).ConfigureAwait(false);
                List<int[]> rows;
                if (cached is not null && DateTimeOffset.UtcNow - cached.RecordedAt <= lifetime()) rows = cached.Value.Rows;
                else
                {
                    var query = "query($spot:Int){biteTimes:bite_time_per_fish_per_spot(where:{spot:{_eq:$spot},flooredBiteTime:{_gt:1,_lt:600},occurences:{_gte:3}}){itemId flooredBiteTime occurences}}";
                    var body = JsonSerializer.Serialize(new { query, variables = new { spot = (int)spot } });
                    using var response = await client.PostAsync(Endpoint, new StringContent(body, Encoding.UTF8, "application/json"), token).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var text = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    if (text.Length > 1024 * 1024) throw new InvalidDataException("Bite time response too large.");
                    using var document = JsonDocument.Parse(text);
                    rows = document.RootElement.GetProperty("data").GetProperty("biteTimes").EnumerateArray().Take(4000)
                        .Select(row => new[] { row.GetProperty("itemId").GetInt32(), row.GetProperty("flooredBiteTime").GetInt32(), row.GetProperty("occurences").GetInt32() })
                        .ToList();
                    if (cache is not null) await cache.WriteAsync(Family, spot.ToString(), Version, new FishBiteCache(rows), DateTimeOffset.UtcNow).ConfigureAwait(false);
                }
                result = new(FishBiteTimeSummary.Summarize(rows.Where(row => row.Length == 3 && row[0] > 0).Select(row => ((uint)row[0], row[1], row[2]))), false, null);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { return; }
            catch (Exception error) { result = new(null, false, error.Message); }
            lock (gate) states[spot] = result;
        }
    }

    public void Dispose() { cancellation.Cancel(); cancellation.Dispose(); client.Dispose(); }
}
