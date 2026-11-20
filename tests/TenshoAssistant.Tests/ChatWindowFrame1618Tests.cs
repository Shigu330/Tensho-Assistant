using TenshoAssistant.Chat;

// 0.1618 (X-447 D4 to D7, the user's choices of 15/11): the chat window's tabs' names hidden (the buttons stay, one shows them again), its
// margins, corners and frame thickness, its opacity away from the mouse, and its place typed to the pixel or brought back to the centre.
internal static class ChatWindowFrame1618Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath(Path.Combine("src","TenshoAssistant",name))).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat window frame 1618: the defaults keep the window as before, the values are bounded",()=>
        {
            var chat=new ChatSettings();
            Check(!chat.HideTabs && chat.BoundedMarginX==(int)ChatLook.Padding.X && chat.BoundedMarginY==(int)ChatLook.Padding.Y && chat.BoundedRounding==2 && chat.BoundedBorder==1 && chat.BoundedTextOpacity==100,"defaults");
            var wild=new ChatSettings{MarginX=-5,MarginY=99,Rounding=40,Border=-1,TextOpacity=3};
            Check(wild.BoundedMarginX==0 && wild.BoundedMarginY==24 && wild.BoundedRounding==16 && wild.BoundedBorder==0 && wild.BoundedTextOpacity==20,"bounded");
        }),
        ("Chat window frame 1618: D6, the opacity away from the mouse, full under it, the fade under a game window first",()=>
        {
            Check(ChatLook.Alpha(null,100,false) is null && ChatLook.Alpha(null,60,true) is null,"full");
            Check(ChatLook.Alpha(null,60,false)==0.6f && ChatLook.Alpha(null,5,false)==0.2f,"away from the mouse, 20 % at least");
            Check(ChatLook.Alpha(0.3f,60,true)==0.3f && ChatLook.Alpha(0.3f,100,false)==0.3f,"the fade wins");
        }),
        ("Chat window frame 1618: D7, a typed place is kept inside the screen; the centre; out of the screen is said",()=>
        {
            Check(ChatLook.Inside(100,50,560,320,1920,1080)==(100,50,560,320),"inside as typed");
            Check(ChatLook.Inside(1800,1000,560,320,1920,1080)==(1360,760,560,320),"moved in");
            Check(ChatLook.Inside(-40,-10,10,5000,1920,1080)==(0,0,ChatLook.MinWidth,1080),"size bounded, corner in");
            Check(ChatLook.Inside(float.NaN,float.PositiveInfinity,float.NaN,300,1920,1080) is { X:0, Width:ChatLook.MinWidth, Height:300 },"not a number");
            Check(ChatLook.Centered(560,320,1920,1080)==(680,380,560,320) && ChatLook.Centered(4000,320,1920,1080)==(0,380,1920,320),"centre");
            Check(ChatLook.Under((10,10),(0,0),(100,50)) && !ChatLook.Under((100,10),(0,0),(100,50)) && !ChatLook.Under((-1,10),(0,0),(100,50)),"under the mouse");
            Check(ChatLook.Outside(-1,10,100,100,1920,1080) && ChatLook.Outside(1900,10,100,100,1920,1080) && !ChatLook.Outside(0,0,1920,1080,1920,1080),"outside");
        }),
        ("Chat window frame 1618: wiring: the panel takes its frame, fade and place from the settings; tabs hidden with their button",()=>
        {
            var chat=Read("MainWindow.ChatWindow.cs");
            Check(chat.Contains("Fade:ChatFade,") && chat.Contains("Frame:ChatFrame,Once:ChatPlaceOnce);"),"the panel's functions");
            Check(chat.Contains("for(var index=0;index<tabs.Count && !options.HideTabs;index++)") && chat.Contains("if(tabsButton){options.HideTabs=!options.HideTabs;TrySave(\"Chat window\");}"),"D4");
            Check(chat.Contains("SliderSetting(\"ChatWindow.TextOpacity\",") && chat.Contains("DrawChatPlaceSettings();"),"settings");
            var panel=Read("PanelWindow.cs");
            Check(panel.Contains("ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, own.Rounding); framePushed++;") && panel.Contains("if (framePushed > 0) { ImGui.PopStyleVar(framePushed); framePushed = 0; }"),"pushed and popped");
            Check(panel.Contains("if (placed || panel.Once is null) return;"),"a place asked once, not while placed from outside");
        }),
    ];
}
