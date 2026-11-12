namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-177 (0.1162, master's finding of 12/11: Dalamud's « Long …CompetitiveGate…Guarded » hitches at each zone change, without saying which
/// reader): when a slow piece of work is named in the log. Over its threshold, a name is written at once the first time, then at most once
/// per <see cref="Every"/>, with the worst time seen since its last line, so a reader slow at each zone change is seen again (before, a reader was named
/// once per load). At most 256 names are remembered (then the memory starts again). Thread-safe.
/// </summary>
public sealed class SlowNotes(TimeSpan every,double thresholdMs=50)
{
    private const int MaxNames=256;
    private readonly object gate=new();
    private readonly Dictionary<string,(long WrittenAt,double Worst)> seen=new(StringComparer.Ordinal);

    public TimeSpan Every=>every;
    public double ThresholdMs=>thresholdMs;

    /// <summary>The worst time of <paramref name="name"/> so far when it is to be written now; null when it is under the threshold or was written
    /// less than <see cref="Every"/> ago (its worst time is kept for the next line).</summary>
    public double? Note(string name,double spentMs,long nowMs)
    {
        if(spentMs<=thresholdMs)return null;
        lock(gate)
        {
            if(seen.TryGetValue(name,out var known))
            {
                var worst=Math.Max(known.Worst,spentMs);
                if(nowMs-known.WrittenAt<every.TotalMilliseconds){seen[name]=(known.WrittenAt,worst);return null;}
                seen[name]=(nowMs,0);
                return worst;
            }
            if(seen.Count>=MaxNames)seen.Clear();
            seen[name]=(nowMs,0);
            return spentMs;
        }
    }
}
