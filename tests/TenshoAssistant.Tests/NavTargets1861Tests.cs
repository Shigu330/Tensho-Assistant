using System.Text.RegularExpressions;
using TenshoAssistant.Help;

// X-855 (review 0.1860, M2): NavTargets1859Tests knows a jump to a tab by a field's name (…TabRequest, requested…Tab) and a request number in a
// bar's list; X-850 and X-853 were born of fields the earlier guards did not know. Here the guard reads the two ways a tab is chosen, whatever the
// field is called: a variable passed by ref to DetailTabs (its focus) is only declared and passed, never set; an OrderedTab's Select (its fourth
// argument) is one of a written list. The one flag that was left (the item menu's « Market prices », marketFocusSummary) is a row of the table.
internal static class NavTargets1861Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static IEnumerable<(string File, string Code)> Sources() =>
        Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n")));

    // The text between an opening parenthesis (at start) and its closing one, and its arguments split at depth 0.
    private static (string Inner, List<string> Args) Call(string code, int start)
    {
        var depth = 1; var index = start;
        while (depth > 0 && index < code.Length)
        {
            var c = code[index++];
            if (c == '(') depth++;
            else if (c == ')') depth--;
        }
        var inner = code[start..Math.Max(start, index - 1)];
        var args = new List<string>(); var level = 0; var current = new System.Text.StringBuilder();
        foreach (var c in inner)
        {
            if (c is '(' or '[' or '{') level++;
            if (c is ')' or ']' or '}') level--;
            if (c == ',' && level == 0) { args.Add(current.ToString().Trim()); current.Clear(); }
            else current.Append(c);
        }
        args.Add(current.ToString().Trim());
        return (inner, args);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Nav targets 1861: a variable passed by ref to DetailTabs is only declared and passed, never set by a jump", () =>
        {
            var focus = new List<(string File, string Name)>();
            var sources = Sources().ToArray();
            foreach (var (file, code) in sources)
                foreach (Match call in Regex.Matches(code, @"\bDetailTabs\("))
                {
                    var (inner, _) = Call(code, call.Index + call.Length);
                    if (Regex.Match(inner, @"\bref\s+(\w+)\s*$") is { Success: true } name) focus.Add((file, name.Groups[1].Value));
                }
            Check(focus.Count >= 12, "the guard finds the detail bars: " + focus.Count);
            // A declaration (a field, or the item sheet's local set to null once) and the one ref: any other use is a tab chosen outside the table.
            var declared = new Regex(@"^\s*(?:private\s+)?string\?\s+[\w,]+(?:\s*=\s*null)?;\s*$");
            foreach (var (file, name) in focus)
            {
                var uses = 0; var passed = 0;
                foreach (var (other, code) in sources)
                    foreach (var line in code.Split('\n'))
                    {
                        var count = Regex.Matches(line, @"\b" + name + @"\b").Count;
                        if (count == 0) continue;
                        uses += count;
                        if (declared.IsMatch(line)) continue;
                        passed += Regex.Matches(line, @"\bref\s+" + name + @"\b").Count;
                        Check(Regex.Matches(line, @"\bref\s+" + name + @"\b").Count == count, other + ": " + name + " used outside DetailTabs: " + line.Trim());
                    }
                Check(passed == 1 && uses == 2, file + ": " + name + " declared and passed once (" + uses + " uses, " + passed + " passed)");
            }
        }),
        ("Nav targets 1861: an OrderedTab's Select is one of a written list; « Market prices » is a row of the table", () =>
        {
            // Each Select there is, by file, and why it stays outside the table of links.
            var allowed = new HashSet<string>
            {
                "DetailPage.cs|key==requested",                    // DetailTabs' own focus (the variable above)
                "MainWindow.Collections.cs|focusCollectionKind == kind", // the written exception (NavTargets' comment): a kind chosen at run time
                "MainWindow.Settings.cs|selected",                 // requestedSettingsTab==key: MainWindow.Open's Settings tab
                "MainWindow.Items.cs|openProduced",                // the recipes' default tab when another item is shown, no jump
                "MainWindow.Items.cs|openUsedIn",
                "MainWindow.TripleTriad.cs|focus && !triadExSeries", // a card's series at run time, or « Ex. » typed in the search
                "MainWindow.TripleTriad.cs|focus && triadExSeries",
            };
            var seen = new HashSet<string>();
            foreach (var (file, code) in Sources())
            {
                if (!code.Contains("OrderedTab", StringComparison.Ordinal)) continue;
                foreach (Match call in Regex.Matches(code, @"\bnew(?:\s+OrderedTab)?\s*\("))
                {
                    var (inner, args) = Call(code, call.Index + call.Length);
                    var named = Regex.Match(inner, @"\bSelect\s*:\s*([^,)]+)");
                    // An OrderedTab built with four arguments: a key, a label, a body (not text) and its Select.
                    var select = named.Success ? named.Groups[1].Value.Trim()
                        : args.Count == 4 && !args[2].StartsWith('"') && !args[2].StartsWith('$') && !args[3].StartsWith('"') ? args[3] : null;
                    if (select is null) continue;
                    var key = file + "|" + select;
                    Check(allowed.Contains(key), "a tab chosen outside the table of links: " + key);
                    seen.Add(key);
                }
            }
            Check(seen.SetEquals(allowed), "the guard reads the code: " + string.Join(", ", allowed.Except(seen)));
            Check(NavTargets.Bars["Market"] == "MarketTabs" && NavTargets.Of("Item.Market") == new NavTarget("Market", "Summary"), "the Market page's Summary tab is a row");
            Check(Read("src/TenshoAssistant/MainWindow.ItemContextMenu.cs").Contains("case ItemContextRoute.Market:OpenLink(\"Item.Market\");break;", StringComparison.Ordinal), "« Market prices » follows its row");
            foreach (var (file, code) in Sources())
                Check(!code.Contains("marketFocusSummary", StringComparison.Ordinal), file + ": the Market page's flag is gone");
        }),
    ];
}
