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
    /// <remarks>X-504 M1 (review of 0.1486): the outline is drawn in <see cref="OutlineShifts"/>.Length copies that overlap (under a letter,
    /// all of them). Each copy's opacity is chosen so that the copies together are <paramref name="alpha"/> times as dark as they are at full
    /// opacity: faded to 30 %, the halo is at most 30 % dark, as the text (before, each copy at 30 % of its own opacity made 69 % where the four
    /// met). At full opacity each copy keeps its colour, as before.</remarks>
    public static uint Outline(float alpha)
    {
        var a=float.IsNaN(alpha) ? 1f:Math.Clamp(alpha,0f,1f);
        var opacity=(uint)Math.Clamp(Math.Round(255*Copy(Together(OutlineOpacity)*a)),0,255);
        return opacity<<24|OutlineColor&0x00FFFFFF;
    }

    /// <summary>X-504 M1: the outline's own opacity, from 0 to 1 (0xD9).</summary>
    public static double OutlineOpacity=>(OutlineColor>>24)/255d;

    /// <summary>X-504 M1: how dark the copies are where all of them overlap, each at <paramref name="each"/> (0 to 1).</summary>
    public static double Together(double each)=>1-Math.Pow(1-Math.Clamp(each,0,1),OutlineShifts.Length);

    /// <summary>X-504 M1: the opacity of each copy for the copies together to be <paramref name="together"/> dark.</summary>
    public static double Copy(double together)=>1-Math.Pow(1-Math.Clamp(together,0,1),1d/OutlineShifts.Length);
    /// <summary>Narrow margins, as the game's chat: around the window, between its tab bar and its lines.</summary>
    public static readonly (float X,float Y) Padding=(6,4);

    // X-505 (0.1488, the user's captures of 11/16, Tensho's chat and the game's side by side): the rest of the game's look, each a setting.
    /// <summary>The thin scroll bar's width (the theme's is about 14 px), as the game's line.</summary>
    public const float ScrollbarThin=5f;
    /// <summary>The thin bar's grab: the text's colour at this opacity (rest, hovered, held); its track is transparent.</summary>
    public static readonly (float Idle,float Hovered,float Held) ScrollbarGrab=(0.30f,0.45f,0.60f);
    /// <summary>The game's link arrow colour (orange), RGBA.</summary>
    public static readonly (float R,float G,float B,float A) LinkMark=(0.96f,0.55f,0.18f,1f);
    /// <summary>The game's tabs: rounded corners, and their grey, RGBA (inactive, hovered, shown).</summary>
    public const float TabRounding=7f;
    public static readonly (float R,float G,float B,float A) Tab=(0.20f,0.20f,0.20f,0.75f),TabHovered=(0.32f,0.32f,0.32f,0.85f),TabShown=(0.30f,0.30f,0.30f,0.90f);

    /// <summary>A tab's label: with the game's look, the tab shown is marked by a dot before its name (« · »: the game font has no bullet).</summary>
    public static string TabLabel(string label,bool shown,bool game)=>game && shown ? "· "+label:label;

    /// <summary>How far the lines a message wraps onto are indented: about one letter's height, or nothing.</summary>
    public static float Indent(float fontSize,bool on)=>on && fontSize>0 ? MathF.Round(fontSize):0f;

    /// <summary>Whether a link's text holds the game's link glyph (or the arrow written for it, X-506): the place of the game's arrow.</summary>
    public static bool Marked(string? text)=>text is { Length:>0 } && (text.Contains(ChatGlyphs.LinkMarker,StringComparison.Ordinal) || text.Contains(ChatGlyphs.LinkArrow,StringComparison.Ordinal));

    /// <summary>A link's text without the game's link glyph and the arrow written for it.</summary>
    public static string Unmarked(string text)=>text.Replace(ChatGlyphs.LinkMarker.ToString(),"",StringComparison.Ordinal).Replace(ChatGlyphs.LinkArrow,"",StringComparison.Ordinal);
}
