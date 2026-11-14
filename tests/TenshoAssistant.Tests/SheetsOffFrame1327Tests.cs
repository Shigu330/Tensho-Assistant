using TenshoAssistant.Caching;

// 0.1327 (X-332, review of 0.1318 I3 and I4, R07): two sheet reads made on the frame. The ventures' names (RetainerTask, RetainerTaskRandom and
// the whole ClassJob sheet) were read again at each frame for each venture row of the discoveries' table; the allied societies' catalogue (with
// a walk of the whole Quest sheet) was read in Draw at the page's first opening. Now: a table built once off the frame (BackgroundTable), and
// the catalogue read in a task, like the FATEs' and the lexicon's.
internal static class SheetsOffFrame1327Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Wait(Task? task) => Check(task is not null && task.Wait(TimeSpan.FromSeconds(10)), "the build ends");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Sheets off frame 1327: the table is built once, off the calling thread; « not yet » until then, then a lookup", () =>
        {
            var caller = Environment.CurrentManagedThreadId;
            var builtOn = -1;
            using var go = new ManualResetEventSlim(false);
            var table = new BackgroundTable<uint, string>(() => { builtOn = Environment.CurrentManagedThreadId; go.Wait(TimeSpan.FromSeconds(10)); return new Dictionary<uint, string> { [5] = "five" }; });
            Check(!table.TryGet(5, out var early) && early is null && !table.Ready, "not yet while building");
            Check(table.Get(5) is null, "still not yet, without a second build");
            go.Set();
            Wait(table.Building);
            Check(builtOn != caller, "built off the calling thread");
            Check(table.Get(5) == "five" && !table.TryGet(6, out _), "a lookup once built; an absent key answers false");
            Parallel.For(0, 2000, n => table.Get((uint)(n % 7)));
            Check(table.Builds == 1, "built once: " + table.Builds);
        }),
        ("Sheets off frame 1327: many first lookups at once start one build", () =>
        {
            var count = 0;
            var table = new BackgroundTable<int, int>(() => { Interlocked.Increment(ref count); Thread.Sleep(50); return new Dictionary<int, int> { [1] = 1 }; });
            Parallel.For(0, 64, _ => table.Get(1));
            Wait(table.Building);
            Check(count == 1 && table.Builds == 1, "one build: " + count);
        }),
        ("Sheets off frame 1327: a build that throws leaves an empty table, said once, never built again", () =>
        {
            var said = 0;
            var table = new BackgroundTable<int, string>(() => throw new InvalidOperationException("sheet changed"), _ => Interlocked.Increment(ref said));
            Check(table.Get(1) is null, "not yet");
            Wait(table.Building);
            Check(table.Ready && table.Get(1) is null && table.Get(2) is null, "empty: every key falls back");
            Check(said == 1 && table.Builds == 1, "said once, built once");
        }),
        ("Sheets off frame 1327: the ventures and the allied societies' catalogue are no longer read in Draw", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var start = plugin.IndexOf("var ventures=new Caching.BackgroundTable<uint,Discoveries.VentureRow>(", StringComparison.Ordinal);
            Check(start > 0 && plugin.Contains("mainWindow.VentureRowOf=id=>ventures.TryGet(id,out var row) ? row:null;", StringComparison.Ordinal), "ventures from the table");
            Check(plugin.IndexOf("GetExcelSheet<Lumina.Excel.Sheets.ClassJob>()", StringComparison.Ordinal) > start, "ClassJob read in the build only");
            var allied = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.AlliedSocieties.cs"));
            Check(allied.Contains("alliedLoad=Task.Run(load);", StringComparison.Ordinal) && !allied.Contains("alliedCatalog=AlliedCatalogLoad?.Invoke();", StringComparison.Ordinal), "the catalogue read in a task");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.SheetLoaders.cs")).Contains("allied=null;alliedLoad=null;", StringComparison.Ordinal), "forgotten with the page");
        }),
    ];
}
