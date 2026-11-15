using System.Globalization;
using System.Runtime.InteropServices;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-433 (0.1423): the calling thread's own processor time (kernel and user, Windows GetThreadTimes, counted at the system's clock ticks, about
/// 16 ms) and its system id. A slow pass whose processor time is far below its time worked little and waited; one close to it worked.
/// </summary>
public static class ThreadClock
{
    /// <summary>The system id of the game's thread, as last seen by a reader's pass (0: not seen yet).</summary>
    public static long GameThread;
    /// <summary>X-433 (0.1431): the system id of the JIT warm-up's thread, once it ran (0: not yet).</summary>
    public static long WarmupThread;
    /// <summary>X-433 (0.1448): the system id of the readers' compiler's thread, once it ran (0: not yet).</summary>
    public static long CompilerThread;

    public static double? ProcessorMs()
    {
        if(!OperatingSystem.IsWindows())return null;
        try{return Native.GetThreadTimes(Native.GetCurrentThread(),out _,out _,out var kernel,out var user) ? (kernel+user)/10_000.0:null;}
        catch(Exception){return null;}
    }

    public static long SystemId()
    {
        if(!OperatingSystem.IsWindows())return 0;
        try{return Native.GetCurrentThreadId();}
        catch(Exception){return 0;}
    }

    /// <summary>The slow line's part: the processor time of the pass, nothing when unknown.</summary>
    public static string ProcessorText(double? before,double? after)=>
        before is { } start && after is { } end && end>=start ? string.Create(CultureInfo.InvariantCulture,$" (on the processor {end-start:0} ms)"):"";

    private static class Native
    {
        [DllImport("kernel32.dll",SetLastError=true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetThreadTimes(IntPtr thread,out long creation,out long exit,out long kernel,out long user);
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentThread();
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
    }
}

/// <summary>
/// X-433 (0.1423): the runtime's type loads (TypeLoadStart, then TypeLoadStop with the same id and the type's name): a load of
/// <see cref="ThresholdMs"/> or more gives one note with its thread, at most <see cref="MaximumNotes"/>; the time spent loading types on the
/// game's thread (nested loads counted in their outer one too). Pure: fed by <see cref="RuntimeSuspensionListener"/>, or by the tests.
/// </summary>
public sealed class TypeLoadTracker(double thresholdMs=30,int maximumNotes=40)
{
    public double ThresholdMs { get; } = thresholdMs;
    public int MaximumNotes { get; } = maximumNotes;
    private readonly object gate=new();
    private readonly Dictionary<long,(DateTime At,long Thread)> started=[];
    private int notes;
    public int Count { get; private set; }
    public double GameThreadMs { get; private set; }
    public double LongestMs { get; private set; }
    public string Longest { get; private set; } = "";

    public void Start(long id,DateTime at,long thread)
    {
        lock(gate)
        {
            if(started.Count>100_000)started.Clear();
            started[id]=(at,thread);
        }
    }

    /// <summary>The end of a load; a note when it was long, else null.</summary>
    public string? Stop(long id,DateTime at,string? name,long gameThread)
    {
        lock(gate)
        {
            if(!started.Remove(id,out var start))return null;
            var milliseconds=(at-start.At).TotalMilliseconds;
            if(milliseconds<0)return null;
            Count++;
            var onGame=gameThread!=0 && start.Thread==gameThread;
            if(onGame)GameThreadMs+=milliseconds;
            if(milliseconds>LongestMs){LongestMs=milliseconds;Longest=name ?? "?";}
            if(milliseconds<ThresholdMs || notes>=MaximumNotes)return null;
            notes++;
            return string.Create(CultureInfo.InvariantCulture,
                $"Type loaded in {milliseconds:0} ms on {(onGame ? "the game's thread":"thread "+start.Thread)} from {start.At.ToLocalTime():HH:mm:ss.fff}: {name ?? "?"}.");
        }
    }

    public string Summary()
    {
        lock(gate)
            return string.Create(CultureInfo.InvariantCulture,
                $"Type loads in the same time: {Count}, {GameThreadMs:0} ms on the game's thread (nested ones counted in their outer one too), the longest {LongestMs:0} ms: {(Longest.Length>0 ? Longest:"none")}.");
    }
}
