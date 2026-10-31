using Dalamud.Plugin.Services;
using TenshoAssistant.Market;

namespace TenshoAssistant.Items;

internal sealed class ItemHoverReader : IDisposable
{
    private readonly object gate=new();private readonly Configuration configuration;private readonly ItemHoverCoordinator coordinator;
    private readonly MarketService market;private readonly ItemCatalogService items;private readonly IGameGui gui;private readonly IPlayerState player;private readonly ItemHoverService context;
    private readonly IClientState client;private readonly IFramework framework;private readonly IPluginLog log;private DateTimeOffset retryAt;private bool disposed;
    public ItemHoverReader(Configuration configuration,MarketService market,ItemHoverService context,ItemCatalogService items,
        IGameGui gui,IPlayerState player,IClientState client,IFramework framework,IPluginLog log)
    {
        this.configuration=configuration;this.market=market;this.items=items;this.gui=gui;this.player=player;this.client=client;this.framework=framework;this.log=log;this.context=context;
        coordinator=new(context,market);try{framework.Update+=Update;}catch{coordinator.Dispose();throw;}
    }
    private ulong Owner=>client.IsLoggedIn && player.IsLoaded ? player.ContentId:0;
    private void Update(IFramework unused)
    {
        lock(gate)
        {
            if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            var now=DateTimeOffset.UtcNow;if(now<retryAt)return;
            try
            {
                var prices=configuration.MarketPricesOnHover;var usages=configuration.ItemUsagesOnHover;var owner=Owner;
                var raw=(prices || usages) && owner!=0 && !gui.GameUiHidden ? gui.HoveredItem:0;
                if(Owner!=owner || raw!=0 && (gui.GameUiHidden || gui.HoveredItem!=raw)){coordinator.Clear();return;}
                _=coordinator.Observe(prices,usages,owner,market.State.WorldId,raw,items.State.Catalog,now);
                context.SetAnchor(raw==0 ? null:ReadTooltip());
            }
            catch(Exception error){coordinator.Clear();retryAt=now.AddSeconds(5);log.Warning(error,"Item hover context unavailable; retry in five seconds.");}
        }
    }
    // Only the visible game tooltip's screen rectangle is copied; no pointer outlives this call.
    private ItemHoverAnchor? ReadTooltip()
    {
        var addon=gui.GetAddonByName("ItemDetail");
        if(addon.IsNull || !addon.IsReady || !addon.IsVisible)return null;
        var position=addon.Position;var size=addon.ScaledSize;
        return new ItemHoverAnchor(position.X,position.Y,size.X,size.Y);
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;framework.Update-=Update;coordinator.Dispose();}}
}
