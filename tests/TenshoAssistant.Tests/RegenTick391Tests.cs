using TenshoAssistant.Gathering;
using TenshoAssistant.Craft;

// 0.391 (g, X-06): GP tick — a rise while not full is a tick, the bar runs on a 3-second cycle, forgotten after a minute; gatherers only,
// never in combat (DEC-009), off by default.
internal static class RegenTick391Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    private static string ReadLf(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("GP tick: a rise while not full sets the tick, 3-second cycle, full or old ticks not counted; gatherers only, not in combat",() =>
        {
            var start = new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            var tracker = new RegenTickTracker();
            Check(tracker.Observe(500,700,start).Progress is null,"no tick seen yet");
            var after = tracker.Observe(507,700,start.AddSeconds(1));
            Check(after.Progress==0 && after.SecondsLeft==3,"tick at the rise");
            var later = tracker.Observe(507,700,start.AddSeconds(5.5));
            Check(Math.Abs(later.SecondsLeft!.Value-1.5)<1e-9,"cycle of 3 seconds (4.5 s after the tick: 1.5 s left)");
            Check(tracker.Observe(507,700,start.AddMinutes(2)).Progress is null,"forgotten after a minute");
            var full = new RegenTickTracker();
            full.Observe(700,700,start);full.Observe(710,710,start.AddSeconds(1));
            Check(full.Observe(710,710,start.AddSeconds(2)).Progress is null,"a rise from full is not a tick");
            Check(Read("Gathering/RegenTickReader.Dalamud.cs").Contains("local.ClassJob.RowId is not (16 or 17 or 18) || condition[ConditionFlag.InCombat]",StringComparison.Ordinal),"gatherers, not in combat");
            Check(!new TenshoAssistant.Configuration().GpTickBar && TenshoAssistant.ConfigurationReset.Reset.Contains("GpTickBar"),"off by default");
        }),
        ("GP tick: once GP stops rising, no tick is promised; it comes back with the next rise; what a tick gives is seen",() =>
        {
            var at=new DateTimeOffset(2026,11,4,10,0,0,TimeSpan.Zero);
            var tracker=new RegenTickTracker();
            Check(tracker.Observe(100,800,at) is {Stalled:false,Progress:null},"first read: waiting, not stopped");
            var running=tracker.Observe(105,800,at.AddSeconds(3));
            Check(running is {Stalled:false,Gain:0} && running.SecondsLeft is not null,"a first rise: a tick, its size not told yet");
            Check(tracker.Observe(107,800,at.AddSeconds(4)) is {Gain:0},"a rise out of time (in-game report: 2 was kept): not a tick's size");
            Check(tracker.Observe(112,800,at.AddSeconds(7)) is {Gain:5},"a tick after it: 5");
            Check(tracker.Observe(112,800,at.AddSeconds(10)) is {Stalled:false},"within a tick and a half: still running");
            var stopped=tracker.Observe(112,800,at.AddSeconds(12));
            Check(stopped is {Stalled:true,Progress:null,SecondsLeft:null},"in-game report: stopped, at a gathering window");
            Check(tracker.Observe(112,800,at.AddSeconds(100)) is {Stalled:true},"still said after a long time");
            Check(tracker.Observe(115,800,at.AddSeconds(101)) is {Stalled:false,Gain:5},"it rises again; a rise after a pause does not change what a tick gives");
            Check(tracker.Observe(800,800,at.AddSeconds(200)) is {Stalled:false},"full: not stopped");
            Check(ReadLf("MainWindow.RegenTick.cs").Contains("GpTick.Stalled") && ReadLf("MainWindow.RegenTick.cs").Contains("measuredBase!=configuration.GpRegenBase"),"said, and the last measure kept");
            Check(!ReadLf("MainWindow.GatheringPanel.cs").Contains("GpRegenBase=") && !ReadLf("MainWindow.CollectablePanel.cs").Contains("GpRegenBase="),"measured in one place only");
            Check(GpRegen.Traits(16).SequenceEqual([192u,236u,239u]) && GpRegen.Traits(17).SequenceEqual([193u,237u,240u]) && GpRegen.Traits(18).SequenceEqual([194u,238u,241u]) && GpRegen.Traits(8).Count==0,"the traits of the sheet, job by job");
            Check(GpRegen.Bonus([],16)==0 && GpRegen.Bonus([192],16)==1 && GpRegen.Bonus([192,236],16)==2 && GpRegen.Bonus([192,236,239],16)==3 && GpRegen.Bonus([236],16)==2 && GpRegen.Bonus([193],16)==0,"the highest unlocked gives the total; another job's does not count");
            Check(GpRegen.Base(8,3)==5 && GpRegen.Rate(0,5,3)==8 && GpRegen.Rate(0,5,1)==6,"measured on one job, known on another");
            Check(GpRegen.Rate(0,0,3)==0 && GpRegen.Rate(9,5,3)==9,"no measure: unknown, not invented; the setting wins");
        }),
        ("GP tick: the time until full, from what a tick gives; none while unknown or paused",() =>
        {
            RegenTickState At(uint gp,double? left,bool stalled=false)=>new(gp,800,left is null ? null:0.5,left,stalled,5);
            Check(RegenTickTracker.ToFull(At(790,2),5)==TimeSpan.FromSeconds(2+3),"two ticks: this one, then 3 s");
            Check(RegenTickTracker.ToFull(At(793,2),5)==TimeSpan.FromSeconds(2+3),"a part of a tick counts whole");
            Check(RegenTickTracker.ToFull(At(795,1),5)==TimeSpan.FromSeconds(1),"the next tick fills it");
            Check(RegenTickTracker.ToFull(At(800,1),5)==TimeSpan.Zero,"full");
            Check(RegenTickTracker.ToFull(At(700,2),0) is null && RegenTickTracker.ToFull(At(700,null,true),5) is null && RegenTickTracker.ToFull(At(700,null),5) is null,"unknown gain, paused, no tick seen");
            var bar=ReadLf("MainWindow.RegenTick.cs");
            Check(bar.Contains("(float)tick.Gp/tick.MaxGp") && bar.Contains("AddRectFilled") && bar.Contains("GpTick.Full"),"the bar shows GP, the highlight the tick");
        }),
    ];
}
