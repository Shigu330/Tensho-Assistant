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
    /// <summary>X-506 (0.1487): what the window writes for the game's link glyph when a link has nothing else to show; X-505 (0.1488): the
    /// arrow of the game's look. An arrow of the window's font, as the game's. (X-509: the alarms' place link of 0.414 to 0.1490 was not such a
    /// link of the game but a bare one sent by Tensho: docs/evidence/0.1491-alarm-place-link.md.)</summary>
    public const string LinkArrow = "\u2192";
    /// <summary>BitmapFontIcon.CrossWorld: the world icon between a player's name and their world.</summary>
    public const uint CrossWorld = 88;
    /// <summary>X-492 M4, X-523 (0.1501): BitmapFontIcon codes of the sender's decorations (Dalamud 15.0.4, tools/ApiProbe:
    /// docs/evidence/0.1501-bitmapfonticon-codes.md). 127 is RolePlaying, 168 WaitingForDutyFinder and 169 Tural (a region): not decorations.</summary>
    public const uint NewAdventurer = 77, MentorPvP = 81, Returner = 95, MentorProblem = 108, Gladiator = 128, Sage = 167, Viper = 170, Pictomancer = 171;

    /// <summary>X-523 (0.1501): whether an icon is one the game puts before a sender's name: the novice network's status (NewAdventurer 77,
    /// Mentor 78, MentorPvE 79, MentorCrafting 80, MentorPvP 81, Returner 95, MentorProblem 108) and the classes and jobs (Gladiator 128 to
    /// Sage 167, Viper 170, Pictomancer 171). X-525 M3 (0.1503): chosen from the names of Dalamud's BitmapFontIcon, not yet from what the
    /// game really puts before a sender's name (R06): in developer mode the reader writes the icon codes it meets in senders' names
    /// (ChatPayloads.SenderIcons, codes only), so a real payload can prove or correct the set; until then 127 (RolePlaying) stays « · ».</summary>
    public static bool IsSenderDecoration(uint icon)=>icon is >= NewAdventurer and <= MentorPvP or Returner or MentorProblem or >= Gladiator and <= Sage or Viper or Pictomancer;

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

    /// <summary>The readable text of an icon of the game's font (IconPayload, BitmapFontIcon) in a message's text: the world icon « @ », the
    /// auto-translate marks, arrows; « · » for the others, so an icon in the text of a message keeps a trace (X-524 M2, 0.1501).</summary>
    public static string Icon(uint icon)=>icon switch
    {
        CrossWorld=>"@",
        54=>"«",    // AutoTranslateBegin
        55=>"»",    // AutoTranslateEnd
        105=>"^",   // ArrowUp
        106=>"v",   // ArrowDown
        _=>"·",
    };

    /// <summary>X-492 M4, X-523 and X-524 M2 (0.1501): the readable text of an icon in a sender's name: the decorations the game puts before
    /// the name (IsSenderDecoration) are left out, so the name reads as it is (« ·Name » before 0.1499); the others as in a message (Icon).
    /// The name and the channel say who speaks; the icon adds no word to copy. In a message's text, Icon keeps « · ».</summary>
    public static string SenderIcon(uint icon)=>IsSenderDecoration(icon) ? "" : Icon(icon);
}
