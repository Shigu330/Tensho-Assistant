using System.Reflection;
using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1633 (X-564): the chat window's first frame still compiled 7 methods on the game's thread (43 ms): in developer mode the log names them
// (the runtime's JIT events); the first frame takes the equality members of the types it touches.
// 0.1639 (X-671): alone, after the parallel tests, and 30 s to hear both marks: beside the other test threads compiling, under the load of
// Verify -Release, the marks were not heard within 10 s (1 run in 4 with 40 busy processes).
[SerialTests("listens to the runtime's JIT events of the whole process: beside the parallel tests' compilations they came too late or were lost")]
internal static class ChatFrameJit1633Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    internal sealed record Point1633(int X,int Y);
    internal static class Frame1633 { public static int Draw()=>new Point1633(1,2).X; }
    internal static class Fresh1633 { public static int Never()=>Environment.ProcessorCount*7+3; }
    /// <summary>X-703 (0.1684): how long the marks are waited for at most, a condition polled, not a delay (the runner's hang watch is 5 min).</summary>
    private static readonly TimeSpan Patience=TimeSpan.FromMinutes(3);
    /// <summary>X-703: a fresh method compiled on this thread, until the listener hears it (before the window, so never one of its names).</summary>
    private static void Probe()
    {
        var method=new System.Reflection.Emit.DynamicMethod("Probe1633"+Guid.NewGuid().ToString("N"),typeof(int),Type.EmptyTypes,typeof(ChatFrameJit1633Tests).Module);
        var il=method.GetILGenerator();
        il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4_2);
        il.Emit(System.Reflection.Emit.OpCodes.Ret);
        ((Func<int>)method.CreateDelegate(typeof(Func<int>)))();
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat frame jit 1633: a record's equality members are found (Equals, GetHashCode, the contract, the operators); the first frame takes them for the types it touches",()=>
        {
            var names=JitWarmup.EqualityMembers(typeof(Point1633)).Select(method=>method.Name).ToHashSet();
            Check(names.SetEquals(["Equals","GetHashCode","get_EqualityContract","op_Equality","op_Inequality"]),string.Join(", ",names));
            // 0.1636 (X-564): the equality members of the chat part's own types only: Point1633 is part of it here.
            var frame=JitWarmup.ChatFirstFrame([typeof(Frame1633).GetMethod("Draw")!],method=>method.DeclaringType==typeof(Frame1633) || method.DeclaringType==typeof(Point1633),typeof(Frame1633).Assembly);
            Check(frame.Any(method=>method.DeclaringType==typeof(Point1633) && method.Name=="GetHashCode") && frame.Any(method=>method.DeclaringType==typeof(Point1633) && method.Name==".ctor"),
                string.Join(", ",frame.Select(method=>method.DeclaringType!.Name+"."+method.Name)));
        }),
        ("Chat frame jit 1633: behaviour: a method compiled on this thread while the listener listens is named; nothing outside its window",()=>
        {
            // X-703 (0.1684), the cause: one failure on 21/11 near 04:10 (relay 153, Verify beside another session's work), the stop mark
            // not heard within 30 s. Not reproduced: 20 runs of this class alone and 10 of the whole suite beside 32 processor burners, and
            // 90 runs of a copy of the listener in a process of its own beside 32 burners, marks compiled at once after the listener is made,
            // after the runtime's reader had gone to wait, and just after another listener of the runtime's source was disposed (as
            // CacheStream1432Tests does just before): every mark heard within 40 ms. The runtime neither loses nor holds these events; what
            // depended on the load were the test's own two fixed delays, 200 ms before the start mark (a guess at when the listener hears) and
            // 30 s for the marks, which a process starved of processor time overruns (X-671: under Verify -Release the test process once
            // stood still, every thread waiting). Both waits are now conditions: the window opens once the listener has heard this thread,
            // and the marks are waited for up to Patience (the runner says a test hung after 5 min), polled every 10 ms without spinning;
            // a failure says what was heard (JitNames.State).
            using var names=new JitNames();
            var thread=ThreadClock.SystemId();
            var ready=System.Diagnostics.Stopwatch.StartNew();
            while(!names.Hears(thread) && ready.Elapsed<Patience){Probe();Thread.Sleep(10);}
            Check(names.Hears(thread),"the listener hears this thread: "+names.State(thread));
            names.Start();
            RuntimeHelpers.PrepareMethod(typeof(Fresh1633).GetMethod("Never")!.MethodHandle);
            names.Stop();
            var waited=System.Diagnostics.Stopwatch.StartNew();
            while(!names.Heard(thread) && waited.Elapsed<Patience)Thread.Sleep(10);
            Check(names.Heard(thread),$"both marks heard within {Patience.TotalMinutes} min: "+names.State(thread));
            Check(names.State(thread).StartsWith("start mark heard, stop mark heard, ",StringComparison.Ordinal) && !names.Hears(thread+1),"what was heard, said: "+names.State(thread));
            var heard=names.Names(thread);
            Check(heard.Any(name=>name.EndsWith("::Never",StringComparison.Ordinal)) && !heard.Any(name=>name.Contains("TenshoJit",StringComparison.Ordinal)),"named, marks aside: "+string.Join(", ",heard));
            Check(names.Names(thread+1).Count==0,"another thread's are not its own");
        }),
        ("Chat frame jit 1633: wiring: developer mode only, the names logged off the game's thread, the listener let go",()=>
        {
            var chat=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(chat.Contains("var names=developerMode ? new Diagnostics.JitNames():null;",StringComparison.Ordinal),"developer mode only");
            Check(chat.Contains("_=Task.Delay(2000).ContinueWith(",StringComparison.Ordinal) && chat.Contains("finally{names.Dispose();}",StringComparison.Ordinal),"later, then let go");
        }),
    ];
}
