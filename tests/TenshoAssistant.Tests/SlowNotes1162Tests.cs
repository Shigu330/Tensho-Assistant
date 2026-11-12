using TenshoAssistant.Diagnostics;

// 0.1162 (X-177, master's finding of 12/11): the hitches at zone changes are named in the log again and again (a reader at most every 5
// minutes, with its worst time; before, once per load), and the settings saves made on the framework thread are timed (AutoAlarmWatcher took
// 648 ms in one frame on 12/11 at 11:15: its work is a save of the whole settings). Measuring first; the fix follows from what the log says.
internal static class SlowNotes1162Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Slow notes 1162: over the threshold, at once then at most every period, with the worst time meanwhile",() =>
        {
            var notes=new SlowNotes(TimeSpan.FromMinutes(5));
            Check(notes.Note("A",50,0) is null && notes.Note("A",12,0) is null,"at or under 50 ms: nothing");
            Check(notes.Note("A",80,1000)==80,"the first time: at once");
            Check(notes.Note("A",200,2000) is null && notes.Note("A",90,60_000) is null,"within the period: kept for later");
            Check(notes.Note("A",70,301_001)==200,"after the period: written with the worst time meanwhile");
            Check(notes.Note("A",60,302_000) is null,"a new period starts");
            Check(notes.Note("A",55,601_002)==60,"the worst of the new period only");
            Check(notes.Note("B",51,1000)==51,"each name on its own");
            var many=new SlowNotes(TimeSpan.FromMinutes(5));
            for(var n=0;n<300;n++)many.Note("R"+n,60,0);
            Check(many.Note("R5",60,1)==60,"the memory is bounded (it started again past 256 names)");
            var shared=new SlowNotes(TimeSpan.FromMinutes(5));
            var written=0;
            Parallel.For(0,64,_=>{if(shared.Note("same",100,0) is not null)Interlocked.Increment(ref written);});
            Check(written==1,"many threads at once: written once ("+written+")");
        }),
        ("Slow notes 1162: the readers and the five settings saves of the framework thread go through them",() =>
        {
            var gate=Read("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs");
            Check(gate.Contains("private static readonly Diagnostics.SlowNotes slowNotes=new(TimeSpan.FromMinutes(5));") && !gate.Contains("slowNamed"),"readers: every 5 minutes, not once");
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("Slow reader: {0} took {1:0} ms in one frame (worst {2:0} ms; named at most every 5 minutes).")
                && plugin.Contains("Slow settings save: {0} took {1:0} ms (worst {2:0} ms; named at most every 5 minutes)."),"the log lines");
            foreach(var what in new[]{"window","alarms","voyages","character status","automatic alarms"})
                Check(plugin.Contains($"SaveTimed(\"{what}\")"),"timed: "+what);
            Check(!plugin.Contains("()=>pluginInterface.SavePluginConfig(configuration),") && !plugin.Contains("() => pluginInterface.SavePluginConfig(configuration),"),"no untimed save handed to a reader");
        }),
    ];
}
