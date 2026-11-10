using System.Text.Json;
using System.Text.RegularExpressions;
using PluginUiTest.Host;
using TenshoAssistant.Desktop;

// X-119 (plan 4, step 1, R33): the desktop application src/TenshoAssistant.Desktop (was tools/DesktopProbe, X-118). Its pure parts are
// linked here: the page policy (every page of the plugin classified, never forgotten), the start-up version check against the installed
// plugin, where it finds what it does not ship (R05). The wiring: one version source, nothing of Dalamud or the game in the package,
// Verify.ps1 builds, draws every page headless and packages it, Release.ps1 publishes it next to latest.zip.
internal static class DesktopApp1031Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file));

    /// <summary>The plugin's pages, from the enum Page of MainWindow.cs (the same names the app reads by reflection).</summary>
    private static string[] PluginPages()
    {
        var match = Regex.Match(Read("src/TenshoAssistant/MainWindow.cs"),@"private enum Page \{([^}]*)\}");
        Check(match.Success,"enum Page found in MainWindow.cs");
        return match.Groups[1].Value.Split(',').Select(name=>name.Trim()).Where(name=>name.Length>0).ToArray();
    }

    private static Dictionary<string,string> Section(string language,string section)
    {
        using var json = JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json"));
        return json.RootElement.GetProperty(section).EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.GetString() ?? "");
    }

    private static string Placeholders(string text) => string.Join(",",Regex.Matches(text,@"\{\d+\}").Select(m=>m.Value).Order());

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop pages: every page of the plugin is classified once (31 as is, 17 need the link, 3 in game); an unknown page is named",() =>
        {
            var pages = PluginPages();
            Check(pages.Length == 51,"51 pages in the plugin: "+pages.Length);
            Check(DesktopPages.Unclassified(pages).Count == 0,"unclassified: "+string.Join(", ",DesktopPages.Unclassified(pages)));
            var all = DesktopPages.AsIs.Concat(DesktopPages.NeedsLink).Concat(DesktopPages.InGame).ToArray();
            Check(all.Length == all.Distinct().Count(),"a page in one list only");
            Check(all.Order().SequenceEqual(pages.Order()),"no list names a page that does not exist");
            Check(DesktopPages.AsIs.Count == 31 && DesktopPages.NeedsLink.Count == 17 && DesktopPages.InGame.Count == 3,"31 / 17 / 3 as measured in X-118");
            Check(DesktopPages.InGame.SequenceEqual(["Maps","CraftLive","Fashion"]),"the user's three in-game pages");
            Check(DesktopPages.Kind("NewPage") is null && DesktopPages.Unclassified(["Home","NewPage"]).SequenceEqual(["NewPage"]),"a new page is unclassified");
            var message = DesktopPages.UnclassifiedMessage(["NewPage"]);
            Check(message.Contains("NewPage",StringComparison.Ordinal) && message.Contains("DesktopPages.cs",StringComparison.Ordinal),"the message names the page and the file");
            Check(DesktopPages.Shown("Maps") == "Home" && DesktopPages.Shown("Fashion") == "Home" && DesktopPages.Shown("Weather") == "Weather","an in-game page shows Home");
            Check(DesktopPages.LiveKey("Weather") == "Desktop.LiveWeather" && DesktopPages.LiveKey("Lexicon") is null && DesktopPages.LiveKey("Maps") is null,"live parts named for NeedsLink only");
            var headless = Read("src/TenshoAssistant.Desktop/Headless.cs");
            Check(headless.Contains("DesktopPages.Unclassified(host.Pages)",StringComparison.Ordinal) && headless.Contains("return 1;",StringComparison.Ordinal),"the headless run fails on an unclassified page");
        }),
        ("Desktop texts: every Desktop key the app uses is in English and French with the same placeholders, one live text per NeedsLink page",() =>
        {
            var english = Section("en","Desktop");
            var french = Section("fr","Desktop");
            var code = string.Join("\n",Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant.Desktop"),"*.cs").Select(File.ReadAllText));
            var used = Regex.Matches(code,@"""Desktop\.([A-Za-z]+)""(?!\s*\+)").Select(m=>m.Groups[1].Value).Distinct()
                .Concat(DesktopPages.NeedsLink.Select(page=>"Live"+page)).Distinct().ToArray();
            Check(used.Length >= 20,"keys found: "+used.Length);
            foreach (var key in used)
            {
                Check(english.TryGetValue(key,out var en) && en.Length > 0,$"en.json Desktop.{key}");
                Check(french.TryGetValue(key,out var fr) && fr.Length > 0,$"fr.json Desktop.{key}");
                Check(Placeholders(en!) == Placeholders(fr!),$"Desktop.{key}: same placeholders");
            }
            Check(english.Keys.Order().SequenceEqual(french.Keys.Order()),"same keys in both languages");
            Check(english["NotLinked"].Contains("Game not linked",StringComparison.Ordinal) && english["NotLinked"].Contains("actions are off",StringComparison.Ordinal),"the status bar's sentence");
            Check(Regex.IsMatch(Read("src/TenshoAssistant/Help/Faq.cs"),@"new\(""Desktop""\)"),"the FAQ says where the app is (R32)");
        }),
        ("Desktop version check: installed plugin and developer locations read, same / different / not found told clearly",() =>
        {
            var launcher = @"C:\Users\X\AppData\Roaming\XIVLauncher";
            var installed = Path.Combine(launcher,"installedPlugins","TenshoAssistant");
            const string config = """{"$type":"x","DevPluginLoadLocations":{"$type":"y","$values":[{"Path":"D:\\dev\\bin\\TenshoAssistant.dll","IsEnabled":true},{"Path":"D:\\off","IsEnabled":false},{"Path":"D:\\folder"}]}}""";
            var candidates = DesktopVersionCheck.Candidates(launcher,folder=>folder == installed ? [Path.Combine(installed,"0.1029.0.0"),Path.Combine(installed,"0.1031.0.0")] : [],config);
            Check(candidates.Count == 4,"two installed, two enabled developer locations: "+string.Join(" | ",candidates));
            Check(candidates[0].Contains("0.1031.0.0",StringComparison.Ordinal) && candidates[2] == @"D:\dev\bin\TenshoAssistant.dll" && candidates[3] == Path.Combine(@"D:\folder","TenshoAssistant.dll"),"highest installed first, then the developer DLL and folder");
            Check(DesktopVersionCheck.DevLocations("not json").Count == 0 && DesktopVersionCheck.DevLocations(null).Count == 0 && DesktopVersionCheck.DevLocations("""{"DevPluginLoadLocations":[{"Path":"E:\\p"}]}""").Single() == @"E:\p","bad or plain configurations");
            var app = new Version(0,1031,0);
            var same = DesktopVersionCheck.Run(app,candidates,path=>!path.Contains("0.1029",StringComparison.Ordinal),path=>path.Contains("0.1031",StringComparison.Ordinal) ? new Version(0,1031,0,0) : new Version(0,1030,0,0));
            Check(same.State == DesktopVersionState.Same && same.InstalledPath!.Contains("0.1031",StringComparison.Ordinal),"same version (major.minor.build)");
            var different = DesktopVersionCheck.Compare(app,[("a",new Version(0,1030,0,0)),("b",new Version(0,1029,0,0))]);
            Check(different.State == DesktopVersionState.Different && different.Installed == new Version(0,1030,0,0) && different.InstalledPath == "a","different: the highest found");
            var english = DesktopVersionCheck.English(different);
            Check(english.Contains("0.1030.0",StringComparison.Ordinal) && english.Contains("0.1031.0",StringComparison.Ordinal) && english.Contains("same release",StringComparison.Ordinal),"both numbers and the same release said");
            var none = DesktopVersionCheck.Run(app,candidates,_=>false,_=>null);
            Check(none.State == DesktopVersionState.NotFound && DesktopVersionCheck.English(none).Contains("no installed",StringComparison.Ordinal),"not found");
            Check(DesktopVersionCheck.Text(different).Key == "Desktop.VersionDifferent" && DesktopVersionCheck.Text(different).Args.Length == 2 && DesktopVersionCheck.Text(none).Key == "Desktop.VersionNotFound","language keys");
            Check(DesktopVersionCheck.Three(new Version(0,1031)) == "0.1031.0","a version without build reads .0");
        }),
        ("Desktop paths: Dalamud and the game read where installed (DALAMUD_HOME, XIVLauncher, launcher GamePath, Steam), languages follow the settings",() =>
        {
            // X-120: the generic paths moved to the plugin UI test tool's library (DalamudPaths), used by the app.
            Check(DalamudPaths.Hooks(null,@"C:\A") == Path.Combine(@"C:\A","XIVLauncher","addon","Hooks","dev") && DalamudPaths.Hooks(@"D:\dalamud",@"C:\A") == @"D:\dalamud","DALAMUD_HOME first, as the plugin build");
            var sqpack = DalamudPaths.SqpackCandidates(@"E:\given","""{"GamePath":"F:\\FFXIV"}""");
            Check(sqpack.SequenceEqual([@"E:\given",Path.Combine(@"F:\FFXIV","game","sqpack"),DalamudPaths.SteamSqpack]),"--sqpack, launcher GamePath, Steam: "+string.Join(" | ",sqpack));
            Check(DalamudPaths.SqpackCandidates(null,"{broken").SequenceEqual([DalamudPaths.SteamSqpack]),"a broken launcher file leaves Steam's folder");
            Check(DesktopPaths.TextLanguage("FR ","en") == "fr" && DesktopPaths.TextLanguage(null,"de") == "de","saved language first, then Windows");
            Check(DesktopPaths.GameLanguage("fr") == "French" && DesktopPaths.GameLanguage("ja") == "Japanese" && DesktopPaths.GameLanguage("es") == "English","game data language");
            Check(DesktopPaths.PluginConfig(@"C:\A").EndsWith(Path.Combine("pluginConfigs","TenshoAssistant.json"),StringComparison.Ordinal),"the plugin's saved settings");
        }),
        ("Desktop wiring: one version source, nothing of Dalamud shipped, Verify builds, draws headless and packages it, the release carries it",() =>
        {
            var project = Read("src/TenshoAssistant.Desktop/TenshoAssistant.Desktop.csproj");
            Check(!Regex.IsMatch(project,@"<Version>\s*\d") && project.Contains("TenshoAssistant\\TenshoAssistant.csproj",StringComparison.Ordinal),"the app's version is read from the plugin's csproj, never written twice");
            var references = Regex.Matches(project,@"<Reference Include=""([^""]+)"">(.*?)</Reference>",RegexOptions.Singleline);
            Check(references.Count >= 7 && references.All(r=>r.Groups[2].Value.Contains("<Private>false</Private>",StringComparison.Ordinal)),"every Dalamud, Lumina, ImGui, Cecil, Newtonsoft reference stays where installed (R05)");
            Check(!project.Contains("PackageReference",StringComparison.Ordinal),"no package");
            Check(!Directory.Exists(Path.GetFullPath("tools/DesktopProbe")) || !Directory.EnumerateFiles(Path.GetFullPath("tools/DesktopProbe"),"*.cs").Any(),"the probe became the app (git mv), not a copy");
            var verify = Read("scripts/Verify.ps1");
            foreach (var part in new[]{"src/TenshoAssistant.Desktop","--headless","TenshoAssistant.Desktop.zip","SKIP desktop","plugin/TenshoAssistant.dll",
                         "-r win-x64 --self-contained","(Join-Path $taskDesktopPackage 'TenshoAssistant.Desktop.exe') --headless","Mono\\.Cecil|Newtonsoft"})
                Check(verify.Contains(part,StringComparison.Ordinal),"Verify.ps1: "+part);
            Check(Regex.IsMatch(verify,@"if \(\$Release\)[^\n]*throw[^\n]*SKIP",RegexOptions.IgnoreCase) || verify.Contains("-Release refuses",StringComparison.Ordinal),"-Release refuses a skipped headless run");
            var release = Read("scripts/Release.ps1");
            Check(Regex.Matches(release,@"\$desktopZip").Count >= 4 && release.Contains("Assert-DesktopZip",StringComparison.Ordinal),"the GitHub release carries the app next to latest.zip, checked first (normal and resumed)");
            var program = Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.Contains("FreeConsole()",StringComparison.Ordinal) && program.Contains("internal const int Skip=4;",StringComparison.Ordinal),"double-click frees the console; skip code for Verify");
            // X-120: the rewrite itself moved to tools/PluginUiTest (generic); the app's use of it stays marked temporary, its copy outside the repository.
            Check(program.Contains("TEMPORARY (X-119 step 1)",StringComparison.Ordinal) && program.Contains("LOCALAPPDATA",StringComparison.Ordinal) && program.Contains("Rewrite.Plugin(",StringComparison.Ordinal),"the IconFont rewrite is temporary and outside the repository");
        }),
    ];
}
