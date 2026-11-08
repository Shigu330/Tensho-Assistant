using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Item Use (item sheet › Usages): orchestrion roll state, recipes that use the item, Grand Company supply and aquarium candidates, craft
// leves and their requests (game sheets through ItemUseCatalog: CraftLeve, GCSupplyDuty, AquariumFish), then references from other tools
// (0.209): the item as a vendor currency, what it gives (LuminaSupplemental ItemSupplement: desynthesis, coffers…) and the quests that ask
// for it. Static catalogue evidence only: no daily demand, accepted leve or unlock is inferred.
internal sealed partial class MainWindow
{
    // 0.469 (user request, in-game campaign): what the game's supply missions window showed today, or how to see it (Items/GcSupplyToday.cs).
    internal TenshoAssistant.Items.GcSupplyToday GcSupply{get;}=new();
    private string GcSupplyLine(uint item)=>localization.Get(GcSupply.Status(bankBridge.State.CharacterId,TenshoAssistant.Reminders.ResetSchedule.PreviousDaily(DateTimeOffset.UtcNow,TenshoAssistant.Reminders.ResetSchedule.SupplyHour),item) switch
    {
        TenshoAssistant.Items.GcSupplyStatus.Requested=>"GcSupply.Requested",
        TenshoAssistant.Items.GcSupplyStatus.NotListed=>"GcSupply.NotListed",
        _=>"GcSupply.NotObserved",
    });
    private bool focusUsesTab;
    private void DrawItemUses()
    {
        Heading(localization.Get("ItemUsesUi.Title"),localization.Get("ItemUsesUi.Description"));
        if(itemId<=0){ImGui.TextWrapped(localization.Get("ItemUsesUi.Select"));return;}
        DrawRecipeResultHeader();var id=(uint)itemId;
        if(SelectedItem is {OrchestrionId:>0} roll)
        {
            var music=CollectionEntryOf(TenshoAssistant.Collections.CollectionKind.Orchestrion,roll.OrchestrionId);
            ImGui.TextColored(Accent,localization.Get("ItemUsesUi.Roll"));
            ImGui.TextWrapped(music is {ConfirmedUnlocked:true} ? localization.Get("ItemUsesUi.Acquired"):localization.Get("ItemUsesUi.UnknownMusic"));
            ImGui.BeginDisabled(music is null);
            try{if(ImGui.SmallButton(localization.Get("ItemUsesUi.MusicDetails")) && music is not null)OpenOrchestrionFromItem(music);}
            finally{ImGui.EndDisabled();}
            if(music is null)ImGui.TextDisabled(localization.Get("ItemUsesUi.MissingMusic"));
        }
        if(recipeCatalog.State.Catalog is { } recipes)
        {
            var count=recipes.ForIngredient(id).Count;
            if(ImGui.SmallButton(localization.Format("ItemUseCounts.Recipes",count)))focusCraftTab=true;
        }
        else ImGui.TextDisabled(localization.Get("ItemUsesUi.LoadingRecipes"));
        DrawUseReferences(id);
        DrawQuestItemSources(id);
        var state=itemUses.State;
        if(state.Catalog is not { } catalog)
        {
            ImGui.TextWrapped(state.Error is null ? localization.Format("ItemUseCounts.Loading",state.Copied,state.Total):localization.Get("ItemUsesUi.Unavailable"));return;
        }
        ImGui.Separator();
        if(catalog.IsSupplyCandidate(id))
        {ImGui.TextColored(Accent,localization.Get("ItemUsesUi.Gc"));ImGui.TextWrapped(GcSupplyLine(id));}
        if(catalog.IsAquariumFish(id))
        {ImGui.TextColored(Accent,localization.Get("ItemUsesUi.Aquarium"));ImGui.TextWrapped(localization.Get("ItemUsesUi.AquariumHelp"));}
        if(!catalog.IsSupplyCandidate(id) && !catalog.IsAquariumFish(id))ImGui.TextDisabled(localization.Get("ItemUsesUi.NoOther"));
        var leves=catalog.LevesFor(id);ImGui.Spacing();ImGui.TextColored(Accent,localization.Format("ItemUseCounts.Leves",leves.Count));
        ImGui.TextWrapped(localization.Get("ItemUsesUi.LeveHelp"));
        if(leves.Count==0)ImGui.TextDisabled(localization.Get("ItemUsesUi.NoLeve"));
        foreach(var leve in leves)
        {
            ImGui.PushID((int)leve.RowId);
            try
            {
                DrawGameIcon(leve.IconId,new Vector2(24));ImGui.SameLine();
                var job=string.IsNullOrWhiteSpace(leve.Job) ? localization.Get("ItemUsesUi.UnknownJob"):leve.Job;
                var level=leve.Level>0 ? localization.Format("QuestDetails.Level",leve.Level):localization.Get("ItemUsesUi.UnknownLevel");
                if(!TenshoAssistant.UI.UiComponents.Header($"{leve.Name} · {job} · {level}"))continue;
                if(!string.IsNullOrWhiteSpace(leve.Place))ImGui.TextWrapped(localization.Format("ItemUseCounts.Place",leve.Place));
                ImGui.TextDisabled(localization.Get("ItemUsesUi.Requests"));
                for(var index=0;index<leve.Requests.Count;index++)
                {
                    var request=leve.Requests[index];var item=itemCatalog.State.Catalog?.Find(request.ItemId);ImGui.PushID(index);
                    try
                    {
                        var quantityText=request.Quantity is { } quantity ? $"× {quantity}":localization.Get("ItemUsesUi.Quantity");
                        if(ItemCell(item,request.ItemId,28,$"{item?.Name ?? localization.Format("BankUi.Item",request.ItemId)}  {quantityText}"))focusUsesTab=true;
                    }
                    finally{ImGui.PopID();}
                }
            }
            finally{ImGui.PopID();}
        }
        if(catalog.UnresolvedLeveRows>0)ImGui.TextWrapped(localization.Format("ItemUseCounts.Partial",catalog.UnresolvedLeveRows));
        ImGui.TextDisabled(localization.Format("ItemUseCounts.Source",catalog.Language));
    }

    // References found by the other tools (0.209): the item as a currency (game shops → vendor tool), what it gives (LuminaSupplemental
    // ItemSupplement: desynthesis, coffer, card pack…), the quests that ask for it (LuminaSupplemental QuestRequiredItem → quest tool).
    private void DrawUseReferences(uint id)
    {
        var items=itemCatalog.State.Catalog;
        var paid=npcVendors?.State.Catalog?.PaidWith(id) ?? [];
        if(paid.Count>0 && TenshoAssistant.UI.UiComponents.Header(localization.Format("ItemUseRefs.Currency",paid.Select(offer=>offer.Sale.ItemId).Distinct().Count())+"###UseCurrency"))
        {
            ImGui.TextDisabled(localization.Get("ItemUseRefs.CurrencyHelp"));
            foreach(var vendor in paid.GroupBy(offer=>offer.Vendor.NpcId).OrderByDescending(group=>group.Count()).Take(8))
            {
                var first=vendor.First().Vendor;
                ImGui.PushID((int)first.NpcId);
                try
                {
                    DrawVendorLink(id,first,localization.Format("SourceLinks.Vendor",NpcName(first),VendorPlace(first))+$" ({vendor.Select(offer=>offer.Sale.ItemId).Distinct().Count()})");
                    ImGui.Indent();
                    foreach(var offer in vendor.DistinctBy(offer=>offer.Sale.ItemId).Take(12))
                    {
                        var bought=items?.Find(offer.Sale.ItemId);
                        var cost=offer.Sale.Costs.FirstOrDefault(entry=>entry.CurrencyItemId==id);
                        ImGui.PushID((int)offer.Sale.ShopId);
                        try{if(ItemCell(bought,offer.Sale.ItemId,20,$"{bought?.Name ?? offer.Sale.ItemId.ToString()} · {cost?.Amount.ToString("N0") ?? "?"}"))focusUsesTab=false;}
                        finally{ImGui.PopID();}
                    }
                    ImGui.Unindent();
                }
                finally{ImGui.PopID();}
            }
        }
        // X-75.2 (0.875): what this item was seen in game giving (yours or shared: desynthesis, opened), before LuminaSupplemental's lists.
        if(Seen()?.GivesBySource.GetValueOrDefault(id) is { Length:>0 } seenGives
           && TenshoAssistant.UI.UiComponents.Header(localization.Format("ItemUseRefs.SeenGives",seenGives.Length)+"###UseSeenGives"))
        {
            ImGui.Indent();
            foreach(var result in seenGives)
            {
                var got=items?.Find(result);
                ImGui.PushID((int)result);
                try{if(ItemCell(got,result,20,got is null ? result.ToString():null))focusUsesTab=true;}
                finally{ImGui.PopID();}
            }
            ImGui.Unindent();
        }
        if(lumina?.State.Data is { } data)
        {
            var given=data.GivesOf(id);
            if(given.Count>0 && TenshoAssistant.UI.UiComponents.Header(localization.Format("ItemUseRefs.Gives",given.Sum(kind=>kind.Count))+"###UseGives"))
                foreach(var (kind,count,list) in given)
                {
                    ImGui.TextColored(Accent,localization.Get("ItemUseRefs.Kind"+(kind is >= 1 and <= 19 ? kind:0))+(count>list.Count ? " "+localization.Format("HoverUi.SupplementMore",count-list.Count):""));
                    ImGui.Indent();
                    foreach(var result in list)
                    {
                        var got=items?.Find(result);
                        ImGui.PushID(kind.ToString());
                        try{if(ItemCell(got,result,20,got is null ? result.ToString():null))focusUsesTab=true;}
                        finally{ImGui.PopID();}
                    }
                    ImGui.Unindent();
                }
            // X-73.18 (0.889): the quests seen in game asking for it (yours or shared) come first. 0.911: an HQ demand (the item's HQ id) says « HQ ».
            if(DiscoveryStore is { } seenStore && DiscoveriesLoaded
               && seenStore.Find(Discoveries.DiscoveryKind.QuestItemAsked,subject=>Discoveries.QuestItemAsks.IsOf(subject,id),16) is { Count:>0 } seenAsks
               && TenshoAssistant.UI.UiComponents.Header(localization.Format("ItemUseRefs.SeenQuests",seenAsks.Count)+"###UseSeenQuests"))
                foreach(var fact in seenAsks)
                    if(ImGui.Selectable(localization.Format("ItemUseRefs.Quest",DiscoveryQuestName(fact.Detail),LevelRange(fact.LevelMin,fact.LevelMax))
                        +(Discoveries.QuestItemAsks.Hq(fact.Subject) ? " HQ":"")+$"##SeenAsk{fact.Detail}:{fact.Subject}"))
                    {page=Page.Quests;SelectQuest(fact.Detail);}
            var asked=data.QuestsRequiring(id);
            if(asked.Count>0 && TenshoAssistant.UI.UiComponents.Header(localization.Format("ItemUseRefs.Quests",asked.Count)+"###UseQuests"))
                foreach(var (questId,name,quantity,hq) in asked)
                    if(ImGui.Selectable(localization.Format("ItemUseRefs.Quest",name.Length>0 ? name:questId.ToString(),quantity)+(hq ? " HQ":"")+$"##AskQuest{questId}"))
                    {page=Page.Quests;SelectQuest(questId);}
        }
        else if(configuration.CommunityLumina)lumina?.Request();
    }
}
