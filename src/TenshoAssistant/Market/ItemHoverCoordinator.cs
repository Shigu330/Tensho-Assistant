using TenshoAssistant.Items;

namespace TenshoAssistant.Market;

/// <summary>
/// One observation of the hovered game item feeds both hover parts: item information (usages, sources) and the opt-in hover prices.
/// A change of hovered item cancels the background price request; a price is asked only when the cached quote is missing or stale.
/// </summary>
public sealed class ItemHoverCoordinator : IDisposable
{
    private readonly object gate=new();private readonly ItemHoverService context;private readonly MarketService market;
    private readonly MarketHoverTracker tracker=new();private bool disposed;private bool refreshWanted;
    public ItemHoverCoordinator(ItemHoverService context,MarketService market){this.context=context;this.market=market;}
    /// <param name="refresh">0.343 (I-01): the player pressed the refresh key on this item: the price is asked again even if fresh.</param>
    public Task<MarketFetchResult>? Observe(bool prices,bool usages,ulong owner,uint world,ulong raw,ItemCatalog? catalog,DateTimeOffset now,bool refresh=false)
    {
        lock(gate)
        {
            if(disposed)return null;
            context.Observe(prices || usages,owner,raw,catalog,now);
            if(tracker.Observe(prices,owner,world,context.State.Item,catalog,now)){market.CancelBackground();refreshWanted=false;}
            if(!prices || tracker.Current is not { } item){refreshWanted=false;return null;}
            // 0.524 (user report: Ctrl+Alt did nothing): the press lasts one tick, and the 5 s bound was taken on every tick, asked or not, so they almost never met.
            // The press now waits for the bound, and only a real request takes it.
            refreshWanted|=refresh;
            var pending=market.BackgroundState;
            if(pending is {RetryAt:{ } retry} && pending.ItemId==item.ItemId && pending.WorldId==world && retry>now)return null;
            var cached=market.GetCached(world,item.ItemId);
            if(!refreshWanted && cached is not null && market.IsFresh(cached,now))return null;
            if(!tracker.TakeRequest(now))return null;
            var force=refreshWanted;refreshWanted=false;
            var request=market.RequestHoverAsync(item.ItemId,owner,world,force);
            // The market's own 3 s pause, or a request under way, must not lose the press: it waits for the next slot, on this item only.
            if(force)_=request.ContinueWith(done=>
            {
                if(done.Status!=TaskStatus.RanToCompletion || done.Result.Status is not (MarketFetchStatus.Cooldown or MarketFetchStatus.Busy))return;
                lock(gate)if(!disposed && tracker.Current?.ItemId==item.ItemId)refreshWanted=true;
            },TaskScheduler.Default);
            return request;
        }
    }
    public void Clear()
    {
        lock(gate)
        {
            if(disposed)return;
            context.Clear();tracker.Observe(false,0,0,(ItemHoverItem?)null,null,DateTimeOffset.UtcNow);market.CancelBackground();
        }
    }
    public void Dispose(){lock(gate){if(disposed)return;Clear();disposed=true;context.Dispose();}}
}
