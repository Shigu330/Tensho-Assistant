// 0.1104 (X-01): after 0.1103 the map's first frame after a load was still 358 ms, mostly compiling: its methods are compiled first, half a
// second after loading, the rest as before.
using System.Reflection;
using TenshoAssistant.Diagnostics;

internal static class MapWarmup1104Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private sealed class MainWindow
    {
        public void DrawMapCanvas() { }
        public void DrawHome() { }
        public sealed class Inner { public void MapNested() { } }
    }
    private sealed class Other { public void DrawMap() { } }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("JIT warm-up: the window's map methods first, then the rest in their order",() =>
        {
            var methods = JitWarmup.Methods([typeof(MainWindow),typeof(Other)]);
            Check(JitWarmup.MapFirst(typeof(MainWindow).GetMethod("DrawMapCanvas")!) && JitWarmup.MapFirst(typeof(MainWindow.Inner).GetMethod("MapNested")!),"the window's map methods, nested types too");
            Check(!JitWarmup.MapFirst(typeof(MainWindow).GetMethod("DrawHome")!) && !JitWarmup.MapFirst(typeof(Other).GetMethod("DrawMap")!),"not another page, not another type");
            var ordered = JitWarmup.EarlyFirst(methods,JitWarmup.MapFirst);
            var early = ordered.TakeWhile(JitWarmup.MapFirst).Count();
            Check(early == methods.Count(JitWarmup.MapFirst) && early == 2 && ordered.Count == methods.Count,"the map methods first, none lost");
            Check(ordered.Skip(early).SequenceEqual(methods.Where(method => !JitWarmup.MapFirst(method))),"the rest in their former order");
            Check(JitWarmup.EarlyDelay < JitWarmup.Delay,"the map part starts before the rest");
        }),
        ("JIT warm-up: an early part finishes and the whole run is told once",() =>
        {
            var told = 0;
            using (var warmup = JitWarmup.Start(() => [typeof(MainWindow),typeof(Other)],TimeSpan.FromMilliseconds(600),_ => Interlocked.Increment(ref told),JitWarmup.MapFirst))
            {
                Check(warmup.Wait(TimeSpan.FromSeconds(20)) && warmup.Completed,"finished");
                Check(warmup.EarlyPrepared == 2 && warmup.Prepared >= 4,"early part counted: " + warmup.EarlyPrepared + " of " + warmup.Prepared);
            }
            Check(told == 1,"told once");
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.cs"));
            Check(window.Contains("Diagnostics.JitWarmup.MapFirst);",StringComparison.Ordinal) && window.Contains("of the map first.",StringComparison.Ordinal),"the window starts it with the map first");
        }),
    ];
}
