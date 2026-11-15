// X-425 M3 (0.1406, review 0.1404): the layouts of the bells and chests (1 662 files; 0.1405 measured 14.6 ms for the longest one on the
// game's thread) are read on a worker, as the festival layouts are; the framework steps wait for it and only take its rows.
internal static class PlacedObjectsWorker1406Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");
    private static string Between(string text, string from, string to)
    {
        var start = text.IndexOf(from, StringComparison.Ordinal);
        var end = start < 0 ? -1 : text.IndexOf(to, start, StringComparison.Ordinal);
        if (start < 0 || end < 0) throw new Exception("not found: " + from);
        return text[start..end];
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Placed objects worker 1406: the layouts are read on a worker, with the loader's cancellation", () =>
        {
            var loader = Read("Maps/MapLocalIndexLoader.Dalamud.cs");
            var copy = Between(loader, "private IEnumerable<int> CopyPlacedObjects(int count)", "private PlacedRead ReadPlacedObjects(");
            Check(copy.Contains("var reading=placedRead=Task.Run(()=>ReadPlacedObjects(token),token);", StringComparison.Ordinal), "started on a worker");
            Check(!copy.Contains("Layout(", StringComparison.Ordinal) && !copy.Contains("GetExcelSheet", StringComparison.Ordinal), "no layout nor sheet read on the framework's steps");
            Check(copy.Contains("rows.AddRange(placed.Rows);", StringComparison.Ordinal), "the framework takes the rows");
        }),
        ("Placed objects worker 1406: the worker never touches the loader's list and stops with the plugin", () =>
        {
            var loader = Read("Maps/MapLocalIndexLoader.Dalamud.cs");
            var worker = Between(loader, "private PlacedRead ReadPlacedObjects(CancellationToken token)", "private Lumina.Data.Files.LgbFile? Layout(string path)");
            Check(!worker.Contains("rows.", StringComparison.Ordinal) && worker.Contains("found.Add(", StringComparison.Ordinal), "its own list");
            Check(!worker.Contains("yield", StringComparison.Ordinal), "not a framework iterator");
            Check(worker.Contains("token.ThrowIfCancellationRequested();", StringComparison.Ordinal), "cancelled when the loader is disposed");
            Check(loader.Contains("disposed=true;cancellation.Cancel();", StringComparison.Ordinal), "the loader's dispose cancels it");
        }),
        ("Placed objects worker 1406: the framework steps nothing while the worker reads, without spinning its 2 ms", () =>
        {
            var loader = Read("Maps/MapLocalIndexLoader.Dalamud.cs");
            Check(loader.Contains("if(placedRead is { IsCompleted:false })return;", StringComparison.Ordinal), "a frame that finds it reading returns at once");
            Check(loader.Contains("if(placedRead is { IsCompleted:false })break;\n                    if(!copy.MoveNext()){done=true;break;}", StringComparison.Ordinal),
                "the batch stops as soon as the worker is started");
            Check(Read("Maps/MapLocalIndex.cs").Contains("private const int CopyRules=6;", StringComparison.Ordinal), "rebuilt once, so the log line measures the worker");
        }),
    ];
}
