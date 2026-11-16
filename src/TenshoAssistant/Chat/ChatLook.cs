namespace TenshoAssistant.Chat;

/// <summary>X-476 (0.1481, the user's request of 15/11: « more like the game's chat »): what the chat window takes from the game's look. Pure (tests).</summary>
public static class ChatLook
{
    /// <summary>X-447 A3: the outline, the text drawn dark around itself, one pixel each way, before the text itself.</summary>
    public static readonly (float X,float Y)[] OutlineShifts=[(-1,0),(1,0),(0,-1),(0,1)];
    /// <summary>The outline's colour, ABGR: black, nearly opaque.</summary>
    public const uint OutlineColor=0xD9000000;
    /// <summary>X-502 (review of 0.1482): the outline's colour under the window's alpha (style Alpha, pushed before Begin when the chat fades).
    /// A draw list's colours are not multiplied by it, as TextColored's are: the outline stayed dark over a faded text.</summary>
    public static uint Outline(float alpha)
    {
        var a=float.IsNaN(alpha) ? 1f:Math.Clamp(alpha,0f,1f);
        var opacity=(uint)MathF.Round((OutlineColor>>24)*a);
        return opacity<<24|OutlineColor&0x00FFFFFF;
    }
    /// <summary>Narrow margins, as the game's chat: around the window, between its tab bar and its lines.</summary>
    public static readonly (float X,float Y) Padding=(6,4);
}
