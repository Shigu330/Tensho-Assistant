using TenshoAssistant.Diagnostics;

// 0.1625 (X-564, measured idle on 20/11 16:49: the chat part ready 3567 ms after the load, the target under 3 s): the chat part no longer waits
// for the warm-up's first 500 ms; the rest keeps its wait, after it.
internal static class ChatFirst1625Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly TimeSpan Long=TimeSpan.FromSeconds(30);
    internal static class Chat1625 { public static int A()=>1; public static int B()=>2; }
    internal static class Map1625 { public static int C()=>3; }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat first 1625: behaviour: the chat part is ready before the warm-up's first wait is over; the rest waits for it",()=>
        {
            // A first wait of 2 s (as the 500 ms of the plugin, longer so a slow test machine still tells them apart).
            using var warmup=JitWarmup.Start(()=>[typeof(Chat1625),typeof(Map1625)],TimeSpan.FromSeconds(2),null,
                early:method=>method.DeclaringType==typeof(Map1625),chatPart:method=>method.DeclaringType==typeof(Chat1625));
            Check(SpinWait.SpinUntil(()=>warmup.ChatDone,Long),"chat told done");
            Check(warmup.ChatReadyAfter>=0 && warmup.ChatReadyAfter<1500 && warmup.ChatListedAfter>=0 && warmup.ChatListedAfter<=warmup.ChatReadyAfter,$"at once: listed {warmup.ChatListedAfter:F0}, ready {warmup.ChatReadyAfter:F0} ms");
            Check(SpinWait.SpinUntil(()=>warmup.EarlyDone,Long),"then the early phase");
            Check(warmup.EarlyReadyAfter>=JitWarmup.EarlyDelay.TotalMilliseconds-50,$"the rest kept its wait: early ready {warmup.EarlyReadyAfter:F0} ms");
        }),
        ("Chat first 1625: behaviour: without a chat part, nothing changes: the first wait, no listing time",()=>
        {
            using var plain=JitWarmup.Start(()=>[typeof(Chat1625),typeof(Map1625)],TimeSpan.FromSeconds(2),null,early:method=>method.DeclaringType==typeof(Map1625));
            Check(SpinWait.SpinUntil(()=>plain.EarlyDone,Long) && plain.ChatListedAfter<0 && plain.EarlyReadyAfter>=JitWarmup.EarlyDelay.TotalMilliseconds-50,$"early {plain.EarlyReadyAfter:F0} ms");
        }),
    ];
}
