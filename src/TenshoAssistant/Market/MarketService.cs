using System.Diagnostics;
using System.Net.Http;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Market;

// WorldNames holds the public worlds of the current data center, copied from game data on the framework thread.
public sealed record MarketState(uint WorldId, string WorldName, uint ItemId, MarketQuote? Quote, bool Loading, string? Error,
    string DataCenterName = "", IReadOnlyDictionary<uint, string>? WorldNames = null)
{
    public string? NameOf(uint worldId) => worldId == WorldId ? WorldName : WorldNames?.GetValueOrDefault(worldId);
}
public sealed record MarketBackgroundState(uint WorldId, uint ItemId, bool Loading, string? Error, DateTimeOffset? RetryAt = null);

public sealed class MarketService : IDisposable
{
    private enum Purpose { Manual, Hover, Preparation }
    private readonly object gate = new();
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly Dictionary<(uint World, uint Item), MarketQuote> cache = [];
    private CancellationTokenSource? request;
    private MarketState state = new(0, "", 0, null, false, null);
    private ulong owner;
    private int generation;
    private bool disposed;
    private long? lastRequest;
    private bool requestActive;
    private Purpose activePurpose;
    private MarketBackgroundState? backgroundState;
    private Func<TimeSpan> cacheLifetime=()=>TimeSpan.FromMinutes(5);
    private DataCache? disk;
    public TimeSpan CacheLifetime=>Volatile.Read(ref cacheLifetime)();
    public bool IsFresh(MarketQuote quote,DateTimeOffset now)=>CacheAge.Fresh(quote.FetchedAt,now,CacheLifetime);
    public void ConfigureCache(Func<TimeSpan> lifetime,DataCache store){lock(gate){cacheLifetime=lifetime;disk=store;}}
    public MarketBackgroundState? BackgroundState => Volatile.Read(ref backgroundState);
    public MarketState State => Volatile.Read(ref state);

    public MarketService(HttpClient? client = null)
    {
        ownsClient = client is null;
        this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(12), MaxResponseContentBufferSize = 1048576 };
        this.client.DefaultRequestHeaders.UserAgent.ParseAdd($"TenshoAssistant/{typeof(MarketService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}");
    }
    public void SetWorld(ulong character, uint worldId, string worldName, string dataCenterName = "", IReadOnlyDictionary<uint, string>? worldNames = null)
    {
        lock (gate)
        {
            if (disposed || (owner == character && state.WorldId == worldId)) return;
            owner = character;
            generation++;
            request?.Cancel();
            requestActive = false; backgroundState = null;
            state = character == 0 ? new(0, "", 0, null, false, null)
                : new(worldId, worldName, 0, null, false, null, dataCenterName, worldNames is null ? null : new Dictionary<uint, string>(worldNames));
        }
    }
    public MarketQuote? GetCached(uint worldId, uint itemId)
    { lock (gate) { return disposed ? null : cache.GetValueOrDefault((worldId, itemId)); } }
    public void CancelBackground()
    {
        lock (gate)
        {
            if (disposed) return;
            backgroundState = null;
            if (!requestActive || activePurpose != Purpose.Hover) return;
            generation++; request?.Cancel(); requestActive = false;
        }
    }
    public async Task RequestAsync(uint itemId, bool force = false, bool background = false)
        => _ = await RequestCoreAsync(itemId,force,background ? Purpose.Hover : Purpose.Manual).ConfigureAwait(false);
    public Task<MarketFetchResult> RequestHoverAsync(uint itemId,ulong character,uint world)
        => RequestCoreAsync(itemId,false,Purpose.Hover,character,world);

    public Task<MarketFetchResult> RequestForPreparationAsync(uint itemId, ulong character, uint world, CancellationToken cancellation = default)
        => RequestCoreAsync(itemId,false,Purpose.Preparation,character,world,cancellation);

    private async Task<MarketFetchResult> RequestCoreAsync(uint itemId, bool force, Purpose purpose,
        ulong? expectedOwner = null, uint? expectedWorld = null, CancellationToken externalCancellation = default)
    {
        uint savedWorld;ulong savedOwner;
        lock(gate){if(disposed)return new(MarketFetchStatus.ScopeChanged);savedWorld=state.WorldId;savedOwner=owner;}
        expectedWorld ??= savedWorld;expectedOwner ??= savedOwner;
        if(!force && savedWorld!=0 && itemId!=0 && disk is { } store && GetCached(savedWorld,itemId) is null)
        {
            var saved=await store.ReadAsync<MarketQuote>("market",$"{savedWorld}:{itemId}","universalis-aggregated-v2",externalCancellation).ConfigureAwait(false);
            lock(gate)if(!disposed && owner==savedOwner && state.WorldId==savedWorld && saved?.Value is { } value
                && value.WorldId==savedWorld && value.ItemId==itemId && value.FetchedAt==saved.RecordedAt && value.Nq is not null && value.Hq is not null)
            {
                if(cache.Count>=128)cache.Remove(cache.MinBy(pair=>pair.Value.FetchedAt).Key);
                cache[(savedWorld,itemId)]=value;
            }
        }
        uint world;
        ulong requestOwner;
        int ticket;
        CancellationToken token;
        lock (gate)
        {
            if (externalCancellation.IsCancellationRequested) return new(MarketFetchStatus.Cancelled);
            if (disposed || itemId == 0 || state.WorldId == 0 || (expectedOwner is { } expected && (expected == 0 || expected != owner))
                || (expectedWorld is { } expectedId && expectedId != state.WorldId)) return new(MarketFetchStatus.ScopeChanged);
            if (requestActive)
            {
                var preempt = purpose == Purpose.Manual && activePurpose != Purpose.Manual
                    || purpose == Purpose.Preparation && activePurpose == Purpose.Hover;
                if (!preempt) return new(MarketFetchStatus.Busy,RetryAfter:TimeSpan.FromMilliseconds(250));
                generation++; request?.Cancel(); requestActive = false;
                if (activePurpose == Purpose.Hover) backgroundState = null;
            }
            world = state.WorldId;
            requestOwner = owner;
            if (!force && cache.TryGetValue((world, itemId), out var quote) && IsFresh(quote,DateTimeOffset.UtcNow))
            {
                if (purpose == Purpose.Hover) backgroundState = new(world, itemId, false, null);
                else if (purpose == Purpose.Manual) state = state with { ItemId = itemId, Quote = quote, Loading = false, Error = null };
                return new(MarketFetchStatus.Success,quote);
            }
            if (lastRequest is { } last && Stopwatch.GetElapsedTime(last) < TimeSpan.FromSeconds(3))
            {
                if (purpose == Purpose.Hover) backgroundState = new(world, itemId, false, "Patientez quelques secondes.");
                else if (purpose == Purpose.Manual) state = state with { ItemId = itemId, Quote = null, Error = "Patientez quelques secondes avant une nouvelle requête." };
                return new(MarketFetchStatus.Cooldown,RetryAfter:TimeSpan.FromSeconds(3)-Stopwatch.GetElapsedTime(last));
            }
            lastRequest = Stopwatch.GetTimestamp();
            request?.Dispose();
            request = externalCancellation.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(externalCancellation) : new CancellationTokenSource();
            token = request.Token;
            ticket = ++generation;
            requestActive = true; activePurpose = purpose;
            if (purpose == Purpose.Hover) backgroundState = new(world, itemId, true, null);
            else if (purpose == Purpose.Manual) state = state with { ItemId = itemId, Quote = null, Loading = true, Error = null };
        }
        try
        {
            var json = await client.GetStringAsync($"https://universalis.app/api/v2/aggregated/{world}/{itemId}", token).ConfigureAwait(false);
            var quote = MarketQuote.Parse(json, world, itemId, DateTimeOffset.UtcNow);
            lock (gate)
            {
                if (disposed || ticket != generation) return Interrupted();
                if (externalCancellation.IsCancellationRequested) { requestActive = false; return new(MarketFetchStatus.Cancelled); }
                if (!cache.ContainsKey((world,itemId)) && cache.Count >= 128) cache.Remove(cache.MinBy(pair => pair.Value.FetchedAt).Key);
                cache[(world, itemId)] = quote;
                if(disk is { } diskStore)_=diskStore.WriteAsync("market",$"{world}:{itemId}","universalis-aggregated-v2",quote,quote.FetchedAt);
                requestActive = false;
                if (purpose == Purpose.Hover) backgroundState = new(world, itemId, false, null);
                else if (purpose == Purpose.Manual) state = state with { Quote = quote, Loading = false, Error = null };
                return new(MarketFetchStatus.Success,quote);
            }
        }
        catch (Exception error)
        {
            lock (gate)
            {
                if (disposed || ticket != generation) return Interrupted();
                requestActive = false;
                var message = error is OperationCanceledException ? "Requête annulée ou délai dépassé." : $"Lecture des prix impossible : {error.Message}";
                if (purpose == Purpose.Hover) backgroundState = new(world, itemId, false, message, DateTimeOffset.UtcNow.AddSeconds(30));
                else if (purpose == Purpose.Manual) state = state with { Loading = false, Quote = null, Error = message };
                return new(externalCancellation.IsCancellationRequested ? MarketFetchStatus.Cancelled : MarketFetchStatus.Failed,Error:message);
            }
        }
        MarketFetchResult Interrupted() => new(externalCancellation.IsCancellationRequested ? MarketFetchStatus.Cancelled
            : disposed || requestOwner != owner || state.WorldId != world ? MarketFetchStatus.ScopeChanged : MarketFetchStatus.Preempted);
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            generation++;
            request?.Cancel(); request?.Dispose();
            cache.Clear();
            requestActive = false; backgroundState = null;
            state = new(0, "", 0, null, false, null);
            if (ownsClient) client.Dispose();
        }
    }
}
