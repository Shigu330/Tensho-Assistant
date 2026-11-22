namespace TenshoAssistant.Reminders;

/// <summary>
/// X-226 (the user's request of 12/11, the comparison with other plugins K-15, accepted with the R04 agreement): the preparation reminder's
/// settings. A meal's Well Fed status (Status 48) or the chocobo companion (Buddy.CompanionInfo.TimeLeft) about to end, or ended: a chat line
/// and a sound with the alarm options, as the gear wear alert (D-38), on by default. The thresholds in minutes (0: only once it has ended),
/// clamped on use.
/// </summary>
public sealed class PrepReminderSettings
{
    public const int MaximumMinutes=30;
    public bool Meal{get;set;}=true;
    public int MealMinutes{get;set;}=5;
    public bool Chocobo{get;set;}=true;
    public int ChocoboMinutes{get;set;}=5;
    public int MealThreshold=>Math.Clamp(MealMinutes,0,MaximumMinutes);
    public int ChocoboThreshold=>Math.Clamp(ChocoboMinutes,0,MaximumMinutes);
}

public enum PrepKind{Meal,Chocobo}

/// <summary>What to say: <paramref name="Ended"/> false: it ends in <paramref name="Minutes"/> (at least 1); true: it has ended.</summary>
public sealed record PrepNotice(PrepKind Kind,bool Ended,int Minutes);

/// <summary>
/// X-226: one reminder per end, for one thing (the meal or the companion), read every few seconds. While what is left is above the threshold,
/// the reminder is armed again (a new meal, a new bunch of gysahl greens). Once it is at or below the threshold, it is said once (« ends in n
/// min »). When it is gone: said once (« has ended ») only if nothing was said for this end and it ran out by itself: what was left at the last
/// reading, less the time since, is within <see cref="ExpiredWithin"/>. Taken off earlier (a status clicked off, the companion sent away:
/// « to call it back, use another bunch of gysahl greens », the game's help, HowToPage row 372) is the player's choice: nothing said. Nothing is
/// said while <c>busy</c> (in a fight or a duty, R03.1): the reminder waits and is said after, if it still holds. Pure (tests).
/// </summary>
public sealed class PrepTracker(PrepKind kind)
{
    public static readonly TimeSpan ExpiredWithin=TimeSpan.FromSeconds(15);
    private DateTimeOffset? lastAt;
    private double lastLeft;
    private bool told;

    /// <param name="left">The seconds left; null or 0: none (no Well Fed status, no companion out).</param>
    /// <param name="thresholdMinutes">Said at or below this many minutes left; 0: only once it has ended.</param>
    /// <param name="busy">In a fight or a duty: nothing said now.</param>
    public PrepNotice? Read(DateTimeOffset now,double? left,int thresholdMinutes,bool busy)
    {
        if(left is > 0 and var seconds)
        {
            PrepNotice? notice=null;
            if(seconds>thresholdMinutes*60.0)told=false;
            else if(!told && !busy){told=true;notice=new(kind,false,Math.Max(1,(int)Math.Ceiling(seconds/60)));}
            lastAt=now;lastLeft=seconds;
            return notice;
        }
        if(lastAt is not { } at)return null;
        if(lastLeft-(now-at).TotalSeconds>ExpiredWithin.TotalSeconds){Reset();return null;}
        if(busy)return null;
        var say=!told;
        Reset();
        return say ? new(kind,true,0):null;
    }

    public void Reset(){lastAt=null;lastLeft=0;told=false;}
}
