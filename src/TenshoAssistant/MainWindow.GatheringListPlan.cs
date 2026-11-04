using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// 0.478 (user request, in-game campaign): the GP plan knows what the crafting list still needs, how many nodes that takes and the GP that comes back between them; actions can be forbidden, as for macros.
internal sealed partial class MainWindow
{
    private CraftList? gatherNeedsList;
    private RecipeCatalog? gatherNeedsCatalog;
    private volatile IReadOnlyDictionary<uint,long>? gatherNeeds;
    private GpRoute? gatheringPanelRoute;
    private string? gatheringPanelRouteKey;
    private bool gatheringPanelBonus;
    private readonly GatherNodeNorms gatherNodeNorms=new();

    private string GpForbidden=>GpActionRules.Joined(configuration.GpForbiddenActions);
    private int GpRegenPerTick=>configuration.GpRegenPerTick>0 ? configuration.GpRegenPerTick:configuration.GpRegenObserved;

    /// <summary>What the selected crafting list still needs of an item: all its raw materials, computed off the draw when the list changes, minus the stock known now.</summary>
    private long GatherListNeed(uint item)
    {
        if(!configuration.GatherPlanFromList || CraftListStore is not { Unavailable:false } store || store.Lists is not { Count:>0 } lists || recipeCatalog.State.Catalog is not { } recipes)return 0;
        var list=lists[Math.Clamp(craftListIndex,0,lists.Count-1)];
        if(!ReferenceEquals(gatherNeedsList,list) || !ReferenceEquals(gatherNeedsCatalog,recipes))
        {
            gatherNeedsList=list;gatherNeedsCatalog=recipes;gatherNeeds=null;
            var chosen=new Dictionary<uint,RecipeDefinition>();
            foreach(var entry in list.Entries)if(recipes.ForResult(entry.ItemId) is { Count:>0 } own)chosen[entry.ItemId]=PreferredRecipe(own);
            _=Task.Run(()=>
            {
                try
                {
                    var needs=CraftLists.RawNeeds(list,chosen,recipes);
                    if(ReferenceEquals(gatherNeedsList,list))gatherNeeds=needs;
                }
                catch(Exception error){log.Warning(error,"The crafting list's raw materials could not be computed.");}
            });
        }
        if(gatherNeeds?.GetValueOrDefault(item) is not { } gross || gross<=0)return 0;
        RefreshRecipeHoldingsCache();
        var held=GetRecipeHoldings(item);
        return Math.Max(0,gross-(held.HasObservation ? held.Total:0));
    }

    private void DrawGatheringListRoute(GpRoute route,long need,int seconds)
    {
        ImGui.PushTextWrapPos(0);
        try
        {
            if(route.Reached)ImGui.TextColored(Accent,localization.Format("GatheringPanel.ListNeed",need.ToString("N0"),route.Nodes,Math.Max(1,route.Nodes*seconds/60),route.GpSpent.ToString("N0")));
            else ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("GatheringPanel.ListNeedTooMany",need.ToString("N0"),GpRoutes.MaxNodes));
            if(GpRegenPerTick<=0)ImGui.TextDisabled(localization.Get("GatheringPanel.ListNoRegen"));
        }
        finally{ImGui.PopTextWrapPos();}
    }

    /// <summary>Settings › Modules › Gathering, and the GP page.</summary>
    private void DrawGpListSettings()
    {
        BoolSetting("GatheringPanel.ListSetting","GatheringPanel.ListSettingTip",()=>configuration.GatherPlanFromList,value=>configuration.GatherPlanFromList=value,c=>c.GatherPlanFromList);
        BoolSetting("GatheringPanel.SaveForBonus","GatheringPanel.SaveForBonusTip",()=>configuration.GpSaveForBonus,value=>configuration.GpSaveForBonus=value,c=>c.GpSaveForBonus);
        IntSetting("GatheringPanel.NodeSeconds","GatheringPanel.NodeSecondsTip",()=>configuration.GatherSecondsBetweenNodes,value=>configuration.GatherSecondsBetweenNodes=value,5,600,DefaultConfiguration.GatherSecondsBetweenNodes);
        IntSetting("GatheringPanel.Regen","GatheringPanel.RegenTip",()=>configuration.GpRegenPerTick,value=>configuration.GpRegenPerTick=value,0,50,DefaultConfiguration.GpRegenPerTick);
        if(configuration.GpRegenObserved>0)ImGui.TextDisabled(localization.Format("GatheringPanel.RegenObserved",configuration.GpRegenObserved));
        BoolSetting("CollectablePanel.UseScrutiny","CollectablePanel.UseScrutinyTip",()=>configuration.CollectableUseScrutiny,value=>configuration.CollectableUseScrutiny=value,c=>c.CollectableUseScrutiny);
        BoolSetting("CollectablePanel.UseRestore","CollectablePanel.UseRestoreTip",()=>configuration.CollectableUseRestore,value=>configuration.CollectableUseRestore=value,c=>c.CollectableUseRestore);
        DrawGpForbiddenActions();
    }

    private void DrawGpForbiddenActions()
    {
        var forbidden=(configuration.GpForbiddenActions ?? []).ToHashSet(StringComparer.Ordinal);
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("GpUi.ForbiddenTitle",forbidden.Count)+"###GpForbiddenActions"))return;
        ImGui.TextWrapped(localization.Get("GpUi.ForbiddenHelp"));
        if(gpActions.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("GatheringPanel.NoPlan"));return;}
        var changed=false;
        foreach(var (key,miner,botanist) in GpActionRules.Groups(catalog))
        {
            var action=miner ?? botanist;
            if(action is null)continue;
            DrawItemIcon(new ItemDefinition(0,action.Name,false,action.Icon),new Vector2(24));
            if(ImGui.IsItemHovered())WrappedTooltip(action.Description);
            ImGui.SameLine();
            var name=miner is not null && botanist is not null && miner.Name!=botanist.Name ? $"{miner.Name} / {botanist.Name}":action.Name;
            var on=forbidden.Contains(key);
            if(ImGui.Checkbox($"{name}###GpForbid{key}",ref on))
            {
                if(on)forbidden.Add(key);else forbidden.Remove(key);
                changed=true;
            }
            Tip("GpUi.ForbiddenActionTip");
        }
        if(forbidden.Count>0 && ImGui.SmallButton(localization.Get("GpUi.ForbiddenClear"))){forbidden.Clear();changed=true;}
        if(!changed)return;
        var previous=configuration.GpForbiddenActions;
        configuration.GpForbiddenActions=forbidden.Order(StringComparer.Ordinal).ToList();
        gpResult=null;gatheringPanelKey=null;
        try{save();saveError=null;}
        catch(Exception error){configuration.GpForbiddenActions=previous;saveError=localization.Get("Common.SaveError");log.Warning(error,"Forbidden gathering actions could not be saved.");}
    }
}
