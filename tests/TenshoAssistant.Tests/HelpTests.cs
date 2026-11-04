using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Help;

// ONBOARD-001, lot 1 (DEC-031): every page has its help, in every language, like the tooltips: a page without help does not pass.
internal static class HelpTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Help: every page of the list has its four texts, in English and French",() =>
        {
            var enumBody=Regex.Match(Read("src/TenshoAssistant/MainWindow.cs"),@"enum Page \{([^}]*)\}").Groups[1].Value;
            var pages=enumBody.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
            Check(pages.Contains("Help") && pages.Length>=39,"the help page itself");
            foreach(var language in new[]{"en","fr"})
            {
                using var document=JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").TrimStart('﻿'));
                var section=document.RootElement.GetProperty("PageHelp");
                foreach(var page in pages)foreach(var part in new[]{"Title","Purpose","Start","Empty"})
                    Check(section.TryGetProperty(page+part,out var text) && text.GetString() is { Length:>0 },$"{language}: PageHelp.{page}{part}");
            }
        }),
        ("Help: the search finds every word, titles first",() =>
        {
            HelpTopic[] topics=[new("A","Map","Where you are","Open it","Wait"),new("B","Weather","The map of clouds","Pick a zone","Wait"),new("C","Market","Prices","Pick an item","Check the source")];
            Check(HelpIndex.Search(topics,"").Count==3 && HelpIndex.Search(topics,"   ").Count==3,"empty query");
            Check(HelpIndex.Search(topics,"map")[0].Key=="A" && HelpIndex.Search(topics,"map").Count==2,"title first, then the text");
            Check(HelpIndex.Search(topics,"PICK item").Single().Key=="C","every word, any case");
            Check(HelpIndex.Search(topics,"nothing").Count==0,"no result");
        }),
        ("Help: the state of a page is the worst of what it depends on, and names real self-check lines",() =>
        {
            Check(HelpIndex.StateOf([])==HelpPageState.Unknown,"nothing to say");
            Check(HelpIndex.StateOf([TenshoAssistant.Diagnostics.SelfCheck.Ok("a","")])==HelpPageState.Ready,"read");
            Check(HelpIndex.StateOf([new("a",TenshoAssistant.Diagnostics.SelfCheckStatus.Seen,"")])==HelpPageState.Ready,"read earlier");
            Check(HelpIndex.StateOf([TenshoAssistant.Diagnostics.SelfCheck.Ok("a",""),TenshoAssistant.Diagnostics.SelfCheck.NotRead("b","")])==HelpPageState.Waiting,"waiting wins over read");
            Check(HelpIndex.StateOf([TenshoAssistant.Diagnostics.SelfCheck.Suspect("a",""),TenshoAssistant.Diagnostics.SelfCheck.NotRead("b","")])==HelpPageState.Unusual,"unusual over waiting");
            Check(HelpIndex.StateOf([TenshoAssistant.Diagnostics.SelfCheck.Suspect("a",""),TenshoAssistant.Diagnostics.SelfCheck.Failed("b","x")])==HelpPageState.Failed,"failed wins");
            var probes=Read("src/TenshoAssistant/MainWindow.SelfCheck.cs");
            foreach(var domain in HelpIndex.AllDomains)Check(probes.Contains($"\"{domain}\""),"no self-check line named "+domain);
            Check(HelpIndex.DomainsOf("Credits").Count==0,"a page that reads nothing has no state");
        }),
        ("Help: first-visit banner once per page, kept across a reset, with its setting",() =>
        {
            var help=Read("src/TenshoAssistant/MainWindow.Help.cs");
            Check(help.Contains("configuration.HelpSeen?.Contains(page.ToString())==true".Replace("==true"," == true")) && help.Contains("(configuration.HelpSeen ??= []).Add(page.ToString());"),"once per page");
            Check(Read("src/TenshoAssistant/MainWindow.cs").Contains("DrawHelpBanner();"),"drawn above the page");
        }),
        ("Help: « ? » on every page, and a page in the navigation",() =>
        {
            Check(Read("src/TenshoAssistant/MainWindow.cs").Contains("DrawPageHelpButton();"),"the button beside the title");
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Help,\"Preferences\",PageLabel(\"Navigation.Help\"),DrawHelpPage,"),"the page");
        }),
    ];
}
