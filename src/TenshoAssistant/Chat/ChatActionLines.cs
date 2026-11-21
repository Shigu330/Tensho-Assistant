namespace TenshoAssistant.Chat;

/// <summary>
/// X-734 (0.1734, T-0.1720-1 seen in game on 21/11: the line of Teleport showed no tooltip): a combat line names an action in its
/// plain text, neither linked nor coloured: the game's template of that line (LogMessage, the client's language) puts the action's name as a
/// string (« … utilisez &lt;string(lstr1)&gt;. », row 533, kind 43). The coloured text 0.1720 took for an action is an item's name (« … utilisez
/// &lt;frnoun(Item…)&gt;. », row 535, kind 44). Each template of a combat kind says what follows the action's name up to the line's end
/// (« . », « is interrupted. », « ein. »): the name is the longest action name that ends just before it and starts a word (any place in a
/// language written without spaces). docs/evidence/0.1734-chat-action-lines.md. Pure (tests); built once from the client's sheets, apart.
/// </summary>
public sealed class ChatActionLines
{
    /// <summary>The template's string parameter the game fills with the action's name.</summary>
    public const string Slot="<string(lstr1)>";
    private readonly Dictionary<int,string[]> ends;
    private readonly HashSet<string> names;
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> lookup;
    private readonly int longest;
    private readonly bool spaced;

    /// <param name="templates">The game's line templates (LogMessage: its kind and its text as a macro string).</param>
    /// <param name="actionNames">Every action's name (Action sheet), as the game writes it in a line.</param>
    /// <param name="spaced">Words are separated by spaces (every client language but Japanese): a name starts a word.</param>
    public ChatActionLines(IEnumerable<(int Kind,string Macro)> templates,IEnumerable<string> actionNames,bool spaced)
    {
        ends=templates.Select(template=>(template.Kind,End:End(template.Macro))).Where(template=>template.End is not null).GroupBy(template=>template.Kind)
            .ToDictionary(group=>group.Key,group=>group.Select(template=>template.End!).Distinct(StringComparer.Ordinal).OrderByDescending(end=>end.Length).ToArray());
        names=new HashSet<string>(actionNames.Select(name=>name.Trim()).Where(name=>name.Length>0),StringComparer.Ordinal);
        lookup=names.GetAlternateLookup<ReadOnlySpan<char>>();
        longest=names.Count==0 ? 0:names.Max(name=>name.Length);
        this.spaced=spaced;
    }

    /// <summary>The kinds whose lines can end with an action's name (for the log).</summary>
    public int Kinds=>ends.Count;
    /// <summary>The actions' names known.</summary>
    public int Names=>names.Count;

    /// <summary>What a template writes after the action's name, to the line's end (a non-breaking space for &lt;nbsp&gt;, the game's escapes
    /// read); null when the template names no action, ends with it, or writes anything but plain text after it (another macro).</summary>
    public static string? End(string macro)
    {
        var at=macro.LastIndexOf(Slot,StringComparison.Ordinal);
        if(at<0)return null;
        var rest=macro[(at+Slot.Length)..].Replace("<nbsp>"," ",StringComparison.Ordinal);
        if(rest.Length==0 || rest.Contains('<',StringComparison.Ordinal))return null;
        var end=new System.Text.StringBuilder(rest.Length);
        for(var index=0;index<rest.Length;index++)
        {
            if(rest[index]=='\\' && index+1<rest.Length)index++;
            end.Append(rest[index]);
        }
        return end.ToString();
    }

    /// <summary>Where the last text of a line of <paramref name="kind"/> names an action: the longest known name that ends just before one of
    /// the kind's ends and starts a word; null when none.</summary>
    public (int Start,int Length)? Find(int kind,string text)
    {
        if(longest==0 || !ends.TryGetValue(kind,out var list))return null;
        foreach(var end in list)
        {
            if(!text.EndsWith(end,StringComparison.Ordinal))continue;
            var region=text.AsSpan(0,text.Length-end.Length);
            for(var start=Math.Max(0,region.Length-longest);start<region.Length;start++)
            {
                if(char.IsWhiteSpace(region[start]))continue;
                if(spaced && start>0 && char.IsLetterOrDigit(region[start-1]))continue;
                if(lookup.Contains(region[start..]))return (start,region.Length-start);
            }
        }
        return null;
    }
}
