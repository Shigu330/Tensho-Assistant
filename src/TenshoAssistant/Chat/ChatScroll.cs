namespace TenshoAssistant.Chat;

/// <summary>
/// X-457 (0.1462): how the chat window scrolls. The cause of the jumps (0.1434 to 0.1461; measured in 0.1461, « Chat window jumps »: limit 0/31
/// and widths 704/720 in turn, frame after frame): the lines were placed from ImGui.GetCursorStartPos(), which already takes the scroll off,
/// and ImGui.SetCursorPosY takes it off again: every line was drawn the scroll too high, and the height ImGui measured was the rows' height
/// less the scroll. At the bottom (scroll 31), the measure fell to 200 px, the limit to 0, the scroll to 0, the measure back to 231 px, and
/// so on; with a long history the lines were drawn far above the view (an empty window, the bar at the bottom). Now the lines are placed from
/// the top of the content (ImGui.GetCursorPosY() before any line), the lines' height is declared from the tab's rows (<see cref="Content"/>),
/// not measured, and the scroll bar is always kept (one width for the layout). « At the bottom » is read against the rows the frame before
/// showed (<see cref="AtBottom"/>), so a new message is followed, not taken for a scroll up. Pure (tests).
/// </summary>
public static class ChatScroll
{
    /// <summary>The height of the tab's lines: its rows (a message not laid out yet counts one) times the height of a row.</summary>
    public static float Content(int rows,float lineHeight)=>Math.Max(0,rows)*Math.Max(0f,lineHeight);

    /// <summary>The scroll limit: the lines' height less the visible height (0 when everything shows).</summary>
    public static float Max(float content,float visible)=>Math.Max(0f,content-Math.Max(0f,visible));

    /// <summary>The scroll that shows the last line at the bottom of the window.</summary>
    public static float Bottom(int rows,float lineHeight,float visible)=>Max(Content(rows,lineHeight),visible);

    /// <summary>At the bottom: the last line the frame before showed was in view (a pixel of rounding allowed). Read against those rows and
    /// that height, not this frame's (a message just arrived, a window just resized): the window then follows the new message, and keeps the
    /// last line at the bottom after a resize.</summary>
    public static bool AtBottom(float scroll,int rowsBefore,float lineHeight,float visibleBefore)=>scroll>=Bottom(rowsBefore,lineHeight,visibleBefore)-1f;
}
