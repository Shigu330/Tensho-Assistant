using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Market;

public enum MarketScope { World, DataCenter, Region }
/// <summary>Stale = the read failed and an older board (memory or disk cache) is shown instead.</summary>
public sealed record MarketBoardState(string Scope,uint ItemId,MarketBoard? Board,bool Loading,string? Error,bool Cooldown=false,bool Stale=false);

// R-06 step 4 (0.270): listings and recent sales from Universalis for one item and one scope, asked by the Market page and the Market tab of the item sheet.
// Same guards as the price service: one request at a time, 3 s between requests, bounded response, results kept for the « Prices »
// cache lifetime. 0.281: also on disk (DataCache « market-board »), and when Universalis fails the last known board is shown,
// marked as old, instead of an error alone. The data-center → region table (/api/v2/data-centers) is read once per session.
public sealed class MarketBoardService : IDisposable
{
    private const string Family="market-board",Version="universalis-board-v1";
    private readonly object gate=new();
    private readonly HttpClient client;
    private readonly Dictionary<(string Scope,uint Item),MarketBoard> cache=[];
    private readonly CancellationTokenSource disposal=new();
    private Func<TimeSpan> lifetime=()=>TimeSpan.FromMinutes(5);
    private DataCache? disk;
    private MarketBoardState state=new("",0,null,false,null);
    private long? lastRequest;
    private bool busy,disposed;
    private IReadOnlyDictionary<string,string>? regions;
    private bool regionsRequested;
    // 0.321: after a failed read the table is asked again at most once a minute (it was asked again on every frame).
    public static readonly TimeSpan RegionRetry=TimeSpan.FromMinutes(1);
    private long regionsFailedAt;
    // 0.324: the « Universalis » switch; off = no request at all (board and data centre table), known boards still shown.
    public const string SourceOff=MarketService.SourceOff;
    public Func<bool>? Enabled { get; set; }
    /// <summary>
    /// X-73.15 (0.871, user request: the game's own data before community data): data centre name → WorldRegionGroup row, from the game sheet WorldDCGroupType.
    /// A region known here is named without asking Universalis; the others (cloud, unknown) still use its table. docs/evidence/0.871-regions.md.
    /// </summary>
    public IReadOnlyDictionary<string,uint>? GameRegions { get; set; }
    public static string? UniversalisRegion(uint region)=>region switch{1=>"Japan",2=>"North-America",3=>"Europe",4=>"Oceania",5=>"中国",6=>"한국",_=>null};

    public MarketBoardService(HttpClient? client=null)
    {
        this.client=client ?? new HttpClient{Timeout=TimeSpan.FromSeconds(15),MaxResponseContentBufferSize=4*1024*1024};
        this.client.DefaultRequestHeaders.UserAgent.ParseAdd($"TenshoAssistant/{typeof(MarketBoardService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}");
    }
    public MarketBoardState State=>Volatile.Read(ref state);
    public void ConfigureCache(Func<TimeSpan> cacheLifetime,DataCache? store=null){lock(gate){lifetime=cacheLifetime;disk=store;}}

    /// <summary>Universalis region of a data center; null until the table is read (first call starts the read) or when unknown.</summary>
    public string? RegionOf(string dataCenter)
    {
        if(GameRegions?.GetValueOrDefault(dataCenter) is { } row && UniversalisRegion(row) is { } known)return known;
        lock(gate)
        {
            if(regions is { } table)return table.GetValueOrDefault(dataCenter);
            if(regionsRequested || disposed || Enabled?.Invoke()==false)return null;
            if(regionsFailedAt!=0 && System.Diagnostics.Stopwatch.GetElapsedTime(regionsFailedAt)<RegionRetry)return null;
            regionsRequested=true;
        }
        _=Task.Run(async()=>
        {
            try
            {
                var json=await client.GetStringAsync("https://universalis.app/api/v2/data-centers",disposal.Token).ConfigureAwait(false);
                using var document=JsonDocument.Parse(json);
                var table=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                foreach(var row in document.RootElement.EnumerateArray())
                    if(row.TryGetProperty("name",out var name) && row.TryGetProperty("region",out var region) && name.GetString() is { Length:>0 } dc && region.GetString() is { Length:>0 } area)
                        table[dc]=area;
                lock(gate)regions=table;
            }
            catch(Exception){lock(gate){regionsRequested=false;regionsFailedAt=System.Diagnostics.Stopwatch.GetTimestamp();}}
        });
        return null;
    }

    private bool Fresh(MarketBoard board)=>CacheAge.Fresh(board.FetchedAt,DateTimeOffset.UtcNow,lifetime());

    /// <summary>Shows the cached board when fresh (memory, then disk), else asks Universalis (unless a request runs or the 3 s pause is
    /// not over); a failed read falls back to the last known board.</summary>
    public void Request(string scope,uint itemId,bool force=false)
    {
        if(scope.Length==0 || itemId==0)return;
        CancellationToken token;MarketBoard? known;DataCache? store;
        lock(gate)
        {
            if(disposed)return;
            known=cache.GetValueOrDefault((scope,itemId));
            if(!force && known is not null && Fresh(known)){state=new(scope,itemId,known,false,null);return;}
            if(busy)return;
            busy=true;token=disposal.Token;store=disk;
            state=new(scope,itemId,null,true,null);
        }
        _=Task.Run(async()=>
        {
            try
            {
                // Disk cache first (unless a refresh is forced): a fresh board needs no request at all.
                if(store is not null && await store.ReadAsync<MarketBoard>(Family,$"{scope}:{itemId}",Version,token).ConfigureAwait(false) is { Value:{ } saved })
                {
                    if(known is null || saved.FetchedAt>known.FetchedAt)known=saved;
                    if(!force && Fresh(saved)){Publish(scope,itemId,saved,null,false);return;}
                }
                // 0.324: Universalis turned off (Settings › Advanced): no request, the last known board is shown as it is.
                if(Enabled?.Invoke()==false){Publish(scope,itemId,known,SourceOff,known is not null);return;}
                // The 3 s pause between Universalis requests: this same task waits (showing what is known), no new request is queued.
                TimeSpan wait;
                lock(gate)
                {
                    wait=lastRequest is { } last ? TimeSpan.FromSeconds(3)-Stopwatch.GetElapsedTime(last):TimeSpan.Zero;
                    if(wait>TimeSpan.Zero && state.Scope==scope && state.ItemId==itemId)state=new(scope,itemId,known,true,null,true,known is not null);
                }
                if(wait>TimeSpan.Zero)await Task.Delay(wait,token).ConfigureAwait(false);
                lock(gate)lastRequest=Stopwatch.GetTimestamp();
                var url=$"https://universalis.app/api/v2/{Uri.EscapeDataString(scope)}/{itemId}?listings={MarketBoard.MaximumOffers}&entries={MarketBoard.MaximumSales}";
                var json=await client.GetStringAsync(url,token).ConfigureAwait(false);
                var board=MarketBoard.Parse(json,scope,itemId,DateTimeOffset.UtcNow);
                if(store is not null)_=store.WriteAsync(Family,$"{scope}:{itemId}",Version,board,board.FetchedAt);
                Publish(scope,itemId,board,null,false);
            }
            catch(OperationCanceledException) when (token.IsCancellationRequested){lock(gate)busy=false;}
            catch(Exception error)
            {
                var message=error is OperationCanceledException ? "timeout":error.Message;
                Publish(scope,itemId,known,message,known is not null);
            }
        },token);
    }

    private void Publish(string scope,uint itemId,MarketBoard? board,string? error,bool stale)
    {
        lock(gate)
        {
            busy=false;
            if(disposed)return;
            if(board is not null && !stale)
            {
                if(!cache.ContainsKey((scope,itemId)) && cache.Count>=64)cache.Remove(cache.MinBy(pair=>pair.Value.FetchedAt).Key);
                cache[(scope,itemId)]=board;
            }
            if(state.Scope==scope && state.ItemId==itemId)state=new(scope,itemId,board,false,error,false,stale);
        }
    }

    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;}
        disposal.Cancel();disposal.Dispose();client.Dispose();
    }
}
