using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// W-05 (0.691, user request): just the list, on screen: the materials of the workshop project and of the crafting preparation, each with its setting on its page. Out of combat, as the tasks panel.
internal sealed partial class MainWindow
{
    private const ImGuiWindowFlags ListPanelFlags=ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav;
    private WorkshopProject? workshopPanelShown;
    private RecipeDefinition? preparationPanelShown;

    private PanelDefinition WorkshopListPanel=>new("WorkshopPanel","###TenshoWorkshopList",new Vector2(40,420),ListPanelFlags,
        ()=>configuration.WorkshopPanel,WorkshopPanelVisible,DrawWorkshopPanelContents,()=>{if(configuration.WorkshopPanel){configuration.WorkshopPanel=false;SaveReminders();}},()=>localization.Get("FreeCompany.WorkshopPanelTitle"),0.75f);
    private PanelDefinition PreparationListPanel=>new("PreparationPanel","###TenshoPreparationList",new Vector2(340,420),ListPanelFlags,
        ()=>configuration.PreparationPanel,PreparationPanelVisible,DrawPreparationPanelContents,()=>{if(configuration.PreparationPanel){configuration.PreparationPanel=false;SaveReminders();}},()=>localization.Get("PlanUi.PanelTitle"),0.75f);

    private bool WorkshopPanelVisible()
    {
        if(windowDisposed || !configuration.WorkshopPanel || mapLive?.State is { InCombat:true } || itemCatalog.State.Catalog is null || WorkshopProjectsLoad is not { } load)return false;
        workshopProjects??=Task.Run(load);
        workshopPanelShown=workshopProjects.IsCompletedSuccessfully ? workshopProjects.Result.FirstOrDefault(project=>project.Id==configuration.WorkshopPanelProject):null;
        return workshopPanelShown is not null;
    }

    private void DrawWorkshopPanelContents()
    {
        if(workshopPanelShown is not { } project)return;
        RefreshRecipeHoldingsCache();
        ImGui.TextColored(Accent,WorkshopName(project));
        foreach(var (item,total) in project.Totals())DrawListPanelLine(item,total,GetRecipeHoldings(item));
    }

    private bool PreparationPanelVisible()
    {
        if(windowDisposed || !configuration.PreparationPanel || mapLive?.State is { InCombat:true } || configuration.PreparationPanelTarget<=0 || itemCatalog.State.Catalog is null)return false;
        preparationPanelShown=recipeCatalog.State.Catalog?.ForResult(configuration.PreparationPanelItem).FirstOrDefault(recipe=>recipe.RecipeId==configuration.PreparationPanelRecipe);
        return preparationPanelShown is not null;
    }

    private void DrawPreparationPanelContents()
    {
        if(preparationPanelShown is not { } recipe)return;
        RefreshRecipeHoldingsCache();
        var name=itemCatalog.State.Catalog?.Find(recipe.ResultItemId)?.Name ?? recipe.ResultItemId.ToString();
        ImGui.TextColored(Accent,$"{name} ×{configuration.PreparationPanelTarget:N0}");
        foreach(var material in CraftPlan.Build(recipe,configuration.PreparationPanelTarget,GetRecipeHoldings).Materials)DrawListPanelLine(material.ItemId,material.Required,material.Holdings);
    }

    // Stock over what is needed; a dash when the stocks have not been read: unknown is not zero.
    private void DrawListPanelLine(uint itemId,long required,Inventory.ItemHoldingsSummary stock)
    {
        var item=itemCatalog.State.Catalog?.Find(itemId);
        DrawItemIcon(item,new Vector2(ImGui.GetTextLineHeight()));
        if(ImGui.IsItemHovered())DrawItemInfoTooltip(itemId);
        ImGui.SameLine();
        ImGui.TextUnformatted(item?.Name ?? itemId.ToString());
        ImGui.SameLine();
        if(!stock.HasObservation)ImGui.TextDisabled($"— / {required:N0}");
        else if(stock.Total<required)ImGui.TextColored(UI.FfxivTheme.Warning,$"{stock.Total:N0} / {required:N0}");
        else ImGui.TextColored(Success,$"{stock.Total:N0} / {required:N0}");
    }
}
