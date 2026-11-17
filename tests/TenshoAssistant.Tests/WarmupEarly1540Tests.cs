using System.Reflection;
using TenshoAssistant.Diagnostics;

// X-573 M2 (review 0.1539): the early phase's end measured and told done at the same moment, also when every method is early or there
// is none: the chat window (WarmupEarlyDone) and the held compiler never wait while the log already says the phase ready.
internal static class WarmupEarly1540Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    internal static class Early1540
    {
        public static int A() => 1;
        public static int B() => 2;
    }

    private static (bool Done, double Ready, int EarlyPrepared, int Prepared) AtTheLog(Func<IReadOnlyList<Type>> roots, Func<MethodBase, bool> early)
    {
        var seen = (Done: false, Ready: -1.0, EarlyPrepared: -1, Prepared: -1);
        using var warmup = JitWarmup.Start(roots, TimeSpan.Zero, finished: w => seen = (w.EarlyDone, w.EarlyReadyAfter, w.EarlyPrepared, w.Prepared), early: early);
        Check(warmup.Wait(TimeSpan.FromSeconds(30)), "the warm-up ends");
        Check(warmup.EarlyDone, "told done after the end");
        return seen;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Warmup early 1540: X-573 M2, behaviour: every method early: when the log line is written, the phase is measured AND told done", () =>
        {
            var all = AtTheLog(() => [typeof(Early1540)], _ => true);
            Check(all.Ready >= 0 && all.Done && all.EarlyPrepared == all.Prepared && all.Prepared >= 2, $"every method early: {all}");
        }),
        ("Warmup early 1540: X-573 M2, behaviour: no method at all: measured and told done at the log line", () =>
        {
            var none = AtTheLog(() => [], _ => true);
            Check(none.Ready >= 0 && none.Done && none.Prepared == 0, $"no method: {none}");
        }),
        ("Warmup early 1540: X-573 M2, behaviour: part of the methods early: still told done before the log line", () =>
        {
            var part = AtTheLog(() => [typeof(Early1540)], method => method.Name == "A");
            Check(part.Ready >= 0 && part.Done && part.EarlyPrepared < part.Prepared, $"part early: {part}");
        }),
    ];
}
