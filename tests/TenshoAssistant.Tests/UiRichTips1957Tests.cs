using System.Text.RegularExpressions;

// 0.1957 (X-913, review of 0.1956, M1). The ratchet of 0.1956 saw one exact form. This one reads every « if » whose condition holds the
// plain hover test ImGui.IsItemHovered() (not negated: « !ImGui.IsItemHovered() » is another test) and whose statement, or block in
// braces, draws a rich tooltip Draw…Tooltip(: that guard goes through RichTipHovered(), whatever the other conditions and the layout.
internal static class UiRichTips1957Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");
    private static readonly Regex Hover=new(@"(?<!!)ImGui\.IsItemHovered\(\)");
    private static readonly Regex RichTip=new(@"Draw[A-Za-z]*Tooltip\(");

    private static int Closing(string text,int start,char open,char close)
    {
        var depth=0;
        for(var index=start;index<text.Length;index++)
        {
            if(text[index]==open)depth++;
            else if(text[index]==close && --depth==0)return index;
        }
        return -1;
    }

    // The guards written by hand in one source: line numbers of each « if(… ImGui.IsItemHovered() …) » in front of a Draw…Tooltip.
    internal static IReadOnlyList<int> HandGuards(string text)
    {
        var lines=new List<int>();
        foreach(Match match in Regex.Matches(text,@"\bif\s*\("))
        {
            var start=match.Index+match.Length-1;
            var end=Closing(text,start,'(',')');
            if(end<0 || !Hover.IsMatch(text[start..(end+1)]))continue;
            var body=end+1;
            while(body<text.Length && char.IsWhiteSpace(text[body]))body++;
            if(body>=text.Length)continue;
            var stop=text[body]=='{' ? Closing(text,body,'{','}'):text.IndexOf(';',body);
            if(stop<0)continue;
            if(RichTip.IsMatch(text[body..(stop+1)]))lines.Add(text[..match.Index].Count(character=>character=='\n')+1);
        }
        return lines;
    }

    private static IEnumerable<(string Name,string Text)> Sources()
        =>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
            .Select(path=>Path.GetRelativePath(Root,path).Replace('\\','/'))
            .Where(name=>!name.Split('/').Any(part=>part is "obj" or "bin"))
            .Select(name=>(name,File.ReadAllText(Path.Combine(Root,name))));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI rich tips 1957: the ratchet sees the guards with another condition, with braces, on several lines; not the negated test",()=>
        {
            Check(HandGuards("if(ImGui.IsItemHovered())DrawItemInfoTooltip(id);").Count==1,"plain form");
            Check(HandGuards("if(item is not null && ImGui.IsItemHovered())DrawItemInfoTooltip(id);").Count==1,"another condition");
            Check(HandGuards("if(hovered || ImGui.IsItemHovered())DrawItemInfoTooltip(id);").Count==1,"or");
            Check(HandGuards("if (ImGui.IsItemHovered())\n{\n    var x=1;\n    if(x>0)DrawPlaceTooltip(map,null,null,null);\n}").Count==1,"braces on several lines");
            Check(HandGuards("if(ImGui.IsItemHovered()){if(m is not null)DrawCraftActionTooltip(m,t);else WrappedTooltip(t);}").Count==1,"nested");
            Check(HandGuards("if(a && !ImGui.IsItemHovered())DrawRouteLootTooltip(c,r,p);").Count==0,"negated: another test");
            Check(HandGuards("if(ImGui.IsItemHovered())WrappedTooltip(text);").Count==0,"a plain tooltip: Tip/TipText rules");
            Check(HandGuards("if(item is not null && RichTipHovered())DrawItemInfoTooltip(id);").Count==0,"the guard");
        }),
        ("UI rich tips 1957: no hover test by hand in front of a rich tooltip in the sources, whatever the form",()=>
        {
            var found=Sources().SelectMany(source=>HandGuards(source.Text).Select(line=>source.Name+":"+line)).ToArray();
            Check(found.Length==0,"hover test by hand in front of a rich tooltip: "+string.Join(", ",found));
        }),
        ("UI rich tips 1957: the guards of 0.1957 read through RichTipHovered() (another condition, braces)",()=>
        {
            string Read(string name)=>File.ReadAllText(Path.Combine(Root,name));
            Check(Read("MainWindow.MyTools.cs").Contains("if(item is not null && RichTipHovered())DrawItemInfoTooltip(row.Id);",StringComparison.Ordinal),"MyTools");
            Check(Read("MainWindow.DutyFinder.cs").Contains("if(hovered || RichTipHovered())DrawItemInfoTooltip(id);",StringComparison.Ordinal),"DutyFinder");
            Check(Read("MainWindow.RoutePlanner.cs").Contains("!ImGui.IsItemHovered())DrawRouteLootTooltip(",StringComparison.Ordinal),"the negated test stays");
        }),
    ];
}
