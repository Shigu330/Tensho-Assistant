// 0.1578 (X-629): after the single page header of 0.1573 (X-600), four language keys had no use left (CraftPresets.Back, CraftPresets.Title,
// CraftLiveUi.Title, GpUi.Title): taken out of every language, and no source asks for them.
internal static class UnusedKeys1578Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly (string Section, string Key)[] Gone = [("CraftPresets", "Back"), ("CraftPresets", "Title"), ("CraftLiveUi", "Title"), ("GpUi", "Title")];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Unused keys 1578: the four keys are in no language and no source", () =>
        {
            foreach (var language in new[] { "en", "fr", "de", "ja" })
            {
                using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/" + language + ".json")));
                foreach (var (section, key) in Gone)
                    Check(!(document.RootElement.TryGetProperty(section, out var keys) && keys.TryGetProperty(key, out _)), language + ": " + section + "." + key + " is gone");
            }
            var sources = Directory.EnumerateFiles(Path.GetFullPath("src"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));
            foreach (var path in sources)
            {
                var text = File.ReadAllText(path);
                foreach (var (section, key) in Gone)
                    Check(!text.Contains("\"" + section + "." + key + "\"", StringComparison.Ordinal), path + " asks for " + section + "." + key);
            }
        }),
    ];
}
