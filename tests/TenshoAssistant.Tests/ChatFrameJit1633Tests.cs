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
            using var names=new JitNames();
            Thread.Sleep(200);
            names.Start();
            RuntimeHelpers.PrepareMethod(typeof(Fresh1633).GetMethod("Never")!.MethodHandle);
            names.Stop();
            var thread=ThreadClock.SystemId();
            Check(SpinWait.SpinUntil(()=>names.Heard(thread),TimeSpan.FromSeconds(30)),"both marks heard");
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
