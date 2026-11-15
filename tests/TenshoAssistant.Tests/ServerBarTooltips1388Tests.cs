using System.Text.RegularExpressions;

// X-276 (0.1388, user's remark of 13/11, test T-0.1240-1): one way for the tooltips of the server bar (R10). Ratchet: no entry of the game's
// server bar sets its game tooltip while Tensho's is on and its place is known; each draws Tensho's tooltip, above or below the entry
// (setting), from the plugin's draw.
internal static class ServerBarTooltips1388Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(path).Replace("\r\n", "\n");

    private static IEnumerable<string> EntryFiles() =>
        Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && Read(path).Contains("IDtrBarEntry"));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Server bar tooltips 1388: no entry sets the game's tooltip while Tensho's is on (ratchet)", () =>
        {
            var files = EntryFiles().ToList();
            Check(files.Count >= 6, "the server bar entries found: " + files.Count);
            var bad = new List<string>();
            foreach (var file in files)
            {
                var lines = Read(file).Split('\n');
                for (var index = 0; index < lines.Length; index++)
                {
                    if (!Regex.IsMatch(lines[index], @"\.Tooltip\s*=") || Regex.IsMatch(lines[index], @"\.Tooltip\s*=\s*null;")) continue;
                    var guarded = lines[index].Contains("configuration.AlarmBarTooltip && bounds is not null ? null:", StringComparison.Ordinal)
                        || index > 0 && lines[index].TrimStart().StartsWith("else ", StringComparison.Ordinal) && lines[index - 1].Contains("if(configuration.AlarmBarTooltip && bounds is not null)", StringComparison.Ordinal);
                    if (!guarded) bad.Add(Path.GetFileName(file) + ":" + (index + 1));
                }
            }
            Check(bad.Count == 0, "the game's tooltip set while Tensho's is on: " + string.Join(", ", bad));
        }),
        ("Server bar tooltips 1388: each entry draws Tensho's tooltip above or below, from the plugin's draw", () =>
        {
            var plugin = Read(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            foreach (var file in EntryFiles())
            {
                var code = Read(file);
                Check(code.Contains("public void DrawTooltip()") && code.Contains("configuration.ServerBarTooltipAbove") && code.Contains("TooltipScope.Begin()"), Path.GetFileName(file) + ": Tensho's tooltip");
            }
            Check(plugin.Contains("(\"InfoBar\",()=>infoBar?.Invoke()?.DrawTooltip()),"), "the optional entries' tooltip drawn");
            var info = Read(Path.GetFullPath("src/TenshoAssistant/Alarms/InfoDtrEntries.Dalamud.cs"));
            Check(info.Contains("var bounds=AlarmDtrEntry.FindTextNode(gui,shownLine.Label);"), "the optional entries are found on screen as the others");
        }),
    ];
}
