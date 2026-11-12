using System.Reflection;
using System.Text.RegularExpressions;
using TenshoAssistant.Diagnostics;

// 0.1188 (X-01 C3.3, analysis of the slow frames of 12/11): the methods of the map canvas's path whose name does not say Map (vendors,
// labels, edge markers, glyphs, arrow, quest index), the vendors' catalogue and the settings' icon sizes are compiled in the early phase.
internal static class MapWarmup1188Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class MainWindow
    {
        public void VendorLabel() { }
        public void DrawEdgeMarkers() { }
        public void DrawHome() { }
        public void VendorLabelsOfThePage() { }
        public Action Lambda() { var seen = 0; return () => seen += Local(); int Local() => seen; }
        public Func<int> FateLabel() { var count = 3; return () => count; }
    }
    private sealed class Configuration { public void SizeOf() { } public void PriorityOf() { } public void Save() { } }
    private sealed class Other { public void VendorLabel() { } public void SizeOf() { } }

    private static MethodBase Of(Type type, string name) => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public)!;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Map warm-up 1188: the canvas's path is early: the window's vendor and label methods and their lambdas, the vendors' namespace, the icon sizes of the settings", () =>
        {
            Check(JitWarmup.MapFirst(Of(typeof(MainWindow), "VendorLabel")) && JitWarmup.MapFirst(Of(typeof(MainWindow), "DrawEdgeMarkers")), "methods of the path");
            Check(!JitWarmup.MapFirst(Of(typeof(MainWindow), "DrawHome")) && !JitWarmup.MapFirst(Of(typeof(MainWindow), "VendorLabelsOfThePage")), "an exact name or its lambdas, nothing wider");
            var lambdas = typeof(MainWindow).GetNestedTypes(BindingFlags.NonPublic).SelectMany(nested => nested.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)).ToArray();
            Check(lambdas.Any(method => method.Name.Contains("<FateLabel>", StringComparison.Ordinal) && JitWarmup.MapFirst(method)), "a lambda of a path method");
            Check(lambdas.Where(method => method.Name.Contains("<Lambda>", StringComparison.Ordinal)).All(method => !JitWarmup.MapFirst(method)), "a lambda of another method is not early");
            Check(!JitWarmup.MapFirst(Of(typeof(Other), "VendorLabel")) && !JitWarmup.MapFirst(Of(typeof(Other), "SizeOf")), "another type is not the window nor the settings");
            Check(JitWarmup.MapFirst(Of(typeof(Configuration), "SizeOf")) && JitWarmup.MapFirst(Of(typeof(Configuration), "PriorityOf")) && !JitWarmup.MapFirst(Of(typeof(Configuration), "Save")), "the settings' icon sizes only");
            Check(JitWarmup.VendorNamespace == typeof(TenshoAssistant.Npcs.NpcVendorCatalog).Namespace && JitWarmup.MapFirst(typeof(TenshoAssistant.Npcs.NpcVendorCatalog).GetMethods().First(method => method.DeclaringType == typeof(TenshoAssistant.Npcs.NpcVendorCatalog))), "the vendors' catalogue");
            Check(JitWarmup.EarlyMethod(Of(typeof(MainWindow), "VendorLabel")), "in the early phase the window starts");
        }),
        ("Map warm-up 1188: cliquet: every name of the path is still declared in the sources, in the window or the settings", () =>
        {
            var folder = Path.GetFullPath("src/TenshoAssistant");
            var window = string.Join("\n", Directory.EnumerateFiles(folder, "MainWindow*.cs").Append(Path.Combine(folder, "UI", "Components", "DataIndexes.cs")).Select(File.ReadAllText));
            var missing = JitWarmup.MapPathMethods.Where(name => !Regex.IsMatch(window, @"(?m)^\s*(?:private|public|internal)[^=;(\r\n]*\s" + Regex.Escape(name) + @"\(")).ToArray();
            Check(missing.Length == 0, "not declared in the window any more: " + string.Join(", ", missing));
            var settings = File.ReadAllText(Path.Combine(folder, "Configuration.cs"));
            var gone = JitWarmup.MapPathSettings.Where(name => !Regex.IsMatch(settings, @"\s" + Regex.Escape(name) + @"\(")).ToArray();
            Check(gone.Length == 0, "not declared in the settings any more: " + string.Join(", ", gone));
            Check(File.ReadAllText(Path.Combine(folder, "UI", "Components", "DataIndexes.cs")).Contains("internal sealed partial class MainWindow", StringComparison.Ordinal), "the quest index is the window's");
        }),
    ];
}
