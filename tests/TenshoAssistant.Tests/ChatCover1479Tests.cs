using TenshoAssistant.Chat;

// 0.1479 (X-485, the user's request of 15/11: « the chat stays in front of the game's and Tensho's windows; it must be at the back or hide »):
// the chat window behind Tensho's other windows (never brought to the front, put at the back when it appears) and, under a game window, its own
// setting: hide (default), fade (no background, faint text, clicks to the game) or stay; back by itself.
internal static class ChatCover1479Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat cover 1479: hide by default; each mode only while covered; back by itself", () =>
        {
            Check(new ChatSettings().WhenCovered == ChatCovered.Hide, "default: hide");
            Check(ChatCover.Hidden(ChatCovered.Hide, true) && !ChatCover.Hidden(ChatCovered.Hide, false), "hide while covered, back after");
            Check(!ChatCover.Hidden(ChatCovered.Fade, true) && ChatCover.Faded(ChatCovered.Fade, true) && !ChatCover.Faded(ChatCovered.Fade, false), "fade while covered");
            Check(!ChatCover.Hidden(ChatCovered.Stay, true) && !ChatCover.Faded(ChatCovered.Stay, true), "stay");
            Check(ChatCover.Background(ChatCovered.Fade, true, 85) == 0f && ChatCover.Background(ChatCovered.Fade, false, 85) == 0.85f
                  && ChatCover.Background(ChatCovered.Hide, true, 85) == 0.85f, "no background while faded only");
            Check(ChatCover.Hidden((ChatCovered)42, true) && ChatCover.Bounded((ChatCovered)42) == ChatCovered.Hide, "a hand-edited value is « hide »");
        }),
        ("Chat cover 1479: never brought to the front, at the back when it appears; its own setting, not the panels' one; clicks through when faded", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoScrollbar|ImGuiWindowFlags.NoBringToFrontOnFocus,", StringComparison.Ordinal), "never to the front");
            Check(window.Contains("if(frameNow!=chatLastFrame+1)ImGuiP.BringWindowToDisplayBack(ImGuiP.GetCurrentWindow());", StringComparison.Ordinal), "at the back when it appears");
            Check(window.Contains("StepsAside:()=>false,", StringComparison.Ordinal), "the panels' setting does not apply");
            Check(window.Contains("(ChatCover.Faded(ChatOptions.WhenCovered,chatCovered) ? ImGuiWindowFlags.NoInputs:ImGuiWindowFlags.None)", StringComparison.Ordinal), "clicks go through when faded");
            Check(window.Contains("return !ChatCover.Hidden(ChatOptions.WhenCovered,covered);", StringComparison.Ordinal)
                  && window.Contains("chatAt is not null && BehindGame(at,chatSize,chat:true)", StringComparison.Ordinal), "hidden while a game window covers where it was");
            Check(window.Contains("Fade:()=>ChatCover.Faded(ChatOptions.WhenCovered,chatCovered) ? ChatCover.FadedAlpha:null", StringComparison.Ordinal), "faint, whole window (X-498 M1)");
            Check(window.Contains("ComboSetting(\"ChatWindow.WhenCovered\",\"ChatWindow.WhenCoveredTip\"", StringComparison.Ordinal), "the setting");
        }),
    ];
}
