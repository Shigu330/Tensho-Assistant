using Dalamud.Bindings.ImGui;
using TenshoAssistant.Character;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// X-229 (0.1784, the user's request of 12/11, K-31, R04 agreement given): the meal of the gear planner, chosen per job among the meals of the
// sheets (Well Fed with an ItemFood row), NQ or HQ; the stats with it in a fifth column. At a synced level: unknown (formula not proved, R06).
// X-778 (0.1787, the review of 0.1784, I2): the meal eaten taken out of the values read before the meal chosen is added.
// X-780 (the review of 0.1789, M2): unknown until the stats read follow a change of the meal eaten (GearMealWatch).
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
                InputWithHint("###GearMealQuery","Gear.MealSearch",ref gearMealQuery,60);
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

    private ((GearModel Model,uint Meal,bool Hq,int Language,int Eaten,bool Waiting) Key,GearModel Result)? gearMealCache;
    private readonly GearMealWatch gearMealWatch=new();
    private DateTimeOffset gearMealEatenAt;
    private int gearMealEaten;

    /// <summary>X-778: the Well Fed of the player now, read at most once a second (the crafting simulator's reader, CraftDetectedNow): its
    /// Param (the ItemFood row, plus 10000 for HQ); 0: none; -1: not read.</summary>
    private int GearMealEaten()
    {
        var now=DateTimeOffset.UtcNow;
        if(now-gearMealEatenAt>=TimeSpan.FromSeconds(1))
        {
            gearMealEatenAt=now;
            gearMealEaten=CraftDetectedNow?.Invoke(characterStats.State.JobId) is { } detected ? detected.Food ?? 0:-1;
            gearMealWatch.Seen(gearMealEaten,now);
        }
        return gearMealEaten;
    }

    /// <summary>X-229: the model with the meal chosen for the job: the planned values with it and the derived values from them; built once per
    /// change of the model or of the meal. X-778: for the job worn (its values read from the game hold the meal eaten), the meal eaten is taken
    /// out first (in the game a new meal replaces it); when it cannot be, the column is unknown.</summary>
    private GearModel GearMealModel(GearModel model,CharacterStatsState state)
    {
        var choice=configuration.GearMeals?.GetValueOrDefault(state.JobId);
        var fromGame=state.JobId==characterStats.State.JobId;
        var eatenNow=choice is { ItemId:not 0 } && fromGame ? GearMealEaten():0;
        // X-780: the stats read must follow the meal seen (read after it changed), else the column is unknown for that second.
        var waiting=choice is { ItemId:not 0 } && fromGame && !gearMealWatch.Settled(characterStats.State.ObservedAt);
        var key=(model,choice?.ItemId ?? 0,choice?.Hq==true,localization.Revision,eatenNow,waiting);
        if(gearMealCache is { } cached && ReferenceEquals(cached.Key.Model,model) && cached.Key.Meal==key.Item2 && cached.Key.Hq==key.Item3 && cached.Key.Language==key.Item4
           && cached.Key.Eaten==key.Item5 && cached.Key.Waiting==key.Item6)return cached.Result;
        var result=model;
        var meals=GearMealList?.Invoke();
        if(GearMeals.Chosen(meals,choice) is { } meal)
        {
            IReadOnlyDictionary<string,int> values=model.PlannedValues;string? eatenName=null;var unknown=key.Item6;
            if(!unknown && key.Item5!=0)
            {
                if(key.Item5>0 && Craft.CraftConsumables.FromStatus(meals,(ushort)key.Item5,false) is { } eaten
                   && GearMeals.WithoutActive(state.Values,model.PlannedValues,eaten.Item,eaten.Hq) is { } without)
                {values=without.Values;eatenName=eaten.Item.Name+(eaten.Hq ? " HQ":"");}
                else unknown=true;
            }
            if(unknown)result=model with {MealValues=new Dictionary<string,int>(),MealBonuses=new Dictionary<string,int>(),CombatMeal=new Dictionary<string,string>(),MealUnknown=true};
            else
            {
                var (withMeal,bonuses)=GearMeals.Apply(values,meal.Meal,meal.Hq);
                var derived=CombatCategory(state.Category) && state.Modifiers.Div>0;
                result=model with {MealValues=withMeal,MealBonuses=bonuses,MealEaten=eatenName,
                    CombatMeal=derived ? StatFormulas.Combat(withMeal,state.Category,state.Role,state.Modifiers).ToDictionary(row=>row.Key,row=>row.Value):new Dictionary<string,string>()};
            }
        }
        gearMealCache=(key,result);
        return result;
    }
}
