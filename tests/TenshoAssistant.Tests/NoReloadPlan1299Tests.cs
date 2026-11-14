// 0.1299 (X-283, review of the 0.1278 block, I4, R28.14): the « player busy » path is proved by a behaviour test, run by check.sh
// (the agent's noreload-test.sh, an agent file): publish.sh's own decision under a made-up PlayState, Verify.ps1 -Plan (the real
// decision: output folder, stamp), a real build beside the game with the game's DLL unchanged, then the next free publish building where
// the game loads. Here, the wiring that test leans on: -Plan stops before anything is built or written, and reads the same lines as the build.
internal static class NoReloadPlan1299Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");
    private static int At(string text, string part) => text.IndexOf(part, StringComparison.Ordinal);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("No reload plan 1299: Verify.ps1 -Plan says the decision from the build's own lines and stops before anything is built or written", () =>
        {
            var verify = Read("scripts/Verify.ps1");
            var plan = At(verify, "if ($Plan) {");
            Check(plan > 0 && verify.Contains("    [switch]$Plan,\n"), "a switch of Verify.ps1");
            Check(At(verify, "if ($taskStamp.NoReload -and -not $NoReload) { $taskSkip = $false }") < plan, "after the stamp's decision");
            Check(At(verify, "$taskOutput = if ($NoReload)") < plan && At(verify, "$taskNoReloadOutput = Join-Path $taskRoot 'artifacts\\noreload\\'") < plan,
                "the output folder decided once, before -Plan, and read by the build");
            var after = verify[plan..];
            Check(At(after, "exit 0") < At(after, "& $DotNet build TenshoAssistant.sln") && At(after, "exit 0") < At(after, "Set-Content -LiteralPath $taskStampFile"),
                "-Plan stops before the build and before the stamp is written");
            Check(verify.Split("$taskOutput = if ($NoReload)").Length == 2 && verify.Split("$taskNoReloadOutput = Join-Path").Length == 2, "one line for each, no copy");
            Check(verify.Contains("if ($Plan -and $env:TENSHO_VERIFY_STAMP) { $taskStampFile = $env:TENSHO_VERIFY_STAMP }")
                && verify.Contains("if ($Plan -and ($Commit -or $Release -or $TestsOnly)) { throw"), "a made-up stamp for -Plan only; never with -Commit or -Release");
        }),
    ];
}
