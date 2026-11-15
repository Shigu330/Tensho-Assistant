using System.Text.RegularExpressions;

// X-399 (0.1377, review of 0.1362 to 0.1364, M1 to M4): a setting's label is quoted under its current name; a garden bed is an
// « emplacement » (the game's word); the server bar, the resets and the item level have one name each; « infobulle » in the documents too.
internal static class Vocabulary1377Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
    private static string Value(string json, string key) => Regex.Match(json, "\"" + key + "\": \"((?:[^\"\\\\]|\\\\.)*)\"").Groups[1].Value;

    /// <summary>The lines of one top-level section (two-space key) of a language file.</summary>
    private static string Section(string json, string name)
    {
        var start = Regex.Match(json, "^  \"" + name + "\": \\{", RegexOptions.Multiline);
        Check(start.Success, "section " + name);
        var end = Regex.Match(json[(start.Index + start.Length)..], "^  \\}", RegexOptions.Multiline);
        return json.Substring(start.Index, start.Length + end.Index);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Vocabulary 1377: the « game data only » setting is quoted under its label, in both languages", () =>
        {
            foreach (var language in new[] { "en", "fr" })
            {
                var json = Read("src/TenshoAssistant/Localization/Languages/" + language + ".json");
                var label = Value(json, "GameDataOnly");
                Check(label.Length > 0, language + ": the label");
                foreach (var key in new[] { "AnswerOffline", "NotoriousOff" })
                    Check(Value(json, key).Contains("« " + label + " »", StringComparison.Ordinal), language + ": " + key + " quotes « " + label + " »");
            }
        }),
        ("Vocabulary 1377: a garden bed is an « emplacement » in French", () =>
        {
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            Check(!Section(fr, "Garden").Contains("parcelle", StringComparison.OrdinalIgnoreCase), "Garden section");
            foreach (var key in new[] { "GardenPurpose", "GardenStart", "AnswerGardenText", "ReminderFinishedServerBarTip", "BarGarden" })
                Check(!Value(fr, key).Contains("parcelle", StringComparison.OrdinalIgnoreCase), key);
            Check(Value(fr, "EmptyBed") == "Emplacement {0} : vide", "a bed's own name");
            Check(Section(fr, "Places").Contains("\"Plot\": \"Parcelle\"", StringComparison.Ordinal), "a housing plot stays a « parcelle »");
        }),
        ("Vocabulary 1377: the server bar, the resets and the item level have one name, whatever the case", () =>
        {
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            var en = Read("src/TenshoAssistant/Localization/Languages/en.json");
            foreach (var variant in new[] { "barre d'informations", "remise quotidienne", "remise hebdomadaire", "Nv. d'objet" })
                Check(!fr.Contains(variant, StringComparison.OrdinalIgnoreCase), "French: « " + variant + " »");
            Check(!en.Contains("info bar", StringComparison.OrdinalIgnoreCase), "English: « info bar »");
        }),
        ("Vocabulary 1377: « infobulle » in the produced documents and their generator", () =>
        {
            foreach (var path in new[] { "tools/docgen.py", "docs/project/SETTINGS.md", "docs/README.md", "docs/FAQ.md" })
                Check(!Read(path).Contains("info-bulle", StringComparison.OrdinalIgnoreCase), path);
        }),
    ];
}
