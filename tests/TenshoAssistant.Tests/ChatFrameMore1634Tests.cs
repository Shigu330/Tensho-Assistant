using System.Reflection;
using TenshoAssistant.Diagnostics;

// 0.1634 (X-564, named by 0.1633 on 20/11 18:00): the chat's first frame still compiled the log filters' reading, a lambda cache's static
// constructor, a helper two levels out and the settings popup called at each frame. The first frame now takes them; the filters are read off
// the game's thread when the window is open at the load.
internal static class ChatFrameMore1634Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static int sink=Environment.ProcessorCount;
    internal static class Chat1634
    {
        public static int Draw()=>Bar()+DrawPopupSettings();
        public static int Bar()=>Glyph1634.Button()+new[]{1,2}.Select(value=>value+sink).Sum();
        public static int DrawPopupSettings()=>Glyph1634.Deep();
    }
    internal static class Glyph1634
    {
        public static int Button()=>At();
        public static int At()=>3;
        public static int OpenMenu()=>Never();
        public static int Never()=>4;
        public static int Deep()=>5;
    }
    private static bool InChat(MethodBase method)=>method.DeclaringType==typeof(Chat1634);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat frame more 1634: the settings popup itself (not what it calls), a helper two levels out, a lambda cache's static constructor",()=>
        {
            var frame=JitWarmup.ChatFirstFrame([typeof(Chat1634).GetMethod("Draw")!],InChat,typeof(Chat1634).Assembly);
            string Name(MethodBase method)=>method.DeclaringType!.Name+"."+method.Name;
            var names=frame.Select(Name).ToArray();
            Check(names.Contains("Chat1634.DrawPopupSettings") && !names.Contains("Glyph1634.Deep"),"the popup itself, not its callees: "+string.Join(", ",names));
            // 0.1635 (X-564): one level out only (the second took the chat's first part to 1060 methods); a helper further out is a root.
            Check(names.Contains("Glyph1634.Button") && !names.Contains("Glyph1634.At"),"one level out: "+string.Join(", ",names));
            Check(frame.Any(method=>method is ConstructorInfo { IsStatic:true } && method.DeclaringType!.DeclaringType==typeof(Chat1634)),"the lambdas' cache's static constructor: "+string.Join(", ",names));
            Check(!names.Contains("Glyph1634.Never") && !names.Contains("Glyph1634.OpenMenu"),"not what nothing calls");
        }),
        ("Chat frame more 1634: wiring: the log filters read off the game's thread when the window is open at the load, once, under a lock",()=>
        {
            var chat=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(chat.Contains("set{chatFilterSource=value;if(value is not null && ChatOptions.WindowOpen && !inDesktopApp)_=Task.Run(()=>ChatFilterTableNow());}",StringComparison.Ordinal),"read at once, off the game's thread");
            Check(chat.Contains("if(Volatile.Read(ref chatFilterTried))return chatFilterTable;",StringComparison.Ordinal) && chat.Contains("lock(chatFilterGate)",StringComparison.Ordinal)
                && chat.Contains("Volatile.Write(ref chatFilterTried,true);",StringComparison.Ordinal),"once, under a lock");
        }),
    ];
}
