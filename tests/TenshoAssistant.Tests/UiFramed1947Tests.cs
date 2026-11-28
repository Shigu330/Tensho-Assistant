using System.Text.RegularExpressions;

// 0.1947 (X-607, user request of 18/11: « code as easy as possible to maintain, above all for a human »; survey of relay 226, gesture 6).
// A framed region of a page (a list, a pane) goes through BeginFramed(id, size) (UI/Components/Framed.cs), the same call as the twenty-two
// « ImGui.BeginChild(id,size,true) » written by hand before it; each is still closed by ImGui.EndChild(). The form is refused from now on
// in the pages (a child with flags of its own, or without a border, keeps its own call).
internal static class UiFramed1947Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"MainWindow*.cs",SearchOption.TopDirectoryOnly)
            .Select(path=>(Path.GetFileName(path),File.ReadAllText(path)));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI framed 1947: no « ImGui.BeginChild(id,size,true) » written by hand in the pages: use BeginFramed(id, size)",()=>
        {
            var hand=new Regex(@"ImGui\.BeginChild\([^;\n]*,\s*true\s*\)\s*(\)|;)");
            var found=Pages().SelectMany(page=>hand.Matches(page.Text).Select(_=>page.Name)).ToArray();
            Check(found.Length==0,"written by hand: "+string.Join(", ",found));
        }),
        ("UI framed 1947: BeginFramed is the same call (a border, no flags)",()=>
        {
            var component=File.ReadAllText(Path.Combine(Root,"UI/Components/Framed.cs"));
            Check(component.Contains("private static bool BeginFramed(string id,Vector2 size)=>ImGui.BeginChild(id,size,true);"),"the same call");
        }),
        ("UI framed 1947: the pages use it and still close each region (at least as many EndChild as BeginChild and BeginFramed: some regions close on two paths)",()=>
        {
            var calls=Pages().Sum(page=>Regex.Matches(page.Text,@"\bBeginFramed\(").Count);
            Check(calls>=22,"twenty-two regions: "+calls);
            foreach(var (name,text) in Pages())
            {
                var opened=Regex.Matches(text,@"\bBeginFramed\(|ImGui\.BeginChild\(").Count;
                var closed=Regex.Matches(text,@"ImGui\.EndChild\(\)").Count;
                Check(closed>=opened,$"{name}: {opened} opened, {closed} closed");
            }
        }),
    ];
}
