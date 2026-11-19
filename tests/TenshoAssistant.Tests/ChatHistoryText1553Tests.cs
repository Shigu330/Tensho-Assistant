using System.Text.Json;

// 0.1553 (X-593, external audit A04 of 18/11; the user's choice of 18/11: correct the text, not the code). The chat history keeps every message
// the chat window reads: the tabs' filters and the hidden players only choose what is shown (ChatFilters), so the consent, the setting's tip
// and the FAQ say it, in English and French, with the window's own words for the hidden players; the code that keeps them is unchanged.
internal static class ChatHistoryText1553Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Text(string language, string section, string key)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
        return json.RootElement.GetProperty(section).GetProperty(key).GetString() ?? "";
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat history text 1553: the consent, the tip and the FAQ say every message read is kept, also channels no tab shows and the hidden players", () =>
        {
            foreach (var (language, every, noTab) in new[] { ("en", "every message the chat window reads", "channels no tab shows"), ("fr", "message lu par la fenêtre de chat", "canaux qu'aucun onglet ne montre") })
            {
                var hidden = Text(language, "ChatWindow", "HiddenPlayers").ToLowerInvariant();
                foreach (var (section, key) in new[] { ("ChatWindow", "HistoryConsentText"), ("ChatWindow", "KeepHistoryTip"), ("Help", "AnswerChatHistory") })
                {
                    var text = Text(language, section, key);
                    Check(text.Contains(every, StringComparison.OrdinalIgnoreCase) && text.Contains(noTab, StringComparison.Ordinal) && text.Contains(hidden, StringComparison.Ordinal),
                        $"{language} {section}.{key}: every message read, the channels no tab shows and the {hidden} said");
                    Check(!text.Contains(language == "en" ? "messages of the tabs" : "messages des onglets", StringComparison.Ordinal)
                        && !text.Contains(language == "en" ? "message of the tabs" : "message des onglets", StringComparison.Ordinal), $"{language} {section}.{key}: no longer « of the tabs »");
                }
            }
        }),
        ("Chat history text 1553: the behaviour the texts describe is the code's: kept right after the window's store, before any tab or hidden player", () =>
        {
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs")).Replace("\r\n", "\n");
            var added = reader.IndexOf("var record=store.Add(", StringComparison.Ordinal);
            var kept = reader.IndexOf("history?.Keep(record);", StringComparison.Ordinal);
            Check(added > 0 && kept > added && !reader[added..kept].Contains("Filter", StringComparison.Ordinal), "every message copied is kept, no filter between");
            var filters = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatFilters.cs"));
            Check(filters.Contains("if(ChatTextFilter.IsPlayer(record.Sender,hidden))return false;", StringComparison.Ordinal), "the hidden players are a filter of what is shown");
        }),
    ];
}
