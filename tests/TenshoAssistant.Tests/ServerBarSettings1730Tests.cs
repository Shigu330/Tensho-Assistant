using TenshoAssistant.Alarms;

// X-724 (0.1730, the user's request of 21/11): every setting of the server bar in one category, Settings › Tracking › Server bar: no setting
// whose key holds « ServerBar » elsewhere, every entry's setting there, the tooltip over the entries and Tensho's state with them; each label
// says what it shows; the tooltip's setting greyed only while no entry is on (behaviour: each entry alone turns it on).
internal static class ServerBarSettings1730Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Server bar settings 1730: no setting whose key holds « ServerBar » outside Tracking › Server bar; every entry and its tooltip there", () =>
        {
            var tabs = TenshoAssistant.SettingsIndex.Entries.ToDictionary(entry => entry.Key, entry => entry.Tab);
            foreach (var (key, category) in TenshoAssistant.SettingsIndex.Categories)
                if (key.Contains("ServerBar", StringComparison.Ordinal))
                    Check(category == "ServerBar" && tabs[key] == "Tracking", key + " is in " + tabs[key] + " › " + category);
            foreach (var key in ServerBarEntries.LabelKeys.Concat(["Advanced.AlarmBarTooltip", "Advanced.AlarmBarTooltipCount", "Advanced.ServerBarTooltipAbove", "Advanced.StateServerBarProblems", "Advanced.StatePanel"]))
                Check(TenshoAssistant.SettingsIndex.Categories.TryGetValue(key, out var category) && category == "ServerBar" && tabs[key] == "Tracking", key + ": in Tracking › Server bar");
            Check(ServerBarEntries.LabelKeys.Count == 10 && ServerBarEntries.LabelKeys.Distinct().Count() == 10, "ten entries, once each");
        }),
        ("Server bar settings 1730: one entry alone is enough for the tooltip; none: greyed", () =>
        {
            var none = new TenshoAssistant.Configuration { AlarmServerBar = false, ReminderServerBar = false, ReminderFinishedServerBar = false, CurrencyServerBar = false, GilServerBar = false, BagsServerBar = false, WearServerBar = false, PlaceServerBar = false, StateServerBar = false };
            none.Chat.ServerBarAlerts = false;
            Check(!ServerBarEntries.AnyOn(none), "no entry: greyed");
            var alone = new Action<TenshoAssistant.Configuration>[]
            {
                c => c.AlarmServerBar = true, c => c.ReminderServerBar = true, c => c.ReminderFinishedServerBar = true, c => c.CurrencyServerBar = true, c => c.GilServerBar = true,
                c => c.BagsServerBar = true, c => c.WearServerBar = true, c => c.PlaceServerBar = true, c => c.Chat.ServerBarAlerts = true, c => c.StateServerBar = true,
            };
            Check(alone.Length == ServerBarEntries.LabelKeys.Count, "one case per entry");
            for (var index = 0; index < alone.Length; index++)
            {
                var one = new TenshoAssistant.Configuration { AlarmServerBar = false, ReminderServerBar = false, ReminderFinishedServerBar = false, CurrencyServerBar = false, GilServerBar = false, BagsServerBar = false, WearServerBar = false, PlaceServerBar = false, StateServerBar = false };
                one.Chat.ServerBarAlerts = false;
                alone[index](one);
                Check(ServerBarEntries.AnyOn(one), ServerBarEntries.LabelKeys[index] + " alone: the tooltip's setting is on");
            }
        }),
        ("Server bar settings 1730: labels say what each entry shows; the links lead to the category; the chat window keeps its row", () =>
        {
            var fr = Read("Localization/Languages/fr.json"); var en = Read("Localization/Languages/en.json");
            Check(fr.Contains("\"ServerBar\": \"Devises au-dessus du seuil dans la barre du serveur\"", StringComparison.Ordinal) && fr.Contains("\"ServerBarAlerts\": \"Alertes du chat dans la barre du serveur\"", StringComparison.Ordinal), "fr");
            Check(en.Contains("\"ServerBar\": \"Currencies over their threshold in the server bar\"", StringComparison.Ordinal) && en.Contains("\"ServerBarAlerts\": \"Chat alerts in the server bar\"", StringComparison.Ordinal), "en");
            Check(!fr.Contains("\"ServerBar\": \"Dans la barre du serveur\"", StringComparison.Ordinal) && !en.Contains("\"ServerBar\": \"In the server bar\"", StringComparison.Ordinal), "no label without its object");
            Check(Read("MainWindow.SettingsCategories.cs").Contains("new(\"Tracking\",\"ServerBar\",DrawServerBarSettings),", StringComparison.Ordinal), "the category");
            Check(Read("MainWindow.cs").Contains("internal void OpenStateSettings(){OpenLink(\"Dashboard.ServerBar\");IsOpen=true;}", StringComparison.Ordinal) && TenshoAssistant.Help.NavTargets.Of("Dashboard.ServerBar")==new TenshoAssistant.Help.NavTarget("Settings","Tracking","ServerBar"), "the state's links");
            var chat = Read("MainWindow.ChatWindow.cs");
            Check(chat.Contains("{DrawChatPlayers(options);DrawChatServerBarSetting();}", StringComparison.Ordinal), "the chat window's own settings keep the row");
            var players = chat[chat.IndexOf("private void DrawChatPlayers(ChatSettings options)", StringComparison.Ordinal)..chat.IndexOf("private void DrawChatServerBarSetting()", StringComparison.Ordinal)];
            Check(!players.Contains("ServerBarAlerts", StringComparison.Ordinal) && !players.Contains("DrawChatServerBarSetting", StringComparison.Ordinal), "no longer reached by Panels › Chat players");
        }),
    ];
}
