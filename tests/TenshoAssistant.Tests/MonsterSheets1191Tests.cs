using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1191. X-01 C2.2: the hunting log's entries and the English monster names are built on a worker at load, not by the first frame with
// monsters around. X-01 C1.3: the closure of the constructor's outer scope is noted with the other hubs (0.1196: no longer emptied).
internal static class MonsterSheets1191Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Service { public byte[] Heavy = new byte[16]; }

    // What the plugin's constructor looks like: a parameter captured in the outer scope, a local captured in an inner one.
    // The captured objects are made in a frame that has returned: an unoptimised caller would keep its temporaries alive.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Func<int> Inner, WeakReference OuterValue, WeakReference InnerValue) Closures()=>Closures(new Service());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Func<int> Inner, WeakReference OuterValue, WeakReference InnerValue) Closures(Service parameter)
    {
        Func<int> outer = () => parameter.Heavy.Length;
        {
            var local = new Service();
            Func<int> inner = () => local.Heavy.Length + outer();
            return (inner, new WeakReference(parameter), new WeakReference(local));
        }
    }

    // The closures noted as the plugin notes its constructor's at unload; only « keep » holds the inner one afterwards.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Dictionary<string, WeakReference> Parts, WeakReference OuterValue, WeakReference InnerValue, bool Found) NotedClosures(List<object> keep)
    {
        var (inner, outerValue, innerValue) = Closures();
        var closure = inner.Target!;
        var outer = UnloadWatch.OuterClosure(closure);
        var found = outer is not null && outer != closure && UnloadWatch.OuterClosure(outer) is null && UnloadWatch.OuterClosure(null) is null;
        var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
        UnloadWatch.Note(parts, "locals", closure, typeof(MonsterSheets1191Tests).Assembly);
        UnloadWatch.Note(parts, "locals0", outer, typeof(MonsterSheets1191Tests).Assembly);
        keep.Add(inner);
        return (parts, outerValue, innerValue, found);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Monster sheets 1191: wiring: both sheets are lazy and thread-safe, read ahead on a worker at load, a failure said once; the window asks the same value", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).ReplaceLineEndings("\n");
            Check(plugin.Contains("var huntEntries=new Lazy<IReadOnlyList<Hunting.HuntEntry>>(()=>data.GetExcelSheet<Lumina.Excel.Sheets.MonsterNote>()", StringComparison.Ordinal), "hunting log: lazy, built once");
            Check(plugin.Contains("mainWindow.HuntEntries=()=>huntEntries.Value;", StringComparison.Ordinal) && !plugin.Contains("huntEntries??=", StringComparison.Ordinal), "the window reads the same value; no second build");
            Check(plugin.Contains("try{_=huntEntries.Value.Count;_=englishMonsters.Value.Count;}\n                catch(Exception error){log.Warning(error,\"Monster sheets not read ahead", StringComparison.Ordinal), "read ahead on a worker, guarded");
            var ahead = plugin.IndexOf("try{_=huntEntries.Value.Count;", StringComparison.Ordinal);
            Check(plugin.LastIndexOf("_=Task.Run(()=>", ahead, StringComparison.Ordinal) > ahead - 80, "on a worker, not in the constructor's thread");
        }),
        ("Unload 1191: behaviour: the outer closure is found behind an inner one and noted as a hub; let go, the closures and what they captured are collected", () =>
        {
            var keep = new List<object>();
            var (parts, outerValue, innerValue, found) = NotedClosures(keep);
            Check(found, "one scope around, none further");
            Check(parts.ContainsKey("locals0") && parts.Keys.Any(key => key.StartsWith("locals." + UnloadWatch.ClosureField, StringComparison.Ordinal)), "the outer closure is a hub, and the inner closure's field to it is noted: " + string.Join(",", parts.Keys));
            for (var round = 0; round < 3; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            Check(outerValue.IsAlive && innerValue.IsAlive && parts.Values.All(part => part.IsAlive), "held by the closures while a root holds the inner one");
            keep.Clear();
            for (var round = 0; round < 10 && (outerValue.IsAlive || innerValue.IsAlive); round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            Check(!outerValue.IsAlive && !innerValue.IsAlive, "let go, what the closures captured is collected");
            Check(UnloadWatch.Describe("x", parts) == $"x: context unknown, 0 of {parts.Count} parts alive", UnloadWatch.Describe("x", parts));
        }),
        ("Unload 1191: wiring: the outer closure is noted at unload; a line names 24 parts at most", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("Diagnostics.UnloadWatch.Describe(earlier.Key,earlier.Value,24)", StringComparison.Ordinal), "a short line");
            Check(plugin.Contains("Diagnostics.UnloadWatch.Note(parts,\"locals0\",Diagnostics.UnloadWatch.OuterClosure(developerAtUnload.Target),typeof(Plugin).Assembly);", StringComparison.Ordinal), "outer closure noted");
        }),
    ];
}
