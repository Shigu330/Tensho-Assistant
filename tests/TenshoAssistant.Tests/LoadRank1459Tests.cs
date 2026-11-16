using TenshoAssistant.Diagnostics;

// 0.1459 (X-433): the load line says the load's rank in the game process (a process-wide int slot), to tell the heap that reloads leave from
// the version when frame gaps follow a load (docs/evidence/0.1459-gaps-by-heap.md).
internal static class LoadRank1459Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Load rank 1459: counted in the process, 1 at the first load or after an unreadable slot", () =>
        {
            Check(LoadMemory.NextRank(null) == 1 && LoadMemory.NextRank(1) == 2 && LoadMemory.NextRank(144) == 145, "next");
            Check(LoadMemory.NextRank("7") == 1 && LoadMemory.NextRank(0) == 1 && LoadMemory.NextRank(-3) == 1 && LoadMemory.NextRank(int.MaxValue) == 1, "unreadable: 1");
            var slot = "TenshoAssistant.Tests.LoadRank." + Guid.NewGuid();
            for (var load = 0; load < 3; load++) AppDomain.CurrentDomain.SetData(slot, LoadMemory.NextRank(AppDomain.CurrentDomain.GetData(slot)));
            Check(AppDomain.CurrentDomain.GetData(slot) is 3, "three loads in one process");
        }),
        ("Load rank 1459: said in the load line, read before the memory is described", () =>
        {
            Check(LoadMemory.Describe(0, 1600L * 1_048_576, 1, 3, true, 145).EndsWith(", previous instance still alive, load 145 in this game process", StringComparison.Ordinal), "with the rank");
            Check(LoadMemory.Describe(0, 0, 1, 1, null).EndsWith("no previous instance known", StringComparison.Ordinal), "without");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var rank = plugin.IndexOf("var loadRank=Diagnostics.LoadMemory.NextRank(AppDomain.CurrentDomain.GetData(Diagnostics.LoadMemory.RankSlot));", StringComparison.Ordinal);
            Check(rank > 0 && plugin.IndexOf("contexts.Length,previousContext?.IsAlive,loadRank)", rank, StringComparison.Ordinal) > rank, "the load line has it");
        }),
    ];
}
