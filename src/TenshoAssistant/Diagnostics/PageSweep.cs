namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.724: the order of the page sweep of the self-check: every page is drawn a few times, spaced out, to start what it loads; then a second, short pass draws it
/// with what has been loaded. What is kept: the first draw, the worst settled draw, the first error. Numbers only, as in the rest of the self-check.
/// </summary>
public sealed class PageSweep
{
    public const int FramesBetweenDraws=10,FirstPassDraws=20,SecondPassDraws=3;
    /// <summary>One frame at 60 images a second: a settled page that costs this much alone is worth looking at.</summary>
    public const double SlowMs=16;
    private sealed class Seen { public int Draws; public double First=-1,Settled; public string? Error; }
    private readonly IReadOnlyList<string> pages;
    private readonly Seen[] seen;
    private int pass,index,drawsHere,frames;

    public PageSweep(IReadOnlyList<string> pages)
    {
        this.pages=pages;
        seen=pages.Select(_=>new Seen()).ToArray();
    }

    public bool Done=>pages.Count==0 || pass>=2;
    public string? Current=>Done ? null:pages[index];

    /// <summary>Called at each frame that could draw: true when this one draws the current page.</summary>
    public bool Due()
    {
        if(Done)return false;
        frames++;
        if(frames<FramesBetweenDraws)return false;
        frames=0;
        return true;
    }

    public void Drawn(double milliseconds,string? error)
    {
        if(Done)return;
        var page=seen[index];
        page.Draws++;
        if(page.First<0)page.First=milliseconds;
        if(pass==1)page.Settled=Math.Max(page.Settled,milliseconds);
        page.Error??=error;
        if(++drawsHere<(pass==0 ? FirstPassDraws:SecondPassDraws))return;
        drawsHere=0;
        if(++index<pages.Count)return;
        index=0;
        pass++;
    }

    public IReadOnlyList<SelfCheckLine> Lines=>pages.Select((name,position)=>Line(name,seen[position])).ToArray();

    private static SelfCheckLine Line(string name,Seen page)
    {
        var domain="draw "+name;
        var text=FormattableString.Invariant($"{page.Draws} draws, first {page.First:F1} ms, settled {page.Settled:F1} ms");
        return page.Error is { } error ? SelfCheck.Failed(domain,error)
            : page.Settled>=SlowMs ? SelfCheck.Suspect(domain,text+", slow")
            : SelfCheck.Ok(domain,text);
    }

    public static string Summary(IReadOnlyList<SelfCheckLine> lines,int skipped,int requests,string hosts)=>
        $"Selftest pages done: {lines.Count(line=>line.Status==SelfCheckStatus.Ok)} ok, {lines.Count(line=>line.Status==SelfCheckStatus.Suspect)} slow, "
        +$"{lines.Count(line=>line.Status==SelfCheckStatus.Failed)} failed, {skipped} not drawn; community requests meanwhile: {requests}{(requests>0 ? " ("+hosts+")":"")}.";
}
