using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// 0.478 (user request, in-game campaign): GP plan settings; actions can be forbidden, as for macros. 0.515: the plan is for the node in front of the player only; the crafting list only says what it still needs.
internal sealed partial class MainWindow
{
    private CraftList? gatherNeedsList;
    private RecipeCatalog? gatherNeedsCatalog;
    private volatile IReadOnlyDictionary<uint,long>? gatherNeeds;

    private string GpForbidden=>GpActionRules.Joined(configuration.GpForbiddenActions);
    // 0.488 (user request): what the plan may not use on a job: what is forbidden in the settings, and what this job has not unlocked (a level is not enough: some come with a quest).
    internal Func<uint,bool>? GpActionUnlocked{get;set;}
    private readonly Dictionary<uint,(string Locked,DateTimeOffset At)> gpLockedActions=[];
    private string GpForbiddenFor(uint job)
    {
        var now=DateTimeOffset.UtcNow;
        if(!gpLockedActions.TryGetValue(job,out var known) || now-known.At>=TimeSpan.FromSeconds(30))
        {
            var locked=GpActionUnlocked is { } unlocked ? GpActionCatalog.Specs.Where(spec=>spec.JobId==job && !unlocked(spec.Id)).Select(GpActionRules.Key):[];
            gpLockedActions[job]=known=(GpActionRules.Joined(locked),now);
        }
        return GpActionRules.Joined(GpForbidden.Split('|').Concat(known.Locked.Split('|')));
    }
    // 0.488: the job's traits, from the game's unlock state; asked again from time to time, a trait can come with a level or a quest.
    internal Func<uint,int>? GpTraitsUnlocked{get;set;}
    private readonly Dictionary<uint,(int Bonus,DateTimeOffset At)> gpTraitBonus=[];
    private int GpTraitBonus(uint job)
    {
        var now=DateTimeOffset.UtcNow;
        if(gpTraitBonus.TryGetValue(job,out var known) && now-known.At<TimeSpan.FromSeconds(30))return known.Bonus;
        var bonus=GpTraitsUnlocked?.Invoke(job) ?? 0;
        gpTraitBonus[job]=(bonus,now);
        return bonus;
    }
    private int GpRegenFor(uint job)=>GpRegen.Rate(configuration.GpRegenPerTick,configuration.GpRegenBase,GpTraitBonus(job));

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

    /// <summary>Settings › Modules › Gathering, and the GP page.</summary>
    private void DrawGpPlanSettings()
    {
        BoolSetting("GatheringPanel.ListSetting","GatheringPanel.ListSettingTip",()=>configuration.GatherPlanFromList,value=>configuration.GatherPlanFromList=value,c=>c.GatherPlanFromList);
        IntSetting("GatheringPanel.Regen","GatheringPanel.RegenTip",()=>configuration.GpRegenPerTick,value=>configuration.GpRegenPerTick=value,0,50,DefaultConfiguration.GpRegenPerTick);
        if(configuration.GpRegenBase>0)ImGui.TextDisabled(localization.Format("GatheringPanel.RegenObserved",configuration.GpRegenBase,GpTraitBonus(16),GpTraitBonus(17),GpTraitBonus(18)));
        BoolSetting("CollectablePanel.UseScrutiny","CollectablePanel.UseScrutinyTip",()=>configuration.CollectableUseScrutiny,value=>configuration.CollectableUseScrutiny=value,c=>c.CollectableUseScrutiny);
        DrawCollectableRisk();
        if(DefaultButton("CollectableRisk",CollectableRisk.Snap(configuration.CollectableRiskTolerance)!=DefaultConfiguration.CollectableRiskTolerance)){configuration.CollectableRiskTolerance=DefaultConfiguration.CollectableRiskTolerance;SaveReminders();}
        BoolSetting("CollectablePanel.UseFocus","CollectablePanel.UseFocusTip",()=>configuration.CollectableUseFocus,value=>configuration.CollectableUseFocus=value,c=>c.CollectableUseFocus);
        BoolSetting("CollectablePanel.UsePriming","CollectablePanel.UsePrimingTip",()=>configuration.CollectableUsePriming,value=>configuration.CollectableUsePriming=value,c=>c.CollectableUsePriming);
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
