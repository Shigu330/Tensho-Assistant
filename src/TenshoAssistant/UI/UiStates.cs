namespace TenshoAssistant.UI;

/// <summary>X-154 (0.1253): the four states a page says the same way everywhere (« nothing here » is not one of them: EmptyState).</summary>
public enum UiState { Waiting, SourceOff, Stale, Failed }

/// <summary>The theme colour of a state; the colour never tells the state alone (warning, success and accent are close in every palette).</summary>
public enum UiColour { Muted, Warning, Error }

/// <summary>
/// For each state: its short text (a key of the language files), its glyph (the name of a FontAwesomeIcon of Dalamud, as text so this table
/// is tested without Dalamud) and its colour. The glyph and the word tell the state, the colour only supports them.
/// </summary>
public static class UiStates
{
    public static string Key(UiState state)=>state switch
    {
        UiState.Waiting=>"Common.StateWaiting",
        UiState.SourceOff=>"Common.StateSourceOff",
        UiState.Stale=>"Common.StateStale",
        _=>"Common.StateFailed",
    };

    public static string Icon(UiState state)=>state switch
    {
        UiState.Waiting=>"Hourglass",
        UiState.SourceOff=>"PowerOff",
        UiState.Stale=>"History",
        _=>"ExclamationTriangle",
    };

    public static UiColour Colour(UiState state)=>state switch
    {
        UiState.Waiting or UiState.SourceOff=>UiColour.Muted,
        UiState.Stale=>UiColour.Warning,
        _=>UiColour.Error,
    };
}

/// <summary>X-156 (0.1264): where a shown value comes from: read (the game, or a source's own figure), computed by Tensho from a rule, or read too long ago.</summary>
public enum UiOrigin { Measured, Estimated, Old }

/// <summary>
/// For each origin: its short word, its glyph (a FontAwesomeIcon name, as text) and its colour. An old value says it like the « old data »
/// state of a page (same word, same glyph, same colour): one way to say « old » everywhere.
/// </summary>
public static class UiOrigins
{
    public static string Key(UiOrigin origin)=>origin switch
    {
        UiOrigin.Measured=>"Common.OriginMeasured",
        UiOrigin.Estimated=>"Common.OriginEstimated",
        _=>UiStates.Key(UiState.Stale),
    };

    public static string Icon(UiOrigin origin)=>origin switch
    {
        UiOrigin.Measured=>"Eye",
        UiOrigin.Estimated=>"Calculator",
        _=>UiStates.Icon(UiState.Stale),
    };

    public static UiColour Colour(UiOrigin origin)=>origin==UiOrigin.Old ? UiStates.Colour(UiState.Stale):UiColour.Muted;
}
