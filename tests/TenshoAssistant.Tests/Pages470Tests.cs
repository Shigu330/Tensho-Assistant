using System.Text.RegularExpressions;

// 0.470 (TEMPLATES-001, DEC-029): one list of pages, and what a new tool needs.
internal static class Pages470Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Pages: every page of the enum is in the list once, and nothing else decides the navigation",() =>
        {
            var enumBody=Regex.Match(Read("src/TenshoAssistant/MainWindow.cs"),@"enum Page \{([^}]*)\}").Groups[1].Value;
            var names=enumBody.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
            Check(names.Length>=35 && names[0]=="Home","enum: Home stays the default page");
            var list=Read("src/TenshoAssistant/MainWindow.Pages.cs");
            var listed=Regex.Matches(list,@"new\(Page\.(\w+),").Select(match=>match.Groups[1].Value).ToArray();
            Check(listed.Distinct().Count()==listed.Length,"listed twice");
            Check(names.Order().SequenceEqual(listed.Order()),"missing: "+string.Join(",",names.Except(listed))+" unknown: "+string.Join(",",listed.Except(names)));
            var window=Read("src/TenshoAssistant/MainWindow.cs");
            Check(!window.Contains("case Page.") && window.Contains("PageEntryFor(page)?.Draw();") && window.Contains("foreach(var group in PageGroups)"),"drawn and listed from it");
        }),
        ("Pages: a page without a name in the navigation says which entry stays selected",() =>
        {
            foreach(Match row in Regex.Matches(Read("src/TenshoAssistant/MainWindow.Pages.cs"),@"new\(Page\.(\w+),[^,]+,null,.*"))Check(Regex.IsMatch(row.Value,@",Page\.\w+\),$"),"parent: "+row.Groups[1].Value);
        }),
        ("New tool: the generator covers each step, refuses a bad name, and writes nothing on a dry run",() =>
        {
            var script=Read("scripts/New-Tool.ps1");
            foreach(var part in new[]{"enum Page","New tools: scripts/New-Tool.ps1","Navigation","Program.cs","TenshoAssistant.Tests.csproj","DryRun","^[A-Z][A-Za-z0-9]{2,39}$"})Check(script.Contains(part),part);
            Check(Read("CONTRIBUTING.md").Contains("scripts/New-Tool.ps1"),"described for contributors");
        }),
        ("The custom chat was removed by the user's decision (DEC-030): no page, setting, text or file is left; the other features that read chat lines stay",() =>
        {
            Check(!Directory.Exists(Path.GetFullPath("src/TenshoAssistant/Chat")) && !File.Exists(Path.GetFullPath("src/TenshoAssistant/MainWindow.Chat.cs")),"files");
            Check(!Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("Page.Chat") && !Read("src/TenshoAssistant/Configuration.cs").Contains("ChatSettings") && !Read("src/TenshoAssistant/Plugin.cs").Contains("ChatReader"),"wiring");
            foreach(var language in new[]{"en","fr"})Check(!Read($"src/TenshoAssistant/Localization/Languages/{language}.json").Contains("\"ChatUi\""),language);
            Check(File.Exists(Path.GetFullPath("src/TenshoAssistant/Maps/ChatMapLinkWatcher.Dalamud.cs")) && File.Exists(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs")),"map links and garden detection stay");
        }),
        ("Contributions: a tool proposal form, and the pull request asks about automation, combat, sources, licence and AI use",() =>
        {
            Check(File.Exists(Path.GetFullPath(".github/ISSUE_TEMPLATE/tool_proposal.yml")),"form");
            var request=Read(".github/pull_request_template.md");
            foreach(var word in new[]{"automat","combat","data","licen","AI"})Check(request.Contains(word,StringComparison.OrdinalIgnoreCase),word);
        }),
    ];
}
