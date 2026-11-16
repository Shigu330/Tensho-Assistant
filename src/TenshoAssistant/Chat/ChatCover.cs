namespace TenshoAssistant.Chat;

/// <summary>X-485 (0.1479, the user's request of 15/11): what the Tensho chat window does while a window of the game covers it.</summary>
public enum ChatCovered { Hide, Fade, Stay }

/// <summary>
/// X-485 (0.1479): the chat window under a window of the game (a window drawn by Dalamud cannot go under the game's windows): hidden, faded
/// (no background, faint text, the clicks go through to the game), or left as it is; back by itself once nothing covers it. Behind Tensho's
/// other windows it goes by its draw order (never brought to the front, put at the back when it appears). Pure (tests).
/// </summary>
public static class ChatCover
{
    /// <summary>The text's strength while faded (the background is not drawn).</summary>
    public const float FadedAlpha=0.3f;

    public static ChatCovered Bounded(ChatCovered mode)=>Enum.IsDefined(mode) ? mode:ChatCovered.Hide;
    public static bool Hidden(ChatCovered mode,bool covered)=>covered && Bounded(mode)==ChatCovered.Hide;
    public static bool Faded(ChatCovered mode,bool covered)=>covered && Bounded(mode)==ChatCovered.Fade;
    /// <summary>The background's opacity: none while faded, else the one chosen.</summary>
    public static float Background(ChatCovered mode,bool covered,int opacity)=>Faded(mode,covered) ? 0f:Math.Clamp(opacity,0,100)/100f;
}
