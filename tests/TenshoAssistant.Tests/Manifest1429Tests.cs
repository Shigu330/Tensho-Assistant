// 0.1429, X-438 (review 0.1427 P1): the manifest's description (the plugin installer's text) names the settings that act without a click as
// the FAQ does, with their defaults read from a fresh Configuration: the M key that opens Tensho's map (on by default) and the job title (off).
using System.Text.Json;

internal static class Manifest1429Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Manifest 1429: the installer's text names both settings that act without a click, with the defaults a fresh install has", () =>
        {
            using var manifest = JsonDocument.Parse(Read("src/TenshoAssistant/TenshoAssistant.json"));
            var description = manifest.RootElement.GetProperty("Description").GetString() ?? "";
            var fresh = new TenshoAssistant.Configuration();
            Check(fresh.MapView.ReplaceGameMap && !fresh.JobTitlesEnabled, "the defaults the text states: M replaced, job titles off");
            Check(description.Contains("Two settings act without a click: the M key opens Tensho's map instead of the game's (on by default;", StringComparison.Ordinal), "the M key, on by default");
            Check(description.Contains("the title you chose for a job is worn when you change job (off by default).", StringComparison.Ordinal), "the job title, off by default");
            Check(!description.Contains("One setting", StringComparison.Ordinal), "no longer one setting");
            // The FAQ says the same two (MapNotice1183Tests checks its M key); the manifest and the FAQ agree on the count.
            using var english = JsonDocument.Parse(Read("src/TenshoAssistant/Localization/Languages/en.json"));
            Check((english.RootElement.GetProperty("Help").GetProperty("AnswerActions").GetString() ?? "").Contains("Two settings act without a click", StringComparison.Ordinal), "the FAQ counts two as well");
        }),
    ];
}
