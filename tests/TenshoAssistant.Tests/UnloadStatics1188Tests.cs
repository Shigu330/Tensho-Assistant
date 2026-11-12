using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// 0.1188 (X-01 C1.3, second measure: 330 of 338 parts of the instance unloaded at 21:32 were still alive after a full collection, the
// static hooks emptied): what the log adds about an earlier instance still alive, read only: the parts collected when they are the
// fewer, whether its unload finished, and which static fields of its assembly still hold something an instance could hang from.
internal static class UnloadStatics1188Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Scope { private readonly Stack<Action> cleanup = new(); private bool disposed; public void Add(Action release) => cleanup.Push(release); public void Dispose() { disposed = true; cleanup.Clear(); GC.KeepAlive(disposed); } }
    private sealed class FakePlugin { private readonly Scope registrations = new(); public Scope Registrations => registrations; }
    private sealed class Holder { public object? Held = null; public string Text = ""; }
    private sealed record Numbers(int Count, string Name);

    private static class Statics1188
    {
        public static Func<int>? Hook;
        public static Action? StaticLambda = static () => { };
        public static Holder? Kept;
        public static Numbers? Data;
        public static readonly List<Holder> Many = [];
        public static readonly List<string> Texts = ["a"];
        public static readonly Dictionary<Action, Action> Wrappers = [];
        public static string Name = "name";
        public static int Count = 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, WeakReference> SomeCollected()
    {
        var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal) { [UnloadWatch.Context] = new WeakReference(new object()) };
        foreach (var name in new[] { "a", "b", "c" }) parts[name] = new WeakReference(Kept[name] = new object());
        parts["gone"] = new WeakReference(new object());
        return parts;
    }
    private static readonly Dictionary<string, object> Kept = [];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Unload statics 1188: behaviour: the line lists the collected parts when they are the fewer, the alive ones otherwise", () =>
        {
            var parts = SomeCollected();
            for (var round = 0; round < 10 && parts["gone"].IsAlive; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            Check(UnloadWatch.Describe("v", parts) == "v: context collected, 3 of 4 parts alive; collected: gone", UnloadWatch.Describe("v", parts));
            Kept.Remove("a"); Kept.Remove("b");
            for (var round = 0; round < 10 && parts["a"].IsAlive; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            Check(UnloadWatch.Describe("v", parts) == "v: context collected, 1 of 4 parts alive: c", UnloadWatch.Describe("v", parts));
            Kept.Clear();
        }),
        ("Unload statics 1188: an earlier plugin's unload is read by field name: scope disposed or not, releases left; unknown when the fields are not there", () =>
        {
            var plugin = new FakePlugin();
            plugin.Registrations.Add(() => { }); plugin.Registrations.Add(() => { });
            Check(UnloadWatch.Released(plugin) == "registrations disposed: False, releases left: 2", UnloadWatch.Released(plugin));
            plugin.Registrations.Dispose();
            Check(UnloadWatch.Released(plugin) == "registrations disposed: True, releases left: 0", UnloadWatch.Released(plugin));
            Check(UnloadWatch.Released(new Holder()) == "registrations unknown", "another object");
        }),
        ("Unload statics 1188: behaviour: a static that holds a delegate with a target, an object with reference fields or a filled collection is named; data and static lambdas are not", () =>
        {
            try
            {
                var instance = new Holder();
                Statics1188.Hook = () => instance.Text.Length;
                Statics1188.Kept = instance;
                Statics1188.Data = new(1, "x");
                Statics1188.Many.Add(instance);
                Statics1188.Wrappers[() => { }] = () => { };
                var mine = UnloadWatch.Statics(typeof(UnloadStatics1188Tests).Assembly, 100000).Where(line => line.Contains("Statics1188.", StringComparison.Ordinal)).Select(line => line[(line.IndexOf("Statics1188.", StringComparison.Ordinal) + 12)..]).Order(StringComparer.Ordinal).ToList();
                Check(mine.Count == 4, string.Join(" | ", mine));
                Check(mine[0].StartsWith("Hook: delegate on <>c__DisplayClass", StringComparison.Ordinal), mine[0]);
                Check(mine[1] == "Kept: Holder (1 reference fields)", mine[1]);
                Check(mine[2] == "Many: 1 items of List`1<Holder>", mine[2]);
                Check(mine[3] == "Wrappers: 1 items of Dictionary`2<Action,Action>", mine[3]);
                Check(UnloadWatch.Statics(typeof(UnloadStatics1188Tests).Assembly, 3).Count == 3, "bounded");
            }
            finally { Statics1188.Hook = null; Statics1188.Kept = null; Statics1188.Data = null; Statics1188.Many.Clear(); Statics1188.Wrappers.Clear(); }
            Check(!UnloadWatch.Statics(typeof(UnloadStatics1188Tests).Assembly, 100000).Any(line => line.Contains("Statics1188.", StringComparison.Ordinal)), "emptied: nothing named");
        }),
        ("Unload statics 1188: wiring: ten seconds after a load, developer mode, the newest earlier instance still alive gets its unload state and its statics; the constructor's locals are noted", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("log.Information(\"Earlier instance {0}: {1}.\",held.Key,Diagnostics.UnloadWatch.Released(alive));", StringComparison.Ordinal), "unload state");
            Check(plugin.Contains("foreach(var line in Diagnostics.UnloadWatch.Statics(alive.GetType().Assembly))log.Information(\"Earlier instance {0}: static {1}.\",held.Key,line);", StringComparison.Ordinal), "statics of the earlier assembly");
            Check(plugin.Contains("Diagnostics.UnloadWatch.Note(parts,\"locals\",developerAtUnload.Target,typeof(Plugin).Assembly);", StringComparison.Ordinal), "captured locals noted");
            var watch = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/UnloadWatch.cs"));
            Check(!watch.Contains(".SetValue(", StringComparison.Ordinal) && !watch.Contains(".Invoke(", StringComparison.Ordinal) && !watch.Contains("GC.Collect", StringComparison.Ordinal), "read only: nothing of an earlier instance is changed, run or collected");
        }),
    ];
}
