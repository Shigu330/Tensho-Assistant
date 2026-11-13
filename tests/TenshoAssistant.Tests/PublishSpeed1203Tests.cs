using System.Diagnostics;
using System.Text;

// 0.1203 (relay 37, publish.sh measured: 3 min 40 s for 0.1202, of which about 60 s ran every managed test a second time): publish.sh
// writes the test count into the new CHANGELOG entry right after Verify's green run; the documents' fingerprint then differed and
// Verify -Commit ran the tests again. Verify now measures the documents with that count put back to its placeholder: equal to the
// stamp, nothing else changed since the green run. Behaviour test (R28): Verify's own function, run by PowerShell on a real git folder.
internal static class PublishSpeed1203Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static string Quote(string text) => "'" + text.Replace("'", "''") + "'";

    private static void Git(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in new[] { "-C", folder }.Concat(arguments)) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new Exception("git could not be started");
        process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Check(process.ExitCode == 0, "git: " + error);
    }

    /// <summary>Verify.ps1's Get-TaskFingerprint (and the count pattern it uses), taken from the script by PowerShell's parser and run on
    /// a folder; one line per state: the plain fingerprint, then the one with the count put back.</summary>
    private static string[] Fingerprints(string root, IReadOnlyList<byte[]> changelogs)
    {
        var list = string.Join(",", changelogs.Select(bytes => Quote(Convert.ToBase64String(bytes))));
        var command = string.Join("\n",
            "$ErrorActionPreference = 'Stop'",
            "$ast = [System.Management.Automation.Language.Parser]::ParseFile(" + Quote(Path.GetFullPath("scripts/Verify.ps1")) + ", [ref]$null, [ref]$null)",
            "$function = $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-TaskFingerprint' }, $true) | Select-Object -First 1",
            "$pattern = $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -eq '$taskCountLine' }, $true) | Select-Object -First 1",
            "if (-not $function -or -not $pattern) { throw 'Get-TaskFingerprint or $taskCountLine not found in Verify.ps1' }",
            ". ([scriptblock]::Create($pattern.Extent.Text + \"`n\" + $function.Extent.Text))",
            "$taskRoot = " + Quote(root),
            "foreach ($state in @(" + list + ")) {",
            "  [IO.File]::WriteAllBytes((Join-Path $taskRoot 'CHANGELOG.md'), [Convert]::FromBase64String($state))",
            "  Write-Output ((Get-TaskFingerprint -Documents) + ' ' + (Get-TaskFingerprint -Documents -CountPlaceholder))",
            "}");
        var start = new ProcessStartInfo("pwsh") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-Command", command }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new Exception("pwsh could not be started");
        var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Check(process.ExitCode == 0, "pwsh: " + error);
        var lines = output.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Check(lines.Length == changelogs.Count, "one line per state: " + output);
        return lines;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Publish speed 1203: behaviour: only the test count written into CHANGELOG since the green run is recognised; any other change is not", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "tensho-1203-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "docs"));
            try
            {
                Git(root, "init", "-q");
                File.WriteAllText(Path.Combine(root, "README.md"), "readme");
                File.WriteAllText(Path.Combine(root, "docs", "a.md"), "a");
                string Entry(string count, string text = "Change.") => "# Journal\n\n## 0.1203.0\n" + text + "\n- Tests : X. Verify.ps1 (agent) : " + count
                    + ", 0 avertissement, 0 erreur ; DLL 0.1203.0.0.\n\n## 0.1202.0\n- Tests : Y. Verify.ps1 (agent) : 2128/2128, 0 avertissement, 0 erreur.\n";
                var utf8 = new UTF8Encoding(false);
                var states = new List<byte[]>
                {
                    utf8.GetBytes(Entry("VERIFYCOUNT")),                         // 0: as Verify measured it before its green run
                    utf8.GetBytes(Entry("2131/2131")),                           // 1: the count written by publish.sh
                    utf8.GetBytes(Entry("2131/2131", "Change, edited.")),        // 2: the count and another change
                    utf8.GetBytes(Entry("VERIFYCOUNT", "Change, edited.")),      // 3: another change only
                    utf8.GetBytes(Entry("2131/2131").Replace("2128/2128", "2129/2129")), // 4: the count and an older entry's count changed
                    utf8.GetBytes(Entry("2131/2131", "Accents é, « guillemets »")), // 5: written over an entry with accents
                    utf8.GetBytes(Entry("VERIFYCOUNT", "Accents é, « guillemets »")),
                };
                var prints = Fingerprints(root, states).Select(line => line.Split(' ')).ToArray();
                var green = prints[0][0];
                Check(prints[0][1] != green, "the placeholder itself is not touched, so the green state measured that way differs (never mistaken for a count)");
                Check(prints[1][0] != green && prints[1][1] == green, "the count alone: recognised, nothing to run again");
                Check(prints[2][1] != green, "the count and another change: the tests run again");
                Check(prints[3][0] != green && prints[3][1] != green, "another change alone: the tests run again");
                Check(prints[4][1] != green, "only the first count is put back: an older entry changed is still seen");
                Check(prints[5][1] == prints[6][0], "byte for byte with accents (UTF-8 kept as it is)");
            }
            finally
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(root, true);
            }
        }),
        ("Publish speed 1203: wiring: Verify checks the count alone before running the tests again; publish.sh's placeholder is the one put back", () =>
        {
            var verify = Read("scripts/Verify.ps1");
            var branch = verify.IndexOf("if ($taskSkip -and $taskStamp.Documents -ne $taskDocuments) {", StringComparison.Ordinal);
            var countOnly = verify.IndexOf("if ((Get-TaskFingerprint -Documents -CountPlaceholder) -eq $taskStamp.Documents) {", StringComparison.Ordinal);
            var tests = verify.IndexOf("& $DotNet run --project tests/TenshoAssistant.Tests -c Release --no-build", branch, StringComparison.Ordinal);
            Check(branch > 0 && countOnly > branch && countOnly < tests, "checked first, in the documents branch, before the tests");
            Check(verify.Contains("'Verify.ps1 (agent) : VERIFYCOUNT, 0 avertissement', 1)", StringComparison.Ordinal), "the first count only, put back to the placeholder");
            var stamp = verify.IndexOf("$taskStamp | ConvertTo-Json | Set-Content -LiteralPath $taskStampFile", branch, StringComparison.Ordinal);
            Check(stamp > tests && verify.IndexOf("throw 'Managed tests failed (tracked documents changed", branch, StringComparison.Ordinal) < stamp, "the stamp takes the documents after either path, after the tests when they run");
        }),
    ];
}
