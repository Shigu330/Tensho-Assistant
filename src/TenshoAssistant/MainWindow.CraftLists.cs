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
        if(CraftListStore is not { } store || !TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftLists.Title")+"###CraftLists"))return;
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
            if(ImGui.SmallButton(localization.Get("CraftLists.Delete")) && ImGui.GetIO().KeyCtrl){store.Set(craftListIndex,null);craftListIndex=0;return;}
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("CraftLists.DeleteTip"));
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
        if(list.Entries.Count==0){ImGui.TextDisabled(localization.Get("CraftLists.Empty"));return;}
        var items=itemCatalog.State.Catalog;var recipes=recipeCatalog.State.Catalog;
        foreach(var entry in list.Entries)
        {
            ImGui.PushID((int)entry.ItemId);
            try
            {
                var item=items?.Find(entry.ItemId);
                DrawItemIcon(item,new Vector2(22));ImGui.SameLine();
                if(ImGui.SmallButton(item?.Name ?? $"ID {entry.ItemId}") && recipes?.ForResult(entry.ItemId) is { Count:>0 } own)OpenCraftPreparation(PreferredRecipe(own));
                ImGui.SameLine();
                var quantity=(int)Math.Min(entry.Quantity,int.MaxValue);
                ImGui.SetNextItemWidth(110);
                if(ImGui.InputInt("###CraftListQuantity",ref quantity,0))store.Set(craftListIndex,CraftLists.SetQuantity(list,entry.ItemId,quantity));
                Tip("CraftLists.QuantityTip");
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("CraftLists.Remove"))){store.Set(craftListIndex,CraftLists.Remove(list,entry.ItemId));return;}
            }
            finally{ImGui.PopID();}
        }
        // The direct ingredients of every item, against the recorded stocks.
        RefreshRecipeHoldingsCache();
        var plans=list.Entries.Select(entry=>recipes?.ForResult(entry.ItemId) is { Count:>0 } own ? CraftPlan.Build(PreferredRecipe(own),(int)Math.Min(entry.Quantity,int.MaxValue),GetRecipeHoldings):null)
            .OfType<CraftPlan>().ToArray();
        ImGui.TextColored(Accent,localization.Get("CraftLists.Ingredients"));
        foreach(var (itemId,required) in CraftLists.Ingredients(plans))
        {
            var held=GetRecipeHoldings(itemId);
            var missing=held.HasObservation ? Math.Max(0,required-held.Total):(long?)null;
            ImGui.TextDisabled(localization.Format("CraftLists.IngredientRow",items?.Find(itemId)?.Name ?? $"ID {itemId}",required.ToString("N0"),
                held.HasObservation ? held.Total.ToString("N0"):"?",missing?.ToString("N0") ?? "?"));
        }
        if(TeamcraftExport.Link(list.Entries.Select(entry=>(entry.ItemId,recipes?.ForResult(entry.ItemId) is { Count:>0 } own ? (uint?)PreferredRecipe(own).RecipeId:null,entry.Quantity))) is { } teamcraft)
        {
            if(ImGui.Button(localization.Get("PlanUi.TeamcraftCopy")+"###CraftListTeamcraft"))ImGui.SetClipboardText(teamcraft);
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("PlanUi.TeamcraftTip"));
        }
    }

    // Preparation › « Add to the list »: the recipe's item and the quantity to produce.
    private void DrawAddToCraftList(RecipeDefinition recipe,long quantity)
    {
        if(CraftListStore is not { Unavailable:false } store || store.Lists is not { Count:>0 } lists)return;
        var list=lists[Math.Clamp(craftListIndex,0,lists.Count-1)];
        if(ImGui.Button(localization.Format("CraftLists.Add",list.Name)))
            craftListMessage=CraftLists.Add(list,recipe.ResultItemId,quantity) is { } added && store.Set(Math.Clamp(craftListIndex,0,lists.Count-1),added)
                ? localization.Format("CraftLists.Added",list.Name):localization.Get("CraftLists.AddFailed");
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("CraftLists.AddTip"));
    }
}
