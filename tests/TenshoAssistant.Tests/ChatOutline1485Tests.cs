using TenshoAssistant.Chat;

// 0.1485 (X-502: review of 0.1482): the text's outline fades with the chat window (style Alpha), as the text does.
internal static class ChatOutline1485Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat outline 1485: the outline takes the window's alpha (X-502)", () =>
        {
            Check(ChatLook.Outline(1f) == ChatLook.OutlineColor, "not faded: as before");
            var faded = ChatLook.Outline(0.3f);
            Check(faded >> 24 == (uint)MathF.Round(0xD9 * 0.3f), "faded to 30 %: the outline at 30 % of its own opacity");
            Check((faded & 0x00FFFFFF) == (ChatLook.OutlineColor & 0x00FFFFFF), "still black");
            Check(ChatLook.Outline(0f) >> 24 == 0, "gone with the window");
            Check(ChatLook.Outline(2f) == ChatLook.OutlineColor && ChatLook.Outline(-1f) >> 24 == 0 && ChatLook.Outline(float.NaN) == ChatLook.OutlineColor, "bounded");
            Check(ChatLook.Outline(0.5f) >> 24 < ChatLook.Outline(0.8f) >> 24, "the paler the window, the paler the outline");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var body = window[window.IndexOf("private static void ChatOutline(string text)", StringComparison.Ordinal)..];
            body = body[..body.IndexOf("\n    }", StringComparison.Ordinal)];
            Check(body.Contains("ChatLook.Outline(ImGui.GetStyle().Alpha)", StringComparison.Ordinal) && !body.Contains("ChatLook.OutlineColor,text", StringComparison.Ordinal),
                  "drawn with the window's alpha, never the raw colour");
        }),
    ];
}
