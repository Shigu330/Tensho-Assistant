using System.Reflection;
using TenshoAssistant.Diagnostics;

// 0.1438 (X-433): at the load of 18:11:32 on 15/11 the guard delay (20 s from each subscription) let six readers go at once while the
// compiler, still going, had not reached their classes: they compiled in one frame of the game (a gap of 994 ms). The guard now counts
// from the later of the subscription and the compiler's last finished class, with a longest wait of 60 s; the readers it starts are
// counted for the log. Behaviour tests (concurrency): the compiling is held by hand, class by class, on a clock of the test's own.

internal sealed class Reader1438A { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1438B { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1438C { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1438D { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1438E { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1438F { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1438G { public int Calls; public void OnUpdate(int tick) => Calls += tick; }
internal sealed class Reader1438H { public int Calls; public void OnUpdate(int tick) => Calls += tick; }

internal static class ReaderGuard1438Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);

    // The compiling held by hand, class by class: the first method of a held class stops until the test lets that class go.
    private sealed class Gates
    {
        private readonly Dictionary<Type,(ManualResetEventSlim Entered,ManualResetEventSlim Release)> gates = [];
        private readonly HashSet<Type> entered = [];
        public Gates(params Type[] held) { foreach (var type in held) gates[type] = (new(false),new(false)); }
        public bool Entered(Type type) => gates[type].Entered.Wait(Long);
        public void Release(Type type) => gates[type].Release.Set();
        public void Prepare(MethodBase method)
        {
            var owner = JitWarmup.Owner(method)!;
            bool first;
            lock (entered) first = entered.Add(owner);
            if (first && gates.TryGetValue(owner,out var gate)) { gate.Entered.Set(); gate.Release.Wait(Long); }
        }
    }
    private static MethodBase Handler(Action<int> handler) => handler.Method;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Reader guard 1438: behaviour: while the compiler keeps finishing classes, a waiting reader is not let go at its own guard delay; with no progress for the guard delay, it is, and counted",() =>
        {
            long clock = 1_000;
            var gates = new Gates(typeof(Reader1438A),typeof(Reader1438B));
            using var compiler = new ReaderCompiler(() => Interlocked.Read(ref clock),gates.Prepare,TimeSpan.FromSeconds(15),TimeSpan.FromSeconds(12),TimeSpan.FromSeconds(60));
            var a = compiler.Add(Handler(new Reader1438A().OnUpdate));
            var b = compiler.Add(Handler(new Reader1438B().OnUpdate));
            Check(gates.Entered(typeof(Reader1438A)) && !a.Ready && !b.Ready,"A being compiled: neither may run");
            Interlocked.Exchange(ref clock,11_000);
            gates.Release(typeof(Reader1438A));
            Check(gates.Entered(typeof(Reader1438B)) && a.Ready,"A compiled at 11 s: A runs; B being compiled");
            Interlocked.Exchange(ref clock,16_000);
            Check(!b.Ready,"16 s, B's own guard delay over (subscribed at 1 s): the compiler finished A at 11 s, so B still waits (before 0.1438 it ran)");
            Interlocked.Exchange(ref clock,25_999);
            Check(!b.Ready,"one millisecond before 15 s without progress: still waiting");
            Interlocked.Exchange(ref clock,26_000);
            Check(b.Ready && compiler.GuardStarts == 1,"15 s without progress: B runs anyway, counted once");
            Check(b.Ready && compiler.GuardStarts == 1,"asked again: still one");
            gates.Release(typeof(Reader1438B));
            Check(SpinWait.SpinUntil(() => compiler.Idle,Long) && compiler.GuardStarts == 1,"B compiled after: the count stays");
        }),
        ("Reader guard 1438: behaviour: readers compiled in time are never counted; a compiler that keeps going is waited for at most the longest wait",() =>
        {
            long clock = 1_000;
            var gates = new Gates(typeof(Reader1438C),typeof(Reader1438D),typeof(Reader1438E));
            using var compiler = new ReaderCompiler(() => Interlocked.Read(ref clock),gates.Prepare,TimeSpan.FromSeconds(15),TimeSpan.FromSeconds(12),TimeSpan.FromSeconds(30));
            var c = compiler.Add(Handler(new Reader1438C().OnUpdate));
            var d = compiler.Add(Handler(new Reader1438D().OnUpdate));
            var e = compiler.Add(Handler(new Reader1438E().OnUpdate));
            Check(gates.Entered(typeof(Reader1438C)),"C first");
            Interlocked.Exchange(ref clock,14_000);
            gates.Release(typeof(Reader1438C));
            Check(gates.Entered(typeof(Reader1438D)),"C done at 14 s, D next");
            Interlocked.Exchange(ref clock,28_000);
            gates.Release(typeof(Reader1438D));
            Check(gates.Entered(typeof(Reader1438E)) && c.Ready && d.Ready && compiler.GuardStarts == 0,"D done at 28 s: C and D ran when compiled, none counted");
            Interlocked.Exchange(ref clock,30_999);
            Check(!e.Ready,"E: the compiler progressed at 28 s, and the longest wait (30 s from 1 s) is not over");
            Interlocked.Exchange(ref clock,31_000);
            Check(e.Ready && compiler.GuardStarts == 1,"31 s: the longest wait is over: E runs, counted");
            gates.Release(typeof(Reader1438E));
            Check(SpinWait.SpinUntil(() => compiler.Idle,Long),"drained");
        }),
        ("Reader guard 1438: behaviour: a reader that subscribes long after the compiler last finished waits its whole guard delay from its subscription",() =>
        {
            long clock = 1_000;
            var gates = new Gates(typeof(Reader1438G));
            using var compiler = new ReaderCompiler(() => Interlocked.Read(ref clock),gates.Prepare,TimeSpan.FromSeconds(15),TimeSpan.FromSeconds(12),TimeSpan.FromSeconds(60));
            var f = compiler.Add(Handler(new Reader1438F().OnUpdate));
            Check(SpinWait.SpinUntil(() => compiler.Idle,Long) && f.Ready,"F compiled at 1 s");
            Interlocked.Exchange(ref clock,100_000);
            var g = compiler.Add(Handler(new Reader1438G().OnUpdate));
            Check(gates.Entered(typeof(Reader1438G)) && !g.Ready,"G at 100 s: the progress at 1 s does not start it");
            Interlocked.Exchange(ref clock,114_999);
            Check(!g.Ready,"before its own guard delay");
            Interlocked.Exchange(ref clock,115_000);
            Check(g.Ready && compiler.GuardStarts == 1,"its guard delay over with no progress: runs, counted");
            gates.Release(typeof(Reader1438G));
            Check(SpinWait.SpinUntil(() => compiler.Idle,Long),"drained");
        }),
        ("Reader guard 1438: behaviour: a stop lets every waiting reader go without counting them as guard starts",() =>
        {
            long clock = 1_000;
            var gates = new Gates(typeof(Reader1438H));
            var compiler = new ReaderCompiler(() => Interlocked.Read(ref clock),gates.Prepare,TimeSpan.FromSeconds(15),TimeSpan.FromSeconds(12),TimeSpan.FromSeconds(60));
            var h = compiler.Add(Handler(new Reader1438H().OnUpdate));
            Check(gates.Entered(typeof(Reader1438H)) && !h.Ready,"held");
            gates.Release(typeof(Reader1438H));
            compiler.Dispose();
            Check(h.Ready && compiler.GuardStarts == 0,"stopped: ready, not a guard start");
        }),
        ("Reader guard 1438: the bounds and the log line",() =>
        {
            Check(ReaderCompiler.Guard == TimeSpan.FromSeconds(20) && ReaderCompiler.MaxWait == TimeSpan.FromSeconds(60) && ReaderCompiler.MaxWait > ReaderCompiler.MaxHold + ReaderCompiler.Guard,"the longest wait leaves the hold and one guard delay");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("log.Information(\"Readers started by the guard delay before their class was compiled: {0}.\",TenshoAssistant.Competitive.CompetitiveGate.CompilerGuardStarts);",StringComparison.Ordinal),"the log line of the load, developer mode");
        }),
    ];
}
