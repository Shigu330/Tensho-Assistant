using System.Diagnostics.Tracing;
using System.Globalization;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-433 (0.1422): the runtime's suspensions of every managed thread, as its own events tell them (GCSuspendEEBegin_V1 with its reason, then
/// GCRestartEEEnd_V1): their count and time, and the time of those not made for a collection (GC.GetTotalPauseDuration counts the collections
/// only); and the methods tiered compilation recompiled in the background. A suspension of <see cref="ThresholdMs"/> or more gives one note,
/// at most <see cref="MaximumNotes"/>. Pure: fed by <see cref="RuntimeSuspensionListener"/>, or by the tests.
/// </summary>
public sealed class SuspensionTracker(double thresholdMs=20,int maximumNotes=30)
{
    public double ThresholdMs { get; } = thresholdMs;
    public int MaximumNotes { get; } = maximumNotes;
    private readonly object gate=new();
    private DateTime? begun;
    private uint reason;
    private int notes;
    public int Count { get; private set; }
    public double TotalMs { get; private set; }
    public double OtherMs { get; private set; }
    public double LongestMs { get; private set; }
    public long TieredMethods { get; private set; }

    /// <summary>One event of the runtime; a note when it ends a long suspension, else null.</summary>
    public string? See(string? name,DateTime at,uint? reasonValue=null,long? jitted=null)
    {
        lock(gate)
        {
            switch(name)
            {
                case "GCSuspendEEBegin_V1":
                    begun=at;reason=reasonValue ?? 0;
                    return null;
                case "GCRestartEEEnd_V1":
                {
                    if(begun is not { } start)return null;
                    begun=null;
                    var milliseconds=(at-start).TotalMilliseconds;
                    if(milliseconds<0)return null;
                    Count++;TotalMs+=milliseconds;LongestMs=Math.Max(LongestMs,milliseconds);
                    if(!ForCollection(reason))OtherMs+=milliseconds;
                    if(milliseconds<ThresholdMs || notes>=MaximumNotes)return null;
                    notes++;
                    return string.Create(CultureInfo.InvariantCulture,
                        $"Runtime suspended every managed thread {milliseconds:0} ms ({ReasonName(reason)}) from {start.ToLocalTime():HH:mm:ss.fff}.");
                }
                case "TieredCompilationBackgroundJitStop":
                    TieredMethods+=Math.Max(0,jitted ?? 0);
                    return null;
                default:
                    return null;
            }
        }
    }

    /// <summary>The summary line, once the watch ends.</summary>
    public string Summary(TimeSpan watched)
    {
        lock(gate)
            return string.Create(CultureInfo.InvariantCulture,
                $"Runtime suspensions in the {watched.TotalSeconds:0} s after the load: {Count}, {TotalMs:0} ms in all, {OtherMs:0} ms not for a collection, the longest {LongestMs:0} ms; tiered compilation recompiled {TieredMethods} methods in the background.");
    }

    /// <summary>The runtime's suspension reasons (SUSPEND_*): 1 and 6 are for a collection.</summary>
    public static bool ForCollection(uint reason)=>reason is 1 or 6;
    public static string ReasonName(uint reason)=>reason switch
    {
        0=>"not for a collection: tiered compilation, code patching or another runtime task",
        1=>"for a collection",
        2=>"for an app domain shutdown",
        3=>"for code pitching",
        4=>"for the shutdown",
        5=>"for a debugger",
        6=>"to prepare a collection",
        7=>"for a debugger sweep",
        _=>"reason "+reason.ToString(CultureInfo.InvariantCulture),
    };
}

/// <summary>X-433 (0.1422): how long the listener watches after a load, and its end (false when the unload already ended it).</summary>
public static class RuntimeSuspensionWatch
{
    public static readonly TimeSpan Length=TimeSpan.FromMinutes(2);
    public static bool End(RuntimeSuspensionListener listener)
    {
        if(listener.IsDisposed)return false;
        listener.Dispose();
        return true;
    }
}

/// <summary>
/// X-433 (0.1422): listens to the runtime's own events in this process (Microsoft-Windows-DotNETRuntime: the GC keyword for the suspensions,
/// the tiered compilation keyword for its background work, informational level) and gives each long suspension to <paramref name="say"/>.
/// Developer mode only, for a short watch after a load (Plugin): disposed at its end and at the unload.
/// </summary>
public sealed class RuntimeSuspensionListener : EventListener
{
    public const string RuntimeSource="Microsoft-Windows-DotNETRuntime";
    public const long GcKeyword=0x1,TieredCompilationKeyword=0x400000000,TypeDiagnosticKeyword=0x8000000000;
    // Field initialisers run before the base constructor, which may already enable the runtime's source.
    private readonly SuspensionTracker tracker=new();
    // X-433 (0.1423): the type loads too (TypeDiagnostic keyword).
    private readonly TypeLoadTracker types=new();
    private volatile Action<string>? say;
    private int disposed;

    public RuntimeSuspensionListener(Action<string> say)=>this.say=say;

    public SuspensionTracker Tracker=>tracker;
    public TypeLoadTracker Types=>types;
    public bool IsDisposed=>Volatile.Read(ref disposed)!=0;

    protected override void OnEventSourceCreated(EventSource source)
    {
        if(source.Name==RuntimeSource)EnableEvents(source,EventLevel.Informational,(EventKeywords)(GcKeyword|TieredCompilationKeyword|TypeDiagnosticKeyword));
    }

    protected override void OnEventWritten(EventWrittenEventArgs written)
    {
        if(Volatile.Read(ref disposed)!=0)return;
        if(written.EventName=="TypeLoadStart")
        {
            if(Number(written,"TypeLoadStartID") is { } id)types.Start(id,written.TimeStamp.ToUniversalTime(),written.OSThreadId);
            return;
        }
        if(written.EventName=="TypeLoadStop")
        {
            var name=written.PayloadNames?.IndexOf("TypeName") is int at && at>=0 && written.Payload is { } values && at<values.Count ? values[at] as string:null;
            if(Number(written,"TypeLoadStartID") is { } id
                && types.Stop(id,written.TimeStamp.ToUniversalTime(),name,Interlocked.Read(ref ThreadClock.GameThread)) is { } typeNote)say?.Invoke(typeNote);
            return;
        }
        uint? reason=null;long? jitted=null;
        if(written.EventName=="GCSuspendEEBegin_V1")reason=Number(written,"Reason") is { } value ? (uint)value:null;
        else if(written.EventName=="TieredCompilationBackgroundJitStop")jitted=Number(written,"JittedMethodCount");
        if(tracker.See(written.EventName,written.TimeStamp.ToUniversalTime(),reason,jitted) is { } note)say?.Invoke(note);
    }

    private static long? Number(EventWrittenEventArgs written,string name)
    {
        var index=written.PayloadNames?.IndexOf(name) ?? -1;
        if(index<0 || written.Payload is not { } payload || index>=payload.Count)return null;
        return payload[index] switch { uint value=>value, int value=>value, ulong value=>(long)value, long value=>value, ushort value=>value, _=>null };
    }

    public override void Dispose()
    {
        if(Interlocked.Exchange(ref disposed,1)!=0)return;
        say=null;
        base.Dispose();
    }
}
