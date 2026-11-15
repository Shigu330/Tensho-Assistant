using TenshoAssistant.Diagnostics;

// 0.1448 (X-433): the allocations of the two minutes after a load, by thread (the game's, the JIT warm-up's, the readers' compiler's, the
// measure's own listener, the others), each with its largest types, so that Tensho's own is told from the measure's cost.
internal static class MemoryMeasure1448Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const long Kb = 1024, Mb = 1_048_576;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Memory measure 1448: a tick's thread is told from the known ids; an id not known yet never matches",() =>
        {
            Check(AllocationTypes.Of(7,7,9,11,13) == AllocationTypes.Where.Game && AllocationTypes.Of(9,7,9,11,13) == AllocationTypes.Where.Warmup,"game, warm-up");
            Check(AllocationTypes.Of(11,7,9,11,13) == AllocationTypes.Where.Compiler && AllocationTypes.Of(13,7,9,11,13) == AllocationTypes.Where.Listener,"compiler, listener");
            Check(AllocationTypes.Of(5,7,9,11,13) == AllocationTypes.Where.Other && AllocationTypes.Of(0,0,0,0,0) == AllocationTypes.Where.Other,"others; 0 never matches");
        }),
        ("Memory measure 1448: each thread has its amount and its largest types, bounded by thread",() =>
        {
            var types = new AllocationTypes(kept: 2,maximumTypes: 2);
            types.See("System.String",3 * Mb,7,7,9,11,13);
            types.See("System.String",1 * Mb,7,7,9,11,13);
            types.See("Tensho.Row",2 * Mb,7,7,9,11,13);
            types.See("Tensho.Late",5 * Mb,7,7,9,11,13);   // a third type on the game's thread: past the bound
            types.See("System.Reflection.RuntimeMethodInfo",500 * Kb,9,7,9,11,13);
            types.See("System.Byte[]",300 * Kb,11,7,9,11,13);
            types.See("System.UInt32",200 * Kb,13,7,9,11,13);
            Check(types.On(AllocationTypes.Where.Game) == 11 * Mb && types.On(AllocationTypes.Where.Listener) == 200 * Kb && types.Ticks == 7,"amounts");
            var summary = types.Summary(TimeSpan.FromMinutes(2));
            Check(summary == "Allocated in the 120 s after the load (allocation ticks, about one per 100 KB): 12 MB in 7 ticks; the game's thread 11 MB (System.String 4.0 MB, Tensho.Row 2.0 MB, other types 5.0 MB); the JIT warm-up's thread 0.5 MB (System.Reflection.RuntimeMethodInfo 0.5 MB); the readers' compiler's thread 0.3 MB (System.Byte[] 0.3 MB); the measure's own listener 0.2 MB (System.UInt32 0.2 MB).",summary);
        }),
        ("Memory measure 1448: the readers' compiler notes its thread; the listener notes its own and gives both",() =>
        {
            var compiler = Read("src/TenshoAssistant/Diagnostics/ReaderCompiler.cs");
            var run = compiler.IndexOf("private void Run()",StringComparison.Ordinal);
            Check(run > 0 && compiler.IndexOf("Interlocked.Exchange(ref ThreadClock.CompilerThread,ThreadClock.SystemId());",run,StringComparison.Ordinal) is var noted && noted > run && noted < compiler.IndexOf("while(true)",run,StringComparison.Ordinal),"at the thread's start");
            var listener = Read("src/TenshoAssistant/Diagnostics/RuntimeSuspensions.cs");
            Check(listener.Contains("if(Interlocked.Read(ref listenerThread)==0)Interlocked.Exchange(ref listenerThread,ThreadClock.SystemId());",StringComparison.Ordinal),"the listener's thread");
            Check(listener.Contains("Interlocked.Read(ref ThreadClock.CompilerThread),Interlocked.Read(ref listenerThread));",StringComparison.Ordinal),"both given to the ticks");
        }),
    ];
}
