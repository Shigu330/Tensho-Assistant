using TenshoAssistant.Chat;

// 0.1624 (X-455 E1, the user's choice of 15/11): the tab's messages as text to select with the mouse and copy.
internal static class ChatSelect1624Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,17,0,0,TimeSpan.Zero);
    private static string Window()=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat select 1624: the text given to the box is the tab's messages, one per line, as copied (capture mode kept)",()=>
        {
            ChatRecord[] tab=[new(1,At,10,ChatChannel.Say,"Ann Doe",[new ChatSegment("one")]),new(2,At.AddSeconds(1),10,ChatChannel.Say,"Bob Roe",[new ChatSegment("two")])];
            var settings=new ChatSettings{ShowTimestamps=false,ShowChannel=false};
            var text=ChatFormat.Plain(tab,settings,_=>"");
            Check(text.Contains("Ann Doe: one",StringComparison.Ordinal) && text.Contains("Bob Roe: two",StringComparison.Ordinal) && text.IndexOf("one",StringComparison.Ordinal)<text.IndexOf("two",StringComparison.Ordinal) && text.Contains('\n'),text);
            Check(!ChatFormat.Plain(tab,new ChatSettings{ShowTimestamps=false,ShowChannel=false,CaptureNames=true},_=>"").Contains("Ann",StringComparison.Ordinal),"capture mode");
        }),
        ("Chat select 1624: wiring: the menu makes the text once, a read-only box shows it, closing forgets it",()=>
        {
            var chat=Window();
            Check(chat.Contains("if(ImGui.Selectable(localization.Get(\"ChatWindow.SelectText\")))\n                            chatSelectText=ChatFormat.Plain(Enumerable.Range(0,view.Count).Select(view.RecordAt),options,ChatChannelShort,ChatLinkName);"),"made once from the menu");
            Check(chat.Contains("ImGui.InputTextMultiline(\"###ChatSelectText\",ref text,System.Text.Encoding.UTF8.GetByteCount(text)+1,new Vector2(-1,-1),ImGuiInputTextFlags.ReadOnly);"),"read-only box");
            Check(chat.Contains("if(!ImGui.BeginPopup(\"###TenshoChatSelect\")){chatSelectText=null;chatSelectOpen=false;return;}") && chat.Contains("DrawChatSelectText();"),"closed: forgotten");
        }),
    ];
}
