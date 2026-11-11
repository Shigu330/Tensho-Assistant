using System.Text.RegularExpressions;

// 0.1093 (audit A070, D-138): CHANGELOG.md keeps the last release and every later version (the release notes cover them); older entries
// are in docs/changelog/, one file per hundred of versions. All() gives the whole history, newest first, to the tests that read it.
internal static class ChangelogSplit1093Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly Regex Heading = new(@"^## (\d+\.\d+\.\d+)\s*$",RegexOptions.Multiline);

    private static IEnumerable<string> Archives() =>
        Directory.EnumerateFiles(Path.GetFullPath("docs/changelog"),"CHANGELOG-*.md")
            .OrderByDescending(path => int.Parse(Regex.Match(Path.GetFileName(path),@"CHANGELOG-0\.(\d+)-").Groups[1].Value));

    internal static string All() =>
        string.Join("\n",new[] { Path.GetFullPath("CHANGELOG.md") }.Concat(Archives()).Select(path => File.ReadAllText(path).Replace("\r\n","\n")));

    private static List<Version> Versions(string text) => Heading.Matches(text.Replace("\r\n","\n")).Select(m => Version.Parse(m.Groups[1].Value)).ToList();

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("CHANGELOG: the last release and later versions on top, older ones in docs/changelog",() =>
        {
            var main = Versions(File.ReadAllText(Path.GetFullPath("CHANGELOG.md")));
            var repo = File.ReadAllText(Path.GetFullPath("repo.json"));
            var released = Version.Parse(string.Join(".",Regex.Match(repo,@"""AssemblyVersion"":\s*""([\d.]+)""").Groups[1].Value.Split('.').Take(3)));
            Check(main.Count > 0 && main.Min()! <= released,"CHANGELOG.md keeps the released version " + released + " (oldest: " + main.Min() + ")");
            Check(main.Count < 200,"CHANGELOG.md stays short: " + main.Count + " entries");
            var all = Versions(All());
            Check(all.Count > 1000,"the whole history is kept: " + all.Count);
            Check(all.Distinct().Count() == all.Count,"no version twice between CHANGELOG.md and the archive");
            foreach (var path in Archives())
            {
                var text = File.ReadAllText(path);
                Check(Versions(text).All(v => v < released),Path.GetFileName(path) + " holds only versions older than the last release");
                Check(!Regex.IsMatch(text,@"\]\((?:docs|src|tests|tools|scripts)/"),Path.GetFileName(path) + ": links are relative to docs/changelog");
            }
            var header = File.ReadAllText(Path.GetFullPath("CHANGELOG.md"));
            Check(header.Contains("[docs/changelog/](docs/changelog/)"),"the header points to the archive");
            Check(File.ReadAllText(Path.GetFullPath("scripts/Verify.ps1")).Contains("tools\\changelog.py') check"),"Verify -Release checks the cut");
        }),
    ];
}
