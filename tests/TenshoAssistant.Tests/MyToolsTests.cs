using System.Text;
using TenshoAssistant.MyTools;

// TEMPLATES-001 (DEC-029): tools made of data: strict format, sharing, progress, store.
internal static class MyToolsTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static string Tool(string blocks,string extra="")=>$$"""{"schema":1,"id":"my-tool","name":{"en":"Tool","fr":"Outil"}{{extra}},"blocks":[{{blocks}}]}""";
    private const string AllBlocks=
        """
        {"type":"heading","text":{"en":"Title"}},{"type":"note","text":{"fr":"Note"}},
        {"type":"checklist","reset":"daily","entries":[{"en":"One"},{"en":"Two"}]},{"type":"counter","label":{"en":"Runs"},"target":5},
        {"type":"timer","label":{"en":"Reset"},"at":"weekly"},{"type":"timer","label":{"en":"Hour"},"at":"hour","hourUtc":7},
        {"type":"items","items":[{"id":36166,"wanted":3},{"id":2}]},{"type":"link","label":{"en":"Site"},"url":"https://example.com/a?b=1"}
        """;
    private static void Refused(string json,string code)
    {
        var parse=ToolTemplateFormat.Parse(json);
        Check(parse.Template is null && parse.Problem?.Code==code,$"expected {code}, got {parse.Problem?.Code ?? "accepted"}: {json}");
    }
    private static void InFolder(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"Tensho-MyTools-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(Path.Combine(root,"MyTools.json"));}
        finally{Directory.Delete(root,true);}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("My tools format: the seven blocks read and written back the same",() =>
        {
            var parse=ToolTemplateFormat.Parse(Tool(AllBlocks,""","author":"Ann","license":"CC0-1.0" """));
            Check(parse.Template is {Id:"my-tool",Author:"Ann",License:"CC0-1.0",Blocks.Count:8},"read: "+parse.Problem?.Code+" "+parse.Problem?.Where);
            var template=parse.Template!;
            Check(template.Name.For("fr")=="Outil" && template.Name.For("de")=="Tool" && ((NoteBlock)template.Blocks[1]).Text.For("en")=="Note","languages: wanted, English, then the first");
            Check(template.Blocks[2] is ChecklistBlock {Reset:TemplateReset.Daily,Entries.Count:2} && template.Blocks[3] is CounterBlock {Target:5},"checklist, counter");
            Check(template.Blocks[5] is TimerBlock {Timer:TemplateTimer.Hour,HourUtc:7} && ((ItemListBlock)template.Blocks[6]).Items[1]==new TemplateItem(2,1),"timer, default wanted");
            var written=ToolTemplateFormat.Serialize(template);
            var again=ToolTemplateFormat.Parse(written);
            Check(again.Template is not null && ToolTemplateFormat.Serialize(again.Template)==written,"round trip");
        }),
        ("My tools format: refuses what it does not know, with the reason",() =>
        {
            Refused("","Empty");Refused("not json","NotJson");Refused("[]","BadValue");
            Refused("""{"schema":2,"id":"my-tool","name":{"en":"a"},"blocks":[]}""","Schema");
            Refused("""{"schema":"1","id":"my-tool","name":{"en":"a"},"blocks":[]}""","Schema");
            Refused("""{"schema":1,"id":"My Tool","name":{"en":"a"},"blocks":[]}""","BadId");
            Refused("""{"schema":1,"id":"../../x","name":{"en":"a"},"blocks":[]}""","BadId");
            Refused(Tool("",""","script":"x" """),"UnknownField");
            Refused("""{"schema":1,"schema":1,"id":"my-tool","name":{"en":"a"},"blocks":[]}""","RepeatedField");
            Refused(Tool("""{"type":"macro","text":{"en":"/echo"}}"""),"UnknownBlock");
            Refused(Tool("""{"type":"note","text":{"en":"a"},"run":true}"""),"UnknownField");
            Refused(Tool("""{"type":"note","text":{"xx":"a"}}"""),"UnknownField");
            Refused(Tool("""{"type":"note","text":{"en":"  "}}"""),"Missing");
            Refused(Tool("""{"type":"note"}"""),"Missing");
            Refused(Tool("""{"type":"checklist","entries":[]}"""),"Missing");
            Refused(Tool("""{"type":"checklist","reset":"hourly","entries":[{"en":"a"}]}"""),"BadValue");
            Refused(Tool("""{"type":"counter","label":{"en":"a"},"target":-1}"""),"BadValue");
            Refused(Tool("""{"type":"timer","label":{"en":"a"},"at":"hour","hourUtc":24}"""),"BadValue");
            Refused(Tool("""{"type":"timer","label":{"en":"a"},"at":"daily","hourUtc":2}"""),"UnknownField");
            Refused(Tool("""{"type":"items","items":[{"id":0}]}"""),"BadValue");
            Refused(Tool("""{"type":"items","items":[{"id":1},{"id":1}]}"""),"RepeatedItem");
        }),
        ("My tools format: bounds on size, counts and depth",() =>
        {
            static string Text(string type,int length)=>"{\"type\":\""+type+"\",\"text\":{\"en\":\""+new string('a',length)+"\"}}";
            Refused(Tool(Text("note",ToolTemplateFormat.MaxBytes)),"TooLarge");
            Refused(Tool(Text("note",ToolTemplateFormat.MaxNote+1)),"TooLong");
            Refused(Tool(Text("heading",ToolTemplateFormat.MaxLabel+1)),"TooLong");
            Refused(Tool(string.Join(",",Enumerable.Repeat("""{"type":"note","text":{"en":"a"}}""",ToolTemplateFormat.MaxBlocks+1))),"TooMany");
            Refused(new string('[',100)+new string(']',100),"NotJson");
        }),
        ("My tools format: a link is https, without an account in the address; a text stays plain",() =>
        {
            foreach(var url in new[]{"http://example.com","file:///c:/x","javascript:alert(1)","https://user:pw@example.com","https://localhost/x","steam://run/1","/echo"})
                Refused(Tool($$"""{"type":"link","label":{"en":"a"},"url":"{{url}}"}"""),"BadLink");
            Check(ToolTemplateFormat.SafeLink("https://example.com/a")?.Host=="example.com","https");
            Check(ToolTemplateFormat.Clean("a\u0002b\uE03Cc\u202Ed\n e ")=="abcd\n e","control, glyph and direction marks removed, line breaks kept");
            var note=(NoteBlock)ToolTemplateFormat.Parse(Tool("""{"type":"note","text":{"en":"/gearset change 1"}}""")).Template!.Blocks[0];
            Check(note.Text.For("en")=="/gearset change 1","a command is only text");
        }),
        ("My tools sharing: one line and back; a bomb, a broken line and raw JSON",() =>
        {
            var json=ToolTemplateFormat.Serialize(ToolTemplateFormat.Parse(Tool(AllBlocks)).Template!);
            var line=ToolTemplateFormat.Encode(json);
            Check(line.StartsWith(ToolTemplateFormat.SharePrefix) && !line.Contains('\n') && !line.Contains('+') && !line.Contains('/'),"one safe line");
            Check(ToolTemplateFormat.Import(line).Template is {Id:"my-tool"} && ToolTemplateFormat.Import("  "+line+"\n").Template is not null,"read back");
            Check(ToolTemplateFormat.Import(json).Template is not null,"raw JSON");
            Check(ToolTemplateFormat.Import(ToolTemplateFormat.SharePrefix+"!!!").Problem?.Code=="BadShare" && ToolTemplateFormat.Import(ToolTemplateFormat.SharePrefix+"AAAA").Problem is not null,"broken");
            Check(ToolTemplateFormat.Import(ToolTemplateFormat.Encode(new string(' ',5_000_000))).Problem?.Code=="TooLarge","a bomb stops while reading");
        }),
        ("My tools progress: a tick lasts until the reset of its list; timers follow the schedule",() =>
        {
            var template=ToolTemplateFormat.Parse(Tool(AllBlocks)).Template!;
            var now=new DateTimeOffset(2026,11,4,16,0,0,TimeSpan.Zero);
            var tool=new InstalledTool(template,"{}",new Dictionary<string,DateTimeOffset>{["2:0"]=now},new Dictionary<string,int>());
            Check(ToolProgress.IsDone(tool,"2:0",TemplateReset.Daily,now.AddHours(20)) && !ToolProgress.IsDone(tool,"2:0",TemplateReset.Daily,now.AddHours(24)),"daily");
            Check(ToolProgress.IsDone(tool,"2:0",TemplateReset.None,now.AddYears(1)) && !ToolProgress.IsDone(tool,"2:1",TemplateReset.None,now),"no reset; never ticked");
            Check(ToolProgress.NextTime(new TimerBlock(new(new Dictionary<string,string>{["en"]="a"}),TemplateTimer.Hour,17),now)==new DateTimeOffset(2026,11,4,17,0,0,TimeSpan.Zero),"hour");
            Check(ToolProgress.NextTime(new TimerBlock(new(new Dictionary<string,string>{["en"]="a"}),TemplateTimer.Weekly,0),now).DayOfWeek==DayOfWeek.Tuesday,"weekly");
        }),
        ("My tools store: kept through a restart with progress; replaced file resets it; refused and unreadable files are not lost",() => InFolder(path =>
        {
            var now=DateTimeOffset.UtcNow;
            var template=ToolTemplateFormat.Parse(Tool(AllBlocks)).Template!;var json=ToolTemplateFormat.Serialize(template);
            using(var first=new ToolTemplateStore(path,(_,_)=>{}))
            {
                Check(first.Install(template,json) && first.Tools.Count==1,"installed");
                first.SetDone("my-tool","2:0",true,now);first.SetCount("my-tool","3:0",4);first.FlushAsync().Wait();
            }
            using(var second=new ToolTemplateStore(path,(_,_)=>{}))
            {
                Check(second.Tools.Single().Counts["3:0"]==4 && second.Tools.Single().Done.ContainsKey("2:0"),"read back");
                second.Install(template,json);Check(second.Tools.Single().Counts.Count==1,"same file: progress kept");
                var other=ToolTemplateFormat.Parse(Tool("""{"type":"note","text":{"en":"new"}}""")).Template!;
                second.Install(other,ToolTemplateFormat.Serialize(other));
                Check(second.Tools.Count==1 && second.Tools.Single().Counts.Count==0,"same id, other file: replaced, progress reset");
                second.SetCount("my-tool","x",int.MaxValue);Check(second.Tools.Single().Counts["x"]==ToolTemplateFormat.MaxTarget,"bounded");
                second.Remove("my-tool");second.FlushAsync().Wait();Check(second.Tools.Count==0,"removed");
            }
            File.WriteAllText(path,"""{"Schema":1,"Tools":[{"Json":"{\"bad\":1}","Done":null,"Counts":null}]}""");
            using(var third=new ToolTemplateStore(path,(_,_)=>{}))
            {
                Check(third.Tools.Count==0 && third.RefusedCount==1,"refused, not shown");
                third.Install(template,json);third.FlushAsync().Wait();
                Check(File.ReadAllText(path).Contains("bad"),"and kept in the file");
            }
            File.WriteAllText(path,"not json");
            using(var broken=new ToolTemplateStore(path,(_,_)=>{}))
            {
                Check(broken.Unavailable && !broken.Install(template,json),"unavailable");
                broken.FlushAsync().Wait();
            }
            Check(File.ReadAllText(path)=="not json","never written over");
        })),
        ("My tools editor: only what the format accepts is saved; other languages are kept; blocks move",() =>
        {
            var draft=ToolDraft.New();
            Check(ToolTemplateFormat.ValidId(draft.Id) && draft.Id!=ToolDraft.New().Id,"new id");
            Check(draft.Check().Problem?.Code=="Missing","no name yet");
            draft.Name=ToolDraft.With(draft.Name,"fr","Mon outil");
            Check(draft.Check().Template is {Blocks.Count:0},"a name is enough: "+draft.Check().Problem?.Code);
            foreach(var kind in Enum.GetValues<ToolBlockKind>())
            {
                var block=ToolDraft.NewBlock(kind);
                draft.Blocks.Add(block);
                Check(draft.Check().Problem is not null,"a new block is not complete: "+kind);
                draft.Blocks.RemoveAt(0);
            }
            draft.Blocks.Add(new NoteBlock(new(new Dictionary<string,string>{["en"]="a"})));
            draft.Blocks.Add(new HeadingBlock(new(new Dictionary<string,string>{["en"]="b"})));
            Check(draft.Move(1,-1) && draft.Blocks[0] is HeadingBlock,"moved");
            Check(!draft.Move(0,-1) && !draft.Move(1,1) && !draft.Move(9,1),"not past the ends");
            draft.Blocks.Add(new LinkBlock(new(new Dictionary<string,string>{["en"]="c"}),"http://example.com"));
            Check(draft.Check().Problem?.Code=="BadLink","the editor cannot make what an import refuses");
            draft.Blocks.RemoveAt(2);
            draft.FreeToShare=true;draft.Author=" Ann ";
            var saved=draft.Check().Template!;
            Check(saved.License==ToolDraft.FreeLicense && saved.Author=="Ann","licence and author");
            var edited=ToolDraft.From(saved);
            Check(edited.Id==saved.Id && edited.FreeToShare && edited.Blocks.Count==2,"edit an installed tool");
            var both=ToolDraft.With(new(new Dictionary<string,string>{["en"]="a"}),"fr","b");
            Check(both.Values.Count==2 && ToolDraft.With(both,"fr"," ").Values.Count==1,"other languages kept");
        }),
        ("My tools settings: a tool may propose a theme, nothing else, and only a click applies it",() =>
        {
            var themed=ToolTemplateFormat.Parse(Tool("",""","settings":{"theme":"clear-blue"}"""));
            Check(themed.Template?.Theme=="clear-blue" && ToolTemplateFormat.Parse(Tool("")).Template?.Theme is null,"read");
            Check(ToolTemplateFormat.Parse(ToolTemplateFormat.Serialize(themed.Template!)).Template?.Theme=="clear-blue","written back");
            Refused(Tool("",""","settings":{"theme":"neon"}"""),"BadValue");
            foreach(var setting in new[]{"Discord","DiscordWebhook","MapView","themeMode"})Refused(Tool("",$$""","settings":{"{{setting}}":"x"}"""),"UnknownField");
            var names=System.Text.RegularExpressions.Regex.Match(Read("UI/FfxivTheme.cs")+Read("UI/ThemePalettes.cs"),@"enum PluginTheme \{([^}]*)\}").Groups[1].Value.Split(',',StringSplitOptions.TrimEntries);
            Check(names.Length==ToolTemplateFormat.Themes.Count && names.Select(name=>name.ToLowerInvariant()).SequenceEqual(ToolTemplateFormat.Themes.Select(name=>name.Replace("-",""))),"same order as PluginTheme");
            Check(ToolTemplateFormat.ThemeMode("dark")==1 && ToolTemplateFormat.ThemeMode("clear-green")==6 && ToolTemplateFormat.ThemeMode("x")==0,"ThemeMode");
            var page=Read("MainWindow.MyTools.cs");
            Check(page.Contains("\"MyToolsUi.ThemeProposed\"") && page.Contains("###MyToolThemeApply\"))SaveTheme(ToolTemplateFormat.ThemeMode(theme),configuration.ThemeModeBeforeTool ?? configuration.ThemeMode);"),"said in the preview, applied on a click, the theme before it remembered");
            Check(!page.Contains("configuration.ThemeMode=") && System.Text.RegularExpressions.Regex.Matches(page,"SaveTheme\\(").Count==2,"on these two clicks only");
            Check(page.Contains("###MyToolThemeReturn\"))SaveTheme(target);") && page.Contains("DrawMyToolsThemeReturn(store);") && page.Contains("configuration.ThemeModeBeforeTool ?? (fromTool ? DefaultConfiguration.ThemeMode:current)"),"a way back, always in view on the page, even for a theme applied before it was remembered");
            var theme=Read("MainWindow.Theme.cs");
            Check(theme.Contains("configuration.ThemeMode=mode;configuration.ThemeModeBeforeTool=before;") && theme.Contains("configuration.ThemeModeBeforeTool=previousBefore;"),"a choice in the settings forgets it; a failed save restores both");
            Check(TenshoAssistant.ConfigurationReset.Reset.Contains("ThemeModeBeforeTool"),"reset with the theme");
            var draft=ToolDraft.New();draft.Name=ToolDraft.With(draft.Name,"en","a");draft.Theme="light";
            Check(draft.Check().Template?.Theme=="light" && ToolDraft.From(draft.ToTemplate()).Theme=="light","editor");
        }),
        ("My tools wiring: nothing sent to the game; links confirmed; the file is checked and shown before it is installed",() =>
        {
            var page=Read("MainWindow.MyTools.cs");
            foreach(var word in new[]{"ProcessChatBox","SendMessage","Process.Start"})Check(!page.Contains(word),word);
            Check(page.Contains("MyToolsUi.LinkConfirm") && page.IndexOf("Dalamud.Utility.Util.OpenLink(link.Url)",StringComparison.Ordinal)>page.IndexOf("myToolPendingLink!=id",StringComparison.Ordinal),"the link opens after its confirmation");
            Check(page.Contains("ToolTemplateFormat.Import(myToolsPasted)") && page.Contains("DrawMyToolBlocks(template,null,store)"),"checked and previewed");
            Check(Read("Plugin.cs").Contains("new TenshoAssistant.MyTools.ToolTemplateStore(") && Read("MainWindow.Pages.cs").Contains("new(Page.MyTools,null,PageLabel(\"Navigation.MyTools\"),DrawMyTools,"),"registered");
            Check(!Read("MyTools/ToolTemplateFormat.cs").Contains("Assembly.Load") && !Read("MyTools/ToolTemplateStore.cs").Contains("Assembly.Load"),"no code loaded");
        }),
    ];
}
