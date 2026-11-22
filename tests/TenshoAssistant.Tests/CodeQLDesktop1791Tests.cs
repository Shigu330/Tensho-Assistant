// X-148 (0.1791, the end of the GPT audit's A16 of 11/11): CodeQL also analyses the desktop application (net10.0-windows) and the pinned copy
// of PluginUiTest.Host it builds, in a Windows job of its own (the Linux job cannot build them); its actions pinned as the others
// (PinnedActions1215Tests), Dalamud's reference assemblies checked as everywhere (DalamudCheck1336Tests).
internal static class CodeQLDesktop1791Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("CodeQL desktop 1791: a Windows job analyses the desktop application and PluginUiTest.Host, in its own category", () =>
        {
            var codeql = Read(".github/workflows/CodeQL.yml");
            var start = codeql.IndexOf("\n  analyze-desktop:\n", StringComparison.Ordinal);
            Check(start > 0, "the job analyze-desktop");
            var job = codeql[start..];
            Check(job.Contains("    runs-on: windows-latest\n", StringComparison.Ordinal), "on Windows (net10.0-windows)");
            Check(job.Contains("run: dotnet build src/TenshoAssistant.Desktop/TenshoAssistant.Desktop.csproj -c Release --no-restore", StringComparison.Ordinal),
                "builds the desktop application (its project reference builds PluginUiTest.Host)");
            Check(Read("src/TenshoAssistant.Desktop/TenshoAssistant.Desktop.csproj").Contains("<ProjectReference Include=\"..\\..\\tools\\PluginUiTest\\src\\PluginUiTest.Host\\PluginUiTest.Host.csproj\" />", StringComparison.Ordinal),
                "PluginUiTest.Host built with it");
            Check(job.IndexOf("github/codeql-action/init@", StringComparison.Ordinal) is var init and > 0 && job.IndexOf("dotnet build", StringComparison.Ordinal) > init
                  && job.IndexOf("github/codeql-action/analyze@", StringComparison.Ordinal) > job.IndexOf("dotnet build", StringComparison.Ordinal), "init, the build traced, then the analysis");
            Check(job.Contains("build-mode: manual", StringComparison.Ordinal) && job.Contains("languages: csharp", StringComparison.Ordinal), "C#, built by the job");
            Check(job.Contains("category: \"/language:csharp/desktop\"", StringComparison.Ordinal) && codeql[..start].Contains("category: \"/language:${{ matrix.language }}\"", StringComparison.Ordinal),
                "its own category, beside the plugin's");
            Check(job.Contains("./scripts/Get-Dalamud.ps1 -Destination $hooks", StringComparison.Ordinal) && job.Contains("\"DALAMUD_HOME=$hooks\" | Out-File -FilePath $env:GITHUB_ENV", StringComparison.Ordinal),
                "Dalamud checked, given to the projects (DALAMUD_HOME)");
        }),
    ];
}
