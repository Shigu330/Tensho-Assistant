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
/// the reminder is armed again; more time than the last reading left (a new meal, more gysahl greens) is a new end and arms it again too. Once
/// it is at or below the threshold, it is said once (« ends in n min »). When it is gone: said once (« has ended ») only if nothing was said
/// for this end and it ran out by itself: what was left at the last reading, less the time since, is within <see cref="ExpiredWithin"/>. Taken
/// off earlier (a status clicked off, the companion sent away: « to call it back, use another bunch of gysahl greens », the game's help,
/// HowToPage row 372) is the player's choice: nothing said. Nothing is said while <c>busy</c> (in a fight or a duty, R03.1): the reminder
/// waits and is said after, if it still holds. Pure (tests).
/// X-774 (the review of 0.1779): the companion also goes out of sight for a while without being sent away: while the player is mounted, in a
/// place where it cannot be out (it can only be out where it can be ridden, HowToPage row 361), in a duty, in a cutscene, or for a few seconds
/// after a zone change (<c>held</c>, given by the watcher). Nothing is decided then: no reminder is forgotten and nothing is said twice; once
/// nothing holds it any more and it has not come back within <see cref="AwayGrace"/>, it ran out during the absence (« has left », if nothing
/// was said for this end) or it is gone with time left (nothing said). Gone with nothing to explain it: decided after the same grace, by
/// what was left when it went; a companion back after that, with no more time than before, is still the same end (said once).
/// </summary>
public sealed class PrepTracker(PrepKind kind)
{
    public static readonly TimeSpan ExpiredWithin=TimeSpan.FromSeconds(15);
    /// <summary>X-774: how long the companion may stay out of sight, once nothing holds it, before Tensho decides (it comes back a few
    /// seconds after a dismount or a load). None for a meal: a status does not go out of sight.</summary>
    public static readonly TimeSpan AwayGrace=TimeSpan.FromSeconds(15);
    /// <summary>X-774: more than this over what the last reading left is a new end (a new meal, more gysahl greens).</summary>
    public static readonly TimeSpan Grown=TimeSpan.FromSeconds(15);
    private readonly TimeSpan grace=kind==PrepKind.Chocobo ? AwayGrace:TimeSpan.Zero;
    private DateTimeOffset? lastAt;
    private double lastLeft;
    // told: something was said for this end; waiting: an end is still to come (cleared once it is decided).
    private bool told,waiting;
    // X-774: since the first reading without it; whether something held it since; since when nothing holds it.
    private DateTimeOffset? goneAt,unheldAt;
    private bool goneHeld;

    /// <param name="left">The seconds left; null or 0: none (no Well Fed status, no companion out).</param>
    /// <param name="thresholdMinutes">Said at or below this many minutes left; 0: only once it has ended.</param>
    /// <param name="busy">In a fight or a duty: nothing said now.</param>
    /// <param name="held">X-774: out of sight for a reason that is not the player's choice (mounted, a place without mounts, a duty, a
    /// cutscene, just after a zone change): nothing decided now.</param>
    public PrepNotice? Read(DateTimeOffset now,double? left,int thresholdMinutes,bool busy,bool held=false)
    {
        if(left is > 0 and var seconds)
        {
            PrepNotice? notice=null;
            if(seconds>thresholdMinutes*60.0 || seconds>lastLeft+Grown.TotalSeconds)told=false;
            if(seconds<=thresholdMinutes*60.0 && !told && !busy){told=true;notice=new(kind,false,Math.Max(1,(int)Math.Ceiling(seconds/60)));}
            lastAt=now;lastLeft=seconds;waiting=true;goneAt=null;unheldAt=null;goneHeld=false;
            return notice;
        }
        if(!waiting || lastAt is not { } at)return null;
        goneAt??=now;
        if(held){goneHeld=true;unheldAt=null;return null;}
        unheldAt??=now;
        if(now-unheldAt.Value<grace)return null;
        // Out of sight for something that held it: the time went on meanwhile (measured now); else, what was left when it went.
        var measuredAt=goneHeld ? now:goneAt.Value;
        if(lastLeft-(measuredAt-at).TotalSeconds>ExpiredWithin.TotalSeconds){waiting=false;return null;}
        if(busy)return null;
        waiting=false;
        if(told)return null;
        told=true;
        return new(kind,true,0);
    }

    public void Reset(){lastAt=null;lastLeft=0;told=false;waiting=false;goneAt=null;unheldAt=null;goneHeld=false;}
}
