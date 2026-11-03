using Dalamud.Plugin.Services;
using TenshoAssistant.Market;

namespace TenshoAssistant.Items;

internal sealed class ItemHoverReader : IDisposable
{
    private readonly object gate=new();private readonly Configuration configuration;private readonly ItemHoverCoordinator coordinator;
    private readonly MarketService market;private readonly ItemCatalogService items;private readonly IGameGui gui;private readonly IPlayerState player;private readonly ItemHoverService context;
    private readonly IClientState client;private readonly IFramework framework;private readonly IPluginLog log;private DateTimeOffset retryAt;private bool disposed;
    // 0.343 (I-01): Ctrl+Alt while hovering asks the price again (once per press: the keys must be released before the next one).
    public IKeyState? Keys { get; init; }
    private bool refreshHeld;
    public ItemHoverReader(Configuration configuration,MarketService market,ItemHoverService context,ItemCatalogService items,
        IGameGui gui,IPlayerState player,IClientState client,IFramework framework,IPluginLog log)
    {
        this.configuration=configuration;this.market=market;this.items=items;this.gui=gui;this.player=player;this.client=client;this.framework=framework;this.log=log;this.context=context;
        coordinator=new(context,market);try{framework.Update+=Update;}catch{coordinator.Dispose();throw;}
    }
    /// <summary>e) (audit 2026-11-02): true while bound by duty, from the map's live copy (set by Plugin); no copy counts as « not in a duty ».</summary>
    public Func<bool> InDuty { get; init; } = ()=>false;
    private ulong Owner=>client.IsLoggedIn && player.IsLoaded ? player.ContentId:0;
    private void Update(IFramework unused)
    {
        lock(gate)
        {
            if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            var now=DateTimeOffset.UtcNow;if(now<retryAt)return;
            try
            {
                var prices=configuration.CommunityUniversalis && configuration.MarketPricesOnHover;var usages=configuration.ItemUsagesOnHover;var owner=Owner;
                // e) (audit 2026-11-02): « hide in duties » stops the observation itself: no panel, no hover price asked for it.
                var hidden=configuration.HoverHideInDuty && InDuty();
                var raw=(prices || usages) && owner!=0 && !hidden && !gui.GameUiHidden ? gui.HoveredItem:0;
                if(Owner!=owner || raw!=0 && (gui.GameUiHidden || gui.HoveredItem!=raw)){coordinator.Clear();return;}
                var held=Keys is { } keys && keys[Dalamud.Game.ClientState.Keys.VirtualKey.CONTROL] && keys[Dalamud.Game.ClientState.Keys.VirtualKey.MENU];
                var refresh=held && !refreshHeld && raw!=0 && configuration.HoverPriceRefresh;refreshHeld=held;
                _=coordinator.Observe(prices,usages,owner,market.State.WorldId,raw,items.State.Catalog,now,refresh);
                context.SetAnchor(raw==0 ? null:ReadTooltip());
                context.SetStack(raw==0 ? null:ReadStack(raw));
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
    // 0.343 (I-01): quantity of the hovered slot when the game's tooltip is for one of the player's inventory slots (AgentItemDetail:
    // DetailKind InventoryItem, TypeOrId = container, Index = slot); the slot must still hold the hovered item, else nothing.
    private unsafe uint? ReadStack(ulong raw)
    {
        var agent=FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentItemDetail.Instance();
        if(agent==null || agent->DetailKind!=FFXIVClientStructs.FFXIV.Client.Enums.DetailKind.InventoryItem)return null;
        var id=(uint)(raw>=1000000 ? raw-1000000:raw>=500000 ? raw-500000:raw);
        var inventory=FFXIVClientStructs.FFXIV.Client.Game.InventoryManager.Instance();
        if(inventory==null)return null;
        var container=inventory->GetInventoryContainer((FFXIVClientStructs.FFXIV.Client.Game.InventoryType)agent->TypeOrId);
        if(container==null || !container->IsLoaded || agent->Index>=container->Size)return null;
        var slot=container->GetInventorySlot((int)agent->Index);
        return slot!=null && slot->ItemId==id ? (uint)slot->Quantity:null;
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;framework.Update-=Update;coordinator.Dispose();}}
}
