using System.Globalization;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-457 (0.1460): what the chat window's scrolling does frame by frame, to name the cause of a window that jumps (developer mode only): per
/// frame the layout width, the rows of the tab, the scroll, its limit, whether the window followed the bottom and whether the lines were laid
/// out again. <see cref="Frame"/> gives one line, once per second at most, when within one second the lines were laid out again twice or more,
/// or the rows, the scroll limit or the « at the bottom » state changed four times or more; null otherwise. Pure (tests).
/// </summary>
public sealed class ChatScrollWatch
{
    public readonly record struct Sample(double Seconds,float Width,int Rows,float Scroll,float ScrollMax,bool AtBottom,bool Relaid);
    private readonly List<Sample> samples=[];
    private double lastSaid=double.NegativeInfinity;
    public const double Window=1.0;
    public const int RelayoutsSaid=2,ChangesSaid=4,Shown=8;

    /// <summary>One frame; the line to log when the last second looks like a jump.</summary>
    public string? Frame(Sample sample)
    {
        samples.Add(sample);
        samples.RemoveAll(old=>old.Seconds<sample.Seconds-Window);
        if(sample.Seconds-lastSaid<Window || samples.Count<2)return null;
        var relaid=samples.Count(old=>old.Relaid);
        int rows=0,limits=0,bottoms=0;
        for(var index=1;index<samples.Count;index++)
        {
            if(samples[index].Rows!=samples[index-1].Rows)rows++;
            if(Math.Abs(samples[index].ScrollMax-samples[index-1].ScrollMax)>0.5f)limits++;
            if(samples[index].AtBottom!=samples[index-1].AtBottom)bottoms++;
        }
        if(relaid<RelayoutsSaid && rows<ChangesSaid && limits<ChangesSaid && bottoms<ChangesSaid)return null;
        lastSaid=sample.Seconds;
        var widths=string.Join("/",samples.Select(old=>old.Width.ToString("0",CultureInfo.InvariantCulture)).Distinct());
        var last=samples.TakeLast(Shown).Select(old=>string.Create(CultureInfo.InvariantCulture,
            $"{old.Width:0} w, {old.Rows} rows, scroll {old.Scroll:0}/{old.ScrollMax:0}{(old.AtBottom ? " bottom":"")}{(old.Relaid ? " relaid":"")}"));
        return string.Create(CultureInfo.InvariantCulture,
            $"Chat window jumps: in {samples.Count} frames of the last second, laid out again {relaid} times (widths {widths}), rows changed {rows} times, scroll limit {limits} times, « at the bottom » {bottoms} times; last frames: {string.Join("; ",last)}.");
    }
}
