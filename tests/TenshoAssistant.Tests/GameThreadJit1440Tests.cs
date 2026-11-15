using TenshoAssistant.Diagnostics;

// 0.1440 (X-433), a measure: the readers' first passes still compile methods in a frame of the game after their class was compiled, and
// 0.1439 (the game API's direct callees compiled with the class) did not change their counts. Which methods: the runtime's JIT events
// on the game's thread, by area for the two minutes after a load, and by name during each slow pass (developer mode).
[SerialTests("the runtime listener's current tracker (GameThreadJit.Current) is shared by the whole process")]
internal static class GameThreadJit1440Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTime At = new(2026,11,15,17,30,0,DateTimeKind.Utc);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game thread JIT 1440: the area of a type's name: two parts, before any generic mark or nested type",() =>
        {
            Check(GameThreadJit.Area("TenshoAssistant.Inventory.BankInventoryBridge") == "TenshoAssistant.Inventory","Tensho's");
            Check(GameThreadJit.Area("System.Linq.Enumerable") == "System.Linq","System.Linq");
            Check(GameThreadJit.Area("System.Collections.Generic.List`1[[TenshoAssistant.Items.ItemRow, TenshoAssistant]]") == "System.Collections","a generic type");
            Check(GameThreadJit.Area("FFXIVClientStructs.FFXIV.Client.Game.InventoryManager") == "FFXIVClientStructs.FFXIV","ClientStructs");
            Check(GameThreadJit.Area("Plain") == "Plain" && GameThreadJit.Area("A.B") == "A" && GameThreadJit.Area(null) == "?" && GameThreadJit.Area("") == "?","short names");
            Check(GameThreadJit.Area("TenshoAssistant.Plugin+<>c") == "TenshoAssistant","a nested type: its outer type's namespace");
        }),
        ("Game thread JIT 1440: counted by area, the most first; names kept with their time, at most Kept, read by time",() =>
        {
            var jit = new GameThreadJit(3);
            Check(jit.Summary(TimeSpan.FromMinutes(2)) == "Methods compiled on the game's thread in the 120 s after the load: 0; by area: none.","none yet");
            jit.See(At,"System.Linq.Enumerable","Where");
            jit.See(At.AddMilliseconds(10),"TenshoAssistant.Inventory.BankInventoryBridge","OnUpdate");
            jit.See(At.AddMilliseconds(20),"System.Linq.Enumerable","Select");
            jit.See(At.AddMilliseconds(30),"System.Linq.Enumerable","ToList");
            Check(jit.Count == 4,"four counted");
            Check(jit.Summary(TimeSpan.FromMinutes(2)) == "Methods compiled on the game's thread in the 120 s after the load: 4; by area: System.Linq 3, TenshoAssistant.Inventory 1.",jit.Summary(TimeSpan.FromMinutes(2)));
            var between = jit.Between(At.AddMilliseconds(5),At.AddMilliseconds(20));
            Check(between.SequenceEqual(["TenshoAssistant.Inventory.BankInventoryBridge.OnUpdate","System.Linq.Enumerable.Select"]),string.Join(",",between));
            Check(jit.Between(At.AddMilliseconds(25),At.AddSeconds(1)).Count == 0,"past Kept (3): counted, not named");
        }),
        ("Game thread JIT 1440: the listener keeps the game thread's compilations only, is the current tracker while alive, and lets go at its end",() =>
        {
            var listener = new RuntimeSuspensionListener(_ => { });
            try
            {
                Check(ReferenceEquals(GameThreadJit.Current,listener.Jit),"the current tracker");
            }
            finally { RuntimeSuspensionWatch.End(listener); }
            Check(GameThreadJit.Current is null || !ReferenceEquals(GameThreadJit.Current,listener.Jit),"let go at the end");
            var source = Read("src/TenshoAssistant/Diagnostics/RuntimeSuspensions.cs");
            Check(source.Contains("if(game!=0 && written.OSThreadId==game)jit.See(",StringComparison.Ordinal),"the game's thread only");
            Check(source.Contains("TypeDiagnosticKeyword)|(EventKeywords)JitKeyword);",StringComparison.Ordinal),"the JIT keyword listened to");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("log.Information(suspensions.Jit.Summary(Diagnostics.RuntimeSuspensionWatch.Length));",StringComparison.Ordinal),"said with the other summaries");
            Check(plugin.Contains("if(Diagnostics.GameThreadJit.Current is { } jit)",StringComparison.Ordinal) && plugin.Contains("Slow reader: methods the game's thread compiled meanwhile ({0}): {1}.",StringComparison.Ordinal),"named after a slow pass, while listened to");
        }),
    ];
}
