using System.Runtime;
using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1198 (X-177 C4.2): at a reload, the first pass of a reader took 125 to 276 ms of one frame on the game's thread: its code was compiled
// the first time it ran. A reader's own code is now compiled before its first pass (0.1198: when it subscribes, on the subscribing thread;
// 0.1199: on a thread of its own, ReaderStart1199Tests); and a slow pass says how much of it was compiling. Behaviour tests on readers never run before: the runtime's own count of the methods the calling thread
// compiled tells whether a first pass still compiles its class.

// Readers as the plugin's are: top-level classes, their lambdas nested in them. Each is used by one test only (a class is compiled once).
internal sealed class Reader1198Shared
{
    private int total;
    [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(int tick) { total += First(tick) + Second(tick); Action<int> later = value => total += Third(value); later(tick); }
    [MethodImpl(MethodImplOptions.NoInlining)] private int First(int value) => value + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Second(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Third(int value) => value - 3;
    public int Total => total;
}
internal sealed class Reader1198Cold
{
    private int total;
    [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(int tick) { total += First(tick) + Second(tick); Action<int> later = value => total += Third(value); later(tick); }
    [MethodImpl(MethodImplOptions.NoInlining)] private int First(int value) => value + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Second(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Third(int value) => value - 3;
    public int Total => total;
}
internal sealed class Reader1198Ahead
{
    private int total;
    [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(int tick) { total += First(tick) + Second(tick); Action<int> later = value => total += Third(value); later(tick); }
    [MethodImpl(MethodImplOptions.NoInlining)] private int First(int value) => value + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Second(int value) => value * 2;
    [MethodImpl(MethodImplOptions.NoInlining)] private int Third(int value) => value - 3;
    public int Total => total;
}
internal sealed class Reader1198Closure
{
    public int Total;
    // A handler that is a lambda over a local: the compiler puts it in a closure class nested in this one.
    public Action<int> Handler(int offset) => tick => Total += Add(tick, offset);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int Add(int left, int right) => left + right;
    [MethodImpl(MethodImplOptions.NoInlining)] public int Other(int value) => value * 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public int Another(int value) => value * 5;
}

internal static class ReaderWarmup1198Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // The methods this thread compiled while the pass ran.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CompiledBy(Action<int> pass)
    {
        var before = JitInfo.GetCompiledMethodCount(currentThread: true);
        pass(1);
        return JitInfo.GetCompiledMethodCount(currentThread: true) - before;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Reader warm-up 1198: behaviour: a reader compiled when it subscribes no longer compiles its class in its first pass; one left alone still does", () =>
        {
            // What both passes share (the measure itself, the delegates' machinery) is compiled by a first reader of the same shape.
            var shared = new Reader1198Shared();
            CompiledBy(shared.OnUpdate);
            var cold = new Reader1198Cold(); var ahead = new Reader1198Ahead();
            Action<int> coldPass = cold.OnUpdate, aheadPass = ahead.OnUpdate;
            var prepared = JitWarmup.PrepareOwner(aheadPass.Method);
            Check(prepared >= 5, "the handler, the three methods it calls and its lambda at least: " + prepared);
            var coldCompiled = CompiledBy(coldPass);
            var aheadCompiled = CompiledBy(aheadPass);
            Check(cold.Total == ahead.Total && ahead.Total == 2 + 2 - 2, "both passes ran: " + cold.Total + ", " + ahead.Total);
            Check(coldCompiled >= 5, "the leak of time reproduced: a first pass compiles its class in the frame: " + coldCompiled);
            Check(aheadCompiled <= coldCompiled - 5, $"compiled ahead: the first pass finds its class compiled ({aheadCompiled} methods compiled in the pass against {coldCompiled})");
        }),
        ("Reader warm-up 1198: the class of a handler is the outermost type around it; a class over the cap gives only the handler's own type", () =>
        {
            var reader = new Reader1198Closure();
            var handler = reader.Handler(2);
            Check(handler.Method.DeclaringType != typeof(Reader1198Closure) && JitWarmup.Owner(handler.Method) == typeof(Reader1198Closure), "a lambda's closure is nested in its reader: " + handler.Method.DeclaringType);
            Func<int, int> direct = reader.Other;
            Check(JitWarmup.Owner(direct.Method) == typeof(Reader1198Closure), "a method of the reader itself");
            var whole = JitWarmup.Methods([typeof(Reader1198Closure)]).Count;
            var own = JitWarmup.Methods([handler.Method.DeclaringType!]).Count;
            Check(own >= 1 && own < whole, $"the closure is a part of the class: {own} of {whole}");
            // Over the cap: the closure alone; a cap even the closure passes: nothing, and never an error.
            Check(JitWarmup.PrepareOwner(handler.Method, whole - 1) == own, "over the cap: only the handler's own type");
            Check(JitWarmup.PrepareOwner(handler.Method, 0) == 0, "nothing under a cap of zero");
            Check(JitWarmup.PrepareOwner(handler.Method) == whole, "under the cap: the whole class, nested types included");
            handler(1);
            Check(reader.Total == 3 && JitWarmup.OwnerCap == 600, "the handler still runs as before");
        }),
        ("Reader warm-up 1198: a slow pass says how much of it was compiling, in whole milliseconds, and nothing when nothing was compiled", () =>
        {
            Check(JitWarmup.CompiledText(212.4, 318) == " (212 ms of it compiling 318 methods)", JitWarmup.CompiledText(212.4, 318));
            Check(JitWarmup.CompiledText(0, 0) == "" && JitWarmup.CompiledText(3, -1) == "", "nothing compiled: nothing said");
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
                Check(JitWarmup.CompiledText(1234.5, 7) == " (1234 ms of it compiling 7 methods)" || JitWarmup.CompiledText(1234.5, 7) == " (1235 ms of it compiling 7 methods)", JitWarmup.CompiledText(1234.5, 7));
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
        }),
        ("Reader warm-up 1198: wiring: a slow pass carries its compiling part, read for the calling thread (0.1199: the subscriptions' own wiring is in ReaderStart1199Tests)", () =>
        {
            var gate = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs")).ReplaceLineEndings("\n");
            Check(gate.Contains("SlowSubscription?.Invoke(name+Diagnostics.JitWarmup.CompiledText((System.Runtime.JitInfo.GetCompilationTime(true)-compiledBefore).TotalMilliseconds,", StringComparison.Ordinal), "the compiling part of a slow pass, read for the calling thread");
        }),
    ];
}
