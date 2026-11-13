namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-177 C6 (0.1204, dalamud.log of 13/11: « Submarine voyage check » 606 lines, « Reading eventMarkers » 655, « Map event marker » 357,
/// the same text written again at each reload of the plugin): what a research probe wrote is remembered for the game process, across the
/// plugin's reloads, so the same text is written whole once per game session; after that its caller writes a short line, which keeps the
/// start the log reader looks for (scripts/agent/logread.py). The memory is a set of the texts' hashes (integers, a type of the runtime,
/// never one of the plugin's: an unloaded instance keeps nothing alive through it), kept in the process's data slot; bounded.
/// </summary>
public static class SessionOnce
{
    /// <summary>The process data slot (AppDomain data) holding the set.</summary>
    public const string Slot="TenshoAssistant.SessionOnce.v1";
    /// <summary>At most this many texts remembered; past it the memory starts again (a text may then be written whole once more).</summary>
    public const int Limit=4096;
    private static readonly object gate=new();

    /// <summary>True the first time this text is given in this game process (across reloads of the plugin), false afterwards.</summary>
    public static bool First(string text)=>First(text,static ()=>AppDomain.CurrentDomain.GetData(Slot),static value=>AppDomain.CurrentDomain.SetData(Slot,value));

    /// <summary>The same, on a given slot (tests: two « loads » sharing one).</summary>
    public static bool First(string text,Func<object?> read,Action<object> write)
    {
        ArgumentNullException.ThrowIfNull(text);
        HashSet<int> seen;
        lock(gate)
        {
            if(read() is HashSet<int> kept)seen=kept;
            else{seen=[];write(seen);}
        }
        lock(seen)
        {
            if(seen.Count>=Limit)seen.Clear();
            return seen.Add(text.GetHashCode(StringComparison.Ordinal));
        }
    }
}
