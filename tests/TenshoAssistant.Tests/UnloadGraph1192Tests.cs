using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1192 (X-01 C1.3): the plugin's own objects reachable from the hubs are noted at unload under their path, three levels deep, through
// fields and delegates' targets; once the hubs are emptied, the names still alive are the chain an outside root holds.
internal static class UnloadGraph1192Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Hub { public Service? First; public Service? Second; public Action? Callback; public List<Leaf> Many = [new()]; public string Text = "t"; }
    private sealed class Service { public Leaf? Leaf; public Hub? Back; public Service? Next; }
    private sealed class Leaf { public Deep? Deep; }
    private sealed class Deep { public Deeper? Deeper; }
    private sealed class Deeper { public int Number = 1; }
    private sealed class Listener { public Leaf Leaf = new(); public void On() { } }

    private static readonly List<object> OutsideRoot = [];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, WeakReference> Instance(int depth = 3, int cap = 3000)
    {
        var hub = new Hub { First = new Service { Leaf = new Leaf { Deep = new Deep { Deeper = new Deeper() } } }, Second = new Service() };
        hub.First.Back = hub;
        hub.Second.Next = hub.First;
        var listener = new Listener();
        hub.Callback = listener.On;
        hub.Callback += static () => { };
        // The outside root holds one service; the service points back to the hub, so the whole instance hangs from it.
        OutsideRoot.Add(hub.First);
        var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
        UnloadWatch.Note(parts, "plugin", hub, typeof(UnloadGraph1192Tests).Assembly);
        var added = UnloadWatch.NoteGraph(parts, "plugin", hub, typeof(UnloadGraph1192Tests).Assembly, depth, cap);
        Check(added == Math.Min(cap, depth >= 3 ? 4 : 1), "objects added to those the hub's own fields gave: " + added);
        return parts;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Unload graph 1192: the plugin's objects behind a hub are noted by path, through fields and delegates' targets, each once, to the depth given", () =>
        {
            OutsideRoot.Clear();
            try
            {
                var parts = Instance();
                var names = string.Join(" | ", parts.Keys.Order(StringComparer.Ordinal));
                Check(names == "plugin | plugin.Callback→Listener | plugin.Callback→Listener.Leaf | plugin.First | plugin.First.Leaf | plugin.First.Leaf.Deep | plugin.Many | plugin.Second", names);
                Check(!parts.ContainsKey("plugin.First.Leaf.Deep.Deeper"), "not deeper than three levels");
                Check(!parts.Keys.Any(key => key.Contains("Back", StringComparison.Ordinal) || key.Contains("Next", StringComparison.Ordinal)), "an object already noted is not noted again under another path");
                var shallow = Instance(1);
                Check(string.Join(" | ", shallow.Keys.Order(StringComparer.Ordinal)) == "plugin | plugin.Callback→Listener | plugin.First | plugin.Many | plugin.Second", "one level");
                Check(Instance(3, 2).Count == 6, "bounded by the cap (the hub's own four names were noted before)");
                Check(UnloadWatch.NoteGraph([], "none", null, typeof(UnloadGraph1192Tests).Assembly) == 0, "nothing");
            }
            finally { OutsideRoot.Clear(); }
        }),
        ("Unload graph 1192: behaviour: an outside root holds a service: once the hub is emptied, the names still alive are the chain from that service (and the emptied hub behind it)", () =>
        {
            OutsideRoot.Clear();
            try
            {
                var parts = Instance();
                for (var round = 0; round < 3; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
                Check(parts.Values.All(part => part.IsAlive), "the leak reproduced: the service held outside reaches the hub and all it holds");
                Check(UnloadWatch.EmptyHubs(parts) > 0, "emptied");
                for (var round = 0; round < 10 && parts["plugin"].IsAlive; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
                var line = UnloadWatch.Describe("i", parts, 400);
                Check(line == "i: context unknown, hubs emptied, 4 of 8 parts alive: plugin, plugin.First, plugin.First.Leaf, plugin.First.Leaf.Deep", line);
            }
            finally { OutsideRoot.Clear(); }
        }),
        ("Unload graph 1192: wiring: at unload, developer mode, the four hubs' graphs are noted after their fields, before anything is released", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).ReplaceLineEndings("\n");
            var dispose = plugin[plugin.IndexOf("    public void Dispose()\n", StringComparison.Ordinal)..];
            var graph = dispose.IndexOf("Diagnostics.UnloadWatch.NoteGraph(parts,hub,owner,typeof(Plugin).Assembly);", StringComparison.Ordinal);
            Check(graph > 0 && graph < dispose.IndexOf("registrations.Dispose()", StringComparison.Ordinal) && graph > dispose.IndexOf("developerAtUnload()", StringComparison.Ordinal), "noted in developer mode, before the release");
            Check(dispose.Contains("(\"plugin\",this),(\"window\",mainWindow),(\"locals\",developerAtUnload.Target),(\"locals0\",", StringComparison.Ordinal), "the four hubs");
        }),
    ];
}
