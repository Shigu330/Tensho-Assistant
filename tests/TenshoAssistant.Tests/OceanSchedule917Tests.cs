using TenshoAssistant.Gathering;

// X-85 V1 (0.917): the shift of each port is proven by the departures seen aboard, never guessed; a contradicted port shows the routes
// still possible; the usual fishing help is off at sea; the sightings are saved data.
internal static class OceanSchedule917Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static OceanRoute Route(uint id)=>new(id,"R"+id,[new("A",OceanTime.Day)]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ocean timetable 917: port of a route, shifts kept by the sightings, verified / unverified / contradicted, possible routes",() =>
        {
            var routes=new uint[]{1,2,3,11,12,13}.ToDictionary(id=>id,Route);
            // Limsa column 1 2 3 1 2 3, Kugane column 11 12 13 13 12 11.
            var schedule=new OceanSchedule([(1,11),(2,12),(3,13),(1,13),(2,12),(3,11)],routes);
            Check(schedule.PortOf(2)==OceanPort.Aldenard && schedule.PortOf(12)==OceanPort.Othard && schedule.PortOf(99) is null,"port of a route");
            var none=schedule.State(OceanPort.Aldenard,null);
            Check(none.Check==OceanCheck.Unverified && none.Offset==0 && none.Sightings==0 && none.Candidates.Count==6,"no sighting: community alignment kept, unverified");
            // Slot 6 = row 0: the community timetable gives Kugane 11; the game said 13 -> shift 0 excluded, shifts 2 and 3 fit.
            var contradicted=schedule.State(OceanPort.Othard,[new OceanSighting{Slot=6,Route=13}]);
            Check(contradicted.Check==OceanCheck.Contradicted && contradicted.Offset==-1 && contradicted.Candidates.SequenceEqual([2,3]),"contradicted: "+string.Join(",",contradicted.Candidates));
            Check(schedule.WithOffsets(0,-1).At(6).Othard is null && schedule.WithOffsets(0,-1).At(6).Aldenard!.Id==1,"unknown shift: no route shown for that port");
            Check(schedule.Possible(7,OceanPort.Othard,contradicted.Candidates).Select(route=>route.Id).SequenceEqual(new uint[]{12,13}),"possible routes for the next slot");
            // A second sighting settles it: slot 7 route 12 -> only shift 3 (row 10 % 6 = 4: 12) fits with slot 6 route 13 (row 3: 13).
            var settled=schedule.State(OceanPort.Othard,[new OceanSighting{Slot=6,Route=13},new OceanSighting{Slot=7,Route=12},new OceanSighting{Slot=7,Route=2}]);
            Check(settled.Check==OceanCheck.Verified && settled.Offset==3 && settled.Sightings==2,"verified by two sightings of that port only");
            Check(schedule.WithOffsets(0,settled.Offset).At(6).Othard!.Id==13 && schedule.WithOffsets(0,settled.Offset).At(8).Othard!.Id==11,"shift applied");
            var agreeing=schedule.State(OceanPort.Aldenard,[new OceanSighting{Slot=6,Route=1}]);
            Check(agreeing.Check==OceanCheck.Unverified && agreeing.Offset==0 && agreeing.Candidates.SequenceEqual([0,3]) && agreeing.Sightings==1,"agreeing but not unique: still unverified");
            var impossible=schedule.State(OceanPort.Aldenard,[new OceanSighting{Slot=6,Route=1},new OceanSighting{Slot=7,Route=1}]);
            Check(impossible.Check==OceanCheck.Contradicted && impossible.Candidates.Count==0,"inconsistent sightings");
            Check(OceanSchedule.SlotOf(OceanSchedule.LoopStart+TimeSpan.FromHours(2)*5+TimeSpan.FromMinutes(40))==5,"slot aboard");
        }),
        ("Ocean timetable 917: fishing help off at sea, sightings saved and logged, kept by « restore every setting »",() =>
        {
            var fishing=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FishingPanel.cs"));
            Check(fishing.Contains("var atSea=OceanVoyage?.Frame is not null || OceanVoyageData?.Invoke()?.Territories.Contains(now.Territory)==true;")
                && fishing.Contains("now.Job==18 && !atSea"),"usual help off at sea");
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs"));
            Check(panel.Contains("RecordOceanSighting(timetable,Math.Max(0,slot),frame.RouteId);") && panel.Contains("Ocean schedule sighting: slot {0}, loop row {1}"),"sighting recorded and logged");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("territories.Add(fishingSpot.TerritoryType.RowId)") && plugin.Contains("{Territories=territories}"),"territories from the game data");
            Check(TenshoAssistant.ConfigurationReset.Kept.Contains("OceanSightings"),"saved data kept");
        }),
    ];
}
