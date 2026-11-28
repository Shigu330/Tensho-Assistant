using System.Text.RegularExpressions;

// 0.1950 (X-607, user request of 18/11; the user's approval of 28/11 for the gestures that change the pictures). A page's « nothing here »
// line goes through EmptyState(key) (UI/Components/StatusLines.cs): muted and wrapped, the same everywhere. The form written by hand with a
// key of an empty state (…Empty…, …None…, …NoResult…, …NoData…, …NoMatch…, …Nothing…) is refused from now on in the pages.
internal static class UiEmptyStates1950Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"MainWindow*.cs",SearchOption.TopDirectoryOnly)
            .Select(path=>(Path.GetFileName(path),File.ReadAllText(path)));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI empty states 1950: no « nothing here » line written by hand with TextDisabled: use EmptyState(key)",()=>
        {
            var hand=new Regex(@"ImGui\.TextDisabled\(localization\.Get\(""[A-Za-z0-9]+\.[A-Za-z0-9.]*(Empty|None|NoResult|NoData|NoMatch|Nothing)[A-Za-z0-9]*""\)\)");
            var found=Pages().SelectMany(page=>hand.Matches(page.Text).Select(match=>page.Name+" "+match.Value)).ToArray();
            Check(found.Length==0,"written by hand: "+string.Join(", ",found.Take(8)));
        }),
        ("UI empty states 1950: EmptyState stays muted and wrapped",()=>
        {
            var component=File.ReadAllText(Path.Combine(Root,"UI/Components/StatusLines.cs")).Replace("\r\n","\n");
            Check(component.Contains("ImGui.PushStyleColor(ImGuiCol.Text,Muted);\n        try{ImGui.TextWrapped(localization.Get(key));}"),"muted, wrapped");
        }),
        ("UI empty states 1950: the pages use it",()=>
        {
            var calls=Pages().Sum(page=>Regex.Matches(page.Text,@"\bEmptyState\(""").Count);
            Check(calls>=140,"the empty lines of the pages: "+calls);
        }),
    ];
}
