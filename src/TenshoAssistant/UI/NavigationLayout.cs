namespace TenshoAssistant.UI;

/// <summary>
/// X-154 (0.1252): the width of the main window's navigation column and when it folds behind a button. Pure: the window gives its width
/// and Dalamud's scale. Mode: 0 automatic (folded under a width), 1 always shown, 2 always folded; anything else counts as automatic.
/// </summary>
public static class NavigationLayout
{
    public const float BaseWidth=200,FoldBelow=620;
    public const int Auto=0,AlwaysShown=1,AlwaysFolded=2;

    public static float Width(float scale)=>BaseWidth*Scale(scale);

    public static bool Folded(int mode,float windowWidth,float scale)=>mode switch
    {
        AlwaysFolded=>true,
        AlwaysShown=>false,
        _=>windowWidth<FoldBelow*Scale(scale),
    };

    // A scale that is not a positive number (never seen, but read from outside) counts as 1.
    private static float Scale(float scale)=>scale>0 && float.IsFinite(scale) ? scale:1;
}
