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

/// <summary>X-777: what <see cref="PrepTracker.Watch"/> saw this frame: nothing new, the companion gone out of sight, or back.</summary>
public enum PrepSight{Same,Went,Back}

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
/// X-777 (the review of 0.1784, I1): what made it go is decided when it goes, not later: a companion sent away with time left and then
/// held (mounted, a teleport and its load, a city, a duty) is still sent away, and its time ending during that absence is never told. The
/// watcher calls <see cref="Watch"/> every frame: the frame it goes, whether something holds it then, or within <see cref="HeldWithin"/>
/// after (the game may hide it a frame before the mount or the load shows); without it, the first reading without it decides.
/// X-798 (0.1811, the user's reading of 22/11, test T-0.1786-2: « the timer stops »): while the player is mounted (<c>frozen</c>) the game's
/// companion timer does not run: the time spent mounted is not taken off what was left (before, a ride longer than the time it had gave a
/// false « has left » at the dismount). Only the mount freezes it (the other holds are not known to: the time goes on for them, as before).
/// </summary>
public sealed class PrepTracker(PrepKind kind)
{
    public static readonly TimeSpan ExpiredWithin=TimeSpan.FromSeconds(15);
    /// <summary>X-774: how long the companion may stay out of sight, once nothing holds it, before Tensho decides (it comes back a few
    /// seconds after a dismount or a load). None for a meal: a status does not go out of sight.</summary>
    public static readonly TimeSpan AwayGrace=TimeSpan.FromSeconds(15);
    /// <summary>X-774: more than this over what the last reading left is a new end (a new meal, more gysahl greens).</summary>
    public static readonly TimeSpan Grown=TimeSpan.FromSeconds(15);
    /// <summary>X-777: a hold that starts this soon after the companion went out of sight explains it; a later one does not.</summary>
    public static readonly TimeSpan HeldWithin=TimeSpan.FromSeconds(1);
    private readonly TimeSpan grace=kind==PrepKind.Chocobo ? AwayGrace:TimeSpan.Zero;
    private DateTimeOffset? lastAt;
    private double lastLeft;
    // told: something was said for this end; waiting: an end is still to come (cleared once it is decided).
    private bool told,waiting;
    // X-774: since the first reading without it; whether something held it since; since when nothing holds it.
    private DateTimeOffset? goneAt,unheldAt;
    private bool goneHeld;
    // X-798: since when the timer is frozen (mounted) while it is gone, and how long it was frozen before.
    private DateTimeOffset? frozenSince;
    private TimeSpan frozenFor;
    // X-777: seen by Watch every frame: out at the last frame; when it went; whether something held it then (null: not seen going).
    private bool seenOut;
    private DateTimeOffset? wentAt;
    private bool? wentHeld;

    /// <summary>X-777: whether <see cref="Watch"/> needs to know if something holds the companion this frame (the frame it goes, and
    /// <see cref="HeldWithin"/> after): the watcher works it out only then.</summary>
    public bool WantsHeld(DateTimeOffset now,bool outNow)=>
        !outNow && (seenOut || wentHeld==false && wentAt is { } at && now-at<=HeldWithin);

    /// <summary>X-777: every frame, whether the companion is out and, when <see cref="WantsHeld"/>, whether something holds it: the moment it
    /// goes out of sight, and what made it go, are kept for <see cref="Read"/>.</summary>
    public PrepSight Watch(DateTimeOffset now,bool outNow,bool held)
    {
        if(outNow)
        {
            var back=!seenOut && wentAt is not null;
            seenOut=true;wentAt=null;wentHeld=null;
            return back ? PrepSight.Back:PrepSight.Same;
        }
        if(seenOut){seenOut=false;wentAt=now;wentHeld=held;return PrepSight.Went;}
        if(held && wentHeld==false && wentAt is { } at && now-at<=HeldWithin)wentHeld=true;
        return PrepSight.Same;
    }

    /// <param name="left">The seconds left; null or 0: none (no Well Fed status, no companion out).</param>
    /// <param name="thresholdMinutes">Said at or below this many minutes left; 0: only once it has ended.</param>
    /// <param name="busy">In a fight or a duty: nothing said now.</param>
    /// <param name="held">X-774: out of sight for a reason that is not the player's choice (mounted, a place without mounts, a duty, a
    /// cutscene, just after a zone change): nothing decided now, for a companion that went for such a reason (X-777).</param>
    /// <param name="frozen">X-798: the player is mounted: the game's timer of the companion is frozen meanwhile.</param>
    public PrepNotice? Read(DateTimeOffset now,double? left,int thresholdMinutes,bool busy,bool held=false,bool frozen=false)
    {
        if(left is > 0 and var seconds)
        {
            PrepNotice? notice=null;
            if(seconds>thresholdMinutes*60.0 || seconds>lastLeft+Grown.TotalSeconds)told=false;
            if(seconds<=thresholdMinutes*60.0 && !told && !busy){told=true;notice=new(kind,false,Math.Max(1,(int)Math.Ceiling(seconds/60)));}
            lastAt=now;lastLeft=seconds;waiting=true;goneAt=null;unheldAt=null;goneHeld=false;frozenSince=null;frozenFor=TimeSpan.Zero;
            return notice;
        }
        if(!waiting || lastAt is not { } at)return null;
        // X-777: held or not is decided when it goes (seen by Watch, else at this first reading without it); a hold that starts later
        // does not make a companion sent away a held one.
        if(goneAt is null){goneAt=now;goneHeld=wentHeld ?? held;}
        else if(wentHeld==true)goneHeld=true;
        // X-798: the time mounted while it is gone, taken off the time measured.
        if(frozen)frozenSince??=now;
        else if(frozenSince is { } since){frozenFor+=now-since;frozenSince=null;}
        if(goneHeld && held){unheldAt=null;return null;}
        unheldAt??=now;
        if(now-unheldAt.Value<grace)return null;
        // Out of sight for something that held it: the time went on meanwhile (measured now), except while mounted (X-798: frozen); else,
        // what was left when it went.
        var measuredAt=goneHeld ? now-frozenFor-(frozenSince is { } still ? now-still:TimeSpan.Zero):goneAt.Value;
        if(lastLeft-(measuredAt-at).TotalSeconds>ExpiredWithin.TotalSeconds){waiting=false;return null;}
        if(busy)return null;
        waiting=false;
        if(told)return null;
        told=true;
        return new(kind,true,0);
    }

    public void Reset(){lastAt=null;lastLeft=0;told=false;waiting=false;goneAt=null;unheldAt=null;goneHeld=false;seenOut=false;wentAt=null;wentHeld=null;frozenSince=null;frozenFor=TimeSpan.Zero;}
}

/// <summary>
/// X-781 (the review of 0.1789, M1): whether a loading screen or a zone change still explains the chocobo companion out of sight. After one
/// it takes a few seconds to come back (X-774: up to <see cref="Settling"/>); once it has been seen back, out for <see cref="Back"/> without a
/// break after the load, it going again is no longer the load's doing: sent away by the player within the settling time, it is not taken
/// for held (before, it was, and a mount just after kept it held for the whole ride, then a false « has left »). Out for less than
/// <see cref="Back"/> right after the load (what the game may still show of it then) does not end the settling. The watcher calls
/// <see cref="Loaded"/> every frame of a load and at a zone change, <see cref="Seen"/> every frame. Pure (tests).
/// </summary>
public sealed class PrepSettling
{
    /// <summary>X-774: after a loading screen or a zone change, the companion takes a few seconds to come back.</summary>
    public static readonly TimeSpan Settling=TimeSpan.FromSeconds(20);
    /// <summary>X-781: out this long without a break after the load: it is back.</summary>
    public static readonly TimeSpan Back=TimeSpan.FromSeconds(2);
    private DateTimeOffset loadedAt=DateTimeOffset.MinValue;
    private DateTimeOffset? outSince;
    private bool back;

    /// <summary>A frame of a loading screen, or a zone change.</summary>
    public void Loaded(DateTimeOffset now){loadedAt=now;back=false;}

    /// <summary>Every frame: whether the companion is out.</summary>
    public void Seen(DateTimeOffset now,bool outNow)
    {
        if(!outNow){outSince=null;return;}
        if(outSince is not { } since || since<=loadedAt)outSince=since=now;
        if(since>loadedAt && now-since>=Back)back=true;
    }

    /// <summary>Whether the load still explains the companion out of sight now.</summary>
    public bool Holds(DateTimeOffset now)=>!back && now-loadedAt<Settling;
}
