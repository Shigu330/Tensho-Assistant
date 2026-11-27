// 0.1919, X-897 (review of 0.1916 M2). The gate of the runtime's listeners (X-891) was a static of the plugin's assembly: one per load, and
// Dalamud opens a new load context at each reload. A listener of the old load disposed late (the chat's, 2 s after its first frame, by a
// work nobody owned) and one the new load made then took two different gates: the deadlock of X-891 stayed possible across a reload. The
// gate is now the process's one (kept in AppDomain data), and the chat's listener is owned by the window (Held: let go once, by the later
// work or by the window's Dispose). Behaviour tests (R28.14): a second load of the same code in its own load context gets the same gate,
// and listeners of both loads made and disposed at once all end; Held gives its value once under a race.
using System.Reflection;
using System.Runtime.Loader;
using TenshoAssistant.Diagnostics;

internal static class RuntimeGate1919Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly TimeSpan Patience = TimeSpan.FromMinutes(3);

    private sealed class Counted : IDisposable
    {
        public int Disposed;
        public void Dispose() => Interlocked.Increment(ref Disposed);
    }

    // The same code loaded again in a load context of its own, as Dalamud does at each reload of the plugin.
    private static (AssemblyLoadContext Context, Assembly Loaded) Reloaded()
    {
        var context = new AssemblyLoadContext("tensho-gate-1919-" + Guid.NewGuid().ToString("N"), isCollectible: true);
        var loaded = context.LoadFromAssemblyPath(typeof(RuntimeListeners).Assembly.Location);
        Check(!ReferenceEquals(loaded, typeof(RuntimeListeners).Assembly), "a second load of the code");
        return (context, loaded);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Runtime gate 1919: behaviour: a second load of the plugin's code in its own load context gets the same gate, a plain object (X-897)", () =>
        {
            var (context, loaded) = Reloaded();
            try
            {
                var gates = loaded.GetType("TenshoAssistant.Diagnostics.RuntimeListeners", throwOnError: true)!;
                Check(!ReferenceEquals(gates, typeof(RuntimeListeners)), "its own type");
                var other = gates.GetField("Gate", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
                // The old code: one gate per load (a static field initialised with new object()), so two different objects.
                Check(ReferenceEquals(other, RuntimeListeners.Gate), "the same gate for both loads");
                Check(RuntimeListeners.Gate.GetType() == typeof(object), "a System.Object (keeps no load context alive)");
                Check(ReferenceEquals(AppDomain.CurrentDomain.GetData(RuntimeListeners.GateKey), RuntimeListeners.Gate), "kept in the process's data");
            }
            finally { context.Unload(); }
        }),
        ("Runtime gate 1919: behaviour: listeners of two loads of the code made and disposed by 6 threads at once all end (X-897)", () =>
        {
            var (context, loaded) = Reloaded();
            try
            {
                var otherOpen = loaded.GetType("TenshoAssistant.Diagnostics.JitNames", throwOnError: true)!.GetMethod("Open", BindingFlags.Public | BindingFlags.Static)!;
                var failures = new List<string>();
                var ended = 0;
                var threads = Enumerable.Range(0, 6).Select(index => new Thread(() =>
                {
                    try
                    {
                        for (var round = 0; round < 6; round++)
                        {
                            var listener = (index + round) % 2 == 0 ? (IDisposable)otherOpen.Invoke(null, null)! : JitNames.Open();
                            Thread.Sleep(index % 3);
                            listener.Dispose();
                        }
                        Interlocked.Increment(ref ended);
                    }
                    catch (Exception error) { lock (failures) failures.Add(error.Message); }
                }) { IsBackground = true, Name = "gate 1919 " + index }).ToList();
                threads.ForEach(thread => thread.Start());
                var waited = System.Diagnostics.Stopwatch.StartNew();
                foreach (var thread in threads) thread.Join(Patience - waited.Elapsed > TimeSpan.Zero ? Patience - waited.Elapsed : TimeSpan.Zero);
                Check(failures.Count == 0, string.Join("; ", failures));
                Check(ended == threads.Count, $"every thread ended within {Patience.TotalMinutes:0} min: {ended} of {threads.Count}");
            }
            finally { context.Unload(); }
        }),
        ("Runtime gate 1919: behaviour: a held listener is let go once, by the later work or by its owner's Dispose, never both (X-897)", () =>
        {
            var early = new Counted();
            var held = new Held<Counted>(early);
            held.Dispose();
            Check(early.Disposed == 1 && held.Take() is null, "the owner's Dispose lets it go; the later work finds nothing");
            held.Dispose();
            Check(early.Disposed == 1, "let go once");
            var late = new Counted();
            var kept = new Held<Counted>(late);
            Check(ReferenceEquals(kept.Take(), late) && kept.Take() is null, "taken once by the later work");
            kept.Dispose();
            Check(late.Disposed == 0, "the owner's Dispose leaves the one taken to the work that took it");
            for (var round = 0; round < 500; round++)
            {
                var value = new Counted();
                var race = new Held<Counted>(value);
                var taken = 0;
                using var start = new ManualResetEventSlim();
                var work = Task.Run(() => { start.Wait(); if (race.Take() is { } got) { Interlocked.Increment(ref taken); got.Dispose(); } });
                start.Set();
                race.Dispose();
                work.Wait();
                Check(value.Disposed == 1, $"round {round}: let go exactly once ({value.Disposed})");
            }
        }),
        ("Runtime gate 1919: wiring: the chat's listener owned by the window and let go by its Dispose (X-897)", () =>
        {
            var chat = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(chat.Contains("var held=new Diagnostics.Held<Diagnostics.JitNames>(names);", StringComparison.Ordinal) && chat.Contains("chatFrameNames=held;", StringComparison.Ordinal), "owned by the window");
            var lifetime = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Lifetime.cs"));
            Check(lifetime.Contains("chatFrameNames?.Dispose();", StringComparison.Ordinal), "let go by the window's Dispose");
        }),
    ];
}
