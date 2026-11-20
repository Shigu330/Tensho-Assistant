using System.Text.RegularExpressions;

// 0.1612 (X-607, user request of 18/11: « code as easy as possible to maintain, above all for a human: one template for every page »).
// Two ratchets (R10): the tab bars and the tables still written by hand page by page can only go down. A page's blocks in tabs go through
// DetailTabs (UI/Components/DetailPage.cs), a family's tabs through the page header (MainWindow.Pages.cs, Tab); a table through
// UiComponents.BeginDataTable and UiComponents.HeadersRow. CONTRIBUTING « Adding a page » says where each part of a page is declared.
internal static class UiRatchets1612Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    /// <summary>The plugin's sources outside the common components (UI/), by path relative to src/TenshoAssistant.</summary>
    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
            .Select(path=>Path.GetRelativePath(Root,path).Replace('\\','/'))
            .Where(name=>!name.StartsWith("UI/",StringComparison.Ordinal) && !name.Split('/').Any(part=>part is "obj" or "bin"))
            .Select(name=>(name,File.ReadAllText(Path.Combine(Root,name))));

    // The tab bars written by hand on 20/11 (file and id). A new one is refused: use DetailTabs; one converted leaves the list.
    private static readonly (string File,string Id)[] TabBars=
    [
        ("MainWindow.Bank.cs","BankOriginTabs"),("MainWindow.ChatWindow.cs","###TenshoChatTabs"),("MainWindow.Collections.cs","CollectionKinds"),
        ("MainWindow.CraftSimulation.cs","CraftPageTabs"),("MainWindow.CustomDeliveries.cs","###CustomDeliveriesTabs"),("MainWindow.FreeCompany.cs","FreeCompanyTabs"),
        ("MainWindow.IslandSanctuary.cs","IslandTabs"),("MainWindow.Items.cs","StorageScope"),("MainWindow.Items.cs","RecipeDirections"),
        ("MainWindow.MapLocalLists.cs","MapLocalTabs"),("MainWindow.Maps.cs","MapsTabs"),("MainWindow.Market.cs","MarketTabs"),
        ("MainWindow.Settings.cs","SettingsSections"),("MainWindow.Storage.cs","StorageTabs"),("MainWindow.TripleTriad.cs","TriadNotebook"),
        ("MainWindow.TripleTriad.cs","TriadSeries"),
    ];
    // The tables written by hand on 20/11, outside UI/: lower these numbers when a table moves to BeginDataTable / HeadersRow.
    private const int HandTables=96,HandHeaderRows=82;

    private static int Count(string text,string piece)=>Regex.Matches(text,Regex.Escape(piece)).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI ratchets 1612: no new tab bar written by hand: a page's blocks in tabs go through DetailTabs",()=>
        {
            var found=new List<(string,string)>();
            foreach(var (name,text) in Pages())
                foreach(Match match in Regex.Matches(text,@"ImGui\.BeginTabBar\(""([^""]*)"""))found.Add((name,match.Groups[1].Value));
            var extra=found.Where(site=>!TabBars.Contains(site)).ToArray();
            Check(extra.Length==0,"a tab bar written by hand; use DetailTabs(id,[(key,label,draw)…],ref focus): "+string.Join(", ",extra.Select(site=>site.Item1+" "+site.Item2)));
            var gone=TabBars.Where(site=>!found.Contains(site)).ToArray();
            Check(gone.Length==0,"converted or renamed, take it out of TabBars (the ratchet goes down): "+string.Join(", ",gone.Select(site=>site.Item1+" "+site.Item2)));
            Check(Pages().Sum(page=>Count(page.Text,"ImGui.BeginTabBar("))==TabBars.Length,"every tab bar written by hand has a literal id in the list");
        }),
        ("UI ratchets 1612: the tables written by hand can only go down (BeginDataTable and HeadersRow for a new one)",()=>
        {
            var tables=Pages().Sum(page=>Count(page.Text,"ImGui.BeginTable("));
            var headers=Pages().Sum(page=>Count(page.Text,"ImGui.TableHeadersRow()"));
            Check(tables<=HandTables,$"{tables} tables written by hand (at most {HandTables}): open a new one with UiComponents.BeginDataTable");
            Check(headers<=HandHeaderRows,$"{headers} header rows written by hand (at most {HandHeaderRows}): title a new table with UiComponents.HeadersRow");
            Check(tables==HandTables && headers==HandHeaderRows,$"fewer by hand ({tables}, {headers}): lower HandTables and HandHeaderRows to them, so the ratchet holds");
        }),
        ("UI ratchets 1612: the common parts the ratchets point to exist, and CONTRIBUTING says where a page is declared, in English and French",()=>
        {
            Check(File.ReadAllText(Path.Combine(Root,"UI/Components/DetailPage.cs")).Contains("private void DetailTabs(string id,IReadOnlyList<(string Key,string Label,Action Draw)> tabs,ref string? focus)"),"DetailTabs");
            var components=File.ReadAllText(Path.Combine(Root,"UI/UiComponents.cs"));
            Check(components.Contains("public static bool BeginDataTable(") && components.Contains("HeadersRow("),"BeginDataTable and HeadersRow");
            var contributing=File.ReadAllText(Path.GetFullPath("CONTRIBUTING.md"));
            Check(contributing.Contains("### Adding a page: where everything is declared") && contributing.Contains("### Ajouter une page : où tout se déclare"),"both halves");
            foreach(var place in new[]{"enum Page","MainWindow.Pages.cs","DetailTabs(","GuideFlow.cs","DirectoryLeftOut","DesktopPages.cs","PageHelp.<Name>Title","Help/Faq.cs","MainWindow.SelfCheckPages.cs"})
                Check(contributing.Split(place).Length>=3,"named in both halves: "+place);
        }),
    ];
}
