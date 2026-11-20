using System.Reflection;
using TenshoAssistant.Diagnostics;

// 0.1616 (X-564): the chat window showed « Loading… » 8 to 25 s after a load: it waited for the warm-up's whole early phase (map, overlays,
// chat). Its own code is now compiled before anything else when the window is open at the load, with its own signal (ChatDone).
internal static class ChatFirst1616Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);

    internal static class Chat1616 { public static int A() => 1; public static int B() => 2; public static int C() => 3; }
    internal static class Panel1616 { public static int D() => 4; }
    internal static class Map1616 { public static int E() => 5; public static int F() => 6; }
    internal static class Rest1616 { public static int G() => 7; }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat first 1616: behaviour: the chat part is compiled before the panels and the early phase, and told done on its own", () =>
        {
            var held = 1;
            using var warmup = JitWarmup.Start(() => [typeof(Chat1616), typeof(Panel1616), typeof(Map1616), typeof(Rest1616)], TimeSpan.FromSeconds(600), null,
                early: method => method.DeclaringType == typeof(Map1616) || method.DeclaringType == typeof(Chat1616),
                holdRest: () => Volatile.Read(ref held) == 1,
                firstPart: method => method.DeclaringType == typeof(Panel1616),
                chatPart: method => method.DeclaringType == typeof(Chat1616));
            Check(SpinWait.SpinUntil(() => warmup.ChatDone, Long), "the chat part is told done");
            var chat = JitWarmup.Methods([typeof(Chat1616)]).Count;
            Check(warmup.ChatPrepared == chat && chat >= 3 && warmup.ChatReadyAfter >= 0, $"its methods compiled: {warmup.ChatPrepared} of {chat}");
            Check(SpinWait.SpinUntil(() => warmup.EarlyDone, Long), "then the early phase");
            Check(warmup.ChatReadyAfter <= warmup.FirstReadyAfter && warmup.FirstReadyAfter <= warmup.EarlyReadyAfter, $"chat {warmup.ChatReadyAfter:F1}, panels {warmup.FirstReadyAfter:F1}, early {warmup.EarlyReadyAfter:F1} ms");
            var early = JitWarmup.Methods([typeof(Chat1616), typeof(Panel1616), typeof(Map1616)]).Count;
            Check(warmup.EarlyPrepared == early, $"each method compiled once: {warmup.EarlyPrepared} of {early}");
            Volatile.Write(ref held, 0);
        }),
        ("Chat first 1616: behaviour: without a chat part, the chat waits for the early phase; a warm-up stopped leaves nobody waiting", () =>
        {
            using var plain = JitWarmup.Start(() => [typeof(Chat1616), typeof(Map1616)], TimeSpan.Zero, null, early: method => method.DeclaringType == typeof(Map1616));
            Check(plain.Wait(Long) && plain.ChatDone && plain.EarlyDone && plain.ChatPrepared == 0 && plain.ChatReadyAfter < 0, "follows the early phase");
            var stopped = JitWarmup.Start(() => [typeof(Chat1616)], TimeSpan.FromSeconds(600), null, early: _ => true, chatPart: _ => true);
            stopped.Dispose();
            Check(SpinWait.SpinUntil(() => stopped.ChatDone, Long), "stopped: the chat window does not wait forever");
        }),
        ("Chat first 1616: wiring: the chat part when the window is open at the load, the window waits for it, the log says when it was ready", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.cs");
            Check(window.Contains("chatPart: configuration.Chat.WindowOpen ? Diagnostics.JitWarmup.ChatFirst : null,"), "asked when the window is open");
            Check(window.Contains("internal bool WarmupChatDone=>jitWarmup.ChatDone;") && window.Contains("{done.ChatPrepared} methods ready {done.ChatReadyAfter:F0} ms after the load."), "its signal and its log");
            Check(Read("src/TenshoAssistant/MainWindow.ChatWindow.cs").Contains("if(!WarmupChatDone){EmptyState(\"ChatWindow.Loading\");return;}"), "the window waits for the chat part only");
            Check(Read("src/TenshoAssistant/Diagnostics/JitWarmup.cs").Contains("finally{firstPartDone=true;earlyDone=true;chatDone=true;done.Set();}"), "told done when the warm-up ends");
        }),
    ];
}
