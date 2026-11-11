using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;
using TenshoAssistant.Market;

namespace TenshoAssistant;

// Item hover panel, drawn every frame under the game's item tooltip (else above or beside it; the mouse is only a fallback), with
// two opt-in parts: item information and market prices (Universalis: current world, then the cheapest listing of the data center).
// The information body (DrawItemInfoBody) is the common Tensho item tooltip: collection state, stock in known storages, usages and
// how to obtain it: game data and LuminaSupplemental first (vendors, quests, achievements, duties, monsters, FATEs, ventures,
// crafts, gathering), then the consolegameswiki « Acquisition » section. Garland Tools is asked for every item put in context
// (duties, monsters, exchanges); « How to get it » shows it only when nothing else is known.
internal sealed partial class MainWindow
{
    private Vector2 hoverPanelSize=new(380,300);
    private (uint Item,object? Live,object? Bank,ulong Character) hoverHoldingsKey;
    private ItemHoldingsSummary? hoverHoldings;
    // 0.343 (I-01): world names outside the data center (region cheapest), from the World sheet (set by Plugin).
    internal Func<uint,string?>? WorldName { get; set; }
    public void DrawItemHover()
    {
        var observed=itemHover.State;var now=DateTimeOffset.UtcNow;
        if(windowDisposed || !observed.Fresh(now) || observed.Item is not { } hovered)return;
        var priced=MarketHoverTracker.Eligible(hovered,itemCatalog.State.Catalog);
        var prices=configuration.CommunityUniversalis && configuration.MarketPricesOnHover && market.State.WorldId!=0 && priced is not null;
        // e) (audit 2026-11-02): each part of the item information can be turned off; with nothing left to draw for this item the
        // information is left out (no empty panel).
        var item=itemCatalog.State.Catalog?.Find(hovered.ItemId);
        var usages=configuration.ItemUsagesOnHover && observed.Stable(now) && HoverInfoShown(item,hovered.ItemId);
        // 0.377 (g, M-07): the hovered piece against the worn one(s) of its slot, first (the most direct use).
        var compare=configuration.ItemUsagesOnHover && observed.Stable(now) ? HoverComparisons(hovered.ItemId,hovered.Kind==Items.ItemHoverKind.Hq):[];
        if(!prices && !usages && compare.Count==0)return;
        var viewport=ImGui.GetMainViewport();
        var (position,width)=HoverPlacement(observed.Anchor,viewport);
        Panels?.Show("TenshoItemHover",localization.Get("HoverUi.Title"),ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav,()=>{ImGui.SetNextWindowPos(position,ImGuiCond.Always);if(width is { } fixedWidth)ImGui.SetNextWindowSizeConstraints(new Vector2(fixedWidth,0),new Vector2(fixedWidth,viewport.WorkSize.Y));else ImGui.SetNextWindowSizeConstraints(Vector2.Zero,viewport.WorkSize);ImGui.SetNextWindowBgAlpha(0.94f);},()=>
        {
            ImGui.PushTextWrapPos(width is { } wrap ? wrap-ImGui.GetStyle().WindowPadding.X:Math.Min(350,Math.Max(120,viewport.WorkSize.X-32)));
            try
            {
                // Under the game tooltip the name and icon are already visible: only the mouse fallback repeats them.
                if(width is null)
                {
                    DrawItemIcon(item,new Vector2(28));ImGui.SameLine();
                    ImGui.TextColored(Accent,item?.Name ?? localization.Format("BankUi.Item",hovered.ItemId));
                }
                if(compare.Count>0)
                {
                    DrawHoverComparisons(compare);
                    if(usages)ImGui.Separator();
                }
                if(usages)
                {
                    // 0.257: same content as Tensho's item tooltip (it showed only collection, stock and usages).
                    DrawItemInfoBody(item,hovered.ItemId,hoverPanel:true);
                }
                if(prices)
                {
                    if(usages || compare.Count>0)ImGui.Separator();
                    MeasureMap("hover prices",()=>DrawHoveredPrices(priced!,item?.CanBeHq ?? true,observed.Stack));
                }
            }
            finally{ImGui.PopTextWrapPos();}
            hoverPanelSize=ImGui.GetWindowSize();
        });
    }

    // Below the game tooltip, else above it, else beside it; the mouse position is only a fallback when no tooltip is visible.
    private (Vector2 Position,float? Width) HoverPlacement(ItemHoverAnchor? anchor,ImGuiViewportPtr viewport)
    {
        var min=viewport.WorkPos;var max=viewport.WorkPos+viewport.WorkSize;const float gap=2;
        if(anchor is not { } tooltip)
        {
            var mouse=ImGui.GetMousePos()+new Vector2(24);
            return (new(Math.Clamp(mouse.X,min.X,Math.Max(min.X,max.X-hoverPanelSize.X)),Math.Clamp(mouse.Y,min.Y,Math.Max(min.Y,max.Y-hoverPanelSize.Y))),null);
        }
        var width=Math.Max(260,tooltip.Width);var height=hoverPanelSize.Y;
        Vector2 position;
        if(tooltip.Y+tooltip.Height+gap+height<=max.Y)position=new(tooltip.X,tooltip.Y+tooltip.Height+gap);
        else if(tooltip.Y-gap-height>=min.Y)position=new(tooltip.X,tooltip.Y-gap-height);
        else if(tooltip.X+tooltip.Width+gap+width<=max.X)position=new(tooltip.X+tooltip.Width+gap,tooltip.Y);
        else position=new(tooltip.X-gap-width,tooltip.Y);
        position.X=Math.Clamp(position.X,min.X,Math.Max(min.X,max.X-width));
        position.Y=Math.Clamp(position.Y,min.Y,Math.Max(min.Y,max.Y-height));
        return (position,width);
    }

    private void DrawHoveredStock(uint id)
    {
        var live=inventory.Current;var bankState=bank.State;var character=bankBridge.State.CharacterId;
        if(hoverHoldings is null || hoverHoldingsKey!=(id,live,bankState,character))
        {
            hoverHoldings=ItemHoldings.Build(id,character,live,bankState.Entries,StorageFilter.All,includeCrystals:true,crystal:ClassifyCrystal(id) is not null);
            hoverHoldingsKey=(id,live,bankState,character);
        }
        var summary=hoverHoldings;
        ImGui.TextColored(Accent,localization.Get("HoverUi.Stock"));
        if(!summary.HasObservation){ImGui.TextDisabled(localization.Get("HoverUi.OwnedUnknown"));return;}
        ImGui.TextUnformatted(localization.Format(summary.HasUnknown ? "HoverUi.OwnedPartial":"HoverUi.Owned",summary.Total.ToString("N0"),summary.Nq.ToString("N0"),summary.Hq.ToString("N0")));
        var rows=summary.Rows.Where(row=>row.Nq+row.Hq>0).OrderByDescending(row=>row.Nq+row.Hq).ToArray();
        if(rows.Length>0)
            ImGui.TextDisabled(string.Join(" · ",rows.Take(4).Select(row=>localization.Format("HoverUi.StorageRow",StorageRowName(row),(row.Nq+row.Hq).ToString("N0"))))
                +(rows.Length>4 ? localization.Format("HoverUi.More",rows.Length-4):""));
    }

    // Collection state of what the item unlocks (mount, minion, Triple Triad card, orchestrion roll), from the collection
    // observation of the character: obtained / not obtained / unknown when the observation does not prove it.
    private void DrawItemCollection(Items.ItemDefinition? item)
    {
        if(item is null)return;
        (Collections.CollectionKind Kind,uint Id)? link=item.MountId!=0 ? (Collections.CollectionKind.Mount,item.MountId)
            :item.CompanionId!=0 ? (Collections.CollectionKind.Companion,item.CompanionId)
            :item.TripleTriadCardId!=0 ? (Collections.CollectionKind.TripleTriad,item.TripleTriadCardId)
            :item.OrchestrionId!=0 ? (Collections.CollectionKind.Orchestrion,item.OrchestrionId):null;
        if(link is not { } target)
        {
            // X-76 P4 (0.927): any other item that teaches something, as the game says.
            if(GameItemUnlocked(item.Id) is not { } learned)return;
            ImGui.TextColored(learned ? UI.FfxivTheme.Success:UI.FfxivTheme.Error,localization.Format("HoverUi.Unlock",localization.Get(learned ? "HoverUi.UnlockYes":"HoverUi.UnlockNo")));
            return;
        }
        var entry=CollectionEntryOf(target.Kind,target.Id);
        var acquisition=CollectionStateOf(entry);
        if(acquisition==Collections.CollectionAcquisition.Unknown && GameItemUnlocked(item.Id) is { } game)
            acquisition=game ? Collections.CollectionAcquisition.Acquired:Collections.CollectionAcquisition.NotAcquired;
        var colour=acquisition switch{Collections.CollectionAcquisition.Acquired=>UI.FfxivTheme.Success,Collections.CollectionAcquisition.NotAcquired=>UI.FfxivTheme.Error,_=>UI.FfxivTheme.TextMuted};
        ImGui.TextColored(colour,localization.Format("HoverUi.Collection",localization.Get("HoverUi.CollectionKind"+target.Kind),localization.Get("HoverUi.Collection"+acquisition)));
    }
    // Obtained when the game says so; mounts and minions are read live from the game's unlock state at login, so « not
    // unlocked » there is a real « not obtained » once the collections are read (0.142: was shown as unknown); others need proof.
    private Collections.CollectionAcquisition CollectionStateOf(Collections.CollectionEntry? entry)
    {
        if(entry is null)return Collections.CollectionAcquisition.Unknown;
        if(entry.Acquisition!=Collections.CollectionAcquisition.Unknown)return entry.Acquisition;
        var state=collections.State;
        // 0.241 (R-02): Triple Triad cards and orchestrion rolls are tracked like mounts and minions.
        return entry.Kind is Collections.CollectionKind.Mount or Collections.CollectionKind.Companion or Collections.CollectionKind.TripleTriad or Collections.CollectionKind.Orchestrion && !state.Loading && state.CharacterId!=0
            && !(entry.Kind==Collections.CollectionKind.Orchestrion && state.CategoryIssues?.Any(issue=>issue.Kind==Collections.CollectionKind.Orchestrion)==true)
            ? Collections.CollectionAcquisition.NotAcquired:Collections.CollectionAcquisition.Unknown;
    }
    // Collection entry an item unlocks, if any.
    private Collections.CollectionEntry? CollectionOf(Items.ItemDefinition? item)
    {
        if(item is null)return null;
        (Collections.CollectionKind Kind,uint Id)? link=item.MountId!=0 ? (Collections.CollectionKind.Mount,item.MountId)
            :item.CompanionId!=0 ? (Collections.CollectionKind.Companion,item.CompanionId)
            :item.TripleTriadCardId!=0 ? (Collections.CollectionKind.TripleTriad,item.TripleTriadCardId)
            :item.OrchestrionId!=0 ? (Collections.CollectionKind.Orchestrion,item.OrchestrionId):null;
        if(link is not { } target)return null;
        // 0.275: indexed once per collection state (0.290: shared index, UI/Components/DataIndexes.cs).
        return CollectionEntryOf(target.Kind,target.Id);
    }
    // Right click on an item row: open its collection entry (mount, minion, card, roll) in the Collections page.
    private void ItemCollectionMenu(string id,Items.ItemDefinition? item)
    {
        if(CollectionOf(item) is not { } entry || !ImGui.BeginPopupContextItem(id))return;
        try
        {
            if(ImGui.Selectable(localization.Get(entry.Kind==Collections.CollectionKind.TripleTriad ? "ItemMenu.OpenTriad":"HoverUi.OpenCollection")))
            {
                if(entry.Kind==Collections.CollectionKind.TripleTriad)ShowTriadCard(entry.Id);
                else
                {
                    // 0.331: the state filter and the roll's category are reset to the entry's, which they could hide (as OpenOrchestrionFromItem).
                    page=Page.Collections;collectionKind=entry.Kind;focusCollectionKind=entry.Kind;collectionFilter=0;
                    if(entry.Kind==Collections.CollectionKind.Orchestrion){focusOrchestrionTab=true;orchestrionCategoryFilter=entry.Music?.CategoryId;}
                }
                collectionQuery=entry.Name;IsOpen=true;
            }
            if(item is not null){ImGui.Separator();DrawSourceLinks([item]);}
        }
        finally{ImGui.EndPopup();}
    }
    // How to obtain a collection entry: game description and source, unlocking items with their NPC vendors and prices,
    // quests that reward them, recipes that make them.
    private void DrawCollectionTooltip(Collections.CollectionEntry entry)
    {
        using(TooltipScope.Begin())
        {
            ImGui.TextColored(Accent,entry.Name);
            var acquisition=CollectionStateOf(entry);
            ImGui.TextColored(acquisition switch{Collections.CollectionAcquisition.Acquired=>UI.FfxivTheme.Success,Collections.CollectionAcquisition.NotAcquired=>UI.FfxivTheme.Error,_=>UI.FfxivTheme.TextMuted},
                localization.Format("HoverUi.Collection",localization.Get("HoverUi.CollectionKind"+entry.Kind),localization.Get("HoverUi.Collection"+acquisition)));
            if(entry.Location.Length>0)ImGui.TextWrapped(localization.Format("HoverUi.CollectionWhere",entry.Location));
            if(entry.Description.Length>0)ImGui.TextDisabled(entry.Description);
            var catalog=itemCatalog.State.Catalog;
            var items=catalog is null ? []:entry.Kind switch
            {
                Collections.CollectionKind.Mount=>catalog.ForMount(entry.Id),
                Collections.CollectionKind.Companion=>catalog.ForCompanion(entry.Id),
                Collections.CollectionKind.TripleTriad=>catalog.ForTriadCard(entry.Id),
                Collections.CollectionKind.Orchestrion=>catalog.ForOrchestrion(entry.Id),
                _=>[],
            };
            if(items.Count==0 && entry.Kind is Collections.CollectionKind.Mount or Collections.CollectionKind.Companion)ImGui.TextDisabled(localization.Get("HoverUi.CollectionNoItem"));
            // 0.387: the 0.365 kinds without a teaching item in the game data say so instead of an empty tooltip.
            if(items.Count==0 && entry.Kind is Collections.CollectionKind.Glasses or Collections.CollectionKind.Portrait or Collections.CollectionKind.McGuffin
                   or Collections.CollectionKind.OccultLore or Collections.CollectionKind.Hairstyle or Collections.CollectionKind.Ornament or Collections.CollectionKind.Barding
                   or Collections.CollectionKind.MasterBook or Collections.CollectionKind.Folklore)
                ImGui.TextDisabled(localization.Get("HoverUi.CollectionHowUnknown"));
            // Achievements rewarding the entry directly (title or item) are listed even without an unlocking item.
            foreach(var achievement in AchievementsRewarding(items.Select(item=>item.Id)).Take(3))
                ImGui.TextUnformatted(localization.Format("HoverUi.CollectionAchievement",achievement));
            foreach(var item in items.Take(3))
            {
                ImGui.Separator();
                DrawItemIcon(item,new Vector2(ImGui.GetTextLineHeight()));ImGui.SameLine();
                ImGui.TextUnformatted(localization.Format("HoverUi.CollectionItem",item.Name));
                var known=0;
                if(HoverOffers(item.Id) is { Count:>0 } offers)
                    foreach(var offer in offers.Take(3).Select(offer=>{known++;return offer;}))
                    {
                        // 0.146: the price goes on its own indented line; on the same line the wrap squeezed it into a column.
                        ImGui.TextWrapped(localization.Format("HoverUi.CollectionVendor",NpcName(offer.Vendor),offer.Vendor.Locations.FirstOrDefault() is { } place ? NpcPlace(place):localization.Get("NpcUi.NoLocation")));
                        ImGui.Indent();DrawNpcCosts(offer.Sale,ImGui.GetTextLineHeight(),false);ImGui.Unindent();
                    }
                if(quests.State.RewardIndex?.ForItem(item.Id) is { Count:>0 } rewards)
                {known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionQuests",string.Join(", ",rewards.Take(3).Select(reward=>reward.Name))));}
                if(recipeCatalog.State.Catalog?.ForResult(item.Id) is { Count:>0 } recipes){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionCraft",recipes.Count));}
                if(gatheringCatalog.State.Catalog?.ForItem(item.Id) is { Count:>0 } gathered){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionGathering",gathered[0].TypeName,gathered[0].Zone));}
                if(AchievementsRewarding([item.Id]).Any())known++;
                // 0.148: the market is a criterion, not a source: shown, but the real origin is still looked up.
                if(item.IsMarketable)ImGui.TextDisabled(localization.Get("HoverUi.CollectionMarket"));
                // Garland only when the plugin's own data knows nothing (0.146, user request).
                if(known==0)DrawFallbackSources(item.Id);
            }
            // 0.331: the right-click menu exists for these kinds only (MainWindow.Collections.cs), not for emotes.
            if(entry.Kind is Collections.CollectionKind.Mount or Collections.CollectionKind.Companion or Collections.CollectionKind.TripleTriad or Collections.CollectionKind.Orchestrion)
                ImGui.TextDisabled(localization.Get("SourceLinks.Hint"));
        }
    }

    private Community.GarlandItemSourceService? itemSources;
    private Community.WikiItemAcquisitionService? wikiItems;
    internal void ConfigureWikiItems(Community.WikiItemAcquisitionService service)=>wikiItems=service;
    // Last source (0.147): the wiki's « Acquisition » section, read only when the game data and Garland know nothing.
    private int DrawWikiAcquisition(uint itemId)
    {
        if(wikiItems is null)return 0;
        var state=wikiItems.Get(itemId);
        if(state.Loading){ImGui.TextDisabled(localization.Get("HoverUi.WikiLoading"));return 0;}
        if(state.Page is not { Lines.Count:>0 } page)return 0;
        foreach(var line in page.Lines)
            ImGui.TextWrapped("· "+(line.Heading.Length>0 ? WikiTerm(line.Heading)+" : ":"")+(AchievementNamed(line) is { } achievement ? achievement.Name:WikiTerm(line.Text)));
        ImGui.TextDisabled(localization.Get("HoverUi.WikiCredit"));
        return page.Lines.Count;
    }
    // Wiki words in the plugin language (0.150): section headings and known terms through the language files (section
    // « WikiTerms », key = the English term without spaces or punctuation), the two generated phrases with their numbers;
    // free text (event names…) stays in English. A term missing from the language files is shown as written.
    private string WikiTerm(string text)
    {
        if(localization.CurrentCode=="en" || text.Length==0)return text;
        if(System.Text.RegularExpressions.Regex.Match(text,@"^Online Store \((.+) USD\)$") is { Success:true } store)return localization.Format("WikiTerms.OnlineStorePrice",store.Groups[1].Value);
        if(System.Text.RegularExpressions.Regex.Match(text,@"^PvP series reward \(rank (\d+)\)$") is { Success:true } series)return localization.Format("WikiTerms.PvpSeriesRank",series.Groups[1].Value);
        var key="WikiTerms."+System.Text.RegularExpressions.Regex.Replace(text,"[^A-Za-z]","");
        var translated=key.Length>"WikiTerms.".Length ? localization.Get(key):text;
        return translated.StartsWith('[') && translated.EndsWith(']') ? text:translated;
    }
    // Garland, then the wiki, then « no known source » once both answered.
    private void DrawFallbackSources(uint itemId)
    {
        var known=DrawGarlandSources(itemId);
        if(known>0 || itemSources?.Get(itemId) is not { Loading:false })return;
        known=DrawWikiAcquisition(itemId);
        if(known!=0 || wikiItems?.Get(itemId) is not { Loading:false })return;
        // 0.329: a source turned off (Settings › Advanced) was not asked: say so instead of naming it among the sources looked up.
        ImGui.TextDisabled(localization.Get((SourceOn(SourceUse.GarlandItemSources),SourceOn(SourceUse.WikiItemAcquisition)) switch
        {
            (true,true)=>"HoverUi.CollectionNoSource",(false,true)=>"HoverUi.CollectionNoSourceGarlandOff",
            (true,false)=>"HoverUi.CollectionNoSourceWikiOff",_=>"HoverUi.CollectionNoSourceOff",
        }));
    }
    internal void ConfigureItemSources(Community.GarlandItemSourceService service)=>itemSources=service;
    // Garland sources of an item (duties, monsters, FATEs, quests, achievements, leves), loaded on first display; count shown.
    private int DrawGarlandSources(uint itemId)
    {
        if(itemSources is null)return 0;
        var state=itemSources.Get(itemId);
        if(state.Loading){ImGui.TextDisabled(localization.Get("HoverUi.GarlandLoading"));return 0;}
        if(state.List is not { } list || list.Sources.Count==0 && list.Trades is not { Count:>0 })return 0;
        var catalog=itemCatalog.State.Catalog;
        foreach(var trade in list.Trades ?? [])
        {
            ImGui.TextUnformatted(localization.Format("HoverUi.GarlandTrade",trade.Amount.ToString("N0"),catalog?.Find(trade.CurrencyId)?.Name ?? $"#{trade.CurrencyId}"));
            if(catalog?.Find(trade.CurrencyId) is { } currency){ImGui.SameLine(0,4);DrawItemIcon(currency,new Vector2(ImGui.GetTextLineHeight()));}
        }
        foreach(var group in list.Sources.GroupBy(source=>source.Type).Take(6))
            ImGui.TextUnformatted(localization.Format("HoverUi.GarlandSource",localization.Get("HoverUi.GarlandType_"+group.Key),
                string.Join(", ",group.Select(CommunityGarland).Distinct().Take(3))+(group.Count()>3 ? localization.Format("HoverUi.More",group.Count()-3):"")));
        ImGui.TextDisabled(localization.Get("HoverUi.GarlandCredit"));
        return list.Sources.Count+(list.Trades?.Count ?? 0);
    }

    // Achievements whose reward is one of these items (Achievement.Item, kept by the collection observation).
    private IEnumerable<string> AchievementsRewarding(IEnumerable<uint> itemIds)
    {
        // 0.290: achievements indexed by reward item instead of a scan of every collection row.
        return itemIds.Distinct().SelectMany(AchievementsRewardingItem).Select(entry=>entry.Name).Distinct();
    }

    // Item tooltip for the tool lists (NPC vendors…): icon, name, category and levels, collection state, stock and uses.
    private void DrawEnglishItemName(Items.ItemDefinition? item)
    {
        if(configuration.ShowEnglishItemName && item is {EnglishName.Length:>0} && !string.Equals(item.EnglishName,item.Name,StringComparison.Ordinal))ImGui.TextDisabled(item.EnglishName);
    }
    private void DrawItemInfoTooltip(uint id)
    {
        if(ItemTooltipAlreadyDrawn(id))return;
        var item=itemCatalog.State.Catalog?.Find(id);
        using(TooltipScope.Begin())
        {
            DrawEnglishItemName(item);
            DrawItemImage(item,new Vector2(32));ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.TextColored(Accent,item?.Name ?? localization.Format("BankUi.Item",id));
            if(item is { UiCategoryName.Length:>0 })ImGui.TextDisabled(item.UiCategoryName);
            ImGui.EndGroup();
            if(item is { ItemLevel:>1 })ImGui.TextDisabled(localization.Format("HoverUi.Levels",item.ItemLevel,item.EquipLevel));
            if(ShowIds)ImGui.TextDisabled($"ID {id}");
            DrawItemInfoBody(item,id);
        }
    }

    // 0.257: the body of the common item tooltip (collection, stock, usages, how to get it), also drawn under the game's tooltip so
    // both show the same; the header (icon, name) is left to the caller.
    // e) (audit 2026-11-02): hoverPanel = the panel under the game's item tooltip, whose four parts follow Settings › Interface;
    // Tensho's own item tooltips and the item sheet (hoverPanel false) always draw every part.
    // X-89 (0.970, user request): the item's expansion, only when its sure sources agree (Quests/ExpansionOrigin.cs); computed once per item
    // and catalogue (no sheet read each frame), nothing while the gathering catalogue or the quest rewards are not loaded.
    internal Func<uint,string?>? TerritoryExpansion { get; set; }
    internal Func<uint,string?>? QuestExpansion { get; set; }
    private readonly Dictionary<uint,(string Name,int From)?> itemExpansions=[];
    private (object? Gathering,object? Rewards) itemExpansionsFor;
    private void DrawItemExpansion(uint id)
    {
        if(TerritoryExpansion is not { } zoneExpansion || QuestExpansion is not { } questExpansion)return;
        var gathering=gatheringCatalog.State.Catalog;var rewards=quests.State.RewardIndex;
        if(gathering is null || rewards is null)return;
        if(!ReferenceEquals(itemExpansionsFor.Gathering,gathering) || !ReferenceEquals(itemExpansionsFor.Rewards,rewards) || itemExpansions.Count>4096)
        {itemExpansions.Clear();itemExpansionsFor=(gathering,rewards);}
        if(!itemExpansions.TryGetValue(id,out var origin))
        {
            var zones=gathering.ForItem(id).Select(source=>source.TerritoryId).Distinct().Select(territory=>territory==0 ? null:zoneExpansion(territory)).ToArray();
            var fromQuests=rewards.ForItem(id).Select(reward=>reward.QuestId).Distinct().Select(quest=>questExpansion(quest)).ToArray();
            itemExpansions[id]=origin=TenshoAssistant.Quests.ExpansionOrigin.Of(zones,fromQuests);
        }
        if(origin is { } found)ImGui.TextDisabled(localization.Format(found.From switch{1=>"HoverUi.ExpansionZones",2=>"HoverUi.ExpansionQuests",_=>"HoverUi.ExpansionBoth"},found.Name));
    }
    private void DrawItemInfoBody(Items.ItemDefinition? item,uint id,bool hoverPanel=false)
    {
        // 0.347 (g, I-08): the player's own note first.
        DrawItemNoteLine(id);
        DrawItemExpansion(id);
        // X-95 (0.951): the parts of the item panel timed in developer mode (the panel reached 173 ms in the slow frame log).
        if(!hoverPanel || configuration.HoverShowCollection)MeasureMap("hover collection",()=>DrawItemCollection(item));
        if(!hoverPanel || configuration.HoverShowStock)MeasureMap("hover stock",()=>DrawHoveredStock(id));
        // 0.346 (g, I-06): NPC sale price (Item.PriceLow, game data), and the hovered stack's when the game tooltip is for one of the
        // player's slots; for NQ (the HQ price is not in the sheet); nothing when the item does not sell to NPCs (price 0).
        if(item is { SellPrice:>0 } sold)
        {
            var stack=hoverPanel ? itemHover.State.Stack:null;
            ImGui.TextDisabled(stack is > 1 and var count
                ? localization.Format("HoverUi.NpcSaleStack",sold.SellPrice.ToString("N0"),count,(sold.SellPrice*(long)count).ToString("N0"))
                :localization.Format("HoverUi.NpcSale",sold.SellPrice.ToString("N0")));
        }
        if(!hoverPanel || configuration.HoverShowUsages)MeasureMap("hover usages",()=>DrawHoveredUsages(id));
        // « How to get it » is the last part: hidden, its Garland / wiki fallback is not asked from the hover either.
        if(hoverPanel && !configuration.HoverShowSources)return;
        // How to obtain the item itself (0.144: the sources follow the unlocking item, not only the collection page).
        ImGui.TextColored(Accent,localization.Get("HoverUi.HowToGet"));
        var known=0;
        // X-74 (0.865): what was seen in game first (discoveries base, yours or shared); a source seen there makes the Garland / wiki fallback needless.
        foreach(var seen in DiscoveredSourcesOf(id)){known++;ImGui.TextWrapped(localization.Format("HoverUi.SeenSource",seen));}
        if(HoverOffers(id) is { Count:>0 } offers){known++;ImGui.TextUnformatted(localization.Format("HoverUi.VendorCount",offers.Select(offer=>offer.Vendor.NpcId).Distinct().Count()));}
        if(quests.State.RewardIndex?.ForItem(id) is { Count:>0 } rewards){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionQuests",string.Join(", ",rewards.Take(3).Select(reward=>reward.Name))));}
        foreach(var achievement in AchievementsRewarding([id]).Take(3)){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionAchievement",achievement));}
        foreach(var dutyId in LuminaDutiesOf(id).Take(3))
            if(duties.State.Catalog?.Find(dutyId) is { } lootDuty){known++;ImGui.TextUnformatted(localization.Format("HoverUi.LuminaDuty",lootDuty.Name));}
        // X-76 P5 (0.928): targeted retainer ventures from the game's sheets (RetainerTask → RetainerTaskNormal.Item), level and jobs.
        var ventureTasks=VentureTasksOf?.Invoke(id) ?? [];
        if(ventureTasks.Count>0){known++;ImGui.TextWrapped(localization.Format("HoverUi.Venture",string.Join(", ",ventureTasks.Take(3).Select(task=>localization.Format("HoverUi.VentureTask",task.Level,task.Jobs)))));}
        if(lumina?.State.Data is { } supplement)
        {
            if(supplement.MonstersOf(id) is { Count:>0 } mobs){known++;ImGui.TextWrapped(localization.Format("HoverUi.LuminaMonsters",string.Join(", ",mobs.Take(4))));}
            if(supplement.FatesOf(id) is { Count:>0 } fates){known++;ImGui.TextWrapped(localization.Format("HoverUi.LuminaFates",string.Join(", ",fates.Take(3))));}
            // Exploration ventures give no item list in the sheets: the community line stays for them only.
            if(ventureTasks.Count==0 && supplement.FromVenture(id)){known++;ImGui.TextUnformatted(localization.Get("HoverUi.LuminaVenture"));}
            foreach(var (kind,count,sources) in supplement.SupplementsOf(id))
            {
                known++;
                var names=string.Join(", ",sources.Take(3).Select(source=>itemCatalog.State.Catalog?.Find(source)?.Name ?? source.ToString()));
                ImGui.TextWrapped(localization.Format("HoverUi.Supplement"+(kind is >= 1 and <= 19 ? kind:0),names)+(count>3 ? localization.Format("HoverUi.SupplementMore",count-3):""));
            }
            if(supplement.StorePriceCents(id) is { } cents){known++;ImGui.TextUnformatted(localization.Format("HoverUi.LuminaStore",(cents/100m).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)));}
        }
        if(recipeCatalog.State.Catalog?.ForResult(id) is { Count:>0 } recipes){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionCraft",recipes.Count));}
        if(gatheringCatalog.State.Catalog?.ForItem(id) is { Count:>0 } gathered){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionGathering",gathered[0].TypeName,gathered[0].Zone));}
        if(item is { IsMarketable:true })ImGui.TextDisabled(localization.Get("HoverUi.CollectionMarket"));
        if(known==0)DrawFallbackSources(id);
    }

    // e): true when a part turned on has something to draw for this item; Stock and « How to get it » always draw their heading.
    // Cheap lookups, cut short with the default settings (stock on).
    private bool HoverInfoShown(Items.ItemDefinition? item,uint id)=>configuration.HoverShowStock || configuration.HoverShowSources
        || configuration.HoverShowCollection && (item is { MountId:not 0 } or { CompanionId:not 0 } or { TripleTriadCardId:not 0 } or { OrchestrionId:not 0 } || item is not null && GameItemUnlocked(item.Id) is not null)
        || configuration.HoverShowUsages && HoveredUsageLines(id).Count>0;

    private void DrawHoveredUsages(uint id)
    {
        var lines=HoveredUsageLines(id);
        if(lines.Count==0)return;
        ImGui.TextColored(Accent,localization.Get("HoverUi.Usages"));
        foreach(var line in lines)ImGui.TextWrapped(line);
    }
    private List<string> HoveredUsageLines(uint id)
    {
        var lines=new List<string>();
        if(recipeCatalog.State.Catalog is { } recipes && recipes.ForIngredient(id).Count is > 0 and var count)lines.Add(localization.Format("HoverUi.RecipeCount",count));
        if(itemUses.State.Catalog is { } uses)
        {
            if(uses.LevesFor(id).Count is > 0 and var leves)lines.Add(localization.Format("HoverUi.Leves",leves));
            if(uses.IsSupplyCandidate(id))lines.Add(GcSupplyLine(id));
            if(uses.IsAquariumFish(id))lines.Add(localization.Get("HoverUi.Aquarium"));
        }
        return lines;
    }

    private void DrawHoveredPrices(MarketHoverItem hovered,bool canBeHq,uint? stack)
    {
        var state=market.State;var quote=market.GetCached(state.WorldId,hovered.ItemId);var background=market.BackgroundState;var now=DateTimeOffset.UtcNow;
        ImGui.TextColored(Accent,localization.Format("HoverUi.Prices",state.WorldName));
        if(quote is { } prices)
        {
            // 0.343 (I-01): under each minimum, the last sale, the average sale price and the sales per day (unknown = « — »).
            DrawPriceLine("NQ",prices.Nq.Minimum);DrawSaleLine(prices.Nq);
            if(canBeHq){DrawPriceLine("HQ",prices.Hq.Minimum);DrawSaleLine(prices.Hq);}
            // The hovered stack at the minimum of its own quality (lowest listing × quantity: an order of size, not a sale).
            if(stack is > 1 and var count && (hovered.Hq ? prices.Hq.Minimum:prices.Nq.Minimum) is { } unit)
            {
                ImGui.TextUnformatted(localization.Format("HoverUi.Stack",count,(unit*count).ToString("N0")));
                DrawGilIcon();
            }
            ImGui.TextDisabled(prices.UploadedAt is { } date ? localization.Format("HoverUi.Uploaded",date.ToLocalTime().ToString("g")):localization.Get("HoverUi.UnknownDate"));
            if(!prices.HasRecentUpload(now))ImGui.TextColored(Accent,localization.Get("HoverUi.Old"));
            if(prices.NqCheapest is not null || canBeHq && prices.HqCheapest is not null)
            {
                ImGui.TextColored(Accent,localization.Format("HoverUi.Cheapest",state.DataCenterName.Length>0 ? state.DataCenterName:localization.Get("HoverUi.DataCenter")));
                DrawCheapest("NQ",prices.NqCheapest,state,now);
                if(canBeHq)DrawCheapest("HQ",prices.HqCheapest,state,now);
            }
            // 0.343 (I-01): cheapest of the whole region, shown only when cheaper than the data center's.
            if(Cheaper(prices.NqRegion,prices.NqCheapest) || canBeHq && Cheaper(prices.HqRegion,prices.HqCheapest))
            {
                ImGui.TextColored(Accent,localization.Get("HoverUi.Region"));
                if(Cheaper(prices.NqRegion,prices.NqCheapest))DrawCheapest("NQ",prices.NqRegion,state,now);
                if(canBeHq && Cheaper(prices.HqRegion,prices.HqCheapest))DrawCheapest("HQ",prices.HqRegion,state,now);
            }
            // 0.329: date and time, as on the Market page: a quote restored from the disk cache can be from another day.
            ImGui.TextDisabled(localization.Format("HoverUi.Fetched",prices.FetchedAt.ToLocalTime().ToString("G")));
            if(!market.IsFresh(prices,now))ImGui.TextDisabled(localization.Get("HoverUi.Expired"));
            if(configuration.HoverPriceRefresh)ImGui.TextDisabled(localization.Get("HoverUi.RefreshHint"));
        }
        else ImGui.TextWrapped(background?.ItemId==hovered.ItemId && background.WorldId==state.WorldId
            ? background.Loading ? localization.Get("HoverUi.Loading"):background.Error is not null ? localization.Get("HoverUi.Error") : localization.Get("HoverUi.NoPrices")
            :localization.Get("HoverUi.Pending"));
        ImGui.TextDisabled(localization.Get("HoverUi.Source"));
    }

    private static bool Cheaper(MarketListing? region,MarketListing? dataCenter)=>region is { } cheapest && (dataCenter is null || cheapest.Price<dataCenter.Price);

    private void DrawSaleLine(MarketQualityPrice price)
    {
        var last=price.RecentSale is { } sale ? sale.ToString("N0")+(price.SaleAt is { } at ? $" ({at.ToLocalTime():g})":""):"—";
        ImGui.TextDisabled(localization.Format("HoverUi.Sales",last,price.Average is { } average ? average.ToString("N0"):"—",
            price.Velocity is { } velocity ? velocity.ToString("0.#"):"—"));
    }

    // Amounts end with the game's Gil icon instead of a currency word.
    private void DrawPriceLine(string quality,decimal? price)
    {
        if(price is not { } value){ImGui.TextUnformatted(localization.Format("HoverUi.NoListing",quality));return;}
        ImGui.TextUnformatted(localization.Format("HoverUi.PriceLine",quality,value.ToString("N0")));
        DrawGilIcon();
    }
    private void DrawGilIcon()
    {
        if(itemCatalog.State.Catalog?.Find(1) is not { } gil)return;
        ImGui.SameLine(0,3);DrawItemIcon(gil,new Vector2(ImGui.GetTextLineHeight()));
    }

    private void DrawCheapest(string quality,MarketListing? listing,MarketState state,DateTimeOffset now)
    {
        if(listing is not { } cheapest){ImGui.TextDisabled(localization.Format("HoverUi.NoListing",quality));return;}
        var world=cheapest.WorldId==state.WorldId ? localization.Get("HoverUi.ThisWorld"):state.NameOf(cheapest.WorldId) ?? WorldName?.Invoke(cheapest.WorldId) ?? localization.Format("HoverUi.WorldId",cheapest.WorldId);
        ImGui.TextUnformatted(localization.Format("HoverUi.PriceLine",quality,cheapest.Price.ToString("N0")));
        DrawGilIcon();ImGui.SameLine(0,6);ImGui.TextUnformatted(localization.Format("HoverUi.CheapestWorld",world));
        if(!MarketQuote.Recent(cheapest.UploadedAt,now))
            ImGui.TextDisabled(cheapest.UploadedAt is { } date ? localization.Format("HoverUi.CheapestOld",date.ToLocalTime().ToString("g")):localization.Get("HoverUi.UnknownDate"));
    }
}
