using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1196 (X-01 C1.3): the fix of 0.1194 was read in the game (0.1195: an unloaded instance's plugin, window, closures and services
// collected, nothing held but shared values), so the tools of the search are taken out: the emptying of old instances (0.1189) and the
// notes three levels deep (0.1192). What stays is one short line per earlier instance: « 0 of N parts alive » when it was collected
// whole, its first parts held otherwise.
internal static class UnloadShort1196Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Leaf { public int Number = 1; }
    private sealed class Service { public Leaf Leaf = new(); public Hub? Back; }
    private sealed class Hub { public Service First = new(); public Service Second = new(); public Leaf[] Rows = [new()]; }

    private static readonly List<object> OutsideRoot = [];

    private static void Collect(Func<bool> alive)
    {
        for (var round = 0; round < 10 && alive(); round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
    }

    // An instance noted as the plugin notes itself at unload; a root outside holds one of its services when « keep » says so, and
    // the service points back to the hub, so the whole instance hangs from that root.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, WeakReference> Noted(bool keep)
    {
        var hub = new Hub();
        hub.First.Back = hub;
        if (keep) OutsideRoot.Add(hub.First);
        var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
        UnloadWatch.Note(parts, "plugin", hub, typeof(UnloadShort1196Tests).Assembly);
        return parts;
    }

    // Read in a frame of its own: a temporary of the test's own frame would keep the instance alive.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SecondKept(Dictionary<string, WeakReference> parts) => parts["plugin"].Target is Hub { Second: not null };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Unload short 1196: behaviour: a hub is noted by its own fields only, and an instance let go reads as one short line, nothing alive", () =>
        {
            OutsideRoot.Clear();
            var parts = Noted(false);
            Check(string.Join(" | ", parts.Keys.Order(StringComparer.Ordinal)) == "plugin | plugin.First | plugin.Rows | plugin.Second", "one level, no path behind a field: " + string.Join(" | ", parts.Keys.Order(StringComparer.Ordinal)));
            Collect(() => parts.Values.Any(part => part.IsAlive));
            var line = UnloadWatch.Describe("0.1196.0 loaded at 10:00:00", parts, 24);
            Check(line == "0.1196.0 loaded at 10:00:00: context unknown, 0 of 4 parts alive", line);
        }),
        ("Unload short 1196: behaviour: an instance a root still holds is not emptied by the measure: it stays whole, and the line names its parts", () =>
        {
            OutsideRoot.Clear();
            try
            {
                var parts = Noted(true);
                for (var round = 0; round < 3; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
                var line = UnloadWatch.Describe("i", parts, 24);
                Check(line == "i: context unknown, 4 of 4 parts alive: plugin, plugin.First, plugin.Rows, plugin.Second", line);
                // Reading the line twice changes nothing: the leak is reported, never papered over.
                Check(UnloadWatch.Describe("i", parts, 24) == line && SecondKept(parts), "nothing written into the instance");
                OutsideRoot.Clear();
                Collect(() => parts.Values.Any(part => part.IsAlive));
                Check(UnloadWatch.Describe("i", parts, 24) == "i: context unknown, 0 of 4 parts alive", "the root let go: collected whole");
            }
            finally { OutsideRoot.Clear(); }
        }),
        ("Unload short 1196: cliquet: the tools of the search are gone from the measure and from the plugin", () =>
        {
            var watch = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/UnloadWatch.cs"));
            foreach (var gone in new[] { "NoteGraph(", "EmptyHubs(", "IsEmptied(", "int Empty(", ".SetValue(" })
                Check(!watch.Contains(gone, StringComparison.Ordinal), "still in the measure: " + gone);
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).ReplaceLineEndings("\n");
            foreach (var gone in new[] { "NoteGraph(", "EmptyHubs(", "hubs emptied", "instances.Skip(2)" })
                Check(!plugin.Contains(gone, StringComparison.Ordinal), "still in the plugin: " + gone);
            var dispose = plugin[plugin.IndexOf("    public void Dispose()\n", StringComparison.Ordinal)..];
            Check(dispose.Split("Diagnostics.UnloadWatch.Note(parts,").Length == 5, "the four hubs noted at unload, by their own fields");
            Check(plugin.Contains("foreach(var earlier in instances.Skip(1))log.Information(\"Earlier instance {0}.\",Diagnostics.UnloadWatch.Describe(earlier.Key,earlier.Value,24));", StringComparison.Ordinal), "one short line per earlier instance");
        }),
    ];
}
