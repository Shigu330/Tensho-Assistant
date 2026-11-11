// X-76 2b H9 (0.922): logs. No interpolated log message, no « catalog could not be built » at Error level, the three 5-second readers log
// once until a good read, developer captures behind developer mode.
internal static class Logs922Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Logs 922: templates, one level for unavailable catalogs, warn once, developer captures",() =>
        {
            var found=new List<string>();
            var interpolated=new System.Text.RegularExpressions.Regex(@"log\.(Verbose|Debug|Information|Warning|Error|Fatal)\((error, ?)?\$""");
            foreach(var path in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories))
            {
                if(path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))continue;
                var text=File.ReadAllText(path);var name=Path.GetFileName(path);
                if(interpolated.IsMatch(text))found.Add(name+" interpolated");
                if(System.Text.RegularExpressions.Regex.IsMatch(text,@"log\.Error\(error, ?""Tensho [^""]* could not be built"))found.Add(name+" error level");
            }
            Check(found.Count==0,"left: "+string.Join(", ",found.Take(8)));
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("Duties/DutyFinderReader.Dalamud.cs").Contains("if (!failureLogged) { failureLogged = true;") && Read("Items/ItemHoverReader.Dalamud.cs").Contains("if(!failureLogged){failureLogged=true;")
                && Read("Maps/MapLiveReader.Dalamud.cs").Contains("if(!positionFailureLogged){positionFailureLogged=true;"),"warn once");
            // #36 (0.1110): the garden's capture is gone (GardenCapture1110Tests).
            Check(Read("Items/GcSupplyReader.Dalamud.cs").Contains("if(developer?.Invoke()==true)log.Information(\"Grand Company supply window")
                && Read("Market/RetainerSalesReader.Dalamud.cs").Contains("developer?.Invoke() == true && capture != lastRaw"),"developer captures");
        }),
    ];
}
