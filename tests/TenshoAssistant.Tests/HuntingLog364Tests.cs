using TenshoAssistant.Hunting;

// 0.364 (g, C-12): hunting log — row layout, memory slots, kills of earlier, current and later ranks.
internal static class HuntingLog364Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hunting log: rows split into log, rank and number; passed ranks complete, later ranks zero, current rank from memory",() =>
        {
            var entry = new HuntEntry(10023,"Gladiateur 23",[new HuntTarget("bombo",3,"Thanalan"),new HuntTarget("coblyn",2,"Thanalan")]);
            Check(entry.LogKey == 1 && entry.Rank == 2 && entry.Number == 2 && new HuntEntry(1000001,"GC",[]).LogKey == 100,"row layout");
            Check(HuntingLog.MemorySlot(1) == 0 && HuntingLog.MemorySlot(26) == 7 && HuntingLog.MemorySlot(100) == 8 && HuntingLog.MemorySlot(300) == 10
                && HuntingLog.MemorySlot(29) == 11 && HuntingLog.MemorySlot(8) is null,"memory slots");
            IReadOnlyList<IReadOnlyList<byte>> counts = Enumerable.Range(0,10).Select(i => (IReadOnlyList<byte>)(i == 2 ? new byte[]{3,1,0,0} : new byte[4])).ToArray();
            Check(HuntingLog.Killed(entry,0,new HuntSlot(3,counts)) == 3 && HuntingLog.Done(entry,new HuntSlot(3,counts)) == true,"passed rank: done");
            Check(HuntingLog.Killed(entry,1,new HuntSlot(1,counts)) == 0 && HuntingLog.Done(entry,new HuntSlot(1,counts)) == false,"later rank: zero");
            Check(HuntingLog.Killed(entry,0,new HuntSlot(2,counts)) == 3 && HuntingLog.Killed(entry,1,new HuntSlot(2,counts)) == 1
                && HuntingLog.Done(entry,new HuntSlot(2,counts)) == false && HuntingLog.Done(entry,null) is null,"current rank from memory; not read = unknown");
            Check(Read("Hunting/HuntingLogReader.Dalamud.cs").Contains("MonsterNoteManager.Instance()",StringComparison.Ordinal)
                && Read("Plugin.cs").Contains("()=>huntReader?.Dispose()",StringComparison.Ordinal),"reader guarded and disposed");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.364-hunting-log.md")),"evidence");
        }),
    ];
}
