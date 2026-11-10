using System.Text.RegularExpressions;
using PluginUiTest;
using PluginUiTest.Host;

// X-120: tools/PluginUiTest, a test tool for Dalamud plugin creators (draw every window of a plugin without the game, say what breaks).
// Its pure parts are linked here (command line and refusals, report, Dalamud and game paths); the rest is wiring: the generic code moved
// out of the desktop application (not copied, R11), nothing of Dalamud shipped (R05), no request made by the tool, the banner everywhere,
// the sample plugin and the CI example outside the builds of this repository.
internal static class PluginUiTest1032Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");
    private const string Root = "tools/PluginUiTest/";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Plugin UI test: one DLL named by the user, never a folder nor an installed plugin; options parsed, bad ones refused (exit 2)",() =>
        {
            var installed = DalamudPaths.InstalledPlugins(@"C:\Users\X\AppData\Roaming");
            Check(installed == Path.Combine(@"C:\Users\X\AppData\Roaming","XIVLauncher","installedPlugins"),"installedPlugins under XIVLauncher");
            Check(Options.Refusal(@"D:\dev\bin",false,true,installed)!.Contains("is a folder",StringComparison.Ordinal),"a folder is refused");
            Check(Options.Refusal(Path.Combine(installed,"Other","1.0.0.0","Other.dll"),true,false,installed)!.Contains("never loads installed plugins",StringComparison.Ordinal),"an installed plugin is refused");
            Check(Options.UnderInstalledPlugins(@"E:\Games\XIVLauncher\installedPlugins\Other\1.0\Other.dll",installed),"another XIVLauncher's installedPlugins too");
            Check(Options.Refusal(@"D:\dev\bin\Mine.dll",false,false,installed)!.Contains("no such file",StringComparison.Ordinal),"a missing file is refused");
            Check(Options.Refusal(@"D:\dev\bin\Mine.exe",true,false,installed)!.Contains("not a .dll",StringComparison.Ordinal),"not a DLL is refused");
            Check(Options.Refusal(@"D:\dev\bin\Mine.dll",true,false,installed) is null,"the creator's own build is run");
            var (options,error) = Options.Parse(["Mine.dll","--frames=5","--timeout=9","--run-anyway=Main window, constructor","--no-game-data","--out=r.md","--json=r.json"]);
            Check(error is null && options!.Plugin == "Mine.dll" && options.Frames == 5 && options.Timeout == 9 && options.NoGameData && options.Out == "r.md" && options.Json == "r.json","options read");
            Check(options!.RunAnyway.SequenceEqual(["Main window","constructor"]),"run-anyway names trimmed");
            Check(Options.Parse(["--frames=0","Mine.dll"]).Error is { Length:>0 } && Options.Parse(["Mine.dll","--bogus"]).Error is { Length:>0 } && Options.Parse([]).Error is { Length:>0 }
                  && Options.Parse(["A.dll","B.dll"]).Error is { Length:>0 },"bad options refused");
            Check(Options.Matches(["Main window"],"window:Main window###MainId","Main window###MainId") && Options.Matches(["*"],"x") && !Options.Matches(["Other"],"Main window"),"run-anyway names a window with or without its ImGui id");
            Check(Options.WithoutId("Tracker###Id") == "Tracker" && Options.WithoutId("###Id") == "" && Options.WithoutId("Plain") == "Plain","ImGui id removed");
            Check(Options.ExitDrawn == 0 && Options.ExitFailed == 1 && Options.ExitUsage == 2 && Options.ExitNotCreated == 3,"exit codes 0, 1, 2, 3");
        }),
        ("Plugin UI test: the report carries the banner first, and its exit code says threw, blocked or crashed (1), not created (3)",() =>
        {
            var report = new RunReport{Plugin=@"D:\dev\Mine.dll"};
            report.Put(new Entry{Key="constructor",Kind="constructor",Name="Plugin constructor",Outcome=Outcome.Drawn});
            report.Put(new Entry{Key="window:A",Kind="window",Name="A",Outcome=Outcome.Drawn,Frames=20,Vertices=100});
            report.Put(new Entry{Key="window:B",Kind="window",Name="B",Outcome=Outcome.Skipped,Where="B.Draw → FFXIVClientStructs: Framework.Instance"});
            report.Put(new Entry{Key="window:C",Kind="window",Name="C",Outcome=Outcome.Hidden});
            Check(report.ExitCode == 0 && report.Summary.StartsWith("PASS",StringComparison.Ordinal),"drawn, hidden and skipped: exit 0");
            var markdown = report.Markdown();
            var lines = markdown.Split('\n');
            Check(lines[0].StartsWith("# Plugin UI test report",StringComparison.Ordinal) && lines[2].Contains(Options.Banner,StringComparison.Ordinal),"title then the banner");
            Check(Options.Banner == "Test tool, not affiliated with the Dalamud team, does not replace Dalamud.","the banner's words");
            Check(markdown.Contains("| Plugin constructor | constructor | created |",StringComparison.Ordinal),"a constructor reads created");
            report.Put(new Entry{Key="window:B",Kind="window",Name="B",Outcome=Outcome.Crashed,Detail="access violation"});
            Check(report.ExitCode == 1 && report.Entries.Count == 4,"a part reported again replaces its line; crashed: exit 1");
            Check(report.Json().Contains(Options.Banner,StringComparison.Ordinal) && report.Json().Contains("\"Crashed\"",StringComparison.Ordinal),"the JSON report too");
            report.Fatal = "constructor threw";report.FatalCode = Options.ExitNotCreated;
            Check(report.ExitCode == 3 && report.Summary.StartsWith("NOT CREATED",StringComparison.Ordinal),"not created: exit 3");
        }),
        ("Plugin UI test: the generic code moved out of the desktop app (not copied), referenced back; nothing of Dalamud shipped; no request in the tool",() =>
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
            foreach (var project in new[]{"src/PluginUiTest.Host/PluginUiTest.Host.csproj","src/PluginUiTest/PluginUiTest.csproj"})
            {
                var text = Read(Root+project);
                var references = Regex.Matches(text,@"<Reference Include=""([^""]+)"">(.*?)</Reference>",RegexOptions.Singleline);
                Check(references.Count >= 6 && references.All(r=>r.Groups[2].Value.Contains("<Private>false</Private>",StringComparison.Ordinal)),project+": every Dalamud, Lumina, ImGui, Cecil reference stays where installed (R05)");
                Check(!text.Contains("PackageReference",StringComparison.Ordinal),project+": no package");
            }
            var verify = Read("scripts/Verify.ps1");
            Check(verify.Contains("'PluginUiTest.Host.dll'",StringComparison.Ordinal),"Verify.ps1 requires the library in the desktop package");
            var solution = Read("TenshoAssistant.sln");
            Check(!solution.Contains("PluginUiTest",StringComparison.Ordinal) && !solution.Contains("SamplePlugin",StringComparison.Ordinal),"the tool and its sample are outside the solution");
            var tool = string.Join("\n",Directory.EnumerateFiles(Path.GetFullPath(Root+"src"),"*.cs",SearchOption.AllDirectories)
                .Where(file=>!file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")).Select(File.ReadAllText));
            foreach (var request in new[]{"new HttpClient","GetAsync(","SendAsync(","new TcpClient","new Socket(","new WebClient","WebRequest.Create"})
                Check(!tool.Contains(request,StringComparison.Ordinal),"the tool makes no request: "+request);
            Check(tool.Contains("HttpClient.DefaultProxy",StringComparison.Ordinal) && tool.Contains("HttpHandlerDiagnosticListener",StringComparison.Ordinal),"the child blocks the plugin's requests");
            Check(tool.Contains("SetErrorMode(",StringComparison.Ordinal) && tool.Contains("Kill(entireProcessTree:true)",StringComparison.Ordinal),"child process: no error dialog, stopped on timeout");
        }),
        ("Plugin UI test: README in English then French with the banner, sample plugin with three windows, CI example outside .github, evidence",() =>
        {
            var readme = Read(Root+"README.md");
            var halves = readme.Split("\n---\n");
            Check(halves.Length == 2,"README: English, ---, French");
            Check(halves[0].Contains(Options.Banner,StringComparison.Ordinal) && halves[1].Contains("non affilié à l'équipe de Dalamud",StringComparison.Ordinal),"the banner in both halves");
            int Headings(string half) => Regex.Matches(half,@"^#{1,3} ",RegexOptions.Multiline).Count;
            Check(Headings(halves[0]) == Headings(halves[1]) && Headings(halves[0]) >= 8,"same headings in both halves");
            Check(readme.Contains("GPL",StringComparison.Ordinal),"the licence note");
            var sample = Read(Root+"samples/SamplePlugin/Plugin.cs");
            Check(Regex.Matches(sample,@":Window\(""").Count == 3 && sample.Contains("throw new InvalidOperationException",StringComparison.Ordinal) && sample.Contains("FFXIVClientStructs",StringComparison.Ordinal),"three windows: fine, throws, reads game memory");
            Check(Read(Root+"samples/SamplePlugin/SamplePlugin.csproj").Contains(@"<Project Sdk=""Dalamud.NET.Sdk"">",StringComparison.Ordinal),"the sample is built like a plugin");
            Check(File.Exists(Path.GetFullPath(Root+"docs/github-actions-example.yml")) && !Directory.EnumerateFiles(Path.GetFullPath("."),"*.yml",SearchOption.TopDirectoryOnly).Any(f=>f.Contains("github-actions-example",StringComparison.Ordinal)),"the CI example is documentation");
            Check(!File.Exists(Path.GetFullPath(".github/workflows/github-actions-example.yml")),"not one of this repository's workflows");
            Check(Read("tools/README.md").Contains("`PluginUiTest`",StringComparison.Ordinal),"tools/README.md lists it");
            var evidence = Read("docs/evidence/x120-plugin-ui-test.md");
            Check(evidence.Contains(Options.Banner,StringComparison.Ordinal) && evidence.Contains("--run-anyway",StringComparison.Ordinal),"the evidence of X-120");
        }),
    ];
}
