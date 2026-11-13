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
