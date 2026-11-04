using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Help;

// ONBOARD-001, lot 1 (DEC-031): every page has its help, in every language, like the tooltips: a page without help does not pass.
internal static class HelpTests
{
    private sealed class FixedResponse(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(body)});
    }
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
        ("Report a problem: the form's address, with the field ids of the repository's forms",() =>
        {
            var link=ProblemReport.Link(new(ProblemKind.Bug,"Map & flag","Step 1","It works","Report text","0.504.0","fr"));
            Check(!link.ReportLeftOut && link.Url.StartsWith(ProblemReport.Repository+"/issues/new?template=bug_report.yml&title=Map%20%26%20flag",StringComparison.Ordinal),"template and encoded title");
            foreach(var part in new[]{"&version=0.504.0","&client=fr","&steps=Step%201","&expected=It%20works","&report=Report%20text"})Check(link.Url.Contains(part,StringComparison.Ordinal),part);
            var form=Read(".github/ISSUE_TEMPLATE/bug_report.yml");
            foreach(var id in new[]{"version","client","steps","expected","report"})Check(form.Contains("id: "+id,StringComparison.Ordinal),"bug form: "+id);
            var idea=ProblemReport.Link(new(ProblemKind.Idea,"T","Want","Like this","R","v","c")).Url;
            Check(idea.Contains("template=feature_request.yml") && idea.Contains("&need=Want") && idea.Contains("&example=Like%20this") && !idea.Contains("report="),"idea form");
            Check(Read(".github/ISSUE_TEMPLATE/feature_request.yml").Contains("id: need") && Read(".github/ISSUE_TEMPLATE/feature_request.yml").Contains("id: example"),"idea ids");
            var translation=ProblemReport.Link(new(ProblemKind.Translation,"T","Text","","R","v","de")).Url;
            Check(translation.Contains("template=translation.yml") && translation.Contains("&language=de") && translation.Contains("&texts=Text"),"translation form");
            Check(Read(".github/ISSUE_TEMPLATE/translation.yml").Contains("id: language") && Read(".github/ISSUE_TEMPLATE/translation.yml").Contains("id: texts"),"translation ids");
        }),
        ("Report a problem: too long goes to the clipboard, and nothing private leaves",() =>
        {
            var big=new string('x',7000);
            var withoutReport=ProblemReport.Link(new(ProblemKind.Bug,"T","Step","","Short "+big,"v","c"));
            Check(withoutReport.ReportLeftOut && !withoutReport.Url.Contains("report=") && withoutReport.Url.Contains("&steps=Step") && withoutReport.Url.Length<6000,"the report is dropped first");
            var tooMuch=ProblemReport.Link(new(ProblemKind.Bug,"T",big,big,"","v","c"));
            Check(tooMuch.ReportLeftOut && tooMuch.Url.Length<6000 && tooMuch.Url.Contains("&version=v"),"then the player's text");
            var clean=ProblemReport.Clean("C:\\Users\\Someone\\log.txt and https://discord.com/api/webhooks/123/abc end");
            Check(clean=="<path> and <webhook> end","folders and webhook");
            Check(ProblemReport.Link(new(ProblemKind.Bug,"See C:\\Users\\Me\\a","","","","v","c")).Url.Contains("%3Cpath%3E"),"also in the title");
            Check(ProblemReport.SearchAddress("map flag").EndsWith("/issues?q=is%3Aissue%20map%20flag",StringComparison.Ordinal),"search");
            var help=Read("src/TenshoAssistant/MainWindow.Help.cs");
            Check(!help.Contains("HttpClient") && help.Contains("Dalamud.Utility.Util.OpenLink(link.Url)"),"the browser opens, the plugin sends nothing");
        }),
        ("Community guides: hosts, rows and order; a broken list is refused whole",() =>
        {
            foreach(var good in new[]{"https://www.youtube.com/watch?v=abc","https://youtu.be/abc","https://github.com/Shigu330/Tensho-Assistant/blob/main/docs/x.md"})Check(CommunityHelpIndex.Allowed(good),good);
            foreach(var bad in new[]{"http://www.youtube.com/watch?v=abc","https://evil.example/x","https://www.youtube.com.evil.example/x","https://github.com/someone/else","https://user@www.youtube.com/x","https://www.youtube.com:8443/x","youtube.com/x","javascript:alert(1)"})Check(!CommunityHelpIndex.Allowed(bad),bad);
            HashSet<string> pages=["Maps"];
            string Row(string page="Maps",string language="en",string title="A title",string address="https://youtu.be/abc")=>$$"""{"page":"{{page}}","language":"{{language}}","title":"{{title}}","address":"{{address}}","author":"Ann","forVersion":"0.506"}""";
            string Doc(params string[] rows)=>$$"""{"schema":1,"guides":[{{string.Join(",",rows)}}]}""";
            var parsed=CommunityHelpIndex.Parse(Doc(Row()),pages);
            Check(parsed.Guides is { Count:1 } && parsed.Guides[0]==new CommunityGuide("Maps","en","A title","Ann","https://youtu.be/abc","0.506"),"a good row");
            foreach(var bad in new[]{Row(page:"Nope"),Row(language:"english"),Row(title:""),Row(title:new string('x',121)),Row(address:"https://evil.example/x")})Check(CommunityHelpIndex.Parse(Doc(Row(),bad),pages).Problem=="Broken","refused whole: "+bad);
            Check(CommunityHelpIndex.Parse("{broken",pages).Problem=="Broken" && CommunityHelpIndex.Parse("""{"schema":2,"guides":[]}""",pages).Problem=="Newer","broken, newer");
            CommunityGuide[] guides=[new("Maps","en","E",null,"https://youtu.be/e",null),new("Maps","fr","F",null,"https://youtu.be/f",null),new("Maps","de","D",null,"https://youtu.be/d",null),new("Items","fr","I",null,"https://youtu.be/i",null)];
            Check(CommunityHelpIndex.For(guides,"Maps","fr").Select(guide=>guide.Title).SequenceEqual(["F","E"]),"the player's language, then English, this page only");
        }),
        ("Community guides: the repository's list passes the plugin's rules; read on a click, bounded",() =>
        {
            var pages=Regex.Match(Read("src/TenshoAssistant/MainWindow.cs"),@"enum Page \{([^}]*)\}").Groups[1].Value.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var index=CommunityHelpIndex.Parse(Read("community-help/index.json"),pages);
            Check(index.Guides is not null,"community-help/index.json: "+index.Problem);
            using var client=new HttpClient(new FixedResponse(new string(' ',CommunityHelpIndex.MaxBytes+1)));
            using var tooBig=new CommunityHelpIndex(client);
            tooBig.LoadAsync(pages).GetAwaiter().GetResult();
            Check(tooBig.State is {Problem:"Broken",Guides:null,Loading:false},"too big");
            using var fine=new HttpClient(new FixedResponse("""{"schema":1,"guides":[]}"""));
            using var read=new CommunityHelpIndex(fine);
            Check(read.State is {Guides:null,Loading:false},"nothing read before the click");
            read.LoadAsync(pages).GetAwaiter().GetResult();
            Check(read.State.Guides is { Count:0 },"read");
            var help=Read("src/TenshoAssistant/MainWindow.Help.cs");
            Check(help.Contains("Dalamud.Utility.Util.OpenLink(asked.Address)") && help.Contains("Help.GuidesConfirm"),"opened after showing its address");
            Check(Read("CONTRIBUTING.md").Contains("community-help/README.md") && Read("community-help/README.md").Contains("\"address\""),"explained to contributors");
            var script=Read("scripts/New-Tool.ps1");
            Check(script.Contains("PageHelp") && script.Contains("Purpose") && script.Contains("Empty"),"a new tool gets its help");
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
