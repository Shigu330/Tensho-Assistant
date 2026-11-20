namespace TenshoAssistant.Chat;

/// <summary>X-476 (0.1481, the user's request of 15/11: « more like the game's chat »): what the chat window takes from the game's look. Pure (tests).</summary>
public static class ChatLook
{
    /// <summary>X-484 (0.1519): the opacity of the band behind a message of a player put forward (the theme's warning colour).</summary>
    public const float HighlightBandAlpha=0.22f;

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

    /// <summary>X-447 D6 (0.1618): the window's alpha: faded under a game window (that wins), else the opacity chosen while the mouse is
    /// elsewhere (20 % at least); null: full (under the mouse, or 100 %).</summary>
    public static float? Alpha(float? covered,int opacity,bool hovered)
        =>covered ?? (hovered || opacity>=100 ? null:Math.Clamp(opacity,20,100)/100f);

    /// <summary>X-447 D6 (0.1618): whether the mouse is on the window drawn last at <paramref name="at"/> with <paramref name="size"/>.</summary>
    public static bool Under((float X,float Y) mouse,(float X,float Y) at,(float X,float Y) size)
        =>mouse.X>=at.X && mouse.Y>=at.Y && mouse.X<at.X+size.X && mouse.Y<at.Y+size.Y;

    /// <summary>X-447 D1 (0.1620, « Limited »: an ImGui window takes the mouse whole or not at all): whether the clicks and the wheel go through the
    /// chat window to the game this frame: the setting on, the window locked, no menu of the chat open, and the mouse on none of the zones that
    /// answer it (the tab bar with its buttons, each link, « back to the bottom »), as drawn the frame before.</summary>
    public static bool Through(bool setting,bool locked,bool menuOpen,(float X,float Y) mouse,IReadOnlyList<(float X,float Y,float Width,float Height)> zones)
        =>setting && locked && !menuOpen && !zones.Any(zone=>Under(mouse,(zone.X,zone.Y),(zone.Width,zone.Height)));

    /// <summary>X-447 B5 (0.1621): a player's name's own colour, taken from the name alone (case and spaces aside): a hue from a stable hash
    /// (FNV-1a, the same on every PC and at every start), light enough to read on the window's dark background. Nothing is kept.</summary>
    public static (float R,float G,float B) NameColour(string? name)
    {
        var key=(name ?? "").Trim().ToLowerInvariant();
        var at=key.IndexOf('@');
        if(at>0)key=key[..at];
        var hash=2166136261u;
        foreach(var letter in key){hash^=letter;hash*=16777619u;}
        var hue=hash%360/60f;
        const float value=0.95f,saturation=0.45f;
        var chroma=value*saturation;
        var x=chroma*(1-Math.Abs(hue%2-1));
        var (r,g,b)=(int)hue switch{0=>(chroma,x,0f),1=>(x,chroma,0f),2=>(0f,chroma,x),3=>(0f,x,chroma),4=>(x,0f,chroma),_=>(chroma,0f,x)};
        var m=value-chroma;
        return (r+m,g+m,b+m);
    }

    /// <summary>X-447 D8 (0.1620): whether the window is hidden now by the moments chosen (combat, cutscene, duty); it comes back by itself.</summary>
    public static bool HiddenNow(ChatSettings options,bool inCombat,bool inCutscene,bool inDuty)
        =>options.HideInCombat && inCombat || options.HideInCutscene && inCutscene || options.HideInDuty && inDuty;

    /// <summary>X-447 D7 (0.1618): the smallest size a typed size is given.</summary>
    public const float MinWidth=200,MinHeight=80;

    /// <summary>X-447 D7: a typed place kept inside the screen: its size between the smallest and the screen's, its corner moved in so the
    /// whole window shows.</summary>
    public static (float X,float Y,float Width,float Height) Inside(float x,float y,float width,float height,float screenWidth,float screenHeight)
    {
        var w=Math.Clamp(float.IsFinite(width) ? width:MinWidth,Math.Min(MinWidth,screenWidth),Math.Max(Math.Min(MinWidth,screenWidth),screenWidth));
        var h=Math.Clamp(float.IsFinite(height) ? height:MinHeight,Math.Min(MinHeight,screenHeight),Math.Max(Math.Min(MinHeight,screenHeight),screenHeight));
        return (Math.Clamp(float.IsFinite(x) ? x:0,0,Math.Max(0,screenWidth-w)),Math.Clamp(float.IsFinite(y) ? y:0,0,Math.Max(0,screenHeight-h)),w,h);
    }

    /// <summary>X-447 D7: the window brought back to the screen's centre, its size kept inside the screen.</summary>
    public static (float X,float Y,float Width,float Height) Centered(float width,float height,float screenWidth,float screenHeight)
    {
        var inside=Inside(0,0,width,height,screenWidth,screenHeight);
        return ((screenWidth-inside.Width)/2,(screenHeight-inside.Height)/2,inside.Width,inside.Height);
    }

    /// <summary>X-447 D7: whether a part of the window is out of the screen.</summary>
    public static bool Outside(float x,float y,float width,float height,float screenWidth,float screenHeight)
        =>x<0 || y<0 || x+width>screenWidth || y+height>screenHeight;

    /// <summary>How far the lines a message wraps onto are indented: about one letter's height, or nothing.</summary>
    public static float Indent(float fontSize,bool on)=>on && fontSize>0 ? MathF.Round(fontSize):0f;
    /// <summary>X-556 M3 (review of 0.1520): the width left to a message's text when <paramref name="beside"/> pixels follow it on its line
    /// (the « ×n » of a folded message); never below half the width, and the width as it is when nothing follows.</summary>
    public static float WidthBeside(float width,float beside)=>beside>0 ? Math.Max(width/2,width-beside):width;

    /// <summary>Whether a link's text holds the game's link glyph (or the arrow written for it, X-506): the place of the game's arrow.</summary>
    public static bool Marked(string? text)=>text is { Length:>0 } && (text.Contains(ChatGlyphs.LinkMarker,StringComparison.Ordinal) || text.Contains(ChatGlyphs.LinkArrow,StringComparison.Ordinal));

    /// <summary>A link's text without the game's link glyph and the arrow written for it.</summary>
    public static string Unmarked(string text)=>text.Replace(ChatGlyphs.LinkMarker.ToString(),"",StringComparison.Ordinal).Replace(ChatGlyphs.LinkArrow,"",StringComparison.Ordinal);
}
