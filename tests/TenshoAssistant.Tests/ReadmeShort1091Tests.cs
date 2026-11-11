// 0.1091 (audit A078, D-138): the README keeps one short « Features » section per language (the details live in each page's help in the
// plugin); no « Notes » or « Summary » filler; the page table, the credits and the bilingual single file (A079) stay.
internal static class ReadmeShort1091Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("README: short, one Features section per language, credits kept",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("README.md")).Replace("\r\n","\n");
            var half = text.IndexOf("\n---\n\n# TenshoAssistant\n",StringComparison.Ordinal);
            Check(half > 0,"one bilingual README (A079)");
            var en = text[..half];
            var fr = text[half..];
            Check(en.Split('\n').Length <= 250 && fr.Split('\n').Length <= 250,"each half stays short: " + en.Split('\n').Length + " / " + fr.Split('\n').Length + " lines");
            Check(en.Contains("## Features\n") && fr.Contains("## Fonctions\n"),"one Features section per language");
            Check(!en.Contains("## Main feature domains") && !fr.Contains("## Domaines fonctionnels présents"),"no hand-written feature list");
            Check(!en.Contains("## Notes") && !en.Contains("## Summary") && !fr.Contains("## Notes") && !fr.Contains("## Résumé"),"no Notes or Summary filler");
            Check(en.Contains("<!-- docgen:pages-en -->") && fr.Contains("<!-- docgen:pages-fr -->"),"the produced page tables stay");
            Check(en.Contains("## Credits") && fr.Contains("## Crédits"),"the credits stay (licence notices)");
        }),
    ];
}
