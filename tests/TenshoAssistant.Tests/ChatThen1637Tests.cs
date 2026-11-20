using TenshoAssistant.Diagnostics;

// 0.1637 (X-564): what the chat window reads before its first frame (the game's log filters) is done on the warm-up's thread after the chat's
// code and before the window is told ready; a failure there never stops the warm-up.
internal static class ChatThen1637Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly TimeSpan Long=TimeSpan.FromSeconds(30);
    internal static class Chat1637 { public static int A()=>1; public static int B()=>2; }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat then 1637: behaviour: done once, on the warm-up's thread, after the chat's code, before the window is told ready",()=>
        {
            var calls=0;var thread=-1;var doneBefore=true;var preparedThen=-1;
            JitWarmup? seen=null;
            using var warmup=JitWarmup.Start(()=>[typeof(Chat1637)],TimeSpan.FromSeconds(600),null,early:_=>true,chatPart:method=>method.DeclaringType==typeof(Chat1637),
                chatThen:()=>{calls++;thread=Environment.CurrentManagedThreadId;doneBefore=Volatile.Read(ref seen)?.ChatDone ?? false;preparedThen=Volatile.Read(ref seen)?.ChatPrepared ?? -1;});
            Volatile.Write(ref seen,warmup);
            Check(SpinWait.SpinUntil(()=>warmup.ChatDone,Long),"told ready");
            Check(calls==1 && thread!=Environment.CurrentManagedThreadId,"once, on the warm-up's thread: "+calls);
            Check(!doneBefore,"before the window is told ready");
        }),
        ("Chat then 1637: behaviour: a failure there never stops the warm-up; without a chat part it is never called",()=>
        {
            using var failing=JitWarmup.Start(()=>[typeof(Chat1637)],TimeSpan.Zero,null,early:_=>true,chatPart:method=>method.DeclaringType==typeof(Chat1637),
                chatThen:()=>throw new InvalidOperationException("read failed"));
            Check(failing.Wait(Long) && failing.ChatDone && failing.Completed,"the warm-up goes on");
            var called=false;
            using var plain=JitWarmup.Start(()=>[typeof(Chat1637)],TimeSpan.Zero,null,early:_=>true,chatThen:()=>called=true);
            Check(plain.Wait(Long) && !called,"no chat part: not called");
        }),
    ];
}
