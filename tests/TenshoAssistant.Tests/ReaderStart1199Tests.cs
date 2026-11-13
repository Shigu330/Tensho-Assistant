using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using TenshoAssistant.Competitive;
using TenshoAssistant.Diagnostics;

// 0.1199 (X-177 C4.2): 0.1198 compiled each reader's class when it subscribed, on the thread that builds the plugin: the frames were
// spared (3 slow first passes instead of 19) and the load took 5.6 s instead of 2.4 s. The classes are now compiled on a thread of
// their own and each reader is first called once its class is compiled. Behaviour tests (R28, concurrency): the compiling is held by
// hand (no clock): a reader is not called before its class is compiled, a guard delay starts it anyway, an unload stops the thread
// and lets every reader go.

// Readers as the plugin's are: top-level classes. Each is used by one test only (a class is compiled once).
internal sealed class Reader1199A { public int Calls; public void OnUpdate(int tick) => Calls += tick; public void Other(int tick) => Calls -= tick; }
internal sealed class Reader1199B { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1199C { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1199D { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1199E { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1199F { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1199G { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1199Cold
{
    private int total;
    [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(int tick) { total += First(tick) + Second(tick); Action<int> later = value => total += Third(value); later(tick); }
    [MethodImpl(MethodImplOptions.NoInlining)] private int First(int value) => value + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Second(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Third(int value) => value - 3;
}
internal sealed class Reader1199Ahead
{
    private int total;
    [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(int tick) { total += First(tick) + Second(tick); Action<int> later = value => total += Third(value); later(tick); }
    [MethodImpl(MethodImplOptions.NoInlining)] private int First(int value) => value + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Second(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Third(int value) => value - 3;
}
internal sealed class Reader1199Shared
{
    private int total;
    [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(int tick) { total += First(tick) + Second(tick); Action<int> later = value => total += Third(value); later(tick); }
    [MethodImpl(MethodImplOptions.NoInlining)] private int First(int value) => value + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Second(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Third(int value) => value - 3;
}

internal static class ReaderStart1199Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);

    // The compiling held by hand: the first method asked stops until the test lets it go; every method asked is noted by its class.
    private sealed class Held
    {
        public readonly ManualResetEventSlim Entered = new(false), Release = new(false);
        public readonly List<string> Prepared = [];
        private int held;
        public void Prepare(MethodBase method)
        {
            if (Interlocked.Exchange(ref held, 1) == 0) { Entered.Set(); Release.Wait(Long); }
            lock (Prepared) Prepared.Add(JitWarmup.Owner(method)!.Name);
        }
        public string[] Classes { get { lock (Prepared) return [.. Prepared]; } }
    }
    private static MethodBase Handler(Action<int> handler) => handler.Method;
    private static bool Drained(ReaderCompiler compiler) => SpinWait.SpinUntil(() => compiler.Idle, Long);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CompiledBy(Action<int> pass)
    {
        var before = JitInfo.GetCompiledMethodCount(currentThread: true);
        pass(1);
        return JitInfo.GetCompiledMethodCount(currentThread: true) - before;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Reader start 1199: behaviour: a reader is not ready before its class is compiled, ready once it is; classes compiled in the order they subscribed, each once, on another thread", () =>
        {
            var held = new Held();
            using var compiler = new ReaderCompiler(prepare: held.Prepare);
            var a = compiler.Add(Handler(new Reader1199A().OnUpdate));
            var b = compiler.Add(Handler(new Reader1199B().OnUpdate));
            Check(ReferenceEquals(a, compiler.Add(Handler(new Reader1199A().Other))), "one ticket for a class, whatever the handler");
            Check(held.Entered.Wait(Long), "the compiling started, off the calling thread");
            Check(!a.Ready && !b.Ready && compiler.State.Waiting == 2 && !compiler.Idle, "held in the middle of the first class: neither reader may run yet");
            held.Release.Set();
            Check(Drained(compiler) && a.Ready && b.Ready, "compiled: both readers may run");
            var classes = held.Classes;
            var first = JitWarmup.Methods([typeof(Reader1199A)]).Count; var second = JitWarmup.Methods([typeof(Reader1199B)]).Count;
            Check(classes.Length == first + second && classes.Take(first).All(name => name == nameof(Reader1199A)) && classes.Skip(first).All(name => name == nameof(Reader1199B)),
                "the first subscribed first, every method once: " + string.Join(",", classes));
            var state = compiler.State;
            Check(state.Classes == 2 && state.Methods == first + second && state.Waiting == 0 && state.DoneAfter > 0, $"counted: {state}");
            // A reader that subscribes later (a page opened): the thread starts again for it.
            var c = compiler.Add(Handler(new Reader1199C().OnUpdate));
            Check(Drained(compiler) && c.Ready && compiler.State.Classes == 3, "a later subscription is compiled too");
        }),
        ("Reader start 1199: behaviour: a class still compiling when the guard delay is over: the reader starts anyway, and stays started", () =>
        {
            var held = new Held();
            long clock = 1_000;
            using var compiler = new ReaderCompiler(() => Interlocked.Read(ref clock), held.Prepare, TimeSpan.FromSeconds(15));
            var ticket = compiler.Add(Handler(new Reader1199D().OnUpdate));
            Check(held.Entered.Wait(Long) && !ticket.Ready, "held: not ready");
            Interlocked.Add(ref clock, 14_999);
            Check(!ticket.Ready, "one millisecond before the guard delay: still waiting");
            Interlocked.Add(ref clock, 1);
            Check(ticket.Ready, "the guard delay over: the reader starts, its class compiled or not");
            Interlocked.Add(ref clock, -10_000);
            Check(ticket.Ready, "started is started");
            held.Release.Set();
            Check(Drained(compiler), "the compiling ends as usual");
        }),
        ("Reader start 1199: behaviour: an unload in the middle of the compiling: the thread stops after the method under way, every reader is let go, nothing more is compiled", () =>
        {
            var held = new Held();
            var compiler = new ReaderCompiler(prepare: held.Prepare);
            var a = compiler.Add(Handler(new Reader1199E().OnUpdate));
            var b = compiler.Add(Handler(new Reader1199F().OnUpdate));
            Check(held.Entered.Wait(Long) && !a.Ready && !b.Ready, "held in the first class");
            var stopping = Task.Run(compiler.Dispose);
            Check(SpinWait.SpinUntil(() => compiler.Stopped, Long), "the stop is asked while a method is compiling");
            held.Release.Set();
            Check(stopping.Wait(Long) && SpinWait.SpinUntil(() => compiler.Idle, Long), "the thread ended, the unload did not wait for the classes left");
            Check(a.Ready && b.Ready, "no reader is left waiting by an unload");
            var classes = held.Classes;
            Check(classes.Length == 1 && classes[0] == nameof(Reader1199E), "only the method under way was finished: " + string.Join(",", classes));
            var late = compiler.Add(Handler(new Reader1199A().OnUpdate));
            Check(late.Ready && compiler.Idle && held.Classes.Length == 1, "after the stop: ready at once, no thread, nothing compiled");
            compiler.Dispose();
        }),
        ("Reader start 1199: behaviour: a method that cannot be compiled never keeps its reader waiting", () =>
        {
            using var compiler = new ReaderCompiler(prepare: _ => throw new InvalidOperationException("cannot be prepared"));
            var ticket = compiler.Add(Handler(new Reader1199G().OnUpdate));
            Check(Drained(compiler) && ticket.Ready, "ready all the same");
            Check(compiler.State.Classes == 1 && compiler.State.Methods == 0, "counted as a class, none of its methods: " + compiler.State);
        }),
        ("Reader start 1199: behaviour: compiled on the compiler's thread, a reader's first pass compiles nothing of its class on the thread that calls it", () =>
        {
            CompiledBy(new Reader1199Shared().OnUpdate);
            Action<int> coldPass = new Reader1199Cold().OnUpdate, aheadPass = new Reader1199Ahead().OnUpdate;
            using var compiler = new ReaderCompiler();
            var ticket = compiler.Add(aheadPass.Method);
            Check(SpinWait.SpinUntil(() => ticket.Ready, Long) && Drained(compiler) && compiler.State.Methods >= 5, "compiled for real: " + compiler.State);
            var coldCompiled = CompiledBy(coldPass);
            var aheadCompiled = CompiledBy(aheadPass);
            Check(coldCompiled >= 5, "a first pass left alone compiles its class in the frame: " + coldCompiled);
            Check(aheadCompiled <= coldCompiled - 5, $"compiled ahead on another thread: {aheadCompiled} methods compiled in the pass against {coldCompiled}");
        }),
        ("Reader start 1199: behaviour: the gate keeps one compiler per load; the unload stops it and forgets it with the hooks", () =>
        {
            CompetitiveGate.ForgetInstance();
            try
            {
                Check(CompetitiveGate.CompilerState is null, "none before the first subscription");
                var first = CompetitiveGate.Compiler;
                Check(ReferenceEquals(first, CompetitiveGate.Compiler) && CompetitiveGate.CompilerState?.Classes == 0, "one for the load");
                CompetitiveGate.ForgetInstance();
                Check(first.Stopped && CompetitiveGate.CompilerState is null && !CompetitiveGate.HoldsInstance, "stopped and forgotten at unload");
                Check(!ReferenceEquals(first, CompetitiveGate.Compiler), "another load, another compiler");
            }
            finally { CompetitiveGate.ForgetInstance(); }
        }),
        ("Reader start 1199: wiring: every way to subscribe takes the reader's ticket and asks it before each call; the unload forgets the compiler; the load says what was compiled", () =>
        {
            var gate = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs")).ReplaceLineEndings("\n");
            Check(gate.Contains("public static void Add(IFramework framework,IFramework.OnUpdateDelegate handler)\n    {\n        var ticket=Compiler.Add(handler.Method);\n        void Guarded(IFramework running){if(!cut && ticket.Ready)Timed(handler,running);}", StringComparison.Ordinal), "Add");
            Check(gate.Contains("var ticket=Compiler.Add(handler.Method);\n        void Guarded(IFramework running){if((!cut || labyrinthBoard) && ticket.Ready)Timed(handler,running);}", StringComparison.Ordinal), "AddBoard");
            Check(gate.Contains("var ticket=Compiler.Add(handler.Method);\n        void Deferred(IFramework running){if(ticket.Ready)Timed(handler,running);}\n        lock(Gate)Wrappers[handler]=Deferred;\n        framework.Update+=Deferred;", StringComparison.Ordinal), "AddAlways, its wrapper kept for the removal");
            Check(gate.Contains("public static void RemoveAlways(IFramework framework,IFramework.OnUpdateDelegate handler)=>Remove(framework,handler);", StringComparison.Ordinal), "RemoveAlways takes the wrapper off");
            Check(gate.Split("Compiler.Add(handler.Method)").Length == 4 && !gate.Contains("PrepareOwner", StringComparison.Ordinal), "three ways to subscribe, nothing compiled on the subscribing thread");
            var hooks = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGateHooks.cs")).ReplaceLineEndings("\n");
            var forget = hooks[hooks.IndexOf("internal static void ForgetInstance()", StringComparison.Ordinal)..];
            Check(forget.Contains("lock(CompilerGate){running=compiler;compiler=null;}\n        running?.Dispose();", StringComparison.Ordinal), "stopped at unload");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("log.Information(\"Readers compiled on their own thread: {0} classes, {1} methods in {2:0} ms, the last one ready {3:0} ms after the first subscribed; {4} still waiting.\"", StringComparison.Ordinal), "the line of the load, developer mode");
            Check(!plugin.Contains("on the loading thread", StringComparison.Ordinal), "nothing compiled on the loading thread any more");
        }),
    ];
}
