using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Chat;

// X-567 (audit of 17/11, part B): four texts the players read said something the code does not do. Each one is now tied to the code it
// describes (a label of the language files, a constant, the page's group), so that it cannot drift again.
internal static class FaqTruth1538Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ");

    private static string Section(string json, string name)
    {
        var start = Regex.Match(json, "^  \"" + Regex.Escape(name) + "\": \\{\n", RegexOptions.Multiline);
        Check(start.Success, "section " + name);
        var end = Regex.Match(json[start.Index..], "^  \\}", RegexOptions.Multiline);
        return json.Substring(start.Index, end.Index);
    }

    private static string Value(string json, string key)
    {
        var found = Regex.Matches(json, "^    \"" + Regex.Escape(key) + "\": (\".*\"),?$", RegexOptions.Multiline);
        Check(found.Count == 1, $"one key {key} ({found.Count})");
        return JsonSerializer.Deserialize<string>(found[0].Groups[1].Value) ?? "";
    }

    private static string Language(string language) => Read("src/TenshoAssistant/Localization/Languages/" + language + ".json");

    private static string Capital(string word) => word[..1].ToUpperInvariant() + word[1..].ToLowerInvariant();

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("FAQ truth 1538: the chat window's answer names the option that hides the game chat, off by default (no « never hidden »)", () =>
        {
            Check(!new ChatSettings().HideGameMessages, "the option is off by default");
            foreach (var language in new[] { "en", "fr" })
            {
                var json = Language(language);
                var answer = Value(json, "AnswerChatWindow");
                var label = Value(Section(json, "ChatWindow"), "HideGameMessages");
                Check(answer.Contains("« " + label + " »", StringComparison.Ordinal), language + ": the answer quotes the option « " + label + " »");
                Check(!answer.Contains("never hidden", StringComparison.OrdinalIgnoreCase) && !answer.Contains("jamais caché", StringComparison.OrdinalIgnoreCase),
                    language + ": no « never hidden »");
                Check(answer.Contains(language == "en" ? "off by default" : "désactivé par défaut", StringComparison.Ordinal), language + ": off by default");
            }
        }),
        ("FAQ truth 1538: the FAQ and the README say the desktop app rings the alarms (X-163), never that they stay empty", () =>
        {
            Check(Read("src/TenshoAssistant.Desktop/DesktopSurface.cs").Contains("X-163: the alarms to ring now", StringComparison.Ordinal),
                "the desktop app rings the alarms (DesktopSurface)");
            var readme = Flat(Read("README.md"));
            var texts = new[]
            {
                ("en FAQ", Value(Language("en"), "AnswerDesktop")), ("fr FAQ", Value(Language("fr"), "AnswerDesktop")),
                ("README", readme),
            };
            foreach (var (name, text) in texts)
            {
                var flat = Flat(text);
                Check(!flat.Contains("timers, alarms) stay empty", StringComparison.Ordinal) && !flat.Contains("minuteurs, alarmes) restent vides", StringComparison.Ordinal),
                    name + ": alarms said to stay empty");
            }
            Check(Flat(texts[0].Item2).Contains("Alarms still ring from the app", StringComparison.Ordinal), "en FAQ: alarms ring");
            Check(Flat(texts[1].Item2).Contains("Les alarmes sonnent quand même depuis l'application", StringComparison.Ordinal), "fr FAQ: alarms ring");
            Check(readme.Contains("Alarms still ring from the app", StringComparison.Ordinal)
                && readme.Contains("Les alarmes sonnent quand même depuis l'application", StringComparison.Ordinal), "README: both halves");
        }),
        ("FAQ truth 1538: SECURITY gives the chat history's defaults from ChatHistory (messages and days), in both halves", () =>
        {
            var messages = ChatHistory.DefaultMessages.ToString("N0", CultureInfo.InvariantCulture);
            var security = Flat(Read("SECURITY.md"));
            Check(security.Contains($"({messages} messages and {ChatHistory.DefaultDays} days by default)", StringComparison.Ordinal),
                $"English: {messages} messages and {ChatHistory.DefaultDays} days");
            Check(security.Contains($"({messages.Replace(",", " ")} messages et {ChatHistory.DefaultDays} jours par défaut)", StringComparison.Ordinal),
                $"French: {ChatHistory.DefaultDays} jours");
            Check(!security.Contains("7 days by default", StringComparison.Ordinal) && !security.Contains("7 jours par défaut", StringComparison.Ordinal), "no 7 days left");
        }),
        ("FAQ truth 1538: the garden's answer gives the path the navigation draws (its group, the page, the tab, the option)", () =>
        {
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Garden,\"Tracking\",", StringComparison.Ordinal), "the garden page is in the Tracking group");
            Check(Read("src/TenshoAssistant/MainWindow.Garden.cs").Contains("(\"Garden\",localization.Get(\"Garden.TabGarden\")", StringComparison.Ordinal), "its Garden tab");
            foreach (var language in new[] { "en", "fr" })
            {
                var json = Language(language);
                var navigation = Section(json, "Navigation");
                var garden = Section(json, "Garden");
                var path = Capital(Value(navigation, "CategoryTracking")) + " › " + Value(navigation, "Garden") + " › "
                    + (language == "en" ? Value(garden, "TabGarden") + " tab" : "onglet " + Value(garden, "TabGarden"))
                    + " › « " + Value(garden, "DetailedView") + " »";
                var answer = Value(json, "AnswerGardenText");
                Check(answer.StartsWith(path, StringComparison.Ordinal), language + ": the answer starts with « " + path + " »");
                Check(!answer.Contains("Garden journal", StringComparison.Ordinal) && !answer.Contains("Journal de jardinage", StringComparison.Ordinal), language + ": no journal heading");
            }
        }),
    ];
}
