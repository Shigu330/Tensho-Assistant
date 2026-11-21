using TenshoAssistant.Chat;

// 0.1651 (X-675, the user's report of 20/11 about 21:20): the « Select text » popup shrank each frame until its box was gone.
internal static class ChatSelectBox1651Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Window()=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat select box 1651: the size is fixed from the chat window and stays the same 10 s and more (no feedback from the popup)",()=>
        {
            var size=ChatCopy.SelectBox((560f,320f),40f,8f,8f);
            Check(size==(544f,264f),size.ToString());
            // The popup fits its content: its size is the box plus the header and padding. Ten seconds at 144 frames: the box never moves.
            // (Before: box = popup - header - padding, popup = box + header + padding - what ImGui takes off; each frame a little less.)
            var popup=(X:0f,Y:0f);
            for(var frame=0;frame<1440;frame++)
            {
                var box=ChatCopy.SelectBox((560f,320f),40f,8f,8f);
                Check(box==size,$"frame {frame}: {box}");
                popup=(box.Width+16f,box.Height+40f+16f);
            }
            Check(popup==(560f,320f),popup.ToString());
        }),
        ("Chat select box 1651: bounds: as wide as the chat window (320 at least), 200 to 600 high, never under 240 × 120",()=>
        {
            Check(ChatCopy.SelectBox((100f,50f),40f,8f,8f)==(304f,144f),"small window: 320 × 200 less header and padding");
            Check(ChatCopy.SelectBox((2000f,1500f),40f,8f,8f)==(1984f,544f),"big window: as wide, 600 high at most");
            Check(ChatCopy.SelectBox((100f,50f),400f,100f,100f)==(ChatCopy.MinimumSelectWidth,ChatCopy.MinimumSelectHeight),"never gone");
            Check(ChatCopy.SelectBox((0f,float.NaN),40f,8f,8f)==(544f,264f),"size not known yet: 560 × 320");
            Check(ChatCopy.SelectBox((float.PositiveInfinity,-3f),float.NaN,float.NaN,-1f)==(560f,320f),"strange values: no NaN, no infinity");
        }),
        ("Chat select box 1651: wiring: the box takes ChatCopy.SelectBox's size from the chat window, never the space left",()=>
        {
            var chat=Window();
            Check(chat.Contains("var (boxWidth,boxHeight)=ChatCopy.SelectBox((chatSize.X,chatSize.Y),ImGui.GetTextLineHeightWithSpacing()+ImGui.GetFrameHeightWithSpacing(),padding.X,padding.Y);"),"size from the chat window");
            Check(chat.Contains("new Vector2(boxWidth,boxHeight),ImGuiInputTextFlags.ReadOnly);") && !chat.Contains("new Vector2(-1,-1),ImGuiInputTextFlags.ReadOnly"),"fixed box");
        }),
    ];
}
