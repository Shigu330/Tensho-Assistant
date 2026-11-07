namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.740 (user report: a list panel line « blinks » between two values while gathering): remembers the last values of each line and says when one comes back
/// to the value it had just before, within a few seconds, so that the log shows which line and whether the stock or the need moved. Developer mode only.
/// </summary>
public sealed class BlinkWatch
{
    public static readonly TimeSpan Window=TimeSpan.FromSeconds(3),Quiet=TimeSpan.FromSeconds(30);
    private sealed class Seen { public (long Held,long Needed) Last,Before; public DateTimeOffset ChangedAt,ReportedAt=DateTimeOffset.MinValue; public bool HasBefore; }
    private readonly Dictionary<(string,uint),Seen> lines=[];

    /// <summary>A text for the log when this line just went back to its value before last; else null.</summary>
    public string? Observe(string list,uint item,long held,long needed,DateTimeOffset now)
    {
        if(!lines.TryGetValue((list,item),out var seen)){lines[(list,item)]=new Seen{Last=(held,needed),ChangedAt=now};return null;}
        if(seen.Last==(held,needed))return null;
        var back=seen.HasBefore && seen.Before==(held,needed) && now-seen.ChangedAt<=Window;
        var text=back && now-seen.ReportedAt>=Quiet
            ? $"List blink: {list}, item {item}: held {seen.Before.Held}->{seen.Last.Held}->{held}, needed {seen.Before.Needed}->{seen.Last.Needed}->{needed}, {(now-seen.ChangedAt).TotalMilliseconds:0} ms apart"
            :null;
        if(text is not null)seen.ReportedAt=now;
        seen.Before=seen.Last;seen.HasBefore=true;seen.Last=(held,needed);seen.ChangedAt=now;
        return text;
    }
}
