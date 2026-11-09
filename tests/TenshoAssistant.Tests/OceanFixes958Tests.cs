using TenshoAssistant.Gathering;

// X-98 (0.958): bite times passed in red, mission progress capped, points shown only when plausible, the 16:00 departure as a proven sighting.
internal static class OceanFixes958Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ocean fixes 958: plausible points, proven sighting, red when passed, progress capped",() =>
        {
            var at=DateTimeOffset.UtcNow;
            OceanVoyageFrame Frame(int points,int caught)=>new(17,2,2,false,0,[],points,caught,at);
            Check(OceanVoyages.Plausible(Frame(3820,12)) && !OceanVoyages.Plausible(Frame(57359,27127)) && !OceanVoyages.Plausible(Frame(-1,0)),"plausible");
            Check(OceanSchedule.Proven.Any(sighting=>sighting.Slot==249084 && sighting.Route==14),"proven sighting");
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("MainWindow.OceanVoyagePanel.cs").Contains("passed>time.Maximum)ImGui.TextColored(Error,text)") && Read("MainWindow.FishingPanel.cs").Contains("passed>time.Maximum)ImGui.TextColored(Error,text)"),"red");
            Check(Read("MainWindow.OceanVoyagePanel.cs").Contains("target>0 ? Math.Min(progress,target):progress"),"capped");
            Check(Read("MainWindow.OceanFishing.cs").Contains(".Concat(OceanSchedule.Proven)"),"sightings with the proven one");
        }),
    ];
}
