// 0.1413 (X-432): tools/docgen.py lists the files the next commit will hold: tracked ones and new ones not ignored (git ls-files --cached
// --others --exclude-standard). The evidence index (docs/evidence/README.md) and the document index read that list; a new CHANGELOG archive
// written by the same document pass used to be left out, and the release then stopped on « Documents not produced for this version ».
using System.Text.RegularExpressions;

internal static class DocsFresh1413Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Docgen() => File.ReadAllText(Path.GetFullPath("tools/docgen.py")).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Documents X-432: docgen lists the tracked files and the new ones not ignored, once each",() =>
        {
            var body=Regex.Match(Docgen(),@"def git_files\(pattern\):\n(.*?)\n\n\n",RegexOptions.Singleline);
            Check(body.Success,"tools/docgen.py: git_files is missing");
            var text=body.Groups[1].Value;
            foreach(var flag in new[]{"\"--cached\"","\"--others\"","\"--exclude-standard\""})
                Check(text.Contains(flag),$"git_files: {flag} is missing (a new file not yet committed would be left out of the indexes)");
            Check(text.Contains("dict.fromkeys("),"git_files: a file is listed once");
            Check(text.Contains(".exists()"),"git_files: a tracked file deleted in the working tree is still left out (0.1052)");
        }),
        ("Documents X-432: the evidence index and the document index read the list of git_files",() =>
        {
            var text=Docgen();
            var citations=Regex.Match(text,@"def evidence_citations\(\):\n(.*?)\n\n\n",RegexOptions.Singleline).Groups[1].Value;
            Check(citations.Contains("git_files(\"*\")"),"evidence_citations reads git_files");
            Check(Regex.Matches(text,@"subprocess\.run\(\[""git"", ""ls-files""").Count==1,"one way to list the files (git_files)");
        }),
    ];
}
