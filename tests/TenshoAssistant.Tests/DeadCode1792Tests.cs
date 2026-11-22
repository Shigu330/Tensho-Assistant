using System.Text.Json;

// X-569 (0.1792, first slice of the audit of 17/11, part C): the plugin's dead members and unused translation keys removed; this keeps them
// from coming back unseen. Navigation.Character, listed by the audit, is used again since (the Collections page's category): kept.
internal static class DeadCode1792Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Dead code 1792: the dead members of the audit of 17/11 are gone", () =>
        {
            foreach (var (file, text) in new[]
            {
                ("src/TenshoAssistant/MainWindow.ChatWindow.cs", "ChatLayoutsPerFrame"),
                ("src/TenshoAssistant/MainWindow.Gear.cs", "SameLineOrNext"),
                ("src/TenshoAssistant/UI/FfxivTheme.cs", "public static Vector4 Hovered"),
                ("src/TenshoAssistant/Diagnostics/SlowNotes.cs", "public TimeSpan Every"),
                ("src/TenshoAssistant/Diagnostics/SlowNotes.cs", "ThresholdMs"),
                ("src/TenshoAssistant/Monsters/MonsterLootService.cs", "FreshFor"),
            })
                Check(!Read(file).Contains(text, StringComparison.Ordinal), $"{Path.GetFileName(file)}: {text}");
            Check(Read("src/TenshoAssistant/Diagnostics/SlowNotes.cs").Contains("public sealed class SlowNotes(TimeSpan every,double thresholdMs=50)", StringComparison.Ordinal),
                "SlowNotes keeps its pace and threshold, read by Note");
        }),
        ("Dead code 1792: the unused translation keys are gone from every language; Navigation.Character is used and kept", () =>
        {
            var gone = new Dictionary<string, string[]>
            {
                ["Navigation"] = ["Workspace", "Modules", "Preferences"], ["GatheringClock"] = ["Source", "Current", "Next"],
                ["MapView"] = ["ShowQuestAreas", "ShowQuestAreasHelp"], ["BeastUi"] = ["TeamBeast"], ["CurrencyValue"] = ["Close"],
            };
            foreach (var language in new[] { "en", "fr", "de", "ja" })
            {
                using var document = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
                foreach (var (section, keys) in gone)
                    if (document.RootElement.TryGetProperty(section, out var found))
                        foreach (var key in keys)
                            Check(!found.TryGetProperty(key, out _), $"{language}: {section}.{key}");
            }
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("PageLabel(\"Navigation.Character\")", StringComparison.Ordinal), "Navigation.Character used");
        }),
    ];
}
