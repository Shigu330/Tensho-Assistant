namespace TenshoAssistant.Chat;

/// <summary>X-733 (0.1734): the Param of a meal's or a medicine's status on the one who gained it (the ItemFood row, plus 10000 for HQ), read
/// from its line on (ChatStatusWatch, X-756); 0 while not read. Set and read on the game's thread.</summary>
public sealed class ChatStatusParam
{
    public ushort Value{get;set;}
}

/// <summary>
/// X-756 (0.1739, review of 0.1738, I1): which Param a meal's or a medicine's line names, read on the one who gained it from its line on. The
/// game may print the line before it puts the new status on (X-733): someone already Well Fed from a meal A who eats a meal B still holds A
/// when the line arrives, so the first Param seen is not the line's. The Param held at the line is kept and the next frames are watched: a
/// Param that differs from it is the line's (put on after the line); one that stays the same for the whole watch is the line's too (put on
/// before the line, or the same meal eaten again); none: not found. Only the lines of a buff gained (LogFilter: « Effets bénéfiques reçus »,
/// kind 46; 48 is « Fin des effets bénéfiques », a status lost: its Param is no longer there to read). Pure (tests).
/// X-760 (0.1744, review of 0.1742, M2): the watch lasts <see cref="Frames"/> frames and <see cref="WatchMs"/> at least: 30 frames alone
/// were half a second at 60 frames a second, a fifth at 144, and a meal put on later over an older one was taken for the older one. The
/// delay the game takes is not measured yet: the reader writes the longest one seen in the log.
/// X-762 (0.1748, review of 0.1746, I2; D-208, the owner's choice of 21/11/2026 at about 8:15 pm): another player's status has a watch of its
/// own, half a second at most after its line whatever the frame rate (<see cref="Other"/>): what D-207 was asked on (R04).
/// </summary>
public static class ChatStatusWatch
{
    /// <summary>The frames watched after the line, at least (half a second at 60 frames a second).</summary>
    public const int Frames=30;

    /// <summary>X-760 (0.1744): how long the watch lasts at least, whatever the frame rate.</summary>
    public const int WatchMs=2000;

    /// <summary>The log kind of the lines « … gains the effect of … » for a buff (LogFilter rows 56, 63, 76, 83, 96, 103…: kind 46).</summary>
    public const int GainedKind=46;

    /// <summary>A line of this kind names a status just gained, whose Param can be read.</summary>
    public static bool Reads(int kind)=>kind==GainedKind;

    /// <param name="atLine">The status's Param when its line arrived (0: not held then).</param>
    /// <param name="seen">Its Param now (0: not held).</param>
    /// <param name="frame">0 at the line, then 1, 2… for the next frames.</param>
    /// <param name="elapsedMs">The milliseconds since the line (X-760).</param>
    /// <returns>Over: the watch ends, with the line's Param (0: not found) and what happened, for the log.</returns>
    public static (bool Over,ushort Value,string How) Next(ushort atLine,ushort seen,int frame,long elapsedMs)
    {
        if(seen!=0 && seen!=atLine)return (true,seen,atLine==0 ? "put on after its line":"changed after its line (an older one held at the line)");
        if(frame<Frames || elapsedMs<WatchMs)return (false,0,"");
        return seen!=0 ? (true,seen,"held from its line on, unchanged"):(true,(ushort)0,"not found");
    }

    /// <summary>X-762 (D-208): another player's status is never read later than this many milliseconds after its line.</summary>
    public const int OtherMs=500;

    /// <summary>X-762: whether another player's status may still be read this many milliseconds after its line.</summary>
    public static bool OtherMayRead(long elapsedMs)=>elapsedMs<=OtherMs;

    /// <summary>
    /// X-762 (D-208): the watch of another player's status. A Param that differs from the one held at the line is the line's; at half a
    /// second the watch ends: a Param unchanged since the line may be an older meal still held, the line's put on later, so no bonus rather
    /// than an older meal, unless it was put on just before the line (<paramref name="fresh"/>, X-766, D-211); none: not found.
    /// </summary>
    /// <param name="fresh">X-766 (D-211): the Well Fed status held at the line was at its full duration (<see cref="PutOnAtLine"/>).</param>
    public static (bool Over,ushort Value,string How) Other(ushort atLine,ushort seen,long elapsedMs,bool fresh=false)
    {
        if(seen!=0 && seen!=atLine)return (true,seen,atLine==0 ? "another player's, put on after its line":"another player's, changed after its line (an older one held at the line)");
        return elapsedMs<OtherMs ? (false,(ushort)0,""):OtherEnd(atLine,fresh);
    }

    /// <summary>X-762: the end of another player's watch, at half a second or at the first frame past it (then nothing is read). X-766
    /// (D-211): a meal held at its line at its full duration is the line's (<paramref name="fresh"/>).</summary>
    public static (bool Over,ushort Value,string How) OtherEnd(ushort atLine,bool fresh=false)=>
        atLine!=0 && fresh ? (true,atLine,"another player's, held at its line at its full duration: put on just before it")
        :(true,(ushort)0,atLine!=0 ? "another player's, unchanged for half a second: no bonus (maybe an older meal)":"another player's, not found within half a second");

    /// <summary>X-766 (D-211): how many seconds below its full duration another player's Well Fed status may be at its line and still be
    /// the line's (put on just before it). Kept short: an older meal is that close to its full duration only if eaten that many seconds
    /// before the line; the gap seen in game is written in the log.</summary>
    public const float FreshSeconds=3;

    /// <summary>
    /// X-766 (D-211, the owner's choice of 21/11/2026 at about 10:30 pm, R04): whether another player's Well Fed status held when its line
    /// arrives was put on just before it: its remaining time is the meal's full duration (<paramref name="fullSeconds"/>, its ItemAction:
    /// 1800 s for every meal, docs/evidence/0.1739-meal-status-gained.md) down to <see cref="FreshSeconds"/> below it. Not when a
    /// meal-duration extension is held (<see cref="ExtendsMeals"/>: its length is not read; an older meal could then sit at 1800 s), nor
    /// past the full duration (the same meal eaten again adds its time, up to 60 minutes by the item's help: an older meal may be held).
    /// Unknown duration (0): never.
    /// </summary>
    public static bool PutOnAtLine(float remaining,int fullSeconds,bool extended)=>
        fullSeconds>0 && !extended && remaining<=fullSeconds+0.5f && remaining>=fullSeconds-FreshSeconds;

    /// <summary>X-766: the statuses that lengthen a meal's effect (Status 360 « Meat and Mead », the company action, by 5, 10 or 15 minutes
    /// by its items' help; 1084 « Rationing », the Grand Company manuals, by 15 minutes; both « Food effect duration is increased. », the
    /// only two, read on 21/11/2026).</summary>
    public static bool ExtendsMeals(uint status)=>status is 360 or 1084;

    /// <summary>
    /// X-766: the full duration of a meal's Well Fed status by its Param (the ItemFood row, plus 10000 for HQ), from the ItemAction rows:
    /// status (Data[0], 48 for a meal), ItemFood row (Data[1]) and seconds (Data[2]), the same three in DataHQ for the HQ item. A Param given
    /// two different durations has none (unknown); a duration of 0 is not one.
    /// </summary>
    public static IReadOnlyDictionary<ushort,int> MealDurations(IEnumerable<(uint Status,uint Row,uint Seconds,bool Hq)> actions)
    {
        var durations=new Dictionary<ushort,int>();
        var unclear=new HashSet<ushort>();
        foreach(var (status,row,seconds,hq) in actions)
        {
            if(status!=Craft.CraftConsumables.WellFed || seconds==0 || row==0 || row>=10000)continue;
            var param=(ushort)(hq ? row+10000:row);
            if(durations.TryGetValue(param,out var known) && known!=(int)seconds)unclear.Add(param);
            durations[param]=(int)seconds;
        }
        foreach(var param in unclear)durations.Remove(param);
        return durations;
    }
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
