namespace TenshoAssistant.UI;

/// <summary>
/// X-460 (the user's capture of 15/11, test T-0.1130-1): the map's « 100% » boxes (the typed zoom and the button under it) showed their text
/// shifted right. Cause: both were sized to the text plus one frame padding, while ImGui puts a padding on each side: the button's label had
/// less room than its width and started at the left padding, running to the right edge; the field's text, always left-aligned, did the same.
/// The sizes and the field's padding that put a text in the middle of its box, at any font size. Pure.
/// </summary>
public static class CenteredText
{
    /// <summary>The width of a button that holds its text with a padding on each side (never narrower than <paramref name="least"/>).</summary>
    public static float ButtonWidth(float textWidth,float padding,float least)=>Math.Max(least,textWidth+2*Math.Max(0,padding));

    /// <summary>The left padding that puts a text in the middle of a box this wide (a field's text starts at its padding); never below
    /// <paramref name="least"/>, so a text wider than the box starts at the usual padding.</summary>
    public static float Padding(float boxWidth,float textWidth,float least)=>Math.Max(least,(boxWidth-textWidth)/2);
}
