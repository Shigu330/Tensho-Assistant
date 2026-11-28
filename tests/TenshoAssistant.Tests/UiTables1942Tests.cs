using System.Text.RegularExpressions;

// 0.1942 (X-607, user request of 18/11: « code as easy as possible to maintain, above all for a human »; survey of relay 226, gesture 4).
// The fifteen tables written by hand with exactly BeginDataTable's flags (RowBg, BordersInnerH, SizingStretchProp; no scroll, no size) go
// through UI.UiComponents.BeginDataTable, which gives them the same flags: no page changes. The form is refused outside UI/ from now on.
internal static class UiTables1942Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
            .Select(path=>Path.GetRelativePath(Root,path).Replace('\\','/'))
            .Where(name=>!name.StartsWith("UI/",StringComparison.Ordinal) && !name.Split('/').Any(part=>part is "obj" or "bin"))
            .Select(name=>(name,File.ReadAllText(Path.Combine(Root,name))));

    private static readonly Regex HandTable=new(@"ImGui\.BeginTable\([^;]*?,\s*(ImGuiTableFlags\.\w+(\s*\|\s*ImGuiTableFlags\.\w+)*)\s*\)",RegexOptions.Compiled);

    /// <summary>The flags of a table opened by hand with no size, as a set.</summary>
    private static HashSet<string> Flags(string flags)=>flags.Split('|').Select(flag=>flag.Trim().Replace("ImGuiTableFlags.","")).ToHashSet();

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI tables 1942: no table opened by hand with exactly BeginDataTable's flags: use UI.UiComponents.BeginDataTable(id,columns)",()=>
        {
            var plain=new HashSet<string>{"RowBg","BordersInnerH","SizingStretchProp"};
            var found=Pages().SelectMany(page=>HandTable.Matches(page.Text).Where(match=>Flags(match.Groups[1].Value).SetEquals(plain)).Select(match=>page.Name)).ToArray();
            Check(found.Length==0,"opened by hand with BeginDataTable's flags: "+string.Join(", ",found));
        }),
        ("UI tables 1942: BeginDataTable gives the same flags as the tables it replaced, so their pages are unchanged",()=>
        {
            var components=File.ReadAllText(Path.Combine(Root,"UI/UiComponents.cs"));
            Check(components.Contains("var flags=ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.SizingStretchProp;"),"the same three flags");
            Check(components.Contains("return ImGui.BeginTable(id,columns,flags,new Vector2(0,scrollHeight>0 ? scrollHeight:0));"),"no size when no scroll");
        }),
        ("UI tables 1942: the fifteen tables moved, each by its id",()=>
        {
            var texts=Pages().ToDictionary(page=>page.Name,page=>page.Text);
            foreach(var (file,id) in new[]{("MainWindow.AirshipPlanner.cs","\"AirshipRoutes\",6"),("MainWindow.Characters.cs","\"Characters\",8"),("MainWindow.Credits.cs","\"###CreditTable\"+kind,3"),
                ("MainWindow.Currencies.cs","\"Currencies\",4"),("MainWindow.Discoveries.cs","\"Discoveries\",6"),("MainWindow.FreeCompany.cs","\"Voyages\"+kind,5"),("MainWindow.GearWear.cs","\"GearWearTable\",3"),
                ("MainWindow.HuntingLog.cs","\"HuntingLog\",4"),("MainWindow.MyTools.cs","\"###MyToolItems\",5"),("MainWindow.OceanFishing.cs","\"OceanDepartures\",5"),("MainWindow.Reminders.cs","\"Reminders\",7"),
                ("MainWindow.Roulettes.cs","\"DailyRoulettes\",3"),("MainWindow.VesselBuilds.cs","\"BestBuilds\",9"),("MainWindow.VoyageHistory.cs","\"VoyageRoutes\",3"),("MainWindow.VoyageHistory.cs","\"VoyageLast\",4")})
                Check(texts[file].Contains("UI.UiComponents.BeginDataTable("+id+")"),file+": "+id);
        }),
    ];
}
