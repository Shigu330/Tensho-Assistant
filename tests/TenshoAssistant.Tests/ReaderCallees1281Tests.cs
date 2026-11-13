using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1281 (X-177): with its class compiled before its first pass (0.1199), a reader still compiled 27 to 43 methods in its first pass
// (dalamud.log of 13/11: ReminderDtrEntry 54 to 101 ms, LocalLinkFeed, BankInventoryBridge, CraftActionCatalogLoader 50 to 92 ms, each with a
// CompetitiveGate hitch): the methods it calls in other classes and the generic code made for its records. The reader compiler now takes them
// with the class (CallGraph, ReaderCompiler.CalleeCap), each once for all the readers. Behaviour tests: a reader started by its ticket compiles
// nothing in its first pass, where its cold twin compiles its callees; a callee shared by two readers is compiled once; a slow pass names the
// runtime's pauses.

// Each reader and its helpers are used by one test only (a method is compiled once per process).
internal readonly record struct Item1281Cold(int Value);
internal readonly record struct Item1281Ahead(int Value);
internal static class Helper1281Cold
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Twice(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Pick<T>(T first, T second, bool left) => left ? first : second;
}
internal static class Helper1281Ahead
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Twice(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Pick<T>(T first, T second, bool left) => left ? first : second;
}
internal sealed class Reader1281Cold
{
    public int Total;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnUpdate(int tick) => Total += Helper1281Cold.Twice(tick) + Helper1281Cold.Pick(new Item1281Cold(tick), new Item1281Cold(0), true).Value;
}
internal sealed class Reader1281Ahead
{
    public int Total;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnUpdate(int tick) => Total += Helper1281Ahead.Twice(tick) + Helper1281Ahead.Pick(new Item1281Ahead(tick), new Item1281Ahead(0), true).Value;
}
internal static class Shared1281
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Value(int value) => value + 11;
}
internal sealed class Reader1281First { public int Total; [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(int tick) => Total += Shared1281.Value(tick); }
internal sealed class Reader1281Second { public int Total; [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(int tick) => Total += Shared1281.Value(tick) * 2; }

internal static class ReaderCallees1281Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CompiledBy(Action<int> pass)
    {
        var before = JitInfo.GetCompiledMethodCount(currentThread: true);
        pass(2);
        return JitInfo.GetCompiledMethodCount(currentThread: true) - before;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Reader callees 1281: behaviour: a reader started by its ticket compiles nothing in its first pass, its callees in other classes included; its cold twin compiles them", () =>
        {
            var cold = new Reader1281Cold();
            var coldCompiled = CompiledBy(cold.OnUpdate);
            using var compiler = new ReaderCompiler();
            var ahead = new Reader1281Ahead();
            Action<int> pass = ahead.OnUpdate;
            var ticket = compiler.Add(pass.Method);
            Check(SpinWait.SpinUntil(() => ticket.Ready && compiler.Idle, Long), "the class and its callees compiled on the compiler's thread");
            var aheadCompiled = CompiledBy(pass);
            Check(cold.Total == ahead.Total && ahead.Total == 4 + 2, $"both passes ran ({cold.Total}, {ahead.Total})");
            Check(coldCompiled >= 3, $"the cold twin compiled its callees in its first pass ({coldCompiled})");
            Check(aheadCompiled == 0, $"nothing left for the first pass ({aheadCompiled}, cold {coldCompiled})");
            Check(compiler.Callees >= 2 && compiler.State.Methods >= 3, $"Twice and Pick<Item> counted as called from another class ({compiler.Callees} of {compiler.State.Methods})");
        }),
        ("Reader callees 1281: behaviour: a method two readers call is compiled once; each reader's class still in the order they subscribed", () =>
        {
            var prepared = new List<string>();
            using var compiler = new ReaderCompiler(prepare: method => { lock (prepared) prepared.Add(method.DeclaringType!.Name + "." + method.Name); });
            Action<int> first = new Reader1281First().OnUpdate, second = new Reader1281Second().OnUpdate;
            var a = compiler.Add(first.Method);
            var b = compiler.Add(second.Method);
            Check(SpinWait.SpinUntil(() => a.Ready && b.Ready && compiler.Idle, Long), "both ready");
            string[] list;
            lock (prepared) list = [.. prepared];
            Check(list.Count(name => name == "Shared1281.Value") == 1, "the shared callee once: " + string.Join(", ", list));
            Check(Array.IndexOf(list, "Reader1281First.OnUpdate") < Array.IndexOf(list, "Shared1281.Value")
                && Array.IndexOf(list, "Shared1281.Value") < Array.IndexOf(list, "Reader1281Second.OnUpdate"), "the first reader with its callee, then the second: " + string.Join(", ", list));
            Check(ReaderCompiler.CalleeCap == 80, "80 callees at most per class (the slow first passes compiled 27 to 43)");
        }),
        ("Reader callees 1281: without callees asked, a class is compiled alone, as before; a known method is not compiled again", () =>
        {
            Action<int> pass = new Reader1281First().OnUpdate;
            var alone = new List<MethodBase>();
            JitWarmup.PrepareOwner(pass.Method, prepare: alone.Add);
            Check(alone.All(method => JitWarmup.Owner(method) == typeof(Reader1281First)), "the class alone");
            var known = new HashSet<MethodBase>(alone);
            var again = new List<MethodBase>();
            JitWarmup.PrepareOwner(pass.Method, prepare: again.Add, callees: 80, known: known);
            Check(again.Count == 1 && again[0].DeclaringType == typeof(Shared1281), "only the callee not known yet: " + string.Join(", ", again.Select(method => method.Name)));
        }),
        ("Reader callees 1281: a slow pass names the runtime's pauses in whole milliseconds, nothing under 1 ms", () =>
        {
            Check(JitWarmup.PauseText(0.4) == "", "nothing under 1 ms");
            Check(JitWarmup.PauseText(975.6) == " (runtime pauses 976 ms)", JitWarmup.PauseText(975.6));
            var gate = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs"));
            Check(gate.Contains("Diagnostics.JitWarmup.PauseText((GC.GetTotalPauseDuration()-pausedBefore).TotalMilliseconds)", StringComparison.Ordinal), "the slow reader line says it");
            var compiler = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReaderCompiler.cs"));
            Check(compiler.Contains("JitWarmup.PrepareOwner(next.Handler,JitWarmup.OwnerCap,()=>stopped,prepare,CalleeCap,known)", StringComparison.Ordinal), "the readers' classes with their callees");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("Readers' callees: {0} methods called from their classes in other classes", StringComparison.Ordinal), "the readers' line counts them");
        }),
    ];
}
