using System.Reflection;
using TenshoAssistant.Competitive;
using TenshoAssistant.Diagnostics;

// 0.1200 (X-177 C4.2). Read at the load of 0.1199: the load back to 3.0 s (5.6 s in 0.1198), but 12 slow first passes (1073 ms) and a
// first frame of 596 ms: with 32 processors, three threads compiling at once took as long as one after the other (the readers' classes
// in 8.0 s instead of 4.4 s, the warm-up in 54 s instead of 32 s). The runtime compiles one method at a time for the plugin, so each
// background thread compiling makes the game's thread wait longer for its own. One background thread at a time, in the order of the
// need: the interface's code (the warm-up's early part), then the readers' classes, then the rest. Behaviour tests, the waits held by
// hand: the classes wait while told to, never past their bound; the readers' guard delay still holds; the rest waits for the classes.

internal sealed class Reader1200A { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1200B { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1200C { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1200D { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1200E { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Warm1200Early { public int One() => 1; public int Two() => 2; }
internal sealed class Warm1200Rest { public int Three() => 3; }

internal static class CompileOrder1200Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);
    private static MethodBase Handler(Action<int> handler) => handler.Method;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Compile order 1200: behaviour: while the plugin says to wait, no class is compiled and no reader runs; let go, the classes are compiled", () =>
        {
            var held = 1; var asked = 0; var prepared = 0;
            using var compiler = new ReaderCompiler(prepare: _ => Interlocked.Increment(ref prepared)) { Hold = () => { Interlocked.Increment(ref asked); return Volatile.Read(ref held) == 1; } };
            var ticket = compiler.Add(Handler(new Reader1200A().OnUpdate));
            Check(SpinWait.SpinUntil(() => Volatile.Read(ref asked) >= 3, Long), "the worker asks before each class");
            Check(Volatile.Read(ref prepared) == 0 && !ticket.Ready && compiler.Held && compiler.State.Waiting == 1 && !compiler.Idle, "held: nothing compiled, the reader waits");
            Volatile.Write(ref held, 0);
            Check(SpinWait.SpinUntil(() => compiler.Idle, Long) && ticket.Ready && Volatile.Read(ref prepared) > 0 && !compiler.Held, "let go: compiled, the reader may run");
        }),
        ("Compile order 1200: behaviour: the wait is bounded: past it the classes are compiled though the plugin still says to wait", () =>
        {
            long clock = 5_000; var prepared = 0;
            using var compiler = new ReaderCompiler(() => Interlocked.Read(ref clock), _ => Interlocked.Increment(ref prepared), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(12)) { Hold = () => true };
            var ticket = compiler.Add(Handler(new Reader1200B().OnUpdate));
            Interlocked.Add(ref clock, 11_999);
            Check(compiler.Held && !ticket.Ready && Volatile.Read(ref prepared) == 0, "one millisecond before the bound: still held");
            Interlocked.Add(ref clock, 1);
            Check(!compiler.Held && SpinWait.SpinUntil(() => compiler.Idle, Long) && ticket.Ready && Volatile.Read(ref prepared) > 0, "the bound over: compiled all the same");
            Check(ReaderCompiler.MaxHold == TimeSpan.FromSeconds(12) && ReaderCompiler.Guard == TimeSpan.FromSeconds(20) && ReaderCompiler.MaxHold < ReaderCompiler.Guard, "the classes start before the readers' guard delay ends");
        }),
        ("Compile order 1200: behaviour: a reader's guard delay still lets it go while its class waits; a stop while held lets everything go and drops the plugin's word", () =>
        {
            long clock = 0; var prepared = 0;
            var compiler = new ReaderCompiler(() => Interlocked.Read(ref clock), _ => Interlocked.Increment(ref prepared), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(600)) { Hold = () => true };
            var first = compiler.Add(Handler(new Reader1200C().OnUpdate));
            Interlocked.Add(ref clock, 15_000);
            Check(first.Ready && Volatile.Read(ref prepared) == 0, "the guard delay over: the reader starts, though nothing was compiled");
            var second = compiler.Add(Handler(new Reader1200D().OnUpdate));
            Check(!second.Ready && compiler.Held, "a later reader waits in its turn");
            compiler.Dispose();
            Check(second.Ready && compiler.Hold is null && !compiler.Held && SpinWait.SpinUntil(() => compiler.Idle, Long) && Volatile.Read(ref prepared) == 0, "stopped while held: readers let go, the hold dropped, the thread ended, nothing compiled");
        }),
        ("Compile order 1200: behaviour: the warm-up says when its early part is compiled, and its rest waits while told to", () =>
        {
            var held = 1; var asked = 0;
            using var warmup = JitWarmup.Start(() => [typeof(Warm1200Early), typeof(Warm1200Rest)], TimeSpan.FromMilliseconds(40), null,
                method => method.DeclaringType == typeof(Warm1200Early), () => { Interlocked.Increment(ref asked); return Volatile.Read(ref held) == 1; });
            var early = JitWarmup.Methods([typeof(Warm1200Early)]).Count; var all = early + JitWarmup.Methods([typeof(Warm1200Rest)]).Count;
            Check(SpinWait.SpinUntil(() => warmup.EarlyDone, Long), "the early part is compiled");
            Check(SpinWait.SpinUntil(() => Volatile.Read(ref asked) >= 3, Long) && !warmup.Completed && warmup.Prepared == early && warmup.EarlyPrepared == early, $"held: the rest waits ({warmup.Prepared} of {all})");
            Volatile.Write(ref held, 0);
            Check(warmup.Wait(Long) && warmup.Completed && warmup.Prepared == all, "let go: the rest is compiled");
            // No early part: nothing to wait for. Stopped in the middle: what waits for the early part is let go.
            using var plain = JitWarmup.Start(() => [typeof(Warm1200Rest)], TimeSpan.Zero);
            Check(plain.Wait(Long) && plain.EarlyDone, "no early part: done at once");
            var stopped = JitWarmup.Start(() => [typeof(Warm1200Early)], TimeSpan.FromSeconds(600), null, _ => true);
            stopped.Dispose();
            Check(SpinWait.SpinUntil(() => stopped.EarlyDone, Long) && JitWarmup.RestHold == TimeSpan.FromSeconds(30), "stopped before its early part: nobody is left waiting for it");
        }),
        ("Compile order 1200: behaviour: the gate says whether readers' classes still wait, and the unload forgets the compiler with the plugin's word", () =>
        {
            CompetitiveGate.ForgetInstance();
            try
            {
                Check(!CompetitiveGate.CompilerBusy && !CompetitiveGate.HoldsInstance, "no compiler: nothing waits, nothing held");
                var compiler = CompetitiveGate.Compiler;
                compiler.Hold = () => true;
                Check(CompetitiveGate.HoldsInstance, "the compiler carries the plugin's word: counted as held until the unload");
                var ticket = compiler.Add(Handler(new Reader1200E().OnUpdate));
                Check(CompetitiveGate.CompilerBusy && !ticket.Ready, "a class waits: the warm-up's rest waits behind it");
                CompetitiveGate.ForgetInstance();
                Check(!CompetitiveGate.CompilerBusy && ticket.Ready && compiler.Hold is null && !CompetitiveGate.HoldsInstance, "unload: stopped, the reader let go, the word dropped");
            }
            finally { CompetitiveGate.ForgetInstance(); }
        }),
        ("Compile order 1200: wiring: the plugin holds the classes from its first line until the window's early part is compiled; the window's rest waits for the classes", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var hold = plugin.IndexOf("TenshoAssistant.Competitive.CompetitiveGate.Compiler.Hold=()=>mainWindow is not { WarmupEarlyDone:true };", StringComparison.Ordinal);
            Check(hold > 0 && hold < plugin.IndexOf("mainWindow = new MainWindow(", StringComparison.Ordinal), "held before the window exists, so before any reader's class is compiled");
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.cs"));
            Check(window.Contains("holdRest: static ()=>TenshoAssistant.Competitive.CompetitiveGate.CompilerBusy,", StringComparison.Ordinal) && window.Contains("internal bool WarmupEarlyDone=>jitWarmup.EarlyDone;", StringComparison.Ordinal), "the window's warm-up: its rest after the classes, its early part told");
            var compiler = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReaderCompiler.cs"));
            Check(compiler.Contains("while(Held)Thread.Sleep(20);", StringComparison.Ordinal) && compiler.Contains("hold=null;", StringComparison.Ordinal), "asked before each class; dropped at the stop");
        }),
    ];
}
