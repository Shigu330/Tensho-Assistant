using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// W-05 (0.691, user request): just the list, on screen: the materials of the workshop project and of the crafting preparation, each with its setting on its page. 0.693 (user request): shown means shown, unless the player chooses otherwise.
internal sealed partial class MainWindow
{
    private const ImGuiWindowFlags ListPanelFlags=ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav;
    private bool ListPanelsHiddenInCombat=>configuration.ListPanelsHideInCombat && mapLive?.State is { InCombat:true };
    // The same two choices for every list panel, under the box of the one that is on.
    private void DrawListPanelOptions()
    {
        BoolSetting("ListPanels.HideInCombat","ListPanels.HideInCombatTip",()=>configuration.ListPanelsHideInCombat,value=>configuration.ListPanelsHideInCombat=value,c=>c.ListPanelsHideInCombat);
        BoolSetting("ListPanels.HideBehindGame","ListPanels.HideBehindGameTip",()=>configuration.ListPanelsHideBehindGame,value=>configuration.ListPanelsHideBehindGame=value,c=>c.ListPanelsHideBehindGame);
        BoolSetting("ListPanels.Base","ListPanels.BaseTip",()=>configuration.ListPanelsBase,value=>configuration.ListPanelsBase=value,c=>c.ListPanelsBase);
        if(!configuration.ListPanelsBase)return;
        ImGui.Indent();
        BoolSetting("ListPanels.Gathering","ListPanels.OriginTip",()=>configuration.ListPanelsGathering,value=>configuration.ListPanelsGathering=value,c=>c.ListPanelsGathering);
        BoolSetting("ListPanels.Vendor","ListPanels.OriginTip",()=>configuration.ListPanelsVendor,value=>configuration.ListPanelsVendor=value,c=>c.ListPanelsVendor);
        BoolSetting("ListPanels.Monster","ListPanels.OriginTip",()=>configuration.ListPanelsMonster,value=>configuration.ListPanelsMonster=value,c=>c.ListPanelsMonster);
        BoolSetting("ListPanels.Other","ListPanels.OriginTip",()=>configuration.ListPanelsOther,value=>configuration.ListPanelsOther=value,c=>c.ListPanelsOther);
        ImGui.Unindent();
    }
    private IReadOnlyList<WorkshopProject> workshopPanelShown=[];
    private RecipeDefinition? preparationPanelShown;

    private PanelDefinition WorkshopListPanel=>new("WorkshopPanel","###TenshoWorkshopList",new Vector2(40,420),ListPanelFlags,
        ()=>configuration.WorkshopPanel,WorkshopPanelVisible,DrawWorkshopPanelContents,()=>{if(configuration.WorkshopPanel){configuration.WorkshopPanel=false;SaveReminders();}},()=>localization.Get("FreeCompany.WorkshopPanelTitle"),0.75f,()=>configuration.ListPanelsHideBehindGame);
    private PanelDefinition PreparationListPanel=>new("PreparationPanel","###TenshoPreparationList",new Vector2(340,420),ListPanelFlags,
        ()=>configuration.PreparationPanel,PreparationPanelVisible,DrawPreparationPanelContents,()=>{if(configuration.PreparationPanel){configuration.PreparationPanel=false;SaveReminders();}},()=>localization.Get("PlanUi.PanelTitle"),0.75f,()=>configuration.ListPanelsHideBehindGame);

    private CraftList? craftListPanelShown;
    private PanelDefinition CraftListListPanel=>new("CraftListPanel","###TenshoCraftListList",new Vector2(640,420),ListPanelFlags,
        ()=>configuration.CraftListPanel,CraftListPanelVisible,DrawCraftListPanelContents,()=>{if(configuration.CraftListPanel){configuration.CraftListPanel=false;SaveReminders();}},()=>localization.Get("CraftLists.PanelTitle"),0.75f,()=>configuration.ListPanelsHideBehindGame);

    private bool CraftListPanelVisible()
    {
        if(windowDisposed || !configuration.CraftListPanel || ListPanelsHiddenInCombat || itemCatalog.State.Catalog is null || CraftListStore is not { Unavailable:false } store)return false;
        craftListPanelShown=store.Lists.FirstOrDefault(list=>list.Name==configuration.CraftListPanelName);
        return craftListPanelShown is { Entries.Count:>0 };
    }

    private void DrawCraftListPanelContents()
    {
        if(craftListPanelShown is not { } list)return;
        RefreshRecipeHoldingsCache();
        ImGui.TextColored(Accent,list.Name);
        DrawListPanelEntries("CraftList",()=>CraftLists.Ingredients(CraftListPlans(list)).Select(row=>(row.Item1,(long)row.Item2)).ToArray());
    }

    private bool WorkshopPanelVisible()
    {
        if(windowDisposed || !configuration.WorkshopPanel || ListPanelsHiddenInCombat || itemCatalog.State.Catalog is null || WorkshopProjectsLoad is not { } load)return false;
        workshopProjects??=Task.Run(load);
        if(!workshopProjects.IsCompletedSuccessfully){workshopPanelShown=[];return false;}
        // The followed projects together, or else the one chosen on the page.
        var projects=workshopProjects.Result;
        var followed=(configuration.WorkshopFollowed ?? []).Select(id=>projects.FirstOrDefault(project=>project.Id==id)).OfType<WorkshopProject>().ToArray();
        workshopPanelShown=followed.Length>0 ? followed:projects.Where(project=>project.Id==configuration.WorkshopPanelProject).ToArray();
        return workshopPanelShown.Count>0;
    }

    private void DrawWorkshopPanelContents()
    {
        RefreshRecipeHoldingsCache();
        foreach(var project in workshopPanelShown)ImGui.TextColored(Accent,WorkshopName(project));
        DrawListPanelEntries("Workshop",()=>WorkshopStock.Totals(workshopPanelShown).Select(row=>(row.Item,(long)row.Total)).ToArray());
    }

    private bool PreparationPanelVisible()
    {
        if(windowDisposed || !configuration.PreparationPanel || ListPanelsHiddenInCombat || configuration.PreparationPanelTarget<=0 || itemCatalog.State.Catalog is null)return false;
        preparationPanelShown=recipeCatalog.State.Catalog?.ForResult(configuration.PreparationPanelItem).FirstOrDefault(recipe=>recipe.RecipeId==configuration.PreparationPanelRecipe);
        return preparationPanelShown is not null;
    }

    private void DrawPreparationPanelContents()
    {
        if(preparationPanelShown is not { } recipe)return;
        RefreshRecipeHoldingsCache();
        var name=itemCatalog.State.Catalog?.Find(recipe.ResultItemId)?.Name ?? recipe.ResultItemId.ToString();
        ImGui.TextColored(Accent,$"{name} ×{configuration.PreparationPanelTarget:N0}");
        DrawListPanelEntries("Preparation",()=>CraftPlan.Build(recipe,configuration.PreparationPanelTarget,GetRecipeHoldings).Materials.Select(material=>(material.ItemId,material.Required)).ToArray());
    }

    // W-09 (0.699): worked out at most once a second per panel, not on every frame.
    private readonly Dictionary<string,(long At,(uint Item,long Needed)[] Lines)> listPanelLines=[];
    private void DrawListPanelEntries(string key,Func<(uint Item,long Needed)[]> entries)
    {
        var now=Environment.TickCount64;
        if(!listPanelLines.TryGetValue(key,out var known) || now-known.At>1000)
        {
            var wanted=entries();
            listPanelLines[key]=known=(now,configuration.ListPanelsBase ? BaseMaterials.Expand(wanted,ListPanelStock,ListPanelRecipe,ListPanelFetched).ToArray():wanted);
        }
        if(!configuration.ListPanelsBase)
        {
            foreach(var (item,needed) in known.Lines)DrawListPanelLine(item,needed,GetRecipeHoldings(item));
            return;
        }
        foreach(var origin in Enum.GetValues<ItemOrigin>())
        {
            if(!ListPanelOriginShown(origin))continue;
            var lines=known.Lines.Where(line=>ListPanelOrigin(line.Item)==origin).ToArray();
            if(lines.Length==0)continue;
            ImGui.TextDisabled(localization.Get("ListPanels."+origin));
            foreach(var (item,needed) in lines)DrawListPanelLine(item,needed,GetRecipeHoldings(item));
        }
    }
    private long ListPanelStock(uint item){var held=GetRecipeHoldings(item);return held.HasObservation ? held.Total:0;}
    private RecipeDefinition? ListPanelRecipe(uint item)=>recipeCatalog.State.Catalog?.ForResult(item) is { Count:>0 } recipes ? PreferredRecipe(recipes):null;
    private bool ListPanelGathered(uint item)=>gatheringCatalog.State.Catalog?.ForItem(item) is { Count:>0 };
    private bool ListPanelSold(uint item)=>npcVendors?.State.Catalog?.ForItem(item) is { Count:>0 };
    private bool ListPanelFetched(uint item)=>ListPanelGathered(item) || ListPanelSold(item);
    private ItemOrigin ListPanelOrigin(uint item)=>BaseMaterials.OriginOf(ListPanelGathered(item),ListPanelSold(item),lumina?.State.Data?.MonstersOf(item) is { Count:>0 });
    private bool ListPanelOriginShown(ItemOrigin origin)=>origin switch
    {
        ItemOrigin.Gathering=>configuration.ListPanelsGathering,
        ItemOrigin.Vendor=>configuration.ListPanelsVendor,
        ItemOrigin.Monster=>configuration.ListPanelsMonster,
        _=>configuration.ListPanelsOther,
    };

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
