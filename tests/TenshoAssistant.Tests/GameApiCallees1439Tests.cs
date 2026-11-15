using System.Reflection;
using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1439 (X-433): the readers' first passes still compiled 7 to 29 methods in a frame of the game after their class was compiled (five
// loads of 15/11); the call graph followed the plugin's methods only. What a reader calls directly in the game's API assemblies (Dalamud,
// FFXIVClientStructs, InteropGenerator.Runtime, Lumina) is now compiled with its class, as a leaf: taken, its own calls not followed, within
// the cap. The test project has no game assembly: the leaf rule is checked with a stand-in predicate (Math), the game-API rule by its names.

internal sealed class Callees1439Reader
{
    private int total;
    [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(int tick) { total += Math.Max(tick,Own(tick)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private int Own(int value) => value + 1;
}

internal static class GameApiCallees1439Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static MethodInfo Method(Type type,string name) => type.GetMethod(name,BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game API callees 1439: a leaf outside the plugin is taken with the reader's methods and not followed; without the leaf rule, as before",() =>
        {
            var reader = typeof(Callees1439Reader);
            var roots = new MethodBase[]{ Method(reader,"OnUpdate") };
            var home = reader.Assembly;
            var max = typeof(Math).GetMethod("Max",[typeof(int),typeof(int)])!;
            var plain = CallGraph.Closure(roots,home,10);
            Check(plain.Contains(Method(reader,"Own")) && !plain.Contains(max),"no leaf rule: the plugin's callee only");
            var withLeaves = CallGraph.Closure(roots,home,10,leaf: method => method.DeclaringType == typeof(Math));
            Check(withLeaves.Contains(max) && withLeaves.Contains(Method(reader,"Own")),"the outside method called directly is taken, with the plugin's own");
            Check(withLeaves.All(method => method.Module.Assembly == home || method == max),"nothing it calls is followed");
            Check(CallGraph.Closure(roots,home,1,leaf: method => method.DeclaringType == typeof(Math)).Count == 1,"the cap still holds");
        }),
        ("Game API callees 1439: the game's API assemblies by name; the runtime's own and interfaces are not leaves; the readers' path asks for them",() =>
        {
            Check(CallGraph.GameApiAssemblies.SetEquals(["Dalamud","FFXIVClientStructs","InteropGenerator.Runtime","Lumina","Lumina.Excel"]),"the five assemblies");
            Check(!CallGraph.GameApi(typeof(Math).GetMethod("Max",[typeof(int),typeof(int)])!),"a method of the runtime is not a leaf");
            Check(!CallGraph.GameApi(typeof(IDisposable).GetMethod("Dispose")!),"an interface's method is not a leaf");
            Check(!CallGraph.GameApi(Method(typeof(Callees1439Reader),"Own")),"the test's own assembly is not the game's API");
            var warmup = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/JitWarmup.cs"));
            Check(warmup.Contains("CallGraph.Closure(methods,owner.Assembly,methods.Count+callees,stop,CallGraph.GameApi)",StringComparison.Ordinal),"PrepareOwner (the readers' compiler) takes the game's API leaves");
            Check(warmup.Split("CallGraph.GameApi").Length == 2,"only there: the warm-up of the window does not take them");
        }),
    ];
}
