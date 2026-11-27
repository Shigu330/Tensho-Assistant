// 0.1923 (X-900): « TRACE stopped: IOException (There is not enough space on the disk.) » in the error output of every full run
// (artifacts/tests-errors.txt of Verify, the 0.1919 release) while drive C had more than 770 GB free. No trace was written at all:
// Verify does not set TENSHO_TEST_TRACE; the line was TestTrace1913Tests' own behaviour case, whose stream simulates a full disk, said
// on the runner's real error output. The trace now says its stop to a writer it is given (the error output by default), and a test that
// makes it fail gives its own.
internal static class TestTrace1923Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Refusing : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("There is not enough space on the disk.");
        public override void Write(ReadOnlySpan<byte> buffer) => throw new IOException("There is not enough space on the disk.");
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Test trace 1923: behaviour: a trace given a writer says its stop there, once, with the reason (X-900)", () =>
        {
            var said = new StringWriter();
            var trace = new TestTrace(new Refusing(), said);
            trace.Line("BEGIN", "A");
            trace.Line("END", "A");
            Check(trace.Failure == "IOException", "stopped: " + trace.Failure);
            Check(said.ToString() == "TRACE stopped: IOException (There is not enough space on the disk.); the tests go on without it." + Environment.NewLine, "said once: " + said);
            trace.Dispose();
        }),
        ("Test trace 1923: ratchet: every test that builds a trace on its own stream gives it a writer, so nothing it simulates reaches the runner's error output (X-900)", () =>
        {
            var folder = Path.GetFullPath("tests/TenshoAssistant.Tests");
            var bare = Directory.EnumerateFiles(folder, "*Tests.cs")
                .SelectMany(file => File.ReadLines(file).Select(line => (File: Path.GetFileName(file), Line: line.Trim())))
                // A trace built on one argument that is not a file path (path, tracePath: a real file, its stop said to the error output).
                .Where(item => System.Text.RegularExpressions.Regex.IsMatch(item.Line, @"new TestTrace\((?!path\)|tracePath\))[^,()]*\)"))
                .ToList();
            Check(Directory.EnumerateFiles(folder, "TestTrace19*Tests.cs").Count() >= 3, "the test folder found: " + Path.GetFullPath(folder));
            Check(bare.Count == 0, "a trace on a stream of a test without its own writer: " + string.Join(" | ", bare.Select(item => item.File + ": " + item.Line)));
        }),
    ];
}
