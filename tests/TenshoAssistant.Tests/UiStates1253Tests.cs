using TenshoAssistant.UI;

// 0.1253 (X-154, version 2 of the visual base, former X-152): the four states of a page (waiting, source turned off, old data, failed)
// said the same way everywhere: a glyph and a short word in the state's colour, then the page's own sentence (StateLine).
internal static class UiStates1253Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI states 1253: every state has its text in both languages, its glyph and its colour",() =>
        {
            var states=Enum.GetValues<UiState>();
            Check(states.Length==4,"four states");
            Check(states.Select(UiStates.Key).Distinct().Count()==4 && states.Select(UiStates.Icon).Distinct().Count()==4,"one text and one glyph each");
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                var common=text[text.IndexOf("\n  \"Common\": {",StringComparison.Ordinal)..];
                common=common[..common.IndexOf("\n  },",StringComparison.Ordinal)];
                foreach(var state in states)
                {
                    var key=UiStates.Key(state);
                    Check(key.StartsWith("Common.",StringComparison.Ordinal) && common.Contains($"    \"{key["Common.".Length..]}\": \""),$"{file}: {key}");
                }
            }
            // The glyph is a name of Dalamud's icon font, never a text character missing from the game font (« ✓ », « ✗ », « ◆ »).
            foreach(var state in states)Check(UiStates.Icon(state).All(char.IsAsciiLetter),$"{state}: glyph named, not a character");
            Check(UiStates.Colour(UiState.Waiting)==UiColour.Muted && UiStates.Colour(UiState.SourceOff)==UiColour.Muted,"waiting and source off are not alarms");
            Check(UiStates.Colour(UiState.Stale)==UiColour.Warning && UiStates.Colour(UiState.Failed)==UiColour.Error,"old data warns, a failure is an error");
        }),
        ("UI states 1253: the state line draws the glyph of the table and the word, and the pages use it",() =>
        {
            var lines=Read("src/TenshoAssistant/UI/Components/StatusLines.cs");
            var line=lines[lines.IndexOf("private void StateLine(UiState state,string? detail=null)",StringComparison.Ordinal)..lines.IndexOf("private bool CopyButton(",StringComparison.Ordinal)];
            foreach(var state in Enum.GetValues<UiState>())
                Check(line.Contains($"UiState.{state}=>FontAwesomeIcon.{UiStates.Icon(state)}") || (state==UiState.Failed && line.Contains("_=>FontAwesomeIcon."+UiStates.Icon(state))),$"{state}: the glyph drawn is the one of the table");
            Check(line.Contains("localization.Get(UiStates.Key(state))") && line.Contains("UiBuilder.IconFont"),"the word and the glyph");
            Check(line.Contains("UiColour.Warning=>Warning,UiColour.Error=>Error,_=>Muted"),"theme colours only");
            var errors=Read("src/TenshoAssistant/MainWindow.ErrorPresentation.cs");
            var data=errors[errors.IndexOf("private void DrawDataError(",StringComparison.Ordinal)..errors.IndexOf("private void DrawServiceMessage(",StringComparison.Ordinal)];
            Check(data.Contains("StateLine(UiState.SourceOff,localization.Get(\"Advanced.SourceOffTip\"))") && data.Contains("StateLine(UiState.Failed,localization.Get(key))"),"data errors: source off, or failed");
            Check(data.Contains("if(developerMode)ImGui.TextWrapped(RawMessage(message));"),"the raw message stays in developer mode");
            Check(!Read("src/TenshoAssistant/UI/UiComponents.cs").Contains("StatusBadge"),"the badge of Home is gone");
            var home=Read("src/TenshoAssistant/MainWindow.Home.cs");
            Check(home.Contains("StateLine(UiState.Waiting,localization.Get(\"Home.Loading\"))") && home.Contains("DrawStateMark(true)") && !home.Contains("StatusBadge"),"Home: waiting, or ready with the state mark");
            var garden=Read("src/TenshoAssistant/MainWindow.GardenCrosses.cs");
            Check(garden.Split("StateLine(UI.UiState.Failed,localization.Get(\"Garden.CrossesUnavailable\"))").Length==3
                && garden.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"Garden.CrossesLoading\"))"),"garden crosses: failed (absent or faulted) or waiting");
            var weather=Read("src/TenshoAssistant/MainWindow.Weather.cs");
            Check(weather.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"WeatherDetails.Loading\"))") && weather.Contains("StateLine(UI.UiState.Failed,localization.Get(\"WeatherDetails.Error\"))"),"weather: waiting or failed");
        }),
    ];
}
