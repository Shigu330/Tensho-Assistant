namespace TenshoAssistant.Chat;

/// <summary>
/// X-472 (0.1471, user's capture of 11/15): the game writes some letters with glyphs of its own font (private use area U+E000 to U+F8FF:
/// Dalamud's SeIconChar) and puts icons of its font between texts (IconPayload: Dalamud's BitmapFontIcon, such as the world icon between a
/// player's name and their world). The Tensho chat window left both out (« Name [world icon] World : THE EXAMPLE » became
/// « NameWorld: »). Each is now written with a readable equivalent in plain letters, never left out: boxed letters and numbers
/// as letters and numbers, the world icon as « @ » (the game's own « Name@World »), the auto-translate marks as « » ; the link glyph alone is
/// left out (the link is underlined). A glyph or icon not listed is written « · ». The copy of a message keeps the same text. Pure.
/// Codes: Dalamud 15's SeIconChar and BitmapFontIcon (tools/ApiProbe, 11/16).
/// </summary>
public static class ChatGlyphs
{
    /// <summary>SeIconChar.LinkMarker: the game's link glyph, before a link's text.</summary>
    public const char LinkMarker = '';
    /// <summary>BitmapFontIcon.CrossWorld: the world icon between a player's name and their world.</summary>
    public const uint CrossWorld = 88;

    /// <summary>Whether a letter is a glyph of the game's font (private use area).</summary>
    public static bool IsGlyph(char letter)=>letter is >= '' and <= '';

    /// <summary>The readable text of a glyph of the game's font (SeIconChar); empty for the link glyph only.</summary>
    public static string Readable(char letter)=>letter switch
    {
        LinkMarker=>"",
        >= '' and <= ''=>((char)('A'+(letter-''))).ToString(),                       // BoxedLetterA to Z
        >= '' and <= ''=>(letter-'').ToString(System.Globalization.CultureInfo.InvariantCulture),   // BoxedNumber0 to 31
        >= '' and <= ''=>((char)('0'+(letter-''))).ToString(),                        // Number0 to 9
        >= '' and <= ''=>((char)('0'+(letter-''))).ToString(),                        // BoxedOutlinedNumber0 to 9
        >= '' and <= ''=>((char)('1'+(letter-''))).ToString(),                        // Instance1 to 9
        >= '' and <= ''=>Roman[letter-''],                                             // BoxedRoman1 to 6
        >= '' and <= ''=>Clocks[(letter-'')%3],                                        // local, server, Eorzea time (four languages)
        ''=>"+",          // BoxedPlus
        ''=>"?",          // BoxedQuestionMark
        ''=>"*",          // BoxedStar
        ''=>" HQ",        // HighQuality, after an item's name
        ''=>" (C)",       // Collectible
        ''=>"@",          // CrossWorld
        ''=>"«",          // AutoTranslateOpen
        ''=>"»",          // AutoTranslateClose
        ''=>">",          // ArrowRight
        ''=>"v",          // ArrowDown
        ''=>"AM",         // TimeAm
        ''=>"PM",         // TimePm
        '' or ''=>"Lv",   // LevelEn, LevelDe
        ''=>"Niv",        // LevelFr
        ''=>"iLv",        // ItemLevel
        ''=>"gil",        // Gil
        '' or ''=>"EXP",  // Experience, ExperienceFilled
        ''=>"O",          // Circle
        ''=>"[]",         // Square
        ''=>"X",          // Cross
        ''=>"^",          // Triangle
        _=>"·",
    };

    private static readonly string[] Roman=["I","II","III","IV","V","VI"];
    private static readonly string[] Clocks=["LT","ST","ET"];

    /// <summary>The readable text of an icon of the game's font (IconPayload, BitmapFontIcon): the world icon « @ », the auto-translate marks, arrows; « · » for the others.</summary>
    public static string Icon(uint icon)=>icon switch
    {
        CrossWorld=>"@",
        54=>"«",    // AutoTranslateBegin
        55=>"»",    // AutoTranslateEnd
        105=>"^",   // ArrowUp
        106=>"v",   // ArrowDown
        _=>"·",
    };
}
