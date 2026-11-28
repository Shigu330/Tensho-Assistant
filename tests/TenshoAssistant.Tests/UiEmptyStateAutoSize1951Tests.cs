using System.Text.RegularExpressions;

// 0.1951 (X-908, review of 0.1950, I1). A wrapped « nothing here » line (EmptyState, ImGui.TextWrapped) alone in a window that sizes itself
// on its content (AlwaysAutoResize) without a width wraps on the window's width, which follows the text: the window shrinks to a narrow
// column. Such a window uses EmptyStateLine (one line); a panel with a size, or a body that sets its own wrap width, may keep EmptyState.
internal static class UiEmptyStateAutoSize1951Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static string Pages()
        =>string.Concat(Directory.EnumerateFiles(Root,"MainWindow*.cs",SearchOption.TopDirectoryOnly).Select(path=>File.ReadAllText(path).Replace("\r\n","\n")+"\n"));

    // The body of « private void NAME() », braces matched; null when the method is not there.
    private static string? Body(string code,string name)
    {
        var match=Regex.Match(code,@"\bvoid "+Regex.Escape(name)+@"\(\)\s*\{");
        if(!match.Success)return null;
        var depth=0;
        for(var at=match.Index+match.Length-1;at<code.Length;at++)
        {
            if(code[at]=='{')depth++;
            else if(code[at]=='}' && --depth==0)return code.Substring(match.Index,at-match.Index+1);
        }
        return null;
    }

    // The panels that size themselves on their content without a size: the methods named in their definition, with their body.
    private static List<(string Panel,string Method,string Body)> AutoSizedContents(string code)
    {
        var found=new List<(string,string,string)>();
        foreach(Match panel in Regex.Matches(code,@"PanelDefinition\s+(\w+)\s*=>\s*new\((.*?)\);\n",RegexOptions.Singleline))
        {
            var text=panel.Groups[2].Value;
            if(!text.Contains("AlwaysAutoResize") || text.Contains("Size:"))continue;
            foreach(Match name in Regex.Matches(text,@"(?<![\w.])(Draw\w+)(?=[,)]|$)"))
                if(Body(code,name.Groups[1].Value) is { } body)found.Add((panel.Groups[1].Value,name.Groups[1].Value,body));
        }
        return found;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI empty state auto size 1951: no wrapped empty line alone in an auto-sized panel without a width",()=>
        {
            var wrong=AutoSizedContents(Pages())
                .Where(content=>content.Body.Contains("EmptyState(") && !content.Body.Contains("PushTextWrapPos("))
                .Select(content=>content.Panel+"."+content.Method).ToArray();
            Check(wrong.Length==0,"use EmptyStateLine: "+string.Join(", ",wrong));
        }),
        ("UI empty state auto size 1951: the check sees the tracker window, and the tracker uses one line",()=>
        {
            var code=Pages();
            var tracker=AutoSizedContents(code).Where(content=>content.Panel=="TrackerPanel").ToArray();
            Check(tracker.Any(content=>content.Method=="DrawTrackerContents"),"tracker panel not found as an auto-sized panel");
            Check(tracker.Any(content=>content.Body.Contains("EmptyStateLine(\"Tracker.Empty\")")),"tracker empty line");
        }),
        ("UI empty state auto size 1951: the check refuses EmptyState in a made auto-sized panel",()=>
        {
            const string made="private PanelDefinition MadePanel=>new(\"Made\",\"###Made\",new Vector2(1,1),\n        ImGuiWindowFlags.AlwaysAutoResize,()=>true,()=>true,DrawMadeContents);\n"
                +"private void DrawMadeContents()\n{\n    if(true){EmptyState(\"Made.Empty\");return;}\n}\n";
            Check(AutoSizedContents(made).Any(content=>content.Body.Contains("EmptyState(")),"made panel not refused");
            Check(AutoSizedContents(made.Replace("DrawMadeContents);","DrawMadeContents,Size:new Vector2(300,0));")).Count==0,"a panel with a size is allowed");
        }),
        ("UI empty state auto size 1951: EmptyStateLine is muted and on one line",()=>
        {
            var component=File.ReadAllText(Path.Combine(Root,"UI/Components/StatusLines.cs")).Replace("\r\n","\n");
            var body=Body(component.Replace("void EmptyStateLine(string key)","void EmptyStateLine()"),"EmptyStateLine") ?? "";
            Check(body.Contains("ImGui.PushStyleColor(ImGuiCol.Text,Muted);") && body.Contains("ImGui.TextUnformatted(localization.Get(key));") && !body.Contains("TextWrapped"),"muted, one line");
        }),
    ];
}
