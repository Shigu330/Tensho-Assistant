// X-413 (0.1385, review of 0.1375 to 0.1382, I2; R36.1): a text test that accepts a line-end variant with « || » next to « && » at the top
// level of its condition skips parts of it on one of the two trees (C# reads « a || b && c » as « a || (b && c) »): GearPickTip1294Tests lost
// its tooltip check that way in 0.1380, VoyageSqlite1148Tests its first part since 0.1148. A test normalises its text (Replace("\r\n","\n"))
// and requires each part; this ratchet refuses the mixed shape again.
internal static class LineEndChecks1385Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    /// <summary>The first argument of each « Check( » call of a C# text, string and char literals skipped when matching the brackets.</summary>
    internal static IEnumerable<string> Conditions(string code)
    {
        for (var at = code.IndexOf("Check(", StringComparison.Ordinal); at >= 0; at = code.IndexOf("Check(", at + 6, StringComparison.Ordinal))
        {
            if (at > 0 && (char.IsLetterOrDigit(code[at - 1]) || code[at - 1] == '_')) continue;
            var depth = 0; var start = at + 6;
            for (var i = start; i < code.Length; i++)
            {
                var c = code[i];
                if (c == '"' || c == '\'')
                {
                    var verbatim = c == '"' && i > 0 && code[i - 1] == '@';
                    for (i++; i < code.Length && code[i] != c; i++) if (!verbatim && code[i] == '\\') i++;
                    continue;
                }
                if (c is '(' or '[' or '{') depth++;
                else if (c is ')' or ']' or '}') { if (depth-- == 0) { yield return code[start..i]; break; } }
                else if (c == ',' && depth == 0) { yield return code[start..i]; break; }
            }
        }
    }

    /// <summary>« && » and « || » both at the top level of a condition (outside brackets and literals).</summary>
    internal static bool MixedTopLevel(string condition)
    {
        var depth = 0; bool and = false, or = false;
        for (var i = 0; i < condition.Length; i++)
        {
            var c = condition[i];
            if (c == '"' || c == '\'')
            {
                var verbatim = c == '"' && i > 0 && condition[i - 1] == '@';
                for (i++; i < condition.Length && condition[i] != c; i++) if (!verbatim && condition[i] == '\\') i++;
                continue;
            }
            if (c is '(' or '[' or '{') depth++;
            else if (c is ')' or ']' or '}') depth--;
            else if (depth == 0 && i + 1 < condition.Length && c == '&' && condition[i + 1] == '&') { and = true; i++; }
            else if (depth == 0 && i + 1 < condition.Length && c == '|' && condition[i + 1] == '|') { or = true; i++; }
        }
        return and && or;
    }

    private static bool LineEndVariant(string condition) => condition.Contains("\\r\\n", StringComparison.Ordinal) || condition.Contains("Environment.NewLine", StringComparison.Ordinal);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Line end checks 1385: the reader finds conditions and their top-level operators", () =>
        {
            var sample = "Check(a.Contains(\"x\\n\") || a.Contains(\"x\\r\\n\") && b, \"m, n\");\nCheck((a || b) && c, \"ok\");\nCheck(s.Contains(\"(\") && t, \"x\");";
            var found = Conditions(sample).ToList();
            Check(found.Count == 3, "three conditions: " + found.Count);
            Check(MixedTopLevel(found[0]) && LineEndVariant(found[0]), "the 0.1380 shape is found");
            Check(!MixedTopLevel(found[1]), "brackets keep the order explicit");
            Check(!MixedTopLevel(found[2]) && found[2] == "s.Contains(\"(\") && t", "a bracket inside a literal is not counted");
        }),
        ("Line end checks 1385: no test mixes && and || at the top level of a condition next to a line-end variant (X-413)", () =>
        {
            var found = new List<string>();
            foreach (var file in Directory.EnumerateFiles(Path.GetFullPath("tests/TenshoAssistant.Tests"), "*.cs"))
            {
                if (Path.GetFileName(file) == "LineEndChecks1385Tests.cs") continue;
                foreach (var condition in Conditions(File.ReadAllText(file)))
                    if (LineEndVariant(condition) && MixedTopLevel(condition)) found.Add(Path.GetFileName(file) + ": " + condition[..Math.Min(90, condition.Length)]);
            }
            Check(found.Count == 0, "normalise the line ends (Replace(\"\\r\\n\",\"\\n\")) and require each part: " + string.Join(" | ", found.Take(4)));
        }),
    ];
}
