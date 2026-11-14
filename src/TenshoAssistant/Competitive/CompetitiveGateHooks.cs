namespace TenshoAssistant.Competitive;

/// <summary>
/// X-01 C1.3 (0.1187): the gate's static hooks, apart from Dalamud so a test can hold them. They are given by the plugin's constructor as
/// closures: each one holds the constructor's captured locals, that is the whole instance (window, services, catalogues). A static field
/// lives as long as its load context, and the context of an unloaded instance stays alive in the game process (Dalamud's settings API puts
/// the settings types in caches of the shared runtime: docs/evidence/0.1187-unload-roots.md). Left set at unload, these hooks kept the whole
/// unloaded instance in memory at each reload: <see cref="ForgetInstance"/> empties them, called last by the plugin's unload.
/// </summary>
internal static partial class CompetitiveGate
{
    /// <summary>X-109 (0.979): developer mode, given once the configuration is loaded (the gate starts before it).</summary>
    internal static Func<bool>? Developer { get; set; }

    /// <summary>On the framework thread, when the cut starts (true) or ends (false).</summary>
    public static event Action<bool>? Changed;

    /// <summary>0.992, X-177 (0.1162): developer mode, a subscription over 50 ms in one frame is named (reader, time, worst time meanwhile).</summary>
    internal static Action<string,double,double>? SlowSubscription { get; set; }

    internal static void ClearSubscribers()=>Changed=null;

    // X-177 C4.2 (0.1199): what compiles the readers' classes on a thread of its own (Diagnostics.ReaderCompiler): made at the first
    // subscription, stopped and forgotten at unload with the hooks. It holds types and flags, nothing of an instance.
    private static Diagnostics.ReaderCompiler? compiler;
    private static readonly object CompilerGate=new();
    /// <summary>The compiler of this load: every subscription takes its ticket from it.</summary>
    internal static Diagnostics.ReaderCompiler Compiler{get{lock(CompilerGate)return compiler??=new();}}
    /// <summary>What the readers' compiler did so far, for the log; null when no reader subscribed.</summary>
    internal static (int Classes,int Methods,double Compiling,double DoneAfter,int Waiting)? CompilerState{get{lock(CompilerGate)return compiler?.State;}}
    /// <summary>X-177 (0.1281): of the methods compiled for the readers, those called from their classes in other classes.</summary>
    internal static int CompilerCallees{get{lock(CompilerGate)return compiler?.Callees ?? 0;}}
    /// <summary>X-177 C4.2 (0.1200): readers' classes still wait or are being compiled (the warm-up's rest waits for them); false without a compiler.</summary>
    internal static bool CompilerBusy{get{Diagnostics.ReaderCompiler? running;lock(CompilerGate)running=compiler;return running is { Idle:false };}}

    /// <summary>Whether a hook still holds something of an instance (the unload's own check, and the tests').</summary>
    internal static bool HoldsInstance=>Developer is not null || SlowSubscription is not null || Changed is not null || compiler is not null;

    // X-177 C4.4 (0.1193, analysis of 12/11: « GameGearsetReader 354 ms » in the slow readers was the game's own EquipGearset, which
    // changes the gear): the time a subscription spends inside a function of the game it calls on the player's request is told apart
    // from the reader's own time. Framework thread only (the subscriptions run one after the other there).
    private static double gameCallMs;

    /// <summary>Before a call into a function of the game from a subscription: the moment, to give back to <see cref="GameCallEnded"/>.</summary>
    internal static long GameCallStarted()=>System.Diagnostics.Stopwatch.GetTimestamp();

    /// <summary>After that call (in a finally): its time is the game's, taken out of the subscription's measured time.</summary>
    internal static void GameCallEnded(long started)=>gameCallMs+=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;

    // X-01 (0.1312): a reader names its steps (Step after each one); a slow pass says its costliest step, so the next slow reader says where
    // its time went. Per thread: only the thread that started the steps counts them (elsewhere Step does nothing).
    [ThreadStatic] private static long stepAt;
    [ThreadStatic] private static string? costliestStep;
    [ThreadStatic] private static double costliestStepMs;

    /// <summary>The end of a reader's step named <paramref name="name"/> (the time since the last step, or since the pass began).</summary>
    internal static void Step(string name)
    {
        if(stepAt==0)return;
        var now=System.Diagnostics.Stopwatch.GetTimestamp();
        var milliseconds=System.Diagnostics.Stopwatch.GetElapsedTime(stepAt,now).TotalMilliseconds;
        if(milliseconds>costliestStepMs){costliestStepMs=milliseconds;costliestStep=name;}
        stepAt=now;
    }
    /// <summary>A pass begins: its steps are counted on this thread.</summary>
    internal static void StepsStart(){stepAt=System.Diagnostics.Stopwatch.GetTimestamp();costliestStep=null;costliestStepMs=0;}
    /// <summary>The pass ends: its costliest named step for the log line (nothing when no step was named), and nothing counted any more.</summary>
    internal static string StepsEnd()
    {
        var text=costliestStep is { } name ? string.Create(System.Globalization.CultureInfo.InvariantCulture,$" [costliest step {name} {costliestStepMs:0} ms]"):"";
        stepAt=0;costliestStep=null;costliestStepMs=0;
        return text;
    }

    /// <summary>Starts the measure of one subscription: nothing of the game counted yet.</summary>
    internal static long MeasureStart(){gameCallMs=0;return System.Diagnostics.Stopwatch.GetTimestamp();}

    /// <summary>The subscription's own time in milliseconds since <see cref="MeasureStart"/>: the whole, less the game's own functions.</summary>
    internal static double MeasureOwn(long started)
    {
        var total=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var game=Math.Min(total,gameCallMs);
        gameCallMs=0;
        return total-game;
    }

    /// <summary>X-01 C1.3 (0.1187): at unload, nothing of the instance stays reachable from the gate's static hooks.</summary>
    internal static void ForgetInstance()
    {
        Developer=null;
        SlowSubscription=null;
        Changed=null;
        Diagnostics.ReaderCompiler? running;
        lock(CompilerGate){running=compiler;compiler=null;}
        running?.Dispose();
    }
}
