using Dalamud.Bindings.ImGui;
using TenshoAssistant.Character;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// X-229 (0.1784, the user's request of 12/11, K-31, R04 agreement given): the meal of the gear planner, chosen per job among the meals of the
// sheets (Well Fed with an ItemFood row), NQ or HQ; the stats with it in a fifth column. At a synced level: unknown (formula not proved, R06).
internal sealed partial class MainWindow
{
    private void DrawGearMealPicker(uint job)
    {
        var meals=GearMealList?.Invoke();
        var choice=configuration.GearMeals?.GetValueOrDefault(job);
        var chosen=GearMeals.Chosen(meals,choice);
        ImGui.SetNextItemWidth(220*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
        if(ImGui.BeginCombo("###GearMeal",chosen is { } shown ? shown.Meal.Name+(shown.Hq ? " HQ":""):localization.Get("Gear.MealNone")))
        {
            try
            {
                ImGui.SetNextItemWidth(200*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
                ImGui.InputTextWithHint("###GearMealQuery",localization.Get("Gear.MealSearch"),ref gearMealQuery,60);
                Tip("Gear.MealSearchTip");
                if(ImGui.Selectable(localization.Get("Gear.MealNone")+"##GearMealNone",chosen is null) && configuration.GearMeals?.Remove(job)==true)SaveGearPlan();
                if(meals is { Count:>0 })
                    foreach(var meal in meals.Where(meal=>gearMealQuery.Length<2 || SearchText.Contains(meal.Name,gearMealQuery,StringComparison.CurrentCultureIgnoreCase)).Take(200))
                        if(ImGui.Selectable($"{meal.Name}##GearMeal{meal.ItemId}",chosen?.Meal.ItemId==meal.ItemId))
                        {
                            (configuration.GearMeals??=[])[job]=new GearMealChoice{ItemId=meal.ItemId,Hq=choice?.Hq ?? true};
                            SaveGearPlan();
                        }
            }
            finally{ImGui.EndCombo();}
        }
        Tip("Gear.MealTip");
        if(chosen is null || choice is null)return;
        ImGui.SameLine();
        // A toggle of the plan's data (not a setting): HQ or NQ.
        if(ImGui.SmallButton((choice.Hq ? "HQ":"NQ")+"###GearMealHq")){choice.Hq=!choice.Hq;SaveGearPlan();}
        Tip("Gear.MealHqTip");
    }

    private ((GearModel Model,uint Meal,bool Hq,int Language) Key,GearModel Result)? gearMealCache;

    /// <summary>X-229: the model with the meal chosen for the job: the planned values with it and the derived values from them; built once per
    /// change of the model or of the meal.</summary>
    private GearModel GearMealModel(GearModel model,CharacterStatsState state)
    {
        var choice=configuration.GearMeals?.GetValueOrDefault(state.JobId);
        var key=(model,choice?.ItemId ?? 0,choice?.Hq==true,localization.Revision);
        if(gearMealCache is { } cached && ReferenceEquals(cached.Key.Model,model) && cached.Key.Meal==key.Item2 && cached.Key.Hq==key.Item3 && cached.Key.Language==key.Item4)return cached.Result;
        var result=model;
        if(GearMeals.Chosen(GearMealList?.Invoke(),choice) is { } meal)
        {
            var (withMeal,bonuses)=GearMeals.Apply(model.PlannedValues,meal.Meal,meal.Hq);
            var derived=CombatCategory(state.Category) && state.Modifiers.Div>0;
            result=model with {MealValues=withMeal,MealBonuses=bonuses,
                CombatMeal=derived ? StatFormulas.Combat(withMeal,state.Category,state.Role,state.Modifiers).ToDictionary(row=>row.Key,row=>row.Value):new Dictionary<string,string>()};
        }
        gearMealCache=(key,result);
        return result;
    }
}
