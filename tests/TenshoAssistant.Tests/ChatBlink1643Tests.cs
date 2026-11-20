using TenshoAssistant.Chat;

// 0.1643 (X-447 B7, the user's choice of 15/11, after WIM), a part: a tab not shown that holds an alert not seen blinks.
internal static class ChatBlink1643Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat blink 1643: lit for half a second, then not for the next half; on by default (it blinks only once alert words are chosen)",()=>
        {
            Check(ChatLook.Blink(0) && ChatLook.Blink(0.49) && !ChatLook.Blink(0.5) && !ChatLook.Blink(0.99) && ChatLook.Blink(1.0) && ChatLook.Blink(12.2),"half a second each");
            Check(!ChatLook.Blink(double.NaN),"no time: not lit");
            Check(new ChatSettings().BlinkAlerts && string.IsNullOrEmpty(new ChatSettings().AlertWords),"on by default, no alert word by default");
        }),
        ("Chat blink 1643: wiring: only a tab not shown with an alert not seen, the setting on; the colour pushed and popped around the tab; the setting and its texts",()=>
        {
            var chat=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(chat.Contains("var blink=options.BlinkAlerts && view.Alerts>0 && index!=chatTabShown && ChatLook.Blink(ImGui.GetTime());",StringComparison.Ordinal),"when");
            Check(chat.Contains("if(blink)ImGui.PushStyleColor(ImGuiCol.Tab,",StringComparison.Ordinal) && chat.Contains("finally{if(coloured)ImGui.PopStyleColor();if(blink)ImGui.PopStyleColor();}",StringComparison.Ordinal),"pushed and popped");
            Check(chat.Contains("BoolSetting(\"ChatWindow.BlinkAlerts\",\"ChatWindow.BlinkAlertsTip\"",StringComparison.Ordinal),"the setting");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"BlinkAlerts\":",StringComparison.Ordinal) && text.Contains("\"BlinkAlertsTip\":",StringComparison.Ordinal),language);
            }
        }),
    ];
}
