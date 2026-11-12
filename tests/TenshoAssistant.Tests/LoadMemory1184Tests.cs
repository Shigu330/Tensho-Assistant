using TenshoAssistant.Diagnostics;

// 0.1184 (X-01 C1.1, analysis of the slow frames of 12/11): each load line of the log also says the managed heap, the load contexts named after
// Tensho, and whether the previous instance's context was still alive: measurement only, held weakly, nothing collected or forced.
internal static class LoadMemory1184Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Load memory 1184: Tensho's contexts counted by name, the line in megabytes, the previous instance alive, collected or unknown", () =>
        {
            Check(LoadMemory.Named(["Default", "TenshoAssistant", null, "tenshoassistant (unloading)", "OtherPlugin"], "TenshoAssistant") == 2, "two of Tensho's");
            var line = LoadMemory.Describe(300L * 1_048_576, 250L * 1_048_576, 2, 40, true);
            Check(line == "heap 300 MB, managed 250 MB, Tensho load contexts 2 of 40, previous instance still alive", line);
            Check(LoadMemory.Describe(0, 0, 1, 1, false).EndsWith("previous instance collected", StringComparison.Ordinal), "collected");
            Check(LoadMemory.Describe(0, 0, 1, 1, null).EndsWith("no previous instance known", StringComparison.Ordinal), "first load");
        }),
        ("Load memory 1184: a weak reference does not keep a collectible context alive (what the previous-instance check relies on)", () =>
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            static WeakReference Make()
            {
                var context = new System.Runtime.Loader.AssemblyLoadContext("TenshoAssistant.Test1184", isCollectible: true);
                var weak = new WeakReference(context);
                context.Unload();
                return weak;
            }
            var weak = Make();
            for (var round = 0; round < 10 && weak.IsAlive; round++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            Check(!weak.IsAlive, "an unloaded context held only weakly is collected");
        }),
        ("Load memory 1184: wiring: the slot is read before this instance replaces it, both load lines carry the memory, no collection forced", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var read = plugin.IndexOf("AppDomain.CurrentDomain.GetData(Diagnostics.LoadMemory.PreviousSlot) as WeakReference", StringComparison.Ordinal);
            var write = plugin.IndexOf("AppDomain.CurrentDomain.SetData(Diagnostics.LoadMemory.PreviousSlot,new WeakReference(", StringComparison.Ordinal);
            Check(read > 0 && write > read, "read, then replaced, weakly");
            Check(plugin.Contains("full collections, on thread {Thread}; {Memory}.\"", StringComparison.Ordinal) && plugin.Contains("SayLoad(\"10 s later\")", StringComparison.Ordinal), "both lines");
            Check(!plugin.Contains("GC.Collect(", StringComparison.Ordinal), "no collection forced by the plugin");
        }),
    ];
}
