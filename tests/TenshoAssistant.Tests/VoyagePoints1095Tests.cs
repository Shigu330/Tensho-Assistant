using TenshoAssistant.Gathering;

// 0.1095 (user report, 11/11 15 h): the ocean voyage panel's last line stayed at « Points: 0 · fish caught: 0 » while its table showed 20 and 22
// points (42 in the game), the result line read 0, the panel cost 160 to 260 ms on its first frames, and the bite strength was often unknown.
internal static class VoyagePoints1095Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static OceanVoyageFrame Frame(int points,int caught,params OceanZoneCatch[] catches)
        =>new(20,1,2,false,0,[],points,caught,DateTimeOffset.UnixEpoch){Catches=catches};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Voyage points 1095: the voyage's points are the sum of every stop's catches while the player's line is empty",() =>
        {
            var reported=Frame(0,0,new(0,40536,1,20),new(0,40538,1,22));
            Check(OceanVoyages.Totals(reported)==(42,2) && OceanVoyages.ShowTotals(reported),"42 points, 2 fish (the game said 42)");
            Check(OceanVoyages.Totals(Frame(0,0,new(0,40536,1,20),new(1,51231,2,60),new(2,51230,1,30)))==(110,4),"three stops");
            Check(OceanVoyages.Totals(Frame(3820,12,new OceanZoneCatch(0,40536,1,20)))==(3820,12),"the game's line once filled and plausible");
            Check(OceanVoyages.Totals(Frame(57359,27127,new OceanZoneCatch(0,40536,1,20)))==(20,1),"an implausible line is not taken (X-98)");
            Check(OceanVoyages.Totals(Frame(10,1,new(0,40536,1,20),new(0,40538,1,22)))==(42,2),"a line below the catches is not taken");
            Check(!OceanVoyages.ShowTotals(Frame(57359,27127)) && OceanVoyages.ShowTotals(Frame(0,0)),"shown only when it can be true");
        }),
        ("Voyage points 1095: wiring of the panel, the result line and the bite",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            var panel=Read("MainWindow.OceanVoyagePanel.cs");
            Check(panel.Contains("OceanVoyages.Totals(frame)") && !panel.Contains("frame.Points.ToString(\"N0\")"),"the line uses the totals");
            Check(panel.Contains("OceanCacheTick();") && panel.Contains("OceanSpectralSeen(stop)") && !panel.Contains("seenStore.Find(Discoveries.DiscoveryKind.SpectralBefore,fish=>stopFish.Contains(fish),3);\n"),"per-fish data kept, not rebuilt every frame");
            Check(!System.Text.RegularExpressions.Regex.IsMatch(panel,@"DrawOceanFishTable[\s\S]*var row=FishRow\(fishId,spotId\)"),"no GatherBuddy row rebuilt per fish per frame");
            Check(panel.Contains("OceanPart(\"ocean fish table\""),"the table measured in developer mode");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("mainWindow.OceanVoyageData=InBackground(oceanVoyageData);") && plugin.Contains("mainWindow.OceanTimetableNow=InBackground(ocean);"),"sheets read off the frame");
            var reader=Read("Gathering/OceanVoyageReader.Dalamud.cs");
            Check(reader.Contains("++resultWait>=60") && reader.Contains("Tensho counted {5} points, {6} fish"),"result logged once filled, with Tensho's count");
            var watcher=Read("Discoveries/ItemDiscoveryWatcher.Dalamud.cs");
            Check(watcher.Contains("lineEveryFrame") && watcher.Contains("if(state==5 && castStarted>0 && biteStrength==0)biteStrength="),"bite read on every frame while the line bites");
        }),
    ];
}
