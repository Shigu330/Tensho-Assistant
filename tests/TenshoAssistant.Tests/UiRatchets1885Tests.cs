using System.Text.RegularExpressions;

// 0.1885 (X-607, user request of 18/11: « code as easy as possible to maintain, above all for a human »). The survey of the gestures still
// written by hand page by page, after the tab bars and the tables (UiRatchets1612Tests): the tree nodes now all go through
// UiComponents.TreeNode (they open, as the headers, while the Settings search looks for a setting); a tooltip of the last item goes through
// TipText (its text built only on hover); the rest can only go down: the collapsing headers written by hand (UiComponents.Header for a new
// one), the glyph font pushed by hand (ToolButton, GlyphButton, DrawCentredGlyph) and the tooltips drawn by hand (Tip, TipText, TooltipScope).
internal static class UiRatchets1885Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly string Root = Path.GetFullPath("src/TenshoAssistant");

    /// <summary>The plugin's sources outside the common components (UI/), by path relative to src/TenshoAssistant.</summary>
    private static IEnumerable<(string Name, string Text)> Pages()
        => Directory.EnumerateFiles(Root, "*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/'))
            .Where(name => !name.StartsWith("UI/", StringComparison.Ordinal) && !name.Split('/').Any(part => part is "obj" or "bin"))
            .Select(name => (name, File.ReadAllText(Path.Combine(Root, name))));

    // Written by hand on 27/11, outside UI/: lower a number when its gesture moves to its component.
    private const int HandHeaders = 4, HandGlyphFonts = 27, HandTooltips = 26;

    private static int Count(string piece) => Pages().Sum(page => Regex.Matches(page.Text, piece).Count);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("UI ratchets 1885: no tree node written by hand: UiComponents.TreeNode (it opens for the Settings search)", () =>
        {
            var hand = Pages().Where(page => page.Text.Contains("ImGui.TreeNode(", StringComparison.Ordinal)).Select(page => page.Name).ToArray();
            Check(hand.Length == 0, "ImGui.TreeNode( written by hand: use UI.UiComponents.TreeNode(label): " + string.Join(", ", hand));
            Check(Count(@"UiComponents\.TreeNode\(") >= 23, "the tree nodes go through the component");
        }),
        ("UI ratchets 1885: no « if hovered, tooltip » written by hand: TipText(text) or if(TipHovered())TipText(text)", () =>
        {
            var hand = Pages().Where(page => Regex.IsMatch(page.Text, @"IsItemHovered\((?:ImGuiHoveredFlags\.AllowWhenDisabled)?\)\s*\)\s*WrappedTooltip\("))
                .Select(page => page.Name).ToArray();
            Check(hand.Length == 0, "if(ImGui.IsItemHovered())WrappedTooltip(…) written by hand: TipText: " + string.Join(", ", hand));
            Check(!Pages().Any(page => page.Text.Contains("ImGui.SetTooltip(", StringComparison.Ordinal) || page.Text.Contains("ImGui.BeginTooltip(", StringComparison.Ordinal)),
                "a tooltip opened by hand: TooltipScope.Begin() (UI/Components/Tooltip.cs)");
        }),
        ("UI ratchets 1885: the headers, glyph fonts and tooltips still drawn by hand can only go down", () =>
        {
            var headers = Count(@"ImGui\.CollapsingHeader\(");
            var glyphs = Count(@"PushFont\((?:Dalamud\.Interface\.)?UiBuilder\.IconFont\)");
            var tooltips = Count(@"(?<![\w.])WrappedTooltip\(");
            Check(headers <= HandHeaders, $"{headers} collapsing headers by hand (at most {HandHeaders}): UiComponents.Header for a new one");
            Check(glyphs <= HandGlyphFonts, $"{glyphs} glyph fonts pushed by hand (at most {HandGlyphFonts}): ToolButton, GlyphButton or DrawCentredGlyph");
            Check(tooltips <= HandTooltips, $"{tooltips} tooltips drawn by hand (at most {HandTooltips}): Tip, TipText or TooltipScope");
            Check(headers == HandHeaders && glyphs == HandGlyphFonts && tooltips == HandTooltips,
                $"fewer by hand ({headers}, {glyphs}, {tooltips}): lower HandHeaders, HandGlyphFonts and HandTooltips to them, so the ratchet holds");
        }),
        ("UI ratchets 1885: the components exist, and CONTRIBUTING names them in both halves", () =>
        {
            var components = File.ReadAllText(Path.Combine(Root, "UI/UiComponents.cs"));
            Check(components.Contains("public static bool TreeNode(string label)", StringComparison.Ordinal) && components.Contains("public static bool Header(string label", StringComparison.Ordinal), "TreeNode and Header");
            var tooltip = File.ReadAllText(Path.Combine(Root, "UI/Components/Tooltip.cs"));
            Check(tooltip.Contains("private static bool TipHovered()", StringComparison.Ordinal) && !tooltip.Contains("Func<string>", StringComparison.Ordinal), "TipText and its hover guard, without a closure (X-878, TipText1889Tests)");
            var contributing = File.ReadAllText(Path.GetFullPath("CONTRIBUTING.md"));
            foreach (var name in new[] { "UiComponents.Header", "UiComponents.TreeNode", "TipText(", "GlyphButton" })
                Check(contributing.Split(name).Length >= 3, "named in both halves of CONTRIBUTING: " + name);
        }),
    ];
}
