using System.Text.Json;

// 0.327 (queue step e, audit 2026-11-02): one switch per in-game Triple Triad aid (match overlay, NPC challenge panel with the deck
// choice odds, deck editor highlight), all on by default so nothing changes; each guards only its own drawing, never the reading.
internal static class TriadSwitches327Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string relative) => File.ReadAllText(Path.Combine(Source,relative));
    private static string Text(string language,string section,string key)
    {
        using var document = JsonDocument.Parse(Read(Path.Combine("Localization","Languages",language + ".json")));
        return document.RootElement.GetProperty(section).GetProperty(key).GetString() ?? "";
    }
    private static readonly (string Property,string Label,string File,string Guard)[] Switches =
    [
        ("TriadMatchOverlay","TriadMatchOverlay","MainWindow.TriadAdvice.cs","if(windowDisposed || !configuration.TriadMatchOverlay || FreshTriadFrame() is not { } frame)return;"),
        ("TriadChallengePanel","TriadChallengePanel","MainWindow.TriadRequest.cs","if(windowDisposed || !configuration.TriadChallengePanel || triadObservation.State is not { Enabled:true } || triadObservation.Request is not { } request)return;"),
        ("TriadDeckEditorHighlight","TriadDeckEditor","MainWindow.TriadDecks.cs","if(windowDisposed || !configuration.TriadDeckEditorHighlight || triadObservation.State is not { Enabled:true } || triadObservation.EditDeckWindow is null)return;"),
    ];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Triple Triad aids: three switches, on by default, reset with the other settings",() =>
        {
            var fresh = new TenshoAssistant.Configuration();
            Check(fresh.TriadMatchOverlay && fresh.TriadChallengePanel && fresh.TriadDeckEditorHighlight,"all on by default: nothing changes");
            Check(fresh.Observations.Count == 0,"the Triad observation itself stays off by default");
            foreach (var (property,_,_,_) in Switches)
                Check(TenshoAssistant.ConfigurationReset.Reset.Contains(property) && !TenshoAssistant.ConfigurationReset.Kept.Contains(property),"reset with the settings: " + property);
        }),
        ("Triple Triad aids: each drawing is guarded by its own switch, the native reading is unchanged",() =>
        {
            foreach (var (property,_,file,guard) in Switches) Check(Read(file).Contains(guard,StringComparison.Ordinal),"guard of " + property + " in " + file);
            var reader = Read(Path.Combine("MiniGames","TriadObservationReader.Dalamud.cs"));
            foreach (var (property,_,_,_) in Switches) Check(!reader.Contains(property,StringComparison.Ordinal),"the reading does not depend on " + property);
        }),
        ("Triple Triad aids: shared rows under the observation, greyed while it is off, tips end with their default",() =>
        {
            var settings = Read("MainWindow.Settings.cs");
            var observation = settings.IndexOf("DrawObservationSetting(\"Triad\"",StringComparison.Ordinal);
            var greyed = settings.IndexOf("ImGui.BeginDisabled(!triadObservation.State.Enabled);",Math.Max(0,observation),StringComparison.Ordinal);
            Check(observation >= 0 && greyed > observation,"rows greyed while the observation is off");
            foreach (var (property,label,_,_) in Switches)
            {
                var row = $"BoolSetting(\"ModuleSettings.{label}\",\"ModuleSettings.{label}Tip\",()=>configuration.{property},value=>configuration.{property}=value,c=>c.{property});";
                Check(settings.IndexOf(row,StringComparison.Ordinal) > greyed,"shared row with « ↺ » under the observation: " + property);
                Check(Text("fr","ModuleSettings",label).Length > 0 && Text("en","ModuleSettings",label).Length > 0,"labels: " + label);
                Check(Text("fr","ModuleSettings",label + "Tip").EndsWith("\nPar défaut : activé.",StringComparison.Ordinal),"French tip ends with its default: " + label);
                Check(Text("en","ModuleSettings",label + "Tip").EndsWith("\nDefault: on.",StringComparison.Ordinal),"English tip ends with its default: " + label);
            }
        }),
    ];
}
