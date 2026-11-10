using System.Text.RegularExpressions;
using PluginUiTest.Host;

// X-120: tools/PluginUiTest, a test tool for Dalamud plugin creators (draw every window of a plugin without the game, say what breaks).
// X-124 (0.1035): the tool moved to its own repository (Shigu330/PluginUiTest); its command line and report are tested there. What stays
// here is the wiring of its library, kept as a pinned copy for the desktop application (PinnedHost1035Tests): the generic code moved out
// of the desktop application (not copied, R11), nothing of Dalamud shipped (R05), no request in the library, the Dalamud paths.
internal static class PluginUiTest1032Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");
    private const string Root = "tools/PluginUiTest/";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Plugin UI test: the Dalamud paths of the library (installedPlugins under XIVLauncher)",() =>
        {
            var installed = DalamudPaths.InstalledPlugins(@"C:\Users\X\AppData\Roaming");
            Check(installed == Path.Combine(@"C:\Users\X\AppData\Roaming","XIVLauncher","installedPlugins"),"installedPlugins under XIVLauncher");
        }),
        ("Plugin UI test: the generic code moved out of the desktop app (not copied), referenced back; nothing of Dalamud shipped; no request in the library",() =>
        {
            var desktop = Path.GetFullPath("src/TenshoAssistant.Desktop");
            foreach (var moved in new[]{"Rewrite.cs","Win32Gl.cs"})
            {
                Check(!File.Exists(Path.Combine(desktop,moved)),"moved, not copied: src/TenshoAssistant.Desktop/"+moved);
                Check(File.Exists(Path.GetFullPath(Root+"src/PluginUiTest.Host/"+moved)),"in the library: "+moved);
            }
            var desktopCode = string.Join("\n",Directory.EnumerateFiles(desktop,"*.cs").Select(File.ReadAllText));
            foreach (var type in new[]{"class AbsentProxy","class FrameworkProxy","class LogProxy","class DataManagerStub","class TextureProviderProxy","class Win32Gl","class Rewrite","AssemblyLoadContext.Default.Resolving","public static string Hooks(","SqpackCandidates(string?"})
                Check(!desktopCode.Contains(type,StringComparison.Ordinal),"the desktop app no longer holds: "+type);
            Check(Read("src/TenshoAssistant.Desktop/TenshoAssistant.Desktop.csproj").Contains(@"<ProjectReference Include=""..\..\tools\PluginUiTest\src\PluginUiTest.Host\PluginUiTest.Host.csproj"" />",StringComparison.Ordinal),"the app references the library");
            var text = Read(Root+"src/PluginUiTest.Host/PluginUiTest.Host.csproj");
            var references = Regex.Matches(text,@"<Reference Include=""([^""]+)"">(.*?)</Reference>",RegexOptions.Singleline);
            Check(references.Count >= 6 && references.All(r=>r.Groups[2].Value.Contains("<Private>false</Private>",StringComparison.Ordinal)),"every Dalamud, Lumina, ImGui, Cecil reference stays where installed (R05)");
            Check(!text.Contains("PackageReference",StringComparison.Ordinal),"no package");
            Check(Read("scripts/Verify.ps1").Contains("'PluginUiTest.Host.dll'",StringComparison.Ordinal),"Verify.ps1 requires the library in the desktop package");
            var solution = Read("TenshoAssistant.sln");
            Check(!solution.Contains("PluginUiTest",StringComparison.Ordinal) && !solution.Contains("SamplePlugin",StringComparison.Ordinal),"the library is outside the solution (built through the desktop app)");
            var library = string.Join("\n",Directory.EnumerateFiles(Path.GetFullPath(Root+"src"),"*.cs",SearchOption.AllDirectories)
                .Where(file=>!file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")).Select(File.ReadAllText));
            foreach (var request in new[]{"new HttpClient","GetAsync(","SendAsync(","new TcpClient","new Socket(","new WebClient","WebRequest.Create"})
                Check(!library.Contains(request,StringComparison.Ordinal),"the library makes no request: "+request);
        }),
        ("Plugin UI test: tools/README.md lists it, the evidence of X-120 stays",() =>
        {
            Check(Read("tools/README.md").Contains("`PluginUiTest`",StringComparison.Ordinal),"tools/README.md lists it");
            var evidence = Read("docs/evidence/x120-plugin-ui-test.md");
            Check(evidence.Contains("Test tool, not affiliated with the Dalamud team, does not replace Dalamud.",StringComparison.Ordinal) && evidence.Contains("--run-anyway",StringComparison.Ordinal),"the evidence of X-120");
        }),
    ];
}
