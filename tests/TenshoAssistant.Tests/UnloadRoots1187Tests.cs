using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TenshoAssistant.Competitive;
using TenshoAssistant.Diagnostics;
using TenshoAssistant.Gathering;

// 0.1187 (X-01 C1.3, the root of the leak of unloaded instances; docs/evidence/0.1187-unload-roots.md): the static hooks given by the
// plugin's constructor as closures kept the whole unloaded instance alive (a static lives as long as its load context, which stays alive in
// the game process). They are emptied at unload; the log names, at the next loads, the parts of earlier instances that are still alive.
[SerialTests("CompetitiveGate's static hooks and WeatherCatalog.StoryWeatherConfirmed are shared by the whole process")]
internal static class UnloadRoots1187Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Instance { public byte[] Heavy = new byte[64]; }
    private sealed class Owner
    {
        public Instance? Mine = new();
        public List<Instance> Many = [new()];
        public Instance[] Row = [new()];
        public string Text = "text";
        public int Number = 3;
        public List<string> Foreign = ["a"];
        public Instance? Empty = null;
        public Action Hook = () => { };
    }

    private static void Collect(Func<bool> alive)
    {
        for (var round = 0; round < 10 && alive(); round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
    }

    // What the plugin's constructor does: closures over one captured object given to the static hooks; the weak reference tells whether it lives.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference GiveHooks()
    {
        var instance = new Instance();
        CompetitiveGate.Developer = () => instance.Heavy.Length > 0;
        CompetitiveGate.SlowSubscription = (name, spent, worst) => instance.Heavy[0] = 1;
        CompetitiveGate.Changed += isCut => instance.Heavy[1] = 1;
        WeatherCatalog.StoryWeatherConfirmed = territory => instance.Heavy.Length == territory;
        return new WeakReference(instance);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Dictionary<string, WeakReference> Parts, int Noted) NoteOwner(bool keep, out Owner? kept)
    {
        var owner = new Owner();
        var parts = new Dictionary<string, WeakReference>(StringComparer.Ordinal);
        UnloadWatch.Note(parts, "plugin", owner, typeof(UnloadRoots1187Tests).Assembly);
        kept = keep ? owner : null;
        return (parts, parts.Count);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Unload roots 1187: behaviour: an object captured by the static hooks stays alive while they are set, and is collected once the unload empties them", () =>
        {
            var before = WeatherCatalog.StoryWeatherConfirmed;
            try
            {
                var weak = GiveHooks();
                Check(CompetitiveGate.HoldsInstance, "hooks set");
                Collect(() => false);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                Check(weak.IsAlive, "the leak reproduced: while the static hooks are set, the captured instance cannot be collected");
                CompetitiveGate.ForgetInstance();
                Check(!CompetitiveGate.HoldsInstance, "hooks emptied");
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                Check(weak.IsAlive, "the story weather hook alone still holds it: every static hook has to be emptied");
                WeatherCatalog.StoryWeatherConfirmed = null;
                Collect(() => weak.IsAlive);
                Check(!weak.IsAlive, "every static hook emptied: the instance is collected");
            }
            finally { CompetitiveGate.ForgetInstance(); WeatherCatalog.StoryWeatherConfirmed = before; }
        }),
        ("Unload roots 1187: the cut's subscribers alone are forgotten by ClearSubscribers, the other hooks stay (what the watcher's Dispose does)", () =>
        {
            try
            {
                CompetitiveGate.Developer = () => true;
                var told = 0;
                CompetitiveGate.Changed += _ => told++;
                CompetitiveGate.ClearSubscribers();
                Check(CompetitiveGate.Developer?.Invoke() == true && CompetitiveGate.HoldsInstance, "developer hook kept");
                CompetitiveGate.ForgetInstance();
                Check(!CompetitiveGate.HoldsInstance && told == 0, "all forgotten");
            }
            finally { CompetitiveGate.ForgetInstance(); }
        }),
        ("Unload roots 1187: the parts noted are the owner and its fields that hold the plugin's own objects, by themselves, in a list or in an array", () =>
        {
            var (parts, _) = NoteOwner(true, out var kept);
            Check(string.Join(",", parts.Keys.Order(StringComparer.Ordinal)) == "plugin,plugin.Many,plugin.Mine,plugin.Row", string.Join(",", parts.Keys.Order(StringComparer.Ordinal)));
            Check(UnloadWatch.Ours(typeof(Dictionary<uint, Instance[]>), typeof(Instance).Assembly) && !UnloadWatch.Ours(typeof(Dictionary<uint, string[]>), typeof(Instance).Assembly), "generic arguments and arrays");
            var line = UnloadWatch.Describe("0.1 loaded at 10:00:00", parts);
            Check(line == "0.1 loaded at 10:00:00: context unknown, 4 of 4 parts alive: plugin, plugin.Many, plugin.Mine, plugin.Row", line);
            Check(UnloadWatch.Describe("x", parts, 2) == "x: context unknown, 4 of 4 parts alive: plugin, plugin.Many, …", "names bounded");
            GC.KeepAlive(kept);
            var none = new Dictionary<string, WeakReference>();
            UnloadWatch.Note(none, "window", null, typeof(Instance).Assembly);
            Check(none.Count == 0 && UnloadWatch.Describe("y", none) == "y: context unknown, parts not noted", "nothing noted");
        }),
        ("Unload roots 1187: behaviour: the watch keeps nothing alive: parts noted weakly are collected with their owner, and the line says so", () =>
        {
            var (parts, noted) = NoteOwner(false, out _);
            Collect(() => parts.Values.Any(part => part.IsAlive));
            Check(noted == 4 && UnloadWatch.Describe("z", parts) == "z: context unknown, 0 of 4 parts alive", UnloadWatch.Describe("z", parts));
        }),
        ("Unload roots 1187: behaviour: the slot keeps the newest instance first and five at most, holds contexts weakly, and reads anything else as empty", () =>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static List<KeyValuePair<string, Dictionary<string, WeakReference>>> Loads()
            {
                object? slot = "not a list";
                for (var load = 1; load <= 7; load++)
                {
                    var context = new System.Runtime.Loader.AssemblyLoadContext("TenshoAssistant.Test1187." + load, isCollectible: true);
                    slot = UnloadWatch.Push(slot, "load " + load, context);
                    context.Unload();
                }
                return (List<KeyValuePair<string, Dictionary<string, WeakReference>>>)slot!;
            }
            var all = Loads();
            Check(all.Count == UnloadWatch.Kept && string.Join(",", all.Select(entry => entry.Key)) == "load 7,load 6,load 5,load 4,load 3", string.Join(",", all.Select(entry => entry.Key)));
            Collect(() => all.Any(entry => entry.Value[UnloadWatch.Context].IsAlive));
            Check(all.All(entry => !entry.Value[UnloadWatch.Context].IsAlive), "the slot does not keep an unloaded context alive");
            Check(UnloadWatch.Describe(all[1].Key, all[1].Value) == "load 6: context collected, parts not noted", UnloadWatch.Describe(all[1].Key, all[1].Value));
            Check(UnloadWatch.Push(null, "first", null).Single().Value.Count == 0, "no context known");
            // Runtime types only: an object of the plugin's code in the slot would keep its context alive.
            Check(!UnloadWatch.Ours(all.GetType(), typeof(UnloadWatch).Assembly), "slot made of runtime types only");
        }),
        ("Unload roots 1187: cliquet: every static hook of the plugin (settable delegate or event) is emptied by the unload", () =>
        {
            var folder = Path.GetFullPath("src/TenshoAssistant");
            var declared = new Regex(@"\bstatic\s+(?:event\s+)?(?:Func|Action)\b[^;=(]*?[\s>?](\w+)\s*(?:\{\s*get;|;)", RegexOptions.CultureInvariant);
            var found = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal) || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue;
                foreach (var line in File.ReadLines(file))
                    if (!line.Contains("readonly", StringComparison.Ordinal) && declared.Match(line) is { Success: true } match)
                        found.Add(Path.GetFileNameWithoutExtension(file) + ":" + match.Groups[1].Value);
            }
            var emptied = new SortedSet<string>(StringComparer.Ordinal) { "CompetitiveGateHooks:Changed", "CompetitiveGateHooks:Developer", "CompetitiveGateHooks:SlowSubscription", "WeatherForecast:StoryWeatherConfirmed" };
            Check(found.SetEquals(emptied), "static hooks declared: " + string.Join(", ", found) + " (a new one is emptied at unload in Plugin.cs, then listed here)");
            var hooks = File.ReadAllText(Path.Combine(folder, "Competitive", "CompetitiveGateHooks.cs"));
            var forget = hooks[hooks.IndexOf("internal static void ForgetInstance()", StringComparison.Ordinal)..];
            Check(forget.Contains("Developer=null;", StringComparison.Ordinal) && forget.Contains("SlowSubscription=null;", StringComparison.Ordinal) && forget.Contains("Changed=null;", StringComparison.Ordinal), "the gate's three hooks");
        }),
        ("Unload roots 1187: wiring: the hooks are emptied last at unload, forgotten subscriptions released and counted, parts noted before the release, lines ten seconds after a load", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).ReplaceLineEndings("\n");
            var first = plugin.IndexOf("registrations.Register(", StringComparison.Ordinal);
            var release = plugin.IndexOf("var left=TenshoAssistant.Competitive.CompetitiveGate.ReleaseAll(framework);", StringComparison.Ordinal);
            var second = plugin.IndexOf("registrations.Register(", first + 1, StringComparison.Ordinal);
            Check(first > 0 && release > first && release < second, "the first registration, so the last one released");
            var block = plugin[release..second];
            Check(block.Contains("CompetitiveGate.ForgetInstance();", StringComparison.Ordinal) && block.Contains("Gathering.WeatherCatalog.StoryWeatherConfirmed=null;", StringComparison.Ordinal), "all hooks emptied");
            Check(block.Contains("still registered at unload: released.", StringComparison.Ordinal), "a forgotten subscription is said");
            var dispose = plugin[plugin.IndexOf("    public void Dispose()\n", StringComparison.Ordinal)..];
            var note = dispose.IndexOf("Diagnostics.UnloadWatch.Note(parts,\"plugin\",this,typeof(Plugin).Assembly);", StringComparison.Ordinal);
            Check(note > 0 && note < dispose.IndexOf("registrations.Dispose()", StringComparison.Ordinal) && dispose.Contains("developerAtUnload()", StringComparison.Ordinal), "noted before the release, developer mode only");
            Check(plugin.Contains("AppDomain.CurrentDomain.SetData(Diagnostics.UnloadWatch.Slot,instances);", StringComparison.Ordinal)
                && plugin.Contains("foreach(var earlier in instances.Skip(1))log.Information(\"Earlier instance {0}.\",Diagnostics.UnloadWatch.Describe(earlier.Key,earlier.Value,", StringComparison.Ordinal), "the lines");
            Check(!plugin.Contains("GC.Collect(", StringComparison.Ordinal), "no collection forced by the plugin");
            var gate = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs"));
            Check(gate.Contains("lock(Gate){left=[..Wrappers.Values];Wrappers.Clear();}", StringComparison.Ordinal) && gate.Contains("foreach(var guarded in left)framework.Update-=guarded;", StringComparison.Ordinal), "ReleaseAll takes each wrapper off the framework");
            var map = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(map.Contains("Volatile.Write(ref state,null);Log=null;}", StringComparison.Ordinal), "the map reader's static log forgotten");
        }),
    ];
}
