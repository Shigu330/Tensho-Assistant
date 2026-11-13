namespace TenshoAssistant.UI;

/// <summary>
/// X-155 (0.1260): the little movement Tensho allows itself: a brief highlight tied to one event (a period that changes, a save that is
/// confirmed), over in a fraction of a second and never needed to read the state. With « reduced motion » (Dalamud's preference; the
/// desktop application, which has no such preference, counts as reduced) there is none: the final state shows at once. Pure.
/// </summary>
public static class Motion
{
    /// <summary>
    /// The strength of a highlight started at <paramref name="startedMs"/>: 1 at the start, falling to 0 at the end of
    /// <paramref name="durationMs"/>; 0 before the start, after the end, without a duration, or with reduced motion.
    /// Times are Environment.TickCount64 values, compared by difference.
    /// </summary>
    public static float Pulse(long startedMs,long nowMs,int durationMs,bool reduced)
    {
        if(reduced || durationMs<=0)return 0;
        var elapsed=nowMs-startedMs;
        if(elapsed<0 || elapsed>=durationMs)return 0;
        return 1-(float)elapsed/durationMs;
    }

    /// <summary>A countdown as « 04:52 » (minutes and seconds), « 1:04:52 » from one hour; rounded up to the second, « 00:00 » once passed.</summary>
    public static string Countdown(TimeSpan left)
    {
        var seconds=left<=TimeSpan.Zero ? 0:(long)Math.Ceiling(left.TotalSeconds);
        var hours=seconds/3600;var minutes=seconds/60%60;var rest=seconds%60;
        return hours>0 ? string.Create(System.Globalization.CultureInfo.InvariantCulture,$"{hours}:{minutes:00}:{rest:00}")
            : string.Create(System.Globalization.CultureInfo.InvariantCulture,$"{minutes:00}:{rest:00}");
    }
}
