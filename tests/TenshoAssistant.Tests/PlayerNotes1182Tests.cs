using System.Text.RegularExpressions;

// 0.1182 (X-252, « first player » review of 12/11, B5 and G2): the AI disclosure says what is really done (built and tested, loaded once in
// game, detailed tests after); the notes a player reads (installer, GitHub release) are each version's « For players: » line, without the
// agent's identifiers; the release writes the produced indexes again before committing repo.json (system plan 4.23).
internal static class PlayerNotes1182Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    // What a player line must not hold: a plan or decision id, a review finding, a version number, developer mode, a sheet or type name.
    internal static readonly Regex AgentWords = new(@"\b(?:X|D|W|N|R|T|C|I|M|G|DEC)-\d+|\bN\d+\b|\b0\.\d{3,4}\b|developer mode|mode développeur|\b(?!GitHub\b|YouTube\b|PaissaDB\b|LuminaSupplemental\b|GatherBuddy\b)[A-Z][a-z]+[A-Z][A-Za-z]*\b|\b[A-Z]{3}[A-Z][a-z]+");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Player notes 1182: a player line with an identifier, a version, a sheet or developer mode is refused; plain words pass", () =>
        {
            Check(!AgentWords.IsMatch("Occult Crescent: a panel on the island; connect GitHub to send yours."), "plain words");
            foreach (var bad in new[] { "X-188 done", "finding N3", "since 0.1176", "in developer mode", "MKDSupportJob rows", "the DynamicEvent container", "DEC-041 applied" })
                Check(AgentWords.IsMatch(bad), "refused: " + bad);
        }),
        ("Player notes 1182: since the last release every version has its two player lines or none, in plain words", () =>
        {
            // 0.1297: CHANGELOG.md keeps only the versions since the last release (tools/changelog.py archive); the older ones are read in the
            // archives, each version once.
            var archives = Directory.Exists(Path.GetFullPath("docs/changelog"))
                ? Directory.GetFiles(Path.GetFullPath("docs/changelog"), "CHANGELOG-*.md").Select(path => File.ReadAllText(path).Replace("\r\n", "\n"))
                : Enumerable.Empty<string>();
            var lines = new[] { Read("CHANGELOG.md") }.Concat(archives).SelectMany(text => text.Split('\n')).ToArray();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var checkedVersions = 0;
            for (var index = 0; index < lines.Length; index++)
            {
                var head = Regex.Match(lines[index], @"^## 0\.(\d+)\.0$");
                if (!head.Success || int.Parse(head.Groups[1].Value) < 1178 || !seen.Add(head.Groups[1].Value)) continue;
                var end = index + 1;
                while (end < lines.Length && !lines[end].StartsWith("## ", StringComparison.Ordinal)) end++;
                var body = lines[(index + 1)..end];
                var english = body.Where(line => line.StartsWith("- For players: ", StringComparison.Ordinal)).ToArray();
                var french = body.Where(line => line.StartsWith("- Pour les joueurs : ", StringComparison.Ordinal)).ToArray();
                Check(english.Length == french.Length && english.Length <= 1, lines[index] + ": both player lines or none");
                foreach (var line in english.Concat(french)) Check(!AgentWords.IsMatch(line), lines[index] + ": agent words in « " + line + " »");
                checkedVersions++;
            }
            Check(checkedVersions >= 4, "versions read: " + checkedVersions);
        }),
        ("Player notes 1182: the release script gives the player lines to repo.json and the release, and writes the indexes before committing repo.json", () =>
        {
            var release = Read("scripts/Release.ps1");
            Check(Regex.Matches(release, @"\$notes = Get-PlayerNotesSince ").Count == 2, "player notes for a release and a resumed one");
            Check(release.Contains("'^- For players: (.+)$'", StringComparison.Ordinal) && release.Contains("'^- Pour les joueurs : (.+)$'", StringComparison.Ordinal), "the lines read");
            var docgen = release.IndexOf("Step \"docgen write --blocks (indexes with repo.json)\"", StringComparison.Ordinal);
            var commit = release.IndexOf("Step \"git commit repo.json\" { git add repo.json; git add -u;", StringComparison.Ordinal);
            Check(docgen > 0 && commit > docgen, "indexes written again before the repo.json commit");
        }),
        ("Player notes 1182: the AI disclosure says what is really done, and the README names no plan point", () =>
        {
            foreach (var path in new[] { "README.md", "CONTRIBUTING.md" })
            {
                var text = Read(path);
                Check(!text.Contains("once what changed is validated in game", StringComparison.Ordinal) && !text.Contains("une fois ce qui a changé validé en jeu", StringComparison.Ordinal), path + ": no « validated in game » promise");
                Check(text.Contains("loaded\nonce in game with its self-check", StringComparison.Ordinal) || text.Contains("loaded once in game with its self-check", StringComparison.Ordinal), path + ": what is done");
            }
            Check(!Read("README.md").Contains("(X-121)", StringComparison.Ordinal), "README: no plan point");
        }),
    ];
}
