using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Help;
using TenshoAssistant.UI;

// 0.1683 (X-705, user choice of 21/11, D-201 b): in the grouped side bar (X-601), « Alarmes » becomes « Alerte » and « Suivi » becomes
// « Calendrier », the game's own words (ExtraCommand 3 and MainCommand 5, docs/evidence/0.1677-game-names.md); the other languages take the
// same sheet rows. The category's key stays « Tracking »: a fold saved by an older version is kept.
internal static class SideBarNames1683Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static Dictionary<string, string> Texts(string code)
    {
        var values = new Dictionary<string, string>();
        using var document = JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{code}.json").TrimStart('﻿'));
        void Walk(JsonElement element, string prefix)
        {
            foreach (var key in element.EnumerateObject())
                if (key.Value.ValueKind == JsonValueKind.Object) Walk(key.Value, prefix + key.Name + ".");
                else if (key.Value.ValueKind == JsonValueKind.String) values[prefix + key.Name] = key.Value.GetString()!;
        }
        Walk(document.RootElement, "");
        return values;
    }

    /// <summary>Language, the game's word for the alarm (ExtraCommand 3) and for its timers window (MainCommand 5), read with HelpTextProbe « names ».</summary>
    private static readonly (string Code, string Alarm, string Timers)[] Game =
    [
        ("fr", "Alerte", "Calendrier"),
        ("en", "Alarm", "Timers"),
        ("de", "Alarm", "Timer"),
        ("ja", "アラーム", "コンテンツ情報一覧"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Side bar names 1683: « Alerte » and « Calendrier » in the side bar, from the same sheet rows in every language, cited in the evidence", () =>
        {
            foreach (var (code, alarm, timers) in Game)
            {
                var texts = Texts(code);
                Check(texts.TryGetValue("Navigation.Alarms", out var entry) && entry == alarm + " ({0})", $"{code} Navigation.Alarms: « {entry} », the game says « {alarm} » (ExtraCommand 3)");
                // The categories of the side bar are written in capitals in every language (« WORKSPACE », « MODULE »…).
                Check(texts.TryGetValue("Navigation.CategoryTracking", out var category) && category == timers.ToUpper(CultureInfo.GetCultureInfo(code)), $"{code} Navigation.CategoryTracking: « {category} », the game says « {timers} » (MainCommand 5)");
            }
            var evidence = Read("docs/evidence/0.1677-game-names.md");
            Check(evidence.Contains("| ExtraCommand 3 | Alerte | Alarm |") && evidence.Contains("| MainCommand 5 | Calendrier | Timers |"), "the rows cited in both halves of the evidence");
            Check(Regex.Matches(evidence, @"\| ExtraCommand 3 \| Alerte \| Alarm \|").Count == 2 && Regex.Matches(evidence, @"\| MainCommand 5 \| Calendrier \| Timers \|").Count == 2, "English and French halves");
        }),
        ("Side bar names 1683: the page's own title, its settings and the tool list say the same name, and no text sends to the old one", () =>
        {
            foreach (var (code, alarm) in new[] { ("fr", "Alerte"), ("en", "Alarm") })
            {
                var texts = Texts(code);
                foreach (var key in new[] { "Alarms.PageTitle", "Alarms.SettingsTitle", "PageHelp.AlarmsTitle", "SettingsCategories.Alarms", "ToolDirectory.Alarms.Name" })
                    Check(texts.TryGetValue(key, out var shown) && shown == alarm, $"{code} {key}: « {shown} »");
            }
            var old = new Regex(@"(page|module|outil) Alarmes|Suivi ›|dans Alarmes\.|\(Alarmes\)");
            foreach (var (key, text) in Texts("fr")) Check(!old.IsMatch(text), "fr " + key + " still names the old page: " + text);
            var oldEn = new Regex(@"Alarms (page|module|tool)|Tracking ›|in Alarms\.|\(Alarms\)");
            foreach (var (key, text) in Texts("en")) Check(!oldEn.IsMatch(text), "en " + key + " still names the old page: " + text);
        }),
        ("Side bar names 1683: the quick search finds the page by its new name; the fold saved under « Tracking » is kept", () =>
        {
            var french = Texts("fr");
            var pages = new[] { ("Alarms", string.Format(CultureInfo.InvariantCulture, french["Navigation.Alarms"], 3)), ("Reminders", french["Navigation.Reminders"]), ("Garden", french["Navigation.Garden"]) };
            var hits = QuickSearch.Top(pages, page => page.Item2, QuickSearch.Tokens("alerte"));
            Check(hits.Count == 1 && hits[0].Item1 == "Alarms", "« alerte » finds the page: " + string.Join(", ", hits.Select(hit => hit.Item2)));
            Check(QuickSearch.Top(pages, page => page.Item2, QuickSearch.Tokens("ALERTE")).Count == 1, "any case");
            // The category's key is not its label: the saved fold « Tracking » stays, only the shown name changes.
            Check(PageFamilies.SideBarCategories.Contains("Tracking") && PageFamilies.KeepFolded(new HashSet<string> { "Tracking" }, PageFamilies.SideBarCategories) is { Count: 1 }, "the saved fold kept");
            var list = Read("src/TenshoAssistant/MainWindow.Pages.cs");
            foreach (var page in new[] { "Alarms", "Reminders", "Garden", "FreeCompany" })
                Check(list.Contains($"new(Page.{page},\"Tracking\","), page + " stays in the « Tracking » category (its key)");
            Check(Read("src/TenshoAssistant/MainWindow.cs").Contains("NavigationCategory(group.Category,group.Pages)")
                  && Read("src/TenshoAssistant/UI/Navigation/MainWindow.CategoryNavigation.cs").Contains("localization.Get(\"Navigation.Category\"+key)}###NavCategory{key}"),
                "the category's label is read from the language files by its key, its id (and its fold) stays the key");
        }),
    ];
}
