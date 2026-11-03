using System.Text;
using System.Text.RegularExpressions;

// 0.325 (queue step d, review 0.316: filters without tooltips). Every input, filter or sort control of the windows has a tooltip.
// A ratchet: the controls still without one are listed in tooltip-baseline.txt (« file|identity|reason », identity = the first
// string literal of the call, usually its ImGui ID or label key). A new control without a tooltip fails, and so does a baseline
// line that no longer matches anything: remove it once its control has a tooltip, so the list only shrinks.
// Heuristic, regex only (no Roslyn): a control is covered when Tip / WrappedTooltip / IsItemHovered / SetTooltip, or a shared row
// helper, follows it before the next control and within twelve lines.
internal static class TooltipRatchet325Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static readonly string Baseline = Path.GetFullPath("tests/TenshoAssistant.Tests/tooltip-baseline.txt");
    private static readonly Regex Control = new(@"ImGui\.(Checkbox|Combo|BeginCombo|Input\w+|V?Slider\w+|Drag\w+|RadioButton|ColorEdit\d)\(");
    // #28 (review 0.326): another drawn item ends the search too, so a later widget's tooltip never covers the control.
    private static readonly Regex OtherItem = new(@"ImGui\.(Button|SmallButton|ArrowButton|Selectable|ImageButton|InvisibleButton)\(|\bToolButton\(|\bDefaultButton\(|\bSourceButton\(");
    // « Tip( » also matches the page helpers named …Tip( (QuestFilterTip, …).
    private static readonly Regex Covered = new(@"Tip\(|WrappedTooltip\(|IsItemHovered|SetTooltip\(");
    private static readonly Regex Literal = new("\"((?:[^\"\\\\]|\\\\.)*)\"");

    private static IEnumerable<(string File,string[] Lines)> Files() =>
        Directory.EnumerateFiles(Source,"*.cs",SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(Source,path).Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .OrderBy(path => path,StringComparer.Ordinal)
            .Select(path => (Path.GetRelativePath(Source,path).Replace('\\','/'),File.ReadAllLines(path)));

    // Where the next control or other item starts in a piece of line (-1: none).
    private static int Stop(string text)
    {
        var control = Control.Match(text); var other = OtherItem.Match(text);
        return control.Success && (!other.Success || control.Index <= other.Index) ? control.Index : other.Success ? other.Index : -1;
    }

    // Controls seen by the last scan: a scan that sees almost nothing (wrong folder, filter too wide) must fail, not pass.
    private static int scanned;

    private static List<string> Uncovered()
    {
        var found = new List<string>(); scanned = 0;
        foreach (var (file,lines) in Files())
            for (var i = 0; i < lines.Length; i++)
                foreach (Match match in Control.Matches(lines[i]))
                {
                    scanned++;
                    var rest = lines[i][(match.Index + match.Length)..];
                    var window = new StringBuilder();
                    var sameLine = Stop(rest);
                    if (sameLine >= 0) window.Append(rest[..sameLine]);
                    else
                    {
                        window.Append(rest);
                        for (var j = i + 1; j < Math.Min(lines.Length,i + 13); j++)
                        {
                            var next = Stop(lines[j]);
                            if (next >= 0) { window.Append('\n').Append(lines[j][..next]); break; }
                            window.Append('\n').Append(lines[j]);
                        }
                    }
                    // A combo's tooltip usually follows its whole block: also look at the three lines after its EndCombo.
                    if (match.Groups[1].Value == "BeginCombo")
                        for (var j = i; j < Math.Min(lines.Length,i + 80); j++)
                        {
                            if (!lines[j].Contains("EndCombo",StringComparison.Ordinal)) continue;
                            for (var k = j + 1; k < Math.Min(lines.Length,j + 4); k++)
                            {
                                var next = Stop(lines[k]);
                                window.Append('\n').Append(next >= 0 ? lines[k][..next] : lines[k]);
                                if (next >= 0) break;
                            }
                            break;
                        }
                    if (Covered.IsMatch(window.ToString())) continue;
                    found.Add(file + "|" + (Literal.Match(rest) is { Success: true } literal ? literal.Groups[1].Value : rest.Trim()));
                }
        return found;
    }

    private static List<string> Listed() =>
        File.ReadAllLines(Baseline).Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('|')).Select(parts => parts.Length >= 3 && parts[2].Trim().Length > 0 ? parts[0] + "|" + parts[1] : "invalid: " + string.Join('|',parts)).ToList();

    private static List<string> Minus(List<string> left,List<string> right)
    {
        var rest = right.GroupBy(entry => entry).ToDictionary(group => group.Key,group => group.Count());
        var result = new List<string>();
        foreach (var entry in left)
            if (rest.TryGetValue(entry,out var count) && count > 0) rest[entry] = count - 1;
            else result.Add(entry);
        return result;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Every input, filter or sort control has a tooltip, or is still listed in the ratchet baseline",() =>
        {
            var uncovered = Uncovered(); var listed = Listed();
            Check(scanned >= 150,"the scan saw only " + scanned + " controls");
            Check(!listed.Any(entry => entry.StartsWith("invalid: ",StringComparison.Ordinal)),"baseline lines need file|identity|reason: " + string.Join("; ",listed.Where(entry => entry.StartsWith("invalid: ",StringComparison.Ordinal))));
            var missing = Minus(uncovered,listed);
            Check(missing.Count == 0,"controls without a tooltip (add Tip(\"Section.KeyTip\") after the control):\n" + string.Join("\n",missing.Select(entry => entry + "|")));
            var stale = Minus(listed,uncovered);
            Check(stale.Count == 0,"baseline lines that match no control any more (remove them):\n" + string.Join("\n",stale));
        }),
        ("A tooltip line never becomes the body of an if around a control",() =>
        {
            var wrong = new List<string>();
            foreach (var (file,lines) in Files())
                for (var i = 0; i + 1 < lines.Length; i++)
                {
                    var line = lines[i].TrimEnd();
                    // Only an if around a control or button counts; « if(ImGui.IsItemHovered()) WrappedTooltip(…) » on two lines is right.
                    if (!Regex.IsMatch(line,@"^\s*(else\s+)?if\s*\(.*ImGui\.(Checkbox|Combo|BeginCombo|Input\w*|Slider\w*|Drag\w*|RadioButton|ColorEdit\d|Button|SmallButton|ArrowButton|Selectable)\(")
                        || line.EndsWith(';') || line.EndsWith('{') || line.EndsWith('}')) continue;
                    if (Regex.IsMatch(lines[i + 1],@"^\s*(Tip\(|WrappedTooltip\()")) wrong.Add($"{file}:{i + 2}");
                }
            Check(wrong.Count == 0,"Tip as the body of an if: " + string.Join(", ",wrong));
        }),
    ];
}
