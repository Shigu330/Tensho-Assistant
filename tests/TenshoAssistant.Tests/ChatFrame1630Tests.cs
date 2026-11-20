using System.Reflection;
using TenshoAssistant.Diagnostics;

// 0.1630 (X-564, D-196 a): the chat window's first frame compiled first: from its body, the chat part's methods it reaches (settings, menus,
// text selection, game entries, kinds left out: drawn after a click), then what those call elsewhere, one level; the window told ready then,
// the rest of the chat part right after; one thread.
internal static class ChatFrame1630Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly TimeSpan Long=TimeSpan.FromSeconds(30);

    internal static class Frame1630
    {
        public static int DrawBody()=>Line()+Tab();
        public static int Line()=>Format()+Helper1630.Help();
        public static int Tab()=>DrawSettings()+OpenMenu();
        public static int Format()=>1;
        public static int DrawSettings()=>Deep();
        public static int OpenMenu()=>2;
        public static int Deep()=>3;
        public static int Unused()=>4;
    }
    internal static class Helper1630 { public static int Help()=>Far(); public static int Far()=>5; }

    private static bool InFrame(MethodBase method)=>method.DeclaringType==typeof(Frame1630);
    private static MethodBase Of(string name)=>typeof(Frame1630).GetMethod(name)!;
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat frame 1630: the first frame: from the body, the chat methods it reaches, breadth first, none drawn after a click; then what they call elsewhere, one level",()=>
        {
            var frame=JitWarmup.ChatFirstFrame([Of("DrawBody")],InFrame,typeof(Frame1630).Assembly);
            var names=frame.Select(method=>method.DeclaringType!.Name+"."+method.Name).ToArray();
            // 0.1634 (X-564): then the chat methods holding a « later » word themselves (called at each frame), not what they call; and a second
            // level outside the chat part (Helper1630.Far, called by Help).
            Check(names.SequenceEqual(["Frame1630.DrawBody","Frame1630.Line","Frame1630.Tab","Frame1630.Format","Helper1630.Help","Helper1630.Far","Frame1630.DrawSettings","Frame1630.OpenMenu"]),string.Join(", ",names));
            Check(JitWarmup.ChatFirstFrame([Of("DrawBody")],InFrame,typeof(Frame1630).Assembly,()=>true).Count==0,"stopped: nothing");
        }),
        ("Chat frame 1630: behaviour: the window is told ready once its first frame is compiled; the rest of the chat part then, each method once",()=>
        {
            using var warmup=JitWarmup.Start(()=>[typeof(Frame1630),typeof(Helper1630)],TimeSpan.Zero,null,early:_=>true,
                chatPart:InFrame,chatFrame:method=>method.Name=="DrawBody" && InFrame(method));
            Check(SpinWait.SpinUntil(()=>warmup.ChatDone,Long),"told ready");
            Check(warmup.ChatPrepared==8 && warmup.ChatReadyAfter>=0,"the first frame only: "+warmup.ChatPrepared);
            Check(warmup.Wait(Long) && warmup.Completed,"then the rest");
            var all=JitWarmup.Methods([typeof(Frame1630),typeof(Helper1630)]).Count;
            Check(warmup.Prepared==all && all==10,$"each method once: {warmup.Prepared} of {all}");
        }),
        ("Chat frame 1630: the roots and the words drawn after a click still name methods of the chat window; the plugin asks for the first frame",()=>
        {
            var chat=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            foreach(var root in JitWarmup.ChatFrameRoots)Check(chat.Contains("private void "+root+"()",StringComparison.Ordinal),"root "+root);
            foreach(var word in JitWarmup.ChatLaterWords)
                Check(System.Text.RegularExpressions.Regex.IsMatch(chat,@"(void|string|bool) \w*Chat\w*"+word+@"\w*\("),"a chat method holds "+word);
            var window=Read("src/TenshoAssistant/MainWindow.cs");
            Check(window.Contains("chatFrame: Diagnostics.JitWarmup.ChatFrameRoot,",StringComparison.Ordinal),"asked");
            Check(JitWarmup.ChatFrameCap>=1000,"bound");
        }),
    ];
}
