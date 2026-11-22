using TenshoAssistant.Chat;

// 0.1490 (X-505, the user's capture of 11/16 09:58, opacity saved at 0 %): the chat window's background stayed opaque navy. The window's own
// background followed the setting (SetNextWindowBgAlpha), but the lines' area is a child window, and the theme (FfxivTheme) gives children its
// opaque panel colour: drawn over the window's background, it hid it. The area now has no background of its own.
internal static class ChatBackground1490Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat background 1490: the lines' area draws no background over the window's (transparent ChildBg around its start)", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var push = window.IndexOf("ImGui.PushStyleColor(ImGuiCol.ChildBg,Vector4.Zero);", StringComparison.Ordinal);
            var area = window.IndexOf("try{visible=ImGui.BeginChild(ChatFormat.LinesId(tab)", StringComparison.Ordinal);
            var pop = window.IndexOf("finally{ImGui.PopStyleColor();if(thin)", StringComparison.Ordinal);
            Check(push > 0 && area > push && pop > area, "pushed before the area begins, taken off after, even on an error");
            var theme = Read("src/TenshoAssistant/UI/FfxivTheme.cs");
            Check(theme.Contains("ImGui.PushStyleColor(ImGuiCol.ChildBg, c.Panel);", StringComparison.Ordinal), "the cause: the theme's children are painted (if it changes, recheck)");
        }),
        ("Chat background 1490: the window's background follows the setting, and is as light as the game's by default", () =>
        {
            Check(ChatCover.Background(ChatCovered.Hide, false, new ChatSettings().BoundedOpacity) == 0.15f, "15 % by default");
            Check(ChatCover.Background(ChatCovered.Hide, false, 0) == 0f && ChatCover.Background(ChatCovered.Stay, false, 100) == 1f, "0 % to 100 %, as set");
            // X-574 (0.1793): the one-time upgrade of settings saved before 0.1488 is gone: a saved background stays as it is.
            Check(new ChatSettings { Opacity = 40 }.BoundedOpacity == 40, "a saved background is kept as it is");
            var panel = Read("src/TenshoAssistant/PanelWindow.cs");
            Check(panel.Contains("if ((panel.Opacity?.Invoke() ?? panel.Alpha) is { } alpha) ImGui.SetNextWindowBgAlpha(alpha);", StringComparison.Ordinal), "the window's background takes it at each draw");
        }),
    ];
}
