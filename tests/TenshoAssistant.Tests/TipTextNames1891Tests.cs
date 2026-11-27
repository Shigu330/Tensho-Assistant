using System.Text.RegularExpressions;

// X-880 (0.1891, review of 0.1890, M1): the TipText ratchet of 0.1889 takes any name as a text already made. This one traces such a name
// one step back: a local of the same method assigned with a computed text (formatted, joined, concatenated, a call) and no guard, or a
// string parameter of the enclosing method (or local function) that a caller fills with a computed text. Both build the tooltip at each
// frame, hovered or not: the help is then built by the caller behind « if(TipHovered())TipText(…) », after the item.
// Limit, written: one step only (a parameter passed on to another helper is not followed), and a pattern variable (« is { } name »)
// passes as ready: it is the value the line needs anyway (MacroChain's validation marker, Gear's hint field).
internal static class TipTextNames1891Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly string Root = Path.GetFullPath("src/TenshoAssistant");

    private static IReadOnlyList<(string Name, string Text)> Sources()
        => Directory.EnumerateFiles(Root, "*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/'))
            .Where(name => name != "UI/Components/Tooltip.cs" && !name.Split('/').Any(part => part is "obj" or "bin"))
            .Select(name => (name, File.ReadAllText(Path.Combine(Root, name)).Replace("\r\n", "\n"))).ToArray();

    private static readonly Regex Ready = new(@"^(?:[A-Za-z_][\w.]*(?:\[[\w.+-]+\])?|localization\.Get\(""[^""]*""\)|null|""[^""\\]*"")$");
    private static readonly Regex NameCall = new(@"(?<![\w.])TipText\(([A-Za-z_]\w*)\);");
    private static readonly Regex Signature = new(@"(?m)^[ \t]*(?:(?:private|public|internal|protected|static|override)\s+)*(?:void|bool|string|int|float|[A-Z]\w*(?:<[^>\n]*>)?\??)\s+(\w+)\(([^()\n]*)\)[ \t]*$");

    private static int Line(string text, int index) => text[..index].Count(c => c == '\n') + 1;
    private static bool Guarded(string line) => line.Contains("TipHovered()", StringComparison.Ordinal) || line.Contains("IsItemHovered(", StringComparison.Ordinal);

    /// <summary>Splits at the top-level commas (parentheses, brackets, braces, angle brackets in types and strings respected).</summary>
    internal static List<string> Split(string list, bool types)
    {
        var parts = new List<string>(); var depth = 0; var start = 0; var quoted = false;
        for (var at = 0; at < list.Length; at++)
        {
            var c = list[at];
            if (quoted) { if (c == '\\') at++; else if (c == '"') quoted = false; continue; }
            if (c == '"') quoted = true;
            else if (c is '(' or '[' or '{' || types && c == '<') depth++;
            else if (c is ')' or ']' or '}' || types && c == '>') depth--;
            else if (c == ',' && depth == 0) { parts.Add(list[start..at].Trim()); start = at + 1; }
        }
        parts.Add(list[start..].Trim());
        return parts;
    }

    /// <summary>The arguments of the call whose opening parenthesis is at <paramref name="open"/>, or null when it is not closed.</summary>
    private static List<string>? Arguments(string text, int open)
    {
        var depth = 0; var quoted = false;
        for (var at = open; at < text.Length; at++)
        {
            var c = text[at];
            if (quoted) { if (c == '\\') at++; else if (c == '"') quoted = false; continue; }
            if (c == '"') quoted = true;
            else if (c is '(' or '[' or '{') depth++;
            else if (c is ')' or ']' or '}' && --depth == 0) return Split(text[(open + 1)..at], false);
        }
        return null;
    }

    /// <summary>The TipText(name) calls whose name holds a text computed at each frame, as « file:line ».</summary>
    internal static IReadOnlyList<string> Traced(IReadOnlyList<(string Name, string Text)> sources)
    {
        var found = new List<string>();
        foreach (var (file, text) in sources)
            foreach (Match call in NameCall.Matches(text))
            {
                var lineStart = text.LastIndexOf('\n', call.Index) + 1;
                if (Guarded(text[lineStart..call.Index])) continue;
                var name = call.Groups[1].Value;
                var signature = Signature.Matches(text[..call.Index]).LastOrDefault();
                var scope = signature is null ? 0 : signature.Index;
                // A local assigned in the same method: its last assignment before the call.
                var assigned = Regex.Matches(text[scope..call.Index], @"(?<![\w.])" + name + @"\s*(\+?)=(?![=>])([^;]*);").LastOrDefault();
                if (assigned is not null)
                {
                    var at = scope + assigned.Index;
                    var line = text[(text.LastIndexOf('\n', at) + 1)..at];
                    var value = assigned.Groups[2].Value.Trim();
                    if (!Guarded(line) && (assigned.Groups[1].Value == "+" || !Ready.IsMatch(value)))
                        found.Add($"{file}:{Line(text, call.Index)} ({name}, assigned at line {Line(text, at)})");
                    continue;
                }
                if (signature is null) continue;
                var parameters = Split(signature.Groups[2].Value, true);
                var index = parameters.FindIndex(parameter => Regex.IsMatch(parameter, @"^string\??\s+" + name + @"(?:\s*=.*)?$"));
                if (index < 0) continue;
                var method = signature.Groups[1].Value;
                // A local function (indented deeper than a member) is called inside its member only; a member, in any file.
                var local = signature.Value.TakeWhile(c => c is ' ' or '\t').Count() > 4;
                var members = Signature.Matches(text).Where(match => match.Value.TakeWhile(c => c is ' ' or '\t').Count() == 4).ToArray();
                var from = local ? members.LastOrDefault(match => match.Index < signature.Index)?.Index ?? 0 : 0;
                var to = local ? members.FirstOrDefault(match => match.Index > signature.Index)?.Index ?? text.Length : int.MaxValue;
                foreach (var (callerFile, callerText) in local ? [(file, text)] : sources)
                    foreach (Match use in Regex.Matches(callerText, @"(?<![\w.])" + method + @"\("))
                    {
                        if (callerFile == file && use.Index == signature.Groups[1].Index) continue;
                        if (use.Index < from || use.Index >= to) continue;
                        if (Arguments(callerText, use.Index + use.Length - 1) is not { } arguments || arguments.Count <= index) continue;
                        var argument = arguments[index];
                        if (argument.Contains(':') && Regex.IsMatch(argument, @"^\w+\s*:")) continue;
                        if (!Ready.IsMatch(argument))
                            found.Add($"{callerFile}:{Line(callerText, use.Index)} ({method}, its « {name} » built at each frame)");
                    }
            }
        return found;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("TipText 1891: a name given to TipText holds no text computed at each frame (a local assigned before, or a parameter its callers fill)", () =>
        {
            var traced = Traced(Sources());
            Check(traced.Count == 0, "a tooltip text built at each frame, behind a name: build it on hover, if(TipHovered())TipText(…): " + string.Join(", ", traced));
        }),
        ("TipText 1891: the tracing itself: locals and parameters, ready or computed, guarded or not; its limit (one step, pattern variables)", () =>
        {
            static IReadOnlyList<string> One(string text) => Traced([("a", text)]);
            Check(One("    void A()\n    {\n        var text=localization.Format(\"A.B\",x);\n        TipText(text);\n    }").Count == 1, "a local formatted before");
            Check(One("    void A()\n    {\n        var text=localization.Get(\"A.B\");\n        TipText(text);\n    }").Count == 0, "a local of a fixed key");
            Check(One("    void A()\n    {\n        var text=row.Name;\n        text+=\"!\";\n        TipText(text);\n    }").Count == 1, "a local added to");
            Check(One("    void A()\n    {\n        if(TipHovered()){var text=Build(row);TipText(text);}\n    }").Count == 0, "a local built behind the guard");
            Check(One("    void A()\n    {\n        Row(\"x\",localization.Format(\"A.B\",y));\n        Row(\"x\",localization.Get(\"A.C\"));\n    }\n    void Row(string label,string tip)\n    {\n        ImGui.TextUnformatted(label);\n        TipText(tip);\n    }").Count == 1, "a parameter filled with a formatted text by one caller");
            Check(One("    void A()\n    {\n        Row(\"x\",localization.Get(\"A.C\"));\n        Row(\"y\",names[i]);\n    }\n    void Row(string label,string tip)\n    {\n        TipText(tip);\n    }").Count == 0, "a parameter filled with ready texts");
            Check(One("    void A()\n    {\n        if(macro.Error() is { } validation)TipText(validation);\n    }").Count == 0, "the limit, written: a pattern variable passes");
            Check(One("    void A()\n    {\n        Row(\"x\",localization.Get(\"A.C\"));\n        void Row(string label,string tip)\n        {\n            TipText(tip);\n        }\n    }\n    void B()\n    {\n        Row(\"x\",localization.Format(\"A.B\",y));\n    }").Count == 0, "a local function: its own member's calls only");
            Check(Split("string a,Func<int,string> b,string? c=null", true).Count == 3 && Split("a(b,c),\"x,y\",d[1,2]", false).Count == 3, "top-level split");
        }),
        ("TipText 1891: the sites of X-880 build their help on hover; HelpMarkerText (a help passed built) is gone", () =>
        {
            var all = Sources();
            Check(all.All(source => !source.Text.Contains("HelpMarkerText(", StringComparison.Ordinal))
                  && !File.ReadAllText(Path.Combine(Root, "UI/Components/Tooltip.cs")).Contains("void HelpMarkerText(", StringComparison.Ordinal), "HelpMarkerText is back: its text is built before the call");
            string Of(string name) => all.Single(source => source.Name == name).Text;
            Check(!Of("UI/Components/Gauges.cs").Contains("string tip,bool chosen", StringComparison.Ordinal), "Gauge takes its help built");
            Check(Regex.Matches(Of("MainWindow.Dashboard.cs"), @"\)\)\);\n\s*if\(TipHovered\(\)\)TipText\(").Count == 4, "the four gauges: their help on hover, after the gauge");
            Check(Of("MainWindow.CraftLog.cs").Contains("CraftLogTitle(localization.Get(\"CraftLogUi.SectionResult\"));\n            ImGui.SameLine();DrawHelpIcon();\n            if(TipHovered())TipText(localization.Format(\"CraftLogUi.Assumptions\",", StringComparison.Ordinal)
                  && !Of("MainWindow.CraftLog.cs").Contains("string? help", StringComparison.Ordinal), "the craft log result: its assumptions on hover of its marker");
            Check(Of("MainWindow.DutyFinder.cs").Contains("else{DrawHelpIcon();if(TipHovered())TipText(localization.Get(\"DutyUi.Source\")+\"\\n\"+localization.Get(\"DutyUi.Partial\"));}", StringComparison.Ordinal), "the duty source note on hover");
        }),
    ];
}
