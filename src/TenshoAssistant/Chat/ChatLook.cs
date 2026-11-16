namespace TenshoAssistant.Chat;

/// <summary>X-476 (0.1481, the user's request of 15/11: « more like the game's chat »): what the chat window takes from the game's look. Pure (tests).</summary>
public static class ChatLook
{
    /// <summary>X-447 A3: the outline, the text drawn dark around itself, one pixel each way, before the text itself.</summary>
    public static readonly (float X,float Y)[] OutlineShifts=[(-1,0),(1,0),(0,-1),(0,1)];
    /// <summary>The outline's colour, ABGR: black, nearly opaque.</summary>
    public const uint OutlineColor=0xD9000000;
    /// <summary>Narrow margins, as the game's chat: around the window, between its tab bar and its lines.</summary>
    public static readonly (float X,float Y) Padding=(6,4);
}
