using System.Reflection;
using TenshoAssistant.Diagnostics;

// 0.1050 (X-01, measured without the game): the window's code compiled ahead on a background thread, so that a page's first frame is
// not spent compiling it (desktop headless run: Reminders 40.3 ms on its first frame, 38.9 ms of it JIT).
internal static class JitWarmup1050Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private abstract class Sample
    {
        public int Plain(int value)=>value+1;
        public abstract void Abstract();
        public T Generic<T>(T value)=>value;
        public Func<int,int> Lambda()=>value=>value*2;
        private sealed class Inner { public static int Nested()=>3; }
        private sealed class Open<T> { public T? Value()=>default; }
    }

    private static string Root(string file)=>Path.GetFullPath(file);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("JIT warm-up 1050: the methods kept are the type's and its nested types' (lambdas, closures), never generic, abstract or of an open generic type",() =>
        {
            var names=JitWarmup.Methods([typeof(Sample)]).Select(method=>method.DeclaringType!.Name+"."+method.Name).ToArray();
            Check(names.Contains("Sample.Plain") && names.Contains("Sample.Lambda"),"the type's own methods: "+string.Join(", ",names));
            Check(names.Contains("Inner.Nested"),"a nested type's method is kept");
            Check(names.Any(name=>name.StartsWith("<>c",StringComparison.Ordinal)),"the lambda's compiler-made class is kept");
            Check(!names.Contains("Sample.Abstract") && !names.Contains("Sample.Generic"),"abstract and generic methods are skipped");
            Check(!names.Any(name=>name.StartsWith("Open",StringComparison.Ordinal)),"an open generic type is skipped");
            var order=JitWarmup.FirstThenAssembly(typeof(Sample));
            Check(order[0]==typeof(Sample) && order.Count(type=>type==typeof(Sample))==1 && order.Contains(typeof(JitWarmup1050Tests)),"the first type, then the rest of its assembly, once each");
            var first=JitWarmup.Methods(order).Select(method=>method.DeclaringType!).TakeWhile(type=>type==typeof(Sample) || type.DeclaringType==typeof(Sample)).Count();
            Check(first>=names.Length,"the first type and its nested types come before the rest");
        }),
        ("JIT warm-up 1050: it compiles on its own thread after the delay, tells once when finished, and stops on dispose",() =>
        {
            var told=0;
            using(var warmup=JitWarmup.Start(()=>[typeof(Sample)],TimeSpan.Zero,_=>Interlocked.Increment(ref told)))
            {
                Check(warmup.Wait(TimeSpan.FromSeconds(30)),"finished in time");
                Check(warmup.Completed && warmup.Prepared>=3 && warmup.Skipped==0,$"prepared {warmup.Prepared}, skipped {warmup.Skipped}");
            }
            Check(told==1,"finished told once");
            var stopped=JitWarmup.Start(()=>[typeof(Sample)],TimeSpan.FromMinutes(5),_=>Interlocked.Increment(ref told));
            var watch=System.Diagnostics.Stopwatch.StartNew();
            stopped.Dispose();
            Check(watch.Elapsed<TimeSpan.FromSeconds(3) && !stopped.Completed && told==1,"a warm-up still waiting stops at once, without compiling or telling");
            stopped.Dispose();
        }),
        ("JIT warm-up 1050: the window starts it on its own type and disposes it with itself",() =>
        {
            var window=File.ReadAllText(Root("src/TenshoAssistant/MainWindow.cs"));
            Check(window.Contains("jitWarmup = Diagnostics.JitWarmup.Start(() => Diagnostics.JitWarmup.FirstThenAssembly(typeof(MainWindow)), Diagnostics.JitWarmup.Delay,",StringComparison.Ordinal),"started in the window's constructor, the window's own code first");
            Check(File.ReadAllText(Root("src/TenshoAssistant/MainWindow.Lifetime.cs")).Contains("jitWarmup.Dispose();",StringComparison.Ordinal),"disposed with the window");
            Check(typeof(JitWarmup).GetMethod("Wait")?.GetParameters() is [{ ParameterType: var limit }] && limit==typeof(TimeSpan),"Wait(TimeSpan) stays: the desktop headless run calls it by name");
        }),
    ];
}
