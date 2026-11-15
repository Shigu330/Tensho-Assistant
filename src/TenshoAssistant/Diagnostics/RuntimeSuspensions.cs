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

/// <summary>
/// X-433 (0.1430): the runtime's collections, from its own events (GCStart_V2 with its generation, kind and reason, then GCEnd_V1 with the
/// same count): each collection of generation 2 gives one note with its kind (background, blocking), when it began and ended and how many other
/// collections ended meanwhile (at most <see cref="MaximumNotes"/>); the others are counted. Pure: fed by
/// <see cref="RuntimeSuspensionListener"/>, or by the tests.
/// </summary>
public sealed class CollectionTracker(int maximumNotes=20,double shortNoteMs=100)
{
    public int MaximumNotes { get; } = maximumNotes;
    /// <summary>X-433 (0.1435): a collection of generation 0 or 1 this long or longer gives a note too (one of 432 ms froze the game 513 ms).</summary>
    public double ShortNoteMs { get; } = shortNoteMs;
    /// <summary>At most this many collections begun and not ended are kept (an end missed never grows the table).</summary>
    public const int OpenCap=64;
    private readonly object gate=new();
    private readonly Dictionary<long,(DateTime At,long Depth,long Kind,long Reason,int EndedBefore)> open=[];
    private int notes;
    private long backgroundCount=-1;
    /// <summary>X-433 (0.1431): when the background collection running now began (none: null).</summary>
    public DateTime? BackgroundSince { get{lock(gate)return backgroundSince;} }
    private DateTime? backgroundSince;
    public int Ended { get; private set; }
    public int Generation2 { get; private set; }
    public int Background { get; private set; }
    /// <summary>One event of the runtime; a note when it ends a collection of generation 2, else null.</summary>
    public string? See(string? name,DateTime at,long? count,long? depth=null,long? kind=null,long? reason=null)
    {
        if(count is not { } number)return null;
        lock(gate)
        {
            switch(name)
            {
                case "GCStart_V2":
                    if(kind==1){backgroundSince=at;backgroundCount=number;}
                    open[number]=(at,depth ?? 0,kind ?? 0,reason ?? -1,Ended);
                    if(open.Count>OpenCap)open.Remove(open.Keys.Min());
                    return null;
                case "GCEnd_V1":
                {
                    if(number==backgroundCount){backgroundSince=null;backgroundCount=-1;}
                    if(!open.Remove(number,out var started))return null;
                    Ended++;
                    if(started.Depth<2)
                    {
                        var short_=(at-started.At).TotalMilliseconds;
                        if(short_<ShortNoteMs || notes>=MaximumNotes)return null;
                        notes++;
                        return string.Create(CultureInfo.InvariantCulture,
                            $"{KindName(started.Kind)} collection of generation {started.Depth} ({ReasonName(started.Reason)}) from {started.At.ToLocalTime():HH:mm:ss.fff} to {at.ToLocalTime():HH:mm:ss.fff}: {short_:0} ms.");
                    }
                    Generation2++;
                    if(started.Kind==1)Background++;
                    var milliseconds=(at-started.At).TotalMilliseconds;
                    if(milliseconds<0 || notes>=MaximumNotes)return null;
                    notes++;
                    var others=Ended-started.EndedBefore-1;
                    return string.Create(CultureInfo.InvariantCulture,
                        $"{KindName(started.Kind)} collection of generation 2 ({ReasonName(started.Reason)}) from {started.At.ToLocalTime():HH:mm:ss.fff} to {at.ToLocalTime():HH:mm:ss.fff}: {milliseconds:0} ms, {others} other collections ended meanwhile.");
                }
                default:
                    return null;
            }
        }
    }
    public string Summary(TimeSpan watched)
    {
        lock(gate)
            return string.Create(CultureInfo.InvariantCulture,
                $"Collections in the {watched.TotalSeconds:0} s after the load: {Ended}, {Generation2} of generation 2 ({Background} in the background).");
    }
    /// <summary>The runtime's kinds of collection (GCStart_V2 Type).</summary>
    public static string KindName(long kind)=>kind switch { 0=>"Blocking", 1=>"Background", 2=>"Foreground", _=>"Kind "+kind.ToString(CultureInfo.InvariantCulture) };
    /// <summary>The runtime's reasons for a collection (GCStart_V2 Reason).</summary>
    public static string ReasonName(long reason)=>reason switch
    {
        0=>"small allocations",
        1=>"induced",
        2=>"low memory",
        3=>"empty",
        4=>"large allocations",
        5=>"out of space for small objects",
        6=>"out of space for large objects",
        7=>"induced, not forced",
        8=>"internal",
        9=>"induced for low memory",
        10=>"induced compacting",
        11=>"low memory on the host",
        12=>"provisional mode",
        13=>"low memory on the host, blocking",
        _=>"reason "+reason.ToString(CultureInfo.InvariantCulture),
    };
}

/// <summary>
/// X-433 (0.1431): the runtime's allocation ticks (GCAllocationTick: one each time about 100 KB were allocated, with the kind of heap, the type
/// and the thread): the large and pinned ones are counted by thread (the game's, the JIT warm-up's, others); one made while a background
/// collection runs gives a note with its thread, type, size and time (at most <see cref="MaximumNotes"/>). Pure: fed by
/// <see cref="RuntimeSuspensionListener"/>, or by the tests.
/// </summary>
public sealed class LargeAllocationTracker(int maximumNotes=30)
{
    public int MaximumNotes { get; } = maximumNotes;
    private readonly object gate=new();
    private int notes;
    public int Large { get; private set; }
    public int OnGameThread { get; private set; }
    public int OnWarmupThread { get; private set; }
    public int DuringBackground { get; private set; }

    /// <param name="kind">The runtime's AllocationKind: 0 small objects, 1 large objects, 2 pinned objects.</param>
    /// <param name="background">When the background collection running at that time began (none: null).</param>
    public string? See(DateTime at,long kind,long bytes,string? type,long thread,long gameThread,long warmupThread,DateTime? background)
    {
        if(kind is not (1 or 2))return null;
        lock(gate)
        {
            Large++;
            var onGame=gameThread!=0 && thread==gameThread;
            var onWarmup=warmupThread!=0 && thread==warmupThread;
            if(onGame)OnGameThread++;
            if(onWarmup)OnWarmupThread++;
            if(background is not { } since)return null;
            DuringBackground++;
            if(notes>=MaximumNotes)return null;
            notes++;
            var who=onGame ? "the game's thread":onWarmup ? "the JIT warm-up's thread":"thread "+thread.ToString(CultureInfo.InvariantCulture);
            return string.Create(CultureInfo.InvariantCulture,
                $"{(kind==1 ? "Large":"Pinned")} object allocated on {who} at {at.ToLocalTime():HH:mm:ss.fff} during the background collection begun {since.ToLocalTime():HH:mm:ss.fff}: {type ?? "?"}, {bytes/1024.0:0} KB.");
        }
    }

    public string Summary(TimeSpan watched)
    {
        lock(gate)
            return string.Create(CultureInfo.InvariantCulture,
                $"Large or pinned allocation ticks in the {watched.TotalSeconds:0} s after the load: {Large} ({OnGameThread} on the game's thread, {OnWarmupThread} on the JIT warm-up's), {DuringBackground} during a background collection.");
    }
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
/// X-433 (0.1440): the methods the game's thread compiles while the runtime is listened to (developer mode, two minutes after a load): how
/// many, by area (the first two parts of the declaring type's namespace), and their names with the time each started, the first
/// <see cref="Kept"/> of them, to name what a slow first pass of a reader compiled. Thread-safe; pure (fed by the listener or the tests).
/// </summary>
public sealed class GameThreadJit(int kept=4000)
{
    /// <summary>The tracker of the listener alive now (developer mode, two minutes after a load), for the slow passes' lines; null otherwise.</summary>
    public static GameThreadJit? Current=>Volatile.Read(ref current);
    private static GameThreadJit? current;
    internal static void Begin(GameThreadJit jit)=>Volatile.Write(ref current,jit);
    internal static void End(GameThreadJit jit)=>Interlocked.CompareExchange(ref current,null,jit);
    public int Kept { get; } = kept;
    private readonly object gate=new();
    private readonly List<(DateTime At,string Name)> seen=[];
    private readonly Dictionary<string,int> areas=new(StringComparer.Ordinal);
    public int Count { get; private set; }

    /// <summary>The area of a type's full name: its first two dotted parts, before any generic mark (« System.Linq », « TenshoAssistant.Inventory »).</summary>
    public static string Area(string? typeName)
    {
        if(string.IsNullOrEmpty(typeName))return "?";
        var end=typeName.IndexOfAny(['`','[','<','+']);
        var plain=end>0 ? typeName[..end]:typeName;
        var parts=plain.Split('.',StringSplitOptions.RemoveEmptyEntries);
        return parts.Length==0 ? "?":string.Join('.',parts.Take(Math.Min(2,Math.Max(1,parts.Length-1))));
    }

    public void See(DateTime at,string? typeName,string? methodName)
    {
        lock(gate)
        {
            Count++;
            var area=Area(typeName);
            areas[area]=areas.GetValueOrDefault(area)+1;
            if(seen.Count<Kept)seen.Add((at,$"{typeName}.{methodName}"));
        }
    }

    /// <summary>The names of the methods that started compiling between the two times (inclusive), in order.</summary>
    public IReadOnlyList<string> Between(DateTime from,DateTime to)
    {
        lock(gate)return [..seen.Where(entry=>entry.At>=from && entry.At<=to).Select(entry=>entry.Name)];
    }

    public string Summary(TimeSpan watched,int top=12)
    {
        lock(gate)
            return string.Create(CultureInfo.InvariantCulture,
                $"Methods compiled on the game's thread in the {watched.TotalSeconds:0} s after the load: {Count}; by area: {(areas.Count==0 ? "none":string.Join(", ",areas.OrderByDescending(area=>area.Value).ThenBy(area=>area.Key,StringComparer.Ordinal).Take(top).Select(area=>string.Create(CultureInfo.InvariantCulture,$"{area.Key} {area.Value}"))))}.");
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
    public const long GcKeyword=0x1,TieredCompilationKeyword=0x400000000,TypeDiagnosticKeyword=0x8000000000,JitKeyword=0x10;
    // Field initialisers run before the base constructor, which may already enable the runtime's source.
    private readonly SuspensionTracker tracker=new();
    // X-433 (0.1423): the type loads too (TypeDiagnostic keyword).
    private readonly TypeLoadTracker types=new();
    // X-433 (0.1430): the collections, with their kind (GC keyword, already enabled).
    private readonly CollectionTracker collections=new();
    // X-433 (0.1431): the large allocations, by thread (GC keyword at the verbose level: the allocation ticks).
    private readonly LargeAllocationTracker large=new();
    // X-433 (0.1440): the methods compiled on the game's thread (JIT keyword, verbose: one event as each compilation starts).
    private readonly GameThreadJit jit=new();
    private volatile Action<string>? say;
    private int disposed;

    public RuntimeSuspensionListener(Action<string> say)
    {
        this.say=say;
        GameThreadJit.Begin(jit);
    }

    public GameThreadJit Jit=>jit;

    public SuspensionTracker Tracker=>tracker;
    public CollectionTracker Collections=>collections;
    public LargeAllocationTracker Large=>large;
    public TypeLoadTracker Types=>types;
    public bool IsDisposed=>Volatile.Read(ref disposed)!=0;

    protected override void OnEventSourceCreated(EventSource source)
    {
        // X-433 (0.1431): the verbose level, for the GC keyword's allocation ticks (developer mode, two minutes).
        // X-433 (0.1440): and the JIT keyword, to name what the game's thread compiles.
        if(source.Name==RuntimeSource)EnableEvents(source,EventLevel.Verbose,(EventKeywords)(GcKeyword|TieredCompilationKeyword|TypeDiagnosticKeyword)|(EventKeywords)JitKeyword);
    }

    protected override void OnEventWritten(EventWrittenEventArgs written)
    {
        if(Volatile.Read(ref disposed)!=0)return;
        // X-433 (0.1440): a method starts compiling; only the game's thread's are kept (its id is known once a reader ran).
        if(written.EventName?.StartsWith("MethodJittingStarted",StringComparison.Ordinal)==true)
        {
            var game=Interlocked.Read(ref ThreadClock.GameThread);
            if(game!=0 && written.OSThreadId==game)jit.See(written.TimeStamp.ToUniversalTime(),Text(written,"MethodNamespace"),Text(written,"MethodName"));
            return;
        }
        if(written.EventName?.StartsWith("MethodLoad",StringComparison.Ordinal)==true)return;
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
        if(written.EventName?.StartsWith("GCAllocationTick",StringComparison.Ordinal)==true)
        {
            var size=Number(written,"ObjectSize") is { } objectSize and >0 ? objectSize:Number(written,"AllocationAmount64") ?? Number(written,"AllocationAmount") ?? 0;
            var typeName=written.PayloadNames?.IndexOf("TypeName") is int typeAt && typeAt>=0 && written.Payload is { } typeValues && typeAt<typeValues.Count ? typeValues[typeAt] as string:null;
            if(large.See(written.TimeStamp.ToUniversalTime(),Number(written,"AllocationKind") ?? 0,size,typeName,written.OSThreadId,
                Interlocked.Read(ref ThreadClock.GameThread),Interlocked.Read(ref ThreadClock.WarmupThread),collections.BackgroundSince) is { } largeNote)say?.Invoke(largeNote);
            return;
        }
        if(written.EventName is "GCStart_V2" or "GCEnd_V1")
        {
            if(collections.See(written.EventName,written.TimeStamp.ToUniversalTime(),Number(written,"Count"),Number(written,"Depth"),Number(written,"Type"),Number(written,"Reason")) is { } collectionNote)say?.Invoke(collectionNote);
            return;
        }
        uint? reason=null;long? jitted=null;
        if(written.EventName=="GCSuspendEEBegin_V1")reason=Number(written,"Reason") is { } value ? (uint)value:null;
        else if(written.EventName=="TieredCompilationBackgroundJitStop")jitted=Number(written,"JittedMethodCount");
        if(tracker.See(written.EventName,written.TimeStamp.ToUniversalTime(),reason,jitted) is { } note)say?.Invoke(note);
    }

    private static string? Text(EventWrittenEventArgs written,string name)
    {
        var index=written.PayloadNames?.IndexOf(name) ?? -1;
        return index>=0 && written.Payload is { } payload && index<payload.Count ? payload[index] as string:null;
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
        // X-433 (0.1440): the slow passes' lines stop with the listener (nothing kept past the watch or the unload).
        GameThreadJit.End(jit);
        base.Dispose();
    }
}
