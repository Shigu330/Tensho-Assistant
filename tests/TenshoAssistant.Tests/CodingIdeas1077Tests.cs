// 0.1077 (audit A055, rest): the « ideas spotted while coding » of G-PROPOSALS give their real state in both halves (five done, one
// not), and the code still holds what the done ones claim.
internal static class CodingIdeas1077Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("G-PROPOSALS: coding ideas with their state, checked against the code",() =>
        {
            var doc = File.ReadAllText(Path.GetFullPath("docs/project/G-PROPOSALS.md")).Replace("\r\n","\n");
            Check(!doc.Contains("(not done)") && !doc.Contains("(non faites)"),"the section no longer says « not done »");
            Check(doc.Contains("| Spotted in | Idea | State |") && doc.Contains("| Repérée en | Idée | État |"),"a State column in both halves");
            foreach (var v in new[]{"0.605","0.606","0.607","0.609","0.767"})
                Check(doc.Contains("| Done in " + v) && doc.Contains("| Fait en " + v),$"idea done in {v}, both halves");
            Check(doc.Contains("| Not done: needs one more Universalis call") && doc.Contains("| Pas fait : demande un appel Universalis de plus"),"the undercut idea stays open");
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            Check(en.Contains("\"RetainerGil\": \"(+{0} with retainers)\"") && en.Contains("\"OtherCharacters\": \"On your other characters:\"") && en.Contains("\"ListTitle\": \"Annotated items ({0})\""),"the texts the done ideas cite still exist");
        }),
    ];
}
