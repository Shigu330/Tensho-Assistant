// X-752 (the user's idea I-043, chosen on 21/11/2026): the sources the user proposed on 18/11 are surveyed in writing in REFERENCES (licence and
// terms, what each would add, network), in both halves; nothing is used and no network source is added (R04: the user's agreement first).
internal static class Sources1763Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

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
                var part = references[start..references.IndexOf("\n## ", start, StringComparison.Ordinal)];
                Check(new[] { "ff14fish.carbuncleplushy.com", "MIT", "xivapi/ffxiv-datamining", "fr.ff14angler.com", "FF14AnglerRep", "Community Resources", "GNU FDL", "R04" }
                    .All(word => part.Contains(word, StringComparison.Ordinal)), "the four sources, their licence and R04: " + part[..80]);
                Check(part.Split('\n').Count(line => line.StartsWith("| ", StringComparison.Ordinal)) == 5, "a header and four rows");
            }
            foreach (var host in new[] { "carbuncleplushy", "ff14angler", "ffxiv-datamining" })
                Check(!Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories).Any(file => File.ReadAllText(file).Contains(host, StringComparison.OrdinalIgnoreCase)), "no request to " + host);
        }),
    ];
}
