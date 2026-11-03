namespace TenshoAssistant.Craft;

/// <summary>
/// A-02 (0.420): crafting food and medicine — an item whose ItemAction points (Data[1]) to an ItemFood row with Craftsmanship (BaseParam
/// 70), Control (71) or CP (11); food when its status (Data[0]) is Well Fed (48), medicine when Medicated (49).
/// docs/evidence/0.420-craft-food.md. A relative bonus is the given percent of the attribute, capped; NQ and HQ have their own values.
/// </summary>
public sealed record CraftBonus(uint BaseParam,bool Relative,int Value,int Max,int ValueHq,int MaxHq);
public sealed record CraftConsumable(uint ItemId,string Name,bool Medicine,IReadOnlyList<CraftBonus> Bonuses);

public static class CraftConsumables
{
    public const uint Craftsmanship=70,Control=71,Cp=11,WellFed=48,Medicated=49;

    /// <summary>The bonus one consumable gives to one attribute, from the attribute without consumables.</summary>
    public static int Bonus(CraftConsumable? consumable,bool hq,uint baseParam,int attribute)
    {
        if(consumable is null || attribute<0)return 0;
        var total=0;
        foreach(var bonus in consumable.Bonuses.Where(bonus=>bonus.BaseParam==baseParam))
        {
            var value=hq ? bonus.ValueHq:bonus.Value;var max=hq ? bonus.MaxHq:bonus.Max;
            total+=bonus.Relative ? Math.Min((int)(attribute*(long)value/100),max):value;
        }
        return Math.Max(0,total);
    }

    /// <summary>The attributes with a food and a medicine, each computed from the attributes without them, then added.</summary>
    public static (int Craftsmanship,int Control,int Cp) Apply(int craftsmanship,int control,int cp,CraftConsumable? food,bool foodHq,CraftConsumable? medicine,bool medicineHq)
        =>(craftsmanship+Bonus(food,foodHq,Craftsmanship,craftsmanship)+Bonus(medicine,medicineHq,Craftsmanship,craftsmanship),
           control+Bonus(food,foodHq,Control,control)+Bonus(medicine,medicineHq,Control,control),
           cp+Bonus(food,foodHq,Cp,cp)+Bonus(medicine,medicineHq,Cp,cp));
}
