using TenshoAssistant.Chat;

// 0.1622 (X-447 B1 and B2, the user's choices of 15/11): the window fades out after a delay without message and comes back; the lines spaced;
// a tab's own text size.
internal static class ChatIdle1622Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat idle 1622: B1, as it is until the delay, then down to nothing; never when off or kept",()=>
        {
            Check(ChatLook.Idle(null,0,9999,false) is null && ChatLook.Idle(0.6f,0,9999,false)==0.6f,"off: as it is");
            Check(ChatLook.Idle(null,30,29.9,false) is null && ChatLook.Idle(0.6f,30,30,false)==0.6f,"before the delay");
            var half=ChatLook.Idle(null,30,30+ChatLook.IdleFadeSeconds/2,false);
            Check(half is > 0.49f and < 0.51f,"half way: "+half);
            Check(ChatLook.Idle(0.6f,30,30+ChatLook.IdleFadeSeconds/2,false) is > 0.29f and < 0.31f,"the opacity chosen goes down too");
            Check(ChatLook.Idle(null,30,1000,false)==0f && ChatLook.Idle(null,30,double.NaN,false) is null,"faded out; no time: as it is");
            Check(ChatLook.Idle(null,30,1000,true) is null,"the mouse on it or a menu open: back");
        }),
        ("Chat idle 1622: B1 and B2, bounded settings, off by default",()=>
        {
            var chat=new ChatSettings();
            Check(chat.BoundedFadeAfter==0 && chat.BoundedLineSpacing==0,"defaults");
            var wild=new ChatSettings{FadeAfter=-3,LineSpacing=99};
            Check(wild.BoundedFadeAfter==0 && wild.BoundedLineSpacing==12 && new ChatSettings{FadeAfter=9999}.BoundedFadeAfter==600,"bounded");
        }),
        ("Chat idle 1622: B2, a tab's own size, else the window's, bounded",()=>
        {
            var window=new ChatSettings{FontScale=130};
            Check(new ChatTabSettings().ScaleIn(window)==130 && new ChatTabSettings{FontScale=80}.ScaleIn(window)==80 && new ChatTabSettings{FontScale=500}.ScaleIn(window)==200,"own size");
        }),
        ("Chat idle 1622: wiring: the fade, the clicks, the newest message, the size and spacing, the settings",()=>
        {
            var chat=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n","\n");
            Check(chat.Contains("var alpha=ChatLook.Idle(ChatBaseFade(hovered),ChatOptions.BoundedFadeAfter,(Environment.TickCount64-chatLastMessageAt)/1000d,hovered || chatMenuOpen);"),"the fade");
            Check(chat.Contains("|(chatIdleOut ? ImGuiWindowFlags.NoInputs:ImGuiWindowFlags.None)") && chat.Contains("chatShownNewest=view.Count>0 ? view.RecordAt(view.Count-1).Id:-1;"),"the clicks and the newest message");
            Check(chat.Contains("ImGui.SetWindowFontScale(scale/100f);") && chat.Contains("var height=ImGui.GetTextLineHeightWithSpacing()+options.BoundedLineSpacing;") && chat.Contains("maps.State.Catalog is not null))+\"|\"+scale;"),"size and spacing");
            Check(chat.Contains("SliderSetting(\"ChatWindow.LineSpacing\",") && chat.Contains("SliderSetting(\"ChatWindow.FadeAfter\",") && chat.Contains("###ChatTabFontOwn"),"settings");
        }),
    ];
}
