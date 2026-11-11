// Test audit of 2026-11-09, batch 2 (0.1021): OceanVoyage418Tests, OceanFishing392Tests, OceanSchedule917Tests, OceanPanel918Tests, OceanFixes958Tests, OceanMissions961Tests, OceanIntuition963Tests, OceanPoints964Tests merged into OceanVoyageTests; every case is kept.
using TenshoAssistant.Gathering;
using TenshoAssistant.Discoveries;
internal static class OceanVoyageTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    // Rows of docs/evidence/0.418-ocean-voyage.md: route 1 = spots 2,1,3 at moments 1,2,3; spot 1 = 237 / 238; missions 1 and 4.
    private static OceanVoyageData Data()=>new(
        new Dictionary<uint,(uint[],uint[])>{[1]=([2,1,3],[1,2,3])},
        new Dictionary<uint,OceanSpot>{[1]=new(1,"Galadion",237,238),[2]=new(2,"Merlthor S",239,240),[3]=new(3,"Merlthor N",243,244)},
        new Dictionary<uint,IReadOnlyList<uint>>{[237]=[10,11],[238]=[20],[239]=[30]},
        new Dictionary<uint,OceanMission>{[1]=new("Catch jellyfish or crabs",7),[4]=new("Catch fish with a weak bite (!)",34)});
    private static OceanVoyageFrame Frame(int zone,bool spectral)=>new(1,zone,2,spectral,0,[(1,3),(4,34),(99,1)],1200,9,DateTimeOffset.UnixEpoch);
    private static OceanRoute Route(uint id,OceanTime last)=>new(id,"R"+id,[new("A",OceanTime.Night),new("B",OceanTime.Day),new("C",last)]);
    private static OceanRoute DayRoute(uint id)=>new(id,"R"+id,[new("A",OceanTime.Day)]);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From OceanVoyage418Tests: R-07 (0.418): the ocean fishing voyage in progress — stop, moment, spectral spot, next stop, missions.
        ("Ocean voyage: the stop, its moment and fish, the spectral spot, the next stop",() =>
        {
            var data=Data();
            var second=OceanVoyages.Current(data,Frame(1,false))!;
            Check(second.Spot.Place=="Galadion" && second.Time==OceanTime.Sunset && second.Fish.SequenceEqual([10u,11u]),"stop 2: place, moment row 2 = sunset (0.946), main spot fish");
            Check(second.NextSpot?.Place=="Merlthor N" && second.NextTime==OceanTime.Night,"next stop");
            Check(OceanVoyages.Current(data,Frame(1,true))!.Fish.SequenceEqual([20u]),"spectral current: the sub spot");
            Check(OceanVoyages.Current(data,Frame(2,false)) is { NextSpot:null, Fish.Count:0 },"last stop: no next; unknown spot fish: none");
            Check(OceanVoyages.Current(data,Frame(3,false)) is null && OceanVoyages.Current(data,Frame(0,false) with {RouteId=7}) is null,"out of range or unknown route");
        }),
        ("Ocean voyage: missions with their target, unknown types left out; reader checks the game functions",() =>
        {
            var missions=OceanVoyages.Missions(Data(),Frame(0,false));
            Check(missions.Count==2 && missions[0]==("Catch jellyfish or crabs",3,7) && missions[1].Progress==missions[1].Target,"missions");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/OceanVoyageReader.Dalamud.cs"));
            Check(reader.Contains("MemberFunctionPointers.GetContentDirector==0") && reader.Contains("MemberFunctionPointers.GetInstanceContentDirector==0")
                && reader.Contains("framework.IsInFrameworkUpdateThread") && reader.Contains("zone is < 0 or > 2"),"addresses checked, framework thread, bounds");
        }),
        // From OceanFishing392Tests: 0.392 (g, R-01): ocean fishing timetable — loop slot k leaves at the loop start + 2 h × k, boarding open 15 minutes, route filter,
        // moments of the day (IKDTimeDefine 1 night, 2 day, 3 sunset), a voyage named after its last stop.
        ("Ocean fishing: departures every 2 hours from the loop start, boarding 15 minutes, filter by voyage, loop wraps",() =>
        {
            var routes = new Dictionary<uint,OceanRoute>{[7]=Route(7,OceanTime.Sunset),[10]=Route(10,OceanTime.Sunset),[1]=Route(1,OceanTime.Sunset),[19]=Route(19,OceanTime.Sunset),[13]=Route(13,OceanTime.Sunset)};
            var schedule = new OceanSchedule([(7,19),(10,13),(1,19)],routes);
            Check(OceanSchedule.LoopStart==new DateTimeOffset(1970,1,9,16,0,0,TimeSpan.Zero),"loop start");
            var slot = OceanSchedule.LoopStart+TimeSpan.FromHours(2)*3000;
            var next = schedule.Next(slot+TimeSpan.FromMinutes(5),3);
            Check(next[0].At==slot && next[0].Aldenard!.Id==7 && next[1].At==slot+TimeSpan.FromHours(2) && next[1].Othard!.Id==13,"boarding still open, loop index 3000 % 3 = 0");
            Check(schedule.Next(slot+TimeSpan.FromMinutes(16),1)[0].At==slot+TimeSpan.FromHours(2),"boarding closed after 15 minutes");
            var filtered = schedule.Next(slot+TimeSpan.FromMinutes(16),2,1);
            Check(filtered.All(departure=>departure.Aldenard!.Id==1) && filtered[0].At==slot+TimeSpan.FromHours(4) && filtered[1].At==slot+TimeSpan.FromHours(10),"filter by voyage");
            Check(OceanSchedule.TimeOf(1)==OceanTime.Day && OceanSchedule.TimeOf(2)==OceanTime.Sunset && OceanSchedule.TimeOf(3)==OceanTime.Night && OceanSchedule.TimeOf(9)==OceanTime.Unknown,"moments");
            Check(routes[7].Label==OceanTime.Sunset,"named after the last stop");
        }),
        // From OceanSchedule917Tests: X-85 V1 (0.917): the shift of each port is proven by the departures seen aboard, never guessed; a contradicted port shows the routes
        // still possible; the usual fishing help is off at sea; the sightings are saved data.
        ("Ocean timetable 917: port of a route, shifts kept by the sightings, verified / unverified / contradicted, possible routes",() =>
        {
            var routes=new uint[]{1,2,3,11,12,13}.ToDictionary(id=>id,DayRoute);
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
            Check(plugin.Contains("territories.Add(fishingSpot.TerritoryType.RowId)") && plugin.Contains("{Territories=territories,"),"territories from the game data");
            Check(TenshoAssistant.ConfigurationReset.Kept.Contains("OceanSightings"),"saved data kept");
        }),
        // From OceanPanel918Tests: X-85 V2 (0.918): the ocean voyage panel draws its fish as the usual fishing help (icons, bait in hand, bite, bite time of the spot)
        // and shows the spectral current's fish of the stop, folded, while the current is not there.
        ("Ocean panel 918: fish table like the fishing help, spectral fish of the stop, stopwatch, keys in both languages",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs"));
            Check(panel.Contains("private void DrawOceanFishTable(string id,IReadOnlyList<uint> fishList,uint spotId,uint? inHand,int? elapsed"),"shared table");
            Check(panel.Contains("var spotId=stop.Spectral ? stop.Spot.SpectralSpot:stop.Spot.MainSpot;") && panel.Contains("biteTimes?.Get(spotId)"),"bite times of the spot fished");
            Check(panel.Contains("DrawOceanFishTable(\"OceanSpectralFish\",spectralFish,stop.Spot.SpectralSpot,inHand,null,") && panel.Contains("if(!stop.Spectral && stop.Spot.SpectralSpot!=0"),"spectral fish folded outside the current");
            Check(panel.Contains("inHand==row.Route[0]") && panel.Contains("ItemCell(fish,fishId,22"),"icons and bait in hand");
            Check(panel.Contains("oceanFishingSince=DateTimeOffset.UtcNow"),"stopwatch");
            foreach(var lang in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{lang}.json"));
                Check(text.Contains("\"SpectralFish\": ") && text.Contains("\"SpectralFishTip\": ") && text.Contains("\"BiteTime\": "),"keys "+lang);
            }
        }),
        // From OceanFixes958Tests: X-98 (0.958): bite times passed in red, mission progress capped, points shown only when plausible, the 16:00 departure as a proven sighting.
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
        // From OceanMissions961Tests: X-85 V3 (0.961): mission criteria from the game's English texts; a fish counts for a mission not done yet by species or bite.
        ("Ocean missions 961: species words from the bonuses, missions parsed, fish counted until the mission is done",() =>
        {
            var words=OceanMissionRules.SpeciesWords([(14,"Requirement: As a party, catch 200 or more sharks."),(42,"Requirement: As an individual, catch 50 or more shrimp."),
                (41,"Requirement: As a party, catch 400 or more squid."),(56,"Requirement: As an individual, catch 50 or more mantis shrimp."),(55,"Requirement: As a party, catch 300 or more prehistoric wavekin."),(1,"Requirement: Score 2,500 points or higher.")]);
            Check(words["sharks"]==14 && words["mantis shrimp"]==56 && words["prehistoric wavekin"]==55 && !words.ContainsKey("points"),"words");
            var squid=OceanMissionRules.Parse("Catch shrimp or squid",words)!;
            Check(squid.Bonuses.SetEquals([42u,41u]) && squid.Bite is null,"species");
            Check(OceanMissionRules.Parse("Catch mantis shrimp or prehistoric wavekin",words)!.Bonuses.SetEquals([56u,55u]),"two-word species");
            Check(OceanMissionRules.Parse("Catch fish with a ferocious bite (!!!)",words)!.Bite=="Legendary" && OceanMissionRules.Parse("Catch fish with a strong bite (!!)",words)!.Bite=="Strong"
                && OceanMissionRules.Parse("Catch fish with a weak bite (!)",words)!.Bite=="Weak","bites");
            Check(OceanMissionRules.Parse("Catch fish rated ★★★ or higher",words)!.MinStars==3 && OceanMissionRules.Parse("●★★★★を3匹釣る。",words) is null && OceanMissionRules.Parse("Catch dragons",words) is null,"stars and unknown");
            var bonuses=new Dictionary<uint,uint[]>{[100]=[41],[101]=[14]};
            var missions=new[]{("Squid",3,19,(OceanMissionCriterion?)squid),("Ferocious",2,7,OceanMissionRules.Parse("Catch fish with a ferocious bite (!!!)",words)),("Stars",1,5,OceanMissionRules.Parse("Catch fish rated ★★★ or higher",words))};
            Check(OceanMissionRules.CountsFor(100,bonuses,"Weak",missions).SequenceEqual(["Squid"]),"species counts");
            Check(OceanMissionRules.CountsFor(101,bonuses,"Legendary",missions).SequenceEqual(["Ferocious"]),"bite counts, stars left out");
            var done=new[]{("Squid",19,19,(OceanMissionCriterion?)squid)};
            Check(OceanMissionRules.CountsFor(100,bonuses,null,done).Count==0,"done mission not marked");
        }),
        // From OceanIntuition963Tests: X-85 V5 (0.963): ocean fish that need Fisher's Intuition are marked, with the predators to catch first.
        ("Ocean intuition 963: eye mark and predators in the tooltip",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs"));
            Check(panel.Contains("gatherBuddyFish?.State.Data?.Find(fishId) is { Predators.Count:>0 } needs") && panel.Contains("FontAwesomeIcon.Eye"),"marked");
            Check(panel.Contains("localization.Format(\"FishingPanel.Predators\""),"predators");
        }),
        // From OceanPoints964Tests: X-85 V2 and V6 (0.964): points of each catch from the stop's FishData, noted and shown; the voyage's scrips logged at the end for proof.
        ("Ocean points 964: catch points from the zone data, kind, panel, result log",() =>
        {
            Check((int)DiscoveryKind.FishPoints==36,"kind");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/OceanVoyageReader.Dalamud.cs"));
            Check(reader.Contains("if(nq+hq==was.Count+1 && points>was.Points)CatchPoints?.Invoke(item,points-was.Points);"),"one catch, its points");
            Check(reader.Contains("Ocean voyage result: route {0}, points {1}, experience {2}, scrip1 {3}, scrip2 {4};"),"result logged");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs")).Contains("Find(Discoveries.DiscoveryKind.FishPoints,fishId,1)"),"panel");
        }),
    ];
}
