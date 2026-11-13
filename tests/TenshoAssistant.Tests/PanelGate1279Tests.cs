using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1279 (X-01, user's decision of 13/11 around 11:10): the free panels' first frame at each load was 152 to 338 ms of first-run compiling
// (dalamud.log of 13/11, 38 loads). The warm-up now compiles the panels' code first with what it calls in the plugin (CallGraph, read from
// the IL), and the panels wait for it (PanelGate, 3 s at most). Behaviour tests: the gate's states on a hand clock; the callees read from
// the IL; a method compiled ahead with its callees compiles nothing on its first call, where its cold twin compiles several methods.

// Each pair is used by one test only (a method is compiled once per process).
internal readonly record struct Item1279Cold(int Value);
internal readonly record struct Item1279Ahead(int Value);
internal static class Callee1279Cold
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Twice(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Pick<T>(T first, T second, bool left) => left ? first : second;
}
internal static class Callee1279Ahead
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Twice(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Pick<T>(T first, T second, bool left) => left ? first : second;
}
internal static class Caller1279Cold
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(int value)
    {
        Func<int, int> add = number => number + Callee1279Cold.Twice(number);
        return add(value) + Callee1279Cold.Pick(new Item1279Cold(value), new Item1279Cold(1), true).Value;
    }
}
internal static class Caller1279Ahead
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(int value)
    {
        Func<int, int> add = number => number + Callee1279Ahead.Twice(number);
        return add(value) + Callee1279Ahead.Pick(new Item1279Ahead(value), new Item1279Ahead(1), true).Value;
    }
}
internal static class Lists1279Cold
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(int value) { var list = new List<Item1279Cold> { new(value), new(value + 1) }; return list.Count + list[0].Value; }
}
internal static class Lists1279Ahead
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(int value) { var list = new List<Item1279Ahead> { new(value), new(value + 1) }; return list.Count + list[0].Value; }
}
internal static class Root1279
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Draw(int value) => Leaf1279.Value(value) + 1;
}
internal static class Leaf1279
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Value(int value) => value * 7;
}
internal interface IShape1279 { int Size(); }

internal static class PanelGate1279Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static MethodBase Of(Type type, string name) => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
    private static readonly Assembly Home = typeof(PanelGate1279Tests).Assembly;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CompiledBy(Func<int, int> pass)
    {
        var before = JitInfo.GetCompiledMethodCount(currentThread: true);
        pass(3);
        return JitInfo.GetCompiledMethodCount(currentThread: true) - before;
    }
    private static int PrepareAll(MethodBase root) => CallGraph.Closure([root], Home, 500).Count(CallGraph.Prepare);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Panel gate 1279: behaviour: closed while the code is not compiled and the bound not over, open once compiled, never closed again", () =>
        {
            long clock = 1_000;
            var compiled = false;
            var gate = new PanelGate(() => compiled, () => clock, TimeSpan.FromSeconds(3));
            Check(!gate.Open && gate.OpenedAfter == -1, "closed at the load");
            clock += 1_500;
            Check(!gate.Open, "still closed 1.5 s later, nothing compiled");
            compiled = true;
            clock += 200;
            Check(gate.Open && gate.OpenedAfter == 1_700 && !gate.ByBound, "open once compiled, 1.7 s after the load, not by the bound");
            compiled = false;
            clock += 10;
            Check(gate.Open, "stays open whatever the warm-up says next");
        }),
        ("Panel gate 1279: behaviour: the bound opens it when the code is still not compiled; an error of the warm-up opens it at once", () =>
        {
            long clock = 0;
            var gate = new PanelGate(() => false, () => clock, TimeSpan.FromSeconds(3));
            clock = 2_999;
            Check(!gate.Open, "closed just before the bound");
            clock = 3_000;
            Check(gate.Open && gate.ByBound && gate.OpenedAfter == 3_000, "open at the bound, said so");
            var broken = new PanelGate(() => throw new InvalidOperationException("warm-up gone"), () => 0, TimeSpan.FromSeconds(3));
            Check(broken.Open && !broken.ByBound, "an error never keeps the panels away");
            Check(PanelGate.Bound == TimeSpan.FromSeconds(3), "3 s at most, as the user decided");
        }),
        ("Call graph 1279: the callees read from the IL: another class, a lambda, generic code closed over the plugin's type; not itself", () =>
        {
            var callees = CallGraph.Callees(Of(typeof(Caller1279Cold), "Run"));
            Check(callees.Any(method => method.Name == "Twice" && method.DeclaringType == typeof(Callee1279Cold)) == false, "Twice is called by the lambda, not by Run");
            Check(callees.Any(method => method.Name == "Pick" && method is MethodInfo { IsGenericMethod: true } info && info.GetGenericArguments()[0] == typeof(Item1279Cold)), "the closed generic Pick<Item>");
            Check(callees.Any(method => method.Name.Contains("<Run>", StringComparison.Ordinal)), "the lambda (ldftn)");
            Check(callees.All(method => method != Of(typeof(Caller1279Cold), "Run")), "not itself");
            var closure = CallGraph.Closure([Of(typeof(Caller1279Cold), "Run")], Home, 500);
            Check(closure[0] == Of(typeof(Caller1279Cold), "Run"), "the root first");
            Check(closure.Any(method => method.Name == "Twice" && method.DeclaringType == typeof(Callee1279Cold)), "followed through the lambda to the other class");
            Check(closure.All(method => method.Module.Assembly == Home || method.DeclaringType!.IsGenericType || method.IsGenericMethod), "a library's plain method is never taken");
            Check(CallGraph.Closure([Of(typeof(Caller1279Cold), "Run")], Home, 1).Count == 1, "bounded by the count");
            Check(CallGraph.Closure([Of(typeof(Caller1279Cold), "Run")], Home, 500, () => true).Count == 0, "stopped before the first method");
        }),
        ("Call graph 1279: what is worth compiling: the plugin's own and generic code closed over its types, not a library's plain or shared code, not abstract", () =>
        {
            Check(CallGraph.Wanted(typeof(List<Item1279Cold>).GetMethod("Add")!, Home), "List<plugin record>.Add is made for this load");
            Check(!CallGraph.Wanted(typeof(List<int>).GetMethod("Add")!, Home), "List<int>.Add is the process's");
            Check(!CallGraph.Wanted(typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!, Home), "a library's plain method");
            Check(!CallGraph.Wanted(typeof(IShape1279).GetMethod("Size")!, Home), "an interface method has no body");
            Check(!CallGraph.Wanted(typeof(Callee1279Cold).GetMethod("Pick")!, Home), "an open generic method needs its arguments");
            Check(CallGraph.Wanted(Of(typeof(Leaf1279), "Value"), Home), "the plugin's own");
        }),
        ("Call graph 1279: behaviour: compiled ahead with its callees, a method compiles nothing on its first call; its cold twin compiles several", () =>
        {
            var cold = CompiledBy(Caller1279Cold.Run);
            Check(PrepareAll(Of(typeof(Caller1279Ahead), "Run")) >= 4, "the root, the lambda, Twice and Pick<Item> compiled ahead");
            var ahead = CompiledBy(Caller1279Ahead.Run);
            Check(cold >= 3, $"the cold twin compiled its callees on its first call ({cold})");
            Check(ahead == 0, $"nothing left to compile on the first call ({ahead}, cold {cold})");
        }),
        ("Call graph 1279: behaviour: generic library code closed over the plugin's type (a list of its records) compiled ahead too: fewer methods on the first call", () =>
        {
            var cold = CompiledBy(Lists1279Cold.Run);
            PrepareAll(Of(typeof(Lists1279Ahead), "Run"));
            var ahead = CompiledBy(Lists1279Ahead.Run);
            Check(cold >= 2 && ahead < cold, $"ahead {ahead} against cold {cold}");
        }),
        ("Panel gate 1279: the warm-up's first part: the panels' methods with what they call outside their class, ready before the rest; told done", () =>
        {
            using var warmup = JitWarmup.Start(() => [typeof(Root1279)], TimeSpan.Zero, early: method => method.DeclaringType == typeof(Root1279),
                firstPart: method => method.Name == "Draw");
            Check(warmup.Wait(TimeSpan.FromSeconds(30)), "the warm-up ends");
            Check(warmup.FirstPartDone && warmup.EarlyDone, "first part and early part told done");
            Check(warmup.FirstPartMethods == 2 && warmup.FirstPrepared == 2, $"Draw and the leaf it calls in another class ({warmup.FirstPartMethods}, {warmup.FirstPrepared})");
            Check(warmup.FirstReadyAfter >= 0, "the time it was ready");
            Check(warmup.Prepared == 2, $"nothing compiled twice: the rest leaves out what the first part took ({warmup.Prepared})");
        }),
        ("Panel gate 1279: wiring: the window's warm-up compiles the panels first; the overlay pass and the free panels wait for the gate; one line when they come back", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.cs"));
            Check(window.Contains("firstPart: Diagnostics.JitWarmup.OverlayFirst,", StringComparison.Ordinal), "first part: the overlays");
            Check(window.Contains("new Diagnostics.PanelGate(() => jitWarmup.FirstPartDone)", StringComparison.Ordinal), "the gate waits for it");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/PanelWindow.cs")).Contains("if (!owner.PanelsOpen) return false;", StringComparison.Ordinal), "a free panel waits");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("if(!panelsOpen)break;", StringComparison.Ordinal), "the overlay pass waits");
            Check(plugin.Contains("log.Information(mainWindow.PanelsOpenedText(", StringComparison.Ordinal), "the line of the frame where they come back");
        }),
    ];
}
