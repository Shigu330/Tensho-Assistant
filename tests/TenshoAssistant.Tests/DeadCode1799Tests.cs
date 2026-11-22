// 0.1799 (X-569, second slice; audit of 17/11, part C): the seven probes of 31/10 that no longer compiled (the plugin's files they linked
// moved since) are retired from tools/ and from both halves of tools/README.md; the three unused test helpers are gone. Kept absent here.
internal static class DeadCode1799Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly string[] Retired =
        ["CollectionProbe","ToolBonusProbe","FashionProbe","GatheringProbe","QuestRewardsProbe","CraftSimulationProbe","GatheringActionsProbe"];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Dead code 1799: the seven broken probes are out of tools/ and of both halves of its README",() =>
        {
            var readme = File.ReadAllText(Path.GetFullPath("tools/README.md"));
            foreach (var probe in Retired)
            {
                var folder = Path.GetFullPath(Path.Combine("tools",probe));
                Check(!Directory.Exists(folder) || Directory.GetFiles(folder,"*.csproj").Length == 0 && Directory.GetFiles(folder,"*.cs").Length == 0,probe + ": no project left");
                Check(!readme.Contains("`" + probe + "`",StringComparison.Ordinal),probe + ": not listed");
            }
            Check(readme.Contains("| `XbmProbe` |",StringComparison.Ordinal) && readme.Contains("| `ShopCurrencyProbe` |",StringComparison.Ordinal),"the general probes stay");
        }),
        ("Dead code 1799: the unused test helpers stay gone",() =>
        {
            static string Read(string name) => File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/" + name));
            Check(!Read("CollectablesTests.cs").Contains("private static GpOptimization Node(",StringComparison.Ordinal),"CollectablesTests Node");
            Check(!Read("LinkCategories1119Tests.cs").Contains("private static bool Wait(",StringComparison.Ordinal),"LinkCategories1119Tests Wait");
            Check(!Read("SlowCompilations1424Tests.cs").Contains("private static void Other()",StringComparison.Ordinal),"SlowCompilations1424Tests Other");
        }),
    ];
}
