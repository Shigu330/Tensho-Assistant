using System.Text.RegularExpressions;

// X-881 (0.1892, review of 0.1891, M1): TipTextNames1891Tests follows only the bare « TipText(name); » and skips every guarded call.
// AlarmButton showed the hole: it took its help as a parameter and drew « if(TipHovered())TipText(tooltip+…) »; its twelve callers
// formatted that parameter at each frame (a period, a fishing window, a slot), hovered or not, and only the end of the text was guarded.
// This ratchet takes every call of TipText or WrappedTooltip, guarded or not, bare or composed (lambda, interpolation, concatenation),
// and follows each string parameter of the enclosing method (or local function) used in it to its callers: a caller that fills it with
// a computed text, outside a hover guard on its own line, builds the help at each frame. The fix is the AlarmButton one: the component
// takes no help, and its caller writes « if(TipHovered())…Tip(…) » right after it.
// Limit, written: one step (a parameter passed on as a name counts as ready), string parameters only (a Func<string> is built on demand),
// and a caller's guard is seen on the caller's line only.
internal static class TipTextParameters1892Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly Regex HelpCall = new(@"(?<![\w.])(?:TipText|WrappedTooltip)\(");
    private static readonly Regex Literal = new(@"(?<![\$@""])""(?:[^""\\\n]|\\.)*""");
    private static readonly Regex Identifier = new(@"(?<![\w.""\\])[A-Za-z_]\w*(?![\w(])");
    // Ready here too: an on/off word chosen by a value (OnOff: a translated text of a fixed key, nothing formatted).
    private static readonly Regex OnOff = new(@"^OnOff\([^()]*\)$");

    /// <summary>The callers that fill, with a text computed at each frame, a string parameter used in a help text, as « file:line ».</summary>
    internal static IReadOnlyList<string> Traced(IReadOnlyList<(string Name, string Text)> sources)
    {
        var found = new List<string>();
        foreach (var (file, text) in sources)
            foreach (Match call in HelpCall.Matches(text))
            {
                if (TipTextNames1891Tests.Arguments(text, call.Index + call.Length - 1) is not { Count: 1 } arguments) continue;
                var signature = TipTextNames1891Tests.Signature.Matches(text[..call.Index]).LastOrDefault();
                if (signature is null) continue;
                var parameters = TipTextNames1891Tests.Split(signature.Groups[2].Value, true);
                var names = Identifier.Matches(Literal.Replace(arguments[0], "\"\"")).Select(match => match.Value).Distinct();
                foreach (var name in names)
                {
                    var index = parameters.FindIndex(parameter => Regex.IsMatch(parameter, @"^string\??\s+" + name + @"(?:\s*=.*)?$"));
                    if (index < 0) continue;
                    var method = signature.Groups[1].Value;
                    // A local function (indented deeper than a member) is called inside its member only; a member, in any file.
                    var local = signature.Value.TakeWhile(c => c is ' ' or '\t').Count() > 4;
                    var members = TipTextNames1891Tests.Signature.Matches(text).Where(match => match.Value.TakeWhile(c => c is ' ' or '\t').Count() == 4).ToArray();
                    var from = local ? members.LastOrDefault(match => match.Index < signature.Index)?.Index ?? 0 : 0;
                    var to = local ? members.FirstOrDefault(match => match.Index > signature.Index)?.Index ?? text.Length : int.MaxValue;
                    foreach (var (callerFile, callerText) in local ? [(file, text)] : sources)
                        foreach (Match use in Regex.Matches(callerText, @"(?<![\w.])" + method + @"\("))
                        {
                            if (callerFile == file && use.Index == signature.Groups[1].Index) continue;
                            if (use.Index < from || use.Index >= to) continue;
                            var lineStart = callerText.LastIndexOf('\n', use.Index) + 1;
                            if (TipTextNames1891Tests.Guarded(callerText[lineStart..use.Index])) continue;
                            if (TipTextNames1891Tests.Arguments(callerText, use.Index + use.Length - 1) is not { } given || given.Count <= index) continue;
                            var argument = given[index];
                            if (Regex.IsMatch(argument, @"^\w+\s*:(?!:)")) continue;
                            if (!TipTextNames1891Tests.Ready.IsMatch(argument) && !OnOff.IsMatch(argument))
                                found.Add($"{callerFile}:{TipTextNames1891Tests.Line(callerText, use.Index)} ({method}, its « {name} » built at each frame)");
                        }
                }
            }
        return found.Distinct().ToArray();
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("TipText 1892: a string parameter used in a help text, guarded or composed, is not filled with a text computed at each frame", () =>
        {
            var traced = Traced(TipTextNames1891Tests.Sources());
            Check(traced.Count == 0, "a help text built at each frame by the caller of a component: the component takes no help, its caller writes if(TipHovered())…Tip(…) after it: " + string.Join(", ", traced));
        }),
        ("TipText 1892: the tracing itself: guarded and composed uses, guarded callers, ready texts, local functions; its limit (one step)", () =>
        {
            static IReadOnlyList<string> One(string text) => Traced([("a", text)]);
            const string bell = "    void Bell(string id,string tooltip)\n    {\n        ImGui.SmallButton(id);\n        if(TipHovered())TipText(tooltip+\"\\n\"+localization.Get(\"A.L\"));\n    }\n";
            Check(One(bell + "    void A()\n    {\n        Bell(\"x\",localization.Format(\"A.B\",y));\n    }").Count == 1, "a guarded, composed use: the caller formats it at each frame");
            Check(One(bell + "    void A()\n    {\n        Bell(\"x\",localization.Get(\"A.B\"));\n        Bell(\"y\",names[i]);\n    }").Count == 0, "ready texts");
            Check(One("    void Tip2(string text)\n        =>TipText(text+\"!\");\n    void A()\n    {\n        if(TipHovered())Tip2(localization.Format(\"A.B\",y));\n    }").Count == 0, "a caller behind its own guard");
            Check(One("    void Row(string tip)\n    {\n        if(TipHovered())TipText(()=>$\"{tip} · {item.Name}\");\n    }\n    void A()\n    {\n        Row($\"{a} {b}\");\n    }").Count == 1, "a use in a lambda and an interpolation");
            Check(One("    void Row(string label)\n    {\n        if(TipHovered())TipText(\"label\");\n    }\n    void A()\n    {\n        Row($\"{a} {b}\");\n    }").Count == 0, "a word inside a literal is no use");
            Check(One("    void A()\n    {\n        Row(\"x\",localization.Get(\"A.C\"));\n        void Row(string label,string tip)\n        {\n            if(TipHovered())WrappedTooltip(label+tip);\n        }\n    }\n    void B()\n    {\n        Row(\"x\",localization.Format(\"A.B\",y));\n    }").Count == 0, "a local function: its own member's calls only");
            Check(One("    void Row(string label,Func<string> tip)\n    {\n        if(TipHovered())TipText(tip());\n    }\n    void A()\n    {\n        Row(\"x\",()=>localization.Format(\"A.B\",y));\n    }").Count == 0, "the limit, written: a Func<string> is built on demand");
        }),
        ("TipText 1892: AlarmButton takes no help; each bell's help follows it behind its guard (AlarmTip)", () =>
        {
            var all = TipTextNames1891Tests.Sources();
            var alarms = all.Single(source => source.Name == "UI/Components/AlarmBell.cs").Text;
            Check(alarms.Contains("private void AlarmButton(string id,Func<IReadOnlyList<TenshoAlarm>> create)", StringComparison.Ordinal), "AlarmButton takes its help again");
            Check(alarms.Contains("private void AlarmTip(string text,bool daily=false)", StringComparison.Ordinal), "AlarmTip is gone");
            var bells = 0;
            foreach (var (file, text) in all)
                foreach (Match use in Regex.Matches(text, @"(?<![\w.])AlarmButton\("))
                {
                    if (text[(text.LastIndexOf('\n', use.Index) + 1)..use.Index].Contains("void ", StringComparison.Ordinal)) continue;
                    var end = text.IndexOf(");\n", use.Index, StringComparison.Ordinal);
                    var next = text[(end + 3)..].TrimStart(' ', '\t');
                    Check(next.StartsWith("if(TipHovered())AlarmTip(", StringComparison.Ordinal), $"{file}:{TipTextNames1891Tests.Line(text, use.Index)}: a bell without its help right after it");
                    bells++;
                }
            Check(bells >= 16, $"the bells counted: {bells}");
        }),
    ];
}
