// X-378 (0.1362, X-356 review, D-160): one name per notion in fr.json and en.json, the game's official name when there is one (D-161).
// A ratchet: the variants the review found never come back.
internal static class Coherence1362Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Coherence 1362: the French variants the review found are gone", () =>
        {
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach (var variant in new[] { "info-bulle", "Info-bulle", "page Cartes", "barre serveur", "barre d'informations du serveur", "barre d'infos serveur",
                "liste de fabrication", "Par défaut : coché", "Évènement", "Sacoche de chocobo", "dans la discussion", "Remise quotidienne", "Remise hebdomadaire",
                "nourriture et remède", "Les 10 :", "les jugements de la Fashion Report", "\"Debug\": \"Debug\"" })
                Check(!fr.Contains(variant, StringComparison.Ordinal), "French: « " + variant + " »");
            Check(fr.Contains("\"Debug\": \"Débogage\"", StringComparison.Ordinal), "the Debug tab says « Débogage », as the texts that name it");
            Check(fr.Contains("\"ColumnCurrency\": \"Monnaie\"", StringComparison.Ordinal), "« monnaie » kept for the allied societies (D-161)");
        }),
        ("Coherence 1362: the English variants the review found are gone; official names of the game", () =>
        {
            var en = Read("src/TenshoAssistant/Localization/Languages/en.json");
            foreach (var variant in new[] { "server info bar", "Maps page", "sub-recipe", "\"Ocean fishing", "\"Hunting log", "\"Deep dungeons\"", "wish list",
                "Erase this read", "Click: map.", "\"<\" raises", "“", "Compare the next moves" })
                Check(!en.Contains(variant, StringComparison.Ordinal), "English: « " + variant + " »");
            Check(en.Contains("\"OceanFishing\": \"Ocean Fishing\"", StringComparison.Ordinal) && en.Contains("\"HuntingLog\": \"Hunting Log\"", StringComparison.Ordinal)
                && en.Contains("\"DeepDungeons\": \"Deep Dungeons\"", StringComparison.Ordinal), "page names as the game writes them");
            Check(en.Contains("\"Param24\": \"Magic Defense\"", StringComparison.Ordinal), "the game's attribute name kept (BaseParam)");
        }),
        ("Coherence 1362: documents follow the same names", () =>
        {
            var readme = Read("README.md");
            Check(readme.StartsWith("# Tensho Assistant", StringComparison.Ordinal) && !readme.Contains("Preferences › Credits", StringComparison.Ordinal)
                && !readme.Contains("Préférences › Crédits", StringComparison.Ordinal), "README: the plugin's name, Credits under References");
            var contributing = Read("CONTRIBUTING.md");
            Check(contributing.Contains("`/xlsettings` › Expérimental › Dépôts de plugins personnalisés", StringComparison.Ordinal), "CONTRIBUTING: Dalamud's French labels");
            var languages = Read("src/TenshoAssistant/Localization/README.md");
            Check(!languages.Contains("→", StringComparison.Ordinal) && languages.Contains("Paramètres › Débogage", StringComparison.Ordinal), "the languages README: › and the tab's name");
        }),
    ];
}
