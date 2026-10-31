using TenshoAssistant.Community;

namespace TenshoAssistant.Monsters;

public sealed record MonsterLootState(uint ItemId, MonsterItemDocument? Item, ulong MonsterId, MonsterDocument? Monster,
    bool Loading, string? Error);

public sealed class MonsterLootService : IDisposable
{
    public static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(30);
    public const int ItemCacheCapacity = 16;
    public const int MonsterCacheCapacity = 32;
    private readonly object gate = new();
    private readonly GarlandClient transport;
    private readonly TimeProvider clock;
    private readonly Dictionary<uint,MonsterItemDocument> items = [];
    private readonly Dictionary<ulong,MonsterDocument> monsters = [];
    private CancellationTokenSource? request;
    private MonsterLootState state = new(0,null,0,null,false,null);
    private int generation;
    private bool disposed;
    public MonsterLootState State => Volatile.Read(ref state);
    public TimeSpan CacheLifetime=>transport.CacheLifetime;
    // Transport is borrowed; the plugin disposes it after both public-data services.
    public MonsterLootService(GarlandClient transport,TimeProvider? clock = null)
    { this.transport = transport; this.clock = clock ?? TimeProvider.System; }
    public void SelectItem(uint id)
    {
        lock (gate)
        {
            if (disposed || state.ItemId == id) return;
            generation++; request?.Cancel();
            state = new(id,items.GetValueOrDefault(id),0,null,false,null);
        }
    }
    public void SelectMonster(ulong id)
    {
        lock (gate)
        {
            if (disposed || state.MonsterId == id) return;
            if (id == 0 || state.Item is null || !state.Item.Monsters.Any(monster => monster.Id == id))
                throw new ArgumentException("Monster is not referenced by the current item.",nameof(id));
            generation++; request?.Cancel();
            state = state with { MonsterId=id, Monster=monsters.GetValueOrDefault(id), Loading=false, Error=null };
        }
    }
    public void Cancel()
    {
        lock (gate)
        {
            if (disposed || !state.Loading) return;
            generation++; request?.Cancel(); state = state with { Loading=false, Error="Consultation annulée." };
        }
    }
    public Task RequestItemAsync(bool force = false,CancellationToken cancellation = default) => RequestAsync(false,force,cancellation);
    public Task RequestMonsterAsync(bool force = false,CancellationToken cancellation = default) => RequestAsync(true,force,cancellation);
    private async Task RequestAsync(bool detail,bool force,CancellationToken cancellation)
    {
        uint itemId; ulong monsterId; int ticket; CancellationTokenSource source;
        lock (gate)
        {
            if (disposed || state.Loading || state.ItemId == 0 || (detail && state.MonsterId == 0) || cancellation.IsCancellationRequested) return;
            itemId = state.ItemId; monsterId = state.MonsterId;
            var date = detail ? monsters.GetValueOrDefault(monsterId)?.FetchedAt : items.GetValueOrDefault(itemId)?.FetchedAt;
            if (!force && date is { } fetched && Caching.CacheAge.Fresh(fetched,clock.GetUtcNow(),CacheLifetime))
            {
                state = detail ? state with { Monster=monsters[monsterId], Error=null } : state with { Item=items[itemId], Error=null };
                return;
            }
            source = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            source.CancelAfter(TimeSpan.FromSeconds(12)); request=source; ticket=++generation;
            state = state with { Loading=true, Error=null };
        }
        try
        {
            var resource=detail ? GarlandResource.Monster:GarlandResource.Item;var identity=detail ? monsterId:itemId;
            var response = await transport.ReadDocumentAsync(resource,identity,source.Token,force).ConfigureAwait(false);
            var item = detail ? null : MonsterDocuments.ParseItem(response.Json,itemId,response.FetchedAt,source.Token);
            var monster = detail ? MonsterDocuments.ParseMonster(response.Json,monsterId,response.FetchedAt,source.Token) : null;
            lock (gate)
            {
                if (disposed || ticket != generation || source.IsCancellationRequested) return;
                transport.Remember(resource,identity,response);
                if (detail)
                {
                    if (!monsters.ContainsKey(monsterId) && monsters.Count >= MonsterCacheCapacity) monsters.Remove(monsters.MinBy(pair => pair.Value.FetchedAt).Key);
                    monsters[monsterId] = monster!;
                    state = state with { Monster=monster, Loading=false, Error=null };
                }
                else
                {
                    if (!items.ContainsKey(itemId) && items.Count >= ItemCacheCapacity) items.Remove(items.MinBy(pair => pair.Value.FetchedAt).Key);
                    items[itemId] = item!;
                    var retain = state.MonsterId != 0 && item!.Monsters.Any(reference => reference.Id == state.MonsterId);
                    state = state with { Item=item, MonsterId=retain ? state.MonsterId : 0, Monster=retain ? state.Monster : null, Loading=false, Error=null };
                }
            }
        }
        catch (Exception error)
        {
            lock (gate)
            {
                if (disposed || ticket != generation) return;
                state = state with { Loading=false, Error=error is OperationCanceledException
                    ? "Consultation annulée ou délai dépassé." : $"Lecture des sources impossible : {error.Message}" };
            }
        }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(request,source))
                {
                    request=null;
                    if (!disposed && ticket == generation && state.Loading)
                        state = state with { Loading=false, Error="Consultation annulée ou délai dépassé." };
                }
                source.Dispose();
            }
        }
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed=true; generation++; request?.Cancel(); items.Clear(); monsters.Clear();
            state = new(0,null,0,null,false,null);
        }
    }
}
