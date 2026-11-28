using System.Text.RegularExpressions;

// 0.1946 (X-607, user request of 18/11: « code as easy as possible to maintain, above all for a human »; survey of relay 226, gesture 6).
// A text filter where empty means « all » goes through FilterCombo(id, allKey, ref value, options) (UI/Components/FilterCombo.cs), the same
// calls as the four combos written by hand before it. A ratchet: the « All … » first lines written by hand (a Selectable on an « .All… » key)
// do not come back above their count; the ones left filter on a number, an enum or a name with its own label, and stay for now.
internal static class UiFilters1946Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");
    private const int Ceiling=15;

    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"MainWindow*.cs",SearchOption.TopDirectoryOnly)
            .Select(path=>(Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n")));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI filters 1946: the « All … » first lines written by hand stay at or under their ratchet (use FilterCombo)",()=>
        {
            var hand=new Regex(@"Selectable\(localization\.Get\(""[A-Za-z]+\.(All|Any)[A-Za-z]*""\)");
            var found=Pages().SelectMany(page=>hand.Matches(page.Text).Select(_=>page.Name)).ToArray();
            Check(found.Length<=Ceiling,$"{found.Length} > {Ceiling}: "+string.Join(", ",found));
        }),
        ("UI filters 1946: FilterCombo keeps the calls of before (preview, « All » first, each choice, the combo closed in finally)",()=>
        {
            var component=File.ReadAllText(Path.Combine(Root,"UI/Components/FilterCombo.cs")).Replace("\r\n","\n");
            Check(component.Contains("if(!ImGui.BeginCombo(id,value.Length==0 ? all:value))return false;"),"preview");
            Check(component.Contains("if(ImGui.Selectable(all,value.Length==0)){value=\"\";changed=true;}"),"« All » first");
            Check(component.Contains("if(ImGui.Selectable(option,value==option)){value=option;changed=true;}"),"each choice");
            Check(component.Contains("finally{ImGui.EndCombo();}"),"closed");
        }),
        ("UI filters 1946: the four pages use it with their id, their « All » key and their choices",()=>
        {
            string Page(string name)=>File.ReadAllText(Path.Combine(Root,name));
            Check(Page("MainWindow.CraftFromStock.cs").Contains("FilterCombo(localization.Get(\"CraftStock.Job\")+\"###CraftStockJob\",\"RecipeUi.AllJobs\",ref fromStockJob,"),"craft from stock");
            Check(Page("MainWindow.Duties.cs").Contains("FilterCombo(localization.Get(\"DutyUi.Category\"),\"DutyUi.All\",ref dutyCategory,dutyCategories);"),"duties");
            Check(Page("MainWindow.Leves.cs").Contains("FilterCombo(\"##LeveClient\",\"Leves.AllClients\",ref leveClient,leveClients);"),"leves");
            Check(Page("MainWindow.MarketBoard.cs").Contains("FilterCombo(localization.Get(\"MarketBoard.World\")+\"##BoardWorld\",\"MarketBoard.AllWorlds\",ref marketWorld,worlds);"),"market board");
        }),
    ];
}
