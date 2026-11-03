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
