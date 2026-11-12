using System.Text.Json;

// 0.1180 (X-250, « first player » review of 12/11, B1, B2, B4, D1, D2, D3): what leaves the PC is said truly wherever a player reads it before
// connecting or installing: the exact spot of a fish caught, the common base read without an account, « Exchange now » sending even with the
// automatic exchange off, a gist that is unlisted (its link is in a public issue), and every file of the exchange.
internal static class TruthTexts1180Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
    private static string Key(JsonElement root, string section, string key) => root.GetProperty(section).GetProperty(key).GetString() ?? "";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Truth texts 1180: the warning, the credits and the FAQ say what leaves the PC, in English and French", () =>
        {
            foreach (var (language, spot, exchange, automatic) in new[] { ("en", "exact spot", "GitHub exchange", "automatically"), ("fr", "endroit exact", "l'échange GitHub", "automatiquement") })
            {
                using var document = JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                var root = document.RootElement;
                var discoveries = root.EnumerateObject().Where(section => section.Value.ValueKind == JsonValueKind.Object && section.Value.TryGetProperty("PublicWarning", out _)).Select(section => section.Name).Single();
                Check(Key(root, discoveries, "PublicWarning").Contains(spot, StringComparison.Ordinal), language + ": the warning names the fish's exact spot");
                Check(Key(root, discoveries, "AutoExchangeTip").Contains(automatic, StringComparison.Ordinal), language + ": nothing is sent automatically, « Exchange now » still sends");
                var faq = root.EnumerateObject().Where(section => section.Value.ValueKind == JsonValueKind.Object && section.Value.TryGetProperty("AnswerSaved", out _)).Select(section => section.Name).Single();
                Check(Key(root, faq, "AnswerSaved").Contains(exchange, StringComparison.Ordinal) && Key(root, faq, "AnswerSaved").Contains("Discord", StringComparison.Ordinal), language + ": the FAQ names the exchange and Discord");
                var credits = root.GetProperty("Credits").GetProperty("Items").GetProperty("DiscoveriesExchange").GetString() ?? "";
                Check(credits.Contains(spot, StringComparison.Ordinal), language + ": the credits name the exact spot");
                var all = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(!all.Contains("secret gist", StringComparison.Ordinal) && !all.Contains("gist secret", StringComparison.Ordinal), language + ": no « secret » gist");
            }
        }),
        ("Truth texts 1180: the manifest (repo.json's description) and SECURITY say the base is read without an account and list every exchange file", () =>
        {
            using var manifest = JsonDocument.Parse(Read("src/TenshoAssistant/TenshoAssistant.json"));
            var description = manifest.RootElement.GetProperty("Description").GetString() ?? "";
            Check(description.Contains("without any account", StringComparison.Ordinal) && description.Contains("exact spot", StringComparison.Ordinal) && description.Contains("SHA-256", StringComparison.Ordinal), "manifest: base without account, exact spot, desktop app checked");
            Check(!description.Contains("Nothing about your character is sent", StringComparison.Ordinal), "manifest: no « nothing about your character »");
            var security = Read("SECURITY.md");
            Check(security.Contains("`community-v3.json`", StringComparison.Ordinal) && security.Contains("`github.lock`", StringComparison.Ordinal), "SECURITY lists every exchange file");
            Check(!security.Contains("secret gist", StringComparison.Ordinal) && !security.Contains("gist secret", StringComparison.Ordinal), "SECURITY: no « secret » gist");
        }),
    ];
}
