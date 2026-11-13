using System.Globalization;

// 0.1201 (measure 7 of the acceleration plan): where a full run of Verify.ps1 spends its time. Measured on 13 November: build 8.9 s,
// tests 104.4 s, languages 1.2 s, package 0.1 s, desktop publish (into the emptied folder) 3.5 s, desktop pages 26.3 s, desktop zip
// 3.2 s, desktop update 1.5 s; whole run 149.1 s. The publish of the application is not what costs (its folder stays emptied at each
// run: no stale file, ever); the tests are. Verify says its steps' times in one line, and the test run says its slowest tests.
internal static class SlowestTests
{
    /// <summary>The slowest tests in one line: « SLOWEST 12 of 2122 tests take 61.3 s of 104.2 s: 9.8 s name | … » (names cut, invariant numbers).</summary>
    public static string Line(IReadOnlyList<(double Seconds, string Name)> times, double whole, int count = 12, int nameLength = 70)
    {
        var slowest = times.OrderByDescending(time => time.Seconds).ThenBy(time => time.Name, StringComparer.Ordinal).Take(count).ToList();
        string Number(double seconds) => seconds.ToString("0.0", CultureInfo.InvariantCulture);
        var names = slowest.Select(time => Number(time.Seconds) + " s " + (time.Name.Length > nameLength ? time.Name[..nameLength] + "…" : time.Name).Replace(" | ", " / "));
        return $"SLOWEST {slowest.Count} of {times.Count} tests take {Number(slowest.Sum(time => time.Seconds))} s of {Number(whole)} s: {string.Join(" | ", names)}";
    }
}

internal static class VerifyTimes1201Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Verify times 1201: the slowest tests are said in one line: slowest first, names cut, numbers the same in every language", () =>
        {
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                var line = SlowestTests.Line([(0.25, "quick"), (9.84, "slow one"), (1.5, "a | b"), (9.84, "another slow one, with a name long enough to be cut")], 30.04, 3, 20);
                Check(line == "SLOWEST 3 of 4 tests take 21.2 s of 30.0 s: 9.8 s another slow one, wi… | 9.8 s slow one | 1.5 s a / b", line);
                Check(SlowestTests.Line([], 0) == "SLOWEST 0 of 0 tests take 0.0 s of 0.0 s: ", "no test: an empty list, never an error");
            }
            finally { CultureInfo.CurrentCulture = culture; }
        }),
        ("Verify times 1201: wiring: Verify.ps1 says the time of each step in one line before the version; the test run says its slowest tests before the count", () =>
        {
            var verify = File.ReadAllText(Path.GetFullPath("scripts/Verify.ps1")).ReplaceLineEndings("\n");
            // X-267 (0.1275): the tests run alongside the steps after the build; their step is the wait at the end (and « tests alongside »).
            var steps = new[] { "'build'", "'languages'", "'package'", "'desktop publish'", "'desktop pages'", "'desktop zip'", "'desktop update'", "'tests'" };
            var last = -1;
            foreach (var step in steps)
            {
                var at = verify.IndexOf("Add-TaskTime " + step, StringComparison.Ordinal);
                Check(at > last, "a step is timed, in the order of the run: " + step);
                last = at;
            }
            var times = verify.IndexOf("Write-Output (\"TIMES \" + ($taskTimes -join ', ')", StringComparison.Ordinal);
            Check(times > last && times < verify.IndexOf("Write-Output \"VERSION $taskProjectVersion", StringComparison.Ordinal), "one line, after the last step, before the version");
            Check(verify.Contains("ToString('0.0', [Globalization.CultureInfo]::InvariantCulture)", StringComparison.Ordinal), "numbers the same in every language");
            // The measure decided it: the application's folder is still emptied before each publish (about 3 s; no stale file in a package).
            Check(verify.Contains("if (Test-Path -LiteralPath $taskDesktopPackage) { Remove-Item -LiteralPath $taskDesktopPackage -Recurse -Force }\n    & $DotNet publish src/TenshoAssistant.Desktop", StringComparison.Ordinal), "emptied, then published");
            var program = File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/Program.cs")).ReplaceLineEndings("\n");
            // X-267 (0.1275): the count comes from the parallel run (TestRunner.cs); same order, the count still last.
            var slowest = program.IndexOf("Console.WriteLine(SlowestTests.Line(run.Times, run.Whole));", StringComparison.Ordinal);
            Check(slowest > 0 && slowest < program.IndexOf("Console.WriteLine($\"{run.Passed}/{run.Total} passed\");", StringComparison.Ordinal), "before the count, which stays the last line");
        }),
    ];
}
