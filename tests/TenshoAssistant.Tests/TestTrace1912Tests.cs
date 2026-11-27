using System.Text.RegularExpressions;

// 0.1912 (X-889 M4, review of 0.1909): a run under load whose whole process stopped (27/11 on 0.1911, every thread waiting with no processor,
// the runner's own watch included) printed none of its results, so which tests were running stayed unknown. With TENSHO_TEST_TRACE set
// (a sweep under load), each test's start and end are written to that file as they happen; a stopped process leaves the file as it was.
internal static class TestTrace1912Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Test trace 1912: behaviour: each start and end is in the file at once, readable while the run goes on, with its moment and thread", () =>
        {
            var path = Path.Combine(Path.GetTempPath(), "tensho-trace-1912-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                using (var trace = new TestTrace(path))
                {
                    trace.Line("BEGIN", "First: a test: with colons");
                    trace.Line("END", "First: a test: with colons");
                    trace.Line("BEGIN", "Second");
                    string[] lines;
                    using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    Check(lines.Length == 3, "three lines written at once: " + lines.Length);
                    var thread = Environment.CurrentManagedThreadId;
                    Check(Regex.IsMatch(lines[0], $@"^\d+\.\d{{3}} {thread} BEGIN First: a test: with colons$") && Regex.IsMatch(lines[2], $@"^\d+\.\d{{3}} {thread} BEGIN Second$"), lines[0] + " / " + lines[2]);
                }
            }
            finally { File.Delete(path); }
        }),
        ("Test trace 1912: wiring: the runner writes BEGIN before each test and END after it; the run opens the trace only when TENSHO_TEST_TRACE names a file", () =>
        {
            var runner = File.ReadAllText("tests/TenshoAssistant.Tests/TestRunner.cs").ReplaceLineEndings("\n");
            Check(runner.Contains("            Trace?.Line(\"BEGIN\", name);\n            try { run(); }", StringComparison.Ordinal)
                && runner.Contains("            var seconds = Stopwatch.GetElapsedTime(started).TotalSeconds;\n            Trace?.Line(\"END\", name);", StringComparison.Ordinal), "runner");
            var program = File.ReadAllText("tests/TenshoAssistant.Tests/Program.cs").ReplaceLineEndings("\n");
            Check(program.Contains("using var trace = Environment.GetEnvironmentVariable(\"TENSHO_TEST_TRACE\") is { Length: > 0 } tracePath ? new TestTrace(tracePath) : null;\nParallelTests.Trace = trace;\n", StringComparison.Ordinal), "program");
        }),
    ];
}
