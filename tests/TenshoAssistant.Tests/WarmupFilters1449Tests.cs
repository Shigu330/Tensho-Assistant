using System.Reflection;
using TenshoAssistant.Diagnostics;

// 0.1449 (X-433): 0.1448 measured 82 MB of strings on the JIT warm-up's thread in the two minutes after a load: its filters built
// « <Name> » for every method and every name of their lists. The filters now allocate nothing per method and answer as before; the early
// filter is asked once per method.
internal static class WarmupFilters1449Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class MainWindow
    {
        public void DrawSettingsPage() { }
        public void DrawGatheringPanel() { }
        public void DrawMain() { }
        public void VendorLabel() { }
        public void DrawMapCanvas() { }
        public Action Lambda() => () => DrawGatheringPanel();
        public Action Window() => () => DrawMain();
    }
    private sealed class Other { public void DrawMain() { } }

    private static MethodBase Of(Type type, string name) => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public)!;
    private static MethodBase Nested(Type type, string outer) => type.GetNestedTypes(BindingFlags.NonPublic)
        .SelectMany(nested => nested.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        .Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        .First(method => method.Name.Contains("<" + outer + ">", StringComparison.Ordinal));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Warm-up filters 1449: the answers are those of before: names, lambdas in angle brackets, another type", () =>
        {
            Check(JitWarmup.WindowFirst(Of(typeof(MainWindow), "DrawMain")) && !JitWarmup.WindowFirst(Of(typeof(Other), "DrawMain")), "the window's frame, not another type's");
            Check(JitWarmup.WindowFirst(Nested(typeof(MainWindow), "Window")) == false, "a lambda of Window() is not the frame");
            Check(JitWarmup.OverlayFirst(Of(typeof(MainWindow), "DrawGatheringPanel")) && JitWarmup.OverlayFirst(Nested(typeof(MainWindow), "Lambda")) == false, "an overlay; Lambda's own lambda is not");
            Check(JitWarmup.MapFirst(Of(typeof(MainWindow), "VendorLabel")) && JitWarmup.MapFirst(Of(typeof(MainWindow), "DrawMapCanvas")), "the map's path and a name saying Map");
            Check(!JitWarmup.EarlyMethod(Of(typeof(MainWindow), "DrawSettingsPage")) && !JitWarmup.WindowFirst(Of(typeof(MainWindow), "DrawSettingsPage")), "a page is none of them");
        }),
        ("Warm-up filters 1449: asking the filters of a window's method allocates nothing", () =>
        {
            var methods = new[] { Of(typeof(MainWindow), "DrawSettingsPage"), Of(typeof(MainWindow), "DrawGatheringPanel"), Of(typeof(MainWindow), "VendorLabel"), Nested(typeof(MainWindow), "Lambda") };
            foreach (var method in methods) { JitWarmup.EarlyMethod(method); JitWarmup.WindowFirst(method); JitWarmup.OverlayFirst(method); JitWarmup.MapFirst(method); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            var hits = 0;
            for (var round = 0; round < 1000; round++)
                foreach (var method in methods)
                {
                    if (JitWarmup.EarlyMethod(method)) hits++;
                    if (JitWarmup.WindowFirst(method)) hits++;
                    if (JitWarmup.OverlayFirst(method)) hits++;
                }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(hits > 0, "some are early");
            // 0.1453: 64 KB, not 4: a collection during the loop (tests in parallel) lets the runtime drop its weakly held reflection caches,
            // refilled on the next call (8,920 bytes once in Verify). Before 0.1449 these 12,000 answers built megabytes of strings.
            Check(allocated < 65536, "allocated for 12,000 answers: " + allocated + " bytes (each built strings before 0.1449)");
        }),
        ("Warm-up filters 1449: the early filter is asked once per method; the order is kept and the early ones counted", () =>
        {
            var methods = new[] { Of(typeof(MainWindow), "DrawSettingsPage"), Of(typeof(MainWindow), "DrawGatheringPanel"), Of(typeof(Other), "DrawMain"), Of(typeof(MainWindow), "VendorLabel") };
            var asked = new Dictionary<MethodBase, int>();
            var ordered = JitWarmup.EarlyFirst(methods, method => { asked[method] = asked.GetValueOrDefault(method) + 1; return JitWarmup.EarlyMethod(method); }, out var early);
            Check(asked.Count == methods.Length && asked.Values.All(count => count == 1), "once each");
            Check(early == 2 && ordered.SequenceEqual(new[] { methods[1], methods[3], methods[0], methods[2] }), "early first, each part in order, counted");
            Check(JitWarmup.EarlyFirst(methods, JitWarmup.EarlyMethod).SequenceEqual(ordered), "the two-argument form gives the same order");
            var warmup = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/JitWarmup.cs"));
            Check(!warmup.Contains("methods.Count(early)", StringComparison.Ordinal) && warmup.Contains("EarlyFirst(methods,early,out earlyCount)", StringComparison.Ordinal), "the warm-up uses the one pass");
            Check(!warmup.Contains("\"<\"+window+\">\"", StringComparison.Ordinal) && !warmup.Contains("\"<\"+overlay+\">\"", StringComparison.Ordinal) && !warmup.Contains("\"<\"+path+\">\"", StringComparison.Ordinal), "no string built per method");
        }),
    ];
}
