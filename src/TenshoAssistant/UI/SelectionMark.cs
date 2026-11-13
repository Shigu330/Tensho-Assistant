using System.Numerics;

namespace TenshoAssistant.UI;

/// <summary>
/// X-154 (0.1252): the mark of a selected element, beside its background: a bar inside the element's box, on its left edge (a row) or its
/// bottom edge (a button in a row of buttons). A selection is never told by the text colour (accent on the selection background falls to
/// 3.85:1 in the light theme). Pure: the caller draws the rectangle in the theme's accent.
/// </summary>
public static class SelectionMark
{
    /// <summary>Thickness of the bar: 3 px at scale 1, never under 2.</summary>
    public static float Thickness(float scale)=>MathF.Max(2,MathF.Round(3*(scale>0 && float.IsFinite(scale) ? scale:1),MidpointRounding.AwayFromZero));

    /// <summary>The bar on the left edge, the whole height of the element.</summary>
    public static (Vector2 Min,Vector2 Max) SideMark(Vector2 itemMin,Vector2 itemMax,float scale)
        =>(itemMin,new Vector2(MathF.Min(itemMax.X,itemMin.X+Thickness(scale)),itemMax.Y));

    /// <summary>The bar on the bottom edge, the whole width of the element.</summary>
    public static (Vector2 Min,Vector2 Max) UnderMark(Vector2 itemMin,Vector2 itemMax,float scale)
        =>(new Vector2(itemMin.X,MathF.Max(itemMin.Y,itemMax.Y-Thickness(scale))),itemMax);
}
