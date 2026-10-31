using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using TenshoAssistant.Localization;

namespace TenshoAssistant.Items;

internal sealed class ItemContextMenuBridge : IDisposable
{
    private readonly Configuration configuration;
    private readonly IContextMenu menus;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly ItemCatalogService catalog;
    private readonly IPluginLog log;
    private readonly LocalizationManager localization;
    private readonly IGameGui gui;
    private readonly ItemContextMenuSession session=new();
    private bool disposed;
    private ulong Owner=>client.IsLoggedIn && player.IsLoaded ? player.ContentId:0;

    public ItemContextMenuBridge(Configuration configuration,IContextMenu menus,IPlayerState player,IClientState client,ItemCatalogService catalog,IPluginLog log,LocalizationManager localization,IGameGui gui)
    {
        this.configuration=configuration;this.menus=menus;this.player=player;this.client=client;this.catalog=catalog;this.log=log;
        this.localization=localization;this.gui=gui;
        menus.OnMenuOpened+=OnMenuOpened;
        try{client.Logout+=OnLogout;}
        catch{menus.OnMenuOpened-=OnMenuOpened;throw;}
    }

    private void OnLogout(int type,int code)=>session.Clear();
    private bool Current(ItemMenuToken token)=>!disposed && session.IsCurrent(token,configuration.ItemContextMenuEnabled,Owner,catalog.State.Catalog,DateTimeOffset.UtcNow);

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        try
        {
            session.Clear();
            if(disposed || !configuration.ItemContextMenuEnabled || Owner==0)return;
            var owner=Owner;
            ulong raw;
            // TargetItem is read here only. Closures retain just the normalized managed token, never args/target/address.
            if(args.MenuType==ContextMenuType.Inventory)
            {
                if(args.Target is not MenuTargetInventory inventory || inventory.TargetItem is not { } item || item.IsEmpty)return;
                raw=item.ItemId;
            }
            else
            {
                // Shops, exchanges and item lists open a default menu without an item target: the item hovered when the
                // menu opened is used. Menus aimed at a character (player, NPC, party member) never get the Tensho entry.
                if(args.MenuType!=ContextMenuType.Default || args.Target is not MenuTargetDefault target
                   || target.TargetContentId!=0 || target.TargetObject is not null || !string.IsNullOrEmpty(target.TargetName))return;
                raw=gui.HoveredItem;
                if(raw==0)return;
            }
            if(owner!=Owner)return;
            var token=session.Begin(true,owner,raw,catalog.State.Catalog,DateTimeOffset.UtcNow);
            if(token is null)return;
            args.AddMenuItem(new MenuItem
            {
                Name=new SeStringBuilder().AddText(localization.Get("ItemMenu.Title")).Build(),PrefixChar='T',IsSubmenu=true,
                OnClicked=click=>
                {
                    if(!Current(token))return;
                    click.OpenSubmenu(new IMenuItem[]
                    {
                        Entry(localization.Get("ItemUsesUi.Title"),ItemContextRoute.Usages,token),
                        Entry(localization.Get("ItemsUi.Stocks"),ItemContextRoute.Stocks,token),
                        Entry(localization.Get("ItemsUi.Craft"),ItemContextRoute.Craft,token),
                        Entry(localization.Get("GatheringDetails.Title"),ItemContextRoute.Gathering,token),
                        Entry(localization.Get("NpcUi.Title"),ItemContextRoute.NpcVendors,token),
                        Entry(localization.Get("ItemMenu.Tools"),ItemContextRoute.Tools,token),
                    });
                },
            });
        }
        catch(Exception error){session.Clear();log.Warning(error,"Could not add Tensho inventory context menu.");}
    }

    private MenuItem Entry(string name,ItemContextRoute route,ItemMenuToken token)=>new()
    {
        Name=new SeStringBuilder().AddText(name).Build(),PrefixChar='T',
        OnClicked=_=>
        {
            if(!disposed)session.Queue(token,route,configuration.ItemContextMenuEnabled,Owner,catalog.State.Catalog,DateTimeOffset.UtcNow);
        },
    };

    public ItemMenuRequest? Take()=>session.Take(configuration.ItemContextMenuEnabled,Owner,catalog.State.Catalog,DateTimeOffset.UtcNow);
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        menus.OnMenuOpened-=OnMenuOpened;client.Logout-=OnLogout;session.Dispose();
    }
}
