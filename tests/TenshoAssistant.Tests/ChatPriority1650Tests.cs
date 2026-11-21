using TenshoAssistant.Diagnostics;

// 0.1650 (X-564): the chat part of the JIT warm-up (the chat window's first frame) is compiled at normal priority; the rest of the warm-up
// goes back below normal (X-433).
internal static class ChatPriority1650Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly TimeSpan Long=TimeSpan.FromMinutes(2);
    internal static class Chat1650 { public static int A()=>1; public static int B()=>2; }
    internal static class Rest1650 { public static int C()=>3; }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat priority 1650: behaviour: the chat part at normal priority, the rest of the warm-up below normal again; without a chat part, below normal throughout",()=>
        {
            ThreadPriority? listed=null,during=null,after=null;
            using var warmup=JitWarmup.Start(()=>{listed=Thread.CurrentThread.Priority;return [typeof(Chat1650),typeof(Rest1650)];},TimeSpan.Zero,_=>after=Thread.CurrentThread.Priority,
                early:_=>true,chatPart:method=>method.DeclaringType==typeof(Chat1650),chatThen:()=>during=Thread.CurrentThread.Priority);
            Check(warmup.Wait(Long) && warmup.ChatDone,"finished");
            Check(listed==ThreadPriority.BelowNormal && during==ThreadPriority.Normal && after==ThreadPriority.BelowNormal,$"listed {listed}, chat part {during}, after {after}");
            ThreadPriority? plain=null;
            using var none=JitWarmup.Start(()=>[typeof(Rest1650)],TimeSpan.Zero,_=>plain=Thread.CurrentThread.Priority,early:_=>true);
            Check(none.Wait(Long) && plain==ThreadPriority.BelowNormal,"no chat part: "+plain);
        }),
        ("Chat priority 1650: wiring: back below normal in a finally (a cancelled or failed chat part too); the evidence keeps the measures",()=>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/JitWarmup.cs"));
            var raised=code.IndexOf("Thread.CurrentThread.Priority=ThreadPriority.Normal;",StringComparison.Ordinal);
            var back=code.IndexOf("finally{Thread.CurrentThread.Priority=ThreadPriority.BelowNormal;}",StringComparison.Ordinal);
            Check(raised>0 && back>raised && code.IndexOf("chatDone=true;",raised,StringComparison.Ordinal) is var ready and >0 && ready<back,"raised, then back after the chat part");
            var evidence=File.ReadAllText(Path.GetFullPath("docs/evidence/0.1650-chat-part-priority.md"));
            Check(evidence.Contains("732 methods ready",StringComparison.Ordinal) && evidence.Contains("704 ms, of which 109 ms on the processor",StringComparison.Ordinal),"the measures");
        }),
    ];
}
