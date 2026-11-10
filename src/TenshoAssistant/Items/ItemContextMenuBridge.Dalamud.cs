using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using TenshoAssistant.Localization;

namespace TenshoAssistant.Items;

internal sealed class ItemContextMenuBridge : IDisposable
{
    private readonly RegistrationScope registrations=new();
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
        // X-76 H10 (0.924): subscriptions owned by one RegistrationScope: a failure part-way undoes the ones already made; Dispose releases them in reverse order.
        try
        {
            registrations.Register(()=>menus.OnMenuOpened+=OnMenuOpened,()=>menus.OnMenuOpened-=OnMenuOpened);
            registrations.Register(()=>client.Logout+=OnLogout,()=>client.Logout-=OnLogout);
        }
        catch{registrations.Dispose();throw;}
    }

    private void OnLogout(int type,int code)=>session.Clear();
    /// <summary>0.808 (X-44): windows that list items only (crafting log, its material list and tree): their menu is about the hovered item.</summary>
    // 0.852 (X-47, dalamud.log 2026-11-07 22:11:48: « addon JournalAccept, hovered item 5707, name set », left out as a character's menu): the quest windows' rewards.
    internal static readonly IReadOnlySet<string> ItemWindows=new HashSet<string>(StringComparer.Ordinal){"RecipeNote","RecipeMaterialList","RecipeTree","RecipeProductList",
        "JournalAccept","JournalResult","JournalDetail"};
    /// <summary>0.837 (X-03): the chat's link item (AgentChatLog.ContextItemId, framework thread) is the hovered item, HQ or not.</summary>
    private static unsafe bool ChatLinkItem(ulong hovered)
    {
        var agent=FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentChatLog.Instance();
        return agent!=null && ChatLinkMatches(agent->ContextItemId,hovered);
    }
    internal static bool ChatLinkMatches(uint linked,ulong hovered)=>linked!=0 && hovered!=0 && linked%1_000_000==hovered%1_000_000;
    private bool Current(ItemMenuToken token)=>!disposed && session.IsCurrent(token,configuration.ItemContextMenuEnabled,Owner,catalog.State.Catalog,DateTimeOffset.UtcNow);

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        try
        {
            session.Clear();
            // 0.781 (X-03, user report: a right click on the ring's item scrolled the chat and opened nothing): developer mode, what the game opened.
            if(configuration.DeveloperMode)
                log.Information("Context menu opened: type {0}, addon \"{1}\", target {2}, hovered item {3}{4}.",args.MenuType,args.AddonName ?? "",args.Target?.GetType().Name ?? "none",gui.HoveredItem,
                    args.Target is MenuTargetDefault aimed ? $"; content {(aimed.TargetContentId!=0 ? "set":"0")}, object {(aimed.TargetObject is null ? "none":"set")}, name {(string.IsNullOrEmpty(aimed.TargetName) ? "empty":"set")}":"");
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
                // 0.808 (X-44, user report: no Tensho entry in the crafting log): the game's item windows aim no character, whatever their target holds.
                var itemWindow=ItemWindows.Contains(args.AddonName ?? "");
                // 0.837 (X-03, « /tensho linktest », dalamud.log 23:35: the chat's menu opened on the item link, hovered item 12898, « name set »): an item link
                // in the chat is the game's link item, the same as the hovered one; a player's name in the chat still gets no entry.
                var chatItem=args.AddonName=="ChatLog" && ChatLinkItem(gui.HoveredItem);
                if(args.MenuType!=ContextMenuType.Default || args.Target is not MenuTargetDefault target
                   || !itemWindow && !chatItem && (target.TargetContentId!=0 || target.TargetObject is not null || !string.IsNullOrEmpty(target.TargetName)))return;
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
                    // 0.208: the same tools as the context item panel, only those useful for this item (Relevant), in the same order.
                    var routes=Relevant?.Invoke(token.Item.ItemId) ?? DefaultRoutes;
                    // 0.256: then the item's sources, grouped as in Tensho (one sub-menu per kind).
                    var entries=routes.Select(route=>(IMenuItem)Entry(Title?.Invoke(route,token.Item.ItemId) ?? RouteName(route),route,token)).ToList();
                    foreach(var (label,lines) in Sources?.Invoke(token.Item.ItemId) ?? [])
                        entries.Add(lines.Count==1 && lines[0].Text==label ? Action(label,lines[0].Open):new MenuItem
                        {
                            Name=new SeStringBuilder().AddText(label).Build(),PrefixChar='T',IsSubmenu=true,
                            OnClicked=group=>group.OpenSubmenu(lines.SelectMany(line=>line.Flag is { } flag
                                ? new IMenuItem[]{Action(line.Text.Trim(),line.Open),Action("  → "+localization.Get("SourceLinks.Flag"),flag)}
                                :[Action(line.Text.Trim(),line.Open)]).ToArray()),
                        });
                    // 0.349 (g, X-07): the item on the wiki and the Lodestone, opened in the browser on this click.
                    foreach(var (label,open) in Links?.Invoke(token.Item.ItemId) ?? [])entries.Add(Action(label,open));
                    click.OpenSubmenu(entries.ToArray());
                },
            });
        }
        catch(Exception error){session.Clear();log.Warning(error,"Could not add Tensho inventory context menu.");}
    }

    // 0.349 (g, X-07): external pages of an item (label, open), computed by the main window; null = none.
    public Func<uint,IReadOnlyList<(string Label,Action Open)>>? Links { get; set; }
    // Label of a tool for this item (the main window knows, e.g., that a card's collection is the Triple Triad tool).
    public Func<ItemContextRoute,uint,string>? Title { get; set; }
    // Sources of an item, grouped like Tensho's right-click menu (computed by the main window); null = none shown.
    public Func<uint,IReadOnlyList<(string Label,IReadOnlyList<MainWindow.ItemSourceLine> Lines)>>? Sources { get; set; }
    private MenuItem Action(string name,Action open)=>new()
    {
        Name=new SeStringBuilder().AddText(name).Build(),PrefixChar='T',
        OnClicked=_=>{if(!disposed)open();},
    };

    // Tools useful for an item (computed by the main window from the loaded catalogues); null = every tool.
    public Func<uint,IReadOnlyList<ItemContextRoute>>? Relevant { get; set; }
    private static readonly IReadOnlyList<ItemContextRoute> DefaultRoutes=[ItemContextRoute.Usages,ItemContextRoute.Stocks,ItemContextRoute.Craft,
        ItemContextRoute.Gathering,ItemContextRoute.NpcVendors,ItemContextRoute.Market,ItemContextRoute.Sources];
    private string RouteName(ItemContextRoute route)=>route switch
    {
        ItemContextRoute.Usages=>localization.Get("ItemUsesUi.Title"),
        ItemContextRoute.Stocks=>localization.Get("ItemsUi.Stocks"),
        ItemContextRoute.Craft=>localization.Get("ItemsUi.Craft"),
        ItemContextRoute.Gathering=>localization.Get("GatheringDetails.Title"),
        ItemContextRoute.NpcVendors=>localization.Get("NpcUi.Title"),
        ItemContextRoute.Market=>localization.Get("ItemLinks.Market"),
        ItemContextRoute.DutyLoot=>localization.Get("ItemMenu.DutyLoot"),
        ItemContextRoute.MonsterLoot=>localization.Get("ItemMenu.MonsterLoot"),
        ItemContextRoute.Collection=>localization.Get("HoverUi.OpenCollection"),
        ItemContextRoute.Quest=>localization.Get("ItemMenu.Quest"),
        ItemContextRoute.Sources=>localization.Get("ItemMenu.Sources"),
        _=>localization.Get("ItemMenu.Tools"),
    };

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
        registrations.Dispose();session.Dispose();
    }
}
