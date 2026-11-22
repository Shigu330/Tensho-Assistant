using TenshoAssistant.Craft;

namespace TenshoAssistant.Gear;

/// <summary>X-229: the meal chosen in the gear planner for a job: the item and whether it is HQ.</summary>
public sealed class GearMealChoice
{
    public uint ItemId{get;set;}
    public bool Hq{get;set;}
}

/// <summary>
/// X-229 (the user's request of 12/11, the comparison with other plugins K-31, R04 agreement given): a meal in the gear planner. A meal is an
/// item whose ItemAction gives Well Fed (Status 48) with an ItemFood row (its bonuses: BaseParam, relative or not, value and cap, and the HQ
/// values), as the crafting food (docs/evidence/0.420-craft-food.md, CraftConsumables.Bonus: a relative bonus is that percent of the attribute
/// without the meal, capped; a flat one is added). At a synced level the stats stay unknown: the formula is not proved (R06). Pure (tests).
/// </summary>
public static class GearMeals
{
    /// <summary>
    /// The planned values with the meal, and each bonus by attribute. An attribute the planner does not hold (not read for this job) is left
    /// out, never counted from 0; a bonus of 0 is not listed.
    /// </summary>
    public static (Dictionary<string,int> Values,Dictionary<string,int> Bonuses) Apply(IReadOnlyDictionary<string,int> values,CraftConsumable? meal,bool hq)
    {
        var result=new Dictionary<string,int>(values);var bonuses=new Dictionary<string,int>();
        if(meal is null)return (result,bonuses);
        foreach(var param in meal.Bonuses.Select(bonus=>bonus.BaseParam).Distinct())
        {
            if(GearRules.AttributeOf((int)param) is not { } key || !values.TryGetValue(key,out var value))continue;
            var bonus=CraftConsumables.Bonus(meal,hq,param,value);
            if(bonus<=0)continue;
            bonuses[key]=bonus;
            result[key]=value+bonus;
        }
        return (result,bonuses);
    }

    /// <summary>
    /// X-778 (the review of 0.1784, I2): the planned values without the meal the player has eaten. The values read from the game (the
    /// Character window's) already hold its bonus, and in the game a new meal replaces the one eaten: the bonus is taken out before the meal
    /// chosen is added. Its bonus was computed from the real value without it (worn gear), so that value is found back exactly: the one
    /// value whose value plus its bonus is the value read (value plus bonus only grows). Null when one of its stats has no such value (the
    /// stats read not yet up to date, or another bonus in them): then the meal column is unknown, never two meals added up (R06). A stat not
    /// read is left out.
    /// </summary>
    public static (Dictionary<string,int> Values,Dictionary<string,int> Removed)? WithoutActive(IReadOnlyDictionary<string,int> real,
        IReadOnlyDictionary<string,int> planned,CraftConsumable eaten,bool hq)
    {
        var values=new Dictionary<string,int>(planned);var removed=new Dictionary<string,int>();
        foreach(var param in eaten.Bonuses.Select(bonus=>bonus.BaseParam).Distinct())
        {
            if(GearRules.AttributeOf((int)param) is not { } key || !real.TryGetValue(key,out var read) || !values.TryGetValue(key,out var value))continue;
            var most=eaten.Bonuses.Where(bonus=>bonus.BaseParam==param).Sum(bonus=>(long)Math.Max(0,bonus.Relative ? (hq ? bonus.MaxHq:bonus.Max):(hq ? bonus.ValueHq:bonus.Value)));
            int? without=null;
            for(var candidate=read;candidate>=0 && read-candidate<=most;candidate--)
                if(candidate+CraftConsumables.Bonus(eaten,hq,param,candidate)==read){without=candidate;break;}
            if(without is not { } found)return null;
            if(read-found>0)removed[key]=read-found;
            values[key]=value-(read-found);
        }
        return (values,removed);
    }

    /// <summary>The meal chosen for a job in the list read from the sheets; null when none is chosen or the item is no longer a meal.</summary>
    public static (CraftConsumable Meal,bool Hq)? Chosen(IReadOnlyList<CraftConsumable>? meals,GearMealChoice? choice)=>
        choice is { ItemId:not 0 } && meals?.FirstOrDefault(meal=>meal.ItemId==choice.ItemId) is { } found ? (found,choice.Hq):null;
}
