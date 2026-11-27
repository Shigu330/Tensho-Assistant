// 0.1913 (X-892 M4, review of 0.1912): a write of the test trace that failed (a full disk, a locked file) left the runner's loop outside the
// try that catches the tests' failures: the raw test thread died of it and the whole process fell without its results. The trace now stops
// writing, says so once on the error output and never throws; a line after the end of the run (a thread left behind) is dropped too.
internal static class TestTrace1913Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // A stream whose writes fail once it is full, as a disk that fills up.
    private sealed class Filling : MemoryStream
    {
        public volatile bool Full;
        public int Refused;
        public override void Write(byte[] buffer, int offset, int count) { if (Full) { Refused++; throw new IOException("There is not enough space on the disk."); } base.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { if (Full) { Refused++; throw new IOException("There is not enough space on the disk."); } base.Write(buffer); }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Test trace 1913: behaviour: a write that fails stops the trace without throwing; the next lines are dropped, the reason kept (X-892 M4)", () =>
        {
            var stream = new Filling();
            var trace = new TestTrace(stream);
            trace.Line("BEGIN", "First");
            Check(trace.Failure is null && stream.Length > 0, "the first line written");
            stream.Full = true;
            Exception? thrown = null;
            var thread = new Thread(() =>
            {
                try { trace.Line("END", "First"); trace.Line("BEGIN", "Second"); }
                catch (Exception error) { thrown = error; }
            });
            thread.Start();
            Check(thread.Join(TimeSpan.FromSeconds(60)), "the thread ended");
            Check(thrown is null, "no exception out of the trace: " + thrown);
            Check(trace.Failure == "IOException" && stream.Refused == 1, $"stopped at the failed write, the next line not tried: {trace.Failure} {stream.Refused}");
            trace.Dispose();
        }),
        ("Test trace 1913: behaviour: a line after the end of the run (a test thread left behind) is dropped without throwing (X-892 M4)", () =>
        {
            var path = Path.Combine(Path.GetTempPath(), "tensho-trace-1913-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                var trace = new TestTrace(path);
                trace.Line("BEGIN", "Left behind");
                trace.Dispose();
                trace.Line("END", "Left behind");
                Check(File.ReadAllText(path) is var text && text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length == 1 && text.Contains(" BEGIN Left behind", StringComparison.Ordinal), "the line before the end only");
            }
            finally { File.Delete(path); }
        }),
    ];
}
