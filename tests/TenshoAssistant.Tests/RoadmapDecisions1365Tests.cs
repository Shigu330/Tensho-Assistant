// X-398 (0.1365, review of 0.1362 to 0.1364, I2): the decisions block of ROADMAP's English half is English, and neither half
// quotes the user's chat or names the agent's files (D-164 in 0.1364: a French line with « on avait parlé d'un post reddit… »).
internal static class RoadmapDecisions1365Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Block(string text, string key)
    {
        var start = text.IndexOf("<!-- gen:decisions:" + key + " -->", StringComparison.Ordinal);
        var end = text.IndexOf("<!-- /gen:decisions:" + key + " -->", StringComparison.Ordinal);
        Check(start >= 0 && end > start, "ROADMAP: block decisions:" + key);
        return text[start..end];
    }

    // The text a row shows in its subject and applied-choice cells (the « Where » cell may name a French section of an old document).
    private static IEnumerable<(string Id, string Text)> Rows(string block) =>
        block.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("| D-", StringComparison.Ordinal))
            .Select(l => l.Split(" | ")).Select(c => (c[0].TrimStart('|', ' '), c.Length > 2 ? c[1] + " | " + c[2] : string.Join(" | ", c)));

    private static readonly string[] Internal = ["utilisateur", "l'agent", "the agent's", "PLAN-SYSTEME", "docs/ai", "relais ", "relay "];

    // French typography in an English cell: a space before « ; », « ? » or « : ».
    internal static string? FrenchMark(string text) =>
        new[] { " ; ", " ?", " : " }.FirstOrDefault(m => text.Contains(m, StringComparison.Ordinal));

    internal static string? InternalMark(string text) =>
        Internal.FirstOrDefault(m => text.Contains(m, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Roadmap decisions 1365: the English half's block is English, without internal words", () =>
        {
            var roadmap = File.ReadAllText(Path.GetFullPath("docs/project/ROADMAP.md")).Replace("\r\n", "\n");
            foreach (var (id, text) in Rows(Block(roadmap, "en")))
            {
                Check(FrenchMark(text) is null, id + " in English half: French typography « " + FrenchMark(text) + " »");
                Check(InternalMark(text) is null, id + " in English half: internal word « " + InternalMark(text) + " »");
            }
            foreach (var (id, text) in Rows(Block(roadmap, "fr")))
                Check(InternalMark(text) is null, id + " in French half: internal word « " + InternalMark(text) + " »");
        }),
        ("Roadmap decisions 1365: the check finds the 0.1364 line of D-164", () =>
        {
            const string old = "Annonce publique de Tensho : où la poster ? | Un post Reddit (rappel de l'utilisateur du 15/11 : « on avait parlé d'un "
                + "post reddit pour pas ennuyer dalamud ») : pas de message sur le Discord de Dalamud pour Tensho ; texte préparé par l'agent, posté "
                + "par l'utilisateur (Reddit et forums : PLAN-SYSTEME)";
            Check(FrenchMark(old) is not null && InternalMark(old) is not null, "the old D-164 line is caught");
            Check(FrenchMark("Public announcement of Tensho: where to post it? | A Reddit post; the text is prepared beforehand") is null
                && InternalMark("A Reddit post, posted by the plugin's author") is null, "an English line passes");
        }),
    ];
}
