using TenshoAssistant.Chat;

// 0.1481 (X-476, the user's request of 15/11: « more like the game's chat: same layout, same kind of transparency, add a tab and the chat's
// settings in the chat, mirrored in the global settings »): tabs over or under, time in brackets, outline, light background, narrow margins,
// « + » and gear at the end of the tabs, the gear's panel drawn by the same method as the Settings page.
internal static class ChatLook1481Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat look 1481: defaults (tabs over, outline on, light background); time in brackets as the game", () =>
        {
            var settings = new ChatSettings();
            Check(!settings.TabsAtBottom && settings.TextOutline && settings.BoundedOpacity == 40, "defaults");
            var record = new ChatRecord(1, DateTimeOffset.FromUnixTimeSeconds(1_700_000_000), 10, ChatChannel.Say, "Alice", []);
            var runs = ChatFormat.Runs(record, new ChatSettings { ShowTimestamps = true, Clock24 = true }, _ => "", _ => "");
            var time = runs.First(run => run.Role == ChatRole.Timestamp).Text;
            Check(time.Length == "[00:00] ".Length && time[0] == '[' && time[6] == ']' && time[2] != '[', "[22:47] : " + time);
            Check(ChatLook.OutlineShifts.Length == 4 && ChatLook.OutlineShifts.All(shift => Math.Abs(shift.X) + Math.Abs(shift.Y) == 1), "one pixel each way");
            Check(ChatLook.OutlineColor >> 24 > 0xC0 && (ChatLook.OutlineColor & 0xFFFFFF) == 0, "dark, nearly opaque");
        }),
        ("Chat look 1481: one source of settings: the gear's panel and the Settings page draw the same method from configuration.Chat", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var popup = window[window.IndexOf("private void DrawChatSettingsPopup()", StringComparison.Ordinal)..];
            popup = popup[..popup.IndexOf("finally{ImGui.EndPopup();}", StringComparison.Ordinal)];
            Check(popup.Contains("DrawChatWindowSettings();", StringComparison.Ordinal), "the gear's panel");
            Check(Read("src/TenshoAssistant/MainWindow.Settings.cs").Contains("DrawChatWindowSettings();", StringComparison.Ordinal), "the Settings page");
            var settings = window[window.IndexOf("private void DrawChatWindowSettings()", StringComparison.Ordinal)..window.IndexOf("private List<ChatTabSettings> ChatTabs(", StringComparison.Ordinal)];
            foreach (var key in new[] { "ChatWindow.Opacity", "ChatWindow.Borderless", "ChatWindow.Locked", "ChatWindow.FontScale", "ChatWindow.MaxLines", "ChatWindow.TabsAtBottom", "ChatWindow.TextOutline", "ChatWindow.SectionTabs", "ChatWindow.SectionColors" })
                Check(settings.Contains("\"" + key + "\"", StringComparison.Ordinal), "in the chat's settings: " + key);
            Check(!settings.Contains("new ChatSettings", StringComparison.Ordinal) && settings.Contains("configuration.Chat", StringComparison.Ordinal), "no copy: the configuration itself");
        }),
        ("Chat look 1481: « + » and gear at the end of the tabs; the bar over or under the lines; outline before the text; margins", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var buttons = window[window.IndexOf("private void DrawChatFrameButtons(", StringComparison.Ordinal)..];
            Check(buttons.Contains("GlyphTabButton(\"###ChatAddTab\",Dalamud.Interface.FontAwesomeIcon.Plus", StringComparison.Ordinal) && buttons.Contains("AddChatTab(tabs);", StringComparison.Ordinal), "« + » adds a tab");
            Check(buttons.Contains("ImGui.OpenPopup(\"###TenshoChatSettings\");", StringComparison.Ordinal) && buttons.Contains("DrawChatSettingsPopup();", StringComparison.Ordinal), "the gear opens the panel, in the same ID scope");
            var body = window[window.IndexOf("private void DrawChatWindowBody()", StringComparison.Ordinal)..window.IndexOf("private void DrawChatTabBar(", StringComparison.Ordinal)];
            var under = body.IndexOf("if(options.TabsAtBottom)DrawChatShownTab(tabs,options,-ImGui.GetFrameHeightWithSpacing());", StringComparison.Ordinal);
            var bar = body.IndexOf("DrawChatTabBar(tabs,options);", StringComparison.Ordinal);
            var over = body.IndexOf("if(!options.TabsAtBottom)DrawChatShownTab(tabs,options,0);", StringComparison.Ordinal);
            Check(under > 0 && bar > under && over > bar, "lines, bar, lines");
            Check(window.Contains("BeginChild(\"###TenshoChatLines\",new Vector2(0,linesHeight)", StringComparison.Ordinal), "the bar's room kept under the lines");
            Check(window.Contains("if(options.TextOutline)ChatOutline(run.Text);\n                    ImGui.TextColored(", StringComparison.Ordinal)
                  && window.Contains("if(ChatOptions.TextOutline)ChatOutline(run.Text);\n        ImGui.TextColored(Accent,run.Text);", StringComparison.Ordinal), "outline under the text and the links");
            Check(window.Contains("Padding:new Vector2(ChatLook.Padding.X,ChatLook.Padding.Y)", StringComparison.Ordinal), "narrow margins");
            var panel = Read("src/TenshoAssistant/PanelWindow.cs");
            Check(panel.Contains("if (panel.Padding is { } padding) ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding);", StringComparison.Ordinal)
                  && panel.Contains("if (paddingPushed) { ImGui.PopStyleVar(); paddingPushed = false; }", StringComparison.Ordinal), "pushed before Begin, popped");
        }),
    ];
}
