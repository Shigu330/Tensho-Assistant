using System.Text.RegularExpressions;

// X-878 (0.1889, review of 0.1888, M1): a tooltip allocates nothing while the mouse is elsewhere. TipText(()=>text) made a closure per
// call and per frame, hovered or not (one per row of a long list), and TipText(string) wrapped its text in one more; a computed text
// (formatted, joined, concatenated) passed to TipText was built at each frame too. Now TipText takes a string only, and a computed text
// sits behind its hover guard: if(TipHovered())TipText(…). A ready text (a name, a field, localization.Get of a fixed key) goes as is.
internal static class TipText1889Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly string Root = Path.GetFullPath("src/TenshoAssistant");

    private static IEnumerable<(string Name, string Text)> Sources()
        => Directory.EnumerateFiles(Root, "*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/'))
            .Where(name => name != "UI/Components/Tooltip.cs" && !name.Split('/').Any(part => part is "obj" or "bin"))
            .Select(name => (name, File.ReadAllText(Path.Combine(Root, name)).Replace("\r\n", "\n")));

    // A text already made: a name or a field (an index allowed), or the translation of a fixed key (cached by the localization).
    private static readonly Regex Ready = new(@"\G(?:[A-Za-z_][\w.]*(?:\[[\w.+-]+\])?|localization\.Get\(""[^""]*""\))\);");
    private static readonly Regex Call = new(@"(?<![\w.])TipText\(");

    /// <summary>The TipText calls whose text is computed without its hover guard on the same line, as « file:line ».</summary>
    internal static IReadOnlyList<string> Unguarded(string name, string text)
    {
        var found = new List<string>();
        foreach (Match match in Call.Matches(text))
        {
            var lineStart = text.LastIndexOf('\n', match.Index) + 1;
            var before = text[lineStart..match.Index];
            if (before.Contains("private static void TipText(", StringComparison.Ordinal)) continue;
            if (before.Contains("TipHovered()", StringComparison.Ordinal) || before.Contains("IsItemHovered(", StringComparison.Ordinal)) continue;
            if (Ready.IsMatch(text, match.Index + match.Length)) continue;
            found.Add($"{name}:{text[..match.Index].Count(c => c == '\n') + 1}");
        }
        return found;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("TipText 1889: no closure: TipText takes a ready string, never a Func, and wraps nothing", () =>
        {
            var tooltip = File.ReadAllText(Path.Combine(Root, "UI/Components/Tooltip.cs"));
            Check(!tooltip.Contains("Func<string>", StringComparison.Ordinal), "TipText(Func<string>) is back: a closure per call and per frame");
            Check(!tooltip.Contains("=>TipText(", StringComparison.Ordinal), "TipText(string) wraps its text in a lambda");
            Check(tooltip.Contains("private static void TipText(string text)\n    {\n        if(TipHovered())WrappedTooltip(text);", StringComparison.Ordinal)
                  || tooltip.Replace("\r\n", "\n").Contains("private static void TipText(string text)\n    {\n        if(TipHovered())WrappedTooltip(text);", StringComparison.Ordinal),
                "TipText(string) shows its text on hover");
            Check(tooltip.Contains("private static bool TipHovered()=>ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);", StringComparison.Ordinal), "the guard, also when greyed");
            var lambdas = Sources().Where(source => Regex.IsMatch(source.Text, @"(?<![\w.])TipText\(\s*\(\s*\)\s*=>")).Select(source => source.Name).ToArray();
            Check(lambdas.Length == 0, "TipText(()=>…): if(TipHovered())TipText(…): " + string.Join(", ", lambdas));
        }),
        ("TipText 1889: a computed tooltip text is built only behind its hover guard", () =>
        {
            var unguarded = Sources().SelectMany(source => Unguarded(source.Name, source.Text)).ToArray();
            Check(unguarded.Length == 0, "a tooltip text built at each frame: if(TipHovered())TipText(…): " + string.Join(", ", unguarded));
            Check(Sources().Sum(source => Regex.Matches(source.Text, @"if\(TipHovered\(\)\)TipText\(").Count) >= 100, "the computed tooltips keep their guard");
        }),
        ("TipText 1889: the ratchet itself: a ready text passes, a computed one needs the guard on its line", () =>
        {
            Check(Unguarded("a", "        TipText(row.Description);\n        TipText(localization.Get(\"A.B\"));\n        TipText(names[index]);").Count == 0, "ready texts");
            Check(Unguarded("a", "        TipText(localization.Format(\"A.B\",x));").Count == 1, "a formatted text");
            Check(Unguarded("a", "        TipText(a+\"\\n\"+b);").Count == 1, "a concatenation");
            Check(Unguarded("a", "        TipText(string.Join(\", \",places));").Count == 1, "a join");
            Check(Unguarded("a", "        TipText(Build(row));").Count == 1, "a method call");
            Check(Unguarded("a", "        if(open)if(TipHovered())TipText(localization.Format(\"A.B\",x));").Count == 0, "guarded");
            Check(Unguarded("a", "        if(open){if(TipHovered())TipText(Build(row));}\n        else TipText(row.Name);").Count == 0, "guarded in braces before an else");
        }),
    ];
}
