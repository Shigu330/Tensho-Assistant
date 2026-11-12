using System.Numerics;

namespace TenshoAssistant.UI;

/// <summary>
/// X-162 (0.1129, user request of 12/11: the gil tooltip of the server bar opened above while the others opened below): where Tensho's
/// tooltip of a server bar entry goes. Under the entry by default, above it when the setting says so; the other side when the chosen one
/// has less than a quarter of the screen; on the right half of the screen its right edge follows the entry's right edge, so it never
/// leaves the screen. Returns the position and the pivot for ImGui.SetNextWindowPos.
/// </summary>
public static class BarTooltipPlace
{
    public const float Gap=4;

    public static (Vector2 Position,Vector2 Pivot) For(Vector2 min,Vector2 max,Vector2 screen,bool above)
    {
        var roomAbove=min.Y>=screen.Y/4;var roomBelow=screen.Y-max.Y>=screen.Y/4;
        var up=above ? roomAbove || !roomBelow:!roomBelow && roomAbove;
        var right=min.X+max.X>screen.X;
        var x=right ? Math.Min(max.X,screen.X):Math.Max(min.X,0);
        return (new(x,up ? min.Y-Gap:max.Y+Gap),new(right ? 1:0,up ? 1:0));
    }
}
