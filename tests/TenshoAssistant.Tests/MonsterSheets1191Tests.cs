using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1191. X-01 C2.2: the hunting log's entries and the English monster names are built on a worker at load, not by the first frame with
// monsters around. X-01 C1.3: the closure of the constructor's outer scope is noted and emptied with the other hubs, and an emptied
// instance's line names every part still alive.
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
        ("Unload 1191: behaviour: the outer closure is found behind an inner one; emptied with the hubs, what it captured is collected", () =>
        {
            var (inner, outerValue, innerValue) = Closures();
            var closure = inner.Target!;
            var outer = UnloadWatch.OuterClosure(closure);
            Check(outer is not null && outer != closure && UnloadWatch.OuterClosure(outer) is null && UnloadWatch.OuterClosure(null) is null, "one scope around, none further");
            var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
            UnloadWatch.Note(parts, "locals", closure, typeof(MonsterSheets1191Tests).Assembly);
            UnloadWatch.Note(parts, "locals0", outer, typeof(MonsterSheets1191Tests).Assembly);
            Check(parts.Keys.Any(key => key.StartsWith("locals." + UnloadWatch.ClosureField, StringComparison.Ordinal)), "the inner closure's field to the outer one is noted: " + string.Join(",", parts.Keys));
            for (var round = 0; round < 3; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            Check(outerValue.IsAlive && innerValue.IsAlive, "held by the closures while they are whole");
            Check(!UnloadWatch.IsEmptied(parts) && UnloadWatch.EmptyHubs(parts) > 0 && UnloadWatch.IsEmptied(parts), "emptied, marked");
            for (var round = 0; round < 10 && (outerValue.IsAlive || innerValue.IsAlive); round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            Check(!outerValue.IsAlive && !innerValue.IsAlive, "the closures emptied (still held here), what they captured is collected");
            GC.KeepAlive(inner); GC.KeepAlive(outer);
        }),
        ("Unload 1191: an instance noted before « locals0 » existed: its outer closure, noted as a field of « locals », is emptied too", () =>
        {
            var (inner, outerValue, _) = Closures();
            var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
            UnloadWatch.Note(parts, "locals", inner.Target, typeof(MonsterSheets1191Tests).Assembly);
            parts["!emptied"] = new WeakReference(null);
            Check(UnloadWatch.IsEmptied(parts) && UnloadWatch.EmptyHubs(parts) > 0, "the first pass's mark does not stop the second pass");
            for (var round = 0; round < 10 && outerValue.IsAlive; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            Check(!outerValue.IsAlive, "the outer closure's capture is collected");
            Check(UnloadWatch.EmptyHubs(parts) == 0, "once per pass");
            GC.KeepAlive(inner);
        }),
        ("Unload 1191: wiring: an emptied instance's line names every part alive; the outer closure is noted at unload", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("Diagnostics.UnloadWatch.Describe(earlier.Key,earlier.Value,Diagnostics.UnloadWatch.IsEmptied(earlier.Value) ? 400:24)", StringComparison.Ordinal), "every name once emptied");
            Check(plugin.Contains("Diagnostics.UnloadWatch.Note(parts,\"locals0\",Diagnostics.UnloadWatch.OuterClosure(developerAtUnload.Target),typeof(Plugin).Assembly);", StringComparison.Ordinal), "outer closure noted");
        }),
    ];
}
