using System.Text.Json;

// 0.1183 (X-253, « first player » review of 12/11, B6; user's choice: the Tensho map stays on M by default, but Tensho says so): one chat line
// the first time M opens the Tensho map, a FAQ question « M opens another map », and the FAQ no longer says nothing acts without a click.
internal static class MapNotice1183Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Map notice 1183: said once, at the first replaced M, then saved; the map still replaces the game's by default", () =>
        {
            var view = Read("src/TenshoAssistant/MainWindow.MapView.cs");
            var show = view.IndexOf("internal void ShowMapFromGame(GameMapRequest request)", StringComparison.Ordinal);
            var notice = view.IndexOf("if(!configuration.MapReplaceNoticeSaid)", StringComparison.Ordinal);
            Check(show > 0 && notice > show && notice - show < 400, "the notice opens ShowMapFromGame, called only when M was replaced");
            Check(view.Contains("configuration.MapReplaceNoticeSaid=true;\n", StringComparison.Ordinal) || view.Contains("configuration.MapReplaceNoticeSaid=true;\r\n", StringComparison.Ordinal), "said once");
            Check(view.Contains("ChatNotice?.Invoke(localization.Get(\"MapView.ReplacedNotice\"));", StringComparison.Ordinal) && view.Contains("TrySave(\"Map notice\");", StringComparison.Ordinal), "in the chat, then saved");
            var fresh = new TenshoAssistant.Configuration();
            Check(!fresh.MapReplaceNoticeSaid && fresh.MapView.ReplaceGameMap, "not said yet; M still opens the Tensho map by default");
        }),
        ("Map notice 1183: the FAQ answers « M opens another map » and says which settings act without a click, in English and French", () =>
        {
            Check(TenshoAssistant.Help.Faq.Entries.Any(entry => entry.Key == "Map" && entry.Page == "Settings"), "the FAQ question, opening Settings");
            foreach (var (language, map, path) in new[] { ("en", "M key", "Settings › Map"), ("fr", "touche M", "Paramètres › Carte") })
            {
                using var document = JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                var help = document.RootElement.GetProperty("Help");
                Check((help.GetProperty("AnswerActions").GetString() ?? "").Contains(map, StringComparison.Ordinal), language + ": the actions answer names the M key");
                Check((help.GetProperty("AnswerMap").GetString() ?? "").Contains(path, StringComparison.Ordinal), language + ": the map answer names where to turn it off");
                Check((document.RootElement.GetProperty("MapView").GetProperty("ReplacedNotice").GetString() ?? "").Contains(path, StringComparison.Ordinal), language + ": the chat line too");
            }
        }),
    ];
}
