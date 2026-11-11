// 0.1052 (audit A065): the 0.323 drafts of the GitHub workflows (publish/github) are gone; the active files live in .github/ and no
// public document points to the old folder.
internal static class PublishDrafts1052Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("publish/github: removed, the active workflows are in .github",() =>
        {
            Check(!Directory.Exists(Path.GetFullPath(Path.Combine("publish","github"))),"publish/github is gone (git history keeps it)");
            foreach(var file in new[]{"workflows/build.yml","workflows/CodeQL.yml","dependabot.yml"})
                Check(File.Exists(Path.GetFullPath(Path.Combine(".github",file))),"active GitHub file: .github/"+file);
        }),
        ("publish/github: no public document or generator points to it",() =>
        {
            foreach(var file in new[]{"README.md","CONTRIBUTING.md","docs/README.md","tools/docgen.py",".github/workflows/build.yml",".github/dependabot.yml"})
                Check(!File.ReadAllText(Path.GetFullPath(file)).Contains("publish/github"),file+" still points to publish/github");
        }),
    ];
}
