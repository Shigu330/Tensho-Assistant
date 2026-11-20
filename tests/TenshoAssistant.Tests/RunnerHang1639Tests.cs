using System.Diagnostics;

// 0.1639 (X-671): a run under load stopped for good (every thread of the test process waiting, no processor; Verify once so in 0.1637): the
// runner names a test still running past its limit (5 min), says the tests after it in its class not run, and ends.
internal static class RunnerHang1639Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static DateOnly Today=>DateOnly.FromDateTime(DateTime.Now);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Runner hang 1639: behaviour: a test that never ends is said hung by its name, the tests after it in its class not run, the other classes and the serial ones run, and the run ends",()=>
        {
            using var gate=new ManualResetEventSlim();
            var after=0;
            var groups=new TestGroup[]
            {
                new("Stuck",[("waits",()=>gate.Wait()),("after",()=>Interlocked.Increment(ref after))],false),
                new("Fine",[("one",()=>{}),("two",()=>{})],false),
                new("Alone",[("serial",()=>{})],true),
            };
            var clock=Stopwatch.StartNew();
            TestRun run;
            try{run=ParallelTests.Run(groups,2,[],Today,hangAfter:TimeSpan.FromMilliseconds(500));}
            finally{clock.Stop();}
            try
            {
                var stuck=run.Results.Single(result=>result.Name=="waits");
                Check(!stuck.Passed && stuck.Error!.StartsWith("hung: still running after",StringComparison.Ordinal) && stuck.Error.Contains("« Stuck »",StringComparison.Ordinal),"hung: "+stuck.Error);
                var next=run.Results.Single(result=>result.Name=="after");
                Check(!next.Passed && next.Error=="not run: « waits » hung before it (X-671)","not run: "+next.Error);
                Check(run.Results.Length==5 && run.Results.Where(result=>result.Group is "Fine" or "Alone").All(result=>result.Passed),"the others ran");
                Check(run.Hung && run.Failed==2 && ParallelTests.CountLine(run)=="3/5 passed","said: "+ParallelTests.CountLine(run));
                Check(clock.Elapsed<TimeSpan.FromSeconds(60),"the run ended: "+clock.Elapsed);
            }
            finally{gate.Set();}
            Thread.Sleep(300);
            Check(Volatile.Read(ref after)==0,"the thread left behind, back, runs nothing more of its class");
        }),
        ("Runner hang 1639: behaviour: a test slower than nothing but under the limit is not hung; the serial classes run on their own thread",()=>
        {
            var serialThread=-1;
            var groups=new TestGroup[]
            {
                new("Slow",[("sleeps",()=>Thread.Sleep(150))],false),
                new("Alone",[("serial",()=>serialThread=Environment.CurrentManagedThreadId)],true),
            };
            var run=ParallelTests.Run(groups,2,[],Today,hangAfter:TimeSpan.FromSeconds(30));
            Check(!run.Hung && run.Results.All(result=>result.Passed) && run.Failed==0,"all passed");
            Check(serialThread>0 && serialThread!=Environment.CurrentManagedThreadId,"alone, on a thread of its own");
            Check(ParallelTests.HangAfter==TimeSpan.FromMinutes(5),"5 min in a real run");
        }),
        ("Runner hang 1639: wiring: Program.cs ends the process when a test was left behind",()=>
        {
            var program=File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/Program.cs"));
            Check(program.Contains("if (run.Hung) { Console.Out.Flush(); Console.Error.Flush(); Environment.Exit(1); }",StringComparison.Ordinal),"ended");
        }),
    ];
}
