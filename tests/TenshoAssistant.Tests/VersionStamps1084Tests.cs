// 0.1084 (audit A060, D-138): the living documents carry a review date, never a hand-written « state at 0.xxx » stamp; docgen
// refuses one in the documents it scans.
internal static class VersionStamps1084Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly string[] Living = ["docs/project/ARCHITECTURE.md","docs/project/ROADMAP.md","docs/project/PROJECT.md",
        "docs/project/PUBLISHING.md","docs/project/REFERENCES.md"];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Living documents: no « state at 0.xxx » stamp",() =>
        {
            var stamp = new System.Text.RegularExpressions.Regex(@"\bversion\s+0\.\d{3,4}\b|\b(?:checked|vérifié|relu|relue|relues|state at|état au)\b[^.\n]{0,60}\b0\.\d{3,4}\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (var path in Living)
            {
                var text = File.ReadAllText(Path.GetFullPath(path));
                var found = stamp.Match(text);
                Check(!found.Success,path + ": version stamp « " + found.Value + " »");
            }
            var testing = File.ReadAllText(Path.GetFullPath("docs/project/TESTING.md"));
            Check(testing.Contains("## Comment vérifier (relu le 2026-11-04)"),"TESTING heading carries its date only");
        }),
        ("docgen: the stamp check covers the living documents",() =>
        {
            var docgen = File.ReadAllText(Path.GetFullPath("tools/docgen.py"));
            Check(docgen.Contains("state at|état au"),"docgen knows « state at / état au »");
            foreach (var path in Living)
                Check(docgen.Contains("\"" + path + "\""),"docgen scans " + path);
        }),
    ];
}
