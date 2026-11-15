using TenshoAssistant.Diagnostics;

// 0.1447 (X-433): two freezes « induced for low memory » (0.1443, 0.1445) without a word in the log on the machine's memory: the load lines
// and each collection of generation 2 say the process's private memory and the machine's; developer mode, the two minutes after a load
// say what was allocated, by type and by thread, and by the whole process.
internal static class MemoryMeasure1447Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const long Mb = 1_048_576;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Memory measure 1447: the machine's and the process's memory in words; unknown said as such",() =>
        {
            var text = MachineMemory.Text(new MachineMemory.Reading(5819 * Mb,32549 * Mb,82,40485 * Mb),8309 * Mb);
            Check(text == "process private 8309 MB, machine 5819 MB available of 32549 MB (load 82 %), commit 40485 MB available",text);
            Check(MachineMemory.Text(null,-1) == "process private unknown, machine memory unknown",MachineMemory.Text(null,-1));
            Check(MachineMemory.CollectorText(30 * Mb,27 * Mb,32 * Mb) == "; the collector saw a memory load of 93 % (high above 84 %)",MachineMemory.CollectorText(30 * Mb,27 * Mb,32 * Mb));
            Check(MachineMemory.CollectorText(0,0,0) == "","nothing when the collector saw nothing yet");
            Check(MachineMemory.AllocatedText(1536 * Mb,1400 * Mb,1530 * Mb,TimeSpan.FromMinutes(2)) == "Process allocated 1536 MB in the 120 s after the load (every thread); managed memory 1400 MB before the load, 1530 MB after.","allocated");
        }),
        ("Memory measure 1447: the machine's memory is read on Windows, the process's private memory too",() =>
        {
            if (!OperatingSystem.IsWindows()) return;
            var machine = MachineMemory.Read();
            Check(machine is { TotalBytes: > 0 } m && m.AvailableBytes >= 0 && m.AvailableBytes <= m.TotalBytes && m.LoadPercent is >= 0 and <= 100,"a reading: " + machine);
            Check(MachineMemory.ProcessPrivateBytes() > 0,"the process's private memory");
        }),
        ("Memory measure 1447: allocation ticks summed by type and by thread, the largest first, the types bounded",() =>
        {
            var types = new AllocationTypes(kept: 2,maximumTypes: 3);
            types.See("System.Byte[]",100 * 1024,7,7,9);
            types.See("System.String",300 * 1024,9,7,9);
            types.See("System.Byte[]",300 * 1024,5,7,9);
            types.See("Tensho.Row",50 * 1024,5,7,9);
            types.See("Tensho.Late",20 * Mb,5,7,9);   // past the bound: counted with the other types
            types.See(null,0,5,7,9);                  // no amount: not a tick
            Check(types.Ticks == 5 && types.Total == 750 * 1024 + 20 * Mb,"ticks and total: " + types.Ticks + ", " + types.Total);
            Check(types.OnGameThread == 100 * 1024 && types.OnWarmupThread == 300 * 1024,"by thread");
            var summary = types.Summary(TimeSpan.FromMinutes(2));
            Check(summary.Contains("by type: System.Byte[] 0.4 MB, System.String 0.3 MB; other types 20 MB.",StringComparison.Ordinal),summary);
            Check(summary.StartsWith("Allocated in the 120 s after the load (allocation ticks, about one per 100 KB): 20 MB in 5 ticks, 0.1 MB on the game's thread, 0.3 MB on the JIT warm-up's;",StringComparison.Ordinal),summary);
            Check(new AllocationTypes().Summary(TimeSpan.FromMinutes(2)).Contains("by type: none.",StringComparison.Ordinal),"nothing seen");
        }),
        ("Memory measure 1447: the listener weighs every tick, a collection of generation 2 says the memory, the plugin says both",() =>
        {
            var listener = Read("src/TenshoAssistant/Diagnostics/RuntimeSuspensions.cs");
            Check(listener.Contains("allocations.See(typeName,Number(written,\"AllocationAmount64\") ?? Number(written,\"AllocationAmount\") ?? 0,written.OSThreadId,",StringComparison.Ordinal),"every tick, weighed");
            Check(listener.Contains("collectionNote.Contains(\" collection of generation 2 \",StringComparison.Ordinal) ? WithMemory(collectionNote):collectionNote",StringComparison.Ordinal),"generation 2 with the memory");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("+\"; \"+Diagnostics.MachineMemory.Text(Diagnostics.MachineMemory.Read(),Diagnostics.MachineMemory.ProcessPrivateBytes());",StringComparison.Ordinal),"the load lines");
            var jit = plugin.IndexOf("log.Information(suspensions.Jit.Summary(",StringComparison.Ordinal);
            Check(jit > 0 && plugin.IndexOf("log.Information(suspensions.Allocations.Summary(",jit,StringComparison.Ordinal) > jit
                && plugin.IndexOf("Diagnostics.MachineMemory.AllocatedText(GC.GetTotalAllocatedBytes()-allocatedBefore,managedBefore,",jit,StringComparison.Ordinal) > jit,"said at the end of the watch");
        }),
    ];
}
