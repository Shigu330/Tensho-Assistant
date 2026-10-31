using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    internal void OpenItemContext(ItemMenuRequest request)
    {
        SelectItem(request.Token.Item.ItemId);
        OpenRoute(request.Route,request.Token.Item.ItemId);
        IsOpen=true;
    }

    // Opens one tool for the item (inventory menu and context item panel share it, 0.208).
    private void OpenRoute(ItemContextRoute route,uint itemId)
    {
        // Reuse the same tabs, history, queries and future-tool directory as an ordinary item click.
        focusCraftTab=false;focusUsesTab=false;focusStocksTab=false;focusGatherTab=false;focusBankTab=false;
        switch(route)
        {
            case ItemContextRoute.Usages: OpenContextTool("Item Use");break;
            case ItemContextRoute.Stocks:page=Page.Items;focusStocksTab=true;break;
            case ItemContextRoute.Craft:OpenContextTool("Craftimizer");break;
            case ItemContextRoute.Gathering:OpenContextTool("GatherBuddy");break;
            case ItemContextRoute.Tools:toolQuery=string.Empty;page=Page.Home;break;
            case ItemContextRoute.NpcVendors:page=Page.NpcVendors;break;
            case ItemContextRoute.Market:OpenContextTool("Price Insight");break;
            case ItemContextRoute.MonsterLoot:OpenContextTool("Monster Loot Hunter");break;
            case ItemContextRoute.DutyLoot:
                // First duty that drops it (LuminaSupplemental, then Garland), filtered on the item; else the duty list searched by name.
                var item=SelectedItem;
                var duty=FirstDutyOf(itemId);
                if(duty is not null){dutyLoot.Select(duty);showDutyDetails=true;}
                dutyItemQuery=item?.Name ?? "";page=Page.DutyLoot;break;
            case ItemContextRoute.Collection:
                if(CollectionOf(SelectedItem) is { } entry)
                {
                    if(entry.Kind==Collections.CollectionKind.TripleTriad){page=Page.TripleTriad;selectedTriad=entry.Id;}
                    else{page=Page.Collections;collectionKind=entry.Kind;focusCollectionKind=entry.Kind;if(entry.Kind==Collections.CollectionKind.Orchestrion)focusOrchestrionTab=true;}
                    collectionQuery=entry.Name;
                }
                else page=Page.Items;
                break;
            case ItemContextRoute.Sources:page=Page.Items;break;
            case ItemContextRoute.Quest:
                if(quests.State.RewardIndex?.ForItem(itemId) is { Count:>0 } rewards){page=Page.Quests;SelectQuest(rewards[0].QuestId);}
                else page=Page.Quests;
                break;
        }
    }

    // First duty that drops the item: LuminaSupplemental, then Garland (instances) when already loaded.
    private Duties.DutyDefinition? FirstDutyOf(uint itemId)=>
        LuminaDutiesOf(itemId).Select(id=>duties.State.Catalog?.Find(id)).FirstOrDefault(found=>found is not null)
        ?? (itemSources?.Get(itemId).List?.Sources ?? []).Where(source=>source.Type=="instance")
            .Select(source=>duties.State.Catalog?.Entries.FirstOrDefault(entry=>entry.InstanceId==source.Id)).FirstOrDefault(found=>found is not null);

    /// <summary>
    /// Tools useful for an item (0.208), in display order; a tool whose catalogue is not loaded yet is kept (unknown ≠ none). Used by the
    /// inventory's « Tensho — objet » menu and the context item panel.
    /// </summary>
    internal IReadOnlyList<ItemContextRoute> RelevantRoutes(uint itemId)
    {
        var routes=new List<ItemContextRoute>();
        var item=itemCatalog.State.Catalog?.Find(itemId);
        var recipes=recipeCatalog.State.Catalog;
        var uses=itemUses.State.Catalog;
        if(uses is null || recipes is null || uses.LevesFor(itemId).Count>0 || uses.IsSupplyCandidate(itemId) || uses.IsAquariumFish(itemId) || recipes.ForIngredient(itemId).Count>0
           || (npcVendors?.State.Catalog?.PaidWith(itemId).Count ?? 0)>0 || lumina?.State.Data is { } usage && (usage.GivesOf(itemId).Count>0 || usage.QuestsRequiring(itemId).Count>0))
            routes.Add(ItemContextRoute.Usages);
        if(recipes is null || recipes.ForResult(itemId).Count>0)routes.Add(ItemContextRoute.Craft);
        if(gatheringCatalog.State.Catalog is not { } gathering || gathering.ForItem(itemId).Count>0)routes.Add(ItemContextRoute.Gathering);
        if(npcVendors?.State.Catalog is not { } vendors || vendors.ForItem(itemId).Count>0 || LuminaSellers(itemId,null).Count>0)routes.Add(ItemContextRoute.NpcVendors);
        if(item is null || item.IsMarketable)routes.Add(ItemContextRoute.Market);
        if(FirstDutyOf(itemId) is not null)routes.Add(ItemContextRoute.DutyLoot);
        if(lumina?.State.Data?.MonstersOf(itemId).Count>0 || (itemSources?.Get(itemId).List?.Sources ?? []).Any(source=>source.Type=="mob"))routes.Add(ItemContextRoute.MonsterLoot);
        if(quests.State.RewardIndex?.ForItem(itemId) is { Count:>0 })routes.Add(ItemContextRoute.Quest);
        if(CollectionOf(item) is not null)routes.Add(ItemContextRoute.Collection);
        routes.Add(ItemContextRoute.Stocks);
        routes.Add(ItemContextRoute.Sources);
        routes.Add(ItemContextRoute.Tools);
        return routes;
    }
}
