using System.Net.Http;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Community;

public enum GarlandResource { Instance, Item, Monster, ItemEnglish }
public sealed record GarlandResponse(string Json,DateTimeOffset FetchedAt);

/// <summary>Shared bounded transport and cadence; parsed domain caches remain with their services.</summary>
public sealed class GarlandClient : IDisposable
{
    public const int MaximumBytes = 1024 * 1024;
    public static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(3);
    private readonly object gate = new();
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly TimeProvider clock;
    private readonly CancellationTokenSource lifetime = new();
    private long? lastRequest;
    private bool disposed;
    private Func<TimeSpan> cacheLifetime=()=>TimeSpan.FromMinutes(30);
    private DataCache? disk;
    public TimeSpan CacheLifetime=>Volatile.Read(ref cacheLifetime)();
    public void ConfigureCache(Func<TimeSpan> lifetime,DataCache store){lock(gate){cacheLifetime=lifetime;disk=store;}}
    public async Task<GarlandResponse> ReadDocumentAsync(GarlandResource resource,ulong id,CancellationToken token,bool force=false)
    {
        var key=$"{resource}:{id}";
        if(!force && disk is { } store)
        {
            var saved=await store.ReadAsync<string>("sources",key,"garland-routes-fr3-en2-v1",token).ConfigureAwait(false);
            if(saved is not null && CacheAge.Fresh(saved.RecordedAt,clock.GetUtcNow(),CacheLifetime))return new(saved.Value,saved.RecordedAt);
        }
        var json=await ReadAsync(resource,id,token).ConfigureAwait(false);
        return new(json,clock.GetUtcNow());
    }
    public void Remember(GarlandResource resource,ulong id,GarlandResponse response)
    {
        if(disk is { } store)_=store.WriteAsync("sources",$"{resource}:{id}","garland-routes-fr3-en2-v1",response.Json,response.FetchedAt);
    }
    public GarlandClient(HttpClient? client = null, TimeProvider? clock = null)
    { ownsClient = client is null; this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) }; this.clock = clock ?? TimeProvider.System; }

    public async Task<string> ReadAsync(GarlandResource resource, ulong id, CancellationToken cancellation)
    {
        if (id == 0 || id > long.MaxValue || (resource != GarlandResource.Monster && id > uint.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(id));
        var route = resource switch
        {
            GarlandResource.Instance => "instance/en/2",
            GarlandResource.Item => "item/fr/3",
            GarlandResource.Monster => "mob/fr/2",
            GarlandResource.ItemEnglish => "item/en/3",
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };
        CancellationTokenSource linked;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            cancellation.ThrowIfCancellationRequested();
            if (lastRequest is { } last && clock.GetElapsedTime(last) < RequestInterval)
                throw new InvalidOperationException("Patientez trois secondes entre deux consultations Garland Tools.");
            lastRequest = clock.GetTimestamp();
            linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation,lifetime.Token);
            linked.CancelAfter(TimeSpan.FromSeconds(12));
        }
        using(linked)
            return await BoundedHttp.ReadAsync(client,new Uri($"https://www.garlandtools.org/db/doc/{route}/{id}.json"),MaximumBytes,linked.Token).ConfigureAwait(false);
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true; lifetime.Cancel(); lifetime.Dispose();
            if (ownsClient) client.Dispose();
        }
    }
}
