namespace TenshoAssistant.Chat;

/// <summary>X-636 (bench X-631, review of 0.1441, M3): when a link of the chat window acts on the left button's release: the button was
/// pressed on this link, is released over it, and the mouse did not move past the drag threshold meanwhile (a drag moves the window). Pure.</summary>
public static class ChatLinkPress
{
    public static bool Acts(string? pressed,string id,bool hovered,bool dragged)=>pressed==id && hovered && !dragged;

    /// <summary>The mouse moved past ImGui's own drag threshold (6 pixels) since the press: a drag, not a click.</summary>
    public const float DragThreshold=6f;
    public static bool Dragged(System.Numerics.Vector2 pressedAt,System.Numerics.Vector2 now)=>System.Numerics.Vector2.DistanceSquared(pressedAt,now)>=DragThreshold*DragThreshold;
}
