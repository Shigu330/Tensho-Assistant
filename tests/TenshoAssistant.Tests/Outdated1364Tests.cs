// X-380 (0.1364, X-356 review, D-160): outdated texts and internal jargon players could see. A ratchet: each text below was checked
// against the code before being fixed (passage of relay 69) and the old wording never comes back.
internal static class Outdated1364Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Outdated 1364: what the code does now, in both languages", () =>
        {
            var en = Read("src/TenshoAssistant/Localization/Languages/en.json");
            foreach (var old in new[] { "Tending and harvesting are not detected yet", "Tending and harvest are not detected yet", "Tensho pauses inside them", "out of the dungeon: Tensho stops inside",
                "Eureka Moment and Priming Touch are not counted", "Settings → ", "Collections › Wishlist", "Advanced lists the online sources", "turned off in the advanced settings",
                "Reading a storage requires the framework thread", "Grand Company squadron and Ehcatl", "Storage › Sort page", "2026-11-05 21:00", "Map / world" })
                Check(!en.Contains(old, StringComparison.Ordinal), "English: « " + old + " »");
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach (var old in new[] { "L'entretien et la récolte ne sont pas encore détectés", "Tensho se met en pause à l'intérieur", "hors du donjon : Tensho se coupe dedans",
                "Toucher d'amorçage ne sont pas comptés", "Paramètres → ", "Collections › Souhaits", "Avancé liste les sources en ligne", "désactivés dans les réglages avancés",
                "exige le thread framework", "Escouade de Grande compagnie et livraisons", "page Rangements › Tri", "2026-11-05 21:00", "Carte / monde" })
                Check(!fr.Contains(old, StringComparison.Ordinal), "French: « " + old + " »");
            Check(en.Contains("[discordtime], [relative], [et], [zone], [item])", StringComparison.Ordinal) && fr.Contains("[discordtime], [relative], [et], [zone], [item])", StringComparison.Ordinal),
                "the group message lists every marker the alarms replace");
        }),
        ("Outdated 1364: no internal identifier in the public documents the review read", () =>
        {
            var security = Read("SECURITY.md");
            foreach (var id in new[] { "(R-12)", "X-121", "X-119", "X-175" }) Check(!security.Contains(id, StringComparison.Ordinal), "SECURITY: " + id);
            var roadmap = Read("docs/project/ROADMAP.md");
            Check(!roadmap.Contains("| D-151 |", StringComparison.Ordinal) && !roadmap.Contains("relais 47", StringComparison.Ordinal) && !roadmap.Contains("master prompt", StringComparison.Ordinal)
                && !roadmap.Contains("prompt maître", StringComparison.Ordinal), "ROADMAP: no relay, no master prompt, no internal decision");
            var publishing = Read("docs/project/PUBLISHING.md");
            Check(!publishing.Contains(", R04)", StringComparison.Ordinal) && !publishing.Contains("35 keys", StringComparison.Ordinal), "PUBLISHING: no rule number, the real key count");
            var template = Read(".github/pull_request_template.md");
            Check(!template.Contains("Dalamud's plugin policy asks", StringComparison.Ordinal) && template.Contains("This project discloses its AI use.", StringComparison.Ordinal), "the AI note no longer cites a retired policy");
            Check(!Read("community-discoveries/README.md").Contains("(X-183)", StringComparison.Ordinal), "shared discoveries README: no plan id");
        }),
    ];
}
