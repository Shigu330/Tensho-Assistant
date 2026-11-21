namespace TenshoAssistant.Chat;

/// <summary>X-733 (0.1734): the Param of a meal's or a medicine's status on the one who gained it (the ItemFood row, plus 10000 for HQ), read
/// when its line arrives or in the frames after (the line is kept before the game puts the status on); 0 while not read. Set and read on the
/// game's thread.</summary>
public sealed class ChatStatusParam
{
    public ushort Value{get;set;}
}

/// <summary>X-733 (0.1734): one bonus of an ItemFood row: the attribute's name (BaseParam, the client's language), relative (a percent, capped)
/// or not, and its values in normal and high quality.</summary>
public readonly record struct ChatFoodBonus(string Name,bool Relative,int Value,int Max,int ValueHq,int MaxHq);

/// <summary>
/// X-733 (0.1734, T-0.1715-1 seen in game on 21/11: « ne montre pas l'effet du repas »: the tooltip of « Repu » said « Sous l'effet d'un repas. »
/// while the game's own shows the meal's bonuses): a Well Fed or Medicated status (Status 48 and 49, the statuses of the food and medicine
/// items, docs/evidence/0.420-craft-food.md) shows the bonuses of what was eaten or drunk, as the game's status tooltip: « Effets du repas (exp
/// +3%) » for a meal (Addon 1017), then each bonus « Collecte +7% (max 95) » (BaseParam's name, then Addon 1016), the HQ values for an HQ
/// item. The game's words are evaluated by the caller (ISeStringEvaluator); this class only orders them. Pure (tests).
/// </summary>
public static class ChatFoodTip
{
    /// <summary>A status whose bonuses depend on the item that gave it (its Param names the ItemFood row).</summary>
    public static bool Holds(uint status)=>status is Craft.CraftConsumables.WellFed or Craft.CraftConsumables.Medicated;

    /// <summary>The ItemFood row a Param names, and whether the item was HQ (Param of 10000 and more).</summary>
    public static (uint Row,bool Hq) Row(ushort param)=>param>=10000 ? ((uint)(param-10000),true):(param,false);

    /// <param name="status">Well Fed (a meal: the experience line first) or Medicated.</param>
    /// <param name="exp">The meal's experience bonus (ItemFood.EXPBonusPercent).</param>
    /// <param name="expLine">The game's line for it (Addon 1017 with the percent).</param>
    /// <param name="bonusText">The game's words for a bonus (Addon 1016: relative, value, cap).</param>
    public static IReadOnlyList<string> Lines(uint status,bool hq,int exp,IEnumerable<ChatFoodBonus> bonuses,Func<int,string> expLine,Func<bool,int,int,string> bonusText)
    {
        var shown=bonuses.Where(bonus=>bonus.Name.Trim().Length>0).ToList();
        // Two rows of 714 (400 and 402, read on 21/11) hold a negative value: no bonus line then (the status's description stays), never a
        // number the game's words might write wrong.
        if(exp<0 || shown.Any(bonus=>(hq ? bonus.ValueHq:bonus.Value)<0 || (hq ? bonus.MaxHq:bonus.Max)<0))return [];
        var lines=new List<string>();
        if(status==Craft.CraftConsumables.WellFed)lines.Add(expLine(exp));
        foreach(var bonus in shown)
            lines.Add(bonus.Name.Trim()+" "+bonusText(bonus.Relative,hq ? bonus.ValueHq:bonus.Value,hq ? bonus.MaxHq:bonus.Max));
        return lines;
    }
}
