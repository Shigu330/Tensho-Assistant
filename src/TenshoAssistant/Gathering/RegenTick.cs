namespace TenshoAssistant.Gathering;

/// <summary>What the GP tick bar shows: GP now and at most, and where the 3-second regeneration cycle is (null = no tick seen yet).</summary>
/// <param name="Stalled">0.484: ticks were seen, and GP has not risen for longer than a tick, as when a gathering window is open.</param>
/// <param name="Gain">0.488: what the last tick gave: a rise one tick after the one before; 0 before two of them are seen.</param>
public sealed record RegenTickState(uint Gp,uint MaxGp,double? Progress,double? SecondsLeft,bool Stalled=false,int Gain=0,uint JobId=0);

/// <summary>
/// 0.488 (user request): what a tick gives depends on the traits a job has unlocked: Enhanced GP Regeneration raises it by 1, then 2, then 3 in all, job by job (Trait sheet,
/// docs/evidence/0.488-gp-regen-traits.md). A measure on one job gives the base, and the rate of each job follows from what it has unlocked.
/// </summary>
public static class GpRegen
{
    /// <summary>The job's three traits, in order; a job that gathers nothing has none.</summary>
    public static IReadOnlyList<uint> Traits(uint job)=>job switch{16=>[192,236,239],17=>[193,237,240],18=>[194,238,241],_=>[]};
    /// <summary>What the traits add: the total said by the highest one unlocked.</summary>
    public static int Bonus(IEnumerable<uint> unlocked,uint job)
    {
        var traits=Traits(job);var bonus=0;
        foreach(var trait in unlocked){var index=traits.ToList().IndexOf(trait);if(index>=0)bonus=Math.Max(bonus,index+1);}
        return bonus;
    }
    public static int Base(int gain,int bonus)=>gain-bonus;
    /// <summary>What a tick gives on a job: the setting if there is one, otherwise the measured base and its traits; 0 before any measure.</summary>
    public static int Rate(int setting,int measuredBase,int bonus)=>setting>0 ? setting:measuredBase>0 ? measuredBase+bonus:0;
}

/// <summary>
/// g, R-08 (0.403): says once when a gatherer's GP rises to the threshold (0 = the maximum); armed again once GP falls below it.
/// The first read only sets the starting point (logging in with full GP is not an alert).
/// </summary>
public sealed class GpAlertTracker
{
    private uint? last;
    private bool away;
    private uint? fellTo;
    public void Reset(){last=null;away=false;fellTo=null;}
    /// <summary>
    /// 0.657 (docs/evidence/0.656-gp-alert-teleport.md): the game takes GP at a change of zone, 809 to 772 in the reading. GP at the target before it does not ring when it climbs back.
    /// A further fall means GP was spent: the alert follows as usual.
    /// </summary>
    public void Away()=>away=true;
    public static uint Target(uint maxGp,int threshold)=>threshold<=0 ? maxGp:Math.Min((uint)threshold,maxGp);
    public bool Evaluate(uint gp,uint maxGp,int threshold)
    {
        var target=Target(maxGp,threshold);
        if(away)
        {
            away=false;
            if(last is { } known && known>=target && gp<target)fellTo=gp;
        }
        if(fellTo is { } fell)
        {
            if(gp>=target)fellTo=null;
            else if(gp>=fell){fellTo=gp;return false;}
            else fellTo=null;
            last=gp;
            return false;
        }
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
        if(last is { } before && gp>before && before<(lastMax ?? maxGp))
        {
            // 0.488 (in-game report: « full in 1:22 » for 56 GP): only a rise a tick after the previous one tells what a tick gives; the bar had kept the smallest rise ever seen, 2.
            if(tick is { } previous && Math.Abs((now-previous).TotalSeconds-Period.TotalSeconds)<=0.6)gain=(int)Math.Min(gp-before,int.MaxValue);
            tick=now;
        }
        last=gp;lastMax=maxGp;
        if(tick is not { } at || now<at)return new(gp,maxGp,null,null,false,gain);
        if(now-at>TimeSpan.FromMinutes(1))return new(gp,maxGp,null,null,gp<maxGp,gain);
        if(gp<maxGp && now-at>StallAfter)return new(gp,maxGp,null,null,true,gain);
        var into=(now-at).TotalSeconds%Period.TotalSeconds;
        return new(gp,maxGp,into/Period.TotalSeconds,Period.TotalSeconds-into,false,gain);
    }
}
