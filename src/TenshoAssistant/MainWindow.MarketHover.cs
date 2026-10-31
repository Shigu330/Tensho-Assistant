using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;
using TenshoAssistant.Market;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private Vector2 hoverPanelSize=new(380,300);
    private (uint Item,object? Live,object? Bank,ulong Character) hoverHoldingsKey;
    private ItemHoldingsSummary? hoverHoldings;
    private Monsters.MonsterHoverSources? monsterHover;
    internal void ConfigureMonsterHover(Monsters.MonsterHoverSources service)=>monsterHover=service;
    public void DrawItemHover()
    {
        var observed=itemHover.State;var now=DateTimeOffset.UtcNow;
        if(windowDisposed || !observed.Fresh(now) || observed.Item is not { } hovered)return;
        var priced=MarketHoverTracker.Eligible(hovered,itemCatalog.State.Catalog);
        var prices=configuration.MarketPricesOnHover && market.State.WorldId!=0 && priced is not null;
        var usages=configuration.ItemUsagesOnHover && observed.Stable(now);
        if(!prices && !usages)return;
        using var theme=TenshoAssistant.UI.FfxivTheme.Apply();
        var viewport=ImGui.GetMainViewport();
        var (position,width)=HoverPlacement(observed.Anchor,viewport);
        ImGui.SetNextWindowPos(position,ImGuiCond.Always);
        if(width is { } fixedWidth)ImGui.SetNextWindowSizeConstraints(new Vector2(fixedWidth,0),new Vector2(fixedWidth,viewport.WorkSize.Y));
        else ImGui.SetNextWindowSizeConstraints(Vector2.Zero,viewport.WorkSize);
        ImGui.SetNextWindowBgAlpha(0.94f);
        var visible=ImGui.Begin(localization.Get("HoverUi.Title")+"###TenshoItemHover",ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav);
        try
        {
            if(!visible)return;
            var item=itemCatalog.State.Catalog?.Find(hovered.ItemId);
            ImGui.PushTextWrapPos(width is { } wrap ? wrap-ImGui.GetStyle().WindowPadding.X:Math.Min(350,Math.Max(120,viewport.WorkSize.X-32)));
            try
            {
                // Under the game tooltip the name and icon are already visible: only the mouse fallback repeats them.
                if(width is null)
                {
                    DrawItemIcon(item,new Vector2(28));ImGui.SameLine();
                    ImGui.TextColored(Accent,item?.Name ?? localization.Format("HoverUi.Item",hovered.ItemId));
                }
                if(usages)
                {
                    DrawItemCollection(item);
                    DrawHoveredStock(hovered.ItemId);
                    DrawHoveredSources(hovered.ItemId,observed.StableSince is { } since ? now-since:TimeSpan.Zero);
                    DrawHoveredUsages(hovered.ItemId);
                }
                if(prices)
                {
                    if(usages)ImGui.Separator();
                    DrawHoveredPrices(priced!,item?.CanBeHq ?? true);
                }
            }
            finally{ImGui.PopTextWrapPos();}
            hoverPanelSize=ImGui.GetWindowSize();
        }
        finally{ImGui.End();}
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
            hoverHoldings=ItemHoldings.Build(id,character,live,bankState.Entries,StorageFilter.All,includeCrystals:true);
            hoverHoldingsKey=(id,live,bankState,character);
        }
        var summary=hoverHoldings;
        ImGui.TextColored(Accent,localization.Get("HoverUi.Stock"));
        if(!summary.HasObservation){ImGui.TextDisabled(localization.Get("HoverUi.OwnedUnknown"));return;}
        ImGui.TextUnformatted(localization.Format(summary.HasUnknown ? "HoverUi.OwnedPartial":"HoverUi.Owned",summary.Total.ToString("N0"),summary.Nq.ToString("N0"),summary.Hq.ToString("N0")));
        var rows=summary.Rows.Where(row=>row.Nq+row.Hq>0).OrderByDescending(row=>row.Nq+row.Hq).ToArray();
        if(rows.Length>0)
            ImGui.TextDisabled(string.Join(" · ",rows.Take(4).Select(row=>localization.Format("HoverUi.StorageRow",row.Name,(row.Nq+row.Hq).ToString("N0"))))
                +(rows.Length>4 ? localization.Format("HoverUi.More",rows.Length-4):""));
    }

    private void DrawHoveredSources(uint id,TimeSpan hoveredFor)
    {
        var lines=new List<string>();
        string? monsterStatus=null;
        var crafted=recipeCatalog.State.Catalog?.ForResult(id) ?? [];
        var gathered=gatheringCatalog.State.Catalog?.ForItem(id) ?? [];
        if(crafted.Count>0)
            lines.Add(localization.Format("HoverUi.Craftable",crafted.Count,string.Join(", ",crafted.Select(recipe=>recipe.CraftType).Where(name=>name.Length>0).Distinct().Take(3))));
        if(gathered.Count>0)
        {
            var first=gathered[0];
            lines.Add(localization.Format("HoverUi.Gathered",first.TypeName,first.Level,first.Zone)+(gathered.Count>1 ? localization.Format("HoverUi.More",gathered.Count-1):""));
        }
        // Only materials that can be monster drops are looked up; equipment, crafted and gathered items never trigger a request.
        if(Monsters.MonsterDropCandidates.Eligible(itemCatalog.State.Catalog?.Find(id),crafted.Count>0,gathered.Count>0)
            && monsterHover?.Observe(id,hoveredFor,configuration.MonsterSourcesOnHover) is { } monsters)
        {
            if(monsters.Document is { Monsters.Count:>0 } document)
            {
                var first=document.Monsters[0];
                var head=first.Level.Length>0 ? localization.Format("HoverUi.MonsterLevel",first.Name,first.Level):first.Name;
                if(first.Location.Length>0)head+=" · "+first.Location;
                lines.Add(localization.Format("HoverUi.Monsters",head)+(document.Monsters.Count>1 ? localization.Format("HoverUi.More",document.Monsters.Count-1):""));
            }
            monsterStatus=monsters.Status switch
            {
                Monsters.MonsterHoverStatus.Loading=>localization.Get("HoverUi.MonstersLoading"),
                Monsters.MonsterHoverStatus.Unavailable=>localization.Get("HoverUi.MonstersUnavailable"),
                _=>monsters.Stale && monsters.Document is { Monsters.Count:>0 } ? localization.Get("HoverUi.MonstersOld"):null,
            };
        }
        if(quests.State.RewardIndex?.ForItem(id) is { Count:>0 } rewards)lines.Add(localization.Format("HoverUi.QuestRewards",rewards.Count));
        if(lines.Count==0 && monsterStatus is null && HoveredNpcCount(id)==0)return;
        ImGui.TextColored(Accent,localization.Get("HoverUi.Obtain"));
        DrawHoveredNpcs(id);
        foreach(var line in lines)ImGui.TextWrapped(line);
        if(monsterStatus is not null)ImGui.TextDisabled(monsterStatus);
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
        if(link is not { } target)return;
        var entry=collections.State.Entries.FirstOrDefault(row=>row.Kind==target.Kind && row.Id==target.Id);
        var acquisition=CollectionStateOf(entry);
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
        return entry.Kind is Collections.CollectionKind.Mount or Collections.CollectionKind.Companion && !state.Loading && state.CharacterId!=0
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
        return link is { } target ? collections.State.Entries.FirstOrDefault(row=>row.Kind==target.Kind && row.Id==target.Id):null;
    }
    // Right click on an item row: open its collection entry (mount, minion, card, roll) in the Collections page.
    private void ItemCollectionMenu(string id,Items.ItemDefinition? item)
    {
        if(CollectionOf(item) is not { } entry || !ImGui.BeginPopupContextItem(id))return;
        try
        {
            if(ImGui.Selectable(localization.Get("HoverUi.OpenCollection")))
            {
                if(entry.Kind==Collections.CollectionKind.TripleTriad)page=Page.TripleTriad;
                else{page=Page.Collections;collectionKind=entry.Kind;focusCollectionKind=entry.Kind;if(entry.Kind==Collections.CollectionKind.Orchestrion)focusOrchestrionTab=true;}
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
        ImGui.BeginTooltip();
        try
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize()*26);
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
            // Achievements rewarding the entry directly (title or item) are listed even without an unlocking item.
            foreach(var achievement in AchievementsRewarding(items.Select(item=>item.Id)).Take(3))
                ImGui.TextUnformatted(localization.Format("HoverUi.CollectionAchievement",achievement));
            foreach(var item in items.Take(3))
            {
                ImGui.Separator();
                DrawItemIcon(item,new Vector2(ImGui.GetTextLineHeight()));ImGui.SameLine();
                ImGui.TextUnformatted(localization.Format("HoverUi.CollectionItem",item.Name));
                var known=0;
                if(npcVendors?.State.Catalog?.ForItem(item.Id) is { Count:>0 } offers)
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
            ImGui.TextDisabled(localization.Get("SourceLinks.Hint"));
            ImGui.PopTextWrapPos();
        }
        finally{ImGui.EndTooltip();}
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
        if(known==0 && wikiItems?.Get(itemId) is { Loading:false })ImGui.TextDisabled(localization.Get("HoverUi.CollectionNoSource"));
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
                string.Join(", ",group.Select(source=>source.Name).Distinct().Take(3))+(group.Count()>3 ? localization.Format("HoverUi.More",group.Count()-3):"")));
        ImGui.TextDisabled(localization.Get("HoverUi.GarlandCredit"));
        return list.Sources.Count+(list.Trades?.Count ?? 0);
    }

    // Achievements whose reward is one of these items (Achievement.Item, kept by the collection observation).
    private IEnumerable<string> AchievementsRewarding(IEnumerable<uint> itemIds)
    {
        var wanted=itemIds.ToHashSet();
        if(wanted.Count==0)return [];
        return collections.State.Entries.Where(entry=>entry.Kind==Collections.CollectionKind.Achievement && entry.Achievement is { RewardItemId:not 0 } metadata && wanted.Contains(metadata.RewardItemId))
            .Select(entry=>entry.Name).Distinct();
    }

    // Item tooltip for the tool lists (NPC vendors…): icon, name, category and levels, collection state, stock and uses.
    private void DrawItemInfoTooltip(uint id)
    {
        var item=itemCatalog.State.Catalog?.Find(id);
        ImGui.BeginTooltip();
        try
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize()*24);
            DrawItemIcon(item,new Vector2(32));ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.TextColored(Accent,item?.Name ?? localization.Format("HoverUi.Item",id));
            if(item is { UiCategoryName.Length:>0 })ImGui.TextDisabled(item.UiCategoryName);
            ImGui.EndGroup();
            if(item is { ItemLevel:>1 })ImGui.TextDisabled(localization.Format("HoverUi.Levels",item.ItemLevel,item.EquipLevel));
            DrawItemCollection(item);
            DrawHoveredStock(id);
            DrawHoveredUsages(id);
            // How to obtain the item itself (0.144: the sources follow the unlocking item, not only the collection page).
            ImGui.TextColored(Accent,localization.Get("HoverUi.HowToGet"));
            var known=0;
            if(npcVendors?.State.Catalog?.ForItem(id) is { Count:>0 } offers){known++;ImGui.TextUnformatted(localization.Format("HoverUi.VendorCount",offers.Select(offer=>offer.Vendor.NpcId).Distinct().Count()));}
            if(quests.State.RewardIndex?.ForItem(id) is { Count:>0 } rewards){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionQuests",string.Join(", ",rewards.Take(3).Select(reward=>reward.Name))));}
            foreach(var achievement in AchievementsRewarding([id]).Take(3)){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionAchievement",achievement));}
            foreach(var dutyId in LuminaDutiesOf(id).Take(3))
                if(duties.State.Catalog?.Find(dutyId) is { } lootDuty){known++;ImGui.TextUnformatted(localization.Format("HoverUi.LuminaDuty",lootDuty.Name));}
            if(lumina?.State.Data is { } supplement)
            {
                if(supplement.MonstersOf(id) is { Count:>0 } mobs){known++;ImGui.TextWrapped(localization.Format("HoverUi.LuminaMonsters",string.Join(", ",mobs.Take(4))));}
                if(supplement.FatesOf(id) is { Count:>0 } fates){known++;ImGui.TextWrapped(localization.Format("HoverUi.LuminaFates",string.Join(", ",fates.Take(3))));}
                if(supplement.FromVenture(id)){known++;ImGui.TextUnformatted(localization.Get("HoverUi.LuminaVenture"));}
                if(supplement.StorePriceCents(id) is { } cents){known++;ImGui.TextUnformatted(localization.Format("HoverUi.LuminaStore",(cents/100m).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)));}
            }
            if(recipeCatalog.State.Catalog?.ForResult(id) is { Count:>0 } recipes){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionCraft",recipes.Count));}
            if(gatheringCatalog.State.Catalog?.ForItem(id) is { Count:>0 } gathered){known++;ImGui.TextUnformatted(localization.Format("HoverUi.CollectionGathering",gathered[0].TypeName,gathered[0].Zone));}
            if(item is { IsMarketable:true })ImGui.TextDisabled(localization.Get("HoverUi.CollectionMarket"));
            if(known==0)DrawFallbackSources(id);
            ImGui.PopTextWrapPos();
        }
        finally{ImGui.EndTooltip();}
    }

    private void DrawHoveredUsages(uint id)
    {
        var lines=new List<string>();
        if(recipeCatalog.State.Catalog is { } recipes && recipes.ForIngredient(id).Count is > 0 and var count)lines.Add(localization.Format("HoverUi.RecipeCount",count));
        if(itemUses.State.Catalog is { } uses)
        {
            if(uses.LevesFor(id).Count is > 0 and var leves)lines.Add(localization.Format("HoverUi.Leves",leves));
            if(uses.IsSupplyCandidate(id))lines.Add(localization.Get("HoverUi.Supply"));
            if(uses.IsAquariumFish(id))lines.Add(localization.Get("HoverUi.Aquarium"));
        }
        if(lines.Count==0)return;
        ImGui.TextColored(Accent,localization.Get("HoverUi.Usages"));
        foreach(var line in lines)ImGui.TextWrapped(line);
    }

    private void DrawHoveredPrices(MarketHoverItem hovered,bool canBeHq)
    {
        var state=market.State;var quote=market.GetCached(state.WorldId,hovered.ItemId);var background=market.BackgroundState;var now=DateTimeOffset.UtcNow;
        ImGui.TextColored(Accent,localization.Format("HoverUi.Prices",state.WorldName));
        if(quote is { } prices)
        {
            DrawPriceLine("NQ",prices.Nq.Minimum);
            if(canBeHq)DrawPriceLine("HQ",prices.Hq.Minimum);
            ImGui.TextDisabled(prices.UploadedAt is { } date ? localization.Format("HoverUi.Uploaded",date.ToLocalTime().ToString("g")):localization.Get("HoverUi.UnknownDate"));
            if(!prices.HasRecentUpload(now))ImGui.TextColored(Accent,localization.Get("HoverUi.Old"));
            if(prices.NqCheapest is not null || canBeHq && prices.HqCheapest is not null)
            {
                ImGui.TextColored(Accent,localization.Format("HoverUi.Cheapest",state.DataCenterName.Length>0 ? state.DataCenterName:localization.Get("HoverUi.DataCenter")));
                DrawCheapest("NQ",prices.NqCheapest,state,now);
                if(canBeHq)DrawCheapest("HQ",prices.HqCheapest,state,now);
            }
            ImGui.TextDisabled(localization.Format("HoverUi.Fetched",prices.FetchedAt.ToLocalTime().ToString("T")));
            if(!market.IsFresh(prices,now))ImGui.TextDisabled(localization.Get("HoverUi.Expired"));
        }
        else ImGui.TextWrapped(background?.ItemId==hovered.ItemId && background.WorldId==state.WorldId
            ? background.Loading ? localization.Get("HoverUi.Loading"):background.Error is not null ? localization.Get("HoverUi.Error") : localization.Get("HoverUi.NoPrices")
            :localization.Get("HoverUi.Pending"));
        ImGui.TextDisabled(localization.Get("HoverUi.Source"));
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
        var world=cheapest.WorldId==state.WorldId ? localization.Get("HoverUi.ThisWorld"):state.NameOf(cheapest.WorldId) ?? localization.Format("HoverUi.WorldId",cheapest.WorldId);
        ImGui.TextUnformatted(localization.Format("HoverUi.PriceLine",quality,cheapest.Price.ToString("N0")));
        DrawGilIcon();ImGui.SameLine(0,6);ImGui.TextUnformatted(localization.Format("HoverUi.CheapestWorld",world));
        if(!MarketQuote.Recent(cheapest.UploadedAt,now))
            ImGui.TextDisabled(cheapest.UploadedAt is { } date ? localization.Format("HoverUi.CheapestOld",date.ToLocalTime().ToString("g")):localization.Get("HoverUi.UnknownDate"));
    }
}
