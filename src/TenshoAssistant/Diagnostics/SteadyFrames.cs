using System.Globalization;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-01 (0.1190, analysis of the slow frames of 12/11, « what is missing »): the steady state. The log only named the frames over 100 ms:
/// nothing said whether a part costs 5 to 20 ms on every frame. Each measured part of a frame (the same ones <see cref="SlowFrame"/>
/// receives) is added to a running total; once a minute, developer mode, one line gives per part how many times it ran, its average, a
/// bound on its 95th percentile and its worst time, the costliest first. Counters and a fixed histogram only: nothing is kept per frame.
/// The first line after a load still holds the load's first frames: read the following ones for the steady state.
/// </summary>
public sealed class SteadyFrames
{
    /// <summary>How long one line covers, in milliseconds.</summary>
    public const long WindowMs=60_000;
    /// <summary>The name under which the whole frame is added.</summary>
    public const string Frame="frame";
    /// <summary>Upper bounds of the histogram's classes, in milliseconds; a last class holds everything above.</summary>
    public static readonly IReadOnlyList<double> Edges=[0.1,0.2,0.5,1,2,3,5,8,12,16,25,33,50,75,100];

    private sealed class Part
    {
        public long Count;
        public double Sum,Max;
        public readonly int[] Classes=new int[Edges.Count+1];
    }

    private readonly Dictionary<string,Part> parts=new(StringComparer.Ordinal);
    // Timers828Tests: never from long.MinValue; -1 = no window started yet.
    private long windowStart=-1;

    /// <summary>X-264 (0.1269): the whole frame over the last finished window (a minute): how many, average, bound of the 95th percentile
    /// (null: above the last bound), worst. Null until a window with frames has finished. What the dashboard's gauge shows.</summary>
    public (long Count,double Average,double? P95,double Max)? LastFrame { get; private set; }

    public void Add(string name,double milliseconds)
    {
        if(!(milliseconds>=0))return;
        if(!parts.TryGetValue(name,out var part))parts[name]=part=new();
        part.Count++;
        part.Sum+=milliseconds;
        if(milliseconds>part.Max)part.Max=milliseconds;
        var index=0;
        while(index<Edges.Count && milliseconds>Edges[index])index++;
        part.Classes[index]++;
    }

    /// <summary>The bound under which 95 % of a part's times fall (the upper bound of its class); null when it is above the last bound.</summary>
    private static double? Percentile95(Part part)
    {
        var need=(long)Math.Ceiling(part.Count*0.95);
        long seen=0;
        for(var index=0;index<part.Classes.Length;index++)
        {
            seen+=part.Classes[index];
            if(seen>=need)return index<Edges.Count ? Edges[index]:null;
        }
        return null;
    }

    /// <summary>
    /// Null until a window has passed since the first call (or the last line); then the line of that window, and a new one starts. The whole
    /// frame first, then the <paramref name="top"/> costliest parts by total time; a window without any frame gives no line.
    /// </summary>
    public string? Take(long nowMs,int top=14)
    {
        if(windowStart<0){windowStart=nowMs;parts.Clear();return null;}
        if(nowMs-windowStart<WindowMs)return null;
        var seconds=(nowMs-windowStart)/1000.0;
        windowStart=nowMs;
        if(!parts.TryGetValue(Frame,out var frame) || frame.Count==0){parts.Clear();return null;}
        LastFrame=(frame.Count,frame.Sum/frame.Count,Percentile95(frame),frame.Max);
        static string One(string name,Part part)
        {
            var bound=Percentile95(part);
            return string.Create(CultureInfo.InvariantCulture,$"{name} {part.Count}× avg {part.Sum/part.Count:0.00} ms, p95 {(bound is { } under ? "≤ "+under.ToString("0.##",CultureInfo.InvariantCulture):"> "+Edges[^1].ToString("0",CultureInfo.InvariantCulture))} ms, max {part.Max:0.0} ms");
        }
        var line=string.Create(CultureInfo.InvariantCulture,$"Steady frames over {seconds:0} s: ")+One(Frame,frame)+"; "+
            string.Join("; ",parts.Where(pair=>pair.Key!=Frame).OrderByDescending(pair=>pair.Value.Sum).ThenBy(pair=>pair.Key,StringComparer.Ordinal).Take(top).Select(pair=>One(pair.Key,pair.Value)));
        parts.Clear();
        return line;
    }
}
