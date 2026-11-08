using System.Text.RegularExpressions;

// 0.913 (X-76 step 2b, harmonisation H1): one tooltip helper everywhere. A help text from a key goes through Tip(key), which also
// shows it when the control is greyed; tooltips drawn by hand share one wrap width through TooltipScope; help markers are one helper.
internal static class Tooltips913Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");
    private static string Source(string file)=>File.ReadAllText(Path.Combine(Root,file));
    private static IEnumerable<(string Name,string Text)> Files()
        =>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
            .Where(path=>!Path.GetRelativePath(Root,path).Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Any(part=>part is "obj" or "bin"))
            .Select(path=>(Path.GetRelativePath(Root,path).Replace('\\','/'),File.ReadAllText(path)));
    // The hand-written form Tip replaces (it lost the tooltip of a greyed control).
    private static readonly Regex HandWritten=new(@"IsItemHovered\(\)\)\s*WrappedTooltip\(localization\.Get\(");
    // Composite texts kept as they are (a key joined with other text): file and a piece of the line.
    private static readonly (string File,string Piece)[] Kept=
    [
        ("MainWindow.CraftProjection.cs","\"CraftProjectionUi.SuggestTip\")+"),
        ("MainWindow.FishingPanel.cs","\"FishingPanel.BiteUnknown\")+"),
        ("MainWindow.MapView.cs","\"MapView.IconsOwnMenuOff\")+"),
    ];
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Tooltips: no hand-written « if(ImGui.IsItemHovered())WrappedTooltip(localization.Get(…)) » left; Tip(key) is used instead",() =>
        {
            var found=new List<string>();
            foreach(var (name,text) in Files())
            {
                if(name=="UI/Components/Tooltip.cs")continue;
                var lines=text.Split('\n');
                for(var index=0;index<lines.Length;index++)
                    if(HandWritten.IsMatch(lines[index]) && !Kept.Any(kept=>kept.File==name && lines[index].Contains(kept.Piece,StringComparison.Ordinal)))
                        found.Add($"{name}:{index+1}");
            }
            Check(found.Count==0,"use Tip(\"Key\") (also shown when greyed): "+string.Join(", ",found));
            foreach(var (file,piece) in Kept)Check(Source(file).Contains(piece,StringComparison.Ordinal),"kept composite site moved, update the list: "+file);
        }),
        ("Tooltips: the three controls greyed while their tooltip was lost now use Tip / TipText",() =>
        {
            Check(Regex.IsMatch(Source("MainWindow.Gathering.cs"),@"finally \{ ImGui\.EndDisabled\(\); \}\r?\n\s*Tip\(""GatheringDetails\.MapHelp""\);"),"gathering: zone map button");
            Check(Regex.IsMatch(Source("MainWindow.MyTools.cs"),@"finally\{ImGui\.EndDisabled\(\);\}\r?\n\s*Tip\(""MyToolsUi\.GalleryOpenTip""\);"),"my tools: gallery open button");
            var simulation=Source("MainWindow.CraftSimulation.cs");
            Check(simulation.Contains("TipText(action.Description);",StringComparison.Ordinal) && !simulation.Contains("WrappedTooltip(action.Description)",StringComparison.Ordinal),"craft simulation: action icon");
        }),
        ("Tooltips: the helpers live in UI/Components/Tooltip.cs; hand-drawn tooltips and help markers go through them",() =>
        {
            var helpers=Source("UI/Components/Tooltip.cs");
            Check(helpers.Contains("private static void WrappedTooltip(string text)",StringComparison.Ordinal) && helpers.Contains("private void Tip(string key)",StringComparison.Ordinal)
                && helpers.Contains("private static void TipText(string text)",StringComparison.Ordinal) && helpers.Contains("private void HelpMarker(string key)",StringComparison.Ordinal)
                && helpers.Contains("internal readonly struct TooltipScope : IDisposable",StringComparison.Ordinal),"helpers");
            Check(Regex.Matches(helpers,@"IsItemHovered\(ImGuiHoveredFlags\.AllowWhenDisabled\)").Count==2,"Tip and TipText also when greyed");
            Check(!Source("MainWindow.Advanced.cs").Contains("void WrappedTooltip(",StringComparison.Ordinal),"moved out of Advanced");
            foreach(var (name,text) in Files())
                if(name!="UI/Components/Tooltip.cs")Check(!text.Contains("ImGui.BeginTooltip()",StringComparison.Ordinal),name+": BeginTooltip by hand, use TooltipScope.Begin()");
            Check(Source("Alarms/AlarmDtrEntry.Dalamud.cs").Contains("TooltipScope.UnderBox(bounds)",StringComparison.Ordinal)
                && Source("Alarms/ReminderDtrEntry.Dalamud.cs").Contains("TooltipScope.UnderBox(bounds)",StringComparison.Ordinal),"server bar tooltips placed by one helper");
            var log=Source("MainWindow.CraftLog.cs");
            Check(log.Contains("HelpMarkerText(help);",StringComparison.Ordinal) && !log.Contains("TextDisabled(\"(?)\")",StringComparison.Ordinal),"craft log help marker");
            Check(Source("MainWindow.DutyFinder.cs").Contains("HelpMarker(\"DutyUi.Source\");",StringComparison.Ordinal),"duty finder help marker");
            var map=Source("MainWindow.MapView.cs");
            Check(map.Contains("Tip(key+\"Help\");",StringComparison.Ordinal) && map.Contains("if(localization.Has(key+\"Help\"))Tip(key+\"Help\");",StringComparison.Ordinal),"map area and setting rows: help also when greyed");
        }),
    ];
}
