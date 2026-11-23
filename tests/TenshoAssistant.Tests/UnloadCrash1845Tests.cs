using System.Runtime.CompilerServices;
using TenshoAssistant.Diagnostics;

// X-802 (0.1845, the game's crash of 22/11 13:46:56, read in its dump: an access violation inside the .NET runtime on a pool thread, in
// RuntimeFieldHandle.GetValue of a static field, one second after the developer mode's « ten seconds after the load » line and before its
// « Earlier instance » lines): the line about an earlier instance never takes one of its objects. A part is asked IsAlive only; its Target
// (an object of a copy of Tensho whose load context is unloading) is never read, so no static field of its types is read by reflection.
internal static class UnloadCrash1845Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    /// <summary>A weak reference that counts the reads of its target: a read is what took an object of the earlier instance.</summary>
    private sealed class Watched(object? target) : WeakReference(target)
    {
        public int TargetReads;
        public override object? Target { get { TargetReads++; return base.Target; } set => base.Target = value; }
    }

    private sealed class Earlier { public readonly List<object> Own = []; }

    private static readonly List<object> Kept = [];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, WeakReference> Parts(List<Watched> watched)
    {
        var plugin = new Earlier();
        Kept.Add(plugin);
        Kept.Add(plugin.Own);
        var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
        foreach (var (name, target) in new (string, object?)[] { ("plugin", plugin), ("plugin.Own", plugin.Own), ("plugin.Gone", new object()) })
        {
            var one = new Watched(target);
            watched.Add(one);
            parts[name] = one;
        }
        return parts;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Unload crash 1845: behaviour: the line about an earlier instance asks its parts IsAlive only, never their Target, alive or collected", () =>
        {
            Kept.Clear();
            try
            {
                var watched = new List<Watched>();
                var parts = Parts(watched);
                for (var round = 0; round < 10 && parts["plugin.Gone"].IsAlive; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
                var line = UnloadWatch.Describe("0.1844.0 loaded at 13:46:44", parts, 24);
                Check(line == "0.1844.0 loaded at 13:46:44: context unknown, 2 of 3 parts alive; collected: plugin.Gone", line);
                Check(watched.All(one => one.TargetReads == 0), "a target was read: " + string.Join(", ", watched.Select(one => one.TargetReads)));
                // An object of the earlier instance kept alive by a root is named, and still nothing of it is read.
                Kept.Clear();
                for (var round = 0; round < 10 && parts.Values.Any(part => part.IsAlive); round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
                Check(UnloadWatch.Describe("i", parts, 24) == "i: context unknown, 0 of 3 parts alive", UnloadWatch.Describe("i", parts, 24));
                Check(watched.All(one => one.TargetReads == 0), "a target was read once collected");
            }
            finally { Kept.Clear(); }
        }),
        ("Unload crash 1845: wiring: ten seconds after a load, the earlier instances get one line each, and none of their objects is taken", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("foreach(var earlier in instances.Skip(1))log.Information(\"Earlier instance {0}.\",Diagnostics.UnloadWatch.Describe(earlier.Key,earlier.Value,24));", StringComparison.Ordinal), "one line each");
            Check(!plugin.Contains("GetValueOrDefault(\"plugin\")?.Target", StringComparison.Ordinal), "the earlier plugin object is not taken");
            Check(!plugin.Contains("UnloadWatch.Statics(", StringComparison.Ordinal) && !plugin.Contains("UnloadWatch.Released(", StringComparison.Ordinal), "nothing of an earlier assembly read");
            var watch = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/UnloadWatch.cs"));
            var describe = watch[watch.IndexOf("public static string Describe(", StringComparison.Ordinal)..];
            describe = describe[..describe.IndexOf("    }", StringComparison.Ordinal)];
            Check(!describe.Contains(".Target", StringComparison.Ordinal) && !describe.Contains("Shared(", StringComparison.Ordinal), "Describe takes no target and reads no statics");
        }),
    ];
}
