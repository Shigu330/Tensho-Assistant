using System.Net.Http;
using TenshoAssistant.Community;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Fashion;

/// <summary>
/// 0.331: why the last request brought nothing new while the data already shown stay valid (15 s between two refreshes, cancelled),
/// so the page can say so instead of « data unavailable »; Error keeps its text for the developer mode.
/// </summary>
public enum FashionNotice { None, Throttled, Cancelled }
public sealed record FashionDataState(FashionDataset? Dataset,bool Loading,string? Error,FashionNotice Notice=FashionNotice.None);
public sealed class FashionDataService : IDisposable
{
    public const string DataBase="https://raw.githubusercontent.com/Infiziert90/FFXIVGachaSpreadsheet/refs/heads/master/website/static/data/";
    public static readonly TimeSpan FreshFor=TimeSpan.FromMinutes(30);
    private readonly object gate=new();
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly TimeProvider clock;
    private CancellationTokenSource? request;
    private FashionDataState state=new(null,false,null);
    private int generation;
    private long? lastRequest;
    private bool disposed;
    public FashionDataState State=>Volatile.Read(ref state);
    // 0.321 (review 0.316, point 2): the source's switch (Settings › Advanced). Off: no download; a fresh copy on disk is still read.
    public Func<bool>? Enabled { get; set; }
    public bool Disabled=>Enabled?.Invoke()==false;
    private Func<TimeSpan> cacheLifetime=()=>FreshFor;
    private DataCache? disk;
    public TimeSpan CacheLifetime=>Volatile.Read(ref cacheLifetime)();
    public void ConfigureCache(Func<TimeSpan> lifetime,DataCache store){lock(gate){cacheLifetime=lifetime;disk=store;}}
    public FashionDataService(HttpClient? client=null,TimeProvider? clock=null)
    { ownsClient=client is null; this.client=client ?? new HttpClient {Timeout=TimeSpan.FromSeconds(15)}; this.clock=clock ?? TimeProvider.System; }
    public void Cancel()
    {
        lock(gate)
        {
            if(disposed || !state.Loading) return;
            generation++; request?.Cancel(); state=state with {Loading=false,Error="Lookup cancelled.",Notice=FashionNotice.Cancelled};
        }
    }
    public async Task RequestAsync(bool force=false,CancellationToken cancellation=default)
    {
        CancellationTokenSource source; int ticket;
        lock(gate)
        {
            if(disposed || state.Loading || cancellation.IsCancellationRequested) return;
            if(!force && state.Dataset is { } saved && CacheAge.Fresh(saved.FetchedAt,clock.GetUtcNow(),CacheLifetime))
            { state=state with {Error=null,Notice=FashionNotice.None}; return; }
            source=CancellationTokenSource.CreateLinkedTokenSource(cancellation); source.CancelAfter(TimeSpan.FromSeconds(12));
            request=source; ticket=++generation; state=state with {Loading=true,Error=null,Notice=FashionNotice.None};
        }
        try
        {
            if(!force && disk is { } store)
            {
                var saved=await store.ReadAsync<FashionCacheData>("fashion","associations","gacha-v1",source.Token).ConfigureAwait(false);
                if(saved is not null && CacheAge.Fresh(saved.RecordedAt,clock.GetUtcNow(),CacheLifetime))
                {
                    var restored=FashionDataset.Parse(saved.Value.Json,saved.Value.Metadata,saved.RecordedAt,source.Token);
                    lock(gate)if(!disposed && ticket==generation && !source.IsCancellationRequested)state=new(restored,false,null);
                    return;
                }
            }
            lock(gate)
            {
                if(disposed || ticket!=generation || source.IsCancellationRequested)return;
                // Source turned off: no network, the state keeps what it had (the page says the source is off).
                if(Disabled){state=state with {Loading=false,Error=null};return;}
                if(lastRequest is { } last && clock.GetElapsedTime(last)<TimeSpan.FromSeconds(15))
                {state=state with {Loading=false,Error="Wait fifteen seconds between two refreshes of the directory.",Notice=FashionNotice.Throttled};return;}
                lastRequest=clock.GetTimestamp();
            }
            // Fixed public URLs only; the operation sends no player state and performs no upload.
            var metadataTask=ReadDateAsync(source.Token);
            var dataTask=BoundedHttp.ReadAsync(client,new Uri(DataBase+"FashionReport.json"),FashionDataset.MaximumBytes,source.Token);
            await Task.WhenAll((Task)dataTask,metadataTask).ConfigureAwait(false);
            var json=await dataTask.ConfigureAwait(false);
            var metadata=await metadataTask.ConfigureAwait(false);
            var parsed=FashionDataset.Parse(json,metadata,clock.GetUtcNow(),source.Token);
            lock(gate)
            {
                if(disposed || ticket!=generation || source.IsCancellationRequested) return;
                state=new(parsed,false,null);
                if(disk is { } diskStore)_=diskStore.WriteAsync("fashion","associations","gacha-v1",new FashionCacheData(json,metadata),parsed.FetchedAt);
            }
        }
        catch(Exception error)
        {
            lock(gate)
            {
                if(disposed || ticket!=generation) return;
                state=state with {Loading=false,Error=error is OperationCanceledException ? "Lookup cancelled or timed out.":$"Associations could not be read: {error.Message}"};
            }
        }
        finally
        {
            lock(gate)
            {
                if(ReferenceEquals(request,source))
                {
                    request=null;
                    if(!disposed && ticket==generation && state.Loading) state=state with {Loading=false,Error="Lookup cancelled or timed out."};
                }
                source.Dispose();
            }
        }
    }
    private async Task<string?> ReadDateAsync(CancellationToken token)
    {
        try { return await BoundedHttp.ReadAsync(client,new Uri(DataBase+"LastUpdate.json"),4096,token).ConfigureAwait(false); }
        catch(Exception) { return null; }
    }
    public void Dispose()
    {
        lock(gate)
        {
            if(disposed) return;
            disposed=true; generation++; request?.Cancel(); state=new(null,false,null); if(ownsClient) client.Dispose();
        }
    }
}
