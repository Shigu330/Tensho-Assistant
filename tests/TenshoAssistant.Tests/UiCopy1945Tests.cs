using System.Text.RegularExpressions;

// 0.1945 (X-607, user request of 18/11: « code as easy as possible to maintain, above all for a human »; survey of relay 226, gesture 6).
// A small copy button goes through CopyButton(label, ()=>text) (UI/Components/StatusLines.cs): the same small button, then « Copied » for two
// seconds. The five small buttons written by hand with no message of their own moved to it. A ratchet: the clipboard written by hand outside a
// menu line (Selectable) and outside the component does not come back above its count; the sites left keep a message of their own (« 12 lines
// copied ») or a full-size button (ImGui.Button), whose picture CopyButton would change.
internal static class UiCopy1945Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");
    private const int Ceiling=20;

    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"MainWindow*.cs",SearchOption.TopDirectoryOnly)
            .Select(path=>(Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n")));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI copy 1945: the clipboard written by hand outside a menu line stays at or under its ratchet (use CopyButton)",()=>
        {
            var found=Pages().SelectMany(page=>page.Text.Split('\n').Where(line=>line.Contains("ImGui.SetClipboardText(") && !line.Contains("Selectable(")).Select(_=>page.Name)).ToArray();
            Check(found.Length<=Ceiling,$"{found.Length} > {Ceiling}: "+string.Join(", ",found));
        }),
        ("UI copy 1945: a small button whose only work is to copy is refused written by hand, even over two lines",()=>
        {
            var hand=new Regex(@"if\s*\((?:[^()\n]|\([^()\n]*(?:\([^()\n]*\))?[^()\n]*\))*ImGui\.SmallButton\((?:[^()\n]|\([^()\n]*(?:\([^()\n]*\))?[^()\n]*\))*\)\s*\)\s*\n?\s*ImGui\.SetClipboardText\(");
            var found=Pages().Where(page=>hand.IsMatch(page.Text)).Select(page=>page.Name).ToArray();
            Check(found.Length==0,"use CopyButton: "+string.Join(", ",found));
        }),
        ("UI copy 1945: the five pages use CopyButton with the same label as before",()=>
        {
            string Page(string name)=>File.ReadAllText(Path.Combine(Root,name));
            Check(Page("MainWindow.PreparationCost.cs").Contains("CopyButton(localization.Get(\"CostUi.Copy\"),()=>"),"preparation cost");
            Check(Page("MainWindow.TriadObservation.cs").Contains("CopyButton(localization.Get(\"TriadObserved.Copy\"),()=>"),"observed triad");
            Check(Page("MainWindow.TripleTriad.cs").Contains("CopyButton(localization.Get(\"TriadUi.Copy\"),()=>localization.Format(\"UiFragments.CardCopy\","),"triad card");
            Check(Page("MainWindow.Alarms.cs").Contains("CopyButton(localization.Get(\"Alarms.CopyRaw\"),()=>"),"game alarms, raw");
            Check(Page("MainWindow.Advanced.cs").Contains("CopyButton(localization.Get(\"Contributor.CopyProbe\"),()=>"),"probe command");
            var component=File.ReadAllText(Path.Combine(Root,"UI/Components/StatusLines.cs"));
            Check(component.Contains("var clicked=ImGui.SmallButton(label);"),"the same small button");
        }),
    ];
}
