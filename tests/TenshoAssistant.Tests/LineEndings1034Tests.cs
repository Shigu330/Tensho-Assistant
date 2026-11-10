// 0.1034: GitHub's checkout writes the repository's text files with CRLF (.gitattributes: text=auto, Windows runner), the local one
// may write LF. A test that cuts a file it read on "\n" separators must normalise the line endings first, or it passes here and fails
// in CI (PluginUiTest1032Tests cut tools/PluginUiTest/README.md on "\n---\n": « Build and test » red from 0.1032 to 0.1033).
internal static class LineEndings1034Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    internal static readonly (string Name,Action Run)[] Cases =
    [
        ("Line endings: a test that cuts a file on \"\\n---\\n\" or \"\\nFrançais :\\n\" normalises CRLF first (CI checks out CRLF)",() =>
        {
            var cutters = new[]{"Split(\"\\n---\\n\")","IndexOf(\"\\n---\\n\"","IndexOf(\"\\nFrançais :\\n\""};
            var found = 0;
            foreach (var file in Directory.EnumerateFiles(Path.GetFullPath("tests/TenshoAssistant.Tests"),"*.cs"))
            {
                var text = File.ReadAllText(file);
                if (!cutters.Any(cut=>text.Contains(cut,StringComparison.Ordinal)) || Path.GetFileName(file) == "LineEndings1034Tests.cs") continue;
                found++;
                Check(text.Contains("Replace(\"\\r\\n\",\"\\n\")",StringComparison.Ordinal) || text.Contains("ReplaceLineEndings",StringComparison.Ordinal),
                    Path.GetFileName(file)+" cuts a file on a \"\\n…\\n\" separator without normalising CRLF");
            }
            Check(found >= 3,"the cutting tests are found (PluginUiTest1032, DesktopUpdate1033, Faq)");
        }),
        ("Line endings: the Plugin UI test README still cuts in two halves once checked out with CRLF",() =>
        {
            var readme = File.ReadAllText(Path.GetFullPath("tools/PluginUiTest/README.md")).Replace("\r\n","\n");
            var crlf = readme.Replace("\n","\r\n");
            Check(crlf.Replace("\r\n","\n").Split("\n---\n").Length == 2,"README: English, ---, French, whatever the line endings");
            Check(crlf.Split("\n---\n").Length == 1,"without normalising, CRLF hides the separator (the 0.1032 failure)");
        }),
    ];
}
