using System.Reflection;
using TenshoAssistant.Diagnostics;

// 0.1631 (X-564, measured idle after 0.1630: the chat part ready at 2247 ms, the lines drawn only when the panels' gate opened, at its bound of
// 6 s): the chat window opens as soon as its own code is compiled, with its panel's code in its first frame; the game's messages hidden then.
internal static class ChatEarly1631Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    // Stand-ins named as the plugin's types (the roots are found by the names of the declaring types).
    internal sealed class PanelWindow { public int Draw()=>1; public int PreDraw()=>2; }
    internal sealed class MainWindow
    {
        public int DrawChatWindowContents()=>1; public bool ChatWindowVisible()=>true; public float? ChatFade()=>null; public int ChatFrame()=>3;
        public int DrawChatWindowBody()=>4; public int DrawSettingsPage()=>5; public int DrawChatWindowSettings()=>6;
    }
    private static MethodBase Of(Type type,string name)=>type.GetMethod(name)!;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat early 1631: behaviour: a panel is drawn once the gate is open, or earlier when it says so (the chat window, its code compiled)",()=>
        {
            Check(PanelGate.Lets(true,null) && PanelGate.Lets(true,()=>false),"the gate open: every panel");
            Check(!PanelGate.Lets(false,null) && !PanelGate.Lets(false,()=>false),"closed: the others wait");
            Check(PanelGate.Lets(false,()=>true),"closed, but the chat window's code is ready: it opens");
        }),
        ("Chat early 1631: the first frame's roots: the panel's methods, the chat panel's callbacks, the body; not a page, not the chat's settings",()=>
        {
            Check(JitWarmup.ChatFrameRoot(Of(typeof(PanelWindow),"Draw")) && JitWarmup.ChatFrameRoot(Of(typeof(PanelWindow),"PreDraw")),"the panel");
            foreach(var name in new[]{"DrawChatWindowContents","ChatWindowVisible","ChatFade","ChatFrame","DrawChatWindowBody"})
                Check(JitWarmup.ChatFrameRoot(Of(typeof(MainWindow),name)),name);
            Check(!JitWarmup.ChatFrameRoot(Of(typeof(MainWindow),"DrawSettingsPage")) && !JitWarmup.ChatFrameRoot(Of(typeof(MainWindow),"DrawChatWindowSettings")),"not a page, not the settings");
        }),
        ("Chat early 1631: the chat panel's callbacks named as roots are still declared in the chat window's sources",()=>
        {
            var chat=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            foreach(var root in JitWarmup.ChatPanelRoots)
            {
                var name=root.StartsWith("get_",StringComparison.Ordinal) ? root[4..]:root;
                Check(System.Text.RegularExpressions.Regex.IsMatch(chat,@"private [\w?<>]+ "+name+@"(\(|=>)"),"declared: "+root);
            }
        }),
        ("Chat early 1631: wiring: the chat panel opens early, the game's messages hidden from the same moment, the log says when the lines were first drawn",()=>
        {
            var chat=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(chat.Contains("Once:ChatPlaceOnce,Early:()=>WarmupChatDone);",StringComparison.Ordinal),"the chat panel opens early");
            Check(chat.Contains("internal bool ChatPanelOpen=>Diagnostics.PanelGate.Lets(PanelsOpen,()=>WarmupChatDone);",StringComparison.Ordinal),"one condition for the window and the game's messages");
            Check(chat.Contains("Chat window: lines first drawn {WarmupSinceMade:F0} ms after the load (X-564)",StringComparison.Ordinal),"the log");
            Check(Read("src/TenshoAssistant/PanelWindow.cs").Contains("Func<PanelPlace?>? Once = null, Func<bool>? Early = null);",StringComparison.Ordinal),"the definition's field");
        }),
    ];
}
