using TenshoAssistant.Diagnostics;

// 0.1625 (X-564, measured idle on 20/11 16:49: the chat part ready 3567 ms after the load, the target under 3 s): the chat part no longer waits
// for the warm-up's first 500 ms; the rest keeps its wait, after it.
// 0.1639 (X-671): no upper bound on a time any more. The warm-up's thread is below normal (X-433): under the load of Verify -Release it waited
// 2.5 to 5 s for the processor before listing anything (« at once: listed 5015, ready 5015 ms », 2 runs in 6 with 32 busy cores), and the
// test asked for 1.5 s. The order is checked, and that the chat part never waits by a first wait no load can reach (5 min).
internal static class ChatFirst1625Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly TimeSpan Long=TimeSpan.FromSeconds(30);
    internal static class Chat1625 { public static int A()=>1; public static int B()=>2; }
    internal static class Map1625 { public static int C()=>3; }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat first 1625: behaviour: as in the plugin (an early phase), the chat part is listed and ready first; the rest keeps its wait, after it",()=>
        {
            using var warmup=JitWarmup.Start(()=>[typeof(Chat1625),typeof(Map1625)],TimeSpan.FromSeconds(2),null,
                early:method=>method.DeclaringType==typeof(Map1625),chatPart:method=>method.DeclaringType==typeof(Chat1625));
            Check(SpinWait.SpinUntil(()=>warmup.ChatDone,Long),"chat told done");
            Check(SpinWait.SpinUntil(()=>warmup.EarlyDone,Long),"then the early phase");
            Check(warmup.ChatListedAfter>=0 && warmup.ChatListedAfter<=warmup.ChatReadyAfter && warmup.ChatReadyAfter<=warmup.EarlyReadyAfter,
                $"in order: listed {warmup.ChatListedAfter:F0}, chat ready {warmup.ChatReadyAfter:F0}, early ready {warmup.EarlyReadyAfter:F0} ms");
            Check(warmup.EarlyReadyAfter>=JitWarmup.EarlyDelay.TotalMilliseconds-50,$"the rest kept its wait: early ready {warmup.EarlyReadyAfter:F0} ms");
        }),
        ("Chat first 1625: behaviour: the chat part never waits for the first wait, however long (5 min here, past any load); the rest waits for it",()=>
        {
            using var warmup=JitWarmup.Start(()=>[typeof(Chat1625),typeof(Map1625)],TimeSpan.FromMinutes(5),null,chatPart:method=>method.DeclaringType==typeof(Chat1625));
            Check(SpinWait.SpinUntil(()=>warmup.ChatReadyAfter>=0,Long),$"the chat part ready within {Long.TotalSeconds:F0} s of a first wait of 5 min: {warmup.ChatReadyAfter:F0}");
            Check(warmup.ChatPrepared>0 && warmup.ChatListedAfter>=0 && warmup.ChatListedAfter<=warmup.ChatReadyAfter,$"listed, then compiled: {warmup.ChatPrepared}");
            Thread.Sleep(200);
            Check(warmup.Prepared==warmup.ChatPrepared && !warmup.Completed,$"the rest still waits: {warmup.Prepared} prepared, {warmup.ChatPrepared} of the chat");
        }),
        ("Chat first 1625: behaviour: without a chat part, nothing changes: the first wait, no listing time",()=>
        {
            using var plain=JitWarmup.Start(()=>[typeof(Chat1625),typeof(Map1625)],TimeSpan.FromSeconds(2),null,early:method=>method.DeclaringType==typeof(Map1625));
            Check(SpinWait.SpinUntil(()=>plain.EarlyDone,Long) && plain.ChatListedAfter<0 && plain.EarlyReadyAfter>=JitWarmup.EarlyDelay.TotalMilliseconds-50,$"early {plain.EarlyReadyAfter:F0} ms");
        }),
    ];
}
