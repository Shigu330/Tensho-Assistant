using System.Text.RegularExpressions;

// 0.1474 (X-487, rule R06): the game's own help is searched before any assumption or in-game reading. tools/HelpTextProbe indexes the
// help sheets of the client (read-only); the sheets it reads by default are the ones its evidence names, and the tools' README documents it
// in both languages. The index itself is Square Enix data: never in the repository, never in the plugin.
internal static class HelpTexts1474Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    /// <summary>The sheet names of the probe's default list (Sheets.Default in its Program.cs).</summary>
    public static string[] ProbeSheets(string program)
    {
        var start = program.IndexOf("public static readonly string[] Default =", StringComparison.Ordinal);
        Check(start >= 0, "the probe's default list");
        var block = program[start..program.IndexOf("];", start, StringComparison.Ordinal)];
        return Regex.Matches(block, "\"([A-Za-z0-9]+)\"").Select(m => m.Groups[1].Value).ToArray();
    }

    /// <summary>The sheet names of the evidence's « index » section (English half), comma separated after each family label.</summary>
    public static string[] EvidenceSheets(string evidence)
    {
        var english = evidence.Split("\n---\n")[0];
        var start = english.IndexOf("## The index", StringComparison.Ordinal);
        Check(start >= 0, "the evidence's index section");
        var section = english[start..english.IndexOf("\nMeasured on", start, StringComparison.Ordinal)];
        return Regex.Matches(section, @"\b([A-Z][A-Za-z0-9]+)(?=[,.])").Select(m => m.Groups[1].Value).ToArray();
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Help texts 1474: the probe reads the sheets its evidence names, each once", () =>
        {
            var probe = ProbeSheets(Read("tools/HelpTextProbe/Program.cs"));
            var evidence = EvidenceSheets(Read("docs/evidence/0.1474-help-texts.md"));
            Check(probe.Length == 50 && probe.Distinct().Count() == probe.Length, "50 sheets, each once: " + probe.Length);
            Check(probe.OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(evidence.OrderBy(s => s, StringComparer.Ordinal)),
                "the probe's list and the evidence's list agree: " + string.Join(",", probe.Except(evidence)) + " / " + string.Join(",", evidence.Except(probe)));
            foreach (var help in new[] { "DescriptionString", "HowToPage", "GuidePageString", "MultipleHelpString", "Addon" })
                Check(probe.Contains(help), "a help sheet is read: " + help);
        }),
        ("Help texts 1474: read-only, the index outside the repository, documented in both halves of the tools' README", () =>
        {
            var program = Read("tools/HelpTextProbe/Program.cs");
            Check(!program.Contains("File.Delete") && !program.Contains("WriteAllBytes") && !Regex.IsMatch(program, @"sqpack[^\n]*Write"), "nothing written next to the game");
            var readme = Read("tools/README.md").Split("\n---\n");
            Check(readme.Length == 2 && readme.All(half => half.Contains("| `HelpTextProbe` | X-487 (0.1474)")), "a row in each half");
            Check(readme[0].Contains("kept outside the repository") && readme[1].Contains("gardé hors du dépôt"), "the index stays out of the repository");
            Check(!Directory.Exists(Path.GetFullPath("tools/HelpTextProbe/index")), "no index in the repository");
        }),
    ];
}
