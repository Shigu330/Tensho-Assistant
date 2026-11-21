// X-752 (the user's idea I-043, chosen on 21/11/2026): the sources the user proposed on 18/11 are surveyed in writing in REFERENCES (licence and
// terms, what each would add, network), in both halves; nothing is used and no network source is added (R04: the user's agreement first).
internal static class Sources1763Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // From a heading to the next heading of its level or above.
    private static string Section(string text, int start) =>
        text[start..new[] { text.IndexOf("\n### ", start + 1, StringComparison.Ordinal), text.IndexOf("\n## ", start, StringComparison.Ordinal) }.Where(at => at > 0).Min()];

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Sources 1763: the four sources proposed by the user are surveyed in REFERENCES, both halves, with their licence, what they add and the network", () =>
        {
            var references = Read("docs/project/REFERENCES.md");
            var english = references.IndexOf("### Sources proposed by the user (X-752, read on 22/11/2026)", StringComparison.Ordinal);
            var french = references.IndexOf("### Sources proposées par l'utilisateur (X-752, lues le 22/11/2026)", StringComparison.Ordinal);
            Check(english > 0 && french > english, "both halves, English first");
            foreach (var start in new[] { english, french })
            {
                var part = Section(references, start);
                Check(new[] { "ff14fish.carbuncleplushy.com", "MIT", "xivapi/ffxiv-datamining", "fr.ff14angler.com", "FF14AnglerRep", "Community Resources", "GNU FDL", "R04" }
                    .All(word => part.Contains(word, StringComparison.Ordinal)), "the four sources, their licence and R04: " + part[..80]);
                Check(part.Split('\n').Count(line => line.StartsWith("| ", StringComparison.Ordinal)) == 5, "a header and four rows");
            }
            foreach (var host in new[] { "carbuncleplushy", "ff14angler", "ffxiv-datamining" })
                Check(!Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories).Any(file => File.ReadAllText(file).Contains(host, StringComparison.OrdinalIgnoreCase)), "no request to " + host);
        }),
        ("Sources 1764: the other tools of the Community Resources page on the user's subjects are surveyed too (X-753), both halves, ten rows, nothing used", () =>
        {
            var references = Read("docs/project/REFERENCES.md");
            var english = references.IndexOf("### Other sources of the « Community Resources » page (X-753, read on 22/11/2026)", StringComparison.Ordinal);
            var french = references.IndexOf("### Autres sources de la page « Community Resources » (X-753, lues le 22/11/2026)", StringComparison.Ordinal);
            Check(english > 0 && french > english, "both halves, English first");
            foreach (var start in new[] { english, french })
            {
                var part = Section(references, start);
                Check(new[] { "DDCompendium", "ffxiv-eureka.com", "tracker.infi.ovh", "ffxiv-occult.com", "ffxiv.oceanfishing.boats", "ffxiv.pf-n.co", "ffxivgardening.com", "mammets-fleet", "ffxivcollect.com", "lalachievements.com", "R04" }
                    .All(word => part.Contains(word, StringComparison.Ordinal)), "the ten sources and R04");
                Check(part.Split('\n').Count(line => line.StartsWith("| ", StringComparison.Ordinal)) == 11, "a header and ten rows");
            }
            foreach (var host in new[] { "ddcompendium", "ffxiv-eureka", "infi.ovh", "ffxiv-occult", "oceanfishing.boats", "pf-n.co", "ffxivgardening", "mammets-fleet", "ffxivcollect", "lalachievements" })
                Check(!Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories).Any(file => File.ReadAllText(file).Contains(host, StringComparison.OrdinalIgnoreCase)), "no request to " + host);
        }),
    ];
}
