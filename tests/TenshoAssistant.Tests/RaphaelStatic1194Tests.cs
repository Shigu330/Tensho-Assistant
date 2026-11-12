using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TenshoAssistant.Caching;
using TenshoAssistant.Craft;
using TenshoAssistant.Diagnostics;

// 0.1194 (X-01 C1.3, the root found): the window's static cache of Raphael's sequences was configured with the data cache and a delegate on
// the window; a static lives as long as the load context, so each unloaded instance stayed whole behind it. It forgets both at unload.
internal static class RaphaelStatic1194Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Window { public TimeSpan Duration = TimeSpan.FromHours(2); public byte[] Heavy = new byte[64]; }

    // As in the window: a static cache, configured by an instance.
    private static readonly RaphaelSequenceCache Sequences = new((recipe, stats, quality, options, token) => throw new InvalidOperationException("not solved in this test"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Window, WeakReference Cache) ConfigureByAnInstance()
    {
        var window = new Window();
        var cache = new DataCache(Path.Combine(Path.GetTempPath(), "tensho-1194-" + Guid.NewGuid().ToString("N")));
        Sequences.Configure(() => window.Duration, cache);
        return (new WeakReference(window), new WeakReference(cache));
    }

    private static void Collect(Func<bool> alive) { for (var round = 0; round < 10 && alive(); round++) { GC.Collect(); GC.WaitForPendingFinalizers(); } }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Raphael static 1194: behaviour: the static cache keeps the window and the data cache alive once configured, and lets both go once it forgets them", () =>
        {
            try
            {
                var (window, cache) = ConfigureByAnInstance();
                Check(Sequences.Configured, "configured");
                for (var round = 0; round < 3; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
                Check(window.IsAlive && cache.IsAlive, "the leak reproduced: the static holds the window (through its duration) and the data cache");
                var line = UnloadWatch.Statics(typeof(RaphaelStatic1194Tests).Assembly, 100000).SingleOrDefault(text => text.Contains("RaphaelStatic1194Tests.Sequences:", StringComparison.Ordinal)) ?? "";
                Check(line.Contains("RaphaelSequenceCache holding lifetime: delegate on <>c__DisplayClass", StringComparison.Ordinal) && line.Contains("store: DataCache", StringComparison.Ordinal), "the measure now names what the static holds: " + line);
                Sequences.Forget();
                Check(!Sequences.Configured, "forgotten");
                Collect(() => window.IsAlive || cache.IsAlive);
                Check(!window.IsAlive && !cache.IsAlive, "forgotten at unload: the window and the data cache are collected");
                Check(!UnloadWatch.Statics(typeof(RaphaelStatic1194Tests).Assembly, 100000).Any(text => text.Contains("RaphaelStatic1194Tests.Sequences: RaphaelSequenceCache holding", StringComparison.Ordinal)), "and the measure no longer names it as a holder");
            }
            finally { Sequences.Forget(); }
        }),
        ("Raphael static 1194: wiring: the unload forgets the window's static cache with the other static hooks, last", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).ReplaceLineEndings("\n");
            var release = plugin.IndexOf("var left=TenshoAssistant.Competitive.CompetitiveGate.ReleaseAll(framework);", StringComparison.Ordinal);
            var forget = plugin.IndexOf("MainWindow.ForgetInstance();", StringComparison.Ordinal);
            var next = plugin.IndexOf("registrations.Register(", release, StringComparison.Ordinal);
            Check(release > 0 && forget > release && forget < next, "in the first registration's release, so run last");
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftLog.cs"));
            Check(window.Contains("internal static void ForgetInstance()=>RaphaelSequences.Forget();", StringComparison.Ordinal), "the window's static cache");
        }),
        ("Raphael static 1194: cliquet: a static object of a class that can be configured with a cache is forgotten at unload (a new one is added to ForgetInstance, then listed here)", () =>
        {
            var folder = Path.GetFullPath("src/TenshoAssistant");
            var files = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Select(file => (File: file, Text: File.ReadAllText(file))).ToArray();
            // The classes an instance configures after building them: they declare Configure or ConfigureCache.
            var configurable = files.Where(file => Regex.IsMatch(file.Text, @"public void Configure(?:Cache)?\("))
                .SelectMany(file => Regex.Matches(file.Text, @"\bclass (\w+)").Select(match => match.Groups[1].Value)).ToHashSet(StringComparer.Ordinal);
            Check(configurable.Contains("RaphaelSequenceCache") && configurable.Count >= 3, "configurable classes read: " + configurable.Count);
            var statics = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var file in files)
                foreach (Match match in Regex.Matches(file.Text, @"(?m)^[^/\r\n]*\bstatic\b[^;=(\r\n]*?\b(\w+)\??\s+(\w+)\s*(?:=|;|\{)"))
                    if (configurable.Contains(match.Groups[1].Value)) statics.Add(match.Groups[1].Value + " " + match.Groups[2].Value);
            Check(statics.SetEquals(["RaphaelSequenceCache RaphaelSequences"]), "static configurable objects: " + string.Join(", ", statics));
        }),
    ];
}
