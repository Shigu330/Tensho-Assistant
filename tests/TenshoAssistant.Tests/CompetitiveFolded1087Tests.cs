// 0.1087 (audit A062, D-138): the finished COMPETITIVE-OFF-PLAN was folded into ARCHITECTURE (both halves) and DEC-038 (D1 to D5),
// then removed; nothing tracked points to it any more (the CHANGELOG and the dated decisions of GLOBAL-PLAN keep its name as history;
// the link test covers GLOBAL-PLAN's links).
internal static class CompetitiveFolded1087Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Competitive plan: folded into ARCHITECTURE and DEC-038, then removed",() =>
        {
            Check(!File.Exists(Path.GetFullPath("docs/project/COMPETITIVE-OFF-PLAN.md")),"the plan is removed");
            var architecture = Read("docs/project/ARCHITECTURE.md");
            Check(architecture.Contains("### Competitive switch-off (`Competitive/`, DEC-038)") && architecture.Contains("### Coupure en JcJ et en contenu classé (`Competitive/`, DEC-038)"),"ARCHITECTURE has the section in both halves");
            Check(architecture.Contains("Known gaps:") && architecture.Contains("Écarts connus :"),"the known gaps are kept in both halves");
            var decisions = Read("docs/project/DECISIONS.md");
            Check(decisions.Contains("D4, the Wolves' Den dock switches off.") && decisions.Contains("D4, le quai de Wolves' Den se coupe."),"DEC-038 holds D1 to D5 in both halves");
            foreach (var path in new[] { "docs/project/DECISIONS.md","docs/project/IDS.md","docs/README.md",
                                         "docs/evidence/0.600-competitive-inventory.md","src/TenshoAssistant/Competitive/CompetitiveZones.cs" })
                Check(!Read(path).Contains("COMPETITIVE-OFF-PLAN"),path + " no longer points to the removed plan");
        }),
    ];
}
