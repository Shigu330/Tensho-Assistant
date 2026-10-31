using TenshoAssistant.Items;

namespace TenshoAssistant.Market;

/// <summary>Shared native observation drives independent usage display and the existing, opt-in market policy.</summary>
public sealed class ItemHoverCoordinator : IDisposable
{
    private readonly object gate=new();private readonly ItemHoverService context;private readonly MarketService market;
    private readonly MarketHoverTracker tracker=new();private bool disposed;
    public ItemHoverCoordinator(ItemHoverService context,MarketService market){this.context=context;this.market=market;}
    public Task<MarketFetchResult>? Observe(bool prices,bool usages,ulong owner,uint world,ulong raw,ItemCatalog? catalog,DateTimeOffset now)
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
