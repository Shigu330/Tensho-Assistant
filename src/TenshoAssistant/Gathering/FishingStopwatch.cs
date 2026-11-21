namespace TenshoAssistant.Gathering;

/// <summary>C10 (0.1011): the stopwatch of the fishing help and of ocean fishing restarts at each cast.</summary>
public static class FishingStopwatch
{
    /// <summary>A cast newer than the stopwatch's start restarts it; an older one (before fishing began, or before a manual restart) or none does not.</summary>
    public static DateTimeOffset? FromCast(DateTimeOffset? since,DateTimeOffset? cast)=>cast is { } at && (since is null || at>since) ? at:since;

    /// <summary>
    /// X-701 (0.1685, user request of 21/11: « the stopwatch must end at the bite, else the right fish is not seen »): the stopwatch is frozen
    /// at the bite of the cast it measures, a bite read from the game (FishingLine); a bite before its start (a manual restart since) does not freeze it.
    /// </summary>
    public static bool Frozen(DateTimeOffset? since,DateTimeOffset? bite)=>since is { } start && bite is { } at && at>=start;

    /// <summary>X-701: the seconds the stopwatch shows: up to the bite once frozen, else up to now; null when it is not running.</summary>
    public static double? Elapsed(DateTimeOffset? since,DateTimeOffset? bite,DateTimeOffset now)
    {
        if(since is not { } start)return null;
        var end=Frozen(since,bite) ? bite!.Value:now;
        return Math.Max(0,(end-start).TotalSeconds);
    }

    /// <summary>
    /// X-702 (0.1685, user request of 21/11): under Chum (status 763, « Amorçage ») the bite times are halved. The game says only that it
    /// « reduces the amount of time for the next fish to bite » (ActionTransient 4104), the source (Teamcraft's bite_time_per_fish_per_spot)
    /// gives no time under Chum: the half is the user's rule (docs/evidence/0.1685-fishing-bite-stopwatch.md).
    /// </summary>
    public const double ChumFactor=0.5;

    /// <summary>The seconds a bite window of floored seconds <paramref name="minimum"/>–<paramref name="maximum"/> spans: [minimum, maximum + 1), halved under Chum.</summary>
    public static (double From,double To) Window(int minimum,int maximum,bool chum)
    {
        var factor=chum ? ChumFactor:1;
        return (minimum*factor,(maximum+1)*factor);
    }

    /// <summary>The stopwatch is in the window (green, and the fish put forward once frozen at the bite).</summary>
    public static bool Inside(double elapsed,int minimum,int maximum,bool chum)=>Window(minimum,maximum,chum) is var (from,to) && elapsed>=from && elapsed<to;

    /// <summary>The stopwatch has passed the window (red).</summary>
    public static bool Passed(double elapsed,int maximum,bool chum)=>elapsed>=Window(0,maximum,chum).To;

    /// <summary>A number of seconds of a bite time as shown: whole, or with its half second under Chum.</summary>
    public static double Shown(int seconds,bool chum)=>chum ? seconds*ChumFactor:seconds;
}
