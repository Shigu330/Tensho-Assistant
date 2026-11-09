// X-76 2b H7 (0.920): page status lines: data errors through DrawDataError, empty and loading lines through EmptyState, copy buttons
// through CopyButton. A ratchet: none of the hand-written forms comes back in MainWindow*.cs.
internal static class StatusLines920Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Status lines 920: no hand-written error, empty or copy line left in the pages",() =>
        {
            var found=new List<string>();
            var empty=new System.Text.RegularExpressions.Regex(@"ImGui\.TextWrapped\(localization\.Get\(""[A-Za-z]+\.[A-Za-z]*(Empty|None|Loading|NoResult|NoData)[A-Za-z]*""\)\)");
            var copy=new System.Text.RegularExpressions.Regex(@"if ?\(ImGui\.(SmallButton|Button)\([^;]*\)\) ?ImGui\.SetClipboardText\(");
            foreach(var path in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs"))
            {
                var lines=File.ReadAllLines(path);
                for(var index=0;index<lines.Length;index++)
                {
                    var line=lines[index];
                    if(line.Contains("? \"Advanced.SourceOffTip\":\"") && line.Contains("RawMessage("))found.Add($"{Path.GetFileName(path)}:{index+1} error");
                    if(empty.IsMatch(line))found.Add($"{Path.GetFileName(path)}:{index+1} empty");
                    if(copy.IsMatch(line))found.Add($"{Path.GetFileName(path)}:{index+1} copy");
                }
            }
            Check(found.Count==0,"use DrawDataError / EmptyState / CopyButton: "+string.Join(", ",found.Take(8)));
            var component=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/Components/StatusLines.cs"));
            Check(component.Contains("private void EmptyState(string key)") && component.Contains("private bool CopyButton(string label,Func<string> text)"),"helpers");
        }),
    ];
}
