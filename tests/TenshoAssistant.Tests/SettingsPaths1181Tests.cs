using System.Text.Json;

// 0.1181 (X-251, « first player » review of 12/11, B3): the source switches are in « Settings › Data and sources » since X-100 (0.999), not in
// « Advanced ». Every « Settings › X » a player reads (texts in English and French, the manifest that becomes repo.json, SECURITY, CONTRIBUTING,
// README) names a tab the Settings page has; Dalamud's own settings and github.com's are left out.
internal static class SettingsPaths1181Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    /// <summary>The places where a cited path names a tab that does not exist, for one language's prefix and tab names.</summary>
    internal static IReadOnlyList<string> Wrong(string text, string prefix, IReadOnlyList<string> tabs, IReadOnlyList<string> others)
    {
        var wrong = new List<string>();
        for (var at = text.IndexOf(prefix, StringComparison.Ordinal); at >= 0; at = text.IndexOf(prefix, at + prefix.Length, StringComparison.Ordinal))
        {
            var before = text[Math.Max(0, at - 14)..at];
            if (before.EndsWith("github.com › ", StringComparison.Ordinal)) continue;
            var rest = text[(at + prefix.Length)..];
            if (others.Any(other => rest.StartsWith(other, StringComparison.Ordinal))) continue;
            if (!tabs.Any(tab => rest.StartsWith(tab, StringComparison.Ordinal))) wrong.Add(rest[..Math.Min(50, rest.Length)]);
        }
        return wrong;
    }

    private static IReadOnlyList<string> Tabs(string language)
    {
        using var document = JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json"));
        var sections = document.RootElement.GetProperty("SettingsSections");
        // The tabs of DrawSettings (MainWindow.Settings.cs), Debug left out: it shows only in developer mode.
        return new[] { "General", "Panels", "Map", "Tracking", "Data", "Advanced" }.Select(key => sections.GetProperty(key).GetString() ?? "").ToArray();
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Settings paths 1181: a path naming no tab is found, Dalamud's and GitHub's are left out", () =>
        {
            var tabs = new[] { "General", "Data and sources", "Advanced" };
            Check(Wrong("in Settings › Advanced.", "Settings › ", tabs, ["Experimental"]).Count == 0, "a real tab");
            Check(Wrong("in Settings › Data.", "Settings › ", tabs, []).Count == 1, "« Data » alone is not the tab « Data and sources »");
            Check(Wrong("on github.com › Settings › Applications", "Settings › ", tabs, []).Count == 0, "github.com's settings");
            Check(Wrong("Settings › Experimental › Custom Plugin Repositories", "Settings › ", tabs, ["Experimental"]).Count == 0, "Dalamud's settings");
            Check(Wrong("Settings › Modules", "Settings › ", tabs, []).Count == 1, "an old tab");
        }),
        ("Settings paths 1181: every « Settings › X » of the texts, the manifest and the documents names a tab of the Settings page", () =>
        {
            var english = Tabs("en"); var french = Tabs("fr");
            Check(english.Contains("Data and sources") && french.Contains("Données et sources"), "tab names read");
            var wrong = new List<string>();
            foreach (var (path, prefix, tabs) in new[] { ("src/TenshoAssistant/Localization/Languages/en.json", "Settings › ", english), ("src/TenshoAssistant/Localization/Languages/fr.json", "Paramètres › ", french),
                ("SECURITY.md", "Settings › ", english), ("SECURITY.md", "Paramètres › ", french), ("CONTRIBUTING.md", "Settings › ", english), ("CONTRIBUTING.md", "Paramètres › ", french),
                ("README.md", "Settings › ", english), ("README.md", "Paramètres › ", french), ("docs/FAQ.md", "Settings › ", english), ("docs/FAQ.md", "Paramètres › ", french) })
                wrong.AddRange(Wrong(Read(path), prefix, tabs, ["Experimental", "Expérimental"]).Select(rest => path + ": " + rest));
            using var manifest = JsonDocument.Parse(Read("src/TenshoAssistant/TenshoAssistant.json"));
            wrong.AddRange(Wrong(manifest.RootElement.GetProperty("Description").GetString() ?? "", "Settings > ", english, []).Select(rest => "manifest: " + rest));
            Check(wrong.Count == 0, "paths naming no tab: " + string.Join(" | ", wrong));
        }),
        ("Settings paths 1181: the source switches are drawn in « Data and sources »", () =>
        {
            var settings = Read("src/TenshoAssistant/MainWindow.Settings.cs");
            Check(settings.Contains("SettingsTab(\"Data\",DrawDataSettings);", StringComparison.Ordinal) && settings.Contains("DrawSourceSettings();", StringComparison.Ordinal), "sources in the Data tab");
            Check(!Read("src/TenshoAssistant/Localization/Languages/en.json").Contains("Settings › Advanced › Community sources", StringComparison.Ordinal), "no community sources under Advanced");
        }),
    ];
}
