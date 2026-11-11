// 0.1057 (audit A085): the CHANGELOG header states its format (French, then "### English" since 0.219.0) and that the English part
// becomes the commit bodies and the release notes; it no longer sends the reader to the git commits for the detail.
internal static class ChangelogHeader1057Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Changelog header: format and readers stated, no circular pointer to the commits",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("CHANGELOG.md")).Replace("\r\n","\n");
            var header = text[..text.IndexOf("\n## ",StringComparison.Ordinal)];
            Check(header.Contains("`### English` (partie anglaise obligatoire depuis 0.219.0)"),"the header states the English rule");
            Check(header.Contains("corps\naux commits et aux notes de release"),"the header says the English part feeds the commits and the release notes");
            Check(!header.Contains("historique détaillé : commits git"),"no pointer to the commits for the detail");
        }),
    ];
}
