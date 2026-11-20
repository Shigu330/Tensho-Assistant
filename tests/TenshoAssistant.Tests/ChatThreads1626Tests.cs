using TenshoAssistant.Diagnostics;

// 0.1626 (X-564): the chat part of the warm-up compiled on a few threads of low priority: each method once, all of them, the count right.
// 0.1630 (X-564, D-196 a): no gain measured from the threads (3155 ms on one, 3560 ms on three, idle): back to one thread; the count stays right.
internal static class ChatThreads1626Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly TimeSpan Long=TimeSpan.FromSeconds(30);
    internal static class Chat1626
    {
        public static int A()=>1; public static int B()=>2; public static int C()=>3; public static int D()=>4; public static int E()=>5;
        public static int F()=>6; public static int G()=>7; public static int H()=>8; public static string I(int x)=>x.ToString(); public static long J(long y)=>y*2;
    }
    internal static class Map1626 { public static int K()=>11; }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat threads 1630: one thread again (D-196 a): no helper thread, no thread count left",()=>
        {
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/JitWarmup.cs"));
            Check(!source.Contains("Tensho JIT warm-up (chat",StringComparison.Ordinal) && !source.Contains("ChatThreads",StringComparison.Ordinal),"no helper threads");
            Check(typeof(JitWarmup).GetProperty("ChatThreads") is null && typeof(JitWarmup).GetMethod("Threads") is null,"no thread count");
        }),
        ("Chat threads 1626: behaviour: every method of the chat part compiled once, counted once, then the rest",()=>
        {
            using var warmup=JitWarmup.Start(()=>[typeof(Chat1626),typeof(Map1626)],TimeSpan.Zero,null,
                early:method=>method.DeclaringType==typeof(Map1626) || method.DeclaringType==typeof(Chat1626),chatPart:method=>method.DeclaringType==typeof(Chat1626));
            Check(SpinWait.SpinUntil(()=>warmup.EarlyDone,Long),"done");
            var chat=JitWarmup.Methods([typeof(Chat1626)]).Count;
            Check(warmup.ChatPrepared==chat && chat>=10,$"chat: {warmup.ChatPrepared} of {chat}");
            Check(warmup.EarlyPrepared==JitWarmup.Methods([typeof(Chat1626),typeof(Map1626)]).Count,$"early part once each: {warmup.EarlyPrepared}");
        }),
    ];
}
