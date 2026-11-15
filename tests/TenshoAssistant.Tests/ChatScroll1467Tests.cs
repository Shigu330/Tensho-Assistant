using TenshoAssistant.Chat;

// 0.1467 (X-447 A4, ticked by the user on 11/15): the Tensho chat window scrolls as the game's chat: the wheel moves whole lines; reading
// higher up, a button brings it back down and says how many messages arrived meanwhile.
internal static class ChatScroll1467Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat scroll 1467: one notch of the wheel moves one whole line, the top line kept whole", () =>
        {
            Check(ChatScroll.Wheel(400, 1, 20, 600) == 380 && ChatScroll.Wheel(400, -1, 20, 600) == 420, "one line up, one line down");
            Check(ChatScroll.Wheel(407, 1, 20, 600) == 380, "a scroll between two lines lands on a whole line");
            Check(ChatScroll.Wheel(400, 0.25f, 20, 600) == 380 && ChatScroll.Wheel(400, -3, 20, 600) == 460, "a touchpad's fraction is a notch; three notches, three lines");
            Check(ChatScroll.Wheel(10, 2, 20, 600) == 0 && ChatScroll.Wheel(590, -2, 20, 600) == 600, "within 0 and the limit");
            Check(ChatScroll.Wheel(400, 0, 20, 600) == 400, "no wheel, no change");
        }),
        ("Chat scroll 1467: reaching the bottom with the wheel follows the new messages again", () =>
        {
            var after = ChatScroll.Wheel(580, -1, 20, 600);
            Check(after == 600 && ChatScroll.AtBottom(after, 40, 20, 200), "at the bottom: followed again");
            Check(!ChatScroll.ShowsBottomButton(after, 600) && ChatScroll.ShowsBottomButton(580, 600) && !ChatScroll.ShowsBottomButton(0, 0), "the button only while the last line is out of view");
        }),
        ("Chat scroll 1467: the window reads the wheel itself and draws the button with the new messages' count", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n", "\n");
            Check(window.Contains("ImGuiWindowFlags.AlwaysVerticalScrollbar|ImGuiWindowFlags.NoScrollWithMouse);", StringComparison.Ordinal)
                  && window.Contains("ImGui.SetScrollY(ChatScroll.Wheel(scroll,wheel,height,ImGui.GetScrollMaxY()));", StringComparison.Ordinal), "the wheel");
            Check(window.Contains("else DrawChatBottomButton(view,scroll,shown,height);", StringComparison.Ordinal)
                  && window.Contains("localization.Format(\"ChatWindow.NewBelow\",view.Unread)", StringComparison.Ordinal)
                  && window.Contains("Tip(\"ChatWindow.ToBottomTip\");", StringComparison.Ordinal), "the button");
        }),
    ];
}
