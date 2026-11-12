using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TenshoAssistant.Diagnostics;

// 0.1189. X-177 C6: the « Nearby monsters » line of the log is built in developer mode only (it was built every second for every player).
// X-01 C1.3: the hubs of an instance unloaded at least one whole load ago are emptied by a later load in developer mode, so that what an
// outside root still holds no longer reaches the rest of that instance.
internal static class NearbyLog1189Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private class HubBase { protected object? inherited = new(); public bool InheritedEmpty => inherited is null; }
    private sealed class Hub : HubBase
    {
        public readonly Service Service = new();
        private List<Service>? many = [new()];
        public string Name = "kept";
        public int Number = 7;
        public object? Nothing = null;
        public Action? Callback;
        public bool ManyEmpty => many is null;
    }
    private sealed class Service { public byte[] Heavy = new byte[32]; public Hub? Back; }

    // An outside root (here a static list) holds one service of the instance; the service reaches everything through the hub.
    private static readonly List<object> OutsideRoot = [];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, WeakReference> Instance()
    {
        var hub = new Hub();
        var other = new Service();
        hub.Callback = () => other.Heavy[0] = 1;
        hub.Service.Back = hub;
        OutsideRoot.Add(hub.Service);
        var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
        UnloadWatch.Note(parts, "plugin", hub, typeof(NearbyLog1189Tests).Assembly);
        parts["other"] = new WeakReference(other);
        return parts;
    }

    private static void Collect() { for (var round = 0; round < 3; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); } }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Nearby log 1189: cliquet: every « Nearby monsters » line is built only in developer mode", () =>
        {
            var source = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.NearbyMonsters.cs"));
            var calls = Regex.Matches(source, @"(.{0,24})NearbyMonstersLog\(\$").Select(match => match.Groups[1].Value).ToArray();
            Check(calls.Length >= 3, "calls read: " + calls.Length);
            Check(calls.All(before => before.EndsWith("if(developerMode)", StringComparison.Ordinal)), "a line built without asking developerMode first");
            Check(Regex.Matches(source, @"NearbyMonstersLog\(").Count == calls.Length + 1, "every call builds its text behind the check (plus the declaration)");
        }),
        ("Unload 1189: behaviour: an outside root holds one service: everything stays alive; the hubs emptied, only what the root holds stays", () =>
        {
            OutsideRoot.Clear();
            try
            {
                var parts = Instance();
                Collect();
                Check(parts.Values.All(part => part.IsAlive), "the leak reproduced: one service held outside keeps the hub and all it reaches: " + UnloadWatch.Describe("i", parts));
                var emptied = UnloadWatch.EmptyHubs(parts);
                Check(emptied == 4, "the hub's reference fields, its base type's included: " + emptied);
                Check(UnloadWatch.EmptyHubs(parts) == 0, "once");
                Collect();
                var line = UnloadWatch.Describe("i", parts);
                Check(line == "i: context unknown, hubs emptied, 2 of 4 parts alive: plugin, plugin.Service", line);
                Check(parts["plugin"].IsAlive && parts["plugin.Service"].IsAlive && !parts["other"].IsAlive, "what the root holds stays (the service, and the emptied hub behind it); the rest is collected");
                var hub = (Hub)parts["plugin"].Target!;
                Check(hub.Name == "kept" && hub.Number == 7 && hub.ManyEmpty && hub.InheritedEmpty && hub.Callback is null, "texts and numbers kept, references emptied");
            }
            finally { OutsideRoot.Clear(); }
        }),
        ("Unload 1189: an object of another assembly is left alone, nothing alive means nothing emptied and no mark", () =>
        {
            var list = new List<object> { new() };
            Check(UnloadWatch.Empty(list, typeof(NearbyLog1189Tests).Assembly) == 0 && list.Count == 1, "runtime types untouched");
            Check(UnloadWatch.Empty(null, typeof(NearbyLog1189Tests).Assembly) == 0, "nothing");
            var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal) { ["plugin"] = new WeakReference(null) };
            Check(UnloadWatch.EmptyHubs(parts) == 0 && !parts.ContainsKey(UnloadWatch.Emptied), "no mark");
            Check(string.Join(",", UnloadWatch.Hubs) == "plugin,window,locals", "the three hubs noted at unload");
        }),
        ("Unload 1189: wiring: developer mode, ten seconds after a load, only the instances two loads back or more are emptied, after their lines", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).ReplaceLineEndings("\n");
            var lines = plugin.IndexOf("foreach(var earlier in instances.Skip(1))log.Information(\"Earlier instance {0}.\"", StringComparison.Ordinal);
            var empty = plugin.IndexOf("foreach(var old in instances.Skip(2))\n                    if(Diagnostics.UnloadWatch.EmptyHubs(old.Value) is var emptied and >0)", StringComparison.Ordinal);
            var developer = plugin.IndexOf("if(!configuration.DeveloperMode)return;", StringComparison.Ordinal);
            Check(developer > 0 && lines > developer && empty > lines, "developer mode only, lines first, never the instance just unloaded");
            Check(!plugin.Contains("UnloadWatch.Empty(", StringComparison.Ordinal), "the plugin empties through EmptyHubs only (once, marked)");
        }),
    ];
}
