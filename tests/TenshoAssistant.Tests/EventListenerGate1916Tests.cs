// 0.1916, X-891: the whole test process stood still under load (27/11, a run beside 32 processor burners: every thread waiting, none
// running, the runner's watchdog included, killed after 16 min). Its memory image, read through the runtime's own data access library
// (docs/evidence/0.1916-event-listener-deadlock.md), named the cause: two listeners of the runtime's events disposed at the same time
// (LargeAllocations1431Tests and Collections1430Tests) took the runtime library's two locks in both orders, and every thread that made an
// EventSource afterwards waited on one of them. A copy made of the same listeners, made and disposed by 6 threads at once, froze 4 times in 4;
// under one gate, never. Every listener of the runtime's events is now made and disposed under RuntimeListeners.Gate.
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text.RegularExpressions;
using TenshoAssistant.Diagnostics;

internal static class EventListenerGate1916Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly TimeSpan Patience = TimeSpan.FromMinutes(3);

    /// <summary>The listeners of the runtime's events in the plugin and the tests, each made and disposed under the gate.</summary>
    private static readonly string[] Gated = ["RuntimeSuspensionListener", "JitNames", "LargeBytes"];
    /// <summary>X-896 (review of 0.1916 M1): each listener of the runtime's events as « file|class », the desktop app's included.</summary>
    private static readonly string[] GatedFiles =
    [
        "src/TenshoAssistant/Diagnostics/RuntimeSuspensions.cs|RuntimeSuspensionListener",
        "src/TenshoAssistant/Diagnostics/JitNames.cs|JitNames",
        "tests/TenshoAssistant.Tests/CacheStream1432Tests.cs|LargeBytes",
        "src/TenshoAssistant.Desktop/Headless.cs|JitNames",
    ];
    /// <summary>X-899 (review of 0.1919 M2): a class and its first base, by its last name (« class X(int a) : A.B.MyBase&lt;T&gt;, I » gives X and MyBase).
    /// A primary constructor with one level of inner parentheses and type parameters are read (X-896).</summary>
    // Not covered, on purpose (X-902, review of 0.1921 M2, as D-233 for the raw newlines' ratchet): a base named through a using alias
    // (« using L = System.Diagnostics.Tracing.EventListener; class Y : L »), a primary constructor with two levels of inner parentheses
    // (« class V(int a = (1 + (2))) : EventListener ») or an attribute with typeof in it (« class X([Foo(typeof(int))] int a) : EventListener »).
    // No source of the plugin, the desktop app or the tests writes these forms, and the listeners compiled into the tests are still checked
    // by their type (IsSubclassOf), whatever the form. A later review finding more forms of this kind sets them aside here with its reason.
    internal static readonly Regex Declared = new(@"\bclass\s+(\w+)\s*(?:<[^>]*>)?\s*(?:\((?:[^()]|\([^()]*\))*\))?\s*:\s*(?:global::)?(?:\w+\.)*(\w+)\b");

    /// <summary>X-899 (review of 0.1919 M2): the listeners of the runtime's events in the sources, as « file|class »: a class whose first base
    /// is EventListener, or a class found a listener here (a base class of the plugin's own, in any file, any number of steps away).</summary>
    internal static List<string> Listeners(IEnumerable<(string File, string Text)> sources)
    {
        var classes = sources.SelectMany(source => Declared.Matches(source.Text).Select(match => (source.File, Name: match.Groups[1].Value, Base: match.Groups[2].Value))).ToList();
        var names = new HashSet<string>(StringComparer.Ordinal) { "EventListener" };
        while (classes.Where(type => names.Contains(type.Base) && !names.Contains(type.Name)).Select(type => type.Name).ToList() is { Count: > 0 } more)
            names.UnionWith(more);
        return classes.Where(type => names.Contains(type.Base)).Select(type => type.File + "|" + type.Name).Distinct().ToList();
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Event listener gate 1916: behaviour: listeners of the runtime's events made and disposed by 6 threads at once all end; none left open", () =>
        {
            var failures = new List<string>();
            var ended = 0;
            var threads = Enumerable.Range(0, 6).Select(index => new Thread(() =>
            {
                try
                {
                    for (var round = 0; round < 6; round++)
                    {
                        if ((index + round) % 2 == 0)
                        {
                            var listener = RuntimeSuspensionListener.Open(_ => { });
                            Thread.Sleep(index % 3);
                            Check(RuntimeSuspensionWatch.End(listener) && listener.IsDisposed, "ended");
                        }
                        else
                        {
                            using var names = JitNames.Open();
                            Thread.Sleep(index % 3);
                        }
                    }
                    Interlocked.Increment(ref ended);
                }
                catch (Exception error) { lock (failures) failures.Add(error.Message); }
            }) { IsBackground = true, Name = "listener gate " + index }).ToList();
            threads.ForEach(thread => thread.Start());
            var waited = System.Diagnostics.Stopwatch.StartNew();
            foreach (var thread in threads) thread.Join(Patience - waited.Elapsed > TimeSpan.Zero ? Patience - waited.Elapsed : TimeSpan.Zero);
            Check(failures.Count == 0, string.Join("; ", failures));
            Check(ended == threads.Count, $"every thread ended within {Patience.TotalMinutes:0} min: {ended} of {threads.Count}");
        }),
        ("Event listener gate 1916: the gated listeners cannot be made outside the gate (no constructor but a private one) and dispose under it", () =>
        {
            foreach (var type in new[] { typeof(RuntimeSuspensionListener), typeof(JitNames) })
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Check(constructors.Length > 0 && constructors.All(constructor => constructor.IsPrivate), type.Name + ": private constructors only");
                Check(type.GetMethod("Open", BindingFlags.Public | BindingFlags.Static) is not null, type.Name + ": Open");
                Check(type.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes)?.DeclaringType == type, type.Name + ": its own Dispose");
            }
            foreach (var file in GatedFiles.Select(entry => entry.Split('|')[0]).Distinct())
            {
                var text = File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n", "\n");
                Check(Regex.IsMatch(text, @"lock ?\((?:TenshoAssistant\.Diagnostics\.)?RuntimeListeners\.Gate\) ?base\.Dispose\(\);"), file + ": base.Dispose under the gate");
                Check(text.Contains("RuntimeListeners.Open(", StringComparison.Ordinal), file + ": made under the gate");
            }
        }),
        ("Event listener gate 1916: every listener of the runtime's events in the plugin and the tests is a gated one", () =>
        {
            // X-896 (review of 0.1916 M1): the desktop app's sources too, and a class with a primary constructor.
            // X-899 (review of 0.1919 M2): a listener through a base class of its own too, in every source read (Listeners).
            var sources = new List<(string File, string Text)>();
            var here = Path.GetFullPath(".");
            foreach (var root in new[] { "src/TenshoAssistant", "src/TenshoAssistant.Desktop", "tests/TenshoAssistant.Tests" })
                foreach (var file in Directory.EnumerateFiles(Path.GetFullPath(root), "*.cs", SearchOption.AllDirectories))
                {
                    if (file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) || file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)) continue;
                    if (file.EndsWith("EventListenerGate1916Tests.cs", StringComparison.Ordinal)) continue;
                    sources.Add((Path.GetRelativePath(here, file).Replace('\\', '/'), File.ReadAllText(file)));
                }
            var found = Listeners(sources);
            Check(found.Count > 0 && found.All(GatedFiles.Contains), "listeners found: " + string.Join(", ", found));
            Check(GatedFiles.All(found.Contains), "the gated ones still exist: " + string.Join(", ", found));
            // The listeners compiled into the tests (the plugin's Diagnostics sources are linked there), indirect ones included.
            var compiled = typeof(EventListenerGate1916Tests).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(EventListener))).Select(type => type.Name).ToList();
            Check(compiled.Count > 0 && compiled.All(Gated.Contains), "listeners compiled into the tests: " + string.Join(", ", compiled));
        }),
        ("Event listener gate 1916: the ratchet finds a class with a primary constructor, type parameters or the full base name, and nothing else (X-896, X-901)", () =>
        {
            // X-901 (review of 0.1921 M1): through Listeners, the pattern the ratchet really uses (Declared), one sample per file.
            string[] listeners =
            [
                "private sealed class A : EventListener",
                "internal sealed class SuspensionWatch(Action<string> say) : EventListener",
                "sealed class B<T>(Func<int> make, int count = (1)) : System.Diagnostics.Tracing.EventListener, IDisposable",
                "class C:global::System.Diagnostics.Tracing.EventListener",
            ];
            string[] others = ["class D : EventListenerGate", "class E(int a) : EventSource", "class F : IEventListener", "class G : Base, IObserver<EventListener>"];
            foreach (var line in listeners)
                Check(Listeners([("x.cs", line + " { }")]) is [var one] && one == "x.cs|" + Declared.Match(line).Groups[1].Value, "found: " + line);
            foreach (var line in others) Check(Listeners([("x.cs", line + " { }")]).Count == 0, "not a listener: " + line);
            var names = Listeners(listeners.Concat(others).Select((line, index) => (index + ".cs", line + " { }"))).Order(StringComparer.Ordinal);
            Check(string.Join(", ", names) == "0.cs|A, 1.cs|SuspensionWatch, 2.cs|B, 3.cs|C", "all at once: " + string.Join(", ", names));
        }),
        ("Event listener gate 1916: the ratchet finds a listener deriving through base classes of its own, across files, and nothing else (X-899)", () =>
        {
            (string File, string Text)[] sources =
            [
                ("a.cs", "internal sealed class Mine : MyBase { }\nclass Plain : Other { }"),
                ("b.cs", "internal abstract class MyBase(int a) : global::System.Diagnostics.Tracing.EventListener, IDisposable { }"),
                ("c.cs", "sealed class Far<T> : Space.Mine { }\nclass Direct : EventListener { }\nclass G : Base, IObserver<EventListener> { }\nclass D : EventListenerGate { }"),
            ];
            var found = Listeners(sources).Order(StringComparer.Ordinal).ToList();
            Check(string.Join(", ", found) == "a.cs|Mine, b.cs|MyBase, c.cs|Direct, c.cs|Far", "found: " + string.Join(", ", found));
        }),
    ];
}
