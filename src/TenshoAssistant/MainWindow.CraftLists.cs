using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

/// <summary>
/// A-05 (0.433): crafting lists on the Crafting page (folded « Crafting lists »): choose or create a list, its items with quantities
/// (a click opens the item's recipe on Preparation), the total of their direct ingredients against the recorded stocks, and the whole
/// list as a Teamcraft link. Preparation › « Add to the list » puts the recipe's item and the quantity to produce in it.
/// </summary>
internal sealed partial class MainWindow
{
    internal CraftListStore? CraftListStore { get; set; }
    private int craftListIndex;
    private string craftListName="";
    private string? craftListMessage;

    private void DrawCraftLists()
    {
        if(CraftListStore is not { } store)return;
        if(store.Unavailable){ImGui.TextWrapped(localization.Get("CraftLists.Unavailable"));return;}
        var lists=store.Lists;
        craftListIndex=Math.Clamp(craftListIndex,0,Math.Max(0,lists.Count-1));
        if(lists.Count>0)
        {
            ImGui.SetNextItemWidth(240);
            if(ImGui.BeginCombo(localization.Get("CraftLists.List")+"###CraftListPick",lists[craftListIndex].Name))
            {
                try{for(var index=0;index<lists.Count;index++)if(ImGui.Selectable($"{lists[index].Name}##CraftList{index}",index==craftListIndex))craftListIndex=index;}
                finally{ImGui.EndCombo();}
            }
            Tip("CraftLists.ListTip");
            ImGui.SameLine();
            if(DeleteButton("CraftLists.Delete")){store.Set(craftListIndex,null);craftListIndex=0;return;}
            Tip("CraftLists.DeleteTip");
        }
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("###CraftListName",localization.Get("CraftLists.NewName"),ref craftListName,CraftLists.MaxName);
        Tip("CraftLists.NewNameTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("CraftLists.Create")))
        {
            if(CraftLists.CleanName(craftListName) is { } name && store.Set(lists.Count,new(name,[]))){craftListIndex=lists.Count;craftListName="";craftListMessage=null;}
            else craftListMessage=localization.Get("CraftLists.CreateFailed");
        }
        if(craftListMessage is { } message)ImGui.TextDisabled(message);
        lists=store.Lists;
        if(lists.Count==0){ImGui.TextDisabled(localization.Get("CraftLists.None"));return;}
        var list=lists[Math.Clamp(craftListIndex,0,lists.Count-1)];
        // 0.692 (user report): the list on screen; the panel follows the list chosen here.
        BoolSetting("CraftLists.Panel","CraftLists.PanelTip",()=>configuration.CraftListPanel,value=>configuration.CraftListPanel=value,c=>c.CraftListPanel);
        if(configuration.CraftListPanel)DrawListPanelOptions();
        if(configuration.CraftListPanelName!=list.Name){configuration.CraftListPanelName=list.Name;SaveReminders();}
        if(list.Entries.Count==0){ImGui.TextDisabled(localization.Get("CraftLists.Empty"));return;}
        var items=itemCatalog.State.Catalog;var recipes=recipeCatalog.State.Catalog;
        foreach(var entry in list.Entries)
        {
            ImGui.PushID((int)entry.ItemId);
            try
            {
                var item=items?.Find(entry.ItemId);
                DrawItemIcon(item,new Vector2(22));
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(entry.ItemId);
                ImGui.SameLine();
                if(ImGui.SmallButton(item?.Name ?? $"ID {entry.ItemId}") && recipes?.ForResult(entry.ItemId) is { Count:>0 } own)OpenCraftPreparation(PreferredRecipe(own));
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(entry.ItemId);
                ImGui.SameLine();
                var quantity=(int)Math.Min(entry.Quantity,int.MaxValue);
                ImGui.SetNextItemWidth(110);
                if(ImGui.InputInt("###CraftListQuantity",ref quantity,0))store.Set(craftListIndex,CraftLists.SetQuantity(list,entry.ItemId,quantity));
                Tip("CraftLists.QuantityTip");
                ImGui.SameLine();
                if(DeleteButton("CraftLists.Remove")){store.Set(craftListIndex,CraftLists.Remove(list,entry.ItemId));return;}
            }
            finally{ImGui.PopID();}
        }
        // The direct ingredients of every item, against the recorded stocks.
        RefreshRecipeHoldingsCache();
        var plans=CraftListPlans(list);
        UI.UiComponents.SubTitle(localization.Get("CraftLists.Ingredients"));
        // 0.464 (user request, in-game campaign): a table with icons and the full item tooltip.
        if(UI.UiComponents.BeginDataTable("CraftListIngredients",4))
        {
            try
            {
                foreach(var column in new[]{"ColumnIngredient","ColumnRequired","ColumnHeld","ColumnMissing"})ImGui.TableSetupColumn(localization.Get("CraftLists."+column));
                ImGui.TableHeadersRow();
                foreach(var (itemId,required) in CraftLists.Ingredients(plans))
                {
                    var held=GetRecipeHoldings(itemId);
                    var missing=held.HasObservation ? Math.Max(0,required-held.Total):(long?)null;
                    var ingredient=items?.Find(itemId);
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    DrawItemIcon(ingredient,new Vector2(24));
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(itemId);
                    ImGui.SameLine();ImGui.TextUnformatted(ingredient?.Name ?? $"ID {itemId}");
                    if(ImGui.IsItemHovered())DrawItemInfoTooltip(itemId);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(required.ToString("N0",Numbers));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(held.HasObservation ? held.Total.ToString("N0",Numbers):"?");
                    ImGui.TableNextColumn();
                    if(missing is { } short_)ImGui.TextColored(short_>0 ? UI.FfxivTheme.Error:UI.FfxivTheme.Success,short_.ToString("N0",Numbers));
                    else ImGui.TextDisabled("?");
                }
            }
            finally{ImGui.EndTable();}
        }
        if(TeamcraftExport.Link(list.Entries.Select(entry=>(entry.ItemId,recipes?.ForResult(entry.ItemId) is { Count:>0 } own ? (uint?)PreferredRecipe(own).RecipeId:null,entry.Quantity))) is { } teamcraft)
        {
            CopyButton(localization.Get("PlanUi.TeamcraftCopy")+"###CraftListTeamcraft",()=>teamcraft);
            Tip("PlanUi.TeamcraftTip");
        }
    }

    private CraftPlan[] CraftListPlans(CraftList list)
    {
        var recipes=recipeCatalog.State.Catalog;
        return list.Entries.Select(entry=>recipes?.ForResult(entry.ItemId) is { Count:>0 } own ? CraftPlan.Build(PreferredRecipe(own),(int)Math.Min(entry.Quantity,int.MaxValue),GetRecipeHoldings):null)
            .OfType<CraftPlan>().ToArray();
    }

    /// <summary>
    /// 0.826 (X-58, user request: Tensho's right-click menu adds a recipe to a list already made): one entry per crafting list, for an item that has a recipe; one more
    /// to make, or the item added with one. Used by Tensho's own item menu and by the game's (its « Tensho » sub-menu).
    /// </summary>
    internal IReadOnlyList<(string Label,Action Open)> CraftListAdds(uint itemId)
    {
        if(CraftListStore is not { Unavailable:false } store || store.Lists is not { Count:>0 } lists || recipeCatalog.State.Catalog?.ForResult(itemId) is not { Count:>0 })return [];
        return lists.Select((list,index)=>(localization.Format("ItemMenu.AddToList",list.Name),(Action)(()=>
        {
            var current=store.Lists is { } now && index<now.Count ? now[index]:null;
            var done=current is not null && CraftLists.Add(current,itemId,1) is { } added && store.Set(index,added);
            if(!done)log.Warning("The item {0} could not be added to the crafting list {1}.",itemId,list.Name);
        }))).ToArray();
    }

    // Preparation › « Add to the list »: the recipe's item and the quantity to produce.
    private void DrawAddToCraftList(RecipeDefinition recipe,long quantity)
    {
        if(CraftListStore is not { Unavailable:false } store || store.Lists is not { Count:>0 } lists)return;
        var list=lists[Math.Clamp(craftListIndex,0,lists.Count-1)];
        if(ImGui.Button(localization.Format("CraftLists.Add",list.Name)))
            craftListMessage=CraftLists.Add(list,recipe.ResultItemId,quantity) is { } added && store.Set(Math.Clamp(craftListIndex,0,lists.Count-1),added)
                ? localization.Format("CraftLists.Added",list.Name):localization.Get("CraftLists.AddFailed");
        Tip("CraftLists.AddTip");
    }
}
