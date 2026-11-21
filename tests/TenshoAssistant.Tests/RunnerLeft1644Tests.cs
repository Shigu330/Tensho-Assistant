using System.Collections.Concurrent;

// 0.1644 (X-673 M2, review of 0.1643): a test thread left behind on a hung test (X-671), once its test comes back while the run still goes
// on, begins nothing more: neither the next tests of its class nor a class still in the queue (they would run unwatched, maybe beside the
// serial classes, and be read half way).
internal static class RunnerLeft1644Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static DateOnly Today=>DateOnly.FromDateTime(DateTime.Now);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Runner left 1644: behaviour: the thread left behind, back before the run ends, begins nothing more; what it did not take is said not run",()=>
        {
            using var release=new ManualResetEventSlim();
            using var back=new ManualResetEventSlim();
            var ran=new ConcurrentDictionary<string,int>();
            Thread? stuck=null;
            var groups=new TestGroup[]
            {
                new("Stuck",[("waits",()=>{stuck=Thread.CurrentThread;release.Wait();back.Set();}),("next",()=>ran["next"]=1),("third",()=>ran["third"]=1)],false),
                new("Queued",[("late",()=>ran["late"]=1)],false),
            };
            var ended=false;
            // The signal comes from inside the watch: the run is not over yet (as when another thread is still at work), the stuck test
            // is let go, and the thread left behind is waited for until it ends (never a delay: X-672).
            var run=ParallelTests.Run(groups,1,[],Today,hangAfter:TimeSpan.FromMilliseconds(300),onLeft:()=>
            {
                release.Set();
                ended=back.Wait(TimeSpan.FromMinutes(2)) && stuck!.Join(TimeSpan.FromMinutes(2));
            });
            Check(ended,"the thread left behind came back and ended");
            Check(ran.IsEmpty,"nothing more begun by it: "+string.Join(", ",ran.Keys));
            Check(run.Hung && run.Results.Single(result=>result.Name=="waits").Error!.StartsWith("hung: ",StringComparison.Ordinal),"hung");
            Check(run.Results.Single(result=>result.Name=="next").Error=="not run: « waits » hung before it (X-671)","the next test of its class");
            var late=run.Results.Single(result=>result.Name=="late");
            Check(!late.Passed && late.Error=="not run: every thread was left behind on a hung test (X-671)","the class left in the queue: "+late.Error);
        }),
        ("Runner left 1644: behaviour: without a hung test no thread is left, the signal never called",()=>
        {
            var calls=0;
            var run=ParallelTests.Run([new TestGroup("Fine",[("one",()=>{}),("two",()=>{})],false),new TestGroup("Alone",[("serial",()=>{})],true)],2,[],Today,
                hangAfter:TimeSpan.FromMinutes(5),onLeft:()=>Interlocked.Increment(ref calls));
            Check(!run.Hung && run.Failed==0 && run.Passed==3 && calls==0,"all ran, no signal");
        }),
    ];
}
