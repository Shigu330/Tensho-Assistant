// 0.1632 (X-564): the chat window's first frame with lines timed once for the log, with what the game's thread compiled meanwhile.
internal static class ChatFirstFrame1632Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat first frame 1632: wiring: the first frame with lines timed once, its compiling counted on the game's thread, the lines drawn by one method either way",()=>
        {
            var chat=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var body=chat[chat.IndexOf("private void DrawChatWindowBody()",StringComparison.Ordinal)..chat.IndexOf("private void DrawChatWindowLines()",StringComparison.Ordinal)];
            Check(body.Contains("var jitBefore=System.Runtime.JitInfo.GetCompilationTime(true);",StringComparison.Ordinal)
                && body.Contains("var methodsBefore=System.Runtime.JitInfo.GetCompiledMethodCount(true);",StringComparison.Ordinal),"the game's thread's own compiling");
            Check(body.Contains("try{DrawChatWindowLines();}",StringComparison.Ordinal) && body.Contains("\n        DrawChatWindowLines();\n    }",StringComparison.Ordinal),"the same lines, timed or not");
            Check(body.Contains("Chat window: its first frame with lines ",StringComparison.Ordinal) && body.Contains("Diagnostics.JitWarmup.CompiledText(",StringComparison.Ordinal),"the log line");
            Check(body.IndexOf("chatFirstLinesLogged=true;",StringComparison.Ordinal) < body.IndexOf("try{DrawChatWindowLines();}",StringComparison.Ordinal),"once");
        }),
    ];
}
