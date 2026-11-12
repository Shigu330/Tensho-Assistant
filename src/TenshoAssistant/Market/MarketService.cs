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

/// <summary>
/// Universalis prices of one item on the current world (https://universalis.app/api/v2/aggregated/{world}/{item}), for the Market
/// page (manual), the craft cost lists (preparation) and the item hover panel (background). One request at a time (a manual request
/// preempts the others, a preparation one preempts the hover), 3 s between requests, 12 s timeout, 1 MB response cap; quotes kept in
/// memory and on disk (DataCache « market ») for the « Prices » cache duration.
/// </summary>
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
    /// <summary>0.608 (I4): what Universalis answers in one request (docs/evidence/0.608-universalis-batch.md: past it, items are dropped without a message).</summary>
    public const int BatchMaximum=100;
    /// <summary>0.710 (user request): quotes kept in memory, for a list of several hundred items: with 128, the first ones of a long list were gone before its end.</summary>
    public const int CacheCapacity=4096;
    /// <summary>0.710: a long list goes one request at a time, this far apart: docs/evidence/0.710-market-pace.md.</summary>
    public static readonly TimeSpan ListPause=TimeSpan.FromSeconds(1);
    public static readonly TimeSpan BusyServerPause=TimeSpan.FromSeconds(30);
    public static uint[] BatchIds(IEnumerable<uint> ids,Func<uint,bool> known,int minimum=2)
    {
        var missing=ids.Where(id=>id!=0).Distinct().Where(id=>!known(id)).Take(BatchMaximum).ToArray();
        return missing.Length>=minimum ? missing:[];
    }
    /// <summary>0.710: how long a server that asks for room gets: what it says, at least the usual pause. Any other answer: none.</summary>
    public static TimeSpan? ServerPause(int status,TimeSpan? retryAfter)=>status is 429 or 503 ? retryAfter is { } asked && asked>BusyServerPause ? asked:BusyServerPause:null;
    private long? serverPauseUntil;

    /// <summary>
    /// 0.608 (I4): one request for a list: its quotes go to the cache, and the usual requests then find them. Under the same pause as the others. An item
    /// without a price stays theirs. The message of a server failure is returned: the list stops there rather than asking again for each item.
    /// </summary>
    public async Task<string?> FillCacheAsync(IReadOnlyList<uint> ids,ulong expectedOwner,uint expectedWorld,CancellationToken token,bool bulk=false)
    {
        if(Enabled?.Invoke()==false)return null;
        uint world;uint[] missing;
        while(true)
        {
            // X-143 part 2 (0.1124, audit A07): the setting read again at each turn, after every wait: turned off meanwhile, nothing leaves.
            if(Enabled?.Invoke()==false)return null;
            TimeSpan wait;
            lock(gate)
            {
                if(disposed || state.WorldId==0 || expectedWorld!=state.WorldId || expectedOwner==0 || expectedOwner!=owner)return null;
                if(serverPauseUntil is { } until && Stopwatch.GetTimestamp()<until && bulk)return "Universalis asked for a pause.";
                var pause=bulk ? ListPause:TimeSpan.FromSeconds(3);
                var since=lastRequest is { } last ? Stopwatch.GetElapsedTime(last):pause;
                wait=requestActive ? TimeSpan.FromMilliseconds(250):since<pause ? pause-since:TimeSpan.Zero;
                if(wait==TimeSpan.Zero || !bulk)
                {
                    // 0.710: a long list waits for its turn; a short one leaves it to the usual requests, as before.
                    if(wait!=TimeSpan.Zero)return null;
                    world=state.WorldId;
                    var now=DateTimeOffset.UtcNow;
                    missing=BatchIds(ids,id=>cache.TryGetValue((world,id),out var known) && IsFresh(known,now),bulk ? 1:2);
                    if(missing.Length==0)return null;
                    lastRequest=Stopwatch.GetTimestamp();
                    break;
                }
            }
            await Task.Delay(wait,token).ConfigureAwait(false);
        }
        try
        {
            using var response=await client.GetAsync($"https://universalis.app/api/v2/aggregated/{world}/{string.Join(',',missing)}",token).ConfigureAwait(false);
            if(ServerPause((int)response.StatusCode,response.Headers.RetryAfter?.Delta) is { } room)
            {
                lock(gate)serverPauseUntil=Stopwatch.GetTimestamp()+(long)(room.TotalSeconds*Stopwatch.Frequency);
                return $"Universalis asked for a pause ({(int)response.StatusCode}).";
            }
            response.EnsureSuccessStatusCode();
            var json=await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            var at=DateTimeOffset.UtcNow;
            var speed=await SpeedAsync(world,missing,token).ConfigureAwait(false);
            // Read off the lock: a page reads the cache on every frame.
            var quotes=new List<MarketQuote>(missing.Length);
            foreach(var id in missing)
            {
                try{quotes.Add(MarketQuote.Parse(json,world,id,at).WithSpeed(speed));}
                catch(InvalidDataException){}
            }
            lock(gate)
            {
                if(disposed || state.WorldId!=world)return null;
                foreach(var quote in quotes)
                {
                    var id=quote.ItemId;
                    if(!cache.ContainsKey((world,id)) && cache.Count>=CacheCapacity)cache.Remove(cache.MinBy(pair=>pair.Value.FetchedAt).Key);
                    cache[(world,id)]=quote;
                    if(disk is { } diskStore)_=diskStore.WriteAsync("market",$"{world}:{id}","universalis-aggregated-v4",quote,quote.FetchedAt);
                }
                lastRequest=Stopwatch.GetTimestamp();
            }
            return null;
        }
        catch(Exception error) when(error is HttpRequestException or System.Text.Json.JsonException or InvalidDataException){return $"Prices could not be read: {error.Message}";}
    }

    // 0.725: the second answer of a price request. Refused or failed: the prices stay, the speed stays unknown, and a server asking for room gets it.
    private async Task<string?> SpeedAsync(uint world,IReadOnlyCollection<uint> ids,CancellationToken token)
    {
        // X-143 part 2 (0.1124, audit A07): the second request (sale speed) asks the setting again too.
        if(Enabled?.Invoke()==false)return null;
        try
        {
            using var response=await client.GetAsync(MarketQuote.SpeedUrl(world,ids),token).ConfigureAwait(false);
            if(ServerPause((int)response.StatusCode,response.Headers.RetryAfter?.Delta) is { } room)
            {
                lock(gate)serverPauseUntil=Stopwatch.GetTimestamp()+(long)(room.TotalSeconds*Stopwatch.Frequency);
                return null;
            }
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(token).ConfigureAwait(false):null;
        }
        catch(HttpRequestException){return null;}
        catch(TaskCanceledException) when(!token.IsCancellationRequested){return null;}
    }

    public bool IsFresh(MarketQuote quote,DateTimeOffset now)=>CacheAge.Fresh(quote.FetchedAt,now,CacheLifetime);
    public void ConfigureCache(Func<TimeSpan> lifetime,DataCache store){lock(gate){cacheLifetime=lifetime;disk=store;}}
    public MarketBackgroundState? BackgroundState => Volatile.Read(ref backgroundState);
    // 0.324: the « Universalis » switch (Settings › Advanced); off = no request for prices, hover or preparation (copies still read).
    public const string SourceOff = "Universalis" + Community.SourceDisabledException.Suffix;
    public Func<bool>? Enabled { get; set; }
    public MarketState State => Volatile.Read(ref state);

    public MarketService(HttpClient? client = null)
    {
        ownsClient = client is null;
        this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(12), MaxResponseContentBufferSize = 1048576 };
        // X-76 (step 2): Tensho's one User-Agent, on its own client only (a given client keeps its headers).
        if (ownsClient) this.client.DefaultRequestHeaders.UserAgent.ParseAdd(Community.BoundedHttp.UserAgent);
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
    // X-52 (0.817, user request): the prices seen at the game's market board, used in place of Universalis' when newer (set by the plugin).
    public Func<uint,uint,MarketQuote?,MarketQuote?>? Seen { get; set; }
    public MarketQuote? GetCached(uint worldId, uint itemId)
    {
        MarketQuote? cached;
        lock (gate) { if (disposed) return null; cached = cache.GetValueOrDefault((worldId, itemId)); }
        return Seen?.Invoke(worldId, itemId, cached) ?? cached;
    }
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
    public Task<MarketFetchResult> RequestHoverAsync(uint itemId,ulong character,uint world,bool force=false)
        => RequestCoreAsync(itemId,force,Purpose.Hover,character,world);

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
            // 0.343 (I-01): v3 adds velocity, average and region; 0.725: v4, the seven-day speed. Older copies lack them and are fetched again.
            var saved=await store.ReadAsync<MarketQuote>("market",$"{savedWorld}:{itemId}","universalis-aggregated-v4",externalCancellation).ConfigureAwait(false);
            lock(gate)if(!disposed && owner==savedOwner && state.WorldId==savedWorld && saved?.Value is { } value
                && value.WorldId==savedWorld && value.ItemId==itemId && value.FetchedAt==saved.RecordedAt && value.Nq is not null && value.Hq is not null)
            {
                if(cache.Count>=CacheCapacity)cache.Remove(cache.MinBy(pair=>pair.Value.FetchedAt).Key);
                cache[(savedWorld,itemId)]=value;
            }
        }
        if (Enabled?.Invoke() == false)
        {
            lock (gate)
                if (!disposed && purpose == Purpose.Manual && itemId != 0)
                {
                    var known = cache.GetValueOrDefault((state.WorldId, itemId));
                    state = state with { ItemId = itemId, Quote = known, Loading = false, Error = known is null ? SourceOff : null };
                }
            return new(MarketFetchStatus.Failed, Error: SourceOff);
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
                // 0.329: the 3 s pause is not a failure: the hover panel shows « pending » (the tracker asks again at its next slot, no
                // error for 5 s) and a manual lookup keeps the quote already known, as a failed read does (0.281).
                if (purpose == Purpose.Hover) backgroundState = null;
                else if (purpose == Purpose.Manual) state = state with { ItemId = itemId, Quote = cache.GetValueOrDefault((world, itemId)), Loading = false, Error = "Wait a few seconds before a new request." };
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
            var quote = MarketQuote.Parse(json, world, itemId, DateTimeOffset.UtcNow).WithSpeed(await SpeedAsync(world,[itemId],token).ConfigureAwait(false));
            lock (gate)
            {
                if (disposed || ticket != generation) return Interrupted();
                if (externalCancellation.IsCancellationRequested) { requestActive = false; return new(MarketFetchStatus.Cancelled); }
                if (!cache.ContainsKey((world,itemId)) && cache.Count >= CacheCapacity) cache.Remove(cache.MinBy(pair => pair.Value.FetchedAt).Key);
                cache[(world, itemId)] = quote;
                if(disk is { } diskStore)_=diskStore.WriteAsync("market",$"{world}:{itemId}","universalis-aggregated-v4",quote,quote.FetchedAt);
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
                var message = error is OperationCanceledException ? "Request cancelled or timed out." : $"Prices could not be read: {error.Message}";
                if (purpose == Purpose.Hover) backgroundState = new(world, itemId, false, message, DateTimeOffset.UtcNow.AddSeconds(30));
                // 0.281: a failed read keeps showing the last known quote (memory or disk cache, even expired) next to the error.
                else if (purpose == Purpose.Manual) state = state with { Loading = false, Quote = cache.GetValueOrDefault((world, itemId)), Error = message };
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
