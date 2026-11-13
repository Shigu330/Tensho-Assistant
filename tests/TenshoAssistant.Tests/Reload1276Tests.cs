// X-268 (0.1276, R19.3): while the player is in a duty, a version is still published, built beside the folder the game loads
// (Verify.ps1 -NoReload); the next free publish loads it, and a release is always built where the game loads it.
internal static class Reload1276Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");
    private static int At(string text, string part) => text.IndexOf(part, StringComparison.Ordinal);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Reload 1276: -NoReload builds the plugin beside the game's folder, runs every check on that build, and is refused with -Release", () =>
        {
            var verify = Read("scripts/Verify.ps1");
            Check(verify.Contains("    [switch]$NoReload\n)"), "a switch of Verify.ps1");
            Check(verify.Contains("if ($NoReload -and $Release) { throw '-NoReload builds beside the game: a release is always built where the game loads it (R19.3)"), "never a release");
            Check(verify.Contains("if ($NoReload) { & $DotNet build TenshoAssistant.sln -c Release --nologo \"-p:TenshoOutputPath=$taskNoReloadOutput\" }")
                && verify.Contains("$taskNoReloadOutput = Join-Path $taskRoot 'artifacts\\noreload\\'"), "built into artifacts\\noreload\\ (ignored by git)");
            Check(verify.Contains("$taskOutput = if ($NoReload) { $taskNoReloadOutput.TrimEnd('\\') } else { Join-Path $taskRoot 'src\\TenshoAssistant\\bin\\x64\\Release' }"),
                "the package, the desktop application and the stamp read that very build");
            Check(At(verify, "$taskOutput = if ($NoReload)") < At(verify, "$taskManifest = Get-Content -Raw -LiteralPath (Join-Path $taskOutput 'TenshoAssistant.json')"), "before the package checks");
            Check(verify.Contains("NoReload = [bool]$NoReload; VerifiedAt") && verify.Contains("if ($taskStamp.NoReload -and -not $NoReload) { $taskSkip = $false }"),
                "a build made beside the game is never taken for the one the game loads: the next run without the switch builds again");
            var project = Read("src/TenshoAssistant/TenshoAssistant.csproj");
            var usual = At(project, "<OutputPath>$(MSBuildProjectDirectory)\\bin\\x64\\$(Configuration)\\</OutputPath>");
            var other = At(project, "<OutputPath Condition=\"'$(TenshoOutputPath)' != ''\">$(TenshoOutputPath)</OutputPath>");
            Check(usual > 0 && other > usual, "the usual folder unless TenshoOutputPath is given (the later one wins)");
            Check(Read(".gitignore").Contains("artifacts/"), "the build beside is never tracked");
        }),
    ];
}
