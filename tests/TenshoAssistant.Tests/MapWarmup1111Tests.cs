// 0.1111 (X-01 suite): the map's own types (TenshoAssistant.Maps) join the early part of the JIT warm-up, after 0.1104 left their
// compiling in the map's first frame.
using TenshoAssistant.Diagnostics;
using TenshoAssistant.Maps;

internal static class MapWarmup1111Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("JIT warm-up 1111: every method of the Maps namespace goes first, nested types included; other namespaces do not",() =>
        {
            Check(JitWarmup.MapNamespace=="TenshoAssistant.Maps" && typeof(MapLocalIndex).Namespace==JitWarmup.MapNamespace,"the map's namespace");
            Check(JitWarmup.MapFirst(typeof(MapLocalIndex).GetMethod("Quest")!),"a method whose name does not say Map");
            Check(JitWarmup.MapFirst(typeof(MapLocalLists).GetMethod("Quests")!),"a static class of the namespace");
            var nested=typeof(MapLocalIndex).Assembly.GetTypes().Where(type=>type.Namespace==JitWarmup.MapNamespace && type.IsNested)
                .SelectMany(type=>type.GetMethods(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.DeclaredOnly)).FirstOrDefault();
            Check(nested is null || JitWarmup.MapFirst(nested),"a nested type (lambdas) too");
            Check(!JitWarmup.MapFirst(typeof(TenshoAssistant.Items.Wishlist).GetMethod("Toggle")!),"another namespace waits as before");
        }),
    ];
}
