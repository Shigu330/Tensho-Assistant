// 0.1066 (audit A051): PROJECT and ROADMAP keep no hand-written copy of the page groups; they point, in both halves, to the README
// table that tools/docgen.py writes from MainWindow.Pages.cs.
internal static class PageLists1066Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Page lists: PROJECT and ROADMAP point to the README table written from the code",() =>
        {
            foreach(var document in new[]{"docs/project/PROJECT.md","docs/project/ROADMAP.md"})
            {
                var text = File.ReadAllText(Path.GetFullPath(document)).Replace("\r\n","\n");
                Check(text.Contains("Every page of the code, by group, is in the table of the [README](../../README.md) (« Overview »)"),document + ": English pointer");
                Check(text.Contains("Toutes les pages du code, par groupe, sont dans le tableau du [README](../../README.md) (« Vue d'ensemble »)"),document + ": French pointer");
                Check(!text.Contains("version 0.971) has these pages") && !text.Contains("| Group | Pages added |"),document + ": no hand-written page list left");
            }
            var readme = File.ReadAllText(Path.GetFullPath("README.md"));
            Check(readme.Contains("<!-- docgen:pages-en -->") && readme.Contains("<!-- docgen:pages-fr -->"),"the README table is produced");
        }),
    ];
}
