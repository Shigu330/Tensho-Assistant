using TenshoAssistant.Chat;

// 0.1467 (X-447 A4, ticked by the user on 11/15): the Tensho chat window scrolls as the game's chat: the wheel moves whole lines; reading
// higher up, a button brings it back down and says how many messages arrived meanwhile.
// 0.1472 (X-482 M3, M2): the wheel's values add up to whole notches (a fraction alone no longer moves a line); the button is placed before
// the lines are drawn.
internal static class ChatScroll1467Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static float Wheel(float scroll, float wheel, float lineHeight, float scrollMax) { var rest = 0f; return ChatScroll.Wheel(scroll, wheel, lineHeight, scrollMax, ref rest); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat scroll 1467: one notch of the wheel moves one whole line, the top line kept whole", () =>
        {
            Check(Wheel(400, 1, 20, 600) == 380 && Wheel(400, -1, 20, 600) == 420, "one line up, one line down");
            Check(Wheel(407, 1, 20, 600) == 380, "a scroll between two lines lands on a whole line");
            Check(Wheel(400, 0.25f, 20, 600) == 400 && Wheel(400, -3, 20, 600) == 460, "a touchpad's fraction alone moves nothing (0.1472); three notches, three lines");
            Check(Wheel(10, 2, 20, 600) == 0 && Wheel(590, -2, 20, 600) == 600, "within 0 and the limit");
            Check(Wheel(400, 0, 20, 600) == 400, "no wheel, no change");
        }),
        ("Chat scroll 1467: reaching the bottom with the wheel follows the new messages again", () =>
        {
            var after = Wheel(580, -1, 20, 600);
            Check(after == 600 && ChatScroll.AtBottom(after, 40, 20, 200), "at the bottom: followed again");
            Check(!ChatScroll.ShowsBottomButton(after, 600) && ChatScroll.ShowsBottomButton(580, 600) && !ChatScroll.ShowsBottomButton(0, 0), "the button only while the last line is out of view");
        }),
        ("Chat scroll 1467: the window reads the wheel itself and draws the button with the new messages' count", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n", "\n");
            Check(window.Contains("ImGuiWindowFlags.AlwaysVerticalScrollbar|ImGuiWindowFlags.NoScrollWithMouse);", StringComparison.Ordinal)
                  && window.Contains("ImGui.SetScrollY(ChatScroll.Wheel(drawn,wheel,height,ImGui.GetScrollMaxY()+shift,ref chatWheelRest));", StringComparison.Ordinal), "the wheel");
            Check(window.Contains("else if(button is { } bottom)DrawChatBottomButton(view,bottom,shown,height);", StringComparison.Ordinal)
                  && window.Contains("localization.Format(\"ChatWindow.NewBelow\",view.Unread)", StringComparison.Ordinal)
                  && window.Contains("Tip(\"ChatWindow.ToBottomTip\");", StringComparison.Ordinal), "the button");
        }),
    ];
}
