using System.Text.RegularExpressions;

// 0.1944 (X-607, user request of 18/11: « code as easy as possible to maintain, above all for a human »; survey of relay 226, gesture 6a).
// A text field with a translated hint goes through InputWithHint(id, hintKey, ref text, maximum) (UI/Components/InputWithHint.cs), the
// same call as the fields written by hand before it, so no page changes; the form written by hand is refused from now on.
internal static class UiInputs1944Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static IEnumerable<(string Name,string Text)> Sources()
        =>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
            .Select(path=>Path.GetRelativePath(Root,path).Replace('\\','/'))
            .Where(name=>!name.Split('/').Any(part=>part is "obj" or "bin"))
            .Select(name=>(name,File.ReadAllText(Path.Combine(Root,name))));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI inputs 1944: no text field with a translated hint written by hand: use InputWithHint(id, hintKey, ref text, maximum)",()=>
        {
            // The four arguments of InputWithHint (a field with flags, as the settings' list draft, keeps its own call).
            var hand=new Regex(@"ImGui\.InputTextWithHint\(""[^""]*"",\s*localization\.Get\(""[^""]*""\),\s*ref \w+,\s*[A-Za-z0-9.* +]+\)");
            var found=Sources().SelectMany(source=>hand.Matches(source.Text).Select(_=>source.Name)).ToArray();
            Check(found.Length==0,"written by hand: "+string.Join(", ",found));
        }),
        ("UI inputs 1944: InputWithHint is the same call as before (the translated hint, the same maximum), so the pages are unchanged",()=>
        {
            var component=File.ReadAllText(Path.Combine(Root,"UI/Components/InputWithHint.cs")).Replace("\r\n","\n");
            Check(component.Contains("    private bool InputWithHint(string id,string hintKey,ref string text,int maximum)\n        =>ImGui.InputTextWithHint(id,localization.Get(hintKey),ref text,maximum);"),"the same call");
        }),
        ("UI inputs 1944: the pages use it (search boxes and names to type), their tooltips kept after it",()=>
        {
            var calls=Sources().Sum(source=>Regex.Matches(source.Text,@"\bInputWithHint\(""").Count);
            Check(calls>=60,"the fields moved: "+calls);
            var all=string.Concat(Sources().Select(source=>source.Text));
            Check(all.Contains("InputWithHint(\"##BeastQuery\", \"BeastUi.Search\", ref beastQuery, 64); Tip(\"BeastUi.SearchTip\");"),"a field and its tooltip");
        }),
    ];
}
