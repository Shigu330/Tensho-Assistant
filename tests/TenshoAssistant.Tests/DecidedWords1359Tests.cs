using System.Text.RegularExpressions;

// 0.1359 (D-161 and D-162 decided by the user: « none, official name » and the straight apostrophe everywhere): the public documents
// outside the language files follow them too. Archives (docs/changelog) and proofs (docs/evidence) keep their text as written.
internal static class DecidedWords1359Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Decided words 1359: no curly apostrophe in the living Markdown documents (D-162)",() =>
        {
            var files=new List<string>{"README.md","SECURITY.md","CONTRIBUTING.md","CODE_OF_CONDUCT.md","CHANGELOG.md","docs/FAQ.md","tools/README.md",
                "community-discoveries/README.md","community-help/README.md","src/TenshoAssistant/Localization/README.md"};
            files.AddRange(Directory.EnumerateFiles(Path.GetFullPath("docs/project"),"*.md").Select(path => "docs/project/"+Path.GetFileName(path)));
            var found=files.Where(path => File.Exists(Path.GetFullPath(path)) && Read(path).Contains('’')).ToList();
            Check(found.Count==0,"curly apostrophe in "+string.Join(", ",found.Take(5)));
        }),
        ("Decided words 1359: SECURITY and tools/README say the game's words (devises, hauts faits, cibles d'élite, Grande compagnie; D-161)",() =>
        {
            var security=Read("SECURITY.md");
            Check(!Regex.IsMatch(security,@"\bmonnaies?\b") && !security.Contains("des succès") && !security.Contains("marques de chasse"),"SECURITY: devises, hauts faits, cibles d'élite");
            var tools=Read("tools/README.md");
            Check(!Regex.IsMatch(tools,@"\bmonnaies\b") && !tools.Contains("Grande Compagnie"),"tools/README: devises, Grande compagnie");
        }),
    ];
}
