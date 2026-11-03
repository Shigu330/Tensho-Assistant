using TenshoAssistant.Items;

namespace TenshoAssistant.Market;

/// <summary>
/// One observation of the hovered game item feeds both hover parts: item information (usages, sources) and the opt-in hover prices.
/// A change of hovered item cancels the background price request; a price is asked only when the cached quote is missing or stale.
/// </summary>
public sealed class ItemHoverCoordinator : IDisposable
{
    private readonly object gate=new();private readonly ItemHoverService context;private readonly MarketService market;
    private readonly MarketHoverTracker tracker=new();private bool disposed;
    public ItemHoverCoordinator(ItemHoverService context,MarketService market){this.context=context;this.market=market;}
    /// <param name="refresh">0.343 (I-01): the player pressed the refresh key on this item: the price is asked again even if fresh.</param>
    public Task<MarketFetchResult>? Observe(bool prices,bool usages,ulong owner,uint world,ulong raw,ItemCatalog? catalog,DateTimeOffset now,bool refresh=false)
    {
        lock(gate)
        {
            if(disposed)return null;
            context.Observe(prices || usages,owner,raw,catalog,now);
            if(tracker.Observe(prices,owner,world,context.State.Item,catalog,now))market.CancelBackground();
            if(!prices || tracker.Current is not { } item)return null;
            var pending=market.BackgroundState;
            if(pending is {RetryAt:{ } retry} && pending.ItemId==item.ItemId && pending.WorldId==world && retry>now)return null;
            if(!tracker.TakeRequest(now))return null;
            if(refresh)return market.RequestHoverAsync(item.ItemId,owner,world,true);
            var cached=market.GetCached(world,item.ItemId);
            return cached is null || !market.IsFresh(cached,now) ? market.RequestHoverAsync(item.ItemId,owner,world):null;
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
