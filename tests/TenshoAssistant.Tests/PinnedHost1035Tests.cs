using System.Text.RegularExpressions;

// X-124 (0.1035): PluginUiTest moved to its own repository, Shigu330/PluginUiTest (GPL v3, its own CI and releases). Tensho keeps only
// its library, tools/PluginUiTest/src/PluginUiTest.Host, as a pinned copy referenced by the desktop application, so this repository
// builds without the other one. The tool, its sample and its CI example are gone from here; the short README says where the source is.
internal static class PinnedHost1035Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");
    private const string Root = "tools/PluginUiTest/";

    internal static readonly (string Name,Action Run)[] Cases =
    [
        ("Pinned PluginUiTest.Host: only the library stays; the tool, the sample and the CI example moved to Shigu330/PluginUiTest",() =>
        {
            var tracked = Directory.EnumerateFiles(Path.GetFullPath(Root),"*",SearchOption.AllDirectories)
                .Select(f=>Path.GetRelativePath(Path.GetFullPath(Root),f).Replace('\\','/'))
                .Where(f=>!f.Contains("/bin/",StringComparison.Ordinal) && !f.Contains("/obj/",StringComparison.Ordinal)).ToList();
            Check(tracked.All(f=>f == "README.md" || f.StartsWith("src/PluginUiTest.Host/",StringComparison.Ordinal)),"only README.md and src/PluginUiTest.Host: "+string.Join(", ",tracked.Where(f=>f != "README.md" && !f.StartsWith("src/PluginUiTest.Host/",StringComparison.Ordinal)).Take(5)));
            foreach (var gone in new[]{"src/PluginUiTest/Program.cs","samples/SamplePlugin/Plugin.cs","docs/github-actions-example.yml"})
                Check(!File.Exists(Path.GetFullPath(Root+gone)),"moved to the PluginUiTest repository: "+gone);
            var files = Directory.EnumerateFiles(Path.GetFullPath(Root+"src/PluginUiTest.Host"),"*.cs").Select(Path.GetFileName).Order().ToArray();
            Check(files.SequenceEqual(["CallGraph.cs","DalamudPaths.cs","DalamudRuntime.cs","HostWindows.cs","ImGuiHeadless.cs","Rewrite.cs","StandIns.cs","Win32Gl.cs"]),"the eight files of the library: "+string.Join(", ",files));
            var project = Read(Root+"src/PluginUiTest.Host/PluginUiTest.Host.csproj");
            Check(project.Contains("Pinned copy",StringComparison.Ordinal) && project.Contains("Shigu330/PluginUiTest",StringComparison.Ordinal),"the project says it is a pinned copy and where the source is");
            Check(Read("src/TenshoAssistant.Desktop/TenshoAssistant.Desktop.csproj").Contains("pinned copy",StringComparison.Ordinal),"the desktop app's reference says pinned copy");
            Check(!Read("tests/TenshoAssistant.Tests/TenshoAssistant.Tests.csproj").Contains("src/PluginUiTest/",StringComparison.Ordinal),"no test links a file of the removed tool");
        }),
        ("Pinned PluginUiTest.Host: the README points at the repository, gives the pinned version and commit, English then French",() =>
        {
            var readme = Read(Root+"README.md");
            var halves = readme.Split("\n---\n");
            Check(halves.Length == 2,"README: English, ---, French");
            int Headings(string half) => Regex.Matches(half,@"^#{1,3} ",RegexOptions.Multiline).Count;
            Check(Headings(halves[0]) == Headings(halves[1]) && Headings(halves[0]) >= 3,"same headings in both halves");
            foreach (var half in halves)
            {
                Check(half.Contains("https://github.com/Shigu330/PluginUiTest",StringComparison.Ordinal),"each half links the repository");
                Check(Regex.IsMatch(half,@"v\d+\.\d+\.\d+") && Regex.IsMatch(half,@"`[0-9a-f]{7,40}`"),"each half gives the pinned version and commit");
                Check(half.Contains("GPL v3",StringComparison.Ordinal),"each half gives the licence");
            }
            Check(halves[0].Contains("Test tool, not affiliated with the Dalamud team, does not replace Dalamud.",StringComparison.Ordinal) && halves[1].Contains("non affilié à l'équipe de Dalamud",StringComparison.Ordinal),"the banner in both halves");
            var tools = Read("tools/README.md");
            Check(Regex.Matches(tools,@"Shigu330/PluginUiTest").Count >= 2,"tools/README.md points at the repository in both halves");
        }),
    ];
}
