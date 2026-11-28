using System.Text.RegularExpressions;

// 0.1948 (X-607, user request of 18/11: « code as easy as possible to maintain, above all for a human »; survey of relay 226, gesture 6).
// The widths the pages give most often to a control are named in UI/Components/Widths.cs with the value written by hand before, so no page
// changes. The seven named values are refused as bare numbers in the pages, and the other bare numbers do not grow (a ratchet).
internal static class UiWidths1948Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");
    private const int Ceiling=87;

    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"MainWindow*.cs",SearchOption.TopDirectoryOnly)
            .Select(path=>(Path.GetFileName(path),File.ReadAllText(path)));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI widths 1948: a named width is never written as a bare number in the pages (use Widths.…)",()=>
        {
            var hand=new Regex(@"SetNextItemWidth\((70|90|110|160|220|260|320)(\.0)?f?\)");
            var found=Pages().SelectMany(page=>hand.Matches(page.Text).Select(match=>page.Name+" "+match.Value)).ToArray();
            Check(found.Length==0,"written by hand: "+string.Join(", ",found.Take(8)));
        }),
        ("UI widths 1948: the other bare widths stay at or under their ratchet",()=>
        {
            var bare=new Regex(@"SetNextItemWidth\(\d+(\.\d+)?f?\)");
            var count=Pages().Sum(page=>bare.Matches(page.Text).Count);
            Check(count<=Ceiling,$"{count} > {Ceiling}");
        }),
        ("UI widths 1948: the names keep the values of before, and the pages use them",()=>
        {
            var widths=File.ReadAllText(Path.Combine(Root,"UI/Components/Widths.cs"));
            foreach(var (name,value) in new[]{("Tiny",70),("Number",90),("NumberWide",110),("Short",160),("Medium",220),("Long",260),("Wide",320)})
                Check(widths.Contains($"public const float {name}={value};"),name);
            var used=Pages().Sum(page=>Regex.Matches(page.Text,@"SetNextItemWidth\(Widths\.").Count);
            Check(used>=140,"the pages use them: "+used);
        }),
    ];
}
