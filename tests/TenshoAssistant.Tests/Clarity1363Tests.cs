// X-379 (0.1363, X-356 review, D-160): clarity. A ratchet: the unclear phrasings the review found never come back; the names they now cite
// are the game's or the label they point to.
internal static class Clarity1363Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Clarity 1363: the unclear phrasings are gone, in both languages", () =>
        {
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach (var phrase in new[] { "sans compte dedans", "Demande encore une fois.", "Elle ne fait que lire et afficher.", "Le « ? » garde l'aide.",
                "il vaut celui des actions", "Il lit la liste et ne change rien.", "à part des icônes", "identité de lieu conservée", "enveloppe non prise en charge",
                "portez-le pour calculer", "les objets qui apprennent les autres", "Plus d'expérience à gagner", "Pour préparer dehors", "monstre dont se dresse" })
                Check(!fr.Contains(phrase, StringComparison.Ordinal), "French: « " + phrase + " »");
            var en = Read("src/TenshoAssistant/Localization/Languages/en.json");
            foreach (var phrase in new[] { "without an account in them", "Asks once more.", "It only reads and shows.", "The « ? » keeps the help.", "equals the actions' one",
                "It reads the list and changes nothing.", "apart from the icons", "location identity kept", "envelope not supported", "wear it to compute",
                "the items that teach the others", "(see the log)", "Without this box," })
                Check(!en.Contains(phrase, StringComparison.Ordinal), "English: « " + phrase + " »");
        }),
        ("Clarity 1363: cited names are the game's or the label's", () =>
        {
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            var en = Read("src/TenshoAssistant/Localization/Languages/en.json");
            Check(fr.Contains("Calme intérieur requis pour Bénédiction de Byregot.", StringComparison.Ordinal) && en.Contains("Inner Quiet required for Byregot's Blessing.", StringComparison.Ordinal),
                "the action's full name (CraftAction 100009)");
            Check(fr.Contains("(« Pas de son en mission » s'applique aussi)", StringComparison.Ordinal) && fr.Contains("\"MuteInDuties\": \"Pas de son en mission\"", StringComparison.Ordinal)
                && en.Contains("(« No sound in duties » applies too)", StringComparison.Ordinal) && en.Contains("\"MuteInDuties\": \"No sound in duties\"", StringComparison.Ordinal),
                "the setting quoted by its label");
            var manifest = Read("src/TenshoAssistant/TenshoAssistant.json");
            Check(!System.Text.RegularExpressions.Regex.IsMatch(manifest, @"[a-z,]\\n[a-z]"), "the manifest's description keeps line breaks between paragraphs only");
        }),
    ];
}
