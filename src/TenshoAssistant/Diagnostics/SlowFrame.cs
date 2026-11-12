namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.745 (user report: Tensho feels heavy): the parts of one frame of Tensho's drawing, and a line for the log when the frame is slow. dalamud.log showed frames of
/// 120 to 230 ms every few minutes, and one of 2.6 s, without saying which part. Developer mode only.
/// </summary>
public sealed class SlowFrame
{
    public const double Limit=100;
    private readonly List<(string Name,double Ms)> parts=[];
    private int frame=-1;
    /// <summary>X-01 (0.1190): the steady state: every part added here also goes to the running totals (one line a minute, developer mode).</summary>
    public SteadyFrames Steady { get; }=new();

    public void Add(int currentFrame,string name,double milliseconds)
    {
        if(currentFrame!=frame){frame=currentFrame;parts.Clear();}
        parts.Add((name,milliseconds));
        Steady.Add(name,milliseconds);
    }

    /// <summary>Null under the limit; else the frame's cost, its four costliest parts, what is left, and the runtime's pauses meanwhile.</summary>
    public string? Report(int currentFrame,double total,double gcPauseMs)
    {
        if(total<Limit)return null;
        var mine=currentFrame==frame ? parts:[];
        // 0.801 (X-01): ten parts, not four; the four were the nested wholes (all windows, Map, map overlay layout, map canvas) and hid the 0.791 checkpoints.
        // 0.806 (X-01): every part of 1 ms or more (24 at most): ten were still filled by the nested wholes and hid half of the map's checkpoints (20:08:14).
        var top=mine.OrderByDescending(part=>part.Ms).Where(part=>part.Ms>=1).Take(24).ToArray();
        var rest=Math.Max(0,total-mine.Sum(part=>part.Ms));
        return FormattableString.Invariant($"Slow frame: {total:0} ms; ")+string.Join(", ",top.Select(part=>FormattableString.Invariant($"{part.Name} {part.Ms:0} ms")))
            +FormattableString.Invariant($"{(top.Length>0 ? ", ":"")}not measured {rest:0} ms; runtime pauses {gcPauseMs:0} ms");
    }
}
