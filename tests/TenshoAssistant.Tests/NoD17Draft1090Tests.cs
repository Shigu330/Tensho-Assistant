// 0.1090 (audit A080, D-138): publish/d17 only served a submission to the official repository (DEC-039: none); it is removed. What
// the custom repository uses stays: the plugin icon, repo.json and its IconUrl, the release script, the CI build check.
internal static class NoD17Draft1090Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Submission draft: publish/d17 removed, the custom repository keeps what it uses",() =>
        {
            Check(!Directory.Exists(Path.GetFullPath("publish/d17")),"publish/d17 is removed (git history keeps it)");
            Check(File.Exists(Path.GetFullPath("src/TenshoAssistant/images/icon.png")),"the plugin icon stays");
            Check(Read("repo.json").Contains("/main/src/TenshoAssistant/images/icon.png"),"repo.json points to the plugin icon");
            Check(Read(".github/workflows/build.yml").Contains("name: Build like the Dalamud repository"),"the CI build check stays");
            foreach (var path in new[] { "README.md","docs/README.md","docs/project/PUBLISHING.md","tools/docgen.py","scripts/Release.ps1","scripts/Verify.ps1" })
                Check(!Read(path).Contains("publish/d17/") || path == "docs/project/PUBLISHING.md","no pointer into publish/d17: " + path);
            Check(!Read("docs/project/PUBLISHING.md").Contains("`publish/d17/"),"PUBLISHING names no file of the removed draft");
        }),
    ];
}
