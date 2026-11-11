// 0.1100 (relais 21): the in-game test campaign keeps only what helps the code. An open box of TESTING.md that only says there is
// nothing to do or nothing to see, or a single stop of the guided tour (tested as a whole, and by GuideVisit1045Tests), does not come back.
using System.Text.RegularExpressions;

internal static class TestingTriage1100Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Testing campaign: no empty box, no single stop of the guided tour",() =>
        {
            var lines = File.ReadAllLines(Path.GetFullPath("docs/project/TESTING.md"));
            var empty = new Regex(@"^- \[ \] (\([0-9.]+\) )?(Rien à faire\.?|Rien de visible\.?|Rien à essayer en jeu\.?|Aucun essai.*|Visite guidée, arrêt .*)$");
            var bad = lines.Where(l => empty.IsMatch(l.TrimEnd())).ToList();
            Check(bad.Count == 0,"empty or single-stop boxes are back in TESTING.md: " + string.Join(" | ",bad.Take(3)));
            Check(lines.Count(l => l.StartsWith("- [ ] ",StringComparison.Ordinal)) > 500,"TESTING.md still holds the campaign");
            Check(lines.Any(l => l.Contains("Prise en main : relancer le guide",StringComparison.Ordinal)),"the guided tour is still tested as a whole");
        }),
    ];
}
