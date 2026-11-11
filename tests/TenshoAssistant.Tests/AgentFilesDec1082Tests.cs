// 0.1082 (audit A039, D-138): DEC-051 records that the agent's working files left git in 0.802 and that the agent commits;
// DEC-010 says it is amended.
internal static class AgentFilesDec1082Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("DECISIONS: DEC-051 in both halves, DEC-010 amended",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md")).Replace("\r\n","\n");
            var half = text.IndexOf("\n---\n",StringComparison.Ordinal);
            Check(half > 0,"DECISIONS has two halves");
            var en = text[..half];
            var fr = text[half..];
            Check(en.Contains("ID: DEC-051\n"),"DEC-051 in the English half");
            Check(fr.Contains("DEC-ID: DEC-051\n"),"DEC-051 in the French half");
            Check(en.Contains("amended by DEC-050 and DEC-051"),"DEC-010 consequences amended (English)");
            Check(fr.Contains("modifié par DEC-050 et DEC-051"),"DEC-010 consequences amended (French)");
            Check(!System.Text.RegularExpressions.Regex.IsMatch(text,@"docs/ai/|scripts/agent/|CLAUDE\.md"),"no path to the agent's files (R22)");
        }),
    ];
}
