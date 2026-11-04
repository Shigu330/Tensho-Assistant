namespace TenshoAssistant.Gathering;

/// <summary>What the GP tick bar shows: GP now and at most, and where the 3-second regeneration cycle is (null = no tick seen yet).</summary>
/// <param name="Stalled">0.484: ticks were seen, and GP has not risen for longer than a tick, as when a gathering window is open.</param>
/// <param name="Gain">0.484: the last natural-looking rise of GP, to know what a tick gives; 0 before the first.</param>
public sealed record RegenTickState(uint Gp,uint MaxGp,double? Progress,double? SecondsLeft,bool Stalled=false,int Gain=0);

/// <summary>
/// g, R-08 (0.403): says once when a gatherer's GP rises to the threshold (0 = the maximum); armed again once GP falls below it.
/// The first read only sets the starting point (logging in with full GP is not an alert).
/// </summary>
public sealed class GpAlertTracker
{
    private uint? last;
    public void Reset()=>last=null;
    public static uint Target(uint maxGp,int threshold)=>threshold<=0 ? maxGp:Math.Min((uint)threshold,maxGp);
    public bool Evaluate(uint gp,uint maxGp,int threshold)
    {
        var target=Target(maxGp,threshold);
        var reached=last is { } before && before<target && gp>=target && maxGp>0;
        last=gp;
        return reached;
    }
}

/// <summary>
/// g, X-06 (0.391): the regeneration tick of a gatherer's GP (ideas from Tick Tracker, AGPL, read only; original code). The game adds GP
/// every 3 seconds; a rise of GP while it was not full is taken as a tick, and the bar runs from it. A rise from a potion or an action
/// can be taken for a tick: the next natural one puts the bar right again. A tick older than a minute is forgotten.
/// </summary>
public sealed class RegenTickTracker
{
    public static readonly TimeSpan Period=TimeSpan.FromSeconds(3);
    /// <summary>0.486 (user request): the time until GP is full, at what a tick gives: null while it is not known, or paused.</summary>
    public static TimeSpan? ToFull(RegenTickState state,int gainPerTick)
    {
        if(gainPerTick<=0 || state.Stalled || state.SecondsLeft is not { } left)return null;
        if(state.Gp>=state.MaxGp)return TimeSpan.Zero;
        var ticks=(long)Math.Ceiling((state.MaxGp-state.Gp)/(double)gainPerTick);
        return TimeSpan.FromSeconds(left+(ticks-1)*Period.TotalSeconds);
    }
    private uint? last,lastMax;
    private DateTimeOffset? tick;
    private int gain;
    /// <summary>0.484 (user report: the bar ran on while GP had stopped, at a gathering window): beyond this without a rise, no tick is promised.</summary>
    public static readonly TimeSpan StallAfter=TimeSpan.FromSeconds(4.5);

    public void Reset(){last=null;lastMax=null;tick=null;gain=0;}

    public RegenTickState Observe(uint gp,uint maxGp,DateTimeOffset now)
    {
        // Full means full against the maximum of the previous read: a higher maximum (gear changed) at full GP is not a tick.
        if(last is { } before && gp>before && before<(lastMax ?? maxGp)){tick=now;gain=(int)Math.Min(gp-before,int.MaxValue);}
        last=gp;lastMax=maxGp;
        if(tick is not { } at || now<at)return new(gp,maxGp,null,null,false,gain);
        if(now-at>TimeSpan.FromMinutes(1))return new(gp,maxGp,null,null,gp<maxGp,gain);
        if(gp<maxGp && now-at>StallAfter)return new(gp,maxGp,null,null,true,gain);
        var into=(now-at).TotalSeconds%Period.TotalSeconds;
        return new(gp,maxGp,into/Period.TotalSeconds,Period.TotalSeconds-into,false,gain);
    }
}
