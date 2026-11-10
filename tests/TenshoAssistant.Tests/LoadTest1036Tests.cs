using System.Text.RegularExpressions;

// X-93 part 1 (0.1036): the load test lives outside Verify (minutes at a million facts); here only that its two tools, its evidence and its link to
// the plugin's code stay in place, and that the C# probe and the Python harness invent facts from the same table of kinds.
internal static class LoadTest1036Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Load test 1036: tools and evidence kept, same kind table in C# and Python",() =>
        {
            var probe=File.ReadAllText(Path.GetFullPath("tools/DiscoveryLoadProbe/Program.cs"));
            var project=File.ReadAllText(Path.GetFullPath("tools/DiscoveryLoadProbe/DiscoveryLoadProbe.csproj"));
            var harness=File.ReadAllText(Path.GetFullPath("tools/discoveries-load.py"));
            Check(File.Exists(Path.GetFullPath("docs/evidence/x93-load-test.md")),"evidence");
            Check(project.Contains("src/TenshoAssistant/Discoveries/DiscoveriesModel.cs") && project.Contains("src/TenshoAssistant/Discoveries/CommunityDiscoveries.cs"),"the probe calls the plugin's own code");
            Check(harness.Contains("discoveries-merge.py") && harness.Contains("merge.rebuild()") && harness.Contains("merge.clean("),"the harness calls the merge tool as it is");
            // (kind, weight, subjects, details, lowest, highest), in the same order on both sides.
            var csharp=Regex.Matches(probe,@"\(DiscoveryKind\.(\w+),(\d+),([\d_]+),([\d_]+),(\d+),(\d+)\)").Select(match=>
                ((int)Enum.Parse<TenshoAssistant.Discoveries.DiscoveryKind>(match.Groups[1].Value),string.Join(",",Enumerable.Range(2,5).Select(group=>match.Groups[group].Value.Replace("_",""))))).ToArray();
            var python=Regex.Matches(harness,@"\((\d+),(\d+),(\d+),(\d+),(\d+),(\d+)\)").Select(match=>
                (int.Parse(match.Groups[1].Value),string.Join(",",Enumerable.Range(2,5).Select(group=>match.Groups[group].Value)))).ToArray();
            Check(csharp.Length==35 && csharp.SequenceEqual(python),$"kind tables differ: C# {csharp.Length}, Python {python.Length}");
        }),
    ];
}
