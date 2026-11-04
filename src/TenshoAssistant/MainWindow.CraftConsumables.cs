using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// A-02 (0.420): food and medicine in the simulator — chosen in two lists (game items with crafting bonuses), NQ or HQ, added to the
// attributes typed or copied before the simulation and the search. Company actions are not modelled (their size is not in the data read).
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<CraftConsumable>>? CraftConsumableList { get; set; }
    private CraftConsumable? simulationFood,simulationMedicine;
    private bool simulationFoodHq=true,simulationMedicineHq=true;
    // A-13 (0.434): the job is a specialist (specialist actions and recipes).
    private bool simulationSpecialist;
    internal Func<uint,CraftDetected?>? CraftDetectedNow { get; set; }
    private CraftDetected? craftDetected;
    private DateTimeOffset craftDetectedAt;
    private (uint Recipe,uint Job,bool Worn)? craftDetectionFor;

    /// <summary>
    /// 0.464 (user request, in-game campaign): Manipulation, specialist, food and medicine detected instead of asked, once for a recipe (the boxes stay editable). With the job worn the
    /// attributes are copied from the game and already hold the food and medicine; otherwise they are chosen in the lists, to be added to typed attributes.
    /// </summary>
    private void DetectCraftState(RecipeDefinition recipe,uint job,bool worn,Action copy)
    {
        var now=DateTimeOffset.UtcNow;
        if(now-craftDetectedAt>=TimeSpan.FromSeconds(1)){craftDetectedAt=now;craftDetected=CraftDetectedNow?.Invoke(job);}
        if(craftDetectionFor==(recipe.RecipeId,job,worn))return;
        craftDetectionFor=(recipe.RecipeId,job,worn);
        if(ManipulationLearned is { } learned)simulationManipulation=learned(job);
        if(craftDetected?.Specialist is { } specialist)simulationSpecialist=specialist;
        var list=CraftConsumableList?.Invoke();
        if(worn){simulationFood=null;simulationMedicine=null;copy();}
        else
        {
            if(CraftConsumables.FromStatus(list,craftDetected?.Food,false) is { } food){simulationFood=food.Item;simulationFoodHq=food.Hq;}
            if(CraftConsumables.FromStatus(list,craftDetected?.Medicine,true) is { } medicine){simulationMedicine=medicine.Item;simulationMedicineHq=medicine.Hq;}
        }
        simulationResult=null;simulationError=null;
    }

    private void DrawCraftDetection(bool worn)
    {
        DrawDetectedConsumables(craftDetected);
        ImGui.SameLine();ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(localization.Format("CraftSimulation.Detected",CraftYesNo(simulationManipulation),CraftYesNo(craftDetected?.Specialist)));
        if(worn && (craftDetected?.Food is > 0 || craftDetected?.Medicine is > 0))ImGui.TextDisabled(localization.Get("CraftSimulation.DetectedCounted"));
    }

    /// <summary>
    /// 0.469 (user request, in-game campaign): the active food and medicine as the item's icon with its tooltip. Only those with a crafting bonus count (CraftConsumableList holds no others),
    /// so a food for combat reads as none.
    /// </summary>
    private void DrawDetectedConsumables(CraftDetected? detected)
    {
        var list=CraftConsumableList?.Invoke();
        DrawDetectedConsumable("CraftSimulation.DetectedFood",CraftConsumables.FromStatus(list,detected?.Food,false));
        ImGui.SameLine();
        DrawDetectedConsumable("CraftSimulation.DetectedMedicine",CraftConsumables.FromStatus(list,detected?.Medicine,true));
    }
    private void DrawDetectedConsumable(string labelKey,(CraftConsumable Item,bool Hq)? consumable)
    {
        ImGui.AlignTextToFramePadding();ImGui.TextUnformatted(localization.Get(labelKey));ImGui.SameLine();
        if(consumable is not { } known){ImGui.TextDisabled(localization.Get("CraftSimulation.DetectedNone"));return;}
        DrawItemIcon(itemCatalog.State.Catalog?.Find(known.Item.ItemId),new System.Numerics.Vector2(28));
        if(ImGui.IsItemHovered())DrawItemInfoTooltip(known.Item.ItemId);
        if(known.Hq){ImGui.SameLine();ImGui.TextDisabled("HQ");}
    }
    private string CraftYesNo(bool? value)=>localization.Get(value switch{true=>"CraftSimulation.DetectedYes",false=>"CraftSimulation.DetectedNo",null=>"CraftSimulation.DetectedUnknown"});

    private bool CraftInput(string label,ref int value,bool? ok,string tip)
    {
        ImGui.SetNextItemWidth(110);
        if(ok is { } good)ImGui.PushStyleColor(ImGuiCol.Text,good ? UI.FfxivTheme.Success:UI.FfxivTheme.Error);
        bool changed;
        try{changed=ImGui.InputInt(label,ref value,0);}
        finally{if(ok is not null)ImGui.PopStyleColor();}
        Tip(tip);
        return changed;
    }

    private CraftStats EffectiveCraftStats()
    {
        var (cs,control,cp)=CraftConsumables.Apply(simulationCs,simulationControl,simulationCp,simulationFood,simulationFoodHq,simulationMedicine,simulationMedicineHq);
        return new(simulationLevel,cs,control,cp,simulationManipulation,simulationSpecialist);
    }

    private bool DrawCraftConsumables()
    {
        var list=CraftConsumableList?.Invoke() ?? [];
        if(list.Count==0)return false;
        var changed=false;
        changed|=ConsumableCombo("CraftSimulation.Food","CraftSimulation.FoodTip",list.Where(item=>!item.Medicine).ToArray(),ref simulationFood,ref simulationFoodHq);
        changed|=ConsumableCombo("CraftSimulation.Medicine","CraftSimulation.MedicineTip",list.Where(item=>item.Medicine).ToArray(),ref simulationMedicine,ref simulationMedicineHq);
        if(simulationFood is not null || simulationMedicine is not null)
        {
            var stats=EffectiveCraftStats();
            ImGui.TextColored(Accent,localization.Format("CraftSimulation.WithConsumables",stats.Craftsmanship,stats.Control,stats.Cp));
            ImGui.TextDisabled(localization.Get("CraftSimulation.ConsumablesHelp"));
        }
        return changed;
    }

    private bool ConsumableCombo(string labelKey,string tipKey,IReadOnlyList<CraftConsumable> items,ref CraftConsumable? selected,ref bool hq)
    {
        var changed=false;
        ImGui.SetNextItemWidth(260);
        if(ImGui.BeginCombo(localization.Get(labelKey)+"###"+labelKey,selected?.Name ?? localization.Get("CraftSimulation.NoConsumable")))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("CraftSimulation.NoConsumable"),selected is null)){selected=null;changed=true;}
                foreach(var item in items)
                    if(ImGui.Selectable($"{item.Name}##Consumable{item.ItemId}",selected?.ItemId==item.ItemId)){selected=item;changed=true;}
            }
            finally{ImGui.EndCombo();}
        }
        Tip(tipKey);
        ImGui.SameLine();
        changed|=ImGui.Checkbox("HQ###"+labelKey+"Hq",ref hq);
        Tip("CraftSimulation.HqTip");
        return changed;
    }
}
