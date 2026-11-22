using System.Text.RegularExpressions;
using TenshoAssistant;
using TenshoAssistant.Help;

// X-809 (0.1807, the user's report of 22/11): every link target (Help/NavTargets.cs) is read against the pages, tab bars and Settings
// categories as they are now; a key used in the code and missing from the table, or a target gone stale, fails here.
internal static class NavTargets1809Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static string[] Pages()=>Regex.Match(Read("src/TenshoAssistant/MainWindow.cs"),@"enum Page \{([^}]*)\}").Groups[1].Value
        .Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);

    private static string AllPageCode()=>string.Join("\n",Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs").Select(path=>File.ReadAllText(path).Replace("\r\n","\n")));

    /// <summary>The tab keys of a bar: the call OrderedTabs("bar" or DetailTabs("bar" up to the end of its list.</summary>
    private static string[] TabsOf(string code,string bar)
    {
        var at=code.IndexOf("Tabs(\""+bar+"\",",StringComparison.Ordinal);
        if(at<0)return [];
        var end=code.IndexOf("\n        ]",at,StringComparison.Ordinal);
        var list=code[at..(end<0 ? code.Length:end)];
        return Regex.Matches(list,@"^\s*(?:new)?\(""(\w+)"",",RegexOptions.Multiline).Select(match=>match.Groups[1].Value).ToArray();
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Nav targets 1809: every target names a page, a tab of its bar, or a Settings category and setting that exist now",() =>
        {
            var pages=Pages();
            var code=AllPageCode();
            var categories=Read("src/TenshoAssistant/MainWindow.SettingsCategories.cs");
            Check(NavTargets.Table.Count>=15,"the table holds the links");
            foreach(var (page,bar) in NavTargets.Bars)
            {
                Check(pages.Contains(page),$"bar of an unknown page {page}");
                Check(TabsOf(code,bar).Length>=2,$"{page}: no tab bar {bar} in the code");
            }
            foreach(var (key,target) in NavTargets.Table)
            {
                Check(pages.Contains(target.Page),$"{key}: no page {target.Page}");
                if(target.Page=="Settings")
                {
                    Check(target.Tab is not null && target.Category is not null,$"{key}: a Settings link names its tab and category");
                    Check(categories.Contains($"new(\"{target.Tab}\",\"{target.Category}\",",StringComparison.Ordinal),$"{key}: no category {target.Tab} › {target.Category}");
                    if(target.SettingKey is { } setting)
                        Check(SettingsIndex.Categories.GetValueOrDefault(setting)==target.Category && SettingsIndex.Entries.Any(entry=>entry.Key==setting && entry.Tab==target.Tab),$"{key}: the setting {setting} is not in {target.Tab} › {target.Category}");
                    continue;
                }
                Check(target.Category is null && target.SettingKey is null,$"{key}: a category outside Settings");
                if(target.Tab is { } tab)
                {
                    Check(NavTargets.Bars.TryGetValue(target.Page,out var bar),$"{key}: {target.Page} has no known tab bar");
                    Check(TabsOf(code,bar!).Contains(tab),$"{key}: no tab {tab} in {bar}");
                }
            }
        }),
        ("Nav targets 1809: an FAQ answer about settings opens its category; a key unknown to the table is refused",() =>
        {
            foreach(var entry in Faq.Entries.Where(entry=>entry.Page=="Settings"))
                Check(NavTargets.Table.ContainsKey("Faq."+entry.Key),$"{entry.Key}: a Settings answer without its category would open the one last shown");
            foreach(var key in NavTargets.Table.Keys.Where(key=>key.StartsWith("Faq.",StringComparison.Ordinal)))
                Check(Faq.Entries.Any(entry=>"Faq."+entry.Key==key && entry.Page==NavTargets.Table[key].Page),$"{key}: no such FAQ answer with that page");
            var language=NavTargets.OfFaq(Faq.Entries.First(entry=>entry.Key=="Language"));
            Check(language==new NavTarget("Settings","General","Appearance"),"the language answer: General › Language and theme (it opened « Opening »)");
            Check(NavTargets.OfFaq(Faq.Entries.First(entry=>entry.Key=="Marks")) is null,"an answer without a page: no button");
            Check(NavTargets.OfFaq(Faq.Entries.First(entry=>entry.Key=="Wishlist"))==new NavTarget("Wishlist"),"an answer with a page only: that page");
            var refused=false;
            try{NavTargets.Of("Home.Nowhere");}
            catch(KeyNotFoundException){refused=true;}
            Check(refused,"an unknown key is a fault, never a guessed page");
        }),
        ("Nav targets 1809: every key the code asks for is in the table, and links go through Open",() =>
        {
            var code=AllPageCode();
            var asked=Regex.Matches(code,@"(?:\bTo|NavTargets\.Of)\(""([\w.]+)""\)").Select(match=>match.Groups[1].Value)
                .Concat(Regex.Matches(code,@"""(Home\.(?:Alarms(?:Jobs|Other)|Vessels|Garden))""").Select(match=>match.Groups[1].Value)).Distinct().ToArray();
            Check(asked.Length>=8,"the keys of the code are found");
            foreach(var key in asked)Check(NavTargets.Table.ContainsKey(key),$"{key} is asked by the code and missing from the table");
            var today=Read("src/TenshoAssistant/MainWindow.HomeToday.cs");
            Check(today.Contains("if(ImGui.Selectable(text+\"###Today\"+key))Open(target);") && !today.Contains("page=next.Kind switch"),"Today's lines and the next deadline follow their target");
            Check(Read("src/TenshoAssistant/MainWindow.Home.cs").Contains("alarm.Enabled && !IsReminderAlarm(alarm)") && today.Contains("alarm.Enabled && !IsReminderAlarm(alarm)"),"a reminder's alarm (a vessel back) is not the Alarms tool's next alarm");
            var help=Read("src/TenshoAssistant/MainWindow.Help.cs");
            Check(help.Contains("NavTargets.OfFaq(faq) is { } target") && help.Contains("Open(target);") && !help.Contains("Faq.PageOf(topic.Key)"),"the FAQ's button follows its target");
            var detail=Read("src/TenshoAssistant/UI/Components/DetailPage.cs");
            Check(detail.Contains("var linked=TakeNavTab(id);") && detail.Contains("select || key==linked ||"),"a bar selects the tab a link asks of it, once");
            var open=Read("src/TenshoAssistant/MainWindow.Navigation.cs");
            Check(open.Contains("OpenSettingsCategory(tab,category,target.SettingKey);") && open.Contains("navTabRequest=(bar,key);"),"Open: the Settings category, or the tab asked of the page's bar");
        }),
    ];
}
