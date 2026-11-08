using Dalamud.Plugin.Services;
using TenshoAssistant.Market;

namespace TenshoAssistant.Items;

internal sealed class ItemHoverReader : IDisposable
{
    private readonly object gate=new();private readonly Configuration configuration;private readonly ItemHoverCoordinator coordinator;
    private readonly MarketService market;private readonly ItemCatalogService items;private readonly IGameGui gui;private readonly IPlayerState player;private readonly ItemHoverService context;
    private readonly IClientState client;private readonly IFramework framework;private readonly IPluginLog log;private DateTimeOffset retryAt;private bool disposed;
    private bool failureLogged;
    // 0.343 (I-01): Ctrl+Alt while hovering asks the price again (once per press: the keys must be released before the next one).
    public IKeyState? Keys { get; init; }
    private bool refreshHeld;
    public ItemHoverReader(Configuration configuration,MarketService market,ItemHoverService context,ItemCatalogService items,
        IGameGui gui,IPlayerState player,IClientState client,IFramework framework,IPluginLog log)
    {
        this.configuration=configuration;this.market=market;this.items=items;this.gui=gui;this.player=player;this.client=client;this.framework=framework;this.log=log;this.context=context;
        coordinator=new(context,market);try{TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);}catch{coordinator.Dispose();throw;}
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
                // 0.852 (X-47, user report: no Tensho panel under a quest reward's tooltip): developer mode, why a hovered item gets no panel, once per item and reason.
                if(Developer() && raw==0 && gui.HoveredItem is var seen and not 0)
                {
                    var why=!prices && !usages ? "prices and uses off":owner==0 ? "not logged in":hidden ? "hidden in duties":gui.GameUiHidden ? "game UI hidden":"other";
                    if(blockedLogged!=(seen,why)){blockedLogged=(seen,why);log.Information("Item hover without panel: item {0}, {1}.",seen,why);}
                }
                if(Owner!=owner || raw!=0 && (gui.GameUiHidden || gui.HoveredItem!=raw)){coordinator.Clear();return;}
                var held=Keys is { } keys && keys[Dalamud.Game.ClientState.Keys.VirtualKey.CONTROL] && keys[Dalamud.Game.ClientState.Keys.VirtualKey.MENU];
                var refresh=held && !refreshHeld && raw!=0 && configuration.HoverPriceRefresh;refreshHeld=held;
                _=coordinator.Observe(prices,usages,owner,market.State.WorldId,raw,items.State.Catalog,now,refresh);
                context.SetAnchor(raw==0 ? null:ReadTooltip());
                // R-14 (0.507): the stack is an extra: its failure leaves it unknown, logged once, and no longer costs the price and the uses.
                if(raw==0 || stackFailed)context.SetStack(null);
                else
                    try{context.SetStack(ReadStack(raw));}
                    catch(Exception error){stackFailed=true;context.SetStack(null);log.Warning(error,"Stack of the hovered item unavailable.");}
                failureLogged=false;
            }
            // X-76 H9 (0.922): logged once until a read succeeds again (it was logged every 5 s).
            catch(Exception error){coordinator.Clear();retryAt=now.AddSeconds(5);if(!failureLogged){failureLogged=true;log.Warning(error,"Item hover context unavailable; retried every five seconds, logged again only after a good read.");}}
        }
    }
    private bool stackFailed;
    private (ulong Item,string Why) blockedLogged;
    /// <summary>0.852 (X-47): developer mode, set by the plugin; false when not given.</summary>
    public Func<bool> Developer { get; init; } = ()=>false;
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
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);coordinator.Dispose();}}
}
