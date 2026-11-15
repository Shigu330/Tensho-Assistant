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

    /// <summary>X-447 A4 (0.1467): the lines one notch of the mouse wheel moves, as the game's chat.</summary>
    public const int WheelLines=1;

    /// <summary>X-447 A4 (0.1467): the scroll after the mouse wheel: whole lines (the top line kept whole), within 0 and the limit; the
    /// limit itself when the wheel reaches it (the window then follows the bottom again). X-482 M3 (0.1472): the wheel's values add up in
    /// <paramref name="rest"/> and a line moves for each whole notch: a fine wheel or a touchpad sends small values every frame, and each one
    /// moved a whole line (Ceiling or Floor of 0.1), far too fast.</summary>
    public static float Wheel(float scroll,float wheel,float lineHeight,float scrollMax,ref float rest)
    {
        if(lineHeight<=0)return scroll;
        rest+=wheel;
        var notches=(int)Math.Truncate(rest);
        if(notches==0)return scroll;
        rest-=notches;
        var line=(float)Math.Round(scroll/lineHeight)-notches*WheelLines;
        return Math.Clamp(line*lineHeight,0f,Math.Max(0f,scrollMax));
    }

    /// <summary>X-482 M2 (0.1472): the « back to the bottom » button's top left corner, in the lines' own coordinates (scroll included): at the
    /// bottom right of the visible lines. Known before the lines are drawn, so that the links under it do not answer the mouse.</summary>
    public static (float X,float Y) ButtonCorner(float contentRight,float scroll,float shown,float buttonWidth,float buttonHeight)
        =>(Math.Max(0f,contentRight-buttonWidth-4f),scroll+shown-buttonHeight-4f);

    /// <summary>X-482 M2 (0.1472): the point is on the button (its corner and size in the same coordinates).</summary>
    public static bool OnButton(float x,float y,(float X,float Y) corner,float buttonWidth,float buttonHeight)
        =>x>=corner.X && x<=corner.X+buttonWidth && y>=corner.Y && y<=corner.Y+buttonHeight;

    /// <summary>X-447 A4 (0.1467): the « back to the bottom » button shows while the last line is out of view.</summary>
    public static bool ShowsBottomButton(float scroll,float scrollMax)=>scrollMax>0 && scroll<scrollMax-1f;
}
