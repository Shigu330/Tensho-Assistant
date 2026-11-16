using TenshoAssistant.Chat;

// 0.1454 (X-448, the user's request of 11/15): the Tensho chat window without a frame, locked, its background down to transparent; two
// buttons at the end of its tab bar, always visible, turn the frame and the lock on and off.
internal static class ChatFrame1454Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat frame 1454: framed and unlocked by default; the settings give their flags; the background goes down to transparent", () =>
        {
            var options = new ChatSettings();
            Check(!options.Borderless && !options.Locked, "framed and free by default");
            options.Opacity = 0; Check(options.BoundedOpacity == 0, "transparent");
            options.Opacity = -5; Check(options.BoundedOpacity == 0, "bounded");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("private ImGuiWindowFlags ChatFrameFlags()=>(ChatOptions.Borderless ? ImGuiWindowFlags.NoTitleBar:ImGuiWindowFlags.None)\n        |(ChatOptions.Locked ? ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoResize:ImGuiWindowFlags.None)\n", StringComparison.Ordinal), "borderless: no title bar; locked: neither moved nor resized");
        }),
        ("Chat frame 1454: the panel reads the flags and the frame line at each draw, and puts the style back", () =>
        {
            var panel = Read("src/TenshoAssistant/PanelWindow.cs");
            Check(panel.Contains("if (panel.MoreFlags is { } more) Flags = panel.Flags | more();", StringComparison.Ordinal), "flags at each draw");
            var pushed = panel.IndexOf("if (borderPushed) ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);", StringComparison.Ordinal);
            var popped = panel.IndexOf("if (borderPushed) { ImGui.PopStyleVar(); borderPushed = false; }", StringComparison.Ordinal);
            Check(pushed > 0 && popped > pushed && popped < panel.IndexOf("UI.FfxivTheme.Pop();", popped, StringComparison.Ordinal), "popped before the theme");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("MoreFlags:ChatFrameFlags,NoBorder:()=>ChatOptions.Borderless", StringComparison.Ordinal), "the chat window gives them");
        }),
        ("Chat frame 1454: the two buttons sit at the end of the tab bar, with their tips; the settings have both; the slider goes to 0", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var tabs = window.IndexOf("if(!ImGui.BeginTabBar(\"###TenshoChatTabs\"", StringComparison.Ordinal);
            var buttons = window.IndexOf("DrawChatFrameButtons(options);", tabs, StringComparison.Ordinal);
            Check(tabs > 0 && buttons > tabs && buttons < window.IndexOf("finally{ImGui.EndTabBar();}", tabs, StringComparison.Ordinal), "inside the tab bar, so also borderless");
            Check(window.Split("ImGuiTabItemFlags.Trailing|ImGuiTabItemFlags.NoTooltip").Length == 3, "two buttons at the end");
            Check(window.Contains("Tip(options.Borderless ? \"ChatWindow.FrameOnTip\":\"ChatWindow.FrameOffTip\");", StringComparison.Ordinal) && window.Contains("Tip(options.Locked ? \"ChatWindow.UnlockTip\":\"ChatWindow.LockTip\");", StringComparison.Ordinal), "their tips");
            Check(window.Contains("BoolSetting(\"ChatWindow.Borderless\"", StringComparison.Ordinal) && window.Contains("BoolSetting(\"ChatWindow.Locked\"", StringComparison.Ordinal), "the settings");
            Check(window.Contains("value=>configuration.Chat.Opacity=value,0,100,defaults.Opacity", StringComparison.Ordinal), "the slider from 0");
        }),
    ];
}
