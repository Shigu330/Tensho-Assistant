using System.Globalization;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-433 (0.1433): the gaps between two of the game's frames (one call of the plugin interface's Draw each), whatever ran on its thread
/// meanwhile (a reader, Dalamud, the game, a stop of every managed thread by a collection): the longest kept with the time each ended, and a
/// note for each gap of <see cref="NoteMs"/> or more (at most <see cref="MaximumNotes"/>), to compare with the collections' lines. Developer
/// mode, two minutes after a load (Plugin). Dalamud skips Draw while the plugin interface is hidden (a cutscene, gpose, the interface hidden
/// by the player): <see cref="Hidden"/> (the interface's HideUi, 0.1437, X-440) ends the current run, so such a period is not read as a
/// frame gap. Pure: fed with a clock in milliseconds and the time of day, or by the tests.
/// </summary>
public sealed class FrameGaps(double noteMs=150,int kept=5,int maximumNotes=20)
{
    public double NoteMs { get; } = noteMs;
    public int Kept { get; } = kept;
    public int MaximumNotes { get; } = maximumNotes;
    private readonly object gate=new();
    private double last=-1;
    private int notes;
    private readonly List<(double Milliseconds,DateTimeOffset Ended)> longest=[];
    public int Frames { get; private set; }
    public int Long { get; private set; }
    public int HiddenPeriods { get; private set; }

    /// <summary>The plugin interface is hidden (no Draw until it shows again): the next frame starts a new run, without a gap.</summary>
    public void Hidden()
    {
        lock(gate)
        {
            if(last>=0)HiddenPeriods++;
            last=-1;
        }
    }

    /// <summary>One frame of the game, at this point of a steady clock (ms) and of the day; a note when the gap before it was long.</summary>
    public string? Frame(double atMs,DateTimeOffset now)
    {
        lock(gate)
        {
            Frames++;
            var previous=last;
            last=atMs;
            if(previous<0)return null;   // the first frame, or the first after the interface was hidden
            var gap=atMs-previous;
            if(gap<=0)return null;
            if(longest.Count<Kept || gap>longest[^1].Milliseconds)
            {
                longest.Add((gap,now));
                longest.Sort((left,right)=>right.Milliseconds.CompareTo(left.Milliseconds));
                if(longest.Count>Kept)longest.RemoveAt(longest.Count-1);
            }
            if(gap<NoteMs)return null;
            Long++;
            if(notes>=MaximumNotes)return null;
            notes++;
            return string.Create(CultureInfo.InvariantCulture,$"Game frame gap: {gap:0} ms without a frame, ended {now.ToLocalTime():HH:mm:ss.fff}.");
        }
    }

    public string Summary(TimeSpan watched)
    {
        lock(gate)
            return string.Create(CultureInfo.InvariantCulture,
                $"Game frames in the {watched.TotalSeconds:0} s after the load: {Frames}, {Long} gaps of {NoteMs:0} ms or more; the longest: {(longest.Count==0 ? "none":string.Join(", ",longest.Select(gap=>string.Create(CultureInfo.InvariantCulture,$"{gap.Milliseconds:0} ms (ended {gap.Ended.ToLocalTime():HH:mm:ss.fff})"))))}{(HiddenPeriods==0 ? "":string.Create(CultureInfo.InvariantCulture,$"; {HiddenPeriods} period(s) with the plugin interface hidden, not counted"))}.");
    }
}
