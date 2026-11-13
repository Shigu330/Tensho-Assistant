namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-264 (0.1269, user request of 2026-11-13): one point of the dashboard's history: measures Tensho already takes, copied as they are.
/// A value not known is null, never 0 (R08): the frame's times are only measured in developer mode. Counters are totals of the session
/// (their rate is worked out between two points).
/// </summary>
public readonly record struct HealthSample(long AtMs,HealthLevel Level,double? HeapMb,double? PageMs,double? FrameAvgMs,double? FrameP95Ms,
    int SlowFrames,int Requests,int WritesFailed,int PanelsOff,int Unavailable);

/// <summary>X-264 (0.1269): the fixed tops of the dashboard's scales: a frame at 60 images a second (the page and frame gauges).</summary>
public static class HealthHistoryScales
{
    public const double FrameMs=1000.0/60;
}

/// <summary>
/// X-264 (0.1269): the dashboard's history, a ring of the last hour: one point every ten seconds, 360 points, about 30 KB, never more.
/// Used on one thread (the draw thread in game; the application's own thread on the desktop).
/// </summary>
public sealed class HealthHistory
{
    public const int Capacity=360;
    public const long StepMs=10_000;

    private readonly HealthSample[] ring=new HealthSample[Capacity];
    private int next,count;

    public int Count=>count;
    /// <summary>The point at <paramref name="index"/>, 0 being the oldest kept.</summary>
    public HealthSample this[int index]=>index<0 || index>=count ? throw new ArgumentOutOfRangeException(nameof(index)):ring[(next-count+index+2*Capacity)%Capacity];
    public HealthSample? Last=>count==0 ? null:this[count-1];
    /// <summary>X-264 (0.1270): the time the kept points cover, in milliseconds (0 with fewer than two points).</summary>
    public long SpanMs=>count<2 ? 0:this[count-1].AtMs-this[0].AtMs;

    /// <summary>Whether a new point is due: none yet, ten seconds since the last one, or a clock that went back (kept points are then dropped by Add).</summary>
    public bool Due(long nowMs)=>Last is not { } last || nowMs-last.AtMs>=StepMs || nowMs<last.AtMs;

    /// <summary>Adds the point when one is due; a point older than the last one (clock gone back) starts the history again. False: not due, nothing kept.</summary>
    public bool Add(HealthSample sample)
    {
        if(!Due(sample.AtMs))return false;
        if(Last is { } last && sample.AtMs<last.AtMs){next=0;count=0;}
        ring[next]=sample;next=(next+1)%Capacity;count=Math.Min(count+1,Capacity);
        return true;
    }

    /// <summary>One value per point, oldest first; null where it was not known.</summary>
    public double?[] Series(Func<HealthSample,double?> pick)
    {
        var values=new double?[count];
        for(var index=0;index<count;index++)values[index]=pick(this[index]);
        return values;
    }

    /// <summary>
    /// A session counter as a rate per minute, one value per point: what was added since the point before, over the time between them.
    /// Null for the first point and where the counter went down (another session: nothing to compare).
    /// </summary>
    public double?[] PerMinute(Func<HealthSample,int> counter)
    {
        var values=new double?[count];
        for(var index=1;index<count;index++)values[index]=Rate(this[index-1],this[index],counter);
        return values;
    }

    /// <summary>The same rate over the last <paramref name="steps"/> points (a minute by default); null with fewer than two points.</summary>
    public double? LastPerMinute(Func<HealthSample,int> counter,int steps=6)
        =>count<2 ? null:Rate(this[Math.Max(0,count-1-Math.Max(1,steps))],this[count-1],counter);

    private static double? Rate(HealthSample from,HealthSample to,Func<HealthSample,int> counter)
    {
        var added=counter(to)-counter(from);var elapsed=to.AtMs-from.AtMs;
        return added<0 || elapsed<=0 ? null:added*60_000.0/elapsed;
    }
}
