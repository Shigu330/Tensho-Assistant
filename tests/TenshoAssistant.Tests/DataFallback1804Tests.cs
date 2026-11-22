using Microsoft.Data.Sqlite;
using TenshoAssistant;

// 0.1804 (X-796 M3, review of 0.1802): TenshoDataFile.JsonFallback, read in Draw by the main window's banner on every page, took the lock
// Database() keeps during the whole first open (journal, tables with busy_timeout of 5 s when another client writes, quick_check of the whole
// file): an open off the frame at the same moment as a frame made the game wait. It is now read without the lock, false until that open is
// over. Behaviour: an open held inside its lock (the log callback it calls there blocked) while the banner's read must answer at once.
internal static class DataFallback1804Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-1804-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    /// <summary>Opens <paramref name="data"/> on a task while <paramref name="inside"/> (set by a callback Database() calls under its lock) holds
    /// it; the banner's read is timed meanwhile. → (the read's answer, its time in ms, the open's result once released).</summary>
    private static (bool During,long Ms,Task<TenshoDataDatabase?> Open) WhileOpening(TenshoDataFile data,ManualResetEventSlim inside,ManualResetEventSlim release)
    {
        var open = Task.Run(data.Database);
        Check(inside.Wait(TimeSpan.FromSeconds(20)),"the open reached its callback");
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var read = Task.Run(() => data.JsonFallback);
        var answered = read.Wait(TimeSpan.FromSeconds(5));
        clock.Stop();
        release.Set();
        Check(answered,"the banner's read waited for the open (the lock of Database())");
        Check(open.Wait(TimeSpan.FromSeconds(20)),"the open finished once released");
        return (read.Result,clock.ElapsedMilliseconds,open);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Data fallback 1804: behaviour: the banner's read never waits for a first open held in its lock; « json » known only once it is over",() => InTemp(root =>
        {
            var notFolder = Path.Combine(root,"a file, not a folder");
            File.WriteAllText(notFolder,"x");
            using var inside = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var data = new TenshoDataFile(Path.Combine(notFolder,TenshoDataDatabase.FileName),(_,_) => { inside.Set(); release.Wait(TimeSpan.FromSeconds(30)); });
            var (during,ms,open) = WhileOpening(data,inside,release);
            Check(!during && ms < 5000,"read during the open: false, at once (" + ms + " ms)");
            Check(open.Result is null && data.JsonFallback && data.Mode == "json","then the « json » mode, the banner shows");
        })),
        ("Data fallback 1804: behaviour: a data file created is never the « json » mode, during its open or after",() => InTemp(root =>
        {
            using var inside = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var data = new TenshoDataFile(Path.Combine(root,TenshoDataDatabase.FileName),(_,_) => { },_ => { inside.Set(); release.Wait(TimeSpan.FromSeconds(30)); });
            var (during,ms,open) = WhileOpening(data,inside,release);
            Check(!during && ms < 5000,"read during the open: false, at once (" + ms + " ms)");
            Check(open.Result is not null && !data.JsonFallback && data.Mode == "sqlite","created: no banner");
        })),
        ("Data fallback 1804: wiring: JsonFallback takes no lock; the outcome written once the first open is over",() =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/TenshoDataDatabase.cs")).ReplaceLineEndings("\n");
            Check(code.Contains("public bool JsonFallback=>Volatile.Read(ref outcome)==Json;",StringComparison.Ordinal),"read without the lock");
            Check(code.Contains("Volatile.Write(ref outcome,database is not null ? 1 : blocked is not null ? 3 : Json);\n            }\n            if(blocked is not null)throw",StringComparison.Ordinal),
                "written at the end of the first open, inside it");
        }),
    ];
}
