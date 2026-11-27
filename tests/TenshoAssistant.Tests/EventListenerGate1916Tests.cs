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
            foreach (var file in new[] { "src/TenshoAssistant/Diagnostics/RuntimeSuspensions.cs", "src/TenshoAssistant/Diagnostics/JitNames.cs", "tests/TenshoAssistant.Tests/CacheStream1432Tests.cs" })
            {
                var text = File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n", "\n");
                Check(Regex.IsMatch(text, @"lock ?\(RuntimeListeners\.Gate\) ?base\.Dispose\(\);"), file + ": base.Dispose under the gate");
                Check(text.Contains("RuntimeListeners.Open(", StringComparison.Ordinal), file + ": made under the gate");
            }
        }),
        ("Event listener gate 1916: every listener of the runtime's events in the plugin and the tests is a gated one", () =>
        {
            var found = new List<string>();
            foreach (var root in new[] { "src/TenshoAssistant", "tests/TenshoAssistant.Tests" })
                foreach (var file in Directory.EnumerateFiles(Path.GetFullPath(root), "*.cs", SearchOption.AllDirectories))
                {
                    if (file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) || file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)) continue;
                    if (file.EndsWith("EventListenerGate1916Tests.cs", StringComparison.Ordinal)) continue;
                    foreach (Match match in Regex.Matches(File.ReadAllText(file), @"class (\w+)\s*:\s*(?:System\.Diagnostics\.Tracing\.)?EventListener\b"))
                        found.Add(match.Groups[1].Value);
                }
            Check(found.Count > 0 && found.All(name => Gated.Contains(name)), "listeners found: " + string.Join(", ", found));
            Check(Gated.All(found.Contains), "the gated ones still exist: " + string.Join(", ", found));
        }),
    ];
}
