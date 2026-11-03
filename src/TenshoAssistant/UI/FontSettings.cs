namespace TenshoAssistant.UI;

/// <summary>
/// 0.394 (user request: « a setting for the fonts, so they can be changed »): the fonts of the Tensho window — page titles, section
/// headers and the text — each a game font family (from the game files, through Dalamud) and a size in pixels. The text can also keep
/// Dalamud's own font (« Dalamud », the default). Values are checked on use: an unknown family falls back to the default.
/// </summary>
public sealed class FontSettings
{
    public const string DalamudFont="Dalamud";
    public const int MinimumSize=10,MaximumSize=40;
    /// <summary>The game font families offered (Dalamud GameFontFamily names).</summary>
    public static readonly string[] GameFamilies=["Axis","Jupiter","Meidinger","MiedingerMid","TrumpGothic"];

    public string Title { get; set; }="TrumpGothic";
    public int TitleSize { get; set; }=24;
    public string Section { get; set; }="TrumpGothic";
    public int SectionSize { get; set; }=19;
    public string Body { get; set; }=DalamudFont;
    public int BodySize { get; set; }=17;

    public static int ClampSize(int size)=>Math.Clamp(size,MinimumSize,MaximumSize);

    /// <summary>A usable family: one of the game families (or « Dalamud » where allowed), else the fallback.</summary>
    public static string Family(string? name,string fallback,bool dalamudAllowed=false)
        =>name is not null && (GameFamilies.Contains(name,StringComparer.Ordinal) || dalamudAllowed && name==DalamudFont) ? name:fallback;

    /// <summary>The three fonts as used (families checked, sizes clamped); equal keys need no rebuild.</summary>
    public (string Title,int TitleSize,string Section,int SectionSize,string Body,int BodySize) Key
        =>(Family(Title,"TrumpGothic"),ClampSize(TitleSize),Family(Section,"TrumpGothic"),ClampSize(SectionSize),Family(Body,DalamudFont,true),ClampSize(BodySize));
}
