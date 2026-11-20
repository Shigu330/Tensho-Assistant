using System.Reflection;
using TenshoAssistant.Diagnostics;

// 0.1533 (X-558, second part): the chat's code is compiled in the warm-up's early phase and the chat window waits for it, so a reload's first
// frame of the chat no longer compiles it (dalamud.log of 17/11 13:05:40: « Chat 62 ms », its refresh 5 ms and layout under 1 ms).
internal static class ChatWarmup1533Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class MainWindow
    {
        public void DrawChatWindowBody() { }
        public void LayOutChatLine() { }
        public void DrawSettingsPage() { }
    }
    private sealed class Other { public void DrawChatWindowBody() { } }

    private static MethodBase Of(Type type, string name) => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public)!;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat warm-up 1533: the chat's types and the window's chat methods are early; a page's method and another type's are not", () =>
        {
            Check(JitWarmup.ChatFirst(Of(typeof(MainWindow), "DrawChatWindowBody")) && JitWarmup.ChatFirst(Of(typeof(MainWindow), "LayOutChatLine")), "the window's chat methods");
            Check(JitWarmup.EarlyMethod(typeof(TenshoAssistant.Chat.ChatTabView).GetMethod("Refresh")!) && JitWarmup.EarlyMethod(typeof(TenshoAssistant.Chat.ChatFormat).GetMethod("TabLabel")!), "the chat's types");
            Check(!JitWarmup.ChatFirst(Of(typeof(MainWindow), "DrawSettingsPage")) && !JitWarmup.ChatFirst(Of(typeof(Other), "DrawChatWindowBody")), "not a page, not another type");
            Check(JitWarmup.ChatNamespace == typeof(TenshoAssistant.Chat.ChatTabView).Namespace, "the namespace");
        }),
        ("Chat warm-up 1533: the chat window waits for the early phase", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(code.Contains("if(!WarmupChatDone){EmptyState(\"ChatWindow.Loading\");return;}"), "waits (its own part since 0.1616, X-564)");
        }),
    ];
}
