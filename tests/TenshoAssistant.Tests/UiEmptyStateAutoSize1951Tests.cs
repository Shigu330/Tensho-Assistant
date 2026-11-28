using System.Text;
using System.Text.RegularExpressions;

// 0.1951 (X-908, review of 0.1950, I1). A wrapped « nothing here » line (EmptyState, ImGui.TextWrapped) alone in a window that sizes itself
// on its content (AlwaysAutoResize) without a width wraps on the window's width, which follows the text: the window shrinks to a narrow
// column. Such a window uses EmptyStateLine (one line); a panel with a size, or an EmptyState drawn inside its own wrap width
// (PushTextWrapPos before it, not popped), may keep EmptyState.
// 0.1952 (X-910, review of 0.1951, M1): the check now also sees the flags given by a constant (ListPanelFlags), the windows opened by
// Panels?.Show without a width, and the helpers the contents call (any depth); a PushTextWrapPos elsewhere in a body no longer exempts.
internal static class UiEmptyStateAutoSize1951Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static string Pages()
        =>string.Concat(Directory.EnumerateFiles(Root,"*.cs",SearchOption.TopDirectoryOnly).Order()
            .Select(path=>WithoutComments(File.ReadAllText(path).Replace("\r\n","\n"))+"\n"));

    // From « at » on a string or character literal, the index of its last character.
    private static int LiteralEnd(string code,int at)
    {
        if(code[at]=='\'')
        {
            if(at+1<code.Length && code[at+1]=='\\'){var end=code.IndexOf('\'',at+3);return end<0 ? code.Length-1:end;}
            return Math.Min(at+2,code.Length-1);
        }
        var verbatim=at>0 && code[at-1]=='@';
        for(var index=at+1;index<code.Length;index++)
        {
            var c=code[index];
            if(verbatim)
            {
                if(c!='"')continue;
                if(index+1<code.Length && code[index+1]=='"'){index++;continue;}
                return index;
            }
            if(c=='\\')index++;
            else if(c=='"' || c=='\n')return index;
        }
        return code.Length-1;
    }

    // The code without its comments: an apostrophe or a call written in a comment would mislead the scan.
    private static string WithoutComments(string code)
    {
        var text=new StringBuilder(code.Length);
        for(var index=0;index<code.Length;index++)
        {
            var c=code[index];
            var next=index+1<code.Length ? code[index+1]:'\0';
            if(c=='/' && next=='/'){var end=code.IndexOf('\n',index);if(end<0)break;index=end-1;continue;}
            if(c=='/' && next=='*'){var end=code.IndexOf("*/",index+2,StringComparison.Ordinal);if(end<0)break;index=end+1;continue;}
            if(c is '"' or '\''){var end=LiteralEnd(code,index);text.Append(code,index,end-index+1);index=end;continue;}
            text.Append(c);
        }
        return text.ToString();
    }

    // From « at » on an opening bracket, the index of the matching closing one, literals skipped; -1 when unmatched.
    private static int Closing(string code,int at,char open,char close)
    {
        var depth=0;
        for(var index=at;index<code.Length;index++)
        {
            var c=code[index];
            if(c is '"' or '\''){index=LiteralEnd(code,index);continue;}
            if(c==open)depth++;
            else if(c==close && --depth==0)return index;
        }
        return -1;
    }

    // The body of « void|bool NAME(…) { … } » or « … => …; », braces matched; null when the method is not there.
    private static string? Body(string code,string name)
    {
        var match=Regex.Match(code,@"\b(?:void|bool) "+Regex.Escape(name)+@"\((?:[^(){};]|\([^()]*\))*\)\s*(\{|=>)");
        if(!match.Success)return null;
        var start=match.Groups[1].Index;
        if(match.Groups[1].Value=="=>")
        {
            var end=code.IndexOf(";\n",start,StringComparison.Ordinal);
            return end<0 ? null:code.Substring(match.Index,end-match.Index+1);
        }
        var close=Closing(code,start,'{','}');
        return close<0 ? null:code.Substring(match.Index,close-match.Index+1);
    }

    // The arguments of the call whose « ( » is at « open », split at the top level.
    private static List<string> Arguments(string code,int open)
    {
        var close=Closing(code,open,'(',')');
        var list=new List<string>();
        if(close<0)return list;
        var depth=0;var from=open+1;
        for(var index=open+1;index<close;index++)
        {
            var c=code[index];
            if(c is '"' or '\''){index=LiteralEnd(code,index);continue;}
            if(c is '(' or '{' or '[')depth++;
            else if(c is ')' or '}' or ']')depth--;
            else if(c==',' && depth==0){list.Add(code[from..index]);from=index+1;}
        }
        list.Add(code[from..close]);
        return list;
    }

    // Flags text with the window-flag constants it names replaced by their value.
    private static string Flags(string code,string flags)
    {
        foreach(Match constant in Regex.Matches(code,@"const ImGuiWindowFlags (\w+)=([^;]+);"))
            flags=Regex.Replace(flags,@"(?<![\w.])"+constant.Groups[1].Value+@"(?!\w)",constant.Groups[2].Value);
        return flags;
    }

    // An EmptyState call in « text » not drawn inside its own wrap width, reached through the methods it calls or names; its path when found.
    private static string? WrappedEmpty(string code,string text,string where,HashSet<string> seen)
    {
        foreach(Match call in Regex.Matches(text,@"(?<![\w.])(\w+)(?=\s*\(|\s*[,)]|\s*$)"))
        {
            var name=call.Groups[1].Value;
            var before=text[..call.Index];
            if(before.LastIndexOf("PushTextWrapPos(",StringComparison.Ordinal)>before.LastIndexOf("PopTextWrapPos(",StringComparison.Ordinal))continue;
            // A popup, menu, tooltip or child opened here is a window of its own, not the auto-sized one.
            if(Regex.Count(before,@"\.Begin(?:Popup\w*|Menu|Tooltip|Child)\(|\bTooltipScope\b")>Regex.Count(before,@"\.End(?:Popup|Menu|Tooltip|Child)\("))continue;
            if(name=="EmptyState")return where;
            if(!seen.Add(name) || Body(code,name) is not { } body)continue;
            if(WrappedEmpty(code,body,where+" > "+name,seen) is { } found)return found;
        }
        return null;
    }

    // The windows that size themselves on their content without a width: name and the text of their contents.
    private static List<(string Window,string Contents)> AutoSizedContents(string code)
    {
        var found=new List<(string,string)>();
        foreach(Match panel in Regex.Matches(code,@"PanelDefinition\s+(\w+)\s*=>\s*new\("))
        {
            var arguments=Arguments(code,panel.Index+panel.Length-1);
            if(arguments.Count<7 || !Flags(code,arguments[3]).Contains("AlwaysAutoResize")
                || arguments.Any(argument=>argument.TrimStart().StartsWith("Size:",StringComparison.Ordinal)))continue;
            found.Add((panel.Groups[1].Value,string.Join(",",arguments.Skip(4))));
        }
        foreach(Match show in Regex.Matches(code,@"Panels\?\.Show\("))
        {
            var arguments=Arguments(code,show.Index+show.Length-1);
            if(arguments.Count<5 || !Flags(code,arguments[2]).Contains("AlwaysAutoResize"))continue;
            if(arguments[3].Contains("SetNextWindowSize(") || arguments[3].Contains("SetNextWindowSizeConstraints("))continue;
            found.Add((arguments[0].Trim().Trim('$','"'),arguments[4]));
        }
        return found;
    }

    private static string[] Wrong(string code)
        =>AutoSizedContents(code).Select(content=>WrappedEmpty(code,content.Contents,content.Window,[])).OfType<string>().ToArray();

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI empty state auto size 1951: no wrapped empty line alone in an auto-sized panel without a width",()=>
        {
            var wrong=Wrong(Pages());
            Check(wrong.Length==0,"use EmptyStateLine: "+string.Join(", ",wrong));
        }),
        ("UI empty state auto size 1951: the check sees the tracker window, and the tracker uses one line",()=>
        {
            var code=Pages();
            var tracker=AutoSizedContents(code).Where(content=>content.Window=="TrackerPanel").ToArray();
            Check(tracker.Any(content=>content.Contents.Contains("DrawTrackerContents")),"tracker panel not found as an auto-sized panel");
            Check((Body(code,"DrawTrackerContents") ?? "").Contains("EmptyStateLine(\"Tracker.Empty\")"),"tracker empty line");
        }),
        ("UI empty state auto size 1951: the check refuses EmptyState in a made auto-sized panel",()=>
        {
            const string made="private PanelDefinition MadePanel=>new(\"Made\",\"###Made\",new Vector2(1,1),\n        ImGuiWindowFlags.AlwaysAutoResize,()=>true,()=>true,DrawMadeContents);\n"
                +"private void DrawMadeContents()\n{\n    if(true){EmptyState(\"Made.Empty\");return;}\n}\n";
            Check(Wrong(made).Length==1,"made panel not refused");
            Check(Wrong(made.Replace("DrawMadeContents);","DrawMadeContents,Size:new Vector2(300,0));")).Length==0,"a panel with a size is allowed");
        }),
        ("UI empty state auto size 1951: EmptyStateLine is muted and on one line",()=>
        {
            var component=File.ReadAllText(Path.Combine(Root,"UI/Components/StatusLines.cs")).Replace("\r\n","\n");
            var body=Body(component,"EmptyStateLine") ?? "";
            Check(body.Contains("ImGui.PushStyleColor(ImGuiCol.Text,Muted);") && body.Contains("ImGui.TextUnformatted(localization.Get(key));") && !body.Contains("TextWrapped"),"muted, one line");
        }),
        ("UI empty state auto size 1952: flags by a constant, as the list panels, are seen",()=>
        {
            var windows=AutoSizedContents(Pages()).Select(content=>content.Window).ToHashSet();
            foreach(var panel in new[]{"WorkshopListPanel","PreparationListPanel","CraftListListPanel"})Check(windows.Contains(panel),panel+" not seen");
            const string made="private const ImGuiWindowFlags MadeFlags=ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoNav;\n"
                +"private PanelDefinition MadePanel=>new(\"Made\",\"###Made\",new Vector2(1,1),MadeFlags,()=>true,()=>true,DrawMadeContents);\n"
                +"private void DrawMadeContents(){EmptyState(\"Made.Empty\");}\n";
            Check(Wrong(made).Length==1,"constant flags not refused");
        }),
        ("UI empty state auto size 1952: Panels?.Show without a width is seen, with a width allowed",()=>
        {
            var windows=AutoSizedContents(Pages()).Select(content=>content.Window).ToHashSet();
            foreach(var window in new[]{"TenshoFishingPanel","TenshoOceanVoyage","TenshoCharacterPanel"})Check(windows.Contains(window),window+" not seen");
            Check(!windows.Contains("TenshoGatheringPanel") && !windows.Contains("TenshoRetainerSales"),"a window with a width or constraints is seen");
            const string made="private void DrawMade()\n{\n    Panels?.Show(\"Made\",\"t\",ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.AlwaysAutoResize,()=>{ImGui.SetNextWindowPos(at,ImGuiCond.Always);},()=>\n    {\n        EmptyState(\"Made.Empty\");\n    });\n}\n";
            Check(Wrong(made).Length==1,"Panels?.Show without a width not refused");
            Check(Wrong(made.Replace("ImGuiCond.Always);}","ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(300,0),ImGuiCond.Always);}")).Length==0,"a width is allowed");
        }),
        ("UI empty state auto size 1952: EmptyState in a helper is refused, and a wrap pushed elsewhere does not exempt",()=>
        {
            const string made="private PanelDefinition MadePanel=>new(\"Made\",\"###Made\",new Vector2(1,1),ImGuiWindowFlags.AlwaysAutoResize,()=>true,()=>true,()=>DrawMadeContents(state));\n"
                +"private void DrawMadeContents(State state)\n{\n    DrawMadeEmpty(state.Lines);\n}\n"
                +"private void DrawMadeEmpty(IReadOnlyList<string> lines)\n{\n    if(lines.Count==0){EmptyState(\"Made.Empty\");return;}\n    ImGui.PushTextWrapPos(300);\n    ImGui.TextUnformatted(lines[0]);\n    ImGui.PopTextWrapPos();\n}\n";
            Check(Wrong(made).SequenceEqual(["MadePanel > DrawMadeContents > DrawMadeEmpty"]),"helper not refused: "+string.Join(", ",Wrong(made)));
            var wrapped=made.Replace("if(lines.Count==0){EmptyState(\"Made.Empty\");return;}\n    ImGui.PushTextWrapPos(300);","ImGui.PushTextWrapPos(300);\n    if(lines.Count==0){EmptyState(\"Made.Empty\");return;}");
            Check(Wrong(wrapped).Length==0,"an EmptyState inside its own wrap width is allowed");
            var menu=made.Replace("if(lines.Count==0){EmptyState(\"Made.Empty\");return;}","if(ImGui.BeginPopupContextItem(\"m\")){EmptyState(\"Made.Empty\");ImGui.EndPopup();}");
            Check(Wrong(menu).Length==0,"an EmptyState in a popup of its own is allowed");
            Check(Wrong(menu.Replace("ImGui.EndPopup();}","ImGui.EndPopup();}\n    EmptyState(\"Made.After\");")).Length==1,"an EmptyState after the popup is refused");
        }),
    ];
}
