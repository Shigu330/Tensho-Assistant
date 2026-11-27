using TenshoAssistant.Chat;

// X-467 (0.1502, probe): the game chat's layout line says where the typing box and the message panels are, never a word of a message; the
// probe only reads (no move, hide, focus or send) and stays in developer mode.
internal static class ChatLogLayout1502Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat log layout 1502: the line gives boxes and where they lie",() =>
        {
            var window=new ChatLogBox(20,700,600,300);
            var layout=new ChatLogLayout(true,1,window,new ChatLogBox(60,970,500,24),new ChatLogBox(24,970,34,24),new ChatLogBox(20,700,600,300),new ChatLogBox(24,704,80,22),false,
                [new(0,true,new ChatLogBox(20,726,600,240)),new(1,false,new ChatLogBox(700,100,300,200))]);
            var line=layout.Line();
            Check(line.Contains("window shown 20,700 600x300 scale 1.00",StringComparison.Ordinal),line);
            Check(line.Contains("input 60,970 500x24 inside;",StringComparison.Ordinal),"the input inside the window");
            Check(line.Contains("input below the shown panels True",StringComparison.Ordinal),"the input under the shown panel");
            Check(line.Contains("panel0 shown 20,726 600x240 inside",StringComparison.Ordinal) && line.Contains("panel1 hidden 700,100 300x200 outside",StringComparison.Ordinal),"each panel");
            var unread=new ChatLogLayout(false,1.5f,window,null,null,null,null,true,[]).Line();
            Check(unread.Contains("input unread focus node",StringComparison.Ordinal) && !unread.Contains("focused",StringComparison.Ordinal) && unread.Contains("window hidden",StringComparison.Ordinal),unread);
        }),
        ("Chat log layout 1502: the probe reads only, in developer mode, and logs no text",() =>
        {
            var probe=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatLogLayoutProbe.Dalamud.cs"));
            foreach(var forbidden in new[]{"SetPosition","SetSize","Hide(","Show(","SetFocusNode","Focus()","NodeText","GetText","ProcessChatBox","SendMessage","SetAlpha","->SetVisibility","ToggleVisibility"})
                Check(!probe.Contains(forbidden,StringComparison.Ordinal),$"the probe does not use {forbidden}");
            Check(probe.Contains("!developerMode()",StringComparison.Ordinal) && probe.Contains("CompetitiveGate.Add(framework,OnUpdate)",StringComparison.Ordinal),"developer mode, off in competitive content");
            // X-153 (0.1894): registered by the chat family.
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Chat.cs"));
            Check(plugin.Contains("new Chat.ChatLogLayoutProbe(framework,gameGui,log,()=>configuration.DeveloperMode),()=>chatLayoutProbe?.Dispose()",StringComparison.Ordinal),"registered and disposed");
        }),
    ];
}
