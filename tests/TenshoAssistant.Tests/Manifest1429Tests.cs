// 0.1429, X-438 (review 0.1427 P1): the manifest's description (the plugin installer's text) names the settings that act without a click as
// the FAQ does, with their defaults read from a fresh Configuration: the M key that opens Tensho's map (on by default) and the job title (off).
using System.Text.Json;

internal static class Manifest1429Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Manifest 1429: the installer's text names the three settings that act without a click, with the defaults a fresh install has", () =>
        {
            using var manifest = JsonDocument.Parse(Read("src/TenshoAssistant/TenshoAssistant.json"));
            var description = manifest.RootElement.GetProperty("Description").GetString() ?? "";
            var fresh = new TenshoAssistant.Configuration();
            // X-875 I2 (0.1882): the third one, the game chat's messages hidden behind Tensho's chat window (ChatSettings.HideGameMessages).
            Check(fresh.MapView.ReplaceGameMap && !fresh.JobTitlesEnabled && !fresh.Chat.HideGameMessages, "the defaults the text states: M replaced, job titles off, game chat shown");
            Check(description.Contains("Three settings act without a click: the M key opens Tensho's map instead of the game's (on by default;", StringComparison.Ordinal), "the M key, on by default");
            Check(description.Contains("the title you chose for a job is worn when you change job (off by default),", StringComparison.Ordinal), "the job title, off by default");
            Check(description.Contains("« Hide the game chat's messages » hides the game chat's message panels and tabs while Tensho's chat window is open in their place (off by default;", StringComparison.Ordinal), "the game chat hidden, off by default");
            Check(!description.Contains("Two settings act without a click", StringComparison.Ordinal), "no longer two settings");
            Check(!description.Contains("One setting", StringComparison.Ordinal), "no longer one setting");
            // The FAQ says the same two (MapNotice1183Tests checks its M key); the manifest and the FAQ agree on the count.
            using var english = JsonDocument.Parse(Read("src/TenshoAssistant/Localization/Languages/en.json"));
            var answer = english.RootElement.GetProperty("Help").GetProperty("AnswerActions").GetString() ?? "";
            Check(answer.Contains("Three settings act without a click", StringComparison.Ordinal), "the FAQ counts three as well");
            Check(answer.Contains("« Hide the game chat's messages »", StringComparison.Ordinal), "the FAQ names the game chat setting");
            // The setting's name in the text is the one shown in Settings (en.json ChatWindow.HideGameMessages).
            Check(english.RootElement.GetProperty("ChatWindow").GetProperty("HideGameMessages").GetString() == "Hide the game chat's messages", "the setting's shown name");
            using var french = JsonDocument.Parse(Read("src/TenshoAssistant/Localization/Languages/fr.json"));
            var reponse = french.RootElement.GetProperty("Help").GetProperty("AnswerActions").GetString() ?? "";
            var nom = french.RootElement.GetProperty("ChatWindow").GetProperty("HideGameMessages").GetString() ?? "?";
            Check(reponse.Contains("Trois réglages agissent sans clic", StringComparison.Ordinal) && reponse.Contains("« " + nom + " »", StringComparison.Ordinal), "the French FAQ counts three and names the setting as Settings shows it");
        }),
    ];
}
